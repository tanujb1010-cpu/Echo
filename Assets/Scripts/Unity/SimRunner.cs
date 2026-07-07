using System.Collections.Generic;
using UnityEngine;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Player;
using Echo.Infra;

namespace Echo.Unity
{
    /// <summary>
    /// The bridge between the deterministic 60 Hz simulation and Unity rendering. Drives the sim on a
    /// fixed-timestep accumulator (so frame rate never affects determinism), reads device-agnostic input,
    /// pools one <see cref="SimView"/> per body, and interpolates views between ticks for smooth motion
    /// at any refresh rate. Handles the core verb — restart — with a hold-to-confirm.
    ///
    /// The sim is authoritative; this class only *reads* it. Nothing here can desync a replay.
    /// </summary>
    public sealed class SimRunner : MonoBehaviour
    {
        [SerializeField] private LevelDefinition _level;
        [SerializeField] private GameObject _viewPrefab;   // optional; a fallback square is generated if null
        [SerializeField] private float _restartHoldSeconds = 0.4f;

        public const float SimDt = 1f / 60f;
        private const float MaxFrameCatchUp = 0.25f;        // avoid the "spiral of death" on hitches

        private IInputProvider _input;
        private LevelSimulation _sim;
        private ObjectPool<SimView> _viewPool;
        private readonly Dictionary<int, SimView> _views = new Dictionary<int, SimView>();
        private readonly List<LevelSimulation.BodyRender> _render = new List<LevelSimulation.BodyRender>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _toRemove = new List<int>();

        private float _acc;
        private float _restartHeld;
        private Sprite _fallbackSprite;
        private HazardTelegraphView _telegraphs; // null when the level has no telegraphable hazards
        private GeometryRenderer _geometry;      // the visible world (tiles, doors, crates, zone markers)
        private BackgroundDirector _background;  // per-world mood: gradient, haze, ambient motes

        public LevelSimulation Sim => _sim;
        public int EchoCount => _sim?.EchoCount ?? 0;
        public LevelDefinition Level => _level;

        /// <summary>The player body's view transform, if spawned (juice hooks: squash, afterimages).</summary>
        public Transform PlayerViewTransform
            => _views.TryGetValue(LevelSimulation.PlayerKey, out var v) ? v.transform : null;

        /// <summary>All live body views — player and Echoes (juice hook: motion trails for the braid).</summary>
        public void CollectViews(List<SimView> into)
        {
            into.Clear();
            foreach (var kv in _views) into.Add(kv.Value);
        }

        /// <summary>True once the player has reached the level's exit zone. Latched until restart/reload.</summary>
        public bool LevelComplete { get; private set; }

        /// <summary>Raised once, on the tick the player first reaches the exit zone.</summary>
        public event System.Action<SimRunner> OnLevelComplete;

        /// <summary>Raised on any hazard death, just before the run is banked as an Echo (audio/juice hook).</summary>
        public event System.Action<SimRunner> OnPlayerDied;

        /// <summary>Raised on every restart, manual or death-driven (audio/juice hook).</summary>
        public event System.Action<SimRunner> OnRestarted;

        /// <summary>Raised when the player's center first enters a secret zone this session (Truth #docs/02 §8).</summary>
        public event System.Action<int> OnSecretTouched;

        /// <summary>Raised on a grounded jump press (audio hook; coyote-time jumps are close enough to ignore).</summary>
        public event System.Action OnPlayerJumped;

        /// <summary>Raised the tick the player transitions airborne → grounded (juice hook: squash, dust).</summary>
        public event System.Action OnPlayerLanded;

        private readonly HashSet<int> _secretsTouched = new HashSet<int>();
        private bool _prevJumpHeld;
        private bool _prevGrounded;

        /// <summary>Presentation pause (menus). The deterministic sim simply doesn't step; nothing desyncs.</summary>
        public bool Paused { get; set; }

        /// <summary>Assist mode: scales how fast real time feeds the fixed-tick accumulator. The sim still
        /// steps its exact 60 Hz ticks — 0.8 just plays them at 80% speed, so determinism is untouched.</summary>
        public float SimSpeed { get; set; } = 1f;

        /// <summary>Total sim ticks since the level loaded, across restarts (speedrun clock; 60 = 1 s).</summary>
        public int TotalTicks { get; private set; }

