using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Fakebook-/Gugel-Anzeigen, Fake-Profile, Fake-Seiten, KI-Texte und Speichern.</summary>
    public class AdsTests
    {
        private const string Pid = "huelle";

        private static Sim Ready(int level = 3, int money = 5000)
        {
            var sim = TestUtil.Fresh();
            sim.AddXp(GameData.LevelThreshold(level));
            sim.Money = money;
            sim.SetListed(Pid, true);
            sim.AdsReseed(5);
            return sim;
        }

        private static int Target(string id) => GameData.AdTargetIndex(id);

        [Test]
        public void FakebookLockedBelowLevel()
        {
            var sim = TestUtil.Fresh();
            sim.Money = 1000;
            sim.SetListed(Pid, true);
            Assert.Less(sim.Level, GameData.FakebookLevel);
            Assert.IsNull(sim.StartFakebookAd(Pid, 0, 0, 50));
            Assert.AreEqual(0, sim.AdCampaigns.Count);
        }

        [Test]
        public void FitAndBudgetRaiseMultiplier()
        {
            var sim = Ready();
            float good = sim.FakebookMult(Pid, Target("teens"), 0, 100);
            float bad = sim.FakebookMult(Pid, Target("fitness"), 0, 100);
            float cheap = sim.FakebookMult(Pid, Target("teens"), 0, 20);
            Assert.Greater(good, bad, "passende Zielgruppe wirkt stärker");
            Assert.Greater(good, cheap, "mehr Budget wirkt stärker");
            Assert.LessOrEqual(sim.FakebookMult(Pid, Target("teens"), 0, 100000), GameData.FakebookMaxMult);
            Assert.Greater(sim.FakebookCtr(Pid, Target("teens"), 1), sim.FakebookCtr(Pid, Target("teens"), 0), "Clickbait klickt besser");
        }

        [Test]
        public void FakebookCampaignBoostsDemandAndSpendsBudgetOverTime()
        {
            var sim = Ready();
            float before = sim.DemandRate(Pid);
            int money = sim.Money;
            var c = sim.StartFakebookAd(Pid, Target("teens"), 3, 100);
            Assert.IsNotNull(c);
            Assert.IsTrue(c.Active);
            Assert.AreEqual(money, sim.Money, "Kosten laufen über die Zeit, nicht sofort");
            Assert.Greater(sim.DemandRate(Pid), before * 1.1f);
            Assert.IsNotNull(sim.ActiveBoost("ad:" + c.Id));

            sim.AdvanceMinutes(360f);
            Assert.That(c.Spent, Is.EqualTo(50f).Within(1f));
            Assert.Greater(c.Impressions, 1000f);
            Assert.Greater(c.Clicks, 0f);
            Assert.Less(sim.Money, money - 45);

            // über Feierabend hinaus bis zum Ende der Laufzeit
            sim.AdvanceMinutes(GameData.DayEnd - sim.TimeMinutes + 1f);
            sim.AdvanceMinutes(400f);
            Assert.IsFalse(c.Active);
            Assert.AreEqual("done", c.EndReason);
            Assert.That(c.Spent, Is.EqualTo(100f).Within(0.5f));
            Assert.IsNull(sim.ActiveBoost("ad:" + c.Id));
        }

        [Test]
        public void OnlyOneCampaignPerProductAndMaxThree()
        {
            var sim = Ready(level: 6);
            Assert.IsNotNull(sim.StartFakebookAd(Pid, 0, 0, 20));
            Assert.IsNull(sim.StartFakebookAd(Pid, 1, 0, 20), "gleiches Produkt läuft schon");
            sim.SetListed("led", true);
            sim.SetListed("massage", true);
            sim.SetListed("kopfhoerer", true);
            Assert.IsNotNull(sim.StartFakebookAd("led", 0, 0, 20));
            Assert.IsNotNull(sim.StartFakebookAd("massage", 0, 0, 20));
            Assert.AreNotEqual("", sim.CanStartAd("fakebook", "kopfhoerer", 20));
            Assert.AreEqual("", sim.CanStartAd("gugel", "led", 30), "Gugel zählt getrennt");
        }

        [Test]
        public void StopCampaignRemovesBoost()
        {
            var sim = Ready();
            var c = sim.StartFakebookAd(Pid, 0, 0, 50);
            Assert.IsTrue(sim.StopAdCampaign(c.Id));
            Assert.IsFalse(c.Active);
            Assert.AreEqual("stopped", c.EndReason);
            Assert.IsNull(sim.ActiveBoost("ad:" + c.Id));
            Assert.IsFalse(sim.StopAdCampaign(c.Id));
        }

        [Test]
        public void SalesAreAttributedToRunningCampaign()
        {
            var sim = Ready();
            var c = sim.StartFakebookAd(Pid, Target("teens"), 0, 250);
            int n = 0;
            for (int i = 0; i < 4; i++)
                if (sim.SpawnOrder(Pid, false) != null) n++;
            Assert.Greater(n, 0);
            float expected = n * (c.Mult - 1f) / sim.BoostMult(Pid);
            Assert.That(c.Sales, Is.EqualTo(expected).Within(0.01f));
            Assert.Greater(c.Revenue, 0f);
        }

        [Test]
        public void GugelPositionFollowsBid()
        {
            int[] rivals = GameData.GugelRivalBids(Pid, 0);
            Assert.AreEqual(3, rivals.Length);
            Assert.GreaterOrEqual(rivals[0], rivals[1]);
            Assert.AreEqual(1, Sim.GugelPosition(Pid, 0, rivals[0] + 1));
            Assert.AreEqual(4, Sim.GugelPosition(Pid, 0, GameData.GugelMinBid));
            Assert.AreEqual(rivals[0] + 1, Sim.GugelCpc(Pid, 0, rivals[0] + 50), "Zweitpreis: nächstes Gebot + 1 Cent");
            Assert.AreEqual(rivals[1] + 1, Sim.GugelCpc(Pid, 0, rivals[0]), "Platz 2 zahlt Gebot von Platz 3 + 1 Cent");
            Assert.Greater(Sim.GugelMult(Pid, 0, rivals[0] + 1), Sim.GugelMult(Pid, 0, GameData.GugelMinBid));
            Assert.Greater(Sim.GugelMult(Pid, 0, 400), Sim.GugelMult(Pid, 4, 400), "Kaufabsicht zählt");
            Assert.LessOrEqual(GameData.GugelRivalBids("drohne", 0)[0], GameData.GugelMaxBid, "Platz 1 immer erreichbar");
        }

        [Test]
        public void GugelCampaignStopsWhenBudgetIsGone()
        {
            var sim = Ready();
            Assert.IsTrue(sim.GugelUnlocked);
            var c = sim.StartGugelAd(Pid, 0, GameData.GugelMaxBid, 30);
            Assert.IsNotNull(c);
            Assert.AreEqual(1, c.Position);
            for (int i = 0; i < 70 && c.Active; i++) sim.AdvanceMinutes(10f);
            Assert.IsFalse(c.Active);
            Assert.AreEqual("budget", c.EndReason);
            Assert.That(c.Spent, Is.EqualTo(30f).Within(0.05f));
            Assert.Greater(c.Clicks, 10f);
        }

        [Test]
        public void FakeProfilesGiveProofAndCanBeExposed()
        {
            var sim = Ready(money: 1000);
            for (int i = 0; i < GameData.MaxFakeProfiles + 2; i++) sim.CreateFakeProfile();
            Assert.AreEqual(GameData.MaxFakeProfiles, sim.FakeProfiles);
            Assert.AreEqual(1000 - GameData.MaxFakeProfiles * GameData.FakeProfileCost, sim.Money);
            var proof = sim.ActiveBoost("proof");
            Assert.IsNotNull(proof);
            Assert.That(proof.Mult, Is.EqualTo(sim.SocialProofMult()).Within(0.0001f));
            Assert.Greater(sim.FakeProfileRisk(), 0.2f);

            float rep = sim.Reputation;
            string kind = null;
            sim.FakeExposed += (k, _) => kind = k;
            sim.AdminExposeFakes();
            Assert.AreEqual(0, sim.FakeProfiles);
            Assert.AreEqual("profiles", kind);
            Assert.Less(sim.Reputation, rep);
            Assert.IsNull(sim.ActiveBoost("proof"));
        }

        [Test]
        public void ExposureHappensOverDaysAtHighRisk()
        {
            var sim = Ready(money: 1000);
            for (int i = 0; i < GameData.MaxFakeProfiles; i++) sim.CreateFakeProfile();
            sim.AdminNoBankrupt = true;
            int exposedDay = -1;
            for (int d = 0; d < 30 && exposedDay < 0; d++)
            {
                sim.EndDayNow();
                sim.StartNextDay();
                if (sim.FakeProfiles == 0) exposedDay = sim.Day;
                else Assert.IsNotNull(sim.ActiveBoost("proof"), "Dauer-Boost wird jeden Tag erneuert");
            }
            Assert.Greater(exposedDay, 0, "bei 30 % pro Tag fliegt es innerhalb von 30 Tagen auf");
            Assert.AreEqual(1, sim.FakeExposedTotal);
        }

        [Test]
        public void FakeSitesBoostSeoPerProduct()
        {
            var sim = Ready(money: 1000);
            Assert.IsNotNull(sim.CreateFakeSite(Pid));
            Assert.IsNotNull(sim.CreateFakeSite(Pid));
            Assert.AreEqual(2, sim.FakeSiteCount(Pid));
            var seo = sim.ActiveBoost("seo:" + Pid);
            Assert.IsNotNull(seo);
            Assert.AreEqual(Pid, seo.Product);
            Assert.That(seo.Mult, Is.EqualTo(1f + 2 * GameData.FakeSiteSeo).Within(0.0001f));
            Assert.IsNotNull(sim.CreateFakeSite(Pid));
            Assert.IsNull(sim.CreateFakeSite(Pid), "max. 3 pro Produkt");
            Assert.AreEqual(1000 - 3 * GameData.FakeSiteCost, sim.Money);
        }

        [Test]
        public void SlopTextIsDeterministicAndFilled()
        {
            var site = new FakeSite { Product = "kopfhoerer", Seed = 1234, Day = 3 };
            Assert.AreEqual(SlopGen.Title(site), SlopGen.Title(site));
            Assert.IsTrue(SlopGen.Domain(site).Contains("bluetooth-kopfhoerer"), SlopGen.Domain(site));
            var reasons = SlopGen.Reasons10(site, "Nordlicht");
            Assert.AreEqual(10, reasons.Count);
            Assert.AreEqual(reasons.Count, new HashSet<string>(reasons).Count, "keine doppelten Gründe");
            var all = new List<string>(reasons) { SlopGen.Title(site), SlopGen.Domain(site), SlopGen.Snippet(site, "Nordlicht") };
            all.AddRange(SlopGen.Body(site, "Nordlicht", 49));
            foreach (var t in all)
            {
                Assert.IsFalse(t.Contains("{"), t);
                Assert.IsFalse(string.IsNullOrEmpty(t));
            }
            Assert.IsTrue(SlopGen.Title(site).Contains("Bluetooth-Kopfhörer"));
            for (int seed = 0; seed < 50; seed++)
            {
                var s = new FakeSite { Product = "led", Seed = seed };
                var b = SlopGen.Body(s, "X", 10);
                Assert.AreNotEqual(b[0], b[1], "zwei verschiedene Absätze");
            }
        }

        [Test]
        public void SaveRoundTripKeepsAds()
        {
            var sim = Ready(money: 2000);
            var c = sim.StartFakebookAd(Pid, Target("teens"), 2, 100);
            sim.AdvanceMinutes(60f);
            sim.CreateFakeProfile();
            sim.CreateFakeProfile();
            sim.CreateFakeSite(Pid);
            var json = Json.Write(sim.ToJson());
            var b = TestUtil.Fresh();
            b.FromJson(Json.Parse(json) as Dictionary<string, object>);
            Assert.AreEqual(1, b.ActiveAds("fakebook").Count);
            var c2 = b.ActiveAds("fakebook")[0];
            Assert.AreEqual(c.Id, c2.Id);
            Assert.That(c2.Spent, Is.EqualTo(c.Spent).Within(0.01f));
            Assert.That(c2.Mult, Is.EqualTo(c.Mult).Within(0.0001f));
            Assert.AreEqual(2, b.FakeProfiles);
            Assert.AreEqual(1, b.FakeSites.Count);
            Assert.AreEqual(sim.FakeSites[0].Seed, b.FakeSites[0].Seed);
            Assert.IsNotNull(b.ActiveBoost("ad:" + c.Id));
            Assert.IsNotNull(b.ActiveBoost("proof"));
            Assert.IsNotNull(b.ActiveBoost("seo:" + Pid));
            Assert.Greater(b.NextAdId, c.Id);
        }

        [Test]
        public void OldSaveWithoutAdsLoadsClean()
        {
            var sim = Ready();
            var data = sim.ToJson();
            data.Remove("ads");
            var b = TestUtil.Fresh();
            b.FromJson(Json.Parse(Json.Write(data)) as Dictionary<string, object>);
            Assert.AreEqual(0, b.AdCampaigns.Count);
            Assert.AreEqual(0, b.FakeProfiles);
            Assert.AreEqual(0, b.FakeSites.Count);
            Assert.AreEqual(1, b.NextAdId);
        }

        [Test]
        public void EmptyAccountStopsCampaigns()
        {
            var sim = Ready(money: 100);
            var c = sim.StartFakebookAd(Pid, 0, 0, 100);
            sim.Money = 0;
            sim.AdvanceMinutes(30f);
            Assert.IsFalse(c.Active);
            Assert.AreEqual("money", c.EndReason);
        }
    }
}
