using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DropshippingGame
{
    /// <summary>
    /// Post-Processing (URP): filmisches Tonemapping, Bloom für Neon/Lampen, leichte Farbkorrektur
    /// und Vignette. Außerdem Kantenglättung pro Kamera und die Grafikstufen (Niedrig/Mittel/Hoch).
    /// Ohne URP (Standard-Pipeline) passiert hier einfach nichts.
    /// </summary>
    public static class PostFX
    {
        private static Volume _volume;
        private static Bloom _bloom;
        private static Vignette _vignette;
        private static ColorAdjustments _color;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _volume = null;
            _bloom = null;
            _vignette = null;
            _color = null;
        }

        public static void Create(Transform parent)
        {
            if (_volume != null) return;
            var go = new GameObject("PostFX");
            go.transform.SetParent(parent, false);
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 1f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume.sharedProfile = profile;

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            _bloom = profile.Add<Bloom>(true);
            _bloom.intensity.Override(0.6f);
            _bloom.threshold.Override(1.2f);
            _bloom.scatter.Override(0.55f);
            // Begrenzt extrem helle HDR-Pixel (Sonnenscheibe, Spiegelungen), damit sie nicht grell ausstrahlen.
            _bloom.clamp.Override(12f);

            _color = profile.Add<ColorAdjustments>(true);
            _color.postExposure.Override(0.35f);
            _color.contrast.Override(8f);
            _color.saturation.Override(10f);

            _vignette = profile.Add<Vignette>(true);
            _vignette.intensity.Override(0.2f);
            _vignette.smoothness.Override(0.45f);

            ApplyQuality();
        }

        /// <summary>Menü-Look: etwas mehr Vignette und Kontrast.</summary>
        public static void SetMenuLook(bool menu)
        {
            if (_vignette != null) _vignette.intensity.Override(menu ? 0.34f : 0.2f);
        }

        /// <summary>Dämpft das Bild (z.B. hinter dem Laptop oder im Pausemenü).</summary>
        public static void SetDimmed(float amount)
        {
            if (_color != null) _color.saturation.Override(Mathf.Lerp(10f, -35f, amount));
        }

        public static void SetupCamera(Camera cam)
        {
            if (cam == null || !Mats.Urp) return;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = Settings.Quality > 0;
            data.antialiasing = Settings.Quality > 0 ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.FastApproximateAntialiasing;
            data.antialiasingQuality = Settings.Quality == 2 ? AntialiasingQuality.High : AntialiasingQuality.Medium;
        }

        /// <summary>Wendet die Grafikstufe auf die URP-Einstellungen an.</summary>
        public static void ApplyQuality()
        {
            int q = Settings.Quality;
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset != null)
            {
                asset.shadowDistance = q == 0 ? 30f : (q == 1 ? 55f : 85f);
                // Mehr Kaskaden = höhere Schattenauflösung nahe der Kamera, weniger Flimmern bei drehender Sonne.
                asset.shadowCascadeCount = q == 0 ? 1 : (q == 1 ? 2 : 4);
                asset.msaaSampleCount = q == 2 ? 4 : (q == 1 ? 2 : 1);
                asset.renderScale = q == 0 ? 0.8f : 1f;
            }
            if (_bloom != null) _bloom.active = q > 0;
            if (_vignette != null) _vignette.active = q > 0;
            QualitySettings.shadowDistance = q == 0 ? 30f : (q == 1 ? 55f : 85f);
            QualitySettings.vSyncCount = 1;
            SetupCamera(Camera.main);
        }
    }
}
