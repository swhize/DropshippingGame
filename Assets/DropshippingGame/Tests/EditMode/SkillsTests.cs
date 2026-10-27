using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>F5 Hustle-Skills: Punkte, Baum, Effekte in den Formeln, Welt-Flags.</summary>
    public class SkillsTests
    {
        [Test]
        public void SkillTree_HasThreeBranchesWithFourTiers()
        {
            Assert.AreEqual(12, GameData.Skills.Length, "12 Skills");
            Assert.AreEqual(3, GameData.SkillBranches.Length, "3 Äste");
            var ids = new HashSet<string>();
            foreach (var b in GameData.SkillBranches)
            {
                var list = Sim.SkillsOfBranch(b.Id);
                Assert.AreEqual(4, list.Count, "4 Stufen in " + b.Name);
                for (int t = 0; t < 4; t++)
                {
                    var s = list[t];
                    Assert.AreEqual(t + 1, s.Tier, "Stufe " + (t + 1));
                    Assert.AreEqual(GameData.SkillTierLevels[t], s.Level, "Mindestlevel " + s.Id);
                    Assert.IsFalse(string.IsNullOrEmpty(s.Name) || string.IsNullOrEmpty(s.Desc) || string.IsNullOrEmpty(s.Icon), "Texte " + s.Id);
                    Assert.IsTrue(ids.Add(s.Id), "Eindeutige ID " + s.Id);
                    Assert.AreSame(t == 0 ? null : list[t - 1], Sim.PreviousSkill(s), "Vorstufe " + s.Id);
                }
                Assert.IsNotNull(GameData.SkillBranch(b.Id));
            }
        }

        [Test]
        public void SkillPoints_OneAtStartPlusOnePerLevel()
        {
            var gm = TestUtil.Fresh();
            var learned = new List<string>();
            gm.SkillLearned += s => learned.Add(s.Id);
            Assert.AreEqual(1, gm.SkillPointsAvailable(), "Ein Punkt zum Start");
            Assert.AreEqual("available", gm.SkillState("l_arme"));
            Assert.AreEqual("level", gm.SkillState("l_tetris"), "Stufe 2 erst ab Level 3");
            Assert.AreEqual("unknown", gm.SkillState("gibtsnicht"));
            Assert.IsTrue(gm.LearnSkill("l_arme"), "Gelernt");
            Assert.IsFalse(gm.LearnSkill("l_arme"), "Nicht doppelt");
            Assert.AreEqual(0, gm.SkillPointsAvailable());
            Assert.AreEqual("learned", gm.SkillState("l_arme"));
            Assert.AreEqual("points", gm.SkillState("v_feilschen"), "Kein Punkt frei");
            Assert.IsFalse(gm.LearnSkill("v_feilschen"));
            gm.AddXp(GameData.LevelThreshold(3));
            Assert.AreEqual(3, gm.Level);
            Assert.AreEqual(2, gm.SkillPointsAvailable(), "+1 je Level-Aufstieg");
            Assert.AreEqual("requires", gm.SkillState("v_kulanz"), "Vorstufe fehlt");
            Assert.IsTrue(gm.LearnSkill("l_tetris"), "Stufe 2 mit Vorstufe");
            Assert.IsTrue(gm.LearnSkill("v_feilschen"));
            Assert.AreEqual(0, gm.SkillPointsAvailable());
            CollectionAssert.AreEqual(new[] { "l_arme", "l_tetris", "v_feilschen" }, learned, "SkillLearned gemeldet");
            Assert.IsTrue(gm.Events.Mails.Exists(m => m.Title == GameData.MailSkills[0]), "Erklärung beim ersten Level-Aufstieg");
            gm.Events.Apply(new Effects { SkillPoints = 1 });
            Assert.AreEqual(1, gm.SkillPointsAvailable(), "Bonuspunkt (z. B. Seminar)");
            Assert.AreEqual(4, gm.SkillPointsTotal());
        }

        [Test]
        public void Skills_ChangeTheFormulas()
        {
            var a = TestUtil.Fresh();
            var b = TestUtil.Fresh();
            a.Level = b.Level = 10;
            b.Skills.UnionWith(TestUtil.AllSkillIds());
            Assert.IsTrue(b.BulkCost(0, 1, 1) < a.BulkCost(0, 1, 1), "Feilschen: günstiger einkaufen");
            Assert.AreEqual(Mathx.RoundToInt(a.BulkCost(4, 2, 1) * 0.9f), b.BulkCost(4, 2, 1), 1, "−10 %");
            Assert.AreEqual(a.LeadMinutes(1) * 0.7f, b.LeadMinutes(1), 0.01f, "Kurze Wege: −30 % Lieferzeit");
            Assert.AreEqual(Mathx.RoundToInt(a.Capacity() * 1.3f), b.Capacity(), "Lager-Tetris: +30 % Platz");
            Assert.AreEqual(a.QueueCapacity() + 3, b.QueueCapacity(), "Stammkundschaft: +3 Warteschlange");
            var pkg = new ItemData { Kind = ItemKind.Labeled, Product = "huelle", Quality = 1f, PackSize = 0 };
            Assert.AreEqual(a.ReturnChance(pkg) * 0.65f, b.ReturnChance(pkg), 0.0001f, "Kundenflüsterer: −35 % Retouren");
            Assert.AreEqual(GameData.AdTiers[1].Cost, a.AdCost(1));
            Assert.AreEqual(Mathx.RoundToInt(GameData.AdTiers[1].Cost * 0.75f), b.AdCost(1), "Viral-Gen: Werbung −25 %");
            Assert.AreEqual(GameData.TikTokCooldown * 0.6f, b.TikTokCooldownMinutes(), 0.01f, "Viral-Gen: kürzere Abklingzeit");
            Assert.IsTrue(b.TrendForecastAccuracy() > a.TrendForecastAccuracy() && b.TrendSightDays() == 3, "Trendradar");
            Assert.AreEqual(a.MaxActiveContracts() + 1, b.MaxActiveContracts(), "Networking: +1 Großauftrag");
            Assert.AreEqual(1.25f, b.StaffSpeedMult(), 0.001f, "Prozess-Flow: Personal schneller");
            Assert.AreEqual(3f, b.ConveyorMinutes(), 0.001f, "Prozess-Flow: Förderband schneller");
            Assert.AreEqual(0.85f, a.CarrySpeedMult(ItemKind.Crate), 0.001f, "Ohne Skill bremst eine Kiste");
            Assert.AreEqual(1f, a.CarrySpeedMult(ItemKind.Package), 0.001f);
            Assert.AreEqual(1.1f, b.CarrySpeedMult(ItemKind.Crate), 0.001f, "Starke Arme: schneller mit vollen Händen");
            Assert.AreEqual(1f, b.CarrySpeedMult(ItemKind.None), 0.001f, "Mit leeren Händen normal");
            a.TriggerTikTok(0.5f, true);
            b.TriggerTikTok(0.5f, true);
            Assert.IsTrue(b.ActiveBoost("tiktok").Mult > a.ActiveBoost("tiktok").Mult, "Content Creator: stärkere TikToks");
            Assert.IsTrue(b.ActiveBoost("tiktok").EndsAt > a.ActiveBoost("tiktok").EndsAt, "... und länger");
            Assert.IsTrue(b.TikTokReadyAt < a.TikTokReadyAt, "Kürzere Abklingzeit");
        }

        [Test]
        public void Networking_PaysMoreForContracts()
        {
            var a = TestUtil.Fresh(4);
            var b = TestUtil.Fresh(4);
            a.Level = b.Level = 6;
            b.Skills.Add("v_netzwerk");
            var ca = a.GenerateContractOffer(1f, false, "huelle");
            var cb = b.GenerateContractOffer(1f, false, "huelle");
            Assert.AreEqual(ca.Quantity, cb.Quantity, "Gleiches Angebot");
            Assert.AreEqual(ca.Payment * 1.2f, cb.Payment, 6f, "+20 % Vergütung (auf 5 € gerundet)");
        }

        [Test]
        public void Stammkundschaft_SoftensBadReviews()
        {
            float Rep(bool skill)
            {
                var gm = TestUtil.Fresh(13);
                if (skill) gm.Skills.Add("v_stamm");
                gm.Reputation = 4.5f;
                gm.ReviewCount = 50;
                for (int i = 0; i < 40; i++)
                {
                    var pkg = TestUtil.Labeled(gm, "huelle");
                    pkg.Created -= 1000f;
                    pkg.DueAt -= 1000f;
                    gm.ShipPackage(pkg);
                }
                return gm.Reputation;
            }
            Assert.IsTrue(Rep(true) > Rep(false) + 0.2f, "Schlechte Bewertungen wirken nur halb so stark");
        }

        [Test]
        public void Trendsetter_GoodTikTokStartsHype()
        {
            var gm = TestUtil.Fresh();
            gm.Level = 7;
            gm.SetListed("led", true);
            gm.Stock["led"].Qty = 50;
            gm.Skills.Add("m_trendsetter");
            var st = gm.Trends.State("led");
            st.Phase = TrendPhase.Normal;
            gm.TriggerTikTok(0.4f);
            Assert.AreEqual(TrendPhase.Normal, gm.Trends.Phase("led"), "Schwaches TikTok reicht nicht");
            gm.TriggerTikTok(0.9f, true, "led");
            Assert.AreEqual(TrendPhase.Normal, gm.Trends.Phase("led"), "TikToks der Praktikant:in lösen keinen Hype aus");
            gm.TriggerTikTok(0.9f, false, "led");
            Assert.AreEqual(TrendPhase.Rising, gm.Trends.Phase("led"), "Gutes TikTok startet einen Hype");
        }

        [Test]
        public void Respec_CostsMoneyAndFreesPoints()
        {
            var gm = TestUtil.Fresh();
            gm.AddXp(GameData.LevelThreshold(3));
            gm.LearnSkill("l_arme");
            gm.LearnSkill("v_feilschen");
            gm.Money = 100;
            Assert.IsFalse(gm.ResetSkills(), "Zu teuer");
            gm.Money = 1000;
            int cost = gm.RespecCost();
            Assert.AreEqual(GameData.RespecCostPerLevel * 3, cost);
            Assert.IsTrue(gm.ResetSkills(), "Umschulung");
            Assert.AreEqual(1000 - cost, gm.Money, "Bezahlt");
            Assert.AreEqual(0, gm.Skills.Count, "Alles vergessen");
            Assert.AreEqual(3, gm.SkillPointsAvailable(), "Punkte wieder frei");
        }
    }
}
