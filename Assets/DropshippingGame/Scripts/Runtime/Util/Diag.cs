using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Abgesicherte Fehlermeldungen für Code, der jeden Frame läuft: Die erste Ausnahme je Schlüssel wird
    /// vollständig geloggt, Wiederholungen nur noch gezählt und höchstens alle paar Sekunden zusammengefasst
    /// gemeldet. So bleibt das Spiel bedienbar und Log/Konsole laufen nicht voll.
    /// <code>try { Tick(); } catch (Exception e) { SafeLog.Warn("hud.tick", e); }</code>
    /// </summary>
    public static class SafeLog
    {
        /// <summary>Mindestabstand zwischen zwei Meldungen desselben Schlüssels (Sekunden, Echtzeit).</summary>
        public const float Interval = 10f;

        private sealed class Gate
        {
            public float Next;
            public int Suppressed;
            public int Total;
        }

        private static readonly Dictionary<string, Gate> Gates = new Dictionary<string, Gate>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Gates.Clear();

        /// <summary>Anzahl aller über SafeLog gemeldeten Ausnahmen in dieser Sitzung (für das Perf-Overlay).</summary>
        public static int TotalReported { get; private set; }

        /// <summary>Meldet eine Ausnahme rate-begrenzt als Warnung (erste mit Stacktrace).</summary>
        public static void Warn(string key, Exception e)
        {
            if (e == null) return;
            TotalReported++;
            key = key ?? "?";
            if (!Gates.TryGetValue(key, out var g))
            {
                g = new Gate();
                Gates[key] = g;
            }
            g.Total++;
            float now = Time.realtimeSinceStartup;
            if (g.Total > 1 && now < g.Next)
            {
                g.Suppressed++;
                return;
            }
            g.Next = now + Interval;
            string extra = g.Suppressed > 0 ? " (" + g.Suppressed + " gleiche Meldungen unterdrückt)" : "";
            g.Suppressed = 0;
            try
            {
                if (g.Total == 1) Debug.LogWarning("[" + key + "] " + e);
                else Debug.LogWarning("[" + key + "] " + e.GetType().Name + ": " + e.Message + extra);
            }
            catch (Exception)
            {
                // Logging darf nie selbst das Spiel stören.
            }
        }

        /// <summary>Rate-begrenzte Warnung ohne Ausnahme (z. B. fehlendes Objekt).</summary>
        public static void Warn(string key, string message)
        {
            key = key ?? "?";
            if (!Gates.TryGetValue(key, out var g))
            {
                g = new Gate();
                Gates[key] = g;
            }
            g.Total++;
            float now = Time.realtimeSinceStartup;
            if (g.Total > 1 && now < g.Next)
            {
                g.Suppressed++;
                return;
            }
            g.Next = now + Interval;
            string extra = g.Suppressed > 0 ? " (" + g.Suppressed + "× unterdrückt)" : "";
            g.Suppressed = 0;
            Debug.LogWarning("[" + key + "] " + message + extra);
        }

        /// <summary>Führt eine Aktion abgesichert aus. false, wenn sie eine Ausnahme warf.</summary>
        public static bool Try(string key, Action a)
        {
            if (a == null) return true;
            try
            {
                a();
                return true;
            }
            catch (Exception e)
            {
                Warn(key, e);
                return false;
            }
        }
    }

    /// <summary>
    /// Sitzungs-Logdatei für Fehlerberichte: schreibt Warnungen, Fehler und Ausnahmen (plus Start-Infos zu
    /// System und Spielversion) nach <c>persistentDataPath/logs/session_*.log</c> und behält die letzten 5
    /// Dateien. Gleiche Meldungen werden zusammengefasst, die Datei ist auf ein paar MB begrenzt.
    /// Spieler finden den Ordner über das Pausemenü/Hilfe oder <see cref="Folder"/>.
    /// </summary>
    public static class SessionLog
    {
        private const int KeepFiles = 5;
        private const long MaxBytes = 4 * 1024 * 1024;
        private const int MaxRepeats = 20;

        private static readonly object Lock = new object();
        private static readonly Dictionary<string, int> Repeats = new Dictionary<string, int>();
        private static StreamWriter _writer;
        private static long _written;
        private static bool _capped;

        /// <summary>Ordner der Logdateien (leer, falls nicht verfügbar).</summary>
        public static string Folder { get; private set; } = "";

        /// <summary>Pfad der aktuellen Logdatei (leer, falls nicht verfügbar).</summary>
        public static string CurrentFile { get; private set; } = "";

        /// <summary>Fehler + Ausnahmen in dieser Sitzung (für das Perf-Overlay).</summary>
        public static int ErrorCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Close();
            ErrorCount = 0;
            lock (Lock) Repeats.Clear();
            try
            {
                Folder = Path.Combine(Application.persistentDataPath, "logs");
                Directory.CreateDirectory(Folder);
                Prune(Folder, KeepFiles - 1);
                CurrentFile = Path.Combine(Folder, "session_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log");
                _writer = new StreamWriter(new FileStream(CurrentFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false));
                _written = 0;
                _capped = false;
                Line("Dropshipping Simulator – Sitzungsprotokoll");
                Line("Start: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " · Version " + Application.version + " · Unity " + Application.unityVersion);
                Line("System: " + SystemInfo.operatingSystem + " · CPU " + SystemInfo.processorType + " (" + SystemInfo.processorCount + " Kerne) · RAM " +
                     SystemInfo.systemMemorySize + " MB");
                Line("Grafik: " + SystemInfo.graphicsDeviceName + " · " + SystemInfo.graphicsDeviceType + " · VRAM " + SystemInfo.graphicsMemorySize + " MB · " +
                     Screen.width + "x" + Screen.height);
                Line("--------------------------------------------------------------------------");
                _writer.Flush();
                Application.logMessageReceivedThreaded -= OnLog;
                Application.logMessageReceivedThreaded += OnLog;
                Application.quitting -= Close;
                Application.quitting += Close;
            }
            catch (Exception e)
            {
                Close();
                Debug.LogWarning("Sitzungsprotokoll nicht verfügbar: " + e.Message);
            }
        }

        /// <summary>Löscht die ältesten session_*.log, bis höchstens <paramref name="keep"/> übrig sind.</summary>
        private static void Prune(string dir, int keep)
        {
            try
            {
                var files = new List<FileInfo>(new DirectoryInfo(dir).GetFiles("session_*.log"));
                files.Sort((a, b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));
                for (int i = Math.Max(0, keep); i < files.Count; i++)
                {
                    try { files[i].Delete(); }
                    catch (Exception) { /* Datei evtl. noch offen – nächstes Mal */ }
                }
            }
            catch (Exception)
            {
                // Aufräumen ist nicht kritisch.
            }
        }

        /// <summary>Freier Eintrag (z. B. „Spiel geladen: Slot 2“). Threadsicher.</summary>
        public static void Note(string text)
        {
            lock (Lock) Line(DateTime.Now.ToString("HH:mm:ss") + " [Info] " + text);
            Flush();
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log) return;
            bool error = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
            lock (Lock)
            {
                if (_writer == null) return;
                if (error) ErrorCount++;
                string key = type + ":" + (condition ?? "");
                if (key.Length > 300) key = key.Substring(0, 300);
                Repeats.TryGetValue(key, out int n);
                Repeats[key] = n + 1;
                if (n >= MaxRepeats)
                {
                    if ((n + 1) % 500 == 0) Line(DateTime.Now.ToString("HH:mm:ss") + " [" + type + "] (" + (n + 1) + "× bisher) " + Trim(condition, 200));
                    return;
                }
                Line(DateTime.Now.ToString("HH:mm:ss") + " [" + type + "] " + Trim(condition, 4000));
                if (error && !string.IsNullOrEmpty(stackTrace)) Line(Trim(stackTrace, 6000));
                if (n + 1 == MaxRepeats) Line("   (diese Meldung wird ab jetzt nur noch gezählt)");
                if (error) _writer.Flush();
            }
        }

        private static string Trim(string s, int max) => string.IsNullOrEmpty(s) ? "" : (s.Length > max ? s.Substring(0, max) + " …" : s);

        private static void Line(string s)
        {
            if (_writer == null || _capped) return;
            try
            {
                _writer.WriteLine(s);
                _written += s.Length + 2;
                if (_written > MaxBytes)
                {
                    _capped = true;
                    _writer.WriteLine("(Protokoll-Limit erreicht – weitere Meldungen werden nicht mehr geschrieben)");
                    _writer.Flush();
                }
            }
            catch (Exception)
            {
                _capped = true;
            }
        }

        public static void Flush()
        {
            lock (Lock)
            {
                try { _writer?.Flush(); }
                catch (Exception) { /* egal */ }
            }
        }

        private static void Close()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            lock (Lock)
            {
                try
                {
                    if (_writer != null)
                    {
                        _writer.WriteLine("Ende: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " · Fehler: " + ErrorCount);
                        _writer.Flush();
                        _writer.Dispose();
                    }
                }
                catch (Exception)
                {
                    // egal
                }
                _writer = null;
            }
        }
    }
}
