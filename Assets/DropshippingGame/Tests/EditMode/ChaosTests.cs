using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>
    /// QA-„Chaos-Affe“ für die Spiellogik: viele Seeds, je 30–60 Spieltage mit zufälligen gültigen UND
    /// ungültigen Aufrufen der öffentlichen Sim-API (Einkauf, Shop, Packkette, Retouren, Aufträge, Werbung,
    /// TikTok, Fest, Läden &amp; Tiere, Börse, Kredite, Perks/Skills, Ausbau, Personal, Möbel, Admin-Cheats,
    /// Schwierigkeit, Endlos-Modus, Speichern/Laden mitten im Geschehen). Nach jedem Schritt werden
    /// Invarianten geprüft (siehe <see cref="ChaosMonkey.CheckInvariants"/>).
    ///
    /// Standard: kurze Fassung (wenige Seeds, 30 Tage). Lange Fassung: Kategorie „ChaosLong“ (Explicit),
    /// z. B. <c>dotnet test tools/compilecheck/Tests.csproj --filter TestCategory=ChaosLong</c>.
    ///
    /// Erlaubte Unterschiede beim Speichern→Laden→Speichern: nur "saved_unix" (Uhrzeit des Speicherns) –
    /// und beim Speichern NACH Feierabend (vor dem nächsten Tag) "time_minutes" (der Stand lädt eine Minute
    /// vor 20 Uhr, damit der Tag sauber neu abgerechnet wird).
    /// </summary>
    public class ChaosTests
    {
        private static IEnumerable<int> ShortSeeds()
        {
            for (int s = 1; s <= 6; s++) yield return s;
        }

        [Test, TestCaseSource(nameof(ShortSeeds))]
        public void Chaos_Short(int seed)
        {
            var m = new ChaosMonkey(seed);
            try
            {
                m.Run(30);
            }
            finally
            {
                m.Cleanup();
            }
            TestContext.Out.WriteLine(m.Report());
        }

        [Test, Explicit("Lange Chaos-Runde (~40 Seeds × 60 Tage)"), Category("ChaosLong")]
        public void Chaos_Long()
        {
            var sb = new StringBuilder();
            // Andere Seeds ohne Code-Änderung: Umgebungsvariablen CHAOS_SEED_START / CHAOS_SEED_COUNT.
            int start = int.TryParse(Environment.GetEnvironmentVariable("CHAOS_SEED_START"), out int st) ? st : 100;
            int count = int.TryParse(Environment.GetEnvironmentVariable("CHAOS_SEED_COUNT"), out int ct) ? ct : 40;
            for (int seed = start; seed < start + count; seed++)
            {
                var m = new ChaosMonkey(seed);
                try
                {
                    m.Run(30 + seed % 31);
                }
                finally
                {
                    m.Cleanup();
                }
                sb.AppendLine(m.Report());
            }
            TestContext.Out.WriteLine(sb.ToString());
        }
    }

    /// <summary>Der Chaos-Affe: spielt eine Sim mit zufälligen Aktionen und prüft Invarianten.</summary>
    public sealed class ChaosMonkey
    {
        public readonly int Seed;
        public readonly Sim Sim;
        private readonly Random _r;
        private readonly string _dir;

        /// <summary>Gegenstände in der Hand oder auf dem Boden (die "Welt" des Affen).</summary>
        private readonly List<ItemData> _hands = new List<ItemData>();
        private readonly Queue<string> _log = new Queue<string>();
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        private readonly List<KeyValuePair<string, float>> _weights = new List<KeyValuePair<string, float>>();
        private readonly Dictionary<string, Action> _actions = new Dictionary<string, Action>();
        private float _weightSum;

        // ---- Statistik -----------------------------------------------------------------------
        public int Steps, Roundtrips, FileSaves, GameOvers, Days, MamaRescues, EndlessMax, Achievements, NewGames;

        // ---- Verfolgung für Invarianten ---------------------------------------------------------
        private int _lastLevel = 1, _lastXp, _lastDay = 1, _lastEndless, _lastMoney;
        private float _lastClock;
        private bool _regressOk;
        private readonly HashSet<string> _achSeen = new HashSet<string>();
        private readonly HashSet<string> _goalsSeen = new HashSet<string>();
        private readonly HashSet<string> _challengesSeen = new HashSet<string>();
        private DaySummary _summary;
        private bool _gameOver;
        private int _dayEndedFor = -1;
        private int _gameOversInRow;
        private string _current = "";

        private static readonly string[] Placeholders = { "{product}", "{company}", "{n}", "{tag}", "{brand}", "{p}", "{key:" };

        public ChaosMonkey(int seed)
        {
            Seed = seed;
            _r = new Random(seed * 7919 + 17);
            _dir = Path.Combine(Path.GetTempPath(), "ds_chaos_" + seed + "_" + Guid.NewGuid().ToString("N"));
            Sim = new Sim(seed);
            Sim.AdsReseed(seed + 11);
            Sim.FestivalReseed(seed + 13);
            Sim.Market.SeedExchange(seed + 17);
            Sim.SaveDir = _dir;
            Sim.Slot = 1 + seed % Sim.SaveSlots;
            Sim.CollectWorldState = CollectWorld;
            Hook();
            Register();
            StartGame(seed % 5 == 0 ? "tutorial" : (seed % 7 == 3 ? "intro" : "skip"));
        }

        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            }
            catch (Exception)
            {
                // Aufräumen ist nicht kritisch
            }
        }

        public string Report()
        {
            var sb = new StringBuilder();
            sb.Append("Seed " + Seed + ": " + Days + " Tage, " + Steps + " Schritte, " + Roundtrips + " Roundtrips, " + FileSaves +
                      " Datei-Saves, Pleiten " + GameOvers + ", Mama " + MamaRescues + ", Neustarts " + NewGames + ", Erfolge " + Achievements +
                      ", Endlos-Stufe " + EndlessMax + ", Level " + Sim.Level + ", Umsatz " + Sim.TotalEarned + ", Aktionen " + _counts.Count);
            return sb.ToString();
        }

        // =====================================================================================
        // Ablauf
        // =====================================================================================
        private void StartGame(string mode)
        {
            _hands.Clear();
            Sim.NextDifficulty = Pick(new[] { "normal", "normal", "entspannt", "hart", "quatsch" });
            Sim.NewGame(mode);
            Sim.ShopSeed = 1000 + Seed;
            Sim.InGame = true;
            NewGames++;
            if (mode == "intro") PlayIntro();
            ResetTracking();
        }

        private void PlayIntro()
        {
            Do("intro", () =>
            {
                Sim.StartShift();
                for (int i = 0; i < 6; i++)
                {
                    var plate = Sim.DinerTakePlate();
                    if (plate == null) break;
                    if (_r.Next(4) == 0) Sim.DinerServe(99, plate); // falscher Tisch
                    Sim.DinerServe(plate.Table, plate);
                }
                Assert.IsFalse(Sim.SaveGame(true), "Im Imbiss wird nicht gespeichert");
                Sim.FinishIntro();
            });
        }

        private void ResetTracking()
        {
            _lastLevel = Sim.Level;
            _lastXp = Sim.Xp;
            _lastDay = Sim.Day;
            _lastClock = Sim.BClock();
            _lastEndless = Sim.EndlessTier;
            _lastMoney = Sim.Money;
            _achSeen.Clear();
            foreach (var k in Sim.Achievements.Keys) _achSeen.Add(k);
            _goalsSeen.Clear();
            foreach (var g in Sim.GoalsDone) _goalsSeen.Add(g);
            _challengesSeen.Clear();
            foreach (var c in Sim.Challenges)
                if (c.Done) _challengesSeen.Add(c.Week + ":" + c.Id);
            _regressOk = false;
        }

        public void Run(int days)
        {
            int startDay = Sim.Day, guard = 0;
            while (Days < days && guard++ < days * 3)
            {
                RunDay();
            }
            Assert.GreaterOrEqual(Days, days, "Seed " + Seed + ": zu wenige Tage gespielt (Pleiten/Neustarts?) – Start " + startDay);
        }

        private void RunDay()
        {
            int safety = 0;
            while (!Sim.DayOver)
            {
                if (++safety > 4000) Fail("Tag endet nicht (Uhr " + Sim.TimeMinutes + ")");
                if (safety % 400 == 0) Sim.AdminTimeScale = 1f; // Tester stellt die Zeit wieder an
                Step();
            }
            DayOverPhase();
        }

        private void Step()
        {
            Steps++;
            float dt = _r.Next(10) == 0 ? (float)_r.NextDouble() * 60f : 0.5f + (float)_r.NextDouble() * 8f;
            Do("tick", () => Sim.Tick(dt));
            CheckInvariants("tick");
            if (Sim.DayOver) return;
            int n = _r.Next(4);
            for (int i = 0; i < n && !Sim.DayOver; i++)
            {
                string name = PickAction();
                Do(name, _actions[name]);
                CheckInvariants(name);
            }
        }

        private void DayOverPhase()
        {
            Assert.IsNotNull(_summary, Ctx("Feierabend ohne DayEnded"));
            Assert.AreEqual(Sim.Day, _summary.Day, Ctx("DaySummary.Day"));
            var s = _summary;
            // Manchmal mitten im Feierabend speichern wollen: wird abgelehnt.
            if (_r.Next(25) == 0)
            {
                // Nach Feierabend wird bewusst nicht gespeichert (sonst Doppel-Abrechnung beim Laden).
                Do("save_dayover", () => Assert.IsFalse(Sim.SaveGame(true), Ctx("Speichern nach Feierabend muss abgelehnt werden")));
                if (!Sim.DayOver)
                {
                    // Der geladene Stand liegt eine Minute vor Feierabend: den Tag sauber zu Ende spielen.
                    _summary = null;
                    int g = 0;
                    Sim.AdminTimeScale = 1f;
                    while (!Sim.DayOver && g++ < 50) Do("tick", () => Sim.Tick(1f));
                    Assert.IsTrue(Sim.DayOver, Ctx("Tag nach Laden beendet"));
                    Assert.IsNotNull(_summary, Ctx("DayEnded nach Laden"));
                    s = _summary;
                }
            }
            bool expectOver = s.Bankrupt && !Sim.AdminNoBankrupt;
            _gameOver = false;
            int day = Sim.Day;
            Do("next_day", () => Sim.StartNextDay());
            Days++;
            if (_gameOver)
            {
                Assert.IsTrue(expectOver, Ctx("Pleite ohne Pleite-Abrechnung (MoneyAfter " + s.MoneyAfter + ")"));
                GameOvers++;
                _gameOversInRow++;
                Assert.AreEqual(day, Sim.Day, Ctx("Pleite: kein neuer Tag"));
                // Wie im Spiel: letzten Spielstand laden – nach zwei Pleiten in Folge neu anfangen.
                if (_gameOversInRow <= 2 && Sim.HasSave(Sim.Slot) && Sim.LoadGame(Sim.Slot))
                {
                    RebuildHands();
                    ResetTracking();
                }
                else
                {
                    StartGame("skip");
                    _gameOversInRow = 0;
                }
                _summary = null;
                return;
            }
            Assert.IsFalse(expectOver, Ctx("Pleite-Abrechnung, aber kein GameOver"));
            _gameOversInRow = 0;
            Assert.AreEqual(day + 1, Sim.Day, Ctx("Neuer Tag"));
            Assert.IsFalse(Sim.DayOver, Ctx("Neuer Tag offen"));
            _summary = null;
            CheckInvariants("next_day");
        }

        // =====================================================================================
        // Ereignisse beobachten
        // =====================================================================================
        private void Hook()
        {
            var s = Sim;
            s.DayEnded += d =>
            {
                Expect(d != null, "DayEnded(null)");
                if (d == null) return;
                Expect(s.DayOver, "DayEnded ohne Feierabend");
                Expect(!(_summary != null && _dayEndedFor == s.Day), "DayEnded doppelt am selben Tag");
                _dayEndedFor = s.Day;
                _summary = d;
                Expect(d.Bankrupt == (d.MoneyAfter < GameData.BankruptLimit && !s.AdminNoBankrupt), "Bankrupt-Flag passt nicht zu MoneyAfter " + d.MoneyAfter);
                int fixedCosts = d.Rent + d.Wages + d.Upkeep + d.Interest;
                Expect(s.Money - fixedCosts == d.MoneyAfter, "MoneyAfter " + d.MoneyAfter + " ≠ Geld − Fixkosten " + (s.Money - fixedCosts));
                foreach (var n in d.Notes) CheckText(n, "Kassenbon");
            };
            s.DayStarted += day =>
            {
                Expect(s.Day == day, "DayStarted-Tag");
                Expect(!s.DayOver, "DayStarted bei Feierabend");
            };
            s.GameOver += reason =>
            {
                Expect(!string.IsNullOrEmpty(reason), "GameOver ohne Grund");
                _gameOver = true;
            };
            s.MamaRescued += gift =>
            {
                Expect(gift > 0, "Mama-Geschenk " + gift);
                Expect(s.MamaRescuesLeft >= 0, "Mama-Rettungen negativ");
                MamaRescues++;
            };
            s.LevelUp += lvl =>
            {
                Expect(lvl >= 2 && lvl <= GameData.MaxLevel, "LevelUp " + lvl);
                Expect(s.Level == lvl, "LevelUp ≠ Level");
            };
            s.AchievementUnlocked += a =>
            {
                Expect(a != null && _achSeen.Add(a.Id), "Erfolg doppelt freigeschaltet: " + a?.Id);
                Achievements++;
            };
            s.GoalCompleted += g =>
            {
                Expect(g != null && _goalsSeen.Add(g.Id), "Ziel doppelt erreicht: " + g?.Id);
            };
            s.ChallengeCompleted += c =>
            {
                Expect(c != null && _challengesSeen.Add(c.Week + ":" + c.Id), "Wochenziel doppelt: " + c?.Id);
            };
            s.Toast += (text, kind) => CheckText(text, "Toast");
            s.Events.MailReceived += m =>
            {
                Expect(m != null, "Mail null");
                if (m == null) return;
                CheckText(m.Title, "Mail-Titel");
                CheckText(m.Text, "Mail-Text");
                CheckText(m.Sender, "Mail-Absender");
            };
            s.OrderReceived += o =>
            {
                Expect(o != null && GameData.IsProduct(o.Product) && o.Price > 0, "Bestellung unvollständig: " + o?.Product + " " + o?.Price);
            };
            s.OrderShipped += (o, reward, onTime) =>
            {
                Expect(o != null && reward > 0, "OrderShipped ohne Zettel/Erlös");
            };
            s.PackageShipped += (pkg, reward, where) =>
            {
                Expect(pkg != null && GameData.IsProduct(pkg.Product), "PackageShipped ungültig");
            };
            s.ContractOffered += c =>
            {
                Expect(c != null && GameData.IsProduct(c.Product) && c.Quantity > 0 && c.Payment > 0, "Auftrag unvollständig");
            };
            s.ContractDelivered += (c, n) => Expect(c != null && n > 0, "ContractDelivered");
            s.TrendChanged += (id, phase) => Expect(GameData.IsProduct(id), "Trend für unbekanntes Produkt " + id);
            s.FestivalEnded += f => Expect(f != null && f.Sold >= 0 && f.Revenue >= 0, "Fest-Bilanz");
            s.ReturnArrived += r => Expect(r != null && GameData.IsProduct(r.Product) && r.Kind == ItemKind.Return, "Retoure");
            s.MilestoneReached += (t, x) => CheckText(t + " " + x, "Meilenstein");
            s.DeliveryArrived += pid => Expect(GameData.IsProduct(pid), "Lieferung unbekannt");
        }

        private void CheckText(string text, string what)
        {
            if (text == null)
            {
                Expect(false, what + " ist null");
                return;
            }
            foreach (var p in Placeholders)
                if (text.Contains(p)) Expect(false, what + " mit offenem Platzhalter " + p + ": " + text);
            if (text.Contains("NaN") || text.Contains("∞")) Expect(false, what + " enthält NaN/∞: " + text);
        }

        // =====================================================================================
        // Invarianten
        // =====================================================================================
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

        private void F(float v, string what)
        {
            if (!Finite(v)) Fail(what + " ist " + v);
        }

        public void CheckInvariants(string after)
        {
            var s = Sim;
            _current = after;
            // ---- Geld & Zahlen ----
            // Überlauf-Erkennung: Vorzeichensprung von sehr viel zu sehr wenig (oder umgekehrt) oder Wert am int-Rand.
            if (s.Money < -2100000000 || s.Money > 2100000000) Fail("Geld am int-Rand (Überlauf?): " + s.Money);
            if (!after.StartsWith("admin", StringComparison.Ordinal) && after != "go_broke" && (long)_lastMoney * s.Money < 0 && Math.Abs((long)_lastMoney - s.Money) > 1500000000L) Fail("Geld springt über das Vorzeichen (Überlauf?): " + _lastMoney + " → " + s.Money);
            _lastMoney = s.Money;
            if (s.Debt < 0) Fail("Schulden negativ: " + s.Debt);
            if (s.MamaDebt < 0) Fail("Mama-Schulden negativ");
            if (s.Market.Savings < 0) Fail("Sparkonto negativ");
            if (s.TotalEarned < 0) Fail("Umsatz negativ (Überlauf?)");
            F(s.Reputation, "Bewertung");
            if (s.Reputation < 0.5f - 1e-4f || s.Reputation > 5f + 1e-4f) Fail("Bewertung außerhalb 0,5–5: " + s.Reputation);
            F(s.Awareness, "Bekanntheit");
            if (s.Awareness < -1e-4f || s.Awareness > 1.5f + 1e-4f) Fail("Bekanntheit außerhalb 0–1,5: " + s.Awareness);
            F(s.Energy, "Energie");
            if (s.Energy < 0f || s.Energy > GameData.EnergyMax + 1e-3f) Fail("Energie " + s.Energy);
            F(s.TimeMinutes, "Uhrzeit");
            if (s.StoryStage == "business" && (s.TimeMinutes < GameData.DayStart - 1e-3f || s.TimeMinutes > GameData.DayEnd + 1e-3f))
                Fail("Uhrzeit außerhalb des Geschäftstags: " + s.TimeMinutes);
            F(s.PortoCarry, "Porto-Rest");
            if (s.Waste < 0) Fail("Müll negativ");
            if (s.Level < 1 || s.Level > GameData.MaxLevel) Fail("Level " + s.Level);
            if (s.Xp < 0) Fail("XP negativ");
            if (s.Day < 1) Fail("Tag " + s.Day);
            if (s.EndlessTier < 0 || s.EndlessTier > 20) Fail("Endlos-Stufe " + s.EndlessTier);
            if (s.MamaRescuesLeft < 0) Fail("Mama-Rettungen negativ");
            // ---- Zeit & Fortschritt monoton ----
            float clock = s.BClock();
            F(clock, "Geschäftsuhr");
            if (!_regressOk)
            {
                if (s.Day < _lastDay) Fail("Tag läuft rückwärts: " + _lastDay + " → " + s.Day);
                if (clock < _lastClock - 1e-3f) Fail("Uhr läuft rückwärts: " + _lastClock + " → " + clock);
                if (s.Level < _lastLevel) Fail("Level sinkt: " + _lastLevel + " → " + s.Level);
                if (s.Xp < _lastXp) Fail("XP sinkt: " + _lastXp + " → " + s.Xp);
                if (s.EndlessTier < _lastEndless) Fail("Endlos-Stufe sinkt");
            }
            _regressOk = false;
            _lastDay = s.Day;
            _lastClock = clock;
            _lastLevel = s.Level;
            _lastXp = s.Xp;
            _lastEndless = s.EndlessTier;
            EndlessMax = Math.Max(EndlessMax, s.EndlessTier);
            // ---- Lager ----
            foreach (var p in GameData.Products)
            {
                if (!s.Stock.TryGetValue(p.Id, out var e)) Fail("Lagereintrag fehlt: " + p.Id);
                if (e.Qty < 0) Fail("Lager negativ: " + p.Id + " = " + e.Qty);
                F(e.Quality, "Qualität " + p.Id);
                if (s.StandStock.TryGetValue(p.Id, out int st) && st < 0) Fail("Stand negativ: " + p.Id);
                if (s.FestivalQty(p.Id) < 0) Fail("Festvorrat negativ");
                if (s.DefectsOf(p.Id) < 0) Fail("Defekte negativ");
            }
            foreach (var k in s.Stock.Keys)
                if (!GameData.IsProduct(k)) Fail("Lager mit unbekanntem Produkt " + k);
            foreach (var k in s.Listed.Keys)
                if (!GameData.IsProduct(k)) Fail("Shop listet unbekanntes Produkt " + k);
            foreach (var k in s.ShopPrices.Keys)
                if (!GameData.IsProduct(k)) Fail("Preis für unbekanntes Produkt " + k);
            foreach (var kv in s.ShopPrices)
                if (kv.Value < 1 || kv.Value > 999) Fail("Shop-Preis außerhalb 1–999: " + kv.Key + " = " + kv.Value);
            if (s.StandTotal() > GameData.StandCapacity) Fail("Stand überfüllt");
            if (s.FestivalTotal() > GameData.FestivalCapacity) Fail("Marktstand überfüllt");
            for (int i = 0; i < 3; i++)
                if (s.Packaging[i] < 0 || s.FlatPackaging[i] < 0) Fail("Kartons negativ (Größe " + i + ")");
            foreach (var c in s.DockCrates)
            {
                if (c == null) Fail("Kiste null am Wareneingang");
                if (c.Quantity < 0 || !GameData.IsProduct(c.Product)) Fail("Kiste ungültig: " + c.Product + " × " + c.Quantity);
            }
            foreach (var d in s.TravelingDeliveries)
                if (d.Quantity <= 0 || !GameData.IsProduct(d.Product) || !Finite(d.ArriveAt)) Fail("Lieferung ungültig");
            // ---- Bestellzettel ----
            var ids = new HashSet<int>();
            foreach (var o in s.OrderQueue)
            {
                if (o == null) Fail("Bestellung null in Warteschlange");
                if (!ids.Add(o.Id)) Fail("Doppelte Bestellnummer " + o.Id);
                if (o.Stage != OrderStage.Queued) Fail("Wartende Bestellung " + o.Id + " im Stand " + o.Stage);
                CheckOrder(o);
            }
            foreach (var o in s.OrdersInWork)
            {
                if (o == null) Fail("Bestellung null in Arbeit");
                if (!ids.Add(o.Id)) Fail("Doppelte Bestellnummer " + o.Id + " (Warteschlange/Arbeit)");
                if (o.Stage == OrderStage.Queued) Fail("Angefangene Bestellung " + o.Id + " im Stand Queued");
                CheckOrder(o);
            }
            // Jeder angefangene Zettel hat einen Gegenstand (Hand/Boden, Paketstapel, Band) – und umgekehrt.
            var alive = new HashSet<int>();
            foreach (var it in _hands)
                if (it != null && it.OrderId > 0 && (it.Kind == ItemKind.Item || it.Kind == ItemKind.Package || it.Kind == ItemKind.Labeled)) alive.Add(it.OrderId);
            foreach (var p in s.PackedPackages)
            {
                if (p == null) Fail("Paket null im Stapel");
                if (p.OrderId > 0 && !alive.Add(p.OrderId)) Fail("Bestellung " + p.OrderId + " existiert doppelt als Gegenstand");
            }
            foreach (var c in s.ConveyorQueue)
            {
                if (c == null || c.Pkg == null) Fail("Leerer Eintrag auf dem Band");
                if (c.Pkg.OrderId > 0 && !alive.Add(c.Pkg.OrderId)) Fail("Bestellung " + c.Pkg.OrderId + " doppelt auf dem Band");
            }
            foreach (var o in s.OrdersInWork)
                if (!alive.Contains(o.Id)) Fail("Angefangene Bestellung " + o.Id + " ohne Gegenstand");
            foreach (int id in alive)
            {
                bool inWork = false;
                foreach (var o in s.OrdersInWork)
                    if (o.Id == id) inWork = true;
                if (!inWork) Fail("Gegenstand zu Bestellung " + id + ", aber Zettel nicht in Arbeit");
            }
            foreach (int id in ids)
                if (id >= s.NextOrderId) Fail("NextOrderId " + s.NextOrderId + " ≤ vergebene Nummer " + id);
            // ---- Großaufträge ----
            var cids = new HashSet<int>();
            foreach (var c in s.Contracts)
            {
                if (!cids.Add(c.Id)) Fail("Doppelte Auftragsnummer " + c.Id);
                if (c.Delivered < 0 || c.Delivered > c.Quantity) Fail("Auftrag " + c.Id + ": geliefert " + c.Delivered + " von " + c.Quantity);
                if (c.State == ContractState.Completed && c.Delivered != c.Quantity) Fail("Erfüllter Auftrag unvollständig");
                if (c.Id >= s.NextContractId) Fail("NextContractId zu klein");
                if (c.State == ContractState.Active && c.DeadlineDay < c.AcceptedDay - 1) Fail("Frist vor Annahme");
                if (!GameData.IsProduct(c.Product)) Fail("Auftrag mit unbekanntem Produkt");
            }
            // ---- Boosts, Trends, Markt ----
            foreach (var b in s.Boosts)
            {
                F(b.Mult, "Boost " + b.Source);
                F(b.EndsAt, "Boost-Ende " + b.Source);
                if (b.Mult <= 0f) Fail("Boost ≤ 0: " + b.Source);
            }
            foreach (var p in GameData.Products)
            {
                float t = s.TrendMult(p.Id);
                F(t, "Trend " + p.Id);
                if (t < GameData.TrendMin - 1e-4f || t > GameData.TrendMax + 1e-4f) Fail("Trend außerhalb: " + t);
                float d = s.DemandRate(p.Id);
                F(d, "Nachfrage " + p.Id);
                if (d < 0f) Fail("Nachfrage negativ");
            }
            F(s.OrderIntervalMinutes(), "Bestellintervall");
            foreach (var a in Market.AllAssets)
            {
                float price = s.Market.Prices[a.Id];
                F(price, "Kurs " + a.Id);
                if (price <= 0f) Fail("Kurs ≤ 0: " + a.Id);
                F(s.Market.Holdings[a.Id], "Bestand " + a.Id);
                F(s.Market.Invested[a.Id], "Einsatz " + a.Id);
                if (s.Market.Holdings[a.Id] < -1e-3f) Fail("Bestand negativ: " + a.Id);
            }
            // ---- Werbung, Tiere, Wochenziele ----
            foreach (var c in s.AdCampaigns)
            {
                F(c.Spent, "Werbeausgaben");
                if (c.Spent > c.Budget + 0.05f) Fail("Kampagne über Budget: " + c.Spent + " / " + c.Budget);
                F(c.Impressions, "Impressionen");
                F(c.Clicks, "Klicks");
            }
            if (s.FakeProfiles < 0 || s.FakeProfiles > GameData.MaxFakeProfiles) Fail("Fake-Profile " + s.FakeProfiles);
            foreach (var p in s.Pets)
            {
                F(p.Happiness, "Tierlaune");
                if (p.Happiness < 0f || p.Happiness > 1f) Fail("Tierlaune außerhalb 0–1");
            }
            if (s.Pets.Count > ShopData.MaxPets) Fail("Zu viele Tiere");
            if (s.PetFood < 0 || s.PetFoodPremium < 0) Fail("Futter negativ");
            foreach (var c in s.Challenges)
            {
                F(c.Progress, "Wochenziel-Fortschritt");
                F(c.Target, "Wochenziel-Ziel");
                if (c.Target <= 0f) Fail("Wochenziel ohne Ziel");
            }
            if (s.Skills.Count > s.SkillPointsTotal() && !_adminLeveledDown) Fail("Mehr Skills als Skillpunkte");
            if (s.PerkPointsSpent() > s.PerkPointsTotal() && !_adminLeveledDown) Fail("Mehr Perks als Hustle-Punkte");
            foreach (var r in s.ReturnsIncoming)
                if (r == null || r.Item == null || !GameData.IsProduct(r.Item.Product)) Fail("Retoure unterwegs ungültig");
            foreach (var r in s.DockReturns)
                if (r == null || r.Kind != ItemKind.Return) Fail("Retoure am Wareneingang ungültig");
            if (s.Events.Mails.Count > 40) Fail("Postfach > 40");
            var mids = new HashSet<int>();
            foreach (var m in s.Events.Mails)
                if (!mids.Add(m.Id)) Fail("Doppelte Mail-Nummer " + m.Id);
        }

        private bool _adminLeveledDown;

        private void CheckOrder(Order o)
        {
            if (!GameData.IsProduct(o.Product)) Fail("Bestellung " + o.Id + " mit unbekanntem Produkt " + o.Product);
            if (o.Id < GameData.FirstOrderId) Fail("Bestellnummer " + o.Id);
            if (o.Price <= 0) Fail("Bestellpreis " + o.Price);
            F(o.Created, "Bestell-Eingang");
            F(o.DueAt, "Fälligkeit");
            if (o.DueAt < o.Created) Fail("Fälligkeit vor Eingang");
        }

        // =====================================================================================
        // Speichern / Laden
        // =====================================================================================
        private void CollectWorld()
        {
            Sim.WorldItems.Clear();
            foreach (var it in _hands)
                if (it != null) Sim.WorldItems.Add(new WorldItemSave { Item = it, Pos = new V3(1f, 0f, 2f), RotY = 0f });
            Sim.PlayerState = null;
        }

        private void RebuildHands()
        {
            _hands.Clear();
            foreach (var w in Sim.WorldItems)
                if (w != null && w.Item != null) _hands.Add(w.Item);
        }

        private string Snapshot(bool dropTime = false)
        {
            CollectWorld();
            var d = Sim.ToJson();
            d.Remove("saved_unix");
            if (dropTime) d.Remove("time_minutes");
            return Json.Write(d);
        }

        /// <summary>Speichern → Laden (im Speicher) → Speichern: das JSON muss gleich bleiben.</summary>
        private void MemoryRoundtrip()
        {
            if (Sim.StoryStage != "business" || Sim.DayOver) return;
            string a = Snapshot();
            int slot = Sim.Slot;
            var parsed = J.Obj(Json.Parse(a));
            Sim.ResetState();
            Sim.Slot = slot;
            Sim.FromJson(parsed);
            RebuildHands();
            string b = Snapshot();
            Roundtrips++;
            if (a != b) Fail("Speichern→Laden→Speichern weicht ab: " + Diff(J.Obj(Json.Parse(a)), J.Obj(Json.Parse(b)), ""));
            _regressOk = true;
        }

        /// <summary>Echter Datei-Spielstand: SaveGame → LoadGame → Vergleich.</summary>
        private void FileRoundtrip(bool dayOver = false)
        {
            if (Sim.StoryStage != "business") return;
            string a = Snapshot(dayOver);
            Assert.IsTrue(Sim.SaveGame(true), Ctx("Speichern fehlgeschlagen: " + Sim.LastSaveError));
            Assert.IsTrue(Sim.HasSave(Sim.Slot), Ctx("Spielstand fehlt"));
            var sum = Sim.ReadSummary(Sim.Slot);
            Assert.IsNotNull(sum, Ctx("Zusammenfassung"));
            Assert.AreEqual(Sim.Day, sum.Day, Ctx("Zusammenfassung Tag"));
            Assert.IsTrue(Sim.LoadGame(Sim.Slot), Ctx("Laden fehlgeschlagen: " + Sim.LastLoadError));
            Assert.IsFalse(Sim.LoadedFromBackup, Ctx("Aus Sicherung geladen"));
            RebuildHands();
            string b = Snapshot(dayOver);
            FileSaves++;
            if (a != b) Fail("Datei Speichern→Laden weicht ab: " + Diff(J.Obj(Json.Parse(a)), J.Obj(Json.Parse(b)), ""));
            _regressOk = true;
        }

        private static string Diff(object a, object b, string path)
        {
            if (a is Dictionary<string, object> da && b is Dictionary<string, object> db)
            {
                foreach (var kv in da)
                {
                    if (!db.TryGetValue(kv.Key, out object bv)) return path + "/" + kv.Key + " fehlt nach dem Laden";
                    string d = Diff(kv.Value, bv, path + "/" + kv.Key);
                    if (d != null) return d;
                }
                foreach (var k in db.Keys)
                    if (!da.ContainsKey(k)) return path + "/" + k + " neu nach dem Laden";
                return null;
            }
            if (a is List<object> la && b is List<object> lb)
            {
                if (la.Count != lb.Count) return path + ": Länge " + la.Count + " ≠ " + lb.Count + Short("\n  vorher: ", la) + Short("\n  nachher: ", lb);
                for (int i = 0; i < la.Count; i++)
                {
                    string d = Diff(la[i], lb[i], path + "[" + i + "]");
                    if (d != null) return d;
                }
                return null;
            }
            string sa = Json.Write(a), sb = Json.Write(b);
            return sa == sb ? null : path + ": " + sa + " ≠ " + sb;
        }

        private static string Short(string label, object o)
        {
            string s = Json.Write(o);
            return label + (s.Length > 1500 ? s.Substring(0, 1500) + " …" : s);
        }

        // =====================================================================================
        // Hilfen
        // =====================================================================================
        private void Do(string name, Action a)
        {
            _log.Enqueue("T" + Sim.Day + " " + Fmt.Clock(Sim.TimeMinutes) + " " + name + " (Geld " + Sim.Money + ")");
            while (_log.Count > 40) _log.Dequeue();
            _counts[name] = (_counts.TryGetValue(name, out int n) ? n : 0) + 1;
            _current = name;
            try
            {
                a();
            }
            catch (AssertionException)
            {
                throw;
            }
            catch (Exception e)
            {
                Fail("Ausnahme in '" + name + "': " + e);
            }
            if (_deferred != null) Assert.Fail(_deferred);
        }

        private string Ctx(string msg) =>
            "Seed " + Seed + ", Tag " + Sim.Day + " " + Fmt.Clock(Sim.TimeMinutes) + ", nach '" + _current + "': " + msg +
            "\nLetzte Aktionen:\n  " + string.Join("\n  ", _log.ToArray());

        private void Fail(string msg) => Assert.Fail(Ctx(msg));

        /// <summary>Prüfung in Ereignis-Handlern: merkt sich den ersten Fehler (SafeEvents schluckt Ausnahmen).</summary>
        private void Expect(bool ok, string msg)
        {
            if (!ok && _deferred == null) _deferred = Ctx(msg);
        }

        private string _deferred;

        private T Pick<T>(IList<T> l) => l[_r.Next(l.Count)];
        private bool Chance(double p) => _r.NextDouble() < p;

        private string Pid()
        {
            int r = _r.Next(40);
            if (r == 0) return "quatsch";
            if (r == 1) return "";
            if (r == 2) return null;
            // Bevorzugt gelistete/freigeschaltete Produkte, damit die Packkette wirklich läuft.
            if (r < 25)
            {
                var listed = Sim.Events.ListedProducts();
                if (listed.Count > 0) return Pick(listed);
            }
            return Pick(GameData.Products).Id;
        }

        private string ValidPid() => Pick(GameData.Products).Id;

        private int ExtremeInt()
        {
            switch (_r.Next(12))
            {
                case 0: return 0;
                case 1: return -1;
                case 2: return -100000;
                case 3: return int.MinValue;
                case 4: return int.MaxValue;
                case 5: return 1000000000;
                default: return _r.Next(1, 500);
            }
        }

        private float ExtremeFloat()
        {
            switch (_r.Next(12))
            {
                case 0: return float.NaN;
                case 1: return float.PositiveInfinity;
                case 2: return float.NegativeInfinity;
                case 3: return -1f;
                case 4: return 0f;
                case 5: return 1e9f;
                default: return (float)_r.NextDouble();
            }
        }

        private int Idx(int length) => Chance(0.08) ? Pick(new[] { -1, length, length + 5, int.MinValue }) : _r.Next(Math.Max(1, length));

        private ItemData Hand(Func<ItemData, bool> pred)
        {
            var l = _hands.FindAll(i => i != null && pred(i));
            return l.Count > 0 ? Pick(l) : null;
        }

        private void Hold(ItemData it)
        {
            if (it == null) return;
            _hands.Add(it);
            // Zu viel Kram am Boden: Ältestes geht verloren (wie aus der Welt gefallen).
            if (_hands.Count > 25)
            {
                var old = _hands[0];
                _hands.RemoveAt(0);
                Sim.DiscardItem(old);
            }
        }

        private string PickAction()
        {
            double r = _r.NextDouble() * _weightSum;
            foreach (var kv in _weights)
            {
                r -= kv.Value;
                if (r <= 0) return kv.Key;
            }
            return _weights[_weights.Count - 1].Key;
        }

        private void A(string name, float weight, Action a)
        {
            _weights.Add(new KeyValuePair<string, float>(name, weight));
            _actions[name] = a;
            _weightSum += weight;
        }

        // =====================================================================================
        // Aktionen
        // =====================================================================================
        private void Register()
        {
            var s = Sim;
            // ---- Einkauf & Shop ----
            A("buy_bulk", 6f, () =>
            {
                int p = Chance(0.9) ? GameData.ProductIndex(Pid() ?? "") : Idx(GameData.Products.Length);
                s.BuyBulk(p, Idx(GameData.BulkOptions.Length), Idx(GameData.Suppliers.Length));
            });
            A("quick_reorder", 2f, () => s.QuickReorder(Pid()));
            A("express_delivery", 0.5f, () => s.SetExpressDelivery(Chance(0.3)));
            A("list", 4f, () => s.SetListed(Pid(), Chance(0.85)));
            A("price", 2f, () =>
            {
                string pid = Pid();
                int price = Chance(0.3) ? ExtremeInt() : Mathx.RoundToInt(GameData.Product(pid ?? "").RefPrice * (0.5f + (float)_r.NextDouble()));
                s.SetShopPrice(pid, price);
            });
            A("promo", 0.5f, () => s.SetPromoActive(Chance(0.3)));
            A("pc", 0.5f, () =>
            {
                if (Chance(0.5)) s.OpenPc();
                else s.ClosePc();
            });
            // ---- Wareneingang ----
            A("pickup_crate", 4f, () => Hold(s.PickupCrateAt(Chance(0.9) ? 0 : Idx(s.DockCrates.Count))));
            A("unbox", 5f, () =>
            {
                var c = Hand(i => i.Kind == ItemKind.Crate);
                if (c == null) return;
                string pid = Chance(0.95) ? c.Product : Pid();
                // Falsches Regal prüft die Welt (Station.cs); der Kern bucht auf das genannte Produkt.
                if (pid != c.Product) pid = c.Product;
                if (s.UnboxCrate(pid, c.Quantity, c.Quality)) _hands.Remove(c);
            });
            A("return_crate", 1f, () =>
            {
                var c = Hand(i => i.Kind == ItemKind.Crate && i.Quantity > 0);
                if (c == null) return;
                _hands.Remove(c);
                s.ReturnCrate(c);
            });
            A("null_crate", 0.3f, () => s.ReturnCrate(null));
            // ---- Packkette ----
            A("pick", 8f, () => Hold(s.PickItem(Pid() ?? "")));
            A("return_item", 1.5f, () =>
            {
                var it = Hand(i => i.Kind == ItemKind.Item);
                if (it == null) return;
                _hands.Remove(it);
                s.ReturnItem(it);
            });
            A("wrap", 8f, () =>
            {
                var it = Hand(i => i.Kind == ItemKind.Item);
                if (it != null && s.WrapItem(it)) _hands.Remove(it);
                if (Chance(0.05)) s.WrapItem(null);
            });
            A("fold", 1.5f, () => s.FoldCarton());
            A("buy_packaging", 3f, () =>
            {
                int size = Idx(3);
                s.BuyPackaging(size, Chance(0.3), Idx(GameData.PackagingBatches.Length));
            });
            A("pickup_package", 7f, () => Hold(s.PickupPackage()));
            A("label", 7f, () =>
            {
                var p = Hand(i => i.Kind == ItemKind.Package);
                if (p == null) return;
                var labeled = p.Clone();
                labeled.Kind = ItemKind.Labeled;
                _hands[_hands.IndexOf(p)] = labeled;
                s.OnLabeled(labeled);
            });
            A("return_package", 1f, () =>
            {
                var p = Hand(i => i.Kind == ItemKind.Package || i.Kind == ItemKind.Labeled);
                if (p == null) return;
                _hands.Remove(p);
                s.ReturnPackage(p);
            });
            A("ship", 8f, () =>
            {
                var p = Hand(i => i.Kind == ItemKind.Labeled);
                if (p == null) return;
                _hands.Remove(p);
                int reward = s.ShipPackage(p, new V3(1f, 0f, 1f));
                Assert.Greater(reward, 0, Ctx("Versand ohne Erlös"));
            });
            A("conveyor", 1.5f, () =>
            {
                var p = Hand(i => i.Kind == ItemKind.Labeled);
                if (p != null && s.ConveyorInsert(p)) _hands.Remove(p);
                if (Chance(0.1)) s.ConveyorInsert(null);
            });
            A("discard", 0.8f, () =>
            {
                var it = Hand(i => true);
                if (it == null) return;
                _hands.Remove(it);
                s.DiscardItem(it);
            });
            A("prune", 1f, () =>
            {
                var alive = new List<int>();
                foreach (var it in _hands)
                    if (it != null) alive.Add(it.OrderId);
                int a = s.PruneOrdersInWork(alive), b = s.PruneOrdersInWork(alive);
                Assert.AreEqual(0, a + b, Ctx("PruneOrdersInWork entfernt Zettel, deren Gegenstand noch existiert"));
            });
            A("bad_calls", 0.5f, () =>
            {
                s.ShipPackage(null);
                s.ReturnPackage(null);
                s.ReturnItem(null);
                s.DiscardItem(null);
                s.ContractDeliver(null);
                s.ProcessReturn(null, true);
                s.PutBackReturn(null);
                s.StandAddCrate(null);
                s.FestivalAddCrate(null);
                s.UnboxCrate(Pid(), ExtremeInt(), ExtremeFloat());
                s.UpgradeState("quatsch");
                s.Events.Choose(-5, 0);
                s.RerollChallenge("quatsch");
                s.Fire(-1);
                s.Fire(99);
            });
            // ---- Retouren ----
            A("pickup_return", 2f, () => Hold(s.PickupReturn()));
            A("process_return", 2.5f, () =>
            {
                var r = Hand(i => i.Kind == ItemKind.Return);
                if (r != null && s.ProcessReturn(r, Chance(0.6))) _hands.Remove(r);
            });
            A("putback_return", 0.5f, () =>
            {
                var r = Hand(i => i.Kind == ItemKind.Return);
                if (r == null) return;
                _hands.Remove(r);
                s.PutBackReturn(r);
            });
            A("return_wave", 0.2f, () => s.AdminReturnWave(_r.Next(-2, 5)));
            // ---- Großaufträge ----
            A("contract_accept", 2f, () =>
            {
                var offers = s.ContractOffers();
                int id = offers.Count > 0 && Chance(0.85) ? Pick(offers).Id : Pick(new[] { -1, 0, 9999, s.NextContractId });
                if (s.AcceptContract(id)) Assert.LessOrEqual(s.ActiveContractCount(), s.MaxActiveContracts(), Ctx("Annahme über dem Auftrags-Limit"));
            });
            A("contract_decline", 0.6f, () =>
            {
                var offers = s.ContractOffers();
                if (offers.Count > 0) s.DeclineContract(Pick(offers).Id);
                else s.DeclineContract(-3);
            });
            A("contract_offer", 0.5f, () => s.GenerateContractOffer(Chance(0.2) ? ExtremeFloat() : 1f, Chance(0.2), Pid(), Chance(0.2) ? null : ""));
            A("contract_deliver", 3f, () =>
            {
                var it = Hand(i => i.Kind == ItemKind.Crate || (i.Kind == ItemKind.Item && i.ContractId > 0) || Chance(0.1));
                if (it == null) return;
                int before = it.Quantity;
                int n = s.ContractDeliver(it);
                Assert.GreaterOrEqual(n, 0, Ctx("Negative Liefermenge"));
                if (it.Kind == ItemKind.Item && n > 0) _hands.Remove(it);
                else if (it.Kind == ItemKind.Crate)
                {
                    Assert.AreEqual(before - n, it.Quantity, Ctx("Kiste: Restmenge"));
                    if (it.Quantity <= 0) _hands.Remove(it);
                }
            });
            A("pick_contract", 2f, () => Hold(s.PickForContract(Pid() ?? "")));
            A("rush", 0.3f, () =>
            {
                var act = s.ActiveContracts();
                if (act.Count > 0) s.RushContract(Pick(act), Chance(0.2) ? ExtremeFloat() : 1.3f);
            });
            A("wholesale", 0.3f, () => s.GenerateWholesaleOffer(Chance(0.5) ? "" : "Test GmbH", Pid() ?? "", 1f));
            // ---- Marketing ----
            A("ad_old", 1f, () => s.StartAdCampaign(Idx(GameData.AdTiers.Length)));
            A("ad_fakebook", 1.5f, () => s.StartFakebookAd(Pid(), Idx(GameData.AdTargets.Length), Idx(GameData.AdSlogans.Length), Chance(0.2) ? ExtremeInt() : _r.Next(5, 150)));
            A("ad_gugel", 1.5f, () => s.StartGugelAd(Pid(), Idx(GameData.GugelKeywords.Length), Chance(0.2) ? ExtremeInt() : _r.Next(5, 300), Chance(0.2) ? ExtremeInt() : _r.Next(5, 150)));
            A("ad_stop", 0.6f, () =>
            {
                var act = s.ActiveAds();
                s.StopAdCampaign(act.Count > 0 ? Pick(act).Id : -1);
            });
            A("fake_profile", 0.8f, () =>
            {
                if (Chance(0.85)) s.CreateFakeProfile();
                else s.DeleteFakeProfiles();
            });
            A("fake_site", 0.8f, () =>
            {
                if (Chance(0.8)) s.CreateFakeSite(Pid());
                else if (s.FakeSites.Count > 0) s.DeleteFakeSite(Pick(s.FakeSites));
                else s.DeleteFakeSite(null);
            });
            A("expose", 0.15f, () => s.AdminExposeFakes());
            A("tiktok", 2f, () =>
            {
                var take = new TikTokTake
                {
                    Duration = Chance(0.1) ? ExtremeFloat() : 5f + (float)_r.NextDouble() * 15f, VisibleFrac = (float)_r.NextDouble(),
                    Centered = (float)_r.NextDouble(), DistanceFrac = (float)_r.NextDouble(), SteadyFrac = (float)_r.NextDouble(),
                    Light = Chance(0.05) ? float.NaN : (float)_r.NextDouble(), AngleDegrees = (float)_r.NextDouble() * 180f, Hook = Chance(0.5),
                    Actions = _r.Next(5), BrandFrac = (float)_r.NextDouble(), Hype = s.TrendMult(ValidPid()),
                };
                string format = Chance(0.9) ? Pick(TikTokFormats.All) : "quatsch";
                var rating = TikTokScoring.Rate(take, format, s.TrendingTikTokFormat());
                s.PostTikTokVideo(rating, Pid(), format);
            });
            A("tiktok_raw", 0.5f, () => s.TriggerTikTok(ExtremeFloat(), Chance(0.5), Pid()));
            // ---- Fest & Stand ----
            A("festival_admin", 0.3f, () =>
            {
                switch (_r.Next(3))
                {
                    case 0: s.AdminStartFestival(Chance(0.5)); break;
                    case 1: s.AdminAnnounceFestival(); break;
                    default: s.AdminEndFestival(); break;
                }
            });
            A("festival_pack", 2f, () =>
            {
                if (s.Festival == FestivalPhase.None && Chance(0.8)) return;
                Hold(s.FestivalPackCrate(Chance(0.8) ? s.FestivalNextPackProduct() : Pid()));
            });
            A("festival_stock", 2f, () =>
            {
                var c = Hand(i => i.Kind == ItemKind.Crate);
                if (c == null) return;
                s.FestivalAddCrate(c);
                if (c.Quantity <= 0) _hands.Remove(c);
            });
            A("festival_visit", 3f, () =>
            {
                if (s.Festival != FestivalPhase.Live) return;
                for (int i = 0; i < 6; i++)
                {
                    var v = s.FestivalVisitor();
                    if (v.Sold) Assert.IsTrue(v.Qty > 0 && v.Price > 0, Ctx("Festverkauf ohne Menge/Preis"));
                }
            });
            A("stand", 1.5f, () =>
            {
                if (!s.HasUpgrade("stand") && Chance(0.5)) s.BuyUpgrade("stand");
                var c = Hand(i => i.Kind == ItemKind.Crate);
                if (c != null)
                {
                    s.StandAddCrate(c);
                    if (c.Quantity <= 0) _hands.Remove(c);
                }
                for (int i = 0; i < 4; i++) s.StandTryBuy(out _, out _, out _);
            });
            // ---- Läden & Tiere ----
            A("shop", 2f, () =>
            {
                string store = Chance(0.5) ? ShopData.Electro : ShopData.PetShop;
                var items = ShopData.ItemsOf(store);
                for (int i = _r.Next(1, 4); i > 0; i--) s.CartAdd(Chance(0.95) ? Pick(items).Id : "quatsch");
                if (Chance(0.2)) s.CartRemove(Pick(items).Id);
                if (Chance(0.15)) s.CartClear(store);
                else s.ShopCheckout(store);
            });
            A("pets", 1f, () =>
            {
                string sp = Chance(0.9) ? Pick(ShopData.Species).Id : "drache";
                switch (_r.Next(4))
                {
                    case 0: s.PetPet(sp); break;
                    case 1: s.PetSetFollow(sp, Chance(0.5)); break;
                    case 2: s.AdoptPet(sp); break;
                    default:
                        if (Chance(0.1)) s.RemoveAllPets();
                        break;
                }
            });
            // ---- Finanzen ----
            A("trade_buy", 1.5f, () =>
            {
                string id = Chance(0.9) ? Pick(Market.AllAssets).Id : "QUATSCH";
                int amount = Chance(0.3) ? ExtremeInt() : _r.Next(1, Math.Max(2, Math.Min(2000, s.Money)));
                s.Market.Buy(id, amount);
            });
            A("trade_sell", 1.5f, () =>
            {
                string id = Chance(0.9) ? Pick(Market.AllAssets).Id : "QUATSCH";
                s.Market.Sell(id, Chance(0.3) ? ExtremeFloat() : (float)_r.NextDouble());
            });
            A("savings", 1f, () =>
            {
                if (Chance(0.5)) s.Market.Deposit(Chance(0.3) ? ExtremeInt() : _r.Next(1, Math.Max(2, s.Money)));
                else s.Market.Withdraw(Chance(0.3) ? ExtremeInt() : _r.Next(1, 3000));
            });
            A("market_news", 0.3f, () => s.Market.PostNews(_r.Next(-2, 5), _r.Next(-2, 3)));
            A("market_admin", 0.2f, () => s.Market.AdminMoveAll(Chance(0.2) ? ExtremeFloat() : 0.5f + (float)_r.NextDouble(), Chance(0.5)));
            A("loan", 1f, () =>
            {
                if (Chance(0.5)) s.TakeLoan(Chance(0.3) ? ExtremeInt() : _r.Next(100, 3000));
                else s.RepayLoan(Chance(0.3) ? ExtremeInt() : _r.Next(1, 3000));
            });
            // ---- Fortschritt ----
            A("perk", 1f, () => s.BuyPerk(Chance(0.9) ? Pick(GameData.Perks).Id : "quatsch"));
            A("home", 1f, () => s.BuyHomeItem(Chance(0.9) ? Pick(GameData.HomeSlots).Id : "dach"));
            A("equipment", 1f, () => s.BuyEquipment(Chance(0.9) ? Pick(GameData.Equipment).Id : "quatsch"));
            A("carrier", 0.5f, () => s.SetCarrier(Idx(GameData.Carriers.Length)));
            A("defects", 0.6f, () =>
            {
                if (Chance(0.5)) s.RecallDefects(Pid());
                else s.AddDefectBatch(Pid(), Chance(0.3) ? ExtremeInt() : _r.Next(1, 20));
            });
            A("garbage", 0.8f, () =>
            {
                if (Chance(0.6)) s.OrderGarbagePickup();
                else s.CollectGarbage();
            });
            A("disruption", 0.2f, () =>
            {
                if (Chance(0.6)) s.StartDisruption(Chance(0.9) ? Pick(GameData.Disruptions).Id : "quatsch", _r.Next(-1, 4));
                else s.EndDisruption();
            });
            A("skill", 1f, () => s.LearnSkill(Chance(0.9) ? Pick(GameData.Skills).Id : "quatsch"));
            A("respec", 0.2f, () => s.ResetSkills());
            A("skill_points", 0.2f, () => s.AddSkillPoints(_r.Next(-3, 3)));
            A("upgrade", 1.5f, () => s.BuyUpgrade(Chance(0.9) ? Pick(GameData.Upgrades).Id : "quatsch"));
            A("decor", 0.5f, () => s.BuyDecor(Chance(0.9) ? Pick(GameData.Decor).Id : "quatsch"));
            A("lifestyle", 0.5f, () => s.BuyLifestyle(Chance(0.9) ? Pick(GameData.Lifestyle).Id : "quatsch"));
            A("hire", 1f, () => s.Hire(Chance(0.9) ? Pick(GameData.StaffRoles).Id : "quatsch"));
            A("fire", 0.3f, () => s.Fire(Idx(s.Staff.Count)));
            A("furniture", 0.6f, () =>
            {
                string key = Chance(0.9) ? "st:" + _r.Next(1, 4) + ":Regal:" + _r.Next(5) : (Chance(0.5) ? "" : null);
                s.SetFurniture(key, Chance(0.2) ? ExtremeFloat() : (float)_r.NextDouble() * 20f, Chance(0.2) ? ExtremeFloat() : (float)_r.NextDouble() * 20f,
                    Chance(0.2) ? ExtremeFloat() : _r.Next(-720, 720));
                s.GetFurniture(key);
            });
            A("brand", 0.4f, () =>
            {
                switch (_r.Next(3))
                {
                    case 0: s.SetBrandName(Chance(0.2) ? null : (Chance(0.2) ? new string('X', 80) : "Marke" + _r.Next(100))); break;
                    case 1: s.SetBrandLogo(Idx(GameData.LogoNames.Length)); break;
                    default: s.SetBrandColor(Pick(GameData.BrandPalette)); break;
                }
            });
            A("challenge", 0.4f, () =>
            {
                if (s.Challenges.Count > 0 && Chance(0.7)) s.RerollChallenge(Pick(s.Challenges).Id);
                else s.AddBonusChallenge("ship", Chance(0.2) ? ExtremeFloat() : 10f, 100, 20, "Chaos", "Chaos-Wette", "Test", "phone");
            });
            // ---- Ereignisse & Mails ----
            A("mail_choose", 2f, () =>
            {
                var pending = s.Events.Mails.FindAll(m => m.Pending);
                if (pending.Count == 0) return;
                var mail = Pick(pending);
                int idx = Chance(0.9) ? _r.Next(Math.Max(1, mail.Choices.Count)) : Pick(new[] { -1, 99 });
                if (idx >= 0 && idx < mail.Choices.Count && mail.Choices[idx].Minigame == "pitch")
                {
                    var pitch = new PitchGame(s);
                    while (!pitch.Finished) pitch.Choose(_r.Next(-1, 5));
                    var res = pitch.Evaluate();
                    s.Events.ResolveMinigame(mail.Id, mail.Choices[idx].Label, res.Title, res.Effects);
                }
                else s.Events.Choose(mail.Id, idx);
                s.Events.MarkRead(mail.Id);
            });
            A("event", 0.6f, () =>
            {
                string id = Chance(0.95) ? Pick(EventData.All).Id : "quatsch";
                s.AdminTriggerEvent(id);
            });
            // ---- Speichern & Laden ----
            A("roundtrip", 1.2f, MemoryRoundtrip);
            A("file_save", 0.4f, () => FileRoundtrip());
            A("old_save", 0.15f, () =>
            {
                // Zeitreise: Stand in einen anderen Slot sichern und später wieder laden (wie "Spielstand laden" im Menü).
                if (s.StoryStage != "business") return;
                int other = s.Slot % Sim.SaveSlots + 1, own = s.Slot;
                if (Chance(0.5) || !s.HasSave(other))
                {
                    s.Slot = other;
                    Assert.IsTrue(s.SaveGame(true), Ctx("Speichern in Slot " + other));
                    s.Slot = own;
                }
                else if (s.LoadGame(other))
                {
                    RebuildHands();
                    ResetTracking();
                }
                else Fail("Alter Spielstand nicht ladbar: " + s.LastLoadError);
            });
            A("go_broke", 0.12f, () =>
            {
                // Grober Fehlkauf kurz vor Feierabend: Pleite-Abrechnung, Mama, GameOver, Laden.
                if (s.TimeMinutes < GameData.DayEnd - 120f) return;
                s.AdjustMoney(-(Math.Max(0, s.Money) + 600 + _r.Next(5000)), "other");
                if (Chance(0.5)) s.EndDayNow();
            });
            // ---- Admin & Cheats ----
            A("admin_money", 0.3f, () =>
            {
                if (Chance(0.7)) s.AdminAddMoney(Chance(0.4) ? ExtremeInt() : _r.Next(-500, 5000));
                else s.AdminSetMoney(Chance(0.4) ? ExtremeInt() : _r.Next(-400, 50000));
            });
            A("admin_level", 0.2f, () =>
            {
                int lvl = Chance(0.2) ? ExtremeInt() : _r.Next(0, 12);
                if (Mathx.Clamp(lvl, 1, GameData.MaxLevel) < s.Level)
                {
                    _regressOk = true;
                    _adminLeveledDown = true;
                }
                s.AdminSetLevel(lvl);
            });
            A("admin_xp", 0.3f, () => s.AdminAddXp(Chance(0.2) ? ExtremeInt() : _r.Next(0, 800)));
            A("admin_misc", 0.6f, () =>
            {
                switch (_r.Next(16))
                {
                    case 0: s.AdminUnlockUpgrades(); break;
                    case 1: s.AdminLearnAllSkills(); break;
                    case 2: s.AdminDeliver(Pid(), Chance(0.3) ? ExtremeInt() : _r.Next(1, 60), Chance(0.2) ? ExtremeFloat() : 1f); break;
                    case 3: s.AdminArriveDeliveries(); break;
                    case 4: s.AdminSpawnOrders(_r.Next(-1, 6), Chance(0.3)); break;
                    case 5: s.AdminListAll(); break;
                    case 6: s.AdminClearDebt(); break;
                    case 7: s.AdminSetReputation(Chance(0.3) ? ExtremeFloat() : (float)_r.NextDouble() * 6f); break;
                    case 8: s.AdminMaxAwareness(); break;
                    case 9: s.AdminResetTikTok(); break;
                    case 10: s.AdminAddPackaging(Chance(0.3) ? -_r.Next(1, 50) : _r.Next(1, 50)); break;
                    case 11: s.AdminSkipMinutes(Chance(0.2) ? ExtremeFloat() : _r.Next(1, 200)); break;
                    case 12: s.AdminTriggerTrend(Pid(), (TrendPhase)_r.Next(-1, 6)); break;
                    case 13: s.AdminContractOffer(); break;
                    case 14: s.AdminUnlockAchievement(Chance(0.8) ? Pick(AchievementData.All).Id : "quatsch"); break;
                    default: if (Chance(0.05)) s.AdminUnlockEverything(); break;
                }
            });
            A("admin_switches", 0.3f, () =>
            {
                switch (_r.Next(4))
                {
                    case 0: s.AdminInfiniteMoney = Chance(0.3); break;
                    case 1: s.AdminNoBankrupt = Chance(0.3); break;
                    case 2: s.AdminTimeScale = Chance(0.2) ? ExtremeFloat() : (float)_r.NextDouble() * 10f; break;
                    default: break;
                }
                s.AdminTick();
            });
            A("difficulty", 0.15f, () =>
            {
                s.Difficulty = Pick(new[] { "entspannt", "normal", "hart" });
                s.NextDifficulty = Pick(new[] { "entspannt", "normal", "hart", "quatsch", null });
            });
            A("endless", 0.15f, () =>
            {
                // Test-Cheat: Umsatz springt (Endlos-Modus, Börsengang). Begrenzt, damit int nicht überläuft.
                if (s.TotalEarned < 1500000000) s.TotalEarned = Math.Min(1500000000, s.TotalEarned + Pick(new[] { 50000, 400000, 1500000, 9000000, 200000000 }));
                s.CheckGoals();
                s.CurrentObjective();
            });
            A("end_day", 0.25f, () => s.EndDayNow());
            // ---- Nur lesen (dürfen nie werfen) ----
            A("queries", 1f, () =>
            {
                s.CurrentObjective();
                s.StatusChips();
                s.StatsLines();
                s.Tickets();
                foreach (var o in s.Tickets())
                {
                    s.OrderTimeLeftText(o);
                    Sim.OrderNextPlace(o);
                    s.OrderTimeTone(o);
                }
                string pid = Pid();
                s.DemandLabel(pid ?? "");
                s.Trends.Forecast(ValidPid());
                s.ExpectedReturnRate(pid ?? "");
                s.QuickReorderCost(ValidPid());
                s.FestivalStatusText();
                s.TikTokBlocker();
                s.NextGarbagePickupText();
                s.Market.CreditScore();
                s.Market.NetWorth();
                s.Market.BiggestMover(10, out _);
                s.ShopDealsToday(ShopData.Electro);
                s.ShopCheaperThanWholesaleAny();
                foreach (var c in s.Contracts) s.ContractDeadlineText(c);
                foreach (var a in AchievementData.All) s.AchievementProgress(a);
                foreach (var u in GameData.Upgrades) s.UpgradeState(u.Id);
                foreach (var k in GameData.Skills) s.SkillState(k.Id);
                foreach (var p in GameData.Perks) s.PerkState(p.Id);
                s.EndlessGoal();
                s.Events.UnreadCount();
                s.WalkSpeedMult();
                s.CarrySpeedMult((ItemKind)_r.Next(0, 7));
            });
        }
    }
}
