using System;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Spieler-Einstellungen (Lautstärke, Maus, Sichtfeld, Grafik, Laptop-Design ...).
    /// Werden in den PlayerPrefs gespeichert und sofort angewendet.
    /// </summary>
    public static class Settings
    {
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
        /// <summary>Laptop-Oberfläche: "frachtbrief" oder "hype"</summary>
        public static string LaptopDesign = "frachtbrief";
        public static bool LaptopDark = true;

        public static event Action Changed;
        private static bool _loaded;

        public static readonly string[] QualityNames = { "Niedrig", "Mittel", "Hoch" };

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
            PlayerPrefs.Save();
        }

        /// <summary>Nach jeder Änderung aufrufen: speichert, wendet an, benachrichtigt.</summary>
        public static void Apply()
        {
            Save();
            ApplyDisplay();
            Changed?.Invoke();
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
