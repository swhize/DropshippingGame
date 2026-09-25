using System.Collections.Generic;

namespace DropshippingGame.Core
{
    public sealed class ProductDef
    {
        public string Id, Name, Short, Icon;
        public float UnitCost;
        public int RefPrice;
        /// <summary>Kartongröße: 0 = S, 1 = M, 2 = L</summary>
        public int Size;
        public float Popularity;
        public int UnlockLevel;
        public RGBA Color;
    }

    public sealed class BulkOption
    {
        public string Name;
        public int Quantity, Level, Stage;
        public float Discount;
    }

    public sealed class SupplierDef
    {
        public string Name, Desc;
        public float Quality, PriceMult, LeadMult;
        public int Level;
    }

    public sealed class PackagingSize
    {
        public string Name;
        public int Cost;
    }

    public sealed class PackagingBatch
    {
        public int Qty;
        public float Mult;
    }

    public sealed class AdTier
    {
        public string Id, Name, Icon;
        public int Cost, Level;
        public float Mult, Minutes, Awareness;
    }

    public sealed class UpgradeDef
    {
        public string Id, Name, Icon, Requires, Desc;
        public int Cost, Level;
    }

    public sealed class ShopItemDef
    {
        public string Id, Name, Desc;
        public int Cost;
        /// <summary>Nur für Deko: -1 = überall, 1 = nur Lagerhalle</summary>
        public int Stage = -1;
    }

    public sealed class GoalDef
    {
        public string Id, Title, Desc, Type;
        public float Target;
        public int Reward;
        public bool Final;
    }

    public sealed class StaffRoleDef
    {
        public string Id, Name, Icon, Desc;
        public int Wage, Max;
        public float Interval;
        public RGBA Shirt;
    }

    public sealed class TutorialStep
    {
        public string Title, Text;
    }

    public sealed class LoanOption
    {
        public int Amount, Level;
    }

    /// <summary>
    /// Alle festen Spieldaten an einem Ort: Katalog, Balancing, Freischaltungen und Texte.
    /// Reine Konstanten - der veränderliche Spielzustand liegt in <see cref="Sim"/>.
    /// </summary>
    public static class GameData
    {
        // ---- Zeit -------------------------------------------------------------------------
        public const float DayStart = 480f;            // 08:00
        public const float DayEnd = 1200f;             // 20:00
        public const float BusinessMinutes = 720f;     // ein Geschäftstag = 12 Spielstunden
        public const float DayLengthSeconds = 540f;    // ... dauert 9 echte Minuten
        public const float MinutesPerSecond = BusinessMinutes / DayLengthSeconds;
        public const float IntroTime = 1050f;          // 17:30 - Schicht im Imbiss

        // ---- Start, Standorte, Kosten -------------------------------------------------------
        public const int StartCapital = 150;
        public const int BankruptLimit = -500;
        public static readonly string[] StageNames = { "Garage", "Lagerhalle" };
        public static readonly int[] StageRent = { 15, 140 };
        public static readonly int[] StageCapacity = { 400, 3000 };
        public static readonly int[] StageQueue = { 6, 14 };
        public const int GarageProducts = 3;

        // ---- Nachfrage & Lieferung -----------------------------------------------------------
        public const float BaseOrderMinutes = 72f;
        public const float OrderExpireMinutes = 720f;
        public const float PromoDiscount = 0.2f;
        public const float BaseLeadMinutes = 45f;
        public const int ExpressSurcharge = 8;
        public const float ReviewChance = 0.4f;

