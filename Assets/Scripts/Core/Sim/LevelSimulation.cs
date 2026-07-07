using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Echo;
using Echo.Core.Replay;
using Echo.Infra;

namespace Echo.Core.Sim
{
    /// <summary>
    /// The deterministic heart of a level: runs the live player + every banked Echo through one
    /// identical kinematic pipeline each fixed tick, records the live run, and respawns the whole
    /// braid on restart. Pure C# (no UnityEngine) so it runs headless in tests (docs/06 §3).
    ///
    /// THE KEY INVARIANT (why determinism survives a growing braid, docs/05 §1):
    ///   On restart, all runs replay from tick 0 simultaneously, and run K only collides with runs
    ///   0..K-1 — exactly the braid that existed when run K was recorded. Reproducing each run's
    ///   original collision environment is what keeps replays bit-identical while still letting you
    ///   stand on, and be blocked by, your past selves.
    /// </summary>
    public sealed class LevelSimulation
    {
        private sealed class EchoActor
        {
            public SimEntity Body;
            public LocomotionState Loco;
            public EchoBrain Brain;
            public bool PrevActive; // tracks active→inactive transitions (a hazard sacrifice) for events
        }

        /// <summary>Optional semantic-event sink (sacrifice/prune). Decouples narrative/achievements from the sim.</summary>
        public EventBus Events;

        public ICollisionWorld World { get; private set; }
        public MotorTuning Tuning;
        public ulong SaveSeed { get; private set; }
        public string LevelId { get; private set; }
        public GateMask EnabledGates { get; private set; }
        public Fix64Vec2 Spawn { get; private set; }
        public int Tick { get; private set; }

        private readonly List<Timeline> _bankedRuns = new List<Timeline>(8);
        private readonly List<EchoActor> _echoes = new List<EchoActor>(8);
        private readonly List<SimEntity> _solidsScratch = new List<SimEntity>(8); // reused → zero-GC
        private readonly List<SimEntity> _allBodies = new List<SimEntity>(8);       // reused → zero-GC
        private readonly List<SimEntity> _moduleSolids = new List<SimEntity>(8);    // reused → zero-GC
        private readonly List<ILevelModule> _modules = new List<ILevelModule>(4);   // plates/doors/hazards…

        private readonly ObjectPool<SimEntity> _bodyPool;
        private readonly ObjectPool<LocomotionState> _locoPool;
        private readonly ObjectPool<EchoBrain> _brainPool;
        private readonly ObjectPool<EchoActor> _actorPool;

        private SimEntity _playerBody;
        private LocomotionState _playerLoco;
        private readonly Recorder _recorder = new Recorder();
        private int _nextRunId;
        private int _nextCloneId = 50000; // cloned runs (Resonance #44) get ids out of the live-run range
        private readonly Dictionary<int, int> _runDelays = new Dictionary<int, int>();    // Echo-Delay (#E1)
        private readonly Dictionary<int, bool> _runReversed = new Dictionary<int, bool>(); // Reverse-Replay (#E16)
        private int _lastRestEchoRunId = -1; // edge-detects a fresh landing for Trust (#Reliance)

        private static readonly Fix64 RestEps = Fix64.FromFloat(0.06f);      // "resting exactly on top" tolerance
        private static readonly Fix64 TrustPerLanding = Fix64.FromFloat(0.05f);
        private static readonly Fix64 GrievancePerSacrifice = Fix64.FromFloat(0.15f);

        private readonly int _maxEchoes;

        public LevelSimulation(int maxEchoes)
        {
            _maxEchoes = maxEchoes;
            _bodyPool = new ObjectPool<SimEntity>(() => new SimEntity(), maxEchoes + 1);
            _locoPool = new ObjectPool<LocomotionState>(() => new LocomotionState(), maxEchoes + 1);
            _brainPool = new ObjectPool<EchoBrain>(() => new EchoBrain(), maxEchoes);
            _actorPool = new ObjectPool<EchoActor>(() => new EchoActor(), maxEchoes);
        }

        /// <summary>Echo Budget (#26): the hard cap this level was configured with.</summary>
        public int MaxEchoes => _maxEchoes;
        public int EchoCount => _echoes.Count;
        public Fix64Vec2 PlayerPosition => _playerBody.Position;
        public bool DebugPlayerGrounded => _playerBody != null && _playerBody.Grounded;

        /// <summary>True once the live player has entered a hazard this run (caller should bank+restart).</summary>
        public bool PlayerDead => _playerBody != null && !_playerBody.Active;

