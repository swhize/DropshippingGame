using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Kassen-Fenster (Kassenbon) für MediaMarkd und Fressnix.</summary>
    public static class ShopCheckoutView
    {
        private static readonly string[] CashierLines =
        {
            "„Sammeln Sie Punkte? Nein? Wir auch nicht.“",
            "„Brauchen Sie eine Tüte? Kostet 20 Cent. Und Ihre Würde.“",
            "„Garantieverlängerung? Wir verlängern alles. Außer Öffnungszeiten.“",
            "„Bar oder mit Karte? Oder mit Influencer-Rabattcode?“",
        };

        public static void Show(string store)
        {
            var ui = Game.UI;
            var sim = Game.Sim;
            if (ui == null || sim == null) return;
            var lines = sim.CartLines(store);
            int total = sim.CartTotal(store);
            var buttons = new List<ModalButton>();
            if (lines.Count > 0)
                buttons.Add(new ModalButton("Bezahlen · " + Fmt.Money(total), () => sim.ShopCheckout(store), "accent", "cart"));
            buttons.Add(new ModalButton(lines.Count > 0 ? "Weiter einkaufen" : "Schließen", null, "ghost"));
            string sub = ShopData.StoreName(store) + " · Konto: " + Fmt.Money(sim.Money);
            var body = ui.Modal.Open("Kasse", sub, 540, buttons, true, "shop_checkout", null, "receipt");
            ui.Modal.Text(body, CashierLines[(sim.Day + lines.Count) % CashierLines.Length], "muted");
            if (lines.Count == 0)
            {
                ui.Modal.Text(body, store == ShopData.Electro ? "Dein Wagen ist leer. Schieb ihn zu den Regalen!" : "Dein Korb ist leer.");
                return;
            }
            foreach (var l in lines)
            {
                var it = ShopData.Item(l.ItemId);
                if (it == null) continue;
                var row = UIX.Row(body, 8f);
                row.style.alignItems = Align.Center;
                var name = UIX.Text(row, l.Qty + "× " + it.Name, "modal-text");
                name.style.flexGrow = 1;
                UIX.Text(row, Fmt.Money(l.UnitPrice * l.Qty), "modal-text");
                string id = l.ItemId;
                UIX.Button(row, "−", () =>
                {
                    sim.CartRemove(id);
                    Show(store);
                }, "ghost");
            }
            int saved = sim.CartSavings(store);
            ui.Modal.KV(body, "Summe", Fmt.Money(total));
            if (saved > 0) ui.Modal.KV(body, "Gespart (Aktion)", Fmt.Money(saved), new Color(0.2f, 0.65f, 0.3f));
            bool ware = false;
            foreach (var l in lines)
            {
                var it = ShopData.Item(l.ItemId);
                if (it != null && it.Kind == "ware") ware = true;
            }
            if (ware) ui.Modal.Text(body, "Ware fürs Lager bringt der Lieferservice als Kiste an deinen Wareneingang.", "muted");
            if (total > sim.Money) ui.Modal.Text(body, "Achtung: Dir fehlen " + Fmt.Money(total - sim.Money) + ".", "muted");
        }
    }
}
