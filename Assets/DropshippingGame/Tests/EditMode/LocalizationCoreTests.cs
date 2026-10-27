using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>
    /// Englisch für die Kern-Logik: Zahlenformat (Fmt) und Abdeckung der Sim-Texte (EnCore/EnEvents).
    /// Der Balance-Bot spielt mit aktivem Englisch; jede Meldung, Mail, Tagesnotiz, Zielbeschreibung usw.
    /// wird durch <see cref="Lang.T"/> geschickt. Was fehlt und nicht aus den Inhalts-Tabellen stammt
    /// (GameData, ShopData, Erfolge, FinanceText – Bereich EnContent), lässt den Test scheitern.
    /// </summary>
    public class LocalizationCoreTests
    {
        [TearDown]
        public void Reset() => Lang.ResetForTests();

        // =====================================================================================
        // Fmt
        // =====================================================================================
        [Test]
        public void Fmt_GermanUnchanged()
        {
            Lang.ResetForTests();
            Assert.AreEqual("1.234 €", Fmt.Money(1234));
            Assert.AreEqual("−1.234.567 €", Fmt.Money(-1234567));
            Assert.AreEqual("12,50 €", Fmt.Eur(12.5f));
            Assert.AreEqual("4,5", Fmt.Dec(4.5f));
            Assert.AreEqual("4,50", Fmt.Dec(4.5f, 2));
            Assert.AreEqual("+12,5 %", Fmt.Pct(0.125f));
            Assert.AreEqual("−3,0 %", Fmt.Pct(-0.03f));
            Assert.AreEqual("4,2", Fmt.Rating(4.2f));
            Assert.AreEqual("12 Tsd.", Fmt.Thousands(12345));
            Assert.AreEqual("1,5 Mio.", Fmt.Thousands(1500000));
            Assert.AreEqual("45 min", Fmt.Duration(45f));
            Assert.AreEqual("1:05 h", Fmt.Duration(65f));
            Assert.AreEqual("09:30", Fmt.Clock(570f));
        }

        [Test]
        public void Fmt_English()
        {
            Lang.ResetForTests();
            Lang.Set("en");
            Assert.AreEqual("1,234 €", Fmt.Money(1234));
            Assert.AreEqual("−1,234,567 €", Fmt.Money(-1234567));
            Assert.AreEqual("+1,000 €", Fmt.SignedMoney(1000));
            Assert.AreEqual("999 €", Fmt.Money(999));
            Assert.AreEqual("12.50 €", Fmt.Eur(12.5f));
            Assert.AreEqual("4.5", Fmt.Dec(4.5f));
            Assert.AreEqual("4.50", Fmt.Dec(4.5f, 2));
            Assert.AreEqual("+12.5 %", Fmt.Pct(0.125f));
            Assert.AreEqual("4.2", Fmt.Rating(4.2f));
            Assert.AreEqual("12k", Fmt.Thousands(12345));
            Assert.AreEqual("1.5M", Fmt.Thousands(1500000));
            Assert.AreEqual("45 min", Fmt.Duration(45f));
            Assert.AreEqual("09:30", Fmt.Clock(570f));
            Lang.Set("de");
            Assert.AreEqual("1.234 €", Fmt.Money(1234));
        }

        // =====================================================================================
        // Stichproben: Vorlagen treffen die echten Sim-Texte
        // =====================================================================================
        [Test]
        public void CoreTemplates_MatchSimOutput()
        {
            Lang.ResetForTests();
            Lang.Set("en");
            Assert.AreEqual("Not enough money!", Lang.T("Nicht genug Geld!"));
            Assert.AreEqual("Loan of 1,000 € paid out. Interest: 2 % per day.", Lang.T("Kredit über 1,000 € ausgezahlt. Zinsen: 2 % pro Tag."));
            Assert.AreEqual("by Thu, 20:00 (3 days)", Lang.T("bis Do, 20:00 (3 Tage)"));
            Assert.AreEqual("Rating +0.1 stars", Lang.T("Bewertung +0.1 Sterne"));
            Assert.AreEqual("Ship 12 parcels.", Lang.T("Verschicke 12 Pakete."));
            Assert.AreEqual("Day 3", Lang.T("Tag 3"));
            Assert.AreEqual("Tuesday", Lang.T("Dienstag"));
            // Ereignis mit {product}: Platzhalter wird vor der Anzeige ersetzt
            var sim = TestUtil.Fresh(3);
            sim.NewGame("skip");
            var mail = sim.Events.Trigger("trend_alarm");
            Assert.IsNotNull(mail);
            StringAssert.StartsWith("#", Lang.T(mail.Text));
            StringAssert.Contains("is trending!", Lang.T(mail.Text));
        }

        // =====================================================================================
        // Abdeckung: Bot spielt auf Englisch
        // =====================================================================================

        /// <summary>Texte, die absichtlich (noch) nicht übersetzt werden.</summary>
        private static readonly string[] Whitelist = { };

        [Test]
        public void BalanceBot_English_SimTextsTranslated()
        {
            Lang.ResetForTests();
            Lang.Set("en");
            var seen = new HashSet<string>();
            void Add(string s)
            {
                if (!string.IsNullOrEmpty(s)) seen.Add(s);
            }

            foreach (PlayerType type in Enum.GetValues(typeof(PlayerType)))
            {
                foreach (int seed in new[] { 3, 11 })
                {
                    var bot = new BalanceBot(type, seed);
                    Hook(bot.Gm, Add);
                    for (int d = 0; d < 28 && !bot.Broke; d++)
                    {
                        bot.RunDay();
                        Snapshot(bot.Gm, Add);
                    }
                    Snapshot(bot.Gm, Add);
                }
            }
            AllEvents(Add);
            Extras(Add);

            var content = ContentStrings();
            foreach (var s in seen) Check(s);
            var mine = new List<string>();
            int foreign = 0;
            foreach (var m in Lang.Missing)
            {
                if (IsContent(m, content) || Array.IndexOf(Whitelist, m) >= 0) foreign++;
                else mine.Add(m);
            }
            mine.Sort(StringComparer.Ordinal);
            var sb = new StringBuilder();
            sb.AppendLine("Einträge gesamt: " + Lang.EntryCount + ", geprüfte Texte: " + seen.Count + ", fehlend (Inhalt/EnContent): " + foreign + ", fehlend (Kern): " + mine.Count);
            foreach (var m in mine) sb.AppendLine("  FEHLT: " + m.Replace("\n", "\\n"));
            Console.WriteLine(sb.ToString());
            string dump = Environment.GetEnvironmentVariable("LOC_DUMP");
            if (!string.IsNullOrEmpty(dump))
            {
                var all = new StringBuilder();
                foreach (var m in Lang.Missing) all.AppendLine("MISSING\t" + m.Replace("\n", "\\n"));
                foreach (var x in seen) all.AppendLine("PAIR\t" + x.Replace("\n", "\\n") + "\t=>\t" + Lang.T(x).Replace("\n", "\\n"));
                System.IO.File.WriteAllText(dump, all.ToString());
            }
            Assert.LessOrEqual(mine.Count, 0, sb.ToString());
        }

        /// <summary>Text übersetzen; bei mehrzeiligen Texten zusätzlich Zeilen prüfen, die unverändert übrig bleiben.</summary>
        private static void Check(string s)
        {
            string t = Lang.T(s);
            if (s.IndexOf('\n') < 0 || t == s) return;
            var outLines = new HashSet<string>(t.Split('\n'));
            foreach (var l in s.Split('\n'))
                if (l.Trim().Length > 0 && outLines.Contains(l)) Lang.T(l);
        }

        private static void Hook(Sim gm, Action<string> add)
        {
            gm.Toast += (text, kind) => add(text);
            gm.Events.MailReceived += m =>
            {
                add(m.Title);
                add(m.Sender);
                add(m.Text);
            };
            gm.MilestoneReached += (title, text) =>
            {
                add(title);
                add(text);
            };
            gm.DayEnded += s =>
            {
                foreach (var n in s.Notes) add(n);
                add(s.WeekdayName);
            };
        }

        private static void Snapshot(Sim gm, Action<string> add)
        {
            foreach (var m in gm.Events.Mails)
            {
                add(m.Title);
                add(m.Sender);
                add(m.Text);
                add(m.Result);
                add(m.Chosen);
                foreach (var c in m.Choices) add(c.Label);
            }
            foreach (var c in gm.Challenges)
            {
                add(c.Title);
                add(c.Desc);
                add(c.Sponsor);
            }
            foreach (var c in gm.Contracts)
            {
                add(c.Description);
                add(c.QualityText);
                add(gm.ContractDeadlineText(c));
            }
            var o = gm.CurrentObjective();
            add(o.Title);
            add(o.Text);
            foreach (var chip in gm.StatusChips()) add(chip[0]);
            add(gm.FestivalStatusText());
            foreach (var b in gm.Boosts) add(b.Name);
            foreach (var r in gm.Reviews) add(r.Text);
            foreach (var l in gm.StatsLines())
            {
                add(l[0]);
                add(l[1]);
                add(l[2]);
            }
            foreach (var p in GameData.Products)
            {
                add(gm.DemandLabel(p.Id));
                add(gm.Trends.State(p.Id).Reason);
                add(gm.Trends.Label(p.Id));
                var f = gm.Trends.Forecast(p.Id);
                add(f.Label);
                add(f.Hint);
            }
            foreach (var ord in gm.OrderQueue)
            {
                add(Sim.OrderNextStep(ord));
                add(Sim.OrderNextPlace(ord));
                add(gm.OrderTimeLeftText(ord));
            }
            foreach (var v in gm.TikTokVideos) add(v.Title);
            add(gm.TikTokBlocker());
            add(gm.NextGarbagePickupText());
        }

        /// <summary>Jedes Ereignis mit jeder Entscheidung einmal auslösen.</summary>
        private static void AllEvents(Action<string> add)
        {
            int seed = 100;
            foreach (var ev in EventData.All)
            {
                int choices = ev.HasChoices ? ev.Choices.Length : 1;
                for (int ci = 0; ci < choices; ci++)
                {
                    for (int rep = 0; rep < 3; rep++)
                    {
                        var sim = TestUtil.Level3(seed++);
                        sim.NewGame("skip");
                        sim.AddXp(GameData.LevelThreshold(5));
                        sim.Money = 5000;
                        foreach (var p in GameData.Products)
                        {
                            if (!sim.ProductAvailable(p.Id)) continue;
                            sim.Listed[p.Id] = true;
                            if (sim.Stock.ContainsKey(p.Id)) sim.Stock[p.Id].Qty = 30;
                        }
                        Hook(sim, add);
                        var m = sim.Events.Trigger(ev.Id);
                        if (m == null) continue;
                        if (ev.HasChoices && string.IsNullOrEmpty(ev.Choices[ci].Minigame)) sim.Events.Choose(m.Id, ci);
                        if (ev.HasChoices && !string.IsNullOrEmpty(ev.Choices[ci].Minigame))
                        {
                            var pitch = new PitchGame(sim);
                            foreach (var q in pitch.Questions)
                            {
                                add(q.Juror);
                                add(q.Text);
                                foreach (var a in q.Answers)
                                {
                                    add(a.Text);
                                    add(a.StatLabel);
                                }
                            }
                            for (int qi = 0; !pitch.Finished; qi++) add(pitch.Choose((qi + rep) % 3));
                            var res = pitch.Evaluate();
                            add(res.Title);
                            add(res.Text);
                            add(sim.Events.ResolveMinigame(m.Id, ev.Choices[ci].Label, res.Text, res.Effects));
                        }
                        sim.Events.ResolvePendingChoices();
                        Snapshot(sim, add);
                    }
                }
            }
        }

        /// <summary>Seltene Pfade gezielt auslösen (Fest, Störung, Defekt, Mama, Kredit, Fehlermeldungen).</summary>
        private static void Extras(Action<string> add)
        {
            var sim = TestUtil.Level3(77);
            sim.NewGame("skip");
            sim.AddXp(GameData.LevelThreshold(6));
            Hook(sim, add);
            sim.AdvanceMinutes(60f);
            sim.AdminStartFestival(false);
            add(sim.FestivalStatusText());
            sim.AdvanceMinutes(200f);
            add(sim.FestivalStatusText());
            foreach (var p in GameData.Products)
                if (sim.ProductAvailable(p.Id)) sim.AddDefectBatch(p.Id, 3);
            sim.TakeLoan(500);
            sim.TakeLoan(99999999);
            sim.RepayLoan(100);
            sim.RepayLoan(100000);
            sim.Money = 0;
            sim.BuyUpgrade("conveyor");
            sim.ResetSkills();
            sim.Hire("packer");
            Snapshot(sim, add);

            // Fehlermeldungen und seltene Pfade auf einer frischen Simulation
            var s2 = TestUtil.Fresh(78);
            s2.NewGame("skip");
            Hook(s2, add);
            s2.PickupCrate();
            s2.PickupPackage();
            s2.FoldCarton();
            s2.PickupReturn();
            foreach (var p in GameData.Products)
            {
                s2.PickItem(p.Id);
                add(s2.CanStartAd("fakebook", p.Id, 50));
                add(s2.CanStartAd("gugel", p.Id, 50));
                add(s2.CanCreateFakeSite(p.Id));
            }
            foreach (var store in new[] { ShopData.Electro, ShopData.PetShop })
                foreach (var it in ShopData.ItemsOf(store))
                {
                    add(s2.ShopBlockReason(it.Id));
                    s2.CartAdd(it.Id);
                }
            s2.ShopCheckout(ShopData.Electro);
            s2.Money = 100000;
            s2.AdminSetLevel(10);
            add(s2.TikTokBlocker());
            foreach (var store in new[] { ShopData.Electro, ShopData.PetShop })
            {
                foreach (var it in ShopData.ItemsOf(store)) s2.CartAdd(it.Id);
                s2.ShopCheckout(store);
            }
            foreach (var sp in new[] { "hund", "katze" })
            {
                var pet = s2.AdoptPet(sp);
                if (pet == null) continue;
                add(s2.PetMoodText(pet));
                s2.PetPet(sp);
                s2.PetPet(sp);
                s2.PetSetFollow(sp, true);
                s2.PetSetFollow(sp, false);
            }
            s2.PetsNewDay();
            foreach (var p in GameData.Products)
            {
                if (!s2.ProductAvailable(p.Id)) continue;
                s2.Listed[p.Id] = true;
                var ad = s2.StartFakebookAd(p.Id, 0, 0, 50);
                if (ad != null) s2.StopAdCampaign(ad.Id);
                s2.StartGugelAd(p.Id, 0, 80, 60);
                s2.CreateFakeSite(p.Id);
            }
            for (int i = 0; i < 6; i++) s2.CreateFakeProfile();
            s2.AdminExposeFakes();
            s2.DeleteFakeProfiles();
            s2.AdminFinishAds();
            foreach (var pk in GameData.Perks) s2.BuyPerk(pk.Id);
            foreach (var h in new[] { "bed", "chair", "desk", "coffee", "laptop" }) s2.BuyHomeItem(h);
            foreach (var e in GameData.Equipment) s2.BuyEquipment(e.Id);
            foreach (var d in GameData.Disruptions)
            {
                s2.StartDisruption(d.Id, 2);
                foreach (var chip in s2.StatusChips()) add(chip[0]);
                s2.EndDisruption();
            }
            s2.OrderGarbagePickup();
            s2.OrderGarbagePickup();
            s2.CollectGarbage();
            s2.AdminReturnWave(4);
            s2.AdminSkipMinutes(300f);
            for (int i = 0; i < 4; i++)
            {
                var r = s2.PickupReturn();
                if (r != null) s2.ProcessReturn(r, i % 2 == 0);
            }
            s2.AdminContractOffer();
            foreach (var c in s2.Contracts.ToArray())
            {
                add(c.Description);
                s2.AcceptContract(c.Id);
            }
            s2.GenerateWholesaleOffer();
            foreach (var c in s2.Challenges.ToArray()) s2.RerollChallenge(c.Id);
            s2.AdminAnnounceFestival();
            add(s2.FestivalStatusText());
            s2.TriggerTikTok(0.9f, false, GameData.Products[0].Id);
            // alle Ziele, Börsengang, Endlos-Ziele
            s2.TotalEarned = 9000000;
            s2.TotalShipped = 100000;
            s2.CheckGoals();
            s2.CheckGoals();
            Snapshot(s2, add);

            // Pleite mit Mama-Rettung (zweimal: einmal mit, einmal ohne weitere Rettung)
            for (int i = 0; i < 2; i++)
            {
                s2.Money = -50000;
                s2.EndDayNow();
                s2.StartNextDay();
            }
            Snapshot(s2, add);
            s2.Money = 100000;
            s2.EndDayNow();
            s2.StartNextDay();
            Snapshot(s2, add);
        }

        // =====================================================================================
        // Inhalts-Texte (Bereich EnContent) per Reflexion sammeln
        // =====================================================================================
        private static HashSet<string> ContentStrings()
        {
            var set = new HashSet<string>();
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (var t in new[] { typeof(GameData), typeof(ShopData), typeof(AchievementData), typeof(FinanceText) })
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    Walk(f.GetValue(null), set, visited, 0);
            var extra = new List<string>();
            foreach (var s in set)
            {
                string k = GameData.FillKeys(s);
                if (k != s) extra.Add(k);
            }
            foreach (var s in extra) set.Add(s);
            return set;
        }

        private static void Walk(object o, HashSet<string> set, HashSet<object> visited, int depth)
        {
            if (o == null || depth > 6) return;
            if (o is string s)
            {
                set.Add(s);
                return;
            }
            var type = o.GetType();
            if (type.IsPrimitive || type.IsEnum || o is RGBA || o is V3) return;
            if (!type.IsValueType && !visited.Add(o)) return;
            if (o is IDictionary dict)
            {
                foreach (DictionaryEntry e in dict)
                {
                    Walk(e.Key, set, visited, depth + 1);
                    Walk(e.Value, set, visited, depth + 1);
                }
                return;
            }
            if (o is IEnumerable en)
            {
                foreach (var x in en) Walk(x, set, visited, depth + 1);
                return;
            }
            if (type.Namespace != null && type.Namespace.StartsWith("DropshippingGame", StringComparison.Ordinal) || type.IsValueType)
                foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    Walk(f.GetValue(o), set, visited, depth + 1);
        }

        private static bool IsContent(string s, HashSet<string> content)
        {
            if (content.Contains(s)) return true;
            // Mehrzeilig: alle Zeilen aus dem Inhalt (z. B. Erklär-Mails)
            if (s.IndexOf('\n') >= 0)
            {
                foreach (var l in s.Split('\n'))
                    if (l.Trim().Length > 0 && !content.Contains(l) && !content.Contains(l.Trim())) return false;
                return true;
            }
            return false;
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object o) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o);
        }
    }
}
