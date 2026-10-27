using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>F6 Wochenziele: Montag = neue Ziele, Fortschritt, Belohnung, Wochenbilanz.</summary>
    public class ChallengesTests
    {
        private static WeeklyChallenge Add(Sim gm, string type, float target, string product = "") =>
            gm.AddBonusChallenge(type, target, 100, 40, "Test", "Test " + type, "Test", "trophy", product);

        [Test]
        public void Week_StartsMondayWithThreeChallenges()
        {
            var gm = new Sim(3);
            gm.NewGame("skip");
            Assert.AreEqual(0, gm.Weekday, "Tag 1 ist ein Montag");
            Assert.AreEqual("Montag", gm.WeekdayName());
            Assert.AreEqual("Mo", gm.WeekdayShort());
            Assert.AreEqual(1, gm.Week);
            Assert.AreEqual(6, gm.DaysLeftInWeek());
            Assert.AreEqual(3, gm.Challenges.Count, "Drei Wochenziele");
            var ids = new HashSet<string>();
            bool core = false;
            foreach (var c in gm.Challenges)
            {
                Assert.IsTrue(ids.Add(c.Id), "Verschiedene Ziele");
                Assert.IsTrue(c.Target > 0 && c.Reward > 0 && c.Xp > 0, "Ziel und Belohnung: " + c.Id);
                Assert.IsFalse(string.IsNullOrEmpty(c.Title) || string.IsNullOrEmpty(c.Desc), "Texte: " + c.Id);
                if (c.Type == "ship" || c.Type == "revenue") core = true;
            }
            Assert.IsTrue(core, "Ein Durchsatz-Ziel ist immer dabei");
            int started = 0;
            gm.WeekStarted += w => started = w;
            for (int d = 0; d < 7; d++)
            {
                gm.EndDayNow();
                gm.StartNextDay();
            }
            Assert.IsTrue(gm.Day == 8 && gm.Weekday == 0 && gm.Week == 2, "Tag 8 ist wieder Montag");
            Assert.AreEqual(2, started, "WeekStarted für Woche 2");
            Assert.AreEqual(2, gm.ChallengeWeek);
            Assert.AreEqual(3, gm.Challenges.Count, "Neue Wochenziele");
            Assert.AreEqual("Sonntag", GameData.WeekdayName(7));
            Assert.AreEqual("Sa", GameData.WeekdayShort(13));
        }

        [Test]
        public void Challenge_CompletesWithMoneyAndXp()
        {
            var gm = TestUtil.Fresh();
            var c = Add(gm, "ship", 3);
            var done = new List<WeeklyChallenge>();
            gm.ChallengeCompleted += x => done.Add(x);
            int xp0 = gm.Xp;
            int revenue = 0;
            for (int i = 0; i < 3; i++) revenue += TestUtil.FulfillOne(gm, "huelle");
            Assert.IsTrue(c.Done, "Geschafft");
            Assert.AreEqual(1, done.Count, "ChallengeCompleted gemeldet");
            Assert.AreEqual("3 / 3", c.ProgressText);
            Assert.AreEqual(1f, c.Fraction, 0.001f);
            Assert.AreEqual(100, gm.Daily.ChallengeRewards, "Belohnung verbucht");
            Assert.IsTrue(gm.Daily.IncomeOther >= 100, "Als sonstige Einnahme");
            Assert.AreEqual(1, gm.Daily.ChallengesDone);
            Assert.AreEqual(1, gm.TotalChallengesDone);
            Assert.IsTrue(gm.Xp >= xp0 + 40, "XP");
            Assert.IsTrue(gm.Daily.Notes.Exists(n => n.Contains("Test ship")), "Meldung für den Kassenbon");
            TestUtil.FulfillOne(gm, "huelle");
            Assert.AreEqual(1, done.Count, "Nur einmal belohnt");
        }

        [Test]
        public void ChallengeTypes_CountTheRightThings()
        {
            var gm = TestUtil.Fresh();
            gm.Level = 3;
            gm.Upgrades.Add("stand");
            var ship = Add(gm, "ship", 100);
            var rev = Add(gm, "revenue", 100000);
            var express = Add(gm, "express", 100);
            var product = Add(gm, "product", 100, "led");
            var tiktok = Add(gm, "tiktok", 100);
            var restock = Add(gm, "restock", 100);
            var stand = Add(gm, "stand", 100);
            var contract = Add(gm, "contract", 100);

            int r1 = TestUtil.FulfillOne(gm, "huelle");
            Assert.AreEqual(1f, ship.Progress, "Paket gezählt");
            Assert.AreEqual(r1, rev.Progress, 0.01f, "Umsatz gezählt");
            Assert.AreEqual(0f, express.Progress + product.Progress, "Kein Express, kein LED");
            TestUtil.FulfillOne(gm, "led", true);
            Assert.AreEqual(1f, express.Progress, "Pünktliches Express");
            Assert.AreEqual(1f, product.Progress, "LED verkauft");
            var late = TestUtil.Labeled(gm, "led", true);
            late.Created -= 500f;
            late.DueAt -= 500f;
            gm.ShipPackage(late);
            Assert.AreEqual(1f, express.Progress, "Verspätetes Express zählt nicht");
            Assert.AreEqual(2f, product.Progress);
            gm.TriggerTikTok(0.5f, true);
            Assert.AreEqual(0f, tiktok.Progress, "Stille TikToks (Praktikant, Ereignis) zählen nicht");
            gm.TriggerTikTok(0.5f);
            Assert.AreEqual(1f, tiktok.Progress, "Eigenes TikTok zählt");
            var ret = new ItemData { Kind = ItemKind.Return, Product = "huelle", Quality = 1f, Price = 20 };
            gm.ProcessReturn(ret, false);
            Assert.AreEqual(0f, restock.Progress, "Entsorgen zählt nicht");
            gm.ProcessReturn(ret, true);
            Assert.AreEqual(1f, restock.Progress, "B-Ware zählt");
            gm.StandAddCrate(ItemData.Crate("huelle", 20, 1f));
            gm.SetShopPrice("huelle", 5);
            for (int i = 0; i < 20 && stand.Progress < 1f; i++) gm.StandTryBuy(out _, out _, out _);
            Assert.AreEqual(1f, stand.Progress, "Standverkauf");
            var c = gm.ContractOffers().Count > 0 ? gm.ContractOffers()[0] : gm.GenerateContractOffer();
            gm.AcceptContract(c.Id);
            gm.ContractDeliver(ItemData.Crate(c.Product, c.Quantity, 1f));
            Assert.AreEqual(1f, contract.Progress, "Großauftrag");
        }

        [Test]
        public void RatingAndPerfectDay_AreStateBased()
        {
            var gm = TestUtil.Fresh();
            gm.ReviewCount = 10;
            gm.Reputation = 3.5f;
            var rating = Add(gm, "rating", 4f);
            gm.ChangeReputation(0.3f);
            Assert.IsFalse(rating.Done, "3,8 reicht nicht");
            Assert.AreEqual(3.8f, rating.Progress, 0.001f, "Fortschritt = aktuelle Bewertung");
            Assert.AreEqual("3,8 / 4,0 ★", rating.ProgressText);
            gm.ChangeReputation(0.3f);
            Assert.IsTrue(rating.Done, "4,1 ≥ 4,0");

            var perfect = Add(gm, "perfect_day", 3);
            TestUtil.FulfillOne(gm, "huelle");
            TestUtil.FulfillOne(gm, "huelle");
            Assert.IsFalse(perfect.Done);
            TestUtil.FulfillOne(gm, "huelle");
            Assert.IsTrue(perfect.Done, "Drei Pakete ohne verlorene Bestellung");

            var gm2 = TestUtil.Fresh();
            var perfect2 = Add(gm2, "perfect_day", 2);
            gm2.Daily.Lost = 1;
            TestUtil.FulfillOne(gm2, "huelle");
            TestUtil.FulfillOne(gm2, "huelle");
            Assert.IsFalse(perfect2.Done, "Mit verlorener Bestellung zählt der Tag nicht");
        }

        [Test]
        public void Sunday_SummaryShowsTheWeek()
        {
            var gm = new Sim(2);
            gm.NewGame("skip");
            DaySummary last = null;
            gm.DayEnded += s => last = s;
            for (int d = 1; d < 7; d++)
            {
                gm.EndDayNow();
                Assert.IsFalse(last.WeekEnded, "Unter der Woche keine Wochenbilanz");
                gm.StartNextDay();
            }
            Assert.AreEqual(6, gm.Weekday, "Sonntag");
            gm.Challenges[0].Done = true;
            gm.EndDayNow();
            Assert.IsTrue(last.WeekEnded, "Sonntag: Wochenbilanz");
            Assert.AreEqual("Sonntag", last.WeekdayName);
            Assert.AreEqual(6, last.Weekday);
            Assert.AreEqual(gm.Challenges.Count, last.WeekChallengesTotal);
            Assert.AreEqual(1, last.WeekChallengesDone);
            Assert.IsTrue(last.Notes.Exists(n => n.StartsWith("Wochenbilanz")), "Meldung auf dem Kassenbon");
        }

        [Test]
        public void KallesWette_AddsBonusChallenge()
        {
            var gm = new Sim(4);
            gm.NewGame("skip");
            gm.Day = 3;
            Assert.IsTrue(gm.Events.Eligible(EventData.Find("kalles_wette")), "Mo-Do möglich");
            var mail = gm.Events.Trigger("kalles_wette");
            Assert.IsTrue(mail.CtxNumber >= 5 && mail.Text.Contains(mail.CtxNumber + " Pakete"), "Zahl im Text: " + mail.Text);
            gm.Events.Choose(mail.Id, 0);
            var bonus = gm.Challenges.Find(c => c.Bonus);
            Assert.IsNotNull(bonus, "Bonusziel");
            Assert.AreEqual("Kalle", bonus.Sponsor);
            Assert.AreEqual(mail.CtxNumber, (int)bonus.Target);
            Assert.AreEqual(4, gm.Challenges.Count, "Zusätzlich zu den drei Wochenzielen");
            gm.Day = 6;
            Assert.IsFalse(gm.Events.Eligible(EventData.Find("kalles_wette")), "Nicht am Samstag");
        }

        [Test]
        public void MidWeekStart_ScalesTargets()
        {
            var gm = TestUtil.Fresh(9);
            gm.Day = 1;
            gm.StartWeek(false);
            float full = gm.Challenges.Find(c => c.Type == "ship" || c.Type == "revenue").Target;
            string type = gm.Challenges.Find(c => c.Type == "ship" || c.Type == "revenue").Type;
            var gm2 = TestUtil.Fresh(9);
            gm2.Day = 5;
            gm2.StartWeek(false);
            var same = gm2.Challenges.Find(c => c.Type == type);
            Assert.IsNotNull(same, "Gleiche Zufallsfolge");
            Assert.IsTrue(same.Target < full, "Ab Freitag kleinere Ziele (" + same.Target + " < " + full + ")");
        }
    }
}
