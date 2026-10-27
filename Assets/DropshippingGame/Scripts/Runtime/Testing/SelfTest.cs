using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DropshippingGame.Core;
using DropshippingGame.UI;
using Unity.Profiling;
using UnityEngine;

namespace DropshippingGame.Testing
{
    /// <summary>
    /// Automatischer Selbsttest im laufenden Spiel („ein Klick, ein Bericht“). Spielt das echte Spiel wie ein
    /// Spieler durch (neues Spiel auf einem eigenen Test-Spielstand, Kernschleife über die echten Stationen,
    /// alle Laptop-/Handy-Seiten, Menüs, Moped, Sondermodi, Stadtviertel, Wetter, Tageszeiten, Englisch,
    /// Speichern/Laden, Feierabend, Leistung) und schreibt nach
    /// <c>persistentDataPath/selftest/&lt;Zeitstempel&gt;/</c>: <c>report.md</c> (deutsch), <c>report.json</c>,
    /// Screenshots, alle Log-Meldungen und eine Kopie des Sitzungsprotokolls.
    ///
    /// Start: Admin-Panel (F10) › Allgemein › „Selbsttest starten“, im Hauptmenü F10, im Editor
    /// Menü Dropshipping › Selbsttest (Play Mode) oder als PlayMode-Tests im Test Runner (Tests/PlayMode).
    /// Jeder Schritt läuft abgesichert (try/catch um jeden Teilschritt, Zeitlimit) – der Test darf das Spiel
    /// nie zum Absturz bringen und endet immer im Hauptmenü. Die Spielstände des Spielers werden nie
    /// angefasst: Der Test schreibt nur in den eigenen Ordner (<see cref="Sim.SaveDir"/> wird umgebogen
    /// und erst im Hauptmenü zurückgestellt). Schritte: <c>SelfTest.Steps.cs</c>, Oberfläche: <see cref="SelfTestUI"/>.
    /// </summary>
    public sealed partial class SelfTest : MonoBehaviour
    {
        private sealed class StepDef
        {
            public string Id, Title;
            public float Timeout;
            public bool NeedsGame;
            public Func<SelfTest, IEnumerator> Body;
        }

        /// <summary>Werte vor dem Test – werden am Ende wiederhergestellt.</summary>
        private sealed class Saved
        {
            public bool Captured;
            public string SaveDir;
            public int Slot = 1;
            public string Difficulty;
            public bool NoBankrupt;
            public float TimeScale = 1f;
            public string Language = "de";
            public int WeatherForce = -1;
            public bool ShowUpcoming;
            public bool RunInBackground;
        }

        public static SelfTest Current { get; private set; }

        /// <summary>true, solange ein Selbsttest läuft.</summary>
        public static bool Running => Current != null && Current._running;

        /// <summary>Berichtsordner dieses Laufs.</summary>
        public string Folder { get; private set; } = "";
        public SelfTestReport Report { get; private set; } = new SelfTestReport();
        public bool Finished { get; private set; }

        /// <summary>Index (1-basiert) und Titel des laufenden Schritts – für die Einblendung.</summary>
        public int StepIndex { get; private set; }
        public string StepTitle { get; private set; } = "";

        private bool _running, _begun, _restored;

        // Spielstand-Ordner des Spielers, der noch zurückgestellt werden muss (statisch, damit ein neuer Lauf
        // ihn übernimmt, falls der alte nicht mehr ins Hauptmenü kam).
        private static bool _dirPending;
        private static string _dirOrig;
        private static int _slotOrig = 1;
        private static string _diffOrig;
        private StepResult _step;
        private readonly Saved _saved = new Saved();
        private readonly List<string> _log = new List<string>();
        private float _t0;
        private int _shot;
        private bool _autoCloseModals = true;
        private ProfilerRecorder _gcRec;
        private PerfResult _lastPerf = new PerfResult();
        private Sim _hookedSim;
        private string _lastToast = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            _dirPending = false;
            ResetInput();
            GameInput.TestActive = false;
        }

