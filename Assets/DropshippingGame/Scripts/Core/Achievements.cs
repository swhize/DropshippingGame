using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Ein Erfolg (Steam-artig). <see cref="Id"/> ist zugleich der API-Name im Steamworks-Backend
    /// (z. B. "ACH_FIRST_PARCEL"). Bedingung und Fortschritt lesen nur den Spielzustand.
    /// </summary>
    public sealed class AchievementDef
    {
        public string Id, Name, Desc, Icon = "trophy";
        /// <summary>Geheim: Name/Beschreibung erst nach dem Freischalten sichtbar.</summary>
        public bool Hidden;
        public Func<Sim, bool> Done;
        /// <summary>Optional: Fortschritt 0..1 für die Liste.</summary>
        public Func<Sim, float> Progress;
    }

    public static class AchievementData
    {
        private static float P(float v, float target) => target <= 0f ? 1f : Mathx.Clamp01(v / target);

        private static AchievementDef A(string id, string name, string desc, string icon, Func<Sim, bool> done, Func<Sim, float> progress = null, bool hidden = false) =>
            new AchievementDef { Id = id, Name = name, Desc = desc, Icon = icon, Done = done, Progress = progress, Hidden = hidden };

        private static AchievementDef Count(string id, string name, string desc, string icon, Func<Sim, float> value, float target) =>
            A(id, name, desc, icon, s => value(s) >= target, s => P(value(s), target));

        public static readonly AchievementDef[] All =
        {
            // ---- Pakete & Umsatz ----
            Count("ACH_FIRST_PARCEL", "Das erste Paket", "Verschicke dein allererstes Paket.", "package", s => s.TotalShipped, 1),
            Count("ACH_PARCELS_100", "Paketprofi", "Verschicke 100 Pakete.", "package", s => s.TotalShipped, 100),
            Count("ACH_PARCELS_1000", "Paketlawine", "Verschicke 1.000 Pakete.", "boxes", s => s.TotalShipped, 1000),
            Count("ACH_PARCELS_5000", "Logistik-Legende", "Verschicke 5.000 Pakete.", "truck", s => s.TotalShipped, 5000),
            Count("ACH_REVENUE_1K", "Vierstellig", "Erreiche 1.000 € Umsatz.", "coin", s => s.TotalEarned, 1000),
            Count("ACH_REVENUE_10K", "Fünfstellig", "Erreiche 10.000 € Umsatz.", "coin", s => s.TotalEarned, 10000),
            Count("ACH_REVENUE_100K", "Vom Imbiss zum Imperium", "Erreiche 100.000 € Umsatz.", "trophy", s => s.TotalEarned, 100000),
            Count("ACH_IPO", "Börsengang", "Erreiche 1.000.000 € Umsatz und bring deine Firma an die Börse.", "rocket", s => s.TotalEarned, 1000000),
            A("ACH_ALL_PRODUCTS", "Volles Sortiment", "Verkaufe jedes Produkt mindestens einmal.", "grid", s => s.ProductsSoldCount() >= GameData.Products.Length,
                s => P(s.ProductsSoldCount(), GameData.Products.Length)),
            // ---- Firma ----
            A("ACH_BRAND", "Eine echte Marke", "Gib deinem Shop einen eigenen Namen.", "star", s => s.BrandNamed),
            Count("ACH_LEVEL_5", "Aufsteiger", "Erreiche Firmenlevel 5.", "sparkle", s => s.Level, 5),
            Count("ACH_LEVEL_10", "Legende", "Erreiche Firmenlevel 10.", "sparkle", s => s.Level, 10),
            A("ACH_WAREHOUSE", "Raus aus der Garage", "Kaufe die Lagerhalle.", "building", s => s.LocationStage >= 1),
            A("ACH_OUTPOST", "Zweites Standbein", "Eröffne im Endlos-Modus das Außenlager.", "building", s => s.HasUpgrade(GameData.OutpostId)),
            Count("ACH_FIRST_HIRE", "Chef sein", "Stelle deine erste Mitarbeiterin oder deinen ersten Mitarbeiter ein.", "users", s => s.Staff.Count, 1),
            Count("ACH_FULL_TEAM", "Volle Besetzung", "Beschäftige 6 Leute gleichzeitig.", "users", s => s.Staff.Count, 6),
            A("ACH_FIVE_STARS", "Kundenliebling", "Halte eine Bewertung von 4,8 Sternen (ab 30 Bewertungen).", "star",
                s => s.ReviewCount >= 30 && s.Reputation >= 4.8f, s => s.ReviewCount < 30 ? P(s.ReviewCount, 30) * 0.5f : P(s.Reputation, 4.8f)),
            A("ACH_HOME_MAX", "Traumbüro", "Bau jede Einrichtung (Laptop, Bett, Stuhl, Schreibtisch, Kaffee) voll aus.", "home",
                s => s.HomeMaxedCount() >= GameData.HomeSlots.Length, s => P(s.HomeMaxedCount(), GameData.HomeSlots.Length)),
            // ---- Aufträge & Ziele ----
            Count("ACH_CONTRACT_1", "Business to Business", "Erfülle deinen ersten Großauftrag.", "pallet", s => s.TotalContractsDone, 1),
            Count("ACH_CONTRACT_25", "Lieferant des Vertrauens", "Erfülle 25 Großaufträge.", "pallet", s => s.TotalContractsDone, 25),
            Count("ACH_WHOLESALE", "Im Regal der Großen", "Beliefere einen Großhändler mit deiner Markenware.", "factory", s => s.Stats.WholesaleDone, 1),
            Count("ACH_CHALLENGES_10", "Wochenheld", "Schaffe insgesamt 10 Wochenziele.", "trophy", s => s.TotalChallengesDone, 10),
            Count("ACH_DAY_100", "Dauerbrenner", "Führe deine Firma 100 Tage lang.", "clock", s => s.Day, 100),
            // ---- Marketing ----
            A("ACH_TIKTOK_VIRAL", "Viral!", "Dreh ein TikTok mit mindestens 85 % Wertung.", "phone", s => s.Stats.BestTikTok >= 0.85f, s => P(s.Stats.BestTikTok, 0.85f)),
            Count("ACH_TIKTOK_50", "Content-Maschine", "Poste 50 eigene TikToks.", "phone", s => s.Stats.OwnTikToks, 50),
            Count("ACH_FESTIVAL", "Marktschreier", "Verkaufe auf einem Straßenfest 30 Artikel.", "store", s => s.Stats.BestFestivalSold, 30),
            Count("ACH_STAND_100", "Straßenhändler", "Verkaufe 100 Artikel am Verkaufsstand.", "store", s => s.StandSoldTotal, 100),
            A("ACH_FAKE_EXPOSED", "Aufgeflogen", "Lass dich mit Fake-Profilen oder Fake-Seiten erwischen.", "warning", s => s.FakeExposedTotal >= 1, null, true),
            // ---- Logistik ----
            Count("ACH_RECALL", "Qualitätskontrolle", "Schick eine defekte Charge an den Lieferanten zurück.", "warning", s => s.Stats.Recalled, 1),
            A("ACH_STRIKE", "Streikbrecher", "Verschick während einer Lieferstörung mit TurboKurier.", "bolt", s => s.Stats.StrikeShipped >= 1),
            // ---- Finanzen ----
            Count("ACH_DIVIDEND", "Passives Einkommen", "Kassiere insgesamt 100 € Dividende.", "coin", s => s.Market.DividendsTotal, 100),
            Count("ACH_SAVINGS", "Notgroschen", "Spare 10.000 € auf dem Sparkonto an.", "bank", s => s.Market.Savings, 10000),
            A("ACH_DEBT_FREE", "Schuldenfrei", "Nimm einen Kredit auf und zahl ihn komplett zurück.", "check", s => s.Stats.HadDebt && s.Debt <= 0),
            A("ACH_DIAMOND_HANDS", "Diamond Hands", "Halte eine Krypto-Position, die mehr als die Hälfte verloren hat.", "rocket", s => s.CryptoCrashHeld(), null, true),
            A("ACH_MAMA", "Mama hilft", "Lass dich von Mama vor der Pleite retten.", "heart", s => s.Stats.MamaRescues >= 1, null, true),
            // ---- Lifestyle & Tiere ----
            A("ACH_LIFESTYLE", "Gönn dir", "Kauf dir etwas aus der Lifestyle-App.", "gem", s => s.LifestyleOwned.Count >= 1),
            A("ACH_PENTHOUSE", "Ganz oben", "Kauf das Penthouse mit Stadtblick.", "building", s => s.LifestyleOwned.Contains("penthouse")),
            Count("ACH_PET", "Bürotier", "Adoptiere ein Haustier.", "heart", s => s.Pets.Count, 1),
            // ---- Herausforderungen ----
            A("ACH_SPEEDRUN", "Turbo-Gründung", "Erreiche 100.000 € Umsatz vor Tag 35.", "bolt", s => s.Stats.GoalDay > 0 && s.Stats.GoalDay < 35),
            A("ACH_HARD", "Hardcore-Hustle", "Erreiche 100.000 € Umsatz auf „Hart“.", "fire", s => s.Stats.GoalDay > 0 && s.Difficulty == "hart"),
        };

        public static AchievementDef Find(string id)
        {
            foreach (var a in All)
                if (a.Id == id) return a;
            return null;
        }
    }

    /// <summary>Laufende Gesamtzahlen für Erfolge und die Statistik-Seite (im Spielstand).</summary>
    public sealed class LifetimeStats
    {
        public int OwnTikToks, AllTikToks, FestivalsDone, FestivalSold, BestFestivalSold, FestivalRevenue, WholesaleDone, Recalled, StrikeShipped;
        public int MamaRescues, MarketingSpent, PeakMoney, GoalDay, Lost;
        public float BestTikTok;
        public double PlaySeconds;
        public bool HadDebt;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "own_tiktoks", OwnTikToks }, { "all_tiktoks", AllTikToks }, { "festivals", FestivalsDone }, { "festival_sold", FestivalSold },
            { "best_festival", BestFestivalSold }, { "festival_revenue", FestivalRevenue }, { "wholesale", WholesaleDone }, { "recalled", Recalled },
            { "strike_shipped", StrikeShipped }, { "mama", MamaRescues }, { "marketing", MarketingSpent }, { "peak_money", PeakMoney },
            { "goal_day", GoalDay }, { "lost", Lost }, { "best_tiktok", (double)BestTikTok }, { "play_seconds", PlaySeconds }, { "had_debt", HadDebt },
        };

        public static LifetimeStats FromJson(Dictionary<string, object> d)
        {
            var s = new LifetimeStats();
            if (d == null) return s;
            s.OwnTikToks = Math.Max(0, J.I(d, "own_tiktoks"));
            s.AllTikToks = Math.Max(0, J.I(d, "all_tiktoks"));
            s.FestivalsDone = Math.Max(0, J.I(d, "festivals"));
            s.FestivalSold = Math.Max(0, J.I(d, "festival_sold"));
            s.BestFestivalSold = Math.Max(0, J.I(d, "best_festival"));
            s.FestivalRevenue = Math.Max(0, J.I(d, "festival_revenue"));
            s.WholesaleDone = Math.Max(0, J.I(d, "wholesale"));
            s.Recalled = Math.Max(0, J.I(d, "recalled"));
            s.StrikeShipped = Math.Max(0, J.I(d, "strike_shipped"));
            s.MamaRescues = Math.Max(0, J.I(d, "mama"));
            s.MarketingSpent = Math.Max(0, J.I(d, "marketing"));
            s.PeakMoney = J.I(d, "peak_money");
            s.GoalDay = Math.Max(0, J.I(d, "goal_day"));
            s.Lost = Math.Max(0, J.I(d, "lost"));
            float best = J.F(d, "best_tiktok");
            s.BestTikTok = float.IsNaN(best) || float.IsInfinity(best) ? 0f : Mathx.Clamp01(best);
            double ps = J.F(d, "play_seconds");
            s.PlaySeconds = double.IsNaN(ps) || double.IsInfinity(ps) ? 0 : Math.Max(0, ps);
            s.HadDebt = J.B(d, "had_debt");
            return s;
        }
    }
}