        // ---- Produkte -------------------------------------------------------------------------
        public static readonly ProductDef[] Products =
        {
            new ProductDef { Id = "huelle", Name = "Handyhülle", Short = "Hülle", UnitCost = 2f, RefPrice = 25, Size = 0, Popularity = 1.2f, UnlockLevel = 1, Color = new RGBA(0.93f, 0.42f, 0.62f), Icon = "phone" },
            new ProductDef { Id = "led", Name = "LED-Lichterkette", Short = "LED", UnitCost = 4f, RefPrice = 45, Size = 1, Popularity = 1.0f, UnlockLevel = 2, Color = new RGBA(0.98f, 0.8f, 0.24f), Icon = "bulb" },
            new ProductDef { Id = "massage", Name = "Massagepistole", Short = "Massage", UnitCost = 12f, RefPrice = 110, Size = 2, Popularity = 0.55f, UnlockLevel = 3, Color = new RGBA(0.32f, 0.34f, 0.42f), Icon = "bolt" },
            new ProductDef { Id = "kopfhoerer", Name = "Bluetooth-Kopfhörer", Short = "Kopfhörer", UnitCost = 8f, RefPrice = 69, Size = 1, Popularity = 0.8f, UnlockLevel = 4, Color = new RGBA(0.3f, 0.55f, 0.96f), Icon = "headphones" },
            new ProductDef { Id = "ringlicht", Name = "Ringlicht", Short = "Ringlicht", UnitCost = 6f, RefPrice = 60, Size = 1, Popularity = 0.85f, UnlockLevel = 5, Color = new RGBA(0.82f, 0.88f, 1.0f), Icon = "ring" },
            new ProductDef { Id = "katzenbrunnen", Name = "Katzen-Trinkbrunnen", Short = "Katzenbr.", UnitCost = 9f, RefPrice = 79, Size = 2, Popularity = 0.7f, UnlockLevel = 6, Color = new RGBA(0.36f, 0.78f, 0.84f), Icon = "drop" },
            new ProductDef { Id = "haltung", Name = "Haltungskorrektor", Short = "Haltung", UnitCost = 3f, RefPrice = 35, Size = 0, Popularity = 1.1f, UnlockLevel = 7, Color = new RGBA(0.58f, 0.46f, 0.9f), Icon = "user" },
            new ProductDef { Id = "smartwatch", Name = "Smartwatch", Short = "Watch", UnitCost = 15f, RefPrice = 129, Size = 0, Popularity = 0.6f, UnlockLevel = 8, Color = new RGBA(0.2f, 0.22f, 0.26f), Icon = "clock" },
            new ProductDef { Id = "beamer", Name = "Mini-Beamer", Short = "Beamer", UnitCost = 22f, RefPrice = 179, Size = 2, Popularity = 0.45f, UnlockLevel = 9, Color = new RGBA(0.95f, 0.55f, 0.25f), Icon = "screen" },
            new ProductDef { Id = "drohne", Name = "Kamera-Drohne", Short = "Drohne", UnitCost = 35f, RefPrice = 249, Size = 2, Popularity = 0.35f, UnlockLevel = 10, Color = new RGBA(0.85f, 0.2f, 0.25f), Icon = "drone" },
        };

        public static readonly BulkOption[] BulkOptions =
        {
            new BulkOption { Name = "Klein", Quantity = 20, Discount = 1.0f, Level = 1, Stage = 0 },
            new BulkOption { Name = "Mittel", Quantity = 50, Discount = 0.85f, Level = 1, Stage = 0 },
            new BulkOption { Name = "Groß", Quantity = 200, Discount = 0.68f, Level = 2, Stage = 0 },
            new BulkOption { Name = "Palette", Quantity = 500, Discount = 0.55f, Level = 5, Stage = 1 },
        };

        public static readonly SupplierDef[] Suppliers =
        {
            new SupplierDef { Name = "Billig-Fabrik", Quality = 0.6f, PriceMult = 0.65f, LeadMult = 1.8f, Level = 1, Desc = "Spottbillig, aber langsam und die Ware ist... naja." },
            new SupplierDef { Name = "Standard-Großhändler", Quality = 1.0f, PriceMult = 1.0f, LeadMult = 1.0f, Level = 1, Desc = "Solide Qualität, normale Lieferzeit." },
            new SupplierDef { Name = "Premium-Hersteller", Quality = 1.6f, PriceMult = 1.75f, LeadMult = 0.55f, Level = 2, Desc = "Teuer, blitzschnell, Kunden lieben es." },
        };

        // ---- Verpackung -------------------------------------------------------------------------
        public static readonly PackagingSize[] PackagingSizes =
        {
            new PackagingSize { Name = "S", Cost = 15 },
            new PackagingSize { Name = "M", Cost = 22 },
            new PackagingSize { Name = "L", Cost = 32 },
        };

        public static readonly PackagingBatch[] PackagingBatches =
        {
            new PackagingBatch { Qty = 10, Mult = 1.0f },
            new PackagingBatch { Qty = 50, Mult = 4.0f },
        };

        public const float PackagingFlatDiscount = 0.5f;

