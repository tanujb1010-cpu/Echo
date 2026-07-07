using System.Collections.Generic;
using UnityEngine;
using Echo.Core.Sim;
using Echo.Gameplay.Systems;

namespace Echo.Unity
{
    /// <summary>
    /// Presentation-only telegraphs for the autonomous timing hazards (docs/07 playtest note: rhythm
    /// hazards need visual countdown hints). Draws one translucent quad per Crusher Piston blast volume
    /// and per Self-Turret blast corridor, colored by live phase:
    ///
    ///   piston extended / turret firing  → hot red (lethal NOW)
    ///   piston about to extend (&lt;0.5 s)  → amber pulse that quickens as the flip approaches
    ///   otherwise                        → faint cool tint (safe, but marked)
    ///
    /// Like <see cref="SimView"/>, this NEVER feeds back into the simulation — it only calls the modules'
    /// read-only telegraph accessors, so it cannot desync a replay.
    /// </summary>
    public sealed class HazardTelegraphView : MonoBehaviour
    {
        private const int WarnTicks = 30; // amber window: last half-second before a piston extends

        /// <summary>Accessibility (SettingsBlock.ReduceMotion): steady warn color instead of pulsing.</summary>
        public static bool ReduceMotion;

        /// <summary>Accessibility (SettingsBlock.ColorblindMode): 0 = hue-coded, 1 = luminance-coded
        /// (bright/high-alpha = lethal, dim/low-alpha = safe — readable under any color vision).</summary>
        public static int ColorblindMode;

        private static readonly Color LethalHue = new Color(0.95f, 0.25f, 0.2f, 0.55f);
        private static readonly Color WarnHue = new Color(0.95f, 0.7f, 0.2f, 0.45f);
        private static readonly Color SafeHue = new Color(0.3f, 0.5f, 0.6f, 0.12f);
        private static readonly Color LethalLum = new Color(1f, 1f, 1f, 0.75f);
        private static readonly Color WarnLum = new Color(1f, 1f, 1f, 0.4f);
        private static readonly Color SafeLum = new Color(0.2f, 0.3f, 0.9f, 0.10f);

        private static Color Lethal => ColorblindMode != 0 ? LethalLum : LethalHue;
        private static Color Warn => ColorblindMode != 0 ? WarnLum : WarnHue;
        private static Color Safe => ColorblindMode != 0 ? SafeLum : SafeHue;

        private struct Quad { public SpriteRenderer Sr; }

        private CrusherPistonModule _pistons;
        private SelfTurretModule _turrets;
        private BodyShieldModule _beams;
        private readonly List<Quad> _pistonQuads = new List<Quad>();
        private readonly List<Quad> _turretQuads = new List<Quad>();
        private readonly List<Quad> _beamQuads = new List<Quad>();
        private Sprite _sprite;

        /// <summary>Scan built modules for telegraphable hazards; no-op (returns null) if none exist.</summary>
        public static HazardTelegraphView Attach(Transform parent, IReadOnlyList<ILevelModule> modules)
        {
            CrusherPistonModule cp = null;
            SelfTurretModule st = null;
            BodyShieldModule bs = null;
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] is CrusherPistonModule p) cp = p;
                if (modules[i] is SelfTurretModule t) st = t;
                if (modules[i] is BodyShieldModule b) bs = b;
            }
            if (cp == null && st == null && bs == null) return null;

            var go = new GameObject("HazardTelegraphs");
            go.transform.SetParent(parent, worldPositionStays: false);
            var view = go.AddComponent<HazardTelegraphView>();
            view.Build(cp, st, bs);
            return view;
        }

        private void Build(CrusherPistonModule pistons, SelfTurretModule turrets, BodyShieldModule beams)
        {
            _pistons = pistons;
            _turrets = turrets;
            _beams = beams;

            if (_pistons != null)
                for (int i = 0; i < _pistons.PistonCount; i++)
                {
                    _pistons.GetPistonBounds(i, out var min, out var max);
                    _pistonQuads.Add(MakeQuad(min.X.ToFloat(), min.Y.ToFloat(), max.X.ToFloat(), max.Y.ToFloat()));
                }

            if (_turrets != null)
                for (int i = 0; i < _turrets.EmitterCount; i++)
                {
                    _turrets.GetBlastBounds(i, out var min, out var max);
                    _turretQuads.Add(MakeQuad(min.X.ToFloat(), min.Y.ToFloat(), max.X.ToFloat(), max.Y.ToFloat()));
                }

            if (_beams != null)
                for (int i = 0; i < _beams.BeamCount; i++)
                {
                    _beams.GetBeamBounds(i, out var min, out var max);
                    _beamQuads.Add(MakeQuad(min.X.ToFloat(), min.Y.ToFloat(), max.X.ToFloat(), max.Y.ToFloat()));
                }
        }

        /// <summary>Called by <see cref="SimRunner"/> once per sim tick (60 Hz), after the sim stepped.</summary>
        public void OnSimTick()
        {
            for (int i = 0; i < _pistonQuads.Count; i++)
            {
                Color c;
                if (_pistons.IsExtended(i)) c = Lethal;
                else
                {
                    int flip = _pistons.TicksUntilPhaseFlip(i);
                    if (flip <= WarnTicks)
                    {
                        // Pulse that quickens toward the flip; steady blend when Reduce Motion is on.
                        float urgency = 1f - flip / (float)WarnTicks;
                        float pulse = ReduceMotion ? 1f : 0.5f + 0.5f * Mathf.Sin(Time.time * (8f + 16f * urgency));
                        c = Color.Lerp(Safe, Warn, Mathf.Lerp(0.4f, 1f, urgency * pulse));
                    }
                    else c = Safe;
                }
                _pistonQuads[i].Sr.color = c;
            }

            for (int i = 0; i < _turretQuads.Count; i++)
                _turretQuads[i].Sr.color = _turrets.IsFiring(i) ? Lethal : Safe;

            // Body Shield beams: charged (will kill the next body in) reads hot; recharging reads as the
            // visible safe window the sacrifice just bought.
            for (int i = 0; i < _beamQuads.Count; i++)
                _beamQuads[i].Sr.color = _beams.IsCharged(i) ? Lethal : Safe;
        }

        private Quad MakeQuad(float minX, float minY, float maxX, float maxY)
        {
            if (_sprite == null)
            {
                var tex = Texture2D.whiteTexture;
                _sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            var go = new GameObject("TelegraphQuad");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0.5f); // behind bodies
            go.transform.localScale = new Vector3(maxX - minX, maxY - minY, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = Safe;
            return new Quad { Sr = sr };
        }
    }
}
