using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Split-Speed Floor (#30): a tile starts solid; the instant any body rests on top of it, a fuse
    /// starts, and once it expires the tile collapses (becomes non-solid) for the rest of the run —
    /// forcing either a fast crossing before the fuse runs out, or a second self left standing on a
    /// separate support while the first one crosses before the shared tile gives way. Re-triggers fresh
    /// every restart (the same "re-spent every run" pattern as a consumable Hazard #42), so a replaying
    /// Echo standing on the tile collapses it again on schedule each time.
    /// </summary>
    public sealed class CrumbleFloorModule : ILevelModule
    {
        private struct Tile { public SimEntity Body; public int FuseTicks; public bool Triggered; public int Countdown; }
        private static readonly Fix64 RestEps = Fix64.FromFloat(0.06f);

        private readonly List<Tile> _tiles = new List<Tile>();
        private int _tileId = 1_060_000;

        public IReadOnlyList<SimEntity> Tiles => GetBodies();

        public SimEntity AddTile(Fix64Vec2 center, Fix64Vec2 halfExtents, int fuseTicks)
        {
            var body = SimEntityFactory.CreateStaticBody(_tileId++, center, halfExtents);
            _tiles.Add(new Tile { Body = body, FuseTicks = fuseTicks, Triggered = false, Countdown = 0 });
            return body;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                Tile t = _tiles[i];
                if (!t.Body.SolidToEntities) continue; // already collapsed this run

                if (!t.Triggered)
                {
                    for (int b = 0; b < allBodies.Count; b++)
                    {
                        SimEntity body = allBodies[b];
                        if (!body.Active) continue;
                        bool restingOnTop = Fix64.Abs(body.MinY - t.Body.MaxY) < RestEps
                                            && body.MaxX > t.Body.MinX && body.MinX < t.Body.MaxX;
                        if (restingOnTop) { t.Triggered = true; t.Countdown = t.FuseTicks; break; }
                    }
                }
                else
                {
                    t.Countdown--;
                    if (t.Countdown <= 0) t.Body.SolidToEntities = false;
                }
                _tiles[i] = t;
            }
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _tiles.Count; i++)
                if (_tiles[i].Body.SolidToEntities) into.Add(_tiles[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                h.Add(_tiles[i].Body.SolidToEntities);
                h.Add(_tiles[i].Triggered);
                h.Add(_tiles[i].Countdown);
            }
        }

        public void ResetModule()
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                Tile t = _tiles[i];
                t.Body.SolidToEntities = true;
                t.Triggered = false;
                t.Countdown = 0;
                _tiles[i] = t;
            }
        }

        private List<SimEntity> GetBodies()
        {
            var list = new List<SimEntity>(_tiles.Count);
            for (int i = 0; i < _tiles.Count; i++) list.Add(_tiles[i].Body);
            return list;
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
