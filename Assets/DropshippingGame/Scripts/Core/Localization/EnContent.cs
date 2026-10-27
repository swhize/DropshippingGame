namespace DropshippingGame.Core
{
    /// <summary>
    /// Englische Übersetzungen: Spielinhalte aus GameData*.cs (Produkte, Upgrades, Skills, Perks, Ziele, Bewertungen, Kundennotizen, Lieferanten, Mitarbeiter, Tutorial, Onboarding-Mails), ShopData, Ads.Content, FinanceText, Achievements, Difficulty, Vehicles.
    /// Einträge: Add("Deutsch", "English"); Platzhalter {0}, {1} … für eingesetzte Werte (Zahlen, Namen).
    /// </summary>
    internal static class EnContent
    {
        public static void Register()
        {
            System.Action<string, string> A = Lang.Add;
        }
    }
}