        private static float Now => Time.realtimeSinceStartup;

        // =====================================================================================
        // Start
        // =====================================================================================

        /// <summary>Kompletter Lauf mit Einblendung, Fertig-Fenster und Öffnen des Berichtsordners.</summary>
        public static SelfTest Launch()
        {
            if (Running)
            {
                Game.Notify("Der Selbsttest läuft bereits.", "info");
                return Current;
            }
            var t = Create("Spiel");
            t.StartCoroutine(t.RunAll(true, true));
            return t;
        }

        /// <summary>Neuen Testlauf anlegen (ohne zu starten). Ein alter, fertiger Lauf wird entfernt.</summary>
        public static SelfTest Create(string mode)
        {
            if (Current != null)
            {
                if (Current._running) return Current;
                Destroy(Current.gameObject);
            }
            var go = new GameObject("Selbsttest");
            DontDestroyOnLoad(go);
            var t = go.AddComponent<SelfTest>();
            t.Report.Mode = mode ?? "";
            Current = t;
            return t;
        }

        /// <summary>Für die PlayMode-Tests: sorgt dafür, dass ein vorbereiteter Lauf existiert.</summary>
        public static IEnumerator EnsureSession()
        {
            if (Current == null || Current.Finished) Create("PlayMode-Tests (Test Runner)");
            if (!Current._begun) yield return Current.Begin();
        }

        /// <summary>Für die PlayMode-Tests: Lauf sofort beenden (Werte zurück, Bericht schreiben).</summary>
        public static void AbortAll()
        {
            var t = Current;
            if (t == null) return;
            try
            {
                if (!t.Finished)
                {
                    t._running = false;
                    t.RestoreNow();
                    t.WriteReport();
                    t.Finished = true;
                    SelfTestUI.RemoveOverlay();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Selbsttest] Abbruch: " + e.Message);
            }
        }

        /// <summary>Alle Schritte nacheinander, danach Bericht.</summary>
        public IEnumerator RunAll(bool openFolder, bool showPanel)
        {
            yield return Begin();
            foreach (var def in Steps) yield return RunStep(def.Id);
            yield return Finish(openFolder, showPanel);
        }

        /// <summary>Vorbereitung: Ordner, Log-Mitschnitt, Originalwerte merken, Test-Spielstand-Ordner setzen.</summary>
        public IEnumerator Begin()
        {
            if (_begun) yield break;
            _begun = true;
            _running = true;
            _t0 = Now;
            try
            {
                string root = Path.Combine(Application.persistentDataPath, "selftest");
                Folder = Path.Combine(root, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
                Directory.CreateDirectory(Path.Combine(Folder, "screenshots"));
                PruneOldReports(root, 10);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Selbsttest] Berichtsordner: " + e.Message);
            }
            Report.Folder = Folder;
            Report.Started = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            Line("Selbsttest gestartet: " + Report.Started + " · Ordner " + Folder);

            // Spiel bereit? (GameRoot startet automatisch; im Test Runner ggf. selbst anlegen.)
            float end = Now + 20f;
            while ((Game.Root == null || Game.UI == null || Game.Sim == null) && Now < end) yield return null;
            if (Game.Root == null)
            {
                Line("GameRoot fehlte – wird angelegt.");
                try { new GameObject("Dropshipping Game").AddComponent<GameRoot>(); }
                catch (Exception e) { Line("GameRoot konnte nicht angelegt werden: " + e.Message); }
                end = Now + 15f;
                while ((Game.Root == null || Game.UI == null || Game.Sim == null) && Now < end) yield return null;
            }
            end = Now + 10f;
            while (Game.UI != null && Game.UI.Fader != null && Game.UI.Fader.Busy && Now < end) yield return null;
            try
            {
                CaptureOriginals();
            }
            catch (Exception e)
            {
                Report.Notes.Add("Vorbereitung unvollständig: " + e.Message);
                Debug.LogException(e);
            }
            try
            {
                _gcRec = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            }
            catch (Exception)
            {
                // Ohne Profiler-Zähler wird nur die GC-Anzahl gemessen.
            }
            FillSystemInfo();
            GameInput.TestActive = true;
            Application.runInBackground = true;
            SelfTestUI.ShowOverlay();
        }

