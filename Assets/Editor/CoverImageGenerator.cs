using System.IO;
using UnityEditor;
using UnityEngine;

namespace Echo.EditorTools
{
    /// <summary>
    /// Bakes the itch.io cover image (630x500 recommended) procedurally — same zero-external-asset
    /// ethos as <see cref="IconGenerator"/> and the game's own synthesized audio. Reuses World 1's
    /// actual palette (see WorldPalette.cs) for the backdrop gradient/haze/motes, the same
    /// concentric-square "echo ripple" mark as the app icon, and a blocky rectangle-grid wordmark —
    /// consistent with the game's own geometric-square visual language, no font rendering needed.
    ///
    /// CI usage: Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    ///   Echo.EditorTools.CoverImageGenerator.Generate -quit
    /// </summary>
    public static class CoverImageGenerator
    {
        private const int W = 630;
        private const int H = 500;
        private const string OutPath = "Assets/Resources/CoverImage.png";

        // World 1 palette (WorldPalette.cs) — kept in sync manually since this is an Editor-only script.
        private static readonly Color BgTop = new Color(0.02f, 0.09f, 0.10f);
        private static readonly Color BgBottom = new Color(0.06f, 0.17f, 0.16f);
        private static readonly Color Haze = new Color(0.35f, 0.85f, 0.75f);
        private static readonly Color Mote = new Color(0.55f, 0.95f, 0.85f);

        [MenuItem("Echo/Build/Generate Cover Image")]
        public static void Generate()
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false, false);

            // Vertical gradient backdrop, same top-darker-than-bottom convention as BackgroundDirector.
            for (int y = 0; y < H; y++)
            {
                Color row = Color.Lerp(BgBottom, BgTop, (float)y / H);
                for (int x = 0; x < W; x++) tex.SetPixel(x, y, row);
            }

            // Two soft haze bands (fake parallax depth, mirrors BackgroundDirector's drifting bands).
            DrawHazeBand(tex, y0: H * 0.18f, thickness: H * 0.10f, alpha: 0.10f);
            DrawHazeBand(tex, y0: H * 0.62f, thickness: H * 0.14f, alpha: 0.07f);

            // Ambient motes.
            var rng = new System.Random(1234);
            for (int i = 0; i < 40; i++)
            {
                int mx = rng.Next(0, W), my = rng.Next(0, H);
                float r = (float)(rng.NextDouble() * 1.6 + 0.6);
                DrawDot(tex, mx, my, r, WithAlpha(Mote, 0.35f));
            }

            // The echo-ripple mark, off-center left so the wordmark has clear room on the right.
            float cx = W * 0.28f, cy = H * 0.52f;
            float unit = H * 0.015f; // matches IconGenerator's proportions, scaled to this canvas
            DrawSquare(tex, cx, cy, unit * 21f, WithAlpha(Haze, 0.18f));
            DrawSquare(tex, cx, cy, unit * 15f, WithAlpha(Haze, 0.32f));
            DrawSquare(tex, cx, cy, unit * 10f, WithAlpha(Haze, 0.55f));
            DrawSquare(tex, cx, cy, unit * 5.5f, Color.white);

            // Blocky rectangle-grid wordmark "ECHO" — no font rendering; same geometric-square
            // language as the player/Echo bodies themselves. Sized to fit fully within the canvas
            // with margin (checked against the 24.2-cell total width of "ECHO" at this cellSize).
            DrawWord(tex, W * 0.58f, H * 0.40f, cellSize: H * 0.020f, Color.white);

            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(OutPath)!);
            File.WriteAllBytes(OutPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(OutPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(OutPath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            Debug.Log($"[CoverImageGenerator] Cover image baked: {OutPath} ({W}x{H})");
        }

        private static readonly string[] LetterE = { "11111", "1....", "1....", "11111", "1....", "1....", "11111" };
        private static readonly string[] LetterC = { ".1111", "1....", "1....", "1....", "1....", "1....", ".1111" };
        private static readonly string[] LetterH = { "1...1", "1...1", "1...1", "11111", "1...1", "1...1", "1...1" };
        private static readonly string[] LetterO = { ".111.", "1...1", "1...1", "1...1", "1...1", "1...1", ".111." };

        private static void DrawWord(Texture2D tex, float startX, float startY, float cellSize, Color col)
        {
            string[][] letters = { LetterE, LetterC, LetterH, LetterO };
            float penX = startX;
            foreach (var glyph in letters)
            {
                for (int row = 0; row < glyph.Length; row++)
                    for (int col2 = 0; col2 < glyph[row].Length; col2++)
                        if (glyph[row][col2] == '1')
                        {
                            float px = penX + col2 * cellSize;
                            float py = startY + row * cellSize;
                            FillRect(tex, px, py, cellSize * 0.92f, cellSize * 0.92f, col);
                        }
                penX += (glyph[0].Length + 1.4f) * cellSize; // glyph width + letter spacing
            }
        }

        private static void FillRect(Texture2D tex, float x, float y, float w, float h, Color col)
        {
            int minX = Mathf.Max(0, Mathf.RoundToInt(x)), maxX = Mathf.Min(W - 1, Mathf.RoundToInt(x + w));
            int minY = Mathf.Max(0, Mathf.RoundToInt(y)), maxY = Mathf.Min(H - 1, Mathf.RoundToInt(y + h));
            for (int py = minY; py <= maxY; py++)
                for (int px = minX; px <= maxX; px++)
                    tex.SetPixel(px, py, Blend(tex.GetPixel(px, py), col));
        }

        private static void DrawSquare(Texture2D tex, float cx, float cy, float halfExtent, Color col)
        {
            int minX = Mathf.Max(0, Mathf.RoundToInt(cx - halfExtent));
            int maxX = Mathf.Min(W - 1, Mathf.RoundToInt(cx + halfExtent));
            int minY = Mathf.Max(0, Mathf.RoundToInt(cy - halfExtent));
            int maxY = Mathf.Min(H - 1, Mathf.RoundToInt(cy + halfExtent));
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    tex.SetPixel(x, y, Blend(tex.GetPixel(x, y), col));
        }

        private static void DrawHazeBand(Texture2D tex, float y0, float thickness, float alpha)
        {
            int minY = Mathf.Max(0, Mathf.RoundToInt(y0));
            int maxY = Mathf.Min(H - 1, Mathf.RoundToInt(y0 + thickness));
            for (int y = minY; y <= maxY; y++)
            {
                float t = 1f - Mathf.Abs((y - (y0 + thickness * 0.5f)) / (thickness * 0.5f));
                Color c = WithAlpha(Haze, alpha * Mathf.Clamp01(t));
                for (int x = 0; x < W; x++) tex.SetPixel(x, y, Blend(tex.GetPixel(x, y), c));
            }
        }

        private static void DrawDot(Texture2D tex, int cx, int cy, float radius, Color col)
        {
            int r = Mathf.CeilToInt(radius);
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                {
                    if (x * x + y * y > radius * radius) continue;
                    int px = cx + x, py = cy + y;
                    if (px < 0 || px >= W || py < 0 || py >= H) continue;
                    tex.SetPixel(px, py, Blend(tex.GetPixel(px, py), col));
                }
        }

        private static Color Blend(Color under, Color over) =>
            Color.Lerp(under, new Color(over.r, over.g, over.b, 1f), over.a);

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
