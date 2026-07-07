using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Echo.Core.Replay;
using Echo.Gameplay.Narrative;
using Echo.Infra;
using Echo.Services;

namespace Echo.Unity
{
    /// <summary>
    /// The complete game loop around the simulation: main menu → level select → play → pause /
    /// level-complete → next level → final choice → ending. IMGUI (like <see cref="HudV0"/>) so the whole
    /// game runs from a single empty scene with this component on a GameObject — no prefab/canvas setup.
    ///
    /// Owns progression: levels unlock in <see cref="LevelCatalog"/> order, completion is persisted
    /// through <see cref="ISaveService"/> immediately (plus debounced autosave for stats), and the ending
    /// is resolved by the existing <see cref="EndingResolver"/> over a <see cref="BranchState"/>.
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        private enum Screen { MainMenu, LevelSelect, WorldIntro, Playing, Paused, LevelComplete, FinalChoice, Ending, Settings, Intro, Credits }

        private const int SaveSlot = 0;

        private Screen _screen = Screen.MainMenu;
        private ISaveService _save;
        private SimRunner _runner;
        private AudioDirector _audio;
        private MusicDirector _music;

        // Narrative spine: the sim publishes semantic events (EchoSacrificed, EchoReliedOn, EchoPruned)
        // onto this bus; NarrativeDirector folds them into BranchState; we persist BranchState into the
        // profile's BranchBlock — so Trust/Mercy/Truth are earned across the whole campaign.
        private readonly EventBus _bus = new EventBus();
        private readonly BranchState _branch = new BranchState();
        private NarrativeDirector _director;

        private int _currentIndex;        // index into LevelCatalog.Levels of the level being played
        private int _introWorld;          // world whose intro card is showing
        private int _echoesUsedOnComplete;
        private int _ticksOnComplete;     // speedrun clock at the moment of completion
        private bool _wasBestTime;
        private EndingId _ending;
        private Vector2 _selectScroll;
        private string _toast;            // transient HUD message ("secret found")
        private float _toastUntil;
        private bool _replayView;         // current runner is a replay-theater playback, not a live run
        private bool _hintShown, _hintCharged;
        private float _levelStartTime;    // per-level timing (hint fades), not per-app-launch
        private float _introStart;        // first-launch cinematic clock
        private string _promptText;       // contextual key-prompt: shown once ever, then persisted
        private float _promptUntil;
        private readonly List<WorldHint> _worldHints = new List<WorldHint>(4); // diegetic labels this level
        private int _prevEchoes;          // detects "a new Echo banked" across restarts
        private JuiceDirector _juice;
        private Color _flashCol;          // brief full-screen tint: echo banked / complete / death
        private float _flash;

        private GUIStyle _title, _h1, _body, _btn, _btnLocked, _diegetic;

        private void Awake()
        {
            // Self-sufficient: if the bootstrap didn't register a save service, build the real one here.
            if (GameBootstrap.Services == null || !GameBootstrap.Services.TryGet(out _save))
            {
                _save = new SaveService(new FileSaveBackend(System.IO.Path.Combine(Application.persistentDataPath, "saves")));
                GameBootstrap.Services?.Register<ISaveService>(_save);
            }
            _save.Load(SaveSlot);

            _director = new NarrativeDirector(_bus, _branch);
            _bus.Subscribe<EchoSacrificedEvent>(OnEchoSacrificed);
            HydrateBranchFromProfile();
            RetireStaleRecords();
            ApplySettings();

            _audio = AudioDirector.Attach(gameObject);
            _music = MusicDirector.Attach(gameObject);
            _music.Enabled = _save.Current.Settings.MusicOn;
            _music.PlayMenu();

            // First launch ever: show the ~40 s "what is this game" vignette (skippable any time).
            // Marked seen up front so a crash mid-intro can never loop it.
            if (!_save.Current.SeenPrompts.Contains("intro"))
            {
                _save.Current.SeenPrompts.Add("intro");
                _save.RequestAutosave();
                _introStart = Time.time;
                _screen = Screen.Intro;
            }
        }

        /// <summary>Contextual key-prompt shown once per profile, ever — then persisted as seen.</summary>
        private void PromptOnce(string id, string text, float seconds = 6f)
        {
            if (_save.Current.SeenPrompts.Contains(id)) return;
            _save.Current.SeenPrompts.Add(id);
            _promptText = text;
            _promptUntil = Time.time + seconds;
            _save.RequestAutosave();
        }