        // --- Undo / prune (docs/05 §6 undo): drop a banked run before the next restart re-derives state. ---
        public int BankedRunCount => _bankedRuns.Count;
        public void PruneBankedRun(int index)
        {
            if (index < 0 || index >= _bankedRuns.Count) return;
            Events?.Publish(new EchoPrunedEvent(_bankedRuns[index].RunId));
            _bankedRuns.RemoveAt(index);
        }

        /// <summary>Resonance Plates (#44): duplicate a banked run into N additional identical Echoes.</summary>
        public void CloneBankedRun(int index, int copies = 1)
        {
            if (index < 0 || index >= _bankedRuns.Count) return;
            for (int i = 0; i < copies; i++)
                _bankedRuns.Add(_bankedRuns[index].CloneWithRunId(_nextCloneId++));
        }
        public Fix64Vec2 DebugEchoPosition(int index) => _echoes[index].Body.Position;
        public EchoBrain DebugEchoBrain(int index) => _echoes[index].Brain;
        public bool DebugEchoActive(int index) => _echoes[index].Body.Active;

        /// <summary>
        /// Self-Negotiation (#50): lets a module (e.g. one gating a "will this Echo cooperate?" check) read
        /// a specific Echo's current relationship state by its body id, without the module needing its own
        /// coupling to <see cref="EchoBrain"/>/<see cref="_echoes"/> internals. Returns Fix64.Zero for a
        /// body id that isn't a currently-active Echo (e.g. the live player, or an Echo that hasn't spawned).
        /// </summary>
        public Fix64 TrustForBodyId(int bodyId)
        {
            for (int i = 0; i < _echoes.Count; i++)
                if (_echoes[i].Body.Id == bodyId) return _echoes[i].Brain.Drives.Attachment;
            return Fix64.Zero;
        }

        /// <summary>
        /// Caretaker Drones (Content Bible D19): lets a module read a specific Echo's current Salience by
        /// body id — the same "expose relationship/AI state without a module owning EchoBrain internals"
        /// pattern as <see cref="TrustForBodyId"/>. Returns Fix64.Zero for a body id that isn't a currently
        /// active Echo (e.g. the live player).
        /// </summary>
        public Fix64 SalienceForBodyId(int bodyId)
        {
            for (int i = 0; i < _echoes.Count; i++)
                if (_echoes[i].Body.Id == bodyId) return _echoes[i].Brain.Salience.Salience;
            return Fix64.Zero;
        }
        public IReadOnlyList<EchoBrain> EchoBrains
        {
            get { var l = new List<EchoBrain>(_echoes.Count); foreach (var a in _echoes) l.Add(a.Brain); return l; }
        }

        /// <summary>Read-only render snapshot of a body (consumed by the presentation/interpolation layer).</summary>
        public readonly struct BodyRender
        {
            public readonly int Key;          // player = int.MinValue; echoes = runId
            public readonly Fix64Vec2 Position;
            public readonly int Facing;
            public readonly bool IsPlayer;
            public readonly Trait Trait;
            public readonly Fix64 Salience;
            public readonly GateType LastGate; // for telegraph/expression cues
            public BodyRender(int key, Fix64Vec2 pos, int facing, bool isPlayer, Trait trait, Fix64 salience, GateType lastGate)
            { Key = key; Position = pos; Facing = facing; IsPlayer = isPlayer; Trait = trait; Salience = salience; LastGate = lastGate; }
        }

        public const int PlayerKey = int.MinValue;

        /// <summary>Fill <paramref name="into"/> with the current render state of every body (reused list → no per-frame GC).</summary>
        public void CollectRender(List<BodyRender> into)
        {
            into.Clear();
            for (int k = 0; k < _echoes.Count; k++)
            {
                EchoActor a = _echoes[k];
                if (!a.Body.Active) continue; // dead/sacrificed Echo → no view
                into.Add(new BodyRender(a.Body.Id, a.Body.Position, a.Body.FacingSign, false,
                    a.Brain.CurrentTrait, a.Brain.Salience.Salience, a.Brain.LastDecision.Type));
            }
            if (_playerBody != null && _playerBody.Active)
                into.Add(new BodyRender(PlayerKey, _playerBody.Position, _playerBody.FacingSign, true,
                    Trait.Devoted, Fix64.Zero, GateType.None));
        }

        public void Configure(ICollisionWorld world, MotorTuning tuning, ulong saveSeed,
            string levelId, Fix64Vec2 spawn, GateMask enabledGates)
        {
            World = world; Tuning = tuning; SaveSeed = saveSeed;
            LevelId = levelId; Spawn = spawn; EnabledGates = enabledGates;
        }

