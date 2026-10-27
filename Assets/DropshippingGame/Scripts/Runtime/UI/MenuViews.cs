using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Hilfe: Tastenbelegung (Tastatur &amp; Controller) und der Spielablauf.</summary>
    public static class HelpView
    {
        public static void Build(VisualElement parent)
        {
            var keys = UIX.Card(parent);
            var kh = UIX.Row(keys, 8f);
            UIX.Icon(kh, "keyboard", 16f, Theme.Accent);
            UIX.Text(kh, "STEUERUNG", "eyebrow");
            UIX.Spacer(kh);
            UIX.Text(kh, "Controller wird automatisch erkannt", "small");
            var head = UIX.Row(keys, 10f, "help-row", "help-head");
            UIX.Text(head, "AKTION", "help-action");
            var hk = UIX.Row(head, 6f, "help-kb");
            UIX.Icon(hk, "mouse", 13f, Theme.Muted);
            UIX.Text(hk, "TASTATUR & MAUS");
            var hp = UIX.Row(head, 6f, "help-pad");
            UIX.Icon(hp, "gamepad", 13f, Theme.Muted);
            UIX.Text(hp, "CONTROLLER");
            int shown = 0;
            for (int i = 0; i < GameInput.HelpTable.Length; i++)
            {
                var r = GameInput.HelpTable[i];
                if (!DevTools.Enabled && r[0].StartsWith("Noclip", StringComparison.Ordinal)) continue; // Testfunktion
                var row = UIX.Row(keys, 10f, "help-row");
                if (shown++ % 2 == 0) row.AddToClassList("alt");
                UIX.Text(row, r[0], "help-action");
                var kb = UIX.Div(row, "help-kb");
                KeyCell(kb, r[1]);
                var pad = UIX.Div(row, "help-pad");
                KeyCell(pad, r[2]);
            }
            UIX.Text(keys, "PlayStation: Kreuz = A · Kreis = B · Quadrat = X · Dreieck = Y · Options = Start · Touchpad/Share = Select", "small").style.marginTop = 6;

            var loop = UIX.Card(parent);
            var lh = UIX.Row(loop, 8f);
            UIX.Icon(lh, "package", 16f, Theme.Accent);
            UIX.Text(lh, "SO LÄUFT DEIN BUSINESS", "eyebrow");
            string[] steps =
            {
                "Am <b>Laptop</b> (Werkbank in der Garage) bei <b>AllesExpress</b> Ware bestellen – Nachbestellen geht später auch per Handy.",
                "Die Kiste vom Lieferwagen am <b>Wareneingang</b> holen und ins passende <b>Regal</b> räumen.",
                "Im Laptop unter <b>Mein Shop</b> das Produkt online schalten und den Preis festlegen.",
                "Bestellungen erscheinen <b>oben rechts als Zettel</b> mit Countdown. Je schneller verschickt, desto besser die Bewertung.",
                "Artikel aus dem Regal nehmen und am <b>Packtisch</b> verpacken.",
                "Das Paket vom Packtisch nehmen und am <b>Labeldrucker</b> das Versandlabel drucken.",
                "Paket abgeben: <b>Packstation</b> in der Stadt oder <b>PaketBlitz-Filiale</b> (gratis) – oder bequem an der <b>Abholbox am Garagentor</b> (3 € pro Paket). Geld kassieren!",
            };
            for (int i = 0; i < steps.Length; i++)
            {
                var s = UIX.Div(loop, "help-step");
                UIX.Text(s, (i + 1).ToString(), "help-step-num");
                UIX.Text(s, steps[i], "help-step-text");
            }

            var grow = UIX.Card(parent);
            var gh = UIX.Row(grow, 8f);
            UIX.Icon(gh, "rocket", 16f, Theme.Accent);
            UIX.Text(gh, "HANDY, LAPTOP & WACHSTUM", "eyebrow");
            UIX.Text(grow, "Mit dem <b>Handy</b> (" + GameInput.KeyboardLabel("phone") + " / " + GameInput.PadLabel("phone") + ") siehst du Bestellzettel, beantwortest Nachrichten, bestellst mit einem Tipp nach und prüfst deinen Kontostand – die Welt läuft dabei weiter. " +
                                "Den Laptop gibt es an der Werkbank bzw. am Schreibtisch: AllesExpress, Mein Shop, PaketBlitz, TikTak, Revoluut, Firma und mehr. " +
                                "Der <b>Kompass</b> oben zeigt Garage, nächste Packstation, Post, Bank, Börse, MediaMarkd und Fressnix.", "help-step-text");
            UIX.Text(grow, "Gute Preise, schneller Versand und Qualität bringen gute Bewertungen und mehr Kundschaft. Mit Erfahrung steigt dein Firmenlevel und schaltet Produkte, " +
                           "Lagerhalle, Personal und mehr frei. Um 20 Uhr ist Feierabend – dann werden Miete, Löhne und Zinsen fällig. " +
                           "Unter 40 <b>Energie</b> wirst du langsamer (Schlaf, Bett, Kaffee helfen). Neue Funktionen erklärt dir Marvin per Nachricht im Handy.", "help-step-text");
            UIX.Text(grow, "<b>TikTok</b> (ab Level " + GameData.TikTokLevel + "): Handy › TikTak, Laptop › TikTak oder das Ringlicht am Schreibtisch. Produkt und Format wählen, dann filmst du 10–20 Sekunden selbst: " +
                           "Produkt mittig im 9:16-Rahmen, nah genug, ruhig, gutes Licht, einmal herumgehen, gleich zu Beginn eine Aktion (" + GameInput.KeyboardLabel("rec_action") + " / " + GameInput.PadLabel("rec_action") +
                           ") als Hook und Deko mit ins Bild. Danach Ergebnis ansehen und posten oder neu aufnehmen.", "help-step-text");
        }

        private static void KeyCell(VisualElement parent, string keys)
        {
            if (string.IsNullOrEmpty(keys) || keys == "–")
            {
                UIX.Text(parent, "–", "muted");
                return;
            }
            // Mehrwort-Beschreibungen ("Linker Stick", "Maus") als Text, Tasten als Kappen
            bool words = keys.Contains("Stick") || keys.Contains("Maus") || keys.Contains("klick") || keys.Contains("Mausrad") || keys.Contains("Leertaste");
            if (words && !keys.Contains("/")) UIX.Key(parent, keys);
            else if (words)
            {
                var r = UIX.Row(parent, 4f);
                foreach (var part in keys.Split('/'))
                {
                    if (r.childCount > 0) UIX.Text(r, "/", "keys-sep");
                    UIX.Key(r, part.Trim());
                }
            }
            else UIX.Keys(parent, keys);
        }
    }

    /// <summary>Credits mit allen Asset-Quellen aus Resources/credits.json (fehlt die Datei: freundlicher Hinweis).</summary>
    public static class CreditsView
    {
        private sealed class Entry
        {
            public string Category, Name, Author, License, LicenseUrl, Url, Files;
        }

        public static void Build(VisualElement parent)
        {
            var team = UIX.Card(parent);
            UIX.Text(team, "Dropshipping Simulator – Vom Imbiss zum Imperium", "h2");
            UIX.KV(team, "Idee & Game Design", "du");
            UIX.KV(team, "Code, Spielwelt & Oberfläche", "Claude");
            UIX.KV(team, "Engine", "Unity 6 (URP, UI Toolkit)");

            List<Entry> entries = null;
            string error = null;
            try
            {
                entries = Load();
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            if (entries == null || entries.Count == 0)
            {
                var c = UIX.Card(parent);
                UIX.Empty(c, "heart", "Noch keine Asset-Liste",
                    error != null ? "credits.json konnte nicht gelesen werden (" + error + ")." :
                        "Resources/credits.json wurde nicht gefunden. Alles, was ohne importierte Assets auskommt, entsteht beim Start per Code.");
                return;
            }
            UIX.Text(parent, entries.Count + " Assets aus freien Quellen (CC0, CC-BY, SIL OFL). Danke an alle Urheberinnen und Urheber!", "muted").style.marginTop = 4;
            var order = new List<string>();
            var groups = new Dictionary<string, List<Entry>>();
            foreach (var e in entries)
            {
                string cat = string.IsNullOrEmpty(e.Category) ? "Sonstiges" : e.Category;
                if (!groups.ContainsKey(cat))
                {
                    groups[cat] = new List<Entry>();
                    order.Add(cat);
                }
                groups[cat].Add(e);
            }
            foreach (var cat in order)
            {
                UIX.Text(parent, cat.ToUpperInvariant(), "credit-cat");
                foreach (var e in groups[cat]) Item(parent, e);
            }
        }

        private static void Item(VisualElement parent, Entry e)
        {
            var c = UIX.Col(parent, 3f, "credit-item");
            var h = UIX.Row(c, 8f);
            var n = UIX.Text(h, string.IsNullOrEmpty(e.Name) ? "Unbenannt" : e.Name, "credit-name");
            n.style.flexShrink = 1;
            UIX.Spacer(h);
            if (!string.IsNullOrEmpty(e.License)) UIX.Chip(h, e.License, null, null, "license-pill");
            if (!string.IsNullOrEmpty(e.Author)) UIX.Text(c, "von " + e.Author, "credit-meta");
            if (!string.IsNullOrEmpty(e.Url)) UIX.Text(c, e.Url, "credit-meta");
            if (!string.IsNullOrEmpty(e.Files)) UIX.Text(c, "Dateien: " + e.Files, "credit-meta");
            bool url = IsWeb(e.Url), lic = IsWeb(e.LicenseUrl);
            if (url || lic)
            {
                var r = UIX.Row(c, 6f);
                r.style.marginTop = 4;
                if (url) UIX.Button(r, "Quelle", () => Application.OpenURL(e.Url), "ghost", false, "link").AddToClassList("btn-sm");
                if (lic) UIX.Button(r, "Lizenz", () => Application.OpenURL(e.LicenseUrl), "ghost", false, "paper").AddToClassList("btn-sm");
            }
        }

        private static bool IsWeb(string s) => !string.IsNullOrEmpty(s) && (s.StartsWith("http://") || s.StartsWith("https://"));

        private static List<Entry> Load()
        {
            var ta = Resources.Load<TextAsset>("credits");
            if (ta == null || string.IsNullOrEmpty(ta.text)) return null;
            if (!Json.TryParse(ta.text, out object root)) throw new Exception("ungültiges JSON");
            List<object> list = root as List<object>;
            if (list == null && root is Dictionary<string, object> d)
            {
                foreach (var key in new[] { "credits", "assets", "items", "entries", "sources" })
                {
                    if (d.TryGetValue(key, out object v) && v is List<object> l)
                    {
                        list = l;
                        break;
                    }
                }
            }
            if (list == null) return null;
            var result = new List<Entry>();
            foreach (var o in list)
            {
                if (!(o is Dictionary<string, object> e)) continue;
                result.Add(new Entry
                {
                    Category = Str(e, "category"),
                    Name = Str(e, "name"),
                    Author = Str(e, "author"),
                    License = Str(e, "license"),
                    LicenseUrl = Str(e, "licenseUrl", "license_url"),
                    Url = Str(e, "url", "source"),
                    Files = Str(e, "files"),
                });
            }
            return result;
        }

        private static string Str(Dictionary<string, object> d, string key, string alt = null)
        {
            if (!d.TryGetValue(key, out object v) && (alt == null || !d.TryGetValue(alt, out v))) return "";
            if (v == null) return "";
            if (v is string s) return s;
            if (v is List<object> l)
            {
                var parts = new List<string>();
                foreach (var x in l)
                    if (x != null)
                        parts.Add(x.ToString());
                return string.Join(", ", parts);
            }
            return v.ToString();
        }
    }

    /// <summary>Versionsnummer für Menüs (echte Build-Version aus <see cref="BuildInfo"/>).</summary>
    public static class GameVersion
    {
        public static string Text
        {
            get
            {
                try
                {
                    return "v" + BuildInfo.Version;
                }
                catch (Exception)
                {
                    return "v" + Application.version;
                }
            }
        }

        public static string Long
        {
            get
            {
                try
                {
                    return BuildInfo.Short;
                }
                catch (Exception)
                {
                    return Application.version;
                }
            }
        }
    }

    /// <summary>
    /// Pausemenü (Esc / Start): Weiterspielen, Speichern, Einstellungen, Hilfe, Hauptmenü, Speichern &amp; Beenden.
    /// Hauptmenü und Beenden fragen nach (mit Hinweis, ob gespeichert wird).
    /// </summary>
    public sealed class PauseView
    {
        private readonly VisualElement _layer;
        private readonly VisualElement _card;
        private string _page = "";
        private ScrollView _scroll;

        public bool IsOpen { get; private set; }
        public Action OnMainMenu, OnQuit, OnHelp;
        public VisualElement Card => _card;
        /// <summary>Gerade die Einstellungen offen (LB/RB wechseln dann die Reiter)?</summary>
        public bool InSettings => IsOpen && _page == "settings";

        public PauseView(VisualElement layer)
        {
            _layer = layer;
            var dim = UIX.Div(_layer, "layer", "dim");
            _card = UIX.Col(dim, 0f, "modal-card", "pause-card");
            UIX.Show(_layer, false);
        }

        private static bool CanSave(Sim sim) => sim != null && sim.StoryStage == "business" && !sim.DayOver;

        private void ShowMenu()
        {
            _page = "menu";
            _scroll = null;
            _card.Clear();
            _card.style.width = 440;
            var sim = Game.Sim;
            var head = UIX.Col(_card, 2f, "pause-head");
            UIX.Text(head, "PAUSE", "eyebrow");
            UIX.Text(head, sim != null ? sim.BrandName : "Pause", "h1").style.unityTextAlign = TextAnchor.MiddleCenter;
            if (sim != null)
            {
                string meta = sim.StoryStage == "business" ? UiFmt.DayLong(sim.Day) + " · " + Fmt.Clock(sim.TimeMinutes) + " Uhr · Spielstand " + sim.Slot : "Kalles Imbiss · Schicht läuft";
                UIX.Text(head, meta, "pause-meta");
                if (sim.LastSavedUnix > 0)
                {
                    long ago = Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - sim.LastSavedUnix);
                    string when = ago < 60 ? "gerade eben" : (ago < 3600 ? "vor " + (ago / 60) + " min" : "vor " + (ago / 3600) + " h");
                    UIX.Text(head, "Zuletzt gespeichert " + when, "pause-meta");
                }
            }
            var menu = UIX.Col(_card, 8f);
            menu.style.marginTop = 10;
            UIX.Button(menu, "Weiterspielen", Close, "accent", false, "play").AddToClassList("btn-big");
            UIX.Button(menu, "Spiel speichern", Save, "", !CanSave(sim), "save");
            UIX.Button(menu, "Einstellungen", ShowSettings, "", false, "gear");
            UIX.Button(menu, "Steuerung & Hilfe", ShowHelp, "", false, "help");
            UIX.Button(menu, "Feedback & Bug melden", ShowFeedback, "", false, "chat");
            if (DevTools.Enabled)
                UIX.Button(menu, "Admin-Panel (Test)", () =>
                {
                    Close();
                    Game.UI?.Admin?.Open();
                }, "", false, "bolt");
            UIX.Button(menu, "Zum Hauptmenü", () => Confirm("Zum Hauptmenü?", "home", OnMainMenu), "", false, "home");
            UIX.Button(menu, CanSave(sim) ? "Speichern & Beenden" : "Spiel beenden", () => Confirm("Spiel beenden?", "exit", OnQuit), "danger", false, "exit");
            UIX.Text(_card, "Version " + GameVersion.Long, "pause-version");
        }

        /// <summary>Rückfrage vor Hauptmenü/Beenden – mit Hinweis, ob gespeichert wird.</summary>
        private void Confirm(string title, string icon, Action action)
        {
            _page = "confirm";
            _scroll = null;
            _card.Clear();
            _card.style.width = 440;
            var sim = Game.Sim;
            var head = UIX.Col(_card, 4f, "pause-head");
            UIX.Icon(head, icon, 28f, Theme.Accent);
            UIX.Text(head, title, "h1").style.unityTextAlign = TextAnchor.MiddleCenter;
            string info = CanSave(sim)
                ? "Dein Spiel wird vorher automatisch gespeichert (Spielstand " + sim.Slot + ")."
                : (sim != null && sim.StoryStage == "diner" ? "Die Imbiss-Schicht wird nicht gespeichert – du fängst sie beim nächsten Mal neu an." : "Der Tag ist vorbei – gespeichert wird beim nächsten Tagesstart.");
            var l = UIX.Text(head, info, "pause-meta");
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            var menu = UIX.Col(_card, 8f);
            menu.style.marginTop = 12;
            UIX.Button(menu, "Abbrechen", ShowMenu, "", false, "arrow_left");
            UIX.Button(menu, "Ja, " + (icon == "exit" ? "beenden" : "zum Hauptmenü"), () => action?.Invoke(), icon == "exit" ? "danger" : "accent", false, "check");
            if (GameInput.UsingGamepad) _card.schedule.Execute(() => UIX.FocusFirst(_card));
        }

        private ScrollView Page(float width)
        {
            _card.Clear();
            _card.style.width = width;
            var sv = UIX.Scroll(_card);
            sv.style.maxHeight = 820;
            _scroll = sv;
            return sv;
        }

        private void ShowSettings()
        {
            _page = "settings";
            var sv = Page(760);
            var col = UIX.Col(sv.contentContainer, 10f);
            SettingsView.Build(col, ShowMenu);
        }

        private void ShowHelp()
        {
            _page = "help";
            var sv = Page(820);
            var col = UIX.Col(sv.contentContainer, 10f);
            var head = UIX.Row(col, 10f);
            UIX.Text(head, "Steuerung & Hilfe", "h1");
            UIX.Spacer(head);
            UIX.Button(head, "Zurück", ShowMenu, "ghost", false, "arrow_left");
            HelpView.Build(col);
        }

        private void ShowFeedback()
        {
            _page = "feedback";
            var sv = Page(720);
            BugReportView.Build(UIX.Col(sv.contentContainer, 10f), ShowMenu);
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
            ShowMenu();
        }

        /// <summary>Rechter Stick scrollt lange Seiten (Einstellungen, Hilfe).</summary>
        public void Tick(float dt)
        {
            if (IsOpen && _scroll != null) UIX.PadScroll(_scroll, dt);
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
            InputBindings.CancelCapture();
            IsOpen = false;
            _page = "";
            _scroll = null;
            UIX.Show(_layer, false);
            Game.Root?.Lock("pause", false);
            Game.Root?.SetPaused("pause", false);
        }

        /// <summary>Unterseite direkt öffnen ("menu", "settings", "help", "feedback") – für den Selbsttest.</summary>
        public void ShowPage(string page)
        {
            if (!IsOpen) Open();
            switch (page)
            {
                case "settings": ShowSettings(); break;
                case "help": ShowHelp(); break;
                case "feedback": ShowFeedback(); break;
                default: ShowMenu(); break;
            }
        }

        /// <summary>Esc im Pausemenü: aus Unterseiten zurück, sonst schließen.</summary>
        public void Back()
        {
            if (_page == "settings" || _page == "help" || _page == "feedback" || _page == "confirm") ShowMenu();
            else Close();
        }
    }

    /// <summary>
    /// Hauptmenü: die Spielwelt in der Abenddämmerung als Kulisse, links Logo und Menü
    /// (Fortsetzen, Neues Spiel mit Spielstand-Karten, Laden, Einstellungen, Hilfe, Credits,
    /// Beenden), rechts das Versions-Panel („Neu in Version 1.0“).
    /// </summary>
    public sealed class MainMenuView
    {
        private readonly VisualElement _layer;
        private VisualElement _panel, _content, _whatsNew;
        private ScrollView _scroll;
        private string _page = "main";
        public Action<string, int> OnNewGame;
        public Action<int> OnLoad;
        public Action OnQuit;
        private string _pendingMode = "intro";

        public VisualElement Content => _content;

        public MainMenuView(VisualElement layer)
        {
            _layer = layer;
            Build();
            UIX.Show(_layer, false);
        }

        private void Build()
        {
            var shade = UIX.Div(_layer, "menu-shade");
            shade.style.backgroundImage = new StyleBackground(UiTex.Shade(true));
            shade.pickingMode = PickingMode.Ignore;
            var shadeR = UIX.Div(_layer, "menu-shade-right");
            shadeR.style.backgroundImage = new StyleBackground(UiTex.Shade(false));
            shadeR.pickingMode = PickingMode.Ignore;

            _panel = UIX.Col(_layer, 0f, "menu-panel");
            UIX.Text(_panel, "HUSTLE GAMES PRÄSENTIERT", "wordmark-eyebrow");
            UIX.Text(_panel, "DROPSHIPPING", "wordmark");
            var wr = UIX.Div(_panel, "wordmark-row");
            var pill = UIX.Div(wr, "wordmark-pill");
            UIX.Text(pill, "SIMULATOR", "wordmark-pill-text", "display");
            var ver = UIX.Div(wr, "version-pill");
            UIX.Text(ver, GameVersion.Text, "version-pill-text", "bold");
            UIX.Text(_panel, "Vom Imbiss zum Imperium.", "menu-tagline");
            _scroll = UIX.Scroll(_panel, "menu-content");
            _content = UIX.Col(_scroll.contentContainer, 10f);

            _whatsNew = UIX.Col(_layer, 0f, "whatsnew");
            BuildWhatsNew();
            UIX.Text(_layer, "Version " + GameVersion.Long + " · Asset-Quellen unter Credits", "menu-version").pickingMode = PickingMode.Ignore;
        }

        private void BuildWhatsNew()
        {
            _whatsNew.Clear();
            var h = UIX.Col(_whatsNew, 0f, "wn-head");
            UIX.Text(h, WhatsNew.Eyebrow, "eyebrow").style.color = Theme.Accent;
            UIX.Text(h, WhatsNew.Title, "wn-title", "display");
            var sv = UIX.Scroll(_whatsNew);
            sv.style.flexShrink = 1;
            foreach (var item in WhatsNew.Items)
            {
                if (item == null || item.Length < 3) continue;
                var r = UIX.Div(sv.contentContainer, "wn-item");
                var ic = UIX.Div(r, "wn-icon");
                UIX.Icon(ic, item[0], 17f);
                var col = UIX.Col(r, 0f);
                col.style.flexShrink = 1;
                UIX.Text(col, item[1].Replace("{phone}", GameInput.KeyboardLabel("phone")), "wn-item-title");
                UIX.Text(col, item[2], "wn-item-text");
            }
            foreach (var el in _whatsNew.Query<Label>().ToList()) el.pickingMode = PickingMode.Ignore;
        }

        public void Open()
        {
            UIX.Show(_layer, true);
            _layer.pickingMode = PickingMode.Ignore;
            ShowMain();
            UIX.FadeSlideIn(_panel, 0f, 0.3f, 0f, -24f);
            UIX.FadeSlideIn(_whatsNew, 0f, 0.3f, 0.08f, 24f);
        }

        public void Close() => UIX.Show(_layer, false);

        public bool IsOpen => _layer.style.display != DisplayStyle.None;
        /// <summary>Einstellungen offen (LB/RB wechseln die Reiter)?</summary>
        public bool InSettings => IsOpen && _page == "settings";

        public void Tick(float dt)
        {
            if (!IsOpen) return;
            UIX.PadScroll(_scroll, dt);
        }

        /// <summary>Esc / B: eine Seite zurück.</summary>
        public void Back()
        {
            switch (_page)
            {
                case "newgame":
                case "slots":
                case "settings":
                case "help":
                case "credits":
                    ShowMain();
                    Game.Sound("click", 0.05f, -8f);
                    break;
                case "pickslot":
                    ShowNewGame();
                    break;
                case "confirm_overwrite":
                    PickSlot(_pendingMode);
                    break;
                case "confirm_delete":
                    ShowSlots();
                    break;
                case "confirm_quit":
                    ShowMain();
                    break;
            }
        }

        private void SetPage(string page)
        {
            if (_page == "settings" && page != "settings") InputBindings.CancelCapture();
            _page = page;
            _content.Clear();
            _scroll.scrollOffset = Vector2.zero;
            UIX.Show(_whatsNew, page == "main");
            // Breite Seiten (Tabellen, Regler) bekommen mehr Platz – das Neuigkeiten-Panel ist dann ausgeblendet.
            _panel.style.width = page == "help" || page == "settings" || page == "credits" ? 860 : 560;
            UIX.FadeSlideIn(_content, 8f, 0.16f);
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
                inner.style.flexShrink = 1;
                inner.pickingMode = PickingMode.Ignore;
                var label = b.Q<Label>(className: "btn-label");
                if (label != null)
                {
                    label.RemoveFromHierarchy();
                    inner.Add(label);
                }
                var s = UIX.Text(inner, sub, "menu-btn-sub");
                s.pickingMode = PickingMode.Ignore;
                b.Add(inner);
            }
            return b;
        }

        private void PageTitle(string title, string sub = null)
        {
            UIX.Text(_content, title, "menu-page-title", "display");
            if (!string.IsNullOrEmpty(sub)) UIX.Text(_content, sub, "muted");
        }

        private static string SlotMeta(SaveSummary sum)
        {
            if (sum.Story == "diner") return "Imbiss-Schicht · noch kein Business";
            return UiFmt.DayLong(sum.Day) + " · Level " + sum.Level + " · " + Fmt.Money(sum.Money);
        }

        private static string SavedAt(SaveSummary sum)
        {
            if (sum.SavedUnix <= 0) return "";
            try
            {
                var when = DateTimeOffset.FromUnixTimeSeconds(sum.SavedUnix).ToLocalTime();
                return "gespeichert am " + when.ToString("dd.MM.") + " um " + when.ToString("HH:mm") + " Uhr";
            }
            catch (Exception)
            {
                return "";
            }
        }

        private void ShowMain()
        {
            SetPage("main");
            var sim = Game.Sim;
            int recent = 0;
            SaveSummary sum = null;
            try
            {
                recent = sim != null ? sim.MostRecentSlot() : 0;
                sum = recent > 0 ? sim.ReadSummary(recent) : null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            if (sum != null)
            {
                string brand = string.IsNullOrEmpty(sum.Brand) ? "Spielstand " + recent : sum.Brand;
                var card = UIX.PressRow(_content, 14f, () => OnLoad?.Invoke(recent), "slot-card", "continue-card");
                SlotThumb(card, recent, sum);
                var info = UIX.Col(card, 2f);
                info.style.flexGrow = 1;
                info.style.flexShrink = 1;
                UIX.Text(info, "FORTSETZEN", "eyebrow").style.color = Theme.Accent;
                UIX.Text(info, brand, "slot-title");
                UIX.Text(info, SlotMeta(sum), "slot-meta");
                string saved = SavedAt(sum);
                if (saved != "") UIX.Text(info, saved, "slot-meta");
                UIX.Icon(card, "play", 22f, Theme.Accent);
                UIX.PassThrough(card);
            }
            MenuButton("Neues Spiel", ShowNewGame, sum != null ? "" : "accent", "Drei Wege ins Business – mit oder ohne Imbiss-Story", "sparkle");
            bool anySave = false;
            try
            {
                anySave = sim != null && sim.HasAnySave();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            if (anySave) MenuButton("Spielstand laden", ShowSlots, "", null, "list");
            MenuButton("Einstellungen", ShowSettings, "", null, "gear");
            MenuButton("Steuerung & Hilfe", ShowHelp, "", null, "help");
            MenuButton("Credits & Quellen", ShowCredits, "", null, "heart");
            MenuButton("Beenden", ConfirmQuit, "", null, "exit");
        }

        private void ConfirmQuit()
        {
            SetPage("confirm_quit");
            PageTitle("Spiel beenden?", "Bis bald – deine Spielstände bleiben erhalten.");
            MenuButton("Ja, beenden", () => OnQuit?.Invoke(), "danger", null, "exit");
            MenuButton("Abbrechen", ShowMain, "ghost", null, "arrow_left");
        }

        /// <summary>Vorschaubild des Spielstands (beim Speichern aufgenommen) oder farbige Markenkarte.</summary>
        private static void SlotThumb(VisualElement card, int slot, SaveSummary sum)
        {
            var t = UIX.Div(card, "slot-thumb");
            Texture2D tex = null;
            try
            {
                tex = sum != null ? SaveThumbs.Load(slot) : null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            if (tex != null)
            {
                t.style.backgroundImage = new StyleBackground(tex);
                return;
            }
            t.AddToClassList("fallback");
            if (sum == null)
            {
                UIX.Icon(t, "plus", 26f, Theme.Muted);
                return;
            }
            string brand = string.IsNullOrEmpty(sum.Brand) ? "Spielstand " + slot : sum.Brand;
            var col = SaveThumbs.BrandColor(brand, slot);
            t.style.backgroundColor = col;
            var on = Theme.OnColor(col);
            UIX.Icon(t, sum.Story == "diner" ? "plate" : (sum.Stage >= 1 ? "building" : "home"), 22f, on);
            var l = UIX.Text(t, brand.Length > 14 ? brand.Substring(0, 13) + "…" : brand, "slot-thumb-brand");
            l.style.color = on;
        }

        private void ShowNewGame()
        {
            SetPage("newgame");
            PageTitle("Neues Spiel", "Wie willst du starten?");
            MenuButton("Mit Story starten (empfohlen)", () => PickDifficulty("intro"), "accent", "Beginne als Aushilfe in Kalles Imbiss. Inklusive Tutorial.", "fire");
            MenuButton("Direkt in die Garage", () => PickDifficulty("tutorial"), "", "Ohne Imbiss-Intro, mit Tutorial.", "home");
            MenuButton("Profi-Start", () => PickDifficulty("skip"), "", "Kein Intro, kein Tutorial. Du weißt, was du tust.", "bolt");
            MenuButton("Zurück", ShowMain, "ghost", null, "arrow_left");
        }

        /// <summary>Schwierigkeit wählen (Entspannt / Normal / Hart) – gilt für das nächste neue Spiel.</summary>
        private void PickDifficulty(string mode)
        {
            _pendingMode = mode;
            SetPage("difficulty");
            PageTitle("Schwierigkeit", "Wie hart soll das Hustler-Leben werden? Gilt für diesen Spielstand.");
            foreach (var d in GameData.Difficulties)
            {
                string id = d.Id;
                MenuButton(d.Name + (id == GameData.DefaultDifficulty ? " (empfohlen)" : ""), () =>
                {
                    if (Game.Sim != null) Game.Sim.NextDifficulty = id;
                    PickSlot(mode);
                }, id == GameData.DefaultDifficulty ? "accent" : "", d.Desc, d.Icon);
            }
            MenuButton("Zurück", ShowNewGame, "ghost", null, "arrow_left");
        }

        private void PickSlot(string mode)
        {
            _pendingMode = mode;
            SetPage("pickslot");
            PageTitle("Spielstand wählen", "In welchem Speicherplatz soll dein neues Spiel liegen?");
            var sim = Game.Sim;
            for (int s = 1; s <= Sim.SaveSlots; s++)
            {
                int slot = s;
                SaveSummary sum = null;
                try
                {
                    sum = sim != null ? sim.ReadSummary(s) : null;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                var captured = sum;
                var card = UIX.PressRow(_content, 0f, () =>
                {
                    if (captured == null) OnNewGame?.Invoke(_pendingMode, slot);
                    else ConfirmOverwrite(slot, captured);
                }, "slot-card");
                SlotBadge(card, slot, sum == null);
                SlotThumb(card, slot, sum);
                var info = UIX.Col(card, 1f);
                info.style.flexGrow = 1;
                info.style.flexShrink = 1;
                if (sum == null)
                {
                    UIX.Text(info, "Freier Platz", "slot-title");
                    UIX.Text(info, "Neues Spiel hier starten", "slot-meta");
                    UIX.Icon(card, "plus", 18f, Theme.Accent);
                }
                else
                {
                    UIX.Text(info, (string.IsNullOrEmpty(sum.Brand) ? "Spielstand " + slot : sum.Brand) + " überschreiben", "slot-title");
                    UIX.Text(info, SlotMeta(sum), "slot-meta");
                    UIX.Icon(card, "warning", 18f, Theme.Bad);
                }
                UIX.PassThrough(card);
            }
            MenuButton("Zurück", () => PickDifficulty(_pendingMode), "ghost", null, "arrow_left");
        }

        private static void SlotBadge(VisualElement card, int slot, bool empty)
        {
            var n = UIX.Div(card, "slot-num");
            UIX.Text(n, slot.ToString(), "slot-num-text", "display");
            if (empty) card.AddToClassList("slot-empty");
        }

        private void ConfirmOverwrite(int slot, SaveSummary sum)
        {
            SetPage("confirm_overwrite");
            PageTitle("Spielstand " + slot + " überschreiben?", (string.IsNullOrEmpty(sum.Brand) ? "Dieser Spielstand" : sum.Brand) + " (" + SlotMeta(sum) + ") wird beim ersten Speichern ersetzt.");
            MenuButton("Ja, neu beginnen", () => OnNewGame?.Invoke(_pendingMode, slot), "accent", null, "check");
            MenuButton("Abbrechen", () => PickSlot(_pendingMode), "ghost", null, "close");
        }

        private void ShowSlots()
        {
            SetPage("slots");
            PageTitle("Spielstand laden");
            var sim = Game.Sim;
            for (int s = 1; s <= Sim.SaveSlots; s++)
            {
                int slot = s;
                SaveSummary sum = null;
                try
                {
                    sum = sim != null ? sim.ReadSummary(s) : null;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                var card = UIX.Row(_content, 0f, "slot-card");
                SlotBadge(card, slot, sum == null);
                SlotThumb(card, slot, sum);
                var info = UIX.Col(card, 1f);
                info.style.flexGrow = 1;
                info.style.flexShrink = 1;
                if (sum == null)
                {
                    UIX.Text(info, "Leer", "slot-title");
                    UIX.Text(info, "Noch kein Spielstand in diesem Platz", "slot-meta");
                    continue;
                }
                UIX.Text(info, string.IsNullOrEmpty(sum.Brand) ? "Spielstand " + slot : sum.Brand, "slot-title");
                UIX.Text(info, SlotMeta(sum), "slot-meta");
                string saved = SavedAt(sum);
                if (saved != "") UIX.Text(info, saved, "slot-meta");
                var captured = sum;
                var actions = UIX.Row(card, 6f);
                UIX.Button(actions, "Laden", () => OnLoad?.Invoke(slot), "accent", false, "play");
                UIX.Button(actions, "", () => ConfirmDelete(slot, captured), "danger", false, "close").tooltip = Loc.Tr("Löschen");
            }
            MenuButton("Zurück", ShowMain, "ghost", null, "arrow_left");
        }

        private void ConfirmDelete(int slot, SaveSummary sum)
        {
            SetPage("confirm_delete");
            PageTitle("Spielstand " + slot + " löschen?", (string.IsNullOrEmpty(sum.Brand) ? "Dieser Spielstand" : sum.Brand) + " ist danach weg. Das lässt sich nicht rückgängig machen.");
            MenuButton("Endgültig löschen", () =>
            {
                var sim = Game.Sim;
                if (sim != null)
                {
                    try
                    {
                        sim.DeleteSave(slot);
                        SaveThumbs.Delete(slot);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                if (sim != null && sim.HasAnySave()) ShowSlots();
                else ShowMain();
            }, "danger", null, "close");
            MenuButton("Abbrechen", ShowSlots, "ghost", null, "arrow_left");
        }

        private void ShowSettings()
        {
            SetPage("settings");
            var col = UIX.Col(_content, 10f);
            SettingsView.Build(col, ShowMain);
        }

        private void ShowHelp()
        {
            SetPage("help");
            var head = UIX.Row(_content, 10f);
            UIX.Text(head, "Steuerung & Hilfe", "menu-page-title", "display");
            UIX.Spacer(head);
            UIX.Button(head, "Zurück", ShowMain, "ghost", false, "arrow_left");
            HelpView.Build(_content);
        }

        private void ShowCredits()
        {
            SetPage("credits");
            var head = UIX.Row(_content, 10f);
            UIX.Text(head, "Credits & Quellen", "menu-page-title", "display");
            UIX.Spacer(head);
            UIX.Button(head, "Zurück", ShowMain, "ghost", false, "arrow_left");
            CreditsView.Build(_content);
        }
    }
}
