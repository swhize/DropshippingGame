using System;
using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Spieler-Einstellungen: Grafik (Auflösung, Fenstermodus, VSync, FPS-Limit, Qualität, Renderskala,
    /// Sichtfeld, Effekte), Audio, Steuerung (Empfindlichkeit, Tastenbelegung in <see cref="InputBindings"/>),
    /// Gameplay (Sprache, Kompass, Streamer-Modus) und Barrierefreiheit (UI-Größe, Untertitel,
    /// Textgeschwindigkeit, Farbenblind-Modus, weniger Bewegung).
    /// Werden in den PlayerPrefs gespeichert und sofort angewendet (<see cref="Apply"/>).
    /// </summary>
    public static class Settings
    {
        /// <summary>Version der gespeicherten Einstellungen. Ab 3 ist „Hype“ dunkel das Standard-Design.</summary>
        private const int PrefsVersion = 3;

        public const float UiScaleMin = 0.8f;
        public const float UiScaleMax = 1.4f;
        public const float RenderScaleMin = 0.5f;

        // ---- Audio --------------------------------------------------------------------------------
        public static float MasterVolume = 0.85f;
        public static float MusicVolume = 0.5f;
        public static float SfxVolume = 0.8f;

        // ---- Steuerung ----------------------------------------------------------------------------
        public static float MouseSensitivity = 1f;
        public static float PadSensitivity = 1f;
        public static bool InvertY;

        // ---- Grafik -------------------------------------------------------------------------------
        /// <summary>0 = Vollbild (exklusiv), 1 = Rahmenlos, 2 = Fenster.</summary>
        public static int WindowMode = 1;
        /// <summary>Gewählte Auflösung (0 × 0 = Bildschirmauflösung).</summary>
        public static int ResolutionWidth, ResolutionHeight;
        public static bool VSync = true;
        /// <summary>Bildrate begrenzen (0 = unbegrenzt; greift nur ohne VSync).</summary>
        public static int FpsLimit;
        /// <summary>0 = Niedrig, 1 = Mittel, 2 = Hoch</summary>
        public static int Quality = 2;
        /// <summary>Renderauflösung relativ zum Bildschirm (0,5–1).</summary>
        public static float RenderScale = 1f;
        public static float Fov = 75f;
        public static bool MotionBlur;
        public static bool HeadBob = true;
        public static bool CameraShake = true;
        public static bool ShowFps;

        // ---- Oberfläche & Gameplay ----------------------------------------------------------------
        /// <summary>Laptop-Oberfläche: "hype" (Standard) oder "frachtbrief"</summary>
        public static string LaptopDesign = "hype";
        public static bool LaptopDark = true;
        /// <summary>Größe der gesamten Oberfläche (1 = 100 %).</summary>
        public static float UiScale = 1f;
        /// <summary>Tastenhinweise im HUD anzeigen.</summary>
        public static bool ShowHints = true;
        /// <summary>Kompass mit wichtigen Orten oben im HUD.</summary>
        public static bool ShowCompass = true;
        /// <summary>Spielsprache ("de", "en"). Übersetzungen hängen an <see cref="LanguageChanged"/> (siehe <see cref="Loc"/>).</summary>
        public static string Language = "de";
        /// <summary>Streamer-Modus: keine lizenzkritische Musik, keine Wackel-/Blitzeffekte.</summary>
        public static bool StreamerMode;
        /// <summary>Im Streamer-Modus Musik ganz stummschalten (falls Content-ID-Systeme fälschlich anschlagen).</summary>
        public static bool StreamerNoMusic;

        // ---- Barrierefreiheit ---------------------------------------------------------------------
        /// <summary>Gesprochene Zeilen der Figuren zusätzlich als Untertitel im HUD.</summary>
        public static bool Subtitles = true;
        /// <summary>Textgeschwindigkeit in Dialogen: 0 langsam, 1 normal, 2 schnell, 3 sofort.</summary>
        public static int TextSpeed = 1;
        /// <summary>Farbenblind-freundlich: Blau/Orange statt Grün/Rot für gut/schlecht.</summary>
        public static bool ColorBlind;
        /// <summary>Weniger Bewegung: kein Pulsieren/Blinken in der Oberfläche.</summary>
        public static bool ReduceMotion;

        public static event Action Changed;
        /// <summary>Sprache wurde umgestellt (Parameter: neuer Sprachcode).</summary>
        public static event Action<string> LanguageChanged;
        private static bool _loaded;

        public static readonly string[] QualityNames = { "Niedrig", "Mittel", "Hoch" };
        public static readonly string[] WindowModeNames = { "Vollbild", "Rahmenlos", "Fenster" };
        public static readonly int[] FpsLimits = { 0, 30, 60, 90, 120, 144, 165, 240 };
        public static readonly string[] TextSpeedNames = { "Langsam", "Normal", "Schnell", "Sofort" };
        public static readonly string[] LanguageCodes = { "de", "en" };
        public static readonly string[] LanguageNames = { "Deutsch", "English" };

        /// <summary>Musik, die im Streamer-Modus stumm bleibt (Clip-Namen). Derzeit ist alle Musik CC0.</summary>
        public static readonly HashSet<string> StreamerUnsafeMusic = new HashSet<string>();

        /// <summary>Darf dieses Musikstück gerade laufen? (Streamer-Modus: ohne Musik bzw. ohne lizenzkritische Stücke.)</summary>
        public static bool MusicAllowed(string clipName)
        {
            if (!StreamerMode) return true;
            if (StreamerNoMusic) return false;
            return string.IsNullOrEmpty(clipName) || !StreamerUnsafeMusic.Contains(clipName);
        }

        /// <summary>Kamerawackeln erlaubt (Einstellung an, kein Streamer-Modus, nicht „weniger Bewegung“).</summary>
        public static bool ShakeEnabled => CameraShake && !StreamerMode && !ReduceMotion;
        /// <summary>Ruhige Effekte: kein Blinken/Pulsieren (weniger Bewegung oder Streamer-Modus).</summary>
        public static bool CalmEffects => ReduceMotion || StreamerMode;
        /// <summary>Kompatibilität: true bei Vollbild oder Rahmenlos.</summary>
        public static bool Fullscreen => WindowMode != 2;

        /// <summary>Zeichen pro Sekunde im Dialog (sofort = sehr groß).</summary>
        public static float TextCharsPerSecond
        {
            get
            {
                switch (TextSpeed)
                {
                    case 0: return 28f;
                    case 2: return 110f;
                    case 3: return 100000f;
                    default: return 55f;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = false;
            Changed = null;
            LanguageChanged = null;
        }

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("ds.master", MasterVolume));
                MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("ds.music", MusicVolume));
                SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("ds.sfx", SfxVolume));
                MouseSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("ds.mouse", MouseSensitivity), 0.1f, 5f);
                PadSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("ds.pad", PadSensitivity), 0.1f, 5f);
                Fov = Mathf.Clamp(PlayerPrefs.GetFloat("ds.fov", Fov), 55f, 110f);
                // Fenstermodus: Standard ist rahmenloses Vollbild in Bildschirmauflösung. Das alte „ds.fullscreen“
                // wurde immer mitgespeichert (Standard war „aus“) und ist daher keine echte Wahl – wird ignoriert.
                WindowMode = Mathf.Clamp(PlayerPrefs.GetInt("ds.winmode", WindowMode), 0, 2);
                ResolutionWidth = Mathf.Max(0, PlayerPrefs.GetInt("ds.resw", 0));
                ResolutionHeight = Mathf.Max(0, PlayerPrefs.GetInt("ds.resh", 0));
                VSync = PlayerPrefs.GetInt("ds.vsync", VSync ? 1 : 0) == 1;
                FpsLimit = Mathf.Max(0, PlayerPrefs.GetInt("ds.fpslimit", FpsLimit));
                HeadBob = PlayerPrefs.GetInt("ds.headbob", HeadBob ? 1 : 0) == 1;
                CameraShake = PlayerPrefs.GetInt("ds.shake", CameraShake ? 1 : 0) == 1;
                MotionBlur = PlayerPrefs.GetInt("ds.motionblur", MotionBlur ? 1 : 0) == 1;
                InvertY = PlayerPrefs.GetInt("ds.inverty", InvertY ? 1 : 0) == 1;
                Quality = Mathf.Clamp(PlayerPrefs.GetInt("ds.quality", Quality), 0, 2);
                RenderScale = Mathf.Clamp(PlayerPrefs.GetFloat("ds.renderscale", RenderScale), RenderScaleMin, 1f);
                ShowFps = PlayerPrefs.GetInt("ds.fps", ShowFps ? 1 : 0) == 1;
                LaptopDesign = PlayerPrefs.GetString("ds.laptop", LaptopDesign);
                LaptopDark = PlayerPrefs.GetInt("ds.laptopdark", LaptopDark ? 1 : 0) == 1;
                UiScale = Mathf.Clamp(PlayerPrefs.GetFloat("ds.uiscale", UiScale), UiScaleMin, UiScaleMax);
                ShowHints = PlayerPrefs.GetInt("ds.hints", ShowHints ? 1 : 0) == 1;
                ShowCompass = PlayerPrefs.GetInt("ds.compass", ShowCompass ? 1 : 0) == 1;
                Language = PlayerPrefs.GetString("ds.lang", Language);
                StreamerMode = PlayerPrefs.GetInt("ds.streamer", StreamerMode ? 1 : 0) == 1;
                StreamerNoMusic = PlayerPrefs.GetInt("ds.streamer.nomusic", StreamerNoMusic ? 1 : 0) == 1;
                Subtitles = PlayerPrefs.GetInt("ds.subs", Subtitles ? 1 : 0) == 1;
                TextSpeed = Mathf.Clamp(PlayerPrefs.GetInt("ds.textspeed", TextSpeed), 0, 3);
                ColorBlind = PlayerPrefs.GetInt("ds.cb", ColorBlind ? 1 : 0) == 1;
                ReduceMotion = PlayerPrefs.GetInt("ds.reducemotion", ReduceMotion ? 1 : 0) == 1;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            if (LaptopDesign != "hype" && LaptopDesign != "frachtbrief") LaptopDesign = "hype";
            if (Array.IndexOf(LanguageCodes, Language) < 0) Language = "de";
            // Kaputte/fremde Registry-Werte (NaN, Extremwerte) würden Kamera und Audio unbrauchbar machen.
            MasterVolume = Sane(MasterVolume, 0f, 1f, 0.85f);
            MusicVolume = Sane(MusicVolume, 0f, 1f, 0.5f);
            SfxVolume = Sane(SfxVolume, 0f, 1f, 0.8f);
            MouseSensitivity = Sane(MouseSensitivity, 0.1f, 5f, 1f);
            PadSensitivity = Sane(PadSensitivity, 0.1f, 5f, 1f);
            Fov = Sane(Fov, 55f, 110f, 75f);
            RenderScale = Sane(RenderScale, RenderScaleMin, 1f, 1f);
            UiScale = Sane(UiScale, UiScaleMin, UiScaleMax, 1f);

            // v3.0: Das neue Standard-Design ist „Hype“ (dunkel). Ältere Einstellungen wurden bei
            // jeder Änderung komplett gespeichert – deshalb einmalig auf den neuen Standard setzen.
            if (PlayerPrefs.GetInt("ds.prefsver", 0) < PrefsVersion)
            {
                LaptopDesign = "hype";
                LaptopDark = true;
                PlayerPrefs.SetInt("ds.prefsver", PrefsVersion);
                Save();
            }
            InputBindings.Load();
        }

        private static float Sane(float v, float min, float max, float fallback) =>
            float.IsNaN(v) || float.IsInfinity(v) ? fallback : Mathf.Clamp(v, min, max);

        public static void Save()
        {
            try
            {
                PlayerPrefs.SetFloat("ds.master", MasterVolume);
                PlayerPrefs.SetFloat("ds.music", MusicVolume);
                PlayerPrefs.SetFloat("ds.sfx", SfxVolume);
                PlayerPrefs.SetFloat("ds.mouse", MouseSensitivity);
                PlayerPrefs.SetFloat("ds.pad", PadSensitivity);
                PlayerPrefs.SetFloat("ds.fov", Fov);
                PlayerPrefs.SetInt("ds.winmode", WindowMode);
                PlayerPrefs.SetInt("ds.fullscreen", Fullscreen ? 1 : 0);
                PlayerPrefs.SetInt("ds.resw", ResolutionWidth);
                PlayerPrefs.SetInt("ds.resh", ResolutionHeight);
                PlayerPrefs.SetInt("ds.vsync", VSync ? 1 : 0);
                PlayerPrefs.SetInt("ds.fpslimit", FpsLimit);
                PlayerPrefs.SetInt("ds.headbob", HeadBob ? 1 : 0);
                PlayerPrefs.SetInt("ds.shake", CameraShake ? 1 : 0);
                PlayerPrefs.SetInt("ds.motionblur", MotionBlur ? 1 : 0);
                PlayerPrefs.SetInt("ds.inverty", InvertY ? 1 : 0);
                PlayerPrefs.SetInt("ds.quality", Quality);
                PlayerPrefs.SetFloat("ds.renderscale", RenderScale);
                PlayerPrefs.SetInt("ds.fps", ShowFps ? 1 : 0);
                PlayerPrefs.SetString("ds.laptop", LaptopDesign);
                PlayerPrefs.SetInt("ds.laptopdark", LaptopDark ? 1 : 0);
                PlayerPrefs.SetFloat("ds.uiscale", UiScale);
                PlayerPrefs.SetInt("ds.hints", ShowHints ? 1 : 0);
                PlayerPrefs.SetInt("ds.compass", ShowCompass ? 1 : 0);
                PlayerPrefs.SetString("ds.lang", Language);
                PlayerPrefs.SetInt("ds.streamer", StreamerMode ? 1 : 0);
                PlayerPrefs.SetInt("ds.streamer.nomusic", StreamerNoMusic ? 1 : 0);
                PlayerPrefs.SetInt("ds.subs", Subtitles ? 1 : 0);
                PlayerPrefs.SetInt("ds.textspeed", TextSpeed);
                PlayerPrefs.SetInt("ds.cb", ColorBlind ? 1 : 0);
                PlayerPrefs.SetInt("ds.reducemotion", ReduceMotion ? 1 : 0);
                PlayerPrefs.SetInt("ds.prefsver", PrefsVersion);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>Nach jeder Änderung aufrufen: speichert, wendet an, benachrichtigt.</summary>
        public static void Apply()
        {
            UiScale = Mathf.Clamp(UiScale, UiScaleMin, UiScaleMax);
            RenderScale = Mathf.Clamp(RenderScale, RenderScaleMin, 1f);
            Save();
            ApplyDisplay();
            try
            {
                Changed?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>Während ein Regler gezogen wird: sofort anwenden, aber noch nicht speichern.</summary>
        public static void ApplyLive()
        {
            try
            {
                Changed?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>Sprache umstellen (speichert und meldet <see cref="LanguageChanged"/>).</summary>
        public static void SetLanguage(string code)
        {
            if (Array.IndexOf(LanguageCodes, code) < 0 || code == Language) return;
            Language = code;
            Apply();
            try
            {
                LanguageChanged?.Invoke(code);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>VSync und Bildraten-Limit.</summary>
        public static void ApplyFrameRate()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync || FpsLimit <= 0 ? -1 : FpsLimit;
        }

        public static FullScreenMode ScreenMode
        {
            get
            {
                switch (WindowMode)
                {
                    case 0: return FullScreenMode.ExclusiveFullScreen;
                    case 2: return FullScreenMode.Windowed;
                    default: return FullScreenMode.FullScreenWindow;
                }
            }
        }

        /// <summary>Zielauflösung (gewählt oder Bildschirm; im Fenster höchstens 90 % des Bildschirms).</summary>
        public static Vector2Int TargetResolution()
        {
            int sw = Display.main != null ? Display.main.systemWidth : Screen.currentResolution.width;
            int sh = Display.main != null ? Display.main.systemHeight : Screen.currentResolution.height;
            if (sw <= 0 || sh <= 0)
            {
                sw = 1920;
                sh = 1080;
            }
            int w = ResolutionWidth, h = ResolutionHeight;
            if (w <= 0 || h <= 0)
            {
                if (WindowMode == 2)
                {
                    w = Mathf.Min(1600, Mathf.RoundToInt(sw * 0.9f));
                    h = Mathf.RoundToInt(w * 9f / 16f);
                }
                else
                {
                    w = sw;
                    h = sh;
                }
            }
            return new Vector2Int(Mathf.Max(640, w), Mathf.Max(360, h));
        }

        /// <summary>Alle angebotenen Auflösungen (absteigend, ohne Doppelte, mindestens 1280 × 720).</summary>
        public static List<Vector2Int> AvailableResolutions()
        {
            var list = new List<Vector2Int>();
            try
            {
                foreach (var r in Screen.resolutions)
                {
                    var v = new Vector2Int(r.width, r.height);
                    if (v.x < 1280 || v.y < 720 || list.Contains(v)) continue;
                    list.Add(v);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            if (list.Count == 0)
            {
                list.Add(new Vector2Int(1920, 1080));
                list.Add(new Vector2Int(1600, 900));
                list.Add(new Vector2Int(1280, 720));
            }
            if (ResolutionWidth > 0 && ResolutionHeight > 0)
            {
                var cur = new Vector2Int(ResolutionWidth, ResolutionHeight);
                if (!list.Contains(cur)) list.Add(cur);
            }
            list.Sort((a, b) => a.x != b.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));
            return list;
        }

        /// <summary>Fenstermodus, Auflösung, VSync und Bildraten-Limit anwenden (Fenster/Auflösung nicht im Editor).</summary>
        public static void ApplyDisplay()
        {
            ApplyFrameRate();
            if (Application.isEditor) return;
            try
            {
                var mode = ScreenMode;
                var res = TargetResolution();
                if (Screen.fullScreenMode != mode || Screen.width != res.x || Screen.height != res.y)
                    Screen.SetResolution(res.x, res.y, mode);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>Alle Grafik-, Audio- und Oberflächenwerte auf Standard (Tastenbelegung separat).</summary>
        public static void ResetToDefaults(string group)
        {
            switch (group)
            {
                case "grafik":
                    WindowMode = 1;
                    ResolutionWidth = ResolutionHeight = 0;
                    VSync = true;
                    FpsLimit = 0;
                    Quality = 2;
                    RenderScale = 1f;
                    Fov = 75f;
                    MotionBlur = false;
                    HeadBob = true;
                    CameraShake = true;
                    ShowFps = false;
                    break;
                case "audio":
                    MasterVolume = 0.85f;
                    MusicVolume = 0.5f;
                    SfxVolume = 0.8f;
                    break;
                case "steuerung":
                    MouseSensitivity = 1f;
                    PadSensitivity = 1f;
                    InvertY = false;
                    InputBindings.ResetAll();
                    break;
                case "gameplay":
                    ShowHints = true;
                    ShowCompass = true;
                    StreamerMode = false;
                    StreamerNoMusic = false;
                    LaptopDesign = "hype";
                    LaptopDark = true;
                    break;
                case "zugang":
                    UiScale = 1f;
                    Subtitles = true;
                    TextSpeed = 1;
                    ColorBlind = false;
                    ReduceMotion = false;
                    break;
            }
            Apply();
        }
    }
}
