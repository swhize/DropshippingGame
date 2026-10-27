namespace DropshippingGame.Core
{
    /// <summary>
    /// Englische Übersetzungen: 3D-Welt (Runtime/World, Player, Vehicles, Feel, Rendering: Schilder, Sprechblasen, Hinweise, NPC-Sprüche, Ladennamen).
    /// Einträge: Add("Deutsch", "English"); Platzhalter {0}, {1} … für eingesetzte Werte (Zahlen, Namen).
    /// </summary>
    internal static class EnWorld
    {
        public static void Register()
        {
            System.Action<string, string> A = Lang.Add;
        }
    }
}
