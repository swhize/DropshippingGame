using System.Collections.Generic;
using DropshippingGame.Core;

namespace DropshippingGame.Tests
{
    /// <summary>Gemeinsame Hilfen für die v3.0-Tests.</summary>
    internal static class TestUtil
    {
        /// <summary>Frische Simulation ohne Tutorial, ohne gelistete Produkte und ohne Wochenziele.</summary>
        public static Sim Fresh(int seed = 1)
        {
            var sim = new Sim(seed);
            sim.TutorialStep = -1;
            return sim;
        }

        /// <summary>Simulation auf Level 3 (Großaufträge freigeschaltet, erstes Angebot liegt vor).</summary>
        public static Sim Level3(int seed = 1)
        {
            var sim = Fresh(seed);
            sim.AddXp(GameData.LevelThreshold(3));
            return sim;
        }

        /// <summary>Versandfertiges Paket zu einem neuen Bestellzettel (ohne Lager/Verpackung).</summary>
        public static ItemData Labeled(Sim sim, string pid, bool express = false, float quality = 1f)
        {
            var o = sim.CreateOrder(pid, express);
            var pkg = o.ToItem(quality);
            pkg.Kind = ItemKind.Labeled;
            pkg.PackSize = GameData.Product(pid).Size;
            return pkg;
        }

        /// <summary>Kompletter Weg Bestellung → Regal → Packtisch → Label → Versand. Gibt den Erlös zurück.</summary>
        public static int FulfillOne(Sim sim, string pid, bool express = false)
        {
            if (sim.StockQty(pid) <= 0) sim.Stock[pid].Qty = 5;
            int size = GameData.Product(pid).Size;
            if (sim.Packaging[size] <= 0) sim.Packaging[size] = 5;
            sim.SpawnOrder(pid, express);
            var item = sim.PickItem(pid);
            sim.WrapItem(item);
            var pkg = sim.PickupPackage();
            pkg.Kind = ItemKind.Labeled;
            sim.OnLabeled(pkg);
            return sim.ShipPackage(pkg);
        }

        public static List<string> AllSkillIds()
        {
            var l = new List<string>();
            foreach (var s in GameData.Skills) l.Add(s.Id);
            return l;
        }
    }
}