        /// <summary>Register a gameplay mechanic (plates/doors, hazards, …). Authored by the LevelDefinition.</summary>
        public void AddModule(ILevelModule module)
        {
            _modules.Add(module);
            if (module is ISimAware aware) aware.SetSimulation(this);
        }

        /// <summary>Echo-Delay (#E1): the Echo for this run replays offset by <paramref name="ticks"/> ticks.</summary>
        public void SetRunDelay(int runId, int ticks) => _runDelays[runId] = ticks;

        /// <summary>Reverse-Replay (#E16): the Echo for this run replays its timeline backwards.</summary>
        public void SetRunReversed(int runId, bool reversed) => _runReversed[runId] = reversed;

        /// <summary>Conductor / Phase-Shift: nudge every currently-spawned Echo's playhead by the same
        /// <paramref name="deltaTicks"/> at once — a live, whole-braid resync the player can trigger mid-run
        /// (e.g., to fix a timing mismatch without re-recording). Positive skips ahead, negative rewinds.</summary>
        public void PhaseShiftBraid(int deltaTicks)
        {
            for (int k = 0; k < _echoes.Count; k++) _echoes[k].Brain.PhaseShift(deltaTicks);
        }

        /// <summary>Pause-Self: freeze/resume one Echo's playhead (identified by its banked run id) in place.</summary>
        public void SetEchoPaused(int runId, bool paused)
        {
            for (int k = 0; k < _echoes.Count; k++)
                if (_echoes[k].Brain.RunId == runId) { _echoes[k].Brain.SetPaused(paused); return; }
        }

        /// <summary>Start a fresh level: no Echoes yet, just the live player recording run 0.</summary>
        public void BeginLevel()
        {
            DespawnAll();
            _bankedRuns.Clear();
            _runDelays.Clear();
            _runReversed.Clear();
            _nextRunId = 0;
            _nextCloneId = 50000;
            Restart(bankCurrent: false);
        }

        /// <summary>True iff banking one more run would exceed this level's Echo Budget (#26).</summary>
        public bool AtEchoBudget => _bankedRuns.Count >= _maxEchoes;

        /// <summary>The live run's in-progress recording — read at completion, this IS the winning run.</summary>
        public Timeline CurrentRecording => _recorder.Current;

        /// <summary>
        /// Presentation: every module-owned body that exists right now — closed doors, extended pistons,
        /// crates, drones, elevator platforms. Read-only; lets a view draw the interactive world without
        /// knowing any module's type. Doors drop out of <paramref name="solids"/> the tick they open.
        /// </summary>
        public void CollectModuleGeometry(List<SimEntity> solids, List<SimEntity> dynamics)
        {
            solids.Clear();
            dynamics.Clear();
            for (int m = 0; m < _modules.Count; m++)
            {
                _modules[m].CollectSolids(solids);
                _modules[m].CollectDynamicBodies(dynamics);
            }
        }

        /// <summary>A banked run's timeline, for persistence (replay theater). Index &lt; <see cref="BankedRunCount"/>.</summary>
        public Timeline BankedRunAt(int index) => _bankedRuns[index];

        /// <summary>
        /// Replay theater / cross-session restore: rebuild the braid from previously banked timelines,
        /// exactly as if those runs had just been played this session. Call after <see cref="BeginLevel"/>;
        /// the next live run gets the runId after the highest restored one, so entity ids and gate-seed
        /// streams reproduce the original session bit-for-bit.
        /// </summary>
        public void RestoreBraid(IReadOnlyList<Timeline> runs)
        {
            _bankedRuns.Clear();
            int maxRunId = -1;
            for (int i = 0; i < runs.Count && i < _maxEchoes; i++)
            {
                _bankedRuns.Add(runs[i]);
                if (runs[i].RunId > maxRunId) maxRunId = runs[i].RunId;
            }
            _nextRunId = maxRunId + 1;
            Restart(bankCurrent: false);
        }

