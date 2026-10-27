using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Vorschaubilder der Spielstände: beim Speichern rendert die Spielerkamera (ohne Oberfläche) ein
    /// kleines Bild, das als PNG neben dem Spielstand liegt („savegame_N.png“). Das Hauptmenü zeigt es
    /// auf den Spielstand-Karten; fehlt es, gibt es eine farbige Markenkarte (<see cref="BrandColor"/>).
    /// </summary>
    public static class SaveThumbs
    {
        public const int Width = 384, Height = 216;
        private static readonly Dictionary<int, Texture2D> _cache = new Dictionary<int, Texture2D>();
        private static float _lastCapture = -100f;
        private static int _lastSlot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _cache.Clear();
            _lastCapture = -100f;
            _lastSlot = 0;
        }

        public static string PathFor(int slot)
        {
            var sim = Game.Sim;
            if (sim == null || string.IsNullOrEmpty(sim.SaveDir)) return null;
            return Path.Combine(sim.SaveDir, "savegame_" + slot + ".png");
        }

        /// <summary>Nach dem Speichern: Bild aufnehmen (höchstens alle 20 s für denselben Slot, z. B. Autosave).</summary>
        public static void OnSaved(int slot)
        {
            if (slot == _lastSlot && Time.unscaledTime - _lastCapture < 20f && File.Exists(PathFor(slot) ?? "")) return;
            if (Capture(slot))
            {
                _lastSlot = slot;
                _lastCapture = Time.unscaledTime;
            }
        }

        /// <summary>Rendert die Spielerkamera in eine kleine Textur und speichert sie als PNG.</summary>
        public static bool Capture(int slot)
        {
            string path = PathFor(slot);
            var player = Game.Player;
            var cam = player != null ? player.Cam : null;
            if (path == null || cam == null || !cam.isActiveAndEnabled) return false;
            RenderTexture rt = null;
            Texture2D tex = null;
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            bool ok = false;
            try
            {
                rt = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply(false);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Invalidate(slot);
                ok = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Vorschaubild für Spielstand " + slot + " nicht möglich: " + e.Message);
            }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
            return ok;
        }

        /// <summary>Vorschaubild laden (zwischengespeichert) oder null.</summary>
        public static Texture2D Load(int slot)
        {
            if (_cache.TryGetValue(slot, out var t) && t != null) return t;
            string path = PathFor(slot);
            if (path == null || !File.Exists(path)) return null;
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!tex.LoadImage(File.ReadAllBytes(path)))
                {
                    UnityEngine.Object.Destroy(tex);
                    return null;
                }
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                _cache[slot] = tex;
                return tex;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Vorschaubild für Spielstand " + slot + " nicht lesbar: " + e.Message);
                return null;
            }
        }

        public static void Delete(int slot)
        {
            Invalidate(slot);
            try
            {
                string path = PathFor(slot);
                if (path != null && File.Exists(path)) File.Delete(path);
            }
            catch (Exception)
            {
                // nicht kritisch
            }
        }

        private static void Invalidate(int slot)
        {
            if (_cache.TryGetValue(slot, out var t) && t != null) UnityEngine.Object.Destroy(t);
            _cache.Remove(slot);
        }

        /// <summary>Stabile Markenfarbe aus dem Namen (Ersatzkarte ohne Vorschaubild).</summary>
        public static Color BrandColor(string brand, int slot)
        {
            Color[] palette =
            {
                new Color(1f, 0.48f, 0.27f), new Color(0.3f, 0.44f, 1f), new Color(0.13f, 0.7f, 0.66f), new Color(0.61f, 0.42f, 1f),
                new Color(0.95f, 0.35f, 0.55f), new Color(0.98f, 0.74f, 0.16f),
            };
            int h = Core.Sim.StableSeed(brand ?? "", slot);
            return palette[h % palette.Length];
        }
    }
}
