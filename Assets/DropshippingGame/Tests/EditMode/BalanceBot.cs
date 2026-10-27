using System;
using System.Collections.Generic;
using System.Text;
using DropshippingGame.Core;

namespace DropshippingGame.Tests
{
    /// <summary>Spielertyp für den Balance-Bot.</summary>
    public enum PlayerType
    {
        /// <summary>Vorsichtig: Standard-Ware, Packstation, Rückrufe, TurboKurier bei Streik, Sparkonto, keine Tricks.</summary>
        Careful,
        /// <summary>Durchschnitt: nutzt fast alles ein bisschen (Werbung, TikTok, Fest, ETF, Haustier).</summary>
        Average,
        /// <summary>Gierig: Billig-Ware, hohe Preise, Werbung stapeln, Fakes, Krypto, Kredit, Abholung gegen Gebühr.</summary>
        Greedy,
    }

    /// <summary>
    /// Balance-Bot für die ganze Kampagne. Spielt über die öffentliche Sim-API mit menschlichem Tempo
    /// (Hände-Budget: eine Bestellung von Hand dauert <see cref="Pace"/> Spielminuten, Laufwege werden
    /// mit Energie/Müdigkeit langsamer) und nutzt je nach Spielertyp alle Systeme: Perks, Einrichtung &amp;
    /// Energie, Müll, Porto &amp; Packstation, Lieferstörungen, Defekte, Großaufträge &amp; Großhändler,
    /// Werbung (alt, Fakebook, Gugel, Fake-Profile, Fake-Seiten), TikTok (Wertung aus <see cref="TikTokScoring"/>),
    /// Straßenfest, Verkaufsstand, Läden &amp; Haustiere, Finanzen (Aktien/ETF/Krypto/Sparkonto), Kredite,
    /// Personal, Förderband. Mit Schaltern lassen sich einzelne Exploits gezielt einschalten.
    /// Echtzeit: 1 Geschäftsminute = 0,75 s, dazu 30 s pro Tag für Tagesabrechnung &amp; Menüs.
    /// </summary>
    public sealed class BalanceBot
    {
        public readonly Sim Gm;
        public readonly PlayerType Type;
        public readonly int Seed;
        public bool Log;

        // ---- Verhalten (aus dem Typ, einzeln überschreibbar) ------------------------------------
        public float Pace;
        public int Supplier;
        public float PriceCap;
        public bool UsePickupFee;
        public bool UseContracts = true, UseReturns = true, RecallDefects, TurboOnStrike, HomeUpgrades, Pets, Stand, Festival = true;
        public float TikTokQuality;
        public int TikToksPerDay;
        public int FakebookPerDay, GugelPerDay;
        public int FakebookBudget = 50, GugelBudget = 60;
        public bool OldAds;
        public int FakeProfilesWanted, FakeSitesWanted;
        public string InvestAsset = "";
        public float InvestShare;
        public bool SavingsAccount, LoanToInvest, LoanForWarehouse, MediaMarkdArbitrage, LifestyleSpender;
        public int FestivalStock = 60;
        /// <summary>Sofort-Auswahl bei Ereignissen: -1 = Standard verfallen lassen, 0 = erste Option, 99 = günstigste vernünftige.</summary>
        public int EventChoice = -1;

        // ---- Ergebnis ----------------------------------------------------------------------------
        public bool Broke;
        public int BrokeDay = -1;
        public float FirstLevelUpMin = -1f;
        public float WarehouseMin = -1f, GoalMin = -1f, Level10Min = -1f;
        public int WarehouseDay = -1, GoalDay = -1;
        public int ContractsAccepted;
        public int TikToks, FestivalSold, FestivalRevenue, StandSold, ArbitrageSaved;
        public float TikTokScoreSum;
        public int PeakDebt, MamaRescues;
        public readonly List<float> RewardTimes = new List<float>();
        public readonly List<string> Lines = new List<string>();
        public readonly HashSet<string> SystemsUsed = new HashSet<string>();

        private float _hands;
        private int _orders, _lost, _lostSeen;
        private float _interval;
        private int _contractInc, _standInc;
        private float _priceCheck, _festAcc, _standAcc;
        private readonly Random _rnd;
        private int _perkSeen;

