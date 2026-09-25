using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>F3 Trends/Hype: Nachfrage-Faktor, Zyklen, Trendradar, Auslöser.</summary>
    public class TrendsTests
    {
        [Test]
        public void Hype_MultipliesDemand()
        {
            var gm = TestUtil.Fresh();
            gm.SetListed("huelle", true);
            var st = gm.Trends.State("huelle");
            st.Start = st.Hype = 1f;
            float normal = gm.DemandRate("huelle");
            st.Start = st.Hype = 2f;
            Assert.AreEqual(normal * 2f, gm.DemandRate("huelle"), 0.0001f, "Hype 2,0 verdoppelt die Nachfrage");
            st.Start = st.Hype = 0.5f;
            Assert.AreEqual(normal * 0.5f, gm.DemandRate("huelle"), 0.0001f, "Toter Trend halbiert sie");
            Assert.AreEqual(0.5f, gm.TrendMult("huelle"), 0.0001f, "Sim.TrendMult liefert den Faktor");
        }

        [Test]
        public void Hype_ChangesSmoothlyOverTheDay()
        {
            var gm = TestUtil.Fresh();
            var st = gm.Trends.State("led");
            st.Start = 1f;
            st.Hype = 1.6f;
            st.StartAt = GameData.DayStart;
            Assert.AreEqual(1f, gm.TrendMult("led"), 0.001f, "08:00: Startwert");
            gm.TimeMinutes = (GameData.DayStart + GameData.DayEnd) / 2f;
            Assert.AreEqual(1.3f, gm.TrendMult("led"), 0.001f, "14:00: halber Weg");
            gm.TimeMinutes = GameData.DayEnd;
            Assert.AreEqual(1.6f, gm.TrendMult("led"), 0.001f, "20:00: Zielwert");
        }

        [Test]
        public void Cycles_StayInRangeAndAverageNearOne()
        {
            var gm = TestUtil.Fresh(5);
            var seen = new HashSet<TrendPhase>();
            double sum = 0;
            int n = 0;
            for (int d = 0; d < 400; d++)
            {
                gm.Trends.NewDay();
                int hyped = 0;
                foreach (var p in GameData.Products)
                {
                    var st = gm.Trends.State(p.Id);
                    Assert.IsTrue(st.Hype >= GameData.TrendMin && st.Hype <= GameData.TrendMax, "Hype im Bereich 0,5-2,0");
                    seen.Add(st.Phase);
                    if (st.Phase == TrendPhase.Rising || st.Phase == TrendPhase.Peak) hyped++;
                    sum += st.Hype;
                    n++;
                }
                Assert.IsTrue(hyped <= GameData.MaxHypedProducts, "Höchstens " + GameData.MaxHypedProducts + " Hypes gleichzeitig");
            }
            Assert.AreEqual(5, seen.Count, "Alle Phasen kommen vor (stabil, steigend, Peak, fallend, tot)");
            double avg = sum / n;
            Assert.IsTrue(avg >= 0.95 && avg <= 1.2, "Im Mittel ungefähr 1 (" + avg + ")");
            Assert.AreEqual(GameData.TrendHistoryDays, gm.Trends.State("huelle").History.Count, "Verlauf der letzten 14 Tage");
        }

        [Test]
        public void Forecast_IsMoreAccurateWithTrendradar()
        {
            double Err(bool skill, int ahead)
            {
                var gm = TestUtil.Fresh(11);
                if (skill) gm.Skills.Add("m_radar");
                double err = 0;
                int n = 0;
                var pending = new List<Dictionary<string, float>>();
                for (int d = 0; d < 300; d++)
                {
                    var fc = new Dictionary<string, float>();
                    foreach (var p in GameData.Products) fc[p.Id] = gm.Trends.Forecast(p.Id).Values[ahead - 1];
                    pending.Add(fc);
                    gm.Day++;
                    gm.Trends.NewDay();
                    if (pending.Count < ahead) continue;
                    var due = pending[pending.Count - ahead];
                    foreach (var p in GameData.Products)
                    {
                        err += Math.Abs(due[p.Id] - gm.Trends.State(p.Id).Hype);
                        n++;
                    }
                }
                return err / n;
            }
            double plain1 = Err(false, 1), radar1 = Err(true, 1), plain3 = Err(false, 3), radar3 = Err(true, 3);
            TestContext.Out.WriteLine($"Fehler morgen: {plain1:0.000} → {radar1:0.000}, in 3 Tagen: {plain3:0.000} → {radar3:0.000}");
            Assert.IsTrue(radar1 < plain1 * 0.75, "Trendradar ist für morgen genauer");
            Assert.IsTrue(radar3 < plain3 * 0.8, "... und für in drei Tagen");
            Assert.AreEqual(3, TestUtil.Fresh().Trends.Forecast("huelle").Values.Length, "3-Tage-Prognose");
        }

        [Test]
        public void Forecast_DoesNotChangeTheSimulation()
        {
            var a = TestUtil.Fresh(9);
            var b = TestUtil.Fresh(9);
            for (int i = 0; i < 50; i++)
                foreach (var p in GameData.Products)
                    a.Trends.Forecast(p.Id);
            Assert.AreEqual(a.Rng.Value(), b.Rng.Value(), "Prognosen verbrauchen keine Zufallszahlen");
        }

        [Test]
        public void Forecast_SeesUpcomingHypeWithRadar()
        {
            var gm = TestUtil.Fresh(2);
            var st = gm.Trends.State("led");
            st.Phase = TrendPhase.Normal;
            st.DaysLeft = 3;
            st.PhaseDays = 5;
            st.Start = st.Hype = 1f;
            st.RiseDays = 2;
            st.Peak = 1.9f;
            var plain = gm.Trends.Forecast("led");
            Assert.AreEqual("stabil", plain.Label, "Ohne Radar bleibt der Hype in 2 Tagen unsichtbar");
            gm.Skills.Add("m_radar");
            var radar = gm.Trends.Forecast("led");
            Assert.AreEqual("Hype im Anflug", radar.Label, "Mit Radar sichtbar");
            Assert.IsTrue(radar.Values[2] > 1.2f, "Prognose steigt");
            Assert.IsTrue(radar.High[2] >= radar.Values[2] && radar.Low[2] <= radar.Values[2], "Unsicherheitsband");
            Assert.IsFalse(string.IsNullOrEmpty(radar.Hint), "Tipp vorhanden");
        }

        [Test]
        public void Events_TriggerHypeForTheirProduct()
        {
            var gm = TestUtil.Fresh();
            gm.Level = 2;
            gm.SetListed("huelle", true);
            var changes = new List<string>();
            gm.TrendChanged += (id, phase) => changes.Add(id + ":" + phase);
            var mail = gm.Events.Trigger("trend_alarm");
            Assert.AreEqual("huelle", mail.CtxProduct, "Kontext-Produkt");
            Assert.IsTrue(mail.Text.Contains("Handyhülle") && mail.Text.Contains("#HülleChallenge"), "Platzhalter ersetzt: " + mail.Text);
            Assert.AreEqual(TrendPhase.Rising, gm.Trends.Phase("huelle"), "Trend steigt");
            Assert.IsTrue(changes.Contains("huelle:Rising"), "TrendChanged gemeldet");
            Assert.AreEqual("steigend", gm.TrendLabel("huelle"), "Label");
            gm.TimeMinutes = GameData.DayEnd;
            Assert.IsTrue(gm.TrendMult("huelle") >= 1.25f, "Schon heute spürbar mehr Nachfrage");

            var gm2 = TestUtil.Fresh(3);
            gm2.SetListed("huelle", true);
            gm2.Events.Trigger("viral");
            Assert.AreEqual(TrendPhase.Rising, gm2.Trends.Phase("huelle"), "Viraler Hit startet einen Hype");
            Assert.AreEqual("huelle", gm2.Boosts[0].Product, "Boost und Hype betreffen dasselbe Produkt");
        }

        [Test]
        public void TrendCrash_AndReactionVideo()
        {
            var gm = TestUtil.Fresh(6);
            gm.Level = 3;
            gm.SetListed("led", true);
            gm.Level = 3;
            var mail = gm.Events.Trigger("trend_crash");
            gm.Events.Choose(mail.Id, 1);
            Assert.AreEqual(TrendPhase.Falling, gm.Trends.Phase(mail.CtxProduct), "Aussitzen: Hype flaut ab");
            gm.Trends.Trigger(mail.CtxProduct, TrendPhase.Dead);
            Assert.AreEqual(TrendPhase.Dead, gm.Trends.Phase(mail.CtxProduct), "Tot");
            gm.TimeMinutes = GameData.DayEnd;
            Assert.IsTrue(gm.TrendMult(mail.CtxProduct) <= 0.7f, "Kaum noch Nachfrage");
        }
    }
}
