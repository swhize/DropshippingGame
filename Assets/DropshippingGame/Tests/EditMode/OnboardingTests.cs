using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Tutorial der ersten Stunde, Onboarding-Nachrichten und „erstes Paket“-Moment.</summary>
    public class OnboardingTests
    {
        private static Sim Tutorial()
        {
            var sim = new Sim(3);
            sim.NewGame("tutorial");
            return sim;
        }

        /// <summary>Spielt das Tutorial mit einem beliebigen Produkt durch (Index in GameData.Products).</summary>
        private static void PlayTutorial(Sim gm, int productIndex)
        {
            string pid = GameData.Products[productIndex].Id;
            gm.OpenPc();
            gm.ClosePc();
            Assert.IsTrue(gm.BuyBulk(productIndex, 0, 1), "Einkauf klappt");
            gm.AdvanceMinutes(gm.LeadMinutes(1) + 1f);
            var c = gm.PickupCrate();
            Assert.IsNotNull(c, "Kiste am Wareneingang");
            gm.UnboxCrate(pid, c.Quantity, c.Quality);
            Assert.AreEqual(4, gm.TutorialStep, "Eingeräumt → Online stellen");
            gm.SetListed(pid, true);
            Assert.AreEqual(5, gm.TutorialStep, "Online → Erste Bestellung");
            gm.AdvanceMinutes(5f);
            Assert.IsTrue(gm.PendingCountFor(pid) >= 1, "Garantierte erste Bestellung für das gekaufte Produkt");
            var it = gm.PickItem(pid);
            Assert.IsNotNull(it, "Artikel aus dem Regal");
            Assert.IsTrue(gm.WrapItem(it), "Verpacken (Karton vorhanden oder gefaltet)");
            var pk = gm.PickupPackage();
            gm.OnLabeled();
            gm.ShipPackage(pk);
        }

        [Test]
        public void Tutorial_WorksWithOtherStarterProduct()
        {
            var gm = Tutorial();
            int idx = Array.FindIndex(GameData.Products, p => p.Id == "led");
            Assert.IsTrue(idx >= 0 && gm.ProductAvailable("led"), "LED-Kette ist ein Startprodukt");
            gm.Packaging[GameData.Products[idx].Size] = 5;
            PlayTutorial(gm, idx);
            Assert.AreEqual(-1, gm.TutorialStep, "Tutorial auch ohne Handyhülle abgeschlossen");
        }

        [Test]
        public void Tutorial_TextsAreShortAndCurrent()
        {
            foreach (var t in GameData.Tutorial)
            {
                Assert.IsTrue(t.Title.Length <= 28, "Kurzer Titel: " + t.Title);
                Assert.IsTrue(t.Text.Length <= 200, "Kurzer Text: " + t.Title);
            }
            string all = "";
            for (int i = 0; i < GameData.Tutorial.Length; i++) all += GameData.TutorialText(i) + " ";
            Assert.IsFalse(all.Contains("Box vor der Garage"), "Alte Ortsangabe entfernt");
            Assert.IsFalse(all.Contains("Tab '"), "Keine alten Reiter-Namen");
            Assert.IsTrue(all.Contains("AllesExpress") && all.Contains("Mein Shop"), "Aktuelle App-Namen");
            Assert.IsTrue(all.Contains("Packstation"), "Gratis-Abgabe wird erwähnt");
        }

        [Test]
        public void Onboarding_NoTipsDuringTutorial_ThenPaced()
        {
            var gm = Tutorial();
            gm.AdvanceMinutes(60f);
            Assert.IsFalse(gm.Explained.Contains("post"), "Während des Tutorials keine Onboarding-Nachricht");
            Assert.AreEqual(0, gm.Events.Mails.FindAll(m => m.Sender == GameData.CoachName).Count, "Keine Coach-Mails im Tutorial");
            PlayTutorial(gm, 0);
            Assert.AreEqual(-1, gm.TutorialStep);
            Assert.IsTrue(gm.Explained.Contains("week") && gm.Explained.Contains("post"), "Abschluss: Wochenziele + Versand erklärt");
            Assert.IsFalse(gm.Explained.Contains("phone"), "Die nächste Nachricht kommt nicht sofort hinterher");
            gm.AdvanceMinutes(GameData.OnboardingGapMinutes + 6f);
            Assert.IsTrue(gm.Explained.Contains("phone"), "Nach der Pause kommt die nächste Erklärung");
        }

        [Test]
        public void Onboarding_UnlocksAreExplainedWhenRelevant()
        {
            var gm = TestUtil.Fresh(5);
            gm.Explained.Add("post");
            gm.Explained.Add("phone");
            gm.Explained.Add("dayend");
            Assert.IsNull(FindDue(gm, "tiktok"), "TikTok erst ab Level " + GameData.TikTokLevel);
            gm.AddXp(GameData.LevelThreshold(GameData.TikTokLevel));
            Assert.IsTrue(gm.OnboardingDue("tiktok"), "Nach dem Aufstieg fällig");
            int sent = 0;
            for (int i = 0; i < 20 && gm.OnboardingStep(true); i++) sent++;
            Assert.IsTrue(gm.Explained.Contains("tiktok"), "TikTok erklärt");
            Assert.IsFalse(gm.Explained.Contains("staff"), "Personal (Lagerhalle + Level 5) noch nicht");
            Assert.IsTrue(sent >= 1);
        }

        private static OnboardingTip FindDue(Sim gm, string key)
        {
            var t = gm.NextOnboardingTip();
            while (t != null && t.Key != key)
            {
                gm.Explained.Add(t.Key);
                t = gm.NextOnboardingTip();
            }
            return t;
        }

        [Test]
        public void Onboarding_TipsAreValid()
        {
            var keys = new HashSet<string>();
            foreach (var t in GameData.OnboardingTips)
            {
                Assert.IsTrue(keys.Add(t.Key), "Schlüssel eindeutig: " + t.Key);
                Assert.IsFalse(string.IsNullOrEmpty(t.Title) || string.IsNullOrEmpty(t.Text), "Text vorhanden: " + t.Key);
                Assert.IsFalse(GameData.FillKeys(t.Text).Contains("{key:"), "Tasten ersetzt: " + t.Key);
                Assert.IsTrue(t.Text.Length <= 260, "Kurz genug: " + t.Key);
            }
        }

        [Test]
        public void FirstParcel_FiresOnceAndNotForOldSaves()
        {
            var gm = Tutorial();
            int fired = 0;
            gm.FirstParcelShipped += () => fired++;
            PlayTutorial(gm, 0);
            Assert.AreEqual(1, fired, "Feier beim ersten Paket");
            TestUtil.FulfillOne(gm, "huelle");
            Assert.AreEqual(1, fired, "Nur einmal");

            var old = TestUtil.Fresh(7);
            old.TotalShipped = 12;
            int f2 = 0;
            old.FirstParcelShipped += () => f2++;
            TestUtil.FulfillOne(old, "huelle");
            Assert.AreEqual(0, f2, "Alter Spielstand mit Versand: keine Feier");
        }
    }
}
