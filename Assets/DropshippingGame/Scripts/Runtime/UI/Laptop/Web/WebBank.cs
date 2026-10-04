using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Revoluut Business (Neobank, clean lila, englische Texte): Kontostand, Umsätze aus den
    /// Tageszahlen, Ausgaben-Analyse (Ring + Verlauf), Kredit mit Schieberegler
    /// (<see cref="Sim.TakeLoan"/> / <see cref="Sim.RepayLoan"/>), Sparziele und Depot.
    /// Routen: "" · "analytics" · "loans" · "vaults".
    /// </summary>
    public sealed class WebBank : WebApp
    {
        public override string AppId => "bank";
        public override string SkinClass => "rv";
        public override WebSkin Skin => WebSkin.Neo;
        protected override string Domain => "app.revoluut.com/business";
        public override string Url => Domain + "/" + (string.IsNullOrEmpty(Route) ? "home" : Route);

        private static int _loan = 500;
        private Label _loanBig, _loanRate, _loanTotal;
        private Button _loanBtn;

        private static readonly Color Purple = new Color(0.24f, 0.17f, 1f);

        public override void Build()
        {
            var s = S;
            if (s == null) return;
            string r = Route ?? "";
            var rv = Row(Root, 0f, "rv-wrap");
            rv.style.alignItems = Align.Stretch;

            var nv = Wd(Col(rv, 4f, "rv-nav"), 200f);
            var lg = Row(nv, 4f, "rv-logo");
            H(lg, "Revoluut", "rv-logo-text");
            B(lg, "BIZ", "rv-logo-sup");
            NavItem(nv, "Home", "", r);
            NavItem(nv, "Analytics", "analytics", r);
            NavItem(nv, "Loans", "loans", r);
            NavItem(nv, "Vaults & Invest", "vaults", r);
            Fill(nv);
            

            var mid = Flex(Col(rv, 14f, "rv-mid"));
            if (r == "analytics") Analytics(mid, s);
            else if (r == "loans") Loans(mid, s);
            else if (r == "vaults") Vaults(mid, s);
            else Home(mid, s);

            var rt = Wd(Col(rv, 14f, "rv-right"), 300f);
            RightColumn(rt, s);
        }

        private void NavItem(VisualElement nv, string text, string route, string current)
        {
            Btn(nv, text, () => Nav(route), "rv-nav-item", route == current ? "on" : "");
        }

        // =====================================================================================
        private void Home(VisualElement mid, Sim s)
        {
            var toast = Row(mid, 10f, "rv-toast");
            B(toast, "Tip", "rv-toast-tag");
            Flex(T(toast, Insight(s), "rv-toast-text"));

            var bal = Col(mid, 2f, "rv-bal");
            B(bal, "Business · EUR", "rv-muted");
            H(bal, W.EnMoney(s.Money), "rv-bal-v");
            if (s.Debt > 0) T(bal, "Loan outstanding: " + W.EnMoney(s.Debt), "rv-muted");

            var pills = Row(mid, 22f, "rv-pills");
            Pill(pills, "+", "Add money", () => Nav("loans"));
            Pill(pills, "→", "Send", () => Toast("Sending money to Mom for garage rent … just kidding, that's automatic."));
            Pill(pills, "↗", "Invest", () => View?.OpenApp("trading"));
            Pill(pills, "…", "More", () => Nav("vaults"));

            var box = Col(mid, 2f, "rv-box");
            var bh = Row(box, 8f, "rv-box-head");
            Flex(B(bh, "Transactions", "rv-h4"));
            Btn(bh, "Analytics", () => Nav("analytics"), "rv-link");
            var tx = Transactions(s);
            if (tx.Count == 0) T(box, "No transactions yet. Suspiciously quiet.", "rv-muted");
            foreach (var t in tx) TxRow(box, t);
        }

        private void Pill(VisualElement parent, string glyph, string text, Action onClick)
        {
            var b = Press(parent, onClick, "rv-pill");
            var ic = Div(b, "rv-pill-ic");
            W.Sym(ic, glyph, "rv-pill-glyph");
            T(b, text, "rv-pill-text");
            UIX.PassThrough(b);
        }

        private sealed class Tx
        {
            public string Name, Sub, Letter;
            public int Amount;
            public Color Color;
        }

        private static List<Tx> Transactions(Sim s)
        {
            var list = new List<Tx>();
            var d = s.Daily;
            void Add(string name, string sub, int amount, string hex, string letter)
            {
                if (amount == 0) return;
                list.Add(new Tx { Name = name, Sub = sub, Amount = amount, Color = W.Hex(hex), Letter = letter });
            }
            Add("Shopifly payout", "Income · Today", d.Revenue - d.ContractIncome, "#4C6FFF", "S");
            Add("B2B contracts", "Income · Today", d.ContractIncome, "#7AC142", "B");
            Add("AllesExpress", "Stock · Today", -d.Purchases, "#FF5A36", "A");
            Add("PaketBlitz boxes", "Packaging · Today", -d.Packaging, "#FFCC00", "P");
            Add("TikTak Ads", "Marketing · Today", -d.Marketing, "#FE2C55", "T");
            Add("Refunds", "Returns · Today", -d.Refunds, "#20B3A8", "R");
            Add("Contract penalties", "Oops · Today", -d.Penalties, "#EF5350", "!");
            Add("Other income", "Rewards & events · Today", d.IncomeOther, "#0A9D58", "+");
            Add("Other spending", "Upgrades, decor, events · Today", -d.Other, "#9B6BFF", "O");
            Add("TradingViech", "Trading · Today", d.Trading, "#2962FF", "V");
            for (int i = s.History.Count - 1; i >= 0 && list.Count < 12; i--)
            {
                var h = s.History[i];
                list.Add(new Tx
                {
                    Name = "Day " + h.Day + " closing", Sub = "Net cashflow · " + h.Shipped + " parcels", Amount = h.Profit,
                    Color = new Color(0.55f, 0.55f, 0.6f), Letter = "D",
                });
            }
            return list;
        }

        private void TxRow(VisualElement box, Tx t)
        {
            var r = Row(box, 10f, "rv-tx");
            var ic = Div(r, "rv-tx-ic");
            ic.style.backgroundColor = t.Color;
            B(ic, t.Letter, "rv-tx-letter");
            var v = Flex(Col(r, 1f));
            B(v, t.Name, "rv-text");
            T(v, t.Sub, "rv-muted");
            var am = B(r, W.EnSigned(t.Amount), "rv-amount");
            if (t.Amount > 0) am.AddToClassList("pos");
        }

        private static string Insight(Sim s)
        {
            var d = s.Daily;
            if (s.Money < 0) return "Your balance is negative. Bold strategy. Below " + W.EnMoney(GameData.BankruptLimit) + " at 8 PM it's game over.";
            if (d.Marketing > 0 && d.Marketing > d.Revenue / 3) return "You spent more on ads than a third of today's revenue. Bold strategy.";
            if (d.Refunds > 0) return "Returns cost you " + W.EnMoney(d.Refunds) + " today. That's more than coffee. You don't drink coffee.";
            if (s.Debt > 0) return "Interest: " + W.EnMoney(s.InterestPerDay()) + " per day. Our algorithm (Gary) says thank you.";
            if (d.Revenue > 0) return "Revenue today: " + W.EnMoney(d.Revenue) + ". Gary is impressed. Gary is never impressed.";
            return "Open for business! Your shop hasn't sold anything today. Yet.";
        }

        // =====================================================================================
        private void Analytics(VisualElement mid, Sim s)
        {
            var d = s.Daily;
            var box = Col(mid, 8f, "rv-box");
            B(box, "Spending today", "rv-h4");
            var row = Row(box, 18f);
            var donut = new Donut(130f);
            row.Add(donut);
            var lg = Col(row, 4f, "rv-legend");
            var cats = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>("Stock", d.Purchases), new KeyValuePair<string, int>("Packaging", d.Packaging),
                new KeyValuePair<string, int>("Ads", d.Marketing), new KeyValuePair<string, int>("Returns", d.Refunds),
                new KeyValuePair<string, int>("Other", d.Other + d.Penalties), new KeyValuePair<string, int>("Fixed costs (8 PM)", s.FixedCostsPerDay()),
            };
            string[] cols = { "#FF5A36", "#FFCC00", "#FE2C55", "#20B3A8", "#9B6BFF", "#3D2CFF" };
            int total = 0;
            foreach (var c in cats) total += Math.Max(0, c.Value);
            for (int i = 0; i < cats.Count; i++)
            {
                int v = Math.Max(0, cats[i].Value);
                donut.Add(v, W.Hex(cols[i]));
                var lr = Row(lg, 6f);
                var dot = Div(lr, "rv-dot");
                dot.style.backgroundColor = W.Hex(cols[i]);
                T(lr, cats[i].Key + " " + (total > 0 ? Mathf.RoundToInt(v * 100f / total) : 0) + "% · " + W.EnMoney(v), "rv-small");
            }
            T(box, total == 0 ? "Insight: You spent nothing today. Gary is confused." :
                (d.Refunds > d.Marketing ? "Insight: Returns cost you more than ads. Maybe sell things that work?" : "Insight: Stock is an investment. Ads are a hope."), "rv-muted");

            var cb = Col(mid, 8f, "rv-box");
            B(cb, "Revenue & cashflow per day", "rv-h4");
            var chart = new LineChart { IncludeZero = true, Suffix = " €" };
            chart.style.height = 200;
            var rev = new LineChart.Series { Color = Purple, Label = "Revenue" };
            var prof = new LineChart.Series { Color = new Color(0.04f, 0.62f, 0.35f), Label = "Cashflow" };
            foreach (var h in s.History)
            {
                rev.Values.Add(h.Revenue);
                prof.Values.Add(h.Profit);
            }
            chart.Set(rev, prof);
            cb.Add(chart);
            if (s.History.Count < 2) T(cb, "Come back in a few days. Charts need history, like trust.", "rv-muted");

            var kb = Col(mid, 2f, "rv-box");
            B(kb, "Lifetime", "rv-h4");
            Kv(kb, "Total revenue", W.EnMoney(s.TotalEarned));
            Kv(kb, "Parcels shipped", Fmt.Thousands(s.TotalShipped));
            Kv(kb, "Lost orders", s.TotalLostOrders.ToString());
            float stockValue = 0f;
            foreach (var p in GameData.Products) stockValue += p.UnitCost * s.StockQty(p.Id);
            Kv(kb, "Stock value", W.EnMoney(Mathf.RoundToInt(stockValue)));
            Btn(kb, "Detailed stats (HustleOS)", () => View?.OpenApp("company/stats"), "rv-btn3");
        }

        private void Kv(VisualElement parent, string k, string v)
        {
            var r = Row(parent, 8f, "rv-kv");
            Flex(T(r, k, "rv-text"));
            B(r, v, "rv-text");
        }

        // =====================================================================================
        private void Loans(VisualElement mid, Sim s)
        {
            int avail = s.CreditAvailable();
            var box = Col(mid, 8f, "rv-box");
            B(box, "Business loan", "rv-h4");
            var bal = Col(box, 2f, "rv-bal");
            T(bal, "You want", "rv-muted");
            _loanBig = H(bal, "", "rv-bal-v", "small");
            const int step = 100;
            if (avail < step)
            {
                _loanBig.text = W.EnMoney(0);
                T(box, s.Debt > 0 ? "Your credit line is used up. Pay something back first." : "Your credit line is too small right now. Level up!", "rv-muted");
            }
            else
            {
                _loan = Mathf.Clamp(_loan / step * step, step, avail / step * step);
                UIX.Slider(box, "Amount", step, avail / step * step, _loan, step, v => W.EnMoney(Mathf.RoundToInt(v)), v =>
                {
                    _loan = Mathf.RoundToInt(v);
                    UpdateLoan();
                });
            }
            _loanRate = null;
            Kv(box, "Term", "Repay anytime");
            Kv(box, "Interest", Fmt.Dec(GameData.LoanDailyInterest * 100f, 0) + "% per day (APR: don't ask)");
            var rr = Row(box, 8f, "rv-kv");
            Flex(T(rr, "Daily interest", "rv-text"));
            _loanRate = B(rr, "", "rv-text");
            var tr = Row(box, 8f, "rv-kv");
            Flex(T(tr, "Debt after", "rv-text"));
            _loanTotal = B(tr, "", "rv-text");
            Kv(box, "Credit line", W.EnMoney(s.CreditLimit()) + " (level " + s.Level + ")");
            Kv(box, "Credit score", Score(s));
            _loanBtn = BtnIf(box, avail >= step, "Get it in 3 seconds", () =>
            {
                if (S.TakeLoan(_loan)) Toast("Money's in. Gary approved it while eating a sandwich.", "good");
            }, "rv-btn3");
            
            UpdateLoan();

            var rb = Col(mid, 8f, "rv-box");
            B(rb, "Repay", "rv-h4");
            if (s.Debt <= 0) T(rb, "You're debt-free. Gary is sad. Gary lives on interest.", "rv-muted");
            else
            {
                Kv(rb, "Outstanding", W.EnMoney(s.Debt));
                Kv(rb, "Interest per day", W.EnMoney(s.InterestPerDay()));
                var br = Row(rb, 8f);
                br.style.flexWrap = Wrap.Wrap;
                foreach (int amt in new[] { 100, 500, 2000 })
                {
                    if (amt >= s.Debt) continue;
                    int a = amt;
                    BtnIf(br, s.Money >= a, "Repay " + W.EnMoney(a), () => S.RepayLoan(a), "rv-btn2");
                }
                BtnIf(br, s.Money > 0, "Repay all", () => S.RepayLoan(S.Debt), "rv-btn3");
            }
        }

        private void UpdateLoan()
        {
            var s = S;
            if (s == null || _loanBig == null) return;
            int avail = s.CreditAvailable();
            int amt = avail >= 100 ? _loan : 0;
            _loanBig.text = W.EnMoney(amt);
            if (_loanRate != null) _loanRate.text = W.EnMoney(Mathf.Max(amt > 0 ? 1 : 0, Mathf.RoundToInt((s.Debt + amt) * GameData.LoanDailyInterest)));
            if (_loanTotal != null) _loanTotal.text = W.EnMoney(s.Debt + amt);
        }

        private static string Score(Sim s)
        {
            if (s.Level >= 8) return "\"Penthouse\" (A+)";
            if (s.Level >= 5) return "\"Warehouse\" (B)";
            if (s.Level >= 3) return "\"Garage deluxe\" (C+)";
            return "\"Garage\" (C−)";
        }

        // =====================================================================================
        private void Vaults(VisualElement mid, Sim s)
        {
            var box = Col(mid, 6f, "rv-box");
            B(box, "Vaults", "rv-h4");
            T(box, "Savings goals. Money stays in main account.", "rv-muted");
            int shown = 0;
            foreach (var u in GameData.Upgrades)
            {
                if (s.HasUpgrade(u.Id) || shown >= 3) continue;
                shown++;
                Vault(box, u.Name, s.Money, u.Cost, shown == 1 ? "#3D2CFF" : (shown == 2 ? "#FF5A36" : "#20B3A8"));
            }
            foreach (var l in GameData.Lifestyle)
            {
                if (s.LifestyleOwned.Contains(l.Id)) continue;
                Vault(box, l.Name + " (dream)", s.Money, l.Cost, "#FFB020");
                break;
            }
            Vault(box, "Taxes (don't touch)", 0, Mathf.Max(100, s.Daily.Revenue / 5), "#9B6BFF");
            Btn(box, "Open HustleOS build menu", () => View?.OpenApp("company/build"), "rv-btn2");

            var inv = Col(mid, 4f, "rv-box");
            B(inv, "Invest", "rv-h4");
            var m = s.Market;
            foreach (var a in Market.Assets)
            {
                if (!m.Prices.ContainsKey(a.Id)) continue;
                float c = m.ChangePct(a.Id);
                var r = Row(inv, 8f, "rv-kv");
                Flex(T(r, a.Name, "rv-text"));
                B(r, W.EnMoney(Mathf.RoundToInt(m.HoldingValue(a.Id))), "rv-text");
                var cl = B(r, Fmt.Pct(c), "rv-small");
                cl.AddToClassList(c >= 0 ? "pos" : "neg");
            }
            Kv(inv, "Portfolio", W.EnMoney(m.PortfolioValue()));
            string lk = LaptopView.LockedReason("trading");
            BtnIf(inv, lk == "", lk == "" ? "Open TradingViech" : "TradingViech: " + lk, () => View?.OpenApp("trading"), "rv-btn3");
        }

        private void Vault(VisualElement parent, string name, int have, int target, string hex)
        {
            var r = Row(parent, 10f, "rv-vault");
            var v = Flex(Col(r, 4f));
            B(v, name, "rv-text");
            var bar = Div(v, "rv-bar2");
            var fill = Div(bar, "rv-bar2-fill");
            fill.style.backgroundColor = W.Hex(hex);
            fill.style.width = Length.Percent(Mathf.Clamp(target > 0 ? have * 100f / target : 0f, 3f, 100f));
            var rv = Col(r, 0f);
            rv.style.alignItems = Align.FlexEnd;
            B(rv, W.EnMoney(Mathf.Min(Mathf.Max(0, have), target)), "rv-text");
            T(rv, "of " + W.EnMoney(target), "rv-muted");
        }

        // =====================================================================================
        private void RightColumn(VisualElement rt, Sim s)
        {
            var card = Col(rt, 0f, "rv-card");
            B(card, s.Level >= 8 ? "ULTRA" : (s.Level >= 5 ? "METAL" : "STANDARD"), "rv-card-tier");
            B(card, "Revoluut", "rv-card-brand");
            Div(card, "rv-card-chip");
            W.Text(card, "•••• " + (1000 + W.Hash(s.BrandName) % 9000), WebSkin.Neo, WebFonts.Body, "rv-card-no");
            T(card, s.BrandName.ToUpperInvariant(), "rv-card-name");

            var up = Col(rt, 4f, "rv-up");
            B(up, "Upgrade to Ultra Metal Pro Max", "rv-up-title");
            T(up, "€49.99/month. Heavier card.", "rv-up-text");
            Btn(up, "Upgrade", () => Toast("Nice try. Gary says your garage doesn't qualify for a heavier card."), "rv-btn2");

            var box = Col(rt, 2f, "rv-box", "grey");
            B(box, "Upcoming at 8 PM", "rv-h4");
            Kv(box, "Rent (" + GameData.StageNames[Mathf.Clamp(s.LocationStage, 0, GameData.StageNames.Length - 1)] + ")", W.EnMoney(-s.RentPerDay()));
            if (s.WagesPerDay() > 0) Kv(box, "Wages", W.EnMoney(-s.WagesPerDay()));
            if (s.UpkeepPerDay() > 0) Kv(box, "Power (conveyor)", W.EnMoney(-s.UpkeepPerDay()));
            if (s.InterestPerDay() > 0) Kv(box, "Loan interest", W.EnMoney(-s.InterestPerDay()));
            Kv(box, "Total", W.EnMoney(-s.FixedCostsPerDay()));
            Kv(box, "Card freeze", "Off");
            if (s.Money - s.FixedCostsPerDay() < GameData.BankruptLimit + 200)
                B(box, "Careful: below " + W.EnMoney(GameData.BankruptLimit) + " tonight means bankruptcy.", "rv-warn");
        }
    }
}
