using System.Collections.Generic;
using UnityEngine;
using Echo.Core.Sim;

namespace Echo.Unity
{
    /// <summary>
    /// The complete visible world, zero assets. Three layers, all read-only over the sim:
    ///
    ///  1. TILES  — the collision grid (floor, borders, authored solids), diffed every sim tick so
    ///     tile mutations (crumble tiles) appear/disappear live.
    ///  2. BODIES — module-owned geometry via <see cref="LevelSimulation.CollectModuleGeometry"/>:
    ///     closed doors, crates, elevator platforms, pendulums, drones. Pooled quads keyed by entity
    ///     id, re-positioned per tick. A door that opens simply stops being collected and vanishes.
    ///  3. MARKERS — static zone hints from the LevelDefinition (plates, switches, spikes, pads,
    ///     portals, fields…), drawn once. Faint door FRAMES stay visible even when the door is open,
    ///     so you can see where a door lives and that it is currently open.
    ///
    /// Palette: world = slate; doors = steel; crates = tan; lethal = red family; helpful/interactive
    /// = teal/green family; exit = green. Never feeds back into the simulation.
    /// </summary>
    public sealed class GeometryRenderer : MonoBehaviour
    {
        // -- palette ----------------------------------------------------------------------------
        private static readonly Color TileCol = new Color(0.32f, 0.40f, 0.52f, 1f);
        private static readonly Color DoorCol = new Color(0.75f, 0.78f, 0.85f, 0.95f);
        private static readonly Color DoorFrame = new Color(0.75f, 0.78f, 0.85f, 0.16f);
        private static readonly Color CrateCol = new Color(0.78f, 0.62f, 0.38f, 1f);
        private static readonly Color LethalDyn = new Color(0.95f, 0.30f, 0.25f, 1f); // moving killers stay red
        private static readonly Color ExitCol = new Color(0.20f, 0.90f, 0.35f, 0.55f);
        private static readonly Color LethalCol = new Color(0.95f, 0.25f, 0.20f, 0.45f);
        private static readonly Color ZoneCol = new Color(0.30f, 0.85f, 0.75f, 0.28f); // plates/switches/zones you *use*
        private static readonly Color InfoCol = new Color(0.55f, 0.65f, 0.95f, 0.18f); // fields that change rules
        private static readonly Color SecretCol = new Color(1.00f, 0.90f, 0.25f, 0.20f);

        private LevelSimulation _sim;
        private TileCollisionWorld _world;
        private Color _tileCol = TileCol; // per-world tint (WorldPalette); semantic colors stay fixed

        private Sprite _sprite;
        private bool[] _tileShown;                 // last-known solid state per tile
        private SpriteRenderer[] _tileQuads;       // one per tile, activated on demand

        private readonly List<SimEntity> _solids = new List<SimEntity>(32);
        private readonly List<SimEntity> _dynamics = new List<SimEntity>(32);
        private readonly Dictionary<int, SpriteRenderer> _bodyQuads = new Dictionary<int, SpriteRenderer>();
        private readonly HashSet<int> _bodySeen = new HashSet<int>();
        private readonly List<int> _bodyGone = new List<int>();

        public static GeometryRenderer Attach(Transform parent, LevelDefinition level,
                                              ICollisionWorld world, LevelSimulation sim, Color? tileColor = null)
        {
            var go = new GameObject("GeometryRenderer");
            go.transform.SetParent(parent, worldPositionStays: false);
            var view = go.AddComponent<GeometryRenderer>();
            view._sim = sim;
            view._world = world as TileCollisionWorld;
            if (tileColor.HasValue) view._tileCol = tileColor.Value;
            view.BuildStatic(level);
            view.OnSimTick(); // initial state: closed doors etc. visible before the first step
            return view;
        }

        /// <summary>Called once per sim tick by <see cref="SimRunner"/> (and once at attach).</summary>
        public void OnSimTick()
        {
            SyncTiles();
            SyncBodies();
        }

        // ------------------------------------------------------------------ layer 1: tile grid

        private void SyncTiles()
        {
            if (_world == null) return;
            if (_tileShown == null)
            {
                _tileShown = new bool[_world.Width * _world.Height];
                _tileQuads = new SpriteRenderer[_world.Width * _world.Height];
            }

            for (int y = 0; y < _world.Height; y++)
                for (int x = 0; x < _world.Width; x++)
                {
                    int i = y * _world.Width + x;
                    bool solid = _world.IsSolid(x, y);
                    if (solid == _tileShown[i]) continue;
                    _tileShown[i] = solid;

                    if (_tileQuads[i] == null)
                        _tileQuads[i] = Quad(new Vector2(x, y), new Vector2(x + 1, y + 1), _tileCol, z: 2f);
                    _tileQuads[i].gameObject.SetActive(solid);
                }
        }

