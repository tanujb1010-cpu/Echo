using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Infra;

namespace Echo.Core.Echo
{
    /// <summary>
    /// The full intelligence of one Echo: authoritative replay (Layer A) gated by a deterministic
    /// evolution model (drives → trait → divergence gates), per docs/04. Pooled and reset, never
    /// allocated per restart. Produces the effective <see cref="InputCommand"/> the CharacterMotor runs.
    ///
    /// The expressive Layer B (animation/barks/glow) lives in ExpressionController and only *reads*
    /// this brain's <see cref="LastDecision"/>/<see cref="CurrentTrait"/> — it can never alter the sim.
    /// </summary>
    public sealed class EchoBrain : IPoolable
    {
        public readonly ReplaySource Source = new ReplaySource();
        public readonly DriveModel Drives = new DriveModel();
        public readonly SalienceTracker Salience = new SalienceTracker();

        public int RunId { get; private set; }
        public ulong SaveSeed { get; private set; }
        public GateMask EnabledGates { get; private set; }
        public Trait CurrentTrait { get; private set; }
        public GateDecision LastDecision { get; private set; }

        // Tiny per-tick drive evolution rates (production: TuningProfile per world).
        private static readonly Fix64 AutonomyGrow = Fix64.FromRaw(10);
        private static readonly Fix64 CuriosityGrow = Fix64.FromRaw(6);

        public void Init(Timeline timeline, ulong saveSeed, GateMask enabledGates, int replayDelayTicks = 0, bool reversed = false)
        {
            RunId = timeline.RunId;
            SaveSeed = saveSeed;
            EnabledGates = enabledGates;
            Source.Init(timeline, replayDelayTicks, reversed); // Echo-Delay (#E1) + Reverse-Replay (#E16)
            Salience.Init(timeline.Salience);

            // Seed personality from what the foundry already "remembers" of this run: persisted salience,
            // plus persisted Trust/Grievance (docs/04 §3) accumulated from how the player has treated this
            // specific run's Echo across past restarts — relied-on Echoes lean more Attached, sacrificed/
            // defied ones carry more Spite into their very first tick back.
            Fix64 s = Salience.Salience;
            Drives.Set(
                cur:  s * Fix64.Half,
                aut:  s * Fix64.FromFloat(0.8f),
                att:  DriveModel.Clamp01(s * Fix64.FromFloat(0.4f) + timeline.Trust),
                self: s * Fix64.FromFloat(0.3f),
                spite: DriveModel.Clamp01(timeline.Grievance));
            CurrentTrait = TraitResolver.Resolve(Drives, s);
        }

        /// <summary>
        /// Advance one fixed tick and return the effective input for the motor.
        /// Order matters and is fixed for determinism: evolve → resolve trait → evaluate gate →
        /// advance playhead.
        /// </summary>
        public InputCommand Step(int tick, bool nearSibling)
        {
            // 1) Evolve the personality substrate (deterministic, salience-gated).
            Salience.Tick(nearSibling);
            EvolveDrives();
            CurrentTrait = TraitResolver.Resolve(Drives, Salience.Salience);

            // 2) Pull the recorded action and possibly diverge through a gate.
            InputCommand baseInput = Source.CurrentInput();
            LastDecision = GateEvaluator.Evaluate(
                CurrentTrait, Drives, Salience.Salience, baseInput,
                SaveSeed, RunId, tick, EnabledGates);

            // 3) Advance the playhead for next tick.
            Source.Advance();
            return LastDecision.Output;
        }

        private void EvolveDrives()
        {
            // Repeated existence at high salience breeds autonomy + curiosity (docs/04 §3.1).
            Fix64 s = Salience.Salience;
            Drives.Autonomy = DriveModel.Clamp01(Drives.Autonomy + AutonomyGrow * s);
            Drives.Curiosity = DriveModel.Clamp01(Drives.Curiosity + CuriosityGrow * s);
        }

        /// <summary>Raised externally (e.g., player relied on / sacrificed this Echo) to shape personality.
        /// Persists onto the underlying <see cref="Timeline"/> (not just the live Drives) so the effect
        /// survives this Echo being despawned and re-Init'd on the next restart.</summary>
        public void ApplyTrust(Fix64 amount)
        {
            Drives.Attachment = DriveModel.Clamp01(Drives.Attachment + amount);
            if (Source.Timeline != null) Source.Timeline.Trust = DriveModel.Clamp01(Source.Timeline.Trust + amount);
        }

        public void ApplyGrievance(Fix64 amount)
        {
            Drives.Spite = DriveModel.Clamp01(Drives.Spite + amount);
            if (Source.Timeline != null) Source.Timeline.Grievance = DriveModel.Clamp01(Source.Timeline.Grievance + amount);
        }

        /// <summary>Pause-Self (Content Bible §E): freeze this Echo's playhead — it holds its last pose but
        /// still occupies space (still a solid, still hashed) until resumed.</summary>
        public void SetPaused(bool paused) => Source.SetPaused(paused);

        /// <summary>Conductor / Phase-Shift (Content Bible §E): nudge this Echo's playhead by
        /// <paramref name="deltaTicks"/> ticks instantly — a live mid-run resync, distinct from Echo-Delay
        /// (#E1) which is a fixed offset fixed at spawn time. Safe in either direction: <see cref="ReplaySource"/>
        /// falls back to Idle for any out-of-range playhead rather than throwing.</summary>
        public void PhaseShift(int deltaTicks) => Source.Seek(Source.Playhead + deltaTicks);

        public bool Finished => Source.Finished;

        public void ContributeHash(ref StateHash h)
        {
            h.Add(RunId);
            h.Add(Salience.Salience);
            Drives.ContributeHash(ref h);
            h.Add((int)CurrentTrait);
            h.Add(Source.Playhead);
        }

        public void OnSpawned() { }
        public void OnDespawned()
        {
            Source.Reset();
            Drives.Reset();
            Salience.Salience = Fix64.Zero;
            CurrentTrait = Trait.Devoted;
            LastDecision = default;
        }
    }
}
