using System;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Börse als Nebenspiel: Sparplan (jeden Tag kaufen, nie verkaufen) schlägt das Sparkonto nicht deutlich.</summary>
    public class FinanceBalanceTests
    {
        private const int TicksPerDay = 270;

        /// <summary>Ø Endwert / Einsatz eines täglichen Sparplans über 45 Tage (8 Seeds).</summary>
        private static double DcaRatio(string asset, out double worst, out double best)
        {
            double sum = 0;
            worst = double.MaxValue;
            best = 0;
            for (int seed = 0; seed < 8; seed++)
            {
                var gm = new Sim(500 + seed);
                gm.Market.SeedExchange(900 + seed);
                gm.Market.Reset();
                gm.Money = 1000000;
                int invested = 0;
                for (int d = 0; d < 45; d++)
                {
                    gm.Market.Buy(asset, 1000);
                    invested += 1000;
                    for (int t = 0; t < TicksPerDay; t++) gm.Market.Tick(false);
                    gm.Money += Mathx.RoundToInt(gm.Market.DividendPerDay(asset));
                }
                double r = gm.Market.HoldingValue(asset) * (1 - Market.FeeFor(asset)) / invested;
                sum += r;
                worst = Math.Min(worst, r);
                best = Math.Max(best, r);
            }
            return sum / 8;
        }

        [Test]
        public void SavingsPlan_NoAssetIsAMoneyPrinter()
        {
            foreach (var a in Market.AllAssets)
            {
                double avg = DcaRatio(a.Id, out double worst, out double best);
                TestContext.Out.WriteLine($"{a.Id,-5} Sparplan 45 Tage: Ø ×{avg:0.00} (schlechtester ×{worst:0.00}, bester ×{best:0.00})");
                Assert.LessOrEqual(avg, 1.25, a.Id + ": Sparplan im Schnitt höchstens +25 % in 45 Tagen");
                Assert.GreaterOrEqual(avg, 0.6, a.Id + ": aber auch kein sicheres Grab");
            }
        }
    }
}
