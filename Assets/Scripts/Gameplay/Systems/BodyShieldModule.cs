using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Body Shield (#12): an Echo blocks a laser/projectile with itself so the live player can pass. A
    /// blast zone kills AT MOST ONE body per firing tick, and an Echo occupying the zone is always checked
    /// before the live player — so if an Echo is standing in the beam's path the same tick the live player
    /// also happens to be in it, the Echo "soaks" the hit and the live player is spared entirely.
    /// Distinguishes "Echo" from the player using the id-range convention already established elsewhere
    /// (the live player's id starts at 100,000; every Echo's is its small runId+1), scanning Echoes first.
    ///
    /// A beam may optionally take <c>rechargeTicks</c>: after claiming a victim it must recharge that many
    /// ticks before it can fire again. This is what makes a Body Shield LEVEL solvable — a corridor takes
    /// many ticks to walk, so with an always-firing beam (recharge 0) the Echo's sacrifice only ever bought
    /// one tick. With a recharge, one absorbed shot buys the player a real crossing window. Recharge 0
    /// (the default) preserves the original fires-every-tick semantics exactly.
    /// </summary>
    public sealed class BodyShieldModule : ILevelModule
    {
        private const int PlayerIdBase = 100000;

        private struct Beam { public Fix64Vec2 Min, Max; public int RechargeTicks; public int Cooldown; }
        private readonly List<Beam> _beams = new List<Beam>();

        public int KillsThisRun { get; private set; }

        public void AddBeam(Fix64Vec2 min, Fix64Vec2 max) => AddBeam(min, max, 0);

        public void AddBeam(Fix64Vec2 min, Fix64Vec2 max, int rechargeTicks)
            => _beams.Add(new Beam { Min = min, Max = max, RechargeTicks = rechargeTicks, Cooldown = 0 });

        /// <summary>Read-only telegraph data for the presentation layer.</summary>
        public int BeamCount => _beams.Count;
        public bool IsCharged(int index) => _beams[index].Cooldown <= 0;
        public void GetBeamBounds(int index, out Fix64Vec2 min, out Fix64Vec2 max)
        {
            min = _beams[index].Min;
            max = _beams[index].Max;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _beams.Count; i++)
            {
                Beam beam = _beams[i];

                if (beam.Cooldown > 0)
                {
                    beam.Cooldown--;
                    _beams[i] = beam;
                    continue;
                }

                // Echoes first: whichever Echo is in the beam this tick absorbs it, sparing the player.
                SimEntity victim = FindOverlapping(allBodies, beam, requirePlayer: false, excludePlayer: true);
                if (victim == null)
                    victim = FindOverlapping(allBodies, beam, requirePlayer: true, excludePlayer: false);

                if (victim != null)
                {
                    victim.Active = false;
                    KillsThisRun++;
                    beam.Cooldown = beam.RechargeTicks;
                    _beams[i] = beam;
                }
            }
        }

        private static SimEntity FindOverlapping(IReadOnlyList<SimEntity> bodies, Beam beam, bool requirePlayer, bool excludePlayer)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                SimEntity b = bodies[i];
                if (!b.Active) continue;
                bool isPlayer = b.Id >= PlayerIdBase;
                if (requirePlayer && !isPlayer) continue;
                if (excludePlayer && isPlayer) continue;
                if (b.MaxX > beam.Min.X && b.MinX < beam.Max.X && b.MaxY > beam.Min.Y && b.MinY < beam.Max.Y)
                    return b;
            }
            return null;
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
        public void CollectSolids(List<SimEntity> into) { }
        public void ContributeHash(ref StateHash h)
        {
            h.Add(KillsThisRun);
            for (int i = 0; i < _beams.Count; i++) h.Add(_beams[i].Cooldown);
        }

        public void ResetModule()
        {
            KillsThisRun = 0;
            for (int i = 0; i < _beams.Count; i++)
            {
                Beam b = _beams[i];
                b.Cooldown = 0;
                _beams[i] = b;
            }
        }
    }
}
