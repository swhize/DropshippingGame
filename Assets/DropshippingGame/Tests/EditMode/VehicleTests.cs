using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Moped: Heckbox-Regeln, Fahrphysik-Formeln, Spielstand (abwärtskompatibel), Hinweis-Nachricht, Klänge.</summary>
    public class VehicleTests
    {
        private static ItemData Item(ItemKind kind) => new ItemData { Kind = kind, Product = "", Price = 10 };

        [Test]
        public void Box_TakesFourParcelsOrOneCrate()
        {
            var def = VehicleRules.Def(VehicleRules.StarterId);
            var box = new List<ItemData>();
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(VehicleRules.CanLoad(box, Item(i % 2 == 0 ? ItemKind.Labeled : ItemKind.Package), def));
                box.Add(Item(ItemKind.Labeled));
            }
            Assert.IsFalse(VehicleRules.CanLoad(box, Item(ItemKind.Labeled), def), "5. Paket passt nicht");
            Assert.IsFalse(VehicleRules.CanLoad(new List<ItemData> { Item(ItemKind.Package) }, Item(ItemKind.Crate), def), "Kiste nur in leere Box");

            var crateBox = new List<ItemData> { Item(ItemKind.Crate) };
            Assert.IsTrue(VehicleRules.CanLoad(new List<ItemData>(), Item(ItemKind.Crate), def));
            Assert.IsFalse(VehicleRules.CanLoad(crateBox, Item(ItemKind.Package), def), "Kiste belegt die ganze Box");
            Assert.AreEqual(0, VehicleRules.FreeParcelSlots(crateBox, def));
            Assert.AreEqual(4, VehicleRules.FreeParcelSlots(new List<ItemData>(), def));
        }

        [Test]
        public void Box_RejectsPlatesAndLooseItems()
        {
            var def = VehicleRules.Def(VehicleRules.StarterId);
            Assert.IsFalse(VehicleRules.CanLoad(new List<ItemData>(), Item(ItemKind.Plate), def));
            Assert.IsFalse(VehicleRules.CanLoad(new List<ItemData>(), Item(ItemKind.Item), def));
            Assert.IsFalse(VehicleRules.CanLoad(new List<ItemData>(), null, def));
            Assert.IsTrue(VehicleRules.CanLoad(new List<ItemData>(), Item(ItemKind.Return), def), "Retouren dürfen mit");
        }

        [Test]
        public void Def_UnknownIdFallsBackToStarter()
        {
            Assert.AreEqual(VehicleRules.StarterId, VehicleRules.Def("gibtsnicht").Id);
            Assert.AreEqual(35f, VehicleRules.Def(null).MaxKmh, 0.001f);
        }

        [Test]
        public void Speed_ReachesButNeverExceedsMaxKmh()
        {
            var def = VehicleRules.Def(VehicleRules.StarterId);
            float v = 0f;
            float t10 = -1f;
            for (int i = 0; i < 60 * 30; i++)
            {
                v = VehicleRules.StepSpeed(v, 1f, 0f, 1f / 60f, def);
                if (t10 < 0f && VehicleRules.MsToKmh(v) >= 30f) t10 = i / 60f;
                Assert.LessOrEqual(VehicleRules.MsToKmh(v), 35.001f);
            }
            Assert.Greater(VehicleRules.MsToKmh(v), 34f, "Vollgas erreicht ~35 km/h");
            Assert.Greater(t10, 3f, "nicht sofort");
            Assert.Less(t10, 12f, "aber auch nicht ewig");
        }

        [Test]
        public void Speed_BrakeStopsThenReversesSlowly()
        {
            var def = VehicleRules.Def(VehicleRules.StarterId);
            float v = VehicleRules.KmhToMs(30f);
            float t = 0f;
            while (v > 0f && t < 10f)
            {
                v = VehicleRules.StepSpeed(v, 0f, 1f, 1f / 60f, def);
                t += 1f / 60f;
            }
            Assert.Less(t, 2f, "Bremsen aus 30 km/h in unter 2 s");
            for (int i = 0; i < 300; i++) v = VehicleRules.StepSpeed(v, 0f, 1f, 1f / 60f, def);
            Assert.Less(v, -1f, "rückwärts schieben");
            Assert.GreaterOrEqual(v, -1.6f, "nur langsam");
            // Ausrollen ohne Eingabe: steht irgendwann
            v = 5f;
            for (int i = 0; i < 60 * 40; i++) v = VehicleRules.StepSpeed(v, 0f, 0f, 1f / 60f, def);
            Assert.AreEqual(0f, v, 0.0001f);
        }

        [Test]
        public void Steering_TighterWhenSlow_LeanIntoCurve()
        {
            Assert.Greater(VehicleRules.SteerLimitDeg(0f), VehicleRules.SteerLimitDeg(1f));
            float yaw = VehicleRules.YawRateDeg(8f, 10f);
            Assert.Greater(yaw, 0f, "rechts lenken = nach rechts drehen");
            Assert.Greater(VehicleRules.LeanDeg(8f, yaw), 0f, "in die Kurve legen");
            Assert.LessOrEqual(VehicleRules.LeanDeg(30f, 400f), 28f);
            Assert.Less(VehicleRules.YawRateDeg(-1f, 10f), 0f, "rückwärts dreht andersherum");
            Assert.Greater(VehicleRules.EnginePitch(1f, 1f), VehicleRules.EnginePitch(0f, 0f));
        }

        [Test]
        public void Sim_LoadTakeAndShipFromBox()
        {
            var sim = TestUtil.Fresh();
            var pid = GameData.Products[0].Id;
            var a = TestUtil.Labeled(sim, pid);
            var b = TestUtil.Labeled(sim, pid);
            var plain = Item(ItemKind.Package);
            Assert.IsTrue(sim.LoadIntoMoped(a));
            Assert.IsTrue(sim.LoadIntoMoped(plain));
            Assert.IsTrue(sim.LoadIntoMoped(b));
            Assert.AreEqual(2, sim.CountInMoped(ItemKind.Labeled));
            var shipped = sim.TakeLabeledFromMoped();
            Assert.AreEqual(2, shipped.Count);
            Assert.AreEqual(1, sim.Moped.Box.Count, "unetikettiertes Paket bleibt drin");
            Assert.AreSame(plain, sim.TakeFromMoped());
            Assert.IsNull(sim.TakeFromMoped());
        }

        [Test]
        public void Save_RoundTripsPositionAndBox()
        {
            var sim = TestUtil.Fresh();
            var pid = GameData.Products[0].Id;
            var pkg = TestUtil.Labeled(sim, pid);
            sim.LoadIntoMoped(pkg);
            sim.Moped.Placed = true;
            sim.Moped.Pos = new V3(12.5f, 0.02f, -5.5f);
            sim.Moped.RotY = 77f;
            sim.Moped.HintSent = true;
            var text = Json.Write(sim.ToJson());
            Assert.IsTrue(Json.TryParse(text, out object o));
            var loaded = TestUtil.Fresh(3);
            loaded.FromJson((Dictionary<string, object>)o);
            Assert.IsTrue(loaded.Moped.Placed);
            Assert.AreEqual(12.5f, loaded.Moped.Pos.x, 0.001f);
            Assert.AreEqual(-5.5f, loaded.Moped.Pos.z, 0.001f);
            Assert.AreEqual(77f, loaded.Moped.RotY, 0.001f);
            Assert.IsTrue(loaded.Moped.HintSent);
            Assert.AreEqual(1, loaded.Moped.Box.Count);
            Assert.AreEqual(pkg.OrderId, loaded.Moped.Box[0].OrderId);
            Assert.AreEqual(ItemKind.Labeled, loaded.Moped.Box[0].Kind);
        }

        [Test]
        public void Save_OldSaveWithoutMopedLoadsWithParkedEmptyMoped()
        {
            var sim = TestUtil.Fresh();
            sim.Moped.Placed = true;
            sim.Moped.Box.Add(Item(ItemKind.Package));
            var state = sim.ToJson();
            state.Remove("moped");
            var loaded = TestUtil.Fresh(2);
            loaded.Moped.Placed = true; // Reste eines vorigen Spiels dürfen nicht übrig bleiben
            loaded.FromJson(state);
            Assert.IsFalse(loaded.Moped.Placed, "alter Spielstand: Moped am Stammplatz");
            Assert.AreEqual(0, loaded.Moped.Box.Count);
            Assert.AreEqual(VehicleRules.StarterId, loaded.Moped.Id);
            Assert.IsFalse(loaded.Moped.HintSent);
        }

        [Test]
        public void Save_BrokenMopedDataIsSanitized()
        {
            var d = new Dictionary<string, object>
            {
                { "id", "" }, { "placed", true }, { "pos", new List<object> { double.NaN, 0.0, 1.0 } }, { "rot", 5.0 },
                { "box", new List<object> { "kaputt", new ItemData { Kind = ItemKind.Plate }.ToJson(), new ItemData { Kind = ItemKind.Labeled }.ToJson() } },
            };
            var v = VehicleState.FromJson(d);
            Assert.IsFalse(v.Placed, "NaN-Position -> Stammplatz");
            Assert.AreEqual(VehicleRules.StarterId, v.Id);
            Assert.AreEqual(1, v.Box.Count, "nur gültige Pakete bleiben");
            Assert.IsNotNull(VehicleState.FromJson(null));
        }

        [Test]
        public void Orders_InBoxAreNotPrunedAsOrphans()
        {
            var sim = TestUtil.Fresh();
            var pid = GameData.Products[0].Id;
            var order = sim.CreateOrder(pid, false);
            order.Stage = OrderStage.Picked;
            sim.OrdersInWork.Add(order);
            var pkg = order.ToItem(1f);
            pkg.Kind = ItemKind.Labeled;
            sim.LoadIntoMoped(pkg);
            Assert.IsTrue(sim.OrdersInWork.Exists(o => o.Id == pkg.OrderId));
            // Zweimal prüfen (erst Verdacht, dann Entfernen) – der Zettel muss bleiben.
            sim.PruneOrdersInWork(new List<int>());
            sim.PruneOrdersInWork(new List<int>());
            Assert.IsTrue(sim.OrdersInWork.Exists(o => o.Id == pkg.OrderId), "Paket in der Box hält den Zettel am Leben");

            var loaded = TestUtil.Fresh(4);
            loaded.FromJson(sim.ToJson());
            Assert.IsTrue(loaded.OrdersInWork.Exists(o => o.Id == pkg.OrderId), "auch nach dem Laden");
        }

        [Test]
        public void Hint_OnlyAfterTutorialAndOnce()
        {
            var sim = TestUtil.Fresh();
            sim.TutorialStep = 0;
            Assert.IsFalse(sim.SendMopedHint("E"), "nicht während des Tutorials");
            sim.TutorialStep = -1;
            int mails = sim.Events.Mails.Count;
            Assert.IsTrue(sim.SendMopedHint("E"));
            Assert.AreEqual(mails + 1, sim.Events.Mails.Count);
            Assert.IsFalse(sim.SendMopedHint("E"), "nur einmal");
            sim.ResetState();
            Assert.IsFalse(sim.Moped.HintSent, "neues Spiel: wieder offen");
        }

        [Test]
        public void Synth_ClipsAreValidAndEngineLoopsSeamlessly()
        {
            foreach (var name in new[] { "moped_engine", "moped_horn", "moped_bump" })
            {
                var b = VehicleSynth.Render(name);
                Assert.Greater(b.Length, 1000, name);
                float peak = 0f;
                foreach (float v in b)
                {
                    Assert.IsFalse(float.IsNaN(v), name);
                    peak = Math.Max(peak, Math.Abs(v));
                }
                Assert.LessOrEqual(peak, 1f, name);
                Assert.Greater(peak, 0.3f, name);
            }
            var e = VehicleSynth.EngineLoop();
            Assert.Less(Math.Abs(e[e.Length - 1] - e[0]), 0.2f, "Loop-Naht ohne Knacken");
            CollectionAssert.AreEqual(e, VehicleSynth.EngineLoop(), "deterministisch");
        }
    }
}