        // ---- Marketing -----------------------------------------------------------------------------
        public static readonly AdTier[] AdTiers =
        {
            new AdTier { Id = "flyer", Name = "Flyer & Plakate", Cost = 15, Mult = 1.25f, Minutes = 90f, Awareness = 0.02f, Level = 1, Icon = "paper" },
            new AdTier { Id = "facebook", Name = "Facebook-Ads", Cost = 60, Mult = 1.6f, Minutes = 120f, Awareness = 0.05f, Level = 2, Icon = "thumb" },
            new AdTier { Id = "google", Name = "Google-Ads", Cost = 160, Mult = 2.0f, Minutes = 180f, Awareness = 0.08f, Level = 6, Icon = "search" },
            new AdTier { Id = "influencer", Name = "Influencer-Kampagne", Cost = 650, Mult = 2.8f, Minutes = 240f, Awareness = 0.15f, Level = 7, Icon = "star" },
        };

        public const int TikTokLevel = 2;
        public const float TikTokCooldown = 120f;
        public const float TikTokMinMult = 1.2f;
        public const float TikTokMaxMult = 2.4f;
        public const float TikTokMinMinutes = 60f;
        public const float TikTokMaxMinutes = 180f;

        // ---- Branding --------------------------------------------------------------------------------
        public static readonly RGBA[] BrandPalette =
        {
            new RGBA(0.9f, 0.26f, 0.26f), new RGBA(0.95f, 0.52f, 0.18f), new RGBA(0.96f, 0.78f, 0.2f), new RGBA(0.3f, 0.78f, 0.42f),
            new RGBA(0.2f, 0.7f, 0.72f), new RGBA(0.22f, 0.5f, 0.9f), new RGBA(0.5f, 0.36f, 0.88f), new RGBA(0.9f, 0.36f, 0.7f),
            new RGBA(0.14f, 0.14f, 0.16f), new RGBA(0.95f, 0.95f, 0.95f),
        };

        public static readonly string[] LogoNames = { "Kreis", "Quadrat", "Raute", "Stern", "Blitz", "Herz" };

        // ---- Fortschritt ------------------------------------------------------------------------------
        public static readonly int[] LevelXp = { 0, 150, 500, 1200, 2500, 4500, 7500, 12000, 18000, 26000 };
        public const int MaxLevel = 10;

        public static readonly Dictionary<int, string> LevelUnlocks = new Dictionary<int, string>
        {
            { 2, "LED-Lichterkette · Premium-Hersteller · Facebook-Ads · TikTok · Großbestellung · Verkaufsstand" },
            { 3, "Massagepistole · Trading-App · Shop-Server-Upgrade · größerer Kredit" },
            { 4, "Lagerhalle kaufbar · Bluetooth-Kopfhörer" },
            { 5, "Ringlicht · Personal · Palettenbestellung · eigener Lieferwagen" },
            { 6, "Katzen-Trinkbrunnen · Google-Ads · Förderband" },
            { 7, "Haltungskorrektor · Influencer-Kampagnen · Großkredit" },
            { 8, "Smartwatch · Hochregal-Erweiterung" },
            { 9, "Mini-Beamer" },
            { 10, "Kamera-Drohne · Legendenstatus" },
        };

        public const int TradingLevel = 3;
        public const int StaffLevel = 5;

        public static readonly UpgradeDef[] Upgrades =
        {
            new UpgradeDef { Id = "stand", Name = "Verkaufsstand", Icon = "store", Cost = 300, Level = 2, Requires = "",
                Desc = "Ein Stand vor der Garage. Stell eine Kiste drauf – Passanten kaufen direkt, ganz ohne Karton und Versand." },
            new UpgradeDef { Id = "server", Name = "Shop-Server-Upgrade", Icon = "server", Cost = 800, Level = 3, Requires = "",
                Desc = "+6 Plätze in der Bestell-Warteschlange. Weniger verlorene Bestellungen." },
            new UpgradeDef { Id = "warehouse", Name = "Lagerhalle kaufen", Icon = "building", Cost = 4500, Level = 4, Requires = "",
                Desc = "Endlich raus aus der Garage: 3.000 Lagerplätze, 10 Regale, Platz für Personal. Miete 140 €/Tag." },
            new UpgradeDef { Id = "van", Name = "Eigener Lieferwagen", Icon = "truck", Cost = 3500, Level = 5, Requires = "",
                Desc = "Alle Einkäufe kommen 25 % schneller an." },
            new UpgradeDef { Id = "conveyor", Name = "Förderband", Icon = "gear", Cost = 1500, Level = 6, Requires = "warehouse",
                Desc = "Etikettierte Pakete aufs Band legen - sie werden automatisch verschickt. 10 €/Tag Strom." },
            new UpgradeDef { Id = "highrack", Name = "Hochregal-Erweiterung", Icon = "rack", Cost = 2500, Level = 8, Requires = "warehouse",
                Desc = "+3.000 Lagerplätze." },
        };