        private void OnDestroy()
        {
            _bus.Unsubscribe<EchoSacrificedEvent>(OnEchoSacrificed);
            _director?.Dispose();
        }

        private void OnEchoSacrificed(EchoSacrificedEvent _)
            => _save.Current.Stats.EchoesSacrificed++; // Mercy is handled by NarrativeDirector

        /// <summary>
        /// Banked replay braids are recorded INPUTS — replayed against redesigned geometry they desync
        /// into nonsense, and old best times are meaningless. Retire replays/times earned on an older
        /// <see cref="LevelRevisions"/> number; completion (progression) is always kept.
        /// </summary>
        private void RetireStaleRecords()
        {
            int retired = 0;
            foreach (var kv in _save.Current.Levels)
            {
                var rec = kv.Value;
                if (rec.Revision == LevelRevisions.Of(kv.Key)) continue;
                if (rec.BankedTimelines.Count > 0 || rec.BestTimeTicks != 0) retired++;
                rec.BankedTimelines.Clear();
                rec.BestTimeTicks = 0;
                rec.BestRunCount = int.MaxValue;
                rec.Revision = LevelRevisions.Of(kv.Key);
            }
            if (retired > 0) _save.RequestAutosave();
        }

        private void HydrateBranchFromProfile()
        {
            var br = _save.Current.Branch;
            _branch.Trust = br.Trust;
            _branch.Mercy = br.Mercy;
            _branch.SecretsFound = br.SecretsFound.Count;
        }

        private void SyncBranchToProfile()
        {
            var br = _save.Current.Branch;
            br.Trust = _branch.Trust;
            br.Mercy = _branch.Mercy;
            // SecretsFound ids are appended at discovery time; only the scalars flow back here.
        }

        private void ApplySettings()
        {
            var s = _save.Current.Settings;
            HazardTelegraphView.ReduceMotion = s.ReduceMotion;
            HazardTelegraphView.ColorblindMode = s.ColorblindMode;
            if (_music != null) _music.Enabled = s.MusicOn;
        }

