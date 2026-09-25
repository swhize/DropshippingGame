using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>F1 Bestellzettel: Nummer, Kundschaft, Fälligkeit, Express, Bearbeitungsstand.</summary>
    public class OrderTicketTests
    {
        [Test]
        public void Ticket_HasNumberCustomerNoteAndDueTime()
        {
            var gm = TestUtil.Fresh();
            gm.SpawnOrder("huelle");
            var o = gm.OrderQueue[0];
            Assert.IsTrue(o.Id >= GameData.FirstOrderId, "Nummer ab " + GameData.FirstOrderId);
            Assert.AreEqual("#" + o.Id, o.Number, "Anzeige-Nummer");
            Assert.IsFalse(string.IsNullOrEmpty(o.Customer) || string.IsNullOrEmpty(o.City) || string.IsNullOrEmpty(o.Note), "Kundschaft, Ort und Notiz");
            Assert.AreEqual(o.Created + GameData.OrderDueMinutes, o.DueAt, 0.01f, "Fälligkeit 4 Spielstunden");
            Assert.AreEqual(o.Created + GameData.OrderExpireMinutes, o.ExpiresAt, 0.01f, "Storno nach 12 Stunden");
            Assert.IsFalse(o.Express, "Auf Level 1 gibt es kein Express");
            Assert.AreEqual(OrderStage.Queued, o.Stage, "Wartet");
            Assert.AreEqual(1, gm.Tickets().Count, "Zettel in der Übersicht");
            Assert.AreSame(o, gm.FindOrder(o.Id), "Zettel auffindbar");
            gm.SpawnOrder("huelle");
            Assert.AreEqual(o.Id + 1, gm.OrderQueue[1].Id, "Nummern laufen fortlaufend");
        }

        [Test]
        public void Express_FromLevel2WithShorterDeadlineAndHigherPrice()
        {
            var gm = TestUtil.Fresh(3);
            int express = 0;
            for (int i = 0; i < 200; i++)
            {
                gm.SpawnOrder("huelle");
                if (gm.OrderQueue[0].Express) express++;
                gm.OrderQueue.Clear();
            }
            Assert.AreEqual(0, express, "Level 1: nie Express");
            Assert.AreEqual(0f, gm.ExpressChance(), "Keine Express-Chance auf Level 1");
            foreach (int level in new[] { 2, 5, 10 })
            {
                gm.Level = level;
                Assert.IsTrue(gm.ExpressChance() >= 0.15f && gm.ExpressChance() <= 0.25f, "Express-Chance 15-25 % (Level " + level + ")");
            }
            gm.Level = 4;
            express = 0;
            const int n = 600;
            for (int i = 0; i < n; i++)
            {
                gm.SpawnOrder("huelle");
                if (gm.OrderQueue[0].Express) express++;
                gm.OrderQueue.Clear();
            }
            float ratio = express / (float)n;
            Assert.IsTrue(ratio >= 0.12f && ratio <= 0.28f, "Etwa jede fünfte Bestellung ist Express (" + ratio + ")");
            int basePrice = gm.CurrentSalePrice("huelle");
            var e = gm.SpawnOrder("huelle", true);
            Assert.AreEqual(Mathx.RoundToInt(basePrice * GameData.ExpressPriceMult), e.Price, "Express kostet 40 % mehr");
            Assert.AreEqual(GameData.ExpressDueMinutes, e.DueAt - e.Created, 0.01f, "Kürzere Frist");
            Assert.AreEqual(e.Created + GameData.ExpressExpireMinutes, e.ExpiresAt, 0.01f, "Express-Zettel verfallen früher");
            Assert.IsTrue(gm.Events.Mails.Exists(m => m.Title == GameData.MailExpress[0]), "Erklärung zu Express im Postfach");
        }

        [Test]
        public void Ticket_TravelsWithItemPackageAndLabel()
        {
            var gm = TestUtil.Fresh();
            gm.Stock["huelle"].Qty = 5;
            int shipped = 0;
            bool onTime = false;
            gm.OrderShipped += (o, reward, punctual) =>
            {
                shipped++;
                onTime = punctual;
            };
            int changes = 0;
            gm.OrdersChanged += () => changes++;
            var order = gm.SpawnOrder("huelle", false);
            var item = gm.PickItem("huelle");
            Assert.AreEqual(order.Id, item.OrderId, "Artikel trägt die Zettel-Nummer");
            Assert.AreEqual(order.Customer, item.Customer, "... und die Kundschaft");
            Assert.AreEqual(order.DueAt, item.DueAt, 0.001f, "... und die Fälligkeit");
            Assert.AreEqual(0, gm.PendingCount(), "Nicht mehr in der Warteschlange");
            Assert.AreEqual(OrderStage.Picked, order.Stage, "Stand: entnommen");
            Assert.IsTrue(order.InWork && gm.OrdersInWork.Contains(order), "Zettel ist in Arbeit");
            Assert.AreEqual(1, gm.Tickets().Count, "Zettel bleibt sichtbar, solange er in Arbeit ist");
            Assert.IsTrue(gm.WrapItem(item), "Verpackt");
            Assert.AreEqual(OrderStage.Packed, order.Stage, "Stand: verpackt");
            var pkg = gm.PickupPackage();
            Assert.AreEqual(order.Id, pkg.OrderId, "Paket trägt die Zettel-Nummer");
            Assert.AreEqual(0, pkg.PackSize, "Passender Karton S");
            Assert.IsFalse(pkg.Oversized, "Karton passt");
            var labeled = pkg.Clone();
            labeled.Kind = ItemKind.Labeled;
            gm.OnLabeled(labeled);
            Assert.AreEqual(OrderStage.Labeled, order.Stage, "Stand: etikettiert");
            gm.ShipPackage(labeled);
            Assert.AreEqual(0, gm.OrdersInWork.Count, "Zettel erledigt");
            Assert.AreEqual(0, gm.Tickets().Count, "Keine offenen Zettel mehr");
            Assert.IsTrue(shipped == 1 && onTime, "OrderShipped meldet pünktlichen Versand");
            Assert.IsTrue(changes >= 4, "OrdersChanged bei jedem Schritt");
        }

        [Test]
        public void ReturnItem_RestoresSameTicket()
        {
            var gm = TestUtil.Fresh();
            gm.Stock["huelle"].Qty = 3;
            var order = gm.SpawnOrder("huelle", false);
            var item = gm.PickItem("huelle");
            gm.ReturnItem(item);
            Assert.AreEqual(1, gm.PendingCount(), "Zettel wartet wieder");
            Assert.AreSame(order, gm.OrderQueue[0], "Derselbe Zettel");
            Assert.AreEqual(OrderStage.Queued, order.Stage, "Stand zurückgesetzt");
            Assert.AreEqual(0, gm.OrdersInWork.Count, "Nicht mehr in Arbeit");
            Assert.AreEqual(3, gm.StockQty("huelle"), "Ware zurück im Regal");
        }

        [Test]
        public void PickItem_TakesMostUrgentTicket()
        {
            var gm = TestUtil.Fresh();
            gm.Level = 2;
            gm.Stock["huelle"].Qty = 5;
            var normal = gm.SpawnOrder("huelle", false);
            gm.AdvanceMinutes(10f);
            var express = gm.SpawnOrder("huelle", true);
            Assert.IsTrue(express.DueAt < normal.DueAt, "Express ist früher fällig");
            var item = gm.PickItem("huelle");
            Assert.AreEqual(express.Id, item.OrderId, "Das Regal gibt den dringendsten Zettel zuerst");
            Assert.AreEqual(0, gm.ExpressPendingCount(), "Express-Zettel wartet nicht mehr");
        }

        [Test]
        public void LateShipments_GiveWorseReviews_ExpressCountsMore()
        {
            float Rep(bool late, bool express)
            {
                var gm = TestUtil.Fresh(21);
                gm.Level = 2;
                for (int i = 0; i < 150; i++)
                {
                    var pkg = TestUtil.Labeled(gm, "huelle", express);
                    if (late)
                    {
                        float shift = 3f * (pkg.DueAt - pkg.Created);
                        pkg.Created -= shift;
                        pkg.DueAt -= shift;
                    }
                    gm.ShipPackage(pkg);
                }
                return gm.Reputation;
            }
            float onTime = Rep(false, false), late = Rep(true, false), expressOnTime = Rep(false, true), expressLate = Rep(true, true);
            Assert.IsTrue(onTime > late + 1f, "Verspätung kostet Sterne (" + onTime + " vs. " + late + ")");
            Assert.IsTrue(expressOnTime >= onTime - 0.05f, "Pünktliches Express wird gefeiert (" + expressOnTime + ")");
            Assert.IsTrue(expressLate < late, "Verspätetes Express wird härter bestraft (" + expressLate + " vs. " + late + ")");
        }

        [Test]
        public void ExpressOrders_ExpireSooner()
        {
            var gm = TestUtil.Fresh();
            gm.Level = 2;
            var expired = new List<Order>();
            gm.OrderExpired += o => expired.Add(o);
            var normal = gm.SpawnOrder("huelle", false);
            var express = gm.SpawnOrder("huelle", true);
            float age = GameData.ExpressExpireMinutes + 1f;
            normal.Created -= age;
            express.Created -= age;
            gm.CheckExpiryNow();
            Assert.AreEqual(1, gm.OrderQueue.Count, "Nur die Express-Bestellung ist storniert");
            Assert.AreSame(normal, gm.OrderQueue[0], "Normale Bestellung wartet noch");
            Assert.IsTrue(expired.Count == 1 && expired[0] == express, "OrderExpired gemeldet");
            Assert.AreEqual(1, gm.Daily.Expired, "Statistik");
            Assert.AreEqual(1, gm.TotalLostOrders, "Zählt als verloren");
        }

        [Test]
        public void NewProduct_GetsFirstOrderWithinAboutOneRealMinute()
        {
            var gm = TestUtil.Fresh(4);
            gm.SetShopPrice("huelle", 999);
            float t0 = gm.BClock();
            float firstAt = -1f;
            gm.OrderReceived += o =>
            {
                if (firstAt < 0f) firstAt = gm.BClock();
            };
            gm.SetListed("huelle", true);
            for (int i = 0; i < 90 && firstAt < 0f; i++) gm.AdvanceMinutes(1f);
            Assert.IsTrue(firstAt >= 0f, "Erste Bestellung kommt trotz Wucherpreis (Neuheiten-Bonus)");
            float after = firstAt - t0;
            Assert.IsTrue(after <= GameData.LaunchOrderMax + 1f && after <= 80f, "Innerhalb ~1 echter Minute (" + after + " Spielminuten)");
            Assert.AreEqual(0, gm.LaunchOrders.Count, "Nur einmal");
        }

        [Test]
        public void Countdown_TextsAndClock()
        {
            Assert.AreEqual("45 min", Fmt.Duration(45f));
            Assert.AreEqual("1:05 h", Fmt.Duration(65f));
            Assert.AreEqual("0 min", Fmt.Duration(0f));
            var gm = TestUtil.Fresh();
            var o = gm.SpawnOrder("huelle", false);
            Assert.AreEqual("4:00 h", gm.OrderTimeLeftText(o), "Countdown");
            Assert.AreEqual("12:00", gm.BClockText(o.DueAt), "Fällig heute 12:00");
            Assert.AreEqual("Di 08:30", gm.BClockText(GameData.BusinessMinutes + 30f), "Fälligkeit am nächsten Tag mit Wochentag");
            gm.AdvanceMinutes(250f);
            Assert.IsTrue(gm.OrderOverdue(o) && gm.OrderUrgency(o) > 1f, "Überfällig");
            Assert.IsTrue(gm.OrderTimeLeftText(o).StartsWith("überfällig"), "Countdown zeigt Verspätung");
            Assert.AreEqual(1, gm.OverdueCount(), "Überfällige Zettel zählen");
        }

        [Test]
        public void Packing_FallsBackToLargerCartonWithReturnRisk()
        {
            var gm = TestUtil.Fresh();
            gm.Packaging = new[] { 0, 3, 0 };
            gm.Stock["huelle"].Qty = 2;
            gm.SpawnOrder("huelle", false);
            var item = gm.PickItem("huelle");
            Assert.AreEqual(1, gm.CartonFor("huelle"), "Nächstgrößerer Karton");
            Assert.IsTrue(gm.WrapItem(item), "Verpacken klappt trotzdem");
            Assert.AreEqual(2, gm.Packaging[1], "Karton M verbraucht");
            var pkg = gm.PickupPackage();
            Assert.IsTrue(pkg.PackSize == 1 && pkg.Oversized && pkg.EffectivePackSize == 1, "Paket steckt im zu großen Karton");
            var fit = pkg.Clone();
            fit.PackSize = 0;
            Assert.IsTrue(gm.ReturnChance(pkg) > gm.ReturnChance(fit), "Zu großer Karton → mehr Retouren");
        }

        [Test]
        public void QuickReorder_RepeatsLastPurchase()
        {
            var gm = TestUtil.Fresh();
            gm.Money = 1000;
            CollectionAssert.AreEqual(new[] { 1, 1 }, gm.QuickReorderPlan("huelle"), "Standard: 50 Stück beim Großhändler");
            Assert.IsTrue(gm.BuyBulk(0, 0, 0), "Einkauf");
            CollectionAssert.AreEqual(new[] { 0, 0 }, gm.QuickReorderPlan("huelle"), "Merkt sich den letzten Einkauf");
            int cost = gm.QuickReorderCost("huelle");
            int money = gm.Money;
            Assert.IsTrue(gm.QuickReorder("huelle"), "Schnell nachbestellt");
            Assert.AreEqual(money - cost, gm.Money, "Kosten wie angezeigt");
            Assert.AreEqual(20, gm.TravelingDeliveries[1].Quantity, "Gleiche Menge");
            Assert.IsFalse(gm.QuickReorder("gibtsnicht"), "Unbekanntes Produkt");
        }

        [Test]
        public void DiscardItem_CancelsTheTicket()
        {
            var gm = TestUtil.Fresh();
            gm.Stock["huelle"].Qty = 2;
            var o = gm.SpawnOrder("huelle", false);
            var item = gm.PickItem("huelle");
            gm.DiscardItem(item);
            Assert.AreEqual(0, gm.Tickets().Count, "Zettel verschwunden");
            Assert.AreEqual(1, gm.TotalLostOrders, "Zählt als verloren");
            Assert.IsNull(gm.FindOrder(o.Id), "Nicht mehr auffindbar");
        }

        [Test]
        public void Staff_PackerKeepsTicketAndShipperPrefersUrgent()
        {
            var gm = TestUtil.Fresh();
            gm.LocationStage = 1;
            gm.Upgrades.Add("warehouse");
            gm.Level = 5;
            gm.Stock["huelle"].Qty = 5;
            Assert.IsTrue(gm.Hire("packer"));
            var normal = gm.SpawnOrder("huelle", false);
            var express = gm.SpawnOrder("huelle", true);
            gm.UpdateStaff(13f);
            Assert.AreEqual(1, gm.PackedCount(), "Ein Paket gepackt");
            Assert.AreEqual(express.Id, gm.PackedPackages[0].OrderId, "Express zuerst");
            Assert.AreEqual(OrderStage.Packed, express.Stage, "Zettel in Arbeit (verpackt)");
            gm.UpdateStaff(13f);
            Assert.IsTrue(gm.Hire("versand"));
            int shippedExpress = -1;
            gm.OrderShipped += (o, r, t) =>
            {
                if (shippedExpress < 0) shippedExpress = o.Id;
            };
            gm.UpdateStaff(11f);
            Assert.AreEqual(express.Id, shippedExpress, "Versandkraft verschickt das dringendste Paket zuerst");
            Assert.AreEqual(OrderStage.Packed, normal.Stage, "Normaler Zettel wartet noch verpackt");
        }
    }
}
