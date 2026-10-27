using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Übersetzung zur Anzeigezeit: Das Spiel ist im Code deutsch geschrieben. Ist Englisch aktiv, wird
    /// jeder angezeigte Text über Wörterbücher (Deutsch → Englisch) übersetzt:
    /// <list type="number">
    /// <item>exakter Eintrag (z. B. "Feierabend" → "Closing time"),</item>
    /// <item>Vorlage mit Platzhaltern (z. B. "Lieferung angekommen: {0}× {1}." → "Delivery arrived: {0}× {1}.");
    /// die eingesetzten Teile werden selbst wieder übersetzt (Produktnamen usw.),</item>
    /// <item>mehrzeilige Texte zeilenweise, wenn der ganze Text nicht passt.</item>
    /// </list>
    /// Nicht gefundene Texte bleiben deutsch und werden gesammelt (<see cref="Missing"/>), damit man sie
    /// nachtragen kann. Ergebnisse werden zwischengespeichert. Die Tabellen liegen in Localization/En*.cs.
    /// </summary>
    public static class Lang
    {
        /// <summary>Aktuelle Sprache: "de" (Standard) oder "en".</summary>
        public static string Current { get; private set; } = "de";
        public static bool English => Current == "en";

        private static readonly Dictionary<string, string> Exact = new Dictionary<string, string>(4096);
        private static readonly List<Template> Templates = new List<Template>(1024);
        private static readonly HashSet<string> TemplateKeys = new HashSet<string>();
        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>(4096);
        private static readonly HashSet<string> MissingSet = new HashSet<string>();
        // Bereits englische Texte (Ziel-Werte der Tabellen) gelten nicht als fehlend.
        private static readonly HashSet<string> EnglishValues = new HashSet<string>();
        private static bool _loaded;
        private static bool _sorted;
        private static readonly object Gate = new object();

        private sealed class Template
        {
            public string De, En;
            public Regex Rx;
            public int LiteralLength;
        }

        /// <summary>Sprache setzen ("de"/"en"). Leert den Zwischenspeicher.</summary>
        public static void Set(string code)
        {
            string c = code == "en" ? "en" : "de";
            if (c == Current) return;
            Current = c;
            lock (Gate) Cache.Clear();
        }

        /// <summary>Texte, für die (noch) keine Übersetzung gefunden wurde (nur bei Englisch gesammelt).</summary>
        public static IEnumerable<string> Missing
        {
            get { lock (Gate) return new List<string>(MissingSet); }
        }

        /// <summary>Eintrag hinzufügen. Enthält <paramref name="de"/> Platzhalter {0}, {1} …, wird es eine Vorlage.</summary>
        public static void Add(string de, string en)
        {
            if (string.IsNullOrEmpty(de) || en == null) return;
            if (de.IndexOf('{') >= 0 && Regex.IsMatch(de, @"\{\d+\}"))
            {
                if (!TemplateKeys.Add(de)) return; // erster Eintrag gewinnt (siehe Ladereihenfolge)
                try
                {
                    var t = new Template { De = de, En = en, Rx = BuildRegex(de, out int lit), LiteralLength = lit };
                    Templates.Add(t);
                    _sorted = false;
                }
                catch (ArgumentException) { }
                return;
            }
            if (Exact.ContainsKey(de)) return; // erster Eintrag gewinnt (siehe Ladereihenfolge)
            Exact[de] = en;
            EnglishValues.Add(en);
        }

        /// <summary>Übersetzt <paramref name="de"/> in die aktuelle Sprache (Deutsch: unverändert).</summary>
        public static string T(string de)
        {
            if (!English || string.IsNullOrEmpty(de)) return de;
            EnsureLoaded();
            lock (Gate)
            {
                if (Cache.TryGetValue(de, out var hit)) return hit;
                string r = Translate(de, 0);
                if (Cache.Count > 20000) Cache.Clear();
                Cache[de] = r;
                return r;
            }
        }

        /// <summary>Text in Deutsch oder Englisch – für Stellen, die beide Fassungen direkt im Code haben.</summary>
        public static string Pick(string de, string en) => English && !string.IsNullOrEmpty(en) ? en : de;

        /// <summary>Anzahl geladener Einträge (für Tests/Diagnose).</summary>
        public static int EntryCount
        {
            get
            {
                EnsureLoaded();
                return Exact.Count + Templates.Count;
            }
        }

        private static string Translate(string s, int depth)
        {
            if (depth > 4 || string.IsNullOrEmpty(s)) return s;
            if (Exact.TryGetValue(s, out var ex)) return ex;

            // Leerraum am Rand erhalten ("  Text " → "  Text ")
            int a = 0, b = s.Length;
            while (a < b && char.IsWhiteSpace(s[a])) a++;
            while (b > a && char.IsWhiteSpace(s[b - 1])) b--;
            if (a > 0 || b < s.Length)
            {
                string core = s.Substring(a, b - a);
                if (core.Length == 0) return s;
                string tc = Translate(core, depth + 1);
                return ReferenceEquals(tc, core) || tc == core ? s : s.Substring(0, a) + tc + s.Substring(b);
            }

            if (!_sorted)
            {
                Templates.Sort((x, y) => y.LiteralLength.CompareTo(x.LiteralLength));
                _sorted = true;
            }
            foreach (var t in Templates)
            {
                var m = t.Rx.Match(s);
                if (!m.Success) continue;
                var args = new object[m.Groups.Count - 1];
                for (int i = 1; i < m.Groups.Count; i++)
                {
                    // Gruppen heißen g0, g1 … passend zu {0}, {1} …
                    args[i - 1] = "";
                }
                foreach (string name in t.Rx.GetGroupNames())
                {
                    if (!name.StartsWith("g", StringComparison.Ordinal)) continue;
                    if (!int.TryParse(name.Substring(1), out int idx) || idx < 0 || idx >= args.Length) continue;
                    args[idx] = Translate(m.Groups[name].Value, depth + 1);
                }
                try { return string.Format(t.En, args); }
                catch (FormatException) { return s; }
            }

            // Mehrzeilig: zeilenweise versuchen
            if (s.IndexOf('\n') >= 0)
            {
                var lines = s.Split('\n');
                bool any = false;
                var sb = new StringBuilder(s.Length + 16);
                for (int i = 0; i < lines.Length; i++)
                {
                    string tl = Translate(lines[i], depth + 1);
                    if (tl != lines[i]) any = true;
                    if (i > 0) sb.Append('\n');
                    sb.Append(tl);
                }
                if (any) return sb.ToString();
            }

            // Reine Zahlen/Symbole brauchen keine Übersetzung
            if (depth == 0 && HasLetters(s) && !EnglishValues.Contains(s)) MissingSet.Add(s);
            return s;
        }

        private static bool HasLetters(string s)
        {
            int letters = 0;
            foreach (char c in s)
                if (char.IsLetter(c) && ++letters >= 2) return true;
            return false;
        }

        private static Regex BuildRegex(string de, out int literalLength)
        {
            var sb = new StringBuilder("^");
            literalLength = 0;
            var seen = new HashSet<int>();
            int i = 0;
            while (i < de.Length)
            {
                if (de[i] == '{')
                {
                    int close = de.IndexOf('}', i);
                    if (close > i && int.TryParse(de.Substring(i + 1, close - i - 1), out int idx))
                    {
                        // Gleicher Platzhalter zweimal: Rückverweis
                        sb.Append(seen.Add(idx) ? "(?<g" + idx + ">.+?)" : @"\k<g" + idx + ">");
                        i = close + 1;
                        continue;
                    }
                }
                sb.Append(Regex.Escape(de[i].ToString()));
                literalLength++;
                i++;
            }
            sb.Append('$');
            return new Regex(sb.ToString(), RegexOptions.Singleline | RegexOptions.CultureInvariant);
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            lock (Gate)
            {
                if (_loaded) return;
                _loaded = true;
                // Feste Liste (keine Reflexion – IL2CPP-sicher). Jede Datei gehört einem Bereich.
                // Bei doppelten Schlüsseln gewinnt der ERSTE Eintrag: Nachträge/Korrekturen (EnSystems) zuerst,
                // dann die Spielinhalte als maßgebliche Begriffe, danach Meldungen, Oberfläche, Laptop, Welt.
                Safe(EnSystems.Register);
                Safe(EnContent.Register);
                Safe(EnEvents.Register);
                Safe(EnCore.Register);
                Safe(EnUi.Register);
                Safe(EnLaptop.Register);
                Safe(EnWorld.Register);
            }
        }

        private static void Safe(Action a)
        {
            try { a(); }
            catch (Exception) { }
        }

        /// <summary>Nur für Tests: alles neu laden.</summary>
        public static void ResetForTests()
        {
            lock (Gate)
            {
                Exact.Clear();
                Templates.Clear();
                Cache.Clear();
                MissingSet.Clear();
                EnglishValues.Clear();
                TemplateKeys.Clear();
                _loaded = false;
                _sorted = false;
                Current = "de";
            }
        }
    }
}
