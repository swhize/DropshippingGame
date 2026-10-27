using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Mini-Event Straßenfest: Ablauf, Packen, Verkaufen, Bilanz, Speichern.</summary>
    public class FestivalTests
    {
        private static string Pid => GameData.Products[0].Id;

        private static Sim Live(int seed = 3, int stock = 40)
        {
            var sim = TestUtil.Fresh(seed);
            sim.Stock[Pid].Qty = stock;
            Assert.IsTrue(sim.AdminStartFestival(true));
            Assert.AreEqual(FestivalPhase.Live, sim.Festival);
            return sim;
        }

        [Test]
        public void AnnounceThenPhasesFollowClock()
        {
            var sim = TestUtil.Fresh();
            Assert.IsTrue(sim.FestivalAnnounce(sim.Day));
            Assert.IsFalse(sim.FestivalAnnounce(sim.Day), "nur ein Fest gleichzeitig");
            Assert.AreEqual(FestivalPhase.Announced, sim.Festival);
            sim.AdvanceMinutes(GameData.FestivalPrepAt - sim.TimeMinutes + 1f);
            Assert.AreEqual(FestivalPhase.Prep, sim.Festival);
            sim.AdvanceMinutes(GameData.FestivalLiveAt - sim.TimeMinutes + 1f);
            Assert.AreEqual(FestivalPhase.Live, sim.Festival);
            FestivalSummary ended = null;
            sim.FestivalEnded += s => ended = s;
            sim.AdvanceMinutes(GameData.FestivalEndAt - sim.TimeMinutes + 1f);
            Assert.AreEqual(FestivalPhase.None, sim.Festival);
            Assert.IsNotNull(ended);
            Assert.AreEqual(sim.Day, sim.FestivalLastDay);
        }

        [Test]
        public void PackCrateMovesStockAndStandTakesIt()
        {
            var sim = TestUtil.Fresh();
            sim.Stock[Pid].Qty = 25;
            Assert.IsNull(sim.FestivalPackCrate(Pid), "vor dem Fest kein Packen");
            Assert.IsTrue(sim.AdminStartFestival(false));
            Assert.AreEqual(FestivalPhase.Prep, sim.Festival);
            Assert.AreEqual(Pid, sim.FestivalNextPackProduct());
            var crate = sim.FestivalPackCrate(Pid);
            Assert.IsNotNull(crate);
            Assert.AreEqual(GameData.FestivalCrateSize, crate.Quantity);
            Assert.AreEqual(25 - GameData.FestivalCrateSize, sim.StockQty(Pid));
            Assert.AreEqual(GameData.FestivalCrateSize, sim.FestivalAddCrate(crate));
            Assert.AreEqual(0, crate.Quantity);
            Assert.AreEqual(GameData.FestivalCrateSize, sim.FestivalTotal());
        }

        [Test]
        public void StandCapacityIsRespected()
        {
            var sim = Live();
            var big = ItemData.Crate(Pid, GameData.FestivalCapacity + 30, 1f);
            Assert.AreEqual(GameData.FestivalCapacity, sim.FestivalAddCrate(big));
            Assert.AreEqual(30, big.Quantity);
        }

        [Test]
        public void FairPricesSellAndBookMoneyXpAwareness()
        {
            var sim = Live();
            sim.ShopPrices[Pid] = System.Math.Max(1, (int)(sim.Market.MarketPrice(Pid) * 0.7f));
            sim.FestivalAddCrate(ItemData.Crate(Pid, 60, 1f));
            int money = sim.Money, xp = sim.Xp;
            float aw = sim.Awareness;
            int sold = 0;
            for (int i = 0; i < 80; i++) if (sim.FestivalVisitor().Sold) sold++;
            Assert.Greater(sold, 20);
            Assert.Greater(sim.Money, money);
            Assert.Greater(sim.Xp, xp);
            Assert.Greater(sim.Awareness, aw);
            Assert.AreEqual(sim.Money - money, sim.FestivalStats.Revenue);
            Assert.AreEqual(60 - sim.FestivalStats.Sold, sim.FestivalTotal());
        }

        [Test]
        public void OverpricedGoodsDoNotSell()
        {
            var sim = Live();
            sim.ShopPrices[Pid] = (int)(sim.Market.MarketPrice(Pid) * 4f) + 10;
            sim.FestivalAddCrate(ItemData.Crate(Pid, 30, 1f));
            int money = sim.Money;
            for (int i = 0; i < 60; i++) Assert.IsFalse(sim.FestivalVisitor().Sold);
            Assert.AreEqual(money, sim.Money);
            Assert.Greater(sim.FestivalStats.TooExpensive, 0);
        }

        [Test]
        public void HagglingHappensSlightlyAboveReference()
        {
            var sim = Live(5);
            sim.ShopPrices[Pid] = (int)(sim.Market.MarketPrice(Pid) * 1.3f) + 1;
            sim.FestivalAddCrate(ItemData.Crate(Pid, 100, 1f));
            bool haggleBuy = false;
            for (int i = 0; i < 300; i++)
            {
                var v = sim.FestivalVisitor();
                if (v.Outcome == FestivalOutcome.HaggledBought)
                {
                    haggleBuy = true;
                    Assert.Less(v.Price, sim.ShopPrices[Pid]);
                    Assert.GreaterOrEqual(v.Price, sim.ShopPrices[Pid] * 0.8f - 0.5f);
                }
            }
            Assert.Greater(sim.FestivalStats.Haggled, 0);
            Assert.IsTrue(haggleBuy);
        }

        [Test]
        public void EmptyStandSellsNothing()
        {
            var sim = Live();
            Assert.AreEqual(FestivalOutcome.Empty, sim.FestivalVisitor().Outcome);
        }

        [Test]
        public void LeftoverReturnsToStockAtDayEnd()
        {
            var sim = Live(3, 30);
            var c = sim.FestivalPackCrate(Pid);
            sim.FestivalAddCrate(c);
            Assert.AreEqual(20, sim.StockQty(Pid));
            sim.EndDayNow();
            Assert.AreEqual(FestivalPhase.None, sim.Festival);
            Assert.AreEqual(30, sim.StockQty(Pid));
            Assert.IsNotNull(sim.LastFestival);
            Assert.AreEqual(10, sim.LastFestival.Leftover);
        }

        [Test]
        public void MissedAnnouncedFestivalIsCleanedUpNextDay()
        {
            var sim = TestUtil.Fresh();
            sim.FestivalAnnounce(sim.Day + 1);
            sim.EndDayNow();
            sim.StartNextDay();
            Assert.AreEqual(FestivalPhase.Announced, sim.Festival);
            sim.EndDayNow();
            sim.StartNextDay();
            Assert.AreNotEqual(FestivalDayOrNone(sim), sim.Day - 1);
        }

        private static int FestivalDayOrNone(Sim sim) => sim.Festival == FestivalPhase.None ? -1 : sim.FestivalDay;

        [Test]
        public void InvitationsArriveOverTimeAtLevel2()
        {
            var sim = TestUtil.Fresh(11);
            sim.AddXp(GameData.LevelThreshold(GameData.FestivalMinLevel));
            bool seen = false;
            for (int d = 0; d < 40 && !seen; d++)
            {
                sim.EndDayNow();
                sim.StartNextDay();
                seen = sim.Festival == FestivalPhase.Announced;
            }
            Assert.IsTrue(seen);
        }

        [Test]
        public void SaveRoundTripKeepsFestival()
        {
            var sim = Live();
            sim.FestivalAddCrate(ItemData.Crate(Pid, 15, 1f));
            var json = Json.Write(sim.ToJson());
            var b = TestUtil.Fresh(9);
            b.FromJson(Json.Parse(json) as System.Collections.Generic.Dictionary<string, object>);
            Assert.AreEqual(FestivalPhase.Live, b.Festival);
            Assert.AreEqual(15, b.FestivalQty(Pid));
            Assert.AreEqual(sim.FestivalLiveEnd, b.FestivalLiveEnd, 0.01f);
        }
    }
}
