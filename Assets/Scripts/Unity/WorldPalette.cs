using UnityEngine;

namespace Echo.Unity
{
    /// <summary>
    /// Per-world visual identity, zero assets. Each world gets a mood palette (background gradient,
    /// ambient particle color, solid-tile tint) that presentation layers pull from. Semantic colors —
    /// lethal red, interactive teal, exit green — deliberately do NOT vary per world, so the gameplay
    /// language stays learnable and colorblind-safe while the ambience shifts.
    ///
    /// Moods mirror the music voicings: W1 cold arrival → W2 wonder → W3 amber melancholy →
    /// W4 oxide oppression → W5 violet precision → W6 monochrome climax.
    /// </summary>
    public static class WorldPalette
    {
        public struct Theme
        {
            public Color BgTop, BgBottom; // vertical backdrop gradient (top is darker: depth overhead)
            public Color Haze;            // slow-drifting soft bands (fake parallax depth)
            public Color Mote;            // ambient drifting dust/echo-shimmer particles
            public Color Tile;            // tint for the solid collision-grid tiles
        }

        /// <summary>"W4_L2" → 4. Anything unparseable falls back to world 1.</summary>
        public static int WorldOf(string levelId)
        {
            if (!string.IsNullOrEmpty(levelId) && levelId.Length >= 2 && levelId[0] == 'W'
                && int.TryParse(levelId.Substring(1, 1), out int w))
                return Mathf.Clamp(w, 1, 6);
            return 1;
        }

        public static Theme For(int world) => world switch
        {
            2 => new Theme // wonder: deep verdigris
            {
                BgTop = new Color(0.02f, 0.09f, 0.10f), BgBottom = new Color(0.06f, 0.17f, 0.16f),
                Haze = new Color(0.35f, 0.85f, 0.75f), Mote = new Color(0.55f, 0.95f, 0.85f),
                Tile = new Color(0.27f, 0.44f, 0.42f),
            },
            3 => new Theme // melancholy: amber dusk
            {
                BgTop = new Color(0.10f, 0.07f, 0.03f), BgBottom = new Color(0.17f, 0.12f, 0.06f),
                Haze = new Color(0.95f, 0.70f, 0.35f), Mote = new Color(1.00f, 0.80f, 0.50f),
                Tile = new Color(0.46f, 0.39f, 0.28f),
            },
            4 => new Theme // the facility pushes back: oxide red
            {
                BgTop = new Color(0.10f, 0.04f, 0.05f), BgBottom = new Color(0.17f, 0.08f, 0.10f),
                Haze = new Color(0.90f, 0.40f, 0.35f), Mote = new Color(1.00f, 0.55f, 0.45f),
                Tile = new Color(0.45f, 0.31f, 0.33f),
            },
            5 => new Theme // precision and identity: deep violet
            {
                BgTop = new Color(0.06f, 0.04f, 0.12f), BgBottom = new Color(0.12f, 0.08f, 0.21f),
                Haze = new Color(0.65f, 0.50f, 0.95f), Mote = new Color(0.78f, 0.65f, 1.00f),
                Tile = new Color(0.39f, 0.33f, 0.51f),
            },
            6 => new Theme // climax: monochrome ice
            {
                BgTop = new Color(0.04f, 0.04f, 0.06f), BgBottom = new Color(0.10f, 0.10f, 0.13f),
                Haze = new Color(0.85f, 0.88f, 0.95f), Mote = new Color(0.95f, 0.97f, 1.00f),
                Tile = new Color(0.41f, 0.45f, 0.53f),
            },
            _ => new Theme // W1 — cold arrival: slate blue (the palette the game shipped with)
            {
                BgTop = new Color(0.04f, 0.06f, 0.12f), BgBottom = new Color(0.10f, 0.14f, 0.22f),
                Haze = new Color(0.50f, 0.65f, 0.90f), Mote = new Color(0.65f, 0.78f, 1.00f),
                Tile = new Color(0.32f, 0.40f, 0.52f),
            },
        };
    }
}
