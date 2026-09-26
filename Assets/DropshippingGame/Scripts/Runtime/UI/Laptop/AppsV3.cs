using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    // =========================================================================================
    // v3.0-Apps: Aufträge (B2B), Retouren, Trendradar, Skills, Wochenziele
    // Nur lesende Core-Abfragen plus die dokumentierten App-Aktionen (CORE_API §12/§14).
    // =========================================================================================

    /// <summary>Aufträge › Angebote / Laufend / Verlauf.</summary>
    public sealed class AppContracts : LaptopApp
    {
        private readonly string _mode;

        public AppContracts(string mode) => _mode = mode;

        public override string Lead
        {
            get
            {
                var s = S;
                if (s == null) return null;
                if (!s.ContractsUnlocked) return "Großaufträge gibt es ab Firmenlevel " + GameData.ContractLevel + ".";
                return "Laufend " + s.ActiveContractCount() + " / " + s.MaxActiveContracts() + " · erfüllt " + s.TotalContractsDone +
                       (s.TotalContractsFailed > 0 ? " · verpasst " + s.TotalContractsFailed : "") + ". Geliefert wird am Palettenplatz – ganze Kisten oder Einzelstücke.";
            }
        }

        public override void Build()
        {
            var s = S;
            if (!s.ContractsUnlocked)
            {
                var c = UIX.Card(Root);
                UIX.Empty(c, "pallet", "Ab Level " + GameData.ContractLevel, "Firmen bestellen palettenweise: Angebot annehmen, am Palettenplatz liefern, Frist einhalten.");
                return;
            }
            switch (_mode)
            {
                case "active": BuildActive(s); break;
                case "history": BuildHistory(s); break;
                default: BuildOffers(s); break;
            }
        }

        private void Head(VisualElement card, Contract c)
        {
            var h = UIX.Row(card, 12f);
            var p = GameData.Product(c.Product);
            UIX.Round(UIX.Swatch(h, p.Color.ToColor(), 46f, p.Icon), 14f);
            var v = Grow(UIX.Col(h, 2f));
            var tr = UIX.Row(v, 6f);
            UIX.Text(tr, c.Title, "h3");
            if (c.Special) UIX.Chip(tr, "Sonderkonditionen", "sparkle", Theme.LaptopAccent);
            UIX.Text(v, c.Company, "muted");
            var r = UIX.Col(h, 2f);
            r.style.alignItems = Align.FlexEnd;
            UIX.Num(r, Fmt.Money(c.Payment), true, "h2");
            UIX.Text(r, "+" + c.Xp + " XP", "small");
        }

        private void BuildOffers(Sim s)
        {
            var offers = s.ContractOffers();
            if (offers.Count == 0)
            {
                var c = UIX.Card(Root);
                UIX.Empty(c, "inbox", "Gerade keine Anfragen", "Werktags melden sich Firmen – mit höherem Level öfter.");
                return;
            }
            foreach (var c in offers)
            {
                var card = UIX.Card(Root, null, "card-hi");
                Head(card, c);
                P(card, c.Description, "");
                var chips = UIX.Wrap(card);
                UIX.Chip(chips, c.QualityText, "star", Theme.LaptopAccent);
                UIX.Chip(chips, "Frist " + c.Days + " Tage ab Annahme", "calendar", null);
                UIX.Chip(chips, "Strafe " + Fmt.Money(c.Penalty), "warning", Theme.LaptopBad);
                int have = s.StockQty(c.Product) + s.TravelingCountFor(c.Product) + s.DockCountFor(c.Product);
                UIX.Chip(chips, "Bestand " + have + " (Lager " + s.StockQty(c.Product) + ")", "boxes", have >= c.Quantity ? Theme.LaptopGood : (Color?)null);
                var br = UIX.Row(card, 8f);
                int days = s.ContractDaysLeft(c);
                P(br, days <= 0 ? "Angebot gilt nur noch heute." : "Angebot gilt noch " + (days == 1 ? "bis morgen." : days + " Tage."), "small");
                UIX.Spacer(br);
                int id = c.Id;
                bool ok = s.CanAcceptContract(c, out string reason);
                Btn(br, "Ablehnen", () => S.DeclineContract(id), "ghost", false, "close");
                var acc = Btn(br, "Annehmen", () => S.AcceptContract(id), "accent", !ok, "check");
                if (!ok && !string.IsNullOrEmpty(reason))
                {
                    acc.tooltip = reason;
                    P(card, reason, "bad-text");
                }
            }
        }

        private void BuildActive(Sim s)
        {
            var list = s.ActiveContracts();
            if (list.Count == 0)
            {
                var c = UIX.Card(Root);
                UIX.Empty(c, "pallet", "Kein Auftrag läuft", "Nimm unter „Angebote“ einen Auftrag an.");
                Btn(c, "Zu den Angeboten", () => Go("orders/offers"), "soft", false, "inbox").style.alignSelf = Align.Center;
                return;
            }
            foreach (var c in list)
            {
                var card = UIX.Card(Root);
                Head(card, c);
                var pr = UIX.Row(card, 10f);
                UIX.Bar(pr, c.Progress, Theme.LaptopTeal, 10f);
                UIX.Num(pr, c.Delivered + " / " + c.Quantity, true);
                var info = UIX.Wrap(card);
                int days = s.ContractDaysLeft(c);
                UIX.Chip(info, "Frist " + s.ContractDeadlineText(c), "clock", days <= 0 ? Theme.LaptopBad : (Color?)null);
                UIX.Chip(info, c.QualityText, "star", null);
                int stock = s.StockQty(c.Product), travel = s.TravelingCountFor(c.Product), dock = s.DockCountFor(c.Product);
                UIX.Chip(info, "Lager " + stock + (travel > 0 ? " · " + travel + " unterwegs" : "") + (dock > 0 ? " · " + dock + " am Eingang" : ""), "boxes",
                    stock + travel + dock >= c.Remaining ? Theme.LaptopGood : Theme.LaptopBad);
                var br = UIX.Row(card, 8f);
                P(br, "Noch " + c.Remaining + " Stück · bei Fristablauf Strafe " + Fmt.Money(c.Penalty), "small");
                UIX.Spacer(br);
                if (stock + travel + dock < c.Remaining)
                {
                    string pid = c.Product;
                    int cost = s.QuickReorderCost(pid);
                    Btn(br, "Nachbestellen · " + Fmt.Money(cost), () => S.QuickReorder(pid), "soft", s.Money < cost, "refresh");
                }
            }
            Tip(Root, "Ganze Kisten kannst du direkt auf die Palette stellen – ohne Auspacken. Die Lagerist:in bestückt Paletten automatisch aus dem Lager.");
        }

        private void BuildHistory(Sim s)
        {
            var list = s.ContractHistory();
            var top = Columns(Root);
            Stat(top, "ERFÜLLT", s.TotalContractsDone.ToString(), "good");
            Stat(top, "VERPASST", s.TotalContractsFailed.ToString(), s.TotalContractsFailed > 0 ? "bad" : null);
            Stat(top, "GLEICHZEITIG", s.ActiveContractCount() + " / " + s.MaxActiveContracts());
            var card = Card(Root, "Verlauf");
            if (list.Count == 0)
            {
                UIX.Empty(card, "list", "Noch nichts abgeschlossen");
                return;
            }
            float[] w = { -1f, 150f, 90f, 110f };
            bool[] num = { false, false, true, true };
            TableRow(card, w, new[] { "AUFTRAG", "FIRMA", "STATUS", "AUSGEZAHLT" }, true, null, num);
            bool first = true;
            foreach (var c in list)
            {
                string st = c.State == ContractState.Completed ? "erfüllt" : (c.State == ContractState.Failed ? "verpasst" : (c.State == ContractState.Declined ? "abgelehnt" : "verfallen"));
                string tone = c.State == ContractState.Completed ? "good-text" : (c.State == ContractState.Failed ? "bad-text" : "muted");
                var row = TableRow(card, w, new[] { c.Title, c.Company, st, c.State == ContractState.Completed || c.State == ContractState.Failed ? Fmt.Money(c.PaidOut) : "–" }, false,
                    new[] { "", "muted", tone, c.PaidOut < 0 ? "bad-text" : "" }, num);
                if (first) row.AddToClassList("first");
                first = false;
            }
        }
    }

    /// <summary>Retouren: unterwegs, am Wareneingang, Quoten je Produkt, Tipps.</summary>
    public sealed class AppReturns : LaptopApp
    {
        public override string Lead => "Zurückgeschickte Pakete kommen am Wareneingang an. Am Retourenplatz: als B-Ware einlagern oder entsorgen.";

        public override void Build()
        {
            var s = S;
            var top = Columns(Root);
            Stat(top, "QUOTE GESAMT", UiFmt.Percent(s.ReturnRateTotal()), s.ReturnRateTotal() > 0.08f ? "bad" : null);
            Stat(top, "AM WARENEINGANG", s.ReturnsAtDock.ToString(), s.ReturnsAtDock > 0 ? "accent" : null);
            Stat(top, "UNTERWEGS", s.ReturnsIncoming.Count.ToString());
            Stat(top, "ERSTATTET", Fmt.Money(s.TotalRefunds), s.TotalRefunds > 0 ? "bad" : null, s.TotalReturnsRestocked + " B-Ware · " + s.TotalReturnsDisposed + " entsorgt");

            var h = Columns(Root, 14f);
            var dock = Grow(Card(h, "Am Wareneingang", s.ReturnsAtDock > 0 ? s.ReturnsAtDock + " Pakete" : null));
            if (s.DockReturns.Count == 0) UIX.Empty(dock, "check_circle", "Nichts zu tun");
            int n = 0;
            foreach (var r in s.DockReturns)
            {
                if (++n > 10) break;
                ReturnRow(dock, r, "Grund: " + (string.IsNullOrEmpty(r.Note) ? "keine Angabe" : r.Note), "−" + Fmt.Money(r.Price));
            }
            var inc = Grow(Card(h, "Kommen noch", s.ReturnsIncoming.Count > 0 ? s.ReturnsIncoming.Count.ToString() : null));
            if (s.ReturnsIncoming.Count == 0) UIX.Empty(inc, "truck", "Keine Rücksendung unterwegs");
            n = 0;
            foreach (var r in s.ReturnsIncoming)
            {
                if (++n > 10) break;
                ReturnRow(inc, r.Item, (string.IsNullOrEmpty(r.Item.Customer) ? "" : r.Item.Customer + " · ") + "Ankunft " + s.BClockText(r.ArriveAt), "−" + Fmt.Money(r.Item.Price));
            }

            var tc = Card(Root, "Quote je Produkt");
            float[] w = { -1f, 90f, 110f, 110f };
            bool[] num = { false, true, true, true };
            TableRow(tc, w, new[] { "PRODUKT", "RETOUREN", "BISHER", "ERWARTET" }, true, null, num);
            bool first = true;
            foreach (var p in GameData.Products)
            {
                if (!s.ProductUnlocked(p.Id)) continue;
                int cnt = s.ReturnsPerProduct.TryGetValue(p.Id, out int c) ? c : 0;
                float exp = s.ExpectedReturnRate(p.Id);
                var row = TableRow(tc, w, new[] { p.Name, cnt.ToString(), UiFmt.Percent(s.ReturnRate(p.Id)), UiFmt.Percent(exp) }, false,
                    new[] { "", "muted", "", exp > 0.08f ? "bad-text" : "good-text" }, num);
                if (first) row.AddToClassList("first");
                first = false;
            }
            Tip(Root, "Billig-Ware kommt deutlich öfter zurück, Premium kaum. Zu große Kartons und verspätete Pakete erhöhen die Quote. Skill „Kundenflüsterer“ senkt sie um ein Drittel.");
        }

        private static void ReturnRow(VisualElement parent, ItemData r, string sub, string right)
        {
            var p = GameData.IsProduct(r.Product) ? GameData.Product(r.Product) : null;
            var row = UIX.Row(parent, 10f, "feed-item");
            UIX.Swatch(row, p != null ? p.Color.ToColor() : Theme.LaptopMuted, 32f, "return");
            var mid = Grow(UIX.Col(row, 0f));
            UIX.Ellipsis(UIX.Text(mid, p != null ? p.Name : r.Describe(), "feed-title"));
            var sl = UIX.Text(mid, sub, "feed-sub");
            sl.style.whiteSpace = WhiteSpace.Normal;
            UIX.Num(row, right, true, "bad-text");
        }
    }

    /// <summary>Markt & Trends › Trendradar: Hype je Produkt, Verlauf und Prognose mit Band.</summary>
    public sealed class AppTrends : LaptopApp
    {
        public override string Lead
        {
            get
            {
                var s = S;
                if (s == null) return null;
                return "Hype multipliziert die Nachfrage (×0,5 bis ×2). Prognose " + s.TrendSightDays() + " Tag(e), Genauigkeit " + UiFmt.Percent(s.TrendForecastAccuracy()) + ".";
            }
        }

        public override void Build()
        {
            var s = S;
            if (!s.HasSkill("m_radar"))
                Tip(Root, "Prognose ungenau – Skill „Trendradar“ (Marketing) macht sie genauer und zeigt 3 Tage.");
            var grid = UIX.Wrap(Root);
            foreach (var p in GameData.Products)
            {
                if (!s.ProductUnlocked(p.Id)) continue;
                var st = s.Trends.State(p.Id);
                var fc = s.Trends.Forecast(p.Id, Mathf.Max(1, s.TrendSightDays()));
                var card = UIX.Card(grid, null, fc.Phase == TrendPhase.Rising || fc.Phase == TrendPhase.Peak ? "card-hi" : null);
                card.style.width = 392;
                var h = UIX.Row(card, 10f);
                UIX.Round(UIX.Swatch(h, p.Color.ToColor(), 38f, p.Icon), 12f);
                var v = Grow(UIX.Col(h, 0f));
                UIX.Text(v, p.Name, "h3");
                UIX.Text(v, "#" + TrendSystem.HashTag(p.Id) + (s.IsListed(p.Id) ? "" : " · nicht online"), "small");
                bool down = fc.Phase == TrendPhase.Falling || fc.Phase == TrendPhase.Dead;
                var r = UIX.Col(h, 0f);
                r.style.alignItems = Align.FlexEnd;
                UIX.Num(r, "×" + Fmt.Dec(s.TrendMult(p.Id), 2), true, down ? "bad-text" : (s.TrendMult(p.Id) > 1.05f ? "good-text" : ""));
                UIX.Chip(r, fc.Label, Icons.Has(fc.Icon) ? fc.Icon : "dot", down ? Theme.LaptopBad : Theme.LaptopAccent);

                var chart = new TrendChart(st.History, fc, Theme.LaptopAccent);
                chart.style.height = 92;
                chart.style.marginTop = 6;
                card.Add(chart);
                var legend = UIX.Row(card, 8f);
                P(legend, "14 Tage", "small");
                UIX.Spacer(legend);
                var fr = new System.Text.StringBuilder("Prognose: ");
                for (int i = 0; i < fc.Values.Length; i++)
                {
                    if (i > 0) fr.Append(" → ");
                    fr.Append("×").Append(Fmt.Dec(fc.Values[i], 1));
                }
                P(legend, fr.ToString(), "small");
                if (!string.IsNullOrEmpty(st.Reason) && fc.Phase != TrendPhase.Normal) P(card, st.Reason, "");
                if (!string.IsNullOrEmpty(fc.Hint)) P(card, fc.Hint, "small");
            }
        }
    }

    /// <summary>Hype-Verlauf (Linie) + Prognose (gestrichelte Punkte mit Unsicherheitsband).</summary>
    public sealed class TrendChart : VisualElement
    {
        private readonly List<float> _hist = new List<float>();
        private readonly TrendForecast _fc;
        private readonly Color _color;

        public TrendChart(IList<float> history, TrendForecast fc, Color color)
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("trend-chart");
            if (history != null) _hist.AddRange(history);
            _fc = fc;
            if (fc != null) _hist.Add(fc.Now);
            _color = color;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width < 20f || r.height < 20f) return;
            int fcN = _fc != null && _fc.Values != null ? _fc.Values.Length : 0;
            int total = Mathf.Max(2, _hist.Count + fcN);
            const float lo = 0.4f, hi = 2.1f;
            float X(int i) => 4f + (r.width - 8f) * i / (total - 1);
            float Y(float v) => 4f + (r.height - 8f) * (1f - Mathf.InverseLerp(lo, hi, v));
            var m = new UIDraw.MeshBuilder();
            var grid = new Color(0.5f, 0.5f, 0.55f, 0.22f);
            m.Line(new Vector2(0f, Y(1f)), new Vector2(r.width, Y(1f)), 1f, grid);
            if (_hist.Count > 0 && fcN > 0)
            {
                int start = _hist.Count - 1;
                var band = new Color(_color.r, _color.g, _color.b, 0.18f);
                float prevLo = _fc.Now, prevHi = _fc.Now;
                for (int i = 0; i < fcN; i++)
                {
                    float lw = i < _fc.Low.Length ? _fc.Low[i] : _fc.Values[i];
                    float hg = i < _fc.High.Length ? _fc.High[i] : _fc.Values[i];
                    float x0 = X(start + i), x1 = X(start + i + 1);
                    m.Quad(new Vector2(x0, Y(prevHi)), new Vector2(x1, Y(hg)), new Vector2(x1, Y(lw)), new Vector2(x0, Y(prevLo)), band);
                    prevLo = lw;
                    prevHi = hg;
                }
                var fcol = new Color(_color.r, _color.g, _color.b, 0.75f);
                float pv = _fc.Now;
                for (int i = 0; i < fcN; i++)
                {
                    var a = new Vector2(X(start + i), Y(pv));
                    var b = new Vector2(X(start + i + 1), Y(_fc.Values[i]));
                    for (int d = 0; d < 4; d++)
                        m.Line(Vector2.Lerp(a, b, d / 4f), Vector2.Lerp(a, b, d / 4f + 0.14f), 2f, fcol);
                    m.Circle(b, 3.5f, fcol, 12);
                    pv = _fc.Values[i];
                }
            }
            if (_hist.Count >= 2)
            {
                var pts = new List<Vector2>();
                for (int i = 0; i < _hist.Count; i++) pts.Add(new Vector2(X(i), Y(_hist[i])));
                m.Polyline(pts, 2.5f, _color);
                m.Circle(pts[pts.Count - 1], 4.5f, _color, 16);
            }
            else if (_hist.Count == 1) m.Circle(new Vector2(X(0), Y(_hist[0])), 4.5f, _color, 16);
            m.Flush(ctx);
        }
    }

    /// <summary>Firma › Skills: drei Äste mit je vier Stufen, Lernen und Umschulung.</summary>
    public sealed class AppSkills : LaptopApp
    {
        private static float _confirmUntil = -10f;

        public override string Lead => "1 Skillpunkt zum Start und je Level-Aufstieg. Stufe 2–4 brauchen die Vorstufe und Level 3 / 5 / 7. Alle 12 schaffst du nicht – wähle bewusst.";

        public override void Build()
        {
            var s = S;
            var top = UIX.Card(Root, null, s.SkillPointsAvailable() > 0 ? "card-hi" : null);
            var th = UIX.Row(top, 12f);
            var badge = UIX.Div(th, "level-badge");
            UIX.Text(badge, s.SkillPointsAvailable().ToString(), "level-badge-text");
            var tv = Grow(UIX.Col(th, 2f));
            UIX.Text(tv, s.SkillPointsAvailable() == 1 ? "1 Skillpunkt frei" : s.SkillPointsAvailable() + " Skillpunkte frei", "h2");
            UIX.Text(tv, s.Skills.Count + " gelernt · " + s.SkillPointsTotal() + " Punkte insgesamt", "small");
            bool confirm = Time.unscaledTime < _confirmUntil;
            int cost = s.RespecCost();
            Btn(th, confirm ? "Wirklich zurücksetzen? · " + Fmt.Money(cost) : "Umschulung · " + Fmt.Money(cost), () =>
            {
                if (Time.unscaledTime < _confirmUntil)
                {
                    _confirmUntil = -10f;
                    S.ResetSkills();
                }
                else _confirmUntil = Time.unscaledTime + 4f;
                Rebuild();
            }, confirm ? "danger" : "ghost", s.Skills.Count == 0 || s.Money < cost, "refresh");

            var cols = Columns(Root, 12f);
            foreach (var br in GameData.SkillBranches)
            {
                var col = Grow(UIX.Col(cols, 8f));
                var bc = br.Color.ToColor();
                var head = UIX.Card(col);
                var hh = UIX.Row(head, 10f);
                UIX.Round(UIX.Swatch(hh, bc, 36f, br.Icon), 12f);
                var hv = Grow(UIX.Col(hh, 0f));
                UIX.Text(hv, br.Name, "h3");
                var d = UIX.Text(hv, br.Desc, "small");
                d.style.whiteSpace = WhiteSpace.Normal;
                foreach (var sk in Sim.SkillsOfBranch(br.Id))
                {
                    string state = s.SkillState(sk.Id);
                    bool learned = state == "learned";
                    var node = UIX.Card(col, null, learned ? "skill-learned" : (state == "available" ? "card-hi" : null));
                    node.AddToClassList("skill-node");
                    if (learned) node.style.borderLeftColor = bc;
                    var nh = UIX.Row(node, 10f);
                    UIX.Round(UIX.Swatch(nh, learned ? bc : new Color(0.5f, 0.52f, 0.58f, 0.35f), 30f, Icons.Has(sk.Icon) ? sk.Icon : br.Icon), 10f);
                    var nv = Grow(UIX.Col(nh, 0f));
                    UIX.Text(nv, sk.Name, "h3");
                    UIX.Text(nv, "Stufe " + sk.Tier + " · ab Level " + sk.Level, "small");
                    var desc = P(node, sk.Desc, learned ? "" : "muted");
                    desc.style.marginTop = 4;
                    string id = sk.Id;
                    switch (state)
                    {
                        case "learned":
                            UIX.Chip(node, "Gelernt", "check", Theme.LaptopGood).style.alignSelf = Align.FlexStart;
                            break;
                        case "available":
                            Btn(node, "Lernen · 1 Punkt", () => S.LearnSkill(id), "accent", false, "sparkle").style.alignSelf = Align.FlexStart;
                            break;
                        case "level":
                            Btn(node, "Ab Level " + sk.Level, null, "", true, "lock").style.alignSelf = Align.FlexStart;
                            break;
                        case "requires":
                            Btn(node, "Erst die Vorstufe", null, "", true, "lock").style.alignSelf = Align.FlexStart;
                            break;
                        case "points":
                            Btn(node, "Kein Skillpunkt frei", null, "", true, "lock").style.alignSelf = Align.FlexStart;
                            break;
                    }
                }
            }
        }
    }

    /// <summary>Wochenziele – gemeinsamer Baustein für Laptop (Firma › Ziele) und Handy.</summary>
    public static class WeeklyUi
    {
        public static string Summary(Sim s)
        {
            int total = s.Challenges.Count;
            int done = s.ChallengesDoneThisWeek();
            int left = s.DaysLeftInWeek();
            return "Woche " + s.Week + " · " + done + " / " + total + " geschafft · " + (left == 0 ? "endet heute Abend" : "noch " + left + (left == 1 ? " Tag" : " Tage"));
        }

        /// <summary>Liste der Wochenziele. compact = Handy.</summary>
        public static void Build(VisualElement parent, Sim s, bool compact, Action changed)
        {
            if (s.Challenges.Count == 0)
            {
                UIX.Empty(parent, "flag", "Noch keine Wochenziele", "Jeden Montag gibt es drei neue.");
                return;
            }
            foreach (var c in s.Challenges)
            {
                var row = UIX.Col(parent, 4f, compact ? "phone-item" : "table-row", "challenge");
                row.style.alignItems = Align.Stretch;
                if (c.Bonus) row.AddToClassList("challenge-bonus");
                var h = UIX.Row(row, 10f);
                UIX.Icon(h, c.Done ? "check_circle" : (Icons.Has(c.Icon) ? c.Icon : "flag"), 18f, c.Done ? Theme.LaptopGood : (c.Bonus ? Theme.LaptopAccent : (Color?)null));
                var v = UIX.Col(h, 0f);
                v.style.flexGrow = 1;
                v.style.flexShrink = 1;
                var t = UIX.Text(v, (c.Bonus && !string.IsNullOrEmpty(c.Sponsor) ? c.Sponsor + ": " : "") + c.Title, compact ? "phone-item-title" : "h3");
                t.style.whiteSpace = WhiteSpace.Normal;
                if (!compact || !c.Done)
                {
                    var d = UIX.Text(v, c.Desc, compact ? "phone-item-sub" : "small");
                    d.style.whiteSpace = WhiteSpace.Normal;
                }
                var rw = UIX.Col(h, 0f);
                rw.style.alignItems = Align.FlexEnd;
                UIX.Num(rw, "+" + Fmt.Money(c.Reward), true, c.Done ? "muted" : "accent-text");
                UIX.Text(rw, "+" + c.Xp + " XP", compact ? "phone-item-sub" : "small");
                var pr = UIX.Row(row, 8f);
                UIX.Bar(pr, c.Fraction, c.Done ? Theme.LaptopGood : Theme.LaptopTeal, 6f);
                UIX.Num(pr, c.ProgressText, false, compact ? "phone-item-sub" : "small");
                if (!c.Done && s.CanRerollChallenge(c.Id))
                {
                    string id = c.Id;
                    var b = UIX.Button(row, "Tauschen (1× pro Woche)", () =>
                    {
                        Game.Sim?.RerollChallenge(id);
                        changed?.Invoke();
                    }, "ghost", false, "refresh");
                    b.AddToClassList("btn-sm");
                    b.style.alignSelf = Align.FlexStart;
                }
            }
        }
    }
}
