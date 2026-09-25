using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Button-Beschreibung für Fenster.</summary>
    public struct ModalButton
    {
        public string Text, Variant, Icon;
        public Action OnClick;
        /// <summary>false = Fenster bleibt nach dem Klick offen.</summary>
        public bool KeepOpen;

        public ModalButton(string text, Action onClick = null, string variant = "", string icon = null)
        {
            Text = text;
            OnClick = onClick;
            Variant = variant;
            Icon = icon;
            KeepOpen = false;
        }
    }

    /// <summary>
    /// Zentriertes Fenster (Karte) mit Titel, Inhalt und Buttons. Genutzt für Ereignis-
    /// Entscheidungen, Tagesabschluss, Bestätigungen, Hilfe, Pitch-Minispiel und das Spielende.
    /// Pausiert optional das Spiel, solange es offen ist.
    /// </summary>
    public sealed class ModalView
    {
        private readonly VisualElement _layer;
        private readonly string _key;
        private VisualElement _card, _body, _buttons, _scroll;
        private Label _title, _sub;
        private bool _pause;
        private Action _onClose;

        public bool IsOpen { get; private set; }
        public string Current { get; private set; } = "";

        public ModalView(VisualElement layer, string key)
        {
            _layer = layer;
            _key = key;
            Build();
            UIX.Show(_layer, false);
        }

        private void Build()
        {
            var dim = UIX.Div(_layer, "layer", "dim");
            _card = UIX.Col(dim, 0f, "modal-card");
            _title = UIX.Text(_card, "", "modal-title");
            _sub = UIX.Text(_card, "", "modal-sub");
            var sv = new ScrollView(ScrollViewMode.Vertical);
            sv.AddToClassList("modal-body");
            sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _card.Add(sv);
            _scroll = sv;
            _body = UIX.Col(sv.contentContainer, 9f);
            _buttons = UIX.Row(_card, 10f, "modal-buttons");
        }

        /// <summary>Öffnet das Fenster und gibt den Inhaltsbereich zurück.</summary>
        public VisualElement Open(string title, string subtitle, float width, IList<ModalButton> buttons, bool pause = true, string current = "", Action onClose = null)
        {
            if (IsOpen) CloseSilently();
            _body.Clear();
            _buttons.Clear();
            _title.text = title;
            _sub.text = subtitle ?? "";
            UIX.Show(_sub, !string.IsNullOrEmpty(subtitle));
            _card.style.width = width;
            foreach (var b in buttons ?? new ModalButton[0]) AddButton(b);
            _pause = pause;
            _onClose = onClose;
            Current = current;
            IsOpen = true;
            UIX.Show(_layer, true);
            _layer.pickingMode = PickingMode.Position;
            Game.Root?.Lock(_key, true);
            if (pause) Game.Root?.SetPaused(_key, true);
            UIX.PopIn(_card);
            Game.Sound("whoosh", 0.05f, -10f);
            _scroll.scrollOffset = Vector2.zero;
            return _body;
        }

        public Button AddButton(ModalButton b)
        {
            var btn = UIX.Button(_buttons, b.Text, null, b.Variant, false, b.Icon);
            btn.style.minWidth = 120;
            var mb = b;
            btn.clicked += () =>
            {
                if (!mb.KeepOpen) Close();
                mb.OnClick?.Invoke();
            };
            return btn;
        }

        public void ClearButtons() => _buttons.Clear();

        public void Close()
        {
            if (!IsOpen) return;
            CloseSilently();
            var cb = _onClose;
            _onClose = null;
            cb?.Invoke();
        }

        private void CloseSilently()
        {
            IsOpen = false;
            Current = "";
            UIX.Show(_layer, false);
            Game.Root?.Lock(_key, false);
            if (_pause) Game.Root?.SetPaused(_key, false);
        }

        public Label Text(VisualElement parent, string text, string cls = "")
        {
            var l = UIX.Text(parent, text);
            if (!string.IsNullOrEmpty(cls)) l.AddToClassList(cls);
            return l;
        }

        public VisualElement KV(VisualElement parent, string key, string value, Color? color = null) => UIX.KV(parent, key, value, color);
    }

    /// <summary>
    /// Gesprächsfenster unten im Bild: Sprecher, Text mit Schreibmaschinen-Effekt, optional
    /// Antwortmöglichkeiten. Start(lines, choices, onDone) - onDone(choiceIndex) am Ende.
    /// </summary>
    public sealed class DialogueView
    {
        public struct Line
        {
            public string Speaker, Text;

            public Line(string speaker, string text)
            {
                Speaker = speaker;
                Text = text;
            }
        }

        private readonly VisualElement _layer;
        private VisualElement _panel, _choices;
        private Label _speaker, _text, _hint;
        private List<Line> _lines = new List<Line>();
        private string[] _choiceTexts = new string[0];
        private Action<int> _onDone;
        private int _idx;
        private float _chars;
        private bool _choicesShown;
        private string _full = "";
        private float _startedAt;

        public bool Active { get; private set; }

        public DialogueView(VisualElement layer)
        {
            _layer = layer;
            _panel = UIX.Col(_layer, 0f, "dialogue");
            _speaker = UIX.Text(_panel, "", "dlg-speaker");
            _text = UIX.Text(_panel, "", "dlg-text");
            _choices = UIX.Row(_panel, 10f, "dlg-choices");
            _hint = UIX.Text(_panel, "", "dlg-hint");
            UIX.Show(_layer, false);
        }

        public void Start(IList<Line> lines, string[] choices = null, Action<int> onDone = null)
        {
            _lines = new List<Line>(lines);
            _choiceTexts = choices ?? new string[0];
            _onDone = onDone;
            _idx = 0;
            _choicesShown = false;
            Active = true;
            _startedAt = Time.unscaledTime;
            UIX.Show(_layer, true);
            Game.Root?.Lock("dialogue", true);
            UIX.PopIn(_panel);
            ShowLine();
        }

        private void ShowLine()
        {
            var line = _lines[_idx];
            _speaker.text = line.Speaker;
            _full = line.Text;
            _text.text = "";
            _chars = 0f;
            _choices.Clear();
            UIX.Show(_choices, false);
            _hint.text = "[" + GameInput.KeyLabel("interact") + "] / Klick – weiter";
            UIX.Show(_hint, true);
        }

        private bool Typing => _chars < _full.Length;

        public void Tick(float dt)
        {
            if (!Active) return;
            if (Typing)
            {
                _chars += dt * 55f;
                _text.text = _full.Substring(0, Mathf.Min(_full.Length, (int)_chars));
            }
            else if (_text.text != _full) _text.text = _full;
            if (!Typing && _idx == _lines.Count - 1 && _choiceTexts.Length > 0 && !_choicesShown)
            {
                _choicesShown = true;
                UIX.Show(_hint, false);
                UIX.Show(_choices, true);
                for (int i = 0; i < _choiceTexts.Length; i++)
                {
                    int ci = i;
                    UIX.Button(_choices, _choiceTexts[i], () => Finish(ci), i == 0 ? "accent" : "");
                }
            }
            if (Time.unscaledTime - _startedAt > 0.15f && GameInput.AdvanceDown && !(_choicesShown && _choiceTexts.Length > 0)) Advance();
        }

        private void Advance()
        {
            if (!Active) return;
            if (Typing)
            {
                _chars = _full.Length;
                _text.text = _full;
                return;
            }
            if (_idx < _lines.Count - 1)
            {
                _idx++;
                Game.Sound("click", 0.05f, -8f);
                ShowLine();
            }
            else if (_choiceTexts.Length == 0) Finish(-1);
        }

        private void Finish(int choice)
        {
            if (!Active) return;
            Active = false;
            UIX.Show(_layer, false);
            Game.Root?.Lock("dialogue", false);
            Game.Sound("click");
            var cb = _onDone;
            _onDone = null;
            cb?.Invoke(choice);
        }
    }

    /// <summary>Schwarzblende für Übergänge (mit optionalem Text).</summary>
    public sealed class FaderView
    {
        private readonly VisualElement _layer;
        private readonly VisualElement _rect;
        private readonly Label _label;
        public bool Busy { get; private set; }

        public FaderView(VisualElement layer)
        {
            _layer = layer;
            _rect = UIX.Div(_layer, "layer", "fader");
            _label = UIX.Text(_rect, "", "fader-text");
            _rect.style.opacity = 1f;
            _layer.pickingMode = PickingMode.Ignore;
            _rect.pickingMode = PickingMode.Ignore;
        }

        public void SetBlack()
        {
            _rect.style.opacity = 1f;
            _label.style.opacity = 0f;
            UIX.Show(_layer, true);
        }

        public void FadeIn(float duration = 0.8f)
        {
            UIX.Show(_layer, true);
            _label.style.opacity = 0f;
            Anim.Run(duration, t => _rect.style.opacity = 1f - t, () => UIX.Show(_layer, false), Ease.InOutSine, true, this);
        }

        /// <summary>Abblenden, Text zeigen, mid aufrufen, wieder aufblenden.</summary>
        public void Transition(string text, Action mid, float hold = 1.2f)
        {
            if (Busy) return;
            Busy = true;
            _label.text = text;
            UIX.Show(_layer, true);
            _rect.pickingMode = PickingMode.Position;
            Anim.Run(0.7f, t => _rect.style.opacity = t, () =>
            {
                Anim.Run(0.35f, t => _label.style.opacity = string.IsNullOrEmpty(text) ? 0f : t, () =>
                {
                    mid?.Invoke();
                    Anim.Delay(hold, () =>
                    {
                        Anim.Run(0.35f, t => _label.style.opacity = string.IsNullOrEmpty(text) ? 0f : 1f - t, () =>
                        {
                            Anim.Run(0.8f, t => _rect.style.opacity = 1f - t, () =>
                            {
                                Busy = false;
                                _rect.pickingMode = PickingMode.Ignore;
                                UIX.Show(_layer, false);
                            }, Ease.InOutSine, true);
                        }, Ease.Linear, true);
                    }, true);
                }, Ease.Linear, true);
            }, Ease.InOutSine, true, this);
        }

        /// <summary>Nur abblenden, dann Callback.</summary>
        public void FadeOut(Action then, float duration = 0.6f)
        {
            UIX.Show(_layer, true);
            _label.style.opacity = 0f;
            _rect.pickingMode = PickingMode.Position;
            Anim.Run(duration, t => _rect.style.opacity = t, () =>
            {
                _rect.pickingMode = PickingMode.Ignore;
                then?.Invoke();
            }, Ease.InOutSine, true, this);
        }
    }
}
