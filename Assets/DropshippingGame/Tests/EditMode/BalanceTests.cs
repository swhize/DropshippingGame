using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>
    /// Balance-Simulation: ein fleißiger Spieler, der jede Bestellung mit menschlichem Tempo
    /// erfüllt (etwa ein Paket pro 40 Spielminuten ohne Personal) und rechtzeitig nachkauft.
    /// Prüft, dass das Spiel weder zu leicht noch zu schwer ist.
    /// </summary>
    public class BalanceTests
    {
        [Test]
        public void TwentyFiveDays_ReachWarehouseWithoutBankruptcy()
        {
            var gm = new Sim(42);
            gm.NewGame("skip");
            gm.InGame = true;
            bool broke = false;
            gm.GameOver += _ => broke = true;
            int warehouseDay = -1;
            float handBudget = 0f;
            for (int d = 0; d < 25 && !broke; d++)
            {
                while (!gm.DayOver)
                {
                    gm.AdvanceMinutes(2f);
                    for (int i = 0; i < GameData.Products.Length; i++)
                    {
                        string id = GameData.Products[i].Id;
                        if (!gm.ProductAvailable(id)) continue;
                        gm.Listed[id] = true;
                        if (gm.StockQty(id) + gm.TravelingCountFor(id) * 50 < 30 && gm.Money >= gm.BulkCost(i, 1, 1))
                            gm.BuyBulk(i, 1, 1);
                    }
                    while (gm.DockCrates.Count > 0)
                    {
                        var c = gm.PickupCrate();
                        if (!gm.UnboxCrate(c.Product, c.Quantity, c.Quality))
                        {
                            gm.ReturnCrate(c);
                            break;
                        }
                    }
                    for (int i = 0; i < GameData.Products.Length; i++)
                    {
                        var p = GameData.Products[i];
                        if (gm.ProductAvailable(p.Id) && gm.Packaging[p.Size] < 5 && gm.Money >= gm.PackagingCost(p.Size, false) + 40)
                            gm.BuyPackaging(p.Size, false, 0);
                    }
                    handBudget += gm.Staff.Count > 0 ? 0f : 2f / 40f;
                    while (gm.OrderQueue.Count > 0 && handBudget >= 1f)
                    {
                        handBudget -= 1f;
                        var it = gm.PickItem(gm.OrderQueue[0].Product);
                        if (it == null) break;
                        if (!gm.WrapItem(it))
                        {
                            gm.ReturnItem(it);
                            break;
                        }
                        gm.ShipPackage(gm.PickupPackage());
                    }
                }
                gm.StartNextDay();
                if (broke) break;
                var h = gm.History[gm.History.Count - 1];
                if (warehouseDay < 0 && gm.Level >= 4 && gm.Money >= 5000)
                {
                    gm.BuyUpgrade("warehouse");
                    warehouseDay = gm.Day;
                }
                if (gm.LocationStage >= 1 && gm.Level >= 5 && gm.Money > 800 && gm.Staff.Count < 3)
                {
                    foreach (var r in new[] { "packer", "versand", "lager" })
                        if (gm.StaffCount(r) == 0 && gm.Money > 600) gm.Hire(r);
                }
                handBudget = System.Math.Min(handBudget, 1f);
                TestContext.Out.WriteLine(
                    $"Tag {h.Day,2}: Umsatz {h.Revenue,5}  Gewinn {h.Profit,5}  Pakete {h.Shipped,3}  Konto {gm.Money,6}  Level {gm.Level}  Bewertung {gm.Reputation:0.00} {(gm.Day == warehouseDay ? "LAGERHALLE" : "")}");
            }
            Assert.IsFalse(broke, "Ein fleißiger Spieler geht nicht pleite");
            Assert.IsTrue(warehouseDay > 0 && warehouseDay <= 22, "Lagerhalle innerhalb von ~3 Wochen erreichbar (Tag " + warehouseDay + ")");
            Assert.IsTrue(warehouseDay >= 6, "Lagerhalle nicht schon in der ersten Woche (Tag " + warehouseDay + ")");
            Assert.IsTrue(gm.Level >= 5, "Nach 25 Tagen mindestens Level 5 (ist " + gm.Level + ")");
        }
    }
}
