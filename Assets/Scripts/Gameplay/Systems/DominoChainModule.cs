using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// "Domino Crew" (#39): a chain of dominoes topples in sequence, not all at once. Only domino 0 needs a
    /// body to physically enter its zone to arm its fuse; every later domino is instead auto-armed the
    /// instant the PREVIOUS domino's fuse expires and it falls — no one has to stand in its zone. Each fall
    /// re-arms the next link with its own fuse duration, so the cascade has a visible, deterministic delay
    /// between links rather than an instant chain-wide trigger. The linked door opens once the last domino
    /// in the chain has fallen.
    /// </summary>
    public sealed class DominoChainModule : ILevelModule
    {
        private struct Domino
        {
            public Fix64Vec2 ZoneMin, ZoneMax;
            public int FallTicks;
            public bool Armed;
            public int Countdown;
            public bool Fallen;
        }

        private const int DoorBodyIdStart = 1_110_000;

        private readonly List<Domino> _dominoes = new List<Domino>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = DoorBodyIdStart;

        public IReadOnlyList<Door> Doors => _doors;

        public bool AllFallen { get; private set; }

        public void AddDomino(Fix64Vec2 zoneMin, Fix64Vec2 zoneMax, int fallTicks)
            => _dominoes.Add(new Domino
            {
                ZoneMin = zoneMin,
                ZoneMax = zoneMax,
                FallTicks = fallTicks,
                Armed = false,
                Countdown = 0,
                Fallen = false
            });

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public bool HasFallen(int dominoIndex) => _dominoes[dominoIndex].Fallen;

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            if (_dominoes.Count > 0)
            {
                Domino first = _dominoes[0];
                if (!first.Armed && !first.Fallen)
                {
                    for (int b = 0; b < allBodies.Count; b++)
                    {
                        SimEntity body = allBodies[b];
                        if (!body.Active) continue;
                        if (body.MaxX > first.ZoneMin.X && body.MinX < first.ZoneMax.X && body.MaxY > first.ZoneMin.Y && body.MinY < first.ZoneMax.Y)
                        {
                            first.Armed = true;
                            first.Countdown = first.FallTicks;
                            break;
                        }
                    }
                    _dominoes[0] = first;
                }
            }

            bool allFallen = _dominoes.Count > 0;
            for (int i = 0; i < _dominoes.Count; i++)
            {
                Domino d = _dominoes[i];
                if (d.Armed && !d.Fallen)
                {
                    d.Countdown--;
                    if (d.Countdown <= 0)
                    {
                        d.Fallen = true;
                        _dominoes[i] = d;

                        if (i + 1 < _dominoes.Count)
                        {
                            Domino next = _dominoes[i + 1];
                            next.Armed = true;
                            next.Countdown = next.FallTicks;
                            _dominoes[i + 1] = next;
                        }
                    }
                    else
                    {
                        _dominoes[i] = d;
                    }
                }

                if (!_dominoes[i].Fallen) allFallen = false;
            }

            AllFallen = allFallen;

            for (int d = 0; d < _doors.Count; d++)
                _doors[d].SetActivated(AllFallen);
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _dominoes.Count; i++)
            {
                h.Add(_dominoes[i].Armed);
                h.Add(_dominoes[i].Countdown);
                h.Add(_dominoes[i].Fallen);
            }
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            AllFallen = false;
            for (int i = 0; i < _dominoes.Count; i++)
            {
                Domino d = _dominoes[i];
                d.Armed = false;
                d.Countdown = 0;
                d.Fallen = false;
                _dominoes[i] = d;
            }
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void OnCharacterStep(SimEntity character, in InputCommand cmd) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