        public BalanceBot(PlayerType type, int seed, string difficulty = "normal")
        {
            Type = type;
            Seed = seed;
            _rnd = new Random(seed * 7 + (int)type);
            Gm = new Sim(seed);
            Gm.AdsReseed(seed + 11);
            Gm.FestivalReseed(seed + 13);
            Gm.Market.SeedExchange(seed + 17);
            DifficultyHook.Apply(Gm, difficulty);
            Gm.NewGame("skip");
            Gm.ShopSeed = 1000 + seed;
            Gm.InGame = true;
            Gm.GameOver += _ =>
            {
                Broke = true;
                BrokeDay = Gm.Day;
            };
            Gm.LevelUp += lvl =>
            {
                if (FirstLevelUpMin < 0f) FirstLevelUpMin = RealMinutes();
                if (lvl >= 10 && Level10Min < 0f) Level10Min = RealMinutes();
                Reward();
            };
            Gm.GoalCompleted += g =>
            {
                Reward();
                if (g.Final && GoalMin < 0f)
                {
                    GoalMin = RealMinutes();
                    GoalDay = Gm.Day;
                }
            };
            Gm.ChallengeCompleted += _ => Reward();
            Gm.ContractCompleted += _ => Reward();
            Gm.FestivalEnded += _ => Reward();
            DifficultyHook.Subscribe(Gm, Reward, () => MamaRescues++);
            Configure();
        }

        private void Reward() => RewardTimes.Add(RealMinutes());

        /// <summary>Echtzeit-Minuten seit Spielbeginn (0,75 s je Geschäftsminute + 30 s je Tageswechsel).</summary>
        public float RealMinutes() => Gm.BClock() * 0.75f / 60f + (Gm.Day - 1) * 0.75f;

        private void Configure()
        {
            switch (Type)
            {
                case PlayerType.Careful:
                    Pace = 46f; Supplier = 1; PriceCap = 1.12f; UsePickupFee = false; RecallDefects = true; TurboOnStrike = true;
                    HomeUpgrades = true; Pets = false; Stand = false; TikTokQuality = 0.42f; TikToksPerDay = 1;
                    FakebookPerDay = 1; FakebookBudget = 20; GugelPerDay = 0; SavingsAccount = true; FestivalStock = 40; EventChoice = 99;
                    break;
                case PlayerType.Average:
                    Pace = 40f; Supplier = 1; PriceCap = 1.2f; UsePickupFee = true; RecallDefects = true; TurboOnStrike = true;
                    HomeUpgrades = true; Pets = true; Stand = true; TikTokQuality = 0.36f; TikToksPerDay = 2;
                    FakebookPerDay = 1; GugelPerDay = 1; FakeSitesWanted = 2; InvestAsset = "HSTL"; InvestShare = 0.15f; FestivalStock = 60;
                    break;
                default:
                    Pace = 34f; Supplier = 0; PriceCap = 1.5f; UsePickupFee = true; RecallDefects = false; TurboOnStrike = false;
                    HomeUpgrades = false; Pets = true; Stand = true; TikTokQuality = 0.28f; TikToksPerDay = 3;
                    FakebookPerDay = 3; FakebookBudget = 100; GugelPerDay = 3; GugelBudget = 120; OldAds = true;
                    FakeProfilesWanted = GameData.MaxFakeProfiles; FakeSitesWanted = GameData.MaxFakeSites;
                    InvestAsset = "MOON"; InvestShare = 0.4f; LoanForWarehouse = true; MediaMarkdArbitrage = true; LifestyleSpender = true;
                    FestivalStock = 120; EventChoice = 0;
                    break;
            }
        }

        // =====================================================================================
        // Ablauf
        // =====================================================================================
        public void RunDays(int days)
        {
            for (int d = 0; d < days && !Broke; d++) RunDay();
        }

        /// <summary>Spielt bis zum Endziel (100.000 € Umsatz) oder maxDays.</summary>
        public void RunUntilGoal(int maxDays)
        {
            while (!Broke && GoalMin < 0f && Gm.Day <= maxDays) RunDay();
        }

        public void RunDay()
        {
            while (!Gm.DayOver) Step();
            _orders = Gm.Daily.Orders;
            _lost = Gm.Daily.Lost;
            _interval = Gm.OrderIntervalMinutes();
            _contractInc = Gm.Daily.ContractIncome;
            _standInc = Gm.Daily.Stand;
            Gm.StartNextDay();
            _lostSeen = 0;
            if (Broke) return;
            AfterDay();
        }

