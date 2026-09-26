using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// TradingViech (Comic, dunkel): Kerzen-Chart aus dem Kursverlauf des Markts, Beobachtungsliste,
    /// Depot und Kaufen/Verkaufen über <see cref="Market.Buy"/> / <see cref="Market.Sell"/>.
    /// Aktualisiert sich bei jedem Trading-Tick ohne Neuaufbau.
    /// Route: "{Symbol}".
    /// </summary>
    public sealed class WebTrading : WebApp, ILiveApp
    {
        public override string AppId => "trading";
        public override string SkinClass => "tv";
        public override WebSkin Skin => WebSkin.Comic;
        protected override string Domain => "tradingviech.com/chart";

        private static int _tf = 1;
        private static readonly string[] Tf = { "1T", "1W", "1M", "ALLES" };
        private static readonly int[] TfPoints = { 40, 90, 180, 100000 };

        private static readonly string[] Quips =
        {
            "Das ist garantiert der Boden.", "Linie geht hoch = gut. Glaube ich.", "Onkel Ralf sagt: jetzt kaufen.",
            "Nicht Finanzberatung. Eher Finanzraten.", "Der Kurs fühlt sich heute nach Tee an.", "Buy high, sell low. Oder umgekehrt?",
        };

        private CandleChart _chart;
        private Label _price, _hold, _pl, _depot, _cash;
        private readonly Dictionary<string, Label[]> _wl = new Dictionary<string, Label[]>();

        private string Sel
        {
            get
            {
                var m = S?.Market;
                string r = Route ?? "";
                if (m != null && m.Prices.ContainsKey(r)) return r;
                return Market.Assets.Length > 0 ? Market.Assets[0].Id : "";
            }
        }

        public override void Build()
        {
            var s = S;
            if (s == null || Market.Assets.Length == 0) return;
            var m = s.Market;
            string sel = Sel;
            if (!m.Prices.ContainsKey(sel)) return;
            var asset = Market.Asset(sel);
            _wl.Clear();

            var top = Row(Root, 14f, "tv-top");
            H(top, "TradingViech", "tv-logo");
            B(top, sel, "tv-sym");
            T(top, asset.Name, "tv-text");
            var tf = Row(top, 2f, "tv-tf");
            for (int i = 0; i < Tf.Length; i++)
            {
                int ti = i;
                Btn(tf, Tf[i], () =>
                {
                    _tf = ti;
                    Rebuild();
                }, "tv-tf-btn", i == _tf ? "on" : "");
            }
            Fill(top);
            _price = B(top, "", "tv-price");

            var body = Row(Root, 0f, "tv-body");
            body.style.alignItems = Align.Stretch;
            var chartBox = Flex(Col(body, 0f, "tv-chart"));
            _chart = new CandleChart();
            _chart.style.flexGrow = 1;
            chartBox.Add(_chart);
            var quip = Col(chartBox, 0f, "tv-quip");
            B(quip, Quips[Mathf.Abs(W.Hash(sel)) % Quips.Length], "tv-quip-text");
            Div(quip, "tv-quip-tail");
            T(chartBox, asset.Desc + " · " + Mathf.RoundToInt(Market.Fee * 100f) + " % Gebühr pro Kauf und Verkauf.", "tv-note");

            var side = Wd(Col(body, 0f, "tv-side"), 270f);
            var wh = Row(side, 6f, "tv-wl-head");
            Flex(T(wh, "Beobachtungsliste", "tv-muted"));
            foreach (var a in Market.Assets)
            {
                string id = a.Id;
                var r = Press(side, () => Nav(id), "tv-wl-row");
                r.EnableInClassList("on", id == sel);
                var rr = Row(r, 6f);
                var name = Flex(Col(rr, 0f));
                B(name, a.Id, "tv-text");
                T(name, a.Name, "tv-small");
                var price = Wd(T(rr, "", "tv-text", "right"), 70f);
                var chg = Wd(B(rr, "", "tv-text", "right"), 62f);
                _wl[id] = new[] { price, chg };
                UIX.PassThrough(r);
            }
            var order = Col(side, 6f, "tv-order");
            _depot = B(order, "", "tv-text");
            _cash = T(order, "", "tv-small");
            _hold = T(order, "", "tv-small");
            _pl = B(order, "", "tv-text");
            T(order, "Kaufen", "tv-muted");
            var buy = Div(order, "tv-btnrow");
            foreach (int amt in new[] { 50, 200, 1000 })
            {
                int a = amt;
                BtnIf(buy, s.Money >= a, Fmt.Money(a), () => S.Market.Buy(Sel, a), "tv-btn", "buy");
            }
            BtnIf(buy, s.Money >= 8, "25 %", () => S.Market.Buy(Sel, (int)(S.Money * 0.25f)), "tv-btn", "buy");
            T(order, "Verkaufen", "tv-muted");
            var sell = Div(order, "tv-btnrow");
            bool has = m.Holdings.TryGetValue(sel, out float hq) && hq > 0f;
            foreach (float f in new[] { 0.25f, 0.5f, 1f })
            {
                float fr = f;
                BtnIf(sell, has, fr >= 1f ? "Alles" : Mathf.RoundToInt(fr * 100f) + " %", () =>
                {
                    int v = S.Market.Sell(Sel, fr);
                    if (v > 0) Toast("Verkauft für " + Fmt.Money(v) + ".", "good");
                }, "tv-btn", "sell");
            }
            T(order, "Revoluut-Kreditgeld hier reinzustecken ist mutig. Gary schaut zu.", "tv-small");
            LiveUpdate();
        }

        public void LiveUpdate()
        {
            var s = S;
            if (s == null || _chart == null) return;
            var m = s.Market;
            string sel = Sel;
            if (!m.Prices.ContainsKey(sel) || !m.History.ContainsKey(sel)) return;
            var hist = m.History[sel];
            int pts = TfPoints[Mathf.Clamp(_tf, 0, TfPoints.Length - 1)];
            int from = Mathf.Max(0, hist.Count - pts);
            var slice = new List<float>(hist.Count - from);
            for (int i = from; i < hist.Count; i++) slice.Add(hist[i]);
            _chart.Set(slice, Mathf.Min(48, Mathf.Max(8, slice.Count / 2)));
            float first = slice.Count > 0 ? slice[0] : m.Prices[sel];
            float ch = first > 0f ? m.Prices[sel] / first - 1f : 0f;
            _price.text = Fmt.Eur(m.Prices[sel]) + " (" + Fmt.Pct(ch) + ")";
            _price.EnableInClassList("up", ch >= 0f);
            _price.EnableInClassList("dn", ch < 0f);
            foreach (var kv in _wl)
            {
                if (!m.Prices.ContainsKey(kv.Key)) continue;
                float c = m.ChangePct(kv.Key);
                kv.Value[0].text = Fmt.Dec(m.Prices[kv.Key], 2);
                kv.Value[1].text = Fmt.Pct(c);
                kv.Value[1].EnableInClassList("up", c >= 0f);
                kv.Value[1].EnableInClassList("dn", c < 0f);
            }
            _depot.text = "Depot: " + Fmt.Money(m.PortfolioValue());
            _cash.text = "Konto: " + Fmt.Money(s.Money);
            float h = m.Holdings.TryGetValue(sel, out float hq) ? hq : 0f;
            _hold.text = "Bestand " + sel + ": " + Fmt.Dec(h, 3) + " Anteile · " + Fmt.Money(m.HoldingValue(sel));
            float pl = m.Profit(sel);
            _pl.text = "G/V: " + Fmt.SignedMoney(Mathf.RoundToInt(pl));
            _pl.EnableInClassList("up", pl >= 0f);
            _pl.EnableInClassList("dn", pl < 0f);
        }
    }
}