        // Replay theater: when set, the "player" is driven by a recorded winning run instead of live input.
        private Timeline _replayRun;
        private List<Timeline> _replayBraid;
        public bool IsReplay => _replayRun != null;

        /// <summary>Must be called while the GameObject is inactive (before Awake) for programmatic loads.</summary>
        public void SetLevel(LevelDefinition def) => _level = def;

        /// <summary>Arm replay playback (call with SetLevel, before Awake): the braid's banked runs are
        /// restored and the final run's recorded inputs replace live input. Deterministic, so the whole
        /// solve reproduces bit-for-bit.</summary>
        public void SetReplay(List<Timeline> bankedRuns, Timeline finalRun)
        {
            _replayBraid = bankedRuns;
            _replayRun = finalRun;
        }

        private void Awake()
        {
            if (!TryGetComponent(out _input))
                _input = gameObject.AddComponent<LegacyInputProvider>();

            if (_level == null) _level = SampleLevels.World1Level1(); // playable on Play with no authoring

            var (world, spawn, modules) = LevelBuilder.Build(_level);
            _sim = new LevelSimulation(_level.MaxEchoes);
            _sim.Configure(world, MotorTuning.Default, (ulong)_level.SaveSeed, _level.LevelId, spawn, _level.EnabledGates);
            foreach (var m in modules) _sim.AddModule(m);
            _sim.BeginLevel();
            if (_replayBraid != null && _replayBraid.Count > 0) _sim.RestoreBraid(_replayBraid);

            var theme = WorldPalette.For(WorldPalette.WorldOf(_level.LevelId));
            _background = BackgroundDirector.Attach(transform, _level, theme);
            _telegraphs = HazardTelegraphView.Attach(transform, modules);
            _geometry = GeometryRenderer.Attach(transform, _level, world, _sim, theme.Tile);
            _viewPool = new ObjectPool<SimView>(CreateView, prewarm: _level.MaxEchoes + 1);
            SyncViews(firstFrame: true);
        }

        private void Update()
        {
            if (Paused || LevelComplete) { _acc = 0f; return; }

            // Restart: hold-to-confirm so a stray tap never wipes your in-progress run. (Disabled in replay.)
            if (!IsReplay && _input.RestartHeld)
            {
                _restartHeld += Time.deltaTime;
                if (_restartHeld >= _restartHoldSeconds) { DoRestart(); _restartHeld = -999f; }
            }
            else _restartHeld = 0f;

            _acc += Mathf.Min(Time.deltaTime, MaxFrameCatchUp) * SimSpeed;
            while (_acc >= SimDt)
            {
                StepSim();
                _acc -= SimDt;
            }
        }

        private void LateUpdate()
        {
            float alpha = Mathf.Clamp01(_acc / SimDt);
            foreach (var kv in _views) kv.Value.Render(alpha, Time.deltaTime);
        }

        private void StepSim()
        {
            InputCommand cmd = IsReplay ? _replayRun.InputAt(_sim.Tick) : InputRouter.ToCommand(_input);

            bool jumpHeld = cmd.Has(InputButtons.Jump);
            if (jumpHeld && !_prevJumpHeld && _sim.DebugPlayerGrounded) OnPlayerJumped?.Invoke();
            _prevJumpHeld = jumpHeld;

            _sim.Step(cmd);
            TotalTicks++;

            bool grounded = _sim.DebugPlayerGrounded;
            if (grounded && !_prevGrounded) OnPlayerLanded?.Invoke();
            _prevGrounded = grounded;

            // Replay safety valve: a recording that somehow fails to re-reach the exit (should be
            // impossible — the sim is deterministic) still ends instead of idling forever.
            if (IsReplay && _sim.Tick > _replayRun.TickCount + 600 && !LevelComplete)
            {
                LevelComplete = true;
                OnLevelComplete?.Invoke(this);
            }

            // Death (hazard) ends the run: bank it as an Echo and respawn — so a sacrifice run becomes
            // an Echo that can clear a one-shot hazard for the next attempt.
            if (_sim.PlayerDead) { OnPlayerDied?.Invoke(this); DoRestart(); return; }

            // Completion: player's center inside the exit zone. Read-only check, so determinism is safe.
            float px = _sim.PlayerPosition.X.ToFloat(), py = _sim.PlayerPosition.Y.ToFloat();
            if (!LevelComplete
                && px > _level.ExitMin.x && px < _level.ExitMax.x && py > _level.ExitMin.y && py < _level.ExitMax.y)
            {
                LevelComplete = true;
                OnLevelComplete?.Invoke(this);
            }

            // Secrets: same read-only pattern; each fires once per loaded level (the flow layer dedupes per profile).
            for (int i = 0; i < _level.Secrets.Count; i++)
            {
                var s = _level.Secrets[i];
                if (_secretsTouched.Contains(s.Id)) continue;
                if (px > s.Min.x && px < s.Max.x && py > s.Min.y && py < s.Max.y)
                {
                    _secretsTouched.Add(s.Id);
                    OnSecretTouched?.Invoke(s.Id);
                }
            }

            _telegraphs?.OnSimTick();
            _geometry?.OnSimTick();
            SyncViews(firstFrame: false);
        }

