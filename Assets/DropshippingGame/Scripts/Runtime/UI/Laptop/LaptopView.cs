using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// "HustleOS" – die Laptop-Oberfläche. Zwei Designs (Frachtbrief / Hype, je hell oder dunkel):
    /// Kopfzeile mit Marke, Tag, Bewertung und Kontostand, Navigation mit allen Apps und der
    /// Inhaltsbereich, der die jeweilige App lädt. Aktualisiert sich bei Wirtschaftsänderungen.
    /// </summary>
    public sealed class LaptopView
    {
        public sealed class AppDef
        {
            public string Id, Title, Icon, Group;
            public Func<LaptopApp> Make;
        }

        public static readonly AppDef[] Apps =
        {
            new AppDef { Id = "home", Title = "Übersicht", Icon = "home", Group = "BETRIEB", Make = () => new AppHome() },
            new AppDef { Id = "mail", Title = "Postfach", Icon = "mail", Group = "BETRIEB", Make = () => new AppMail() },
            new AppDef { Id = "buy", Title = "Einkauf", Icon = "cart", Group = "BETRIEB", Make = () => new AppBuy() },
            new AppDef { Id = "pack", Title = "Verpackung", Icon = "package", Group = "BETRIEB", Make = () => new AppPackaging() },
            new AppDef { Id = "shop", Title = "Webshop", Icon = "globe", Group = "VERKAUF", Make = () => new AppShop() },
            new AppDef { Id = "marketing", Title = "Marketing", Icon = "mega", Group = "VERKAUF", Make = () => new AppMarketing() },
            new AppDef { Id = "brand", Title = "Branding", Icon = "tag", Group = "VERKAUF", Make = () => new AppBranding() },
            new AppDef { Id = "market", Title = "Marktanalyse", Icon = "store", Group = "VERKAUF", Make = () => new AppMarket() },
            new AppDef { Id = "trading", Title = "Trading", Icon = "trend", Group = "FINANZEN", Make = () => new AppTrading() },
            new AppDef { Id = "bank", Title = "Bank", Icon = "bank", Group = "FINANZEN", Make = () => new AppBank() },
            new AppDef { Id = "stats", Title = "Analytics", Icon = "bars", Group = "FINANZEN", Make = () => new AppAnalytics() },
            new AppDef { Id = "staff", Title = "Personal", Icon = "users", Group = "FIRMA", Make = () => new AppStaff() },
            new AppDef { Id = "build", Title = "Ausbau", Icon = "building", Group = "FIRMA", Make = () => new AppBuild() },
            new AppDef { Id = "goals", Title = "Ziele & Lifestyle", Icon = "trophy", Group = "FIRMA", Make = () => new AppGoals() },
        };

        private readonly VisualElement _layer;
        private VisualElement _laptop, _screen, _top, _body, _sideHost, _frame;
        private ScrollView _content;
        private Label _brand, _day, _rating, _money;
        private readonly Dictionary<string, VisualElement> _nav = new Dictionary<string, VisualElement>();
        private LaptopApp _app;
        private string _current = "home";
        private bool _dirty;
        private float _dirtyT;
        private string _themeSig = "";

        public bool IsOpen { get; private set; }
        public string Current => _current;

        public LaptopView(VisualElement layer)
        {
            _layer = layer;
            UIX.Show(_layer, false);
            Settings.Changed += () =>
            {
                if (IsOpen) ApplyTheme(false);
            };
        }

        private void Build()
        {
            _layer.Clear();
            _nav.Clear();
            _laptop = UIX.Div(_layer, "layer", "laptop");
            _laptop.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.target == _laptop) Game.Sim?.ClosePc();
            });
            _frame = UIX.Col(_laptop, 0f, "os-frame");
            _screen = UIX.Col(_frame, 0f, "os-screen");
            _top = UIX.Div(_screen, "os-top");
            BuildTop();
            _body = UIX.Div(_screen, "os-body");
            _sideHost = UIX.Div(_body);
            _content = new ScrollView(ScrollViewMode.Vertical);
            _content.AddToClassList("os-content");
            _content.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _body.Add(_content);
        }

        private void BuildTop()
        {
            _top.Clear();
            bool hype = Settings.LaptopDesign == "hype";
            var brand = UIX.Div(_top, "os-brand");
            var ic = UIX.Icon(brand, hype ? "sparkle" : "bars", hype ? 26f : 20f, null);
            if (hype) ic.style.paddingLeft = 4;
            UIX.Text(brand, hype ? "HustleOS" : "HUSTLEOS", "os-brand-text");
            if (hype) UIX.Spacer(_top);
            _brand = Field(_top, "MARKE", true);
            _day = Field(_top, "TAG");
            _rating = Field(_top, "BEWERTUNG");
            _money = Field(_top, "KONTO");
            var close = UIX.Button(_top, "Schließen", () => Game.Sim?.ClosePc(), "ghost", false, "close");
            close.AddToClassList("os-close");
        }

        private Label Field(VisualElement parent, string key, bool grow = false)
        {
            var f = UIX.Col(parent, 0f, "os-field");
            if (grow && Settings.LaptopDesign != "hype") f.style.flexGrow = 1;
            UIX.Text(f, key, "os-field-key");
            return UIX.Text(f, "", "os-field-value");
        }

        private void BuildSide()
        {
            _sideHost.Clear();
            _nav.Clear();
            bool hype = Settings.LaptopDesign == "hype";
            var sv = new ScrollView(hype ? ScrollViewMode.Horizontal : ScrollViewMode.Vertical);
            sv.AddToClassList("os-side");
            sv.horizontalScrollerVisibility = hype ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
            sv.verticalScrollerVisibility = hype ? ScrollerVisibility.Hidden : ScrollerVisibility.Auto;
            if (hype) sv.contentContainer.style.flexDirection = FlexDirection.Row;
            _sideHost.Add(sv);
            _sideHost.style.flexShrink = 0;
            string group = "";
            foreach (var a in Apps)
            {
                if (a.Group != group)
                {
                    group = a.Group;
                    UIX.Text(sv.contentContainer, group, "os-group");
                }
                var nav = UIX.Div(sv.contentContainer, "os-nav");
                UIX.Icon(nav, a.Icon, 17f);
                UIX.Text(nav, a.Title, "os-nav-label");
                var badge = UIX.Text(nav, "", "badge");
                badge.name = "badge";
                UIX.Show(badge, false);
                var lockIcon = UIX.Icon(nav, "lock", 13f);
                lockIcon.name = "lock";
                UIX.Show(lockIcon, false);
                string id = a.Id;
                nav.AddManipulator(new Clickable(() => OpenApp(id)));
                _nav[a.Id] = nav;
            }
        }

        /// <summary>Design-Klassen setzen; bei geändertem Layout Navigation und Kopfzeile neu bauen.</summary>
        private void ApplyTheme(bool force)
        {
            string sig = Settings.LaptopDesign + Settings.LaptopDark;
            if (!force && sig == _themeSig) return;
            _themeSig = sig;
            bool hype = Settings.LaptopDesign == "hype";
            _laptop.EnableInClassList("fb", !hype);
            _laptop.EnableInClassList("hype", hype);
            _laptop.EnableInClassList("dark", Settings.LaptopDark);
            _laptop.EnableInClassList("light", !Settings.LaptopDark);
            BuildTop();
            BuildSide();
            UpdateBadges();
            UpdateTop();
            if (_app != null) OpenApp(_current, true);
        }

        public static string LockedReason(string id)
        {
            var sim = Game.Sim;
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

        public void UpdateBadges()
        {
            var sim = Game.Sim;
            if (sim == null) return;
            foreach (var a in Apps)
            {
                if (!_nav.TryGetValue(a.Id, out var nav)) continue;
                nav.EnableInClassList("active", a.Id == _current);
                var badge = nav.Q<Label>("badge");
                string b = "";
                if (a.Id == "mail" && sim.Events.UnreadCount() > 0) b = sim.Events.UnreadCount().ToString();
                if (a.Id == "marketing" && sim.TikTokAvailable()) b = "!";
                if (a.Id == "buy" && sim.DockCrates.Count > 0 && sim.StoryStage == "business") b = "";
                badge.text = b;
                UIX.Show(badge, b != "");
                bool locked = LockedReason(a.Id) != "";
                nav.EnableInClassList("locked", locked);
                UIX.Show(nav.Q("lock"), locked);
            }
        }

        private void UpdateTop()
        {
            var sim = Game.Sim;
            if (sim == null || _money == null) return;
            _brand.text = sim.BrandName;
            _day.text = (Settings.LaptopDesign == "hype" ? "Tag " : "") + sim.Day.ToString("00") + " · " + Fmt.Clock(sim.TimeMinutes);
            _rating.text = Fmt.Rating(sim.Reputation) + " ★";
            _money.text = Fmt.Money(sim.Money);
            _money.style.color = sim.Money < 0 ? Theme.LaptopBad : Theme.LaptopGood;
        }

        // ---- Öffnen / Schließen --------------------------------------------------------------------
        public void Open(string target = null)
        {
            if (_laptop == null) Build();
            IsOpen = true;
            UIX.Show(_layer, true);
            _layer.pickingMode = PickingMode.Position;
            ApplyTheme(true);
            var sim = Game.Sim;
            string app = target ?? _current;
            if (target == null)
            {
                if (sim.TutorialStep == 1) app = "buy";
                else if (sim.TutorialStep == 4) app = "shop";
                else if (sim.Events.PendingCount() > 0) app = "mail";
            }
            OpenApp(app, true);
            UIX.PopIn(_frame, 0.16f);
            Game.Sound("whoosh", 0.05f, -6f);
        }

        public void Close()
        {
            IsOpen = false;
            UIX.Show(_layer, false);
            _app?.Closed();
            _app = null;
        }

        public void OpenApp(string id, bool silent = false)
        {
            _current = id;
            _dirty = false;
            if (!silent) Game.Sound("click", 0.05f, -6f);
            _app?.Closed();
            _content.Clear();
            var def = Array.Find(Apps, a => a.Id == id) ?? Apps[0];
            string reason = LockedReason(def.Id);
            _app = reason != "" ? new AppLocked(def, reason) : def.Make();
            _app.View = this;
            _app.Root = UIX.Col(_content.contentContainer, 14f, "app-page");
            _app.Build();
            _content.scrollOffset = Vector2.zero;
            UpdateBadges();
            UpdateTop();
        }

        /// <summary>App neu aufbauen (Scrollposition bleibt erhalten).</summary>
        public void Rebuild()
        {
            if (_app == null) return;
            var offset = _content.scrollOffset;
            _app.Root.Clear();
            _app.Build();
            _content.schedule.Execute(() => _content.scrollOffset = offset);
            UpdateBadges();
        }

        public void MarkDirty() => _dirty = true;

        public void Tick(float dt)
        {
            if (!IsOpen) return;
            UpdateTop();
            _dirtyT += dt;
            if (_dirty && _dirtyT > 0.25f && _app != null && _app.CanRebuild())
            {
                _dirty = false;
                _dirtyT = 0f;
                Rebuild();
            }
            else _app?.Tick(dt);
        }

        public void OnTradingTick()
        {
            if (IsOpen && _app is AppTrading t) t.LiveUpdate();
        }
    }
}