        private void Step()
        {
            var gm = Gm;
            gm.AdvanceMinutes(2f);
            if (gm.IsOpen)
            {
                gm.Market.Process(1.5f);
                gm.Events.Process();
            }
            if (gm.DayOver) return;
            // Müdigkeit macht Laufwege langsamer (Wege ≈ 60 % der Arbeit).
            _hands += 2f / (Pace * (gm.LocationStage >= 1 ? 1.3f : 1f)) * (0.4f + 0.6f * gm.WalkSpeedMult()) * (gm.HasEquipment("labelgun") ? 1.08f : 1f);
            Buying();
            Listing();
            if (UseContracts) HandleContracts();
            AdjustPrices();
            Logistics();
            if (Festival) HandleFestival();
            if (gm.Festival == FestivalPhase.Live && gm.FestivalDay == gm.Day && gm.FestivalTotal() > 0) return; // Spieler steht am Fest
            HandleStand();
            HandleReturns();
            HandleEvents();
            Marketing();
            HandOrders();
        }

        private void Buying()
        {
            var gm = Gm;
            int bulk = gm.BulkAvailable(2) && gm.Money > 2500 ? 2 : 1;
            for (int i = 0; i < GameData.Products.Length; i++)
            {
                string id = GameData.Products[i].Id;
                if (!gm.ProductAvailable(id)) continue;
                int want = gm.LocationStage >= 1 ? 60 : 30;
                int sup = gm.SupplierAvailable(Supplier) ? Supplier : 1;
                if (gm.StockQty(id) + gm.TravelingCountFor(id) * 50 + gm.DockCountFor(id) * 50 < want && gm.Money >= gm.BulkCost(i, bulk, sup) + 60)
                    gm.BuyBulk(i, bulk, sup);
            }
            for (int i = 0; i < GameData.Products.Length; i++)
            {
                var p = GameData.Products[i];
                int need = gm.LocationStage >= 1 ? 25 : 8;
                if (gm.ProductAvailable(p.Id) && gm.Packaging[p.Size] + gm.FlatPackaging[p.Size] < need && gm.Money >= gm.PackagingCost(p.Size, false, 1) + 80)
                {
                    bool flat = gm.HasEquipment("faltmaschine") || gm.StaffCount("packer") > 0;
                    gm.BuyPackaging(p.Size, flat, gm.LocationStage >= 1 ? 1 : 0);
                }
            }
        }

        private void Listing()
        {
            var gm = Gm;
            foreach (var p in GameData.Products)
            {
                if (!gm.ProductAvailable(p.Id)) continue;
                bool supply = gm.StockQty(p.Id) > 0 || gm.TravelingCountFor(p.Id) > 0 || gm.DockCountFor(p.Id) > 0;
                if (gm.IsListed(p.Id) != supply) gm.SetListed(p.Id, supply);
            }
        }

        /// <summary>Preise: läuft die Warteschlange über, rauf (bis Markenpreis × Deckel), ist sie leer, runter.</summary>
        private void AdjustPrices()
        {
            var gm = Gm;
            if (gm.BClock() < _priceCheck) return;
            _priceCheck = gm.BClock() + 30f;
            int open = gm.OrderQueue.Count;
            bool losing = gm.Daily.Lost > _lostSeen;
            _lostSeen = gm.Daily.Lost;
            foreach (var p in GameData.Products)
            {
                if (!gm.IsListed(p.Id)) continue;
                int price = gm.ShopPrices[p.Id];
                int floor = (int)Math.Round(p.RefPrice * 0.8f);
                int cap = (int)Math.Round(gm.SuggestedPrice(p.Id) * PriceCap);
                if ((open >= gm.QueueCapacity() / 2 || losing) && price < cap) gm.SetShopPrice(p.Id, Math.Min(cap, price + Math.Max(1, price / 20)));
                else if (open <= 1 && price > floor) gm.SetShopPrice(p.Id, Math.Max(floor, price - Math.Max(1, price / 20)));
            }
        }

        private void Logistics()
        {
            var gm = Gm;
            if (TurboOnStrike) gm.SetCarrier(gm.ActiveDisruption != null ? 1 : 0);
            if (RecallDefects)
                foreach (var pid in gm.DefectiveProducts())
                {
                    gm.RecallDefects(pid);
                    SystemsUsed.Add("defects");
                }
            while (gm.DockCrates.Count > 0 && (gm.StaffCount("lager") == 0 || gm.DockCrates.Count > 2))
            {
                var c = gm.PickupCrate();
                if (c == null) break;
                if (UseContracts && gm.ContractAccepts(c)) gm.ContractDeliver(c);
                if (Stand && gm.HasUpgrade("stand") && gm.StandTotal() < 20 && c.Quantity > 0 && gm.LocationStage == 0) StandSold += 0 * gm.StandAddCrate(c);
                if (c.Quantity <= 0) continue;
                if (!gm.UnboxCrate(c.Product, c.Quantity, c.Quality))
                {
                    gm.ReturnCrate(c);
                    break;
                }
            }
            if (gm.WasteFull)
            {
                if (gm.EquipmentState("container") == "available" && gm.Money > 600) gm.BuyEquipment("container");
                else if (Type == PlayerType.Careful && !gm.GarbagePending && gm.Money > 300) gm.OrderGarbagePickup();
            }
        }

