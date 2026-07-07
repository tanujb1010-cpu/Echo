using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Self-Turret (#22): a repeated recorded interaction makes an Echo a rhythmic emitter. Whoever holds
    /// Interact inside an emitter's control zone this tick arms it "firing" for the FOLLOWING tick's kill
    /// sweep — a deliberate one-tick lag, the same latency every other "hold to actuate" module in this
    /// codebase already has (module solids/sensors are evaluated once per tick, before any character's
    /// OnCharacterStep can react — see GeneratorCrankModule for the identical pattern). Since an Echo's
    /// recorded Interact presses replay bit-identically, whatever rhythm it pressed during recording plays
    /// back as an exact, dodgeable firing pattern on every future run.
    /// </summary>
    public sealed class SelfTurretModule : ILevelModule
    {
        private struct Emitter { public Fix64Vec2 ControlMin, ControlMax; public Fix64Vec2 BlastMin, BlastMax; public bool Firing; }

        private readonly List<Emitter> _emitters = new List<Emitter>();
        private bool _clearedThisTick;

        public int KillsThisRun { get; private set; }

        public void AddEmitter(Fix64Vec2 controlMin, Fix64Vec2 controlMax, Fix64Vec2 blastMin, Fix64Vec2 blastMax)
            => _emitters.Add(new Emitter { ControlMin = controlMin, ControlMax = controlMax, BlastMin = blastMin, BlastMax = blastMax, Firing = false });

        public bool IsFiring(int index) => _emitters[index].Firing;
        public int EmitterCount => _emitters.Count;

        /// <summary>Read-only telegraph data for the presentation layer (never mutates sim state).</summary>
        public void GetBlastBounds(int index, out Fix64Vec2 min, out Fix64Vec2 max)
        {
            min = _emitters[index].BlastMin;
            max = _emitters[index].BlastMax;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _emitters.Count; i++)
            {
                Emitter e = _emitters[i];
                if (!e.Firing) continue;
                for (int b = 0; b < allBodies.Count; b++)
                {
                    SimEntity body = allBodies[b];
                    if (!body.Active) continue;
                    if (body.MaxX > e.BlastMin.X && body.MinX < e.BlastMax.X && body.MaxY > e.BlastMin.Y && body.MinY < e.BlastMax.Y)
                    {
                        body.Active = false;
                        KillsThisRun++;
                    }
                }
            }
            _clearedThisTick = false;
        }

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            if (!_clearedThisTick)
            {
                for (int i = 0; i < _emitters.Count; i++)
                {
                    Emitter e = _emitters[i];
                    e.Firing = false;
                    _emitters[i] = e;
                }
                _clearedThisTick = true;
            }

            if (!cmd.Has(InputButtons.Interact)) return;
            for (int i = 0; i < _emitters.Count; i++)
            {
                Emitter e = _emitters[i];
                if (character.MaxX > e.ControlMin.X && character.MinX < e.ControlMax.X
                    && character.MaxY > e.ControlMin.Y && character.MinY < e.ControlMax.Y)
                {
                    e.Firing = true;
                    _emitters[i] = e;
                }
            }
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _emitters.Count; i++) h.Add(_emitters[i].Firing);
            h.Add(KillsThisRun);
        }

        public void ResetModule()
        {
            KillsThisRun = 0;
            _clearedThisTick = false;
            for (int i = 0; i < _emitters.Count; i++)
            {
                Emitter e = _emitters[i];
                e.Firing = false;
                _emitters[i] = e;
            }
        }
    }
}