        public static readonly ShopItemDef[] Decor =
        {
            new ShopItemDef { Id = "pflanze", Name = "Zimmerpflanze FLÖRP", Cost = 15 },
            new ShopItemDef { Id = "poster", Name = "Motivationsposter", Cost = 20 },
            new ShopItemDef { Id = "stehlampe", Name = "Stehlampe GLÜMP", Cost = 30 },
            new ShopItemDef { Id = "teppich", Name = "Teppich LÅNGSAM", Cost = 35 },
            new ShopItemDef { Id = "billy", Name = "BILLY-Regal", Cost = 40 },
            new ShopItemDef { Id = "whiteboard", Name = "Whiteboard mit Businessplan", Cost = 60 },
            new ShopItemDef { Id = "kaffee", Name = "Siebträgermaschine", Cost = 180 },
            new ShopItemDef { Id = "sofa", Name = "Ledersofa (nur Lagerhalle)", Cost = 250, Stage = 1 },
        };

        public static readonly ShopItemDef[] Lifestyle =
        {
            new ShopItemDef { Id = "sneaker", Name = "Designer-Sneaker", Cost = 150, Desc = "Stehen in der Vitrine. Zum Anschauen, nicht zum Tragen." },
            new ShopItemDef { Id = "gamingstuhl", Name = "Gaming-Stuhl", Cost = 300, Desc = "RGB macht 20 % produktiver. Angeblich." },
            new ShopItemDef { Id = "neon", Name = "Neon-Schild 'HUSTLE'", Cost = 400, Desc = "Leuchtet dich jeden Morgen motivierend an." },
            new ShopItemDef { Id = "auto", Name = "Gebrauchter Kombi", Cost = 1500, Desc = "Parkt vor der Garage. TÜV bis nächsten Monat." },
            new ShopItemDef { Id = "uhr", Name = "Protzige Uhr", Cost = 5000, Desc = "Deine Uhrzeit-Anzeige wird golden." },
            new ShopItemDef { Id = "sportwagen", Name = "Sportwagen", Cost = 25000, Desc = "Parkt vor der Lagerhalle. Nachbarn gucken." },
            new ShopItemDef { Id = "penthouse", Name = "Penthouse mit Stadtblick", Cost = 80000, Desc = "Das oberste Stockwerk im Hochhaus gegenüber leuchtet golden." },
        };

        public static readonly GoalDef[] Goals =
        {
            new GoalDef { Id = "first_sale", Title = "Der erste Verkauf", Desc = "Verschicke dein erstes Paket.", Type = "shipped", Target = 1, Reward = 50 },
            new GoalDef { Id = "brand", Title = "Eine echte Marke", Desc = "Gib deinem Shop in der Branding-App einen eigenen Namen.", Type = "brand", Target = 1, Reward = 25 },
            new GoalDef { Id = "ship10", Title = "Läuft bei dir", Desc = "Verschicke 10 Pakete.", Type = "shipped", Target = 10, Reward = 100 },
            new GoalDef { Id = "rev1k", Title = "Vierstellig", Desc = "Erreiche 1.000 € Umsatz.", Type = "revenue", Target = 1000, Reward = 150 },
            new GoalDef { Id = "stand10", Title = "Straßenhändler", Desc = "Verkaufe 10 Artikel am Verkaufsstand.", Type = "stand", Target = 10, Reward = 120 },
            new GoalDef { Id = "rating4", Title = "Kundenliebling", Desc = "Erreiche eine Bewertung von 4,0 Sternen.", Type = "rating", Target = 4.0f, Reward = 200 },
            new GoalDef { Id = "ship100", Title = "Paketprofi", Desc = "Verschicke 100 Pakete.", Type = "shipped", Target = 100, Reward = 300 },
            new GoalDef { Id = "warehouse", Title = "Raus aus der Garage", Desc = "Kaufe die Lagerhalle (Ausbau-App).", Type = "stage", Target = 1, Reward = 500 },
            new GoalDef { Id = "staff1", Title = "Chef sein", Desc = "Stelle deinen ersten Mitarbeiter ein.", Type = "staff", Target = 1, Reward = 200 },
            new GoalDef { Id = "rev10k", Title = "Fünfstellig", Desc = "Erreiche 10.000 € Umsatz.", Type = "revenue", Target = 10000, Reward = 1000 },
            new GoalDef { Id = "level7", Title = "Volles Sortiment", Desc = "Erreiche Firmenlevel 7.", Type = "level", Target = 7, Reward = 1500 },
            new GoalDef { Id = "staff4", Title = "Kleines Team", Desc = "Beschäftige 4 Mitarbeiter gleichzeitig.", Type = "staff", Target = 4, Reward = 1000 },
            new GoalDef { Id = "rev100k", Title = "Imperium", Desc = "Erreiche 100.000 € Umsatz. Kalle wird staunen.", Type = "revenue", Target = 100000, Reward = 10000, Final = true },
        };

