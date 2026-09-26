using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Tagesabrechnung als Kassenbon: Zeilen erscheinen nacheinander und die Beträge zählen hoch,
    /// unten das Tagesergebnis mit Stempel (GEWINN / VERLUST / PLEITE) und Strichcode.
    /// Erweiterungspunkt (Phase B): <see cref="ExtraLines"/> für Retouren, Aufträge, Wochenziele.
    /// </summary>
    public static class SummaryView
    {
        /// <summary>Zusätzliche Bon-Zeilen (Bezeichnung, Betrag in €, true = Kosten).</summary>
        public static Func<Sim, DaySummary, List<KeyValuePair<string, int>>> ExtraLines;

        private static readonly Color Ink = new Color(0.16f, 0.15f, 0.13f);

        public static void Show(ModalView modal, Sim sim, DaySummary s, Action onNext)
        {
            if (modal == null || s == null) return;
            var body = modal.Open("Feierabend – Tag " + s.Day, "Zeit für die Abrechnung.", 470, new[]
            {
                new ModalButton("Nächster Tag", onNext, "accent", "arrow_right"),
            }, true, "summary", null, null, "plain");
            string brand = sim != null ? sim.BrandName : "MeinShop";

            var wrap = UIX.Col(body, 0f, "receipt-wrap");
            wrap.Add(new ReceiptEdge(true, Theme.Paper));
            var r = UIX.Col(wrap, 0f, "receipt");

            var top = UIX.Col(r, 2f);
            top.style.alignItems = Align.Center;
            UIX.Icon(top, "receipt", 24f, Ink);
            Center(UIX.Text(top, brand.ToUpperInvariant(), "receipt-brand", "receipt-text", "num-b"));
            Center(UIX.Text(top, "KASSENBON · TAG " + s.Day, "receipt-text", "num"));
            Center(UIX.Text(top, (string.IsNullOrEmpty(s.WeekdayName) ? UiFmt.Weekday(s.Day) : s.WeekdayName) + " · 20:00 Uhr", "receipt-text", "receipt-muted", "num"));

            var lines = new List<VisualElement>();
            int delay = 0;
            Sep(r, false);
            lines.Add(Line(r, "Pakete verschickt", s.Shipped, v => v.ToString(), null, ref delay));
            if (s.Express > 0) lines.Add(Line(r, "  davon Express", s.Express, v => v.ToString(), "receipt-muted", ref delay));
            if (s.Late > 0) lines.Add(Line(r, "  davon verspätet", s.Late, v => v.ToString(), "bad-text", ref delay));
            if (s.Lost > 0) lines.Add(Line(r, "Bestellungen verloren", s.Lost, v => v.ToString(), "bad-text", ref delay));
            if (s.Expired > 0) lines.Add(Line(r, "  davon storniert", s.Expired, v => v.ToString(), "receipt-muted", ref delay));
            if (s.Returns > 0) lines.Add(Line(r, "Retouren", s.Returns, v => v.ToString(), "bad-text", ref delay));
            if (s.ReturnsRestocked + s.ReturnsDisposed > 0)
                lines.Add(Line(r, "  B-Ware / entsorgt", s.ReturnsRestocked, v => v + " / " + s.ReturnsDisposed, "receipt-muted", ref delay));
            if (s.ContractsDone > 0) lines.Add(Line(r, "Großaufträge erfüllt", s.ContractsDone, v => v.ToString(), "good-text", ref delay));
            if (s.ContractsFailed > 0) lines.Add(Line(r, "Großaufträge verpasst", s.ContractsFailed, v => v.ToString(), "bad-text", ref delay));
            if (s.ChallengesDone > 0) lines.Add(Line(r, "Wochenziele geschafft", s.ChallengesDone, v => v.ToString(), "good-text", ref delay));
            lines.Add(Line(r, "Erfahrung", s.XpGained, v => "+" + v + " XP", null, ref delay));
            Sep(r, false);
            lines.Add(Line(r, "Umsatz", s.Revenue, v => Fmt.Money(v), "good-text", ref delay));
            if (s.Stand > 0) lines.Add(Line(r, "  davon Verkaufsstand", s.Stand, v => Fmt.Money(v), "receipt-muted", ref delay));
            if (s.ContractIncome > 0) lines.Add(Line(r, "  davon Großaufträge", s.ContractIncome, v => Fmt.Money(v), "receipt-muted", ref delay));
            if (s.IncomeOther != 0) lines.Add(Line(r, "Sonstige Einnahmen", s.IncomeOther, v => Fmt.Money(v), "good-text", ref delay));
            if (s.ChallengeRewards > 0) lines.Add(Line(r, "  davon Wochenziele", s.ChallengeRewards, v => Fmt.Money(v), "receipt-muted", ref delay));

            void Cost(string label, int v)
            {
                if (v != 0) lines.Add(Line(r, label, -v, x => Fmt.Money(x), "bad-text", ref delay));
            }

            Cost("Wareneinkauf", s.Purchases);
            Cost("Verpackung", s.Packaging);
            Cost("Marketing", s.Marketing);
            Cost("Sonstiges", s.Other);
            Cost("Erstattungen (Retouren)", s.Refunds);
            Cost("Vertragsstrafen", s.Penalties);
            Cost("Miete", s.Rent);
            Cost("Löhne", s.Wages);
            Cost("Strom Förderband", s.Upkeep);
            Cost("Kreditzinsen", s.Interest);
            if (ExtraLines != null)
            {
                try
                {
                    var extra = ExtraLines(sim, s);
                    if (extra != null)
                        foreach (var kv in extra)
                            lines.Add(Line(r, kv.Key, kv.Value, x => Fmt.Money(x), kv.Value < 0 ? "bad-text" : "good-text", ref delay));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            Sep(r, true);
            var totalRow = UIX.Div(r, "receipt-row");
            UIX.Text(totalRow, "TAGESERGEBNIS", "receipt-key", "receipt-text", "receipt-strong", "num-b");
            var total = UIX.Text(totalRow, Fmt.SignedMoney(s.Profit), "receipt-total", s.Profit >= 0 ? "good-text" : "bad-text");
            Fonts.AddClass(total, "num-b");
            UIX.CountUp(total, s.Profit, v => Fmt.SignedMoney(v), 0.9f, 0.08f * delay + 0.1f);
            Sep(r, false);

            var after = UIX.Div(r, "receipt-row");
            UIX.Text(after, "Kontostand danach", "receipt-key", "receipt-text", "num");
            UIX.Text(after, Fmt.Money(s.MoneyAfter), "receipt-text", "receipt-strong", "num-b", s.MoneyAfter < 0 ? "bad-text" : null);
            if (s.Debt > 0) Plain(r, "Offener Kredit", Fmt.Money(s.Debt), "bad-text");
            if (s.Trading != 0) Plain(r, "Trading (Käufe/Verkäufe)", Fmt.SignedMoney(s.Trading), s.Trading >= 0 ? "good-text" : "bad-text");
            float dr = s.RepEnd - s.RepStart;
            Plain(r, "Bewertung", Fmt.Rating(s.RepEnd) + " (" + (dr >= 0 ? "+" : "−") + Fmt.Dec(Mathf.Abs(dr), 2) + ")", dr >= 0 ? "good-text" : "bad-text");

            // Stempel
            string stampText = s.Bankrupt ? "PLEITE" : (s.Profit >= 0 ? "GEWINN" : "VERLUST");
            var stamp = UIX.Div(r, "receipt-stamp");
            stamp.style.position = Position.Absolute;
            if (s.Bankrupt || s.Profit < 0) stamp.AddToClassList("bad");
            UIX.Text(stamp, stampText, "receipt-stamp-text", "num-b");
            stamp.style.opacity = 0f;
            float stampDelay = 0.08f * delay + 0.9f;
            Anim.Run(0.22f, t =>
            {
                stamp.style.opacity = t * 0.85f;
                float sc = Mathf.Lerp(1.6f, 1f, t);
                stamp.style.scale = new Scale(new Vector3(sc, sc, 1f));
            }, () => Game.Sound("place", 0.05f, -4f), Ease.OutCubic, true, stamp, stampDelay);

            if (s.WeekEnded)
            {
                Sep(r, false);
                Plain(r, "Wochenbilanz", s.WeekChallengesDone + " von " + s.WeekChallengesTotal + " Zielen", s.WeekChallengesDone >= s.WeekChallengesTotal ? "good-text" : null);
            }
            if (s.Notes != null && s.Notes.Count > 0)
            {
                Sep(r, false);
                int nn = 0;
                foreach (var note in s.Notes)
                {
                    if (++nn > 6) break;
                    var nl = UIX.Text(r, "· " + note, "receipt-text", "receipt-muted");
                    nl.style.whiteSpace = WhiteSpace.Normal;
                }
            }
            if (s.ReturnsIncoming > 0 || s.ContractsActive > 0)
                Plain(r, "Offen", (s.ReturnsIncoming > 0 ? s.ReturnsIncoming + " Retouren unterwegs" : "") + (s.ReturnsIncoming > 0 && s.ContractsActive > 0 ? " · " : "") + (s.ContractsActive > 0 ? s.ContractsActive + " Aufträge laufen" : ""), null);

            Barcode(r, s.Day);
            Center(UIX.Text(r, "Danke für deinen Einkauf bei " + brand + "! Bis morgen.", "receipt-text", "receipt-muted")).style.marginTop = 6;
            wrap.Add(new ReceiptEdge(false, Theme.Paper));

            if (s.Bankrupt)
                UIX.Chip(body, "Dein Konto fällt unter " + Fmt.Money(GameData.BankruptLimit) + " – das ist die Insolvenz!", "warning", Theme.Bad, "receipt-note");
            else if (s.MoneyAfter < 0)
                UIX.Chip(body, "Du bist im Minus. Ab " + Fmt.Money(GameData.BankruptLimit) + " ist Schluss – verkauf mehr, spar Kosten oder nimm einen Kredit.", "warning", Theme.Accent, "receipt-note");

            for (int i = 0; i < lines.Count; i++) UIX.FadeSlideIn(lines[i], 4f, 0.16f, 0.08f * i + 0.1f);
            Game.Sound("printer", 0.02f, -4f);
        }

        private static Label Center(Label l)
        {
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            return l;
        }

        private static void Sep(VisualElement parent, bool strong) => UIX.Div(parent, strong ? "receipt-sep-strong" : "receipt-sep");

        private static VisualElement Line(VisualElement parent, string key, int value, Func<int, string> fmt, string tone, ref int index)
        {
            var row = UIX.Div(parent, "receipt-row");
            UIX.Text(row, key, "receipt-key", "receipt-text", "num");
            var v = UIX.Text(row, fmt(value), "receipt-text", "receipt-strong", tone);
            Fonts.AddClass(v, "num-b");
            UIX.CountUp(v, value, fmt, 0.5f, 0.08f * index + 0.12f);
            index++;
            return row;
        }

        private static void Plain(VisualElement parent, string key, string value, string tone)
        {
            var row = UIX.Div(parent, "receipt-row");
            UIX.Text(row, key, "receipt-key", "receipt-text", "num");
            var v = UIX.Text(row, value, "receipt-text", "receipt-strong", tone);
            Fonts.AddClass(v, "num-b");
        }

        private static void Barcode(VisualElement parent, int seed)
        {
            var bc = UIX.Div(parent, "barcode");
            var rng = new System.Random(seed * 7919 + 17);
            for (int i = 0; i < 64; i++)
            {
                var line = UIX.Div(bc, "barcode-line");
                line.style.width = rng.Next(1, 4);
                line.style.marginRight = rng.Next(1, 3);
            }
        }
    }

    /// <summary>Ereignis-Fenster (Entscheidung) und Ergebnis-Fenster.</summary>
    public static class EventView
    {
        public static void Show(ModalView modal, Sim sim, Mail mail, Action<int> decide)
        {
            if (modal == null || mail == null) return;
            var body = modal.Open(mail.Title, "Von " + mail.Sender + " · " + Fmt.Clock(mail.Time) + " Uhr", 640, new[]
            {
                new ModalButton("Später beantworten", null, "ghost", "clock"),
            }, true, "event", null, AppMail.MailIcon(mail.Icon));
            modal.Text(body, mail.Text);
            UIX.Text(body, "DEINE ENTSCHEIDUNG", "eyebrow").style.marginTop = 6;
            var list = UIX.Col(body, 8f);
            for (int i = 0; i < mail.Choices.Count; i++)
            {
                var c = mail.Choices[i];
                int ci = i;
                string cost = c.Cost > 0 ? Fmt.Money(c.Cost) : (string.IsNullOrEmpty(c.Minigame) ? null : "Minispiel");
                var b = modal.AddChoice(list, c.Label, cost, () => decide?.Invoke(ci), i == 0, string.IsNullOrEmpty(c.Minigame) ? null : "gamepad");
                if (sim != null && c.Cost > sim.Money)
                {
                    b.SetEnabled(false);
                    b.tooltip = "Dafür fehlt dir das Geld.";
                }
            }
            UIX.Text(body, "Du kannst auch später im Handy (" + GameInput.KeyLabel("phone") + ") antworten. Um 20 Uhr gilt automatisch die vorsichtigste Antwort.", "small").style.marginTop = 4;
        }

        public static void ShowResult(ModalView modal, Mail mail, string result)
        {
            if (modal == null) return;
            var body = modal.Open("Ergebnis", mail != null ? mail.Title : null, 560, new[] { new ModalButton("OK", null, "accent", "check") }, true, "event_result", null, "check_circle");
            var lines = (result ?? "").Split('\n');
            bool first = true;
            var chips = UIX.Wrap(body);
            foreach (var raw in lines)
            {
                string l = raw.Trim();
                if (l == "") continue;
                if (first)
                {
                    modal.Text(body, l).PlaceBehind(chips);
                    first = false;
                    continue;
                }
                bool bad = l.Contains("−") || l.Contains("gesperrt") || l.Contains("verspätet") || l.Contains("beschädigt") || l.Contains("offline") || l.Contains("verloren") || l.Contains("Preiskampf");
                bool good = !bad && (l.StartsWith("+") || l.StartsWith("×") || l.Contains("steigt") || l.Contains("Boost") || l.Contains("verkauft"));
                UIX.Chip(chips, l, bad ? "trend_down" : (good ? "trend" : "dot"), bad ? Theme.Bad : (good ? Theme.Good : Theme.Muted));
            }
        }
    }
}
