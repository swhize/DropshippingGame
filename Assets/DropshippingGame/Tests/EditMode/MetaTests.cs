using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Erfolge, Endlos-Modus, Mamas Notkredit, Schwierigkeit und Statistik.</summary>
    public class MetaTests
    {
        private static Sim Biz(int seed = 3, string difficulty = "normal")
        {
            var gm = new Sim(seed) { NextDifficulty = difficulty };
            gm.NewGame("skip");
            gm.InGame = true;
            return gm;
        }

        // ---- Erfolge ----------------------------------------------------------------------------
        [Test]
        public void Achievements_CatalogIsSteamReady()
        {
            var ids = new HashSet<string>();
            int count = AchievementData.All.Length;
            Assert.IsTrue(count >= 30 && count <= 40, "30–40 Erfolge (" + count + ")");
            foreach (var a in AchievementData.All)
            {
                Assert.IsTrue(Regex.IsMatch(a.Id, "^ACH_[A-Z0-9_]+$"), "Steam-API-Name: " + a.Id);
                Assert.IsTrue(ids.Add(a.Id), "Eindeutig: " + a.Id);
                Assert.IsFalse(string.IsNullOrEmpty(a.Name) || string.IsNullOrEmpty(a.Desc), "Name und Beschreibung: " + a.Id);
                Assert.IsNotNull(a.Done, "Bedingung: " + a.Id);
            }
            Assert.IsNotNull(AchievementData.Find("ACH_FIRST_PARCEL"));
        }

        [Test]
        public void Achievements_UnlockOnceAndAreSaved()
        {
            var gm = Biz();
            var got = new List<string>();
            gm.AchievementUnlocked += a => got.Add(a.Id);
            TestUtil.FulfillOne(gm, "huelle");
            gm.CheckAchievements();
            Assert.IsTrue(gm.HasAchievement("ACH_FIRST_PARCEL"), "Erstes Paket");
            int n = got.Count;
            gm.CheckAchievements();
            Assert.AreEqual(n, got.Count, "Nicht doppelt");
            Assert.AreEqual(1, got.FindAll(id => id == "ACH_FIRST_PARCEL").Count);

            var json = Json.Write(gm.ToJson());
            var loaded = new Sim(9);
            var resent = new List<string>();
            loaded.AchievementsLoaded += ids => resent.AddRange(ids);
            loaded.FromJson(J.Obj(Json.Parse(json)));
            Assert.IsTrue(loaded.HasAchievement("ACH_FIRST_PARCEL"), "Gespeichert");
            CollectionAssert.Contains(resent, "ACH_FIRST_PARCEL", "Nach dem Laden für Steam erneut gemeldet");
            Assert.AreEqual(gm.Stats.ToJson()["own_tiktoks"], loaded.Stats.ToJson()["own_tiktoks"]);
        }

        [Test]
        public void Achievements_ProgressAndHidden()
        {
            var gm = Biz();
            var parcels = AchievementData.Find("ACH_PARCELS_100");
            gm.TotalShipped = 50;
            Assert.AreEqual(0.5f, gm.AchievementProgress(parcels), 0.001f);
            Assert.IsTrue(AchievementData.Find("ACH_MAMA").Hidden, "Geheimer Erfolg");
            Assert.IsTrue(gm.AdminUnlockAchievement("ACH_MAMA"));
            Assert.AreEqual(1f, gm.AchievementProgress(AchievementData.Find("ACH_MAMA")));
        }

        [Test]
        public void OldSave_WithoutMetaFields_LoadsAsNormal()
        {
            var gm = Biz(4, "hart");
            var state = gm.ToJson();
            foreach (var k in new[] { "achievements", "lifetime_stats", "difficulty", "mama_rescues_left", "mama_debt", "endless_tier" }) state.Remove(k);
            var loaded = new Sim(5);
            loaded.FromJson(J.Obj(Json.Parse(Json.Write(state))));
            Assert.AreEqual("normal", loaded.Difficulty, "Alter Spielstand = Normal");
            Assert.AreEqual(1, loaded.MamaRescuesLeft, "Mama hilft einmal");
            Assert.AreEqual(0, loaded.AchievementCount());
            Assert.AreEqual(0, loaded.EndlessTier);
        }

        // ---- Mama ---------------------------------------------------------------------------------
        [Test]
        public void Mama_RescuesOnce_ThenRepaymentAndBankruptcy()
        {
            var gm = Biz();
            bool over = false;
            int gift = 0;
            gm.GameOver += _ => over = true;
            gm.MamaRescued += g => gift = g;
            gm.Money = -900;
            gm.EndDayNow();
            gm.StartNextDay();
            Assert.IsFalse(over, "Mama verhindert die erste Pleite");
            Assert.Greater(gift, 0);
            Assert.AreEqual(gift, gm.MamaDebt, "Zinslos geliehen");
            Assert.IsTrue(gm.Money > 0, "Wieder im Plus (" + gm.Money + ")");
            Assert.AreEqual(0, gm.MamaRescuesLeft);
            // Läuft es wieder, wird automatisch zurückgezahlt.
            gm.Money = 5000;
            gm.EndDayNow();
            gm.StartNextDay();
            Assert.Less(gm.MamaDebt, gift, "Automatische Rückzahlung");
            // Zweites Mal: keine Rettung.
            gm.Money = -900;
            gm.EndDayNow();
            gm.StartNextDay();
            Assert.IsTrue(over, "Beim zweiten Mal ist Schluss");
        }

        [Test]
        public void Mama_DependsOnDifficulty()
        {
            Assert.AreEqual(2, Biz(3, "entspannt").MamaRescuesLeft);
            Assert.AreEqual(1, Biz(3, "normal").MamaRescuesLeft);
            Assert.AreEqual(0, Biz(3, "hart").MamaRescuesLeft);
        }

        // ---- Schwierigkeit ------------------------------------------------------------------------
        [Test]
        public void Difficulty_MultipliersApply()
        {
            var easy = Biz(3, "entspannt");
            var normal = Biz(3, "normal");
            var hard = Biz(3, "hart");
            foreach (var gm in new[] { easy, normal, hard }) gm.SetListed("led", true);
            Assert.Greater(easy.DemandRate("led"), normal.DemandRate("led"), "Entspannt: mehr Nachfrage");
            Assert.Less(hard.DemandRate("led"), normal.DemandRate("led"), "Hart: weniger Nachfrage");
            Assert.Less(easy.BulkCost(1, 1, 1), normal.BulkCost(1, 1, 1), "Entspannt: günstiger Einkauf");
            Assert.Greater(hard.BulkCost(1, 1, 1), normal.BulkCost(1, 1, 1), "Hart: teurer Einkauf");
            foreach (var gm in new[] { easy, normal, hard })
            {
                gm.LocationStage = 1;
                gm.Upgrades.Add("warehouse");
                gm.Level = 5;
                gm.Money = 5000;
                gm.Hire("packer");
            }
            Assert.Less(easy.FixedCostsPerDay(), normal.FixedCostsPerDay(), "Entspannt: weniger Miete/Lohn");
            Assert.Greater(hard.FixedCostsPerDay(), normal.FixedCostsPerDay(), "Hart: mehr Miete/Lohn");
            Assert.AreEqual(300, Biz(3, "entspannt").Money, "Startgeld Entspannt");
            Assert.AreEqual(GameData.StartCapital, Biz(3, "normal").Money, "Startgeld Normal");
            Assert.AreEqual(100, Biz(3, "hart").Money, "Startgeld Hart");
        }

        [Test]
        public void Difficulty_ReviewHarshness()
        {
            float Drop(string diff)
            {
                var gm = Biz(3, diff);
                gm.Reputation = 4f;
                gm.ReviewCount = 50;
                // abgelaufene Bestellung = 1-Stern-Bewertung
                gm.SetListed("huelle", true);
                var o = gm.SpawnOrder("huelle", false);
                o.Created = -10000f;
                gm.CheckExpiryNow();
                return 4f - gm.Reputation;
            }
            float easy = Drop("entspannt"), normal = Drop("normal"), hard = Drop("hart");
            Assert.Less(easy, normal, "Entspannt: schlechte Bewertungen wiegen weniger");
            Assert.Greater(hard, normal, "Hart: schlechte Bewertungen wiegen mehr");
        }

        [Test]
        public void Difficulty_SurvivesSaveAndNewGame()
        {
            var gm = Biz(3, "hart");
            Assert.AreEqual("hart", gm.Difficulty);
            var loaded = new Sim(1);
            loaded.FromJson(J.Obj(Json.Parse(Json.Write(gm.ToJson()))));
            Assert.AreEqual("hart", loaded.Difficulty);
            Assert.AreEqual(0, loaded.MamaRescuesLeft);
            loaded.NextDifficulty = "quatsch";
            loaded.NewGame("skip");
            Assert.AreEqual("normal", loaded.Difficulty, "Unbekannt → Normal");
        }

        [Test]
        public void Difficulty_ChangesCampaignLength()
        {
            var days = new Dictionary<string, int>();
            foreach (var d in new[] { "entspannt", "normal", "hart" })
            {
                var b = new BalanceBot(PlayerType.Average, 7, d);
                b.RunDays(30);
                Assert.IsFalse(b.Broke, d + ": Durchschnitt geht nicht pleite");
                days[d] = b.Gm.TotalEarned;
                TestContext.Out.WriteLine(d + ": Umsatz nach 30 Tagen " + b.Gm.TotalEarned + ", Lagerhalle Tag " + b.WarehouseDay);
            }
            Assert.Greater(days["entspannt"], days["normal"], "Entspannt geht schneller");
            Assert.Greater(days["normal"], days["hart"], "Hart dauert länger");
        }

        // ---- Endlos-Modus ---------------------------------------------------------------------------
        [Test]
        public void Endless_GoalsContinueAfterTheEnding()
        {
            var gm = Biz();
            bool won = false;
            gm.GameWon += () => won = true;
            string milestone = null;
            gm.MilestoneReached += (t, x) => milestone = t;
            foreach (var g in GameData.Goals)
            {
                if (g.Id == "rev100k") break;
                gm.GoalsDone.Add(g.Id); // Kampagne bis kurz vor dem Ende geschafft
            }
            gm.TotalEarned = 100000;
            gm.CheckGoals();
            Assert.IsTrue(won && gm.EndingSeen, "Abspann bei 100.000 €");
            Assert.AreEqual("rev250k", gm.NextGoal().Id, "Danach geht es weiter (Endlos-Modus)");
            Assert.IsTrue(gm.CurrentObjective().Title.Contains("Viertelmillion"));
            gm.TotalEarned = 1000000;
            gm.CheckGoals();
            Assert.IsNotNull(milestone, "Börsengang bei 1 Mio.");
            Assert.AreEqual("outpost", gm.NextGoal().Id, "Außenlager fehlt noch");
            gm.Level = 10;
            gm.Upgrades.Add("warehouse");
            gm.LocationStage = 1;
            gm.Money = 50000;
            int cap = gm.Capacity(), queue = gm.QueueCapacity();
            Assert.IsTrue(gm.BuyUpgrade(GameData.OutpostId), "Außenlager kaufen");
            Assert.AreEqual(cap + GameData.OutpostCapacity, gm.Capacity());
            Assert.AreEqual(queue + GameData.OutpostQueue, gm.QueueCapacity());
            Assert.IsNull(gm.NextGoal(), "Alle festen Ziele erledigt");
            var e = gm.EndlessGoal();
            Assert.AreEqual(GameData.EndlessFirstTarget, (int)e.Target, "Endlos-Ziel 2 Mio.");
            Assert.IsTrue(gm.CurrentObjective().Title.Contains("Endlos"), "Ziel-Anzeige zeigt das Endlos-Ziel");
            gm.TotalEarned = 4100000;
            int money = gm.Money;
            gm.CheckGoals();
            Assert.AreEqual(2, gm.EndlessTier, "2 und 4 Mio. geschafft");
            Assert.Greater(gm.Money, money, "Belohnung");
            Assert.AreEqual(8000000, (int)gm.EndlessGoal().Target, "Nächstes Ziel verdoppelt sich");
            var loaded = new Sim(2);
            loaded.FromJson(J.Obj(Json.Parse(Json.Write(gm.ToJson()))));
            Assert.AreEqual(2, loaded.EndlessTier);
        }

        // ---- Statistik ----------------------------------------------------------------------------
        [Test]
        public void Stats_LinesCoverTotals()
        {
            var gm = Biz();
            TestUtil.FulfillOne(gm, "huelle");
            var lines = gm.StatsLines();
            Assert.IsTrue(lines.Count >= 20, "Viele Kennzahlen (" + lines.Count + ")");
            Assert.IsTrue(lines.Exists(l => l[1] == "Pakete verschickt" && l[2] == "1"));
            Assert.IsTrue(lines.Exists(l => l[1] == "Erfolge"));
        }

        // ---- Balance-Regeln ---------------------------------------------------------------------
        [Test]
        public void PriceGouging_HurtsDemandAndReviews()
        {
            var gm = Biz();
            gm.SetListed("led", true);
            float fair = gm.SuggestedPrice("led");
            gm.SetShopPrice("led", (int)fair);
            float d1 = gm.DemandRate("led");
            gm.SetShopPrice("led", (int)(fair * 1.5f));
            float d2 = gm.DemandRate("led");
            Assert.Less(d2, d1 * Math.Pow(1 / 1.5, 2.2), "Über dem Markenpreis fällt die Nachfrage steil");
            var pkg = TestUtil.Labeled(gm, "led");
            Assert.GreaterOrEqual(gm.PriceStarPenalty(pkg), 1, "Wucherpreis kostet Sterne");
        }

        [Test]
        public void StaffSlots_GrowWithLevel()
        {
            var gm = Biz();
            gm.LocationStage = 1;
            gm.Upgrades.Add("warehouse");
            gm.Level = 5;
            gm.Money = 10000;
            Assert.IsTrue(gm.Hire("packer"));
            Assert.IsFalse(gm.Hire("packer"), "Zweite Packkraft erst später");
            gm.Level = GameData.StaffSecondSlotLevel("packer");
            Assert.IsTrue(gm.Hire("packer"), "Ab Level " + gm.Level + " zwei");
        }
    }
}
