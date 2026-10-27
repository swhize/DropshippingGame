using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>F7 Inhalte: neue Ereignisse, Namen, Texte, Tutorial, Tagesabrechnung.</summary>
    public class ContentTests
    {
        private static readonly string[] NewEvents =
        {
            "retourenwelle", "grosskunde", "trend_alarm", "trend_crash", "express_rush", "eilauftrag", "kuriose_retoure", "seminar",
            "kalles_wette", "messe",
        };

        /// <summary>Ein Spielstand, in dem alle neuen Ereignisse möglich sind.</summary>
        private static Sim RichSim(int seed)
        {
            var gm = TestUtil.Level3(seed);
            gm.Level = 5;
            gm.LocationStage = 1;
            gm.Upgrades.Add("warehouse");
            gm.Day = 3;
            gm.Money = 5000;
            gm.TotalShipped = 40;
            gm.ShippedPerProduct["huelle"] = 30;
            gm.ShippedPerProduct["led"] = 10;
            gm.TotalReturns = 2;
            gm.StartWeek(false);
            foreach (var p in GameData.Products)
            {
                if (!gm.ProductAvailable(p.Id)) continue;
                gm.SetListed(p.Id, true);
                gm.Stock[p.Id].Qty = 40;
            }
            var c = gm.ContractOffers()[0];
            c.Days = 4;
            gm.AcceptContract(c.Id);
            gm.Day = 3;
            return gm;
        }

        [Test]
        public void AtLeastEightNewEvents_AreEligibleAndDecidable()
        {
            Assert.IsTrue(EventData.All.Length >= 30, "Mindestens 30 Ereignisse insgesamt");
            int seed = 100;
            foreach (var id in NewEvents)
            {
                var ev = EventData.Find(id);
                Assert.IsNotNull(ev, id);
                Assert.IsFalse(string.IsNullOrEmpty(ev.Title) || string.IsNullOrEmpty(ev.Text) || string.IsNullOrEmpty(ev.Sender), "Texte: " + id);
                int choices = ev.HasChoices ? ev.Choices.Length : 1;
                for (int ci = 0; ci < choices; ci++)
                {
                    var gm = RichSim(seed++);
                    if (id == "kuriose_retoure") gm.TotalReturns = 1;
                    Assert.IsTrue(gm.Events.Eligible(ev), "Ereignis möglich: " + id);
                    var mail = gm.Events.Trigger(id);
                    Assert.IsFalse(mail.Title.Contains("{") || mail.Text.Contains("{") || mail.Sender.Contains("{"), "Platzhalter ersetzt: " + id);
                    if (!mail.Pending)
                    {
                        Assert.IsFalse(string.IsNullOrEmpty(mail.Result), "Ergebnis: " + id);
                        continue;
                    }
                    string res = gm.Events.Choose(mail.Id, ci);
                    Assert.IsFalse(string.IsNullOrEmpty(res), "Entscheidung " + ci + " bei " + id);
                    Assert.IsFalse(res.Contains("{"), "Platzhalter im Ergebnis ersetzt: " + id);
                }
            }
        }

        [Test]
        public void NewEvents_ResolveByDefaultAtDayEnd()
        {
            foreach (var id in NewEvents)
            {
                var ev = EventData.Find(id);
                if (!ev.HasChoices) continue;
                var gm = RichSim(7);
                var mail = gm.Events.Trigger(id);
                gm.EndDayNow();
                Assert.IsFalse(mail.Pending, "Standardwahl um 20 Uhr: " + id);
            }
        }

        [Test]
        public void Names_CustomersCitiesCompaniesAndTexts()
        {
            Assert.IsTrue(GameData.CustomerFirstNames.Length >= 40, "Vornamen");
            Assert.IsTrue(GameData.Cities.Length >= 30, "Orte");
            Assert.IsTrue(GameData.Companies.Length >= 20, "Firmen");
            Assert.IsTrue(GameData.ContractReasons.Length >= 8, "Anlässe");
            Assert.IsTrue(GameData.OrderNotes.Length >= 15 && GameData.ExpressNotes.Length >= 5, "Notizen");
            Assert.IsTrue(GameData.ReturnReasons.Length >= 5 && GameData.ReturnReasonsQuality.Length >= 3, "Rücksendegründe");
            foreach (var p in GameData.Products) Assert.IsTrue(GameData.ProductNotes.ContainsKey(p.Id), "Produktnotiz: " + p.Id);
            foreach (var kv in GameData.ReviewTexts) Assert.IsTrue(kv.Value.Length >= 5, "Bewertungstexte " + kv.Key + " Sterne");
            Assert.IsTrue(GameData.ReviewTextsExpressGood.Length >= 3 && GameData.ReviewTextsExpressLate.Length >= 3, "Express-Bewertungen");
            AssertDistinct(GameData.CustomerFirstNames, "Vornamen");
            AssertDistinct(GameData.Cities, "Orte");
            AssertDistinct(GameData.Companies, "Firmen");
            Assert.AreEqual(7, GameData.WeekdayNames.Length);
            var gm = TestUtil.Fresh(3);
            string customer = gm.RandomCustomer();
            Assert.IsTrue(customer.EndsWith(".") && customer.Contains(" "), "Format 'Sabine K.': " + customer);
        }

        private static void AssertDistinct(string[] list, string what)
        {
            var set = new HashSet<string>(list);
            Assert.AreEqual(list.Length, set.Count, "Keine Doppelten: " + what);
        }

        [Test]
        public void Tutorial_TextsMentionTicketsAndPhone()
        {
            Assert.AreEqual(10, GameData.Tutorial.Length, "10 Schritte wie bisher");
            string all = "";
            for (int i = 0; i < GameData.Tutorial.Length; i++) all += GameData.TutorialText(i) + " ";
            Assert.IsTrue(all.Contains("oben rechts") && all.Contains("Handy") && all.Contains("Laptop"), "Neue Oberfläche erklärt");
            Assert.IsFalse(all.Contains("{key:"), "Platzhalter ersetzt");
            Assert.IsTrue(GameData.TutorialText(5).Contains("Tab"), "Standardtaste für das Handy");
            try
            {
                GameData.KeyLabel = a => a == "phone" ? "Y" : a;
                Assert.IsTrue(GameData.TutorialText(5).Contains("Mit Y"), "Oberfläche kann die Taste liefern");
                Assert.AreEqual("E", GameData.Key("interact"), "Unbekannt → Standardtaste");
                GameData.KeyLabel = a => throw new InvalidOperationException();
                Assert.AreEqual("Tab", GameData.Key("phone"), "Fehler im Resolver → Standard");
            }
            finally
            {
                GameData.KeyLabel = null;
            }
            var gm = new Sim(1);
            gm.NewGame("tutorial");
            for (int step = 0; step < 3; step++)
            {
                var obj = gm.CurrentObjective();
                Assert.IsFalse(obj.Text.Contains("{"), "Ziel-Text ohne Platzhalter");
                if (step == 0) gm.OpenPc();
                if (step == 1) gm.BuyBulk(0, 0, 1);
            }
            Assert.AreEqual("E", GameData.FillKeys("{key:interact}"));
            Assert.AreEqual("ohne Taste", GameData.FillKeys("ohne Taste"));
        }

        [Test]
        public void DaySummary_IncludesReturnsContractsAndChallenges()
        {
            var gm = TestUtil.Level3(4);
            gm.Money = 1000;
            gm.StartWeek(false);
            gm.Challenges.Clear();
            gm.AddBonusChallenge("ship", 1, 80, 20, "Test", "Eins", "Ein Paket");
            TestUtil.FulfillOne(gm, "huelle", false);
            gm.ScheduleReturn(new ItemData { Kind = ItemKind.Labeled, Product = "huelle", Price = 25, Quality = 1f }, 0f);
            gm.AdvanceMinutes(1f);
            var ret = gm.PickupReturn();
            gm.ProcessReturn(ret, true);
            var c = gm.ContractOffers()[0];
            gm.AcceptContract(c.Id);
            gm.ContractDeliver(ItemData.Crate(c.Product, c.Quantity, 1f));
            DaySummary s = null;
            gm.DayEnded += x => s = x;
            gm.EndDayNow();
            Assert.AreEqual(1, s.Returns, "Retouren");
            Assert.AreEqual(25, s.Refunds, "Erstattungen");
            Assert.AreEqual(1, s.ReturnsRestocked, "B-Ware");
            Assert.AreEqual(c.Payment, s.ContractIncome, "Großauftrag");
            Assert.AreEqual(1, s.ContractsDone);
            Assert.AreEqual(1, s.ChallengesDone, "Wochenziel");
            Assert.AreEqual(80, s.ChallengeRewards);
            Assert.AreEqual(0, s.Weekday);
            Assert.AreEqual("Montag", s.WeekdayName);
            Assert.IsTrue(s.Notes.Count >= 2, "Meldungen für den Kassenbon");
            int fixedCosts = s.Rent + s.Wages + s.Upkeep + s.Interest;
            int expected = s.Revenue + s.IncomeOther - s.Purchases - s.Packaging - s.Marketing - s.Other - s.Refunds - s.Penalties - s.Shipping - fixedCosts;
            Assert.AreEqual(expected, s.Profit, "Gewinn berücksichtigt Erstattungen und Strafen");
            Assert.AreEqual(gm.Money - fixedCosts, s.MoneyAfter, "Kontostand danach");
            gm.StartNextDay();
            var h = gm.History[gm.History.Count - 1];
            Assert.IsTrue(h.Returns == 1 && h.Contracts == 1, "Verlauf speichert Retouren und Aufträge");
            Assert.AreEqual(s.Profit, h.Profit, "Gewinn im Verlauf = Abrechnung");
        }

        [Test]
        public void NewGoals_ForContractsAndChallenges()
        {
            Assert.IsNotNull(Array.Find(GameData.Goals, g => g.Id == "first_contract"));
            Assert.IsNotNull(Array.Find(GameData.Goals, g => g.Id == "challenges3"));
            foreach (var g in GameData.Goals) Assert.IsTrue(g.Xp >= 0, "XP-Belohnung gesetzt: " + g.Id);
            var gm = TestUtil.Fresh();
            gm.TotalChallengesDone = 3;
            int xp = gm.Xp;
            gm.CheckGoals();
            Assert.IsTrue(gm.GoalsDone.Contains("challenges3"), "Ziel 'Wochenheld'");
            Assert.IsTrue(gm.Xp > xp, "Ziele geben jetzt auch XP");
        }

        [Test]
        public void Pitch_UsesReturnRate()
        {
            var good = TestUtil.Fresh(3);
            good.TotalShipped = 200;
            good.TotalReturns = 2;
            var bad = TestUtil.Fresh(3);
            bad.TotalShipped = 200;
            bad.TotalReturns = 30;
            Assert.IsTrue(new PitchGame(good).StatValue("returns") > new PitchGame(bad).StatValue("returns"), "Niedrige Retourenquote überzeugt");
        }
    }
}
