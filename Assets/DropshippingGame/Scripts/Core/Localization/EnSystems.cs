namespace DropshippingGame.Core
{
    /// <summary>
    /// Englische Übersetzungen: Sonstiges/Nachträge (fehlende Texte, die beim Testen auffallen).
    /// Einträge: Add("Deutsch", "English"); Platzhalter {0}, {1} … für eingesetzte Werte (Zahlen, Namen).
    /// </summary>
    internal static class EnSystems
    {
        public static void Register()
        {
            System.Action<string, string> A = Lang.Add;
        }
    }
}