        /// <summary>True when the most recent restart hit the Echo Budget (#26) and the run was
        /// discarded instead of banked — the flow layer surfaces this, since a silently-vanishing
        /// run reads as "my Echo is broken".</summary>
        public bool LastRestartDiscarded { get; private set; }

        public void DoRestart(bool bankCurrent = true)
        {
            LastRestartDiscarded = bankCurrent && _sim.AtEchoBudget;
            _sim.Restart(bankCurrent);
            _acc = 0f;
            OnRestarted?.Invoke(this);
            _geometry?.OnSimTick();     // doors re-close on restart; reflect it immediately
            SyncViews(firstFrame: true); // teleport all bodies to spawn without interpolation smear
        }

        /// <summary>Pause-menu affordance for the Echo Budget: drop the oldest banked run and restart
        /// WITHOUT banking the run in progress — a deliberate "clear a slot and start clean" action,
        /// distinct from the Tab prune panel (<see cref="PruneTimelineHud"/>), which swaps the current
        /// run into the freed slot instead. Both are intentional and serve different moments: use this
        /// one when the run in progress isn't worth keeping either; use Tab-prune when it is. Because
        /// this one always nets the budget DOWN by one, the flow layer must tell the player so — see
        /// GameFlow's "ECHO PRUNED" toast — or repeated presses read as "my Echoes vanished."</summary>
        public void PruneOldestAndRestart()
        {
            _sim.PruneBankedRun(0);
            DoRestart(bankCurrent: false);
        }

        private void SyncViews(bool firstFrame)
        {
            _sim.CollectRender(_render);
            _seen.Clear();

            for (int i = 0; i < _render.Count; i++)
            {
                LevelSimulation.BodyRender r = _render[i];
                _seen.Add(r.Key);
                bool isNew = !_views.TryGetValue(r.Key, out SimView view);
                if (isNew) { view = _viewPool.Get(); _views[r.Key] = view; }

                var pos = new Vector3(r.Position.X.ToFloat(), r.Position.Y.ToFloat(), r.IsPlayer ? -0.1f : 0f);
                view.PushSnapshot(pos, r.Facing, r.IsPlayer, r.Trait, r.Salience.ToFloat(), r.LastGate, firstFrame || isNew);
            }

            // Release views for bodies that no longer exist (e.g., after a restart re-shuffles the braid).
            _toRemove.Clear();
            foreach (var kv in _views) if (!_seen.Contains(kv.Key)) _toRemove.Add(kv.Key);
            for (int i = 0; i < _toRemove.Count; i++)
            {
                _viewPool.Release(_views[_toRemove[i]]);
                _views.Remove(_toRemove[i]);
            }
        }

        private SimView CreateView()
        {
            GameObject go = _viewPrefab != null
                ? Instantiate(_viewPrefab)
                : BuildFallbackView();
            go.transform.SetParent(transform, worldPositionStays: false);
            if (!go.TryGetComponent(out SimView view)) view = go.AddComponent<SimView>();
            return view;
        }

        private GameObject BuildFallbackView()
        {
            if (_fallbackSprite == null)
            {
                var tex = Texture2D.whiteTexture;
                _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            var go = new GameObject("SimView");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _fallbackSprite;
            go.transform.localScale = new Vector3(0.8f, 0.9f, 1f); // ~body AABB (half 0.4 x 0.45)
            return go;
        }
    }
}
