namespace DropshippingGame.Core
{
    /// <summary>
    /// Englische Übersetzungen: Ereignisse und Mails (EventData.cs, EventSystem.cs, Trends, Market-News, Festival, Endgame).
    /// Einträge: Add("Deutsch", "English"); Platzhalter {0}, {1} … für eingesetzte Werte (Zahlen, Namen).
    /// </summary>
    internal static class EnEvents
    {
        public static void Register()
        {
            System.Action<string, string> A = Lang.Add;
        }
    }
}
