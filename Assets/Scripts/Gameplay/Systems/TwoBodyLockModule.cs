using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Two-Body Lock (#41): a linked door unlocks only once zone A and zone B have EACH been actively held
    /// (Interact + in-zone) within <c>toleranceTicks</c> ticks of one another — not necessarily the same
    /// tick, but close together, and not a persistent "both pressed once ever" latch either. Each lock
    /// tracks how many ticks have elapsed since zone A was last held and since zone B was last held; every
    /// tick both counters age by one, and whichever zone is freshly held this tick resets its own counter
    /// to zero. If one key sits idle too long past the tolerance window, its counter ages past the
    /// threshold and that attempt has effectively expired — the other zone must be re-struck close enough
    /// to it to ever satisfy the pair. Once both counters are simultaneously within tolerance the lock is
    /// STICKY: it opens its door(s) and never re-locks for the rest of the run.
    ///
    /// Module solids are snapshotted ONCE per tick via <see cref="CollectSolids"/>, called BEFORE any
    /// <see cref="OnCharacterStep"/> runs that tick (docs/05 §1) — so CollectSolids always reflects the
    /// PREVIOUS tick's OnCharacterStep passes, which is expected. The trap is the per-tick aging step:
    /// it must NOT run in <see cref="Tick"/>, because Tick executes before CollectSolids has ever read the
    /// prior tick's result, and aging every lock there would stomp state that hasn't been observed yet.
    /// Instead, exactly like <see cref="GeneratorCrankModule"/>, the aging happens once on the FIRST
    /// OnCharacterStep call of the tick (guarded by a bookkeeping flag reset in Tick), so every character's
    /// pass that tick shares one aged baseline and only fresh holds push a counter back to zero — multiple
    /// characters in the same tick combine instead of clobbering each other.
    /// </summary>
    public sealed class TwoBodyLockModule : ILevelModule
    {
        private const int NeverHeld = int.MaxValue / 2;

        private sealed class Lock
        {
            public int LinkId;
            public Fix64Vec2 AMin, AMax, BMin, BMax;
            public int ToleranceTicks;
            public int TickSinceALastHeld;
            public int TickSinceBLastHeld;
            public bool Unlocked;
        }

        private readonly List<Lock> _locks = new List<Lock>();
        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = 1_090_000;
        private bool _agedThisTick;

        public IReadOnlyList<Door> Doors => _doors;

        public void Configure(int linkId, Fix64Vec2 zoneAMin, Fix64Vec2 zoneAMax, Fix64Vec2 zoneBMin, Fix64Vec2 zoneBMax, int toleranceTicks)
        {
            _locks.Add(new Lock
            {
                LinkId = linkId,
                AMin = zoneAMin,
                AMax = zoneAMax,
                BMin = zoneBMin,
                BMax = zoneBMax,
                ToleranceTicks = toleranceTicks,
                TickSinceALastHeld = NeverHeld,
                TickSinceBLastHeld = NeverHeld,
                Unlocked = false,
            });
        }

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public bool IsUnlocked(int linkId)
        {
            for (int i = 0; i < _locks.Count; i++)
                if (_locks[i].LinkId == linkId)
                    return _locks[i].Unlocked;
            return false;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies) => _agedThisTick = false;

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            if (!_agedThisTick)
            {
                for (int i = 0; i < _locks.Count; i++)
                {
                    Lock l = _locks[i];
                    l.TickSinceALastHeld++;
                    l.TickSinceBLastHeld++;
                }
                _agedThisTick = true;
            }

            bool interacting = cmd.Has(InputButtons.Interact);

            for (int i = 0; i < _locks.Count; i++)
            {
                Lock l = _locks[i];

                if (interacting)
                {
                    if (character.MaxX > l.AMin.X && character.MinX < l.AMax.X
                        && character.MaxY > l.AMin.Y && character.MinY < l.AMax.Y)
                    {
                        l.TickSinceALastHeld = 0;
                    }
                    if (character.MaxX > l.BMin.X && character.MinX < l.BMax.X
                        && character.MaxY > l.BMin.Y && character.MinY < l.BMax.Y)
                    {
                        l.TickSinceBLastHeld = 0;
                    }
                }

                if (!l.Unlocked && l.TickSinceALastHeld <= l.ToleranceTicks && l.TickSinceBLastHeld <= l.ToleranceTicks)
                {
                    l.Unlocked = true;
                    for (int d = 0; d < _doors.Count; d++)
                        if (_doors[d].LinkId == l.LinkId) _doors[d].SetActivated(true);
                }
            }
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            for (int i = 0; i < _locks.Count; i++)
            {
                h.Add(_locks[i].TickSinceALastHeld);
                h.Add(_locks[i].TickSinceBLastHeld);
                h.Add(_locks[i].Unlocked);
            }
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void ResetModule()
        {
            _agedThisTick = false;
            for (int i = 0; i < _locks.Count; i++)
            {
                Lock l = _locks[i];
                l.TickSinceALastHeld = NeverHeld;
                l.TickSinceBLastHeld = NeverHeld;
                l.Unlocked = false;
            }
            for (int i = 0; i < _doors.Count; i++) _doors[i].SetActivated(false);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
