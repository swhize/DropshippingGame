using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Admin-Panel: Testfunktionen müssen ohne Fehler alles freischalten.</summary>
    public class AdminTests
    {
        [Test]
        public void UnlockEverything_GivesMaxLevelUpgradesSkillsAndMoney()
        {
            var sim = TestUtil.Fresh();
            sim.Debt = 500;
            sim.AdminUnlockEverything();
            Assert.AreEqual(GameData.MaxLevel, sim.Level);
            Assert.AreEqual(1, sim.LocationStage);
            foreach (var u in GameData.Upgrades) Assert.IsTrue(sim.HasUpgrade(u.Id), u.Id);
            foreach (var s in GameData.Skills) Assert.IsTrue(sim.HasSkill(s.Id), s.Id);
            Assert.GreaterOrEqual(sim.SkillPointsAvailable(), 0);
            Assert.GreaterOrEqual(sim.Money, Sim.AdminMoneyFloor);
            Assert.AreEqual(0, sim.Debt);
            Assert.AreEqual("business", sim.StoryStage);
        }

        [Test]
        public void SetLevel_UpAndDown()
        {
            var sim = TestUtil.Fresh();
            sim.AdminSetLevel(5);
            Assert.AreEqual(5, sim.Level);
            sim.AdminSetLevel(2);
            Assert.AreEqual(2, sim.Level);
            sim.AdminSetLevel(99);
            Assert.AreEqual(GameData.MaxLevel, sim.Level);
        }

        [Test]
        public void InfiniteMoney_RefillsOnTick()
        {
            var sim = TestUtil.Fresh();
            sim.AdminInfiniteMoney = true;
            sim.Money = 5;
            sim.AdminTick();
            Assert.AreEqual(Sim.AdminMoneyFloor, sim.Money);
        }

        [Test]
        public void Deliver_PutsCrateOnDock()
        {
            var sim = TestUtil.Fresh();
            string pid = GameData.Products[0].Id;
            int before = sim.DockCrates.Count;
            sim.AdminDeliver(pid, 50);
            Assert.AreEqual(before + 1, sim.DockCrates.Count);
            sim.AdminDeliver("gibtsnicht", 5);
            Assert.AreEqual(before + 1, sim.DockCrates.Count);
        }

        [Test]
        public void SpawnOrders_AndTimeFreeze()
        {
            var sim = TestUtil.Fresh();
            sim.AdminListAll();
            int n = sim.AdminSpawnOrders(3, true);
            Assert.Greater(n, 0);
            sim.InGame = true;
            float t = sim.TimeMinutes;
            sim.AdminTimeScale = 0f;
            sim.Tick(5f);
            Assert.AreEqual(t, sim.TimeMinutes, 0.0001f);
        }

        [Test]
        public void AllEventsCanBeTriggered()
        {
            var sim = TestUtil.Fresh();
            sim.AdminUnlockEverything();
            foreach (var ev in EventData.All) Assert.DoesNotThrow(() => sim.AdminTriggerEvent(ev.Id), ev.Id);
        }
    }
}
