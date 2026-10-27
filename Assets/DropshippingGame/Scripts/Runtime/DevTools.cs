namespace DropshippingGame
{
    /// <summary>
    /// Schalter für Entwickler-Werkzeuge: Admin-Panel (F10/F9/^), Noclip (Alt), Vogelperspektive,
    /// Teleports und Test-Knöpfe. An im Unity-Editor, in Development-Builds und wenn das
    /// Scripting-Define <c>DS_DEV</c> gesetzt ist – in Release-Builds immer aus (die Tasten tun dann nichts).
    /// Release-/Development-Builds erzeugt <c>Dropshipping › Build</c> (Editor/BuildTools.cs).
    /// </summary>
    public static class DevTools
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DS_DEV
        /// <summary>true = Entwickler-Werkzeuge verfügbar.</summary>
        public static readonly bool Enabled = true;
#else
        /// <summary>true = Entwickler-Werkzeuge verfügbar.</summary>
        public static readonly bool Enabled = false;
#endif
    }
}
