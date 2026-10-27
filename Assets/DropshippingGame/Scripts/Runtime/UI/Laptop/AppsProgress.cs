using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Registriert die Fortschritts-Reiter in der Firma-App (Perks, Einrichtung, Logistik).</summary>
    public static class ProgressApps
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            LaptopView.RegisterTab("company", new LaptopView.TabDef
            {
                Id = "perks", Title = "Perks", Icon = "star", Make = () => new AppPerks(),
                Badge = () => Game.Sim != null && Game.Sim.PerkPointsAvailable() > 0 ? Game.Sim.PerkPointsAvailable().ToString() : "",
            }, "skills");
            LaptopView.RegisterTab("company", new LaptopView.TabDef { Id = "home", Title = "Einrichtung", Icon = "home", Make = () => new AppHomeGear() }, "build");
            LaptopView.RegisterTab("company", new LaptopView.TabDef { Id = "logistics", Title = "Logistik", Icon = "truck", Make = () => new AppLogistics() }, "home");
            LaptopView.Aliases["perks"] = "company/perks";
            LaptopView.Aliases["einrichtung"] = "company/home";
            LaptopView.Aliases["logistik"] = "company/logistics";
        }
    }

    /// <summary>Firma › Perks: Hustle-Punkte aus Erfahrung in mehrstufige Freischaltungen stecken.</summary>
    public sealed class AppPerks : LaptopApp
    {
        public override string Lead => "Für gesammelte Erfahrung gibt's Hustle-Punkte – zusätzlich zu Level und Skills. Jede Stufe kostet 1 Punkt.";

        public override void Build()
        {
            var s = S;
            var top = Columns(Root);
            Stat(top, "PUNKTE FREI", s.PerkPointsAvailable().ToString(), s.PerkPointsAvailable() > 0 ? "good" : null);
            Stat(top, "AUSGEGEBEN", s.PerkPointsSpent() + " / " + s.PerkPointsTotal());
            Stat(top, "NÄCHSTER PUNKT", "in " + Fmt.Thousands(s.XpToNextPerkPoint()) + " XP");
            var grid = UIX.Wrap(Root);
            foreach (var p in GameData.Perks)
            {
                int rank = s.PerkRank(p.Id);
                string st = s.PerkState(p.Id);
                var c = UIX.Card(grid, null, st == "available" ? "card-hi" : null);
                c.style.width = 300;
                var h = UIX.Row(c, 10f);
                UIX.Round(UIX.Swatch(h, rank > 0 ? Theme.LaptopGood : Theme.LaptopAccent, 38f, Icons.Has(p.Icon) ? p.Icon : "star"), 12f);
                var v = Grow(UIX.Col(h, 0f));
                UIX.Text(v, p.Name, "h3");
                UIX.Text(v, p.Effect + " je Stufe · " + rank + "/" + p.MaxRank, "small");
                UIX.Bar(c, rank / (float)p.MaxRank, Theme.LaptopTeal, 6f);
                P(c, p.Desc);
                string id = p.Id;
                switch (st)
                {
                    case "max": UIX.Chip(c, "Maximal", "check", Theme.LaptopGood).style.alignSelf = Align.FlexStart; break;
                    case "level": Btn(c, LockText(p.Level), null, "", true, "lock").style.alignSelf = Align.FlexStart; break;
                    case "points": Btn(c, "Kein Punkt frei", null, "", true, "lock").style.alignSelf = Align.FlexStart; break;
                    default: Btn(c, "Stufe " + (rank + 1) + " · 1 Punkt", () => S.BuyPerk(id), "accent", false, "plus").style.alignSelf = Align.FlexStart; break;
                }
            }
        }
    }

    /// <summary>Firma › Einrichtung: Laptop, Bett, Stuhl, Schreibtisch, Kaffee (Energie) und Ausrüstung, Müll.</summary>
    public sealed class AppHomeGear : LaptopApp
    {
        public override string Lead => "Bessere Einrichtung = mehr Energie und schnellere Abläufe. Müde wirst du langsamer.";

        public override void Build()
        {
            var s = S;
            var top = Columns(Root);
            Stat(top, "ENERGIE", Mathf.RoundToInt(s.Energy) + " / 100", s.Tired ? "bad" : "good", "morgen früh: " + Mathf.RoundToInt(s.MorningEnergy()));
            Stat(top, "LAUFTEMPO", Mathf.RoundToInt(s.WalkSpeedMult() * 100f) + " %", s.WalkSpeedMult() < 0.99f ? "bad" : null);
            Stat(top, "VERBRAUCH", Mathf.RoundToInt(s.EnergyDrainMult() * 100f) + " %");
            Stat(top, "MÜLL", s.Waste + " / " + s.WasteCapacity(), s.WasteFull ? "bad" : null, "Abholung " + s.NextGarbagePickupText());
            UIX.Bar(Root, s.Energy / 100f, s.Tired ? Theme.LaptopBad : Theme.LaptopGood, 8f);

            Section(Root, "Einrichtung");
            var grid = UIX.Wrap(Root);
            foreach (var slot in GameData.HomeSlots)
            {
                int tier = s.HomeTier(slot.Id);
                var cur = GameData.HomeItem(slot.Id, tier);
                var next = s.NextHomeItem(slot.Id);
                var c = UIX.Card(grid);
                c.style.width = 300;
                var h = UIX.Row(c, 10f);
                UIX.Round(UIX.Swatch(h, tier > 0 ? Theme.LaptopGood : Theme.LaptopAccent, 38f, Icons.Has(slot.Icon) ? slot.Icon : "home"), 12f);
                var v = Grow(UIX.Col(h, 0f));
                UIX.Text(v, slot.Name + " · Stufe " + tier, "h3");
                UIX.Text(v, cur != null ? cur.Name : slot.BaseName, "small");
                P(c, "Jetzt: " + (cur != null ? cur.Effect : slot.BaseEffect), "");
                if (next == null) UIX.Chip(c, "Voll ausgebaut", "check", Theme.LaptopGood).style.alignSelf = Align.FlexStart;
                else
                {
                    P(c, next.Name + ": " + next.Effect);
                    string sid = slot.Id;
                    if (s.Level < next.Level) Btn(c, LockText(next.Level), null, "", true, "lock").style.alignSelf = Align.FlexStart;
                    else Btn(c, "Kaufen · " + Fmt.Money(next.Cost), () => S.BuyHomeItem(sid), "accent", s.Money < next.Cost).style.alignSelf = Align.FlexStart;
                }
            }

            Section(Root, "Ausrüstung");
            var eg = UIX.Wrap(Root);
            foreach (var e in GameData.Equipment)
            {
                var c = UIX.Card(eg);
                c.style.width = 300;
                var h = UIX.Row(c, 10f);
                int n = s.EquipmentCount(e.Id);
                UIX.Round(UIX.Swatch(h, n > 0 ? Theme.LaptopGood : Theme.LaptopAccent, 38f, Icons.Has(e.Icon) ? e.Icon : "gear"), 12f);
                var v = Grow(UIX.Col(h, 0f));
                UIX.Text(v, e.Name, "h3");
                UIX.Text(v, e.Effect, "small");
                P(c, e.Desc + (n > 0 ? " (" + s.EquipmentActive(e.Id) + "× aufgestellt)" : ""));
                string id = e.Id;
                switch (s.EquipmentState(e.Id))
                {
                    case "max": UIX.Chip(c, "Vorhanden", "check", Theme.LaptopGood).style.alignSelf = Align.FlexStart; break;
                    case "level": Btn(c, LockText(e.Level), null, "", true, "lock").style.alignSelf = Align.FlexStart; break;
                    default: Btn(c, "Kaufen · " + Fmt.Money(e.Cost), () => S.BuyEquipment(id), "accent", s.Money < e.Cost).style.alignSelf = Align.FlexStart; break;
                }
            }
            var wc = Card(Root, "Verpackungsmüll", s.Waste + " / " + s.WasteCapacity());
            UIX.Bar(wc, Mathf.Clamp01(s.WasteFill), s.WasteFull ? Theme.LaptopBad : Theme.LaptopTeal, 8f);
            P(wc, "Jedes Paket und jede Kiste macht Müll. Volle Tonnen bremsen dich und dein Team und kosten Bewertung. Abholung Mo/Mi/Fr 10:00.");
            Btn(wc, "Sonderabholung · " + Fmt.Money(s.SpecialPickupCost()), () => S.OrderGarbagePickup(), "soft", s.Waste == 0 || s.GarbagePending, "truck").style.alignSelf = Align.FlexStart;
        }
    }

    /// <summary>Firma › Logistik: Paketdienst, Porto-Tarif, Lieferstörungen, defekte Chargen, Mengenrabatt, Marke.</summary>
    public sealed class AppLogistics : LaptopApp
    {
        public override string Lead => "Je mehr du verschickst und einkaufst, desto günstiger wird's. Und manchmal streikt halt jemand.";

        public override void Build()
        {
            var s = S;
            var tier = s.CurrentPortoTier();
            var next = s.NextPortoTier();
            var top = Columns(Root);
            Stat(top, "TARIF", tier.Name, null, next != null ? "noch " + (next.Shipped - s.TotalShipped) + " Pakete bis " + next.Name : "Höchste Stufe");
            Stat(top, "PORTO S/M/L", Fmt.Dec(s.PortoFor(0), 2) + " / " + Fmt.Dec(s.PortoFor(1), 2) + " / " + Fmt.Dec(s.PortoFor(2), 2) + " €");
            Stat(top, "PORTO HEUTE", Fmt.Money(s.Daily.Shipping), s.Daily.Shipping > 0 ? "bad" : null);
            Stat(top, "MARKENSTÄRKE", Mathf.RoundToInt(s.BrandStrength() * 100f) + " %", null, "+" + Mathf.RoundToInt((s.BrandPriceMult() - 1f) * 100f) + " % Preis möglich");

            var dis = s.ActiveDisruption;
            if (dis != null)
            {
                var dc = UIX.Card(Root, null, "card-hi");
                UIX.Text(dc, dis.Name, "h2");
                P(dc, dis.Text + " Betroffen: ca. " + Mathf.RoundToInt(dis.DelayChance * 100f) + " % der Standard-Pakete.", "");
            }

            Section(Root, "Paketdienst");
            var cg = UIX.Wrap(Root);
            for (int i = 0; i < GameData.Carriers.Length; i++)
            {
                var c = GameData.Carriers[i];
                bool on = s.Carrier == i;
                var card = UIX.Card(cg, null, on ? "card-hi" : null);
                card.style.width = 380;
                var h = UIX.Row(card, 10f);
                UIX.Round(UIX.Swatch(h, on ? Theme.LaptopGood : Theme.LaptopAccent, 38f, c.Icon), 12f);
                var v = Grow(UIX.Col(h, 0f));
                UIX.Text(v, c.Name, "h3");
                UIX.Text(v, "Paket M: " + Fmt.Dec(s.PortoFor(1, i), 2) + " €" + (c.Reliable ? " · immer pünktlich" : ""), "small");
                P(card, c.Desc);
                int idx = i;
                if (on) UIX.Chip(card, "Aktiv", "check", Theme.LaptopGood).style.alignSelf = Align.FlexStart;
                else Btn(card, "Wählen", () => S.SetCarrier(idx), "accent").style.alignSelf = Align.FlexStart;
            }

            var defects = s.DefectiveProducts();
            if (defects.Count > 0)
            {
                var dc = Card(Root, "Fehlerhafte Chargen", defects.Count + " Produkt(e)");
                foreach (var pid in defects)
                {
                    var r = UIX.Row(dc, 10f, "table-row");
                    UIX.Icon(r, "warning", 18f, Theme.LaptopBad);
                    Grow(UIX.Text(r, GameData.Product(pid).Name + ": " + s.DefectsOf(pid) + " defekt (" + Mathf.RoundToInt(s.DefectShare(pid) * 100f) + " % des Bestands)", "h3"));
                    string id = pid;
                    Btn(r, "Zurückschicken", () => S.RecallDefects(id), "danger");
                }
                P(dc, "Defekte Ware kommt oft zurück und bringt 1-Stern-Bewertungen. Der Lieferant erstattet den Einkaufspreis.");
            }

            var vc = Card(Root, "Mengenrabatt beim Einkauf", "Händlerstatus −" + Fmt.Dec(s.LevelDiscount() * 100f, 1) + " %");
            foreach (var p in GameData.Products)
            {
                if (!s.ProductUnlocked(p.Id)) continue;
                int units = s.PurchasedOf(p.Id);
                int toNext = s.UnitsToNextVolumeTier(p.Id);
                TableRow(vc, new[] { 220f, 120f, 120f, 200f },
                    new[] { p.Name, units + " gekauft", "−" + Mathf.RoundToInt(s.VolumeDiscount(p.Id) * 100f) + " %", toNext > 0 ? "nächste Stufe in " + toNext : "Maximum" });
            }
            Tip(Root, "Großhändler (ab Level " + GameData.WholesaleLevel + ", mit Markenname) bestellen große Mengen deiner bewährten Produkte – Angebote in der App 'Aufträge'.");
        }
    }
}