        private void HandleReturns()
        {
            var gm = Gm;
            if (!UseReturns) return;
            while (gm.DockReturns.Count > 0 && _hands >= 0.4f && gm.StaffCount("lager") == 0)
            {
                _hands -= 0.4f;
                var r = gm.PickupReturn();
                if (r == null) break;
                if (!gm.ProcessReturn(r, gm.BStockQuality(r) >= 0.5f)) gm.ProcessReturn(r, false);
            }
        }

        private void HandOrders()
        {
            var gm = Gm;
            // Paketabgabe: Packstation (gratis, längerer Weg) oder Abholung (Gebühr); Förderband spart den Weg.
            float cost = gm.HasUpgrade("conveyor") ? 0.8f : (UsePickupFee ? 0.92f : 1f);
            while (gm.OrderQueue.Count > 0 && _hands >= cost)
            {
                var next = gm.Tickets().Find(o => !o.InWork && gm.StockQty(o.Product) > 0);
                if (next == null) break;
                var it = gm.PickItem(next.Product);
                if (it == null) break;
                if (!gm.WrapItem(it))
                {
                    gm.ReturnItem(it);
                    break;
                }
                _hands -= cost;
                var pkg = gm.PickupPackage();
                pkg.Kind = ItemKind.Labeled;
                gm.OnLabeled(pkg);
                if (UsePickupFee && !gm.HasUpgrade("conveyor"))
                {
                    int fee = PostRules.PickupFee(1, gm.LocationStage, false);
                    if (gm.Money >= fee) gm.AdjustMoney(-fee, "shipping");
                }
                if (gm.HasUpgrade("conveyor")) gm.ConveyorInsert(pkg);
                else gm.ShipPackage(pkg);
            }
        }

        private void HandleContracts()
        {
            var gm = Gm;
            foreach (var offer in gm.ContractOffers())
            {
                if (gm.ActiveContractCount() >= gm.MaxActiveContracts()) break;
                if (offer.MinQuality > GameData.Suppliers[Math.Max(1, Supplier)].Quality) continue;
                int pi = GameData.ProductIndex(offer.Product);
                int crates = (offer.Quantity + 49) / 50;
                if (gm.Money < gm.BulkCost(pi, 1, 1) * crates + 150) continue;
                if (!gm.AcceptContract(offer.Id)) continue;
                ContractsAccepted++;
                SystemsUsed.Add(offer.Wholesale ? "wholesale" : "contracts");
                for (int k = 0; k < crates; k++) gm.BuyBulk(pi, 1, 1);
            }
            if (gm.TimeMinutes < 1080f) return;
            foreach (var c in gm.ActiveContracts())
            {
                if (c.DeadlineDay != gm.Day) continue;
                while (c.Remaining > 0 && _hands >= 0.1f && gm.StockQty(c.Product) > 0)
                {
                    var it = gm.PickForContract(c.Product);
                    if (it == null) break;
                    _hands -= 0.1f;
                    gm.ContractDeliver(it);
                }
            }
        }

        private void HandleEvents()
        {
            var gm = Gm;
            if (EventChoice < 0) return;
            foreach (var m in gm.Events.Mails.ToArray())
            {
                if (!m.Pending) continue;
                var ev = EventData.Find(m.EventId);
                if (ev == null || !ev.HasChoices) continue;
                int pick = 0;
                if (EventChoice == 99)
                {
                    // Vorsichtig: sichere Option (Steuerberater ja, Krypto-Bro nein, sonst Standard).
                    pick = m.EventId == "tax" ? 0 : ev.DefaultChoice;
                }
                if (!string.IsNullOrEmpty(ev.Choices[pick].Minigame)) pick = ev.DefaultChoice;
                if (!string.IsNullOrEmpty(ev.Choices[pick].Minigame)) continue;
                gm.Events.Choose(m.Id, pick);
            }
        }