        private void CaptureOriginals()
        {
            var sim = Game.Sim;
            _saved.Language = Settings.Language;
            _saved.WeatherForce = Weather.ForceKind;
            _saved.ShowUpcoming = LaptopView.ShowUpcoming;
            _saved.RunInBackground = Application.runInBackground;
            if (sim != null)
            {
                // Hängt noch eine Rückstellung aus einem abgebrochenen Lauf, gelten deren Werte als Original.
                _saved.SaveDir = _dirPending ? _dirOrig : sim.SaveDir;
                _saved.Slot = _dirPending ? _slotOrig : sim.Slot;
                _saved.Difficulty = _dirPending ? _diffOrig : sim.NextDifficulty;
                _dirPending = false;
                _saved.NoBankrupt = sim.AdminNoBankrupt;
                _saved.TimeScale = sim.AdminTimeScale;
                // Laufendes Spiel des Spielers vorher in SEINEN Ordner sichern (wie beim Gang ins Hauptmenü).
                if (sim.InGame && sim.StoryStage == "business" && !sim.DayOver && !string.IsNullOrEmpty(sim.SaveDir) && sim.SaveDir == _saved.SaveDir)
                {
                    bool ok = false;
                    try { ok = sim.SaveGame(true); }
                    catch (Exception e) { Debug.LogException(e); }
                    Report.Notes.Add(ok
                        ? "Dein laufendes Spiel wurde vor dem Test gespeichert (Spielstand " + sim.Slot + ")."
                        : "Dein laufendes Spiel konnte vor dem Test nicht gespeichert werden.");
                }
                else if (sim.InGame) Report.Notes.Add("Beim Start lief ein Spiel, das nicht gespeichert werden konnte (Imbiss-Schicht oder Feierabend).");
                string dir = Path.Combine(Folder, "saves");
                Directory.CreateDirectory(dir);
                sim.SaveDir = dir;
                sim.NextDifficulty = GameData.DefaultDifficulty;
                HookSim(sim);
            }
            _saved.Captured = true;
            Report.Notes.Add("Test-Spielstände liegen nur im Berichtsordner (saves/) – deine eigenen Spielstände bleiben unberührt.");
        }

        private void HookSim(Sim sim)
        {
            if (_hookedSim == sim) return;
            if (_hookedSim != null) _hookedSim.Toast -= OnToast;
            _hookedSim = sim;
            if (sim != null) sim.Toast += OnToast;
        }

        private void FillSystemInfo()
        {
            var r = Report;
            r.Version = GameVersion.Long;
            r.Unity = Application.unityVersion + (Application.isEditor ? " (Editor)" : "");
            r.Os = SystemInfo.operatingSystem;
            r.Cpu = SystemInfo.processorType + " (" + SystemInfo.processorCount + " Kerne)";
            r.Ram = SystemInfo.systemMemorySize + " MB RAM";
            r.Gpu = SystemInfo.graphicsDeviceName + " · " + SystemInfo.graphicsDeviceType + " · " + SystemInfo.graphicsMemorySize + " MB";
            r.Resolution = Screen.width + "×" + Screen.height;
            int q = Mathf.Clamp(Settings.Quality, 0, Settings.QualityNames.Length - 1);
            r.Quality = "Grafik " + Settings.QualityNames[q] + " · Renderskalierung " + Settings.RenderScale.ToString("0.00") + (Settings.VSync ? " · VSync an" : "");
            r.Language = _saved.Language;
        }

        // =====================================================================================
        // Schritte ausführen
        // =====================================================================================