        /// <summary>
        /// Restart the level. Banks the current live run (if any) into an Echo, then respawns the
        /// whole braid from tick 0. Bodies/brains come from pools — zero runtime allocation.
        /// Echo Budget (#26): banking is refused once <see cref="_bankedRuns"/> is already at the
        /// level's MaxEchoes cap — the just-played run is simply discarded (not added as a new Echo),
        /// so a player who wants to keep experimenting must first <see cref="PruneBankedRun"/> one.
        /// </summary>
        public void Restart(bool bankCurrent = true)
        {
            if (bankCurrent && _recorder.IsRecording && _bankedRuns.Count < _maxEchoes)
                _bankedRuns.Add(_recorder.Bank());
            _lastRestEchoRunId = -1;

            DespawnActors();
            Tick = 0;
            for (int i = 0; i < _modules.Count; i++) _modules[i].ResetModule();

            // Respawn one Echo per banked run, in record order (run K will see runs 0..K-1 as solids).
            for (int i = 0; i < _bankedRuns.Count; i++)
                SpawnEcho(_bankedRuns[i]);

            // Respawn the live player as the newest run and begin recording.
            // _nextRunId must advance per live run so every banked run gets a unique runId — this keeps
            // entity Ids (runId+1) and gate-seed streams (keyed on runId) distinct across the braid.
            SpawnPlayer();
            _recorder.Begin(LevelId, _nextRunId, SaveSeed, 60);
            _nextRunId++;
        }

        private void SpawnEcho(Timeline timeline)
        {
            EchoActor a = _actorPool.Get();
            a.Body = _bodyPool.Get();
            a.Loco = _locoPool.Get();
            a.Brain = _brainPool.Get();

            ConfigureBody(a.Body, a.Loco, idForRun: timeline.RunId + 1, solid: true);
            int delay = _runDelays.TryGetValue(timeline.RunId, out int d) ? d : 0;
            bool reversed = _runReversed.TryGetValue(timeline.RunId, out bool rv) && rv;
            a.Brain.Init(timeline, SaveSeed, EnabledGates, delay, reversed);
            _echoes.Add(a);
        }

        private void SpawnPlayer()
        {
            _playerBody = _bodyPool.Get();
            _playerLoco = _locoPool.Get();
            // Live player gets a high id so it sorts last in the hash order; not solid to its own Echoes.
            ConfigureBody(_playerBody, _playerLoco, idForRun: 100000 + _nextRunId, solid: false);
        }

        private void ConfigureBody(SimEntity body, LocomotionState loco, int idForRun, bool solid)
        {
            body.Id = idForRun;
            body.Position = Spawn;
            body.Velocity = Fix64Vec2.Zero;
            body.HalfExtents = SimEntityFactory.CharacterHalfExtents;
            body.Grounded = false;
            body.FacingSign = 1;
            body.SolidToEntities = solid;
            loco.Reset();
            body.Add(loco);
        }

