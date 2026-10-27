using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Bestellzettel im HUD: Storno überfälliger Zettel, verwaiste Zettel, Anzeige-Hilfen, mehrfaches Entnehmen.</summary>
    public class OrderHudTests
    {
        [Test]
        public void OverdueWaitingTicket_IsCancelledAfterGrace()
        {
            var gm = TestUtil.Fresh();
            var o = gm.SpawnOrder("huelle", false);
            Assert.Greater(o.ExpiresAt, o.DueAt, "Kulanz nach der Frist");
            Assert.LessOrEqual(o.ExpiresAt - o.DueAt, 180f, "Überfällige Zettel verschwinden nach spätestens 3 h");
            o.Created -= GameData.OrderDueMinutes + 1f;
            o.DueAt -= GameData.OrderDueMinutes + 1f;
            gm.CheckExpiryNow();
            Assert.IsTrue(gm.OrderOverdue(o) && gm.Tickets().Contains(o), "Kurz überfällig: bleibt noch sichtbar");
            o.Created -= GameData.OrderExpireMinutes;
            gm.CheckExpiryNow();
            Assert.IsFalse(gm.Tickets().Contains(o), "Storniert: verschwindet aus dem HUD");
            Assert.AreEqual(0, gm.OverdueCount());
        }

        [Test]
        public void Expiry_AlwaysAfterDueTime()
        {
            Assert.Greater(GameData.ExpressExpireMinutes, GameData.ExpressDueMinutes);
            Assert.Greater(GameData.OrderExpireMinutes, GameData.OrderDueMinutes);
        }

        [Test]
        public void OrphanedInWorkTicket_IsPrunedAfterTwoChecks()
        {
            var gm = TestUtil.Fresh();
            gm.Stock["huelle"].Qty = 5;
            gm.SpawnOrder("huelle", false);
            gm.SpawnOrder("huelle", false);
            var held = gm.PickItem("huelle");
            var lost = gm.PickItem("huelle");
            Assert.AreEqual(2, gm.OrdersInWork.Count);
            var alive = new List<int> { held.OrderId };
            Assert.AreEqual(0, gm.PruneOrdersInWork(alive), "Erster Aufruf: nur vorgemerkt");
            Assert.AreEqual(1, gm.PruneOrdersInWork(alive), "Zweiter Aufruf: verwaister Zettel entfernt");
            Assert.AreEqual(1, gm.OrdersInWork.Count);
            Assert.AreEqual(held.OrderId, gm.OrdersInWork[0].Id, "Gehaltener Artikel bleibt");
            Assert.IsNull(gm.FindOrder(lost.OrderId));
        }

        [Test]
        public void Prune_KeepsPackedAndReappearingTickets()
        {
            var gm = TestUtil.Fresh();
            gm.Stock["huelle"].Qty = 5;
            gm.Packaging[GameData.Product("huelle").Size] = 5;
            gm.SpawnOrder("huelle", false);
            gm.SpawnOrder("huelle", false);
            var a = gm.PickItem("huelle");
            var b = gm.PickItem("huelle");
            Assert.IsTrue(gm.WrapItem(a), "a liegt verpackt auf dem Packtisch");
            gm.PruneOrdersInWork(new int[0]);
            gm.PruneOrdersInWork(new[] { b.OrderId });
            gm.PruneOrdersInWork(new int[0]);
            Assert.AreEqual(2, gm.OrdersInWork.Count, "Paket auf dem Tisch zählt, b war zwischendurch wieder da");
        }

        [Test]
        public void SeveralOrders_EachPickFromShelfSucceeds()
        {
            var gm = TestUtil.Fresh();
            gm.Stock["huelle"].Qty = 10;
            for (int i = 0; i < 4; i++) gm.SpawnOrder("huelle", false);
            var ids = new HashSet<int>();
            for (int i = 0; i < 4; i++)
            {
                var it = gm.PickItem("huelle");
                Assert.IsNotNull(it, "Entnahme " + (i + 1));
                ids.Add(it.OrderId);
            }
            Assert.AreEqual(4, ids.Count, "Jeder Artikel gehört zu einem anderen Zettel");
            Assert.AreEqual(6, gm.StockQty("huelle"));
            Assert.AreEqual(0, gm.PendingCount());
        }

        [Test]
        public void StepHelpers_FollowTheWorkflow()
        {
            var gm = TestUtil.Fresh();
            var o = gm.CreateOrder("huelle", false);
            Assert.AreEqual("Holen", Sim.OrderNextStep(o));
            StringAssert.StartsWith("Regal", Sim.OrderNextPlace(o));
            o.Stage = OrderStage.Picked;
            Assert.AreEqual("Packtisch", Sim.OrderNextPlace(o));
            o.Stage = OrderStage.Packed;
            Assert.AreEqual("Labeldrucker", Sim.OrderNextPlace(o));
            o.Stage = OrderStage.Labeled;
            Assert.AreEqual(3, Sim.OrderStepIndex(o));
            Assert.AreEqual("Versand", Sim.OrderNextPlace(o));
        }

        [Test]
        public void TimeTone_GreenYellowRed()
        {
            var gm = TestUtil.Fresh();
            var o = gm.CreateOrder("huelle", false);
            Assert.AreEqual(0, gm.OrderTimeTone(o), "frisch = grün");
            float now = gm.BClock();
            o.Created = now - 168f;
            o.DueAt = now + 72f;
            Assert.AreEqual(1, gm.OrderTimeTone(o), "30 % übrig = gelb");
            o.DueAt = now - 1f;
            Assert.AreEqual(2, gm.OrderTimeTone(o), "überfällig = rot");
        }
    }
}
