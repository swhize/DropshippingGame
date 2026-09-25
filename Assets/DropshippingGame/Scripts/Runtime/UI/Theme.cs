using UnityEngine;

namespace DropshippingGame.UI
{
    /// <summary>Farbwerte, die im Code gebraucht werden (die meisten Farben stehen in den USS-Dateien).</summary>
    public static class Theme
    {
        public static readonly Color Accent = new Color(1f, 0.74f, 0.26f);
        public static readonly Color AccentDark = new Color(0.86f, 0.58f, 0.12f);
        public static readonly Color Teal = new Color(0.32f, 0.86f, 0.78f);
        public static readonly Color Good = new Color(0.42f, 0.88f, 0.52f);
        public static readonly Color Bad = new Color(1f, 0.44f, 0.44f);
        public static readonly Color Text = new Color(0.94f, 0.95f, 0.97f);
        public static readonly Color Muted = new Color(0.62f, 0.66f, 0.74f);

        /// <summary>Gute/schlechte Farbe passend zum Laptop-Design (hell braucht dunklere Töne).</summary>
        public static Color LaptopGood => Settings.LaptopDark ? new Color(0.42f, 0.84f, 0.56f) : new Color(0.12f, 0.54f, 0.3f);
        public static Color LaptopBad => Settings.LaptopDark ? new Color(0.96f, 0.46f, 0.4f) : new Color(0.78f, 0.22f, 0.18f);
        public static Color LaptopAccent =>
            Settings.LaptopDesign == "hype"
                ? (Settings.LaptopDark ? new Color(1f, 0.48f, 0.27f) : new Color(0.91f, 0.36f, 0.14f))
                : (Settings.LaptopDark ? new Color(0.96f, 0.76f, 0.18f) : new Color(0.72f, 0.52f, 0.02f));
        public static Color LaptopMuted => Settings.LaptopDark ? new Color(0.56f, 0.59f, 0.61f) : new Color(0.42f, 0.44f, 0.45f);
        public static Color LaptopTeal => Settings.LaptopDark ? new Color(0.24f, 0.84f, 0.69f) : new Color(0.04f, 0.6f, 0.47f);

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
    }
}