        /// <summary>Advance the whole braid one fixed tick. Returns the deterministic world hash.</summary>
        public ulong Step(in InputCommand liveInput)
        {
            // 0) Gather all character + dynamic (crate) bodies, then run module sensors/hazards.
            //    Reads last-tick positions → a deterministic 1-tick actuation latency.
            _allBodies.Clear();
            for (int k = 0; k < _echoes.Count; k++)
            {
                _echoes[k].PrevActive = _echoes[k].Body.Active;
                if (_echoes[k].Body.Active) _allBodies.Add(_echoes[k].Body);
            }
            if (_playerBody.Active) _allBodies.Add(_playerBody);
            for (int m = 0; m < _modules.Count; m++) _modules[m].CollectDynamicBodies(_allBodies);
            for (int m = 0; m < _modules.Count; m++) _modules[m].Tick(_allBodies); // may kill bodies (hazards)

            // An Echo that just died to a hazard is a sacrifice → raise a semantic event (narrative/achievements)
            // and let it color that Echo's own future replays with a little more Spite (docs/04 §3).
            for (int k = 0; k < _echoes.Count; k++)
                if (_echoes[k].PrevActive && !_echoes[k].Body.Active)
                {
                    Events?.Publish(new EchoSacrificedEvent(_echoes[k].Brain.RunId));
                    _echoes[k].Brain.ApplyGrievance(GrievancePerSacrifice);
                }

            // Cache the module solids (closed doors + free crates) once — same for every character.
            _moduleSolids.Clear();
            for (int m = 0; m < _modules.Count; m++) _modules[m].CollectSolids(_moduleSolids);

            // 1) Echoes, in record order. Each only collides with its predecessors (the invariant)
            //    plus module solids. Dead/sacrificed Echoes are frozen and skipped.
            for (int k = 0; k < _echoes.Count; k++)
            {
                EchoActor a = _echoes[k];
                if (!a.Body.Active) continue;

                _solidsScratch.Clear();
                for (int j = 0; j < k; j++)
                    if (_echoes[j].Body.Active) _solidsScratch.Add(_echoes[j].Body);
                _solidsScratch.AddRange(_moduleSolids);

                InputCommand eff = a.Brain.Step(Tick, nearSibling: _echoes.Count > 1);
                if (Events != null && a.Brain.LastDecision.Diverged)
                    Events.Publish(new EchoDefiedEvent(a.Brain.RunId, a.Brain.CurrentTrait.ToString()));
                CharacterMotor.Step(a.Body, a.Loco, eff, Tuning, World, _solidsScratch);
                for (int m = 0; m < _modules.Count; m++) _modules[m].OnCharacterStep(a.Body, eff);
            }

            // 2) Live player collides with the whole existing braid + module solids. Skip if dead this tick.
            if (_playerBody.Active)
            {
                _solidsScratch.Clear();
                for (int k = 0; k < _echoes.Count; k++)
                    if (_echoes[k].Body.Active) _solidsScratch.Add(_echoes[k].Body);
                _solidsScratch.AddRange(_moduleSolids);
                CharacterMotor.Step(_playerBody, _playerLoco, liveInput, Tuning, World, _solidsScratch);
                for (int m = 0; m < _modules.Count; m++) _modules[m].OnCharacterStep(_playerBody, liveInput);

                // Reliance (#Trust): the player is USING a specific Echo as support (standing on it) right
                // now. Edge-triggered on a fresh landing (not every tick standing still) so it reads as one
                // discrete act of reliance, not a firehose while idling on top of it.
                int restingOn = FindEchoSupporting(_playerBody);
                if (restingOn >= 0 && restingOn != _lastRestEchoRunId)
                {
                    Events?.Publish(new EchoReliedOnEvent(restingOn));
                    for (int k = 0; k < _echoes.Count; k++)
                        if (_echoes[k].Brain.RunId == restingOn) { _echoes[k].Brain.ApplyTrust(TrustPerLanding); break; }
                }
                _lastRestEchoRunId = restingOn;

                // 3) Record the live run (only while alive; death ends the run, banked on restart).
                StateHash playerHash = StateHash.New();
                _playerBody.ContributeHash(ref playerHash);
                _recorder.RecordTick(liveInput, playerHash);
            }

            // 4) Simulate module-owned free dynamic bodies (crate gravity/collision) against everything.
            _solidsScratch.Clear();
            for (int k = 0; k < _echoes.Count; k++)
                if (_echoes[k].Body.Active) _solidsScratch.Add(_echoes[k].Body);
            if (_playerBody.Active) _solidsScratch.Add(_playerBody);
            _solidsScratch.AddRange(_moduleSolids);
            for (int m = 0; m < _modules.Count; m++) _modules[m].StepDynamics(World, _solidsScratch);

            Tick++;
            return ComputeWorldHash();
        }

        /// <summary>RunId of the Echo the player is currently standing on top of, or -1 if none (#Trust).</summary>
        private int FindEchoSupporting(SimEntity player)
        {
            if (!player.Grounded) return -1;
            for (int k = 0; k < _echoes.Count; k++)
            {
                SimEntity e = _echoes[k].Body;
                if (!e.Active) continue;
                bool restingOnTop = Fix64.Abs(player.MinY - e.MaxY) < RestEps
                                    && player.MaxX > e.MinX && player.MinX < e.MaxX;
                if (restingOnTop) return _echoes[k].Brain.RunId;
            }
            return -1;
        }

        /// <summary>Whole-world deterministic hash (echoes in order, then player). Used by the soak test.</summary>
        public ulong ComputeWorldHash()
        {
            StateHash h = StateHash.New();
            for (int k = 0; k < _echoes.Count; k++)
            {
                _echoes[k].Body.ContributeHash(ref h);
                _echoes[k].Brain.ContributeHash(ref h);
            }
            _playerBody?.ContributeHash(ref h);
            for (int m = 0; m < _modules.Count; m++) _modules[m].ContributeHash(ref h);
            return h.Value;
        }

        private void DespawnActors()
        {
            for (int i = 0; i < _echoes.Count; i++)
            {
                EchoActor a = _echoes[i];
                _bodyPool.Release(a.Body);
                _locoPool.Release(a.Loco);
                _brainPool.Release(a.Brain);
                a.Body = null; a.Loco = null; a.Brain = null;
                _actorPool.Release(a);
            }
            _echoes.Clear();

            if (_playerBody != null) { _bodyPool.Release(_playerBody); _playerBody = null; }
            if (_playerLoco != null) { _locoPool.Release(_playerLoco); _playerLoco = null; }
        }

        private void DespawnAll()
        {
            DespawnActors();
            _recorder.Discard();
        }
    }
}
