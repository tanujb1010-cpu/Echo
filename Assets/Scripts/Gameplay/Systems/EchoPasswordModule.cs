using System.Collections.Generic;
using Echo.Core.Determinism;
using Echo.Core.Replay;
using Echo.Core.Sim;
using Echo.Gameplay.Components;

namespace Echo.Gameplay.Systems
{
    /// <summary>
    /// Echo Password (#36): a button combination performed across several runs, one button per run. The
    /// live player enters exactly one button of the target sequence per run by holding it while standing
    /// in the entry zone; state splits into a PERSISTENT index (<see cref="_enteredCount"/>, survives
    /// <see cref="ResetModule"/> the same way Cumulative Lever banks its total there) and a PER-RUN capture
    /// (<see cref="_capturedThisRun"/>/<see cref="_hasCapture"/>, cleared every ResetModule). Banking happens
    /// in ResetModule — the exact hook LevelSimulation.Restart() calls at the end of every run — by comparing
    /// the run's captured button (if any) against the next expected button in the target sequence: a match
    /// advances the index, but like a real combination lock a WRONG button fails the whole attempt and resets
    /// the index to zero rather than half-counting (mirrors Torch Sequence #28 snuffing its whole sequence on
    /// a mis-light). Only the live player's input is ever captured; Echoes replaying old input never
    /// contribute, using the same id-range convention as AntiEchoFieldModule (live player id >= 100,000).
    /// </summary>
    public sealed class EchoPasswordModule : ILevelModule
    {
        private const int PlayerIdBase = 100000; // mirrors LevelSimulation.SpawnPlayer's id scheme

        private Fix64Vec2 _entryZoneMin;
        private Fix64Vec2 _entryZoneMax;
        private readonly List<InputButtons> _targetSequence = new List<InputButtons>();

        private readonly List<Door> _doors = new List<Door>();
        private int _doorBodyId = 1_080_000;

        private int _enteredCount;
        private bool _hasCapture;
        private InputButtons _capturedThisRun;

        public bool Solved => _targetSequence.Count > 0 && _enteredCount >= _targetSequence.Count;
        public int EnteredCount => _enteredCount;
        public IReadOnlyList<Door> Doors => _doors;

        public void Configure(Fix64Vec2 entryZoneMin, Fix64Vec2 entryZoneMax, params InputButtons[] targetSequence)
        {
            _entryZoneMin = entryZoneMin;
            _entryZoneMax = entryZoneMax;
            _targetSequence.Clear();
            if (targetSequence != null) _targetSequence.AddRange(targetSequence);
        }

        public Door AddDoor(int linkId, Fix64Vec2 center, Fix64Vec2 halfExtents)
        {
            var body = SimEntityFactory.CreateStaticBody(_doorBodyId++, center, halfExtents);
            var door = new Door(linkId, body);
            _doors.Add(door);
            return door;
        }

        public void OnCharacterStep(SimEntity character, in InputCommand cmd)
        {
            if (character.Id < PlayerIdBase) return; // Echoes never contribute to password entry
            if (_hasCapture) return; // only the first qualifying tick this run counts
            if (cmd.Buttons == InputButtons.None) return;
            if (character.MaxX <= _entryZoneMin.X || character.MinX >= _entryZoneMax.X ||
                character.MaxY <= _entryZoneMin.Y || character.MinY >= _entryZoneMax.Y) return;

            _hasCapture = true;
            _capturedThisRun = cmd.Buttons;
        }

        public void Tick(IReadOnlyList<SimEntity> allBodies)
        {
            for (int d = 0; d < _doors.Count; d++) _doors[d].SetActivated(Solved);
        }

        public void ResetModule()
        {
            if (_hasCapture && _targetSequence.Count > 0 && _enteredCount < _targetSequence.Count)
            {
                if (_capturedThisRun == _targetSequence[_enteredCount]) _enteredCount++;
                else _enteredCount = 0;
            }

            _hasCapture = false;
            _capturedThisRun = InputButtons.None;

            for (int d = 0; d < _doors.Count; d++) _doors[d].SetActivated(false);
        }

        public void CollectSolids(List<SimEntity> into)
        {
            for (int i = 0; i < _doors.Count; i++)
                if (_doors[i].Body.SolidToEntities) into.Add(_doors[i].Body);
        }

        public void ContributeHash(ref StateHash h)
        {
            h.Add(_enteredCount);
            h.Add(_hasCapture);
            for (int i = 0; i < _doors.Count; i++) _doors[i].ContributeHash(ref h);
        }

        public void CollectDynamicBodies(List<SimEntity> into) { }
        public void StepDynamics(ICollisionWorld world, IReadOnlyList<SimEntity> solids) { }
    }
}
