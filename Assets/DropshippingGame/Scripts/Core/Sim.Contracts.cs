using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    public enum ContractState
    {
        /// <summary>Angebot, wartet auf Zusage.</summary>
        Offered = 0,
        /// <summary>Angenommen, läuft.</summary>
        Active = 1,
        /// <summary>Erfüllt und bezahlt.</summary>
        Completed = 2,
        /// <summary>Frist verpasst (Vertragsstrafe).</summary>
        Failed = 3,
        /// <summary>Abgelehnt.</summary>
        Declined = 4,
        /// <summary>Angebot verfallen.</summary>
        Expired = 5,
    }

    /// <summary>Ein Großauftrag (B2B): Firma will eine Menge eines Produkts bis zu einer Frist.</summary>
    public sealed class Contract
    {
        public int Id;
        public string Company = "", Reason = "", Product = "huelle";
        /// <summary>Bestellte Menge und bereits gelieferte Stück.</summary>
        public int Quantity, Delivered;
        /// <summary>Vergütung bei Erfüllung, Vertragsstrafe bei verpasster Frist, Erfahrungspunkte.</summary>
        public int Payment, Penalty, Xp;
        /// <summary>Bewertungseffekt bei Erfolg (+) bzw. Misserfolg (−, als positiver Wert gespeichert).</summary>
        public float RepBonus, RepPenalty;
        /// <summary>Mindestqualität der Ware (0 = egal, 0,8 = mind. Standard, 1,3 = Premium – wie <see cref="GameData.QualityName"/>).</summary>
        public float MinQuality;
        /// <summary>Frist in Tagen ab Annahme (inklusive Annahmetag, bis 20:00 Uhr).</summary>
        public int Days = 3;
        public int OfferDay, OfferExpiresDay, AcceptedDay, DeadlineDay, ClosedDay;
        public ContractState State;
        /// <summary>Sonderangebot aus einem Ereignis (höhere Vergütung).</summary>
        public bool Special;
        /// <summary>Tatsächlich ausgezahlter Betrag nach Abschluss (bei Misserfolg: Teilzahlung − Strafe).</summary>
        public int PaidOut;

        public int Remaining => Math.Max(0, Quantity - Delivered);
        public float Progress => Quantity > 0 ? Mathx.Clamp01((float)Delivered / Quantity) : 0f;
        public bool IsOffer => State == ContractState.Offered;
        public bool IsActive => State == ContractState.Active;
        public bool IsClosed => State != ContractState.Offered && State != ContractState.Active;
        public string Title => Quantity + "× " + GameData.Product(Product).Name;
        public string QualityText => MinQuality >= GameData.QualityPremium - 0.001f ? "nur Premium-Qualität"
            : (MinQuality >= GameData.QualityStandard - 0.001f ? "mind. Standard-Qualität" : "Qualität egal");
        public string Description => Company + " braucht " + Title + " " + Reason + ".";

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "id", Id }, { "company", Company }, { "reason", Reason }, { "product", Product }, { "quantity", Quantity },
            { "delivered", Delivered }, { "payment", Payment }, { "penalty", Penalty }, { "xp", Xp }, { "rep_bonus", (double)RepBonus },
            { "rep_penalty", (double)RepPenalty }, { "min_quality", (double)MinQuality }, { "days", Days }, { "offer_day", OfferDay },
            { "offer_expires_day", OfferExpiresDay }, { "accepted_day", AcceptedDay }, { "deadline_day", DeadlineDay },
            { "closed_day", ClosedDay }, { "state", (int)State }, { "special", Special }, { "paid_out", PaidOut },
        };

        public static Contract FromJson(object o)
        {
            var d = J.Obj(o);
            var c = new Contract
            {
                Id = J.I(d, "id"), Company = J.S(d, "company"), Reason = J.S(d, "reason"), Product = J.S(d, "product", "huelle"),
                Quantity = Math.Max(1, J.I(d, "quantity", 1)), Delivered = J.I(d, "delivered"), Payment = J.I(d, "payment"),
                Penalty = J.I(d, "penalty"), Xp = J.I(d, "xp"), RepBonus = J.F(d, "rep_bonus"), RepPenalty = J.F(d, "rep_penalty"),
                MinQuality = J.F(d, "min_quality"), Days = Math.Max(1, J.I(d, "days", 3)), OfferDay = J.I(d, "offer_day", 1),
                OfferExpiresDay = J.I(d, "offer_expires_day", 1), AcceptedDay = J.I(d, "accepted_day"), DeadlineDay = J.I(d, "deadline_day"),
                ClosedDay = J.I(d, "closed_day"), State = (ContractState)Mathx.Clamp(J.I(d, "state"), 0, 5), Special = J.B(d, "special"),
                PaidOut = J.I(d, "paid_out"),
            };
            if (!GameData.IsProduct(c.Product)) c.Product = "huelle";
            return c;
        }
    }

    /// <summary>
    /// F4 Großaufträge (B2B): ab Level 3 täglich 1-3 Angebote von Firmen. Angenommene Aufträge
    /// werden am Palettenplatz erfüllt – ganze Kisten (ohne Auspacken) oder Einzelartikel.
    /// Erfüllt → Geld, XP, Ruf; Frist verpasst → Vertragsstrafe und Rufverlust.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Angebote, laufende oder abgeschlossene Großaufträge haben sich geändert.</summary>
        public event Action ContractsChanged;
        /// <summary>Neues Angebot ist eingegangen.</summary>
        public event Action<Contract> ContractOffered;
        public event Action<Contract> ContractAccepted;
        /// <summary>Ware am Palettenplatz geliefert: (Auftrag, Stück).</summary>
        public event Action<Contract, int> ContractDelivered;
        /// <summary>Auftrag erfüllt – die Spedition holt die Palette ab.</summary>
        public event Action<Contract> ContractCompleted;
        /// <summary>Frist verpasst – Vertragsstrafe wurde abgebucht.</summary>
        public event Action<Contract> ContractFailed;

        /// <summary>Alle Aufträge: Angebote, laufende und die letzten abgeschlossenen (Verlauf).</summary>
        public List<Contract> Contracts = new List<Contract>();
        public int NextContractId = 1;
        public int TotalContractsDone, TotalContractsFailed;
        /// <summary>Uhrzeiten (Minuten) heute, zu denen noch Angebote eintreffen.</summary>
        public List<float> ContractOfferTimes = new List<float>();

        private const int MaxOpenOffers = 4;

        public bool ContractsUnlocked => Level >= GameData.ContractLevel;

        /// <summary>Wie viele Aufträge gleichzeitig laufen dürfen (Level + Skill "Networking").</summary>
        public int MaxActiveContracts() => ContractsUnlocked ? GameData.MaxActiveContracts(Level) + ContractSlotBonus() : 0;

        public List<Contract> ContractOffers()
        {
            var l = new List<Contract>();
            foreach (var c in Contracts)
                if (c.State == ContractState.Offered) l.Add(c);
            return l;
        }

        /// <summary>Laufende Aufträge, früheste Frist zuerst.</summary>
        public List<Contract> ActiveContracts()
        {
            var l = new List<Contract>();
            foreach (var c in Contracts)
                if (c.State == ContractState.Active) l.Add(c);
            l.Sort((a, b) => a.DeadlineDay != b.DeadlineDay ? a.DeadlineDay.CompareTo(b.DeadlineDay) : a.Id.CompareTo(b.Id));
            return l;
        }

        /// <summary>Abgeschlossene Aufträge (neueste zuerst).</summary>
        public List<Contract> ContractHistory()
        {
            var l = new List<Contract>();
            foreach (var c in Contracts)
                if (c.IsClosed) l.Add(c);
            l.Sort((a, b) => b.Id.CompareTo(a.Id));
            return l;
        }

        public int ActiveContractCount()
        {
            int n = 0;
            foreach (var c in Contracts)
                if (c.State == ContractState.Active) n++;
            return n;
        }

        public Contract FindContract(int id)
        {
            foreach (var c in Contracts)
                if (c.Id == id) return c;
            return null;
        }

        /// <summary>Offene Stückzahl eines Produkts über alle laufenden Aufträge.</summary>
        public int ContractUnitsNeeded(string pid)
        {
            int n = 0;
            foreach (var c in Contracts)
                if (c.State == ContractState.Active && c.Product == pid) n += c.Remaining;
            return n;
        }

        /// <summary>Tage bis zur Frist (laufend) bzw. bis das Angebot verfällt (0 = heute 20:00).</summary>
        public int ContractDaysLeft(Contract c) => c == null ? 0 : (c.State == ContractState.Active ? c.DeadlineDay : c.OfferExpiresDay) - Day;

        /// <summary>"heute bis 20:00", "morgen bis 20:00" oder "bis Do, 20:00 (3 Tage)".</summary>
        public string ContractDeadlineText(Contract c)
        {
            if (c == null) return "";
            int left = ContractDaysLeft(c);
            int day = Day + left;
            if (left <= 0) return "heute bis 20:00";
            if (left == 1) return "morgen bis 20:00";
            return "bis " + GameData.WeekdayShort(day) + ", 20:00 (" + (left + 1) + " Tage)";
        }

        /// <summary>Kann dieses Angebot jetzt angenommen werden? reason = deutscher Grund, falls nicht.</summary>
        public bool CanAcceptContract(Contract c, out string reason)
        {
            reason = "";
            if (c == null || c.State != ContractState.Offered)
            {
                reason = "Dieses Angebot gibt es nicht mehr.";
                return false;
            }
            if (!ContractsUnlocked)
            {
                reason = "Großaufträge gibt es ab Level " + GameData.ContractLevel + ".";
                return false;
            }
            if (ActiveContractCount() >= MaxActiveContracts())
            {
                reason = "Du hast schon " + MaxActiveContracts() + " laufende(n) Großauftrag/-aufträge. Erst liefern!";
                return false;
            }
            return true;
        }

        // =====================================================================================
        // Angebote
        // =====================================================================================
        private int RoundTo5(float v) => Math.Max(5, Mathx.RoundToInt(v / 5f) * 5);

        /// <summary>Grundwert eines Auftrags (€): wächst mit Level und Standort, damit Aufträge ein Bonus bleiben.</summary>
        private float ContractValue() =>
            (GameData.ContractValueBase + GameData.ContractValuePerLevel * Level) * (LocationStage >= 1 ? GameData.ContractWarehouseMult : 1f) *
            Rng.Range(0.85f, 1.25f);

        /// <summary>Stückzahl zum Auftragswert (teure Produkte = weniger Stück), auf 5 gerundet.</summary>
        private int ContractQuantity(ProductDef p) =>
            Mathx.Clamp(Mathx.RoundToInt(ContractValue() / (p.RefPrice * 0.35f) / 5f) * 5, 10, 250);

        /// <summary>
        /// Erzeugt ein neues Angebot (auch für Ereignisse). payBonus multipliziert die Vergütung,
        /// product erzwingt ein Produkt (muss verfügbar sein), company einen Firmennamen.
        /// Gibt null zurück, wenn kein Produkt verfügbar ist.
        /// </summary>
        public Contract GenerateContractOffer(float payBonus = 1f, bool special = false, string product = "", string company = "")
        {
            var pool = new List<string>();
            foreach (var p in GameData.Products)
            {
                if (!ProductAvailable(p.Id)) continue;
                pool.Add(p.Id);
                if (IsListed(p.Id)) pool.Add(p.Id);
            }
            if (pool.Count == 0) return null;
            string pid = !string.IsNullOrEmpty(product) && ProductAvailable(product) ? product : pool[Rng.Index(pool.Count)];
            var pd = GameData.Product(pid);
            int qty = ContractQuantity(pd);
            // Qualitätsanspruch nach den Stufen von GameData.QualityName (Standard ab 0,8, Premium ab 1,3).
            float minQ = 0f, qMult = 1f;
            float r = Rng.Value();
            if (Level >= 5 && r < 0.1f)
            {
                minQ = GameData.QualityPremium;
                qMult = 1.45f;
            }
            else if (r < 0.3f)
            {
                minQ = GameData.QualityStandard;
                qMult = 1.15f;
            }
            int days = qty <= 40 ? Rng.RangeInt(2, 3) : (qty <= 100 ? Rng.RangeInt(2, 4) : Rng.RangeInt(3, 4));
            float unit = pd.RefPrice * Rng.Range(GameData.ContractPayMin, GameData.ContractPayMax) * qMult * payBonus * ContractPayMult();
            int pay = RoundTo5(qty * unit);
            var c = new Contract
            {
                Id = NextContractId++, Company = string.IsNullOrEmpty(company) ? Rng.Pick(GameData.Companies) : company,
                Reason = Rng.Pick(GameData.ContractReasons), Product = pid,
                Quantity = qty, Payment = pay, Penalty = RoundTo5(pay * GameData.ContractPenaltyFactor),
                Xp = Math.Max(20, Mathx.RoundToInt(pay * GameData.ContractXpFactor)),
                RepBonus = GameData.ContractRepBonus + (qty >= 100 ? 0.04f : 0f), RepPenalty = GameData.ContractRepPenalty,
                MinQuality = minQ, Days = days, OfferDay = Day, OfferExpiresDay = Day + 1, State = ContractState.Offered, Special = special,
            };
            Contracts.Add(c);
            Notify("Neue Großanfrage: " + c.Company + " will " + c.Title + " (" + Fmt.Money(c.Payment) + ").", "info");
            Sound("notify", 0.03f, -6f);
            ContractOffered?.Invoke(c);
            ContractsChanged?.Invoke();
            RaiseEconomyChanged();
            return c;
        }

        public bool AcceptContract(int id)
        {
            var c = FindContract(id);
            if (!CanAcceptContract(c, out string reason))
            {
                Notify(reason, "bad");
                Sound("error");
                return false;
            }
            c.State = ContractState.Active;
            c.AcceptedDay = Day;
            c.DeadlineDay = Day + c.Days - 1;
            Notify("Großauftrag angenommen: " + c.Title + " für " + c.Company + " – " + ContractDeadlineText(c) + ".", "good");
            Sound("click");
            ContractAccepted?.Invoke(c);
            ContractsChanged?.Invoke();
            RaiseEconomyChanged();
            return true;
        }

        public bool DeclineContract(int id)
        {
            var c = FindContract(id);
            if (c == null || c.State != ContractState.Offered) return false;
            c.State = ContractState.Declined;
            c.ClosedDay = Day;
            TrimContractHistory();
            ContractsChanged?.Invoke();
            RaiseEconomyChanged();
            return true;
        }

        /// <summary>Eilauftrag (Ereignis): Frist 1 Tag kürzer, Vergütung × payMult.</summary>
        public bool RushContract(Contract c, float payMult)
        {
            if (c == null || c.State != ContractState.Active || c.DeadlineDay - Day < 1) return false;
            c.DeadlineDay -= 1;
            c.Payment = RoundTo5(c.Payment * payMult);
            c.Penalty = RoundTo5(c.Payment * GameData.ContractPenaltyFactor);
            ContractsChanged?.Invoke();
            RaiseEconomyChanged();
            return true;
        }

        /// <summary>Würfelt die Angebote für heute: werktags 1 (Level 3-4), 1-2 (Level 5-7), 1-3 (ab Level 8); am Wochenende 0-1.</summary>
        private int RollContractOffersToday()
        {
            if (!ContractsUnlocked) return 0;
            if (Weekday >= 5) return Rng.Value() < 0.5f ? 1 : 0;
            if (Level < 5) return 1;
            return Level < 8 ? Rng.RangeInt(1, 2) : Rng.RangeInt(1, 3);
        }

        private void ContractsNewDay()
        {
            ContractOfferTimes.Clear();
            int count = RollContractOffersToday();
            for (int i = 0; i < count; i++) ContractOfferTimes.Add(Rng.Range(500f, 900f));
            ContractOfferTimes.Sort();
        }

        private void UpdateContractOffers()
        {
            while (ContractOfferTimes.Count > 0 && TimeMinutes >= ContractOfferTimes[0])
            {
                ContractOfferTimes.RemoveAt(0);
                if (ContractOffers().Count < MaxOpenOffers) GenerateContractOffer();
            }
        }

        /// <summary>Feierabend: Fristen prüfen, verfallene Angebote schließen.</summary>
        private void ContractsEndDay()
        {
            bool changed = false;
            foreach (var c in Contracts.ToArray())
            {
                if (c.State == ContractState.Active && Day >= c.DeadlineDay && c.Remaining > 0)
                {
                    FailContract(c);
                    changed = true;
                }
                else if (c.State == ContractState.Offered && Day >= c.OfferExpiresDay)
                {
                    c.State = ContractState.Expired;
                    c.ClosedDay = Day;
                    changed = true;
                }
            }
            ContractOfferTimes.Clear();
            if (!changed) return;
            TrimContractHistory();
            ContractsChanged?.Invoke();
        }

        private void TrimContractHistory()
        {
            var closed = ContractHistory();
            for (int i = GameData.ContractHistory; i < closed.Count; i++) Contracts.Remove(closed[i]);
        }

        // =====================================================================================
        // Liefern (Palettenplatz)
        // =====================================================================================
        /// <summary>Laufender Auftrag, der diesen Gegenstand annehmen würde (früheste Frist zuerst) oder null.</summary>
        public Contract ContractFor(ItemData item)
        {
            if (item == null || (item.Kind != ItemKind.Crate && item.Kind != ItemKind.Item)) return null;
            if (item.Kind == ItemKind.Item && item.OrderId > 0) return null;
            if (item.Kind == ItemKind.Crate && item.Quantity <= 0) return null;
            if (item.ContractId > 0)
            {
                var own = FindContract(item.ContractId);
                if (own != null && own.State == ContractState.Active && own.Product == item.Product && own.Remaining > 0 &&
                    item.Quality >= own.MinQuality - 0.001f) return own;
            }
            foreach (var c in ActiveContracts())
                if (c.Product == item.Product && c.Remaining > 0 && item.Quality >= c.MinQuality - 0.001f) return c;
            return null;
        }

        public bool ContractAccepts(ItemData item) => ContractFor(item) != null;

        /// <summary>
        /// Palettenplatz: liefert eine Kiste (so viele Stück wie gebraucht, der Rest bleibt in der Kiste –
        /// item.Quantity sinkt) oder einen Einzelartikel an den passenden laufenden Auftrag.
        /// Gibt die gelieferte Stückzahl zurück (0 = abgelehnt, mit Hinweis).
        /// </summary>
        public int ContractDeliver(ItemData item)
        {
            if (item == null) return 0;
            if (item.Kind == ItemKind.Item && item.OrderId > 0)
            {
                Notify("Der Artikel gehört zur Bestellung #" + item.OrderId + " – ab zum Packtisch damit.", "info");
                return 0;
            }
            if (item.Kind != ItemKind.Crate && item.Kind != ItemKind.Item)
            {
                Notify("Die Spedition nimmt hier nur Kisten und einzelne Artikel für Großaufträge an.", "info");
                return 0;
            }
            var c = ContractFor(item);
            if (c == null)
            {
                string pname = GameData.Product(item.Product).Name;
                if (ActiveContractCount() == 0) Notify("Kein laufender Großauftrag. Angebote findest du in der App 'Aufträge'.", "info");
                else if (ContractUnitsNeeded(item.Product) <= 0) Notify("Kein laufender Großauftrag braucht " + pname + ".", "bad");
                else Notify("Qualität reicht nicht: Die Firma will " + QualityFor(item.Product) + ".", "bad");
                Sound("error");
                return 0;
            }
            int units = item.Kind == ItemKind.Crate ? Math.Min(item.Quantity, c.Remaining) : 1;
            if (units <= 0) return 0;
            c.Delivered += units;
            if (item.Kind == ItemKind.Crate) item.Quantity -= units;
            Sound("place");
            ContractDelivered?.Invoke(c, units);
            if (c.Remaining <= 0) CompleteContract(c);
            else Notify(units + "× " + GameData.Product(c.Product).Name + " für " + c.Company + " auf die Palette (" + c.Delivered + "/" + c.Quantity + ").", "good");
            ContractsChanged?.Invoke();
            RaiseEconomyChanged();
            return units;
        }

        private string QualityFor(string pid)
        {
            foreach (var c in ActiveContracts())
                if (c.Product == pid && c.Remaining > 0) return c.QualityText;
            return "bessere Qualität";
        }

        /// <summary>Laufender Auftrag, der dieses Produkt noch braucht (früheste Frist) oder null.</summary>
        public Contract ContractWanting(string pid)
        {
            foreach (var c in ActiveContracts())
                if (c.Product == pid && c.Remaining > 0) return c;
            return null;
        }

        /// <summary>
        /// Regal: nimmt einen Einzelartikel für einen Großauftrag (ohne Kundenbestellung). Macht
        /// <see cref="PickItem"/> automatisch, wenn keine Bestellung für das Produkt offen ist.
        /// </summary>
        public ItemData PickForContract(string id)
        {
            var c = ContractWanting(id);
            string pname = GameData.Product(id).Name;
            if (c == null)
            {
                Notify("Kein laufender Großauftrag braucht " + pname + ".", "info");
                return null;
            }
            if (StockQty(id) <= 0)
            {
                Notify("Kein " + pname + " mehr auf Lager! Im Laptop nachbestellen.", "bad");
                return null;
            }
            if (StockQuality(id) < c.MinQuality - 0.001f)
            {
                Notify("Die Ware im Regal ist zu billig für " + c.Company + " (" + c.QualityText + ").", "bad");
                return null;
            }
            Stock[id].Qty -= 1;
            RaiseEconomyChanged();
            return new ItemData { Kind = ItemKind.Item, Product = id, Quality = StockQuality(id), ContractId = c.Id, Created = BClock() };
        }

        /// <summary>
        /// Lagerist:in bestückt die Palette des dringendsten Auftrags aus dem Lager (bis zu 10 Stück je
        /// Arbeitsgang, 20 Stück bleiben als Reserve für Kundenbestellungen im Regal).
        /// </summary>
        private bool StaffStockContract()
        {
            foreach (var c in ActiveContracts())
            {
                if (c.Remaining <= 0 || StockQuality(c.Product) < c.MinQuality - 0.001f) continue;
                int n = Math.Min(Math.Min(10, c.Remaining), StockQty(c.Product) - GameData.ContractStockReserve);
                if (n <= 0) continue;
                Stock[c.Product].Qty -= n;
                c.Delivered += n;
                ContractDelivered?.Invoke(c, n);
                if (c.Remaining <= 0) CompleteContract(c);
                ContractsChanged?.Invoke();
                RaiseEconomyChanged();
                return true;
            }
            return false;
        }

        private void CompleteContract(Contract c)
        {
            c.State = ContractState.Completed;
            c.ClosedDay = Day;
            c.PaidOut = c.Payment;
            Money += c.Payment;
            Daily.Revenue += c.Payment;
            Daily.ContractIncome += c.Payment;
            TotalEarned += c.Payment;
            Daily.ContractsDone++;
            TotalContractsDone++;
            Daily.Notes.Add("Großauftrag erfüllt: " + c.Company + " (+" + Fmt.Money(c.Payment) + ")");
            Notify("Großauftrag erfüllt! " + c.Company + " zahlt " + Fmt.Money(c.Payment) + ". Die Spedition holt die Palette ab.", "good");
            Sound("cash");
            ChangeReputation(c.RepBonus);
            AddXp(c.Xp);
            ChallengeProgress("contract", 1f);
            ChallengeProgress("revenue", c.Payment);
            ContractCompleted?.Invoke(c);
            TrimContractHistory();
            CheckGoals();
        }

        private void FailContract(Contract c)
        {
            c.State = ContractState.Failed;
            c.ClosedDay = Day;
            int partial = Mathx.RoundToInt(c.Payment * ((float)c.Delivered / Math.Max(1, c.Quantity)) * GameData.ContractPartialPay);
            if (partial > 0)
            {
                Money += partial;
                Daily.Revenue += partial;
                Daily.ContractIncome += partial;
                TotalEarned += partial;
            }
            Money -= c.Penalty;
            Daily.Penalties += c.Penalty;
            c.PaidOut = partial - c.Penalty;
            Daily.ContractsFailed++;
            TotalContractsFailed++;
            Daily.Notes.Add("Großauftrag geplatzt: " + c.Company + " (Strafe " + Fmt.Money(c.Penalty) + ")");
            Notify("Frist verpasst! " + c.Company + " verlangt " + Fmt.Money(c.Penalty) + " Vertragsstrafe" +
                   (partial > 0 ? " (Teillieferung: +" + Fmt.Money(partial) + ")" : "") + ".", "bad");
            Sound("bad");
            ChangeReputation(-c.RepPenalty);
            ContractFailed?.Invoke(c);
        }
    }
}
