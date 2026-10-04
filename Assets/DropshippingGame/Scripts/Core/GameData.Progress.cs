using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>Ein Hustle-Perk: mehrstufige Freischaltung, bezahlt mit Hustle-Punkten aus Erfahrung (XP).</summary>
    public sealed class PerkDef
    {
        public string Id, Name, Icon, Desc;
        /// <summary>Wirkung je Stufe als kurzer Text, z. B. "+5 % Nachfrage".</summary>
        public string Effect;
        public int MaxRank = 5;
        /// <summary>Benötigtes Firmenlevel.</summary>
        public int Level = 1;
    }

    /// <summary>Eine Stufe der Einrichtung (Laptop, Bett, Stuhl, Schreibtisch, Kaffee).</summary>
    public sealed class HomeItemDef
    {
        public string Id, Slot, Name, Icon, Desc;
        /// <summary>Wirkung als kurzer Text für die Oberfläche.</summary>
        public string Effect;
        /// <summary>Stufe 1 oder 2 (0 = Grundausstattung, nicht kaufbar).</summary>
        public int Tier;
        public int Cost;
        public int Level = 1;
        /// <summary>Optional: Deko-/Lifestyle-ID, die mit dieser Stufe in der Welt erscheint ("kaffee", "gamingstuhl").</summary>
        public string Grants = "";
    }

    /// <summary>Ein Einrichtungs-Platz mit Grundausstattung.</summary>
    public sealed class HomeSlotDef
    {
        public string Id, Name, Icon, BaseName, BaseEffect;
    }

    /// <summary>Kaufbare Ausrüstung (Faltmaschine, Labelgerät, zusätzliche Stationen, Container).</summary>
    public sealed class EquipmentDef
    {
        public string Id, Name, Icon, Desc, Effect;
        public int Cost, Level = 1;
        /// <summary>Höchstzahl in der Garage / in der Lagerhalle (mehr wird nicht aufgestellt und wirkt nicht).</summary>
        public int MaxGarage = 1, MaxWarehouse = 1;
    }

    /// <summary>Versanddienstleister für Kundenpakete.</summary>
    public sealed class CarrierDef
    {
        public string Id, Name, Icon, Desc;
        /// <summary>Faktor auf das Porto und fester Aufschlag je Paket (€).</summary>
        public float PortoMult, PortoExtra;
        /// <summary>Zuverlässig = keine Verspätung durch Streik/Unwetter.</summary>
        public bool Reliable;
        /// <summary>Chance auf +1 Stern in der Bewertung (Kundschaft freut sich über schnelle Lieferung).</summary>
        public float StarBonusChance;
    }

    /// <summary>Störung beim Paketdienst (Streik, Unwetter ...).</summary>
    public sealed class DisruptionDef
    {
        public string Id, Name, Icon, Text, Review;
        /// <summary>Anteil der Standard-Pakete, die währenddessen verspätet ankommen.</summary>
        public float DelayChance;
    }

    /// <summary>Geschäftskunden-Tarif: Porto sinkt mit der Zahl verschickter Pakete.</summary>
    public sealed class PortoTier
    {
        public string Name;
        public int Shipped;
        public float Mult;
    }

    /// <summary>Mengenrabatt: Einkaufspreis sinkt mit der insgesamt gekauften Stückzahl eines Produkts.</summary>
    public sealed class VolumeTier
    {
        public int Units;
        public float Discount;
    }

    /// <summary>
    /// Inhalte für Fortschritt &amp; Wirtschaftstiefe: Hustle-Perks, Einrichtung &amp; Energie, Ausrüstung,
    /// Porto &amp; Paketdienste, Lieferstörungen, fehlerhafte Chargen, Verpackungsmüll, Großhändler und
    /// Markenstärke. Logik in <c>Sim.Perks.cs</c>, <c>Sim.Home.cs</c>, <c>Sim.Equipment.cs</c>,
    /// <c>Sim.Logistics.cs</c>, <c>Sim.Waste.cs</c> und <c>Sim.Brand.cs</c>.
    /// </summary>
    public static partial class GameData
    {
        // =====================================================================================
        // Regale in der Garage
        // =====================================================================================
        /// <summary>Produkte mit eigenem Regal in der Garage (in dieser Reihenfolge). In der Lagerhalle hat jedes Produkt ein Regal.</summary>
        public static readonly string[] GarageShelves = { "huelle", "led", "massage", "toaster", "bartglitzer" };

        public static bool HasGarageShelf(string id) => Array.IndexOf(GarageShelves, id) >= 0;

        // =====================================================================================
        // Hustle-Perks (Erfahrung ausgeben)
        // =====================================================================================
        /// <summary>
        /// XP-Schwelle für den k-ten Hustle-Punkt: 500 · k^1,5 (500, 1.414, 2.598, 4.000 ...).
        /// Bis Level 10 (30.000 XP) kommen so rund 15 Punkte zusammen – und nach Level 10 geht es weiter.
        /// </summary>
        public static int PerkXpThreshold(int k) => k <= 0 ? 0 : (int)Math.Round(500.0 * Math.Pow(k, 1.5));

        /// <summary>Wie viele Hustle-Punkte diese Erfahrung insgesamt bringt.</summary>
        public static int PerkPointsForXp(int xp)
        {
            int k = 0;
            while (k < 500 && PerkXpThreshold(k + 1) <= xp) k++;
            return k;
        }

        public static readonly PerkDef[] Perks =
        {
            new PerkDef { Id = "p_kunden", Name = "Mehr Kundschaft", Icon = "users", Effect = "+5 % Nachfrage",
                Desc = "Bessere Produktfotos, mehr Hashtags: Es kommen mehr Bestellungen rein." },
            new PerkDef { Id = "p_queue", Name = "Größere Warteschlange", Icon = "list", Effect = "+2 Plätze",
                Desc = "Dein Shop-System verkraftet mehr offene Bestellungen gleichzeitig." },
            new PerkDef { Id = "p_einkauf", Name = "Einkaufsprofi", Icon = "coin", Effect = "−3 % Einkaufspreis",
                Desc = "Du kennst alle Gutscheincodes. Wirklich alle." },
            new PerkDef { Id = "p_liefer", Name = "Schnelle Lieferanten", Icon = "truck", Effect = "−6 % Lieferzeit",
                Desc = "Die Spedition kennt deinen Namen und fährt schneller." },
            new PerkDef { Id = "p_porto", Name = "Versandrabatt", Icon = "send", Effect = "−6 % Porto",
                Desc = "Du verhandelst mit dem Paketdienst. Mit Kuchen." },
            new PerkDef { Id = "p_team", Name = "Schnelleres Personal", Icon = "gear", Effect = "+8 % Arbeitstempo", Level = 5,
                Desc = "Motivationsposter, Obstkorb, Bluetooth-Box: Dein Team packt schneller." },
            new PerkDef { Id = "p_lager", Name = "Mehr Lagerplatz", Icon = "rack", Effect = "+10 % Lagerplatz", MaxRank = 3,
                Desc = "Stapeln bis unter die Decke. Statiker sind überbewertet." },
            new PerkDef { Id = "p_tragen", Name = "Packesel", Icon = "package", Effect = "+1 Paket tragen", MaxRank = 2, Level = 3,
                Desc = "Du trägst mehr Pakete auf einmal. Rückenschule nicht inklusive." },
            new PerkDef { Id = "p_ausdauer", Name = "Ausdauer", Icon = "battery", Effect = "−10 % Energieverbrauch", MaxRank = 3,
                Desc = "Treppe statt Aufzug, Smoothie statt Energydrink. Du hältst länger durch." },
        };

        public static PerkDef Perk(string id)
        {
            foreach (var p in Perks)
                if (p.Id == id) return p;
            return null;
        }

        public static readonly string[] MailPerks =
        {
            "Hustle-Punkte!",
            "Erfahrung lohnt sich doppelt: Neben Level-Aufstiegen bekommst du für gesammelte XP Hustle-Punkte. Gib sie in der Firma-App unter 'Perks' aus – mehr Kundschaft, größere Warteschlange, günstigerer Einkauf, schnelleres Personal und mehr.",
        };

        // =====================================================================================
        // Einrichtung & Energie
        // =====================================================================================
        public const float EnergyMax = 100f;
        /// <summary>Energieverbrauch je Geschäftsminute (720 min ≈ 47 Punkte ohne Upgrades).</summary>
        public const float EnergyDrainPerMinute = 0.065f;
        /// <summary>Unter diesem Wert wirst du langsamer (bis −20 % bei 0).</summary>
        public const float EnergyTiredBelow = 40f;
        public const float EnergyMinSpeed = 0.8f;
        /// <summary>Erholung über Nacht je Bett-Stufe (0 = Matratze).</summary>
        public static readonly float[] BedRest = { 70f, 85f, 100f };
        /// <summary>Morgendlicher Energie-Schub je Kaffee-Stufe.</summary>
        public static readonly float[] CoffeeMorning = { 0f, 10f, 20f };

        public static readonly HomeSlotDef[] HomeSlots =
        {
            new HomeSlotDef { Id = "laptop", Name = "Laptop", Icon = "screen", BaseName = "Omas alter Laptop", BaseEffect = "Läuft. Irgendwie." },
            new HomeSlotDef { Id = "bed", Name = "Bett", Icon = "moon", BaseName = "Matratze auf dem Boden", BaseEffect = "Erholung über Nacht: 70 Energie" },
            new HomeSlotDef { Id = "chair", Name = "Stuhl", Icon = "user", BaseName = "Wackliger Klappstuhl", BaseEffect = "Kein Effekt" },
            new HomeSlotDef { Id = "desk", Name = "Schreibtisch", Icon = "grid", BaseName = "Werkbank", BaseEffect = "Kein Effekt" },
            new HomeSlotDef { Id = "coffee", Name = "Kaffee", Icon = "fire", BaseName = "Leitungswasser", BaseEffect = "Kein Effekt" },
        };

        public static readonly HomeItemDef[] HomeItems =
        {
            new HomeItemDef { Id = "laptop1", Slot = "laptop", Tier = 1, Cost = 250, Name = "Gaming-Laptop", Icon = "screen",
                Effect = "Einkauf −2 % · Lieferzeit −5 %", Desc = "RGB-Tastatur, 17 Tabs gleichzeitig. Bestellen geht viel flotter." },
            new HomeItemDef { Id = "laptop2", Slot = "laptop", Tier = 2, Cost = 1200, Level = 4, Name = "Workstation mit 3 Monitoren", Icon = "screen",
                Effect = "Einkauf −4 % · Lieferzeit −12 % · +1 Warteschlange", Desc = "Ein Monitor für Bestellungen, einer für Lieferanten, einer für Katzenvideos." },
            new HomeItemDef { Id = "bed1", Slot = "bed", Tier = 1, Cost = 120, Name = "Bettsofa", Icon = "moon",
                Effect = "Erholung über Nacht: 85 Energie", Desc = "Tagsüber Sofa, nachts Bett, immer gemütlich." },
            new HomeItemDef { Id = "bed2", Slot = "bed", Tier = 2, Cost = 650, Level = 3, Name = "Boxspringbett", Icon = "moon",
                Effect = "Erholung: 100 Energie · +5 % Lauftempo", Desc = "Du wachst auf wie ein Influencer: ausgeschlafen und unerträglich motiviert." },
            new HomeItemDef { Id = "chair1", Slot = "chair", Tier = 1, Cost = 90, Name = "Bürostuhl", Icon = "user",
                Effect = "Energie sinkt 15 % langsamer", Desc = "Mit Rollen! Und einer Lehne, die nicht nachgibt." },
            new HomeItemDef { Id = "chair2", Slot = "chair", Tier = 2, Cost = 300, Name = "Gaming-Stuhl", Icon = "gamepad", Grants = "gamingstuhl",
                Effect = "Energie sinkt 30 % langsamer", Desc = "RGB macht produktiver. Diesmal wirklich." },
            new HomeItemDef { Id = "desk1", Slot = "desk", Tier = 1, Cost = 80, Name = "Schreibtisch mit Ablage", Icon = "grid",
                Effect = "+1 Platz in der Warteschlange", Desc = "Endlich Ordnung: Ablage für Bestellzettel statt Pizzakarton." },
            new HomeItemDef { Id = "desk2", Slot = "desk", Tier = 2, Cost = 450, Level = 3, Name = "Stehschreibtisch", Icon = "grid",
                Effect = "+3 Warteschlange · Energie sinkt 10 % langsamer", Desc = "Im Stehen arbeitet es sich wacher. Sagt jedenfalls LinkedIn." },
            new HomeItemDef { Id = "coffee1", Slot = "coffee", Tier = 1, Cost = 40, Name = "Filterkaffeemaschine", Icon = "fire",
                Effect = "Morgens +10 Energie", Desc = "Röchelt laut, macht aber wach." },
            new HomeItemDef { Id = "coffee2", Slot = "coffee", Tier = 2, Cost = 180, Name = "Siebträgermaschine", Icon = "fire", Grants = "kaffee",
                Effect = "Morgens +20 Energie · Energie sinkt 10 % langsamer", Desc = "Barista-Level. Du redest jetzt über Crema." },
        };

        public static HomeItemDef HomeItem(string slot, int tier)
        {
            foreach (var h in HomeItems)
                if (h.Slot == slot && h.Tier == tier) return h;
            return null;
        }

        public static HomeItemDef HomeItemById(string id)
        {
            foreach (var h in HomeItems)
                if (h.Id == id) return h;
            return null;
        }

        public static HomeSlotDef HomeSlot(string id)
        {
            foreach (var s in HomeSlots)
                if (s.Id == id) return s;
            return null;
        }

        // =====================================================================================
        // Ausrüstung
        // =====================================================================================
        /// <summary>Faltmaschine: so viele Geschäftsminuten pro gefaltetem Karton.</summary>
        public const float FoldMachineMinutes = 5f;
        /// <summary>Zusätzliche Packtische/Labeldrucker: Tempo-Bonus fürs passende Personal je Station.</summary>
        public const float ExtraStationStaffBonus = 0.15f;

        public static readonly EquipmentDef[] Equipment =
        {
            new EquipmentDef { Id = "faltmaschine", Name = "Faltmaschine", Icon = "gear", Cost = 450, Level = 2,
                Effect = "Faltet alle 5 Minuten einen Karton", Desc = "Brummt, klappert, faltet: Ungefaltete Kartons werden automatisch fertig." },
            new EquipmentDef { Id = "labelgun", Name = "Hand-Labelgerät", Icon = "tag", Cost = 220, Level = 2,
                Effect = "Etikettiert beim Aufnehmen · +1 Paket tragen", Desc = "Pakete, die du am Packtisch nimmst, bekommen sofort ihr Label – alle auf einmal." },
            new EquipmentDef { Id = "packtisch", Name = "Zusätzlicher Packtisch", Icon = "package", Cost = 300, Level = 3, MaxGarage = 1, MaxWarehouse = 2,
                Effect = "Kürzere Wege · Packer:innen +15 % je Tisch", Desc = "Stell ihn hin, wo er passt (Taste B im Lager)." },
            new EquipmentDef { Id = "labeldrucker", Name = "Zusätzlicher Labeldrucker", Icon = "receipt", Cost = 200, Level = 3, MaxGarage = 1, MaxWarehouse = 2,
                Effect = "Kürzere Wege · Versandkräfte +15 % je Drucker", Desc = "Noch ein Drucker, noch mehr Papierstau – aber kürzere Wege." },
            new EquipmentDef { Id = "container", Name = "Großcontainer für Müll", Icon = "boxes", Cost = 250, Level = 1,
                Effect = "+150 Platz für Verpackungsmüll", Desc = "Ein grüner Riese neben den Tonnen. Nie wieder überquellender Müll." },
        };

        public static EquipmentDef EquipmentDef(string id)
        {
            foreach (var e in Equipment)
                if (e.Id == id) return e;
            return null;
        }

        // =====================================================================================
        // Mengenrabatt (Einkauf) & Porto (Versand)
        // =====================================================================================
        public static readonly VolumeTier[] VolumeTiers =
        {
            new VolumeTier { Units = 0, Discount = 0f },
            new VolumeTier { Units = 150, Discount = 0.03f },
            new VolumeTier { Units = 400, Discount = 0.06f },
            new VolumeTier { Units = 800, Discount = 0.09f },
            new VolumeTier { Units = 1500, Discount = 0.12f },
            new VolumeTier { Units = 3000, Discount = 0.15f },
        };

        /// <summary>Händlerstatus: je Firmenlevel über 1 sinkt der Einkaufspreis um 0,5 % (max. 4,5 %).</summary>
        public const float LevelDiscountPerLevel = 0.005f;

        /// <summary>Porto je Kartongröße S/M/L in € (vor Tarif, Perks und Paketdienst).</summary>
        public static readonly float[] PortoBySize = { 1.2f, 1.8f, 2.6f };

        public static readonly PortoTier[] PortoTiers =
        {
            new PortoTier { Name = "Privatkunde", Shipped = 0, Mult = 1f },
            new PortoTier { Name = "Bronze", Shipped = 100, Mult = 0.9f },
            new PortoTier { Name = "Silber", Shipped = 300, Mult = 0.8f },
            new PortoTier { Name = "Gold", Shipped = 800, Mult = 0.7f },
            new PortoTier { Name = "Platin", Shipped = 2000, Mult = 0.6f },
        };

        public static readonly CarrierDef[] Carriers =
        {
            new CarrierDef { Id = "paketblitz", Name = "PaketBlitz Standard", Icon = "truck", PortoMult = 1f, PortoExtra = 0f,
                Desc = "Günstig. Kommt meistens an. Bei Streik oder Unwetter wird's zäh." },
            new CarrierDef { Id = "turbo", Name = "TurboKurier Express", Icon = "bolt", PortoMult = 2f, PortoExtra = 1f, Reliable = true, StarBonusChance = 0.35f,
                Desc = "Doppelt so teuer, aber immer pünktlich – Streik? Nicht bei uns. Die Kundschaft freut sich." },
        };

        // =====================================================================================
        // Lieferstörungen beim Paketdienst
        // =====================================================================================
        /// <summary>Tägliche Chance auf eine neue Störung (ab Tag <see cref="DisruptionMinDay"/>).</summary>
        public const float DisruptionDailyChance = 0.07f;
        public const int DisruptionMinDay = 4;

        public static readonly DisruptionDef[] Disruptions =
        {
            new DisruptionDef { Id = "streik", Name = "Streik bei PaketBlitz", Icon = "angry", DelayChance = 0.7f,
                Text = "Die Zusteller:innen streiken. Standardpakete kommen tagelang zu spät – die Kundschaft wird sauer.",
                Review = "Paket hing im Streik fest. Eine Woche! Nie wieder." },
            new DisruptionDef { Id = "unwetter", Name = "Unwetter-Chaos", Icon = "drop", DelayChance = 0.5f,
                Text = "Sturm und Starkregen: Die Lieferwagen stehen im Stau, viele Pakete kommen verspätet.",
                Review = "Wegen Unwetter zu spät. Kann der Shop nix für, ich bin trotzdem sauer." },
            new DisruptionDef { Id = "glatteis", Name = "Glatteis", Icon = "warning", DelayChance = 0.45f,
                Text = "Spiegelglatte Straßen: Die Fahrer rutschen mehr, als sie liefern.",
                Review = "Kam zu spät. Der Bote ist angeblich ausgerutscht. Drei Mal." },
            new DisruptionDef { Id = "sortier", Name = "Sortierzentrum überlastet", Icon = "boxes", DelayChance = 0.4f,
                Text = "Im Paketzentrum stapeln sich die Sendungen bis zur Decke. Vieles bleibt liegen.",
                Review = "Paket war drei Tage 'im Sortierzentrum'. Was machen die da?" },
        };

        public static DisruptionDef Disruption(string id)
        {
            foreach (var d in Disruptions)
                if (d.Id == id) return d;
            return null;
        }

        // =====================================================================================
        // Fehlerhafte Chargen
        // =====================================================================================
        public const int DefectMinDay = 3;
        /// <summary>Chance je Lieferung: Billig-Ware / Standard / Premium.</summary>
        public const float DefectChanceCheap = 0.07f;
        public const float DefectChanceStandard = 0.025f;
        public const float DefectChancePremium = 0.005f;
        public const float DefectShareMin = 0.3f;
        public const float DefectShareMax = 0.5f;
        /// <summary>Ein defekter Artikel kommt mit dieser Wahrscheinlichkeit zurück (sonst nur schlechte Bewertung).</summary>
        public const float DefectReturnChance = 0.65f;
        /// <summary>Rückruf: der Lieferant erstattet diesen Anteil des Einkaufspreises.</summary>
        public const float DefectRefundShare = 1f;

        public static readonly string[] DefectReviews =
        {
            "Kam kaputt an. Funktioniert nicht mal eine Sekunde.", "Defekt! Hat beim Auspacken schon gequalmt.",
            "Totalausfall. Hab's mit Liebe ausgepackt und mit Wut eingepackt.", "Teile fehlen, der Rest wackelt. Retoure.",
        };

        public const string DefectReturnReason = "Defekt angekommen – funktioniert nicht.";

        // =====================================================================================
        // Verpackungsmüll
        // =====================================================================================
        /// <summary>Platz in den Tonnen je Standort (Garage / Lagerhalle).</summary>
        public static readonly int[] WasteCapacityByStage = { 60, 180 };
        public const int WasteContainerBonus = 150;
        public const int WastePerPackage = 1;
        public const int WastePerCrate = 2;
        public const int WastePerReturn = 1;
        /// <summary>Abholung an diesen Wochentagen (0 = Montag) um <see cref="GarbagePickupMinute"/>.</summary>
        public static readonly int[] GarbagePickupWeekdays = { 0, 2, 4 };
        public const float GarbagePickupMinute = 600f;
        /// <summary>Kommt kein Müllwagen (keine Welt angeschlossen), wird spätestens nach dieser Zeit automatisch geleert.</summary>
        public const float GarbageFallbackMinutes = 90f;
        public static readonly int[] GarbageSpecialCost = { 35, 60 };
        /// <summary>Volle Tonnen: Personal arbeitet langsamer, du stolperst über Säcke, die Nachbarn meckern.</summary>
        public const float WasteFullStaffMult = 0.75f;
        public const float WasteFullWalkMult = 0.92f;
        public const float WasteFullRepPenalty = 0.04f;

        // =====================================================================================
        // Großhändler (B2B-Erweiterung) & Markenstärke
        // =====================================================================================
        public const int WholesaleLevel = 4;
        /// <summary>So oft muss ein Produkt schon verkauft worden sein, damit Großhändler es als Markenware wollen.</summary>
        public const int WholesaleMinSold = 15;
        public const float WholesalePayMin = 0.34f;
        public const float WholesalePayMax = 0.4f;
        public const float WholesaleQtyMult = 2f;
        public const float WholesaleReorderBonus = 1.1f;
        /// <summary>Bis zu +20 % Preisaufschlag bei voller Markenstärke.</summary>
        public const float BrandMaxPremium = 0.2f;

        public static readonly string[] Wholesalers =
        {
            "Großhandel Krause KG", "MegaMarkt Zentrallager", "Ramsch & Co. Restposten", "Kaufhaus Zentrale Nord",
            "Tankstellen-Verbund Sprit+", "Geschenke-Kette Schenkmal", "Elektro-Discount Blitzpreis", "Drogerie-Kette Glanz & Gloria",
        };

        public static readonly string[] WholesaleReasons =
        {
            "für 14 Filialen (Markenware)", "fürs Weihnachtsregal (Markenware)", "für die Kassenzone (Markenware)",
            "als Aktionsware im Prospekt", "für den Online-Shop des Händlers (Markenware)",
        };

        public static readonly string[] MailWholesale =
        {
            "Großhändler wollen deine Marke",
            "Deine Marke spricht sich herum! Großhändler fragen jetzt größere Mengen deiner bewährten Produkte an – zu besseren Preisen als normale Firmen. Je stärker deine Marke (Bewertung, Bekanntheit, Name), desto mehr zahlen sie. Angebote in der App 'Aufträge'.",
        };
    }
}
