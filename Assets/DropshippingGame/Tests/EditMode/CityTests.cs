using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Stadt-Saat (pro Spielstand stabil) und PaketBlitz-Abholgebühren.</summary>
    public class CityTests
    {
        [Test]
        public void CitySeed_IsPositiveAndStableWithinGame()
        {
            var sim = TestUtil.Fresh();
            int a = sim.CitySeed;
            Assert.Greater(a, 0);
            Assert.AreEqual(a, sim.CitySeed);
        }

        [Test]
        public void CitySeed_RoundTripsThroughSave()
        {
            var sim = TestUtil.Fresh();
            sim.CitySeed = 123456;
            var text = Json.Write(sim.ToJson());
            Assert.IsTrue(Json.TryParse(text, out object o));
            var loaded = TestUtil.Fresh(2);
            loaded.FromJson((Dictionary<string, object>)o);
            Assert.AreEqual(123456, loaded.CitySeed);
        }

        [Test]
        public void CitySeed_OldSaveWithoutSeedGetsStableSeed()
        {
            var sim = TestUtil.Fresh();
            sim.BrandName = "KartonKönig";
            var state = sim.ToJson();
            state.Remove("city_seed");

            var a = TestUtil.Fresh();
            a.Slot = 2;
            a.FromJson(state);
            var b = TestUtil.Fresh(7);
            b.Slot = 2;
            b.FromJson(state);
            Assert.Greater(a.CitySeed, 0);
            Assert.AreEqual(a.CitySeed, b.CitySeed, "gleicher alter Spielstand -> gleiche Stadt");
            Assert.AreEqual(Sim.StableSeed("KartonKönig", 2), a.CitySeed);
        }

        [Test]
        public void CitySeed_ResetForNewGame()
        {
            var sim = TestUtil.Fresh();
            sim.CitySeed = 42;
            sim.ResetState();
            // Neue Saat wird lazy erzeugt (positiv), die alte ist weg.
            Assert.Greater(sim.CitySeed, 0);
            sim.CitySeed = 0;
            Assert.AreEqual(1, sim.CitySeed, "0 ist reserviert, Setter macht 1 daraus");
        }

        [Test]
        public void CitySeed_DoesNotConsumeSimRandom()
        {
            var a = new Sim(5);
            var b = new Sim(5);
            int unused = a.CitySeed;
            Assert.Greater(unused, 0);
            Assert.AreEqual(b.Rng.Value(), a.Rng.Value());
        }

        [Test]
        public void StableSeed_DiffersBySaltAndText()
        {
            Assert.AreNotEqual(Sim.StableSeed("A", 1), Sim.StableSeed("A", 2));
            Assert.AreNotEqual(Sim.StableSeed("A", 1), Sim.StableSeed("B", 1));
            Assert.AreEqual(Sim.StableSeed(null, 3), Sim.StableSeed("", 3));
            Assert.Greater(Sim.StableSeed("x", int.MinValue), 0);
        }

        [Test]
        public void PickupFee_PerParcelAndStage()
        {
            Assert.AreEqual(0, PostRules.PickupFee(0, 0, false));
            Assert.AreEqual(PostRules.GaragePickupFee * 3, PostRules.PickupFee(3, 0, false));
            Assert.AreEqual(PostRules.WarehousePickupFee * 2, PostRules.PickupFee(2, 1, false));
            Assert.AreEqual(0, PostRules.PickupFee(5, 0, true), "im Tutorial gratis");
            Assert.Less(PostRules.WarehousePickupFee, PostRules.GaragePickupFee);
        }

        [Test]
        public void PickupFee_IsSmallComparedToCheapestProduct()
        {
            int cheapest = int.MaxValue;
            foreach (var p in GameData.Products) cheapest = System.Math.Min(cheapest, p.RefPrice);
            Assert.LessOrEqual(PostRules.GaragePickupFee * 5, cheapest, "Abholung soll bequem, aber nicht ruinös sein");
        }

        [Test]
        public void TutorialActive_FollowsTutorialStep()
        {
            var sim = TestUtil.Fresh();
            Assert.IsFalse(PostRules.TutorialActive(sim));
            sim.TutorialStep = 0;
            Assert.IsTrue(PostRules.TutorialActive(sim));
            sim.TutorialStep = GameData.Tutorial.Length;
            Assert.IsFalse(PostRules.TutorialActive(sim));
            Assert.IsFalse(PostRules.TutorialActive(null));
        }
    }
}