        /// <summary>Alle Schritt-IDs in Ausführungsreihenfolge.</summary>
        public static IEnumerable<string> StepIds
        {
            get
            {
                foreach (var s in Steps) yield return s.Id;
            }
        }

        private static StepDef Find(string id)
        {
            foreach (var s in Steps)
                if (s.Id == id)
                    return s;
            return null;
        }

        private static int IndexOf(string id)
        {
            for (int i = 0; i < Steps.Length; i++)
                if (Steps[i].Id == id)
                    return i;
            return -1;
        }

        /// <summary>Ergebnis eines Schritts (null, wenn er noch nicht lief).</summary>
        public StepResult Result(string id) => Report.Steps.Find(s => s.Id == id);

        private static bool GameReady
        {
            get
            {
                var sim = Game.Sim;
                return sim != null && sim.InGame && Game.Player != null && Game.World != null && sim.StoryStage == "business";
            }
        }

        /// <summary>Einen Schritt abgesichert ausführen (mit Zeitlimit, Log-Mitschnitt, Aufräumen).</summary>
        public IEnumerator RunStep(string id)
        {
            if (!_begun) yield return Begin();
            var def = Find(id);
            var r = new StepResult(id, def != null ? def.Title : id);
            if (def == null)
            {
                r.Fail("Unbekannter Schritt „" + id + "“.");
                r.Finish();
                Report.Steps.Add(r);
                yield break;
            }
            // Einzeln gestartet (Test Runner): Voraussetzung „Neues Spiel“ automatisch nachholen.
            if (def.NeedsGame && !GameReady && Result("newgame") == null) yield return RunStep("newgame");

            Report.Steps.RemoveAll(s => s.Id == id);
            Report.Steps.Add(r);
            _step = r;
            _lastToast = "";
            StepIndex = IndexOf(id) + 1;
            StepTitle = def.Title;
            SelfTestUI.UpdateOverlay(StepIndex, Steps.Length, def.Title);
            Line("── Schritt " + StepIndex + ": " + def.Title);
            float start = Now;
            if (def.NeedsGame && !GameReady)
                r.Skip("Kein laufendes Spiel – der Schritt „Neues Spiel“ ist fehlgeschlagen.");
            else
            {
                _autoCloseModals = true;
                ClearBlockers();
                yield return Drive(def.Body(this), def.Timeout, r);
                ResetInput();
                _autoCloseModals = true;
                ClearBlockers();
            }
            r.Seconds = Now - start;
            r.Finish();
            _step = null;
            Line("   → " + r.Status + " (" + r.Seconds.ToString("0.0") + " s)" + (r.Problems.Count > 0 ? " · " + r.Problems[0] : ""));
        }