        private void HandleFestival()
        {
            var gm = Gm;
            if (gm.FestivalDay != gm.Day) return;
            if (gm.Festival == FestivalPhase.Prep && gm.FestivalCanStock && gm.FestivalTotal() < FestivalStock && _hands >= 0.25f)
            {
                string pid = BestFestivalProduct();
                if (pid != "")
                {
                    var crate = gm.FestivalPackCrate(pid);
                    if (crate != null)
                    {
                        gm.FestivalAddCrate(crate);
                        if (crate.Quantity > 0) gm.ReturnCrate(crate);
                        _hands -= 0.25f;
                        SystemsUsed.Add("festival");
                    }
                }
            }
            if (gm.Festival == FestivalPhase.Live)
            {
                // Welt: alle 1,1–2,2 s ein Besucher / Andrang (1,5 s je Schritt).
                _festAcc += 1.5f / 1.65f * gm.FestivalCrowdFactor();
                while (_festAcc >= 1f)
                {
                    _festAcc -= 1f;
                    var v = gm.FestivalVisitor();
                    if (v.Sold)
                    {
                        FestivalSold += v.Qty;
                        FestivalRevenue += v.Price * v.Qty;
                    }
                }
            }
        }

        private string BestFestivalProduct()
        {
            var gm = Gm;
            string best = "";
            float bv = 0f;
            foreach (var p in GameData.Products)
            {
                if (gm.StockQty(p.Id) < 10) continue;
                float v = p.RefPrice * gm.TrendMult(p.Id) / Math.Max(1, gm.FestivalQty(p.Id) + 10);
                if (v > bv)
                {
                    bv = v;
                    best = p.Id;
                }
            }
            return best;
        }

        private void HandleStand()
        {
            var gm = Gm;
            if (!Stand || !gm.StandOpen) return;
            // Passanten: grob alle 15 Geschäftsminuten einer, der am Stand stehen bleibt.
            _standAcc += 2f / 30f;
            while (_standAcc >= 1f)
            {
                _standAcc -= 1f;
                if (gm.StandTryBuy(out _, out _, out _))
                {
                    StandSold++;
                    SystemsUsed.Add("stand");
                }
            }
        }

        private void Marketing()
        {
            var gm = Gm;
            if (gm.TimeMinutes > 1000f) return;
            // TikTok: echte Aufnahme ≈ 25 Spielminuten Hände.
            if (TikToksPerDay > 0 && gm.TikTokBlocker() == null && gm.Daily.TikToks < TikToksPerDay && _hands >= 0.6f && gm.TimeMinutes >= 540f)
            {
                string pid = MostStocked();
                if (pid != "")
                {
                    var take = MakeTake(TikTokQuality, gm.TrendMult(pid));
                    var rating = TikTokScoring.Rate(take, gm.TrendingTikTokFormat(), gm.TrendingTikTokFormat());
                    if (gm.PostTikTokVideo(rating, pid, gm.TrendingTikTokFormat()) != null)
                    {
                        _hands -= 0.6f;
                        TikToks++;
                        TikTokScoreSum += rating.Score;
                        SystemsUsed.Add("tiktok");
                    }
                }
            }
            if (gm.TimeMinutes > 700f || gm.Money < 400) return;
            if (OldAds && gm.ActiveBoost("ad") == null)
            {
                for (int t = GameData.AdTiers.Length - 1; t >= 0; t--)
                    if (gm.Level >= GameData.AdTiers[t].Level && gm.Money > gm.AdCost(t) * 3 && gm.StartAdCampaign(t))
                    {
                        SystemsUsed.Add("ads_old");
                        break;
                    }
            }
            if (gm.FakebookUnlocked && gm.ActiveAdCount("fakebook") < FakebookPerDay)
                foreach (var pid in RankedListed())
                {
                    if (gm.ActiveAdCount("fakebook") >= FakebookPerDay || gm.Money < FakebookBudget * 4) break;
                    if (gm.CanStartAd("fakebook", pid, FakebookBudget) != "") continue;
                    int target = BestTarget(pid);
                    if (gm.StartFakebookAd(pid, target, BestSlogan(target), FakebookBudget) != null) SystemsUsed.Add("fakebook");
                }
            if (gm.GugelUnlocked && gm.ActiveAdCount("gugel") < GugelPerDay)
                foreach (var pid in RankedListed())
                {
                    if (gm.ActiveAdCount("gugel") >= GugelPerDay || gm.Money < GugelBudget * 4) break;
                    if (gm.CanStartAd("gugel", pid, GugelBudget) != "") continue;
                    int bid = GameData.GugelRivalBids(pid, 0)[1] + 1;
                    if (gm.StartGugelAd(pid, 0, bid, GugelBudget) != null) SystemsUsed.Add("gugel");
                }
            while (gm.FakeProfiles < FakeProfilesWanted && gm.Money > 300 && gm.CreateFakeProfile()) SystemsUsed.Add("fakeprofiles");
            if (gm.FakeSitesUnlocked)
                foreach (var pid in RankedListed())
                {
                    if (gm.FakeSites.Count >= FakeSitesWanted || gm.Money < 300) break;
                    while (gm.FakeSites.Count < FakeSitesWanted && gm.CanCreateFakeSite(pid) == "")
                        if (gm.CreateFakeSite(pid) != null) SystemsUsed.Add("fakesites");
                }
        }