        // ------------------------------------------------------------------ layer 2: live bodies

        private void SyncBodies()
        {
            if (_sim == null) return;
            _sim.CollectModuleGeometry(_solids, _dynamics);
            _bodySeen.Clear();

            for (int i = 0; i < _solids.Count; i++) SyncBody(_solids[i], DoorCol);
            for (int i = 0; i < _dynamics.Count; i++)
            {
                SimEntity e = _dynamics[i];
                // Semantic-color audit: lethal movers (pendulum weights, decoy hazards, caretaker
                // drones — classified by their module id ranges) must read red, never cargo-tan.
                bool lethal = e != null
                    && ((e.Id >= 1_120_000 && e.Id < 1_140_000) || (e.Id >= 1_160_000 && e.Id < 1_170_000));
                SyncBody(e, lethal ? LethalDyn : CrateCol);
            }

            _bodyGone.Clear();
            foreach (var kv in _bodyQuads) if (!_bodySeen.Contains(kv.Key)) _bodyGone.Add(kv.Key);
            for (int i = 0; i < _bodyGone.Count; i++)
            {
                _bodyQuads[_bodyGone[i]].gameObject.SetActive(false); // pooled; a reopening door reuses it
                _bodySeen.Remove(_bodyGone[i]);
            }
        }

        private void SyncBody(SimEntity e, Color color)
        {
            if (e == null || !e.Active) return;
            _bodySeen.Add(e.Id);
            if (!_bodyQuads.TryGetValue(e.Id, out var sr))
            {
                sr = Quad(Vector2.zero, Vector2.one, color, z: 1.5f);
                _bodyQuads[e.Id] = sr;
            }
            sr.gameObject.SetActive(true);
            sr.color = color;
            var t = sr.transform;
            t.position = new Vector3(e.Position.X.ToFloat(), e.Position.Y.ToFloat(), 1.5f);
            t.localScale = new Vector3(e.HalfExtents.X.ToFloat() * 2f, e.HalfExtents.Y.ToFloat() * 2f, 1f);
        }

        // ------------------------------------------------------------------ layer 3: static markers

