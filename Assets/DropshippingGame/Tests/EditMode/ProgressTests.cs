using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Fortschritt &amp; Wirtschaftstiefe: Perks, Einrichtung, Ausrüstung, Porto, Störungen, Defekte, Müll, Großhändler, Marke.</summary>
    public class ProgressTests
    {
        private static Sim Biz(int seed = 3)
        {
            var gm = TestUtil.Fresh(seed);
            gm.NewGame("skip");
            gm.InGame = true;
            return gm;
        }

        [Test]
        public void Perks_PointsFromXp_AndEffects()
        {
            var gm = Biz();
            Assert.AreEqual(0, gm.PerkPointsTotal());
            gm.AddXp(GameData.PerkXpThreshold(3));
            Assert.AreEqual(3, gm.PerkPointsTotal(), "3 Hustle-Punkte");
            int q = gm.QueueCapacity();
            float buy = gm.PurchasePriceMult();
            float demand = gm.DemandPerkMult();
            Assert.IsTrue(gm.BuyPerk("p_queue") && gm.BuyPerk("p_einkauf") && gm.BuyPerk("p_kunden"));
            Assert.AreEqual(q + 2, gm.QueueCapacity(), "Größere Warteschlange");
            Assert.Less(gm.PurchasePriceMult(), buy, "Billiger einkaufen");
            Assert.Greater(gm.DemandPerkMult(), demand, "Mehr Nachfrage");
            Assert.AreEqual(0, gm.PerkPointsAvailable());
            Assert.IsFalse(gm.BuyPerk("p_liefer"), "Kein Punkt mehr");
            Assert.AreEqual("level", Biz().PerkState("p_team"), "Personal-Perk erst ab Level 5");
        }

        [Test]
        public void Home_UpgradesHaveEffects_AndEnergy()
        {
            var gm = Biz();
            gm.Money = 5000;
            gm.Level = 5;
            float lead = gm.LeadMinutes(1);
            Assert.IsTrue(gm.BuyHomeItem("laptop"));
            Assert.Less(gm.LeadMinutes(1), lead, "Laptop verkürzt Lieferzeit");
            int q = gm.QueueCapacity();
            Assert.IsTrue(gm.BuyHomeItem("desk"));
            Assert.AreEqual(q + 1, gm.QueueCapacity(), "Schreibtisch +1 Warteschlange");
            float drain = gm.EnergyDrainMult();
            Assert.IsTrue(gm.BuyHomeItem("chair") && gm.BuyHomeItem("chair"));
            Assert.Less(gm.EnergyDrainMult(), drain, "Stuhl spart Energie");
            Assert.IsTrue(gm.LifestyleOwned.Contains("gamingstuhl"), "Gaming-Stuhl erscheint in der Welt");
            Assert.IsNull(gm.NextHomeItem("chair"), "Stuhl voll ausgebaut");
            Assert.AreEqual(70f, gm.MorningEnergy(), 0.01f, "Matratze: 70");
            gm.BuyHomeItem("bed");
            gm.BuyHomeItem("coffee");
            Assert.AreEqual(95f, gm.MorningEnergy(), 0.01f, "Bettsofa + Filterkaffee");
            gm.Energy = 100f;
            Assert.AreEqual(1f, gm.WalkSpeedMult(), 0.001f, "Ausgeruht: volles Tempo");
            gm.Energy = 0f;
            Assert.AreEqual(GameData.EnergyMinSpeed, gm.WalkSpeedMult(), 0.001f, "Erschöpft: langsamer");
            gm.Energy = 100f;
            gm.AdvanceMinutes(300f);
            Assert.Less(gm.Energy, 100f, "Energie sinkt über den Tag");
        }

        [Test]
        public void Equipment_FoldingMachineAndStations()
        {
            var gm = Biz();
            gm.Money = 5000;
            gm.Level = 3;
            gm.Packaging = new[] { 0, 0, 0 };
            gm.FlatPackaging = new[] { 10, 0, 0 };
            Assert.IsTrue(gm.BuyEquipment("faltmaschine"));
            gm.AdvanceMinutes(GameData.FoldMachineMinutes * 3 + 0.5f);
            Assert.AreEqual(3, gm.Packaging[0], "Faltmaschine faltet automatisch");
            Assert.IsTrue(gm.BuyEquipment("packtisch"));
            Assert.AreEqual("max", gm.EquipmentState("packtisch"), "Garage: nur ein Zusatz-Packtisch");
            Assert.Greater(gm.StaffRoleMult("packer"), 1f, "Packer:innen schneller");
            Assert.IsTrue(gm.BuyEquipment("labelgun"));
            Assert.IsTrue(gm.AutoLabelOnPickup && gm.CarryBonus() == 1);
        }

        [Test]
        public void Scale_VolumeDiscountAndPortoTiers()
        {
            var gm = Biz();
            int before = gm.BulkCost(0, 1, 1);
            gm.PurchasedUnits["huelle"] = 400;
            Assert.Less(gm.BulkCost(0, 1, 1), before, "Mengenrabatt");
            float p0 = gm.PortoFor(0);
            gm.TotalShipped = 300;
            Assert.Less(gm.PortoFor(0), p0, "Porto sinkt mit Volumen");
            Assert.Greater(gm.PortoFor(0, 1), gm.PortoFor(0, 0), "Express-Kurier ist teurer");
            gm.TotalShipped = 0;
            int money = gm.Money;
            for (int i = 0; i < 5; i++) TestUtil.FulfillOne(gm, "huelle");
            Assert.Greater(gm.Daily.Shipping, 0, "Porto wird gebucht");
            Assert.AreEqual(gm.Daily.Shipping, gm.TotalPorto);
        }

        [Test]
        public void Disruption_DelaysStandard_NotTurbo()
        {
            var gm = Biz();
            gm.StartDisruption("streik", 1);
            Assert.IsNotNull(gm.ActiveDisruption);
            for (int i = 0; i < 20; i++) TestUtil.FulfillOne(gm, "huelle");
            Assert.Greater(gm.TotalDelayed, 0, "Streik verspätet Pakete");
            int d = gm.TotalDelayed;
            gm.SetCarrier(1);
            for (int i = 0; i < 20; i++) TestUtil.FulfillOne(gm, "huelle");
            Assert.AreEqual(d, gm.TotalDelayed, "TurboKurier ist immun");
            gm.EndDayNow();
            gm.StartNextDay();
            Assert.IsNull(gm.ActiveDisruption, "Störung endet");
        }

        [Test]
        public void Defects_CauseReturns_RecallRefunds()
        {
            var gm = Biz();
            gm.Stock["huelle"].Qty = 40;
            gm.AddDefectBatch("huelle", 20);
            Assert.AreEqual(20, gm.DefectsOf("huelle"));
            Assert.Contains("huelle", gm.DefectiveProducts());
            int money = gm.Money;
            int n = gm.RecallDefects("huelle");
            Assert.AreEqual(20, n);
            Assert.AreEqual(20, gm.StockQty("huelle"));
            Assert.Greater(gm.Money, money, "Lieferant erstattet");
            var gm2 = Biz(9);
            gm2.Stock["huelle"].Qty = 30;
            gm2.AddDefectBatch("huelle", 30);
            for (int i = 0; i < 10; i++) TestUtil.FulfillOne(gm2, "huelle");
            Assert.Greater(gm2.TotalDefectsShipped, 0);
            Assert.Greater(gm2.ReturnsIncoming.Count, 0, "Defekte kommen zurück");
        }

        [Test]
        public void Waste_FillsBins_CollectViaEvent()
        {
            var gm = Biz();
            gm.AddWaste(gm.WasteCapacity());
            Assert.IsTrue(gm.WasteFull);
            Assert.Less(gm.WalkSpeedMult(), 1f, "Volle Tonnen bremsen");
            float staff = gm.StaffSpeedMult();
            Assert.AreEqual(gm.WasteCapacity(), gm.CollectGarbage());
            Assert.Greater(gm.StaffSpeedMult(), staff);
            // Mit Müllwagen: Ereignis statt Sofort-Leerung.
            int calls = 0;
            gm.GarbagePickupDue += _ => calls++;
            gm.AddWaste(10);
            Assert.IsTrue(gm.OrderGarbagePickup());
            Assert.AreEqual(1, calls);
            Assert.IsTrue(gm.GarbagePending && gm.Waste == 10);
            gm.CollectGarbage();
            Assert.AreEqual(0, gm.Waste);
            int cap = gm.WasteCapacity();
            gm.Money = 1000;
            gm.BuyEquipment("container");
            Assert.AreEqual(cap + GameData.WasteContainerBonus, gm.WasteCapacity());
            TestUtil.FulfillOne(gm, "huelle");
            Assert.AreEqual(GameData.WastePerPackage, gm.Waste, "Packen macht Müll");
        }

        [Test]
        public void Wholesale_And_BrandPricing()
        {
            var gm = Biz();
            gm.AddXp(GameData.LevelThreshold(4));
            Assert.IsFalse(gm.WholesaleUnlocked, "Ohne Markenname keine Großhändler");
            gm.SetBrandName("Glanz");
            gm.ShippedPerProduct["huelle"] = 50;
            var c = gm.GenerateWholesaleOffer();
            Assert.IsNotNull(c);
            Assert.IsTrue(c.Wholesale && c.Quantity >= 30);
            var json = c.ToJson();
            Assert.IsTrue(Contract.FromJson(json).Wholesale, "Großhändler-Flag gespeichert");
            float weak = gm.BrandPriceMult();
            gm.Reputation = 5f;
            gm.ReviewCount = 200;
            gm.Awareness = 1f;
            Assert.Greater(gm.BrandPriceMult(), weak);
            Assert.Greater(gm.SuggestedPrice("huelle"), (int)gm.Market.MarketPrice("huelle"), "Markenpreis über Markt");
            gm.SetListed("huelle", true);
            gm.SetShopPrice("huelle", (int)gm.Market.MarketPrice("huelle"));
            float atMarket = gm.DemandRate("huelle");
            gm.SetShopPrice("huelle", gm.SuggestedPrice("huelle"));
            Assert.AreEqual(atMarket, gm.DemandRate("huelle"), atMarket * 0.02f, "Markenaufschlag kostet keine Kundschaft");
        }

        [Test]
        public void Save_RoundTrip_AndOldSaveDefaults()
        {
            var gm = Biz();
            gm.Money = 5000;
            gm.AddXp(GameData.PerkXpThreshold(2));
            gm.BuyPerk("p_porto");
            gm.BuyHomeItem("bed");
            gm.BuyEquipment("container");
            gm.PurchasedUnits["led"] = 123;
            gm.SetCarrier(1);
            gm.StartDisruption("unwetter", 2);
            gm.Stock["huelle"].Qty = 10;
            gm.AddDefectBatch("huelle", 4);
            gm.AddWaste(7);
            gm.Energy = 33f;
            var gm2 = new Sim(1);
            gm2.FromJson(Json.Parse(Json.Write(gm.ToJson())) as Dictionary<string, object>);
            Assert.AreEqual(1, gm2.PerkRank("p_porto"));
            Assert.AreEqual(1, gm2.HomeTier("bed"));
            Assert.AreEqual(1, gm2.EquipmentCount("container"));
            Assert.AreEqual(123, gm2.PurchasedOf("led"));
            Assert.AreEqual(1, gm2.Carrier);
            Assert.AreEqual("unwetter", gm2.DisruptionId);
            Assert.AreEqual(4, gm2.DefectsOf("huelle"));
            Assert.AreEqual(7, gm2.Waste);
            Assert.AreEqual(33f, gm2.Energy, 0.01f);
            // Alter Spielstand ohne neue Felder
            var old = gm.ToJson();
            foreach (var k in new[] { "perk_ranks", "home_tiers", "energy", "equipment", "purchased_units", "carrier", "waste", "defect_units", "disruption_id" })
                old.Remove(k);
            var gm3 = new Sim(1);
            gm3.FromJson(old);
            Assert.AreEqual(0, gm3.PerkPointsSpent());
            Assert.AreEqual(GameData.EnergyMax, gm3.Energy);
            Assert.AreEqual(0, gm3.Carrier);
            Assert.AreEqual(0, gm3.Waste);
            Assert.IsNull(gm3.ActiveDisruption);
        }
    }
}