        private void Update()
        {
            (_save as SaveService)?.Pump(Time.deltaTime);

            if (_screen == Screen.Playing && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
            {
                if (_replayView) EndReplay();
                else SetPaused(true);
            }
            else if (_screen == Screen.Paused && Input.GetKeyDown(KeyCode.Escape))
                SetPaused(false);
        }

        private void OnApplicationQuit() => _save.Save();

        // ------------------------------------------------------------------ flow transitions

        private void StartLevel(int index)
        {
            KillRunner();
            _currentIndex = index;
            var entry = LevelCatalog.Levels[index];

            // Show the world intro card the first time this world is entered this profile.
            string introKey = $"intro_w{entry.World}";
            if (!_save.Current.Levels.ContainsKey(introKey))
            {
                _save.Current.Levels[introKey] = new LevelRecord { LevelId = introKey, Completed = true };
                _save.RequestAutosave();
                _introWorld = entry.World;
                _screen = Screen.WorldIntro;
                return; // BeginPlaying() continues from the intro card
            }
            BeginPlaying();
        }

        private void BeginPlaying()
        {
            var entry = LevelCatalog.Levels[_currentIndex];
            var go = new GameObject($"SimRunner_{entry.Id}");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.SetActive(false); // SetLevel must land before Awake
            _runner = go.AddComponent<SimRunner>();
            _runner.SetLevel(entry.Create());
            go.SetActive(true);

            _runner.OnLevelComplete += HandleLevelComplete;
            _runner.OnPlayerDied += _ =>
            {
                _audio.Play(Sfx.Death);
                Flash(new Color(1f, 0.3f, 0.25f), 0.20f);
                PromptOnce("death", "Death ends the run — but the body is recorded, like any restart.");
            };
            _runner.OnRestarted += r =>
            {
                _save.Current.Stats.TotalRestarts++;
                _audio.Play(Sfx.Restart);
                if (r.EchoCount > _prevEchoes) // the just-played run became an Echo — celebrate it
                {
                    _audio.Play(Sfx.EchoSpawn);
                    _toast = $"ECHO BANKED  ({r.EchoCount}/{r.Level.MaxEchoes}) — it will repeat that run";
                    _toastUntil = Time.time + 2.5f;
                    Flash(new Color(0.5f, 0.75f, 1f), 0.30f);
                    _juice?.BurstAtPlayer(14, new Color(0.55f, 0.75f, 1f), 4f);
                    PromptOnce("echo", "The blue self is your last run, replayed exactly — it can press, block, and carry.");
                }
                else if (r.LastRestartDiscarded)
                {
                    _toast = "ECHO BUDGET FULL — that run was NOT kept. Prune an Echo from the pause menu.";
                    _toastUntil = Time.time + 4f;
                }
                // A deliberate prune: budget count DROPPED (freed, not swapped) — the run in progress
                // was intentionally not kept either. Without this, pruning read as silent/broken (a
                // real playtest report): the pip count changing with no explanation on screen.
                else if (r.EchoCount < _prevEchoes)
                {
                    _toast = $"ECHO PRUNED  ({r.EchoCount}/{r.Level.MaxEchoes}) — that run was NOT kept. Play a full one and it'll fill the slot.";
                    _toastUntil = Time.time + 4f;
                }
                _prevEchoes = r.EchoCount;
            };
            _runner.OnPlayerJumped += () => _audio.Play(Sfx.Jump);
            _runner.OnSecretTouched += HandleSecretTouched;
            _juice = JuiceDirector.Attach(_runner);
            _prevEchoes = 0;

            // Hand the sim the narrative bus: from here on, sacrifices/reliance/prunes shape the ending.
            _runner.Sim.Events = _bus;

            _runner.SimSpeed = _save.Current.Settings.AssistMode ? 0.8f : 1f;
            _hintShown = false; _hintCharged = false;
            _levelStartTime = Time.time;

            // Diegetic teaching: object-anchored labels, only until this level is first completed.
            bool completedBefore = _save.Current.Levels.TryGetValue(entry.Id, out var known) && known.Completed;
            if (completedBefore) _worldHints.Clear();
            else DiegeticHints.Collect(_runner.Level, _worldHints);
            PromptOnce("move", "← →  move      SPACE  jump      hold R  restart (your run is recorded)", 8f);

            _music.PlayWorld(entry.World);
            _save.Current.CurrentLevelId = entry.Id;
            _save.RequestAutosave();
            _screen = Screen.Playing;
        }

        private void HandleSecretTouched(int secretId)
        {
            var found = _save.Current.Branch.SecretsFound;
            if (found.Contains(secretId)) return; // once per profile

            found.Add(secretId);
            _bus.Publish(new SecretFoundEvent(secretId)); // → NarrativeDirector → BranchState (Truth)
            _audio.Play(Sfx.EchoSpawn);
            _toast = $"SECRET FOUND  ({found.Count}/{_branch.TotalSecrets})";
            _toastUntil = Time.time + 3f;
            _save.RequestAutosave();
        }

        private void HandleLevelComplete(SimRunner runner)
        {
            _echoesUsedOnComplete = runner.EchoCount;
            _ticksOnComplete = runner.TotalTicks;
            _audio.Play(Sfx.LevelComplete);
            Flash(new Color(0.45f, 1f, 0.6f), 0.28f);

            var entry = LevelCatalog.Levels[_currentIndex];
            if (!_save.Current.Levels.TryGetValue(entry.Id, out var rec))
                _save.Current.Levels[entry.Id] = rec = new LevelRecord { LevelId = entry.Id };
            rec.Completed = true;
            rec.Revision = LevelRevisions.Of(entry.Id); // records below are earned on THIS geometry
            if (_echoesUsedOnComplete < rec.BestRunCount) rec.BestRunCount = _echoesUsedOnComplete;

            // Speedrun clock + replay theater: a new fastest solve banks the whole braid (banked runs in
            // record order, the winning live run last) so it can be replayed bit-for-bit from level select.
            _wasBestTime = rec.BestTimeTicks == 0 || _ticksOnComplete < rec.BestTimeTicks;
            if (_wasBestTime)
            {
                rec.BestTimeTicks = _ticksOnComplete;
                rec.BankedTimelines.Clear();
                var sim = runner.Sim;
                for (int i = 0; i < sim.BankedRunCount; i++)
                    rec.BankedTimelines.Add(TimelineCodec.Encode(sim.BankedRunAt(i)));
                if (sim.CurrentRecording != null)
                    rec.BankedTimelines.Add(TimelineCodec.Encode(sim.CurrentRecording));
            }

            SyncBranchToProfile();
            _save.Save(); // completion is a hard save, not a debounced one

            _screen = LevelCatalog.IsLast(_currentIndex) ? Screen.FinalChoice : Screen.LevelComplete;
        }

        // ------------------------------------------------------------------ replay theater

        private void StartReplay(int index)
        {
            var entry = LevelCatalog.Levels[index];
            if (!_save.Current.Levels.TryGetValue(entry.Id, out var rec) || rec.BankedTimelines.Count == 0) return;

            KillRunner();
            _currentIndex = index;

            var runs = new List<Timeline>(rec.BankedTimelines.Count);
            for (int i = 0; i < rec.BankedTimelines.Count; i++) runs.Add(TimelineCodec.Decode(rec.BankedTimelines[i]));
            Timeline finalRun = runs[runs.Count - 1];
            runs.RemoveAt(runs.Count - 1);

            var go = new GameObject($"Replay_{entry.Id}");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.SetActive(false);
            _runner = go.AddComponent<SimRunner>();
            _runner.SetLevel(entry.Create());
            _runner.SetReplay(runs, finalRun);
            go.SetActive(true);

            // Flavor only: replays get sounds and juice, but never the save/narrative hooks.
            _runner.OnLevelComplete += HandleReplayFinished;
            _runner.OnPlayerDied += _ => _audio.Play(Sfx.Death);
            _runner.OnRestarted += _ => _audio.Play(Sfx.Restart);
            _runner.OnPlayerJumped += () => _audio.Play(Sfx.Jump);
            JuiceDirector.Attach(_runner);

            _replayView = true;
            _music.PlayWorld(entry.World);
            _screen = Screen.Playing;
        }

        private void HandleReplayFinished(SimRunner _) => EndReplay();

        private void EndReplay()
        {
            _replayView = false;
            KillRunner();
            _screen = Screen.LevelSelect;
        }

        private void ChooseEnding(FinalChoice choice)
        {
            _branch.Choice = choice;
            _ending = EndingResolver.Resolve(_branch);
            _audio.Play(Sfx.LevelComplete);
            _music.PlayEnding();
            _screen = Screen.Ending;
        }

        private void SetPaused(bool paused)
        {
            if (_runner != null) _runner.Paused = paused;
            _screen = paused ? Screen.Paused : Screen.Playing;
            _audio.Play(Sfx.Click);
        }

        private void QuitToMenu()
        {
            KillRunner();
            SyncBranchToProfile();
            _save.Save();
            _music.PlayMenu();
            _screen = Screen.MainMenu;
        }

        private void KillRunner()
        {
            if (_runner == null) return;
            _runner.OnLevelComplete -= HandleLevelComplete;
            _runner.OnSecretTouched -= HandleSecretTouched;
            if (_runner.Sim != null) _runner.Sim.Events = null;
            Destroy(_runner.gameObject);
            _runner = null;
        }

        private bool IsUnlocked(int index)
        {
            if (index == 0) return true;
            return _save.Current.Levels.TryGetValue(LevelCatalog.Levels[index - 1].Id, out var prev) && prev.Completed;
        }

        private int FirstIncomplete()
        {
            for (int i = 0; i < LevelCatalog.Count; i++)
                if (!_save.Current.Levels.TryGetValue(LevelCatalog.Levels[i].Id, out var r) || !r.Completed)
                    return i;
            return LevelCatalog.Count - 1;
        }

        // ------------------------------------------------------------------ screens (IMGUI)

        private void OnGUI()
        {
            EnsureStyles();
            switch (_screen)
            {
                case Screen.MainMenu: DrawMainMenu(); break;
                case Screen.LevelSelect: DrawLevelSelect(); break;
                case Screen.WorldIntro: DrawWorldIntro(); break;
                case Screen.Playing: DrawHud(); break;
                case Screen.Paused: DrawPause(); break;
                case Screen.LevelComplete: DrawLevelComplete(); break;
                case Screen.FinalChoice: DrawFinalChoice(); break;
                case Screen.Ending: DrawEnding(); break;
                case Screen.Settings: DrawSettings(); break;
                case Screen.Credits: DrawCredits(); break;
                case Screen.Intro:
                    if (IntroCinematic.Draw(Time.time - _introStart)) _screen = Screen.MainMenu;
                    break;
            }
            DrawFlash();
        }

        /// <summary>One brief full-screen tint over everything: blue = Echo banked, green = level
        /// complete, red = death. Halved under ReduceMotion.</summary>
        private void Flash(Color c, float strength)
        {
            _flashCol = c;
            _flash = HazardTelegraphView.ReduceMotion ? strength * 0.5f : strength;
        }

        private void DrawFlash()
        {
            if (_flash <= 0f) return;
            _flash = Mathf.Max(0f, _flash - Time.deltaTime * 1.2f);
            Color c = _flashCol; c.a = _flash;
            GUI.color = c;
            GUI.DrawTexture(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawMainMenu()
        {
            DrawVignette();
            float cx = UnityEngine.Screen.width / 2f;
            GUI.Label(new Rect(cx - 300, 120, 600, 80), NarrativeContent.GameTitle, _title);
            GUI.Label(new Rect(cx - 300, 200, 600, 30), NarrativeContent.GameSubtitle, _h1);

            float y = 300;
            bool hasProgress = _save.Current.Levels.Count > 0;
            if (Button(new Rect(cx - 120, y, 240, 44), hasProgress ? "CONTINUE" : "BEGIN"))
                StartLevel(FirstIncomplete());
            y += 56;
            if (Button(new Rect(cx - 120, y, 240, 44), "LEVEL SELECT")) _screen = Screen.LevelSelect;
            y += 56;
            if (Button(new Rect(cx - 120, y, 240, 44), "SETTINGS")) _screen = Screen.Settings;
            y += 56;
            if (Button(new Rect(cx - 120, y, 240, 44), "REPLAY INTRO")) { _introStart = Time.time; _screen = Screen.Intro; }
            y += 56;
            if (Button(new Rect(cx - 120, y, 240, 44), "CREDITS")) _screen = Screen.Credits;
            y += 56;
            if (hasProgress && Button(new Rect(cx - 120, y, 240, 44), "WIPE SAVE"))
            {
                _save.DeleteSlot(SaveSlot);
                _save.Load(SaveSlot);
                HydrateBranchFromProfile();
                ApplySettings();
            }
            y += 56;
            if (Button(new Rect(cx - 120, y, 240, 44), "QUIT")) Application.Quit();
        }

        private void DrawSettings()
        {
            DrawVignette();
            float cx = UnityEngine.Screen.width / 2f;
            GUI.Label(new Rect(cx - 300, 100, 600, 40), "SETTINGS", _title);

            var s = _save.Current.Settings;
            float y = 220;
            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 16 };

            s.MusicOn = DrawToggle(cx, ref y, "☑ Music (generative ambient, per world)", "☐ Music (off)", s.MusicOn, toggleStyle);
            s.ReduceMotion = DrawToggle(cx, ref y, "☑ Reduce motion (steady hazard pulses)", "☐ Reduce motion (off)", s.ReduceMotion, toggleStyle);
            bool cvd = DrawToggle(cx, ref y, "☑ Colorblind palette (luminance-coded)", "☐ Colorblind palette (off)", s.ColorblindMode != 0, toggleStyle);
            s.ColorblindMode = cvd ? 1 : 0;
            s.AssistMode = DrawToggle(cx, ref y, "☑ Assist mode (80% speed)", "☐ Assist mode (off)", s.AssistMode, toggleStyle);

            if (Button(new Rect(cx - 120, y + 24, 240, 44), "BACK"))
            {
                ApplySettings();
                _save.RequestAutosave();
                _screen = Screen.MainMenu;
            }
        }

        private void DrawCredits()
        {
            DrawVignette();
            float cx = UnityEngine.Screen.width / 2f;
            GUI.Label(new Rect(cx - 300, 120, 600, 60), NarrativeContent.GameTitle, _title);
            GUI.Label(new Rect(cx - 300, 190, 600, 30), $"v{Application.version} — a puzzle game about the selves you leave behind", _h1);
            GUI.Label(new Rect(cx - 300, 260, 600, 200),
                "Every sound is synthesized at runtime — no audio files.\n" +
                "Every level runs on one deterministic, fixed-point simulation —\n" +
                "the same 60 Hz math on every platform, bit-for-bit.\n\n" +
                "This is a playtest build. If something breaks, confuses, or\n" +
                "delights you, that's exactly the feedback it's built to collect.",
                _body);
            if (Button(new Rect(cx - 120, UnityEngine.Screen.height - 120, 240, 44), "BACK")) _screen = Screen.MainMenu;
        }

        private bool DrawToggle(float cx, ref float y, string labelOn, string labelOff, bool value, GUIStyle style)
        {
            bool now = GUI.Toggle(new Rect(cx - 200, y, 400, 32), value, value ? labelOn : labelOff, style);
            y += 44;
            return now;
        }

        private void DrawLevelSelect()
        {
            DrawVignette();
            float cx = UnityEngine.Screen.width / 2f;
            GUI.Label(new Rect(cx - 300, 40, 600, 40), "SELECT LEVEL", _h1);

            _selectScroll = GUI.BeginScrollView(
                new Rect(cx - 340, 100, 680, UnityEngine.Screen.height - 180),
                _selectScroll, new Rect(0, 0, 640, LevelCatalog.Count * 40 + 6 * 44 + 20));

            float y = 0; int lastWorld = 0;
            for (int i = 0; i < LevelCatalog.Count; i++)
            {
                var e = LevelCatalog.Levels[i];
                if (e.World != lastWorld)
                {
                    lastWorld = e.World;
                    GUI.Label(new Rect(0, y + 8, 640, 30), $"— WORLD {e.World} —", _h1);
                    y += 44;
                }
                bool unlocked = IsUnlocked(i);
                bool done = _save.Current.Levels.TryGetValue(e.Id, out var r) && r.Completed;
                string best = done && r.BestRunCount != int.MaxValue ? $"   best: {r.BestRunCount} echoes" : "";
                if (done && r.BestTimeTicks > 0) best += $" · {FormatTicks(r.BestTimeTicks)}";
                string label = $"{(done ? "✔" : unlocked ? "▶" : "🔒")}  {e.Id}  {e.Title}{best}";
                if (unlocked)
                {
                    bool hasReplay = done && r.BankedTimelines.Count > 0;
                    float w = hasReplay ? 546f : 640f;
                    if (GUI.Button(new Rect(0, y, w, 36), label, _btn)) { GUI.EndScrollView(); StartLevel(i); return; }
                    if (hasReplay && GUI.Button(new Rect(552, y, 88, 36), "WATCH", _btn))
                    { GUI.EndScrollView(); StartReplay(i); return; }
                }
                else GUI.Label(new Rect(0, y + 4, 640, 32), label, _btnLocked);
                y += 40;
            }
            GUI.EndScrollView();

            if (Button(new Rect(cx - 120, UnityEngine.Screen.height - 64, 240, 44), "BACK")) _screen = Screen.MainMenu;
        }

        private void DrawWorldIntro()
        {
            DrawVignette();
            float cx = UnityEngine.Screen.width / 2f;
            GUI.Label(new Rect(cx - 300, 120, 600, 40), $"WORLD {_introWorld}", _h1);
            GUI.Label(new Rect(cx - 300, 180, 600, 320), NarrativeContent.WorldIntro(_introWorld), _body);
            if (Button(new Rect(cx - 120, UnityEngine.Screen.height - 120, 240, 44), "ENTER")) BeginPlaying();
        }

        private void DrawHud()
        {
            if (_runner == null) return;
            var entry = LevelCatalog.Levels[_currentIndex];
            int max = _runner.Level != null ? _runner.Level.MaxEchoes : 6;
            var sb = new StringBuilder();
            for (int i = 0; i < max; i++) sb.Append(i < _runner.EchoCount ? "◉" : "○");
            GUI.Label(new Rect(16, 12, 700, 28), $"{entry.Id}  {entry.Title}    ECHOES {sb}", _h1);
            GUI.Label(new Rect(16, 44, 700, 24),
                _replayView ? "REPLAY — your best solve, reproduced from its recording — Esc to exit"
                            : "Hold R to restart (your run becomes an Echo) — Esc to pause", _body);

            // Goal: what you're trying to accomplish this level.
            GUI.Label(new Rect(16, 72, 700, 24), $"Goal: {LevelGoals.Goal(entry.Id)}", _body);

            // Mechanic primer: the glossary line — what this level's named thing (beacon, crank, winch…)
            // actually DOES and which key drives it. Shown until the level has ever been completed.
            if (!_replayView)
            {
                string primer = MechanicPrimers.Primer(entry.Id);
                bool done = _save.Current.Levels.TryGetValue(entry.Id, out var lrec) && lrec.Completed;
                if (!string.IsNullOrEmpty(primer) && !done)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.8f);
                    GUI.Label(new Rect(16, 96, 900, 24), primer, _body);
                    GUI.color = Color.white;
                }
            }

            // Speedrun clock, top-right. Sim ticks, so it's exact and cheat-proof by construction.
            GUI.Label(new Rect(UnityEngine.Screen.width - 176, 12, 160, 28), FormatTicks(_runner.TotalTicks), _h1);

            if (Time.time < _toastUntil && !string.IsNullOrEmpty(_toast))
                GUI.Label(new Rect(UnityEngine.Screen.width / 2f - 200, 80, 400, 30), _toast, _h1);

            // Echo Budget state: warn while full, because "hold R" quietly stops making Echoes.
            // (y=124: below the mechanic-primer line at 96.)
            if (!_replayView && _runner.Sim != null && _runner.Sim.AtEchoBudget)
                GUI.Label(new Rect(16, 124, 700, 24),
                    "Echo budget FULL — restarting discards the run. Pause (Esc) → PRUNE OLDEST ECHO to free a slot.", _body);

            // Diegetic teaching: short labels floating above the objects themselves (first encounter
            // only — cleared once the level has ever been completed). Gentle pulse; steady when
            // ReduceMotion is on.
            if (!_replayView && _worldHints.Count > 0 && Camera.main != null)
            {
                float a = HazardTelegraphView.ReduceMotion ? 0.75f : 0.6f + 0.2f * Mathf.Sin(Time.time * 2.2f);
                GUI.color = new Color(1f, 1f, 1f, a);
                for (int i = 0; i < _worldHints.Count; i++)
                {
                    Vector3 sp = Camera.main.WorldToScreenPoint(new Vector3(_worldHints[i].Pos.x, _worldHints[i].Pos.y, 0f));
                    GUI.Label(new Rect(sp.x - 180, UnityEngine.Screen.height - sp.y - 22, 360, 44),
                        _worldHints[i].Text, _diegetic);
                }
                GUI.color = Color.white;
            }

            // Contextual key-prompt (once ever, persisted), bottom-center — never a wall of text.
            if (Time.time < _promptUntil && !string.IsNullOrEmpty(_promptText))
            {
                float a = Mathf.Min(1f, (_promptUntil - Time.time) / 0.5f);
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(UnityEngine.Screen.width / 2f - 340, UnityEngine.Screen.height - 90, 680, 40),
                    _promptText, _diegetic);
                GUI.color = Color.white;
            }
        }

        private static string FormatTicks(int ticks)
        {
            int cs = ticks * 100 / 60;                    // centiseconds (60 Hz sim)
            return $"{cs / 6000}:{cs / 100 % 60:00}.{cs % 100:00}";
        }

        private void DrawPause()
        {
            DrawVignette();
            float cx = UnityEngine.Screen.width / 2f;
            GUI.Label(new Rect(cx - 300, 140, 600, 40), "PAUSED", _title);
            float y = 260;
            if (Button(new Rect(cx - 120, y, 240, 44), "RESUME")) SetPaused(false);
            y += 56;
            if (Button(new Rect(cx - 120, y, 240, 44), "RESTART RUN")) { SetPaused(false); _runner.DoRestart(); }
            y += 56;
            if (_runner != null && _runner.Sim != null && _runner.Sim.BankedRunCount > 0)
            {
                if (Button(new Rect(cx - 140, y, 280, 44), $"PRUNE OLDEST ECHO ({_runner.Sim.BankedRunCount} banked)"))
                {
                    SetPaused(false);
                    _runner.PruneOldestAndRestart(); // frees a budget slot; the run in progress is NOT kept
                }
                y += 56;
            }
            if (Button(new Rect(cx - 120, y, 240, 44), _hintShown ? "HIDE HINT" : "HINT"))
            {
                _hintShown = !_hintShown;
                if (_hintShown && !_hintCharged)
                {
                    _hintCharged = true; // one charge per level visit, however often it's re-read
                    _save.Current.Stats.HintsUsed++;
                    _save.RequestAutosave();
                }
            }
            y += 56;
            if (Button(new Rect(cx - 120, y, 240, 44), "QUIT TO MENU")) QuitToMenu();
            if (_hintShown)
                GUI.Label(new Rect(cx - 300, y + 64, 600, 120),
                    HintContent.Hint(LevelCatalog.Levels[_currentIndex].Id), _body);
        }

        private void DrawLevelComplete()
        {
            DrawVignette();
            float cx = UnityEngine.Screen.width / 2f;
            var entry = LevelCatalog.Levels[_currentIndex];
            GUI.Label(new Rect(cx - 300, 140, 600, 40), $"{entry.Title} — COMPLETE", _title);
            GUI.Label(new Rect(cx - 300, 220, 600, 30),
                _echoesUsedOnComplete == 0 ? "Alone. No Echoes needed." : $"It took {_echoesUsedOnComplete} of you.", _h1);
            GUI.Label(new Rect(cx - 300, 254, 600, 26),
                $"{FormatTicks(_ticksOnComplete)}{(_wasBestTime ? "  — NEW BEST (replay saved)" : "")}", _body);
            float y = 300;
            if (Button(new Rect(cx - 120, y, 240, 44), "NEXT LEVEL")) StartLevel(_currentIndex + 1);
            y += 56;
            if (Button(new Rect(cx - 120, y, 240, 44), "LEVEL SELECT")) { KillRunner(); _screen = Screen.LevelSelect; }
        }

        private void DrawFinalChoice()
        {
            DrawVignette();
            float cx = UnityEngine.Screen.width / 2f;
            GUI.Label(new Rect(cx - 340, 60, 680, 110), NarrativeContent.FinalRoomPrompt, _body);

            float y = 180;
            void Offer(FinalChoice c)
            {
                if (Button(new Rect(cx - 280, y, 560, 40), NarrativeContent.ChoiceLabel(c))) ChooseEnding(c);
                y += 48;
            }

            // The base five are always on the console.
            Offer(FinalChoice.Restart);
            Offer(FinalChoice.Release);
            Offer(FinalChoice.Erase);
            Offer(FinalChoice.Merge);
            Offer(FinalChoice.Refuse);

            // Earned inputs (docs/02 §8) — each one appears only if the campaign shaped you toward it.
            if (_branch.Trust >= 0.75f)
            {
                _branch.BefriendFirst(); // enough reliance means First trusts you back
                Offer(FinalChoice.YieldToFirst);
            }
            if (_branch.Mercy >= 0.65f) Offer(FinalChoice.SideCaretaker);
            if (_save.Current.Stats.EchoesSacrificed >= 12) Offer(FinalChoice.YieldToSaboteur);
            if (AllLevelsComplete()) Offer(FinalChoice.WakeOrigin);
            if (_branch.SecretsFound >= _branch.TotalSecrets) Offer(FinalChoice.Ritual);
        }

        private bool AllLevelsComplete()
        {
            for (int i = 0; i < LevelCatalog.Count; i++)
                if (!_save.Current.Levels.TryGetValue(LevelCatalog.Levels[i].Id, out var r) || !r.Completed)
                    return false;
            return true;
        }

        private void DrawEnding()
        {
            DrawVignette();
            float cx = UnityEngine.Screen.width / 2f;
            GUI.Label(new Rect(cx - 300, 100, 600, 40), EndingResolver.Title(_ending).ToUpperInvariant(), _title);
            GUI.Label(new Rect(cx - 300, 180, 600, 340), NarrativeContent.EndingText(_ending), _body);
            if (Button(new Rect(cx - 120, UnityEngine.Screen.height - 100, 240, 44), "TITLE SCREEN")) QuitToMenu();
        }

        // ------------------------------------------------------------------ chrome

        private bool Button(Rect r, string label)
        {
            bool hit = GUI.Button(r, label, _btn);
            if (hit) _audio.Play(Sfx.Click);
            return hit;
        }

        private static void DrawVignette()
        {
            var c = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), Texture2D.whiteTexture);
            GUI.color = c;
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _h1 = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, alignment = TextAnchor.UpperCenter };
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            _btnLocked = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            _btnLocked.normal.textColor = new Color(1f, 1f, 1f, 0.35f);
            _diegetic = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _diegetic.normal.textColor = new Color(0.85f, 0.90f, 1f);
        }
    }
}
