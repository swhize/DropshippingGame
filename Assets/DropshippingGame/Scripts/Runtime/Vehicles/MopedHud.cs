using DropshippingGame.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.Vehicles
{
    /// <summary>
    /// Tacho im HUD (unten mittig), solange man Moped fährt: km/h groß, Tempo-Balken, Inhalt der Heckbox
    /// und die Tasten (Absteigen, Hupe, Ansicht). Hängt sich in die HUD-Ebene der Oberfläche (eigene Inline-Stile).
    /// </summary>
    public sealed class MopedHud
    {
        private VisualElement _root, _bar, _fill, _cross;
        private Label _speed, _unit, _box, _keys;
        private int _lastKmh = -1;
        private string _lastBox, _lastKeys;
        private bool _shown;

        private bool Ensure()
        {
            if (_root != null && _root.panel != null) return true;
            var ui = Game.UI;
            if (ui == null || ui.Root == null) return false;
            var layer = ui.Root.Q<VisualElement>("hud");
            if (layer == null) return false;
            _root = new VisualElement { name = "moped-hud", pickingMode = PickingMode.Ignore };
            var s = _root.style;
            s.position = Position.Absolute;
            s.left = 0;
            s.right = 0;
            s.bottom = 22;
            s.alignItems = Align.Center;
            var card = new VisualElement { pickingMode = PickingMode.Ignore };
            card.AddToClassList("hud-card");
            card.style.flexDirection = FlexDirection.Column;
            card.style.alignItems = Align.Center;
            card.style.minWidth = 230;
            card.style.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 0.72f);
            card.style.borderTopLeftRadius = 16;
            card.style.borderTopRightRadius = 16;
            card.style.borderBottomLeftRadius = 16;
            card.style.borderBottomRightRadius = 16;
            card.style.paddingLeft = 16;
            card.style.paddingRight = 16;
            card.style.paddingTop = 8;
            card.style.paddingBottom = 10;
            _root.Add(card);
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexEnd;
            card.Add(row);
            _speed = Lbl(row, "0", 44, Theme.Text, true);
            _speed.AddToClassList("display");
            _unit = Lbl(row, " km/h", 16, Theme.Muted, false);
            _unit.style.marginBottom = 9;
            _bar = new VisualElement { pickingMode = PickingMode.Ignore };
            _bar.style.width = 196;
            _bar.style.height = 6;
            _bar.style.marginTop = 2;
            _bar.style.backgroundColor = new Color(1f, 1f, 1f, 0.12f);
            _bar.style.borderTopLeftRadius = 3;
            _bar.style.borderTopRightRadius = 3;
            _bar.style.borderBottomLeftRadius = 3;
            _bar.style.borderBottomRightRadius = 3;
            card.Add(_bar);
            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            _fill.style.height = 6;
            _fill.style.width = 0;
            _fill.style.backgroundColor = Theme.Accent;
            _fill.style.borderTopLeftRadius = 3;
            _fill.style.borderTopRightRadius = 3;
            _fill.style.borderBottomLeftRadius = 3;
            _fill.style.borderBottomRightRadius = 3;
            _bar.Add(_fill);
            _box = Lbl(card, "", 14, Theme.Ink2, false);
            _box.style.marginTop = 6;
            _keys = Lbl(card, "", 13, Theme.Muted, false);
            _keys.style.marginTop = 2;
            layer.Add(_root);
            _root.style.display = DisplayStyle.None;
            _cross = layer.Q<VisualElement>(className: "crosshair");
            return true;
        }

        private static Label Lbl(VisualElement parent, string text, int size, Color color, bool bold)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.style.fontSize = size;
            l.style.color = color;
            if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.marginLeft = 0;
            l.style.marginRight = 0;
            l.style.paddingLeft = 0;
            l.style.paddingRight = 0;
            parent.Add(l);
            return l;
        }

        /// <summary>Pro Bild. visible = Spieler fährt; kmh, max für den Balken; Box-Inhalt als Text.</summary>
        public void Tick(bool visible, float kmh, float maxKmh, string boxText)
        {
            if (!visible)
            {
                if (_shown && _root != null) _root.style.display = DisplayStyle.None;
                if (_shown && _cross != null) _cross.style.opacity = StyleKeyword.Null;
                _shown = false;
                return;
            }
            if (!Ensure()) return;
            if (!_shown)
            {
                _root.style.display = DisplayStyle.Flex;
                if (_cross != null) _cross.style.opacity = 0f; // kein Fadenkreuz beim Fahren
                _shown = true;
                _lastKmh = -1;
                _lastBox = null;
                _lastKeys = null;
            }
            int k = Mathf.RoundToInt(Mathf.Abs(kmh));
            if (k != _lastKmh)
            {
                _lastKmh = k;
                _speed.text = k.ToString();
                float f = Mathf.Clamp01(Mathf.Abs(kmh) / Mathf.Max(1f, maxKmh));
                _fill.style.width = 196f * f;
                _fill.style.backgroundColor = f > 0.97f ? Theme.Bad : Theme.Accent;
            }
            if (!string.Equals(boxText, _lastBox))
            {
                _lastBox = boxText;
                _box.text = boxText ?? "";
            }
            string keys = GameInput.KeyLabel("interact") + " Absteigen  ·  " + GameInput.KeyLabel("horn") + " Hupe  ·  " + GameInput.KeyLabel("vehcam") + " Ansicht";
            if (!string.Equals(keys, _lastKeys))
            {
                _lastKeys = keys;
                _keys.text = keys;
            }
        }

        public void Destroy()
        {
            if (_cross != null) _cross.style.opacity = StyleKeyword.Null;
            if (_root != null) _root.RemoveFromHierarchy();
            _root = null;
            _cross = null;
            _shown = false;
        }
    }
}