        private TikTokTake MakeTake(float q, float hype)
        {
            float N(float m) => (float)Math.Max(0, Math.Min(1, m + (_rnd.NextDouble() - 0.5) * 0.4));
            return new TikTokTake
            {
                Duration = 9f + 9f * N(q), VisibleFrac = N(q + 0.25f), Centered = N(q + 0.1f), DistanceFrac = N(q), SteadyFrac = N(q),
                Light = N(q + 0.1f), AngleDegrees = 160f * N(q - 0.1f), Hook = _rnd.NextDouble() < q, Actions = (int)Math.Round(4 * N(q - 0.1f)),
                BrandFrac = N(q - 0.25f), Hype = hype,
            };
        }

        private string MostStocked()
        {
            string best = "";
            foreach (var id in Gm.Events.ListedProducts())
                if (best == "" || Gm.StockQty(id) * GameData.Product(id).RefPrice > Gm.StockQty(best) * GameData.Product(best).RefPrice) best = id;
            return best;
        }

        private List<string> RankedListed()
        {
            var l = new List<string>(Gm.Events.ListedProducts());
            l.RemoveAll(id => Gm.StockQty(id) < 15);
            l.Sort((a, b) => GameData.Product(b).RefPrice.CompareTo(GameData.Product(a).RefPrice));
            return l;
        }

        private static int BestTarget(string pid)
        {
            for (int i = 0; i < GameData.AdTargets.Length; i++)
                if (Sim.AdTargetFit(pid, i) >= 0.99f) return i;
            return GameData.AdTargets.Length - 1;
        }

        private static int BestSlogan(int target)
        {
            string tid = GameData.AdTargets[target].Id;
            for (int i = 0; i < GameData.AdSlogans.Length; i++)
                if (GameData.AdSlogans[i].Target == tid) return i;
            return 0;
        }

        // =====================================================================================
        // Abends: Ausbau, Personal, Perks, Einrichtung, Läden, Finanzen
        // =====================================================================================
        private static readonly string[] SkillOrder =
        {
            "v_feilschen", "l_arme", "v_kulanz", "l_tetris", "m_content", "v_netzwerk", "l_wege", "m_radar",
            "v_stamm", "l_flow", "m_viral", "m_trendsetter",
        };

        private static readonly string[] PerkOrder =
        {
            "p_kunden", "p_queue", "p_einkauf", "p_porto", "p_team", "p_liefer", "p_lager", "p_ausdauer", "p_tragen",
        };

