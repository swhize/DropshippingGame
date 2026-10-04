using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>
    /// F1 Bestellzettel: Nummer, Kundschaft, Notiz, Fälligkeit, Express, Bearbeitungsstand.
    /// Wartende Zettel liegen in <see cref="OrderQueue"/>, angefangene (Artikel entnommen) in
    /// <see cref="OrdersInWork"/>. Artikel und Pakete tragen die Zettel-Daten selbst mit sich.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Warteschlange oder Bearbeitungsstand eines Bestellzettels hat sich geändert.</summary>
        public event Action OrdersChanged;
        /// <summary>Neuer Bestellzettel ist eingegangen.</summary>
        public event Action<Order> OrderReceived;
        /// <summary>Ein wartender Bestellzettel ist abgelaufen (storniert).</summary>
        public event Action<Order> OrderExpired;
        /// <summary>Bestellung ging verloren, weil die Warteschlange voll war (Produkt-ID).</summary>
        public event Action<string> OrderLost;
        /// <summary>Paket zu einem Bestellzettel verschickt: (Zettel, Erlös, pünktlich).</summary>
        public event Action<Order, int, bool> OrderShipped;

        /// <summary>Angefangene Bestellzettel (Artikel entnommen, verpackt, etikettiert, auf dem Band).</summary>
        public List<Order> OrdersInWork = new List<Order>();
        public int NextOrderId = GameData.FirstOrderId;
        /// <summary>Neu gelistete Produkte: Zeitpunkt (Geschäftsuhr) der garantierten ersten Bestellung.</summary>
        public Dictionary<string, float> LaunchOrders = new Dictionary<string, float>();
        /// <summary>Ereignis "Express-Fieber": zusätzliche Express-Chance bis zu diesem Zeitpunkt.</summary>
        public float ExpressBoostUntil = -1f;
        public float ExpressBoostChance;
        /// <summary>Letzter Einkauf je Produkt: {Mengen-Index, Lieferanten-Index} (für Schnell-Nachbestellen).</summary>
        public Dictionary<string, int[]> LastPurchase = new Dictionary<string, int[]>();

        public void RaiseOrdersChanged() => OrdersChanged?.Invoke();

        // =====================================================================================
        // Express
        // =====================================================================================
        public bool ExpressUnlocked => Level >= GameData.ExpressMinLevel;

        /// <summary>Wahrscheinlichkeit, dass eine neue Bestellung Express ist (0 unter Level 2).</summary>
        public float ExpressChance()
        {
            if (!ExpressUnlocked) return 0f;
            float c = Math.Min(GameData.ExpressMaxChance,
                GameData.ExpressBaseChance + (Level - GameData.ExpressMinLevel) * GameData.ExpressChancePerLevel);
            if (BClock() < ExpressBoostUntil) c += ExpressBoostChance;
            return Mathx.Clamp01(c);
        }

        /// <summary>Für eine Weile mehr Express-Bestellungen (Ereignisse).</summary>
        public void AddExpressBoost(float chance, float minutes)
        {
            ExpressBoostChance = Math.Max(0f, chance);
            ExpressBoostUntil = BClock() + Math.Max(1f, minutes);
            RaiseEconomyChanged();
        }

        /// <summary>Express nur, solange die Warteschlange nicht (fast) voll ist – sonst wäre die Frist unschaffbar.</summary>
        private bool RollExpress() => ExpressUnlocked && OrderQueue.Count < QueueCapacity() - 1 && Rng.Value() < ExpressChance();

        /// <summary>Premium-Manufaktur: Tag der letzten Bestellung je Produkt (Kontingent 1 pro Tag).</summary>
        public Dictionary<string, int> PremiumBoughtDay = new Dictionary<string, int>();

        /// <summary>Darf heute noch bei der Premium-Manufaktur für dieses Produkt bestellt werden?</summary>
        public bool PremiumQuotaLeft(string pid) => !(PremiumBoughtDay.TryGetValue(pid, out int d) && d == Day);

        // =====================================================================================
        // Bestellzettel erzeugen
        // =====================================================================================
        public string RandomCustomer() => Rng.Pick(GameData.CustomerFirstNames) + " " + Rng.Pick(GameData.CustomerInitials);

        private string RandomOrderNote(string id, bool express)
        {
            if (express && Rng.Value() < 0.6f) return Rng.Pick(GameData.ExpressNotes);
            if (GameData.ProductNotes.TryGetValue(id, out var pn) && Rng.Value() < 0.35f) return Rng.Pick(pn);
            return Rng.Pick(GameData.OrderNotes);
        }

        /// <summary>Erzeugt einen Bestellzettel zum aktuellen Preis, ohne ihn einzureihen.</summary>
        public Order CreateOrder(string id, bool express)
        {
            int price = CurrentSalePrice(id);
            if (express) price = Math.Max(price + 1, Mathx.RoundToInt(price * GameData.ExpressPriceMult));
            float now = BClock();
            return new Order
            {
                Id = NextOrderId++, Product = id, Price = price, Created = now, Express = express,
                DueAt = now + (express ? GameData.ExpressDueMinutes : GameData.OrderDueMinutes),
                Customer = RandomCustomer(), City = Rng.Pick(GameData.Cities), Note = RandomOrderNote(id, express),
            };
        }

        /// <summary>Neue Bestellung (Express wird ab Level 2 ausgewürfelt).</summary>
        public void SpawnOrder(string id) => SpawnOrder(id, RollExpress());

        /// <summary>
        /// Neue Bestellung mit festgelegtem Express-Status. Gibt den Zettel zurück oder null, wenn die
        /// Warteschlange voll war (dann gilt die Bestellung als verloren).
        /// </summary>
        public Order SpawnOrder(string id, bool express)
        {
            Order order = null;
            if (OrderQueue.Count >= QueueCapacity())
            {
                TotalLostOrders++;
                Daily.Lost++;
                if (Rng.Value() < 0.6f) AddReview(id, 1, "Shop überlastet – Bestellung ging nicht durch");
                if (RealTime - _lastLostToast > 20f)
                {
                    _lastLostToast = RealTime;
                    Notify("Bestellung verloren – deine Warteschlange ist voll! (Preis erhöhen oder Personal holen)", "bad");
                }
                OrderLost?.Invoke(id);
            }
            else
            {
                order = CreateOrder(id, express);
                OrderQueue.Add(order);
                Daily.Orders++;
                Sound("order", 0.02f, -4f);
                if (express)
                {
                    Notify("Express-Bestellung " + order.Number + ": " + GameData.Product(id).Name + " – fällig in " +
                           Fmt.Duration(order.DueWindow) + "!", "info");
                    Explain("express", GameData.MailExpress, "bolt");
                }
                TutorialCheck();
                AdsOnOrder(order);
                OrderReceived?.Invoke(order);
                OrdersChanged?.Invoke();
            }
            RaiseEconomyChanged();
            return order;
        }

        // ---- Erste Bestellung für neue Produkte ------------------------------------------------------
        /// <summary>Plant für ein neu gelistetes, noch nie verkauftes Produkt eine erste Bestellung ein.</summary>
        public void ScheduleLaunchOrder(string id)
        {
            if (LaunchOrders.ContainsKey(id)) return;
            if ((ShippedPerProduct.TryGetValue(id, out int n) ? n : 0) > 0) return;
            if (PendingCountFor(id) > 0) return;
            LaunchOrders[id] = BClock() + Rng.Range(GameData.LaunchOrderMin, GameData.LaunchOrderMax);
        }

        private void UpdateLaunchOrders()
        {
            if (LaunchOrders.Count == 0) return;
            float now = BClock();
            List<string> due = null;
            foreach (var kv in LaunchOrders)
            {
                if (now < kv.Value) continue;
                if (due == null) due = new List<string>();
                due.Add(kv.Key);
            }
            if (due == null) return;
            foreach (var id in due)
            {
                LaunchOrders.Remove(id);
                if (!IsListed(id) || !ProductAvailable(id) || now < ShopOfflineUntil) continue;
                if (PendingCountFor(id) > 0 || (ShippedPerProduct.TryGetValue(id, out int n) ? n : 0) > 0) continue;
                var o = SpawnOrder(id, false);
                if (o != null && TutorialStep < 0)
                    Notify("Neu im Shop: die erste Bestellung für " + GameData.Product(id).Name + " ist da!", "good");
            }
        }

        // =====================================================================================
        // Abfragen für Oberfläche und Welt
        // =====================================================================================
        /// <summary>Alle offenen Bestellzettel (wartend + in Arbeit), sortiert nach Fälligkeit.</summary>
        public List<Order> Tickets()
        {
            var l = new List<Order>(OrderQueue.Count + OrdersInWork.Count);
            l.AddRange(OrderQueue);
            l.AddRange(OrdersInWork);
            l.Sort((a, b) =>
            {
                int c = a.DueAt.CompareTo(b.DueAt);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            });
            return l;
        }

        /// <summary>Bestellzettel mit dieser Nummer (wartend oder in Arbeit) oder null.</summary>
        public Order FindOrder(int orderId)
        {
            if (orderId <= 0) return null;
            foreach (var o in OrderQueue)
                if (o.Id == orderId) return o;
            foreach (var o in OrdersInWork)
                if (o.Id == orderId) return o;
            return null;
        }

        /// <summary>Minuten bis zur Fälligkeit (negativ = überfällig).</summary>
        public float OrderTimeLeft(Order o) => o == null ? 0f : o.DueAt - BClock();

        /// <summary>0 = frisch, 1 = jetzt fällig, &gt;1 = überfällig (max. 3). Für Farben/Balken.</summary>
        public float OrderUrgency(Order o)
        {
            if (o == null) return 0f;
            return Mathx.Clamp((BClock() - o.Created) / Math.Max(1f, o.DueWindow), 0f, 3f);
        }

        public bool OrderOverdue(Order o) => o != null && BClock() > o.DueAt;

        /// <summary>Countdown-Text: "45 min", "1:30 h" oder "überfällig (12 min)".</summary>
        public string OrderTimeLeftText(Order o)
        {
            float left = OrderTimeLeft(o);
            if (left >= 0f) return Fmt.Duration(left);
            return "überfällig (" + Fmt.Duration(-left) + ")";
        }

        /// <summary>Wandelt einen Geschäftsuhr-Zeitpunkt in "11:30" (heute) oder "Di 11:30" um.</summary>
        public string BClockText(float b)
        {
            int day = (int)Math.Floor(b / GameData.BusinessMinutes) + 1;
            float minutes = GameData.DayStart + (b - (day - 1) * GameData.BusinessMinutes);
            string clock = Fmt.Clock(minutes);
            return day == Day ? clock : GameData.WeekdayShort(day) + " " + clock;
        }

        public int ExpressPendingCount()
        {
            int n = 0;
            foreach (var o in OrderQueue)
                if (o.Express) n++;
            return n;
        }

        public int OverdueCount()
        {
            int n = 0;
            float now = BClock();
            foreach (var o in OrderQueue)
                if (now > o.DueAt) n++;
            foreach (var o in OrdersInWork)
                if (now > o.DueAt) n++;
            return n;
        }

        // =====================================================================================
        // Bearbeitungsstand
        // =====================================================================================
        private Order TakeFromWork(int orderId)
        {
            if (orderId <= 0) return null;
            for (int i = 0; i < OrdersInWork.Count; i++)
            {
                if (OrdersInWork[i].Id != orderId) continue;
                var o = OrdersInWork[i];
                OrdersInWork.RemoveAt(i);
                return o;
            }
            return null;
        }

        private void SetOrderStage(int orderId, OrderStage stage)
        {
            if (orderId <= 0) return;
            foreach (var o in OrdersInWork)
            {
                if (o.Id != orderId) continue;
                if (o.Stage != stage)
                {
                    o.Stage = stage;
                    OrdersChanged?.Invoke();
                }
                return;
            }
        }

        /// <summary>
        /// Labeldrucker: Paket wurde etikettiert (Bestellzettel-Stand "Labeled"). Ersetzt den
        /// parameterlosen Aufruf <see cref="OnLabeled()"/> – der bleibt für alte Aufrufer erhalten.
        /// </summary>
        public void OnLabeled(ItemData pkg)
        {
            if (pkg != null) SetOrderStage(pkg.OrderId, OrderStage.Labeled);
            OnLabeled();
        }

        /// <summary>
        /// Ein Gegenstand ist unwiederbringlich verloren (z. B. aus der Welt gefallen). Gehört er zu einem
        /// Bestellzettel, wird der Zettel storniert und zählt als verlorene Bestellung (ohne Bewertung).
        /// Kisten, Retouren und Großauftrags-Artikel verschwinden einfach.
        /// </summary>
        public void DiscardItem(ItemData item)
        {
            if (item == null) return;
            var o = TakeFromWork(item.OrderId);
            if (o != null)
            {
                TotalLostOrders++;
                Daily.Lost++;
                Notify("Bestellung " + o.Number + " (" + GameData.Product(o.Product).Name + ") ist verloren gegangen.", "bad");
                OrderExpired?.Invoke(o);
                OrdersChanged?.Invoke();
            }
            RaiseEconomyChanged();
        }

        // ---- Verwaiste Zettel im laufenden Spiel ------------------------------------------------
        private HashSet<int> _orphanSuspects = new HashSet<int>();

        /// <summary>
        /// Entfernt angefangene Zettel, zu denen es keinen Gegenstand mehr gibt (weder in
        /// <paramref name="aliveIds"/> – Hände und Boden, von der Unity-Schicht gesammelt – noch in
        /// fertigen Paketen oder auf dem Band). Sicherheitsnetz, damit verlorene Zettel nicht ewig
        /// im HUD stehen. Ein Zettel muss bei zwei Aufrufen hintereinander fehlen, bevor er entfernt
        /// wird. Gibt die Anzahl entfernter Zettel zurück.
        /// </summary>
        public int PruneOrdersInWork(ICollection<int> aliveIds)
        {
            if (OrdersInWork.Count == 0)
            {
                _orphanSuspects.Clear();
                return 0;
            }
            var ids = new HashSet<int>();
            if (aliveIds != null)
                foreach (int id in aliveIds) ids.Add(id);
            foreach (var p in PackedPackages)
                if (p != null) ids.Add(p.OrderId);
            foreach (var c in ConveyorQueue)
                if (c != null && c.Pkg != null) ids.Add(c.Pkg.OrderId);
            var suspects = new HashSet<int>();
            int removed = 0;
            for (int i = OrdersInWork.Count - 1; i >= 0; i--)
            {
                var o = OrdersInWork[i];
                if (o == null)
                {
                    OrdersInWork.RemoveAt(i);
                    removed++;
                    continue;
                }
                if (ids.Contains(o.Id)) continue;
                if (_orphanSuspects.Contains(o.Id))
                {
                    OrdersInWork.RemoveAt(i);
                    removed++;
                }
                else suspects.Add(o.Id);
            }
            _orphanSuspects = suspects;
            if (removed > 0) OrdersChanged?.Invoke();
            return removed;
        }

        // ---- Anzeige-Hilfen für Bestellzettel ---------------------------------------------------
        /// <summary>Arbeitsschritt 0 = holen, 1 = packen, 2 = Label, 3 = versenden, 4 = läuft (Band).</summary>
        public static int OrderStepIndex(Order o)
        {
            if (o == null) return 0;
            switch (o.Stage)
            {
                case OrderStage.Picked: return 1;
                case OrderStage.Packed: return 2;
                case OrderStage.Labeled: return 3;
                case OrderStage.Conveyor: return 4;
                default: return 0;
            }
        }

        /// <summary>Nächster Arbeitsschritt als kurzer Text ("Holen", "Packen", "Label", "Versenden").</summary>
        public static string OrderNextStep(Order o)
        {
            switch (OrderStepIndex(o))
            {
                case 1: return "Packen";
                case 2: return "Label drucken";
                case 3: return "Versenden";
                case 4: return "Auf dem Band";
                default: return "Holen";
            }
        }

        /// <summary>Wohin für den nächsten Schritt ("Regal Handyhülle", "Packtisch", ...).</summary>
        public static string OrderNextPlace(Order o)
        {
            switch (OrderStepIndex(o))
            {
                case 1: return "Packtisch";
                case 2: return "Labeldrucker";
                case 3: return "Versand";
                case 4: return "läuft automatisch";
                default:
                    return o != null && GameData.IsProduct(o.Product) ? "Regal " + GameData.Product(o.Product).Name : "Regal";
            }
        }

        /// <summary>Zeitampel: 0 = grün (mehr als die Hälfte der Frist übrig), 1 = gelb, 2 = rot (unter 20 % oder überfällig).</summary>
        public int OrderTimeTone(Order o)
        {
            if (o == null) return 0;
            float left = OrderTimeLeft(o);
            if (left <= 0f) return 2;
            float frac = left / Math.Max(1f, o.DueWindow);
            if (frac < 0.2f) return 2;
            return frac < 0.5f ? 1 : 0;
        }

        /// <summary>Entfernt angefangene Zettel, zu denen kein Gegenstand mehr existiert (nach dem Laden).</summary>
        private void ReconcileOrdersInWork()
        {
            if (OrdersInWork.Count == 0) return;
            var ids = new HashSet<int>();
            foreach (var p in PackedPackages) ids.Add(p.OrderId);
            foreach (var c in ConveyorQueue)
                if (c.Pkg != null) ids.Add(c.Pkg.OrderId);
            foreach (var w in WorldItems)
                if (w.Item != null) ids.Add(w.Item.OrderId);
            if (PlayerState != null && PlayerState.Held != null) ids.Add(PlayerState.Held.OrderId);
            OrdersInWork.RemoveAll(o => !ids.Contains(o.Id));
        }

        /// <summary>Vergibt Nummer/Kundschaft an Zettel aus alten Spielständen.</summary>
        private void FixupLegacyOrders()
        {
            foreach (var o in OrderQueue) NextOrderId = Math.Max(NextOrderId, o.Id + 1);
            foreach (var o in OrdersInWork) NextOrderId = Math.Max(NextOrderId, o.Id + 1);
            foreach (var o in OrderQueue)
            {
                o.Stage = OrderStage.Queued;
                if (o.Id <= 0) o.Id = NextOrderId++;
                if (string.IsNullOrEmpty(o.Customer)) o.Customer = RandomCustomer();
                if (string.IsNullOrEmpty(o.City)) o.City = Rng.Pick(GameData.Cities);
                if (string.IsNullOrEmpty(o.Note)) o.Note = RandomOrderNote(o.Product, o.Express);
            }
            foreach (var o in OrdersInWork)
                if (o.Stage == OrderStage.Queued) o.Stage = OrderStage.Picked;
        }

        // =====================================================================================
        // Verpackung
        // =====================================================================================
        /// <summary>
        /// Karton, der für dieses Produkt benutzt würde: der passende, sonst der nächstgrößere
        /// (zu groß = höheres Retourenrisiko). -1 = kein gefalteter Karton da.
        /// </summary>
        public int CartonFor(string pid)
        {
            int size = GameData.Product(pid).Size;
            for (int s = size; s < 3; s++)
                if (Packaging[s] > 0) return s;
            return -1;
        }

        // =====================================================================================
        // Schnell-Nachbestellen (Handy)
        // =====================================================================================
        /// <summary>{Mengen-Index, Lieferanten-Index} für eine Schnell-Nachbestellung (letzter Einkauf oder 50 Stück Standard).</summary>
        public int[] QuickReorderPlan(string pid)
        {
            int bulk = 1, supplier = 1;
            if (LastPurchase.TryGetValue(pid, out var lp) && lp != null && lp.Length >= 2)
            {
                bulk = Mathx.Clamp(lp[0], 0, GameData.BulkOptions.Length - 1);
                supplier = Mathx.Clamp(lp[1], 0, GameData.Suppliers.Length - 1);
            }
            while (bulk > 0 && !BulkAvailable(bulk)) bulk--;
            if (!SupplierAvailable(supplier)) supplier = SupplierAvailable(1) ? 1 : 0;
            return new[] { bulk, supplier };
        }

        public int QuickReorderCost(string pid)
        {
            var plan = QuickReorderPlan(pid);
            return BulkCost(GameData.ProductIndex(pid), plan[0], plan[1]);
        }

        /// <summary>Bestellt dieselbe Menge beim selben Lieferanten wie beim letzten Mal nach.</summary>
        public bool QuickReorder(string pid)
        {
            if (!GameData.IsProduct(pid)) return false;
            var plan = QuickReorderPlan(pid);
            return BuyBulk(GameData.ProductIndex(pid), plan[0], plan[1]);
        }
    }
}