        public static readonly StaffRoleDef[] StaffRoles =
        {
            new StaffRoleDef { Id = "lager", Name = "Lagerist:in", Icon = "box", Wage = 60, Interval = 16f, Max = 2,
                Desc = "Holt Kisten vom Wareneingang und räumt sie ins passende Regal.", Shirt = new RGBA(0.25f, 0.45f, 0.8f) },
            new StaffRoleDef { Id = "packer", Name = "Packer:in", Icon = "package", Wage = 70, Interval = 12f, Max = 2,
                Desc = "Kommissioniert offene Bestellungen und verpackt sie (braucht Kartons!).", Shirt = new RGBA(0.3f, 0.65f, 0.4f) },
            new StaffRoleDef { Id = "versand", Name = "Versandkraft", Icon = "truck", Wage = 65, Interval = 10f, Max = 2,
                Desc = "Etikettiert fertige Pakete und verschickt sie.", Shirt = new RGBA(0.85f, 0.5f, 0.2f) },
            new StaffRoleDef { Id = "social", Name = "Social-Media-Praktikant:in", Icon = "phone", Wage = 45, Interval = 0f, Max = 1,
                Desc = "Postet jeden Vormittag ein TikTok. Mal viral, mal peinlich.", Shirt = new RGBA(0.75f, 0.35f, 0.75f) },
        };

        public static readonly string[] StaffNames =
        {
            "Jonas", "Leonie", "Mehmet", "Sophie", "Luca", "Aylin", "Finn", "Mia", "Kevin",
            "Chantal", "Emre", "Lena", "Tim", "Sara", "Nico", "Paula",
        };

        // ---- Bank ------------------------------------------------------------------------------------
        public static readonly LoanOption[] Loans =
        {
            new LoanOption { Amount = 500, Level = 1 },
            new LoanOption { Amount = 2000, Level = 3 },
            new LoanOption { Amount = 5000, Level = 5 },
            new LoanOption { Amount = 15000, Level = 7 },
        };

        /// <summary>Zinsen pro Tag auf die offene Kreditsumme.</summary>
        public const float LoanDailyInterest = 0.02f;

        /// <summary>Kreditrahmen: Summe aller Kredite, die man sich leisten darf.</summary>
        public static int CreditLimit(int level) => 500 + (level - 1) * 1500 + (level >= 7 ? 8000 : 0);

        // ---- Verkaufsstand ---------------------------------------------------------------------------
        public const int StandCapacity = 60;
        public const float StandPriceFactor = 0.9f;

        // ---- Bewertungen ---------------------------------------------------------------------------
        public static readonly Dictionary<int, string[]> ReviewTexts = new Dictionary<int, string[]>
        {
            { 5, new[] { "Top! Kam super schnell an.", "Genau wie beschrieben, gerne wieder.", "Meine Oma liebt es. 10/10.", "Die Verpackung allein ist schon ein Erlebnis.", "Schneller als der Pizzadienst!" } },
            { 4, new[] { "Gutes Produkt, Versand okay.", "Macht, was es soll.", "Solide. Karton war etwas zerdrückt.", "Würde wieder kaufen." } },
            { 3, new[] { "Naja. Hatte es mir größer vorgestellt.", "Ganz okay für den Preis.", "Hat gedauert, ist aber angekommen." } },
            { 2, new[] { "Riecht irgendwie nach Plastik.", "Hat ewig gedauert.", "Anleitung nur auf Chinesisch." } },
            { 1, new[] { "Nie angekommen. Nie wieder!", "Nach fünf Minuten kaputt.", "Ich will mein Geld zurück!!!", "Sieht nicht aus wie auf dem Foto." } },
        };

