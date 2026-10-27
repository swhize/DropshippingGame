using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>F2 Retouren: Wahrscheinlichkeit, Ankunft mit Erstattung, Retourenplatz, Statistik.</summary>
    public class ReturnsTests
    {
        private static ItemData Pkg(float quality, int packSize = 0) =>
            new ItemData { Kind = ItemKind.Labeled, Product = "huelle", Quality = quality, PackSize = packSize, Price = 20 };

        [Test]
        public void ReturnChance_DependsOnQualityPackagingLatenessAndSkill()
        {
            var gm = TestUtil.Fresh();
            float std = gm.ReturnChance(Pkg(1f));
            Assert.AreEqual(GameData.ReturnBaseChance, std, 0.0001f, "Basis 3 % bei Standard-Ware");
            Assert.IsTrue(gm.ReturnChance(Pkg(0.6f)) > std * 2.5f, "Billig-Ware kommt deutlich öfter zurück");
            Assert.IsTrue(gm.ReturnChance(Pkg(1.6f)) < std * 0.5f, "Premium kommt seltener zurück");
            Assert.IsTrue(gm.ReturnChance(Pkg(1f, 2)) > std, "Zu großer Karton erhöht das Risiko");
            Assert.IsTrue(gm.ReturnChance(Pkg(1f), true) > std, "Verspätung erhöht das Risiko");
            Assert.IsTrue(gm.ReturnChance(Pkg(0.3f, 2), true) <= GameData.ReturnMaxChance + 0.0001f, "Obergrenze");
            gm.Skills.Add("v_kulanz");
            Assert.AreEqual(std * 0.65f, gm.ReturnChance(Pkg(1f)), 0.0001f, "Skill 'Kundenflüsterer' senkt die Quote");
            Assert.IsTrue(gm.ExpectedReturnRate("huelle") > 0f, "Erwartete Quote für die Anzeige");
        }

        [Test]
        public void Shipping_ProducesReturnsAtPlausibleRates()
        {
            int Count(float quality)
            {
                var gm = TestUtil.Fresh(8);
                for (int i = 0; i < 500; i++) gm.ShipPackage(TestUtil.Labeled(gm, "huelle", false, quality));
                return gm.ReturnsIncoming.Count;
            }
            int cheap = Count(0.6f), std = Count(1f), premium = Count(1.6f);
            Assert.IsTrue(cheap >= 25 && cheap <= 75, "Billig ≈ 9 % (" + cheap + "/500)");
            Assert.IsTrue(std >= 5 && std <= 30, "Standard ≈ 3 % (" + std + "/500)");
            Assert.IsTrue(premium <= 12, "Premium ≈ 1 % (" + premium + "/500)");
            Assert.IsTrue(cheap > std && std > premium, "Qualität macht den Unterschied");
        }

        [Test]
        public void Return_ArrivesLaterWithRefundAtTheDock()
        {
            var gm = TestUtil.Fresh();
            gm.Money = 500;
            var arrived = new List<ItemData>();
            gm.ReturnArrived += r => arrived.Add(r);
            var pkg = new ItemData { Kind = ItemKind.Labeled, Product = "led", Price = 36, Quality = 1f, OrderId = 1234, Customer = "Uschi B.", City = "Bottrop" };
            var ret = gm.ScheduleReturn(pkg, 30f, "Passt nicht zu meinem Sternzeichen.");
            Assert.AreEqual(ItemKind.Return, ret.Kind, "Neuer Gegenstandstyp");
            Assert.AreEqual(1, gm.ReturnsIncoming.Count, "Unterwegs");
            gm.AdvanceMinutes(29f);
            Assert.AreEqual(0, gm.DockReturns.Count, "Noch nicht da");
            Assert.AreEqual(500, gm.Money, "Erstattung erst bei Ankunft");
            gm.AdvanceMinutes(2f);
            Assert.AreEqual(1, gm.DockReturns.Count, "Liegt am Wareneingang");
            Assert.AreEqual(464, gm.Money, "36 € erstattet");
            Assert.IsTrue(gm.Daily.Returns == 1 && gm.Daily.Refunds == 36, "Tagesstatistik");
            Assert.IsTrue(gm.TotalReturns == 1 && gm.TotalRefunds == 36 && gm.ReturnsPerProduct["led"] == 1, "Gesamtstatistik");
            Assert.AreEqual(1, arrived.Count, "ReturnArrived gemeldet");
            Assert.AreEqual("Uschi B.", gm.DockReturns[0].Customer, "Retoure kennt die Kundschaft");
            Assert.AreEqual("Passt nicht zu meinem Sternzeichen.", gm.DockReturns[0].Note, "... und den Grund");
            Assert.AreEqual("Retoure (LED-Lichterkette)", gm.DockReturns[0].Describe());
            Assert.IsTrue(gm.Events.Mails.Exists(m => m.Title == GameData.MailReturns[0]), "Erklärung im Postfach");
            gm.ScheduleReturn(pkg, 1f);
            gm.AdvanceMinutes(2f);
            Assert.AreEqual(1, gm.Events.Mails.FindAll(m => m.Title == GameData.MailReturns[0]).Count, "Erklärung nur einmal");
        }

        [Test]
        public void ReturnsStation_RestockAsBStockOrDispose()
        {
            var gm = TestUtil.Fresh();
            gm.Stock["huelle"].Qty = 10;
            gm.ScheduleReturn(Pkg(1f), 0f);
            gm.ScheduleReturn(Pkg(1f), 0f);
            gm.AdvanceMinutes(1f);
            Assert.AreEqual(2, gm.ReturnsAtDock, "Zwei Retouren da");
            var processed = new List<bool>();
            gm.ReturnProcessed += (r, restock) => processed.Add(restock);
            var r1 = gm.PickupReturn();
            Assert.AreEqual(1, gm.DockReturns.Count, "Aufgenommen");
            Assert.AreEqual(0.75f, gm.BStockQuality(r1), 0.001f, "B-Ware: Qualität sinkt");
            Assert.IsTrue(gm.ProcessReturn(r1, true), "Als B-Ware eingelagert");
            Assert.AreEqual(11, gm.StockQty("huelle"), "Zurück im Lager");
            Assert.AreEqual((10f + 0.75f) / 11f, gm.StockQuality("huelle"), 0.001f, "Lagerqualität sinkt leicht");
            var r2 = gm.PickupReturn();
            gm.PutBackReturn(r2);
            Assert.AreEqual(1, gm.DockReturns.Count, "Zurückgelegt");
            r2 = gm.PickupReturn();
            Assert.IsTrue(gm.ProcessReturn(r2, false), "Entsorgt");
            Assert.AreEqual(11, gm.StockQty("huelle"), "Entsorgen ändert das Lager nicht");
            Assert.IsTrue(gm.Daily.ReturnsRestocked == 1 && gm.Daily.ReturnsDisposed == 1, "Tagesstatistik");
            Assert.IsTrue(gm.TotalReturnsRestocked == 1 && gm.TotalReturnsDisposed == 1, "Gesamtstatistik");
            CollectionAssert.AreEqual(new[] { true, false }, processed, "ReturnProcessed gemeldet");
            Assert.IsFalse(gm.ProcessReturn(ItemData.Crate("huelle", 5, 1f), true), "Nur Retouren");
            Assert.IsNull(gm.PickupReturn(), "Nichts mehr da");

            gm.Stock["huelle"].Qty = gm.Capacity();
            var full = new ItemData { Kind = ItemKind.Return, Product = "huelle", Quality = 1f, Price = 20 };
            Assert.IsFalse(gm.ProcessReturn(full, true), "Volles Lager: keine B-Ware");
            Assert.IsTrue(gm.ProcessReturn(full, false), "... aber entsorgen geht");
        }

        [Test]
        public void Staff_LagerProcessesReturns()
        {
            var gm = TestUtil.Fresh();
            gm.LocationStage = 1;
            gm.Upgrades.Add("warehouse");
            gm.Level = 5;
            Assert.IsTrue(gm.Hire("lager"));
            gm.ScheduleReturn(Pkg(1f), 0f);
            gm.ScheduleReturn(Pkg(0.5f), 0f);
            gm.AdvanceMinutes(1f);
            for (int i = 0; i < 4; i++) gm.UpdateStaff(17f);
            Assert.AreEqual(0, gm.DockReturns.Count, "Lagerist:in bearbeitet Retouren");
            Assert.AreEqual(1, gm.TotalReturnsRestocked, "Gute Ware als B-Ware eingelagert");
            Assert.AreEqual(1, gm.TotalReturnsDisposed, "Schrott entsorgt");
        }

        [Test]
        public void ReturnWaveEvent_SendsReturnsBack()
        {
            var gm = TestUtil.Fresh();
            gm.Level = 2;
            gm.Day = 3;
            gm.TotalShipped = 20;
            gm.ShippedPerProduct["huelle"] = 20;
            Assert.IsTrue(gm.Events.Eligible(EventData.Find("retourenwelle")), "Ereignis möglich");
            var mail = gm.Events.Trigger("retourenwelle");
            Assert.IsTrue(mail.Pending, "Entscheidung");
            string res = gm.Events.Choose(mail.Id, 1);
            Assert.AreEqual(3, gm.ReturnsIncoming.Count, "Drei Retouren unterwegs");
            Assert.IsTrue(res.Contains("3 Retouren"), "Ergebnis nennt die Retouren");
            int money = gm.Money;
            gm.AdvanceMinutes(150f);
            Assert.AreEqual(3, gm.DockReturns.Count, "Alle angekommen");
            Assert.IsTrue(gm.Money < money, "Erstattungen abgebucht");
        }

        [Test]
        public void ReturnRates_AreTracked()
        {
            var gm = TestUtil.Fresh();
            gm.ShippedPerProduct["huelle"] = 50;
            gm.TotalShipped = 50;
            gm.ScheduleReturn(Pkg(1f), 0f);
            gm.AdvanceMinutes(1f);
            Assert.AreEqual(0.02f, gm.ReturnRate("huelle"), 0.0001f, "Quote je Produkt");
            Assert.AreEqual(0.02f, gm.ReturnRateTotal(), 0.0001f, "Gesamtquote");
            Assert.AreEqual(0f, gm.ReturnRate("led"), "Keine Retouren");
        }
    }
}
