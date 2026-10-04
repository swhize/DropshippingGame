using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Admin-Panel-Abschnitt „Finanzviertel“ (im Reiter Ereignisse): Nachrichten, Crash/Rallye, Teleport, Fenster.</summary>
    public static class FinanceAdmin
    {
        public static void Build(VisualElement body, Action<Action> run)
        {
            var sim = Game.Sim;
            if (sim == null || body == null) return;
            var m = sim.Market;
            var title = new Label("FINANZVIERTEL");
            title.style.color = new Color(1f, 0.35f, 0.2f);
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginTop = 14;
            title.style.marginBottom = 6;
            body.Add(title);
            var info = new Label("Depot " + m.PortfolioValue() + " € · Sparkonto " + m.Savings + " € · Hektik " + Mathf.RoundToInt(m.Frenzy * 100f) + " % · Nachrichten " + m.News.Count);
            info.style.color = new Color(0.6f, 0.6f, 0.66f);
            info.style.fontSize = 15;
            body.Add(info);
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            body.Add(row);
            Add(row, "Gute Aktien-News", () => run(() => m.PostNews(0, 1)));
            Add(row, "Schlechte Aktien-News", () => run(() => m.PostNews(0, -1)));
            Add(row, "Krypto-Sommer", () => run(() => m.PostNews(3, 1)));
            Add(row, "Krypto-Winter", () => run(() => m.PostNews(3, -1)));
            Add(row, "Börsencrash −20 %", () => run(() => m.AdminMoveAll(0.8f, false)));
            Add(row, "Krypto ×2", () => run(() => m.AdminMoveAll(2f, true)));
            Add(row, "Makler-Panik (Hektik 100 %)", () => run(() => m.Frenzy = 1f));
            Add(row, "Nacht simulieren (Dividende/Zinsen)", () => run(() => m.NewDay()));
            Add(row, "Bank öffnen", () =>
            {
                Game.UI?.Admin?.Close();
                FinanceUI.OpenBank(false);
            });
            Add(row, "Börsen-Terminal öffnen", () =>
            {
                Game.UI?.Admin?.Close();
                FinanceUI.OpenExchange();
            });
            Add(row, "Teleport Finanzviertel", () =>
            {
                Game.UI?.Admin?.Close();
                Game.Player?.Teleport(FinanceDistrict.Entrance, 0f);
            });
        }

        private static void Add(VisualElement row, string text, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.style.backgroundColor = new Color(0.13f, 0.13f, 0.16f);
            b.style.color = new Color(0.93f, 0.93f, 0.95f);
            b.style.fontSize = 16;
            b.style.marginRight = 6;
            b.style.marginBottom = 6;
            row.Add(b);
        }
    }
}
