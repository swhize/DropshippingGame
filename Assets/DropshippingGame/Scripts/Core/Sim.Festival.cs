using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>Ablauf eines Mini-Events (Straßenfest/Flohmarkt).</summary>
    public enum FestivalPhase
    {
        /// <summary>Kein Fest geplant.</summary>
        None = 0,
        /// <summary>Per Mail angekündigt, findet an <see cref="Sim.FestivalDay"/> statt.</summary>
        Announced = 1,
        /// <summary>Vorbereitung: Ware in Kisten packen und zum Marktstand bringen.</summary>
        Prep = 2,
        /// <summary>Das Fest läuft: Besucher kommen an den Stand.</summary>
        Live = 3,
    }

    /// <summary>Was ein Festbesucher am Stand gemacht hat.</summary>
    public enum FestivalOutcome
    {
        /// <summary>Stand ist leer.</summary>
        Empty = 0,
        /// <summary>Nur geschaut.</summary>
        NoInterest = 1,
        /// <summary>Viel zu teuer, gleich weitergegangen.</summary>
        TooExpensive = 2,
        /// <summary>Gefeilscht, aber keine Einigung.</summary>
        HaggleFailed = 3,
        /// <summary>Zum Standpreis gekauft.</summary>
        Bought = 4,
        /// <summary>Nach Feilschen zum Angebotspreis gekauft.</summary>
        HaggledBought = 5,
    }

    /// <summary>Ergebnis eines Besuchs am Feststand.</summary>
    public struct FestivalVisit
    {
        public FestivalOutcome Outcome;
        public string Product;
        /// <summary>Stückpreis, zu dem verkauft wurde (bzw. Standpreis bei Nicht-Kauf).</summary>
        public int Price;
        /// <summary>Gebotener Stückpreis beim Feilschen (0 = nicht gefeilscht).</summary>
        public int Offer;
        public int Qty;
        public bool Sold => Outcome == FestivalOutcome.Bought || Outcome == FestivalOutcome.HaggledBought;
    }

    /// <summary>Bilanz eines Festes.</summary>
    public sealed class FestivalSummary
    {
        public string Name = "";
        public int Day;
        public int Visitors, Buyers, Sold, Revenue, Haggled, TooExpensive, Leftover, Xp;
        public float AwarenessGain;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "name", Name }, { "day", Day }, { "visitors", Visitors }, { "buyers", Buyers }, { "sold", Sold }, { "revenue", Revenue },
            { "haggled", Haggled }, { "too_expensive", TooExpensive }, { "leftover", Leftover }, { "xp", Xp }, { "awareness", (double)AwarenessGain },
        };

        public static FestivalSummary FromJson(object o)
        {
            var d = J.Obj(o);
            return new FestivalSummary
            {
                Name = J.S(d, "name", ""), Day = J.I(d, "day"), Visitors = J.I(d, "visitors"), Buyers = J.I(d, "buyers"), Sold = J.I(d, "sold"),
                Revenue = J.I(d, "revenue"), Haggled = J.I(d, "haggled"), TooExpensive = J.I(d, "too_expensive"), Leftover = J.I(d, "leftover"),
                Xp = J.I(d, "xp"), AwarenessGain = J.F(d, "awareness"),
            };
        }
    }

    public static partial class GameData
    {
        /// <summary>Ab diesem Firmenlevel kommen Einladungen zu Straßenfesten.</summary>
        public const int FestivalMinLevel = 2;
        /// <summary>Chance pro Tageswechsel auf eine Einladung (wenn keins geplant ist).</summary>
        public const float FestivalChancePerDay = 0.14f;
        /// <summary>Mindestabstand in Tagen zwischen zwei Festen.</summary>
        public const int FestivalCooldownDays = 4;
        /// <summary>Stück, die der Marktstand fasst.</summary>
        public const int FestivalCapacity = 120;
        /// <summary>Stück pro gepackter Festkiste.</summary>
        public const int FestivalCrateSize = 10;
        /// <summary>Geplante Zeiten (Spielminuten): Vorbereitung ab 10:00, Fest 12:30–16:00.</summary>
        public const float FestivalPrepAt = 600f;
        public const float FestivalLiveAt = 750f;
        public const float FestivalEndAt = 960f;
        /// <summary>Admin-Start: Länge von Vorbereitung und Fest in Spielminuten.</summary>
        public const float FestivalAdminPrep = 120f;
        public const float FestivalAdminLive = 180f;
        /// <summary>Bekanntheit pro verkauftem Stück.</summary>
        public const float FestivalAwarenessPerSale = 0.004f;

        public static readonly string[] FestivalNames = { "Straßenfest", "Flohmarkt", "Sommerfest", "Nachtflohmarkt" };

        public static readonly string[] FestivalBuyShouts =
        {
            "Nehm ich!", "Super Teil!", "Perfekt als Geschenk!", "Hab ich schon auf TikTok gesehen!", "Da kann man nicht meckern.",
            "Einmal bitte!", "Endlich hab ich's!",
        };

        public static readonly string[] FestivalHaggleShouts = { "Machst du's billiger?", "Letztes Angebot!", "Na komm, Festpreis?" };
        public static readonly string[] FestivalLookShouts = { "Mal schauen ...", "Nett hier.", "Hmm.", "Ich guck nur." };
    }

    /// <summary>
    /// Mini-Event Straßenfest/Flohmarkt: Einladung per Mail, Vorbereitungsphase (Ware aus dem Lager in
    /// Festkisten packen und zum Marktstand bringen), dann die Festphase, in der viele Besucher an den
    /// Stand kommen, schauen, feilschen oder kaufen. Am Ende gibt es eine Bilanz, Restware geht zurück ins Lager.
    /// Die Welt (Stand, Besucher) hängt sich an <see cref="FestivalChanged"/> und fragt Besuche über
    /// <see cref="FestivalVisitor"/> ab. Nutzt einen eigenen Zufallsgenerator, damit andere Systeme
    /// (und deren Tests) unverändert deterministisch bleiben.
    /// </summary>
    public sealed partial class Sim
    {
        public event Action<FestivalPhase> FestivalChanged;
        /// <summary>(Produkt-ID, Erlös gesamt, Menge)</summary>
        public event Action<string, int, int> FestivalSale;
        public event Action<FestivalSummary> FestivalEnded;

        public FestivalPhase Festival = FestivalPhase.None;
        public int FestivalDay;
        public float FestivalPrepStart, FestivalLiveStart, FestivalLiveEnd;
        public string FestivalName = "";
        public Dictionary<string, int> FestivalStock = new Dictionary<string, int>();
        public FestivalSummary FestivalStats = new FestivalSummary();
        public FestivalSummary LastFestival;
        /// <summary>Tag des letzten Festes (für den Abstand zwischen Einladungen).</summary>
        public int FestivalLastDay = -100;

        private Rng _festRng;
        private Rng FestRng => _festRng ?? (_festRng = new Rng());

        /// <summary>Startet den Festzufall neu (Tests / neues Spiel).</summary>
        public void FestivalReseed(int seed) => _festRng = new Rng(seed);

        public bool FestivalActive => Festival == FestivalPhase.Prep || Festival == FestivalPhase.Live;
        public bool FestivalToday => Festival != FestivalPhase.None && FestivalDay == Day;

        private void FestivalReset()
        {
            Festival = FestivalPhase.None;
            FestivalDay = 0;
            FestivalPrepStart = FestivalLiveStart = FestivalLiveEnd = 0f;
            FestivalName = "";
            FestivalStock.Clear();
            FestivalStats = new FestivalSummary();
            LastFestival = null;
            FestivalLastDay = -100;
        }

        public int FestivalTotal()
        {
            int n = 0;
            foreach (var kv in FestivalStock) n += Math.Max(0, kv.Value);
            return n;
        }

        public int FestivalQty(string pid) => pid != null && FestivalStock.TryGetValue(pid, out int n) ? n : 0;

        /// <summary>Spielminuten bis zum nächsten Phasenwechsel (0, wenn nichts ansteht).</summary>
        public float FestivalMinutesLeft()
        {
            if (Festival == FestivalPhase.Prep) return Math.Max(0f, FestivalLiveStart - TimeMinutes);
            if (Festival == FestivalPhase.Live) return Math.Max(0f, FestivalLiveEnd - TimeMinutes);
            if (Festival == FestivalPhase.Announced && FestivalDay == Day) return Math.Max(0f, FestivalPrepStart - TimeMinutes);
            return 0f;
        }

        /// <summary>Kurzer Statustext für HUD und Admin-Panel.</summary>
        public string FestivalStatusText()
        {
            switch (Festival)
            {
                case FestivalPhase.Announced:
                    return FestivalDay == Day
                        ? FestivalName + " heute ab " + Fmt.Clock(FestivalPrepStart)
                        : FestivalName + " am Tag " + FestivalDay + " (" + GameData.WeekdayName(FestivalDay) + ")";
                case FestivalPhase.Prep: return "Vorbereitung – noch " + FestivalCountdown() + " bis Start";
                case FestivalPhase.Live: return FestivalName + " läuft – noch " + FestivalCountdown();
            }
            return "Kein Fest geplant";
        }

        /// <summary>Restzeit als "1:25 h" bzw. "40 min".</summary>
        public string FestivalCountdown()
        {
            int m = (int)Math.Ceiling(FestivalMinutesLeft());
            return m >= 60 ? (m / 60) + ":" + (m % 60).ToString("00") + " h" : m + " min";
        }

        // =====================================================================================
        // Planung
        // =====================================================================================

        /// <summary>Kündigt ein Fest für einen Tag an (Mail). Gibt false zurück, wenn schon eins geplant ist.</summary>
        public bool FestivalAnnounce(int day)
        {
            if (Festival != FestivalPhase.None || day < Day) return false;
            FestivalName = GameData.FestivalNames[FestRng.Index(GameData.FestivalNames.Length)];
            FestivalDay = day;
            FestivalPrepStart = GameData.FestivalPrepAt;
            FestivalLiveStart = GameData.FestivalLiveAt;
            FestivalLiveEnd = GameData.FestivalEndAt;
            FestivalStock.Clear();
            FestivalStats = new FestivalSummary { Name = FestivalName, Day = day };
            Festival = FestivalPhase.Announced;
            string when = day == Day ? "heute" : (day == Day + 1 ? "morgen" : "am Tag " + day);
            Events.AddMail("Stadtmarketing", FestivalName + " " + when + "!",
                "Hallo " + BrandName + ",\n\n" + when + " (" + GameData.WeekdayName(day) + ") ist " + FestivalName + " vor deiner Tür – die Straße wird gesperrt, " +
                "es kommen hunderte Besucher. Wir haben dir einen Marktstand reserviert, gratis!\n\n" +
                "Ab " + Fmt.Clock(FestivalPrepStart) + " Uhr: Aufbau. Pack Ware aus dem Lager in Festkisten (Packtisch am Stand) " +
                "oder bring volle Lieferkisten direkt hin.\nVon " + Fmt.Clock(FestivalLiveStart) + " bis " + Fmt.Clock(FestivalLiveEnd) +
                " Uhr: Verkauf zu deinen Webshop-Preisen. Faire Preise und gehypte Produkte ziehen, zu teure Ware wird weggefeilscht.\n\nBis dann!", "star");
            FestivalChanged?.Invoke(Festival);
            RaiseEconomyChanged();
            return true;
        }

        /// <summary>Tageswechsel: verpasste Feste aufräumen, ggf. neue Einladung für morgen.</summary>
        private void FestivalNewDay()
        {
            if (Festival != FestivalPhase.None && FestivalDay < Day) FestivalFinish(false);
            if (Festival != FestivalPhase.None || StoryStage != "business" || Level < GameData.FestivalMinLevel) return;
            if (Day - FestivalLastDay < GameData.FestivalCooldownDays) return;
            if (FestRng.Value() < GameData.FestivalChancePerDay) FestivalAnnounce(Day + 1);
        }

        /// <summary>Jeden Zeitschritt: Phasenwechsel nach Uhrzeit.</summary>
        private void FestivalUpdate()
        {
            if (Festival == FestivalPhase.None) return;
            if (FestivalDay < Day)
            {
                FestivalFinish(false);
                return;
            }
            if (FestivalDay != Day) return;
            if (Festival == FestivalPhase.Announced && TimeMinutes >= FestivalPrepStart)
            {
                Festival = FestivalPhase.Prep;
                Notify(FestivalName + ": Aufbau läuft! Pack Ware ein und bring sie zum Marktstand auf der Straße.", "good");
                Sound("notify");
                FestivalChanged?.Invoke(Festival);
            }
            if (Festival == FestivalPhase.Prep && TimeMinutes >= FestivalLiveStart)
            {
                Festival = FestivalPhase.Live;
                Notify(FestivalName + " beginnt! " + (FestivalTotal() > 0 ? FestivalTotal() + " Artikel am Stand." : "Dein Stand ist noch leer!"),
                    FestivalTotal() > 0 ? "good" : "bad");
                Sound("notify");
                FestivalChanged?.Invoke(Festival);
            }
            if (Festival == FestivalPhase.Live && TimeMinutes >= FestivalLiveEnd) FestivalFinish(true);
        }

        // =====================================================================================
        // Vorbereitung
        // =====================================================================================

        /// <summary>Nächstes Produkt (nach 'after') mit Lagerbestand – für den Packtisch am Stand. "" = nichts auf Lager.</summary>
        public string FestivalNextPackProduct(string after = null)
        {
            var list = GameData.Products;
            int start = 0;
            if (!string.IsNullOrEmpty(after))
                for (int i = 0; i < list.Length; i++)
                    if (list[i].Id == after) start = i + 1;
            for (int k = 0; k < list.Length; k++)
            {
                var p = list[(start + k) % list.Length];
                if (StockQty(p.Id) > 0) return p.Id;
            }
            return "";
        }

        public bool FestivalCanStock => FestivalActive && FestivalDay == Day && IsOpen;

        /// <summary>
        /// Packt eine Festkiste (bis <see cref="GameData.FestivalCrateSize"/> Stück) aus dem Lager.
        /// Die Kiste kommt in die Hand und muss zum Marktstand getragen werden. null = nicht möglich.
        /// </summary>
        public ItemData FestivalPackCrate(string pid)
        {
            if (!FestivalCanStock)
            {
                Notify("Packen geht nur während des Festes (ab Aufbau).", "info");
                return null;
            }
            if (!IsProductId(pid) || StockQty(pid) <= 0)
            {
                Notify("Davon ist nichts auf Lager.", "info");
                return null;
            }
            int n = Math.Min(GameData.FestivalCrateSize, StockQty(pid));
            var crate = ItemData.Crate(pid, n, StockQuality(pid));
            Stock[pid].Qty -= n;
            Sound("tape");
            RaiseEconomyChanged();
            return crate;
        }

        /// <summary>Eine (Fest- oder Liefer-)Kiste am Marktstand auspacken. Rest bleibt in der Kiste.</summary>
        public int FestivalAddCrate(ItemData crate)
        {
            if (crate == null || crate.Kind != ItemKind.Crate || crate.Quantity <= 0 || !IsProductId(crate.Product)) return 0;
            if (!FestivalCanStock)
            {
                Notify("Der Marktstand ist gerade nicht aufgebaut.", "info");
                return 0;
            }
            int space = GameData.FestivalCapacity - FestivalTotal();
            if (space <= 0)
            {
                Notify("Der Marktstand ist voll.", "info");
                return 0;
            }
            int put = Math.Min(space, crate.Quantity);
            FestivalStock[crate.Product] = FestivalQty(crate.Product) + put;
            crate.Quantity -= put;
            FestivalChanged?.Invoke(Festival);
            RaiseEconomyChanged();
            return put;
        }

        // =====================================================================================
        // Festphase
        // =====================================================================================

        /// <summary>Wie viele Besucher pro Spielstunde grob kommen (für die Welt): mehr mit Bekanntheit und Bewertung.</summary>
        public float FestivalCrowdFactor() => Mathx.Clamp(0.8f + Awareness * 0.4f + (Reputation - 3f) * 0.1f, 0.6f, 1.6f);

        /// <summary>
        /// Ein Besucher schaut sich den Stand an und entscheidet: kaufen, feilschen, weitergehen.
        /// Preis vs. Referenzpreis (Markt), Hype und Bewertung zählen. Verkäufe buchen Geld, XP und Bekanntheit.
        /// </summary>
        public FestivalVisit FestivalVisitor()
        {
            var v = new FestivalVisit { Outcome = FestivalOutcome.Empty, Product = "" };
            if (Festival != FestivalPhase.Live || !IsOpen) return v;
            FestivalStats.Visitors++;
            int total = FestivalTotal();
            if (total <= 0) return v;

            // Auswahl gewichtet nach Menge und Hype: gehypte Ware springt eher ins Auge.
            float sum = 0f;
            foreach (var p in GameData.Products) sum += FestivalQty(p.Id) * TrendMult(p.Id);
            float r = FestRng.Value() * sum;
            string pid = "";
            foreach (var p in GameData.Products)
            {
                float w = FestivalQty(p.Id) * TrendMult(p.Id);
                if (w <= 0f) continue;
                pid = p.Id;
                if (r < w) break;
                r -= w;
            }
            if (pid == "") return v;
            v.Product = pid;
            int price = CurrentSalePrice(pid);
            v.Price = price;
            float refPrice = Math.Max(1f, Market.MarketPrice(pid));
            float hype = Mathx.Clamp01((TrendMult(pid) - GameData.TrendMin) / Math.Max(0.01f, GameData.TrendMax - GameData.TrendMin));

            float interest = 0.5f + hype * 0.3f + (Reputation - 3f) * 0.05f + Math.Min(0.12f, Awareness * 0.1f);
            if (FestRng.Value() > Mathx.Clamp(interest, 0.2f, 0.92f))
            {
                v.Outcome = FestivalOutcome.NoInterest;
                return v;
            }
            // Zahlungsbereitschaft: Festbesucher wollen Schnäppchen, Hype macht großzügiger.
            float willing = refPrice * (0.95f + hype * 0.35f) * FestRng.Range(0.85f, 1.15f);
            if (price <= willing)
            {
                v.Outcome = FestivalOutcome.Bought;
                v.Qty = FestivalQty(pid) >= 2 && FestRng.Value() < 0.2f ? 2 : 1;
                FestivalSell(pid, price, v.Qty);
                return v;
            }
            if (price <= willing * 1.3f)
            {
                FestivalStats.Haggled++;
                int offer = Math.Max(1, Mathx.RoundToInt(Math.Max(willing, price * 0.75f)));
                v.Offer = Math.Min(offer, price);
                // Du lässt dich runterhandeln, solange das Angebot mind. 80 % deines Preises ist.
                if (v.Offer >= price * 0.8f && FestRng.Value() < 0.75f)
                {
                    v.Outcome = FestivalOutcome.HaggledBought;
                    v.Price = v.Offer;
                    v.Qty = 1;
                    FestivalSell(pid, v.Offer, 1);
                }
                else v.Outcome = FestivalOutcome.HaggleFailed;
                return v;
            }
            FestivalStats.TooExpensive++;
            v.Outcome = FestivalOutcome.TooExpensive;
            return v;
        }

        private void FestivalSell(string pid, int unitPrice, int qty)
        {
            qty = Math.Min(qty, FestivalQty(pid));
            if (qty <= 0) return;
            int amount = unitPrice * qty;
            FestivalStock[pid] = FestivalQty(pid) - qty;
            Money += amount;
            Daily.Revenue += amount;
            Daily.Stand += amount;
            TotalEarned += amount;
            ShippedPerProduct[pid] = (ShippedPerProduct.TryGetValue(pid, out int s) ? s : 0) + qty;
            int xp = Math.Max(1, amount / 3);
            AddXp(xp);
            float aw = GameData.FestivalAwarenessPerSale * qty;
            Awareness = Math.Min(1.5f, Awareness + aw);
            FestivalStats.Buyers++;
            FestivalStats.Sold += qty;
            FestivalStats.Revenue += amount;
            FestivalStats.Xp += xp;
            FestivalStats.AwarenessGain += aw;
            Sound("cash", 0.04f, -4f);
            ChallengeProgress("stand", qty);
            ChallengeProgress("revenue", amount);
            ChallengeProgress("product", qty, pid);
            FestivalSale?.Invoke(pid, amount, qty);
            CheckGoals();
            RaiseEconomyChanged();
        }

        /// <summary>Beendet das Fest: Restware zurück ins Lager, Bilanz als Mail und Ereignis.</summary>
        private void FestivalFinish(bool announce)
        {
            if (Festival == FestivalPhase.None) return;
            bool happened = Festival == FestivalPhase.Live || Festival == FestivalPhase.Prep;
            int left = 0;
            foreach (var kv in FestivalStock)
            {
                if (kv.Value <= 0 || !Stock.ContainsKey(kv.Key)) continue;
                Stock[kv.Key].Qty += kv.Value;
                left += kv.Value;
            }
            FestivalStock.Clear();
            var s = FestivalStats ?? new FestivalSummary();
            s.Name = FestivalName;
            s.Day = FestivalDay;
            s.Leftover = left;
            Festival = FestivalPhase.None;
            if (happened)
            {
                FestivalLastDay = FestivalDay;
                LastFestival = s;
                Daily.Notes.Add(s.Name + ": " + s.Sold + " verkauft, " + Fmt.Money(s.Revenue) + " Umsatz.");
                string text = s.Visitors + " Besucher am Stand, " + s.Buyers + " haben gekauft (" + s.Sold + " Stück).\n" +
                              "Umsatz: " + Fmt.Money(s.Revenue) + " · +" + s.Xp + " XP · Bekanntheit +" + Mathx.RoundToInt(s.AwarenessGain * 100f) + " %\n" +
                              "Gefeilscht: " + s.Haggled + "× · Zu teuer: " + s.TooExpensive + "×" +
                              (left > 0 ? "\n" + left + " Artikel gingen zurück ins Lager." : "");
                Events.AddMail("Stadtmarketing", "Bilanz: " + s.Name, "Danke fürs Mitmachen!\n\n" + text, "star");
                if (announce) Notify(s.Name + " ist vorbei: " + s.Sold + " verkauft, " + Fmt.Money(s.Revenue) + ".", s.Sold > 0 ? "good" : "info");
                FestivalEnded?.Invoke(s);
            }
            FestivalChanged?.Invoke(Festival);
            RaiseEconomyChanged();
        }

        /// <summary>Tagesende: ein laufendes Fest endet, Restware geht zurück.</summary>
        private void FestivalEndDay()
        {
            if (FestivalActive) FestivalFinish(true);
        }

        // =====================================================================================
        // Admin
        // =====================================================================================

        /// <summary>Admin: Fest sofort starten (mit Vorbereitung bzw. direkt live). Bricht ein geplantes Fest ab.</summary>
        public bool AdminStartFestival(bool skipPrep)
        {
            if (!IsOpen)
            {
                Notify("Ein Fest geht nur im laufenden Geschäftstag.", "info");
                return false;
            }
            if (FestivalActive) FestivalFinish(false);
            Festival = FestivalPhase.None;
            if (!FestivalAnnounce(Day)) return false;
            float latest = GameData.DayEnd - 20f;
            FestivalPrepStart = TimeMinutes;
            FestivalLiveStart = Math.Min(TimeMinutes + (skipPrep ? 0f : GameData.FestivalAdminPrep), latest - 10f);
            FestivalLiveEnd = Math.Min(FestivalLiveStart + GameData.FestivalAdminLive, latest);
            if (FestivalLiveEnd <= FestivalLiveStart) FestivalLiveEnd = Math.Min(GameData.DayEnd - 1f, FestivalLiveStart + 10f);
            FestivalUpdate();
            return true;
        }

        /// <summary>Admin: Einladung für morgen (normaler Ablauf).</summary>
        public bool AdminAnnounceFestival() => FestivalAnnounce(Day + 1);

        /// <summary>Admin: laufendes Fest sofort beenden.</summary>
        public void AdminEndFestival() => FestivalFinish(true);

        // =====================================================================================
        // Speichern
        // =====================================================================================
        private Dictionary<string, object> FestivalToJson()
        {
            var stock = new Dictionary<string, object>();
            foreach (var kv in FestivalStock) if (kv.Value > 0) stock[kv.Key] = kv.Value;
            return new Dictionary<string, object>
            {
                { "phase", (int)Festival }, { "day", FestivalDay }, { "prep", (double)FestivalPrepStart }, { "live", (double)FestivalLiveStart },
                { "end", (double)FestivalLiveEnd }, { "name", FestivalName }, { "stock", stock }, { "stats", FestivalStats.ToJson() },
                { "last_day", FestivalLastDay },
            };
        }

        private void FestivalFromJson(Dictionary<string, object> d)
        {
            FestivalReset();
            if (d == null || d.Count == 0) return;
            int phase = J.I(d, "phase");
            Festival = phase >= 0 && phase <= 3 ? (FestivalPhase)phase : FestivalPhase.None;
            FestivalDay = J.I(d, "day");
            FestivalPrepStart = J.F(d, "prep", GameData.FestivalPrepAt);
            FestivalLiveStart = J.F(d, "live", GameData.FestivalLiveAt);
            FestivalLiveEnd = J.F(d, "end", GameData.FestivalEndAt);
            FestivalName = J.S(d, "name", GameData.FestivalNames[0]);
            FestivalLastDay = J.I(d, "last_day", -100);
            FestivalStats = FestivalSummary.FromJson(J.Get(d, "stats"));
            foreach (var kv in J.O(d, "stock"))
                if (IsProductId(kv.Key)) FestivalStock[kv.Key] = Math.Max(0, J.I(kv.Value));
        }
    }
}