        private void AfterDay()
        {
            var gm = Gm;
            var h = gm.History[gm.History.Count - 1];
            if (!gm.BrandNamed) gm.SetBrandName("BotShop");
            PeakDebt = Math.Max(PeakDebt, gm.Debt);
            Finance();
            int wh = GameData.Upgrade("warehouse").Cost;
            if (WarehouseDay < 0 && gm.UpgradeState("warehouse") == "available")
            {
                if (LoanForWarehouse && gm.Money < wh + 300 && gm.Money + gm.CreditAvailable() >= wh + 300)
                {
                    gm.TakeLoan(Math.Min(gm.CreditAvailable(), wh + 300 - gm.Money));
                    SystemsUsed.Add("loan");
                }
                if (gm.Money >= wh + (Type == PlayerType.Careful ? 1200 : 500))
                {
                    gm.BuyUpgrade("warehouse");
                    WarehouseDay = gm.Day;
                    WarehouseMin = RealMinutes();
                }
            }
            if (gm.StaffAllowed())
            {
                string[] roles = Type == PlayerType.Greedy ? new[] { "packer", "versand", "lager", "social", "packer", "versand" }
                    : Type == PlayerType.Average ? new[] { "packer", "versand", "lager", "packer", "versand" } : new[] { "packer", "versand", "lager" };
                var count = new Dictionary<string, int>();
                foreach (var r in roles)
                {
                    count[r] = (count.TryGetValue(r, out int n) ? n : 0) + 1;
                    if (gm.StaffCount(r) >= count[r]) continue;
                    if (gm.Money < gm.WagesPerDay() * 4 + 800) break;
                    if (gm.Hire(r)) SystemsUsed.Add("staff");
                    break; // höchstens eine Einstellung pro Tag
                }
            }
            foreach (var pk in PerkOrder)
                while (gm.PerkState(pk) == "available")
                    if (gm.BuyPerk(pk)) SystemsUsed.Add("perks");
            foreach (var id in SkillOrder)
                if (gm.SkillState(id) == "available") gm.LearnSkill(id);
            if (HomeUpgrades || gm.Money > 8000)
                foreach (var slot in new[] { "bed", "coffee", "chair", "desk", "laptop" })
                {
                    var next = gm.NextHomeItem(slot);
                    if (next != null && gm.Level >= next.Level && gm.Money > next.Cost * 3 + 400 && gm.BuyHomeItem(slot)) SystemsUsed.Add("home");
                }
            foreach (var e in new[] { "labelgun", "faltmaschine", "packtisch", "labeldrucker" })
                if (gm.EquipmentState(e) == "available" && gm.Money > GameData.EquipmentDef(e).Cost * 4 + 500 && gm.BuyEquipment(e)) SystemsUsed.Add("equipment");
            foreach (var u in new[] { "stand", "server", "conveyor", "van", "highrack" })
            {
                if (u == "stand" && !Stand) continue;
                var ud = GameData.Upgrade(u);
                if (gm.UpgradeState(u) == "available" && gm.Money > ud.Cost * 2 + 500 && (WarehouseDay >= 0 || u == "stand" || u == "server"))
                {
                    if (gm.BuyUpgrade(u)) SystemsUsed.Add(u);
                }
            }
            if (LifestyleSpender)
                foreach (var ls in GameData.Lifestyle)
                    if (!gm.LifestyleOwned.Contains(ls.Id) && gm.Money > ls.Cost * 2 + 1000) gm.BuyLifestyle(ls.Id);
            ShopsAndPets();
            _hands = Math.Min(_hands, 1f);
            int perks = gm.PerkPointsTotal();
            if (perks > _perkSeen)
            {
                _perkSeen = perks;
                Reward();
            }
            string line = $"T{h.Day,2} Umsatz {h.Revenue,6} Gewinn {h.Profit,6} Pakete {h.Shipped,3} Konto {gm.Money,7} Netto {gm.Market.NetWorth(),7} " +
                          $"Bew {gm.Reputation:0.00} L{gm.Level} Best {_orders,3}/{_lost,3} Int {_interval:0.0} Staff {gm.Staff.Count} Auftr {_contractInc,5} Stand {_standInc,5} Ges.Umsatz {gm.TotalEarned,7}{(gm.Day - 1 == WarehouseDay ? " HALLE" : "")}";
            Lines.Add(line);
            if (Log) Console.WriteLine(line);
        }

        private void Finance()
        {
            var gm = Gm;
            int reserve = gm.LocationStage >= 1 ? 4000 : 1500;
            if (WarehouseDay < 0) reserve = GameData.Upgrade("warehouse").Cost + 1500;
            if (LoanToInvest && gm.Level >= 3 && gm.CreditAvailable() > 0)
            {
                gm.TakeLoan(gm.CreditAvailable());
                SystemsUsed.Add("loan");
            }
            if (gm.Debt > 0 && !LoanToInvest && gm.Money > reserve + 500) gm.RepayLoan(gm.Money - reserve);
            if (InvestAsset != "" && gm.Level >= GameData.TradingLevel)
            {
                int excess = gm.Money - reserve;
                if (LoanToInvest) excess = gm.Money - 800;
                if (excess > 200)
                {
                    int amt = LoanToInvest ? excess : (int)(excess * InvestShare);
                    if (amt > 50 && gm.Market.Buy(InvestAsset, amt)) SystemsUsed.Add("invest");
                }
            }
            if (SavingsAccount && gm.Money > reserve + 1000)
            {
                gm.Market.Deposit(gm.Money - reserve);
                SystemsUsed.Add("savings");
            }
            // Für den Ausbau und gegen Engpässe wieder flüssig machen.
            int needCash = reserve;
            if (gm.Money < needCash / 2)
            {
                if (gm.Market.Savings > 0) gm.Market.Withdraw(needCash - gm.Money);
                if (gm.Money < needCash / 2 && InvestAsset != "" && gm.Market.HoldingValue(InvestAsset) > 100 && !LoanToInvest) gm.Market.Sell(InvestAsset, 0.5f);
            }
        }

