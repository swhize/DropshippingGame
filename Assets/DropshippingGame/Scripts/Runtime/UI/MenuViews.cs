using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Einstellungen (Hauptmenü und Pausemenü): Audio, Steuerung, Grafik, Laptop-Design.</summary>
    public static class SettingsView
    {
        public static void Build(VisualElement parent, Action onBack)
        {
            UIX.Text(parent, "Einstellungen", "h2");
            UIX.Text(parent, "AUDIO", "eyebrow").style.marginTop = 6;
            Func<float, string> pct = v => Mathf.RoundToInt(v * 100f) + " %";
            UIX.Slider(parent, "Gesamtlautstärke", 0f, 1f, Settings.MasterVolume, 0.05f, pct, v => { Settings.MasterVolume = v; Settings.Apply(); });
            UIX.Slider(parent, "Musik", 0f, 1f, Settings.MusicVolume, 0.05f, pct, v => { Settings.MusicVolume = v; Settings.Apply(); });
            UIX.Slider(parent, "Effekte", 0f, 1f, Settings.SfxVolume, 0.05f, pct, v => { Settings.SfxVolume = v; Settings.Apply(); });

            UIX.Text(parent, "STEUERUNG", "eyebrow").style.marginTop = 8;
            UIX.Slider(parent, "Mausempfindlichkeit", 0.2f, 3f, Settings.MouseSensitivity, 0.1f, v => Fmt.Dec(v, 1), v => { Settings.MouseSensitivity = v; Settings.Apply(); });
            UIX.Slider(parent, "Controller-Empfindlichkeit", 0.2f, 3f, Settings.PadSensitivity, 0.1f, v => Fmt.Dec(v, 1), v => { Settings.PadSensitivity = v; Settings.Apply(); });
            UIX.Toggle(parent, "Y-Achse invertieren", Settings.InvertY, on => { Settings.InvertY = on; Settings.Apply(); });

            UIX.Text(parent, "GRAFIK", "eyebrow").style.marginTop = 8;
            UIX.Slider(parent, "Sichtfeld (FOV)", 60f, 100f, Settings.Fov, 1f, v => Mathf.RoundToInt(v).ToString(), v => { Settings.Fov = v; Settings.Apply(); });
            var q = UIX.Row(parent, 8f);
            UIX.Text(q, "Grafikqualität", "slider-label");
            for (int i = 0; i < Settings.QualityNames.Length; i++)
            {
                int qi = i;
                var b = UIX.Button(q, Settings.QualityNames[i], null, Settings.Quality == i ? "soft" : "");
                b.clicked += () =>
                {
                    Settings.Quality = qi;
                    Settings.Apply();
                    PostFX.ApplyQuality();
                    foreach (var child in q.Children())
                        if (child is Button cb) cb.EnableInClassList("btn-soft", child == b);
                };
            }
            UIX.Toggle(parent, "Vollbild", Settings.Fullscreen, on => { Settings.Fullscreen = on; Settings.Apply(); });
            UIX.Toggle(parent, "Kopfwippen beim Laufen", Settings.HeadBob, on => { Settings.HeadBob = on; Settings.Apply(); });
            UIX.Toggle(parent, "FPS anzeigen", Settings.ShowFps, on => { Settings.ShowFps = on; Settings.Apply(); });

            UIX.Text(parent, "LAPTOP", "eyebrow").style.marginTop = 8;
            var d = UIX.Row(parent, 8f);
            UIX.Text(d, "Design", "slider-label");
            foreach (var (id, label) in new[] { ("frachtbrief", "Frachtbrief"), ("hype", "Hype") })
            {
                var b = UIX.Button(d, label, null, Settings.LaptopDesign == id ? "soft" : "");
                b.clicked += () =>
                {
                    Settings.LaptopDesign = id;
                    Settings.Apply();
                    foreach (var child in d.Children())
                        if (child is Button cb) cb.EnableInClassList("btn-soft", child == b);
                };
            }
            UIX.Toggle(parent, "Dunkles Laptop-Design", Settings.LaptopDark, on => { Settings.LaptopDark = on; Settings.Apply(); });

            if (onBack != null) UIX.Button(parent, "Zurück", onBack, "accent").style.marginTop = 10;
        }
    }

    /// <summary>Pausemenü (Esc / Start): Weiter, Speichern, Einstellungen, Hilfe, Hauptmenü, Beenden.</summary>
    public sealed class PauseView
    {
        private readonly VisualElement _layer;
        private readonly VisualElement _card;
        private VisualElement _menu, _settings;

        public bool IsOpen { get; private set; }
        public Action OnMainMenu, OnQuit, OnHelp;

        public PauseView(VisualElement layer)
        {
            _layer = layer;
            var dim = UIX.Div(_layer, "layer", "dim");
            _card = UIX.Col(dim, 0f, "modal-card", "pause-card");
            UIX.Show(_layer, false);
        }

        private void ShowMenu()
        {
            _card.Clear();
            _card.style.width = 380;
            _menu = UIX.Col(_card, 10f);
            UIX.Text(_menu, "Pause", "h1").style.unityTextAlign = TextAnchor.MiddleCenter;
            UIX.Text(_menu, Game.Sim != null ? Game.Sim.BrandName + " · Tag " + Game.Sim.Day + " · Spielstand " + Game.Sim.Slot : "", "muted").style.unityTextAlign = TextAnchor.MiddleCenter;
            UIX.Button(_menu, "Weiterspielen", Close, "accent", false, "play").AddToClassList("btn-big");
            UIX.Button(_menu, "Spiel speichern", Save, "", false, "save");
            UIX.Button(_menu, "Einstellungen", ShowSettings, "", false, "gear");
            UIX.Button(_menu, "Steuerung & Hilfe", () => OnHelp?.Invoke(), "", false, "help");
            UIX.Button(_menu, "Zum Hauptmenü", () => OnMainMenu?.Invoke(), "", false, "home");
            UIX.Button(_menu, "Spiel beenden", () => OnQuit?.Invoke(), "danger", false, "exit");
        }

        private void ShowSettings()
        {
            _card.Clear();
            _card.style.width = 620;
            var sv = new ScrollView(ScrollViewMode.Vertical);
            sv.style.maxHeight = 720;
            _card.Add(sv);
            _settings = UIX.Col(sv.contentContainer, 6f);
            SettingsView.Build(_settings, ShowMenu);
        }

        private void Save()
        {
            var sim = Game.Sim;
            if (sim == null) return;
            if (sim.StoryStage == "diner")
            {
                sim.Notify("Während der Imbiss-Schicht kann nicht gespeichert werden.", "info");
                return;
            }
            sim.SaveGame();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            ShowMenu();
            UIX.Show(_layer, true);
            Game.Root?.Lock("pause", true);
            Game.Root?.SetPaused("pause", true);
            UIX.PopIn(_card);
            Game.Sound("whoosh", 0.05f, -8f);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            UIX.Show(_layer, false);
            Game.Root?.Lock("pause", false);
            Game.Root?.SetPaused("pause", false);
        }

        /// <summary>Esc im Pausemenü: aus den Einstellungen zurück, sonst schließen.</summary>
        public void Back()
        {
            if (_settings != null && _settings.panel != null)
            {
                _settings = null;
                ShowMenu();
            }
            else Close();
        }
    }

    /// <summary>
    /// Hauptmenü: die Spielwelt in der Abenddämmerung als Kulisse, links das Menü
    /// (Fortsetzen, Neues Spiel mit Spielstand-Wahl, Laden, Einstellungen, Credits, Beenden).
    /// </summary>
    public sealed class MainMenuView
    {
        private readonly VisualElement _layer;
        private VisualElement _content;
        public Action<string, int> OnNewGame;
        public Action<int> OnLoad;
        public Action OnQuit;
        private string _pendingMode = "intro";

        public MainMenuView(VisualElement layer)
        {
            _layer = layer;
            Build();
            UIX.Show(_layer, false);
        }

        private void Build()
        {
            var shade = UIX.Div(_layer, "menu-shade");
            shade.style.backgroundImage = new StyleBackground(ShadeTexture());
            shade.pickingMode = PickingMode.Ignore;
            var panel = UIX.Col(_layer, 0f, "menu-panel");
            UIX.Text(panel, "DROPSHIPPING\nSIMULATOR", "menu-title");
            UIX.Text(panel, "Vom Imbiss zum Imperium", "menu-sub");
            var sv = new ScrollView(ScrollViewMode.Vertical);
            sv.AddToClassList("menu-content");
            sv.style.flexGrow = 1;
            panel.Add(sv);
            _content = UIX.Col(sv.contentContainer, 10f);
            UIX.Text(_layer, "v2.0 · Unity · Alle Modelle, Texturen, Sounds & Musik werden prozedural erzeugt", "menu-version");
        }

        private static Texture2D _shade;

        private static Texture2D ShadeTexture()
        {
            if (_shade != null) return _shade;
            const int w = 256;
            _shade = new Texture2D(w, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);
                float a = Mathf.Lerp(0.94f, 0f, Mathf.SmoothStep(0f, 1f, t));
                _shade.SetPixel(x, 0, new Color(0.02f, 0.03f, 0.05f, a));
            }
            _shade.Apply();
            return _shade;
        }

        public void Open()
        {
            UIX.Show(_layer, true);
            ShowMain();
            UIX.PopIn(_content, 0.4f);
        }

        public void Close() => UIX.Show(_layer, false);

        public bool IsOpen => _layer.style.display != DisplayStyle.None;

        public void Tick(float dt)
        {
        }

        private Button MenuButton(string text, Action cb, string variant = "", string sub = null, string icon = null)
        {
            var b = UIX.Button(_content, text, cb, variant, false, icon);
            b.AddToClassList("menu-btn");
            if (!string.IsNullOrEmpty(sub))
            {
                var inner = new VisualElement();
                inner.style.flexDirection = FlexDirection.Column;
                inner.style.flexGrow = 1;
                inner.pickingMode = PickingMode.Ignore;
                var label = b.Q<Label>(className: "btn-label");
                label.RemoveFromHierarchy();
                inner.Add(label);
                var s = UIX.Text(inner, sub, "menu-btn-sub");
                s.pickingMode = PickingMode.Ignore;
                b.Add(inner);
            }
            return b;
        }

        private void ShowMain()
        {
            _content.Clear();
            var sim = Game.Sim;
            int recent = sim.MostRecentSlot();
            if (recent > 0)
            {
                var sum = sim.ReadSummary(recent);
                MenuButton("Fortsetzen", () => OnLoad?.Invoke(recent), "accent",
                    sum.Brand + " · Tag " + sum.Day + " · " + Fmt.Money(sum.Money) + " · Level " + sum.Level + " · Spielstand " + recent, "play");
            }
            MenuButton("Neues Spiel", ShowNewGame, recent > 0 ? "" : "accent", null, "sparkle");
            if (sim.HasAnySave()) MenuButton("Spielstand laden", ShowSlots, "", null, "list");
            MenuButton("Einstellungen", ShowSettings, "", null, "gear");
            MenuButton("Credits", ShowCredits, "", null, "heart");
            MenuButton("Beenden", () => OnQuit?.Invoke(), "", null, "exit");
        }

        private void ShowNewGame()
        {
            _content.Clear();
            UIX.Text(_content, "Neues Spiel", "h2");
            MenuButton("Mit Story starten (empfohlen)", () => PickSlot("intro"), "accent", "Beginne als Aushilfe in Kalles Imbiss. Inkl. Tutorial.", "fire");
            MenuButton("Direkt in die Garage", () => PickSlot("tutorial"), "", "Ohne Imbiss-Intro, mit Tutorial.", "home");
            MenuButton("Profi-Start", () => PickSlot("skip"), "", "Kein Intro, kein Tutorial. Du weißt, was du tust.", "bolt");
            MenuButton("Zurück", ShowMain, "ghost", null, "chevron");
        }

        private void PickSlot(string mode)
        {
            _pendingMode = mode;
            _content.Clear();
            UIX.Text(_content, "Spielstand wählen", "h2");
            UIX.Text(_content, "In welchem Speicherplatz soll dein neues Spiel liegen?", "muted");
            var sim = Game.Sim;
            for (int s = 1; s <= Sim.SaveSlots; s++)
            {
                int slot = s;
                var sum = sim.ReadSummary(s);
                if (sum == null)
                    MenuButton("Spielstand " + s + " · leer", () => OnNewGame?.Invoke(_pendingMode, slot), s == 1 ? "accent" : "", "Neues Spiel hier starten", "plus");
                else
                    MenuButton("Spielstand " + s + " überschreiben", () => ConfirmOverwrite(slot, sum), "",
                        sum.Brand + " · Tag " + sum.Day + " · " + Fmt.Money(sum.Money), "warning");
            }
            MenuButton("Zurück", ShowNewGame, "ghost", null, "chevron");
        }

        private void ConfirmOverwrite(int slot, SaveSummary sum)
        {
            _content.Clear();
            UIX.Text(_content, "Spielstand " + slot + " überschreiben?", "h2");
            UIX.Text(_content, sum.Brand + " (Tag " + sum.Day + ", " + Fmt.Money(sum.Money) + ") wird beim ersten Speichern ersetzt.", "muted");
            MenuButton("Ja, neu beginnen", () => OnNewGame?.Invoke(_pendingMode, slot), "accent", null, "check");
            MenuButton("Abbrechen", () => PickSlot(_pendingMode), "ghost", null, "close");
        }

        private void ShowSlots()
        {
            _content.Clear();
            UIX.Text(_content, "Spielstand laden", "h2");
            var sim = Game.Sim;
            for (int s = 1; s <= Sim.SaveSlots; s++)
            {
                int slot = s;
                var sum = sim.ReadSummary(s);
                var card = UIX.Col(_content, 6f, "slot");
                if (sum == null)
                {
                    card.AddToClassList("slot-empty");
                    UIX.Text(card, "Spielstand " + s, "h3");
                    UIX.Text(card, "Leer", "muted");
                    continue;
                }
                UIX.Text(card, "Spielstand " + s + " · " + sum.Brand, "h3");
                var when = DateTimeOffset.FromUnixTimeSeconds(sum.SavedUnix).ToLocalTime();
                UIX.Text(card, "Tag " + sum.Day + " · " + Fmt.Money(sum.Money) + " · Level " + sum.Level + " · gespeichert " + when.ToString("dd.MM. HH:mm"), "muted");
                var row = UIX.Row(card, 8f);
                UIX.Button(row, "Laden", () => OnLoad?.Invoke(slot), "accent", false, "play");
                UIX.Button(row, "Löschen", () => ConfirmDelete(slot, sum), "danger", false, "close");
            }
            MenuButton("Zurück", ShowMain, "ghost", null, "chevron");
        }

        private void ConfirmDelete(int slot, SaveSummary sum)
        {
            _content.Clear();
            UIX.Text(_content, "Spielstand " + slot + " löschen?", "h2");
            UIX.Text(_content, sum.Brand + " (Tag " + sum.Day + ") ist danach weg. Das lässt sich nicht rückgängig machen.", "muted");
            MenuButton("Endgültig löschen", () =>
            {
                Game.Sim.DeleteSave(slot);
                if (Game.Sim.HasAnySave()) ShowSlots();
                else ShowMain();
            }, "danger", null, "close");
            MenuButton("Abbrechen", ShowSlots, "ghost", null, "chevron");
        }

        private void ShowSettings()
        {
            _content.Clear();
            var card = UIX.Col(_content, 6f, "card");
            SettingsView.Build(card, ShowMain);
        }

        private void ShowCredits()
        {
            _content.Clear();
            var card = UIX.Col(_content, 10f, "card");
            UIX.Text(card, "Credits", "h2");
            UIX.Text(card, "Idee & Game Design: du\nCode, 3D-Welt, Texturen, Sounds & Musik: Claude\nEngine: Unity 6 (URP, UI Toolkit)\n\n" +
                           "Kein einziges Asset wurde importiert. Jedes Modell, jede Textur, jedes Symbol und jeder Ton entsteht beim Start aus Code.");
            UIX.Button(card, "Zurück", ShowMain, "accent");
        }
    }
}
