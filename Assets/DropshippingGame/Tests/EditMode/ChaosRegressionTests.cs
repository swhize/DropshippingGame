using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>
    /// Regressionstests zu den Fehlern, die der Chaos-Test (<see cref="ChaosTests"/>) gefunden hat –
    /// je Fehler ein gezielter Test.
    /// </summary>
    public class ChaosRegressionTests
    {
        private static Sim Biz(int seed = 3)
        {
            var gm = new Sim(seed);
            gm.NewGame("skip");
            gm.InGame = true;
            return gm;
        }

        private static string Snap(Sim gm)
        {
            var d = gm.ToJson();
            d.Remove("saved_unix");
            return Json.Write(d);
        }

        private static string Roundtrip(Sim gm)
        {
            var loaded = new Sim(99);
            loaded.FromJson(J.Obj(Json.Parse(Json.Write(gm.ToJson()))));
            return Snap(loaded);
        }

        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

        // ---- Zeit -------------------------------------------------------------------------------
        [Test]
        public void BigTimeStep_DoesNotRunPastEightPm()
        {
            // Früher lief AdvanceMinutes über 20 Uhr hinaus: Boosts/Bestellungen der Geschäftsuhr lagen dann
            // schon im nächsten Morgen (Ruckler, Zeitfaktor, Zeitsprung-Ereignis).
            var gm = Biz();
            gm.AdvanceMinutes(GameData.DayEnd - GameData.DayStart - 10f); // 19:50
            gm.AddBoost("event", "Test", 2f, 40f); // läuft bis morgen 08:30
            gm.AdvanceMinutes(120f);
            Assert.IsTrue(gm.DayOver, "Feierabend");
            Assert.AreEqual(GameData.DayEnd, gm.TimeMinutes, 0.001f);
            Assert.IsNotNull(gm.ActiveBoost("event"), "Boost bis morgen 08:30 läuft noch");
            Assert.LessOrEqual(gm.BClock(), gm.Day * GameData.BusinessMinutes + 0.001f, "Uhr nicht im nächsten Tag");
        }

        [Test]
        public void TimeScale_NaNOrNegative_DoesNotBreakTheClock()
        {
            var gm = Biz();
            gm.AdminTimeScale = float.NaN;
            gm.Tick(1f);
            Assert.IsTrue(Finite(gm.TimeMinutes), "Uhr bleibt endlich");
            float t = gm.TimeMinutes;
            gm.AdvanceMinutes(-50f);
            gm.AdvanceMinutes(float.NaN);
            gm.AdminSkipMinutes(float.NaN);
            Assert.AreEqual(t, gm.TimeMinutes, 0.001f, "Keine Zeit rückwärts / NaN");
        }

        // ---- Ereignisse ---------------------------------------------------------------------------
        [Test]
        public void ChoiceThatEndsTheDay_IsDecidedOnlyOnce()
        {
            // Kalle (2 Stunden aushelfen) um 19 Uhr: der Zeitsprung beendet den Tag mitten in der Entscheidung.
            // Früher entschied ResolvePendingChoices dieselbe Mail dann noch einmal mit der Standardwahl.
            var gm = Biz();
            gm.AdvanceMinutes(GameData.DayEnd - GameData.DayStart - 60f);
            var mail = gm.Events.Trigger("kalle_calls");
            Assert.IsTrue(mail.Pending);
            int ended = 0;
            string chosenAtDayEnd = null;
            gm.DayEnded += _ =>
            {
                ended++;
                chosenAtDayEnd = mail.Chosen;
            };
            int money = gm.Money;
            gm.Events.Choose(mail.Id, 0);
            Assert.IsTrue(gm.DayOver, "Zeitsprung beendet den Tag");
            Assert.AreEqual(1, ended);
            Assert.IsFalse(mail.Pending);
            Assert.AreEqual("Aushelfen (2 Stunden weg)", chosenAtDayEnd, "Beim Feierabend gilt schon die eigene Wahl (nicht die Standardwahl)");
            Assert.AreEqual("Aushelfen (2 Stunden weg)", mail.Chosen, "Die eigene Wahl bleibt bestehen");
            StringAssert.Contains("Alte Zeiten", mail.Result);
            Assert.AreEqual(money + 80, gm.Money, "Lohn genau einmal");
        }

        [Test]
        public void ScheduledEvent_DoesNotFireWhenItsPreconditionIsGone()
        {
            // "Zollkontrolle" wurde morgens eingeplant (Lieferung unterwegs). Ist die Lieferung bis dahin da,
            // fällt das Ereignis aus – früher kam die Mail trotzdem ("Eine deiner Lieferungen ...").
            var gm = Biz();
            gm.Day = 3;
            gm.Events.Schedule.Clear();
            gm.Events.Schedule.Add(new EventSystem.Scheduled { Id = "customs", At = gm.TimeMinutes });
            Assert.AreEqual(0, gm.TravelingDeliveries.Count);
            int mails = gm.Events.Mails.Count;
            gm.Events.Process();
            Assert.AreEqual(mails, gm.Events.Mails.Count, "Keine Zoll-Mail ohne Lieferung");
            Assert.AreEqual(0, gm.Events.Schedule.Count, "Termin verbraucht");
        }

        // ---- Unbekannte IDs und ungültige Indizes -------------------------------------------------
        [Test]
        public void UnknownProductIds_AreIgnored()
        {
            var gm = Biz();
            gm.SetListed("quatsch", true);
            gm.SetShopPrice("quatsch", 50);
            Assert.IsFalse(gm.Listed.ContainsKey("quatsch"), "Kein Phantom-Produkt im Shop");
            Assert.IsFalse(gm.ShopPrices.ContainsKey("quatsch"));
            Assert.IsFalse(gm.LaunchOrders.ContainsKey("quatsch"), "Keine Startbestellung für ein Phantom-Produkt");
            Assert.IsFalse(gm.ProductUnlocked("quatsch"));
            Assert.IsNull(gm.PickItem("quatsch"));
            gm.AddXp(GameData.LevelThreshold(3));
            var c = gm.GenerateContractOffer(1f, false, "quatsch");
            Assert.IsNotNull(c);
            Assert.IsTrue(GameData.IsProduct(c.Product), "Auftrag nur für echte Produkte (war: 'quatsch')");
            Assert.AreEqual(Roundtrip(gm), Snap(gm), "Speichern→Laden bleibt gleich");
        }

        [Test]
        public void InvalidIndices_DoNotThrow()
        {
            var gm = Biz();
            gm.Money = 100000;
            Assert.IsFalse(gm.BuyBulk(-1, 0, 0));
            Assert.IsFalse(gm.BuyBulk(0, 99, 0));
            Assert.IsFalse(gm.BuyBulk(0, 0, int.MinValue));
            Assert.IsFalse(gm.BulkAvailable(42));
            Assert.IsFalse(gm.SupplierAvailable(-3));
            Assert.IsFalse(gm.BuyPackaging(3));
            Assert.IsFalse(gm.BuyPackaging(0, false, 17));
            Assert.Greater(gm.PackagingCost(-1, false, 99), 0);
            Assert.IsFalse(gm.StartAdCampaign(GameData.AdTiers.Length));
            Assert.IsFalse(gm.LoanAvailable(-1));
            Assert.Greater(gm.LeadMinutes(99), 0f);
            Assert.AreEqual(100000, gm.Money, "Nichts abgebucht");
        }

        [Test]
        public void HireUnknownRole_IsRejected()
        {
            // Früher: beliebig viele "quatsch"-Kräfte (zählten als Lagerist:in beim Lohn, arbeiteten aber nie).
            var gm = Biz();
            gm.AddXp(GameData.LevelThreshold(GameData.StaffLevel));
            gm.Upgrades.Add("warehouse");
            gm.LocationStage = 1;
            for (int i = 0; i < 5; i++) Assert.IsFalse(gm.Hire("quatsch"));
            Assert.AreEqual(0, gm.Staff.Count);
            Assert.AreEqual(0, gm.WagesPerDay());
            Assert.IsTrue(gm.Hire("lager"), "Echte Rollen gehen weiter");
        }

        [Test]
        public void UpgradeState_UnknownId_IsUnknown()
        {
            var gm = Biz();
            Assert.AreEqual("unknown", gm.UpgradeState("quatsch"), "Früher NullReferenceException");
            Assert.IsFalse(gm.BuyUpgrade("quatsch"));
        }

        // ---- null-Gegenstände ---------------------------------------------------------------------
        [Test]
        public void NullItems_DoNotCorruptState()
        {
            var gm = Biz();
            gm.Upgrades.Add("conveyor");
            Assert.AreEqual(0, gm.ShipPackage(null));
            gm.ReturnPackage(null);
            gm.ReturnCrate(null);
            Assert.IsFalse(gm.ConveyorInsert(null), "Früher: null auf dem Band → Absturz beim Versand und beim Speichern");
            Assert.AreEqual(0, gm.DockCrates.Count, "Keine null-Kiste am Wareneingang");
            Assert.AreEqual(0, gm.ConveyorQueue.Count);
            Assert.AreEqual(0, gm.PackedPackages.Count);
            gm.SaveDir = "";
            Assert.DoesNotThrow(() => Json.Write(gm.ToJson()));
            gm.AdvanceMinutes(30f);
        }

        // ---- Zahlen: NaN, Überlauf -----------------------------------------------------------------
        [Test]
        public void Clamp_NaN_FallsBackToLowerBound()
        {
            Assert.AreEqual(0f, Mathx.Clamp01(float.NaN));
            Assert.AreEqual(2f, Mathx.Clamp(float.NaN, 2f, 5f));
            Assert.AreEqual(0, Mathx.RoundToInt(float.NaN));
            Assert.AreEqual(int.MaxValue, Mathx.RoundToInt(1e20));
            Assert.AreEqual(int.MinValue, Mathx.RoundToInt(-1e20f));
        }

        [Test]
        public void TikTokWithNaNMeasurement_KeepsTheShopAlive()
        {
            // Ein NaN-Messwert (z. B. Licht) machte die Wertung NaN → NaN-Boost, der nie auslief: keine
            // Bestellungen mehr (Bestellintervall NaN).
            var gm = Biz();
            gm.AddXp(GameData.LevelThreshold(GameData.TikTokLevel));
            gm.Stock["huelle"].Qty = 10;
            var take = new TikTokTake { Duration = 12f, VisibleFrac = 0.9f, Centered = 0.8f, DistanceFrac = 0.8f, SteadyFrac = 0.9f, Light = float.NaN, AngleDegrees = 90f, Hook = true, Actions = 2 };
            var rating = TikTokScoring.Rate(take, TikTokFormats.Review, gm.TrendingTikTokFormat());
            Assert.IsTrue(Finite(rating.Score), "Wertung endlich");
            Assert.IsNotNull(gm.PostTikTokVideo(rating, "huelle", TikTokFormats.Review));
            gm.TriggerTikTok(float.NaN, true);
            foreach (var b in gm.Boosts) Assert.IsTrue(Finite(b.Mult) && Finite(b.EndsAt), "Boost endlich: " + b.Source);
            Assert.IsTrue(Finite(gm.OrderIntervalMinutes()), "Bestellintervall endlich");
            Assert.IsTrue(Finite(gm.DemandRate("huelle")));
        }

        [Test]
        public void SellWithNaNFraction_ChangesNothing()
        {
            // Früher: Holdings wurden NaN und das Konto bekam int.MinValue gutgeschrieben.
            var gm = Biz();
            gm.Money = 5000;
            Assert.IsTrue(gm.Market.Buy("ETF", 1000));
            float units = gm.Market.Holdings["ETF"];
            int money = gm.Money;
            Assert.AreEqual(0, gm.Market.Sell("ETF", float.NaN));
            Assert.AreEqual(units, gm.Market.Holdings["ETF"]);
            Assert.AreEqual(money, gm.Money);
        }

        [Test]
        public void MarketAdmin_NonFiniteMultiplier_IsIgnored()
        {
            var gm = Biz();
            float p = gm.Market.Prices["KART"];
            gm.Market.AdminMoveAll(float.PositiveInfinity, false);
            gm.Market.AdminMoveAll(float.NaN, false);
            gm.Market.AdminMoveAll(-2f, false);
            gm.Market.Shock("DROP", float.NaN);
            Assert.AreEqual(p, gm.Market.Prices["KART"]);
            Assert.IsTrue(Finite(gm.Market.Prices["DROP"]) && gm.Market.Prices["DROP"] > 0f);
        }

        [Test]
        public void CrashedCryptoPrice_SurvivesLoading()
        {
            // Spielstände setzten alle Kurse auf mind. 0,05 € – ein abgestürzter Krypto-Kurs (Untergrenze 0,01 €)
            // sprang beim Laden hoch (Gewinn durch Speichern/Laden).
            var gm = Biz();
            gm.Market.Prices["KEKS"] = 0.02f;
            var loaded = new Sim(5);
            loaded.FromJson(J.Obj(Json.Parse(Json.Write(gm.ToJson()))));
            Assert.AreEqual(0.02f, loaded.Market.Prices["KEKS"], 1e-6f);
            Assert.GreaterOrEqual(loaded.Market.Prices["DROP"], 0.05f, "Original-Anlagen behalten ihre Untergrenze");
        }

        [Test]
        public void Overflow_MoneyXpSavingsDefects_Saturate()
        {
            var gm = Biz();
            gm.AdminAddMoney(int.MaxValue);
            gm.AdminAddMoney(int.MaxValue);
            Assert.AreEqual(Sim.AdminMoneyLimit, gm.Money, "Admin-Geld begrenzt (war: Überlauf ins Minus)");
            gm.AdjustMoney(int.MaxValue, "other");
            Assert.AreEqual(Sim.MoneyLimit, gm.Money);
            gm.AdminAddXp(int.MaxValue);
            gm.AdminAddXp(int.MaxValue);
            Assert.Greater(gm.Xp, 0, "XP laufen nicht über (war: negativ)");
            Assert.AreEqual(GameData.MaxLevel, gm.Level);
            Assert.IsTrue(gm.Market.Deposit(int.MaxValue - 5));
            gm.Money = Sim.MoneyLimit;
            gm.Market.Deposit(int.MaxValue);
            Assert.GreaterOrEqual(gm.Market.Savings, 0, "Sparkonto läuft nicht über");
            gm.AddDefectBatch("huelle", int.MaxValue);
            gm.AddDefectBatch("huelle", int.MaxValue);
            Assert.Greater(gm.DefectsOf("huelle"), 0, "Defekte laufen nicht über");
            Assert.IsFalse(gm.UnboxCrate("huelle", int.MaxValue, 1f), "Riesenkiste passt nicht (war: Überlauf der Lagerprüfung)");
            Assert.IsFalse(gm.UnboxCrate("huelle", -5, 1f));
            Assert.GreaterOrEqual(gm.StockQty("huelle"), 0);
        }

        [Test]
        public void ContractPayMultipliers_AreSane()
        {
            var gm = Biz();
            gm.AddXp(GameData.LevelThreshold(3));
            gm.Contracts.Clear();
            var c = gm.GenerateContractOffer(float.NaN);
            Assert.IsNotNull(c);
            Assert.Greater(c.Payment, 0);
            Assert.Less(c.Payment, 1000000);
            Assert.IsTrue(gm.AcceptContract(c.Id));
            c.DeadlineDay = gm.Day + 2;
            Assert.IsFalse(gm.RushContract(c, float.PositiveInfinity));
            Assert.IsTrue(gm.RushContract(c, 1e9f));
            Assert.Less(c.Payment, 1000000, "Eilzuschlag gedeckelt (war: int-Überlauf beim Bezahlen)");
            Assert.Greater(c.Penalty, 0);
        }

        [Test]
        public void AdBudget_IsCapped()
        {
            var gm = Biz();
            gm.AddXp(GameData.LevelThreshold(GameData.GugelLevel));
            gm.Money = 2000000000;
            gm.SetListed("huelle", true);
            Assert.IsNull(gm.StartFakebookAd("huelle", 0, 0, 1000000000), "Kein Milliarden-Budget (Ausgaben pro Schritt liefen über int)");
            Assert.IsNotNull(gm.StartFakebookAd("huelle", 0, 0, 250));
        }

        [Test]
        public void BonusChallenge_NaNTarget_GetsAValidTarget()
        {
            var gm = Biz();
            var c = gm.AddBonusChallenge("ship", float.NaN, 100, 10, "Test", "T", "D");
            Assert.AreEqual(1f, c.Target, "Ziel endlich (war: NaN, nach dem Laden 0)");
            var loaded = new Sim(4);
            loaded.FromJson(J.Obj(Json.Parse(Json.Write(gm.ToJson()))));
            Assert.AreEqual(1f, loaded.Challenges.Find(x => x.Id == c.Id).Target);
        }

        [Test]
        public void NegativeSkillPoints_NeverDropBelowLearnedSkills()
        {
            var gm = Biz();
            gm.AddSkillPoints(2);
            foreach (var s in GameData.Skills)
                if (s.Tier == 1) gm.LearnSkill(s.Id);
            int learned = gm.Skills.Count;
            Assert.Greater(learned, 0);
            gm.AddSkillPoints(-10);
            Assert.GreaterOrEqual(gm.SkillPointsTotal(), gm.Skills.Count, "Nicht mehr Skills als Punkte");
        }

        [Test]
        public void ReputationAndAwareness_IgnoreNaN()
        {
            var gm = Biz();
            float rep = gm.Reputation, aw = gm.Awareness;
            gm.ChangeReputation(float.NaN);
            gm.AddAwareness(float.NaN);
            Assert.AreEqual(rep, gm.Reputation);
            Assert.AreEqual(aw, gm.Awareness);
            gm.AdminSetReputation(float.NaN);
            Assert.IsTrue(Finite(gm.Reputation) && gm.Reputation >= 0.5f);
        }

        // ---- Speichern/Laden verlustfrei ------------------------------------------------------------
        [Test]
        public void PlaySeconds_SurviveLoadingExactly()
        {
            // Spielzeit (double) wurde als float gelesen und verlor bei jedem Laden Nachkommastellen.
            var gm = Biz();
            gm.Stats.PlaySeconds = 267.8522415161133;
            Assert.AreEqual(Snap(gm), Roundtrip(gm));
        }

        [Test]
        public void AdBoostOrder_SurvivesLoading()
        {
            // Nach dem Laden hängte AdsSyncBoosts die Werbe-Boosts hinten an – die Reihenfolge (und damit das
            // gespeicherte JSON) änderte sich bei jedem Speichern/Laden.
            var gm = Biz();
            gm.AddXp(GameData.LevelThreshold(GameData.GugelLevel));
            gm.Money = 5000;
            gm.SetListed("huelle", true);
            Assert.IsNotNull(gm.StartFakebookAd("huelle", 0, 0, 50));
            gm.AddBoost("event", "Danach", 1.5f, 60f);
            Assert.AreEqual("event", gm.Boosts[gm.Boosts.Count - 1].Source);
            Assert.AreEqual(Snap(gm), Roundtrip(gm));
        }

        [Test]
        public void ExpiredAd_EndsEvenWithoutTimePassing()
        {
            // Zeit angehalten (m = 0): UpdateBoosts entfernte den Boost einer abgelaufenen Kampagne, AdsUpdate
            // beendete sie aber nicht – nach dem Laden war der Boost wieder da.
            var gm = Biz();
            gm.AddXp(GameData.LevelThreshold(GameData.FakebookLevel));
            gm.Money = 5000;
            gm.SetListed("huelle", true);
            var c = gm.StartFakebookAd("huelle", 0, 0, 50);
            c.EndsAt = gm.BClock() - 1f;
            gm.AdsSyncBoosts();
            gm.AdvanceMinutes(0f);
            Assert.IsFalse(c.Active, "Kampagne beendet");
            Assert.IsNull(gm.ActiveBoost("ad:" + c.Id));
            Assert.AreEqual(Snap(gm), Roundtrip(gm));
        }

        // ---- Endlos-Modus -----------------------------------------------------------------------------
        [Test]
        public void Endless_StartsAfterTheIpo_EvenWithOptionalGoalsOpen()
        {
            // Früher blockierten offene feste Ziele (Verkaufsstand, Außenlager, Team …) den Endlos-Modus komplett.
            var gm = Biz();
            gm.TotalEarned = 2100000;
            gm.CheckGoals();
            Assert.IsTrue(gm.GoalsDone.Contains("ipo"), "Börsengang erreicht");
            Assert.IsFalse(gm.GoalsDone.Contains("outpost"), "Außenlager fehlt noch");
            Assert.IsNotNull(gm.NextGoal(), "Ziel-Anzeige zeigt das offene feste Ziel");
            Assert.AreEqual(1, gm.EndlessTier, "2 Mio. trotzdem geschafft");
        }

        [Test]
        public void Endless_NoRewardLoopAtTheIntLimit()
        {
            var gm = Biz();
            foreach (var g in GameData.Goals) gm.GoalsDone.Add(g.Id);
            gm.EndlessTier = 11; // Ziel wäre 2 Mio. × 2^11 ≈ 4,1 Mrd. > int
            gm.TotalEarned = int.MaxValue;
            int money = gm.Money;
            gm.CheckGoals();
            gm.CheckGoals();
            Assert.AreEqual(11, gm.EndlessTier, "Keine weiteren Stufen jenseits von int");
            Assert.AreEqual(money, gm.Money, "Keine Belohnungs-Schleife");
            Assert.DoesNotThrow(() => gm.CurrentObjective());
        }
    }
}
