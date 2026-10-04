using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// PaketBlitz in der Welt. Versand-Stationen (StationType.Ship) nach Opts.Stage:
    /// 0 = Abholung an der Garage (Gebühr), 1 = Versandkäfig in der Halle (Gebühr, Firmentarif),
    /// 2 = Packstation in der Stadt (gratis), 3 = Schalter in der Filiale (gratis).
    /// Gebühren: <see cref="PostRules"/> (Core, getestet). Im Tutorial ist die Abholung gratis.
    /// </summary>
    public static class PostService
    {
        public const int StagePackstation = 2, StageCounter = 3;
        public const float PostDoorX = 77.5f;
        public static readonly Vector3 CounterPos = new Vector3(73.5f, 0f, -18.5f);
        public static NPC Clerk;
        private static int _feeTips;

        /// <summary>Packstationen (Position, Drehung: Front = lokal +Z).</summary>
        public static readonly (Vector3 pos, float rot, string name)[] Packstations =
        {
            (new Vector3(8f, 0f, 7.55f), 180f, "Parkecke"),
            (new Vector3(90f, 0f, -7.9f), 0f, "Filiale"),
            (new Vector3(-104f, 0f, 7.6f), 180f, "West"),
            (new Vector3(-18f, 0f, 61f), 180f, "Lindenweg"),
            (new Vector3(138f, 0f, -7.9f), 0f, "Ost"),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Clerk = null;
            _feeTips = 0;
        }

        /// <summary>Zusätzliche Stations-Definitionen für WorldBuilder (Typ Ship): Position, Drehung, Stage.</summary>
        public static IEnumerable<(Vector3 pos, float rot, int stage)> StationDefs()
        {
            foreach (var p in Packstations) yield return (p.pos, p.rot, StagePackstation);
            yield return (CounterPos, 0f, StageCounter);
        }

        public static bool IsFreeDrop(Station s) => s != null && s.Type == StationType.Ship && s.Opts.Stage >= StagePackstation;

        public static string Title(int stage)
        {
            switch (stage)
            {
                case 0: return "PaketBlitz-Abholung";
                case 1: return "Versandkäfig (Abholung)";
                case StageCounter: return "PaketBlitz-Filiale: Schalter";
                default: return "PaketBlitz-Packstation";
            }
        }

        private static int Value(PlayerController player)
        {
            int sum = 0;
            foreach (var it in player.Carried()) if (it.Kind == ItemKind.Labeled) sum += it.Price;
            return sum;
        }

        public static string Prompt(Station st, PlayerController player)
        {
            var gm = Game.Sim;
            if (gm == null || player == null) return "";
            int nl = player.CountCarried(ItemKind.Labeled);
            bool pickup = st.Opts.Stage < StagePackstation;
            if (nl > 0)
            {
                int fee = pickup ? PostRules.PickupFee(nl, st.Opts.Stage, PostRules.TutorialActive(gm)) : 0;
                string what = nl > 1 ? nl + " Pakete abgeben" : "Paket abgeben";
                return what + " (+" + Fmt.Money(Value(player)) + (fee > 0 ? " · Abholung −" + Fmt.Money(fee) : " · gratis") + ")";
            }
            if ((player.Held?.Kind ?? ItemKind.None) == ItemKind.Package) return "Erst ein Versandlabel drucken!";
            return pickup ? "Abholung: " + Fmt.Money(PostRules.PickupFeePerParcel(st.Opts.Stage)) + "/Paket · Packstation gratis" : "Etikettierte Pakete gratis abgeben";
        }

        public static void Interact(Station st, PlayerController player)
        {
            var gm = Game.Sim;
            if (gm == null || player == null) return;
            int nl = player.CountCarried(ItemKind.Labeled);
            if (nl <= 0)
            {
                if ((player.Held?.Kind ?? ItemKind.None) == ItemKind.Package)
                {
                    gm.Notify("Ohne Versandlabel nimmt PaketBlitz nichts an!", "bad");
                    Game.Sound("error");
                }
                else gm.Notify(st.Opts.Stage < StagePackstation ? "Abholung kostet pro Paket. Packstationen in der Stadt nehmen gratis an." : "Hier gibst du etikettierte Pakete gratis ab.", "info");
                return;
            }
            bool pickup = st.Opts.Stage < StagePackstation;
            int fee = pickup ? PostRules.PickupFee(nl, st.Opts.Stage, PostRules.TutorialActive(gm)) : 0;
            if (fee > 0 && gm.Money < fee && !gm.AdminInfiniteMoney)
            {
                gm.Notify("Zu wenig Geld für die Abholung – bring die Pakete zur Packstation (gratis).", "bad");
                return;
            }
            var keep = new List<ItemData>();
            int shipped = 0;
            foreach (var it in player.Carried())
            {
                if (it.Kind != ItemKind.Labeled) { keep.Add(it); continue; }
                gm.ShipPackage(it, (st.transform.position + new Vector3(0, 1.8f + shipped * 0.35f, 0)).ToV3());
                shipped++;
            }
            player.SetCarried(keep);
            if (keep.Count > 0) gm.Notify("Pakete ohne Versandlabel bleiben in der Hand.", "info");
            if (fee > 0)
            {
                gm.AdjustMoney(-fee, "");
                gm.Daily.Other += fee;
                if (_feeTips++ < 3) gm.Notify("PaketBlitz-Abholung: −" + Fmt.Money(fee) + ". Tipp: An Packstationen ist die Abgabe gratis.", "info");
            }
            if (st.Opts.Stage == StageCounter && Clerk != null)
            {
                string[] lines = { "Danke! Geht sofort raus.", "Wieder so viele? Respekt!", "Express? Alles ist Express bei uns.", "Schönen Tag noch!" };
                Clerk.Say(lines[Random.Range(0, lines.Length)], 3f);
            }
            if (!pickup) Game.Sound("latch");
        }

        /// <summary>Optik der Packstation (Stage 2) bzw. des Schalters (Stage 3).</summary>
        public static StationKitResult BuildKit(Transform root, int stage)
        {
            var yellow = Mats.Std(new Color(0.98f, 0.8f, 0.12f), 0.45f);
            var dark = Mats.Std(new Color(0.18f, 0.18f, 0.2f), 0.5f);
            var red = Mats.Std(new Color(0.85f, 0.14f, 0.12f), 0.5f);
            if (stage == StageCounter)
            {
                Props.Box(root, new Vector3(2.6f, 1.05f, 0.8f), yellow, new Vector3(0, 0.525f, 0), default, 0.03f);
                Props.Box(root, new Vector3(2.7f, 0.06f, 0.9f), Mats.Std(new Color(0.9f, 0.9f, 0.88f), 0.3f), new Vector3(0, 1.08f, 0), default, 0.01f);
                Props.Box(root, new Vector3(2.6f, 0.15f, 0.02f), red, new Vector3(0, 0.8f, 0.41f), default, 0f, false);
                Props.Box(root, new Vector3(0.45f, 0.08f, 0.4f), Mats.Metal(), new Vector3(0.7f, 1.15f, 0), default, 0.01f);
                Label3D.Create(root, "PAKETANNAHME", 50f, new Color(0.12f, 0.12f, 0.12f), new Vector3(0, 0.5f, 0.42f), false);
                return new StationKitResult { Size = new Vector3(2.7f, 1.2f, 0.9f), Center = new Vector3(0, 0.6f, 0), LabelH = 1.7f };
            }
            Props.Box(root, new Vector3(2.4f, 2f, 0.6f), yellow, new Vector3(0, 1.05f, 0), default, 0.03f);
            Props.Box(root, new Vector3(2.6f, 0.12f, 0.8f), dark, new Vector3(0, 2.11f, 0.05f), default, 0.02f);
            for (int c = 0; c < 4; c++)
                for (int r = 0; r < 4; r++)
                {
                    if (c == 1 && r == 2) continue;
                    Props.Box(root, new Vector3(0.52f, 0.42f, 0.02f), Mats.Std(new Color(0.9f, 0.7f, 0.08f), 0.5f), new Vector3(-0.85f + c * 0.57f, 0.35f + r * 0.47f, 0.31f), default, 0f, false);
                }
            Props.Box(root, new Vector3(0.5f, 0.35f, 0.03f), Mats.Emit(new Color(0.4f, 0.8f, 1f), 1.5f), new Vector3(-0.28f, 1.3f, 0.32f), default, 0f, false);
            Props.Box(root, new Vector3(2.4f, 0.14f, 0.02f), red, new Vector3(0, 1.92f, 0.31f), default, 0f, false);
            Label3D.Create(root, "PaketBlitz Packstation", 44f, new Color(0.1f, 0.1f, 0.1f), new Vector3(0, 1.92f, 0.33f), false);
            return new StationKitResult { Size = new Vector3(2.4f, 2.1f, 0.7f), Center = new Vector3(0, 1.05f, 0), LabelH = 2.5f };
        }
    }
}
