namespace DropshippingGame.Core
{
    /// <summary>
    /// Kleine Schutz-Hilfen gegen ungültige Eingaben der öffentlichen API (gefunden mit dem Chaos-Test,
    /// Tests/EditMode/ChaosTests.cs). Ungültige Indizes, unbekannte IDs und nicht endliche Zahlen
    /// führen zu "nichts passiert" statt zu Ausnahmen oder kaputtem Zustand.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>true, wenn index ein gültiger Index für eine Liste der Länge length ist.</summary>
        public static bool ValidIndex(int index, int length) => index >= 0 && index < length;

        /// <summary>Kontostand bleibt in ±2 Mrd. € (Buchungen über <see cref="AdjustMoney"/> laufen nie über int).</summary>
        public const int MoneyLimit = 2000000000;

        /// <summary>Höchstbudget einer Fakebook-/Gugel-Kampagne (die Apps bieten höchstens 250 € an).</summary>
        public const int MaxAdBudget = 10000;

        /// <summary>Obergrenze für Erfahrungspunkte (kein int-Überlauf, weit über dem letzten Level).</summary>
        public const int MaxXp = 1000000000;

        /// <summary>Endliche Zahl (kein NaN/±∞)?</summary>
        public static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
