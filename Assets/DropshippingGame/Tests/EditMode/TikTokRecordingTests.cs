using System;
using System.Collections.Generic;
using System.IO;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Echte TikTok-Aufnahmen: Bewertungsregeln, Formate, Posten und Speichern des Feeds.</summary>
    public class TikTokRecordingTests
    {
        private static TikTokTake Great() => new TikTokTake
        {
            Duration = 16f, VisibleFrac = 1f, Centered = 0.95f, DistanceFrac = 1f, SteadyFrac = 1f, Light = 1f,
            AngleDegrees = 180f, Hook = true, Actions = 4, BrandFrac = 0.5f, Hype = 1f,
        };

        private static TikTokTake Bad() => new TikTokTake
        {
            Duration = 12f, VisibleFrac = 0.5f, Centered = 0.2f, DistanceFrac = 0.1f, SteadyFrac = 0.2f, Light = 0.2f,
            AngleDegrees = 0f, Hook = false, Actions = 0, BrandFrac = 0f, Hype = 1f,
        };

        private static Sim Ready()
        {
            var sim = TestUtil.Fresh(3);
            sim.NewGame("skip");
            sim.AddXp(GameData.LevelThreshold(GameData.TikTokLevel));
            return sim;
        }

        [Test]
        public void Rate_PerfectTakeScoresHigh_BadTakeLow()
        {
            foreach (var f in TikTokFormats.All)
            {
                var good = TikTokScoring.Rate(Great(), f, "");
                var bad = TikTokScoring.Rate(Bad(), f, "");
                Assert.Greater(good.Score, 0.95f, f + " gut");
                Assert.Less(bad.Score, 0.4f, f + " schlecht");
                Assert.AreEqual(TikTokScoring.CriterionIds.Length, good.Criteria.Count);
            }
        }

        [Test]
        public void Rate_ScoreIsAlwaysClamped()
        {
            var t = Great();
            t.Hype = 5f;
            var r = TikTokScoring.Rate(t, TikTokFormats.Pov, TikTokFormats.Pov);
            Assert.AreEqual(1f, r.Score, 0.0001f);
            Assert.AreEqual(0f, TikTokScoring.Rate(new TikTokTake(), TikTokFormats.Review, "").Score, 0.0001f);
            Assert.DoesNotThrow(() => TikTokScoring.Rate(null, null, null));
        }

        [Test]
        public void Rate_FormatChangesWhatMatters()
        {
            // Hook + Aktionen, aber wackelig und ohne Winkel: Unboxing/Hack mögen das, POV nicht.
            var t = Great();
            t.SteadyFrac = 0.1f;
            t.AngleDegrees = 0f;
            t.BrandFrac = 0f;
            float unbox = TikTokScoring.Rate(t, TikTokFormats.Unboxing, "").Score;
            float pov = TikTokScoring.Rate(t, TikTokFormats.Pov, "").Score;
            Assert.Greater(unbox, pov + 0.1f);
            // Ruhig, viele Winkel, aber kein Hook: POV gut, Unboxing schwächer.
            var calm = Great();
            calm.Hook = false;
            calm.Actions = 0;
            Assert.Greater(TikTokScoring.Rate(calm, TikTokFormats.Pov, "").Score, TikTokScoring.Rate(calm, TikTokFormats.Unboxing, "").Score + 0.1f);
        }

        [Test]
        public void Rate_TrendingFormatAndHypeGiveBonus()
        {
            var t = Bad();
            float plain = TikTokScoring.Rate(t, TikTokFormats.Review, TikTokFormats.Pov).Score;
            var trending = TikTokScoring.Rate(t, TikTokFormats.Review, TikTokFormats.Review);
            Assert.IsTrue(trending.TrendingFormat);
            Assert.AreEqual(plain + TikTokScoring.TrendingFormatBonus, trending.Score, 0.0001f);
            t.Hype = 1.8f;
            Assert.Greater(TikTokScoring.Rate(t, TikTokFormats.Review, "").Score, plain);
            Assert.AreEqual(TikTokScoring.MaxHypeBonus, TikTokScoring.HypeBonus(3f), 0.0001f);
            Assert.AreEqual(0f, TikTokScoring.HypeBonus(0.6f), 0.0001f);
        }

        [Test]
        public void Rate_ProductNeverVisibleOrTooShortIsPenalized()
        {
            var t = Great();
            t.VisibleFrac = 0f;
            var r = TikTokScoring.Rate(t, TikTokFormats.Review, TikTokFormats.Review);
            Assert.LessOrEqual(r.Score, 0.15f);
            Assert.IsNotEmpty(r.Penalty);
            var s = Great();
            s.Duration = 1.5f;
            Assert.Less(TikTokScoring.Rate(s, TikTokFormats.Review, "").Score, 0.5f);
        }

        [Test]
        public void Duration_ShortVideosLoseAndReviewWantsLonger()
        {
            var t = Great();
            t.Duration = 5f;
            Assert.Less(TikTokScoring.Value(TikTokFormats.Pov, "duration", t), 0.5f);
            t.Duration = 11f;
            Assert.AreEqual(1f, TikTokScoring.Value(TikTokFormats.Pov, "duration", t), 0.0001f);
            Assert.Less(TikTokScoring.Value(TikTokFormats.Review, "duration", t), 1f);
            t.Duration = 15f;
            Assert.AreEqual(1f, TikTokScoring.Value(TikTokFormats.Review, "duration", t), 0.0001f);
        }

        [Test]
        public void PredictViews_GrowsWithScore()
        {
            Assert.Less(TikTokScoring.PredictViews(0.2f, 1f), TikTokScoring.PredictViews(0.8f, 1f));
            Assert.Less(TikTokScoring.PredictViews(0.8f, 1f), TikTokScoring.PredictViews(0.8f, 1.8f));
            Assert.AreEqual(400, TikTokScoring.PredictViews(0f, 1f));
        }

        [Test]
        public void FrameHints_ReflectMeasurements()
        {
            var h = TikTokScoring.FrameHints(true, 0.9f, 1f, 10f, 0.8f, 5f, true);
            CollectionAssert.Contains(h, "Produkt im Bild ✓");
            CollectionAssert.DoesNotContain(h, "wackelt");
            var b = TikTokScoring.FrameHints(true, 0.1f, 4f, 200f, 0.1f, 0.5f, false);
            CollectionAssert.IsSupersetOf(b, new[] { "mehr zur Mitte", "näher ran", "wackelt", "zu dunkel", "Jetzt Hook!" });
            CollectionAssert.Contains(TikTokScoring.FrameHints(false, 0f, 0f, 0f, 1f, 12f, true), "Produkt nicht im Bild!");
        }

        [Test]
        public void TrendingFormat_IsValidAndChangesOverDays()
        {
            var sim = Ready();
            var seen = new HashSet<string>();
            for (int d = 1; d <= 8; d++)
            {
                sim.Day = d;
                string f = sim.TrendingTikTokFormat();
                Assert.IsTrue(TikTokFormats.IsFormat(f));
                seen.Add(f);
            }
            Assert.AreEqual(TikTokFormats.All.Length, seen.Count);
        }

        [Test]
        public void Post_TriggersTikTokAndAddsVideo_RespectsDailyLimit()
        {
            var sim = Ready();
            Assert.IsTrue(sim.TikTokAvailable());
            var rating = TikTokScoring.Rate(Great(), TikTokFormats.Unboxing, "");
            var v = sim.PostTikTokVideo(rating, "huelle", TikTokFormats.Unboxing);
            Assert.IsNotNull(v);
            Assert.AreEqual(1, sim.Daily.TikToks);
            Assert.IsNotNull(sim.ActiveBoost("tiktok"));
            Assert.AreEqual(1, sim.TikTokVideos.Count);
            Assert.AreEqual("huelle", sim.TikTokVideos[0].Product);
            Assert.AreEqual(rating.PredictedViews, v.Views);
            Assert.IsNotEmpty(v.Title);
            // Abklingzeit: sofort nochmal geht nicht
            Assert.IsNull(sim.PostTikTokVideo(rating, "huelle", TikTokFormats.Pov));
            Assert.AreEqual(1, sim.TikTokVideos.Count);
            // Admin setzt zurück
            sim.AdminResetTikTok();
            Assert.IsNotNull(sim.PostTikTokVideo(rating, "led", TikTokFormats.Pov));
            Assert.AreEqual("led", sim.TikTokVideos[0].Product, "Neuestes zuerst");
            // Tageslimit
            while (sim.TikToksLeftToday() > 0)
            {
                sim.TikTokReadyAt = 0f;
                Assert.IsNotNull(sim.PostTikTokVideo(rating, "led", TikTokFormats.Hack));
            }
            sim.TikTokReadyAt = 0f;
            Assert.AreEqual(0, sim.TikToksLeftToday());
            Assert.IsNull(sim.PostTikTokVideo(rating, "led", TikTokFormats.Hack));
        }

        [Test]
        public void Post_KeepsAtMostLimitVideos_AndIgnoresUnknownProduct()
        {
            var sim = Ready();
            var rating = TikTokScoring.Rate(Great(), TikTokFormats.Review, "");
            for (int i = 0; i < Sim.TikTokVideoLimit + 5; i++)
            {
                sim.AdminResetTikTok();
                Assert.IsNotNull(sim.PostTikTokVideo(rating, i == 0 ? "gibtsnicht" : "huelle", TikTokFormats.Review));
            }
            Assert.AreEqual(Sim.TikTokVideoLimit, sim.TikTokVideos.Count);
            Assert.IsNull(sim.PostTikTokVideo(null, "huelle", TikTokFormats.Review));
        }

        [Test]
        public void Save_RoundTripsVideos_AndOldSavesLoadWithout()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ds_tests_tt_" + Guid.NewGuid().ToString("N"));
            try
            {
                var sim = Ready();
                sim.SaveDir = dir;
                sim.Slot = 2;
                var rating = TikTokScoring.Rate(Great(), TikTokFormats.Hack, "");
                sim.PostTikTokVideo(rating, "huelle", TikTokFormats.Hack);
                Assert.IsTrue(sim.SaveGame(true));
                var sim2 = new Sim(9) { SaveDir = dir };
                Assert.IsTrue(sim2.LoadGame(2));
                Assert.AreEqual(1, sim2.TikTokVideos.Count);
                var v = sim2.TikTokVideos[0];
                Assert.AreEqual(sim.TikTokVideos[0].Title, v.Title);
                Assert.AreEqual("huelle", v.Product);
                Assert.AreEqual(TikTokFormats.Hack, v.Format);
                Assert.AreEqual(sim.TikTokVideos[0].Score, v.Score, 0.001f);
                Assert.AreEqual(sim.TikTokVideos[0].Views, v.Views);

                // Alter Spielstand ohne Feld
                var state = sim.ToJson();
                state.Remove("tiktok_videos");
                File.WriteAllText(sim.SavePath(2), Json.Write(state));
                var sim3 = new Sim(4) { SaveDir = dir };
                sim3.TikTokVideos.Add(new TikTokVideo { Title = "alt" });
                Assert.IsTrue(sim3.LoadGame(2));
                Assert.AreEqual(0, sim3.TikTokVideos.Count);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                }
                catch (Exception)
                {
                    // egal
                }
            }
        }

        [Test]
        public void Blocker_ExplainsWhyNoVideoIsPossible()
        {
            var fresh = TestUtil.Fresh(5);
            fresh.NewGame("skip");
            Assert.IsNotNull(fresh.TikTokBlocker(), "Level 1: gesperrt");
            StringAssert.Contains("Firmenlevel", fresh.TikTokBlocker());

            var sim = Ready();
            Assert.IsNull(sim.TikTokBlocker());
            var rating = TikTokScoring.Rate(Great(), TikTokFormats.Review, "");
            Assert.IsNotNull(sim.PostTikTokVideo(rating, "huelle", TikTokFormats.Review));
            StringAssert.Contains("min", sim.TikTokBlocker(), "Abklingzeit");
            sim.AdminResetTikTok();
            sim.Daily.TikToks = GameData.TikTokPerDay;
            StringAssert.Contains("Tageslimit", sim.TikTokBlocker());
            sim.Daily.TikToks = 0;
            sim.DayOver = true;
            Assert.IsNotNull(sim.TikTokBlocker());
            Assert.IsNull(sim.PostTikTokVideo(rating, "huelle", TikTokFormats.Review), "Nach Feierabend kein Posten");
            sim.DayOver = false;
            Assert.IsNull(sim.TikTokBlocker());
        }

        [Test]
        public void Effect_PreviewMatchesPostedBoost()
        {
            var sim = Ready();
            var rating = TikTokScoring.Rate(Great(), TikTokFormats.Unboxing, "");
            sim.TikTokEffect(rating.Score, "huelle", out float mult, out float minutes);
            Assert.Greater(mult, 1f);
            Assert.Greater(minutes, 0f);
            float before = sim.BClock();
            sim.PostTikTokVideo(rating, "huelle", TikTokFormats.Unboxing);
            var boost = sim.ActiveBoost("tiktok");
            Assert.IsNotNull(boost);
            Assert.AreEqual(mult, boost.Mult, 0.0001f);
            Assert.AreEqual(before + minutes, boost.EndsAt, 0.01f);
            // Bessere Wertung = stärkere Wirkung
            sim.TikTokEffect(0.2f, "huelle", out float weak, out _);
            Assert.Less(weak, mult);
        }

        [Test]
        public void Rate_RealisticGoodTakeIsStrong()
        {
            // So sieht eine ordentliche echte Aufnahme aus (nicht perfekt gemessen).
            var t = new TikTokTake
            {
                Duration = 14f, VisibleFrac = 0.9f, Centered = 0.75f, DistanceFrac = 0.85f, SteadyFrac = 0.85f, Light = 0.7f,
                AngleDegrees = 100f, Hook = true, Actions = 3, BrandFrac = 0.25f, Hype = 1f,
            };
            foreach (var f in TikTokFormats.All)
                Assert.Greater(TikTokScoring.Rate(t, f, "").Score, 0.8f, f);
        }

        [Test]
        public void Video_FromJsonIsDefensive()
        {
            var v = TikTokVideo.FromJson(null);
            Assert.AreEqual("", v.Title);
            var w = TikTokVideo.FromJson(new Dictionary<string, object> { { "score", 7.0 }, { "views", -5 } });
            Assert.AreEqual(1f, w.Score, 0.0001f);
            Assert.AreEqual(0, w.Views);
        }
    }
}
