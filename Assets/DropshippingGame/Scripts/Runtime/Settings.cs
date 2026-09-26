using System;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Spieler-Einstellungen (Lautstärke, Maus, Sichtfeld, Grafik, UI-Größe, Laptop-Design ...).
    /// Werden in den PlayerPrefs gespeichert und sofort angewendet.
    /// </summary>
    public static class Settings
    {
        /// <summary>Version der gespeicherten Einstellungen. Ab 3 ist „Hype“ dunkel das Standard-Design.</summary>
        private const int PrefsVersion = 3;

        public const float UiScaleMin = 0.8f;
        public const float UiScaleMax = 1.3f;

        public static float MasterVolume = 0.85f;
        public static float MusicVolume = 0.5f;
        public static float SfxVolume = 0.8f;
        public static float MouseSensitivity = 1f;
        public static float PadSensitivity = 1f;
        public static float Fov = 75f;
        public static bool Fullscreen;
        public static bool HeadBob = true;
        public static bool InvertY;
        /// <summary>0 = Niedrig, 1 = Mittel, 2 = Hoch</summary>
        public static int Quality = 2;
        public static bool ShowFps;
        /// <summary>Laptop-Oberfläche: "hype" (Standard) oder "frachtbrief"</summary>
        public static string LaptopDesign = "hype";
        public static bool LaptopDark = true;
        /// <summary>Größe der gesamten Oberfläche (1 = 100 %).</summary>
        public static float UiScale = 1f;
        /// <summary>Tastenhinweise im HUD anzeigen.</summary>
        public static bool ShowHints = true;

        public static event Action Changed;
        private static bool _loaded;

        public static readonly string[] QualityNames = { "Niedrig", "Mittel", "Hoch" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = false;
            Changed = null;
        }

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            MasterVolume = PlayerPrefs.GetFloat("ds.master", MasterVolume);
            MusicVolume = PlayerPrefs.GetFloat("ds.music", MusicVolume);
            SfxVolume = PlayerPrefs.GetFloat("ds.sfx", SfxVolume);
            MouseSensitivity = PlayerPrefs.GetFloat("ds.mouse", MouseSensitivity);
            PadSensitivity = PlayerPrefs.GetFloat("ds.pad", PadSensitivity);
            Fov = PlayerPrefs.GetFloat("ds.fov", Fov);
            Fullscreen = PlayerPrefs.GetInt("ds.fullscreen", Fullscreen ? 1 : 0) == 1;
            HeadBob = PlayerPrefs.GetInt("ds.headbob", HeadBob ? 1 : 0) == 1;
            InvertY = PlayerPrefs.GetInt("ds.inverty", InvertY ? 1 : 0) == 1;
            Quality = Mathf.Clamp(PlayerPrefs.GetInt("ds.quality", Quality), 0, 2);
            ShowFps = PlayerPrefs.GetInt("ds.fps", ShowFps ? 1 : 0) == 1;
            LaptopDesign = PlayerPrefs.GetString("ds.laptop", LaptopDesign);
            LaptopDark = PlayerPrefs.GetInt("ds.laptopdark", LaptopDark ? 1 : 0) == 1;
            UiScale = Mathf.Clamp(PlayerPrefs.GetFloat("ds.uiscale", UiScale), UiScaleMin, UiScaleMax);
            ShowHints = PlayerPrefs.GetInt("ds.hints", ShowHints ? 1 : 0) == 1;
            if (LaptopDesign != "hype" && LaptopDesign != "frachtbrief") LaptopDesign = "hype";

            // v3.0: Das neue Standard-Design ist „Hype“ (dunkel). Ältere Einstellungen wurden bei
            // jeder Änderung komplett gespeichert – deshalb einmalig auf den neuen Standard setzen.
            if (PlayerPrefs.GetInt("ds.prefsver", 0) < PrefsVersion)
            {
                LaptopDesign = "hype";
                LaptopDark = true;
                PlayerPrefs.SetInt("ds.prefsver", PrefsVersion);
                Save();
            }
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("ds.master", MasterVolume);
            PlayerPrefs.SetFloat("ds.music", MusicVolume);
            PlayerPrefs.SetFloat("ds.sfx", SfxVolume);
            PlayerPrefs.SetFloat("ds.mouse", MouseSensitivity);
            PlayerPrefs.SetFloat("ds.pad", PadSensitivity);
            PlayerPrefs.SetFloat("ds.fov", Fov);
            PlayerPrefs.SetInt("ds.fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt("ds.headbob", HeadBob ? 1 : 0);
            PlayerPrefs.SetInt("ds.inverty", InvertY ? 1 : 0);
            PlayerPrefs.SetInt("ds.quality", Quality);
            PlayerPrefs.SetInt("ds.fps", ShowFps ? 1 : 0);
            PlayerPrefs.SetString("ds.laptop", LaptopDesign);
            PlayerPrefs.SetInt("ds.laptopdark", LaptopDark ? 1 : 0);
            PlayerPrefs.SetFloat("ds.uiscale", UiScale);
            PlayerPrefs.SetInt("ds.hints", ShowHints ? 1 : 0);
            PlayerPrefs.SetInt("ds.prefsver", PrefsVersion);
            PlayerPrefs.Save();
        }

        /// <summary>Nach jeder Änderung aufrufen: speichert, wendet an, benachrichtigt.</summary>
        public static void Apply()
        {
            UiScale = Mathf.Clamp(UiScale, UiScaleMin, UiScaleMax);
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

        public static void ApplyDisplay()
        {
            if (Application.isEditor) return;
            var mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Screen.fullScreenMode != mode)
            {
                if (Fullscreen) Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, mode);
                else Screen.SetResolution(1600, 900, mode);
            }
        }
    }
}
