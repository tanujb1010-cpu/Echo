using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Bounce pads (mechanic #15 "Apex Bounce" family): a body that lands on a pad is launched upward,
    /// reaching far higher than a normal jump — enough to chain off a stacked Echo or clear tall gaps.
    /// Deterministic: the launch is applied the tick the body is grounded on the pad, so an Echo bounces
    /// on replay exactly as the live run did.
    /// </summary>
    public sealed class BouncePadModule : ILevelModule
    {
        private readonly List<SimEntity> _pads = new List<SimEntity>();
        private int _padId = SimEntityFactory.IdRange.BouncePads;

        private static readonly Fix64 BounceSpeed = Fix64.FromInt(24); // > MotorTuning.JumpSpeed (15)
        private static readonly Fix64 TopEps = Fix64.FromFloat(0.06f);

        public SimEntity AddPad(Fix64Vec2 center, Fix64Vec2 half)
        {
            var body = SimEntityFactory.CreateStaticBody(_padId++, center, half);
            _pads.Add(body);
            return body;
        }

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            if (!character.Grounded) return;
            for (int i = 0; i < _pads.Count; i++)
            {
                SimEntity pad = _pads[i];
                bool horizontallyOver = character.MaxX > pad.MinX && character.MinX < pad.MaxX;
                bool restingOnTop = Fix64.Abs(character.MinY - pad.MaxY) < TopEps;
                if (horizontallyOver && restingOnTop)
                {
                    // Launch next tick: the motor integrates this upward velocity (no jump input needed).
                    character.Velocity = new Fix64Vec2(character.Velocity.X, BounceSpeed);
                    return;
                }
            }
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _pads.Count; i++) into.Add(_pads[i]); // pads are static solids you can land on
        }

        public void ContributeHash(ref StateHash h) { /* pads are static geometry */ }
        public void ResetModule() { /* static */ }
        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void Tick(IReadOnlyList<SimEntity> allBodies) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
