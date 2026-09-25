using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Personal: Mitarbeiter einstellen und entlassen. Sie arbeiten automatisch in der Lagerhalle.</summary>
    public sealed class AppStaff : LaptopApp
    {
        public override void Build()
        {
            var s = S;
            Header("Personal", "Mitarbeiter übernehmen Arbeit automatisch – solange Nachschub da ist. Löhne werden jeden Abend abgerechnet.");
            var top = Columns(Root);
            Stat(top, "MITARBEITER", s.Staff.Count.ToString());
            Stat(top, "LÖHNE PRO TAG", Fmt.Money(s.WagesPerDay()), s.WagesPerDay() > 0 ? "bad" : null);
            Stat(top, "UMSATZ HEUTE", Fmt.Money(s.Daily.Revenue), "good");

            Section(Root, "Einstellen");
            var grid = UIX.Wrap(Root);
            foreach (var r in GameData.StaffRoles)
            {
                var c = UIX.Card(grid);
                c.style.width = 380;
                var ch = UIX.Row(c, 12f);
                UIX.Swatch(ch, r.Shirt.ToColor(), 42f, r.Icon);
                var v = Grow(UIX.Col(ch, 2f));
                UIX.Text(v, r.Name, "h3");
                int count = s.StaffCount(r.Id);
                UIX.Text(v, Fmt.Money(r.Wage) + " pro Tag · " + count + "/" + r.Max + " besetzt", "small");
                P(c, r.Desc);
                string id = r.Id;
                Btn(c, count >= r.Max ? "Voll besetzt" : "Einstellen", () => s.Hire(id), "accent", count >= r.Max, "plus").style.alignSelf = Align.FlexStart;
            }

            if (s.Staff.Count > 0)
            {
                var lc = UIX.Card(Root, "Dein Team");
                for (int i = 0; i < s.Staff.Count; i++)
                {
                    var m = s.Staff[i];
                    var role = GameData.StaffRole(m.Role);
                    var sh = UIX.Row(lc, 12f, "table-row");
                    UIX.Swatch(sh, role.Shirt.ToColor(), 28f, role.Icon);
                    UIX.Text(sh, m.Name, "h3").style.width = 110;
                    Grow(UIX.Text(sh, role.Name, "muted"));
                    if (m.Role != "social") UIX.Bar(sh, m.Progress, Theme.LaptopTeal, 6f, 120f);
                    int idx = i;
                    Btn(sh, "Entlassen", () => s.Fire(idx), "danger");
                }
            }
            var tip = UIX.Card(Root);
            var th = UIX.Row(tip, 10f);
            UIX.Icon(th, "bulb", 20f, Theme.LaptopAccent);
            Grow(P(th, "Packer:innen brauchen Lagerbestand und Kartons, Versandkräfte verpackte Pakete, Lagerist:innen Kisten am Wareneingang. Fehlt etwas, warten sie.", ""));
        }
    }

    /// <summary>Ausbau & Einrichtung: Verkaufsstand, Lagerhalle, Upgrades und Deko.</summary>
    public sealed class AppBuild : LaptopApp
    {
        public override void Build()
        {
            var s = S;
            Header("Ausbau & Einrichtung", "Investiere in größere Räume, bessere Technik und ein bisschen Gemütlichkeit.");
            var top = Columns(Root);
            Stat(top, "STANDORT", GameData.StageNames[s.LocationStage]);
            Stat(top, "LAGERPLATZ", Fmt.Thousands(s.StockTotal()) + " / " + Fmt.Thousands(s.Capacity()));
            Stat(top, "MIETE", Fmt.Money(s.RentPerDay()) + "/Tag");
            Stat(top, "WARTESCHLANGE", s.QueueCapacity() + " Plätze");

            Section(Root, "Upgrades");
            foreach (var u in GameData.Upgrades)
            {
                string st = s.UpgradeState(u.Id);
                var c = UIX.Card(Root, null, st == "available" ? "card-hi" : null);
                var h = UIX.Row(c, 12f);
                UIX.Swatch(h, st == "owned" ? Theme.LaptopGood : Theme.LaptopAccent, 46f, u.Icon);
                var v = Grow(UIX.Col(h, 2f));
                UIX.Text(v, u.Name, "h3");
                P(v, u.Desc);
                string id = u.Id;
                switch (st)
                {
                    case "owned":
                        UIX.Chip(h, "Gekauft", "check", Theme.LaptopGood);
                        break;
                    case "level":
                        Btn(h, LockText(u.Level), null, "", true, "lock");
                        break;
                    case "requires":
                        Btn(h, "Braucht " + GameData.Upgrade(u.Requires).Name, null, "", true, "lock");
                        break;
                    default:
                        Btn(h, "Kaufen · " + Fmt.Money(u.Cost), () => s.BuyUpgrade(id), "accent", s.Money < u.Cost);
                        break;
                }
            }

            Section(Root, "Einrichtung");
            var grid = UIX.Wrap(Root);
            foreach (var d in GameData.Decor)
            {
                var c = UIX.Card(grid);
                c.style.width = 250;
                UIX.Text(c, d.Name, "h3");
                string id = d.Id;
                if (s.DecorOwned.Contains(d.Id)) UIX.Chip(c, "Aufgestellt", "check", Theme.LaptopGood);
                else if (d.Stage > s.LocationStage) Btn(c, "Nur in der Lagerhalle", null, "", true, "lock");
                else Btn(c, "Kaufen · " + Fmt.Money(d.Cost), () => s.BuyDecor(id), "soft", s.Money < d.Cost);
            }
        }
    }

    /// <summary>Ziele & Lifestyle: Firmenlevel, Meilensteine mit Belohnung und Statussymbole.</summary>
    public sealed class AppGoals : LaptopApp
    {
        public override void Build()
        {
            var s = S;
            Header("Ziele & Lifestyle", "Deine Meilensteine auf dem Weg zum Imperium – und was du dir gönnst.");
            var lc = UIX.Card(Root, null, "card-hi");
            var lh = UIX.Row(lc, 8f);
            UIX.Text(lh, "Firmenlevel " + s.Level, "h2", "accent-text");
            UIX.Spacer(lh);
            if (s.Level < GameData.MaxLevel) UIX.Text(lh, Fmt.Thousands(s.Xp) + " / " + Fmt.Thousands(GameData.LevelThreshold(s.Level + 1)) + " XP", "muted");
            UIX.Bar(lc, s.LevelProgress(), Theme.LaptopAccent, 10f);
            if (s.Level < GameData.MaxLevel && GameData.LevelUnlocks.TryGetValue(s.Level + 1, out string next))
                P(lc, "Nächstes Level: " + next, "");
            else if (s.Level >= GameData.MaxLevel) P(lc, "Maximales Level erreicht. Legende.", "");

            var gc = UIX.Card(Root, "Ziele");
            foreach (var g in GameData.Goals)
            {
                bool done = s.GoalsDone.Contains(g.Id);
                var gh = UIX.Row(gc, 12f, "table-row");
                UIX.Icon(gh, done ? "check" : "dot", 18f, done ? Theme.LaptopGood : Theme.LaptopMuted);
                var gv = Grow(UIX.Col(gh, 2f));
                var t = UIX.Text(gv, g.Title, "h3");
                if (done) t.AddToClassList("muted");
                UIX.Text(gv, g.Desc, "small");
                if (!done) UIX.Bar(gv, Mathf.Clamp01(s.GoalValue(g) / Mathf.Max(g.Target, 0.001f)), Theme.LaptopTeal, 6f);
                UIX.Text(gh, "+" + Fmt.Money(g.Reward), done ? "muted" : "accent-text");
            }

            Section(Root, "Lifestyle");
            P(Root, "Statussymbole ohne Nutzen, aber mit Stil. Die meisten tauchen in der Welt auf.");
            var grid = UIX.Wrap(Root);
            foreach (var l in GameData.Lifestyle)
            {
                var c = UIX.Card(grid);
                c.style.width = 280;
                UIX.Text(c, l.Name, "h3");
                P(c, l.Desc);
                string id = l.Id;
                if (s.LifestyleOwned.Contains(l.Id)) UIX.Chip(c, "Gegönnt", "check", Theme.LaptopGood);
                else Btn(c, "Kaufen · " + Fmt.Money(l.Cost), () => s.BuyLifestyle(id), "accent", s.Money < l.Cost).style.alignSelf = Align.FlexStart;
            }
        }
    }
}
