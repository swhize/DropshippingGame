using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DropshippingGame
{
    /// <summary>
    /// Post-Processing (URP): filmisches Tonemapping, Bloom für Neon/Lampen, Farbkorrektur nach Tageszeit
    /// (Weißabgleich, kühle Schatten/warme Lichter, Sättigung – siehe <see cref="ArtStyle.Mood"/>) und Vignette.
    /// Außerdem Kantenglättung pro Kamera und die Grafikstufen (Niedrig/Mittel/Hoch).
    /// Ohne URP (Standard-Pipeline) passiert hier einfach nichts.
    /// </summary>
    public static class PostFX
    {
        private static Volume _volume;
        private static Bloom _bloom;
        private static Vignette _vignette;
        private static ColorAdjustments _color;
        private static MotionBlur _motion;
        private static VolumeProfile _profile;
        private static WhiteBalance _white;
        private static ShadowsMidtonesHighlights _smh;
        // Stimmung (Atmosphere) und Abdunkeln (Laptop/Pause) werden kombiniert.
        private static float _moodSat = 10f, _moodContrast = 8f, _dim;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _volume = null;
            _bloom = null;
            _vignette = null;
            _color = null;
            _motion = null;
            _profile = null;
            _white = null;
            _smh = null;
            _moodSat = 10f;
            _moodContrast = 8f;
            _dim = 0f;
        }

        public static void Create(Transform parent)
        {
            if (_volume != null) return;
            var go = new GameObject("PostFX");
            go.transform.SetParent(parent, false);
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 1f;
            // Profil der vorigen Welt freigeben (sonst wächst der Speicher mit jedem Menü-/Spielwechsel).
            if (_profile != null) Object.Destroy(_profile);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile = profile;
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
            // Bloom mit einem Hauch Wärme: Lampen und Neon glühen nicht klinisch weiß.
            if (_bloom.tint != null) _bloom.tint.Override(new Color(1f, 0.93f, 0.84f));

            // Farbkorrektur nach Tageszeit (Werte setzt Atmosphere über SetMood).
            _white = profile.Add<WhiteBalance>(true);
            _smh = profile.Add<ShadowsMidtonesHighlights>(true);
            if (_smh != null)
            {
                _smh.shadowsStart.Override(0f);
                _smh.shadowsEnd.Override(0.32f);
                _smh.highlightsStart.Override(0.55f);
                _smh.highlightsEnd.Override(1f);
                _smh.midtones.Override(new Vector4(1f, 1f, 1f, 0f));
            }

            _vignette = profile.Add<Vignette>(true);
            _vignette.intensity.Override(0.2f);
            _vignette.smoothness.Override(0.45f);

            // Bewegungsunschärfe nur auf Wunsch (Einstellungen › Grafik), dezent.
            _motion = profile.Add<MotionBlur>(true);
            if (_motion != null)
            {
                _motion.intensity.Override(0.22f);
                _motion.active = false;
            }

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
            _dim = Mathf.Clamp01(amount);
            ApplyColor();
        }

        /// <summary>Farbkorrektur der aktuellen Lichtstimmung (von <see cref="Atmosphere"/>, alle 0,2 s).</summary>
        public static void SetMood(ArtStyle.Mood m)
        {
            _moodSat = m.Saturation;
            _moodContrast = m.Contrast;
            if (_white != null) _white.temperature.Override(Mathf.Clamp(m.Temperature, -100f, 100f));
            if (_smh != null)
            {
                _smh.shadows.Override(m.Shadows);
                _smh.highlights.Override(m.Highlights);
            }
            ApplyColor();
        }

        private static void ApplyColor()
        {
            if (_color == null) return;
            _color.saturation.Override(Mathf.Lerp(_moodSat, -35f, _dim));
            _color.contrast.Override(_moodContrast);
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
                asset.renderScale = Mathf.Clamp(Settings.RenderScale, Settings.RenderScaleMin, 1f);
            }
            if (_bloom != null) _bloom.active = q > 0;
            if (_vignette != null) _vignette.active = q > 0;
            // Farbkorrektur kostet praktisch nichts (steckt in der LUT), auf „Niedrig“ ist Post-Processing ohnehin aus.
            if (_white != null) _white.active = q > 0;
            if (_smh != null) _smh.active = q > 0;
            QualitySettings.shadowDistance = q == 0 ? 30f : (q == 1 ? 55f : 85f);
            if (_motion != null) _motion.active = Settings.MotionBlur && q > 0 && !Settings.CalmEffects;
            Settings.ApplyFrameRate();
            SetupCamera(Camera.main);
        }
    }
}
