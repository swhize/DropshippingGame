using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>
    /// Balance-Simulation: ein fleißiger Spieler, der jede Bestellung mit menschlichem Tempo
    /// erfüllt (etwa ein Paket pro 40 Spielminuten ohne Personal), rechtzeitig nachkauft,
    /// Retouren bearbeitet, Großaufträge annimmt und mit Kisten beliefert und Skills lernt.
    /// Prüft, dass das Spiel weder zu leicht noch zu schwer ist.
    /// </summary>
    public class BalanceTests
    {
        /// <summary>Ein simulierter Spieler. Alle Aktionen laufen über die öffentliche Sim-API.</summary>
        public sealed class Bot
        {
            public readonly Sim Gm;
            /// <summary>Spielminuten pro von Hand erledigter Bestellung (menschliches Tempo).</summary>
            public float Pace = 40f;
            public bool UseContracts = true;
            public bool UseReturns = true;
            public bool UseSkills = true;
            public bool BuyWarehouse = true;
            public bool HireStaff = true;
            public bool Log = true;

            public bool Broke;
            public int WarehouseDay = -1;
            public int FirstLevelUpDay = -1;
            public float FirstOrderAt = -1f;
            public int ContractsAccepted;
            public readonly List<string> Lines = new List<string>();

            private float _hands;
            private readonly Dictionary<int, int> _boughtFor = new Dictionary<int, int>();

            private static readonly string[] SkillOrder =
            {
                "v_feilschen", "l_arme", "v_kulanz", "l_tetris", "m_content", "v_netzwerk", "l_wege", "m_radar",
                "v_stamm", "l_flow", "m_viral", "m_trendsetter",
            };

            public Bot(int seed, string mode = "skip")
            {
                Gm = new Sim(seed);
                Gm.NewGame(mode);
                Gm.InGame = true;
                Gm.GameOver += _ => Broke = true;
                Gm.LevelUp += _ =>
                {
                    if (FirstLevelUpDay < 0) FirstLevelUpDay = Gm.Day;
                };
                Gm.OrderReceived += _ =>
                {
                    if (FirstOrderAt < 0f) FirstOrderAt = Gm.BClock();
                };
                Gm.ContractFailed += c => Lines.Add($"GEPLATZT Tag {Gm.Day}: {c.Title} geliefert {c.Delivered}, Qualität min {c.MinQuality:0.0}, " +
                                                   $"Lager {Gm.StockQty(c.Product)} (Q {Gm.StockQuality(c.Product):0.00}), angenommen Tag {c.AcceptedDay}, Frist {c.DeadlineDay}");
            }

            public void RunDays(int days)
            {
                for (int d = 0; d < days && !Broke; d++)
                {
                    while (!Gm.DayOver) Step();
                    Gm.StartNextDay();
                    if (Broke) break;
                    AfterDay();
                }
            }

            private void Step()
            {
                var gm = Gm;
                gm.AdvanceMinutes(2f);
                _hands += 2f / Pace;
                for (int i = 0; i < GameData.Products.Length; i++)
                {
                    string id = GameData.Products[i].Id;
                    if (!gm.ProductAvailable(id)) continue;
                    if (!gm.IsListed(id)) gm.SetListed(id, true);
                    if (gm.StockQty(id) + gm.TravelingCountFor(id) * 50 < 30 && gm.Money >= gm.BulkCost(i, 1, 1))
                        gm.BuyBulk(i, 1, 1);
                }
                if (UseContracts) HandleContracts();
                while (gm.DockCrates.Count > 0)
                {
                    var c = gm.PickupCrate();
                    if (UseContracts && gm.ContractAccepts(c)) gm.ContractDeliver(c);
                    if (c.Quantity <= 0) continue;
                    if (!gm.UnboxCrate(c.Product, c.Quantity, c.Quality))
                    {
                        gm.ReturnCrate(c);
                        break;
                    }
                }
                if (UseReturns && gm.StaffCount("lager") == 0)
                {
                    while (gm.DockReturns.Count > 0 && _hands >= 0.5f)
                    {
                        _hands -= 0.5f;
                        var r = gm.PickupReturn();
                        if (!gm.ProcessReturn(r, gm.BStockQuality(r) >= 0.5f)) gm.ProcessReturn(r, false);
                    }
                }
                for (int i = 0; i < GameData.Products.Length; i++)
                {
                    var p = GameData.Products[i];
                    if (gm.ProductAvailable(p.Id) && gm.Packaging[p.Size] < 5 && gm.Money >= gm.PackagingCost(p.Size, false) + 40)
                        gm.BuyPackaging(p.Size, false, 0);
                }
                if (gm.Staff.Count > 0) return;
                while (gm.OrderQueue.Count > 0 && _hands >= 1f)
                {
                    _hands -= 1f;
                    var next = gm.Tickets().Find(o => !o.InWork);
                    if (next == null) break;
                    var it = gm.PickItem(next.Product);
                    if (it == null) break;
                    if (!gm.WrapItem(it))
                    {
                        gm.ReturnItem(it);
                        break;
                    }
                    var pkg = gm.PickupPackage();
                    pkg.Kind = ItemKind.Labeled;
                    gm.OnLabeled(pkg);
                    gm.ShipPackage(pkg);
                }
            }

            private void HandleContracts()
            {
                var gm = Gm;
                foreach (var offer in gm.ContractOffers())
                {
                    if (gm.ActiveContractCount() >= gm.MaxActiveContracts()) break;
                    if (offer.MinQuality > 1.0f) continue;
                    int pi = GameData.ProductIndex(offer.Product);
                    int crates = (offer.Quantity + 49) / 50;
                    if (gm.Money < gm.BulkCost(pi, 1, 1) * crates + 150) continue;
                    if (!gm.AcceptContract(offer.Id)) continue;
                    ContractsAccepted++;
                    int bought = 0;
                    for (int k = 0; k < crates; k++)
                        if (gm.BuyBulk(pi, 1, 1)) bought += 50;
                    _boughtFor[offer.Id] = bought;
                }
                // Kurz vor Fristende: fehlende Stück einzeln aus dem Regal nachliefern.
                if (gm.TimeMinutes < 1080f) return;
                foreach (var c in gm.ActiveContracts())
                {
                    if (c.DeadlineDay != gm.Day) continue;
                    while (c.Remaining > 0 && _hands >= 0.1f && gm.StockQty(c.Product) > 0)
                    {
                        var it = gm.PickForContract(c.Product);
                        if (it == null) break;
                        _hands -= 0.1f;
                        gm.ContractDeliver(it);
                    }
                }
            }

            private void AfterDay()
            {
                var gm = Gm;
                var h = gm.History[gm.History.Count - 1];
                if (BuyWarehouse && WarehouseDay < 0 && gm.UpgradeState("warehouse") == "available" &&
                    gm.Money >= GameData.Upgrade("warehouse").Cost + 500)
                {
                    gm.BuyUpgrade("warehouse");
                    WarehouseDay = gm.Day;
                }
                if (HireStaff && gm.LocationStage >= 1 && gm.Level >= 5 && gm.Money > 800 && gm.Staff.Count < 3)
                {
                    foreach (var r in new[] { "packer", "versand", "lager" })
                        if (gm.StaffCount(r) == 0 && gm.Money > 600) gm.Hire(r);
                }
                if (UseSkills)
                {
                    foreach (var id in SkillOrder)
                        if (gm.SkillState(id) == "available") gm.LearnSkill(id);
                }
                _hands = Math.Min(_hands, 1f);
                string line = $"Tag {h.Day,2} {GameData.WeekdayShort(h.Day)}: Umsatz {h.Revenue,5}  Gewinn {h.Profit,5}  Pakete {h.Shipped,3}  " +
                              $"Retouren {h.Returns,2}  Aufträge {h.Contracts}  Konto {gm.Money,6}  Level {gm.Level} ({gm.Xp,5} XP)  Bewertung {gm.Reputation:0.00}  " +
                              $"Skills {gm.Skills.Count}  Wochenziele {gm.TotalChallengesDone} {(gm.Day == WarehouseDay ? "LAGERHALLE" : "")}";
                Lines.Add(line);
                if (Log) TestContext.Out.WriteLine(line);
            }
        }

        [Test]
        public void TwentyFiveDays_ReachWarehouseWithoutBankruptcy()
        {
            var bot = new Bot(42);
            bot.RunDays(25);
            var gm = bot.Gm;
            foreach (var l in bot.Lines)
                if (l.StartsWith("GEPLATZT")) TestContext.Out.WriteLine(l);
            TestContext.Out.WriteLine($"Erste Bestellung nach {bot.FirstOrderAt:0} min, erstes Level an Tag {bot.FirstLevelUpDay}, " +
                                      $"Großaufträge {gm.TotalContractsDone}/{bot.ContractsAccepted} (geplatzt {gm.TotalContractsFailed}), " +
                                      $"Retouren {gm.TotalReturns} ({gm.ReturnRateTotal() * 100f:0.0} %), Wochenziele {gm.TotalChallengesDone}");
            Assert.IsFalse(bot.Broke, "Ein fleißiger Spieler geht nicht pleite");
            Assert.IsTrue(bot.FirstOrderAt >= 0f && bot.FirstOrderAt <= 80f, "Erste Bestellung innerhalb ~1 echter Minute nach dem Online-Stellen (" + bot.FirstOrderAt + " min)");
            Assert.AreEqual(1, bot.FirstLevelUpDay, "Erstes Level schon am ersten Tag");
            Assert.IsTrue(bot.WarehouseDay >= 9 && bot.WarehouseDay <= 14, "Lagerhalle ca. an Tag 10-14 (Tag " + bot.WarehouseDay + ")");
            Assert.IsTrue(gm.Level >= 5, "Nach 25 Tagen mindestens Level 5 (ist " + gm.Level + ")");
            Assert.IsTrue(gm.TotalContractsDone >= 3, "Großaufträge werden erfüllt (" + gm.TotalContractsDone + ")");
            Assert.AreEqual(0, gm.TotalContractsFailed, "Ein sorgfältiger Spieler verpasst keine Frist");
            Assert.IsTrue(gm.TotalReturns > 0 && gm.ReturnRateTotal() < 0.08f, "Retouren kommen vor, aber selten (" + gm.TotalReturns + ")");
            Assert.IsTrue(gm.TotalChallengesDone >= 4, "Wochenziele sind schaffbar (" + gm.TotalChallengesDone + ")");
            Assert.IsTrue(gm.Skills.Count >= 5, "Skills werden gelernt (" + gm.Skills.Count + ")");
        }

        [Test]
        public void SeveralSeeds_WarehouseInTargetWindow()
        {
            int sum = 0;
            foreach (int seed in new[] { 1, 7, 1234, 2026 })
            {
                var bot = new Bot(seed) { Log = false };
                bot.RunDays(20);
                TestContext.Out.WriteLine($"Seed {seed}: Lagerhalle Tag {bot.WarehouseDay}, Level {bot.Gm.Level}, Konto {bot.Gm.Money}, " +
                                          $"erstes Level Tag {bot.FirstLevelUpDay}, Aufträge {bot.Gm.TotalContractsDone}");
                Assert.IsFalse(bot.Broke, "Keine Pleite (Seed " + seed + ")");
                Assert.AreEqual(1, bot.FirstLevelUpDay, "Erstes Level am ersten Tag (Seed " + seed + ")");
                Assert.IsTrue(bot.WarehouseDay >= 9 && bot.WarehouseDay <= 15, "Lagerhalle Tag 9-15 (Seed " + seed + ": " + bot.WarehouseDay + ")");
                sum += bot.WarehouseDay;
            }
            float avg = sum / 4f;
            Assert.IsTrue(avg >= 10f && avg <= 14f, "Im Schnitt Lagerhalle an Tag 10-14 (" + avg + ")");
        }

        [Test]
        public void SlowPlayer_NeverGoesBankrupt()
        {
            // Halbes Tempo, keine Großaufträge, keine Skills: langsamer, aber keine Pleite.
            var bot = new Bot(99) { Pace = 80f, UseContracts = false, UseSkills = false, Log = false };
            bot.RunDays(20);
            TestContext.Out.WriteLine("Langsam: Level " + bot.Gm.Level + ", Konto " + bot.Gm.Money + ", Lagerhalle Tag " + bot.WarehouseDay);
            Assert.IsFalse(bot.Broke, "Auch ein gemütlicher Spieler geht nicht pleite");
            Assert.IsTrue(bot.Gm.Level >= 3, "Auch langsam gibt es Fortschritt (Level " + bot.Gm.Level + ")");
        }

        [Test]
        public void GrossMismanagement_EndsInBankruptcy()
        {
            // Grober Fehler: Kredit aufnehmen, alles für Lifestyle verprassen, nie arbeiten.
            var gm = new Sim(5);
            gm.NewGame("skip");
            gm.InGame = true;
            bool broke = false;
            gm.GameOver += _ => broke = true;
            gm.TakeLoan(gm.CreditAvailable());
            gm.BuyLifestyle("gamingstuhl");
            gm.BuyLifestyle("sneaker");
            for (int d = 0; d < 40 && !broke; d++)
            {
                gm.EndDayNow();
                gm.StartNextDay();
            }
            Assert.IsTrue(broke, "Wer nur Schulden macht und nichts verkauft, geht irgendwann pleite");
        }
    }
}
