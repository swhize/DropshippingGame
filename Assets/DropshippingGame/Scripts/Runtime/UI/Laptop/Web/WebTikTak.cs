using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// TikTak Creator-Studio (dunkel, Cyan/Pink-Glitch-Logo): Feed der eigenen, wirklich
    /// aufgenommenen Videos (<see cref="Sim.TikTokVideos"/>, gespeichert), Video aufnehmen (Produkt +
    /// Format wählen, „Video aufnehmen“ klappt den Laptop zu und startet die Aufnahme in der Welt,
    /// max. <see cref="GameData.TikTokPerDay"/> pro Tag), Analysen, Kommentare, Trend-Ideen und Werbung.
    /// Routen: "" (Feed) · "post" · "stats" · "ads".
    /// </summary>
    public sealed class WebTikTak : WebApp
    {
        public override string AppId => "tiktak";
        public override string SkinClass => "tt";
        public override WebSkin Skin => WebSkin.Neo;
        protected override string Domain => "tiktak.com/creator-studio";

        private static int _index;
        private static bool _liked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _index = 0;
            _liked = false;
        }

        private static readonly string[][] Comments =
        {
            new[] { "mandy_k84", "wo gibt's das?? link???", "#FF7AB6" },
            new[] { "kevin2009", "meins kam nach 61 tagen lol", "#25F4EE" },
            new[] { "gisela_1947", "Wie kann man hier bestellen? Mit Überweisung?", "#FFB020" },
            new[] { "dropship_guru_99", "bro das gibts auf allesexpress für 0,89", "#9B6BFF" },
            new[] { "torben.w", "gekauft. danke tiktak", "#7AC142" },
        };

        public override void Build()
        {
            var s = S;
            if (s == null) return;
            string r = Route ?? "";
            var tt = Row(Root, 0f, "tt-wrap");
            tt.style.alignItems = Align.Stretch;

            var nav = Wd(Col(tt, 4f, "tt-nav"), 210f);
            H(nav, "TikTak", "tt-glitch");
            NavItem(nav, "Für dich", "", r);
            NavItem(nav, "Video aufnehmen", "post", r);
            NavItem(nav, "Analysen", "stats", r);
            NavItem(nav, "Werbung (Ads)", "ads", r);
            Btn(nav, "+ Video aufnehmen (" + s.Daily.TikToks + "/" + GameData.TikTokPerDay + " heute)", () => Nav("post"), "tt-post");

            var mid = Flex(Col(tt, 12f, "tt-mid"));
            if (r == "post") Post(mid, s);
            else if (r == "stats") Stats(mid, s);
            else if (r == "ads") Ads(mid, s);
            else Feed(mid, s);

            var side = Wd(Col(tt, 8f, "tt-side"), 290f);
            Side(side, s);
        }

        private void NavItem(VisualElement nav, string text, string route, string cur) =>
            Btn(nav, text, () => Nav(route), "tt-nav-item", route == cur ? "on" : "");

        // =====================================================================================
        private void Feed(VisualElement mid, Sim s)
        {
            var list = new List<TikTokVideo>();
            foreach (var v0 in s.TikTokVideos)
                if (v0 != null)
                    list.Add(v0);
            if (list.Count == 0)
            {
                var e = Col(mid, 8f, "tt-box");
                B(e, "Noch keine Videos", "tt-h");
                T(e, "Hier landen deine echten Aufnahmen. Produkt wählen, Laptop zu, Handy hoch – und los.", "tt-muted");
                Btn(e, "Video aufnehmen", () => Nav("post"), "tt-post");
                return;
            }
            _index = Mathf.Clamp(_index, 0, list.Count - 1);
            var v = list[_index];
            var feed = Row(mid, 16f, "tt-feed");
            var vid = Col(feed, 0f, "tt-vid");
            var col = GameData.IsProduct(v.Product) ? GameData.Product(v.Product).Color.ToColor() : Color.magenta;
            vid.style.backgroundColor = Color.Lerp(col, Color.black, 0.35f);
            var glow = Div(vid, "tt-vid-glow");
            glow.style.backgroundColor = Color.Lerp(col, Color.white, 0.2f);
            if (GameData.IsProduct(v.Product))
            {
                var art = new ProductArt(v.Product, false) { Dots = false, IconScale = 0.55f };
                art.style.position = Position.Absolute;
                art.style.left = 0;
                art.style.right = 0;
                art.style.top = 60;
                art.style.height = 300;
                vid.Add(art);
            }
            B(vid, string.IsNullOrEmpty(v.Title) ? "Mein Video" : v.Title, "tt-overlay");
            var cap = Col(vid, 2f, "tt-cap");
            B(cap, "@" + W.Slug(s.BrandName).Replace("-", "."), "tt-text");
            T(cap, TikTokFormats.Name(v.Format) + " · Tag " + v.Day + " · Treffer " + Mathf.RoundToInt(v.Score * 100f) + " % #fyp #" +
                   W.Slug(s.BrandName).Replace("-", ""), "tt-small");
            T(cap, Fmt.Thousands(v.Views) + " Aufrufe · Video " + (_index + 1) + "/" + list.Count, "tt-small");

            var acts = Col(feed, 16f, "tt-acts");
            Act(acts, "♥", Fmt.Thousands(v.Likes + (_liked ? 1 : 0)), () =>
            {
                _liked = !_liked;
                Rebuild();
            }, _liked);
            Act(acts, "…", Fmt.Thousands(v.Comments), () => Toast("„wo gibt's das?? link???“ – " + Math.Max(3, v.Comments / 5) + " Mal."), false);
            Act(acts, "↗", "Teilen", () => Toast("Link kopiert. An wen? An Mama."), false);
            Act(acts, "↓", "Nächstes", () =>
            {
                _index = (_index + 1) % Mathf.Max(1, list.Count);
                _liked = false;
                Rebuild();
            }, false);
        }

        private void Act(VisualElement parent, string glyph, string text, Action onClick, bool on)
        {
            var b = Press(parent, onClick, "tt-act");
            b.EnableInClassList("on", on);
            var ic = Div(b, "tt-act-ic");
            W.Sym(ic, glyph, "tt-act-glyph");
            B(b, text, "tt-small");
            UIX.PassThrough(b);
        }

        // =====================================================================================
        private void Post(VisualElement mid, Sim s)
        {
            var box = Col(mid, 10f, "tt-box");
            B(box, "Video aufnehmen", "tt-h");
            string block = TikTokStudio.RecordBlocker(s);
            if (TikTokStudio.HasDraft) Draft(mid, s);
            if (block != null)
            {
                T(box, block, "tt-muted");
                if (s.ActiveBoost("tiktok") != null) T(box, "Dein Trend läuft noch! Genieß die Bestellungen.", "tt-muted");
                return;
            }
            TikTokStudio.EnsureSelection(s);
            T(box, "Produkt", "tt-muted");
            var pick = Row(box, 6f);
            pick.style.flexWrap = Wrap.Wrap;
            foreach (var id in TikTokStudio.Filmable(s))
            {
                string pid = id;
                Btn(pick, GameData.Product(pid).Short + " · " + TikTokStudio.ProductSub(s, pid), () =>
                {
                    TikTokStudio.Product = pid;
                    Rebuild();
                }, "tt-chip", TikTokStudio.Product == pid ? "on" : "");
            }
            T(box, "Format", "tt-muted");
            var fr = Row(box, 6f);
            fr.style.flexWrap = Wrap.Wrap;
            foreach (var f in TikTokFormats.All)
            {
                string fid = f;
                Btn(fr, TikTokStudio.FormatLabel(s, fid), () =>
                {
                    TikTokStudio.Format = fid;
                    Rebuild();
                }, "tt-chip", TikTokStudio.Format == fid ? "on" : "");
            }
            T(box, TikTokFormats.Tip(TikTokStudio.Format), "tt-text");
            T(box, "Der Laptop klappt zu, das Produkt steht vor dir und du filmst 10–20 Sekunden in Ego-Sicht. " +
                   GameInput.KeyLabel("rec_action") + " = Aktion (gleich am Anfang = Hook), " + GameInput.KeyLabel("rec_stop") + " = Stopp, " +
                   GameInput.KeyLabel("rec_cancel") + " = Abbrechen. Bewertet werden Bildmitte, Abstand, ruhige Kamera, Licht, Blickwinkel, Länge, Aktionen und Deko im Bild. " +
                   "Trend-Format heute: " + TikTokFormats.Name(s.TrendingTikTokFormat()) + " (+" + Mathf.RoundToInt(TikTokScoring.TrendingFormatBonus * 100f) + " %).", "tt-muted");
            Btn(box, "Video aufnehmen", () => TikTokStudio.StartRecording(), "tt-post");
            T(box, "Noch " + s.TikToksLeftToday() + " von " + GameData.TikTokPerDay + " heute. Produkte mit Hype wirken stärker. Tipp: Das Ringlicht am Schreibtisch startet die Aufnahme auch.", "tt-muted");
        }

        private void Draft(VisualElement mid, Sim s)
        {
            var d = Col(mid, 6f, "tt-box");
            B(d, "Entwurf von vorhin", "tt-h");
            T(d, "Deine letzte Aufnahme wartet noch. Posten oder neu aufnehmen.", "tt-muted");
            bool canPost = s.TikTokBlocker() == null;
            var r = Row(d, 8f);
            BtnIf(r, canPost, "Entwurf posten", () =>
            {
                TikTokStudio.PostDraft();
                Rebuild();
            }, "tt-post");
            Btn(r, "Verwerfen", () =>
            {
                TikTokStudio.ClearDraft();
                Rebuild();
            }, "tt-chip");
        }

        // =====================================================================================
        private void Stats(VisualElement mid, Sim s)
        {
            var grid = Div(mid, "tt-stat");
            int views = 0;
            TikTokVideo best = null;
            foreach (var v in s.TikTokVideos)
            {
                if (v == null) continue;
                views += v.Views;
                if (best == null || v.Views > best.Views) best = v;
            }
            float mult = 1f;
            foreach (var b in s.Boosts) mult *= b.Mult;
            Stat2(grid, "Aufrufe (letzte " + s.TikTokVideos.Count + " Videos)", Fmt.Thousands(views), "");
            Stat2(grid, "Follower", Fmt.Thousands(Mathf.RoundToInt(s.Awareness * 12000f)), "Bekanntheit " + UiFmt.Percent(s.Awareness / 1.5f));
            Stat2(grid, "Reichweite", "×" + Fmt.Dec(mult, 1), s.Boosts.Count + " Boost(s) aktiv");
            Stat2(grid, "Videos heute", s.Daily.TikToks + "/" + GameData.TikTokPerDay, s.TikTokAvailable() ? "bereit" : "Pause");
            if (best != null)
            {
                var bb = Col(mid, 6f, "tt-box");
                B(bb, "Dein bestes Video", "tt-h");
                T(bb, best.Title, "tt-text");
                T(bb, Fmt.Thousands(best.Views) + " Aufrufe · Treffer " + Mathf.RoundToInt(best.Score * 100f) + " % · Tag " + best.Day, "tt-muted");
            }
            var box = Col(mid, 6f, "tt-box");
            B(box, "Aktive Boosts", "tt-h");
            if (s.Boosts.Count == 0) T(box, "Nichts aktiv. Der Algorithmus hat dich vergessen.", "tt-muted");
            foreach (var b in s.Boosts)
                T(box, b.Name + " ×" + Fmt.Dec(b.Mult, 1) + " · noch " + UiFmt.Duration(b.EndsAt - s.BClock()), "tt-text");
        }

        private void Stat2(VisualElement parent, string k, string v, string sub)
        {
            var c = Col(parent, 2f, "tt-stat-cell");
            T(c, k, "tt-muted");
            H(c, v, "tt-stat-v");
            if (sub != "") T(c, sub, "tt-small", "up");
        }

        private void Ads(VisualElement mid, Sim s)
        {
            var box = Col(mid, 8f, "tt-box");
            B(box, "Werbekampagnen", "tt-h");
            bool running = s.ActiveBoost("ad") != null;
            for (int i = 0; i < GameData.AdTiers.Length; i++)
            {
                var t = GameData.AdTiers[i];
                var r = Row(box, 10f, "tt-ad");
                var v = Flex(Col(r, 2f));
                B(v, t.Name, "tt-text");
                T(v, "×" + Fmt.Dec(t.Mult, 1) + " Nachfrage · " + UiFmt.Duration(t.Minutes) + " · +Bekanntheit", "tt-muted");
                bool locked = s.Level < t.Level;
                int idx = i;
                int cost = s.AdCost(i);
                BtnIf(r, !locked && !running && s.Money >= cost, locked ? "Ab Level " + t.Level : (running ? "Läuft …" : "Starten · " + Fmt.Money(cost)),
                    () => S.StartAdCampaign(idx), "tt-post");
            }
        }

        // =====================================================================================
        private void Side(VisualElement side, Sim s)
        {
            B(side, "Kommentare", "tt-h");
            foreach (var c in Comments)
            {
                var r = Row(side, 8f, "tt-cm");
                r.style.alignItems = Align.FlexStart;
                var a = Div(r, "tt-cm-av");
                a.style.backgroundColor = W.Hex(c[2]);
                var v = Flex(Col(r, 1f));
                B(v, c[0], "tt-text");
                T(v, c[1], "tt-text");
                T(v, "vor " + (c[1].Length % 9 + 1) + " Std. · Antworten", "tt-small");
            }
            B(side, "Trend-Ideen für heute", "tt-h");
            int n = 0;
            foreach (var p in GameData.Products)
            {
                if (!s.ProductUnlocked(p.Id)) continue;
                var ph = s.Trends.Phase(p.Id);
                if (ph != TrendPhase.Rising && ph != TrendPhase.Peak) continue;
                string id = p.Id;
                Idea(side, "„Things TikTak made me buy“", p.Name + " · Hype ×" + Fmt.Dec(s.TrendMult(p.Id), 1), () =>
                {
                    if (TikTokStudio.Filmable(S).Contains(id)) TikTokStudio.Product = id;
                    else Toast("Dafür brauchst du " + GameData.Product(id).Name + " im Lager.");
                    Nav("post");
                });
                n++;
            }
            if (n == 0) Idea(side, "Katzen reagieren auf Dinge", "Geht immer · kein Hype gerade", () => Nav("post"));
            Idea(side, "„Wie ich mit 19 meine Firma …“", "Riskant: 30 % Cringe-Chance", () => Toast("Kommentare: „bro ist 34“."));
        }

        private void Idea(VisualElement parent, string title, string sub, Action onClick)
        {
            var b = Press(parent, onClick, "tt-idea");
            B(b, title, "tt-text");
            T(b, sub, "tt-small");
            UIX.PassThrough(b);
        }
    }
}
