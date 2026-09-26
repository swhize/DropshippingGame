using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Firma › Team: Mitarbeiter einstellen und entlassen. Sie arbeiten automatisch in der Lagerhalle.</summary>
    public sealed class AppStaff : LaptopApp
    {
        public override string Lead => "Mitarbeiter übernehmen Arbeit automatisch – solange Nachschub da ist. Löhne werden jeden Abend abgerechnet.";

        public override void Build()
        {
            var s = S;
            var top = Columns(Root);
            Stat(top, "MITARBEITER", s.Staff.Count.ToString());
            Stat(top, "LÖHNE PRO TAG", Fmt.Money(s.WagesPerDay()), s.WagesPerDay() > 0 ? "bad" : null);
            Stat(top, "UMSATZ HEUTE", Fmt.Money(s.Daily.Revenue), "good");

            Section(Root, "Einstellen");
            var grid = UIX.Wrap(Root);
            foreach (var r in GameData.StaffRoles)
            {
                var c = UIX.Card(grid);
                c.style.width = 390;
                var ch = UIX.Row(c, 12f);
                UIX.Round(UIX.Swatch(ch, r.Shirt.ToColor(), 44f, r.Icon), 14f);
                var v = Grow(UIX.Col(ch, 2f));
                UIX.Text(v, r.Name, "h3");
                int count = s.StaffCount(r.Id);
                UIX.Text(v, Fmt.Money(r.Wage) + " pro Tag · " + count + "/" + r.Max + " besetzt", "small");
                P(c, r.Desc);
                string id = r.Id;
                Btn(c, count >= r.Max ? "Voll besetzt" : "Einstellen", () => S.Hire(id), "accent", count >= r.Max, "plus").style.alignSelf = Align.FlexStart;
            }

            var lc = Card(Root, "Dein Team", s.Staff.Count > 0 ? s.Staff.Count + " Personen" : null);
            if (s.Staff.Count == 0) UIX.Empty(lc, "users", "Noch niemand eingestellt", "Dein erstes Teammitglied bringt dir auch das Ziel „Chef sein“.");
            for (int i = 0; i < s.Staff.Count; i++)
            {
                var m = s.Staff[i];
                var role = GameData.StaffRole(m.Role);
                var sh = UIX.Row(lc, 12f, "table-row");
                if (i == 0) sh.AddToClassList("first");
                UIX.Avatar(sh, m.Name, role.Shirt.ToColor(), 32f);
                UIX.Text(sh, m.Name, "h3").style.width = 110;
                Grow(UIX.Text(sh, role.Name, "muted"));
                if (m.Role != "social") UIX.Bar(sh, m.Progress, Theme.LaptopTeal, 6f, 120f);
                int idx = i;
                Btn(sh, "Entlassen", () => S.Fire(idx), "danger").AddToClassList("btn-sm");
            }
            Tip(Root, "Packer:innen brauchen Lagerbestand und Kartons, Versandkräfte verpackte Pakete, Lagerist:innen Kisten am Wareneingang. Fehlt etwas, warten sie.");
        }
    }

    /// <summary>Firma › Ausbau: Verkaufsstand, Lagerhalle, Upgrades und Einrichtung.</summary>
    public sealed class AppBuild : LaptopApp
    {
        public override string Lead => "Investiere in größere Räume, bessere Technik und ein bisschen Gemütlichkeit.";

        public override void Build()
        {
            var s = S;
            var top = Columns(Root);
            Stat(top, "STANDORT", GameData.StageNames[Mathf.Clamp(s.LocationStage, 0, GameData.StageNames.Length - 1)]);
            Stat(top, "LAGERPLATZ", Fmt.Thousands(s.StockTotal()) + " / " + Fmt.Thousands(s.Capacity()));
            Stat(top, "MIETE", Fmt.Money(s.RentPerDay()) + "/Tag");
            Stat(top, "WARTESCHLANGE", s.QueueCapacity() + " Plätze");

            Section(Root, "Upgrades");
            foreach (var u in GameData.Upgrades)
            {
                string st = s.UpgradeState(u.Id);
                var c = UIX.Card(Root, null, st == "available" ? "card-hi" : null);
                var h = UIX.Row(c, 12f);
                UIX.Round(UIX.Swatch(h, st == "owned" ? Theme.LaptopGood : Theme.LaptopAccent, 46f, u.Icon), 14f);
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
                        Btn(h, "Kaufen · " + Fmt.Money(u.Cost), () => S.BuyUpgrade(id), "accent", s.Money < u.Cost);
                        break;
                }
            }

            Section(Root, "Einrichtung");
            var grid = UIX.Wrap(Root);
            foreach (var d in GameData.Decor)
            {
                var c = UIX.Card(grid);
                c.style.width = 262;
                UIX.Text(c, d.Name, "h3");
                string id = d.Id;
                if (s.DecorOwned.Contains(d.Id)) UIX.Chip(c, "Aufgestellt", "check", Theme.LaptopGood).style.alignSelf = Align.FlexStart;
                else if (d.Stage > s.LocationStage) Btn(c, "Nur in der Lagerhalle", null, "", true, "lock").style.alignSelf = Align.FlexStart;
                else Btn(c, "Kaufen · " + Fmt.Money(d.Cost), () => S.BuyDecor(id), "soft", s.Money < d.Cost).style.alignSelf = Align.FlexStart;
            }
        }
    }

    /// <summary>Firma › Ziele: Firmenlevel und Meilensteine mit Belohnung.</summary>
    public sealed class AppGoals : LaptopApp
    {
        public override string Lead => "Deine Meilensteine auf dem Weg zum Imperium.";

        public override void Build()
        {
            var s = S;
            var lc = UIX.Card(Root, null, "card-hi");
            var lh = UIX.Row(lc, 10f);
            var badge = UIX.Div(lh, "level-badge");
            UIX.Text(badge, "LV " + s.Level, "level-badge-text");
            UIX.Text(lh, "Firmenlevel " + s.Level, "h2");
            UIX.Spacer(lh);
            if (s.Level < GameData.MaxLevel) UIX.Num(lh, Fmt.Thousands(s.Xp) + " / " + Fmt.Thousands(GameData.LevelThreshold(s.Level + 1)) + " XP", false, "muted");
            UIX.Bar(lc, s.LevelProgress(), Theme.LaptopAccent, 10f);
            if (s.Level < GameData.MaxLevel && GameData.LevelUnlocks.TryGetValue(s.Level + 1, out string next))
            {
                UIX.Text(lc, "NÄCHSTES LEVEL SCHALTET FREI", "eyebrow");
                var w = UIX.Wrap(lc);
                foreach (var part in next.Split('·'))
                {
                    string t = part.Trim();
                    if (t != "") UIX.Chip(w, t, "sparkle", Theme.LaptopAccent);
                }
            }
            else if (s.Level >= GameData.MaxLevel) P(lc, "Maximales Level erreicht. Legende.", "");

            int done = 0;
            foreach (var g in GameData.Goals)
                if (s.GoalsDone.Contains(g.Id)) done++;
            var gc = Card(Root, "Ziele", done + " / " + GameData.Goals.Length + " erreicht");
            bool first = true;
            foreach (var g in GameData.Goals)
            {
                bool isDone = s.GoalsDone.Contains(g.Id);
                var gh = UIX.Row(gc, 12f, "table-row");
                if (first) gh.AddToClassList("first");
                first = false;
                UIX.Icon(gh, isDone ? "check_circle" : "flag", 18f, isDone ? Theme.LaptopGood : Theme.LaptopMuted);
                var gv = Grow(UIX.Col(gh, 2f));
                var t = UIX.Text(gv, g.Title, "h3");
                if (isDone) t.AddToClassList("muted");
                UIX.Text(gv, g.Desc, "small");
                if (!isDone)
                {
                    float prog = Mathf.Clamp01(s.GoalValue(g) / Mathf.Max(g.Target, 0.001f));
                    var pr = UIX.Row(gv, 8f);
                    UIX.Bar(pr, prog, Theme.LaptopTeal, 6f);
                    UIX.Num(pr, UiFmt.Percent(prog), false, "small");
                }
                UIX.Num(gh, "+" + Fmt.Money(g.Reward), true, isDone ? "muted" : "accent-text");
            }
        }
    }

    /// <summary>Firma › Lifestyle: Statussymbole ohne Nutzen, aber mit Stil.</summary>
    public sealed class AppLifestyle : LaptopApp
    {
        public override string Lead => "Statussymbole ohne Nutzen, aber mit Stil. Die meisten tauchen in der Welt auf.";

        public override void Build()
        {
            var s = S;
            int owned = 0;
            foreach (var l in GameData.Lifestyle)
                if (s.LifestyleOwned.Contains(l.Id)) owned++;
            var top = Columns(Root);
            Stat(top, "GEGÖNNT", owned + " / " + GameData.Lifestyle.Length);
            Stat(top, "KONTOSTAND", Fmt.Money(s.Money), s.Money < 0 ? "bad" : "good");
            var grid = UIX.Wrap(Root);
            foreach (var l in GameData.Lifestyle)
            {
                var c = UIX.Card(grid);
                c.style.width = 292;
                var h = UIX.Row(c, 10f);
                UIX.Round(UIX.Swatch(h, s.LifestyleOwned.Contains(l.Id) ? Theme.LaptopGood : Theme.LaptopAccent, 38f, "crown"), 12f);
                Grow(UIX.Text(h, l.Name, "h3"));
                P(c, l.Desc);
                string id = l.Id;
                if (s.LifestyleOwned.Contains(l.Id)) UIX.Chip(c, "Gegönnt", "check", Theme.LaptopGood).style.alignSelf = Align.FlexStart;
                else Btn(c, "Kaufen · " + Fmt.Money(l.Cost), () => S.BuyLifestyle(id), "accent", s.Money < l.Cost).style.alignSelf = Align.FlexStart;
            }
        }
    }
}
