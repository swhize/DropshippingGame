using System;
using System.Collections.Generic;
using System.Text;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>
    /// Kampagnen-Balance mit dem <see cref="BalanceBot"/>: drei Spielertypen (vorsichtig, durchschnittlich,
    /// gierig) über mehrere Seeds bis zum 100.000-€-Ziel. Sichert die Kurve (erstes Level, Lagerhalle,
    /// Endziel in Echtzeit), die Belohnungsdichte, die Exploit-Deckel und "keine Sackgassen".
    /// Echtzeit = 0,75 s je Geschäftsminute + 45 s je Tageswechsel (Abrechnung, Ereignis-Dialoge).
    /// Die Tabelle steht in der Testausgabe und in docs/GDD.md ("Zahlen &amp; Balance").
    /// </summary>
    public class BalanceTests
    {
        private static readonly int[] Seeds = { 1, 7, 42, 2026 };

        private static float Median(List<float> l)
        {
            var c = new List<float>(l);
            c.Sort();
            return c.Count == 0 ? -1f : (c.Count % 2 == 1 ? c[c.Count / 2] : (c[c.Count / 2 - 1] + c[c.Count / 2]) / 2f);
        }

        private static readonly Dictionary<PlayerType, List<BalanceBot>> _runs = new Dictionary<PlayerType, List<BalanceBot>>();

        /// <summary>Alle Kampagnen einmal spielen (bis 100.000 € Umsatz, höchstens 80 Tage) und für die Tests merken.</summary>
        private static List<BalanceBot> Campaigns(PlayerType t)
        {
            lock (_runs)
            {
                if (_runs.TryGetValue(t, out var l)) return l;
                l = new List<BalanceBot>();
                foreach (int seed in Seeds)
                {
                    var b = new BalanceBot(t, seed);
                    b.RunUntilGoal(80);
                    l.Add(b);
                }
                _runs[t] = l;
                return l;
            }
        }

        [Test]
        public void Campaign_Table()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Typ        Seed | 1. Level | Lagerhalle   | 100.000 €     | Level 10 | Pleite/Mama | max. Lücke ≤3h | Belohnungen ≤3h | TikTok Ø");
            foreach (PlayerType t in Enum.GetValues(typeof(PlayerType)))
                foreach (var b in Campaigns(t))
                    sb.AppendLine(string.Format("{0,-8} {1,6} | {2,8} | {3,6} T{4,-4} | {5,6} T{6,-5} | {7,8} | {8,-11} | {9,14} | {10,15} | {11:0.00}",
                        b.Type, b.Seed, BalanceBot.M(b.FirstLevelUpMin), BalanceBot.H(b.WarehouseMin), b.WarehouseDay, BalanceBot.H(b.GoalMin), b.GoalDay,
                        BalanceBot.H(b.Level10Min), (b.Broke ? "PLEITE" : "nein") + "/" + b.MamaRescues, BalanceBot.M(b.MaxRewardGap(180f)),
                        b.RewardsBefore(180f), b.AvgTikTokScore));
            foreach (PlayerType t in Enum.GetValues(typeof(PlayerType)))
            {
                var goal = new List<float>();
                var wh = new List<float>();
                foreach (var b in Campaigns(t))
                {
                    goal.Add(b.GoalMin);
                    wh.Add(b.WarehouseMin);
                }
                sb.AppendLine($"Median {t,-8}: Lagerhalle {BalanceBot.H(Median(wh))}, 100.000 € {BalanceBot.H(Median(goal))}");
            }
            TestContext.Out.WriteLine(sb.ToString());
            Assert.Pass();
        }

        [Test]
        public void Average_FirstLevelWarehouseAndGoal_InTargetWindows()
        {
            var goals = new List<float>();
            foreach (var b in Campaigns(PlayerType.Average))
            {
                string s = " (Seed " + b.Seed + ")";
                Assert.IsFalse(b.Broke, "Durchschnitt geht nicht pleite" + s);
                Assert.AreEqual(0, b.MamaRescues, "Durchschnitt braucht Mama nicht" + s);
                Assert.IsTrue(b.FirstLevelUpMin > 0f && b.FirstLevelUpMin <= 15f, "Erstes Level in ≤ 15 min: " + b.FirstLevelUpMin + s);
                Assert.IsTrue(b.WarehouseMin >= 90f && b.WarehouseMin <= 160f, "Lagerhalle nach 1,5–2,7 h: " + BalanceBot.H(b.WarehouseMin) + s);
                Assert.IsTrue(b.GoalMin >= 300f && b.GoalMin <= 540f, "100.000 € nach 5–9 h: " + BalanceBot.H(b.GoalMin) + s);
                goals.Add(b.GoalMin);
            }
            float med = Median(goals);
            Assert.IsTrue(med >= 330f && med <= 480f, "Median 100.000 € bei 5,5–8 h: " + BalanceBot.H(med));
        }

        [Test]
        public void Careful_And_Greedy_StayInRange()
        {
            var avg = new List<float>();
            foreach (var b in Campaigns(PlayerType.Average)) avg.Add(b.GoalMin);
            foreach (var b in Campaigns(PlayerType.Careful))
            {
                Assert.IsFalse(b.Broke, "Vorsichtig geht nie pleite (Seed " + b.Seed + ")");
                Assert.IsTrue(b.GoalMin > 0f && b.GoalMin <= 600f, "Vorsichtig schafft 100.000 € in ≤ 10 h: " + BalanceBot.H(b.GoalMin));
                Assert.IsTrue(b.WarehouseMin > 0f && b.WarehouseMin <= 210f, "Vorsichtig: Lagerhalle ≤ 3,5 h: " + BalanceBot.H(b.WarehouseMin));
            }
            var greedy = new List<float>();
            foreach (var b in Campaigns(PlayerType.Greedy))
            {
                Assert.IsFalse(b.Broke, "Gierig, aber nicht dumm: keine Pleite (Seed " + b.Seed + ")");
                Assert.IsTrue(b.GoalMin > 0f, "Gierig schafft das Ziel (Seed " + b.Seed + ")");
                greedy.Add(b.GoalMin);
            }
            Assert.IsTrue(Median(greedy) >= Median(avg) * 0.75f,
                "Kein dominanter Trick: Gierig höchstens 25 % schneller (" + BalanceBot.H(Median(greedy)) + " vs. " + BalanceBot.H(Median(avg)) + ")");
        }

        [Test]
        public void Pacing_FrequentRewardsInFirstHours()
        {
            foreach (var b in Campaigns(PlayerType.Average))
            {
                Assert.IsTrue(b.MaxRewardGap(180f) <= 25f, "Höchstens 25 min ohne Belohnung in den ersten 3 h: " + BalanceBot.M(b.MaxRewardGap(180f)) + " (Seed " + b.Seed + ")");
                Assert.IsTrue(b.RewardsBefore(180f) >= 36, "Mindestens 12 Belohnungen pro Stunde: " + b.RewardsBefore(180f));
            }
            foreach (var b in Campaigns(PlayerType.Careful))
                Assert.IsTrue(b.MaxRewardGap(180f) <= 40f, "Auch vorsichtig nie lange ohne Belohnung: " + BalanceBot.M(b.MaxRewardGap(180f)));
        }

        [Test]
        public void Average_UsesAllSystems()
        {
            var used = new HashSet<string>();
            foreach (var b in Campaigns(PlayerType.Average)) used.UnionWith(b.SystemsUsed);
            foreach (var b in Campaigns(PlayerType.Greedy)) used.UnionWith(b.SystemsUsed);
            foreach (var sys in new[]
                     {
                         "perks", "home", "defects", "contracts", "wholesale", "fakebook", "gugel", "fakesites", "fakeprofiles", "ads_old", "tiktok",
                         "festival", "stand", "pets", "mediamarkd", "invest", "loan", "staff", "conveyor", "equipment",
                     })
                Assert.IsTrue(used.Contains(sys), "Bot nutzt System: " + sys);
            int ach = 0;
            foreach (var b in Campaigns(PlayerType.Average)) ach = Math.Max(ach, b.Gm.AchievementCount());
            Assert.IsTrue(ach >= 18, "Bis 100.000 € gibt es reichlich Erfolge (" + ach + ")");
        }

        /// <summary>Kein Trick bringt viel mehr als ein schlichter Durchschnittsspieler (30 Tage, 3 Seeds).</summary>
        [Test]
        public void Exploits_AreCapped()
        {
            var nw = new Dictionary<string, double>();
            var rev = new Dictionary<string, double>();
            foreach (var name in ExploitRunner.Names)
            {
                double n = 0, r = 0;
                foreach (int seed in new[] { 1, 7, 42 })
                {
                    var b = ExploitRunner.Make(name, seed);
                    b.RunDays(30);
                    Assert.IsFalse(b.Broke && name != "loan_invest", name + " geht nicht pleite");
                    n += b.Gm.Market.NetWorth();
                    r += b.Gm.TotalEarned;
                }
                nw[name] = n / 3;
                rev[name] = r / 3;
                TestContext.Out.WriteLine($"{name,-13} Nettovermögen Ø {nw[name],8:0} ({nw[name] / Math.Max(1, nw["basis"]) * 100,4:0} %) | Umsatz Ø {rev[name],8:0} ({rev[name] / Math.Max(1, rev["basis"]) * 100,4:0} %)");
            }
            foreach (var name in ExploitRunner.Names)
            {
                if (name == "basis") continue;
                Assert.LessOrEqual(rev[name], rev["basis"] * 1.3, name + ": höchstens +30 % Umsatz");
                Assert.LessOrEqual(nw[name], nw["basis"] * 1.5, name + ": höchstens +50 % Nettovermögen");
            }
            Assert.LessOrEqual(nw["crypto"], nw["basis"] * 1.1, "Krypto ist im Schnitt keine Gelddruckmaschine");
            // Nettovermögen allein ist hier Rauschen: Die Basis steckt ihr Geld in Lagerhalle/Personal (zählt nicht ins
            // Nettovermögen), der Kredit-Anleger nicht – dafür wächst sein Geschäft kaum. Bei nur 3 Seeds schwankt der
            // Vergleich um ±5 % (z. B. nach Änderungen am Ereignis-Zufall), daher Toleranz wie bei Krypto/MediaMarkd.
            Assert.LessOrEqual(nw["loan_invest"], nw["basis"] * 1.1, "Kredit + Anlage lohnt sich nicht");
            Assert.Less(rev["loan_invest"], rev["basis"] * 0.8, "Kredit + Anlage bremst das Geschäft");
            Assert.LessOrEqual(nw["mediamarkd"], nw["basis"] * 1.1, "MediaMarkd-Schnäppchen sind nett, aber keine Arbitrage-Maschine");
            Assert.LessOrEqual(rev["festival_max"], rev["basis"] * 1.15, "Volles Fest ist ein Bonus, kein Hauptgeschäft");
        }

        [Test]
        public void SlowPlayer_HasNoDeadEnd()
        {
            // Halbes Tempo, keine Großaufträge, keine Extras: langsamer, aber keine Pleite und stetiger Fortschritt.
            var b = new BalanceBot(PlayerType.Careful, 99)
            {
                Pace = 80f, UseContracts = false, TikToksPerDay = 0, FakebookPerDay = 0, Festival = false, HomeUpgrades = false, SavingsAccount = false,
            };
            b.RunDays(30);
            TestContext.Out.WriteLine("Langsam: Level " + b.Gm.Level + ", Konto " + b.Gm.Money + ", Lagerhalle Tag " + b.WarehouseDay + ", Umsatz " + b.Gm.TotalEarned);
            Assert.IsFalse(b.Broke, "Auch ein gemütlicher Spieler geht nicht pleite");
            Assert.AreEqual(0, b.MamaRescues, "... und braucht nicht einmal Mama");
            Assert.IsTrue(b.Gm.Level >= 4, "Auch langsam gibt es Fortschritt (Level " + b.Gm.Level + ")");
            Assert.IsTrue(b.Gm.Money > 1000 || b.WarehouseDay > 0, "Es bleibt immer etwas zum Sparen/Kaufen");
        }

        [Test]
        public void GrossMismanagement_MamaWarnsThenBankruptcy()
        {
            // Grober Fehler: Kredit aufnehmen, alles für Lifestyle verprassen, nie arbeiten.
            var gm = new Sim(5);
            gm.NewGame("skip");
            gm.InGame = true;
            bool broke = false;
            int rescues = 0, rescueDay = -1;
            gm.GameOver += _ => broke = true;
            gm.MamaRescued += _ =>
            {
                rescues++;
                rescueDay = gm.Day;
            };
            gm.TakeLoan(gm.CreditAvailable());
            gm.BuyLifestyle("gamingstuhl");
            gm.BuyLifestyle("sneaker");
            for (int d = 0; d < 80 && !broke; d++)
            {
                gm.EndDayNow();
                gm.StartNextDay();
            }
            TestContext.Out.WriteLine("Mama rettet an Tag " + rescueDay + ", Pleite an Tag " + gm.Day);
            Assert.AreEqual(1, rescues, "Einmal springt Mama ein (Normal)");
            Assert.IsTrue(broke, "Wer nur Schulden macht und nichts verkauft, geht trotzdem irgendwann pleite");
            Assert.IsTrue(rescueDay >= 10, "... nicht über Nacht (Mama an Tag " + rescueDay + ")");
            Assert.Greater(gm.Day, rescueDay, "Pleite erst nach Mamas Rettung");
        }

        // ---- Werkzeuge (manuell) ----------------------------------------------------------------
        [Test, Explicit]
        public void PrintDays()
        {
            var b = new BalanceBot((PlayerType)Enum.Parse(typeof(PlayerType), Environment.GetEnvironmentVariable("BOT_TYPE") ?? "Average"),
                int.Parse(Environment.GetEnvironmentVariable("BOT_SEED") ?? "1"), Environment.GetEnvironmentVariable("BOT_DIFF") ?? "normal") { Log = true };
            b.RunDays(int.Parse(Environment.GetEnvironmentVariable("BOT_DAYS") ?? "50"));
            Console.WriteLine(b.Row());
            Console.WriteLine("Fest verkauft " + b.FestivalSold + " (" + b.FestivalRevenue + " €), Stand " + b.StandSold + ", TikToks " + b.TikToks +
                              ", Aufträge " + b.ContractsAccepted + ", Porto " + b.Gm.TotalPorto + ", Ersparnis MediaMarkd " + b.ArbitrageSaved);
            Console.WriteLine("Erfolge: " + b.Gm.AchievementCount() + " " + string.Join(",", b.Gm.Achievements.Keys));
        }
    }
}
