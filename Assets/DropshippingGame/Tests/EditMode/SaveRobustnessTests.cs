using System;
using System.Collections.Generic;
using System.IO;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>
    /// Release-Robustheit der Spielstände: „maximaler“ Spielzustand (alle Systeme aktiv) übersteht
    /// Speichern/Laden verlustfrei, kaputte/abgebrochene Dateien führen nie zu Absturz oder halbem
    /// Zustand, und vor jedem Überschreiben liegt eine lesbare Sicherung (.bak) bereit.
    /// </summary>
    public class SaveRobustnessTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ds_tests_robust_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
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

        // ---- Maximaler Spielzustand ---------------------------------------------------------------------
        /// <summary>Spiel mitten am Tag in der Lagerhalle, mit allem, was es gibt.</summary>
        private Sim Maximal(int seed = 11)
        {
            var sim = new Sim(seed) { SaveDir = _dir, Slot = 2 };
            sim.NewGame("skip");
            sim.InGame = true;
            sim.AdminUnlockEverything();
            sim.AdminListAll();
            foreach (var r in GameData.StaffRoles) sim.Hire(r.Id);
            foreach (var p in GameData.Products) sim.Stock[p.Id].Qty = 30;
            for (int i = 0; i < sim.Packaging.Length; i++) sim.Packaging[i] = 50;
            sim.Money = 250000;
            sim.Debt = 3000;
            sim.AdvanceMinutes(150f);

            // Bestellungen in allen Stufen
            string pid = GameData.Products[0].Id;
            sim.SpawnOrder(pid, true);
            sim.SpawnOrder(pid, false);
            sim.SpawnOrder(GameData.Products[1].Id, false);
            var item = sim.PickItem(pid);
            sim.WrapItem(item);
            TestUtil.FulfillOne(sim, GameData.Products[1].Id);

            // Lieferungen, Kisten, Retouren
            sim.AdminDeliver(pid, 20);
            sim.BuyBulk(0, 0, 0);
            sim.ScheduleReturn(new ItemData { Kind = ItemKind.Labeled, Product = pid, Price = 30, Quality = 1f, Customer = "Ben", City = "Kiel" }, 300f, "Test");
            sim.DockReturns.Add(new ItemData { Kind = ItemKind.Return, Product = pid, Price = 20, Quality = 0.7f, Note = "Gefällt nicht" });

            // Großaufträge, Trends, Wochenziele
            var offer = sim.GenerateContractOffer();
            var active = sim.GenerateContractOffer();
            if (active != null) sim.AcceptContract(active.Id);
            sim.Trends.Trigger(pid, TrendPhase.Rising, 1.5f);
            if (sim.Challenges.Count > 0) sim.Challenges[0].Progress = 3;

            // Werbung, Fake-Profile, TikTok-Videos
            sim.StartFakebookAd(pid, 0, 0, 1);
            sim.StartGugelAd(pid, 0, 40, 1);
            sim.CreateFakeProfile();
            var rating = TikTokScoring.Rate(new TikTokTake { Duration = 12f, VisibleFrac = 0.9f, Centered = 0.8f, DistanceFrac = 0.9f, SteadyFrac = 0.9f, Light = 0.8f, AngleDegrees = 90f, Hook = true, Actions = 2, Hype = 1.2f },
                TikTokFormats.Review, sim.TrendingTikTokFormat());
            sim.TikTokReadyAt = -1f;
            Assert.IsNull(sim.TikTokBlocker(), "TikTok möglich");
            Assert.IsNotNull(sim.PostTikTokVideo(rating, pid, TikTokFormats.Review));

            // Haustiere, Einkauf, Möbel, Fortschritt
            sim.PetFood = 10;
            sim.AdoptPet("hund");
            sim.AdoptPet("katze");
            sim.PetSetFollow("hund", true);
            var electro = ShopData.ItemsOf(ShopData.Electro);
            if (electro.Count > 0) sim.CartAdd(electro[0].Id);
            sim.SetFurniture("st:1:Pack:0", 12.5f, -19.75f, 90f);
            sim.SetFurniture("deco:pflanze:1", -1.25f, -15.5f, 45f);
            foreach (var perk in GameData.Perks) sim.BuyPerk(perk.Id);
            foreach (var eq in GameData.Equipment) sim.BuyEquipment(eq.Id);
            if (GameData.Disruptions.Length > 0) sim.StartDisruption(GameData.Disruptions[0].Id, 2);

            // Straßenfest läuft gerade
            sim.Stock[pid].Qty = 60;
            sim.AdminStartFestival(true);

            // Welt: abgelegte Gegenstände + Spieler mit Paket in der Hand
            sim.WorldItems.Add(new WorldItemSave { Item = ItemData.Crate(pid, 12, 0.9f), Pos = new V3(14f, 0.5f, -12f), RotY = 30f });
            sim.PlayerState = new PlayerSave { Pos = new V3(17f, 0f, -9f), RotY = 180f, Held = new ItemData { Kind = ItemKind.Package, Product = pid, Price = 25 } };
            sim.Events.AddMail("Test", "Hallo", "Text mit \"Anführungszeichen\" und Umlauten äöü ß", "info");
            sim.BrandName = "Hustle & Söhne \"GmbH\"";
            return sim;
        }

        /// <summary>Vergleichbare JSON-Form: ohne Zeitstempel, Boost-Reihenfolge egal (Werbe-Boosts werden beim Laden neu einsortiert).</summary>
        private static string Canon(Sim sim)
        {
            var d = sim.ToJson();
            d.Remove("saved_unix");
            if (d.TryGetValue("boosts", out object bo) && bo is List<object> boosts)
            {
                var keyed = new List<string>();
                foreach (var b in boosts) keyed.Add(Json.Write(b));
                keyed.Sort(StringComparer.Ordinal);
                d["boosts"] = keyed;
            }
            return Json.Write(d);
        }

        /// <summary>Erster Unterschied (Schlüssel + Ausschnitt), damit ein Fehlschlag sofort lesbar ist.</summary>
        private static string FirstDiff(string a, string b)
        {
            int i = 0;
            while (i < a.Length && i < b.Length && a[i] == b[i]) i++;
            int from = Math.Max(0, i - 120);
            return "\nvorher: …" + a.Substring(from, Math.Min(240, a.Length - from)) + "\nnachher: …" + b.Substring(from, Math.Min(240, b.Length - from));
        }

        [Test]
        public void MaximalState_RoundTripIsLossless()
        {
            var a = Maximal();
            Assert.AreEqual(1, a.LocationStage, "Lagerhalle");
            Assert.AreEqual(FestivalPhase.Live, a.Festival, "Fest läuft");
            Assert.Greater(a.Pets.Count, 0, "Haustiere");
            Assert.Greater(a.TikTokVideos.Count, 0, "TikTok-Videos");
            Assert.Greater(a.Staff.Count, 0, "Personal");
            string before = Canon(a);
            Assert.IsTrue(a.SaveGame(true), "gespeichert: " + a.LastSaveError);

            var b = new Sim(99) { SaveDir = _dir };
            Assert.IsTrue(b.LoadGame(2), "geladen: " + b.LastLoadError);
            Assert.IsFalse(b.LoadedFromBackup);
            // Zustand der Welt geht über Load an die Unity-Schicht, ToJson schreibt ihn aber wieder.
            string after = Canon(b);
            Assert.IsTrue(before == after, "jeder gespeicherte Wert kommt identisch zurück" + FirstDiff(before, after));

            // Und ein zweiter Durchlauf ist ebenfalls stabil (keine Drift durch Laden).
            var c = new Sim(5) { SaveDir = _dir };
            c.FromJson(Json.Parse(Json.Write(b.ToJson())) as Dictionary<string, object>);
            string again = Canon(c);
            Assert.IsTrue(after == again, "zweiter Durchlauf stabil" + FirstDiff(after, again));
        }

        [Test]
        public void MaximalState_KeepsKeySystems()
        {
            var a = Maximal();
            Assert.IsTrue(a.SaveGame(true));
            var b = new Sim(3) { SaveDir = _dir };
            Assert.IsTrue(b.LoadGame(2));
            Assert.AreEqual(a.Day, b.Day);
            Assert.AreEqual(a.TimeMinutes, b.TimeMinutes, 0.01f, "mitten am Tag");
            Assert.AreEqual(a.Festival, b.Festival, "Fest");
            Assert.AreEqual(a.FestivalTotal(), b.FestivalTotal(), "Fest-Bestand");
            Assert.AreEqual(a.Pets.Count, b.Pets.Count, "Haustiere");
            Assert.AreEqual(a.PetOf("hund").Follow, b.PetOf("hund").Follow);
            Assert.AreEqual(a.TikTokVideos.Count, b.TikTokVideos.Count, "Videos");
            Assert.AreEqual(a.Staff.Count, b.Staff.Count, "Personal");
            Assert.AreEqual(a.OrdersInWork.Count, b.OrdersInWork.Count, "Zettel in Arbeit");
            Assert.AreEqual(a.OrderQueue.Count, b.OrderQueue.Count, "wartende Zettel");
            Assert.AreEqual(a.Contracts.Count, b.Contracts.Count, "Großaufträge");
            Assert.AreEqual(a.LocationStage, b.LocationStage);
            Assert.AreEqual(a.BrandName, b.BrandName);
            Assert.IsNotNull(b.GetFurniture("st:1:Pack:0"));
            Assert.AreEqual(12.5f, b.GetFurniture("st:1:Pack:0").X, 0.001f);
            Assert.IsNotNull(b.PlayerState);
            Assert.AreEqual(ItemKind.Package, b.PlayerState.Held.Kind);
            Assert.AreEqual(1, b.WorldItems.Count);
        }

        // ---- Sicherung (.bak) & kaputte Dateien ---------------------------------------------------------
        [Test]
        public void Save_WritesBackupOfPreviousState()
        {
            var a = Maximal();
            a.Money = 1111;
            Assert.IsTrue(a.SaveGame(true));
            Assert.IsFalse(File.Exists(a.BackupPath(2)), "erster Stand: noch keine Sicherung");
            a.Money = 2222;
            Assert.IsTrue(a.SaveGame(true));
            Assert.IsTrue(File.Exists(a.BackupPath(2)), "Sicherung angelegt");
            var bak = Json.Parse(File.ReadAllText(a.BackupPath(2))) as Dictionary<string, object>;
            Assert.AreEqual(1111, J.I(bak, "money"), "Sicherung = vorheriger Stand");
            Assert.IsFalse(File.Exists(a.SavePath(2) + ".tmp"), "keine Zwischendatei übrig");
        }

        [Test]
        public void Load_CorruptMainFallsBackToBackup()
        {
            var a = Maximal();
            a.Money = 1111;
            a.SaveGame(true);
            a.Money = 2222;
            a.SaveGame(true);
            // Absturz beim Schreiben: Datei abgeschnitten
            string text = File.ReadAllText(a.SavePath(2));
            File.WriteAllText(a.SavePath(2), text.Substring(0, text.Length / 2));

            var b = new Sim(4) { SaveDir = _dir };
            Assert.IsTrue(b.HasSave(2));
            var sum = b.ReadSummary(2);
            Assert.IsNotNull(sum, "Menü zeigt den Slot weiter an");
            Assert.AreEqual(1111, sum.Money, "Zusammenfassung aus der Sicherung");
            Assert.IsTrue(b.LoadGame(2));
            Assert.IsTrue(b.LoadedFromBackup, "aus Sicherung geladen");
            Assert.AreEqual(1111, b.Money);
            Assert.AreEqual(2, b.Slot);
        }

        [Test]
        public void Save_CorruptMainNeverOverwritesGoodBackup()
        {
            var a = Maximal();
            a.Money = 1111;
            a.SaveGame(true);
            a.Money = 2222;
            a.SaveGame(true); // .bak = 1111
            File.WriteAllText(a.SavePath(2), "{ kaputt");

            var b = new Sim(4) { SaveDir = _dir };
            Assert.IsTrue(b.LoadGame(2));
            Assert.IsTrue(b.LoadedFromBackup);
            b.Money = 3333;
            Assert.IsTrue(b.SaveGame(true));
            var bak = Json.Parse(File.ReadAllText(b.BackupPath(2))) as Dictionary<string, object>;
            Assert.AreEqual(1111, J.I(bak, "money"), "die gute Sicherung bleibt erhalten");
            var c = new Sim(5) { SaveDir = _dir };
            Assert.IsTrue(c.LoadGame(2));
            Assert.IsFalse(c.LoadedFromBackup);
            Assert.AreEqual(3333, c.Money);
        }

        [Test]
        public void Load_BrokenWithoutBackupFailsCleanly()
        {
            var a = Maximal();
            a.SaveGame(true);
            File.WriteAllText(a.SavePath(2), "");
            var b = new Sim(4) { SaveDir = _dir };
            b.NewGame("skip");
            b.Money = 777;
            Assert.IsFalse(b.LoadGame(2), "leere Datei");
            File.WriteAllText(a.SavePath(2), "[1,2,3]");
            Assert.IsFalse(b.LoadGame(2), "kein Objekt");
            File.WriteAllText(a.SavePath(2), "{\"money\": ");
            Assert.IsFalse(b.LoadGame(2), "abgeschnitten");
            Assert.IsNull(b.ReadSummary(2));
            Assert.AreEqual(0, b.MostRecentSlot());
        }

        [Test]
        public void Load_RecoversInterruptedFirstSaveFromTmp()
        {
            var a = Maximal();
            a.Money = 4242;
            a.SaveGame(true);
            // Absturz zwischen „alte Datei löschen“ und „Zwischendatei umbenennen“
            File.Move(a.SavePath(2), a.SavePath(2) + ".tmp");
            var b = new Sim(4) { SaveDir = _dir };
            Assert.IsTrue(b.HasSave(2) || File.Exists(a.SavePath(2) + ".tmp"));
            Assert.IsTrue(b.LoadGame(2));
            Assert.AreEqual(4242, b.Money);
        }

        [Test]
        public void NewGameInUsedSlot_DoesNotKeepForeignBackup()
        {
            var a = Maximal();
            a.Money = 1111;
            a.SaveGame(true);
            a.Money = 2222;
            a.SaveGame(true); // altes Spiel: .bak = 1111
            var n = new Sim(8) { SaveDir = _dir, Slot = 2 };
            n.NewGame("skip");
            n.Money = 50;
            Assert.IsTrue(n.SaveGame(true));
            Assert.IsFalse(File.Exists(n.BackupPath(2)), "Sicherung des überschriebenen Spiels ist weg");
            n.Money = 60;
            n.SaveGame(true);
            var bak = Json.Parse(File.ReadAllText(n.BackupPath(2))) as Dictionary<string, object>;
            Assert.AreEqual(50, J.I(bak, "money"), "ab jetzt sichert das neue Spiel sich selbst");
        }

        [Test]
        public void DeleteSave_RemovesBackupToo()
        {
            var a = Maximal();
            a.SaveGame(true);
            a.SaveGame(true);
            Assert.IsTrue(File.Exists(a.BackupPath(2)));
            a.DeleteSave(2);
            Assert.IsFalse(a.HasSave(2));
            Assert.IsFalse(File.Exists(a.BackupPath(2)));
        }

        /// <summary>Zufällig beschädigte Spielstände: Laden wirft nie, und es bleibt nie ein halber Zustand.</summary>
        [Test]
        public void Load_FuzzedSavesNeverThrow()
        {
            var a = Maximal();
            a.SaveGame(true);
            string good = File.ReadAllText(a.SavePath(2));
            var rng = new System.Random(1234);
            var b = new Sim(4) { SaveDir = _dir };
            for (int i = 0; i < 120; i++)
            {
                var chars = good.ToCharArray();
                int mode = i % 3;
                string text;
                if (mode == 0) text = good.Substring(0, rng.Next(good.Length)); // abgeschnitten
                else
                {
                    // Zeichen vertauschen / Werte durch falsche Typen ersetzen
                    for (int k = 0; k < 8; k++)
                    {
                        int pos = rng.Next(chars.Length);
                        const string junk = "0123456789\"{}[],:tfn-.";
                        chars[pos] = junk[rng.Next(junk.Length)];
                    }
                    text = new string(chars);
                    if (mode == 2) text = text.Replace("\"qty\":", "\"qty\":\"x\",\"_\":").Replace(":[", ":{\"a\":[").Replace("],", "]},");
                }
                File.WriteAllText(a.SavePath(2), text);
                bool ok = false;
                Assert.DoesNotThrow(() => ok = b.LoadGame(2), "Durchlauf " + i);
                if (ok) Assert.DoesNotThrow(() => Json.Write(b.ToJson()), "geladener Zustand bleibt speicherbar (" + i + ")");
            }
        }

        [Test]
        public void ReadSummary_SurvivesWrongTypes()
        {
            Directory.CreateDirectory(_dir);
            var b = new Sim(4) { SaveDir = _dir };
            File.WriteAllText(b.SavePath(1), "{\"day\":\"drei\",\"money\":[1],\"brand_name\":5,\"saved_unix\":\"gestern\",\"reputation\":null}");
            SaveSummary sum = null;
            Assert.DoesNotThrow(() => sum = b.ReadSummary(1));
            Assert.IsNotNull(sum);
            Assert.AreEqual(1, sum.Day);
            Assert.DoesNotThrow(() => b.LoadGame(1));
        }
    }
}