        private void ShopsAndPets()
        {
            var gm = Gm;
            if (Pets && gm.Level >= 3 && gm.Money > 1500)
            {
                string sp = Type == PlayerType.Greedy && gm.HasPet("katze") ? "fn_hund" : "fn_katze";
                if (gm.Pets.Count < (Type == PlayerType.Greedy ? 2 : 1) && gm.ShopBlockReason(sp) == "" && gm.CartAdd(sp) && gm.ShopCheckout(ShopData.PetShop))
                    SystemsUsed.Add("pets");
            }
            if (gm.Pets.Count > 0)
            {
                if (gm.PetFoodTotal() < gm.Pets.Count * 3 && gm.Money > 100)
                {
                    if (gm.ShopBlockReason("fn_futter") == "") gm.CartAdd("fn_futter");
                    if (gm.ShopBlockReason("fn_futter") == "") gm.CartAdd("fn_futter");
                }
                int maxGadgets = Type == PlayerType.Greedy ? 8 : 2;
                foreach (var it in ShopData.ItemsOf(ShopData.PetShop))
                    if (it.Kind == "gadget" && gm.PetGadgets.Count + gm.CartCount(ShopData.PetShop) < maxGadgets && gm.Money > 2000 && gm.ShopBlockReason(it.Id) == "") gm.CartAdd(it.Id);
                if (gm.CartCount(ShopData.PetShop) > 0) gm.ShopCheckout(ShopData.PetShop);
                foreach (var p in gm.Pets) gm.PetPet(p.Species);
            }
            if (MediaMarkdArbitrage && _hands >= 0f)
            {
                foreach (var d in gm.ShopDealsToday(ShopData.Electro))
                {
                    if (!gm.ShopCheaperThanWholesale(d.ItemId)) continue;
                    var it = ShopData.Item(d.ItemId);
                    while (gm.ShopBlockReason(d.ItemId) == "" && gm.Money > 500)
                    {
                        gm.CartAdd(d.ItemId);
                        ArbitrageSaved += (int)Math.Round(gm.ShopWholesalePackPrice(d.ItemId) - gm.ShopPrice(d.ItemId));
                    }
                }
                if (gm.CartCount(ShopData.Electro) > 0 && gm.ShopCheckout(ShopData.Electro))
                {
                    _hands -= 0.75f; // Weg in die Stadt
                    SystemsUsed.Add("mediamarkd");
                }
            }
        }

        // =====================================================================================
        // Auswertung
        // =====================================================================================
        /// <summary>Längste Pause zwischen zwei Belohnungen (Level, Ziel, Wochenziel, Auftrag, Fest, Perk-Punkt, Erfolg) bis zu dieser Echtzeit.</summary>
        public float MaxRewardGap(float untilMinutes)
        {
            float last = 0f, gap = 0f;
            foreach (var t in RewardTimes)
            {
                if (t > untilMinutes) break;
                gap = Math.Max(gap, t - last);
                last = t;
            }
            float end = Math.Min(untilMinutes, RealMinutes());
            return Math.Max(gap, end - last);
        }

        public int RewardsBefore(float minutes)
        {
            int n = 0;
            foreach (var t in RewardTimes)
                if (t <= minutes) n++;
            return n;
        }

        public float AvgTikTokScore => TikToks > 0 ? TikTokScoreSum / TikToks : 0f;

        public static string H(float minutes) => minutes < 0f ? "  –  " : (minutes / 60f).ToString("0.00") + "h";
        public static string M(float minutes) => minutes < 0f ? "  –  " : minutes.ToString("0") + "min";

        public string Row()
        {
            var gm = Gm;
            return string.Format("{0,-8} {1,5} | Lv2 {2,6} | Halle {3,6} (T{4,2}) | 100k {5,6} (T{6,2}) | Lv10 {7,6} | T{8,2} Netto {9,8} Umsatz {10,8} | Pleite {11} Mama {12} | Lücke≤3h {13,5} | TikTok Ø{14:0.00}",
                Type, Seed, M(FirstLevelUpMin), H(WarehouseMin), WarehouseDay, H(GoalMin), GoalDay, H(Level10Min), gm.Day,
                gm.Market.NetWorth(), gm.TotalEarned, Broke ? "JA(T" + BrokeDay + ")" : "nein", MamaRescues, M(MaxRewardGap(180f)), AvgTikTokScore);
        }
    }
}
