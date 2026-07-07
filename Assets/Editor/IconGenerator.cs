using System.IO;
using UnityEditor;
using UnityEngine;

namespace Echo.EditorTools
{
    /// <summary>
    /// Bakes the app icon procedurally — consistent with the game's zero-external-asset ethos (see
    /// AudioDirector's synthesized SFX). The mark is an "echo ripple": concentric squares fading
    /// outward from a solid player square, in World 1's palette (teal), on the game's own dark
    /// background color. No image editor, no imported art.
    ///
    /// CI usage: Unity.exe -batchmode -nographics -projectPath &lt;path&gt; -executeMethod
    ///   Echo.EditorTools.IconGenerator.Generate -quit
    /// </summary>
    public static class IconGenerator
    {
        private const int Size = 512;
        private const string OutPath = "Assets/Resources/AppIcon.png";

        [MenuItem("Echo/Build/Generate App Icon")]
        public static void Generate()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            Color bg = new Color(0.04f, 0.06f, 0.07f, 1f); // matches the game's default dark backdrop
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    tex.SetPixel(x, y, bg);

            // Three fading rings (the echo braid) behind one solid square (the live player).
            Color ring = new Color(0.35f, 0.85f, 0.75f); // World 1 haze teal
            float c = Size / 2f;
            DrawSquare(tex, c, c, Size * 0.42f, WithAlpha(ring, 0.18f));
            DrawSquare(tex, c, c, Size * 0.30f, WithAlpha(ring, 0.32f));
            DrawSquare(tex, c, c, Size * 0.20f, WithAlpha(ring, 0.55f));
            DrawSquare(tex, c, c, Size * 0.11f, Color.white);

            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(OutPath)!);
            File.WriteAllBytes(OutPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(OutPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(OutPath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var baked = AssetDatabase.LoadAssetAtPath<Texture2D>(OutPath);

            // SetIconsForTargetGroup silently no-ops if the array length doesn't exactly match the
            // platform's required icon-size count (Standalone wants a specific N, not an arbitrary
            // number) — the first attempt passed a fixed array of 6 and failed silently, so the exe
            // shipped with Unity's default icon despite the log claiming success.
            int[] standaloneSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone);
            var standaloneIcons = new Texture2D[standaloneSizes.Length];
            for (int i = 0; i < standaloneIcons.Length; i++) standaloneIcons[i] = baked;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, standaloneIcons);

            int[] unknownSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Unknown);
            var unknownIcons = new Texture2D[unknownSizes.Length];
            for (int i = 0; i < unknownIcons.Length; i++) unknownIcons[i] = baked;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, unknownIcons);

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(baked);
            AssetDatabase.SaveAssets();

            // Verify in-process before trusting the disk write.
            var readBack = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Standalone);
            int assigned = 0;
            foreach (var t in readBack) if (t != null) assigned++;
            Debug.Log($"[IconGenerator] App icon baked: {OutPath} — Standalone wants {standaloneSizes.Length} icon(s), {assigned} assigned after write.");
        }

        // Square outline ring: a filled square minus a smaller filled square, i.e. a frame — cheap to
        // draw with two nested loops, no external drawing library needed.
        private static void DrawSquare(Texture2D tex, float cx, float cy, float halfExtent, Color col)
        {
            int minX = Mathf.Max(0, Mathf.RoundToInt(cx - halfExtent));
            int maxX = Mathf.Min(Size - 1, Mathf.RoundToInt(cx + halfExtent));
            int minY = Mathf.Max(0, Mathf.RoundToInt(cy - halfExtent));
            int maxY = Mathf.Min(Size - 1, Mathf.RoundToInt(cy + halfExtent));
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    tex.SetPixel(x, y, Blend(tex.GetPixel(x, y), col));
        }

        private static Color Blend(Color under, Color over) =>
            Color.Lerp(under, new Color(over.r, over.g, over.b, 1f), over.a);

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
