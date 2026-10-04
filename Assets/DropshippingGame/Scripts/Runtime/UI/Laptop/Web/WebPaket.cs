using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// PaketBlitz (Comic, gelb/rot): Geschäftskunden-Portal. Sendungsverfolgung mit Stufen
    /// (Bestellung → entnommen → verpackt → etikettiert → Band/Abholung), heutige Pakete,
    /// Label-Ansicht und eingehende Wareneingänge (Lieferungen von AllesExpress).
    /// Routen: "" · "sendung/{id}".
    /// </summary>
    public sealed class WebPaket : WebApp
    {
        public override string AppId => "paket";
        public override string SkinClass => "pb";
        public override WebSkin Skin => WebSkin.Comic;
        protected override string Domain => "paketblitz.de/geschaeftskunden";

        private static readonly string[] Steps = { "Bestellt", "Entnommen", "Verpackt", "Label drauf", "Auf dem Band" };

        public override void Build()
        {
            var s = S;
            if (s == null) return;
            var all = new List<Order>();
            all.AddRange(s.OrdersInWork);
            all.AddRange(s.OrderQueue);

            Order sel = null;
            string arg = RouteArg("sendung/");
            if (arg != null && int.TryParse(arg, out int sid)) sel = all.Find(o => o.Id == sid);
            if (sel == null && all.Count > 0) sel = all[0];

            // Kopf
            var hd = Row(Root, 16f, "pb-hd");
            var logo = Press(hd, () => Nav(""), "pb-logo");
            H(logo, "PaketBlitz", "pb-logo-text");
            UIX.PassThrough(logo);
            var nav = Row(hd, 14f, "pb-nav");
            Btn(nav, "Senden", () => Toast("Du sendest schon. Ständig."), "pb-nav-btn");
            Btn(nav, "Verfolgen", () => Nav(""), "pb-nav-btn");
            Btn(nav, "Abholung", () => Toast("Abholung täglich um 20:00 Uhr. Der Fahrer hupt einmal und fährt dann."), "pb-nav-btn");
            Btn(nav, "Wareneingang", () => View?.OpenApp("allesexpress/orders"), "pb-nav-btn");
            Fill(hd);
            B(hd, "Kundennr. " + (4401 + s.Level) + "-" + (W.Hash(s.BrandName) % 9000 + 1000), "pb-text");

            // Sendung verfolgen
            var box = Col(Root, 8f, "pb-box");
            B(box, "Sendung verfolgen", "pb-h");
            var track = Row(box, 8f, "pb-track");
            var field = Flex(Row(track, 0f, "pb-field"));
            T(field, sel != null ? TrackingNo(sel) : "PB 0000 0000 0000 DE", "pb-mono");
            Btn(track, "Suchen", () => Toast(sel != null ? "Gefunden! Sie ist da, wo sie ist." : "Keine Sendung. Keine Sorgen."), "pb-btn-red");
            if (sel == null)
            {
                T(box, "Nichts zu verschicken.", "pb-muted");
            }
            else
            {
                var v = OrderInfo.Get(s, sel);
                var steps = Row(box, 0f, "pb-steps");
                Div(steps, "pb-steps-line");
                int cur = Mathf.Clamp((int)sel.Stage, 0, Steps.Length - 1);
                for (int i = 0; i < Steps.Length; i++)
                {
                    var st = Flex(Col(steps, 4f, "pb-step"));
                    st.EnableInClassList("ok", i < cur);
                    st.EnableInClassList("now", i == cur);
                    var dot = Div(st, "pb-step-dot");
                    H(dot, (i + 1).ToString(), "pb-step-num");
                    B(st, Steps[i], "pb-step-label");
                }
                string eta = sel.Stage == OrderStage.Conveyor ? "Auf dem Band. Abholung 20:00."
                    : (sel.Express ? "EXPRESS – schnell packen!" : "Zustellung morgen.");
                T(box, v.Number + " · " + v.Product.Name + " an " + (string.IsNullOrEmpty(v.Customer) ? "Kundschaft" : v.Customer) +
                       (string.IsNullOrEmpty(v.City) ? "" : " in " + v.City) + " · " + v.TimeText, "pb-center");
                T(box, eta, "pb-muted", "pb-center");
            }

            // Heute versenden + Label
            var row = Row(Root, 16f, "pb-row");
            row.style.alignItems = Align.FlexStart;
            var today = Flex(Col(row, 4f, "pb-box"));
            var th = Row(today, 8f);
            B(th, "Heute versenden", "pb-h");
            float left = Mathf.Max(0f, GameData.DayEnd - s.TimeMinutes);
            T(th, "· Abholung 20:00 · noch " + UiFmt.Duration(left), "pb-muted");
            var stats = Row(today, 10f, "pb-stats");
            Mini(stats, "Verschickt", s.Daily.Shipped.ToString());
            Mini(stats, "Express", s.Daily.Express.ToString());
            Mini(stats, "Zu spät", s.Daily.Late.ToString());
            Mini(stats, "Storniert", (s.Daily.Expired + s.Daily.Lost).ToString());
            if (all.Count == 0) T(today, "Keine offenen Pakete.", "pb-muted");
            int n = 0;
            foreach (var o in all)
            {
                if (++n > 12) break;
                var v = OrderInfo.Get(s, o);
                var r = Row(today, 10f, "pb-line");
                r.EnableInClassList("on", sel != null && o.Id == sel.Id);
                Wd(B(r, v.Number, "pb-text"), 64f);
                Flex(T(r, v.Product.Name, "pb-text"));
                var kind = Wd(B(r, v.Express ? "EXPRESS" : "Standard", "pb-text"), 80f);
                if (v.Express) kind.AddToClassList("red");
                var tt = Wd(T(r, v.TimeText, "pb-small"), 96f);
                if (v.Tone == "late") tt.AddToClassList("red");
                int id = o.Id;
                Btn(r, o.Stage == OrderStage.Queued ? "Ansehen" : OrderInfo.StageText(o.Stage), () => Nav("sendung/" + id),
                    "pb-btn", o.Stage == OrderStage.Queued ? "red" : "");
            }
            if (all.Count > 12) T(today, "+" + (all.Count - 12) + " weitere (Stapel wird höher)", "pb-muted");

            var lab = Wd(Col(row, 0f, "pb-label"), 280f);
            if (sel != null) DrawLabel(lab, s, sel);
            else
            {
                var e = Col(lab, 6f, "pb-label-empty");
                H(e, "Kein Label", "pb-h");
                T(e, "Kommt mit der nächsten Bestellung.", "pb-text");
            }

            // Wareneingang
            var inb = Col(Root, 6f, "pb-box");
            var ih = Row(inb, 8f);
            B(ih, "Wareneingang", "pb-h");
            int dock = 0;
            foreach (var p in GameData.Products) dock += s.DockCountFor(p.Id);
            T(ih, "· am Tor: " + dock + " Stk" + (s.ReturnsAtDock > 0 ? " · Retouren: " + s.ReturnsAtDock : ""), "pb-muted");
            if (s.TravelingDeliveries.Count == 0) T(inb, "Nichts unterwegs.", "pb-muted");
            foreach (var d in s.TravelingDeliveries)
            {
                if (!GameData.IsProduct(d.Product)) continue;
                var dp = GameData.Product(d.Product);
                var r = Row(inb, 10f, "pb-line");
                Wd(B(r, "LKW", "pb-truck"), 44f);
                Flex(T(r, d.Quantity + "× " + dp.Name + " (" + GameData.QualityName(d.Quality) + ")", "pb-text"));
                B(r, d.VanSent ? "Eigener Wagen" : "Spedition", "pb-small");
                B(r, "in " + s.EtaText(d), "pb-text", "red");
            }
            if (s.ReturnsAtDock > 0 || s.ReturnsIncoming.Count > 0)
                Btn(inb, "Retouren bearbeiten", () => View?.OpenApp("returns"), "pb-btn-red");
        }

        private void Mini(VisualElement parent, string k, string v)
        {
            var c = Flex(Col(parent, 0f, "pb-mini"));
            T(c, k, "pb-small");
            H(c, v, "pb-mini-v");
        }

        private void DrawLabel(VisualElement lab, Sim s, Order o)
        {
            var v = OrderInfo.Get(s, o);
            var top = Row(lab, 0f, "pb-label-top");
            H(top, GameData.SizeName(v.Product.Size), "pb-label-size");
            var tv = Col(top, 1f, "pb-label-head");
            T(tv, "PaketBlitz " + (v.Express ? "EXPRESS" : "Standard"), "pb-label-text");
            T(tv, Fmt.Dec(0.2f + v.Product.Size * 0.6f, 1) + " kg", "pb-label-text");
            T(tv, s.BrandName, "pb-label-text");
            var to = Col(lab, 1f, "pb-label-to");
            T(to, "AN:", "pb-label-text");
            B(to, string.IsNullOrEmpty(v.Customer) ? "Kundschaft" : v.Customer, "pb-label-name");
            T(to, Street(o), "pb-label-text");
            T(to, string.IsNullOrEmpty(v.City) ? "Irgendwo in DE" : v.City, "pb-label-text");
            if (!string.IsNullOrEmpty(v.Note)) T(to, "Hinweis: " + v.Note, "pb-label-text");
            var bc = new Barcode(TrackingNo(o));
            bc.AddToClassList("pb-barcode");
            lab.Add(bc);
            T(lab, TrackingNo(o), "pb-mono", "pb-center");
        }

        private static string TrackingNo(Order o) => "PB 4401 " + (1000 + o.Id % 9000) + " " + (o.Id * 37 % 9000 + 1000) + " DE";

        private static string Street(Order o)
        {
            string[] st = { "Zechenstraße", "Am Kanal", "Hauptstraße", "Lindenweg", "Bahnhofstr.", "Kirchplatz", "Im Winkel" };
            return st[W.Hash(o.Customer ?? "") % st.Length] + " " + (o.Id % 97 + 1);
        }
    }
}
