using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// "HustleOS" – der Laptop am Schreibtisch. Standard-Design „Hype“ (dunkel), alternativ
    /// „Frachtbrief“, jeweils hell oder dunkel. Kopfzeile mit Marke, Tag, Bewertung und Konto,
    /// Navigation mit den Apps, darunter die App mit ihren Reitern. Kurze Boot-Animation beim
    /// ersten Öffnen, weiche Übergänge beim App-Wechsel.
    ///
    /// Erweiterungspunkte (Phase B):
    /// - <see cref="Register"/> fügt eine App hinzu, <see cref="RegisterTab"/> einen Reiter.
    /// - Die Platzhalter „Aufträge“, „Retouren“, „Trends“ und „Skills“ sind schon angelegt
    ///   (Visible = <see cref="ShowUpcoming"/>); Phase B ersetzt Make/Visible.
    /// - <see cref="Aliases"/>: alte App-IDs ("brand", "bank" …) zeigen auf App/Reiter.
    /// </summary>
    public sealed class LaptopView
    {
        public sealed class TabDef
        {
            public string Id, Title, Icon;
            public Func<LaptopApp> Make;
            /// <summary>Sperrgrund ("" = frei). Gesperrte Reiter zeigen eine Erklärung.</summary>
            public Func<string> Locked;
            public Func<bool> Visible;
            public Func<string> Badge;
        }

        public sealed class AppDef
        {
            public string Id, Title, Icon, Group;
            /// <summary>Für Apps ohne Reiter.</summary>
            public Func<LaptopApp> Make;
            /// <summary>Für Apps mit Reitern (Reihenfolge = Anzeige).</summary>
            public List<TabDef> Tabs;
            public Func<string> Locked;
            public Func<bool> Visible;
            public Func<string> Badge;
        }

        /// <summary>true = Platzhalter für Phase-B-Apps sichtbar („bald verfügbar“).</summary>
        public static bool ShowUpcoming;

        public static readonly List<AppDef> Apps = CreateApps();

        /// <summary>Alte/kurze IDs → "app" oder "app/reiter".</summary>
        public static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            { "pack", "buy/pack" }, { "packaging", "buy/pack" }, { "ware", "buy/ware" },
            { "webshop", "shop/webshop" }, { "marketing", "shop/marketing" }, { "brand", "shop/brand" }, { "branding", "shop/brand" },
            { "trends", "market/trends" }, { "analysis", "market/analysis" },
            { "bank", "finance/bank" }, { "stats", "finance/stats" }, { "analytics", "finance/stats" }, { "trading", "finance/trading" },
            { "staff", "company/team" }, { "team", "company/team" }, { "build", "company/build" }, { "goals", "company/goals" },
            { "lifestyle", "company/lifestyle" }, { "skills", "company/skills" },
        };

        private static List<AppDef> CreateApps()
        {
            return new List<AppDef>
            {
                new AppDef { Id = "home", Title = "Übersicht", Icon = "home", Group = "BETRIEB", Make = () => new AppHome() },
                new AppDef
                {
                    Id = "mail", Title = "Postfach", Icon = "mail", Group = "BETRIEB", Make = () => new AppMail(),
                    Badge = () => Game.Sim != null && Game.Sim.Events.UnreadCount() > 0 ? Game.Sim.Events.UnreadCount().ToString() : "",
                },
                new AppDef
                {
                    Id = "buy", Title = "Einkauf", Icon = "cart", Group = "BETRIEB",
                    Tabs = new List<TabDef>
                    {
                        new TabDef { Id = "ware", Title = "Ware", Icon = "boxes", Make = () => new AppBuy() },
                        new TabDef { Id = "pack", Title = "Verpackung", Icon = "package", Make = () => new AppPackaging() },
                    },
                },
                new AppDef
                {
                    Id = "returns", Title = "Retouren", Icon = "return", Group = "BETRIEB", Visible = () => ShowUpcoming,
                    Make = () => new AppSoon("Retouren", "return", "Zurückgeschickte Pakete landen bald am Retourenplatz: als B-Ware einlagern oder entsorgen."),
                },
                new AppDef
                {
                    Id = "shop", Title = "Shop", Icon = "store", Group = "VERKAUF",
                    Badge = () => Game.Sim != null && Game.Sim.TikTokAvailable() ? "!" : "",
                    Tabs = new List<TabDef>
                    {
                        new TabDef { Id = "webshop", Title = "Webshop", Icon = "globe", Make = () => new AppShop() },
                        new TabDef
                        {
                            Id = "marketing", Title = "Marketing", Icon = "mega", Make = () => new AppMarketing(),
                            Badge = () => Game.Sim != null && Game.Sim.TikTokAvailable() ? "!" : "",
                        },
                        new TabDef { Id = "brand", Title = "Branding", Icon = "tag", Make = () => new AppBranding() },
                    },
                },
                new AppDef
                {
                    Id = "market", Title = "Markt & Trends", Icon = "trend", Group = "VERKAUF",
                    Tabs = new List<TabDef>
                    {
                        new TabDef { Id = "analysis", Title = "Marktanalyse", Icon = "store", Make = () => new AppMarket() },
                        new TabDef
                        {
                            Id = "trends", Title = "Trendradar", Icon = "fire", Visible = () => ShowUpcoming,
                            Make = () => new AppSoon("Trendradar", "fire", "Hype-Werte, Zyklen und Prognosen für jedes Produkt – bald hier."),
                        },
                    },
                },
                new AppDef
                {
                    Id = "orders", Title = "Aufträge", Icon = "pallet", Group = "VERKAUF", Visible = () => ShowUpcoming,
                    Make = () => new AppSoon("Großaufträge", "pallet", "Firmen bestellen palettenweise. Angebote annehmen, am Palettenplatz erfüllen, Frist einhalten."),
                },
                new AppDef
                {
                    Id = "finance", Title = "Finanzen", Icon = "bank", Group = "FINANZEN",
                    Tabs = new List<TabDef>
                    {
                        new TabDef { Id = "bank", Title = "Bank", Icon = "bank", Make = () => new AppBank() },
                        new TabDef { Id = "stats", Title = "Analytics", Icon = "bars", Make = () => new AppAnalytics() },
                        new TabDef { Id = "trading", Title = "Trading", Icon = "trend", Make = () => new AppTrading(), Locked = () => LockedReason("trading") },
                    },
                },
                new AppDef
                {
                    Id = "company", Title = "Firma", Icon = "building", Group = "FIRMA",
                    Tabs = new List<TabDef>
                    {
                        new TabDef { Id = "team", Title = "Team", Icon = "users", Make = () => new AppStaff(), Locked = () => LockedReason("staff") },
                        new TabDef
                        {
                            Id = "skills", Title = "Skills", Icon = "sparkle", Visible = () => ShowUpcoming,
                            Make = () => new AppSoon("Hustle-Skills", "sparkle", "Skillpunkte in Logistik, Vertrieb und Marketing – bald hier."),
                        },
                        new TabDef { Id = "build", Title = "Ausbau", Icon = "building", Make = () => new AppBuild() },
                        new TabDef { Id = "goals", Title = "Ziele", Icon = "trophy", Make = () => new AppGoals() },
                        new TabDef { Id = "lifestyle", Title = "Lifestyle", Icon = "crown", Make = () => new AppLifestyle() },
                    },
                },
            };
        }

        /// <summary>App hinzufügen oder ersetzen (gleiche Id). after = Id, hinter der sie erscheint.</summary>
        public static void Register(AppDef def, string after = null)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return;
            int existing = Apps.FindIndex(a => a.Id == def.Id);
            if (existing >= 0)
            {
                Apps[existing] = def;
                return;
            }
            int idx = after != null ? Apps.FindIndex(a => a.Id == after) : -1;
            if (idx >= 0) Apps.Insert(idx + 1, def);
            else Apps.Add(def);
        }

        /// <summary>Reiter zu einer App hinzufügen oder ersetzen (gleiche Id).</summary>
        public static void RegisterTab(string appId, TabDef tab, string after = null)
        {
            var app = Apps.Find(a => a.Id == appId);
            if (app == null || tab == null || string.IsNullOrEmpty(tab.Id)) return;
            if (app.Tabs == null) app.Tabs = new List<TabDef>();
            int existing = app.Tabs.FindIndex(t => t.Id == tab.Id);
            if (existing >= 0)
            {
                app.Tabs[existing] = tab;
                return;
            }
            int idx = after != null ? app.Tabs.FindIndex(t => t.Id == after) : -1;
            if (idx >= 0) app.Tabs.Insert(idx + 1, tab);
            else app.Tabs.Add(tab);
        }

        /// <summary>Sperrgründe (alte IDs bleiben gültig: "trading", "staff").</summary>
        public static string LockedReason(string id)
        {
            var sim = Game.Sim;
            if (sim == null) return "";
            switch (id)
            {
                case "trading":
                    if (sim.Level < GameData.TradingLevel) return "Ab Firmenlevel " + GameData.TradingLevel;
                    break;
                case "staff":
                    if (sim.LocationStage < 1) return "Braucht die Lagerhalle";
                    if (sim.Level < GameData.StaffLevel) return "Ab Firmenlevel " + GameData.StaffLevel;
                    break;
            }
            return "";
        }

        private static bool Call(Func<bool> f, bool fallback)
        {
            if (f == null) return fallback;
            try
            {
                return f();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return fallback;
            }
        }

        private static string Call(Func<string> f)
        {
            if (f == null) return "";
            try
            {
                return f() ?? "";
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return "";
            }
        }

        private static List<AppDef> VisibleApps()
        {
            var list = new List<AppDef>();
            foreach (var a in Apps)
                if (a != null && Call(a.Visible, true))
                    list.Add(a);
            return list;
        }

        private static List<TabDef> VisibleTabs(AppDef app)
        {
            var list = new List<TabDef>();
            if (app?.Tabs == null) return list;
            foreach (var t in app.Tabs)
                if (t != null && Call(t.Visible, true))
                    list.Add(t);
            return list;
        }

        // =====================================================================================
        // Instanz
        // =====================================================================================
        private readonly VisualElement _layer;
        private VisualElement _laptop, _frame, _screen, _top, _body, _navHost, _pageHost, _appRoot, _boot, _foot;
        private ScrollView _content, _nav;
        private Label _brand, _day, _rating, _money;
        private VisualElement _moneyChip;
        private readonly Dictionary<string, VisualElement> _navItems = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, Label> _navBadges = new Dictionary<string, Label>();
        private readonly Dictionary<string, string> _lastTab = new Dictionary<string, string>();
        private LaptopApp _app;
        private string _current = "home", _currentTab = "";
        private bool _dirty;
        private float _dirtyT, _topT;
        private string _themeSig = "", _badgeSig = "", _footSig = "";
        private bool _booted;
        private float _pressedAt = -10f;

        public bool IsOpen { get; private set; }
        public string Current => _current;
        public string CurrentTab => _currentTab;
        /// <summary>Bildschirm des Laptops.</summary>
        public VisualElement Screen => _screen;
        /// <summary>Bereich für die Controller-Fokus-Steuerung (App-Seite ohne Kopfzeile/Navigation).</summary>
        public VisualElement FocusRoot => _pageHost;
        public bool Booting => _boot != null && UIX.IsShown(_boot);

        public LaptopView(VisualElement layer)
        {
            _layer = layer;
            UIX.Show(_layer, false);
            Settings.Changed += () =>
            {
                if (IsOpen) ApplyTheme(false);
            };
        }

        /// <summary>Neue Spielsitzung: beim nächsten Öffnen wieder hochfahren.</summary>
        public void ResetBoot()
        {
            _booted = false;
            _current = "home";
            _currentTab = "";
            _lastTab.Clear();
        }

        private void Build()
        {
            _layer.Clear();
            _laptop = UIX.Div(_layer, "layer", "laptop", "os-theme");
            _laptop.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.target == _laptop) Game.Sim?.ClosePc();
            });
            _frame = UIX.Col(_laptop, 0f, "os-frame");
            // Während eine Maustaste gedrückt ist, nicht neu bauen (sonst geht der Klick verloren).
            _frame.RegisterCallback<PointerDownEvent>(_ => _pressedAt = Time.unscaledTime, TrickleDown.TrickleDown);
            _frame.RegisterCallback<PointerUpEvent>(_ => _pressedAt = -10f, TrickleDown.TrickleDown);
            _screen = UIX.Col(_frame, 0f, "os-screen");
            var glow = UIX.Div(_screen, "os-glow");
            glow.style.position = Position.Absolute;
            glow.style.backgroundImage = new StyleBackground(UiTex.Radial());
            glow.pickingMode = PickingMode.Ignore;
            _top = UIX.Div(_screen, "os-top");
            _body = UIX.Div(_screen, "os-body");
            _navHost = UIX.Div(_body, "os-nav");
            _pageHost = UIX.Col(_body, 0f);
            _pageHost.style.flexGrow = 1;
            _pageHost.style.flexShrink = 1;
            _content = UIX.Scroll(_pageHost, "os-content");
            _foot = UIX.Row(_screen, 0f, "os-foot");
            _foot.style.position = Position.Absolute;
            _foot.pickingMode = PickingMode.Ignore;
            UIX.Show(_foot, false);
        }

        private void BuildTop()
        {
            _top.Clear();
            bool hype = Settings.LaptopDesign != "frachtbrief";
            var logo = UIX.Div(_top, "os-logo");
            var mark = UIX.Div(logo, "os-logo-mark");
            UIX.Icon(mark, hype ? "sparkle" : "package", hype ? 16f : 18f);
            UIX.Text(logo, hype ? "HustleOS" : "HUSTLEOS", "os-brand-text");
            if (hype) UIX.Spacer(_top);
            _brand = Chip("MARKE", null, "brand");
            _day = Chip("TAG", "calendar", "day");
            _rating = Chip("BEWERTUNG", "star", "rating");
            _money = Chip("KONTO", "wallet", "money");
            _moneyChip = _money.parent;
            var close = UIX.Button(_top, "", () => Game.Sim?.ClosePc(), "", false, "close");
            close.AddToClassList("os-close");
            close.tooltip = "Schließen (Esc)";
        }

        private Label Chip(string key, string icon, string cls)
        {
            var c = UIX.Div(_top, "os-chip", cls);
            c.pickingMode = PickingMode.Ignore;
            if (!string.IsNullOrEmpty(icon)) UIX.Icon(c, icon, 13f);
            UIX.Text(c, key, "os-chip-key");
            var v = UIX.Text(c, "", "os-chip-text");
            if (cls == "day" || cls == "money" || cls == "rating") Fonts.AddClass(v, "num-b");
            return v;
        }

        private void BuildNav()
        {
            _navHost.Clear();
            _navItems.Clear();
            _navBadges.Clear();
            bool hype = Settings.LaptopDesign != "frachtbrief";
            _nav = new ScrollView(hype ? ScrollViewMode.Horizontal : ScrollViewMode.Vertical);
            _nav.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _nav.verticalScrollerVisibility = hype ? ScrollerVisibility.Hidden : ScrollerVisibility.Auto;
            _nav.style.flexGrow = 1;
            _navHost.Add(_nav);
            var inner = UIX.Div(_nav.contentContainer, "os-nav-inner");
            string group = "";
            foreach (var a in VisibleApps())
            {
                if (!hype && a.Group != group)
                {
                    group = a.Group;
                    UIX.Text(inner, group, "os-group");
                }
                string id = a.Id;
                var nav = UIX.PressRow(inner, 0f, () => OpenApp(id), "os-nav-item");
                nav.focusable = false; // Controller: LB/RB wechselt die App
                UIX.Icon(nav, a.Icon, 16f);
                UIX.Text(nav, a.Title, "nav-label");
                var badge = UIX.Badge(nav, "");
                UIX.Show(badge, false);
                UIX.PassThrough(nav);
                _navItems[a.Id] = nav;
                _navBadges[a.Id] = badge;
            }
        }

        /// <summary>Design-Klassen setzen; bei geändertem Layout Navigation und Kopfzeile neu bauen.</summary>
        private void ApplyTheme(bool force)
        {
            string sig = Settings.LaptopDesign + Settings.LaptopDark;
            if (!force && sig == _themeSig) return;
            _themeSig = sig;
            bool hype = Settings.LaptopDesign != "frachtbrief";
            _laptop.EnableInClassList("fb", !hype);
            _laptop.EnableInClassList("hype", hype);
            _laptop.EnableInClassList("dark", Settings.LaptopDark);
            _laptop.EnableInClassList("light", !Settings.LaptopDark);
            BuildTop();
            BuildNav();
            _badgeSig = "";
            UpdateBadges();
            UpdateTop();
            if (_app != null) OpenApp(_current, _currentTab, true);
        }

        public void UpdateBadges()
        {
            var sim = Game.Sim;
            if (sim == null || _navItems.Count == 0) return;
            var sb = new System.Text.StringBuilder();
            foreach (var a in VisibleApps()) sb.Append(Call(a.Badge)).Append('|');
            sb.Append(_current);
            string sig = sb.ToString();
            if (sig == _badgeSig) return;
            _badgeSig = sig;
            foreach (var a in VisibleApps())
            {
                if (!_navItems.TryGetValue(a.Id, out var nav)) continue;
                nav.EnableInClassList("active", a.Id == _current);
                string b = Call(a.Badge);
                var badge = _navBadges[a.Id];
                badge.text = b;
                UIX.Show(badge, b != "");
                bool locked = Call(a.Locked) != "";
                nav.EnableInClassList("locked", locked);
            }
        }

        private void UpdateTop()
        {
            var sim = Game.Sim;
            if (sim == null || _money == null) return;
            _brand.text = sim.BrandName;
            _day.text = UiFmt.DayShort(sim.Day) + " · " + Fmt.Clock(sim.TimeMinutes);
            _rating.text = Fmt.Rating(sim.Reputation);
            _money.text = Fmt.Money(sim.Money);
            _moneyChip?.EnableInClassList("neg", sim.Money < 0);
        }

        // =====================================================================================
        // Öffnen / Schließen
        // =====================================================================================
        public void Open(string target = null)
        {
            if (_laptop == null) Build();
            IsOpen = true;
            UIX.Show(_layer, true);
            _layer.pickingMode = PickingMode.Position;
            ApplyTheme(true);
            var sim = Game.Sim;
            string app = target ?? _current;
            string tab = null;
            if (target == null && sim != null)
            {
                if (sim.TutorialStep == 1) app = "buy/ware";
                else if (sim.TutorialStep == 4) app = "shop/webshop";
                else if (sim.Events.PendingCount() > 0) app = "mail";
                else tab = _currentTab;
            }
            OpenApp(app, tab, true);
            UIX.PopIn(_frame, 0.16f);
            Game.Sound("whoosh", 0.05f, -6f);
            if (!_booted) Boot();
        }

        public void Close()
        {
            IsOpen = false;
            UIX.Show(_layer, false);
            try
            {
                _app?.Closed();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            _app = null;
        }

        /// <summary>Kurze Boot-Animation (nur beim ersten Öffnen pro Sitzung). Klick überspringt.</summary>
        private void Boot()
        {
            _booted = true;
            if (_boot != null) _boot.RemoveFromHierarchy();
            _boot = UIX.Col(_screen, 0f, "os-boot");
            _boot.style.position = Position.Absolute;
            var mark = UIX.Div(_boot, "boot-mark");
            UIX.Icon(mark, Settings.LaptopDesign != "frachtbrief" ? "sparkle" : "package", 34f);
            UIX.Text(_boot, "HustleOS", "boot-logo");
            string brand = Game.Sim != null ? Game.Sim.BrandName : "";
            UIX.Text(_boot, "Willkommen zurück" + (brand != "" ? ", " + brand : "") + ".", "boot-sub");
            var bar = UIX.Bar(_boot, 0f, null, 6f, 280f);
            bar.AddToClassList("boot-bar");
            var log = UIX.Text(_boot, "Starte …", "boot-log");
            string[] steps = { "Lade Bestellungen …", "Synchronisiere Lager …", "Verbinde mit PaketBlitz …", "Motivation wird geladen …" };
            var boot = _boot;
            boot.RegisterCallback<PointerDownEvent>(_ => FinishBoot(boot));
            Anim.Run(0.85f, t =>
            {
                UIX.SetBar(bar, t);
                log.text = steps[Mathf.Clamp((int)(t * steps.Length), 0, steps.Length - 1)];
            }, () => FinishBoot(boot), Ease.OutCubic, true, boot);
        }

        private void FinishBoot(VisualElement boot)
        {
            if (boot == null || boot.panel == null || boot != _boot) return;
            Anim.Stop(boot);
            Anim.Run(0.18f, t => boot.style.opacity = 1f - t, () =>
            {
                boot.RemoveFromHierarchy();
                if (_boot == boot) _boot = null;
            }, Ease.OutCubic, true);
            if (_appRoot != null) UIX.FadeSlideIn(_appRoot.parent, 10f, 0.2f);
        }

        /// <summary>App (und Reiter) öffnen. id darf "app", "app/reiter" oder ein alter Alias sein.</summary>
        public void OpenApp(string id, string tab = null, bool silent = false)
        {
            if (_content == null) return;
            if (string.IsNullOrEmpty(id)) id = "home";
            if (Aliases.TryGetValue(id, out string alias)) id = alias;
            int slash = id.IndexOf('/');
            if (slash > 0)
            {
                tab = id.Substring(slash + 1);
                id = id.Substring(0, slash);
            }
            var apps = VisibleApps();
            var def = apps.Find(a => a.Id == id) ?? apps[0];
            bool appChanged = def.Id != _current;
            var tabs = VisibleTabs(def);
            TabDef tabDef = null;
            if (tabs.Count > 0)
            {
                if (string.IsNullOrEmpty(tab) && _lastTab.TryGetValue(def.Id, out string last)) tab = last;
                tabDef = tabs.Find(t => t.Id == tab) ?? tabs[0];
                _lastTab[def.Id] = tabDef.Id;
            }
            bool tabChanged = (tabDef != null ? tabDef.Id : "") != _currentTab;
            _current = def.Id;
            _currentTab = tabDef != null ? tabDef.Id : "";
            _dirty = false;
            if (!silent) Game.Sound("click", 0.05f, -6f);
            try
            {
                _app?.Closed();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            _content.Clear();

            var page = UIX.Col(_content.contentContainer, 0f, "app-page");
            var head = UIX.Div(page, "app-head");
            UIX.Text(head, def.Title, "app-title");
            if (tabs.Count > 1)
            {
                var items = new List<UIX.TabItem>();
                foreach (var t in tabs)
                    items.Add(new UIX.TabItem { Id = t.Id, Title = t.Title, Icon = t.Icon, Badge = Call(t.Badge), Locked = Call(t.Locked) != "" });
                string appId = def.Id;
                UIX.Tabs(head, items, _currentTab, t => OpenApp(appId, t));
            }

            string title = tabDef != null && tabs.Count > 1 ? tabDef.Title : def.Title;
            string reason = tabDef != null ? Call(tabDef.Locked) : Call(def.Locked);
            LaptopApp app;
            try
            {
                if (reason != "") app = new AppLocked(title, reason, def.Id + "/" + _currentTab);
                else app = tabDef != null ? (tabDef.Make != null ? tabDef.Make() : null) : (def.Make != null ? def.Make() : null);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                app = null;
            }
            if (app == null) app = new AppSoon(title, def.Icon, "Diese App ist noch nicht fertig.");
            _app = app;
            _app.View = this;
            string lead = _app.Lead;
            if (!string.IsNullOrEmpty(lead)) UIX.Text(page, lead, "app-lead");
            else head.style.marginBottom = 14;
            _appRoot = UIX.Col(page, 14f);
            _app.Root = _appRoot;
            SafeBuild();
            _content.scrollOffset = Vector2.zero;
            _badgeSig = "";
            UpdateBadges();
            UpdateTop();
            if ((appChanged || tabChanged) && !silent) UIX.FadeSlideIn(page, 8f, 0.16f);
            if (GameInput.UsingGamepad) _content.schedule.Execute(() => UIX.FocusFirst(_appRoot.parent));
        }

        private void SafeBuild()
        {
            try
            {
                _app.Build();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _app.Root.Clear();
                UIX.Empty(_app.Root, "warning", "Diese App hatte einen Fehler.", "Einfach eine andere App öffnen und zurückkommen. (" + e.GetType().Name + ")");
            }
        }

        /// <summary>App neu aufbauen (Scrollposition und Controller-Fokus bleiben erhalten).</summary>
        public void Rebuild()
        {
            if (_app == null || _appRoot == null) return;
            var offset = _content.scrollOffset;
            int focus = GameInput.UsingGamepad ? UIX.FocusIndex(_pageHost) : -1;
            try
            {
                _app.Closed();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            _appRoot.Clear();
            SafeBuild();
            _content.schedule.Execute(() =>
            {
                _content.scrollOffset = offset;
                if (focus >= 0) UIX.RestoreFocus(_pageHost, focus);
            });
            UpdateBadges();
        }

        /// <summary>LB/RB: vorige/nächste App (bei Apps mit Reitern erst durch die Reiter).</summary>
        public void CycleApp(int dir)
        {
            var apps = VisibleApps();
            int idx = apps.FindIndex(a => a.Id == _current);
            if (idx < 0) idx = 0;
            var tabs = VisibleTabs(apps[idx]);
            int ti = tabs.FindIndex(t => t.Id == _currentTab);
            if (tabs.Count > 1 && ti >= 0 && ti + dir >= 0 && ti + dir < tabs.Count)
            {
                OpenApp(apps[idx].Id, tabs[ti + dir].Id);
                return;
            }
            int n = apps.Count;
            var next = apps[((idx + dir) % n + n) % n];
            var nextTabs = VisibleTabs(next);
            OpenApp(next.Id, nextTabs.Count > 0 ? (dir > 0 ? nextTabs[0].Id : nextTabs[nextTabs.Count - 1].Id) : null);
        }

        public void MarkDirty() => _dirty = true;

        public void Tick(float dt)
        {
            if (!IsOpen) return;
            _topT += dt;
            if (_topT >= 0.2f)
            {
                _topT = 0f;
                UpdateTop();
                UpdateBadges();
                UpdateFoot();
            }
            UIX.PadScroll(_content, dt);
            _dirtyT += dt;
            bool canRebuild = true;
            try
            {
                canRebuild = _app != null && _app.CanRebuild();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            bool pressed = Time.unscaledTime - _pressedAt < 1.5f;
            if (_dirty && _dirtyT > 0.25f && canRebuild && !pressed)
            {
                _dirty = false;
                _dirtyT = 0f;
                Rebuild();
            }
            else
            {
                try
                {
                    _app?.Tick(dt);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void UpdateFoot()
        {
            if (_foot == null) return;
            bool pad = GameInput.UsingGamepad;
            string sig = pad.ToString();
            if (sig == _footSig) return;
            _footSig = sig;
            _foot.Clear();
            UIX.Show(_foot, pad);
            if (!pad) return;
            UIX.KeyHint(_foot, "LB/RB", "App & Reiter");
            UIX.KeyHint(_foot, "A", "Auswählen");
            UIX.KeyHint(_foot, "B", "Schließen");
        }

        public void OnTradingTick()
        {
            if (IsOpen && _app is AppTrading t)
            {
                try
                {
                    t.LiveUpdate();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
