namespace DropshippingGame.Core
{
    /// <summary>
    /// Englische Übersetzungen: Allgemeine Kernbegriffe, Einheiten, Wochentage, Formate, Sim-Meldungen (Notify) aus Sim*.cs.
    /// Einträge: Add("Deutsch", "English"); Platzhalter {0}, {1} … für eingesetzte Werte (Zahlen, Namen).
    /// </summary>
    internal static class EnCore
    {
        public static void Register()
        {
            System.Action<string, string> A = Lang.Add;
        }
    }
}