        private void BuildStatic(LevelDefinition d)
        {
            // The destination.
            Quad(d.ExitMin, d.ExitMax, ExitCol, z: 3f);

            // Lethal things.
            foreach (var h in d.Hazards) Quad(h.Min, h.Max, LethalCol, z: 2.5f);
            foreach (var p in d.CrusherPistons) Quad(p.Min, p.Max, new Color(LethalCol.r, LethalCol.g, LethalCol.b, 0.18f), z: 2.6f);
            foreach (var t in d.TurretEmitters) { Quad(t.BlastMin, t.BlastMax, new Color(LethalCol.r, LethalCol.g, LethalCol.b, 0.15f), z: 2.6f); Quad(t.ControlMin, t.ControlMax, ZoneCol, z: 2.5f); }
            foreach (var b in d.BodyShieldBeams) Quad(b.Min, b.Max, new Color(0.9f, 0.3f, 0.8f, 0.15f), z: 2.6f);
            foreach (var c in d.DelayedCharges) { Quad(c.ArmMin, c.ArmMax, ZoneCol, z: 2.5f); Quad(c.BlastMin, c.BlastMax, new Color(LethalCol.r, LethalCol.g, LethalCol.b, 0.15f), z: 2.6f); }
            foreach (var n in d.NoTouchZones) Quad(n.Min, n.Max, new Color(0.95f, 0.5f, 0.2f, 0.2f), z: 2.5f);
            foreach (var dh in d.DecoyHazards) Quad(dh.Start - dh.HalfExtents, dh.Start + dh.HalfExtents, LethalCol, z: 2.5f);

            // Things you stand on / press / use.
            foreach (var p in d.Plates) Quad(p.Min, p.Max, ZoneCol, z: 2.5f);
            // 0.35 alpha, not 0.15: players park Echoes by this marker — at 0.15 it was invisible against
            // darker world themes and W2_L2 playtested as "the door never opens" (echoes parked outside).
            foreach (var q in d.Quorums) Quad(q.Min, q.Max, new Color(ZoneCol.r, ZoneCol.g, ZoneCol.b, 0.35f), z: 2.6f);
            foreach (var s in d.Switches) Quad(s.Min, s.Max, ZoneCol, z: 2.5f);
            foreach (var w in d.WindSwitches) Quad(w.Min, w.Max, ZoneCol, z: 2.5f);
            foreach (var b in d.BouncePads) Quad(b.Center - b.HalfExtents, b.Center + b.HalfExtents, new Color(0.4f, 0.95f, 0.55f, 0.6f), z: 2.5f);
            foreach (var c in d.ArrivalCheckpoints) Quad(c.Min, c.Max, ZoneCol, z: 2.5f);
            foreach (var t in d.Torches) Quad(new Vector2(t.Position.x - 0.4f, t.Position.y - 0.4f), new Vector2(t.Position.x + 0.4f, t.Position.y + 0.4f), new Color(1f, 0.7f, 0.25f, 0.75f), z: 2.5f);
            foreach (var m in d.MemoryCheckpoints) Quad(m.Min, m.Max, ZoneCol, z: 2.5f);
            foreach (var l in d.CumulativeLevers) Quad(l.ZoneMin, l.ZoneMax, ZoneCol, z: 2.5f);
            foreach (var k in d.Keyholes) Quad(k.Min, k.Max, ZoneCol, z: 2.5f);
            foreach (var n in d.NegotiationPlates) Quad(n.Min, n.Max, ZoneCol, z: 2.5f);
            foreach (var t in d.TwoBodyLocks) { Quad(t.AMin, t.AMax, ZoneCol, z: 2.5f); Quad(t.BMin, t.BMax, ZoneCol, z: 2.5f); }
            foreach (var p in d.PressurePairs) { Quad(p.AMin, p.AMax, ZoneCol, z: 2.5f); Quad(p.BMin, p.BMax, ZoneCol, z: 2.5f); }
            foreach (var m in d.MassScales) Quad(m.Min, m.Max, ZoneCol, z: 2.5f);
            foreach (var p in d.Pulleys) Quad(p.ZoneMin, p.ZoneMax, ZoneCol, z: 2.5f);
            foreach (var c in d.CrankZones) Quad(c.Min, c.Max, ZoneCol, z: 2.5f);
            foreach (var e in d.Elevators) Quad(e.ZoneMin, e.ZoneMax, new Color(ZoneCol.r, ZoneCol.g, ZoneCol.b, 0.15f), z: 2.6f);
            foreach (var mp in d.MirrorPoints) Quad(mp.Min, mp.Max, ZoneCol, z: 2.5f);
            foreach (var mc in d.MirrorPathCheckpoints) Quad(mc.Min, mc.Max, ZoneCol, z: 2.5f);
            foreach (var mc in d.MirroredPlayerCheckpoints) Quad(mc.Min, mc.Max, ZoneCol, z: 2.5f);

            // Fields that change the rules while you're inside them.
            foreach (var g in d.GravityZones) Quad(g.Min, g.Max, InfoCol, z: 2.7f);
            foreach (var t in d.TimeFields) Quad(t.Min, t.Max, InfoCol, z: 2.7f);
            foreach (var a in d.AntiEchoFields) Quad(a.Min, a.Max, new Color(0.65f, 0.3f, 0.95f, 0.2f), z: 2.7f);
            foreach (var w in d.WindZones) Quad(w.Min, w.Max, new Color(0.35f, 0.75f, 1f, 0.15f), z: 2.7f);
            foreach (var e in d.EchoLadderZones) Quad(e.Min, e.Max, InfoCol, z: 2.7f);
            foreach (var dr in d.Drains) Quad(dr.Min, dr.Max, new Color(0.3f, 0.5f, 0.95f, 0.3f), z: 2.5f);
            foreach (var s in d.Secrets) Quad(s.Min, s.Max, SecretCol, z: 2.7f);

            // Portals: paired, so tint the pair alike.
            foreach (var p in d.Portals)
            {
                Quad(p.AMin, p.AMax, new Color(0.95f, 0.6f, 0.95f, 0.4f), z: 2.5f);
                Quad(p.BMin, p.BMax, new Color(0.95f, 0.6f, 0.95f, 0.4f), z: 2.5f);
            }

            // Door frames: faint, permanent — the live solid fill comes from layer 2 while closed.
            foreach (var x in d.Doors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.SwitchDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.ArrivalDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.TorchDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.PressureDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.MassScaleDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.PulleyDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.CrankDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.ColorDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.MemoryDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.CumulativeLeverDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.EchoPasswordDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.TwoBodyLockDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.NegotiationDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.DominoDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.MirroredPathDoors) Frame(x.Center, x.HalfExtents);
            foreach (var x in d.MirrorRelayDoors) Frame(x.Center, x.HalfExtents);
        }

        private void Frame(Vector2 center, Vector2 half)
            => Quad(center - half, center + half, DoorFrame, z: 2.8f);

        // ------------------------------------------------------------------ quad factory

        private SpriteRenderer Quad(Vector2 min, Vector2 max, Color color, float z)
        {
            if (_sprite == null)
            {
                var tex = Texture2D.whiteTexture;
                _sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            var go = new GameObject("Geo");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.position = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, z);
            go.transform.localScale = new Vector3(Mathf.Max(0.05f, max.x - min.x), Mathf.Max(0.05f, max.y - min.y), 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = color;
            sr.sortingOrder = -1; // behind bodies and telegraphs
            return sr;
        }
    }
}