        public static readonly string[] ReviewNames =
        {
            "Sabine K.", "Dennis", "Oma Gerda", "xX_Gamer_Xx", "Jürgen aus Bottrop", "Laura M.",
            "Tobi", "Frau Schmidt", "Ben", "Anonym", "Kevin H.", "Mareike",
        };

        // ---- Tutorial --------------------------------------------------------------------------------
        public static readonly TutorialStep[] Tutorial =
        {
            new TutorialStep { Title = "Willkommen in deiner Garage", Text = "Geh zum Laptop auf der Werkbank und öffne ihn." },
            new TutorialStep { Title = "Ware einkaufen", Text = "Öffne im Laptop die App 'Einkauf' und bestelle 20 Handyhüllen." },
            new TutorialStep { Title = "Lieferung annehmen", Text = "Der Lieferwagen kommt gleich. Nimm die Kiste an der Lieferpalette neben dem Tor." },
            new TutorialStep { Title = "Einlagern", Text = "Bring die Kiste zum Regal 'Handyhülle' und räume sie ein." },
            new TutorialStep { Title = "Online gehen", Text = "Öffne am Laptop den 'Webshop' und stell die Handyhülle online." },
            new TutorialStep { Title = "Erste Bestellung", Text = "Warte auf deine erste Bestellung. Sie erscheint oben links." },
            new TutorialStep { Title = "Kommissionieren", Text = "Nimm eine Handyhülle für die Bestellung aus dem Regal." },
            new TutorialStep { Title = "Verpacken", Text = "Verpacke die Hülle am Packtisch in einen Karton." },
            new TutorialStep { Title = "Etikettieren", Text = "Nimm das Paket und druck am Labeldrucker ein Versandlabel." },
            new TutorialStep { Title = "Versenden", Text = "Bring das Paket zur gelben PaketBlitz-Box vor der Garage." },
        };

        public static readonly string[] KalleIdle =
        {
            "Na, Herr Unternehmer? Schon Millionär?",
            "Die Fritteuse vermisst dich. Ich nicht.",
            "Willst du 'nen Döner? Geht aufs Haus. Ausnahmsweise.",
            "Mein Neffe macht auch Internet. Der verkauft Socken.",
            "Früher hattest du Fett an den Fingern. Heute Kartonstaub.",
        };

        public static readonly string[] StandShouts =
        {
            "Oh, das nehm ich mit!", "Was kostet das? ... Gekauft!", "Für meine Nichte. Perfekt.",
            "Hab ich auf TikTok gesehen!", "Zwei Euro billiger als online? Deal.",
        };

        // ---- Hilfsfunktionen ------------------------------------------------------------------------
        public static ProductDef Product(string id)
        {
            foreach (var p in Products)
                if (p.Id == id) return p;
            return Products[0];
        }

        public static int ProductIndex(string id)
        {
            for (int i = 0; i < Products.Length; i++)
                if (Products[i].Id == id) return i;
            return 0;
        }

        public static bool IsProduct(string id)
        {
            foreach (var p in Products)
                if (p.Id == id) return true;
            return false;
        }

        public static StaffRoleDef StaffRole(string id)
        {
            foreach (var r in StaffRoles)
                if (r.Id == id) return r;
            return StaffRoles[0];
        }

        public static UpgradeDef Upgrade(string id)
        {
            foreach (var u in Upgrades)
                if (u.Id == id) return u;
            return null;
        }

        public static ShopItemDef FindShopItem(ShopItemDef[] list, string id)
        {
            foreach (var e in list)
                if (e.Id == id) return e;
            return null;
        }

        public static int LevelThreshold(int lvl) => LevelXp[Mathx.Clamp(lvl - 1, 0, LevelXp.Length - 1)];
        public static string SizeName(int size) => PackagingSizes[Mathx.Clamp(size, 0, 2)].Name;

        public static string QualityName(float q) => q >= 1.3f ? "Premium" : (q < 0.8f ? "Billig" : "Standard");
    }
}
