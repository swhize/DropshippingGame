using System;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Balance-Regeln aus dem Kampagnen-Tuning (siehe docs/GDD.md "Zahlen &amp; Balance" und
    /// Tests/EditMode/BalanceTests.cs): Personal-Staffelung, Wucherpreise, Fest-Kaufkraft.
    /// </summary>
    public static partial class GameData
    {
        /// <summary>Ab diesem Firmenlevel darf je Rolle eine zweite Kraft eingestellt werden (Rollen ohne Eintrag: nie).</summary>
        public static int StaffSecondSlotLevel(string roleId)
        {
            switch (roleId)
            {
                case "packer":
                case "versand":
                case "lager":
                    return 10;
            }
            return 99;
        }
        /// <summary>Preis über Markenpreis × diesem Faktor: Kundschaft fühlt sich abgezockt (−1 Stern, mehr Retouren).</summary>
        public const float PriceGougeMild = 1.25f;
        /// <summary>Ab hier −2 Sterne.</summary>
        public const float PriceGougeHard = 1.6f;
        /// <summary>Retouren-Faktor bei Wucherpreisen.</summary>
        public const float PriceGougeReturnMult = 1.5f;
        /// <summary>Festbesucher geben spontan etwa so viel pro Teil aus; teurere Ware verkauft sich seltener.</summary>
        public const float FestivalImpulseBudget = 45f;
        /// <summary>
        /// Viele Produkte und Boosts zusammen bringen weniger als linear mehr Bestellungen (Exponent auf die
        /// Summe der Nachfrage über 1). Verhindert, dass die Warteschlange ab der Lagerhalle dauernd überläuft.
        /// </summary>
        public const float DemandCompression = 0.65f;
        /// <summary>Preis-Elastizität über dem Markenpreis (darunter 1,6): Wucher kostet viel Kundschaft.</summary>
        public const double PriceElasticityAboveBrand = 2.6;
    }

    public sealed partial class Sim
    {
        /// <summary>Höchstzahl je Rolle: zunächst eine Kraft, ab Level <see cref="GameData.StaffSecondSlotLevel"/> die volle Zahl.</summary>
        public int StaffMax(string roleId)
        {
            var role = GameData.StaffRole(roleId);
            return Level >= GameData.StaffSecondSlotLevel(roleId) ? role.Max : Math.Min(1, role.Max);
        }

        /// <summary>Gesamtnachfrage → wirksame Nachfrage (bis 1 linear, darüber gedämpft).</summary>
        public static float CompressedDemand(float total) => total <= 1f ? total : (float)Math.Pow(total, GameData.DemandCompression);

        /// <summary>Verhältnis Verkaufspreis (ohne Express-Aufschlag) zum Markenpreis (Markt × Markenfaktor).</summary>
        public float PricePremium(string pid, int price, bool express)
        {
            if (!GameData.IsProduct(pid)) return 1f;
            float basePrice = express ? price / GameData.ExpressPriceMult : price;
            float fair = Math.Max(1f, Market.MarketPrice(pid) * BrandPriceMult());
            return basePrice / fair;
        }

        /// <summary>Sterne-Abzug für Wucherpreise (0, 1 oder 2).</summary>
        public int PriceStarPenalty(ItemData pkg)
        {
            if (pkg == null) return 0;
            float r = PricePremium(pkg.Product, pkg.Price, pkg.Express);
            return r >= GameData.PriceGougeHard ? 2 : (r >= GameData.PriceGougeMild ? 1 : 0);
        }

        /// <summary>Kaufbereitschaft am Fest (0,3..1): spontan kauft man eher günstige Sachen.</summary>
        public static float FestivalPriceAppeal(int price) => Mathx.Clamp(GameData.FestivalImpulseBudget / Math.Max(1f, price), 0.3f, 1f);
    }
}
