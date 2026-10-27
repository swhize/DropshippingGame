using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace DropshippingGame.Testing
{
    /// <summary>Zahlenformat für den Bericht (deutsch, notfalls neutral).</summary>
    internal static class ReportFmt
    {
        private static CultureInfo _de;

        public static CultureInfo De
        {
            get
            {
                if (_de != null) return _de;
                try { _de = CultureInfo.GetCultureInfo("de-DE"); }
                catch (Exception) { _de = CultureInfo.InvariantCulture; }
                return _de;
            }
        }
    }

    /// <summary>Ergebnis eines Prüfschritts.</summary>
    public enum StepState
    {
        Pass,
        Warn,
        Fail,
        Skip,
    }

    /// <summary>Bricht einen Prüfschritt mit einer verständlichen Meldung ab (ohne Stacktrace im Bericht).</summary>
    public sealed class SelfTestFailure : Exception
    {
        public SelfTestFailure(string message) : base(message)
        {
        }
    }

    /// <summary>Ein Schritt im Selbsttest-Bericht (wird auch als JSON gespeichert).</summary>
    [Serializable]
    public sealed class StepResult
    {
        private const int MaxEntries = 40;

        public string Id = "", Title = "", Status = "OK";
        public float Seconds;
        public List<string> Notes = new List<string>();
        /// <summary>Probleme, die der Test selbst festgestellt hat (FEHLER bzw. HINWEIS).</summary>
        public List<string> Problems = new List<string>();
        /// <summary>Fehler und Ausnahmen aus dem Unity-Log während des Schritts.</summary>
        public List<string> LogErrors = new List<string>();
        /// <summary>Warnungen aus dem Unity-Log während des Schritts.</summary>
        public List<string> LogWarnings = new List<string>();
        /// <summary>Hinweise, die das Spiel eingeblendet hat (Toasts), z. B. „bad: Nicht genug Geld“.</summary>
        public List<string> Toasts = new List<string>();
        public List<string> Screenshots = new List<string>();

        [NonSerialized] public StepState State = StepState.Pass;
        [NonSerialized] private bool _failed, _warned, _skipped;

        public StepResult(string id, string title)
        {
            Id = id ?? "";
            Title = title ?? "";
        }

        public void Note(string text) => Add(Notes, text);

        public void Warn(string text)
        {
            _warned = true;
            Add(Problems, "HINWEIS: " + text);
        }

        public void Fail(string text)
        {
            _failed = true;
            Add(Problems, "FEHLER: " + text);
        }

        public void Skip(string text)
        {
            _skipped = true;
            Add(Problems, "ÜBERSPRUNGEN: " + text);
        }

        public bool HasFailed => _failed || LogErrors.Count > 0;

        internal static void Add(List<string> list, string text)
        {
            if (list == null || string.IsNullOrEmpty(text)) return;
            if (list.Count < MaxEntries) list.Add(text);
            else if (list.Count == MaxEntries) list.Add("… (weitere Einträge gekürzt)");
        }

        /// <summary>Endgültigen Status aus Fehlern, Hinweisen und Log-Meldungen bestimmen.</summary>
        public void Finish()
        {
            if (_skipped) State = StepState.Skip;
            else if (_failed || LogErrors.Count > 0) State = StepState.Fail;
            else if (_warned || LogWarnings.Count > 0) State = StepState.Warn;
            else State = StepState.Pass;
            Status = StatusText(State);
            if (State == StepState.Fail && !_failed && LogErrors.Count > 0)
                Add(Problems, "FEHLER: " + LogErrors.Count + " Fehler/Ausnahme(n) im Log während dieses Schritts");
            if (State == StepState.Warn && !_warned && LogWarnings.Count > 0)
                Add(Problems, "HINWEIS: " + LogWarnings.Count + " Warnung(en) im Log während dieses Schritts");
        }

        public static string StatusText(StepState s)
        {
            switch (s)
            {
                case StepState.Warn: return "HINWEIS";
                case StepState.Fail: return "FEHLER";
                case StepState.Skip: return "ÜBERSPRUNGEN";
                default: return "OK";
            }
        }

        /// <summary>Kurzfassung für Testausgaben und das Fertig-Fenster.</summary>
        public string Summary()
        {
            var sb = new StringBuilder();
            sb.Append(Title).Append(": ").Append(Status);
            int n = 0;
            foreach (var p in Problems)
            {
                if (n++ >= 6) break;
                sb.Append("\n  ").Append(p);
            }
            n = 0;
            foreach (var e in LogErrors)
            {
                if (n++ >= 3) break;
                sb.Append("\n  Log: ").Append(FirstLine(e));
            }
            return sb.ToString();
        }

        internal static string FirstLine(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int i = s.IndexOf('\n');
            return i < 0 ? s : s.Substring(0, i).TrimEnd('\r');
        }
    }

    /// <summary>Messwerte der Leistungsprobe.</summary>
    [Serializable]
    public sealed class PerfResult
    {
        public string Where = "";
        public int Frames;
        public float Seconds, AvgFps, MinFps, Low1Fps;
        public int SlowFrames;
        public float AvgGcKb, MaxGcKb;
        public int GcSpikes, GcCollections;
        public bool GcMeasured;

        public string Text()
        {
            if (Frames <= 0) return "keine Messwerte";
            var c = ReportFmt.De;
            string s = "Ø " + AvgFps.ToString("0", c) + " FPS · schlechtester Frame " + MinFps.ToString("0", c) + " FPS · 1%-Low " + Low1Fps.ToString("0", c) +
                       " FPS · " + SlowFrames + " Ruckler (> 50 ms)";
            if (GcMeasured)
                s += " · GC Ø " + AvgGcKb.ToString("0.0", c) + " KB/Frame, max " + MaxGcKb.ToString("0", c) + " KB · " + GcSpikes + " Spitzen (> 256 KB)";
            s += " · " + GcCollections + " GC-Läufe";
            return s;
        }
    }

    /// <summary>Gesamter Bericht (report.json) – Kopf mit System-Infos, Zusammenfassung, Schritte.</summary>
    [Serializable]
    public sealed class SelfTestReport
    {
        public string Title = "Selbsttest – Dropshipping Simulator";
        public string Mode = "";
        public string Version = "", Unity = "", Os = "", Cpu = "", Gpu = "", Ram = "", Resolution = "", Quality = "", Language = "";
        public string Started = "", Finished = "", Folder = "";
        public float Seconds;
        public int Total, Ok, Warned, Failed, Skipped;
        public PerfResult Perf = new PerfResult();
        public List<string> Notes = new List<string>();
        public List<string> GeneralLog = new List<string>();
        public List<string> MissingTranslations = new List<string>();
        public List<StepResult> Steps = new List<StepResult>();

        public void Count()
        {
            Total = Steps.Count;
            Ok = Warned = Failed = Skipped = 0;
            foreach (var s in Steps)
            {
                switch (s.State)
                {
                    case StepState.Pass: Ok++; break;
                    case StepState.Warn: Ok++; Warned++; break;
                    case StepState.Fail: Failed++; break;
                    default: Skipped++; break;
                }
            }
        }

        /// <summary>„18 von 20 Schritten OK“.</summary>
        public string Headline => Ok + " von " + Total + " Schritten OK";

        public string Subline
        {
            get
            {
                var parts = new List<string>();
                if (Warned > 0) parts.Add(Warned + " davon mit Hinweisen");
                parts.Add(Failed + " fehlgeschlagen");
                if (Skipped > 0) parts.Add(Skipped + " übersprungen");
                parts.Add("Dauer " + Duration(Seconds));
                return string.Join(" · ", parts);
            }
        }

        public static string Duration(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00") + " min";
        }

        // =====================================================================================
        // report.md (deutsch, für den Inhaber)
        // =====================================================================================
        public string ToMarkdown()
        {
            var c = ReportFmt.De;
            var sb = new StringBuilder();
            sb.Append("# ").Append(Title).Append("\n\n");
            sb.Append("## Ergebnis: ").Append(Headline).Append("\n\n");
            sb.Append(Subline).Append("\n\n");
            if (Failed == 0) sb.Append("Alles Wichtige läuft. ");
            sb.Append("Bitte diesen ganzen Ordner (mit Unterordner `screenshots`) als ZIP an den Entwickler schicken.\n\n");
            sb.Append("| | |\n|---|---|\n");
            Row(sb, "Gestartet", Started + (string.IsNullOrEmpty(Mode) ? "" : " (" + Mode + ")"));
            Row(sb, "Version", Version);
            Row(sb, "Unity", Unity);
            Row(sb, "System", Os);
            Row(sb, "Prozessor / RAM", Cpu + " · " + Ram);
            Row(sb, "Grafikkarte", Gpu);
            Row(sb, "Auflösung / Qualität", Resolution + " · " + Quality);
            Row(sb, "Sprache", Language);
            Row(sb, "Leistung (Stadtspaziergang)", Perf != null ? Perf.Text() : "–");
            Row(sb, "Fehlende englische Texte", MissingTranslations.Count.ToString(c) + (MissingTranslations.Count > 0 ? " (Liste unten und in missing_en.txt)" : ""));
            sb.Append('\n');
            foreach (var n in Notes) sb.Append("- ").Append(n).Append('\n');
            if (Notes.Count > 0) sb.Append('\n');

            sb.Append("## Übersicht\n\n| # | Schritt | Ergebnis | Zeit | Wichtigster Hinweis |\n|---|---|---|---|---|\n");
            for (int i = 0; i < Steps.Count; i++)
            {
                var s = Steps[i];
                string hint = s.Problems.Count > 0 ? s.Problems[0] : (s.LogErrors.Count > 0 ? StepResult.FirstLine(s.LogErrors[0]) : "");
                sb.Append("| ").Append(i + 1).Append(" | ").Append(Cell(s.Title)).Append(" | **").Append(s.Status).Append("** | ")
                  .Append(s.Seconds.ToString("0.0", c)).Append(" s | ").Append(Cell(hint)).Append(" |\n");
            }
            sb.Append('\n');

            sb.Append("## Details\n\n");
            for (int i = 0; i < Steps.Count; i++)
            {
                var s = Steps[i];
                sb.Append("### ").Append(i + 1).Append(". ").Append(s.Title).Append(" – ").Append(s.Status).Append("\n\n");
                sb.Append("Dauer: ").Append(s.Seconds.ToString("0.0", c)).Append(" s\n\n");
                List("Probleme", s.Problems, sb, false);
                List("Ablauf", s.Notes, sb, false);
                List("Fehler im Log", s.LogErrors, sb, true);
                List("Warnungen im Log", s.LogWarnings, sb, true);
                List("Hinweise im Spiel", s.Toasts, sb, false);
                if (s.Screenshots.Count > 0)
                {
                    sb.Append("**Screenshots:**\n\n");
                    foreach (var shot in s.Screenshots) sb.Append("- [").Append(Path.GetFileName(shot)).Append("](").Append(shot).Append(")\n");
                    sb.Append('\n');
                }
            }

            if (GeneralLog.Count > 0)
            {
                sb.Append("## Log-Meldungen außerhalb der Schritte\n\n```\n");
                foreach (var l in GeneralLog) sb.Append(l).Append('\n');
                sb.Append("```\n\n");
            }
            if (MissingTranslations.Count > 0)
            {
                sb.Append("## Fehlende englische Übersetzungen (").Append(MissingTranslations.Count).Append(")\n\n```\n");
                int n = 0;
                foreach (var m in MissingTranslations)
                {
                    if (n++ >= 300)
                    {
                        sb.Append("… (vollständig in missing_en.txt)\n");
                        break;
                    }
                    sb.Append(m.Replace("\r", "").Replace("\n", "\\n")).Append('\n');
                }
                sb.Append("```\n\n");
            }
            sb.Append("---\nDateien in diesem Ordner: `report.md` (dieser Bericht), `report.json` (dasselbe für den Entwickler), ")
              .Append("`selftest_log.txt` (alle Meldungen), `session.log` (Sitzungsprotokoll), `missing_en.txt`, `screenshots/`, `saves/` (Test-Spielstand).\n");
            return sb.ToString();
        }

        private static void Row(StringBuilder sb, string k, string v) => sb.Append("| ").Append(k).Append(" | ").Append(Cell(v)).Append(" |\n");

        private static string Cell(string s) => string.IsNullOrEmpty(s) ? "" : StepResult.FirstLine(s).Replace("|", "/");

        private static void List(string title, List<string> items, StringBuilder sb, bool code)
        {
            if (items == null || items.Count == 0) return;
            sb.Append("**").Append(title).Append(":**\n\n");
            if (code)
            {
                sb.Append("```\n");
                foreach (var it in items) sb.Append(it.TrimEnd()).Append("\n\n");
                sb.Append("```\n\n");
                return;
            }
            foreach (var it in items) sb.Append("- ").Append(it.Replace("\n", " ")).Append('\n');
            sb.Append('\n');
        }
    }
}
