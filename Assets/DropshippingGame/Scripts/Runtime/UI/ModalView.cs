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
    /// Zentriertes Fenster (Karte) mit Symbol, Titel, Inhalt und Buttons. Genutzt für Ereignis-
    /// Entscheidungen, Tagesabschluss (Kassenbon), Bestätigungen, Hilfe, Pitch-Minispiel und das
    /// Spielende. Pausiert optional das Spiel, solange es offen ist.
    /// </summary>
    public sealed class ModalView
    {
        private readonly VisualElement _layer;
        private readonly string _key;
        private VisualElement _card, _head, _iconBox, _body, _buttons;
        private ScrollView _scroll;
        private Label _title, _sub;
        private bool _pause;
        private Action _onClose;
        private string _cardClass;

        public bool IsOpen { get; private set; }
        public string Current { get; private set; } = "";
        /// <summary>Die Karte (für die Fokus-Steuerung per Controller).</summary>
        public VisualElement Card => _card;

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
            _head = UIX.Div(_card, "modal-head");
            _iconBox = UIX.Div(_head, "modal-icon");
            var titles = UIX.Col(_head, 0f, "modal-titles");
            _title = UIX.Text(titles, "", "modal-title");
            _sub = UIX.Text(titles, "", "modal-sub");
            _scroll = UIX.Scroll(_card, "modal-body");
            _body = UIX.Col(_scroll.contentContainer, 10f);
            _buttons = UIX.Row(_card, 10f, "modal-buttons");
        }

        /// <summary>Öffnet das Fenster und gibt den Inhaltsbereich zurück.</summary>
        public VisualElement Open(string title, string subtitle, float width, IList<ModalButton> buttons, bool pause = true, string current = "",
            Action onClose = null, string icon = null, string cardClass = null)
        {
            if (IsOpen) CloseSilently();
            _body.Clear();
            _buttons.Clear();
            _title.text = title ?? "";
            _sub.text = subtitle ?? "";
            UIX.Show(_sub, !string.IsNullOrEmpty(subtitle));
            _iconBox.Clear();
            UIX.Show(_iconBox, !string.IsNullOrEmpty(icon));
            if (!string.IsNullOrEmpty(icon)) UIX.Icon(_iconBox, icon, 22f);
            if (!string.IsNullOrEmpty(_cardClass)) _card.RemoveFromClassList(_cardClass);
            _cardClass = cardClass;
            if (!string.IsNullOrEmpty(_cardClass)) _card.AddToClassList(_cardClass);
            _card.style.width = width;
            if (buttons != null)
                foreach (var b in buttons)
                    AddButton(b);
            UIX.Show(_buttons, _buttons.childCount > 0);
            _pause = pause;
            _onClose = onClose;
            Current = current ?? "";
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
            var mb = b;
            var btn = UIX.Button(_buttons, b.Text, () =>
            {
                if (!mb.KeepOpen) Close();
                mb.OnClick?.Invoke();
            }, b.Variant, false, b.Icon);
            btn.style.minWidth = 120;
            UIX.Show(_buttons, true);
            return btn;
        }

        /// <summary>Große Auswahl-Schaltfläche im Inhalt (z. B. Ereignis-Entscheidungen), optional mit Kosten-Pille.</summary>
        public Button AddChoice(VisualElement parent, string text, string cost, Action onClick, bool primary, string icon = null)
        {
            var b = UIX.Button(parent ?? _body, text, () =>
            {
                Close();
                onClick?.Invoke();
            }, primary ? "accent" : "", false, icon);
            b.AddToClassList("choice");
            if (!string.IsNullOrEmpty(cost))
            {
                var c = UIX.Text(b, cost, "choice-cost");
                Fonts.AddClass(c, "num-b");
                c.pickingMode = PickingMode.Ignore;
            }
            return b;
        }

        public void ClearButtons()
        {
            _buttons.Clear();
            UIX.Show(_buttons, false);
        }

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
            var l = UIX.Text(parent, text, "modal-text");
            if (!string.IsNullOrEmpty(cls)) Fonts.AddClass(l, cls);
            return l;
        }

        public VisualElement KV(VisualElement parent, string key, string value, Color? color = null) => UIX.KV(parent, key, value, color);
    }

    /// <summary>
    /// Gesprächsfenster unten im Bild: Profilbild, Sprecher, Text mit Schreibmaschinen-Effekt,
    /// optional Antwortmöglichkeiten. Start(lines, choices, onDone) - onDone(choiceIndex) am Ende.
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
        private readonly VisualElement _panel, _choices, _avatarHost, _foot;
        private readonly Label _speaker, _text, _hintText;
        private Label _hintKey;
        private List<Line> _lines = new List<Line>();
        private string[] _choiceTexts = new string[0];
        private Action<int> _onDone;
        private int _idx;
        private float _chars;
        private bool _choicesShown;
        private string _full = "";
        private float _startedAt;
        private string _avatarFor = "";

        public bool Active { get; private set; }
        /// <summary>Antwort-Buttons sichtbar (dann nimmt der Controller-Fokus sie).</summary>
        public bool ChoicesShown => Active && _choicesShown;
        public VisualElement Panel => _panel;

        public DialogueView(VisualElement layer)
        {
            _layer = layer;
            _panel = UIX.Div(_layer, "dialogue");
            _avatarHost = UIX.Div(_panel, "dlg-avatar");
            var main = UIX.Col(_panel, 0f, "dlg-main");
            _speaker = UIX.Text(main, "", "dlg-speaker");
            _text = UIX.Text(main, "", "dlg-text");
            _choices = UIX.Row(main, 10f, "dlg-choices");
            _foot = UIX.Row(main, 6f, "dlg-foot");
            _hintKey = UIX.Key(_foot, "E", "keycap-sm");
            _hintText = UIX.Text(_foot, "weiter", "keyhint-text");
            UIX.Show(_layer, false);
        }

        public void Start(IList<Line> lines, string[] choices = null, Action<int> onDone = null)
        {
            if (lines == null || lines.Count == 0)
            {
                onDone?.Invoke(-1);
                return;
            }
            _lines = new List<Line>(lines);
            _choiceTexts = choices ?? new string[0];
            _onDone = onDone;
            _idx = 0;
            _choicesShown = false;
            Active = true;
            _startedAt = Time.unscaledTime;
            UIX.Show(_layer, true);
            Game.Root?.Lock("dialogue", true);
            UIX.FadeSlideIn(_panel, 14f, 0.18f);
            ShowLine();
        }

        private void ShowLine()
        {
            var line = _lines[_idx];
            _speaker.text = (line.Speaker ?? "").ToUpperInvariant();
            if (_avatarFor != line.Speaker)
            {
                _avatarFor = line.Speaker;
                _avatarHost.Clear();
                bool me = line.Speaker == "Du";
                UIX.Avatar(_avatarHost, line.Speaker, me ? new Color(0.3f, 0.55f, 0.96f) : new Color(0.89f, 0.34f, 0.23f), 58f);
            }
            _full = line.Text ?? "";
            _text.text = "";
            _chars = 0f;
            _choices.Clear();
            UIX.Show(_choices, false);
            _hintKey.text = GameInput.KeyLabel("interact");
            _hintText.text = _idx < _lines.Count - 1 || _choiceTexts.Length == 0 ? "weiter  ·  Klick" : "Antwort wählen";
            UIX.Show(_foot, true);
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
                UIX.Show(_foot, false);
                UIX.Show(_choices, true);
                for (int i = 0; i < _choiceTexts.Length; i++)
                {
                    int ci = i;
                    UIX.Button(_choices, _choiceTexts[i], () => Finish(ci), i == 0 ? "accent" : "");
                }
                UIX.FadeSlideIn(_choices, 6f, 0.14f);
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
            _choicesShown = false;
            UIX.Show(_layer, false);
            Game.Root?.Lock("dialogue", false);
            Game.Sound("click");
            var cb = _onDone;
            _onDone = null;
            cb?.Invoke(choice);
        }
    }

    /// <summary>Schwarzblende für Übergänge (mit Titel, Untertitel und wechselndem Tipp).</summary>
    public sealed class FaderView
    {
        private readonly VisualElement _layer;
        private readonly VisualElement _rect, _tipBox;
        private readonly Label _label, _sub, _tip;
        public bool Busy { get; private set; }

        public FaderView(VisualElement layer)
        {
            _layer = layer;
            _rect = UIX.Div(_layer, "layer", "fader");
            var mid = UIX.Col(_rect, 0f);
            mid.style.alignItems = Align.Center;
            _label = UIX.Text(mid, "", "fader-text");
            _sub = UIX.Text(mid, "", "fader-sub");
            _tipBox = UIX.Col(_rect, 0f, "fader-tip");
            _tipBox.style.position = Position.Absolute;
            UIX.Text(_tipBox, "TIPP", "fader-tip-eyebrow");
            _tip = UIX.Text(_tipBox, "", "fader-tip-text");
            _rect.style.opacity = 1f;
            _layer.pickingMode = PickingMode.Ignore;
            _rect.pickingMode = PickingMode.Ignore;
            foreach (var el in _rect.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
            SetTexts("", "", false);
        }

        private void SetTexts(string title, string sub, bool tip)
        {
            _label.text = title ?? "";
            _sub.text = sub ?? "";
            UIX.Show(_sub, !string.IsNullOrEmpty(sub));
            _tip.text = tip ? Tips.Random() : "";
            UIX.Show(_tipBox, tip);
        }

        private void SetTextAlpha(float a)
        {
            _label.style.opacity = a;
            _sub.style.opacity = a;
            _tipBox.style.opacity = a;
        }

        public void SetBlack()
        {
            _rect.style.opacity = 1f;
            SetTextAlpha(0f);
            UIX.Show(_layer, true);
        }

        public void FadeIn(float duration = 0.8f)
        {
            UIX.Show(_layer, true);
            SetTextAlpha(0f);
            Anim.Run(duration, t => _rect.style.opacity = 1f - t, () => UIX.Show(_layer, false), Ease.InOutSine, true, this);
        }

        /// <summary>Abblenden, Text (+ Tipp) zeigen, mid aufrufen, wieder aufblenden.</summary>
        public void Transition(string text, Action mid, float hold = 1.2f, string sub = null)
        {
            if (Busy) return;
            Busy = true;
            bool hasText = !string.IsNullOrEmpty(text);
            SetTexts(text, sub, hasText);
            SetTextAlpha(0f);
            UIX.Show(_layer, true);
            _rect.pickingMode = PickingMode.Position;
            Anim.Run(0.6f, t => _rect.style.opacity = t, () =>
            {
                Anim.Run(0.3f, t => SetTextAlpha(hasText ? t : 0f), () =>
                {
                    try
                    {
                        mid?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    Anim.Delay(hasText ? Mathf.Max(hold, 1.4f) : hold, () =>
                    {
                        Anim.Run(0.3f, t => SetTextAlpha(hasText ? 1f - t : 0f), () =>
                        {
                            Anim.Run(0.7f, t => _rect.style.opacity = 1f - t, () =>
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
            SetTextAlpha(0f);
            _rect.pickingMode = PickingMode.Position;
            Anim.Run(duration, t => _rect.style.opacity = t, () =>
            {
                _rect.pickingMode = PickingMode.Ignore;
                then?.Invoke();
            }, Ease.InOutSine, true, this);
        }
    }
}
