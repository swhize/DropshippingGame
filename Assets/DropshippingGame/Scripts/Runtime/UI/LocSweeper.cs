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
    /// </summary>
    public sealed class LocSweeper : MonoBehaviour
    {
        public VisualElement Root;
        private readonly ConditionalWeakTable<TextElement, string> _done = new ConditionalWeakTable<TextElement, string>();
        private int _errors;

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
            if (e is TextElement te && !(e.parent is TextInputBaseField<string>) && !(e is TextField))
            {
                string cur = te.text;
                if (!string.IsNullOrEmpty(cur))
                {
                    if (!_done.TryGetValue(te, out var last) || !string.Equals(last, cur, System.StringComparison.Ordinal))
                    {
                        string tr = Lang.T(cur);
                        if (!string.Equals(tr, cur, System.StringComparison.Ordinal)) te.text = tr;
                        _done.Remove(te);
                        _done.Add(te, tr);
                    }
                }
            }
            if (e is TextField) return;
            int n = e.hierarchy.childCount;
            for (int i = 0; i < n; i++) Sweep(e.hierarchy[i]);
        }
    }
}
