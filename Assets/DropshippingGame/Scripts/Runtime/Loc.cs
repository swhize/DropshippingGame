using System;

namespace DropshippingGame
{
    /// <summary>
    /// Haken für die spätere Übersetzung (Englisch folgt mit einem Update). Heute ist alles Deutsch:
    /// <see cref="T"/> liefert den deutschen Text, außer es gibt bereits eine englische Fassung und
    /// <see cref="Settings.Language"/> ist „en“. Oberflächen, die sich bei einem Sprachwechsel neu
    /// aufbauen wollen, hängen sich an <see cref="Changed"/> (leitet <see cref="Settings.LanguageChanged"/> weiter).
    /// </summary>
    public static class Loc
    {
        public static string Language => Settings.Language;
        public static bool English => Settings.Language == "en";

        /// <summary>Text in der aktuellen Sprache (ohne englische Fassung: Deutsch).</summary>
        public static string T(string de, string en = null) => English && !string.IsNullOrEmpty(en) ? en : de;

        /// <summary>Deutschen Text über die Wörterbücher übersetzen (siehe <see cref="DropshippingGame.Core.Lang"/>).</summary>
        public static string Tr(string de) => DropshippingGame.Core.Lang.T(de);

        /// <summary>Sprache gewechselt (neuer Sprachcode).</summary>
        public static event Action<string> Changed
        {
            add => Settings.LanguageChanged += value;
            remove => Settings.LanguageChanged -= value;
        }
    }
}
