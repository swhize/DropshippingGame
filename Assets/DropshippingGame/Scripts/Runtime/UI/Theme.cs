using UnityEngine;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Farbwerte, die im Code gebraucht werden (die meisten Farben stehen als Variablen in Game.uss).
    /// Spiel-Oberfläche (HUD, Menüs): Gold-Akzent + Mint/Grün für Gutes + Rot für Schlechtes.
    /// Laptop/Handy („HustleOS“) folgen dem gewählten Design.
    /// </summary>
    public static class Theme
    {
        public static readonly Color Accent = new Color(1f, 0.74f, 0.26f);
        public static readonly Color AccentDark = new Color(0.86f, 0.58f, 0.12f);
        public static readonly Color Teal = new Color(0.32f, 0.86f, 0.78f);
        public static readonly Color Good = new Color(0.4f, 0.87f, 0.55f);
        public static readonly Color Bad = new Color(1f, 0.42f, 0.42f);
        public static readonly Color Text = new Color(0.95f, 0.96f, 0.97f);
        public static readonly Color Muted = new Color(0.62f, 0.66f, 0.74f);
        public static readonly Color Ink2 = new Color(0.79f, 0.81f, 0.85f);
        public static readonly Color Paper = new Color(0.984f, 0.973f, 0.945f);
        public static readonly Color PaperInk = new Color(0.14f, 0.13f, 0.11f);

        /// <summary>Gute/schlechte Farbe passend zum Laptop-Design (hell braucht dunklere Töne).</summary>
        public static Color LaptopGood => Settings.LaptopDark ? new Color(0.24f, 0.84f, 0.69f) : new Color(0.04f, 0.6f, 0.47f);
        public static Color LaptopBad => Settings.LaptopDark ? new Color(1f, 0.36f, 0.45f) : new Color(0.85f, 0.21f, 0.31f);
        public static Color LaptopAccent =>
            Settings.LaptopDesign == "hype"
                ? (Settings.LaptopDark ? new Color(1f, 0.48f, 0.27f) : new Color(0.91f, 0.36f, 0.14f))
                : (Settings.LaptopDark ? new Color(0.96f, 0.76f, 0.18f) : new Color(0.72f, 0.52f, 0.02f));
        public static Color LaptopAccent2 =>
            Settings.LaptopDesign == "hype"
                ? (Settings.LaptopDark ? new Color(1f, 0.7f, 0.28f) : new Color(0.96f, 0.62f, 0.04f))
                : LaptopAccent;
        public static Color LaptopMuted => Settings.LaptopDark ? new Color(0.55f, 0.58f, 0.67f) : new Color(0.36f, 0.39f, 0.47f);
        public static Color LaptopTeal => Settings.LaptopDark ? new Color(0.24f, 0.84f, 0.69f) : new Color(0.04f, 0.6f, 0.47f);
        public static Color LaptopInk => Settings.LaptopDark ? new Color(0.95f, 0.95f, 0.97f) : new Color(0.07f, 0.09f, 0.15f);

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        /// <summary>Lesbare Schriftfarbe auf einer Fläche.</summary>
        public static Color OnColor(Color bg) => bg.grayscale > 0.62f ? new Color(0.1f, 0.1f, 0.12f) : Color.white;
    }
}
