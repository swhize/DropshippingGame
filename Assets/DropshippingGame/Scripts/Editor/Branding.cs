using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace DropshippingGame.EditorTools
{
    /// <summary>
    /// App-Icon und Splash-Screen. Quellen: <c>Assets/DropshippingGame/Branding/AppIcon.png</c> (1024²) und
    /// <c>SplashLogo.png</c> (Wortmarke, transparent) – erzeugt mit <c>tools/branding/make_branding.py</c>.
    /// Fehlt das Icon, wird ein einfaches prozedurales Ersatz-Icon geschrieben.
    /// Unity-Logo im Splash: wird nicht angefasst (ob es abschaltbar ist, hängt von Lizenz/Version ab – siehe docs/RELEASE.md).
    /// </summary>
    public static class Branding
    {
        public const string Dir = "Assets/DropshippingGame/Branding";
        public const string IconPath = Dir + "/AppIcon.png";
        public const string SplashPath = Dir + "/SplashLogo.png";

        /// <summary>Markenfarben (wie Theme.cs): Hintergrund dunkel, Akzent Bernstein.</summary>
        public static readonly Color Background = new Color32(15, 17, 24, 255);
        public static readonly Color Accent = new Color32(255, 189, 66, 255);

        public static void Apply()
        {
            if (!File.Exists(IconPath)) WriteFallbackIcon();
            ConfigureImporter(IconPath, false);
            ConfigureImporter(SplashPath, true);

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
                int n = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Any).Length;
                var arr = new Texture2D[n];
                for (int i = 0; i < n; i++) arr[i] = icon;
                PlayerSettings.SetIcons(NamedBuildTarget.Standalone, arr, IconKind.Any);
            }

            PlayerSettings.SplashScreen.backgroundColor = Background;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.overlayOpacity = 0f;
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(SplashPath);
            if (logo != null) PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2.5f, logo) };
        }

        private static void ConfigureImporter(string path, bool sprite)
        {
            if (!File.Exists(path)) return;
            AssetDatabase.ImportAsset(path);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            bool changed = false;
            var type = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (ti.textureType != type) { ti.textureType = type; changed = true; }
            if (sprite && ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (ti.mipmapEnabled) { ti.mipmapEnabled = false; changed = true; }
            if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }
            if (ti.textureCompression != TextureImporterCompression.Uncompressed) { ti.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (ti.maxTextureSize < 2048) { ti.maxTextureSize = 2048; changed = true; }
            if (!sprite && ti.npotScale != TextureImporterNPOTScale.None) { ti.npotScale = TextureImporterNPOTScale.None; changed = true; }
            if (changed) ti.SaveAndReimport();
        }

        /// <summary>Ersatz-Icon (256²): dunkles abgerundetes Quadrat, bernsteinfarbene Paketkiste, weißer Aufwärtspfeil.</summary>
        private static void WriteFallbackIcon()
        {
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color[n * n];
            Vector2 c = new Vector2(n * 0.5f, n * 0.42f); // Unity-Texturen: y nach oben
            float u = n * 0.25f, h = n * 0.25f, dy = u * 0.5f;
            Vector2 T = c + new Vector2(0, dy), R = c + new Vector2(u, 0), B = c - new Vector2(0, dy), L = c - new Vector2(u, 0);
            Vector2 down = new Vector2(0, -h);
            Vector2[] top = { T, R, B, L };
            Vector2[] left = { L, B, B + down, L + down };
            Vector2[] right = { B, R, R + down, B + down };
            Vector2[] arrow = { new Vector2(n * 0.17f, n * 0.57f), new Vector2(n * 0.36f, n * 0.71f), new Vector2(n * 0.52f, n * 0.65f), new Vector2(n * 0.72f, n * 0.83f) };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                Color col = Color.clear;
                if (RoundedRect(p, n * 0.035f, n - n * 0.035f, n * 0.2f)) col = Color.Lerp(Background, new Color32(40, 44, 62, 255), y / (float)n);
                if (Inside(p, left)) col = new Color32(232, 158, 46, 255);
                if (Inside(p, right)) col = new Color32(190, 116, 26, 255);
                if (Inside(p, top)) col = new Color32(255, 205, 110, 255);
                if (col.a > 0 && (EdgeDist(p, left) < 2.2f || EdgeDist(p, right) < 2.2f || EdgeDist(p, top) < 2.2f) && (Inside(p, left) || Inside(p, right) || Inside(p, top)))
                    col = new Color32(20, 18, 16, 255);
                float ad = PolylineDist(p, arrow);
                if (ad < 9.5f) col = new Color32(20, 18, 16, 255);
                if (ad < 5.5f) col = Color.white;
                px[y * n + x] = col;
            }
            tex.SetPixels(px);
            tex.Apply();
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(IconPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(IconPath);
            Debug.Log("[Dropshipping] Ersatz-Icon erzeugt: " + IconPath);
        }

        private static bool RoundedRect(Vector2 p, float min, float max, float r)
        {
            float cx = Mathf.Clamp(p.x, min + r, max - r), cy = Mathf.Clamp(p.y, min + r, max - r);
            return p.x >= min && p.x <= max && p.y >= min && p.y <= max && (p - new Vector2(cx, cy)).sqrMagnitude <= r * r;
        }

        private static bool Inside(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        private static float EdgeDist(Vector2 p, Vector2[] poly)
        {
            float d = float.MaxValue;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++) d = Math.Min(d, SegDist(p, poly[j], poly[i]));
            return d;
        }

        private static float PolylineDist(Vector2 p, Vector2[] line)
        {
            float d = float.MaxValue;
            for (int i = 1; i < line.Length; i++) d = Math.Min(d, SegDist(p, line[i - 1], line[i]));
            return d;
        }

        private static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
            return (a + ab * t - p).magnitude;
        }
    }
}
