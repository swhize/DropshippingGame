using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>Ein Wochenziel (Montag bis Sonntagabend).</summary>
    public sealed class WeeklyChallenge
    {
        /// <summary>Eindeutig innerhalb der Woche (Typ, bei Bonuszielen mit Zusatz).</summary>
        public string Id = "";
        /// <summary>ship, revenue, rating, stars5, express, contract, stand, tiktok, product, restock, perfect_day.</summary>
        public string Type = "";
        public string Title = "", Desc = "", Icon = "trophy";
        /// <summary>Produkt bei Typ "product".</summary>
        public string Product = "";
        public float Target, Progress;
        public int Reward, Xp, Week;
        public bool Done;
        /// <summary>Zusatzziel aus einem Ereignis (z. B. Kalles Wette).</summary>
        public bool Bonus;
        /// <summary>Wer das Bonusziel ausgelobt hat ("Kalle"), sonst leer.</summary>
        public string Sponsor = "";

        public float Fraction => Target > 0f ? Mathx.Clamp01(Progress / Target) : 0f;

        /// <summary>"12 / 40", "1.234 € / 2.000 €" oder "4,1 / 4,3 ★".</summary>
        public string ProgressText
        {
            get
            {
                switch (Type)
                {
                    case "revenue": return Fmt.Money(Mathx.RoundToInt(Math.Min(Progress, Target))) + " / " + Fmt.Money(Mathx.RoundToInt(Target));
                    case "rating": return Fmt.Rating(Progress) + " / " + Fmt.Rating(Target) + " ★";
                }
                return (int)Math.Min(Progress, Target) + " / " + (int)Target;
            }
        }

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "id", Id }, { "type", Type }, { "title", Title }, { "desc", Desc }, { "icon", Icon }, { "product", Product },
            { "target", (double)Target }, { "progress", (double)Progress }, { "reward", Reward }, { "xp", Xp }, { "week", Week },
            { "done", Done }, { "bonus", Bonus }, { "sponsor", Sponsor },
        };

        public static WeeklyChallenge FromJson(object o)
        {
            var d = J.Obj(o);
            return new WeeklyChallenge
            {
                Id = J.S(d, "id"), Type = J.S(d, "type"), Title = J.S(d, "title"), Desc = J.S(d, "desc"), Icon = J.S(d, "icon", "trophy"),
                Product = J.S(d, "product"), Target = J.F(d, "target", 1f), Progress = J.F(d, "progress"), Reward = J.I(d, "reward"),
                Xp = J.I(d, "xp"), Week = J.I(d, "week", 1), Done = J.B(d, "done"), Bonus = J.B(d, "bonus"), Sponsor = J.S(d, "sponsor"),
            };
        }
    }

    /// <summary>
    /// F6 Wochenziele: Tag 1 ist ein Montag. Jeden Montag gibt es 3 Wochenziele (am Leistungsniveau
    /// der letzten Tage bemessen) mit Geld + XP als Belohnung. Unfertige Ziele verfallen Sonntagabend.
    /// </summary>
    public sealed partial class Sim
    {
        public event Action<WeeklyChallenge> ChallengeCompleted;
        public event Action ChallengesChanged;
        /// <summary>Neue Woche hat begonnen (Wochennummer), neue Wochenziele sind da.</summary>
        public event Action<int> WeekStarted;

        public List<WeeklyChallenge> Challenges = new List<WeeklyChallenge>();
        /// <summary>Woche, für die <see cref="Challenges"/> erzeugt wurden.</summary>
        public int ChallengeWeek;
        public int TotalChallengesDone;

        /// <summary>0 = Montag ... 6 = Sonntag.</summary>
        public int Weekday => GameData.WeekdayOf(Day);
        /// <summary>Wochennummer ab 1.</summary>
        public int Week => (Math.Max(1, Day) - 1) / 7 + 1;
        public string WeekdayName() => GameData.WeekdayName(Day);
        public string WeekdayShort() => GameData.WeekdayShort(Day);
        /// <summary>Verbleibende Tage nach heute bis Sonntag (Sonntag = 0).</summary>
        public int DaysLeftInWeek() => 6 - Weekday;
        public float WeekdayDemand() => GameData.WeekdayDemand[Weekday];

        public int ChallengesDoneThisWeek()
        {
            int n = 0;
            foreach (var c in Challenges)
                if (c.Done) n++;
            return n;
        }

        // =====================================================================================
        // Erzeugen
        // =====================================================================================
        private float AvgShippedPerDay()
        {
            int n = 0, sum = 0;
            for (int i = History.Count - 1; i >= 0 && n < 7; i--, n++) sum += History[i].Shipped;
            if (n >= 2) return Math.Max(3f, sum / (float)n);
            return 3f + Level * 3f;
        }

        private float AvgRevenuePerDay()
        {
            int n = 0, sum = 0;
            for (int i = History.Count - 1; i >= 0 && n < 7; i--, n++) sum += History[i].Revenue;
            if (n >= 2) return Math.Max(60f, sum / (float)n);
            return AvgShippedPerDay() * 21f;
        }

        private static int Round5Int(float v) => Math.Max(5, Mathx.RoundToInt(v / 5f) * 5);
        private static int Round50Int(float v) => Math.Max(50, Mathx.RoundToInt(v / 50f) * 50);

        /// <summary>Neue Wochenziele (ersetzt die alten). Mitten in der Woche werden Ziele anteilig kleiner.</summary>
        public void StartWeek(bool announce = true)
        {
            Challenges.Clear();
            ChallengeWeek = Week;
            float scale = Math.Max(2f, 7f - Weekday) / 7f;
            var pool = new List<string>();
            var weights = new List<float>();
            void Add(string t, float w, bool ok)
            {
                if (!ok) return;
                pool.Add(t);
                weights.Add(w);
            }
            Add("rating", 0.6f, Reputation < 4.6f);
            Add("stars5", 0.6f, true);
            Add("express", 0.9f, ExpressUnlocked);
            Add("contract", 0.9f, ContractsUnlocked);
            Add("stand", 0.7f, HasUpgrade("stand"));
            Add("tiktok", 0.5f, Level >= GameData.TikTokLevel);
            Add("product", 0.7f, Events.ListedProducts().Count > 0);
            Add("restock", 0.5f, TotalReturns >= 3);
            Add("perfect_day", 0.6f, true);
            // Ein Durchsatz-Ziel ist immer dabei, dazu zwei weitere.
            string core = Rng.Value() < 0.5f ? "ship" : "revenue";
            var types = new List<string> { core };
            pool.Add(core == "ship" ? "revenue" : "ship");
            weights.Add(0.8f);
            while (types.Count < GameData.ChallengesPerWeek && pool.Count > 0)
            {
                float total = 0f;
                foreach (var w in weights) total += w;
                float r = Rng.Value() * total;
                int idx = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++)
                {
                    r -= weights[i];
                    if (r <= 0f)
                    {
                        idx = i;
                        break;
                    }
                }
                types.Add(pool[idx]);
                pool.RemoveAt(idx);
                weights.RemoveAt(idx);
            }
            foreach (var t in types)
            {
                var c = MakeChallenge(t, scale);
                if (c != null) Challenges.Add(c);
            }
            if (announce)
            {
                Notify("Neue Woche, neue Ziele! " + Challenges.Count + " Wochenziele warten auf dich.", "info");
                WeekStarted?.Invoke(Week);
            }
            if (TutorialStep < 0 && StoryStage == "business") Explain("week", GameData.MailWeek, "trophy");
            CheckChallenges();
            ChallengesChanged?.Invoke();
        }

        private WeeklyChallenge MakeChallenge(string type, float scale)
        {
            float ship = AvgShippedPerDay();
            float rev = AvgRevenuePerDay();
            var c = new WeeklyChallenge { Id = type, Type = type, Week = Week };
            float factor = 1f;
            switch (type)
            {
                case "ship":
                    c.Target = Math.Max(10, Round5Int(ship * 7f * 1.1f * scale));
                    c.Title = "Paketlawine";
                    c.Desc = "Verschicke " + (int)c.Target + " Pakete.";
                    c.Icon = "package";
                    break;
                case "revenue":
                    c.Target = Math.Max(300, Round50Int(rev * 7f * 1.1f * scale));
                    c.Title = "Umsatzmaschine";
                    c.Desc = "Mach " + Fmt.Money((int)c.Target) + " Umsatz (Shop, Stand und Großaufträge).";
                    c.Icon = "coin";
                    break;
                case "rating":
                    c.Target = Mathx.Clamp((float)Math.Round(Reputation + 0.2f, 1), 3.8f, 4.8f);
                    c.Title = "Sternenhimmel";
                    c.Desc = "Bring deine Bewertung auf mindestens " + Fmt.Rating(c.Target) + " Sterne.";
                    c.Icon = "star";
                    factor = 0.9f;
                    break;
                case "stars5":
                    c.Target = Math.Max(3, Mathx.RoundToInt(ship * 7f * 0.12f * scale));
                    c.Title = "Fünf-Sterne-Regen";
                    c.Desc = "Sammle " + (int)c.Target + " Fünf-Sterne-Bewertungen.";
                    c.Icon = "star";
                    factor = 0.9f;
                    break;
                case "express":
                    c.Target = Math.Max(3, Mathx.RoundToInt(ship * 7f * 0.13f * scale));
                    c.Title = "Blitzversand";
                    c.Desc = "Verschicke " + (int)c.Target + " Express-Bestellungen pünktlich.";
                    c.Icon = "bolt";
                    factor = 1.1f;
                    break;
                case "contract":
                    c.Target = Level >= 6 && scale >= 0.7f ? 2 : 1;
                    c.Title = "Großkunden-Liebling";
                    c.Desc = c.Target >= 2 ? "Erfülle " + (int)c.Target + " Großaufträge." : "Erfülle einen Großauftrag.";
                    c.Icon = "factory";
                    factor = 1.2f;
                    break;
                case "stand":
                {
                    float perDay = StandSoldTotal / (float)Math.Max(1, Day - 1);
                    c.Target = Math.Max(5, Round5Int(Math.Max(10f, perDay * 7f * 1.2f) * scale));
                    c.Title = "Marktschreier";
                    c.Desc = "Verkaufe " + (int)c.Target + " Artikel am Verkaufsstand.";
                    c.Icon = "store";
                    factor = 0.9f;
                    break;
                }
                case "tiktok":
                    c.Target = Math.Max(2, Mathx.RoundToInt(4f * scale));
                    c.Title = "Content-Maschine";
                    c.Desc = "Poste " + (int)c.Target + " TikToks.";
                    c.Icon = "music";
                    factor = 0.8f;
                    break;
                case "product":
                {
                    var listed = Events.ListedProducts();
                    if (listed.Count == 0) return null;
                    string best = listed[0];
                    int bestN = -1, total = 0;
                    foreach (var id in listed)
                    {
                        int n = ShippedPerProduct.TryGetValue(id, out int v) ? v : 0;
                        total += n;
                        if (n > bestN)
                        {
                            bestN = n;
                            best = id;
                        }
                    }
                    if (Rng.Value() < 0.35f) best = listed[Rng.Index(listed.Count)];
                    float share = total > 0 ? Math.Max(0.25f, (ShippedPerProduct.TryGetValue(best, out int bn) ? bn : 0) / (float)total) : 1f / listed.Count;
                    var p = GameData.Product(best);
                    c.Product = best;
                    c.Id = "product_" + best;
                    c.Target = Math.Max(5, Round5Int(ship * 7f * share * 1.05f * scale));
                    c.Title = p.Short + "-Spezialist";
                    c.Desc = "Verkaufe " + (int)c.Target + "× " + p.Name + ".";
                    c.Icon = p.Icon;
                    break;
                }
                case "restock":
                    c.Target = Math.Max(1, Mathx.RoundToInt(3f * scale));
                    c.Title = "Zweite Chance";
                    c.Desc = "Bereite " + (int)c.Target + " Retouren als B-Ware auf.";
                    c.Icon = "box";
                    factor = 0.8f;
                    break;
                case "perfect_day":
                    c.Target = Math.Max(5, Round5Int(ship * 0.9f));
                    c.Title = "Fehlerfreier Tag";
                    c.Desc = "Verschicke an einem Tag " + (int)c.Target + " Pakete, ohne eine Bestellung zu verlieren.";
                    c.Icon = "check";
                    factor = 1.1f;
                    break;
                default:
                    return null;
            }
            c.Reward = Round5Int((GameData.ChallengeRewardBase + GameData.ChallengeRewardPerLevel * Level) * factor * Math.Max(0.5f, scale));
            c.Xp = GameData.ChallengeXpBase + GameData.ChallengeXpPerLevel * Level;
            return c;
        }

        /// <summary>Zusatz-Wochenziel (z. B. aus einem Ereignis). Zählt ab jetzt. product nur für Typ "product".</summary>
        public WeeklyChallenge AddBonusChallenge(string type, float target, int reward, int xp, string sponsor, string title, string desc,
                                                 string icon = "trophy", string product = "")
        {
            var c = new WeeklyChallenge
            {
                Id = "bonus_" + type + "_" + Day + "_" + Challenges.Count, Type = type, Target = Math.Max(1f, target), Reward = reward, Xp = xp,
                Week = Week, Bonus = true, Sponsor = sponsor ?? "", Title = title, Desc = desc, Icon = icon, Product = product ?? "",
            };
            Challenges.Add(c);
            ChallengesChanged?.Invoke();
            RaiseEconomyChanged();
            return c;
        }

        // =====================================================================================
        // Fortschritt
        // =====================================================================================
        /// <summary>Erhöht den Fortschritt aller offenen Wochenziele dieses Typs.</summary>
        public void ChallengeProgress(string type, float amount, string product = "")
        {
            if (Challenges.Count == 0) return;
            bool changed = false;
            foreach (var c in Challenges.ToArray())
            {
                if (c.Done || c.Type != type) continue;
                if (type == "product" && c.Product != product) continue;
                c.Progress += amount;
                changed = true;
                if (c.Progress >= c.Target) CompleteChallenge(c);
            }
            if (changed) ChallengesChanged?.Invoke();
        }

        /// <summary>Prüft zustandsbasierte Wochenziele (Bewertung, fehlerfreier Tag).</summary>
        public void CheckChallenges()
        {
            if (Challenges.Count == 0) return;
            foreach (var c in Challenges.ToArray())
            {
                if (c.Done) continue;
                switch (c.Type)
                {
                    case "rating":
                        c.Progress = Reputation;
                        if (Reputation >= c.Target - 0.0001f && ReviewCount >= 5) CompleteChallenge(c);
                        break;
                    case "perfect_day":
                        if (Daily.Lost == 0) c.Progress = Math.Max(c.Progress, Daily.Shipped);
                        if (c.Progress >= c.Target) CompleteChallenge(c);
                        break;
                }
            }
        }

        private void CompleteChallenge(WeeklyChallenge c)
        {
            if (c.Done) return;
            c.Done = true;
            c.Progress = Math.Max(c.Progress, c.Target);
            Money += c.Reward;
            Daily.IncomeOther += c.Reward;
            Daily.ChallengeRewards += c.Reward;
            Daily.ChallengesDone++;
            TotalChallengesDone++;
            Daily.Notes.Add("Wochenziel geschafft: " + c.Title + " (+" + Fmt.Money(c.Reward) + ")");
            Notify("Wochenziel geschafft: " + c.Title + "! +" + Fmt.Money(c.Reward) + " · +" + c.Xp + " XP", "good");
            Sound("levelup", 0f, -5f);
            AddXp(c.Xp);
            ChallengeCompleted?.Invoke(c);
            ChallengesChanged?.Invoke();
            CheckGoals();
            RaiseEconomyChanged();
        }
    }
}
