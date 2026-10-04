using System;
using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Admin-Panel-Knöpfe (F10 › Welt & Zeit) für Stadt, Post, Verkehr, Müllabfuhr und Emotes.</summary>
    public static class CityDebug
    {
        public static IEnumerable<(string label, bool close, Action run)> Actions()
        {
            yield return ("TP: PaketBlitz-Filiale", true, () => Tp(new Vector3(PostService.PostDoorX, 0.05f, -5.5f), 180f));
            yield return ("TP: Packstation Parkecke", true, () => Tp(new Vector3(8f, 0.05f, 5f), 0f));
            yield return ("TP: Wohnviertel", true, () => Tp(new Vector3(-18f, 0.05f, 52f), 0f));
            yield return ("TP: Finanzviertel (Lot)", true, () => Tp(WorldLots.Finanzviertel.StreetAccess + Vector3.up * 0.05f, 0f));
            yield return ("TP: Einkaufsviertel (Lot)", true, () => Tp(WorldLots.Einkaufsviertel.StreetAccess + Vector3.up * 0.05f, 180f));
            yield return ("TP: Westende", true, () => Tp(new Vector3(-145f, 0.05f, 5.5f), 90f));
            yield return ("TP: Ostende", true, () => Tp(new Vector3(145f, 0.05f, -5.5f), -90f));
            yield return ("Müllabfuhr jetzt", true, () => { if (GarbageTruck.Instance != null) GarbageTruck.Instance.StartRound(); });
            yield return ("Alle Mülleimer voll", false, () => { foreach (var b in CityServices.Bins) CityServices.AddFill(b, 1f); });
            foreach (var k in new[] { "bus", "police", "van", "truck" })
            {
                string kind = k;
                yield return ("Verkehr: " + kind, true, () => Traffic()?.Spawn(UnityEngine.Random.value < 0.5f ? 0 : 1, kind));
            }
            yield return ("+3 etikettierte Pakete in die Hand", true, GivePackages);
            for (int i = 0; i < 6; i++)
            {
                var e = (EmoteController.Emote)i;
                yield return ("Emote: " + e, true, () => Game.Player?.GetComponent<EmoteController>()?.Play(e));
            }
        }

        private static CityTraffic Traffic() => Game.World != null ? Game.World.GetComponent<CityTraffic>() : null;

        private static void Tp(Vector3 p, float yaw)
        {
            if (Game.Player != null) Game.Player.Teleport(p, yaw);
        }

        private static void GivePackages()
        {
            var sim = Game.Sim;
            var p = Game.Player;
            if (sim == null || p == null) return;
            var items = new List<Core.ItemData>();
            for (int i = 0; i < 3; i++)
            {
                var o = sim.CreateOrder(Core.GameData.Products[0].Id, false);
                var pkg = o.ToItem(1f);
                pkg.Kind = Core.ItemKind.Labeled;
                items.Add(pkg);
            }
            p.SetCarried(items);
        }
    }
}
