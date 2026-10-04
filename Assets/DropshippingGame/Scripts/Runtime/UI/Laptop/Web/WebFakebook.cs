using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Fakebook (Comic, blau): Feed mit Posts von Fake-Leuten, dazwischen deine Anzeigen;
    /// Werbeanzeigen-Manager (Produkt, Zielgruppe, Slogan, Budget) und Fake-Profile.
    /// Routen: "" (Feed) · "ads" · "profiles".
    /// </summary>
    public sealed class WebFakebook : WebApp
    {
        public override string AppId => "fakebook";
        public override string SkinClass => "fbk";
        public override WebSkin Skin => WebSkin.Comic;
        protected override string Domain => "fakebook.com";

        private static string _pid = "";
        private static int _target, _slogan, _budget = 1;
        private readonly List<KeyValuePair<AdCampaign, Label>> _live = new List<KeyValuePair<AdCampaign, Label>>();
        private float _t;

        public override void Build()
        {
            var s = S;
            if (s == null) return;
            _live.Clear();
            string r = Route ?? "";
            var top = Row(Root, 12f, "fbk-top");
            var logo = Press(top, () => Nav(""), "fbk-logo");
            H(logo, "fakebook", "fbk-logo-text");
            UIX.PassThrough(logo);
            var search = Flex(Row(top, 6f, "fbk-search"));
            UIX.Icon(search, "search", 14f, null, "fbk-ic");
            T(search, "Suche auf Fakebook", "fbk-search-text");
            NavBtn(top, "home", "", r);
            NavBtn(top, "mega", "ads", r);
            NavBtn(top, "users", "profiles", r);
            var me = Row(top, 6f, "fbk-me");
            me.Add(new FaceAvatar(W.Hash(s.BrandName), 28f));
            B(me, s.BrandName, "fbk-me-name");

            var body = Row(Root, 16f, "fbk-body");
            body.style.alignItems = Align.FlexStart;
            if (r == "ads") Ads(body, s);
            else if (r == "profiles") Profiles(body, s);
            else Feed(body, s);
        }

        private void NavBtn(VisualElement parent, string icon, string route, string current)
        {
            var b = Press(parent, () => Nav(route), "fbk-navbtn");
            b.EnableInClassList("on", current == route);
            UIX.Icon(b, icon, 20f, null, "fbk-ic");
            UIX.PassThrough(b);
        }

        // =====================================================================================
        private void Feed(VisualElement body, Sim s)
        {
            var left = Wd(Col(body, 8f, "fbk-side"), 200f);
            SideLink(left, "home", "Feed", "");
            SideLink(left, "mega", "Werbung", "ads");
            SideLink(left, "users", "Fake-Profile (" + s.FakeProfiles + ")", "profiles");
            SideLink(left, "search", "Gugel", null, () => View?.OpenApp("gugel"));

            var feed = Flex(Col(body, 12f, "fbk-feed"));
            var comp = Row(feed, 10f, "fbk-card");
            comp.Add(new FaceAvatar(W.Hash(s.BrandName), 40f));
            var fake = Flex(Press(comp, () => Nav("ads"), "fbk-compose"));
            T(fake, "Was verkaufst du heute, " + s.BrandName + "?", "fbk-muted");
            UIX.PassThrough(fake);

            var ads = s.ActiveAds("fakebook");
            int ai = 0;
            for (int i = 0; i < 7; i++)
            {
                if (i % 2 == 1 && ai < ads.Count) AdPost(feed, s, ads[ai++]);
                int seed = W.Hash("p" + i + "d" + s.Day);
                Post(feed, seed, s);
            }
            while (ai < ads.Count) AdPost(feed, s, ads[ai++]);

            var right = Wd(Col(body, 8f, "fbk-side"), 240f);
            B(right, "Deine Anzeigen", "fbk-h");
            if (ads.Count == 0) T(right, "Keine aktiv.", "fbk-muted");
            foreach (var c in ads)
            {
                var row = Row(right, 8f, "fbk-mini");
                Wd(MK.Photo(row, c.Product, 40f), 40f);
                var v = Flex(Col(row, 0f));
                B(v, MK.Short(c.Product) + "  ×" + Fmt.Dec(c.Mult, 2), "fbk-text");
                _live.Add(new KeyValuePair<AdCampaign, Label>(c, T(v, LiveText(c), "fbk-muted")));
            }
            Btn(right, "+ Anzeige", () => Nav("ads"), "fbk-btn", "main");
        }

        private void SideLink(VisualElement parent, string icon, string text, string route, Action click = null)
        {
            var b = Press(parent, click ?? (() => Nav(route)), "fbk-sidelink");
            var r = Row(b, 8f);
            UIX.Icon(r, icon, 18f, null, "fbk-ic");
            B(r, text, "fbk-text");
            UIX.PassThrough(b);
        }

        private void Post(VisualElement feed, int seed, Sim s)
        {
            var card = Col(feed, 8f, "fbk-card");
            var head = Row(card, 8f);
            head.Add(new FaceAvatar(seed, 38f));
            var hv = Col(head, 0f);
            B(hv, MK.PeopleNames[seed % MK.PeopleNames.Length], "fbk-text");
            T(hv, (seed % 11 + 1) + " Std. · Öffentlich", "fbk-muted");
            T(card, MK.PeoplePosts[(seed / 3) % MK.PeoplePosts.Length], "fbk-post");
            if (seed % 3 == 0)
            {
                var pic = Div(card, "fbk-pic");
                pic.style.backgroundColor = W.Hex(new[] { "#FFD23F", "#7AC6FF", "#FF9FB3", "#9BE7A4" }[seed % 4]);
                pic.Add(new DotLayer(new Color(1f, 1f, 1f, 0.45f)));
            }
            Reacts(card, seed % 300 + 4, seed % 40);
        }

        private void Reacts(VisualElement card, int likes, int comments)
        {
            var r = Row(card, 14f, "fbk-reacts");
            W.Sym(r, "♥", "fbk-like");
            B(r, likes.ToString(), "fbk-muted");
            Fill(r);
            T(r, comments + " Kommentare", "fbk-muted");
        }

        private void AdPost(VisualElement feed, Sim s, AdCampaign c)
        {
            var card = Col(feed, 8f, "fbk-card", "ad");
            var head = Row(card, 8f);
            head.Add(new FaceAvatar(W.Hash(s.BrandName), 38f));
            var hv = Flex(Col(head, 0f));
            B(hv, s.BrandName, "fbk-text");
            T(hv, "Gesponsert · " + GameData.AdTargets[Mathf.Clamp(c.Target, 0, GameData.AdTargets.Length - 1)].Name, "fbk-muted");
            B(card, GameData.FillProduct(GameData.AdSlogans[Mathf.Clamp(c.Slogan, 0, GameData.AdSlogans.Length - 1)].Text, c.Product), "fbk-slogan");
            MK.Photo(card, c.Product, 220f, "ANGEBOT", Fmt.Money(s.CurrentSalePrice(c.Product)));
            var cta = Row(card, 8f, "fbk-cta");
            Flex(B(cta, W.Slug(s.BrandName) + ".shopifly.de", "fbk-muted"));
            Btn(cta, "Jetzt kaufen", () => View?.Navigate("shop", "item/" + c.Product), "fbk-btn", "main");
            _live.Add(new KeyValuePair<AdCampaign, Label>(c, T(card, LiveText(c), "fbk-muted")));
            Reacts(card, Mathf.RoundToInt(c.Clicks * 0.4f), Mathf.RoundToInt(c.Clicks * 0.05f) + s.FakeProfiles);
            int n = Mathf.Min(3, s.FakeProfiles);
            for (int i = 0; i < n; i++) Comment(card, 1000 + i, GameData.FakeProfileNames[i % GameData.FakeProfileNames.Length], MK.FakeComments[(i + c.Id) % MK.FakeComments.Length]);
            if (n < 2) Comment(card, c.Id * 13, MK.PeopleNames[c.Id % MK.PeopleNames.Length], MK.AdComments[c.Id % MK.AdComments.Length]);
        }

        private void Comment(VisualElement card, int seed, string name, string text)
        {
            var r = Row(card, 8f, "fbk-comment");
            r.Add(new FaceAvatar(seed, 26f));
            var b = Flex(Col(r, 0f, "fbk-bubble"));
            B(b, name, "fbk-small");
            T(b, text, "fbk-text");
        }

        private static string LiveText(AdCampaign c) =>
            MK.Num(c.Impressions) + " Views · " + MK.Num(c.Clicks) + " Klicks · " + Mathf.RoundToInt(c.Sales) + " Verk. · " + Fmt.Money(c.Spent) + "/" + Fmt.Money(c.Budget);

        public override void Tick(float dt)
        {
            _t += dt;
            if (_t < 0.5f) return;
            _t = 0f;
            foreach (var kv in _live)
                if (kv.Value != null) kv.Value.text = LiveText(kv.Key);
        }

        // =====================================================================================
        private void Ads(VisualElement body, Sim s)
        {
            var main = Flex(Col(body, 12f));
            if (!s.FakebookUnlocked)
            {
                var lk = Col(main, 6f, "fbk-card");
                H(lk, "Werbung ab Level " + GameData.FakebookLevel, "fbk-h1");
                T(lk, "Fake-Profile gehen schon.", "fbk-muted");
                return;
            }
            var listed = MK.Listed(s);
            if (listed.Count == 0)
            {
                H(Col(main, 6f, "fbk-card"), "Erst ein Produkt online stellen.", "fbk-h1");
                return;
            }
            if (!listed.Contains(_pid)) _pid = listed[0];
            _target = Mathf.Clamp(_target, 0, GameData.AdTargets.Length - 1);
            _slogan = Mathf.Clamp(_slogan, 0, GameData.AdSlogans.Length - 1);
            _budget = Mathf.Clamp(_budget, 0, GameData.FakebookBudgets.Length - 1);
            int budget = GameData.FakebookBudgets[_budget];

            var card = Col(main, 10f, "fbk-card");
            H(card, "Neue Anzeige", "fbk-h1");
            Step(card, "1", "Produkt");
            var pr = Row(card, 8f, "fbk-wrap");
            foreach (var pid in listed)
            {
                string id = pid;
                var t = Press(pr, () => { _pid = id; Rebuild(); }, "fbk-pick");
                t.EnableInClassList("on", id == _pid);
                Wd(MK.Photo(t, id, 56f), 56f);
                B(t, MK.Short(id), "fbk-small");
                UIX.PassThrough(t);
            }
            Step(card, "2", "Zielgruppe");
            var tr = Row(card, 8f, "fbk-wrap");
            for (int i = 0; i < GameData.AdTargets.Length; i++)
            {
                int ti = i;
                var tg = GameData.AdTargets[i];
                float fit = Sim.AdTargetFit(_pid, i);
                var t = Press(tr, () => { _target = ti; Rebuild(); }, "fbk-pick");
                t.EnableInClassList("on", i == _target);
                UIX.Icon(t, tg.Icon, 24f, null, "fbk-ic");
                B(t, tg.Name, "fbk-small");
                W.Sym(t, fit >= 0.99f ? "★★★" : (fit >= 0.6f ? "★★" : "★"), "fbk-fit");
                UIX.PassThrough(t);
            }
            Step(card, "3", "Slogan");
            var sr = Col(card, 4f);
            for (int i = 0; i < GameData.AdSlogans.Length; i++)
            {
                int si = i;
                var sl = GameData.AdSlogans[i];
                Btn(sr, GameData.FillProduct(sl.Text, _pid) + (sl.Clickbait ? "  [Clickbait]" : ""), () => { _slogan = si; Rebuild(); }, "fbk-chip", i == _slogan ? "on" : "");
            }
            Step(card, "4", "Budget");
            var br = Row(card, 8f);
            for (int i = 0; i < GameData.FakebookBudgets.Length; i++)
            {
                int bi = i;
                Btn(br, Fmt.Money(GameData.FakebookBudgets[i]), () => { _budget = bi; Rebuild(); }, "fbk-chip", i == _budget ? "on" : "");
            }

            // Vorschau
            var side = Wd(Col(body, 10f, "fbk-card"), 300f);
            B(side, "Vorschau", "fbk-h");
            var prev = Col(side, 6f, "fbk-preview");
            B(prev, GameData.FillProduct(GameData.AdSlogans[_slogan].Text, _pid), "fbk-slogan");
            MK.Photo(prev, _pid, 150f, "ANGEBOT");
            float mult = s.FakebookMult(_pid, _target, _slogan, budget);
            float views = budget * GameData.ImpressionsPerEuro * (0.8f + 0.4f * Sim.AdTargetFit(_pid, _target));
            var kp = Row(side, 6f);
            MK.Kpi(kp, "bolt", "×" + Fmt.Dec(mult, 2), "Nachfrage", Skin);
            MK.Kpi(kp, "eye", MK.Num(views), "Views", Skin);
            var kp2 = Row(side, 6f);
            MK.Kpi(kp2, "mouse", MK.Num(views * s.FakebookCtr(_pid, _target, _slogan)), "Klicks", Skin);
            MK.Kpi(kp2, "clock", "12 h", "Laufzeit", Skin);
            string why = s.CanStartAd("fakebook", _pid, budget);
            int t0 = _target, s0 = _slogan;
            string p0 = _pid;
            BtnIf(side, why == "", "Schalten · " + Fmt.Money(budget), () => { if (S.StartFakebookAd(p0, t0, s0, budget) != null) Rebuild(); }, "fbk-btn", "main", "wide");
            if (why != "") B(side, why, "fbk-warn");

            CampaignList(main, s, "fakebook");
        }

        private void Step(VisualElement parent, string n, string text)
        {
            var r = Row(parent, 8f, "fbk-step");
            B(r, n, "fbk-step-n");
            B(r, text, "fbk-h");
        }

        /// <summary>Kampagnenliste (auch von Gugel genutzt, dort mit eigener Skin).</summary>
        private void CampaignList(VisualElement parent, Sim s, string platform)
        {
            var box = Col(parent, 6f, "fbk-card");
            H(box, "Kampagnen", "fbk-h1");
            var hist = s.AdHistory(platform);
            if (hist.Count == 0) T(box, "Noch keine.", "fbk-muted");
            foreach (var c in hist)
            {
                var r = Row(box, 10f, "fbk-row");
                Wd(MK.Photo(r, c.Product, 44f), 44f);
                var v = Flex(Col(r, 2f));
                B(v, MK.Short(c.Product) + "  ×" + Fmt.Dec(c.Mult, 2) + (c.Active ? "" : "  · beendet"), "fbk-text");
                var lt = T(v, LiveText(c), "fbk-muted");
                if (c.Active) _live.Add(new KeyValuePair<AdCampaign, Label>(c, lt));
                MK.Bar(v, c.Budget > 0 ? c.Spent / c.Budget : 0f);
                int id = c.Id;
                if (c.Active) Btn(r, "Stopp", () => S.StopAdCampaign(id), "fbk-btn");
            }
        }

        // =====================================================================================
        private void Profiles(VisualElement body, Sim s)
        {
            var main = Flex(Col(body, 12f));
            var card = Col(main, 10f, "fbk-card");
            var hr = Row(card, 10f);
            Flex(H(hr, "Fake-Profile", "fbk-h1"));
            BtnIf(hr, s.FakeProfiles < GameData.MaxFakeProfiles && s.Money >= GameData.FakeProfileCost, "+ Profil · " + Fmt.Money(GameData.FakeProfileCost),
                () => S.CreateFakeProfile(), "fbk-btn", "main");
            BtnIf(hr, s.FakeProfiles > 0, "Alle löschen", () => S.DeleteFakeProfiles(), "fbk-btn");
            var kp = Row(card, 8f);
            MK.Kpi(kp, "users", s.FakeProfiles + " / " + GameData.MaxFakeProfiles, "Profile", Skin);
            MK.Kpi(kp, "bolt", "×" + Fmt.Dec(s.SocialProofMult(), 2), "Social Proof", Skin);
            MK.Kpi(kp, "warning", UiFmt.Percent(s.FakeProfileRisk()), "Risiko/Tag", Skin, s.FakeProfileRisk() > 0.15f ? "bad" : "");
            MK.Kpi(kp, "star", "−" + Fmt.Dec(s.FakeProfileRepLoss(), 2), "wenn erwischt", Skin);
            MK.Bar(card, s.FakeProfileRisk() / 0.6f, "mk-bar risk");
            var grid = Row(card, 10f, "fbk-wrap");
            for (int i = 0; i < s.FakeProfiles; i++)
            {
                var pc = Col(grid, 4f, "fbk-profile");
                pc.Add(new FaceAvatar(1000 + i, 56f));
                B(pc, GameData.FakeProfileNames[i % GameData.FakeProfileNames.Length], "fbk-small");
            }
            if (s.FakeProfiles == 0) T(card, "Niemand lobt dich. Noch.", "fbk-muted");
        }
    }
}
