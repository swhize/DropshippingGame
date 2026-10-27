using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// „Feedback &amp; Bug melden“ (Pausemenü): sammelt Version, Systeminfos und die letzten Fehlermeldungen,
    /// kopiert sie in die Zwischenablage und öffnet den Ordner mit der Logdatei (Player.log).
    /// Es wird nichts automatisch verschickt – keine Telemetrie. Der Spieler entscheidet selbst, was er teilt.
    /// </summary>
    public static class BugReport
    {
        /// <summary>Wohin Spieler Berichte schicken. Vor dem Release ggf. um Discord/E-Mail ergänzen.</summary>
        public const string Contact = "Steam › Community-Hub des Spiels › Diskussionen › „Bugs & Feedback“";

        private const int MaxErrors = 8;
        private static readonly Queue<string> _errors = new Queue<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            _errors.Clear();
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            string first = message ?? "";
            if (first.Length > 240) first = first.Substring(0, 240) + " …";
            string where = "";
            if (!string.IsNullOrEmpty(stackTrace))
            {
                int nl = stackTrace.IndexOf('\n');
                where = (nl > 0 ? stackTrace.Substring(0, nl) : stackTrace).Trim();
                if (where.Length > 160) where = where.Substring(0, 160);
            }
            _errors.Enqueue(DateTime.Now.ToString("HH:mm:ss") + " " + type + ": " + first + (where.Length > 0 ? "  @ " + where : ""));
            while (_errors.Count > MaxErrors) _errors.Dequeue();
        }

        /// <summary>Pfad der Logdatei des Spiels (Player.log bzw. Editor.log), leer wenn unbekannt.</summary>
        public static string LogFile
        {
            get
            {
                try { return Application.consoleLogPath ?? ""; }
                catch { return ""; }
            }
        }

        /// <summary>Ordner mit der Logdatei; Rückfall: Spieldaten-Ordner.</summary>
        public static string LogFolder
        {
            get
            {
                string f = LogFile;
                string dir = string.IsNullOrEmpty(f) ? "" : Path.GetDirectoryName(f);
                return string.IsNullOrEmpty(dir) ? Application.persistentDataPath : dir;
            }
        }

        /// <summary>Text für die Zwischenablage: Version, System, Spielstand, letzte Fehler.</summary>
        public static string SystemInfoText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("== " + Application.productName + " – Fehlerbericht ==");
            sb.AppendLine("Version: " + BuildInfo.Short + (BuildInfo.Date.Length > 0 ? " vom " + BuildInfo.Date + " UTC" : "") +
                          (BuildInfo.Commit.Length > 0 ? " [" + BuildInfo.Commit + "]" : ""));
            sb.AppendLine("Plattform: " + Platform.Name + " · Unity " + Application.unityVersion + " · " + Application.platform);
            sb.AppendLine("Zeit: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC");
            try
            {
                sb.AppendLine("System: " + SystemInfo.operatingSystem);
                sb.AppendLine("CPU: " + SystemInfo.processorType + " (" + SystemInfo.processorCount + " Threads) · RAM " + SystemInfo.systemMemorySize + " MB");
                sb.AppendLine("GPU: " + SystemInfo.graphicsDeviceName + " · " + SystemInfo.graphicsDeviceVersion + " · VRAM " + SystemInfo.graphicsMemorySize + " MB");
                var r = Screen.currentResolution;
                sb.AppendLine("Bildschirm: " + Screen.width + "×" + Screen.height + " (" + Screen.fullScreenMode + "), Monitor " + r.width + "×" + r.height +
                              " @ " + Mathf.RoundToInt((float)r.refreshRateRatio.value) + " Hz");
                sb.AppendLine("Qualität: " + QualitySettings.names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)] +
                              " · FPS ~" + Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.smoothDeltaTime)) + " · Sprache " + Application.systemLanguage);
            }
            catch (Exception e)
            {
                sb.AppendLine("Systeminfo unvollständig: " + e.Message);
            }
            try
            {
                var sim = Game.Sim;
                if (sim != null)
                    sb.AppendLine("Spielstand: Slot " + sim.Slot + " · Tag " + sim.Day + " · Phase " + sim.StoryStage);
            }
            catch
            {
                // Spielstand-Infos sind optional
            }
            sb.AppendLine("Logdatei: " + (LogFile.Length > 0 ? LogFile : "(unbekannt)"));
            SessionLog.Flush();
            sb.AppendLine("Sitzungsprotokoll: " + (SessionLog.CurrentFile.Length > 0 ? SessionLog.CurrentFile : "(nicht verfügbar)") + " (Ordner logs, letzte 5 Sitzungen)");
            if (_errors.Count > 0)
            {
                sb.AppendLine("Letzte Fehler:");
                foreach (var e in _errors) sb.AppendLine("  " + e);
            }
            else sb.AppendLine("Letzte Fehler: keine");
            sb.AppendLine();
            sb.AppendLine("Was ist passiert? (bitte kurz beschreiben, was du gemacht hast und was du erwartet hast)");
            return sb.ToString();
        }

        /// <summary>Kopiert <see cref="SystemInfoText"/> in die Zwischenablage. Gibt den Text zurück.</summary>
        public static string CopyToClipboard()
        {
            string text = SystemInfoText();
            try { GUIUtility.systemCopyBuffer = text; }
            catch (Exception e) { Debug.LogWarning("[BugReport] Zwischenablage nicht verfügbar: " + e.Message); }
            return text;
        }

        /// <summary>Öffnet den Log-Ordner im Datei-Explorer. false, wenn das nicht ging.</summary>
        public static bool OpenLogFolder()
        {
            try
            {
                string dir = LogFolder;
                if (string.IsNullOrEmpty(dir)) return false;
                Directory.CreateDirectory(dir);
                Application.OpenURL(new Uri(dir + Path.DirectorySeparatorChar).AbsoluteUri);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BugReport] Log-Ordner konnte nicht geöffnet werden: " + e.Message);
                return false;
            }
        }
    }
}
