using System;
using System.Collections.Generic;
using System.IO;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>
    /// Logik-Tests für die komplette Spiellogik (ohne Welt und Eingaben).
    /// In Unity: Window → General → Test Runner → EditMode → Run All.
    /// </summary>
    public class CoreTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ds_tests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            }
            catch (Exception)
            {
                // Temp-Ordner aufräumen ist nicht kritisch
            }
        }

        private Sim Fresh(int seed = 1)
        {
            var sim = new Sim(seed) { SaveDir = _dir };
            sim.TutorialStep = -1;
            return sim;
        }

        // ---- Grundlagen & Einkauf ------------------------------------------------------------
        [Test]
        public void Basics_StartAndBuying()
        {
            var gm = Fresh();
            Assert.AreEqual(GameData.StartCapital, gm.Money, "Startkapital");
            Assert.AreEqual(14, GameData.Products.Length, "14 Produkte im Katalog");
            Assert.IsTrue(gm.Level == 1 && gm.LocationStage == 0, "Level 1, Garage");
            Assert.IsTrue(gm.ProductAvailable("huelle") && gm.ProductAvailable("led") && gm.ProductAvailable("toaster") && !gm.ProductAvailable("massage"), "Drei Produkte zu Beginn verfügbar");
            Assert.AreEqual(40, gm.BulkCost(0, 0, 1), "20 Hüllen beim Standard-Anbieter kosten 40 €");
            Assert.IsTrue(gm.BuyBulk(0, 0, 1), "Einkauf klappt");
            Assert.AreEqual(GameData.StartCapital - 40, gm.Money, "Geld abgezogen");
            Assert.IsFalse(gm.BuyBulk(2, 0, 1), "Gesperrtes Produkt (Massagepistole) kann nicht gekauft werden");
            Assert.IsFalse(gm.BuyBulk(0, 0, 2), "Premium-Anbieter ist auf Level 1 gesperrt");
            float lead = gm.LeadMinutes(1);
            gm.AdvanceMinutes(lead + 0.5f);
            Assert.IsTrue(gm.TravelingDeliveries.Count == 0 && gm.DockCrates.Count == 1, "Lieferung am Wareneingang");
            var crate = gm.PickupCrate();
            Assert.AreEqual(20, crate.Quantity, "Kiste enthält 20 Stück");
            Assert.IsTrue(gm.UnboxCrate("huelle", 20, 1f), "Kiste eingeräumt");
            Assert.AreEqual(20, gm.StockQty("huelle"), "Lagerbestand 20");
            Assert.IsFalse(gm.UnboxCrate("huelle", 1000, 1f), "Lagerkapazität der Garage wird eingehalten");
        }

        [Test]
        public void Delivery_SendsVanShortlyBeforeArrival()
        {
            var gm = Fresh();
            var incoming = new List<string>();
            gm.DeliveryIncoming += p => incoming.Add(p);
            gm.BuyBulk(0, 0, 1);
            gm.AdvanceMinutes(gm.LeadMinutes(1) - 2f);
            Assert.AreEqual(1, incoming.Count, "Lieferwagen fährt kurz vor Ankunft los");
            Assert.AreEqual(1, gm.TravelingDeliveries.Count, "Ware noch unterwegs");
        }

        // ---- Bestellungen, Verpacken, Versand -----------------------------------------------
        [Test]
        public void Orders_PickPackShip()
        {
            var gm = Fresh();
            gm.Stock["huelle"].Qty = 10;
            gm.SetListed("huelle", true);
            gm.SpawnOrder("huelle");
            Assert.AreEqual(1, gm.PendingCount(), "Eine Bestellung eingegangen");
            int price = gm.OrderQueue[0].Price;
            Assert.AreEqual(gm.CurrentSalePrice("huelle"), price, "Preis wird bei Bestellung festgeschrieben");
            var item = gm.PickItem("huelle");
            Assert.IsTrue(item != null && gm.PendingCount() == 0 && gm.StockQty("huelle") == 9, "Kommissioniert");
            gm.ReturnItem(item);
            Assert.IsTrue(gm.PendingCount() == 1 && gm.StockQty("huelle") == 10, "Zurücklegen stellt Bestellung + Bestand wieder her");
            item = gm.PickItem("huelle");
            int before = gm.Packaging[0];
            Assert.IsTrue(gm.WrapItem(item), "Verpackt");
            Assert.AreEqual(before - 1, gm.Packaging[0], "Karton S verbraucht");
            var pkg = gm.PickupPackage();
            Assert.AreEqual(ItemKind.Package, pkg.Kind, "Paket trägt Branding");
            int xpBefore = gm.Xp;
            gm.ShipPackage(pkg);
            Assert.IsTrue(gm.TotalShipped == 1 && gm.Xp > xpBefore, "Versand zählt + gibt XP");
            Assert.IsTrue(gm.GoalsDone.Contains("first_sale"), "Ziel 'Erster Verkauf' erreicht");
        }

        [Test]
        public void Orders_FullQueueLosesOrders()
        {
            var gm = Fresh();
            gm.Stock["huelle"].Qty = 10;
            gm.SetListed("huelle", true);
            for (int i = 0; i < gm.QueueCapacity(); i++) gm.SpawnOrder("huelle");
            int lost = gm.TotalLostOrders;
            int reviews = gm.ReviewCount;
            for (int i = 0; i < 10; i++) gm.SpawnOrder("huelle");
            Assert.AreEqual(lost + 10, gm.TotalLostOrders, "Verlorene Bestellungen gezählt");
            int newReviews = gm.ReviewCount - reviews;
            Assert.IsTrue(newReviews >= 1 && newReviews <= 10 && gm.Reviews[0].Stars == 1, "Verlorene Bestellungen erzeugen 1-Stern-Bewertungen");
        }

        [Test]
        public void Orders_ExpireAfterTwelveHours()
        {
            var gm = Fresh();
            gm.SetListed("huelle", true);
            gm.SpawnOrder("huelle");
            gm.OrderQueue[0].Created = gm.BClock() - GameData.OrderExpireMinutes - 1f;
            gm.CheckExpiryNow();
            Assert.AreEqual(0, gm.OrderQueue.Count, "Zu alte Bestellung wird storniert");
        }

        // ---- Verpackung & Falttisch ----------------------------------------------------------
        [Test]
        public void Packaging_FoldAndBrandPrint()
        {
            var gm = Fresh();
            Assert.IsTrue(gm.BuyPackaging(1, true, 0), "Ungefaltete M-Kartons gekauft");
            Assert.AreEqual(10, gm.FlatPackaging[1], "10 ungefaltet");
            int size = gm.FoldCarton();
            Assert.IsTrue(size == 1 && gm.Packaging[1] == 1 && gm.FlatPackaging[1] == 9, "Falten wandelt 1 Karton um");
            Assert.AreEqual(60, gm.PackagingCost(0, false, 1), "50er-Pack S kostet 60 €");
            gm.SetBrandColor(GameData.BrandPalette[3]);
            gm.Money = 500;
            gm.BuyPackaging(2, false, 0);
            Assert.IsTrue(gm.PackagingBrand[2].Color.Approx(GameData.BrandPalette[3]), "Markenfarbe wird beim Kauf aufgedruckt");
            Assert.IsFalse(gm.PackagingBrand[0].Color.Approx(GameData.BrandPalette[3]), "Alte Kartons behalten ihren Druck");
        }

        [Test]
        public void Packaging_WrapFailsWithoutCartons()
        {
            var gm = Fresh();
            gm.Packaging[0] = 0;
            gm.Stock["huelle"].Qty = 2;
            gm.SetListed("huelle", true);
            gm.SpawnOrder("huelle");
            var item = gm.PickItem("huelle");
            Assert.IsFalse(gm.WrapItem(item), "Ohne Karton kein Verpacken");
        }

        // ---- Nachfrage ------------------------------------------------------------------------
        [Test]
        public void Demand_ReactsToPriceReputationAndBoosts()
        {
            var gm = Fresh();
            gm.SetListed("huelle", true);
            float r1 = gm.DemandRate("huelle");
            gm.SetShopPrice("huelle", 60);
            float r2 = gm.DemandRate("huelle");
            Assert.IsTrue(r1 > 0f && r2 < r1, "Höherer Preis → weniger Nachfrage");
            gm.SetListed("huelle", false);
            Assert.AreEqual(0f, gm.DemandRate("huelle"), "Offline → keine Nachfrage");
            gm.Listed["massage"] = true;
            Assert.AreEqual(0f, gm.DemandRate("massage"), "Gesperrtes Produkt hat keine Nachfrage");
            gm.SetListed("huelle", true);
            gm.SetShopPrice("huelle", 20);
            float baseRate = gm.DemandRate("huelle");
            gm.Reputation = 5f;
            Assert.IsTrue(gm.DemandRate("huelle") > baseRate, "Gute Bewertung → mehr Nachfrage");
            gm.AddBoost("test", "Test", 2f, 30f);
            Assert.IsTrue(gm.BoostMult("huelle") >= 2f, "Boost multipliziert Nachfrage");
            gm.AdvanceMinutes(31f);
            Assert.AreEqual(0, gm.Boosts.Count, "Boost läuft aus");
        }

        [Test]
        public void Demand_OrdersArriveOverTime()
        {
            var gm = Fresh();
            gm.SetListed("huelle", true);
            for (int i = 0; i < 300; i++) gm.AdvanceMinutes(1f);
            Assert.IsTrue(gm.Daily.Orders > 0, "Bestellungen kommen von allein");
        }

        // ---- Tagesablauf -----------------------------------------------------------------------
        [Test]
        public void Day_EndSummaryAndNextDay()
        {
            var gm = Fresh();
            var got = new List<DaySummary>();
            gm.DayEnded += s => got.Add(s);
            gm.EndDayNow();
            Assert.IsTrue(gm.DayOver && got.Count == 1, "Tagesende löst Abrechnung aus");
            Assert.AreEqual(GameData.StageRent[0], got[0].Rent, "Miete in der Abrechnung");
            int moneyBefore = gm.Money;
            gm.StartNextDay();
            Assert.IsTrue(gm.Day == 2 && !gm.DayOver && Math.Abs(gm.TimeMinutes - GameData.DayStart) < 0.001f, "Tag 2 beginnt um 08:00");
            Assert.AreEqual(moneyBefore - GameData.StageRent[0], gm.Money, "Miete abgebucht");
            Assert.AreEqual(1, gm.History.Count, "Tageshistorie gespeichert");
        }

        [Test]
        public void Day_BankruptcyEndsGame()
        {
            var gm = Fresh();
            var over = new List<string>();
            gm.GameOver += r => over.Add(r);
            gm.Money = -600;
            gm.EndDayNow();
            gm.StartNextDay();
            Assert.IsTrue(over.Count == 1 && gm.Day == 1, "Unter −500 € → Insolvenz statt neuem Tag");
        }

        [Test]
        public void Day_ClockRunsAndStopsAtEight()
        {
            var gm = Fresh();
            gm.AdvanceMinutes(100f);
            Assert.AreEqual(580f, gm.TimeMinutes, 0.01f, "Uhr läuft (08:00 + 100 min)");
            gm.AdvanceMinutes(1000f);
            Assert.IsTrue(gm.DayOver && Math.Abs(gm.TimeMinutes - GameData.DayEnd) < 0.001f, "Um 20 Uhr ist Feierabend");
        }

        [Test]
        public void Day_TickUsesRealTime()
        {
            var gm = Fresh();
            gm.InGame = true;
            gm.Tick(9f);
            Assert.AreEqual(GameData.DayStart + 12f, gm.TimeMinutes, 0.01f, "9 echte Sekunden = 12 Spielminuten");
            gm.InGame = false;
            gm.Tick(9f);
            Assert.AreEqual(GameData.DayStart + 12f, gm.TimeMinutes, 0.01f, "Außerhalb des Spiels steht die Uhr");
        }

        // ---- Level, Ziele, Ausbau --------------------------------------------------------------
        [Test]
        public void Progression_LevelsGoalsUpgrades()
        {
            var gm = Fresh();
            var ups = new List<int>();
            gm.LevelUp += l => ups.Add(l);
            gm.AddXp(160);
            Assert.IsTrue(gm.Level == 2 && ups.Count == 1, "150 XP → Level 2");
            Assert.IsTrue(gm.ProductAvailable("massage") && gm.ProductAvailable("bartglitzer"), "Level 2 schaltet Massagepistole und Bart-Glitzer frei");
            Assert.AreEqual("level", gm.UpgradeState("warehouse"), "Lagerhalle erst ab Level 4");
            gm.Level = 4;
            gm.Money = 6000;
            Assert.IsTrue(gm.BuyUpgrade("warehouse"), "Lagerhalle gekauft");
            Assert.IsTrue(gm.LocationStage == 1 && gm.Capacity() == 3000, "Lagerhalle: 3.000 Lagerplätze");
            Assert.IsTrue(gm.GoalsDone.Contains("warehouse"), "Ziel 'Raus aus der Garage'");
            Assert.IsTrue(gm.ProductHasShelf("drohne"), "Lagerhalle hat Regale für alle Produkte");
            Assert.AreEqual("level", gm.UpgradeState("conveyor"), "Förderband braucht Level 6");
            gm.SetBrandName("  NovaGoods  ");
            Assert.IsTrue(gm.BrandName == "NovaGoods" && gm.GoalsDone.Contains("brand"), "Markenname gesetzt + Ziel");
            gm.Money = 500;
            Assert.IsTrue(gm.BuyDecor("pflanze") && gm.DecorOwned.Contains("pflanze"), "Deko kaufen");
            Assert.IsTrue(gm.BuyLifestyle("sneaker") && gm.LifestyleOwned.Contains("sneaker"), "Lifestyle kaufen");
        }

        [Test]
        public void Progression_NewProductsUnlockLate()
        {
            var gm = Fresh();
            gm.Level = 4;
            Assert.IsTrue(gm.ProductUnlocked("kopfhoerer") && !gm.ProductAvailable("kopfhoerer"), "Kopfhörer ab Level 4, aber nur mit Lagerhalle");
            gm.LocationStage = 1;
            Assert.IsTrue(gm.ProductAvailable("kopfhoerer"), "Kopfhörer mit Lagerhalle verfügbar");
            Assert.IsFalse(gm.ProductAvailable("drohne"), "Drohne erst ab Level 10");
            gm.Level = 10;
            Assert.IsTrue(gm.ProductAvailable("drohne"), "Drohne auf Level 10");
        }

        // ---- Personal & Förderband -------------------------------------------------------------
        [Test]
        public void Staff_WorkAutomatically()
        {
            var gm = Fresh();
            Assert.IsFalse(gm.Hire("packer"), "Kein Personal in der Garage");
            gm.LocationStage = 1;
            gm.Upgrades.Add("warehouse");
            gm.Level = 5;
            Assert.IsTrue(gm.Hire("packer"), "Packer:in eingestellt");
            Assert.AreEqual(70, gm.WagesPerDay(), "Lohn 70 €/Tag");
            gm.Stock["huelle"].Qty = 5;
            gm.Listed["huelle"] = true;
            gm.SpawnOrder("huelle");
            gm.UpdateStaff(13f);
            Assert.IsTrue(gm.PackedCount() == 1 && gm.PendingCount() == 0, "Packer:in verpackt automatisch");
            Assert.IsTrue(gm.Hire("versand"), "Versandkraft eingestellt");
            int shipped = gm.TotalShipped;
            gm.UpdateStaff(11f);
            Assert.AreEqual(shipped + 1, gm.TotalShipped, "Versandkraft verschickt automatisch");
            Assert.IsTrue(gm.Hire("lager"), "Lagerist:in eingestellt");
            gm.DockCrates.Add(ItemData.Crate("huelle", 20, 1f));
            gm.UpdateStaff(17f);
            Assert.IsTrue(gm.DockCrates.Count == 0 && gm.StockQty("huelle") >= 20, "Lagerist:in räumt Kisten ein");
            gm.Fire(0);
            Assert.AreEqual(2, gm.Staff.Count, "Entlassen");
        }

        [Test]
        public void Conveyor_ShipsAutomatically()
        {
            var gm = Fresh();
            gm.LocationStage = 1;
            Assert.IsFalse(gm.ConveyorInsert(new ItemData { Kind = ItemKind.Labeled, Product = "huelle", Price = 20 }), "Ohne Förderband geht nichts aufs Band");
            gm.Upgrades.Add("conveyor");
            Assert.IsTrue(gm.ConveyorInsert(new ItemData { Kind = ItemKind.Labeled, Product = "huelle", Price = 20 }), "Paket aufs Band gelegt");
            gm.AdvanceMinutes(7f);
            Assert.IsTrue(gm.ConveyorQueue.Count == 0 && gm.TotalShipped == 1, "Band verschickt das Paket");
            Assert.AreEqual(10, gm.UpkeepPerDay(), "Förderband kostet Strom");
        }

        // ---- Ereignisse ------------------------------------------------------------------------
        [Test]
        public void Events_DecisionsAndEffects()
        {
            var gm = Fresh();
            gm.Level = 3;
            gm.Money = 1000;
            gm.Day = 3;
            var mail = gm.Events.Trigger("shitstorm");
            Assert.IsTrue(mail != null && mail.Pending, "Entscheidungs-Ereignis wartet im Postfach");
            float rep = gm.Reputation;
            string res = gm.Events.Choose(mail.Id, 1);
            Assert.IsTrue(res != "" && gm.Reputation < rep, "Ignorieren senkt die Bewertung");
            Assert.IsFalse(mail.Pending, "Entscheidung ist abgeschlossen");
            var oma = gm.Events.Trigger("oma");
            Assert.IsTrue(oma != null && !oma.Pending && gm.Money == 1100, "Oma schickt 100 €");
            gm.TravelingDeliveries.Add(new Delivery { Product = "huelle", Quantity = 20, Quality = 1f, ArriveAt = gm.BClock() + 10f });
            gm.Events.Trigger("customs");
            Assert.IsTrue(gm.TravelingDeliveries[0].ArriveAt > gm.BClock() + 100f, "Zoll verzögert Lieferung");
            Assert.IsTrue(gm.Events.Mails.Count >= 3, "Postfach füllt sich");
        }

        [Test]
        public void Events_AllProbabilitiesSumToOne()
        {
            var gm = Fresh();
            foreach (var ev in EventData.All)
            {
                if (!ev.HasChoices) continue;
                foreach (var c in ev.Choices)
                {
                    if (!string.IsNullOrEmpty(c.Minigame)) continue;
                    float sum = 0f;
                    foreach (var p in gm.Events.Probabilities(c.Outcomes)) sum += p;
                    Assert.AreEqual(1f, sum, 0.001f, "Wahrscheinlichkeiten bei " + ev.Id);
                }
                Assert.IsTrue(ev.DefaultChoice >= 0 && ev.DefaultChoice < ev.Choices.Length, "Standardwahl gültig bei " + ev.Id);
                Assert.IsTrue(string.IsNullOrEmpty(ev.Choices[ev.DefaultChoice].Minigame), "Standardwahl ist kein Minispiel bei " + ev.Id);
            }
        }

        [Test]
        public void Events_PendingChoicesResolveAtDayEnd()
        {
            var gm = Fresh();
            gm.Level = 3;
            gm.Money = 1000;
            var mail = gm.Events.Trigger("tax");
            gm.EndDayNow();
            Assert.IsFalse(mail.Pending, "Offene Entscheidung wird um 20 Uhr automatisch getroffen");
            Assert.IsTrue(mail.Result != "", "Ergebnis steht im Postfach");
        }

        [Test]
        public void Events_SchedulingRespectsTutorial()
        {
            var gm = Fresh();
            gm.Day = 5;
            gm.Level = 5;
            gm.TutorialStep = 3;
            gm.Events.NewDay();
            Assert.AreEqual(0, gm.Events.Schedule.Count, "Während des Tutorials keine Ereignisse");
            gm.TutorialStep = -1;
            int total = 0;
            for (int i = 0; i < 20; i++)
            {
                gm.Events.NewDay();
                total += gm.Events.Schedule.Count;
            }
            Assert.IsTrue(total > 0, "Nach dem Tutorial werden Ereignisse eingeplant");
        }

        // ---- Pitch Day (Minispiel) ---------------------------------------------------------------
        [Test]
        public void Pitch_MinigameRequestAndResolve()
        {
            var gm = Fresh();
            gm.Level = 5;
            var mail = gm.Events.Trigger("pitch_day");
            string requested = null;
            gm.Events.MinigameRequested += (m, name) => requested = name;
            Assert.AreEqual("", gm.Events.Choose(mail.Id, 0), "Minispiel-Wahl wird nicht sofort ausgewürfelt");
            Assert.AreEqual("pitch", requested, "Minispiel wird angefordert");
            Assert.IsTrue(mail.Pending, "Mail bleibt offen, bis das Minispiel vorbei ist");
            var game = new PitchGame(gm);
            Assert.AreEqual(3, game.Questions.Count, "Drei Fragen");
            while (!game.Finished)
            {
                var q = game.Questions[game.Round];
                int best = 0;
                for (int i = 1; i < q.Answers.Length; i++)
                    if (q.Answers[i].Strength > q.Answers[best].Strength) best = i;
                Assert.IsTrue(game.Choose(best) != "", "Jury reagiert");
            }
            var result = game.Evaluate();
            int money = gm.Money;
            gm.Events.ResolveMinigame(mail.Id, "Pitch gehalten", result.Text, result.Effects);
            Assert.IsFalse(mail.Pending, "Pitch abgeschlossen");
            Assert.AreEqual(money + result.Money, gm.Money, "Deal-Summe gutgeschrieben");
        }

        [Test]
        public void Pitch_StrongCompanyScoresHigher()
        {
            var weak = Fresh(3);
            var strong = Fresh(3);
            strong.Reputation = 4.9f;
            strong.TotalShipped = 400;
            strong.TotalEarned = 30000;
            strong.LocationStage = 1;
            strong.Level = 9;
            for (int i = 0; i < 6; i++) strong.Staff.Add(new StaffMember { Role = "packer", Name = "X" });
            strong.BrandNamed = true;
            strong.Awareness = 1.2f;
            Assert.IsTrue(new PitchGame(strong).StatValue("rating") > new PitchGame(weak).StatValue("rating"), "Bessere Bewertung zählt");
            Assert.IsTrue(new PitchGame(strong).StatValue("company") > 0.9f, "Lager + Team zählt");
        }

        // ---- Markt & Trading -------------------------------------------------------------------------
        [Test]
        public void Market_PriceWarAndTrading()
        {
            var gm = Fresh();
            Assert.IsTrue(gm.Market.MarketPrice("huelle") > 5f, "Marktpreis vorhanden");
            float mp = gm.Market.MarketPrice("huelle");
            gm.Market.ApplyPriceWar("huelle", 0.75f, 2);
            Assert.IsTrue(gm.Market.MarketPrice("huelle") < mp, "Preiskampf senkt Marktpreis");
            gm.Money = 1000;
            Assert.IsTrue(gm.Market.Buy("ETF", 500), "ETF gekauft");
            Assert.IsTrue(gm.Money == 500 && gm.Market.Holdings["ETF"] > 0f, "Geld investiert");
            int back = gm.Market.Sell("ETF", 1f);
            Assert.IsTrue(back > 480 && back < 500, "Verkauf bringt Geld minus Gebühren (" + back + ")");
            Assert.AreEqual(0f, gm.Market.Holdings["ETF"], "Bestand leer");
            gm.Level = 2;
            gm.TikTokReadyAt = 0f;
            Assert.IsTrue(gm.TikTokAvailable(), "TikTok ab Level 2 verfügbar");
            gm.TriggerTikTok(0.9f);
            Assert.IsTrue(gm.ActiveBoost("tiktok") != null && !gm.TikTokAvailable(), "TikTok-Boost aktiv + Abklingzeit");
        }

        [Test]
        public void Market_PricesStayPositive()
        {
            var gm = Fresh(7);
            for (int i = 0; i < 2000; i++) gm.Market.Tick(false);
            foreach (var a in Market.Assets)
            {
                Assert.IsTrue(gm.Market.Prices[a.Id] >= 0.05f, "Kurs bleibt positiv: " + a.Id);
                Assert.IsTrue(gm.Market.History[a.Id].Count <= Market.HistoryLength, "Kursverlauf begrenzt: " + a.Id);
            }
        }

        // ---- Bank ----------------------------------------------------------------------------------
        [Test]
        public void Bank_LoansInterestAndRepay()
        {
            var gm = Fresh();
            Assert.AreEqual(500, gm.CreditLimit(), "Kreditrahmen auf Level 1");
            Assert.IsFalse(gm.TakeLoan(1000), "Mehr als der Rahmen geht nicht");
            Assert.IsTrue(gm.TakeLoan(500), "Kredit aufgenommen");
            Assert.IsTrue(gm.Money == 650 && gm.Debt == 500, "Geld ausgezahlt, Schuld notiert");
            Assert.AreEqual(10, gm.InterestPerDay(), "2 % Zinsen pro Tag");
            var got = new List<DaySummary>();
            gm.DayEnded += s => got.Add(s);
            gm.EndDayNow();
            Assert.AreEqual(10, got[0].Interest, "Zinsen in der Abrechnung");
            gm.StartNextDay();
            Assert.AreEqual(650 - 15 - 10, gm.Money, "Miete + Zinsen abgebucht");
            Assert.AreEqual(200, gm.RepayLoan(200), "Teil getilgt");
            Assert.AreEqual(300, gm.Debt, "Restschuld");
            gm.Money = 10000;
            gm.RepayLoan(99999);
            Assert.AreEqual(0, gm.Debt, "Komplett getilgt");
            Assert.AreEqual(0, gm.InterestPerDay(), "Ohne Schulden keine Zinsen");
        }

        // ---- Verkaufsstand ------------------------------------------------------------------------------
        [Test]
        public void Stand_SellsToPassersby()
        {
            var gm = Fresh(11);
            var crate = ItemData.Crate("huelle", 20, 1f);
            gm.StandAddCrate(crate);
            Assert.AreEqual(0, gm.StandTotal(), "Ohne Verkaufsstand-Upgrade verkauft niemand");
            Assert.IsFalse(gm.StandOpen, "Stand geschlossen ohne Upgrade");
            gm.Upgrades.Add("stand");
            Assert.AreEqual(20, gm.StandAddCrate(crate), "Mit Stand: Kiste abgeladen");
            Assert.AreEqual(0, crate.Quantity, "Kiste ist leer");
            Assert.IsTrue(gm.StandOpen, "Stand offen");
            int sold = 0;
            int money = gm.Money;
            for (int i = 0; i < 40; i++)
                if (gm.StandTryBuy(out _, out _, out _)) sold++;
            Assert.IsTrue(sold > 0, "Passanten kaufen");
            Assert.AreEqual(sold, gm.StandSoldTotal, "Verkäufe gezählt");
            Assert.IsTrue(gm.Money > money && gm.Daily.Stand > 0, "Einnahmen verbucht");
            Assert.AreEqual(20 - sold, gm.StandTotal(), "Bestand sinkt");
        }

        [Test]
        public void Stand_CrateLeftoverWhenFull()
        {
            var gm = Fresh();
            gm.Upgrades.Add("stand");
            var big = ItemData.Crate("huelle", 50, 1f);
            Assert.AreEqual(50, gm.StandAddCrate(big), "50 passen drauf");
            var more = ItemData.Crate("huelle", 20, 1f);
            Assert.AreEqual(GameData.StandCapacity - 50, gm.StandAddCrate(more), "Nur bis zur Kapazität");
            Assert.AreEqual(20 - (GameData.StandCapacity - 50), more.Quantity, "Rest bleibt in der Kiste");
        }

        [Test]
        public void Stand_OverpricedGoodsDoNotSell()
        {
            var gm = Fresh(5);
            gm.Upgrades.Add("stand");
            gm.StandAddCrate(ItemData.Crate("huelle", 30, 1f));
            gm.SetShopPrice("huelle", 400);
            int sold = 0;
            for (int i = 0; i < 30; i++)
                if (gm.StandTryBuy(out _, out _, out _)) sold++;
            Assert.AreEqual(0, sold, "Wucherpreise schrecken ab");
        }

        // ---- Imbiss-Intro & Tutorial ------------------------------------------------------------------
        [Test]
        public void Intro_DinerShift()
        {
            var gm = Fresh();
            gm.NewGame("intro");
            Assert.IsTrue(gm.StoryStage == "diner" && gm.Money == 0, "Intro startet im Imbiss ohne Geld");
            Assert.AreEqual(0, gm.DinerPlatesWaiting(), "Vor dem Gespräch mit Kalle keine Teller");
            gm.StartShift();
            Assert.AreEqual(3, gm.DinerPlatesWaiting(), "Drei Teller warten");
            var plate = gm.DinerTakePlate();
            Assert.AreEqual(2, plate.Table, "Erster Teller für Tisch 2");
            Assert.IsFalse(gm.DinerServe(3, plate), "Falscher Tisch wird abgelehnt");
            Assert.IsTrue(gm.DinerServe(2, plate), "Richtiger Tisch");
            for (int i = 0; i < 2; i++)
            {
                var pl = gm.DinerTakePlate();
                gm.DinerServe(pl.Table, pl);
            }
            Assert.AreEqual(2, gm.IntroStep, "Alle Teller serviert → zurück zu Kalle");
            Assert.IsFalse(gm.SaveGame(true), "Das Intro wird nicht gespeichert");
            gm.FinishIntro();
            Assert.IsTrue(gm.StoryStage == "business" && gm.Money == GameData.StartCapital && gm.TutorialStep == 0, "Kündigung: 150 € und Tutorial startet");
        }

        [Test]
        public void Tutorial_FullFlow()
        {
            var gm = Fresh();
            gm.NewGame("tutorial");
            Assert.AreEqual(0, gm.TutorialStep, "Tutorial bei Schritt 1");
            gm.OpenPc();
            gm.ClosePc();
            Assert.AreEqual(1, gm.TutorialStep, "Laptop geöffnet → Schritt 2");
            gm.BuyBulk(0, 0, 1);
            Assert.AreEqual(2, gm.TutorialStep, "Bestellt → Schritt 3");
            gm.AdvanceMinutes(gm.LeadMinutes(1) + 1f);
            var c = gm.PickupCrate();
            Assert.AreEqual(3, gm.TutorialStep, "Kiste genommen → Schritt 4");
            gm.UnboxCrate("huelle", c.Quantity, c.Quality);
            Assert.AreEqual(4, gm.TutorialStep, "Eingeräumt → Schritt 5");
            gm.SetListed("huelle", true);
            Assert.AreEqual(5, gm.TutorialStep, "Online → Schritt 6");
            gm.AdvanceMinutes(5f);
            Assert.IsTrue(gm.PendingCount() >= 1 && gm.TutorialStep == 6, "Erste Bestellung kommt garantiert");
            var it = gm.PickItem("huelle");
            gm.WrapItem(it);
            var pk = gm.PickupPackage();
            gm.OnLabeled();
            gm.ShipPackage(pk);
            Assert.AreEqual(-1, gm.TutorialStep, "Tutorial abgeschlossen");
        }

        [Test]
        public void Objective_ShowsTutorialThenGoals()
        {
            var gm = Fresh();
            gm.NewGame("tutorial");
            Assert.IsTrue(gm.CurrentObjective().Title.StartsWith("Tutorial 1/10"), "Tutorial als Ziel");
            gm.TutorialStep = -1;
            Assert.IsTrue(gm.CurrentObjective().Title.Contains("Der erste Verkauf"), "Danach das nächste Ziel");
        }

        // ---- Speichern / Laden ---------------------------------------------------------------------
        [Test]
        public void Save_RoundTripKeepsEverything()
        {
            var gm = Fresh();
            gm.Slot = 2;
            gm.Money = 4242;
            gm.Day = 7;
            gm.Level = 5;
            gm.Xp = 3000;
            gm.LocationStage = 1;
            gm.Upgrades.Add("warehouse");
            gm.BrandName = "SaveTest";
            gm.BrandColor = new RGBA(0.1f, 0.8f, 0.3f);
            gm.Stock["led"] = new StockEntry { Qty = 42, Quality = 1.6f };
            gm.Packaging = new[] { 3, 4, 5 };
            gm.PackedPackages.Add(new ItemData { Kind = ItemKind.Package, Product = "led", Price = 33, Quality = 1f, Created = 12.5f, Color = new RGBA(0.9f, 0.1f, 0.1f), Logo = 2 });
            gm.Staff.Add(new StaffMember { Role = "packer", Name = "Mia", Progress = 0.5f });
            gm.Boosts.Add(new Boost { Source = "ad", Name = "Facebook-Ads", Mult = 1.6f, EndsAt = gm.BClock() + 50f });
            gm.Reviews.Add(new Review { Stars = 5, Text = "Top", Name = "Ben", Product = "led", Day = 7 });
            gm.GoalsDone.Add("first_sale");
            gm.Market.Holdings["DROP"] = 12.5f;
            gm.Debt = 1234;
            gm.StandStock["led"] = 9;
            gm.WorldItems.Add(new WorldItemSave { Item = ItemData.Crate("huelle", 20, 1f), Pos = new V3(1, 2, 3), RotY = 45f });
            gm.PlayerState = new PlayerSave { Pos = new V3(-16, 0, -8), RotY = 90f, Held = new ItemData { Kind = ItemKind.Item, Product = "led", Price = 30 } };
            gm.Events.AddMail("Test", "Hallo", "Text");
            Assert.IsTrue(gm.SaveGame(true), "Spielstand geschrieben");
            Assert.IsTrue(gm.HasSave(2) && !gm.HasSave(1), "In Slot 2 gespeichert");
            var summary = gm.ReadSummary(2);
            Assert.IsTrue(summary.Day == 7 && summary.Brand == "SaveTest", "Menü-Zusammenfassung lesbar");
            Assert.AreEqual(2, gm.MostRecentSlot(), "Jüngster Spielstand gefunden");

            var gm2 = Fresh();
            Assert.IsTrue(gm2.LoadGame(2), "Geladen");
            Assert.IsTrue(gm2.Money == 4242 && gm2.Day == 7 && gm2.Level == 5 && gm2.Slot == 2, "Geld/Tag/Level/Slot korrekt");
            Assert.IsTrue(gm2.LocationStage == 1 && gm2.HasUpgrade("warehouse"), "Standort + Upgrades korrekt");
            Assert.IsTrue(gm2.BrandColor.Approx(new RGBA(0.1f, 0.8f, 0.3f)), "Farbe korrekt");
            Assert.IsTrue(gm2.StockQty("led") == 42 && Math.Abs(gm2.StockQuality("led") - 1.6f) < 0.001f, "Lager korrekt");
            Assert.AreEqual(5, gm2.Packaging[2], "Kartons korrekt");
            Assert.IsTrue(gm2.PackedPackages.Count == 1 && gm2.PackedPackages[0].Color.Approx(new RGBA(0.9f, 0.1f, 0.1f)), "Pakete mit Farbe korrekt");
            Assert.IsTrue(gm2.Staff.Count == 1 && gm2.Staff[0].Name == "Mia", "Personal korrekt");
            Assert.IsTrue(gm2.Boosts.Count == 1 && gm2.Reviews.Count == 1 && gm2.GoalsDone.Contains("first_sale"), "Boosts/Bewertungen/Ziele korrekt");
            Assert.AreEqual(12.5f, gm2.Market.Holdings["DROP"], 0.001f, "Trading-Bestand korrekt");
            Assert.AreEqual(1, gm2.Events.Mails.Count, "Postfach korrekt");
            Assert.IsTrue(gm2.Debt == 1234 && gm2.StandStock["led"] == 9, "Kredit + Stand korrekt");
            Assert.IsTrue(gm2.WorldItems.Count == 1 && Math.Abs(gm2.WorldItems[0].Pos.z - 3f) < 0.001f, "Abgelegte Gegenstände korrekt");
            Assert.IsTrue(gm2.PlayerState != null && gm2.PlayerState.Held.Product == "led", "Spielerposition + gehaltener Gegenstand korrekt");
            gm2.DeleteSave(2);
            Assert.IsFalse(gm2.HasSave(2), "Test-Spielstand wieder gelöscht");
        }

        [Test]
        public void Save_CollectWorldStateHookIsCalled()
        {
            var gm = Fresh();
            bool called = false;
            gm.CollectWorldState = () => called = true;
            gm.SaveGame(true);
            Assert.IsTrue(called, "Welt wird vor dem Speichern abgefragt");
        }

        [Test]
        public void Json_RoundTrip()
        {
            var data = new Dictionary<string, object>
            {
                { "n", 5 }, { "f", 1.25 }, { "s", "Grüße \"zitiert\"\nneue Zeile" }, { "b", true }, { "nil", null },
                { "list", new List<object> { 1, 2.5, "x" } }, { "obj", new Dictionary<string, object> { { "a", 1 } } },
            };
            var back = (Dictionary<string, object>)Json.Parse(Json.Write(data));
            Assert.AreEqual(5, J.I(back, "n"));
            Assert.AreEqual(1.25f, J.F(back, "f"), 0.0001f);
            Assert.AreEqual("Grüße \"zitiert\"\nneue Zeile", J.S(back, "s"));
            Assert.IsTrue(J.B(back, "b"));
            Assert.IsNull(back["nil"]);
            Assert.AreEqual(3, J.A(back, "list").Count);
            Assert.AreEqual(1, J.I(J.O(back, "obj"), "a"));
            Assert.IsFalse(Json.TryParse("{kaputt", out _), "Kaputtes JSON wird erkannt");
        }

        [Test]
        public void Audio_AllSoundsAndMusicRender()
        {
            foreach (var n in AudioSynth.SfxNames)
            {
                var b = AudioSynth.Render(n, false);
                Assert.IsTrue(b.Length >= 200, "Soundeffekt zu kurz: " + n);
                float peak = 0f;
                foreach (float v in b) peak = Math.Max(peak, Math.Abs(v));
                Assert.IsTrue(peak > 0.01f && peak < 2f, "Soundeffekt hörbar und nicht übersteuert: " + n);
            }
            foreach (var t in AudioSynth.MusicNames)
            {
                var m = AudioSynth.Render(t, true);
                Assert.IsTrue(m.Length > AudioSynth.Rate * 10, "Musikstück länger als 10 s: " + t);
                float lo = 0f, hi = 0f;
                foreach (float v in m)
                {
                    lo = Math.Min(lo, v);
                    hi = Math.Max(hi, v);
                }
                Assert.IsTrue(lo >= -1f && hi <= 1f && hi > 0.3f, "Musik hörbar und nicht übersteuert: " + t);
            }
        }

        [Test]
        public void Format_GermanNumbers()
        {
            Assert.AreEqual("1.234 €", Fmt.Money(1234));
            Assert.AreEqual("−50 €", Fmt.Money(-50));
            Assert.AreEqual("100.000 €", Fmt.Money(100000));
            Assert.AreEqual("08:05", Fmt.Clock(485));
            Assert.AreEqual("4,2", Fmt.Rating(4.21f));
            Assert.AreEqual("+12,5 %", Fmt.Pct(0.125f));
        }
    }
}
