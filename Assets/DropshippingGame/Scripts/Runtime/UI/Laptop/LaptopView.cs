using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// "HustleOS" – der Laptop am Schreibtisch, als Web-Browser im Comic-Look: oben Reiter (jede
    /// App ein Tab), darunter Zurück/Start, Adressleiste und die wichtigsten Kennzahlen (Tag,
    /// Bewertung, Konto). Die Webseiten (AllesExpress, Mein Shop, iWolke Mail, PaketBlitz,
    /// TikTak, TradingViech, Revoluut) sind <see cref="WebApp"/>s mit eigenem Design
    /// (Resources/UI/Web.uss); die übrigen HustleOS-Seiten (Übersicht, Markt, Aufträge, Retouren,
    /// Firma) behalten ihr bisheriges Design „Hype“/„Frachtbrief“ samt Unter-Reitern.
    ///
    /// Erweiterungspunkte:
    /// - <see cref="Register"/> fügt eine App (einen Browser-Tab) hinzu, <see cref="RegisterTab"/> einen Unter-Reiter.
    /// - <see cref="Aliases"/>: alte App-IDs ("brand", "bank", "buy/ware" …) zeigen auf App/Reiter bzw. App/Route.
    /// - Web-Apps merken ihre Route (<see cref="GetRoute"/>); "app/route" öffnet direkt eine Unterseite.
    /// </summary>
    public sealed partial class LaptopView
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
            /// <summary>Kürzel und Farbe des Favicons im Browser-Tab.</summary>
            public string Fav = "";
            public Color FavColor = new Color(0.4f, 0.4f, 0.45f);
            /// <summary>Adresse für HustleOS-Seiten (Web-Apps liefern ihre eigene).</summary>
            public string Url = "";
        }

        /// <summary>true = Platzhalter/Apps auch dann zeigen, wenn sie noch nichts zu tun haben.</summary>
        public static bool ShowUpcoming;

        public static readonly List<AppDef> Apps = CreateApps();

        /// <summary>Markierung für „Markt & Trends“ nach einem Phasenwechsel (GameRoot setzt sie, Öffnen des Trendradars löscht sie).</summary>
        public static string TrendBadge = "";

        /// <summary>Alte/kurze IDs → "app", "app/reiter" oder "app/route".</summary>
        public static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            { "buy", "allesexpress" }, { "buy/ware", "allesexpress" }, { "ware", "allesexpress" }, { "einkauf", "allesexpress" },
            { "buy/pack", "allesexpress/pack" }, { "pack", "allesexpress/pack" }, { "packaging", "allesexpress/pack" },
            { "webshop", "shop" }, { "shop/webshop", "shop" },
            { "marketing", "tiktak" }, { "shop/marketing", "tiktak" }, { "tiktok", "tiktak" }, { "ads", "tiktak/ads" },
            { "brand", "company/brand" }, { "branding", "company/brand" }, { "shop/brand", "company/brand" },
            { "trends", "market/trends" }, { "analysis", "market/analysis" },
            { "finance", "bank" }, { "finance/bank", "bank" }, { "stats", "bank/analytics" }, { "analytics", "bank/analytics" },
            { "finance/stats", "bank/analytics" }, { "finance/trading", "trading" },
            { "staff", "company/team" }, { "team", "company/team" }, { "build", "company/build" }, { "goals", "company/goals" },
            { "lifestyle", "company/lifestyle" }, { "skills", "company/skills" }, { "weekly", "company/goals" }, { "challenges", "company/goals" },
            { "contracts", "orders/offers" }, { "b2b", "orders/offers" }, { "retouren", "returns" },
            { "shipping", "paket" }, { "versand", "paket" },
        };

        private static List<AppDef> CreateApps()
        {
            return new List<AppDef>
            {
                new AppDef
                {
                    Id = "home", Title = "Übersicht", Icon = "home", Group = "BETRIEB", Make = () => new AppHome(),
                    Fav = "H", FavColor = new Color(1f, 0.48f, 0.27f), Url = "hustle.os/start",
                },
                new AppDef
                {
                    Id = "allesexpress", Title = "AllesExpress", Icon = "cart", Group = "BETRIEB", Make = () => new WebAllesExpress(),
                    Fav = "A", FavColor = new Color(1f, 0.35f, 0.21f),
                },
                new AppDef
                {
                    Id = "shop", Title = "Mein Shop", Icon = "store", Group = "VERKAUF", Make = () => new WebShop(),
                    Fav = "S", FavColor = new Color(0.3f, 0.44f, 1f),
                },
                new AppDef
                {
                    Id = "mail", Title = "iWolke Mail", Icon = "mail", Group = "BETRIEB", Make = () => new WebMail(),
                    Fav = "@", FavColor = new Color(0.18f, 0.49f, 0.96f),
                    Badge = () => Game.Sim != null && Game.Sim.Events.UnreadCount() > 0 ? Game.Sim.Events.UnreadCount().ToString() : "",
                },
                new AppDef
                {
                    Id = "paket", Title = "PaketBlitz", Icon = "truck", Group = "BETRIEB", Make = () => new WebPaket(),
                    Fav = "P", FavColor = new Color(0.83f, 0.02f, 0.07f),
                    Badge = () => Game.Sim != null && Game.Sim.PendingCount() > 0 ? Game.Sim.PendingCount().ToString() : "",
                },
                new AppDef
                {
                    Id = "tiktak", Title = "TikTak", Icon = "music", Group = "VERKAUF", Make = () => new WebTikTak(),
                    Fav = "T", FavColor = new Color(0.1f, 0.1f, 0.12f),
                    Badge = () => Game.Sim != null && Game.Sim.TikTokAvailable() ? "!" : "",
                },
                new AppDef
                {
                    Id = "trading", Title = "TradingViech", Icon = "trend", Group = "FINANZEN", Make = () => new WebTrading(),
                    Fav = "V", FavColor = new Color(0.16f, 0.38f, 1f), Locked = () => LockedReason("trading"),
                },
                new AppDef
                {
                    Id = "bank", Title = "Revoluut", Icon = "bank", Group = "FINANZEN", Make = () => new WebBank(),
                    Fav = "R", FavColor = new Color(0.24f, 0.17f, 1f),
                },
                new AppDef
                {
                    Id = "market", Title = "Markt & Trends", Icon = "trend", Group = "VERKAUF", Fav = "M", FavColor = new Color(0.24f, 0.84f, 0.69f),
                    Url = "hustle.os/markt", Badge = () => TrendBadge,
                    Tabs = new List<TabDef>
                    {
                        new TabDef { Id = "analysis", Title = "Marktanalyse", Icon = "store", Make = () => new AppMarket() },
                        new TabDef
                        {
                            Id = "trends", Title = "Trendradar", Icon = "fire", Badge = () => TrendBadge,
                            Make = () => new AppTrends(),
                        },
                    },
                },
                new AppDef
                {
                    Id = "orders", Title = "Aufträge", Icon = "pallet", Group = "VERKAUF", Fav = "B", FavColor = new Color(0.55f, 0.4f, 0.9f),
                    Url = "hustle.os/grossauftraege",
                    Visible = () => ShowUpcoming || (Game.Sim != null && Game.Sim.ContractsUnlocked),
                    Badge = () => Game.Sim != null && Game.Sim.ContractOffers().Count > 0 ? Game.Sim.ContractOffers().Count.ToString() : "",
                    Tabs = new List<TabDef>
                    {
                        new TabDef
                        {
                            Id = "offers", Title = "Angebote", Icon = "inbox", Make = () => new AppContracts("offers"),
                            Badge = () => Game.Sim != null && Game.Sim.ContractOffers().Count > 0 ? Game.Sim.ContractOffers().Count.ToString() : "",
                        },
                        new TabDef { Id = "active", Title = "Laufend", Icon = "pallet", Make = () => new AppContracts("active") },
                        new TabDef { Id = "history", Title = "Verlauf", Icon = "list", Make = () => new AppContracts("history") },
                    },
                },
                new AppDef
                {
                    Id = "returns", Title = "Retouren", Icon = "return", Group = "BETRIEB", Fav = "R", FavColor = new Color(0.13f, 0.7f, 0.66f),
                    Url = "hustle.os/retouren",
                    Visible = () => ShowUpcoming || (Game.Sim != null && (Game.Sim.TotalReturns > 0 || Game.Sim.ReturnsIncoming.Count > 0 || Game.Sim.ReturnsAtDock > 0)),
                    Badge = () => Game.Sim != null && Game.Sim.ReturnsAtDock > 0 ? Game.Sim.ReturnsAtDock.ToString() : "",
                    Make = () => new AppReturns(),
                },
                new AppDef
                {
                    Id = "company", Title = "Firma", Icon = "building", Group = "FIRMA", Fav = "F", FavColor = new Color(0.23f, 0.13f, 0.25f),
                    Url = "hustle.os/firma",
                    Badge = () => Game.Sim != null && Game.Sim.SkillPointsAvailable() > 0 ? Game.Sim.SkillPointsAvailable().ToString() : "",
                    Tabs = new List<TabDef>
                    {
                        new TabDef { Id = "team", Title = "Team", Icon = "users", Make = () => new AppStaff(), Locked = () => LockedReason("staff") },
                        new TabDef
                        {
                            Id = "skills", Title = "Skills", Icon = "sparkle", Make = () => new AppSkills(),
                            Badge = () => Game.Sim != null && Game.Sim.SkillPointsAvailable() > 0 ? Game.Sim.SkillPointsAvailable().ToString() : "",
                        },
                        new TabDef { Id = "build", Title = "Ausbau", Icon = "building", Make = () => new AppBuild() },
                        new TabDef { Id = "goals", Title = "Ziele", Icon = "trophy", Make = () => new AppGoals() },
                        new TabDef { Id = "brand", Title = "Branding", Icon = "tag", Make = () => new AppBranding() },
                        new TabDef { Id = "pack", Title = "Verpackung", Icon = "package", Make = () => new AppPackaging() },
                        new TabDef { Id = "stats", Title = "Statistik", Icon = "bars", Make = () => new AppAnalytics() },
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

        // ---- Routen der Web-Apps (bleiben über das Schließen hinweg erhalten) --------------------
        private static readonly Dictionary<string, string> Routes = new Dictionary<string, string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Routes.Clear();

        public static string GetRoute(string appId) => appId != null && Routes.TryGetValue(appId, out string r) ? r ?? "" : "";

        public static void SetRoute(string appId, string route)
        {
            if (string.IsNullOrEmpty(appId)) return;
            Routes[appId] = route ?? "";
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
        private struct HistEntry
        {
            public string App, Tab, Route;
        }

        private readonly VisualElement _layer;
        private VisualElement _laptop, _frame, _screen, _chrome, _bar, _pageHost, _appRoot, _boot, _foot;
        private ScrollView _content, _tabs;
        private Label _url, _day, _rating, _money;
        private VisualElement _moneyChip, _lock;
        private readonly Dictionary<string, VisualElement> _navItems = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, Label> _navBadges = new Dictionary<string, Label>();
        private readonly Dictionary<string, string> _lastTab = new Dictionary<string, string>();
        private readonly List<HistEntry> _history = new List<HistEntry>();
        private Button _backBtn;
        private LaptopApp _app;
        private string _current = "home", _currentTab = "";
        private bool _dirty;
        private float _dirtyT, _topT;
        private string _themeSig = "", _badgeSig = "", _footSig = "", _visibleSig = "";
        private bool _booted;
        private float _pressedAt = -10f;

        public bool IsOpen { get; private set; }
        public string Current => _current;
        public string CurrentTab => _currentTab;
        /// <summary>Bildschirm des Laptops.</summary>
        public VisualElement Screen => _screen;
        /// <summary>Bereich für die Controller-Fokus-Steuerung (Seite ohne Browser-Leisten).</summary>
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
            _history.Clear();
            Routes.Clear();
        }

        private void Build()
        {
            _layer.Clear();
            _laptop = UIX.Div(_layer, "layer", "laptop", "os-theme");
            try
            {
                var sheet = Resources.Load<StyleSheet>("UI/Web");
                if (sheet != null) _laptop.styleSheets.Add(sheet);
                else Debug.LogWarning("Resources/UI/Web.uss nicht gefunden – Browser-Seiten ohne Design.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            _laptop.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.target == _laptop) Game.Sim?.ClosePc();
            });
            _frame = UIX.Col(_laptop, 0f, "os-frame", "br-frame");
            // Während eine Maustaste gedrückt ist, nicht neu bauen (sonst geht der Klick verloren).
            _frame.RegisterCallback<PointerDownEvent>(_ => _pressedAt = Time.unscaledTime, TrickleDown.TrickleDown);
            _frame.RegisterCallback<PointerUpEvent>(_ => _pressedAt = -10f, TrickleDown.TrickleDown);
            _screen = UIX.Col(_frame, 0f, "os-screen", "br-screen");
            _chrome = UIX.Col(_screen, 0f, "br-chrome");
            _pageHost = UIX.Col(_screen, 0f, "br-page");
            _pageHost.style.flexGrow = 1;
            _pageHost.style.flexShrink = 1;
            _content = UIX.Scroll(_pageHost, "os-content", "br-content");
            _foot = UIX.Row(_screen, 0f, "os-foot");
            _foot.style.position = Position.Absolute;
            _foot.pickingMode = PickingMode.Ignore;
            UIX.Show(_foot, false);
        }

        /// <summary>Reiterleiste und Adressleiste (Browser-Chrome) aufbauen.</summary>
        private void BuildChrome()
        {
            if (Tablet && BuildTabletChrome()) return;
            _chrome.Clear();
            _navItems.Clear();
            _navBadges.Clear();
            var tabRow = UIX.Row(_chrome, 0f, "br-tabrow");
            _tabs = new ScrollView(ScrollViewMode.Horizontal);
            _tabs.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _tabs.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _tabs.AddToClassList("br-tabs");
            _tabs.style.flexGrow = 1;
            _tabs.style.flexShrink = 1;
            tabRow.Add(_tabs);
            var inner = UIX.Row(_tabs.contentContainer, 0f, "br-tabs-inner");
            var visible = VisibleApps();
            var sig = new System.Text.StringBuilder();
            foreach (var a in visible)
            {
                sig.Append(a.Id).Append('|');
                string id = a.Id;
                var tab = UIX.PressRow(inner, 6f, () => OpenApp(id), "br-tab");
                tab.focusable = false; // Controller: LB/RB wechselt die App
                var fav = UIX.Div(tab, "br-fav");
                fav.style.backgroundColor = a.FavColor;
                W.Text(fav, string.IsNullOrEmpty(a.Fav) ? a.Title.Substring(0, 1) : a.Fav, WebSkin.Comic, WebFonts.Comic, "br-fav-text");
                W.Text(tab, a.Title, WebSkin.Comic, WebFonts.Bold, "br-tab-label");
                var badge = W.Text(tab, "", WebSkin.Comic, WebFonts.Bold, "br-badge");
                UIX.Show(badge, false);
                UIX.PassThrough(tab);
                _navItems[a.Id] = tab;
                _navBadges[a.Id] = badge;
            }
            _visibleSig = sig.ToString();
            var close = UIX.Pressable(tabRow, () => Game.Sim?.ClosePc(), "br-close");
            UIX.Icon(close, "close", 14f, null, "br-icon");
            UIX.PassThrough(close);
            close.tooltip = "Schließen (Esc)";

            _bar = UIX.Row(_chrome, 8f, "br-bar");
            _backBtn = UIX.Pressable(_bar, Back, "br-nb");
            UIX.Icon(_backBtn, "chevron_left", 14f, null, "br-icon");
            UIX.PassThrough(_backBtn);
            _backBtn.tooltip = "Zurück";
            var homeBtn = UIX.Pressable(_bar, GoHome, "br-nb");
            UIX.Icon(homeBtn, "home", 14f, null, "br-icon");
            UIX.PassThrough(homeBtn);
            homeBtn.tooltip = "Startseite der App";
            var reload = UIX.Pressable(_bar, () => Rebuild(), "br-nb");
            UIX.Icon(reload, "refresh", 14f, null, "br-icon");
            reload.tooltip = "Neu laden";
            UIX.PassThrough(reload);
            var urlBox = UIX.Row(_bar, 6f, "br-url");
            urlBox.style.flexGrow = 1;
            urlBox.style.flexShrink = 1;
            _lock = UIX.Div(urlBox, "br-lock");
            _url = W.Text(urlBox, "", WebSkin.Neo, WebFonts.Body, "br-url-text");
            _url.style.flexShrink = 1;
            _day = BarChip("br-chip-day");
            _rating = BarChip("br-chip-rating");
            _money = BarChip("br-chip-money");
            _moneyChip = _money.parent;
        }

        private Label BarChip(string cls)
        {
            var c = UIX.Row(_bar, 4f, "br-chip", cls);
            c.pickingMode = PickingMode.Ignore;
            var l = W.Text(c, "", WebSkin.Comic, WebFonts.Bold, "br-chip-text");
            Fonts.AddClass(l, "num-b");
            return l;
        }

        /// <summary>Design-Klassen setzen; bei geändertem Design die Leisten neu bauen.</summary>
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
            BuildChrome();
            _badgeSig = "";
            UpdateBadges();
            UpdateTop();
            if (_app != null) OpenApp(_current, _currentTab, true, true);
        }

        public void UpdateBadges()
        {
            var sim = Game.Sim;
            if (sim == null || _navItems.Count == 0) return;
            var visible = VisibleApps();
            var vs = new System.Text.StringBuilder();
            foreach (var a in visible) vs.Append(a.Id).Append('|');
            if (vs.ToString() != _visibleSig)
            {
                // neue App sichtbar geworden (z. B. Retouren) → Reiterleiste neu
                BuildChrome();
                _badgeSig = "";
                UpdateTop();
                UpdateUrl();
            }
            var sb = new System.Text.StringBuilder();
            foreach (var a in visible) sb.Append(Call(a.Badge)).Append('|');
            sb.Append(_current);
            string sig = sb.ToString();
            if (sig == _badgeSig) return;
            _badgeSig = sig;
            foreach (var a in visible)
            {
                if (!_navItems.TryGetValue(a.Id, out var nav)) continue;
                nav.EnableInClassList("active", a.Id == _current);
                string b = Call(a.Badge);
                var badge = _navBadges[a.Id];
                badge.text = b;
                UIX.Show(badge, b != "");
                nav.EnableInClassList("locked", Call(a.Locked) != "");
            }
        }

        private void UpdateTop()
        {
            var sim = Game.Sim;
            if (sim == null || _money == null) return;
            _day.text = UiFmt.DayShort(sim.Day) + " · " + Fmt.Clock(sim.TimeMinutes);
            _rating.text = "★ " + Fmt.Rating(sim.Reputation);
            _money.text = Fmt.Money(sim.Money);
            _moneyChip?.EnableInClassList("neg", sim.Money < 0);
            _backBtn?.SetEnabled(_history.Count > 0);
        }

        private void UpdateUrl()
        {
            if (_url == null) return;
            string url;
            bool secure = true;
            if (_app is WebApp w) url = "https://" + (w.Url ?? "");
            else
            {
                var def = Apps.Find(a => a.Id == _current);
                url = "hustleos://" + (def != null && !string.IsNullOrEmpty(def.Url) ? def.Url.Replace("hustle.os/", "") : _current) +
                      (string.IsNullOrEmpty(_currentTab) ? "" : "/" + _currentTab);
                secure = false;
            }
            _url.text = url;
            _lock?.EnableInClassList("off", !secure);
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
            OpenApp(app, tab, true, true);
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
            string[] steps = { "Lade Bestellungen …", "Öffne 7 Tabs gleichzeitig …", "Verbinde mit PaketBlitz …", "Motivation wird geladen …" };
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
            if (_appRoot != null && _appRoot.parent != null) UIX.FadeSlideIn(_appRoot.parent, 10f, 0.2f);
        }

        /// <summary>
        /// App (und Reiter bzw. Route) öffnen. id darf "app", "app/reiter", "app/route" oder ein
        /// alter Alias sein. Bei Apps ohne Reiter wird der Teil nach dem Schrägstrich zur Route.
        /// </summary>
        public void OpenApp(string id, string tab = null, bool silent = false) => OpenApp(id, tab, silent, false);

        private void OpenApp(string id, string tab, bool silent, bool noHistory)
        {
            if (_content == null) return;
            if (string.IsNullOrEmpty(id)) id = "home";
            if (Aliases.TryGetValue(id, out string alias)) id = alias;
            string route = null;
            int slash = id.IndexOf('/');
            if (slash > 0)
            {
                tab = id.Substring(slash + 1);
                id = id.Substring(0, slash);
                if (Aliases.TryGetValue(id, out string a2) && a2.IndexOf('/') < 0) id = a2;
            }
            var apps = VisibleApps();
            if (apps.Count == 0) return;
            var def = apps.Find(a => a.Id == id) ?? apps[0];
            var tabs = VisibleTabs(def);
            if (tabs.Count == 0 && def.Id == id && !string.IsNullOrEmpty(tab)) route = tab;

            // Verlauf für den Zurück-Knopf
            if (!noHistory && _app != null)
            {
                bool same = def.Id == _current && (tabs.Count == 0 ? route == null || route == GetRoute(def.Id) : (tab ?? "") == _currentTab);
                if (!same) PushHistory();
            }
            if (route != null) SetRoute(def.Id, route);

            bool appChanged = def.Id != _current;
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
            if (_current == "market" && _currentTab == "trends") TrendBadge = "";
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

            VisualElement page;
            if (_app is WebApp web)
            {
                page = UIX.Col(_content.contentContainer, 0f, "web-page", web.SkinClass);
                WebFonts.Set(page, web.Skin, WebFonts.Body);
                _appRoot = page;
                _content.AddToClassList("web");
            }
            else
            {
                _content.RemoveFromClassList("web");
                page = UIX.Col(_content.contentContainer, 0f, "app-page");
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
                string lead = _app.Lead;
                if (!string.IsNullOrEmpty(lead)) UIX.Text(page, lead, "app-lead");
                else head.style.marginBottom = 14;
                _appRoot = UIX.Col(page, 14f);
            }
            _app.Root = _appRoot;
            SafeBuild();
            _content.scrollOffset = Vector2.zero;
            _badgeSig = "";
            UpdateBadges();
            UpdateTop();
            UpdateUrl();
            if ((appChanged || tabChanged) && !silent) UIX.FadeSlideIn(page, 8f, 0.16f);
            if (GameInput.UsingGamepad) _content.schedule.Execute(() => UIX.FocusFirst(_pageHost));
        }

        private void PushHistory()
        {
            _history.Add(new HistEntry { App = _current, Tab = _currentTab, Route = GetRoute(_current) });
            if (_history.Count > 40) _history.RemoveAt(0);
        }

        /// <summary>Unterseite einer Web-App öffnen (mit Verlauf).</summary>
        public void Navigate(string appId, string route)
        {
            if (string.IsNullOrEmpty(appId)) return;
            if (appId != _current || _app == null)
            {
                SetRoute(appId, route);
                OpenApp(appId, null, false, false);
                return;
            }
            if ((route ?? "") == GetRoute(appId))
            {
                Rebuild(true);
                return;
            }
            PushHistory();
            SetRoute(appId, route);
            Game.Sound("click", 0.05f, -6f);
            Rebuild(true);
        }

        /// <summary>Zurück-Knopf: vorige Seite/App.</summary>
        public void Back()
        {
            if (_history.Count == 0) return;
            var h = _history[_history.Count - 1];
            _history.RemoveAt(_history.Count - 1);
            SetRoute(h.App, h.Route);
            OpenApp(h.App, string.IsNullOrEmpty(h.Tab) ? null : h.Tab, false, true);
        }

        private void GoHome()
        {
            if (_app is WebApp)
            {
                Navigate(_current, "");
                return;
            }
            OpenApp("home");
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
                UIX.Empty(_app.Root, "warning", "Diese Seite hatte einen Fehler.", "Einfach eine andere App öffnen und zurückkommen. (" + e.GetType().Name + ")");
            }
        }

        /// <summary>App neu aufbauen (Scrollposition und Controller-Fokus bleiben erhalten).</summary>
        public void Rebuild() => Rebuild(false);

        public void Rebuild(bool resetScroll)
        {
            if (_app == null || _appRoot == null) return;
            var offset = resetScroll ? Vector2.zero : _content.scrollOffset;
            int focus = GameInput.UsingGamepad && !resetScroll ? UIX.FocusIndex(_pageHost) : -1;
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
            _content.scrollOffset = offset;
            _content.schedule.Execute(() =>
            {
                _content.scrollOffset = offset;
                if (focus >= 0) UIX.RestoreFocus(_pageHost, focus);
                else if (resetScroll && GameInput.UsingGamepad) UIX.FocusFirst(_pageHost);
            });
            UpdateBadges();
            UpdateUrl();
            UpdateTop();
        }

        /// <summary>LB/RB: vorige/nächste App (bei Apps mit Reitern erst durch die Reiter).</summary>
        public void CycleApp(int dir)
        {
            var apps = VisibleApps();
            if (apps.Count == 0) return;
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
            UIX.KeyHint(_foot, "LB/RB", "Tab & Reiter");
            UIX.KeyHint(_foot, "A", "Auswählen");
            UIX.KeyHint(_foot, "B", "Schließen");
        }

        public void OnTradingTick()
        {
            if (!IsOpen) return;
            try
            {
                if (_app is ILiveApp live) live.LiveUpdate();
                else if (_app is AppTrading t) t.LiveUpdate();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
