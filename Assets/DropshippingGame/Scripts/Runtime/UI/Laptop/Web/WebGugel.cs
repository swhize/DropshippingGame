using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Gugel (Suchmaschine): Startseite, Ergebnisseite mit gesponserten Treffern (deine Gugel-Anzeige
    /// nach Gebotsplatz) und organischen Treffern (deine KI-Fake-Seiten), Gugel Ads (Gebote pro
    /// Suchbegriff), KI-Seiten-Generator und die Fake-Seiten selbst.
    /// Routen: "" · "q/{produkt}/{begriff}" · "ads" · "sites" · "site/{index}".
    /// </summary>
    public sealed class WebGugel : WebApp
    {
        public override string AppId => "gugel";
        public override string SkinClass => "gg";
        public override WebSkin Skin => WebSkin.Neo;
        protected override string Domain => "gugel.de";

        private static string _pid = "";
        private static int _kw, _bid = 40, _budget = 1;
        private static readonly string[] LogoColors = { "#4285F4", "#EA4335", "#FBBC05", "#4285F4", "#34A853", "#EA4335" };

        public override string Url
        {
            get
            {
                var site = SiteFromRoute();
                return site != null ? SlopGen.Domain(site) : base.Url;
            }
        }

        private FakeSite SiteFromRoute()
        {
            string a = RouteArg("site/");
            if (a == null || S == null || !int.TryParse(a, out int i) || i < 0 || i >= S.FakeSites.Count) return null;
            return S.FakeSites[i];
        }

        public override void Build()
        {
            var s = S;
            if (s == null) return;
            string r = Route ?? "";
            var site = SiteFromRoute();
            if (site != null)
            {
                SitePage(s, site);
                return;
            }
            if (r.StartsWith("q/", StringComparison.Ordinal))
            {
                var parts = r.Split('/');
                string pid = parts.Length > 1 ? parts[1] : "";
                int kw = parts.Length > 2 && int.TryParse(parts[2], out int k) ? k : 0;
                if (GameData.IsProduct(pid))
                {
                    Results(s, pid, Mathf.Clamp(kw, 0, GameData.GugelKeywords.Length - 1));
                    return;
                }
            }
            if (r == "ads") AdsPage(s);
            else if (r == "sites") SitesPage(s);
            else Home(s);
        }

        private void Logo(VisualElement parent, float size)
        {
            var row = Press(parent, () => Nav(""), "gg-logo");
            var inner = Row(row, 0f);
            string word = "Gugel";
            for (int i = 0; i < word.Length; i++)
            {
                var l = H(inner, word[i].ToString(), "gg-logo-l");
                l.style.fontSize = size;
                l.style.color = W.Hex(LogoColors[i % LogoColors.Length]);
            }
            UIX.PassThrough(row);
        }

        private void TopBar(VisualElement parent, string query)
        {
            var bar = Row(parent, 14f, "gg-top");
            Logo(bar, 30f);
            var box = Flex(Row(bar, 8f, "gg-box"));
            UIX.Icon(box, "search", 16f, null, "gg-ic");
            T(box, query, "gg-query");
            Tab(bar, "search", "Suche", "");
            Tab(bar, "mega", "Ads", "ads");
            Tab(bar, "globe", "KI-Seiten", "sites");
        }

        private void Tab(VisualElement parent, string icon, string text, string route)
        {
            var b = Press(parent, () => Nav(route), "gg-tab");
            b.EnableInClassList("on", (Route ?? "") == route);
            var r = Row(b, 5f);
            UIX.Icon(r, icon, 15f, null, "gg-ic");
            B(r, text, "gg-tab-text");
            UIX.PassThrough(b);
        }

        // =====================================================================================
        private void Home(Sim s)
        {
            var c = Col(Root, 14f, "gg-home");
            Logo(c, 76f);
            var box = Row(c, 8f, "gg-box", "big");
            UIX.Icon(box, "search", 18f, null, "gg-ic");
            T(box, "Was willst du verkaufen?", "gg-query");
            var sug = Row(c, 8f, "gg-wrap");
            sug.style.justifyContent = Justify.Center;
            foreach (var pid in MK.Listed(s))
            {
                string id = pid;
                Btn(sug, GameData.FillProduct(GameData.GugelKeywords[0].Text, pid), () => Nav("q/" + id + "/0"), "gg-chip");
            }
            if (MK.Listed(s).Count == 0) T(c, "Stell erst ein Produkt online.", "gg-muted");
            var acts = Row(c, 10f);
            Btn(acts, "Gugel Ads", () => Nav("ads"), "gg-btn");
            Btn(acts, "KI-Seiten", () => Nav("sites"), "gg-btn");
            Btn(acts, "Auf gut Glück!", () => Toast("Glück gibt's nicht. Nur Gebote."), "gg-btn");
        }

        // =====================================================================================
        private void Results(Sim s, string pid, int kw)
        {
            string q = GameData.FillProduct(GameData.GugelKeywords[kw].Text, pid);
            TopBar(Root, q);
            var kwRow = Row(Root, 6f, "gg-wrap", "gg-pad");
            for (int i = 0; i < GameData.GugelKeywords.Length; i++)
            {
                int ki = i;
                Btn(kwRow, GameData.FillProduct(GameData.GugelKeywords[i].Text, pid), () => Nav("q/" + pid + "/" + ki), "gg-chip", i == kw ? "on" : "");
            }
            var list = Col(Root, 14f, "gg-results");
            T(list, "Ungefähr " + Fmt.Thousands(GameData.GugelVolume(pid, kw) * 1733) + " Ergebnisse (0,42 Sek.)", "gg-muted");

            AdCampaign mine = null;
            foreach (var c in s.ActiveAds("gugel"))
                if (c.Product == pid && c.Keyword == kw) mine = c;
            int[] rivals = GameData.GugelRivalBids(pid, kw);
            int pos = 1;
            for (int i = 0; i <= rivals.Length; i++)
            {
                if (mine != null && pos == mine.Position)
                {
                    Result(list, true, W.Slug(s.BrandName) + ".shopifly.de", s.BrandName + ": " + MK.Name(pid) + " ab " + Fmt.Money(s.CurrentSalePrice(pid)),
                        "Schneller Versand · ★ " + Fmt.Rating(s.Reputation) + " · " + GameData.FillProduct(GameData.AdSlogans[0].Text, pid), () => View?.Navigate("shop", "item/" + pid), true);
                    pos++;
                }
                if (i < rivals.Length)
                {
                    string rv = GameData.GugelRivals[i];
                    Result(list, true, W.Slug(rv) + ".de", MK.Name(pid) + " – " + rv, "Riesenauswahl. Kleine Preise. Große Versprechen.", () => Toast(rv + " will dein Geld."), false);
                    pos++;
                }
            }
            int n = 0;
            for (int i = 0; i < s.FakeSites.Count; i++)
            {
                var site = s.FakeSites[i];
                if (site.Product != pid) continue;
                int idx = i;
                n++;
                Result(list, false, SlopGen.Domain(site), SlopGen.Title(site), SlopGen.Snippet(site, s.BrandName), () => Nav("site/" + idx), true);
            }
            Result(list, false, "allesexpress.cn", "2026 NEU " + MK.Name(pid) + " Premium Top", "Ab 1 Stück. Oder 5000.", () => View?.Navigate("allesexpress", "item/" + pid), false);
            Result(list, false, "wikipaedia.org", MK.Name(pid) + " – Wikipädia", MK.Name(pid) + " ist ein Gegenstand, den Menschen kaufen.", () => Toast("Bearbeitungskrieg seit 2019."), false);
            Result(list, false, "schlechtefrage.net", "Ist " + MK.Name(pid) + " Betrug??", "Beste Antwort: „Kommt drauf an.“", () => Toast("17 Antworten, alle falsch."), false);
            if (n == 0) Btn(list, "Eigene KI-Seite bauen (SEO)", () => Nav("sites"), "gg-btn");
            if (mine == null) Btn(list, "Hier eigene Anzeige schalten", () => { _pid = pid; _kw = kw; Nav("ads"); }, "gg-btn", "main");
        }

        private void Result(VisualElement list, bool ad, string domain, string title, string snippet, Action click, bool mine)
        {
            var b = Press(list, click, "gg-res");
            b.EnableInClassList("mine", mine);
            var top = Row(b, 6f);
            if (ad) B(top, "Gesponsert", "gg-sponsor");
            T(top, domain, "gg-domain");
            B(b, title, "gg-title");
            T(b, snippet, "gg-snippet");
            UIX.PassThrough(b);
        }

        // =====================================================================================
        private void AdsPage(Sim s)
        {
            TopBar(Root, "Gugel Ads");
            var body = Row(Root, 16f, "gg-pad");
            body.style.alignItems = Align.FlexStart;
            var main = Flex(Col(body, 12f));
            if (!s.GugelUnlocked)
            {
                H(Col(main, 6f, "gg-card"), "Gugel Ads ab Level " + GameData.GugelLevel, "gg-h1");
                return;
            }
            var listed = MK.Listed(s);
            if (listed.Count == 0)
            {
                H(Col(main, 6f, "gg-card"), "Erst ein Produkt online stellen.", "gg-h1");
                return;
            }
            if (!listed.Contains(_pid)) _pid = listed[0];
            _kw = Mathf.Clamp(_kw, 0, GameData.GugelKeywords.Length - 1);
            _bid = Mathf.Clamp(_bid, GameData.GugelMinBid, GameData.GugelMaxBid);
            _budget = Mathf.Clamp(_budget, 0, GameData.GugelBudgets.Length - 1);
            int budget = GameData.GugelBudgets[_budget];

            var card = Col(main, 10f, "gg-card");
            H(card, "Neue Anzeige", "gg-h1");
            var pr = Row(card, 8f, "gg-wrap");
            foreach (var pid in listed)
            {
                string id = pid;
                var t = Press(pr, () => { _pid = id; Rebuild(); }, "gg-pick");
                t.EnableInClassList("on", id == _pid);
                Wd(MK.Photo(t, id, 48f), 48f);
                B(t, MK.Short(id), "gg-small");
                UIX.PassThrough(t);
            }
            B(card, "Suchbegriff", "gg-h");
            for (int i = 0; i < GameData.GugelKeywords.Length; i++)
            {
                int ki = i;
                var kd = GameData.GugelKeywords[i];
                var row = Press(card, () => { _kw = ki; Rebuild(); }, "gg-kw");
                row.EnableInClassList("on", i == _kw);
                var rr = Row(row, 10f);
                Flex(B(rr, GameData.FillProduct(kd.Text, _pid), "gg-text"));
                Wd(T(rr, Fmt.Thousands(GameData.GugelVolume(_pid, i)) + "/Tag", "gg-muted"), 80f);
                Wd(T(rr, "Top " + MK.Cents(GameData.GugelRivalBids(_pid, i)[0]), "gg-muted"), 90f);
                Wd(W.Sym(rr, kd.Intent >= 0.85f ? "€€€" : (kd.Intent >= 0.55f ? "€€" : "€"), "gg-intent"), 40f);
                UIX.PassThrough(row);
            }
            B(card, "Gebot pro Klick", "gg-h");
            var step = Row(card, 6f);
            foreach (int d in new[] { -10, -1, 1, 10 })
            {
                int dd = d;
                Btn(step, (d > 0 ? "+" : "−") + Mathf.Abs(d) + " ct", () => { _bid = Mathf.Clamp(_bid + dd, GameData.GugelMinBid, GameData.GugelMaxBid); Rebuild(); }, "gg-chip");
                if (d == -1) H(step, MK.Cents(_bid), "gg-bid");
            }
            int[] rv = GameData.GugelRivalBids(_pid, _kw);
            Btn(step, "Platz 1", () => { _bid = Mathf.Min(GameData.GugelMaxBid, rv[0] + 1); Rebuild(); }, "gg-chip");
            B(card, "Budget", "gg-h");
            var br = Row(card, 8f);
            for (int i = 0; i < GameData.GugelBudgets.Length; i++)
            {
                int bi = i;
                Btn(br, Fmt.Money(GameData.GugelBudgets[i]), () => { _budget = bi; Rebuild(); }, "gg-chip", i == _budget ? "on" : "");
            }

            var side = Wd(Col(body, 10f, "gg-card"), 300f);
            int pos = Sim.GugelPosition(_pid, _kw, _bid);
            int cpc = Sim.GugelCpc(_pid, _kw, _bid);
            float clicks = Sim.GugelClicksPerDay(_pid, _kw, _bid);
            H(side, "Platz " + pos, "gg-pos", "p" + pos);
            var kp = Row(side, 6f);
            MK.Kpi(kp, "bolt", "×" + Fmt.Dec(Sim.GugelMult(_pid, _kw, _bid), 2), "Nachfrage", Skin);
            MK.Kpi(kp, "mouse", MK.Num(clicks), "Klicks/Tag", Skin);
            var kp2 = Row(side, 6f);
            MK.Kpi(kp2, "coin", MK.Cents(cpc), "pro Klick", Skin);
            MK.Kpi(kp2, "clock", UiFmt.Duration(Mathf.Min(720f, clicks * cpc / 100f > 0f ? budget / (clicks * cpc / 100f) * 720f : 720f)), "Laufzeit", Skin);
            T(side, "Du zahlst nur das nächste Gebot + 1 ct.", "gg-muted");
            string why = s.CanStartAd("gugel", _pid, budget);
            string p0 = _pid;
            int k0 = _kw, b0 = _bid;
            BtnIf(side, why == "", "Starten · " + Fmt.Money(budget), () => { if (S.StartGugelAd(p0, k0, b0, budget) != null) Rebuild(); }, "gg-btn", "main");
            if (why != "") B(side, why, "gg-warn");
            Btn(side, "Vorschau ansehen", () => Nav("q/" + p0 + "/" + k0), "gg-btn");

            var box = Col(main, 6f, "gg-card");
            H(box, "Kampagnen", "gg-h1");
            var hist = s.AdHistory("gugel");
            if (hist.Count == 0) T(box, "Noch keine.", "gg-muted");
            foreach (var c in hist)
            {
                var r = Row(box, 10f, "gg-row");
                Wd(MK.Photo(r, c.Product, 40f), 40f);
                var v = Flex(Col(r, 2f));
                B(v, "„" + GameData.FillProduct(GameData.GugelKeywords[Mathf.Clamp(c.Keyword, 0, GameData.GugelKeywords.Length - 1)].Text, c.Product) + "“ · Platz " + c.Position +
                     (c.Active ? "" : " · beendet"), "gg-text");
                T(v, MK.Num(c.Clicks) + " Klicks · " + Mathf.RoundToInt(c.Sales) + " Verk. · " + Fmt.Money(c.Spent) + "/" + Fmt.Money(c.Budget), "gg-muted");
                MK.Bar(v, c.Budget > 0 ? c.Spent / c.Budget : 0f);
                int id = c.Id;
                if (c.Active) Btn(r, "Stopp", () => S.StopAdCampaign(id), "gg-btn");
            }
        }

        // =====================================================================================
        private void SitesPage(Sim s)
        {
            TopBar(Root, "KI-Seiten-Generator");
            var body = Col(Root, 12f, "gg-pad");
            var card = Col(body, 10f, "gg-card");
            H(card, "Neue KI-Seite", "gg-h1");
            var kp = Row(card, 8f);
            MK.Kpi(kp, "globe", s.FakeSites.Count + " / " + GameData.MaxFakeSites, "Seiten", Skin);
            MK.Kpi(kp, "bolt", "+" + Mathf.RoundToInt(GameData.FakeSiteSeo * 100f) + " %", "pro Seite", Skin);
            MK.Kpi(kp, "warning", UiFmt.Percent(s.FakeSiteRiskTotal()), "Risiko/Tag", Skin, s.FakeSiteRiskTotal() > 0.15f ? "bad" : "");
            if (!s.FakeSitesUnlocked)
            {
                B(card, "Ab Level " + GameData.FakeSiteLevel, "gg-warn");
                return;
            }
            var grid = Row(card, 8f, "gg-wrap");
            foreach (var p in GameData.Products)
            {
                if (!s.ProductUnlocked(p.Id)) continue;
                string id = p.Id;
                string why = s.CanCreateFakeSite(id);
                var t = Col(grid, 4f, "gg-pick");
                Wd(MK.Photo(t, id, 56f), 56f);
                B(t, MK.Short(id) + " (" + s.FakeSiteCount(id) + ")", "gg-small");
                BtnIf(t, why == "", "+ " + Fmt.Money(GameData.FakeSiteCost), () => S.CreateFakeSite(id), "gg-chip");
            }
            var list = Col(body, 6f, "gg-card");
            H(list, "Deine Seiten", "gg-h1");
            if (s.FakeSites.Count == 0) T(list, "Noch keine.", "gg-muted");
            for (int i = 0; i < s.FakeSites.Count; i++)
            {
                var site = s.FakeSites[i];
                int idx = i;
                var r = Row(list, 10f, "gg-row");
                Wd(MK.Photo(r, site.Product, 40f), 40f);
                var v = Flex(Col(r, 1f));
                B(v, SlopGen.Domain(site), "gg-domain");
                T(v, SlopGen.Title(site), "gg-text");
                Btn(r, "Öffnen", () => Nav("site/" + idx), "gg-btn");
                Btn(r, "Löschen", () => S.DeleteFakeSite(site), "gg-btn");
            }
        }

        // =====================================================================================
        private void SitePage(Sim s, FakeSite site)
        {
            var page = Col(Root, 12f, "slop");
            var hd = Row(page, 10f, "slop-hd");
            Flex(B(hd, SlopGen.Domain(site), "slop-domain"));
            Btn(hd, "Zurück zu Gugel", () => Nav("sites"), "slop-btn");
            B(page, "WERBUNG · WERBUNG · WERBUNG", "slop-ad");
            H(page, SlopGen.Title(site), "slop-title");
            T(page, "von " + SlopGen.Author(site) + " · aktualisiert vor 3 Min. · 14 Min. Lesezeit", "slop-meta");
            MK.Photo(page, site.Product, 240f, "TESTSIEGER", "11/10");
            foreach (var para in SlopGen.Body(site, s.BrandName, s.CurrentSalePrice(site.Product))) T(page, para, "slop-text");
            int n = 1;
            foreach (var reason in SlopGen.Reasons10(site, s.BrandName))
            {
                var r = Row(page, 10f, "slop-item");
                H(r, (n++).ToString(), "slop-n");
                Flex(T(r, reason, "slop-text"));
            }
            Btn(page, "JETZT BEI " + s.BrandName.ToUpperInvariant() + " KAUFEN!!!", () => View?.Navigate("shop", "item/" + site.Product), "slop-buy");
            T(page, "* Diese Seite enthält Affiliate-Links, Meinungen und 0 % Fakten.", "slop-meta");
        }
    }
}
