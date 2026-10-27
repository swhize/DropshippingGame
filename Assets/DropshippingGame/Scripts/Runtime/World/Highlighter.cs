using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Legt beim Anvisieren das Highlight-Material über eine Gruppe von Renderern.</summary>
    public sealed class Highlighter
    {
        private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();
        private readonly List<Material[]> _original = new List<Material[]>();
        private bool _on;

        public void Collect(Transform root, Transform exclude = null)
        {
            _renderers.Clear();
            _original.Clear();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (exclude != null && r.transform.IsChildOf(exclude)) continue;
                if (r.GetComponent<TextMesh>() != null) continue;
                _renderers.Add(r);
                _original.Add(r.sharedMaterials);
            }
        }

        public void Set(bool on)
        {
            if (on == _on) return;
            _on = on;
            var hl = Mats.Highlight();
            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                if (on)
                {
                    var orig = _original[i];
                    var mats = new Material[orig.Length + 1];
                    orig.CopyTo(mats, 0);
                    mats[orig.Length] = hl;
                    r.sharedMaterials = mats;
                }
                else r.sharedMaterials = _original[i];
            }
        }
    }
}
