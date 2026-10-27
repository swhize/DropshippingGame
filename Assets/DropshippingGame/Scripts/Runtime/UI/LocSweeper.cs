using System.Runtime.CompilerServices;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Übersetzt die gesamte Oberfläche zur Anzeigezeit: Ist Englisch aktiv, läuft er jeden Frame (nach allen
    /// Updates, vor dem Zeichnen der UI) über den sichtbaren UI-Baum und ersetzt deutsche Texte in Labels und
    /// Buttons über <see cref="Lang.T"/>. Bereits übersetzte Texte werden sich gemerkt, damit nichts doppelt
    /// oder dauernd übersetzt wird. Eingabefelder (Markenname usw.) bleiben unangetastet.
    /// Der deutsche Originaltext wird mitgemerkt: Wechselt die Sprache zurück auf Deutsch, stellt
    /// <see cref="Revert"/> alle noch englisch angezeigten Texte wieder her.
    /// Elemente mit der Klasse <see cref="SkipClass"/> sind schon übersetzt (z. B. Schreibmaschinen-Text) und
    /// werden übersprungen.
    /// </summary>
    public sealed class LocSweeper : MonoBehaviour
    {
        /// <summary>USS-Klasse für Texte, die der Code selbst übersetzt (Loc.Tr) – der Sweeper fasst sie nicht an.</summary>
        public const string SkipClass = "no-loc";

        private sealed class Seen
        {
            public string Raw, Shown;
        }

        public VisualElement Root;
        private readonly ConditionalWeakTable<TextElement, Seen> _done = new ConditionalWeakTable<TextElement, Seen>();
        private int _errors;
        private bool _hooked;

        private void OnEnable()
        {
            if (_hooked) return;
            _hooked = true;
            Loc.Changed += OnLanguageChanged;
        }

        private void OnDisable()
        {
            if (!_hooked) return;
            _hooked = false;
            Loc.Changed -= OnLanguageChanged;
        }

        private void OnLanguageChanged(string code)
        {
            if (Lang.English || Root == null) return;
            try { Revert(Root); }
            catch (System.Exception e)
            {
                if (_errors++ < 3) Debug.LogWarning("[LocSweeper] " + e.Message);
            }
        }

        private void LateUpdate()
        {
            if (!Lang.English || Root == null) return;
            try { Sweep(Root); }
            catch (System.Exception e)
            {
                if (_errors++ < 3) Debug.LogWarning("[LocSweeper] " + e.Message);
            }
        }

        private void Sweep(VisualElement e)
        {
            if (e.resolvedStyle.display == DisplayStyle.None) return;
            if (e is TextElement te && !(e.parent is TextInputBaseField<string>) && !(e is TextField) && !te.ClassListContains(SkipClass))
            {
                string cur = te.text;
                if (!string.IsNullOrEmpty(cur))
                {
                    if (!_done.TryGetValue(te, out var seen) || !string.Equals(seen.Shown, cur, System.StringComparison.Ordinal))
                    {
                        string tr = Lang.T(cur);
                        if (!string.Equals(tr, cur, System.StringComparison.Ordinal)) te.text = tr;
                        if (seen == null)
                        {
                            seen = new Seen();
                            _done.Add(te, seen);
                        }
                        seen.Raw = cur;
                        seen.Shown = tr;
                    }
                }
            }
            if (e is TextField) return;
            int n = e.hierarchy.childCount;
            for (int i = 0; i < n; i++) Sweep(e.hierarchy[i]);
        }

        /// <summary>Zurück auf Deutsch: alle übersetzten Texte (auch ausgeblendete) auf das Original setzen.</summary>
        private void Revert(VisualElement e)
        {
            if (e is TextElement te && _done.TryGetValue(te, out var seen))
            {
                if (string.Equals(te.text, seen.Shown, System.StringComparison.Ordinal) && seen.Raw != null) te.text = seen.Raw;
                _done.Remove(te);
            }
            if (e is TextField) return;
            int n = e.hierarchy.childCount;
            for (int i = 0; i < n; i++) Revert(e.hierarchy[i]);
        }
    }
}
