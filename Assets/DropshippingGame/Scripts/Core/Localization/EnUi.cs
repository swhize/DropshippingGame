namespace DropshippingGame.Core
{
    /// <summary>
    /// Englische Übersetzungen: Oberfläche außerhalb des Laptops (Runtime/UI: HUD, Handy, Menüs, Einstellungen, Hilfe, Admin, Dialoge, TikTok-Studio, Kasse, Finanz-UI).
    /// Einträge: Add("Deutsch", "English"); Platzhalter {0}, {1} … für eingesetzte Werte (Zahlen, Namen).
    /// </summary>
    internal static class EnUi
    {
        public static void Register()
        {
            System.Action<string, string> A = Lang.Add;
        }
    }
}
