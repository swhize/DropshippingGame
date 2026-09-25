using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    public sealed class PrintDesign
    {
        public RGBA Color;
        public int Logo;
    }

    public struct Objective
    {
        public string Title, Text;
        /// <summary>0..1 oder -1 = kein Fortschrittsbalken</summary>
        public float Progress;
    }

    /// <summary>
    /// Zentrale Spiellogik: Wirtschaft, Tagesablauf, Story-Fortschritt, Tutorial, Bewertungen,
    /// Level, Ziele, Personal, Ausbau, Bank, Verkaufsstand und Speichern/Laden.
    /// Die Uhr ist eine "Geschäftsuhr" (<see cref="BClock"/>): nur die offenen Stunden 08-20 Uhr
    /// zählen, die Nacht wird übersprungen. Alle Zeitangaben (Lieferung, Kampagnen) laufen darauf.
    /// Enthält keinerlei Unity-Code - die Unity-Schicht hängt sich an die Ereignisse.
    /// </summary>
    public sealed partial class Sim
    {
        public const int SaveVersion = 3;
        public const int SaveSlots = 3;

        public readonly Rng Rng;
        public readonly Market Market;
        public readonly EventSystem Events;

        // ---- Ereignisse für Oberfläche, Welt und Audio --------------------------------------------
        public event Action EconomyChanged;
        public event Action<string, string> Toast;
        public event Action<bool> PcToggled;
        public event Action<DaySummary> DayEnded;
        public event Action<int> DayStarted;
        public event Action<int> LevelUp;
        public event Action<GoalDef> GoalCompleted;
        public event Action<string> StoryChanged;
        public event Action ObjectiveChanged;
        public event Action<int> LocationChanged;
        public event Action<string> DeliveryIncoming;
        public event Action<string> DeliveryArrived;
        public event Action<ItemData, int, V3> PackageShipped;
        public event Action<string> DialogueRequested;
        public event Action EndDayRequested;
        public event Action<string> GameOver;
        public event Action GameWon;
        public event Action WorldChanged;
        public event Action StaffChanged;
        public event Action ConveyorChanged;
        public event Action<string, int> StandSale;
        /// <summary>(Name, Tonhöhen-Streuung, Lautstärke in dB)</summary>
        public event Action<string, float, float> SoundRequested;

        // ---- Sitzung -------------------------------------------------------------------------------
        public bool InGame;
        public bool PcOpen;
        public string SaveDir = "";
        public int Slot = 1;
        public float RealTime;

        // ---- Spielzustand -------------------------------------------------------------------------
        public int Money;
        public int Day = 1;
        public float TimeMinutes = GameData.DayStart;
        public bool DayOver;
        public string StoryStage = "business";
        public int IntroStep;
        public List<int> IntroServed = new List<int>();
        public int IntroPlateOut;
        private readonly List<int> _assignedPlates = new List<int>();
        public int TutorialStep = -1;
        public HashSet<string> TutFlags = new HashSet<string>();
        public int LocationStage;
        public HashSet<string> Upgrades = new HashSet<string>();
        public HashSet<string> DecorOwned = new HashSet<string>();
        public HashSet<string> LifestyleOwned = new HashSet<string>();
        public float Reputation = 3f;
        public int ReviewCount;
        public List<Review> Reviews = new List<Review>();
        public int Xp;
        public int Level = 1;
        public float Awareness;
        public HashSet<string> GoalsDone = new HashSet<string>();
        public bool BrandNamed;
        public bool EndingSeen;

        public string BrandName = "MeinShop";
        public int BrandLogoIndex;
        public RGBA BrandColor = GameData.BrandPalette[0];

        public List<Delivery> TravelingDeliveries = new List<Delivery>();
        public List<ItemData> DockCrates = new List<ItemData>();
        public Dictionary<string, StockEntry> Stock = new Dictionary<string, StockEntry>();
        public bool ExpressDelivery;
        /// <summary>Lieferanten-Index -> gesperrt bis einschließlich Tag</summary>
        public Dictionary<int, int> BlockedSuppliers = new Dictionary<int, int>();
        public float DamagedNextCrate;

        public List<Order> OrderQueue = new List<Order>();
        public List<ItemData> PackedPackages = new List<ItemData>();
        public int[] Packaging = { 20, 0, 0 };
        public int[] FlatPackaging = { 0, 0, 0 };
        public PrintDesign[] PackagingBrand = new PrintDesign[3];

        public Dictionary<string, bool> Listed = new Dictionary<string, bool>();
        public Dictionary<string, int> ShopPrices = new Dictionary<string, int>();
        public bool PromoActive;
        public float ShopOfflineUntil = -1f;
        public List<Boost> Boosts = new List<Boost>();
        public float TikTokReadyAt;

        public List<StaffMember> Staff = new List<StaffMember>();
        public int SocialPostedDay;
        public List<ConveyorEntry> ConveyorQueue = new List<ConveyorEntry>();
        public List<WorldItemSave> WorldItems = new List<WorldItemSave>();
        public PlayerSave PlayerState;

        public int Debt;
        public Dictionary<string, int> StandStock = new Dictionary<string, int>();
        public int StandSoldTotal;

        public int TotalShipped;
        public int TotalEarned;
        public int TotalLostOrders;
        public Dictionary<string, int> ShippedPerProduct = new Dictionary<string, int>();
        public DailyStats Daily = new DailyStats();
        public List<HistoryEntry> History = new List<HistoryEntry>();
        public long LastSavedUnix;

        private float _orderProgress;
        private float _orderJitter = 1f;
        private float _tutorialOrderAt = -1f;
        private int _conveyorId;
        private float _lastLostToast = -100000f;

        public Sim(int? seed = null)
        {
            Rng = new Rng(seed);
            Market = new Market(this);
            Events = new EventSystem(this);
            ResetState();
        }

        // =====================================================================================
        // Hilfen für Ereignisse
        // =====================================================================================
        public void RaiseEconomyChanged() => EconomyChanged?.Invoke();
        public void Sound(string name, float pitchVar = 0.06f, float volumeDb = 0f) => SoundRequested?.Invoke(name, pitchVar, volumeDb);

        public void Notify(string text, string kind = "info") => Toast?.Invoke(text, kind);

        public void RequestDialogue(string id) => DialogueRequested?.Invoke(id);
        public void RequestEndDay() => EndDayRequested?.Invoke();

        // =====================================================================================
        // Neues Spiel / Reset
        // =====================================================================================
        public void ResetState()
        {
            Money = GameData.StartCapital;
            Day = 1;
            TimeMinutes = GameData.DayStart;
            DayOver = false;
            StoryStage = "business";
            IntroStep = 0;
            IntroServed.Clear();
            IntroPlateOut = 0;
            _assignedPlates.Clear();
            TutorialStep = -1;
            TutFlags.Clear();
            LocationStage = 0;
            Upgrades.Clear();
            DecorOwned.Clear();
            LifestyleOwned.Clear();
            Reputation = 3f;
            ReviewCount = 0;
            Reviews.Clear();
            Xp = 0;
            Level = 1;
            Awareness = 0f;
            GoalsDone.Clear();
            BrandNamed = false;
            EndingSeen = false;
            BrandName = "MeinShop";
            BrandLogoIndex = 0;
            BrandColor = GameData.BrandPalette[0];
            TravelingDeliveries.Clear();
            DockCrates.Clear();
            Stock.Clear();
            ExpressDelivery = false;
            BlockedSuppliers.Clear();
            DamagedNextCrate = 0f;
            OrderQueue.Clear();
            PackedPackages.Clear();
            Packaging = new[] { 20, 0, 0 };
            FlatPackaging = new[] { 0, 0, 0 };
            for (int i = 0; i < PackagingBrand.Length; i++) PackagingBrand[i] = new PrintDesign { Color = BrandColor, Logo = BrandLogoIndex };
            Listed.Clear();
            ShopPrices.Clear();
            PromoActive = false;
            ShopOfflineUntil = -1f;
            Boosts.Clear();
            TikTokReadyAt = 0f;
            Staff.Clear();
            SocialPostedDay = 0;
            ConveyorQueue.Clear();
            WorldItems.Clear();
            PlayerState = null;
            Debt = 0;
            StandStock.Clear();
            StandSoldTotal = 0;
            TotalShipped = 0;
            TotalEarned = 0;
            TotalLostOrders = 0;
            ShippedPerProduct.Clear();
            History.Clear();
            LastSavedUnix = 0;
            _orderProgress = 0f;
            _orderJitter = 1f;
            _tutorialOrderAt = -1f;
            _conveyorId = 0;
            foreach (var p in GameData.Products)
            {
                Stock[p.Id] = new StockEntry { Qty = 0, Quality = 1f };
                Listed[p.Id] = false;
                ShopPrices[p.Id] = Mathx.RoundToInt(p.RefPrice * 0.8f);
                ShippedPerProduct[p.Id] = 0;
                StandStock[p.Id] = 0;
            }
            ResetDaily();
            Market.Reset();
            Events.Reset();
        }

        /// <summary>mode: "intro" (Imbiss-Story + Tutorial), "tutorial" (nur Tutorial), "skip" (alles überspringen)</summary>
        public void NewGame(string mode)
        {
            ResetState();
            switch (mode)
            {
                case "intro":
                    StoryStage = "diner";
                    Money = 0;
                    TimeMinutes = GameData.IntroTime;
                    IntroStep = 0;
                    break;
                case "tutorial":
                    TutorialStep = 0;
                    break;
                default:
                    TutorialStep = -1;
                    Listed["huelle"] = true;
                    break;
            }
            if (mode != "intro")
                Events.AddMail("Mama", "Viel Erfolg, Schatz!", "Ich hab gehört, du machst jetzt was mit Internet. Iss was Ordentliches und vergiss nicht zu schlafen. Hab dich lieb!", "heart");
            RaiseEconomyChanged();
        }

        public void FinishIntro()
        {
            StoryStage = "business";
            Money = GameData.StartCapital;
            Day = 1;
            TimeMinutes = GameData.DayStart;
            IntroStep = 3;
            TutorialStep = 0;
            ResetDaily();
            Events.AddMail("Mama", "Viel Erfolg, Schatz!", "Kalle hat angerufen. Du hast gekündigt?! Naja... ich glaub an dich. Iss was Ordentliches!", "heart");
            StoryChanged?.Invoke(StoryStage);
            ObjectiveChanged?.Invoke();
            RaiseEconomyChanged();
        }

        // =====================================================================================
        // Laptop
        // =====================================================================================
        public void OpenPc()
        {
            if (PcOpen) return;
            PcOpen = true;
            TutFlags.Add("pc_opened");
            TutorialCheck();
            PcToggled?.Invoke(true);
        }

        public void ClosePc()
        {
            if (!PcOpen) return;
            PcOpen = false;
            PcToggled?.Invoke(false);
        }

        // =====================================================================================
        // Zeit
        // =====================================================================================
        public float BClock() => (Day - 1) * GameData.BusinessMinutes + (TimeMinutes - GameData.DayStart);
        public bool IsOpen => StoryStage == "business" && !DayOver;

        /// <summary>Echtzeit-Schritt der Simulation (einmal pro Frame aus der Unity-Schicht).</summary>
        public void Tick(float dt)
        {
            RealTime += dt;
            if (!InGame || !IsOpen) return;
            AdvanceMinutes(dt * GameData.MinutesPerSecond);
            if (!IsOpen) return;
            Market.Process(dt);
            Events.Process();
        }

        public void AdvanceMinutes(float m)
        {
            TimeMinutes += m;
            UpdateDeliveries();
            UpdateOrders(m);
            UpdateBoosts();
            UpdateStaff(m);
            UpdateConveyor();
            UpdateExpiry();
            if (TimeMinutes >= GameData.DayEnd)
            {
                TimeMinutes = GameData.DayEnd;
                EndDay();
            }
        }

        private void ResetDaily()
        {
            Daily = new DailyStats { RepStart = Reputation, XpStart = Xp };
        }

        private void Spend(int amount, string category)
        {
            Money -= amount;
            Daily.Add(category, amount);
        }

        public void AdjustMoney(int amount, string category)
        {
            Money += amount;
            Daily.Add(category, amount);
            RaiseEconomyChanged();
        }

        public int RentPerDay() => GameData.StageRent[LocationStage];

        public int WagesPerDay()
        {
            int w = 0;
            foreach (var s in Staff) w += GameData.StaffRole(s.Role).Wage;
            return w;
        }

        public int UpkeepPerDay() => Upgrades.Contains("conveyor") ? 10 : 0;
        public int InterestPerDay() => Debt > 0 ? Math.Max(1, Mathx.RoundToInt(Debt * GameData.LoanDailyInterest)) : 0;
        public int FixedCostsPerDay() => RentPerDay() + WagesPerDay() + UpkeepPerDay() + InterestPerDay();

        /// <summary>Feierabend vorziehen (Matratze / Stempeluhr).</summary>
        public void EndDayNow()
        {
            if (!IsOpen) return;
            TimeMinutes = GameData.DayEnd;
            EndDay();
        }

        private void EndDay()
        {
            if (DayOver) return;
            DayOver = true;
            Events.ResolvePendingChoices();
            var d = Daily;
            var s = new DaySummary
            {
                Day = Day, Revenue = d.Revenue, IncomeOther = d.IncomeOther, Purchases = d.Purchases, Packaging = d.Packaging,
                Marketing = d.Marketing, Other = d.Other, Shipped = d.Shipped, Lost = d.Lost, Trading = d.Trading, Stand = d.Stand,
                Rent = RentPerDay(), Wages = WagesPerDay(), Upkeep = UpkeepPerDay(), Interest = InterestPerDay(),
                RepStart = d.RepStart, RepEnd = Reputation, XpGained = Xp - d.XpStart, Portfolio = Market.PortfolioValue(), Debt = Debt,
            };
            int costs = s.Purchases + s.Packaging + s.Marketing + s.Other + s.Rent + s.Wages + s.Upkeep + s.Interest;
            s.Profit = s.Revenue + s.IncomeOther - costs;
            s.MoneyAfter = Money - s.Rent - s.Wages - s.Upkeep - s.Interest;
            s.Bankrupt = s.MoneyAfter < GameData.BankruptLimit;
            DayEnded?.Invoke(s);
        }

        /// <summary>Nach der Tagesübersicht: Kosten abbuchen, nächsten Tag starten.</summary>
        public void StartNextDay()
        {
            int rent = RentPerDay(), wages = WagesPerDay(), upkeep = UpkeepPerDay(), interest = InterestPerDay();
            Money -= rent + wages + upkeep + interest;
            int profit = Daily.Revenue + Daily.IncomeOther - Daily.Purchases - Daily.Packaging - Daily.Marketing - Daily.Other
                         - rent - wages - upkeep - interest;
            History.Add(new HistoryEntry { Day = Day, Revenue = Daily.Revenue, Profit = profit, Shipped = Daily.Shipped });
            if (History.Count > 30) History.RemoveAt(0);
            if (Money < GameData.BankruptLimit)
            {
                GameOver?.Invoke("pleite");
                return;
            }
            Day += 1;
            TimeMinutes = GameData.DayStart;
            DayOver = false;
            Awareness *= 0.92f;
            var expired = new List<int>();
            foreach (var kv in BlockedSuppliers)
                if (kv.Value < Day) expired.Add(kv.Key);
            foreach (var k in expired) BlockedSuppliers.Remove(k);
            ResetDaily();
            Market.NewDay();
            Events.NewDay();
            CheckGoals();
            DayStarted?.Invoke(Day);
            RaiseEconomyChanged();
            if (!string.IsNullOrEmpty(SaveDir)) SaveGame(true);
        }

        // =====================================================================================
        // Produkte & Nachfrage
        // =====================================================================================
        public bool ProductUnlocked(string id) => Level >= GameData.Product(id).UnlockLevel;
        public bool ProductHasShelf(string id) => GameData.ProductIndex(id) < GameData.GarageProducts || LocationStage >= 1;
        public bool ProductAvailable(string id) => ProductUnlocked(id) && ProductHasShelf(id);
        public bool IsListed(string id) => Listed.TryGetValue(id, out bool v) && v;
        public int StockQty(string id) => Stock.TryGetValue(id, out var e) ? e.Qty : 0;
        public float StockQuality(string id) => Stock.TryGetValue(id, out var e) ? e.Quality : 1f;

        public int StockTotal()
        {
            int n = 0;
            foreach (var kv in Stock) n += kv.Value.Qty;
            return n;
        }

        public int Capacity() => GameData.StageCapacity[LocationStage] + (Upgrades.Contains("highrack") ? 3000 : 0);
        public int QueueCapacity() => GameData.StageQueue[LocationStage] + (Upgrades.Contains("server") ? 6 : 0);
        public int PendingCount() => OrderQueue.Count;

        public int PendingCountFor(string id)
        {
            int n = 0;
            foreach (var o in OrderQueue)
                if (o.Product == id) n++;
            return n;
        }

        public int TravelingCountFor(string id)
        {
            int n = 0;
            foreach (var d in TravelingDeliveries)
                if (d.Product == id) n++;
            return n;
        }

        public int DockCountFor(string id)
        {
            int n = 0;
            foreach (var c in DockCrates)
                if (c.Product == id) n++;
            return n;
        }

        public int PackedCount() => PackedPackages.Count;
        public int FlatTotal() => FlatPackaging[0] + FlatPackaging[1] + FlatPackaging[2];

        public int CurrentSalePrice(string id)
        {
            int price = ShopPrices.TryGetValue(id, out int p) ? p : 10;
            if (PromoActive) price = Mathx.RoundToInt(price * (1f - GameData.PromoDiscount));
            return Math.Max(1, price);
        }

        public float BoostMult(string id)
        {
            float m = 1f;
            foreach (var b in Boosts)
                if (string.IsNullOrEmpty(b.Product) || b.Product == id) m *= b.Mult;
            return m;
        }

        public Boost ActiveBoost(string source)
        {
            foreach (var b in Boosts)
                if (b.Source == source) return b;
            return null;
        }

        public float DemandRate(string id)
        {
            if (!IsListed(id) || !ProductAvailable(id)) return 0f;
            var p = GameData.Product(id);
            float market = Market.MarketPrice(id);
            float price = CurrentSalePrice(id);
            float priceFactor = Mathx.Clamp((float)Math.Pow(market / Math.Max(price, 1f), 1.6), 0.08f, 2.6f);
            float repMult = Mathx.Lerp(0.35f, 1.6f, Mathx.Clamp01(Reputation / 5f));
            return p.Popularity * priceFactor * repMult * (0.6f + Awareness) * BoostMult(id);
        }

        public string DemandLabel(string id)
        {
            float r = DemandRate(id);
            if (r <= 0f) return "offline";
            if (r < 0.35f) return "sehr niedrig";
            if (r < 0.7f) return "niedrig";
            if (r < 1.2f) return "mittel";
            if (r < 2f) return "hoch";
            return "sehr hoch";
        }

        public float OrderIntervalMinutes()
        {
            float total = 0f;
            foreach (var p in GameData.Products) total += DemandRate(p.Id);
            if (total <= 0f) return 0f;
            float interval = GameData.BaseOrderMinutes / total;
            if (PromoActive) interval *= 0.85f;
            return Math.Max(interval, 2.5f);
        }

        private string PickOrderProduct()
        {
            float total = 0f;
            foreach (var p in GameData.Products) total += DemandRate(p.Id);
            if (total <= 0f) return "";
            float r = Rng.Value() * total;
            foreach (var p in GameData.Products)
            {
                r -= DemandRate(p.Id);
                if (r <= 0f) return p.Id;
            }
            return GameData.Products[0].Id;
        }

        private void UpdateOrders(float m)
        {
            if (_tutorialOrderAt >= 0f && BClock() >= _tutorialOrderAt)
            {
                _tutorialOrderAt = -1f;
                SpawnOrder("huelle");
            }
            if (BClock() < ShopOfflineUntil) return;
            float interval = OrderIntervalMinutes();
            if (interval <= 0f) return;
            _orderProgress += m / (interval * _orderJitter);
            if (_orderProgress >= 1f)
            {
                _orderProgress = 0f;
                _orderJitter = Rng.Range(0.65f, 1.35f);
                string id = PickOrderProduct();
                if (id != "") SpawnOrder(id);
            }
        }

        public void SpawnOrder(string id)
        {
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
            }
            else
            {
                OrderQueue.Add(new Order { Product = id, Price = CurrentSalePrice(id), Created = BClock() });
                Daily.Orders++;
                Sound("order", 0.02f, -4f);
                TutorialCheck();
            }
            RaiseEconomyChanged();
        }

        private void UpdateExpiry()
        {
            float now = BClock();
            bool changed = false;
            for (int i = 0; i < OrderQueue.Count;)
            {
                if (now - OrderQueue[i].Created > GameData.OrderExpireMinutes)
                {
                    var o = OrderQueue[i];
                    OrderQueue.RemoveAt(i);
                    TotalLostOrders++;
                    Daily.Lost++;
                    AddReview(o.Product, 1, "");
                    Notify("Eine Bestellung (" + GameData.Product(o.Product).Name + ") wurde storniert – zu lange gewartet.", "bad");
                    changed = true;
                }
                else i++;
            }
            if (changed) RaiseEconomyChanged();
        }

        /// <summary>Für Tests: Ablauf-Prüfung direkt auslösen.</summary>
        public void CheckExpiryNow() => UpdateExpiry();

        // =====================================================================================
        // Einkauf & Lieferungen
        // =====================================================================================
        public bool SupplierAvailable(int index) => Level >= GameData.Suppliers[index].Level && !BlockedSuppliers.ContainsKey(index);

        public bool BulkAvailable(int bulkIndex)
        {
            var b = GameData.BulkOptions[bulkIndex];
            return Level >= b.Level && LocationStage >= b.Stage;
        }

        public int BulkCost(int productIndex, int bulkIndex, int supplierIndex)
        {
            var p = GameData.Products[productIndex];
            var b = GameData.BulkOptions[bulkIndex];
            var s = GameData.Suppliers[supplierIndex];
            int cost = Mathx.RoundToInt(p.UnitCost * b.Quantity * b.Discount * s.PriceMult);
            if (ExpressDelivery) cost += GameData.ExpressSurcharge;
            return cost;
        }

        public float LeadMinutes(int supplierIndex)
        {
            float t = GameData.BaseLeadMinutes * GameData.Suppliers[supplierIndex].LeadMult;
            if (ExpressDelivery) t *= 0.5f;
            if (Upgrades.Contains("van")) t *= 0.75f;
            return Math.Max(t, 8f);
        }

        public bool BuyBulk(int productIndex, int bulkIndex, int supplierIndex)
        {
            var p = GameData.Products[productIndex];
            if (!ProductUnlocked(p.Id))
            {
                Notify("Dieses Produkt ist noch gesperrt.", "bad");
                return false;
            }
            if (!ProductHasShelf(p.Id))
            {
                Notify("Dafür brauchst du erst die Lagerhalle (kein Regal in der Garage).", "bad");
                return false;
            }
            if (!SupplierAvailable(supplierIndex))
            {
                Notify("Dieser Anbieter ist gerade nicht verfügbar.", "bad");
                return false;
            }
            if (!BulkAvailable(bulkIndex))
            {
                Notify("Diese Bestellmenge ist noch nicht freigeschaltet.", "bad");
                return false;
            }
            int cost = BulkCost(productIndex, bulkIndex, supplierIndex);
            if (Money < cost)
            {
                Notify("Nicht genug Geld für diese Bestellung!", "bad");
                Sound("error");
                return false;
            }
            var b = GameData.BulkOptions[bulkIndex];
            var s = GameData.Suppliers[supplierIndex];
            Spend(cost, "purchases");
            TravelingDeliveries.Add(new Delivery
            {
                Product = p.Id, Quantity = b.Quantity, Quality = s.Quality, ArriveAt = BClock() + LeadMinutes(supplierIndex),
            });
            Notify(b.Quantity + "× " + p.Name + " bestellt bei " + s.Name + ".", "good");
            Sound("cash", 0.02f, -8f);
            TutorialCheck();
            RaiseEconomyChanged();
            return true;
        }

        private void UpdateDeliveries()
        {
            float now = BClock();
            bool changed = false;
            for (int i = 0; i < TravelingDeliveries.Count;)
            {
                var d = TravelingDeliveries[i];
                if (!d.VanSent && now >= d.ArriveAt - 3f)
                {
                    d.VanSent = true;
                    DeliveryIncoming?.Invoke(d.Product);
                }
                if (now >= d.ArriveAt)
                {
                    int qty = d.Quantity;
                    if (DamagedNextCrate > 0f)
                    {
                        int lost = Mathx.RoundToInt(qty * DamagedNextCrate);
                        qty -= lost;
                        DamagedNextCrate = 0f;
                        Notify("Beschädigte Lieferung: " + lost + " Stück unbrauchbar.", "bad");
                    }
                    DockCrates.Add(ItemData.Crate(d.Product, qty, d.Quality));
                    TravelingDeliveries.RemoveAt(i);
                    DeliveryArrived?.Invoke(d.Product);
                    Notify("Lieferung angekommen: " + qty + "× " + GameData.Product(d.Product).Name + " am Wareneingang.", "info");
                    changed = true;
                }
                else i++;
            }
            if (changed) RaiseEconomyChanged();
        }

        public string EtaText(Delivery d) => (int)Math.Ceiling(Math.Max(0f, d.ArriveAt - BClock())) + " min";

        public ItemData PickupCrate()
        {
            if (DockCrates.Count == 0)
            {
                Notify("Nichts am Wareneingang abzuholen.", "info");
                return null;
            }
            var c = DockCrates[0];
            DockCrates.RemoveAt(0);
            TutFlags.Add("crate_picked");
            TutorialCheck();
            RaiseEconomyChanged();
            return c;
        }

        public void ReturnCrate(ItemData crate)
        {
            DockCrates.Insert(0, crate);
            RaiseEconomyChanged();
        }

        private void AddToStock(string id, int quantity, float quality)
        {
            var entry = Stock[id];
            int before = entry.Qty;
            entry.Quality = (entry.Quality * before + quality * quantity) / Math.Max(1, before + quantity);
            entry.Qty = before + quantity;
        }

        public bool UnboxCrate(string id, int quantity, float quality)
        {
            if (StockTotal() + quantity > Capacity())
            {
                Notify("Lager voll! (" + StockTotal() + "/" + Capacity() + ") – verkaufe erst etwas oder baue aus.", "bad");
                Sound("error");
                return false;
            }
            AddToStock(id, quantity, quality);
            TutorialCheck();
            RaiseEconomyChanged();
            return true;
        }

        public ItemData PickItem(string id)
        {
            int idx = -1;
            for (int i = 0; i < OrderQueue.Count; i++)
            {
                if (OrderQueue[i].Product == id)
                {
                    idx = i;
                    break;
                }
            }
            if (idx < 0)
            {
                Notify("Keine offene Bestellung für " + GameData.Product(id).Name + ".", "info");
                return null;
            }
            if (StockQty(id) <= 0)
            {
                Notify("Kein " + GameData.Product(id).Name + " mehr auf Lager! Im Laptop nachbestellen.", "bad");
                return null;
            }
            var order = OrderQueue[idx];
            OrderQueue.RemoveAt(idx);
            Stock[id].Qty -= 1;
            TutFlags.Add("item_picked");
            TutorialCheck();
            RaiseEconomyChanged();
            return new ItemData { Kind = ItemKind.Item, Product = id, Price = order.Price, Quality = StockQuality(id), Created = order.Created };
        }

        public void ReturnItem(ItemData item)
        {
            Stock[item.Product].Qty += 1;
            OrderQueue.Insert(0, new Order { Product = item.Product, Price = item.Price, Created = item.Created });
            RaiseEconomyChanged();
        }

        // =====================================================================================
        // Verpacken, Etikettieren, Versand
        // =====================================================================================
        public bool WrapItem(ItemData item)
        {
            int size = GameData.Product(item.Product).Size;
            if (Packaging[size] <= 0)
            {
                if (FlatPackaging[size] > 0)
                    Notify("Kein gefalteter Karton " + GameData.SizeName(size) + " – erst am Falttisch falten!", "bad");
                else
                    Notify("Keine Kartons in Größe " + GameData.SizeName(size) + "! Im Laptop unter 'Verpackung' kaufen.", "bad");
                Sound("error");
                return false;
            }
            Packaging[size] -= 1;
            var brand = PackagingBrand[size];
            PackedPackages.Add(new ItemData
            {
                Kind = ItemKind.Package, Product = item.Product, Price = item.Price, Quality = item.Quality, Created = item.Created,
                Color = brand.Color, Logo = brand.Logo,
            });
            TutFlags.Add("wrapped");
            TutorialCheck();
            RaiseEconomyChanged();
            return true;
        }

        public ItemData PickupPackage()
        {
            if (PackedPackages.Count == 0)
            {
                Notify("Hier liegt kein fertiges Paket.", "info");
                return null;
            }
            var pkg = PackedPackages[PackedPackages.Count - 1];
            PackedPackages.RemoveAt(PackedPackages.Count - 1);
            RaiseEconomyChanged();
            return pkg;
        }

        public void ReturnPackage(ItemData pkg)
        {
            pkg.Kind = ItemKind.Package;
            PackedPackages.Add(pkg);
            RaiseEconomyChanged();
        }

        public void OnLabeled()
        {
            TutFlags.Add("labeled");
            TutorialCheck();
        }

        public int FoldCarton()
        {
            int best = -1;
            for (int size = 0; size < 3; size++)
            {
                if (FlatPackaging[size] <= 0) continue;
                if (best < 0 || Packaging[size] < Packaging[best]) best = size;
            }
            if (best < 0)
            {
                Notify("Keine ungefalteten Kartons da.", "info");
                return -1;
            }
            FlatPackaging[best] -= 1;
            Packaging[best] += 1;
            Sound("fold");
            RaiseEconomyChanged();
            return best;
        }

        /// <summary>Versand: der Kunde zahlt den Preis vom Bestellzeitpunkt. Qualität + Tempo -> Bewertung.</summary>
        public int ShipPackage(ItemData pkg, V3 where = default)
        {
            int reward = pkg.Price;
            Money += reward;
            Daily.Revenue += reward;
            Daily.Shipped++;
            TotalShipped++;
            TotalEarned += reward;
            string id = pkg.Product;
            ShippedPerProduct[id] = (ShippedPerProduct.TryGetValue(id, out int n) ? n : 0) + 1;
            AddXp(Math.Max(1, reward / 2));
            float q = pkg.Quality;
            float age = BClock() - pkg.Created;
            if (Rng.Value() < GameData.ReviewChance)
            {
                int stars = 4;
                if (age < 90f) stars += 1;
                else if (age > 360f) stars -= 2;
                else if (age > 200f) stars -= 1;
                if (q < 0.8f) stars -= 1;
                else if (q >= 1.3f) stars += 1;
                if (Rng.Value() < 0.25f) stars += Rng.RangeInt(-1, 1);
                AddReview(id, Mathx.Clamp(stars, 1, 5), "");
            }
            if (q < 0.8f && Rng.Value() < 0.08f)
            {
                Money -= reward;
                Daily.Other += reward;
                Notify("Rücksendung! Ein Kunde fand die Billig-Qualität unzumutbar (−" + Fmt.Money(reward) + ").", "bad");
            }
            Sound("cash");
            PackageShipped?.Invoke(pkg, reward, where);
            TutorialCheck();
            CheckGoals();
            RaiseEconomyChanged();
            return reward;
        }

        public bool ConveyorInsert(ItemData pkg)
        {
            if (!Upgrades.Contains("conveyor")) return false;
            _conveyorId++;
            ConveyorQueue.Add(new ConveyorEntry { Id = _conveyorId, Pkg = pkg, DoneAt = BClock() + 6f });
            ConveyorChanged?.Invoke();
            return true;
        }

        private void UpdateConveyor()
        {
            if (ConveyorQueue.Count == 0) return;
            float now = BClock();
            for (int i = 0; i < ConveyorQueue.Count;)
            {
                if (now >= ConveyorQueue[i].DoneAt)
                {
                    var entry = ConveyorQueue[i];
                    ConveyorQueue.RemoveAt(i);
                    ShipPackage(entry.Pkg, new V3(5.8f, 2f, -13.5f));
                    ConveyorChanged?.Invoke();
                }
                else i++;
            }
        }

        // =====================================================================================
        // Bewertungen, XP, Ziele
        // =====================================================================================
        private void AddReview(string id, int stars, string custom)
        {
            var texts = GameData.ReviewTexts[stars];
            string text = custom != "" ? custom : texts[Rng.Index(texts.Length)];
            Reviews.Insert(0, new Review { Stars = stars, Text = text, Name = Rng.Pick(GameData.ReviewNames), Product = id, Day = Day });
            if (Reviews.Count > 25) Reviews.RemoveAt(Reviews.Count - 1);
            ReviewCount++;
            float weight = ReviewCount < 10 ? 0.12f : 0.05f;
            Reputation = Mathx.Clamp(Reputation + (stars - Reputation) * weight, 0.5f, 5f);
            CheckGoals();
        }

        public void ChangeReputation(float delta)
        {
            Reputation = Mathx.Clamp(Reputation + delta, 0.5f, 5f);
            CheckGoals();
            RaiseEconomyChanged();
        }

        public void AddAwareness(float v) => Awareness = Mathx.Clamp(Awareness + v, 0f, 1.5f);

        public void AddXp(int n)
        {
            Xp += n;
            while (Level < GameData.MaxLevel && Xp >= GameData.LevelThreshold(Level + 1))
            {
                Level++;
                LevelUp?.Invoke(Level);
                Sound("levelup");
                GameData.LevelUnlocks.TryGetValue(Level, out string unlock);
                Notify("Firmenlevel " + Level + "! Neu: " + unlock, "good");
                CheckGoals();
            }
        }

        public float LevelProgress()
        {
            if (Level >= GameData.MaxLevel) return 1f;
            int a = GameData.LevelThreshold(Level);
            int b = GameData.LevelThreshold(Level + 1);
            return Mathx.Clamp01((float)(Xp - a) / Math.Max(1, b - a));
        }

        public float GoalValue(GoalDef g)
        {
            switch (g.Type)
            {
                case "shipped": return TotalShipped;
                case "revenue": return TotalEarned;
                case "rating": return ReviewCount >= 5 ? Reputation : 0f;
                case "stage": return LocationStage;
                case "staff": return Staff.Count;
                case "brand": return BrandNamed ? 1f : 0f;
                case "level": return Level;
                case "stand": return StandSoldTotal;
            }
            return 0f;
        }

        public void CheckGoals()
        {
            if (StoryStage != "business") return;
            foreach (var g in GameData.Goals)
            {
                if (GoalsDone.Contains(g.Id)) continue;
                if (GoalValue(g) >= g.Target)
                {
                    GoalsDone.Add(g.Id);
                    Money += g.Reward;
                    Daily.IncomeOther += g.Reward;
                    GoalCompleted?.Invoke(g);
                    Notify("Ziel erreicht: " + g.Title + " (+" + Fmt.Money(g.Reward) + ")", "good");
                    Sound("levelup", 0f, -3f);
                    if (g.Final && !EndingSeen)
                    {
                        EndingSeen = true;
                        GameWon?.Invoke();
                    }
                    ObjectiveChanged?.Invoke();
                }
            }
        }

        public GoalDef NextGoal()
        {
            foreach (var g in GameData.Goals)
                if (!GoalsDone.Contains(g.Id)) return g;
            return null;
        }

        // =====================================================================================
        // Tutorial & Ziele-Anzeige
        // =====================================================================================
        private void TutorialCheck()
        {
            if (TutorialStep < 0) return;
            bool advanced = false;
            while (TutorialStep >= 0 && TutorialStep < GameData.Tutorial.Length && TutorialDone(TutorialStep))
            {
                TutorialStep++;
                advanced = true;
                if (TutorialStep == 5 && OrderQueue.Count == 0) _tutorialOrderAt = BClock() + 4f;
            }
            if (TutorialStep >= GameData.Tutorial.Length)
            {
                TutorialStep = -1;
                Notify("Tutorial geschafft! Ab jetzt läuft dein Shop. Ziele findest du im Laptop.", "good");
                Sound("levelup");
            }
            if (advanced) ObjectiveChanged?.Invoke();
        }

        private bool TutorialDone(int step)
        {
            switch (step)
            {
                case 0: return TutFlags.Contains("pc_opened");
                case 1: return TravelingDeliveries.Count > 0 || DockCrates.Count > 0 || StockTotal() > 0 || TutFlags.Contains("crate_picked");
                case 2: return TutFlags.Contains("crate_picked") || StockTotal() > 0;
                case 3: return StockTotal() > 0 || TotalShipped > 0;
                case 4: return IsListed("huelle");
                case 5: return OrderQueue.Count > 0 || TutFlags.Contains("item_picked") || TotalShipped > 0;
                case 6: return TutFlags.Contains("item_picked") || TotalShipped > 0;
                case 7: return TutFlags.Contains("wrapped") || TotalShipped > 0;
                case 8: return TutFlags.Contains("labeled") || TotalShipped > 0;
                case 9: return TotalShipped > 0;
            }
            return true;
        }

        public Objective CurrentObjective()
        {
            if (StoryStage == "diner")
            {
                switch (IntroStep)
                {
                    case 0: return new Objective { Title = "Schicht im Imbiss", Text = "Sprich mit Kalle an der Theke.", Progress = -1f };
                    case 1:
                        return new Objective
                        {
                            Title = "Teller austragen (" + IntroServed.Count + "/3)",
                            Text = "Hol an der Durchreiche einen Teller und bring ihn an den richtigen Tisch.",
                            Progress = IntroServed.Count / 3f,
                        };
                    default: return new Objective { Title = "Feierabend?", Text = "Sprich mit Kalle. Es ist Zeit für eine Entscheidung.", Progress = -1f };
                }
            }
            if (TutorialStep >= 0 && TutorialStep < GameData.Tutorial.Length)
            {
                var t = GameData.Tutorial[TutorialStep];
                return new Objective
                {
                    Title = "Tutorial " + (TutorialStep + 1) + "/" + GameData.Tutorial.Length + ": " + t.Title,
                    Text = t.Text,
                    Progress = (float)TutorialStep / GameData.Tutorial.Length,
                };
            }
            var g = NextGoal();
            if (g == null)
                return new Objective { Title = "Imperium aufgebaut", Text = "Alle Ziele erreicht. Genieß den Ruhm – oder mach einfach weiter.", Progress = 1f };
            return new Objective { Title = "Ziel: " + g.Title, Text = g.Desc, Progress = Mathx.Clamp01(GoalValue(g) / Math.Max(g.Target, 0.001f)) };
        }

        // =====================================================================================
        // Imbiss-Intro
        // =====================================================================================
        public static readonly int[] IntroTables = { 2, 3, 4 };

        public void StartShift()
        {
            IntroStep = 1;
            ObjectiveChanged?.Invoke();
            RaiseEconomyChanged();
        }

        public int DinerPlatesWaiting()
        {
            if (StoryStage != "diner" || IntroStep != 1) return 0;
            return Math.Max(0, IntroTables.Length - IntroServed.Count - IntroPlateOut);
        }

        /// <summary>0 = leer, 1 = wartet auf Essen, 2 = bedient</summary>
        public int DinerTableState(int tableId)
        {
            if (StoryStage != "diner") return 0;
            if (IntroServed.Contains(tableId)) return 2;
            if (Array.IndexOf(IntroTables, tableId) >= 0 && IntroStep >= 1) return 1;
            return 0;
        }

        public ItemData DinerTakePlate()
        {
            if (DinerPlatesWaiting() <= 0) return null;
            foreach (int t in IntroTables)
            {
                if (!IntroServed.Contains(t) && !_assignedPlates.Contains(t))
                {
                    IntroPlateOut++;
                    _assignedPlates.Add(t);
                    RaiseEconomyChanged();
                    ObjectiveChanged?.Invoke();
                    return new ItemData { Kind = ItemKind.Plate, Table = t };
                }
            }
            return null;
        }

        public bool DinerServe(int tableId, ItemData plate)
        {
            if (plate == null || plate.Table != tableId) return false;
            IntroServed.Add(tableId);
            IntroPlateOut = Math.Max(0, IntroPlateOut - 1);
            if (IntroServed.Count >= IntroTables.Length)
            {
                IntroStep = 2;
                Notify("Neues Video: 'Mit 19 Millionär – dank Dropshipping!' ... interessant.", "info");
            }
            ObjectiveChanged?.Invoke();
            RaiseEconomyChanged();
            return true;
        }

        // =====================================================================================
        // Branding, Webshop, Verpackung
        // =====================================================================================
        public void SetBrandName(string newName)
        {
            string n = (newName ?? "").Trim();
            if (n == "" || n == BrandName) return;
            BrandName = n.Length > 22 ? n.Substring(0, 22) : n;
            BrandNamed = true;
            CheckGoals();
            WorldChanged?.Invoke();
            RaiseEconomyChanged();
        }

        public void SetBrandLogo(int index)
        {
            BrandLogoIndex = Mathx.Clamp(index, 0, GameData.LogoNames.Length - 1);
            RaiseEconomyChanged();
        }

        public void SetBrandColor(RGBA color)
        {
            BrandColor = color;
            WorldChanged?.Invoke();
            RaiseEconomyChanged();
        }

        public void SetListed(string id, bool active)
        {
            if (active && !ProductAvailable(id))
            {
                Notify("Dieses Produkt ist noch nicht verfügbar.", "bad");
                return;
            }
            Listed[id] = active;
            TutorialCheck();
            RaiseEconomyChanged();
        }

        public void SetShopPrice(string id, int price)
        {
            ShopPrices[id] = Mathx.Clamp(price, 1, 999);
            RaiseEconomyChanged();
        }

        public void SetPromoActive(bool active)
        {
            PromoActive = active;
            RaiseEconomyChanged();
        }

        public int PackagingCost(int size, bool flat, int batch = 0)
        {
            float cost = GameData.PackagingSizes[size].Cost * GameData.PackagingBatches[batch].Mult;
            if (flat) cost *= GameData.PackagingFlatDiscount;
            return Mathx.RoundToInt(cost);
        }

        public bool BuyPackaging(int size, bool flat = false, int batch = 0)
        {
            int cost = PackagingCost(size, flat, batch);
            if (Money < cost)
            {
                Notify("Nicht genug Geld für Verpackungsmaterial!", "bad");
                Sound("error");
                return false;
            }
            Spend(cost, "packaging");
            int qty = GameData.PackagingBatches[batch].Qty;
            if (flat) FlatPackaging[size] += qty;
            else Packaging[size] += qty;
            PackagingBrand[size] = new PrintDesign { Color = BrandColor, Logo = BrandLogoIndex };
            Notify(qty + " Kartons " + GameData.SizeName(size) + (flat ? " (ungefaltet)" : "") + " gekauft – mit deinem Logo bedruckt.", "good");
            RaiseEconomyChanged();
            return true;
        }

        public void SetExpressDelivery(bool enabled)
        {
            ExpressDelivery = enabled;
            RaiseEconomyChanged();
        }

        // =====================================================================================
        // Marketing
        // =====================================================================================
        public bool StartAdCampaign(int tierIndex)
        {
            var t = GameData.AdTiers[tierIndex];
            if (Level < t.Level)
            {
                Notify("Diese Werbeform ist erst ab Level " + t.Level + " verfügbar.", "bad");
                return false;
            }
            if (ActiveBoost("ad") != null)
            {
                Notify("Es läuft schon eine Werbekampagne.", "info");
                return false;
            }
            if (Money < t.Cost)
            {
                Notify("Nicht genug Geld für diese Kampagne!", "bad");
                Sound("error");
                return false;
            }
            Spend(t.Cost, "marketing");
            Boosts.Add(new Boost { Source = "ad", Name = t.Name, Mult = t.Mult, EndsAt = BClock() + t.Minutes });
            AddAwareness(t.Awareness);
            Notify(t.Name + " gestartet! Mehr Bestellungen für " + (int)t.Minutes + " Minuten.", "good");
            RaiseEconomyChanged();
            return true;
        }

        public bool TikTokAvailable() => Level >= GameData.TikTokLevel && BClock() >= TikTokReadyAt && ActiveBoost("tiktok") == null;

        public void TriggerTikTok(float score, bool silent = false)
        {
            float s = Mathx.Clamp(score, 0.05f, 1f);
            float mult = Mathx.Lerp(GameData.TikTokMinMult, GameData.TikTokMaxMult, s);
            float minutes = Mathx.Lerp(GameData.TikTokMinMinutes, GameData.TikTokMaxMinutes, s);
            Boosts.RemoveAll(b => b.Source == "tiktok");
            Boosts.Add(new Boost { Source = "tiktok", Name = "TikTok-Trend", Mult = mult, EndsAt = BClock() + minutes });
            TikTokReadyAt = BClock() + GameData.TikTokCooldown;
            AddAwareness(0.05f * s);
            if (!silent)
                Notify("TikTok gepostet! " + (int)(s * 100) + " % Treffer → ×" + Fmt.Dec(mult, 1) + " Nachfrage für " + (int)minutes + " min.", "good");
            RaiseEconomyChanged();
        }

        public void AddBoost(string source, string name, float mult, float minutes, string product = "")
        {
            Boosts.Add(new Boost { Source = source, Name = name, Mult = mult, EndsAt = BClock() + minutes, Product = product ?? "" });
            RaiseEconomyChanged();
        }

        private void UpdateBoosts()
        {
            float now = BClock();
            bool changed = false;
            for (int i = 0; i < Boosts.Count;)
            {
                if (now >= Boosts[i].EndsAt)
                {
                    var b = Boosts[i];
                    Boosts.RemoveAt(i);
                    Notify(b.Name + " ist ausgelaufen.", "info");
                    changed = true;
                }
                else i++;
            }
            if (changed) RaiseEconomyChanged();
        }

        // =====================================================================================
        // Personal
        // =====================================================================================
        public bool StaffAllowed() => LocationStage >= 1 && Level >= GameData.StaffLevel;

        public int StaffCount(string roleId)
        {
            int n = 0;
            foreach (var s in Staff)
                if (s.Role == roleId) n++;
            return n;
        }

        public bool Hire(string roleId)
        {
            var role = GameData.StaffRole(roleId);
            if (!StaffAllowed())
            {
                Notify("Personal gibt es erst mit Lagerhalle und Level " + GameData.StaffLevel + ".", "bad");
                return false;
            }
            if (StaffCount(roleId) >= role.Max)
            {
                Notify("Mehr " + role.Name + " brauchst du nicht.", "info");
                return false;
            }
            var used = new HashSet<string>();
            foreach (var s in Staff) used.Add(s.Name);
            var free = new List<string>();
            foreach (var n in GameData.StaffNames)
                if (!used.Contains(n)) free.Add(n);
            string pname = free.Count > 0 ? free[Rng.Index(free.Count)] : "Aushilfe";
            Staff.Add(new StaffMember { Role = roleId, Name = pname, Progress = 0f });
            Notify(pname + " fängt als " + role.Name + " an (" + Fmt.Money(role.Wage) + "/Tag).", "good");
            StaffChanged?.Invoke();
            CheckGoals();
            RaiseEconomyChanged();
            return true;
        }

        public void Fire(int index)
        {
            if (index < 0 || index >= Staff.Count) return;
            var s = Staff[index];
            Staff.RemoveAt(index);
            Notify(s.Name + " wurde entlassen.", "info");
            StaffChanged?.Invoke();
            RaiseEconomyChanged();
        }

        /// <summary>Für Tests und Balancing: Personal eine Zeitspanne arbeiten lassen.</summary>
        public void UpdateStaff(float m)
        {
            if (Staff.Count == 0) return;
            foreach (var s in Staff.ToArray())
            {
                var role = GameData.StaffRole(s.Role);
                if (s.Role == "social")
                {
                    if (SocialPostedDay != Day && TimeMinutes >= 600f)
                    {
                        SocialPostedDay = Day;
                        float score = Rng.Value() > 0.2f ? Rng.Range(0.25f, 0.85f) : 0.08f;
                        TriggerTikTok(score, true);
                        Notify(s.Name + " hat ein TikTok gepostet (" + (int)(score * 100) + " % Treffer).", score > 0.2f ? "info" : "bad");
                    }
                    continue;
                }
                s.Progress += m / role.Interval;
                if (s.Progress >= 1f) s.Progress = StaffWork(s.Role) ? 0f : 1f;
            }
        }

        private bool StaffWork(string roleId)
        {
            switch (roleId)
            {
                case "lager":
                    for (int i = 0; i < DockCrates.Count; i++)
                    {
                        var c = DockCrates[i];
                        if (StockTotal() + c.Quantity <= Capacity())
                        {
                            DockCrates.RemoveAt(i);
                            AddToStock(c.Product, c.Quantity, c.Quality);
                            RaiseEconomyChanged();
                            return true;
                        }
                    }
                    return false;
                case "packer":
                    for (int i = 0; i < OrderQueue.Count; i++)
                    {
                        var o = OrderQueue[i];
                        string id = o.Product;
                        int size = GameData.Product(id).Size;
                        if (StockQty(id) <= 0) continue;
                        if (Packaging[size] <= 0 && FlatPackaging[size] > 0)
                        {
                            FlatPackaging[size] -= 1;
                            Packaging[size] += 1;
                        }
                        if (Packaging[size] <= 0) continue;
                        OrderQueue.RemoveAt(i);
                        Stock[id].Qty -= 1;
                        Packaging[size] -= 1;
                        var brand = PackagingBrand[size];
                        PackedPackages.Insert(0, new ItemData
                        {
                            Kind = ItemKind.Package, Product = id, Price = o.Price, Quality = StockQuality(id), Created = o.Created,
                            Color = brand.Color, Logo = brand.Logo,
                        });
                        RaiseEconomyChanged();
                        return true;
                    }
                    return false;
                case "versand":
                {
                    if (PackedPackages.Count == 0) return false;
                    var pkg = PackedPackages[0];
                    PackedPackages.RemoveAt(0);
                    ShipPackage(pkg, new V3(5.8f, 2f, -13.5f));
                    return true;
                }
            }
            return false;
        }

        // =====================================================================================
        // Ausbau, Einrichtung, Lifestyle
        // =====================================================================================
        public bool HasUpgrade(string id) => Upgrades.Contains(id);

        /// <summary>"owned", "level", "requires" oder "available"</summary>
        public string UpgradeState(string id)
        {
            var u = GameData.Upgrade(id);
            if (Upgrades.Contains(id)) return "owned";
            if (Level < u.Level) return "level";
            if (!string.IsNullOrEmpty(u.Requires) && !Upgrades.Contains(u.Requires)) return "requires";
            return "available";
        }

        public bool BuyUpgrade(string id)
        {
            var u = GameData.Upgrade(id);
            if (u == null || UpgradeState(id) != "available") return false;
            if (Money < u.Cost)
            {
                Notify("Nicht genug Geld!", "bad");
                Sound("error");
                return false;
            }
            Spend(u.Cost, "other");
            Upgrades.Add(id);
            Notify(u.Name + " gekauft!", "good");
            Sound("levelup");
            if (id == "warehouse")
            {
                LocationStage = 1;
                LocationChanged?.Invoke(1);
            }
            WorldChanged?.Invoke();
            CheckGoals();
            RaiseEconomyChanged();
            return true;
        }

        public bool BuyDecor(string id)
        {
            var d = GameData.FindShopItem(GameData.Decor, id);
            if (d == null || DecorOwned.Contains(id)) return false;
            if (Money < d.Cost)
            {
                Notify("Nicht genug Geld!", "bad");
                Sound("error");
                return false;
            }
            Spend(d.Cost, "other");
            DecorOwned.Add(id);
            Notify(d.Name + " aufgestellt.", "good");
            WorldChanged?.Invoke();
            RaiseEconomyChanged();
            return true;
        }

        public bool BuyLifestyle(string id)
        {
            var d = GameData.FindShopItem(GameData.Lifestyle, id);
            if (d == null || LifestyleOwned.Contains(id)) return false;
            if (Money < d.Cost)
            {
                Notify("Nicht genug Geld!", "bad");
                Sound("error");
                return false;
            }
            Spend(d.Cost, "other");
            LifestyleOwned.Add(id);
            Notify("Gönn dir: " + d.Name + "!", "good");
            Sound("levelup");
            WorldChanged?.Invoke();
            RaiseEconomyChanged();
            return true;
        }

        // =====================================================================================
        // Bank
        // =====================================================================================
        public int CreditLimit() => GameData.CreditLimit(Level);
        public int CreditAvailable() => Math.Max(0, CreditLimit() - Debt);

        public bool LoanAvailable(int optionIndex)
        {
            var o = GameData.Loans[optionIndex];
            return Level >= o.Level && o.Amount <= CreditAvailable();
        }

        public bool TakeLoan(int amount)
        {
            if (amount <= 0) return false;
            if (amount > CreditAvailable())
            {
                Notify("So viel Kredit gibt dir die Bank nicht. Dein Rahmen: " + Fmt.Money(CreditLimit()) + ".", "bad");
                Sound("error");
                return false;
            }
            Debt += amount;
            Money += amount;
            Notify("Kredit über " + Fmt.Money(amount) + " ausgezahlt. Zinsen: " + Fmt.Dec(GameData.LoanDailyInterest * 100f, 0) + " % pro Tag.", "info");
            Sound("cash", 0.02f, -6f);
            RaiseEconomyChanged();
            return true;
        }

        public int RepayLoan(int amount)
        {
            int pay = Math.Min(amount, Math.Min(Debt, Money));
            if (pay <= 0)
            {
                if (Debt <= 0) Notify("Du hast keine Schulden.", "info");
                else Notify("Nicht genug Geld zum Tilgen.", "bad");
                return 0;
            }
            Debt -= pay;
            Money -= pay;
            Notify(Debt == 0 ? "Kredit komplett getilgt. Schuldenfrei!" : Fmt.Money(pay) + " getilgt. Restschuld: " + Fmt.Money(Debt) + ".", "good");
            Sound("click");
            RaiseEconomyChanged();
            return pay;
        }

        // =====================================================================================
        // Verkaufsstand
        // =====================================================================================
        public int StandTotal()
        {
            int n = 0;
            foreach (var kv in StandStock) n += kv.Value;
            return n;
        }

        public bool StandOpen => HasUpgrade("stand") && IsOpen && StandTotal() > 0;

        /// <summary>Eine Kiste auf den Stand stellen. Passt nicht alles drauf, bleibt der Rest in der Kiste.</summary>
        public int StandAddCrate(ItemData crate)
        {
            if (crate == null || crate.Kind != ItemKind.Crate) return 0;
            if (!HasUpgrade("stand"))
            {
                Notify("Den Verkaufsstand musst du erst in der Ausbau-App kaufen.", "info");
                return 0;
            }
            int space = GameData.StandCapacity - StandTotal();
            if (space <= 0)
            {
                Notify("Der Stand ist voll.", "info");
                return 0;
            }
            int put = Math.Min(space, crate.Quantity);
            StandStock[crate.Product] = (StandStock.TryGetValue(crate.Product, out int n) ? n : 0) + put;
            crate.Quantity -= put;
            RaiseEconomyChanged();
            return put;
        }

        /// <summary>Preis am Stand = dein Shop-Preis (inkl. Rabattaktion).</summary>
        public int StandPrice(string pid) => CurrentSalePrice(pid);

        /// <summary>
        /// Ein Passant schaut sich den Stand an. Gibt true zurück, wenn gekauft wurde.
        /// tooExpensive = der Passant fand deinen Preis zu hoch.
        /// </summary>
        public bool StandTryBuy(out string pid, out int price, out bool tooExpensive)
        {
            pid = "";
            price = 0;
            tooExpensive = false;
            if (!StandOpen) return false;
            int total = StandTotal();
            int r = Rng.Index(total);
            foreach (var p in GameData.Products)
            {
                int q = StandStock.TryGetValue(p.Id, out int n) ? n : 0;
                if (r < q)
                {
                    pid = p.Id;
                    break;
                }
                r -= q;
            }
            if (pid == "") return false;
            price = StandPrice(pid);
            float market = Market.MarketPrice(pid);
            float willing = Mathx.Clamp01(1.25f - price / Math.Max(1f, market) * 0.55f) * Mathx.Lerp(0.6f, 1.1f, Reputation / 5f);
            if (Rng.Value() > willing)
            {
                tooExpensive = price > market * 1.05f;
                return false;
            }
            StandStock[pid] -= 1;
            Money += price;
            Daily.Revenue += price;
            Daily.Stand += price;
            TotalEarned += price;
            StandSoldTotal++;
            ShippedPerProduct[pid] = (ShippedPerProduct.TryGetValue(pid, out int s) ? s : 0) + 1;
            AddXp(Math.Max(1, price / 3));
            Sound("cash", 0.04f, -4f);
            StandSale?.Invoke(pid, price);
            CheckGoals();
            RaiseEconomyChanged();
            return true;
        }
    }
}
