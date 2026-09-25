using System;
using System.Collections.Generic;
using System.IO;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Spielstand v4: alle neuen Systeme werden gespeichert, alte v3-Spielstände laden weiter.</summary>
    public class SaveV4Tests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ds_tests_v4_" + Guid.NewGuid().ToString("N"));
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
                // Aufräumen ist nicht kritisch
            }
        }

        [Test]
        public void Save_RoundTripKeepsAllNewSystems()
        {
            var gm = new Sim(6) { SaveDir = _dir, Slot = 3 };
            gm.NewGame("skip");
            gm.AddXp(GameData.LevelThreshold(5));
            gm.LocationStage = 1;
            gm.Upgrades.Add("warehouse");
            gm.Money = 9000;
            // Bestellzettel: einer wartet, einer ist verpackt
            gm.Stock["huelle"].Qty = 10;
            var express = gm.SpawnOrder("huelle", true);
            var normal = gm.SpawnOrder("huelle", false);
            var item = gm.PickItem("huelle");
            gm.WrapItem(item);
            // Retouren
            gm.ScheduleReturn(new ItemData { Kind = ItemKind.Labeled, Product = "led", Price = 36, Quality = 1f, Customer = "Ben", City = "Kiel" }, 200f, "Test");
            gm.DockReturns.Add(new ItemData { Kind = ItemKind.Return, Product = "huelle", Price = 20, Quality = 0.6f, Note = "Zu schön" });
            gm.TotalReturns = 4;
            gm.ReturnsPerProduct["led"] = 2;
            // Trends
            gm.Trends.Trigger("led", TrendPhase.Peak, 1.9f);
            // Großaufträge
            var offer = gm.GenerateContractOffer();
            var active = gm.GenerateContractOffer();
            gm.AcceptContract(active.Id);
            gm.ContractDeliver(ItemData.Crate(active.Product, 5, 1f));
            // Skills, Wochenziele, Sonstiges
            gm.LearnSkill("l_arme");
            gm.LearnSkill("v_feilschen");
            gm.BonusSkillPoints = 2;
            gm.Challenges[0].Progress = 7;
            gm.AddBonusChallenge("ship", 12, 150, 50, "Kalle", "Kalles Wette", "Test");
            gm.LastPurchase["led"] = new[] { 2, 0 };
            gm.AddExpressBoost(0.3f, 50f);
            gm.LaunchOrders["ringlicht"] = 123f;
            gm.ContractOfferTimes.Add(700f);
            Assert.IsTrue(gm.SaveGame(true), "Gespeichert");
            Assert.AreEqual(4, gm.ReadSummary(3).Version, "Version 4");

            var gm2 = new Sim(99) { SaveDir = _dir };
            Assert.IsTrue(gm2.LoadGame(3), "Geladen");
            // Bestellzettel
            Assert.AreEqual(1, gm2.OrderQueue.Count, "Wartender Zettel");
            var q = gm2.OrderQueue[0];
            Assert.IsTrue(q.Id == normal.Id && q.Customer == normal.Customer && q.City == normal.City && q.Note == normal.Note, "Zettel-Daten");
            Assert.AreEqual(normal.DueAt, q.DueAt, 0.01f, "Fälligkeit");
            Assert.AreEqual(1, gm2.OrdersInWork.Count, "Zettel in Arbeit");
            Assert.IsTrue(gm2.OrdersInWork[0].Id == express.Id && gm2.OrdersInWork[0].Express && gm2.OrdersInWork[0].Stage == OrderStage.Packed, "Express-Zettel verpackt");
            Assert.AreEqual(express.Id, gm2.PackedPackages[0].OrderId, "Paket trägt die Nummer");
            Assert.AreEqual(gm.NextOrderId, gm2.NextOrderId, "Nächste Nummer");
            // Retouren
            Assert.IsTrue(gm2.ReturnsIncoming.Count == 1 && gm2.ReturnsIncoming[0].Item.City == "Kiel", "Retoure unterwegs");
            Assert.AreEqual(gm.ReturnsIncoming[0].ArriveAt, gm2.ReturnsIncoming[0].ArriveAt, 0.01f);
            Assert.IsTrue(gm2.DockReturns.Count == 1 && gm2.DockReturns[0].Kind == ItemKind.Return && gm2.DockReturns[0].Note == "Zu schön", "Retoure am Wareneingang");
            Assert.IsTrue(gm2.TotalReturns == 4 && gm2.ReturnsPerProduct["led"] == 2, "Retouren-Statistik");
            // Trends
            Assert.AreEqual(TrendPhase.Peak, gm2.Trends.Phase("led"), "Trend-Phase");
            Assert.AreEqual(gm.Trends.State("led").Hype, gm2.Trends.State("led").Hype, 0.001f, "Hype-Wert");
            Assert.AreEqual(gm.Trends.State("led").Reason, gm2.Trends.State("led").Reason, "Begründung");
            // Großaufträge
            Assert.AreEqual(gm.Contracts.Count, gm2.Contracts.Count, "Aufträge");
            var a2 = gm2.FindContract(active.Id);
            Assert.IsTrue(a2.State == ContractState.Active && a2.Delivered == 5 && a2.Company == active.Company && a2.DeadlineDay == active.DeadlineDay, "Laufender Auftrag");
            Assert.AreEqual(ContractState.Offered, gm2.FindContract(offer.Id).State, "Angebot");
            Assert.AreEqual(gm.NextContractId, gm2.NextContractId);
            Assert.AreEqual(1, gm2.ContractOfferTimes.Count, "Geplante Angebote");
            // Skills, Wochenziele, Sonstiges
            Assert.IsTrue(gm2.HasSkill("l_arme") && gm2.HasSkill("v_feilschen") && gm2.BonusSkillPoints == 2, "Skills");
            Assert.AreEqual(gm.SkillPointsAvailable(), gm2.SkillPointsAvailable(), "Freie Punkte");
            Assert.AreEqual(gm.Challenges.Count, gm2.Challenges.Count, "Wochenziele");
            Assert.AreEqual(7f, gm2.Challenges[0].Progress, 0.001f, "Fortschritt");
            Assert.IsTrue(gm2.Challenges.Exists(c => c.Bonus && c.Sponsor == "Kalle"), "Bonusziel");
            Assert.AreEqual(gm.ChallengeWeek, gm2.ChallengeWeek);
            CollectionAssert.AreEqual(new[] { 2, 0 }, gm2.LastPurchase["led"], "Letzter Einkauf");
            Assert.AreEqual(0.3f, gm2.ExpressBoostChance, 0.001f, "Express-Fieber");
            Assert.AreEqual(123f, gm2.LaunchOrders["ringlicht"], 0.01f, "Geplante Neuheiten-Bestellung");
            Assert.IsTrue(gm2.Explained.Contains("express") && gm2.Explained.Contains("contracts"), "Erklärungen nicht doppelt");
            int mails = gm2.Events.Mails.Count;
            gm2.ScheduleReturn(new ItemData { Kind = ItemKind.Labeled, Product = "huelle", Price = 5, Quality = 1f }, 0f);
            gm2.AdvanceMinutes(1f);
            Assert.AreEqual(mails + (gm.Explained.Contains("returns") ? 0 : 1), gm2.Events.Mails.Count, "Erklär-Mail höchstens einmal");
        }

        [Test]
        public void Save_DropsTicketsWhoseItemIsGone()
        {
            var gm = new Sim(2) { SaveDir = _dir };
            gm.NewGame("skip");
            gm.Stock["huelle"].Qty = 5;
            gm.SpawnOrder("huelle", false);
            var inHand = gm.PickItem("huelle");
            gm.SpawnOrder("huelle", false);
            var lost = gm.PickItem("huelle");
            gm.CollectWorldState = () => gm.PlayerState = new PlayerSave { Pos = new V3(1, 0, 1), Held = inHand };
            Assert.AreEqual(2, gm.OrdersInWork.Count);
            Assert.IsTrue(gm.SaveGame(true));
            var gm2 = new Sim(3) { SaveDir = _dir };
            Assert.IsTrue(gm2.LoadGame(gm.Slot));
            Assert.AreEqual(1, gm2.OrdersInWork.Count, "Nur der Zettel zum Artikel in der Hand bleibt");
            Assert.AreEqual(inHand.OrderId, gm2.OrdersInWork[0].Id);
            Assert.AreEqual(inHand.OrderId, gm2.PlayerState.Held.OrderId, "Gehaltener Artikel trägt seine Nummer");
            Assert.AreNotEqual(lost.OrderId, gm2.OrdersInWork[0].Id);
        }

        /// <summary>Ein Spielstand, wie ihn Version 3 (v2.0 des Spiels) geschrieben hat – ohne v3.0-Felder.</summary>
        private const string V3Json = @"{
  ""version"": 3, ""money"": 2345, ""day"": 10, ""time_minutes"": 600, ""story_stage"": ""business"", ""intro_step"": 3,
  ""tutorial_step"": -1, ""tut_flags"": {}, ""location_stage"": 0, ""upgrades"": {""stand"": true}, ""decor_owned"": {""pflanze"": true},
  ""lifestyle_owned"": {}, ""reputation"": 4.1, ""review_count"": 12,
  ""reviews"": [{""stars"": 5, ""text"": ""Top"", ""name"": ""Ben"", ""product"": ""huelle"", ""day"": 9}],
  ""xp"": 900, ""level"": 3, ""awareness"": 0.2, ""goals_done"": {""first_sale"": true, ""ship10"": true}, ""brand_named"": true,
  ""ending_seen"": false, ""brand_name"": ""AltShop"", ""brand_logo_index"": 1, ""brand_color"": [0.2, 0.5, 0.9, 1],
  ""traveling_deliveries"": [{""product"": ""led"", ""quantity"": 50, ""quality"": 1, ""arrive_at"": 6500, ""van_sent"": false}],
  ""dock_crates"": [{""kind"": 1, ""product"": ""huelle"", ""quantity"": 20, ""quality"": 1, ""price"": 0, ""created"": 0, ""color"": [0.9, 0.3, 0.3, 1], ""logo"": 0, ""table"": 0}],
  ""stock"": {""huelle"": {""qty"": 30, ""quality"": 1}, ""led"": {""qty"": 12, ""quality"": 0.6}},
  ""express_delivery"": false, ""blocked_suppliers"": {}, ""damaged_next_crate"": 0,
  ""order_queue"": [{""product"": ""huelle"", ""price"": 20, ""created"": 6400}, {""product"": ""led"", ""price"": 36, ""created"": 6450}],
  ""packed_packages"": [{""kind"": 3, ""product"": ""huelle"", ""quantity"": 0, ""quality"": 1, ""price"": 20, ""created"": 6300, ""color"": [0.9, 0.3, 0.3, 1], ""logo"": 2, ""table"": 0}],
  ""packaging"": [12, 5, 0], ""flat_packaging"": [0, 3, 0],
  ""packaging_brand"": [{""color"": [0.9, 0.3, 0.3, 1], ""logo"": 0}, {""color"": [0.9, 0.3, 0.3, 1], ""logo"": 0}, {""color"": [0.9, 0.3, 0.3, 1], ""logo"": 0}],
  ""listed"": {""huelle"": true, ""led"": true}, ""shop_prices"": {""huelle"": 20, ""led"": 36}, ""promo_active"": false,
  ""shop_offline_until"": -1, ""boosts"": [], ""tiktok_ready_at"": 0, ""staff"": [], ""social_posted_day"": 0, ""conveyor_queue"": [],
  ""world_items"": [{""item"": {""kind"": 2, ""product"": ""led"", ""quantity"": 0, ""quality"": 1, ""price"": 36, ""created"": 6420, ""color"": [0.9, 0.3, 0.3, 1], ""logo"": 0, ""table"": 0}, ""pos"": [1, 0, 2], ""rot"": 0}],
  ""player_state"": null, ""debt"": 0, ""stand_stock"": {""huelle"": 5}, ""stand_sold_total"": 7, ""total_shipped"": 150,
  ""total_earned"": 4200, ""total_lost_orders"": 3, ""shipped_per_product"": {""huelle"": 120, ""led"": 30},
  ""daily"": {""revenue"": 120, ""purchases"": 40, ""packaging"": 0, ""marketing"": 0, ""other"": 0, ""income_other"": 0, ""shipped"": 6, ""lost"": 0, ""orders"": 7, ""trading"": 0, ""stand"": 0, ""rep_start"": 4.0, ""xp_start"": 850},
  ""history"": [{""day"": 9, ""revenue"": 600, ""profit"": 400, ""shipped"": 20}],
  ""market"": {},
  ""events"": {""mails"": [{""id"": 1, ""event"": """", ""title"": ""Viel Erfolg, Schatz!"", ""sender"": ""Mama"", ""icon"": ""heart"", ""text"": ""Hallo"", ""day"": 1, ""time"": 480, ""read"": true, ""pending"": false, ""result"": """", ""chosen"": """", ""choices"": []}], ""scheduled"": [], ""next_id"": 2, ""last_seen"": {}},
  ""saved_unix"": 1758800000
}";

        [Test]
        public void Load_V3SaveGetsSensibleDefaults()
        {
            Assert.IsTrue(Json.TryParse(V3Json, out object parsed), "v3-JSON ist gültig");
            var gm = new Sim(12) { SaveDir = _dir };
            gm.FromJson((Dictionary<string, object>)parsed);
            // Alte Werte
            Assert.IsTrue(gm.Money == 2345 && gm.Day == 10 && gm.Level == 3 && gm.BrandName == "AltShop", "Altes bleibt erhalten");
            Assert.AreEqual(2, gm.Weekday, "Tag 10 ist ein Mittwoch");
            Assert.AreEqual(4.1f, gm.Reputation, 0.001f);
            // Bestellzettel bekommen Nummer, Kundschaft und Frist
            Assert.AreEqual(2, gm.OrderQueue.Count);
            var ids = new HashSet<int>();
            foreach (var o in gm.OrderQueue)
            {
                Assert.IsTrue(o.Id >= GameData.FirstOrderId && ids.Add(o.Id), "Eindeutige Nummer");
                Assert.IsFalse(string.IsNullOrEmpty(o.Customer) || string.IsNullOrEmpty(o.City) || string.IsNullOrEmpty(o.Note), "Kundschaft ergänzt");
                Assert.AreEqual(o.Created + GameData.OrderDueMinutes, o.DueAt, 0.01f, "Standard-Frist");
                Assert.IsTrue(!o.Express && o.Stage == OrderStage.Queued, "Normale, wartende Bestellung");
            }
            Assert.IsTrue(gm.NextOrderId > Math.Max(gm.OrderQueue[0].Id, gm.OrderQueue[1].Id), "Nächste Nummer passt");
            Assert.AreEqual(0, gm.OrdersInWork.Count, "Keine angefangenen Zettel");
            Assert.AreEqual(1, gm.WorldItems.Count, "Abgelegter Artikel bleibt");
            // Neue Systeme mit Standardwerten
            Assert.AreEqual(0, gm.Skills.Count, "Noch keine Skills");
            Assert.AreEqual(3, gm.SkillPointsAvailable(), "Level 3 = 3 Skillpunkte zum Verteilen");
            Assert.AreEqual(3, gm.Challenges.Count, "Wochenziele für die laufende Woche");
            Assert.AreEqual(gm.Week, gm.ChallengeWeek);
            Assert.IsTrue(gm.DockReturns.Count == 0 && gm.ReturnsIncoming.Count == 0 && gm.TotalReturns == 0, "Keine Retouren");
            Assert.AreEqual(0, gm.Contracts.Count, "Keine Aufträge");
            foreach (var p in GameData.Products)
            {
                float h = gm.TrendMult(p.Id);
                Assert.IsTrue(h >= GameData.TrendMin && h <= GameData.TrendMax, "Trend gültig: " + p.Id);
            }
            Assert.AreEqual(0, gm.Daily.Returns, "Tageswerte ohne neue Felder");
            Assert.AreEqual(0, gm.History[0].Returns);
            // Alte Pakete lassen sich normal verschicken
            var pkg = gm.PickupPackage();
            Assert.AreEqual(0, pkg.OrderId, "Altes Paket ohne Zettel-Nummer");
            Assert.AreEqual(-1, pkg.PackSize, "Kartongröße unbekannt → passend");
            pkg.Kind = ItemKind.Labeled;
            int shipped = gm.TotalShipped;
            gm.ShipPackage(pkg);
            Assert.AreEqual(shipped + 1, gm.TotalShipped, "Versand klappt");
            // Alter Artikel zurück ins Regal → wieder ein vollständiger Zettel
            var oldItem = gm.WorldItems[0].Item;
            gm.ReturnItem(oldItem);
            Assert.AreEqual(3, gm.OrderQueue.Count);
            Assert.IsTrue(gm.OrderQueue[0].Id > 0 && gm.OrderQueue[0].Customer != "", "Zettel ergänzt");
            // Die Datei-Variante (Menü) funktioniert ebenso
            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, "savegame_2.json"), V3Json);
            var sum = gm.ReadSummary(2);
            Assert.IsTrue(sum.Version == 3 && sum.Day == 10 && sum.Weekday == 2, "Menü-Vorschau für alten Spielstand");
            var gm3 = new Sim(1) { SaveDir = _dir };
            Assert.IsTrue(gm3.LoadGame(2), "Alter Spielstand lädt");
            Assert.AreEqual(3, gm3.Challenges.Count);
            gm3.Slot = 2;
            Assert.IsTrue(gm3.SaveGame(true), "... und wird als v4 gespeichert");
            Assert.AreEqual(4, gm3.ReadSummary(2).Version);
        }
    }
}
