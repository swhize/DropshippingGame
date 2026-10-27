using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    public static partial class GameData
    {
        /// <summary>Endlos-Modus: das Außenlager (zweite Halle).</summary>
        public const string OutpostId = "outpost";
        public const int OutpostCapacity = 3000;
        public const int OutpostQueue = 6;
        public const int OutpostRent = 150;
        public const float OutpostStaffMult = 1.2f;

        /// <summary>Mama-Notkredit: so viel liegt danach auf dem Konto (nach Abzug der Fixkosten).</summary>
        public const int MamaRescueCushion = 500;
        /// <summary>Rückzahlung je Tag (Anteil vom Kontostand über <see cref="MamaRepayAbove"/>).</summary>
        public const float MamaRepayShare = 0.2f;
        public const int MamaRepayAbove = 1500;

        /// <summary>Endlos-Ziele nach den festen Zielen: Umsatz 2 Mio., 4 Mio., 8 Mio. …</summary>
        public const int EndlessFirstTarget = 2000000;
    }

    /// <summary>
    /// Endspiel &amp; Meta: Erfolge (Steam-artig), Gesamtstatistik, Endlos-Modus (Außenlager, Börsengang,
    /// immer weiter wachsende Umsatzziele) und Mamas Notkredit vor der Pleite.
    /// Hängt über wenige Haken an Sim (MetaReset/Update/NewDay/EndDay, Speichern über AddProgressState).
    /// </summary>
    public sealed partial class Sim
    {
        // ---- Ereignisse ------------------------------------------------------------------------
        /// <summary>Erfolg freigeschaltet (Runtime: Toast + Plattform/Steam).</summary>
        public event Action<AchievementDef> AchievementUnlocked;
        /// <summary>Nach dem Laden: alle freigeschalteten Erfolge (Runtime: erneut an Steam melden).</summary>
        public event Action<IEnumerable<string>> AchievementsLoaded;
        /// <summary>Großer Moment im Endlos-Modus (Titel, Text) – z. B. der Börsengang.</summary>
        public event Action<string, string> MilestoneReached;
        /// <summary>Mama hat vor der Pleite gerettet (überwiesener Betrag).</summary>
        public event Action<int> MamaRescued;

        // ---- Zustand ---------------------------------------------------------------------------
        /// <summary>Freigeschaltete Erfolge → Tag der Freischaltung.</summary>
        public Dictionary<string, int> Achievements = new Dictionary<string, int>();
        public LifetimeStats Stats = new LifetimeStats();
        public int MamaRescuesLeft = 1;
        /// <summary>Offener Betrag bei Mama (zinslos, wird automatisch abgestottert).</summary>
        public int MamaDebt;
        /// <summary>Erreichte Endlos-Stufen (0 = noch keine).</summary>
        public int EndlessTier;

        private bool _metaHooked;
        private float _achCheckAt;
        private double _lastRealTime;
        private TikTokVideo _lastVideo;

        private void MetaReset()
        {
            Achievements.Clear();
            Stats = new LifetimeStats();
            MamaRescuesLeft = GameData.Difficulty(Difficulty).MamaRescues;
            MamaDebt = 0;
            EndlessTier = 0;
            _achCheckAt = 0f;
            _lastRealTime = RealTime;
            _lastVideo = null;
            if (_metaHooked) return;
            _metaHooked = true;
            FestivalEnded += f =>
            {
                Stats.FestivalsDone++;
                Stats.FestivalSold += f.Sold;
                Stats.FestivalRevenue += f.Revenue;
                Stats.BestFestivalSold = Math.Max(Stats.BestFestivalSold, f.Sold);
                CheckAchievements();
            };
            ContractCompleted += c =>
            {
                if (c != null && c.Wholesale) Stats.WholesaleDone++;
            };
            PackageShipped += (pkg, reward, where) =>
            {
                if (ActiveDisruption != null && CurrentCarrier.Reliable) Stats.StrikeShipped++;
            };
            GoalCompleted += OnMetaGoal;
        }

        // =====================================================================================
        // Erfolge
        // =====================================================================================
        public bool HasAchievement(string id) => id != null && Achievements.ContainsKey(id);
        public int AchievementCount() => Achievements.Count;

        /// <summary>Prüft alle Erfolge und schaltet erfüllte frei. Gibt die Zahl neuer Erfolge zurück.</summary>
        public int CheckAchievements()
        {
            if (StoryStage != "business") return 0;
            int n = 0;
            foreach (var a in AchievementData.All)
            {
                if (Achievements.ContainsKey(a.Id)) continue;
                bool done;
                try
                {
                    done = a.Done != null && a.Done(this);
                }
                catch (Exception)
                {
                    done = false;
                }
                if (!done) continue;
                UnlockAchievement(a);
                n++;
            }
            return n;
        }

        private void UnlockAchievement(AchievementDef a)
        {
            if (a == null || Achievements.ContainsKey(a.Id)) return;
            Achievements[a.Id] = Day;
            Notify("Erfolg freigeschaltet: " + a.Name + " – " + a.Desc, "good");
            Sound("levelup", 0.03f, -5f);
            AchievementUnlocked?.Invoke(a);
        }

        /// <summary>Admin/Tests: einen Erfolg direkt freischalten.</summary>
        public bool AdminUnlockAchievement(string id)
        {
            var a = AchievementData.Find(id);
            if (a == null || Achievements.ContainsKey(id)) return false;
            UnlockAchievement(a);
            return true;
        }

        /// <summary>Fortschritt 0..1 eines Erfolgs (1 = freigeschaltet).</summary>
        public float AchievementProgress(AchievementDef a)
        {
            if (a == null) return 0f;
            if (Achievements.ContainsKey(a.Id)) return 1f;
            if (a.Progress == null) return 0f;
            try
            {
                return Mathx.Clamp01(a.Progress(this));
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        // ---- Hilfen für Bedingungen -------------------------------------------------------------
        public int ProductsSoldCount()
        {
            int n = 0;
            foreach (var p in GameData.Products)
                if (ShippedPerProduct.TryGetValue(p.Id, out int c) && c > 0) n++;
            return n;
        }

        public int HomeMaxedCount()
        {
            int n = 0;
            foreach (var slot in GameData.HomeSlots)
                if (NextHomeItem(slot.Id) == null) n++;
            return n;
        }

        /// <summary>Hält eine Krypto-Position (ab 200 € Einsatz), die über die Hälfte verloren hat.</summary>
        public bool CryptoCrashHeld()
        {
            foreach (var a in Market.AllAssets)
            {
                if (a.Kind != AssetKind.Crypto) continue;
                float inv = Market.Invested.TryGetValue(a.Id, out float v) ? v : 0f;
                if (inv >= 200f && Market.HoldingValue(a.Id) < inv * 0.5f) return true;
            }
            return false;
        }

        // =====================================================================================
        // Ablauf-Haken
        // =====================================================================================
        private void MetaUpdate()
        {
            double rt = RealTime;
            if (rt > _lastRealTime && rt - _lastRealTime < 5.0) Stats.PlaySeconds += rt - _lastRealTime;
            _lastRealTime = rt;
            if (Debt > 0) Stats.HadDebt = true;
            if (Money > Stats.PeakMoney) Stats.PeakMoney = Money;
            if (TikTokVideos.Count > 0 && !ReferenceEquals(TikTokVideos[0], _lastVideo))
            {
                Stats.OwnTikToks++;
                _lastVideo = TikTokVideos[0];
                Stats.BestTikTok = Math.Max(Stats.BestTikTok, TikTokVideos[0].Score);
            }
            float now = BClock();
            if (now < _achCheckAt) return;
            _achCheckAt = now + 4f;
            CheckAchievements();
        }

        private void MetaNewDay()
        {
            if (MamaDebt > 0 && Money > GameData.MamaRepayAbove)
            {
                int pay = Math.Min(MamaDebt, Math.Max(50, Mathx.RoundToInt((Money - GameData.MamaRepayAbove) * GameData.MamaRepayShare)));
                Money -= pay;
                MamaDebt -= pay;
                Daily.Notes.Add("An Mama zurückgezahlt: " + Fmt.Money(pay) + (MamaDebt > 0 ? " (noch " + Fmt.Money(MamaDebt) + ")" : " – alles beglichen!"));
                if (MamaDebt == 0) Events.AddMail("Mama", "Alles zurück!", "Schatz, du hast mir alles zurückgezahlt. Ich hab doch gesagt, du schaffst das. Ich bin so stolz auf dich!", "heart");
            }
            CheckAchievements();
        }

        /// <summary>
        /// Feierabend: Droht die Pleite und Mama hat noch einen Notgroschen, überweist sie so viel, dass nach den
        /// Fixkosten <see cref="GameData.MamaRescueCushion"/> übrig sind (zinslos, wird später automatisch abgestottert).
        /// </summary>
        private void MetaEndDay(DaySummary s)
        {
            Stats.AllTikToks += Daily.TikToks;
            Stats.MarketingSpent += Daily.Marketing;
            Stats.Lost += Daily.Lost;
            if (s == null || !s.Bankrupt || MamaRescuesLeft <= 0 || StoryStage != "business") return;
            int gift = GameData.MamaRescueCushion - s.MoneyAfter;
            gift = (int)Math.Ceiling(gift / 50.0) * 50;
            MamaRescuesLeft--;
            Stats.MamaRescues++;
            Money += gift;
            MamaDebt += gift;
            s.MoneyAfter += gift;
            s.Bankrupt = false;
            s.Notes.Add("Mama hat dich vor der Pleite gerettet: " + Fmt.Money(gift) + " zinslos – du zahlst es zurück, sobald es wieder läuft." +
                        (MamaRescuesLeft > 0 ? "" : " Ein zweites Mal kann sie nicht."));
            Events.AddMail("Mama", "Ich hab dir was überwiesen",
                "Schatz, die Bank hat bei mir angerufen. Ich hab dir " + Fmt.Money(gift) + " überwiesen – zahl's zurück, wenn's wieder läuft. " +
                "Aber bitte: weniger Ausgaben, mehr verkaufen, und lass die Finger von diesem Krypto-Zeug!" +
                (MamaRescuesLeft > 0 ? "" : "\n\nPS: Mehr hab ich wirklich nicht. Das nächste Mal musst du allein klarkommen."), "heart");
            Notify("Mama hat dich gerettet (+" + Fmt.Money(gift) + ")! Noch mal klappt das " + (MamaRescuesLeft > 0 ? MamaRescuesLeft + "×." : "nicht."), "bad");
            MamaRescued?.Invoke(gift);
            CheckAchievements();
        }

        // =====================================================================================
        // Endlos-Modus
        // =====================================================================================
        private void OnMetaGoal(GoalDef g)
        {
            if (g == null) return;
            if (g.Final && Stats.GoalDay <= 0) Stats.GoalDay = Day;
            if (g.Id == "ipo")
            {
                string title = "Börsengang: " + BrandName + " AG";
                string text = "Die Glocke läutet! " + BrandName + " ist ab heute an der Kiez-Börse notiert. Erster Kurs: 12,50 €. " +
                              "Kalle hat drei Aktien gekauft „für die Rente“, Mama hat den Zeitungsartikel eingerahmt. Und du? Packst weiter Pakete. Natürlich.";
                Events.AddMail("Kiez-Börse", title, text + "\n\nAb jetzt zählt nur noch eins: mehr. Neue Ziele: 2, 4, 8 Millionen …", "rocket");
                MilestoneReached?.Invoke(title, text);
            }
            CheckAchievements();
        }

        /// <summary>Ziel nach den festen Zielen (Endlos-Modus): Umsatzziel, das sich jedes Mal verdoppelt.</summary>
        public GoalDef EndlessGoal()
        {
            long target = (long)GameData.EndlessFirstTarget << Math.Min(20, EndlessTier);
            int t = (int)Math.Min(int.MaxValue, target);
            return new GoalDef
            {
                Id = "endless_" + (EndlessTier + 1), Title = "Endlos " + (EndlessTier + 1) + ": " + Fmt.Thousands(t) + " € Umsatz",
                Desc = "Immer weiter: erreiche " + Fmt.Thousands(t) + " € Umsatz.", Type = "revenue", Target = t,
                Reward = Math.Min(1000000, t / 100), Xp = 0,
            };
        }

        /// <summary>Endlos-Ziele prüfen (aus CheckGoals, wenn alle festen Ziele erledigt sind).</summary>
        private void CheckEndlessGoals()
        {
            // Endlos-Ziele folgen auf das letzte feste Umsatzziel (Börsengang). Optionale feste Ziele
            // (Verkaufsstand, Außenlager, Team …) blockieren sie nicht – sonst bliebe der Endlos-Modus stumm.
            if (StoryStage != "business" || !FixedRevenueGoalsDone()) return;
            for (int guard = 0; guard < 4; guard++)
            {
                // Ab hier wäre das Ziel größer als int (Umsatz ist int) – keine weiteren Stufen.
                if (((long)GameData.EndlessFirstTarget << Math.Min(30, EndlessTier)) > int.MaxValue) return;
                var g = EndlessGoal();
                if (GoalValue(g) < g.Target) return;
                EndlessTier++;
                Money += g.Reward;
                Daily.IncomeOther += g.Reward;
                Notify("Endlos-Ziel geschafft: " + Fmt.Thousands((int)g.Target) + " € Umsatz (+" + Fmt.Money(g.Reward) + ")! Nächstes: " +
                       Fmt.Thousands((int)EndlessGoal().Target) + " €.", "good");
                Sound("levelup", 0f, -3f);
                ObjectiveChanged?.Invoke();
            }
        }

        /// <summary>Alle festen Umsatzziele (bis zum Börsengang) erreicht?</summary>
        public bool FixedRevenueGoalsDone()
        {
            foreach (var g in GameData.Goals)
                if (g.Type == "revenue" && !GoalsDone.Contains(g.Id)) return false;
            return true;
        }

        /// <summary>Wert für zusätzliche Zieltypen (aus GoalValue).</summary>
        private float MetaGoalValue(GoalDef g)
        {
            switch (g.Type)
            {
                case "outpost": return HasUpgrade(GameData.OutpostId) ? 1f : 0f;
            }
            return 0f;
        }

        // ---- Außenlager -----------------------------------------------------------------------------
        private int OutpostCapacityBonus() => Upgrades.Contains(GameData.OutpostId) ? GameData.OutpostCapacity : 0;
        private int OutpostQueueBonus() => Upgrades.Contains(GameData.OutpostId) ? GameData.OutpostQueue : 0;
        private int OutpostRent() => Upgrades.Contains(GameData.OutpostId) ? GameData.OutpostRent : 0;
        private float OutpostStaffMult() => Upgrades.Contains(GameData.OutpostId) ? GameData.OutpostStaffMult : 1f;

        // =====================================================================================
        // Statistik-Seite
        // =====================================================================================
        /// <summary>Gesamtzahlen für die Statistik-Seite: {Bereich, Bezeichnung, Wert}.</summary>
        public List<string[]> StatsLines()
        {
            var l = new List<string[]>();
            void Add(string group, string label, string value) => l.Add(new[] { group, label, value });
            int minutes = (int)(Stats.PlaySeconds / 60.0);
            Add("Allgemein", "Spieltage", Day.ToString());
            Add("Allgemein", "Spielzeit", (minutes / 60) + " h " + (minutes % 60).ToString("00") + " min");
            Add("Allgemein", "Schwierigkeit", DifficultyInfo.Name);
            Add("Allgemein", "Erfolge", Achievements.Count + " / " + AchievementData.All.Length);
            Add("Allgemein", "Firmenlevel", Level + " (" + Fmt.Thousands(Xp) + " XP)");
            if (Stats.GoalDay > 0) Add("Allgemein", "100.000 € geschafft an Tag", Stats.GoalDay.ToString());
            Add("Verkauf", "Umsatz gesamt", Fmt.Money(TotalEarned));
            Add("Verkauf", "Pakete verschickt", Fmt.Thousands(TotalShipped));
            Add("Verkauf", "Verlorene Bestellungen", Fmt.Thousands(TotalLostOrders));
            Add("Verkauf", "Verkaufsstand", Fmt.Thousands(StandSoldTotal) + " Artikel");
            Add("Verkauf", "Straßenfeste", Stats.FestivalsDone + " (" + Stats.FestivalSold + " Artikel, " + Fmt.Money(Stats.FestivalRevenue) + ")");
            Add("Verkauf", "Großaufträge", TotalContractsDone + " erfüllt, " + TotalContractsFailed + " geplatzt");
            Add("Verkauf", "Bewertung", Fmt.Dec(Reputation, 2) + " Sterne (" + ReviewCount + " Bewertungen)");
            Add("Logistik", "Porto bezahlt", Fmt.Money(TotalPorto));
            Add("Logistik", "Retouren", TotalReturns + " (" + Fmt.Money(TotalRefunds) + " erstattet)");
            Add("Logistik", "Defekte Ware verschickt", TotalDefectsShipped.ToString());
            Add("Logistik", "Müll entsorgt", Fmt.Thousands(TotalWasteCollected) + " Ladungen");
            Add("Logistik", "Verspätet durch Störungen", TotalDelayed.ToString());
            Add("Marketing", "Werbung ausgegeben", Fmt.Money(Stats.MarketingSpent));
            Add("Marketing", "TikToks", Stats.OwnTikToks + " eigene (bestes: " + Mathx.RoundToInt(Stats.BestTikTok * 100f) + " %)");
            Add("Marketing", "Fakes aufgeflogen", FakeExposedTotal.ToString());
            Add("Finanzen", "Höchster Kontostand", Fmt.Money(Stats.PeakMoney));
            Add("Finanzen", "Dividenden", Fmt.Money(Market.DividendsTotal));
            Add("Finanzen", "Sparzinsen", Fmt.Money(Market.SavingsInterestTotal));
            Add("Finanzen", "Einkaufsbummel (Läden)", Fmt.Money(ShopTotalSpent));
            Add("Finanzen", "Mama-Notkredit", Stats.MamaRescues == 0 ? "nie gebraucht" : Stats.MamaRescues + "× (offen: " + Fmt.Money(MamaDebt) + ")");
            return l;
        }

        // =====================================================================================
        // Speichern / Laden (über AddProgressState / ReadProgressState)
        // =====================================================================================
        private void AddMetaState(Dictionary<string, object> state)
        {
            var ach = new Dictionary<string, object>();
            foreach (var kv in Achievements) ach[kv.Key] = kv.Value;
            state["achievements"] = ach;
            state["lifetime_stats"] = Stats.ToJson();
            state["difficulty"] = Difficulty ?? GameData.DefaultDifficulty;
            state["mama_rescues_left"] = MamaRescuesLeft;
            state["mama_debt"] = MamaDebt;
            state["endless_tier"] = EndlessTier;
        }

        /// <summary>Fehlen die Felder (älterer Spielstand): Normal, ein Mama-Notgroschen, keine Erfolge.</summary>
        private void ReadMetaState(Dictionary<string, object> s)
        {
            Achievements.Clear();
            foreach (var kv in J.O(s, "achievements"))
                if (AchievementData.Find(kv.Key) != null) Achievements[kv.Key] = J.I(kv.Value, 1);
            Stats = LifetimeStats.FromJson(J.O(s, "lifetime_stats"));
            string diff = J.S(s, "difficulty", GameData.DefaultDifficulty);
            Difficulty = GameData.IsDifficulty(diff) ? diff : GameData.DefaultDifficulty;
            MamaRescuesLeft = Mathx.Clamp(J.I(s, "mama_rescues_left", DifficultyInfo.MamaRescues), 0, 5);
            MamaDebt = Math.Max(0, J.I(s, "mama_debt"));
            EndlessTier = Mathx.Clamp(J.I(s, "endless_tier"), 0, 20);
            _lastVideo = TikTokVideos.Count > 0 ? TikTokVideos[0] : null;
            _lastRealTime = RealTime;
            AchievementsLoaded?.Invoke(new List<string>(Achievements.Keys));
        }
    }
}