        /// <summary>
        /// Führt einen Schritt-Iterator selbst aus (verschachtelte IEnumerators, Zeitlimit, try/catch um jedes
        /// MoveNext) – so kann keine Ausnahme und kein Hänger den Testlauf oder das Spiel beenden.
        /// </summary>
        private IEnumerator Drive(IEnumerator body, float timeout, StepResult r)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(body);
            float end = Now + timeout;
            while (stack.Count > 0)
            {
                if (Now > end)
                {
                    r.Fail("Zeitlimit überschritten – Schritt nach " + timeout.ToString("0") + " s abgebrochen.");
                    DisposeAll(stack);
                    yield break;
                }
                var top = stack.Peek();
                object cur = null;
                bool moved;
                try
                {
                    moved = top.MoveNext();
                    if (moved) cur = top.Current;
                }
                catch (SelfTestFailure f)
                {
                    r.Fail(f.Message);
                    DisposeAll(stack);
                    yield break;
                }
                catch (Exception e)
                {
                    r.Fail("Ausnahme: " + e.GetType().Name + ": " + e.Message);
                    StepResult.Add(r.LogErrors, e.ToString());
                    DisposeAll(stack);
                    yield break;
                }
                if (!moved)
                {
                    stack.Pop();
                    continue;
                }
                if (cur is IEnumerator nested)
                {
                    stack.Push(nested);
                    continue;
                }
                yield return cur;
            }
        }

        private static void DisposeAll(Stack<IEnumerator> stack)
        {
            // finally-Blöcke der Schritte ausführen (z. B. Sprache zurückstellen).
            while (stack.Count > 0)
            {
                var it = stack.Pop();
                try { (it as IDisposable)?.Dispose(); }
                catch (Exception e) { Debug.LogWarning("[Selbsttest] Aufräumen: " + e.Message); }
            }
        }

        /// <summary>Ende: Werte zurückstellen, Bericht schreiben, optional Ordner öffnen und Fertig-Fenster zeigen.</summary>
        public IEnumerator Finish(bool openFolder, bool showPanel)
        {
            if (Finished) yield break;
            _running = false;
            _step = null;
            RestoreNow();
            WriteReport();
            Finished = true;
            SelfTestUI.RemoveOverlay();
            yield return null;
            if (openFolder) OpenFolder();
            if (showPanel) SelfTestUI.ShowSummary(this);
        }

        public void OpenFolder()
        {
            try
            {
                if (!string.IsNullOrEmpty(Folder) && Directory.Exists(Folder)) Application.OpenURL("file:///" + Folder.Replace('\\', '/'));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Selbsttest] Ordner öffnen: " + e.Message);
            }
        }

        public void OpenReport()
        {
            try
            {
                string md = Path.Combine(Folder, "report.md");
                if (File.Exists(md)) Application.OpenURL("file:///" + md.Replace('\\', '/'));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Selbsttest] Bericht öffnen: " + e.Message);
            }
        }

        // =====================================================================================
        // Aufräumen & Wiederherstellen
        // =====================================================================================

        /// <summary>Simulierte Eingaben aus.</summary>
        private static void ResetInput()
        {
            GameInput.TestMove = null;
            GameInput.TestPressed.Clear();
            GameInput.TestHeld.Clear();
        }

        /// <summary>Alles schließen, was den nächsten Schritt blockieren könnte (Fenster, Laptop, Modi).</summary>
        private void ClearBlockers()
        {
            try
            {
                var ui = Game.UI;
                var sim = Game.Sim;
                var p = Game.Player;
                SelfTestUI.ClosePanels();
                if (ui != null)
                {
                    if (ui.Admin != null && ui.Admin.IsOpen) ui.Admin.Close();
                    if (ui.Pause != null && ui.Pause.IsOpen) ui.Pause.Close();
                    if (ui.BigModal != null && ui.BigModal.IsOpen) ui.BigModal.Close();
                    if (ui.Modal != null && ui.Modal.IsOpen) ui.Modal.Close();
                    if (ui.Phone != null && ui.Phone.IsOpen) ui.Phone.Close(true);
                }
                if (sim != null && sim.PcOpen) sim.ClosePc();
                if (BirdsEyeView.Active) BirdsEyeView.ForceOff();
                if (p != null)
                {
                    if (p.Riding != null) p.Riding.ForceExit();
                    if (p.Mover.Active) p.Mover.Cancel();
                    if (p.Recorder.Active) p.Recorder.Cancel(true);
                    if (PlayerController.Noclip) p.SetNoclip(false);
                }
                ResetInput();
            }
            catch (Exception e)
            {
                Line("Aufräumen: " + e.Message);
            }
        }

        /// <summary>Alle geänderten Einstellungen zurückstellen. Mehrfach aufrufbar.</summary>
        public void RestoreNow()
        {
            if (_restored) return;
            _restored = true;
            ResetInput();
            GameInput.TestActive = false;
            try
            {
                if (_saved.Captured)
                {
                    if (Settings.Language != _saved.Language) Settings.SetLanguage(_saved.Language);
                    Weather.ForceKind = _saved.WeatherForce;
                    LaptopView.ShowUpcoming = _saved.ShowUpcoming;
                    Application.runInBackground = _saved.RunInBackground;
                    var sim = Game.Sim;
                    if (sim != null)
                    {
                        sim.AdminTimeScale = _saved.TimeScale;
                        sim.AdminNoBankrupt = _saved.NoBankrupt;
                    }
                    if (PlayerController.Noclip && Game.Player != null) Game.Player.SetNoclip(false);
                    BirdsEyeView.ForceOff();
                    _dirOrig = _saved.SaveDir;
                    _slotOrig = _saved.Slot;
                    _diffOrig = _saved.Difficulty;
                    _dirPending = true;
                    TryRestoreSaveDir();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Selbsttest] Wiederherstellen: " + e.Message);
            }
            try
            {
                if (_gcRec.Valid) _gcRec.Dispose();
            }
            catch (Exception)
            {
                // egal
            }
            if (_hookedSim != null) _hookedSim.Toast -= OnToast;
            _hookedSim = null;
            Application.logMessageReceived -= OnLog;
        }

        /// <summary>
        /// Spielstand-Ordner erst zurückstellen, wenn kein Test-Spiel mehr läuft – sonst würde das Auto-Speichern
        /// (oder Speichern beim Beenden) den Test-Spielstand in die echten Spielstände schreiben.
        /// </summary>
        private static void TryRestoreSaveDir()
        {
            if (!_dirPending) return;
            var sim = Game.Sim;
            if (sim == null)
            {
                _dirPending = false;
                return;
            }
            if (sim.InGame) return;
            sim.SaveDir = _dirOrig;
            sim.Slot = Mathf.Clamp(_slotOrig, 1, Sim.SaveSlots);
            if (!string.IsNullOrEmpty(_diffOrig)) sim.NextDifficulty = _diffOrig;
            _dirPending = false;
        }

        /// <summary>true, solange der Spielstand-Ordner noch auf den Test-Ordner zeigt.</summary>
        public static bool SaveDirRedirected => _dirPending || Running;

        private void Update()
        {
            if (_dirPending) TryRestoreSaveDir();
            if (!_running) return;
            try
            {
                // Zufällige Fenster (Ereignisse, Meilensteine …) schließen, damit sie den Ablauf nicht blockieren.
                var ui = Game.UI;
                if (_autoCloseModals && ui != null)
                {
                    if (ui.BigModal != null && ui.BigModal.IsOpen) CloseStray(ui.BigModal);
                    if (ui.Modal != null && ui.Modal.IsOpen) CloseStray(ui.Modal);
                }
            }
            catch (Exception e)
            {
                Line("Wächter: " + e.Message);
            }
        }

        private void CloseStray(ModalView m)
        {
            string what = m.Current;
            m.Close();
            if (_step != null) _step.Note("Fenster „" + (string.IsNullOrEmpty(what) ? "?" : what) + "“ ging auf und wurde automatisch geschlossen.");
        }

        private void OnDestroy()
        {
            if (!Finished)
            {
                _running = false;
                RestoreNow();
            }
            if (Current == this) Current = null;
            SelfTestUI.RemoveOverlay();
        }

        private void OnApplicationQuit()
        {
            // Beim Beenden mitten im Test: Bericht trotzdem schreiben. Spielstand-Ordner bleibt beim Test-Ordner,
            // solange das Test-Spiel läuft (GameRoot speichert beim Beenden).
            if (Finished || !_begun) return;
            _running = false;
            Report.Notes.Add("Das Spiel wurde während des Tests beendet.");
            RestoreNow();
            WriteReport();
            Finished = true;
        }

        // =====================================================================================
        // Log & Hinweise mitschneiden
        // =====================================================================================
        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log) return;
            try
            {
                bool error = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
                string text = condition ?? "";
                if (error && !string.IsNullOrEmpty(stackTrace)) text += "\n" + TrimStack(stackTrace, 8);
                _log.Add("[" + (Now - _t0).ToString("0.0") + " s] " + type + (_step != null ? " (" + _step.Id + ")" : "") + ": " + text);
                if (_step != null)
                {
                    if (error) StepResult.Add(_step.LogErrors, text);
                    else StepResult.Add(_step.LogWarnings, condition);
                }
                else if (Report.GeneralLog.Count < 100) Report.GeneralLog.Add(type + ": " + StepResult.FirstLine(condition));
            }
            catch (Exception)
            {
                // Mitschnitt darf nie stören.
            }
        }

        private static string TrimStack(string stack, int lines)
        {
            var parts = stack.Split('\n');
            var sb = new StringBuilder();
            for (int i = 0; i < parts.Length && i < lines; i++)
            {
                string l = parts[i].TrimEnd('\r');
                if (l.Length == 0) continue;
                sb.Append("    ").Append(l).Append('\n');
            }
            return sb.ToString().TrimEnd();
        }

        private void OnToast(string text, string kind)
        {
            _lastToast = text ?? "";
            if (_step != null) StepResult.Add(_step.Toasts, (kind ?? "info") + ": " + text);
        }

        private void Line(string text)
        {
            _log.Add("[" + (Now - _t0).ToString("0.0") + " s] " + text);
        }

        // =====================================================================================
        // Bericht schreiben
        // =====================================================================================
        private void WriteReport()
        {
            var r = Report;
            try
            {
                r.Finished = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                r.Seconds = Now - _t0;
                r.Count();
                if (string.IsNullOrEmpty(Folder)) return;
                Directory.CreateDirectory(Folder);
                Write("report.md", r.ToMarkdown());
                Write("report.json", JsonUtility.ToJson(r, true));
                var sb = new StringBuilder();
                foreach (var l in _log) sb.Append(l).Append('\n');
                Write("selftest_log.txt", sb.ToString());
                var miss = new StringBuilder();
                miss.Append("# Fehlende englische Übersetzungen aus dem Selbsttest (").Append(r.MissingTranslations.Count).Append(")\n");
                foreach (var m in r.MissingTranslations) miss.Append(m.Replace("\r", "").Replace("\n", "\\n")).Append('\n');
                Write("missing_en.txt", miss.ToString());
                CopySessionLog();
                Debug.Log("[Selbsttest] " + r.Headline + " – Bericht: " + Path.Combine(Folder, "report.md"));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Selbsttest] Bericht konnte nicht geschrieben werden: " + e.Message);
            }
        }

        private void Write(string name, string content)
        {
            try
            {
                File.WriteAllText(Path.Combine(Folder, name), content ?? "", new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Selbsttest] " + name + ": " + e.Message);
            }
        }

        private void CopySessionLog()
        {
            try
            {
                SessionLog.Flush();
                string src = SessionLog.CurrentFile;
                if (string.IsNullOrEmpty(src) || !File.Exists(src)) return;
                // Die Logdatei ist noch zum Schreiben offen: mit FileShare.ReadWrite lesen statt File.Copy.
                using (var input = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var output = new FileStream(Path.Combine(Folder, "session.log"), FileMode.Create, FileAccess.Write))
                    input.CopyTo(output);
            }
            catch (Exception e)
            {
                Line("Sitzungsprotokoll nicht kopiert: " + e.Message);
            }
        }

        /// <summary>Alte Berichtsordner aufräumen (nur Ordner im Format JJJJ-MM-TT_hh-mm-ss).</summary>
        private static void PruneOldReports(string root, int keep)
        {
            try
            {
                if (!Directory.Exists(root)) return;
                var dirs = new List<DirectoryInfo>();
                foreach (var d in new DirectoryInfo(root).GetDirectories())
                    if (d.Name.Length == 19 && d.Name[4] == '-' && d.Name[10] == '_')
                        dirs.Add(d);
                dirs.Sort((a, b) => string.CompareOrdinal(b.Name, a.Name));
                for (int i = keep; i < dirs.Count; i++)
                {
                    try { dirs[i].Delete(true); }
                    catch (Exception) { /* nicht kritisch */ }
                }
            }
            catch (Exception)
            {
                // nicht kritisch
            }
        }
    }
}
