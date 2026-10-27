using System.Collections;
using System.IO;
using DropshippingGame.Testing;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DropshippingGame.Tests.PlayMode
{
    /// <summary>
    /// Der In-Game-Selbsttest (Runtime/Testing/SelfTest.cs) als PlayMode-Tests für den Unity Test Runner
    /// (Window › General › Test Runner › PlayMode › „Run All“). Ein Test pro Schritt, in fester Reihenfolge;
    /// einzeln gestartete Tests holen „Neues Spiel“ automatisch nach. Der Test schreibt denselben Bericht wie
    /// der Selbsttest im Spiel (persistentDataPath/selftest/&lt;Zeitstempel&gt;/report.md) – der letzte Test
    /// gibt den Pfad aus. Fehler im Unity-Log lassen den jeweiligen Schritt (und damit den Test) fehlschlagen;
    /// das eingebaute „Unhandled log message“ des Test Runners ist dafür abgeschaltet.
    /// Hinweis: Der Compile-Check ohne Unity prüft diese Datei nur gegen eine Nachbildung des Test-Frameworks
    /// (tools/compilecheck/TestFrameworkStub.cs).
    /// </summary>
    [TestFixture]
    public sealed class SelbsttestPlayModeTests
    {
        private const int StepTimeoutMs = 240000;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            LogAssert.ignoreFailingMessages = true;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Falls „Run All“ abgebrochen wurde oder nur einzelne Tests liefen: Werte zurück, Bericht schreiben.
            SelfTest.AbortAll();
        }

        private static IEnumerator Run(string id)
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SelfTest.EnsureSession();
            var t = SelfTest.Current;
            Assert.IsNotNull(t, "Selbsttest konnte nicht vorbereitet werden.");
            yield return t.RunStep(id);
            var r = t.Result(id);
            Assert.IsNotNull(r, "Schritt „" + id + "“ hat kein Ergebnis geliefert.");
            TestContext.WriteLine(r.Summary());
            foreach (var n in r.Notes) TestContext.WriteLine("  · " + n);
            if (r.State == StepState.Skip) Assert.Ignore(r.Summary());
            Assert.AreNotEqual(StepState.Fail, r.State, r.Summary());
        }

        [UnityTest, Order(1), Timeout(StepTimeoutMs)]
        public IEnumerator S01_Hauptmenue() => Run("menu");

        [UnityTest, Order(2), Timeout(StepTimeoutMs)]
        public IEnumerator S02_NeuesSpiel() => Run("newgame");

        [UnityTest, Order(3), Timeout(StepTimeoutMs)]
        public IEnumerator S03_Kernschleife() => Run("coreloop");

        [UnityTest, Order(4), Timeout(StepTimeoutMs)]
        public IEnumerator S04_LaptopSpielbeginn() => Run("laptop");

        [UnityTest, Order(5), Timeout(StepTimeoutMs)]
        public IEnumerator S05_MenuesUndEinstellungen() => Run("menus");

        [UnityTest, Order(6), Timeout(StepTimeoutMs)]
        public IEnumerator S06_AllesFreischalten() => Run("unlock");

        [UnityTest, Order(7), Timeout(StepTimeoutMs)]
        public IEnumerator S07_LaptopKomplett() => Run("laptop_full");

        [UnityTest, Order(8), Timeout(StepTimeoutMs)]
        public IEnumerator S08_Handy() => Run("phone");

        [UnityTest, Order(9), Timeout(StepTimeoutMs)]
        public IEnumerator S09_Moped() => Run("moped");

        [UnityTest, Order(10), Timeout(StepTimeoutMs)]
        public IEnumerator S10_MoebelVerschieben() => Run("mover");

        [UnityTest, Order(11), Timeout(StepTimeoutMs)]
        public IEnumerator S11_EmoteRad() => Run("emote");

        [UnityTest, Order(12), Timeout(StepTimeoutMs)]
        public IEnumerator S12_TikTok() => Run("tiktok");

        [UnityTest, Order(13), Timeout(StepTimeoutMs)]
        public IEnumerator S13_Vogelperspektive() => Run("birdseye");

        [UnityTest, Order(14), Timeout(StepTimeoutMs)]
        public IEnumerator S14_Noclip() => Run("noclip");

        [UnityTest, Order(15), Timeout(StepTimeoutMs)]
        public IEnumerator S15_Stadtviertel() => Run("districts");

        [UnityTest, Order(16), Timeout(StepTimeoutMs)]
        public IEnumerator S16_Wetter() => Run("weather");

        [UnityTest, Order(17), Timeout(StepTimeoutMs)]
        public IEnumerator S17_Tageszeiten() => Run("daytime");

        [UnityTest, Order(18), Timeout(StepTimeoutMs)]
        public IEnumerator S18_SpracheEnglisch() => Run("language");

        [UnityTest, Order(19), Timeout(StepTimeoutMs)]
        public IEnumerator S19_SpeichernLaden() => Run("saveload");

        [UnityTest, Order(20), Timeout(StepTimeoutMs)]
        public IEnumerator S20_Feierabend() => Run("dayend");

        [UnityTest, Order(21), Timeout(StepTimeoutMs)]
        public IEnumerator S21_Leistung() => Run("perf");

        [UnityTest, Order(22), Timeout(StepTimeoutMs)]
        public IEnumerator S22_ZurueckInsHauptmenue() => Run("finish");

        [UnityTest, Order(23), Timeout(StepTimeoutMs)]
        public IEnumerator S23_BerichtSchreiben()
        {
            yield return SelfTest.EnsureSession();
            var t = SelfTest.Current;
            Assert.IsNotNull(t);
            yield return t.Finish(false, false);
            string md = Path.Combine(t.Folder, "report.md");
            TestContext.WriteLine(t.Report.Headline + " – " + t.Report.Subline);
            TestContext.WriteLine("Bericht: " + md);
            Assert.IsTrue(File.Exists(md), "report.md wurde nicht geschrieben: " + md);
        }
    }
}
