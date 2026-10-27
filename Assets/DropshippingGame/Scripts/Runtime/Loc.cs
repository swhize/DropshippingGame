using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Übersetzung in der Unity-Schicht. Der Code ist deutsch; übersetzt wird zur Anzeigezeit über die Wörterbücher
    /// (<see cref="DropshippingGame.Core.Lang"/>): UI automatisch (LocSweeper), 3D-Text automatisch (Label3D).
    /// Für Sonderfälle: <see cref="Tr"/> (einzelner Text), <see cref="F"/> (übersetzte Format-Vorlage),
    /// <see cref="Pre"/>/<see cref="Show"/> (schon übersetzte Interaktions-Hinweise). Oberflächen, die sich bei einem
    /// Sprachwechsel neu aufbauen wollen, hängen sich an <see cref="Changed"/> (leitet <see cref="Settings.LanguageChanged"/> weiter).
    /// </summary>
    public static class Loc
    {
        public static string Language => Settings.Language;
        public static bool English => Settings.Language == "en";

        /// <summary>Text in der aktuellen Sprache (ohne englische Fassung: Deutsch).</summary>
        public static string T(string de, string en = null) => English && !string.IsNullOrEmpty(en) ? en : de;

        /// <summary>Deutschen Text über die Wörterbücher übersetzen (siehe <see cref="DropshippingGame.Core.Lang"/>).</summary>
        public static string Tr(string de) => DropshippingGame.Core.Lang.T(de);

        /// <summary>
        /// Wie string.Format, aber die deutsche Vorlage wird vorher übersetzt (sie muss als Vorlage in den
        /// Wörterbüchern stehen, z. B. A("In den Korb: {0} · noch {1}", "Into the basket: {0} · {1} left")).
        /// Die Werte übersetzt der Aufrufer selbst. Auf Deutsch identisch mit string.Format(de, args).
        /// Platzhalter nie direkt aneinanderhängen ("{0}{1}"), sonst kann die Vorlage nicht erkannt werden.
        /// </summary>
        public static string F(string de, params object[] args)
        {
            string fmt = English ? Tr(de) : de;
            try { return string.Format(fmt, args); }
            catch (FormatException) { return string.Format(de, args); }
        }

        /// <summary>Markiert einen schon übersetzten Text (nur Englisch), damit ihn <see cref="Show"/> nicht nochmal übersetzt.</summary>
        public const char PreMark = '\u0001';

        /// <summary>Schon übersetzten Text (z. B. aus <see cref="F"/>) für Interaktions-Hinweise markieren.</summary>
        public static string Pre(string translated) => English && !string.IsNullOrEmpty(translated) ? PreMark + translated : translated;

        /// <summary>Text zur Anzeige: markierte (<see cref="Pre"/>) Texte unverändert ohne Markierung, sonst übersetzt.</summary>
        public static string Show(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            if (s[0] == PreMark) return s.Substring(1);
            return Tr(s);
        }

        /// <summary>Entfernt die <see cref="Pre"/>-Markierung (für Stellen, die den Text weiterverarbeiten).</summary>
        public static string Plain(string s) => !string.IsNullOrEmpty(s) && s[0] == PreMark ? s.Substring(1) : s;

        /// <summary>Anzahl der bisher gesammelten, noch nicht übersetzten Texte (nur bei Englisch gesammelt).</summary>
        public static int MissingCount
        {
            get
            {
                int n = 0;
                foreach (var _ in DropshippingGame.Core.Lang.Missing) n++;
                return n;
            }
        }

        /// <summary>
        /// Schreibt alle fehlenden Übersetzungen (Lang.Missing) sortiert nach persistentDataPath/missing_en.txt –
        /// eine Zeile pro Text, Zeilenumbrüche als \n. Für Tester und das Nachtragen in Localization/En*.cs.
        /// </summary>
        public static string SaveMissing()
        {
            var list = new List<string>(DropshippingGame.Core.Lang.Missing);
            list.Sort(StringComparer.Ordinal);
            string path = Path.Combine(Application.persistentDataPath, "missing_en.txt");
            try
            {
                using (var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(false)))
                {
                    w.WriteLine("# Fehlende englische Übersetzungen (" + list.Count + ") – " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    w.WriteLine("# Format für Localization/En*.cs: A(\"Deutsch\", \"English\"); – Zahlen/Namen als {0}, {1} …");
                    foreach (var s in list) w.WriteLine(s.Replace("\r", "").Replace("\n", "\\n"));
                }
                Game.Notify(list.Count + " fehlende Übersetzungen gespeichert: " + (Settings.StreamerMode ? "missing_en.txt" : path), "info");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Loc] missing_en.txt: " + e.Message);
                Game.Notify("Speichern fehlgeschlagen: " + e.Message, "bad");
            }
            return path;
        }

        /// <summary>Sprache gewechselt (neuer Sprachcode).</summary>
        public static event Action<string> Changed
        {
            add => Settings.LanguageChanged += value;
            remove => Settings.LanguageChanged -= value;
        }
    }
}
