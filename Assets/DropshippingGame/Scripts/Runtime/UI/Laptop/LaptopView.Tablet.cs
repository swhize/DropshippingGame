using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Tablet-Look des Laptops: runder Rahmen mit Kamera, schmale Statusleiste (Zurück, Start,
    /// Adresse, Uhr, Bewertung, Konto, Schließen) und unten ein Dock mit App-Symbolen statt
    /// Browser-Reitern. Stil in Resources/UI/Tablet.uss. <see cref="Tablet"/> = false → alter Browser.
    /// </summary>
    public sealed partial class LaptopView
    {
        public static bool Tablet = true;

        private static readonly Dictionary<string, string> DockNames = new Dictionary<string, string>
        {
            { "allesexpress", "Einkauf" }, { "shop", "Shop" }, { "mail", "Mail" }, { "paket", "Paket" }, { "tiktak", "TikTak" },
            { "fakebook", "Fakebook" }, { "gugel", "Gugel" }, { "trading", "Börse" }, { "bank", "Bank" }, { "market", "Markt" },
            { "orders", "Aufträge" }, { "returns", "Retouren" }, { "company", "Firma" },
        };

        private VisualElement _dock;

        public static string DockName(AppDef a) => a == null ? "" : (DockNames.TryGetValue(a.Id, out string n) ? n : a.Title);

        private bool BuildTabletChrome()
        {
            try
            {
                var sheet = Resources.Load<StyleSheet>("UI/Tablet");
                if (sheet != null && !_laptop.styleSheets.Contains(sheet)) _laptop.styleSheets.Add(sheet);
                _laptop.AddToClassList("tablet");
                if (_frame.Q(className: "tb-cam") == null)
                {
                    var cam = UIX.Div(_frame, "tb-cam");
                    cam.style.position = Position.Absolute;
                    cam.pickingMode = PickingMode.Ignore;
                }
                _chrome.Clear();
                _navItems.Clear();
                _navBadges.Clear();

                _bar = UIX.Row(_chrome, 8f, "tb-status");
                _backBtn = UIX.Pressable(_bar, Back, "tb-nb");
                UIX.Icon(_backBtn, "chevron_left", 16f, null, "tb-icon");
                _backBtn.tooltip = "Zurück";
                UIX.PassThrough(_backBtn);
                var home = UIX.Pressable(_bar, () => OpenApp("home"), "tb-nb");
                UIX.Icon(home, "grid", 16f, null, "tb-icon");
                home.tooltip = "Start";
                UIX.PassThrough(home);
                var urlBox = UIX.PressRow(_bar, 6f, GoHome, "tb-url");
                urlBox.style.flexGrow = 1;
                urlBox.style.flexShrink = 1;
                urlBox.tooltip = "Startseite der App";
                _lock = UIX.Div(urlBox, "br-lock");
                _url = W.Text(urlBox, "", WebSkin.Neo, WebFonts.Body, "tb-url-text");
                _url.style.flexShrink = 1;
                UIX.PassThrough(urlBox);
                _day = Chip("clock", "tb-chip-day");
                _rating = Chip("star", "tb-chip-rating");
                _money = Chip("coin", "tb-chip-money");
                _moneyChip = _money.parent;
                UIX.Icon(_bar, "wifi", 15f, null, "tb-icon");
                UIX.Icon(_bar, "battery", 18f, null, "tb-icon");
                var close = UIX.Pressable(_bar, () => Game.Sim?.ClosePc(), "tb-nb", "tb-close");
                UIX.Icon(close, "close", 14f, null, "tb-icon");
                close.tooltip = "Schließen (Esc)";
                UIX.PassThrough(close);

                _dock?.RemoveFromHierarchy();
                _dock = UIX.Row(_screen, 0f, "tb-dock");
                _dock.pickingMode = PickingMode.Ignore;
                var inner = UIX.Row(_dock, 4f, "tb-dock-inner");
                var sig = new System.Text.StringBuilder();
                foreach (var a in VisibleApps())
                {
                    sig.Append(a.Id).Append('|');
                    if (a.Id == "home") continue;
                    string id = a.Id;
                    var tab = UIX.PressCol(inner, 2f, () => OpenApp(id), "tb-app");
                    tab.focusable = false;
                    tab.tooltip = a.Title;
                    var ic = UIX.Div(tab, "tb-app-ic");
                    ic.style.backgroundColor = a.FavColor;
                    UIX.Icon(ic, string.IsNullOrEmpty(a.Icon) ? "dot" : a.Icon, 22f, Color.white, "tb-app-glyph");
                    var badge = W.Text(ic, "", WebSkin.Neo, WebFonts.Bold, "tb-badge");
                    badge.style.position = Position.Absolute;
                    UIX.Show(badge, false);
                    W.Text(tab, DockName(a), WebSkin.Neo, WebFonts.Bold, "tb-app-label");
                    UIX.PassThrough(tab);
                    _navItems[a.Id] = tab;
                    _navBadges[a.Id] = badge;
                }
                _visibleSig = sig.ToString();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _laptop?.RemoveFromClassList("tablet");
                _dock?.RemoveFromHierarchy();
                _dock = null;
                _chrome?.Clear();
                return false;
            }
        }

        private Label Chip(string icon, string cls)
        {
            var c = UIX.Row(_bar, 4f, "tb-chip", cls);
            c.pickingMode = PickingMode.Ignore;
            UIX.Icon(c, icon, 13f, null, "tb-icon");
            var l = W.Text(c, "", WebSkin.Neo, WebFonts.Bold, "tb-chip-text");
            Fonts.AddClass(l, "num-b");
            return l;
        }
    }

    /// <summary>Startbildschirm des Tablets: Uhr, vier große Kennzahlen und App-Kacheln.</summary>
    public sealed class AppTabletHome : WebApp
    {
        public override string AppId => "home";
        public override string SkinClass => "th";
        public override WebSkin Skin => WebSkin.Neo;
        protected override string Domain => "hustle.os";

        public override void Build()
        {
            var s = S;
            if (s == null) return;
            var hero = Row(Root, 16f, "th-hero");
            var clock = Col(hero, 0f);
            H(clock, Fmt.Clock(s.TimeMinutes), "th-clock");
            T(clock, s.WeekdayName() + " · Tag " + s.Day, "th-date");
            Fill(hero);
            B(hero, s.BrandName, "th-brand");

            var w = Row(Root, 12f, "th-widgets");
            Widget(w, "coin", Fmt.Money(s.Money), "Konto", s.Money < 0 ? "bad" : "", "bank");
            Widget(w, "trend", Fmt.Money(s.Daily.Revenue), "Heute", "", "bank/analytics");
            Widget(w, "cart", s.PendingCount() + "/" + s.QueueCapacity(), "Bestellungen", s.PendingCount() >= s.QueueCapacity() ? "bad" : "", "paket");
            Widget(w, "star", Fmt.Rating(s.Reputation), "Bewertung", "", "shop");
            float mult = 1f;
            foreach (var b in s.Boosts) mult *= b.Mult;
            Widget(w, "bolt", "×" + Fmt.Dec(Mathf.Min(mult, GameData.MaxBoostMult), 1), "Boosts", "", "fakebook");

            var obj = s.CurrentObjective();
            if (!string.IsNullOrEmpty(obj.Title))
            {
                var o = Row(Root, 10f, "th-goal");
                UIX.Icon(o, "flag", 18f, null, "th-ic");
                Flex(B(o, obj.Title, "th-goal-text"));
                if (obj.Progress >= 0f) Wd(MK.Bar(o, obj.Progress), 160f);
            }

            var grid = Div(Root, "th-grid");
            foreach (var a in LaptopView.Apps)
            {
                if (a == null || a.Id == "home") continue;
                bool visible = true;
                try
                {
                    visible = a.Visible == null || a.Visible();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                if (!visible) continue;
                string id = a.Id;
                var t = Press(grid, () => View?.OpenApp(id), "th-tile");
                var ic = Div(t, "th-tile-ic");
                ic.style.backgroundColor = a.FavColor;
                UIX.Icon(ic, string.IsNullOrEmpty(a.Icon) ? "dot" : a.Icon, 34f, Color.white);
                string badge = "";
                try
                {
                    badge = a.Badge != null ? a.Badge() ?? "" : "";
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                if (badge != "")
                {
                    var bl = B(ic, badge, "tb-badge");
                    bl.style.position = Position.Absolute;
                }
                B(t, LaptopView.DockName(a), "th-tile-label");
                UIX.PassThrough(t);
            }
        }

        private void Widget(VisualElement parent, string icon, string value, string label, string tone, string go)
        {
            var c = Flex(Press(parent, () => View?.OpenApp(go), "th-widget"));
            if (tone != "") c.AddToClassList(tone);
            var r = Row(c, 6f);
            UIX.Icon(r, icon, 16f, null, "th-ic");
            T(r, label, "th-label");
            H(c, value, "th-value");
            UIX.PassThrough(c);
        }
    }
}
