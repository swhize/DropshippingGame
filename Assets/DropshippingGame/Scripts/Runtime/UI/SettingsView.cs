using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Einstellungen (Hauptmenü und Pausemenü) in Reitern: Grafik, Audio, Steuerung (inkl. Tasten neu
    /// belegen), Gameplay und Barrierefreiheit. Alles wird sofort angewendet und gespeichert.
    /// Controller: Steuerkreuz/Stick zum Navigieren, links/rechts an Reglern und Auswahlfeldern,
    /// LB/RB wechselt den Reiter (<see cref="CycleTab"/>).
    /// </summary>
    public static class SettingsView
    {
        private static readonly UIX.TabItem[] TabList =
        {
            new UIX.TabItem { Id = "grafik", Title = "Grafik", Icon = "sun" },
            new UIX.TabItem { Id = "audio", Title = "Audio", Icon = "music" },
            new UIX.TabItem { Id = "steuerung", Title = "Steuerung", Icon = "gamepad" },
            new UIX.TabItem { Id = "gameplay", Title = "Gameplay", Icon = "sparkle" },
            new UIX.TabItem { Id = "zugang", Title = "Barrierefreiheit", Icon = "eye" },
        };

        private static string _tab = "grafik";
        private static VisualElement _parent;
        private static Action _onBack;
        private static bool _confirmReset;

        /// <summary>Ist die Einstellungsseite gerade aufgebaut und sichtbar?</summary>
        public static bool IsShown => _parent != null && _parent.panel != null && UIX.IsShown(_parent);

        public static void Build(VisualElement parent, Action onBack)
        {
            _parent = parent;
            _onBack = onBack;
            _confirmReset = false;
            Rebuild(false);
        }

        /// <summary>LB/RB: Reiter wechseln.</summary>
        public static void CycleTab(int dir)
        {
            if (!IsShown || InputBindings.Capturing) return;
            int i = Array.FindIndex(TabList, t => t.Id == _tab);
            i = (i + dir + TabList.Length) % TabList.Length;
            _tab = TabList[i].Id;
            _confirmReset = false;
            Game.Sound("click", 0.05f, -8f);
            Rebuild(true);
        }

        private static void Rebuild(bool focusTab)
        {
            var parent = _parent;
            if (parent == null) return;
            parent.Clear();
            var head = UIX.Row(parent, 10f);
            UIX.Text(head, "Einstellungen", "h1");
            UIX.Spacer(head);
            if (_onBack != null) UIX.Button(head, "Zurück", Back, "ghost", false, "arrow_left");

            var tabs = UIX.Tabs(parent, TabList, _tab, id =>
            {
                if (id == _tab) return;
                _tab = id;
                _confirmReset = false;
                Rebuild(true);
            }, "tabbar");
            tabs.AddToClassList("settings-tabs");

            var body = UIX.Col(parent, 10f, "settings-body");
            try
            {
                switch (_tab)
                {
                    case "audio": Audio(body); break;
                    case "steuerung": Controls(body); break;
                    case "gameplay": Gameplay(body); break;
                    case "zugang": Access(body); break;
                    default: Graphics(body); break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UIX.Text(body, "Diese Seite konnte nicht geladen werden.", "muted");
            }

            var foot = UIX.Row(parent, 10f, "settings-foot");
            UIX.Button(foot, _confirmReset ? "Wirklich zurücksetzen?" : "Standardwerte", () =>
            {
                if (!_confirmReset)
                {
                    _confirmReset = true;
                    Rebuild(false);
                    FocusClass("settings-reset");
                    return;
                }
                _confirmReset = false;
                Settings.ResetToDefaults(_tab);
                Rebuild(false);
                FocusClass("settings-reset");
            }, _confirmReset ? "danger" : "ghost", false, "refresh").AddToClassList("settings-reset");
            UIX.Spacer(foot);
            if (_onBack != null) UIX.Button(foot, "Fertig", Back, "accent", false, "check");

            if (focusTab && GameInput.UsingGamepad)
                parent.schedule.Execute(() =>
                {
                    var active = tabs.Q(className: "active");
                    if (active != null && active.panel != null) active.Focus();
                });
        }

        private static void Back()
        {
            InputBindings.CancelCapture();
            _confirmReset = false;
            _onBack?.Invoke();
        }

        private static void FocusClass(string cls)
        {
            if (!GameInput.UsingGamepad || _parent == null) return;
            _parent.schedule.Execute(() =>
            {
                var el = _parent?.Q(className: cls);
                if (el != null && el.panel != null) el.Focus();
            });
        }

        // =====================================================================================
        // Reiter
        // =====================================================================================
        private static void Graphics(VisualElement p)
        {
            var disp = Section(p, "BILDSCHIRM", "screen");
            Row(disp, "Fenstermodus", r => UIX.Segmented(r, Settings.WindowModeNames, Mathf.Clamp(Settings.WindowMode, 0, 2), i =>
            {
                Settings.WindowMode = i;
                Settings.Apply();
            }));
            var res = Settings.AvailableResolutions();
            var names = new List<string> { "Automatisch (Bildschirm)" };
            int cur = 0;
            for (int i = 0; i < res.Count; i++)
            {
                names.Add(res[i].x + " × " + res[i].y);
                if (res[i].x == Settings.ResolutionWidth && res[i].y == Settings.ResolutionHeight) cur = i + 1;
            }
            Choice(disp, "Auflösung", names.ToArray(), cur, i =>
            {
                if (i <= 0) Settings.ResolutionWidth = Settings.ResolutionHeight = 0;
                else
                {
                    Settings.ResolutionWidth = res[i - 1].x;
                    Settings.ResolutionHeight = res[i - 1].y;
                }
                Settings.Apply();
            });
            if (Application.isEditor) Note(disp, "Fenstermodus und Auflösung wirken nur im fertigen Spiel, nicht im Unity-Editor.");
            UIX.Toggle(disp, "VSync (Bildrate an den Monitor koppeln)", Settings.VSync, on =>
            {
                Settings.VSync = on;
                Settings.Apply();
                Rebuild(false);
                FocusClass("fps-choice");
            });
            var fpsNames = new string[Settings.FpsLimits.Length];
            int fpsIdx = 0;
            for (int i = 0; i < fpsNames.Length; i++)
            {
                fpsNames[i] = Settings.FpsLimits[i] <= 0 ? "Unbegrenzt" : Settings.FpsLimits[i] + " FPS";
                if (Settings.FpsLimits[i] == Settings.FpsLimit) fpsIdx = i;
            }
            var fps = Choice(disp, "FPS-Limit", fpsNames, fpsIdx, i =>
            {
                Settings.FpsLimit = Settings.FpsLimits[i];
                Settings.Apply();
            });
            fps.AddToClassList("fps-row");
            fps.Q(className: "choice")?.AddToClassList("fps-choice");
            if (Settings.VSync) Note(disp, "Das FPS-Limit greift nur bei ausgeschaltetem VSync.");

            var q = Section(p, "QUALITÄT", "sun");
            Row(q, "Grafikqualität", r => UIX.Segmented(r, Settings.QualityNames, Mathf.Clamp(Settings.Quality, 0, Settings.QualityNames.Length - 1), i =>
            {
                Settings.Quality = i;
                Settings.Apply();
            }));
            UIX.Slider(q, "Renderauflösung", Settings.RenderScaleMin, 1f, Settings.RenderScale, 0.05f, Pct, v => Settings.RenderScale = v, v =>
            {
                Settings.RenderScale = v;
                Settings.Apply();
            });
            Note(q, "Unter 100 % läuft das Spiel flüssiger, wird aber etwas unschärfer.");
            UIX.Toggle(q, "FPS anzeigen", Settings.ShowFps, on => { Settings.ShowFps = on; Settings.Apply(); });
            UIX.Toggle(q, "Wechselndes Wetter (Wolken, Regen)", Settings.Weather, on => { Settings.Weather = on; Settings.Apply(); });

            var cam = Section(p, "KAMERA", "eye");
            UIX.Slider(cam, "Sichtfeld (FOV)", 60f, 100f, Settings.Fov, 1f, v => Mathf.RoundToInt(v) + "°", v => { Settings.Fov = v; Settings.ApplyLive(); }, Commit);
            UIX.Toggle(cam, "Bewegungsunschärfe", Settings.MotionBlur, on => { Settings.MotionBlur = on; Settings.Apply(); });
            UIX.Toggle(cam, "Kopfwippen beim Laufen", Settings.HeadBob, on => { Settings.HeadBob = on; Settings.Apply(); });
            UIX.Toggle(cam, "Kamerawackeln (Landen, Stöße)", Settings.CameraShake, on => { Settings.CameraShake = on; Settings.Apply(); });
        }

        private static void Audio(VisualElement p)
        {
            var a = Section(p, "LAUTSTÄRKE", "music");
            UIX.Slider(a, "Gesamtlautstärke", 0f, 1f, Settings.MasterVolume, 0.05f, Pct, v => { Settings.MasterVolume = v; Settings.ApplyLive(); }, Commit);
            UIX.Slider(a, "Musik", 0f, 1f, Settings.MusicVolume, 0.05f, Pct, v => { Settings.MusicVolume = v; Settings.ApplyLive(); }, Commit);
            UIX.Slider(a, "Effekte & Stimmen", 0f, 1f, Settings.SfxVolume, 0.05f, Pct, v => { Settings.SfxVolume = v; Settings.ApplyLive(); }, Commit);
            Note(a, "Streamer-Modus und Untertitel findest du unter Gameplay bzw. Barrierefreiheit.");
        }

        private static void Controls(VisualElement p)
        {
            var s = Section(p, "EMPFINDLICHKEIT", "mouse");
            UIX.Slider(s, "Maus", 0.2f, 3f, Settings.MouseSensitivity, 0.1f, v => Fmt.Dec(v, 1), v => { Settings.MouseSensitivity = v; Settings.ApplyLive(); }, Commit);
            UIX.Slider(s, "Controller", 0.2f, 3f, Settings.PadSensitivity, 0.1f, v => Fmt.Dec(v, 1), v => { Settings.PadSensitivity = v; Settings.ApplyLive(); }, Commit);
            UIX.Toggle(s, "Y-Achse invertieren", Settings.InvertY, on => { Settings.InvertY = on; Settings.Apply(); });

            var b = Section(p, "TASTENBELEGUNG", "keyboard");
            var head = UIX.Row(b, 10f, "bind-row", "bind-head");
            UIX.Text(head, "AKTION", "bind-action");
            UIX.Text(head, "TASTATUR", "bind-col");
            UIX.Text(head, "CONTROLLER", "bind-col");
            for (int i = 0; i < InputBindings.Actions.Length; i++)
            {
                var a = InputBindings.Actions[i];
                var row = UIX.Row(b, 10f, "bind-row");
                if (i % 2 == 0) row.AddToClassList("alt");
                UIX.Text(row, a.Name, "bind-action");
                BindButton(row, a.Id, false);
                BindButton(row, a.Id, true);
            }
            Note(b, "Feld anklicken (bzw. mit A wählen) und die neue Taste drücken – Esc bzw. Start bricht ab. Ist die Taste schon vergeben, tauschen die Aktionen. " +
                    "Laufen (WASD/Stick), Esc/Start und die Menü-Bedienung bleiben fest.");
            var r2 = UIX.Row(b, 8f);
            r2.style.marginTop = 6;
            UIX.Button(r2, "Standardbelegung", () =>
            {
                InputBindings.ResetAll();
                Rebuild(false);
            }, "ghost", false, "refresh");
        }

        private static void BindButton(VisualElement row, string id, bool pad)
        {
            bool waiting = InputBindings.CaptureAction == id && InputBindings.CapturePad == pad;
            string label = waiting ? (pad ? "Taste am Controller …" : "Taste drücken …") : (pad ? InputBindings.PadLabel(id) : InputBindings.KeyLabel(id));
            Button btn = null;
            btn = UIX.Button(row, label, () =>
            {
                if (InputBindings.CaptureAction != null) return;
                UIX.SetButtonText(btn, pad ? "Taste am Controller …" : "Taste drücken …");
                btn.AddToClassList("waiting");
                // Während der Eingabe nichts anderes bedienbar (sonst löst z. B. A gleich den nächsten Knopf aus).
                _parent?.SetEnabled(false);
                InputBindings.BeginCapture(id, pad, ok =>
                {
                    var host = _parent;
                    if (host == null) return;
                    // Einen Frame warten, damit die gedrückte Taste nichts mehr auslöst.
                    host.schedule.Execute(() =>
                    {
                        host.SetEnabled(true);
                        if (host.panel == null) return;
                        Rebuild(false);
                        if (GameInput.UsingGamepad || !ok) FocusClass("bind-" + id + (pad ? "-pad" : "-kb"));
                        if (ok) Game.Sound("click", 0.05f, -4f);
                    }).ExecuteLater(60);
                });
            }, "", false, pad ? "gamepad" : "keyboard");
            btn.AddToClassList("bind-btn");
            btn.AddToClassList("bind-" + id + (pad ? "-pad" : "-kb"));
            if (waiting) btn.AddToClassList("waiting");
            if (InputBindings.Def(id) != null && (pad ? InputBindings.PadName(id) != InputBindings.Def(id).Pad : InputBindings.KeyName(id) != InputBindings.Def(id).Key))
                btn.AddToClassList("changed");
        }

        private static void Gameplay(VisualElement p)
        {
            var l = Section(p, "SPRACHE", "globe");
            int li = Mathf.Max(0, Array.IndexOf(Settings.LanguageCodes, Settings.Language));
            Row(l, "Sprache / Language", r => UIX.Segmented(r, Settings.LanguageNames, li, i =>
            {
                Settings.SetLanguage(Settings.LanguageCodes[i]);
                Rebuild(false);
            }));
            if (Settings.Language != "de") Note(l, "English translation is coming with a future update – texts stay German until then.");

            var h = Section(p, "ANZEIGE IM SPIEL", "screen");
            UIX.Toggle(h, "Kompass mit wichtigen Orten (oben)", Settings.ShowCompass, on => { Settings.ShowCompass = on; Settings.Apply(); });
            UIX.Toggle(h, "Tastenhinweise im HUD", Settings.ShowHints, on => { Settings.ShowHints = on; Settings.Apply(); });
            Row(h, "Design von Laptop & Handy", r =>
            {
                string[] ids = { "hype", "frachtbrief" };
                UIX.Segmented(r, new[] { "Hype", "Frachtbrief" }, Settings.LaptopDesign == "frachtbrief" ? 1 : 0, i =>
                {
                    Settings.LaptopDesign = ids[i];
                    Settings.Apply();
                });
            });
            UIX.Toggle(h, "Dunkles Design (Laptop & Handy)", Settings.LaptopDark, on => { Settings.LaptopDark = on; Settings.Apply(); });

            var st = Section(p, "STREAMER-MODUS", "music");
            UIX.Toggle(st, "Streamer-Modus", Settings.StreamerMode, on =>
            {
                Settings.StreamerMode = on;
                Settings.Apply();
                Rebuild(false);
                FocusClass("streamer-toggle");
            }).AddToClassList("streamer-toggle");
            Note(st, "Für Streams und Videos: kein Kamerawackeln, keine blinkenden Effekte, keine Pfade im Fehlerbericht. Alle Musik im Spiel ist CC0 (gemeinfrei).");
            if (Settings.StreamerMode)
                UIX.Toggle(st, "Musik im Streamer-Modus stumm (falls Content-ID fälschlich anschlägt)", Settings.StreamerNoMusic, on =>
                {
                    Settings.StreamerNoMusic = on;
                    Settings.Apply();
                });
        }

        private static void Access(VisualElement p)
        {
            var ui = Section(p, "OBERFLÄCHE", "screen");
            // Beim Ziehen nur die Anzeige ändern – die Größe wird beim Loslassen übernommen,
            // sonst würde sich der Regler unter dem Mauszeiger verschieben.
            UIX.Slider(ui, "UI-Größe", Settings.UiScaleMin, Settings.UiScaleMax, Settings.UiScale, 0.05f, Pct, null, v =>
            {
                if (Mathf.Abs(v - Settings.UiScale) < 0.001f) return;
                Settings.UiScale = v;
                Settings.Apply();
            });
            UIX.Toggle(ui, "Weniger Bewegung (kein Blinken/Pulsieren)", Settings.ReduceMotion, on => { Settings.ReduceMotion = on; Settings.Apply(); });

            var t = Section(p, "TEXT & UNTERTITEL", "chat");
            UIX.Toggle(t, "Untertitel für Figuren (Kalle, Kundschaft, Passanten)", Settings.Subtitles, on => { Settings.Subtitles = on; Settings.Apply(); });
            Row(t, "Textgeschwindigkeit", r => UIX.Segmented(r, Settings.TextSpeedNames, Mathf.Clamp(Settings.TextSpeed, 0, 3), i =>
            {
                Settings.TextSpeed = i;
                Settings.Apply();
            }));

            var c = Section(p, "FARBEN", "eye");
            UIX.Toggle(c, "Farbenblind-freundlich (Blau/Orange statt Grün/Rot)", Settings.ColorBlind, on =>
            {
                Settings.ColorBlind = on;
                Settings.Apply();
                Rebuild(false);
                FocusClass("cb-toggle");
            }).AddToClassList("cb-toggle");
            var prev = UIX.Row(c, 8f);
            prev.style.marginTop = 4;
            UIX.Chip(prev, "Gut: pünktlich", "check_circle", Theme.Good);
            UIX.Chip(prev, "Schlecht: zu spät", "warning", Theme.Bad);
        }

        // =====================================================================================
        // Bausteine
        // =====================================================================================
        private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + " %";

        /// <summary>Regler losgelassen (bzw. Tastatur/Controller): speichern.</summary>
        private static void Commit(float _) => Settings.Apply();

        private static VisualElement Section(VisualElement parent, string title, string icon)
        {
            var c = UIX.Col(parent, 2f, "card", "settings-card");
            var h = UIX.Row(c, 8f);
            UIX.Icon(h, Icons.Has(icon) ? icon : "gear", 15f, Theme.Accent);
            UIX.Text(h, title, "eyebrow");
            h.style.marginBottom = 4;
            return c;
        }

        private static VisualElement Row(VisualElement parent, string label, Action<VisualElement> fill)
        {
            var r = UIX.Row(parent, 14f, "slider-row");
            UIX.Text(r, label, "slider-label");
            fill?.Invoke(r);
            return r;
        }

        private static void Note(VisualElement parent, string text)
        {
            var l = UIX.Text(parent, text, "small", "settings-note");
            l.style.whiteSpace = WhiteSpace.Normal;
        }

        /// <summary>
        /// Auswahlfeld „‹ Wert ›“: Klick links = zurück, sonst weiter; Controller/Tastatur: links/rechts.
        /// Gibt die Zeile zurück.
        /// </summary>
        public static VisualElement Choice(VisualElement parent, string label, string[] options, int index, Action<int> onChange)
        {
            var row = UIX.Row(parent, 14f, "slider-row");
            UIX.Text(row, label, "slider-label");
            if (options == null || options.Length == 0) return row;
            int cur = Mathf.Clamp(index, 0, options.Length - 1);
            float lastX = -1f;
            Label value = null;
            Button box = null;

            void Set(int i)
            {
                int n = (i % options.Length + options.Length) % options.Length;
                if (n == cur) return;
                cur = n;
                value.text = options[cur];
                try
                {
                    onChange?.Invoke(cur);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            box = UIX.PressRow(row, 8f, () =>
            {
                bool back = lastX >= 0f && lastX < 44f;
                lastX = -1f;
                Set(cur + (back ? -1 : 1));
            }, "choice");
            box.style.flexGrow = 1;
            UIX.Icon(box, "chevron_left", 16f, Theme.Muted, "choice-arrow");
            value = UIX.Text(box, options[cur], "choice-value");
            value.style.flexGrow = 1;
            UIX.Icon(box, "chevron_right", 16f, Theme.Muted, "choice-arrow");
            UIX.PassThrough(box);
            box.RegisterCallback<PointerDownEvent>(e => lastX = e.localPosition.x, TrickleDown.TrickleDown);
            box.RegisterCallback<NavigationMoveEvent>(e =>
            {
                if (e.direction != NavigationMoveEvent.Direction.Left && e.direction != NavigationMoveEvent.Direction.Right) return;
                Set(cur + (e.direction == NavigationMoveEvent.Direction.Right ? 1 : -1));
                Game.Sound("click", 0.05f, -10f);
                e.StopPropagation();
#if UNITY_6000_0_OR_NEWER
                box.focusController?.IgnoreEvent(e);
#else
                e.PreventDefault();
#endif
            });
            return row;
        }
    }
}
