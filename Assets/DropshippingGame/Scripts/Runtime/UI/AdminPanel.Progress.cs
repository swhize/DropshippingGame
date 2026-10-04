using DropshippingGame.Core;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Admin-Reiter "Wirtschaft+": Perks, Energie, Ausrüstung, Müll, Störungen, Defekte, Großhändler.</summary>
    public sealed partial class AdminPanel
    {
        private void BuildProgress()
        {
            var sim = S;
            _body.Add(Info("Energie " + (int)sim.Energy + " · Müll " + sim.Waste + "/" + sim.WasteCapacity() + " · Hustle-Punkte " + sim.PerkPointsAvailable() +
                           " · Porto heute " + sim.Daily.Shipping + " € · Marke " + (int)(sim.BrandStrength() * 100f) + " %"));
            _body.Add(Section("Perks & Einrichtung"));
            var a = WrapRow();
            a.Add(Btn("+5 Hustle-Punkte (XP)", () => Do(() => sim.AdminAddXp(GameData.PerkXpThreshold(sim.PerkPointsTotal() + 5) - sim.Xp))));
            a.Add(Btn("Alle Einrichtung kaufen", () => Do(() =>
            {
                sim.AdminAddMoney(5000);
                foreach (var sl in GameData.HomeSlots)
                    while (sim.NextHomeItem(sl.Id) != null && sim.BuyHomeItem(sl.Id)) { }
            })));
            a.Add(Btn("Energie 100", () => Do(() => sim.Energy = 100f)));
            a.Add(Btn("Energie 5", () => Do(() => sim.Energy = 5f)));
            _body.Add(a);
            _body.Add(Section("Ausrüstung & Müll"));
            var b = WrapRow();
            foreach (var e in GameData.Equipment)
            {
                string id = e.Id;
                b.Add(Btn(e.Name + " (" + sim.EquipmentCount(id) + ")", () => Do(() =>
                {
                    sim.AdminAddMoney(e.Cost);
                    sim.BuyEquipment(id);
                })));
            }
            b.Add(Btn("Müll füllen", () => Do(() => sim.AddWaste(sim.WasteCapacity()))));
            b.Add(Btn("Müll abholen", () => Do(() => sim.CollectGarbage())));
            _body.Add(b);
            _body.Add(Section("Logistik"));
            var c = WrapRow();
            foreach (var d in GameData.Disruptions)
            {
                string id = d.Id;
                c.Add(Btn(d.Name, () => Do(() => sim.StartDisruption(id, 1))));
            }
            c.Add(Btn("Störung beenden", () => Do(sim.EndDisruption)));
            c.Add(Btn("Defekte Charge (Hülle)", () => Do(() =>
            {
                if (sim.StockQty("huelle") < 10) sim.Stock["huelle"].Qty += 20;
                sim.AddDefectBatch("huelle", 10);
            })));
            c.Add(Btn("Großhändler-Anfrage", () => Do(() =>
            {
                if (!sim.BrandNamed) sim.SetBrandName(sim.BrandName);
                sim.BrandNamed = true;
                foreach (var p in GameData.Products)
                    if (sim.ProductAvailable(p.Id) && sim.ShippedPerProduct[p.Id] < GameData.WholesaleMinSold) sim.ShippedPerProduct[p.Id] = GameData.WholesaleMinSold;
                if (sim.GenerateWholesaleOffer() == null) sim.Notify("Kein Produkt verfügbar.", "bad");
            })));
            _body.Add(c);
        }

        private static VisualElement WrapRow()
        {
            var r = Row();
            r.style.flexWrap = Wrap.Wrap;
            return r;
        }
    }
}
