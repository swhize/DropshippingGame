using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    public enum StationType
    {
        Pc,
        Dock,
        Regal,
        Pack,
        Label,
        Ship,
        Fold,
        Conveyor,
        DinerPass,
        DinerTable,
        NpcTalk,
        EndDay,
        Stand,
        /// <summary>v3.0: Retourenfach am Wareneingang (Sim.DockReturns).</summary>
        ReturnTray,
        /// <summary>v3.0: Retourenplatz – Prüftisch "als B-Ware einlagern".</summary>
        ReturnDesk,
        /// <summary>v3.0: Retourenplatz – Container "entsorgen".</summary>
        ReturnBin,
        /// <summary>v3.0: Palettenplatz für Großaufträge (B2B).</summary>
        Pallet,
        /// <summary>v3.0: Bestell-Monitor an der Wand (offene Zettel).</summary>
        Monitor,
        /// <summary>v3.0: Zonen-Schild (nicht interaktiv).</summary>
        Sign,
    }

    /// <summary>Optionen beim Bau einer Station.</summary>
    public sealed class StationOpts
    {
        public int ProductIndex;
        public int TableId;
        public int Stage;
        /// <summary>Text für Schilder (StationType.Sign).</summary>
        public string Text = "";
        /// <summary>Farbe für Schilder.</summary>
        public Color Color = new Color(0.95f, 0.78f, 0.1f);
    }

    /// <summary>Ergebnis von <see cref="StationKit.Build"/>: Kollisionsbox, Label-Höhe und Container für dynamische Inhalte.</summary>
    public sealed class StationKitResult
    {
        public Vector3 Size = Vector3.one;
        public Vector3 Center = new Vector3(0, 0.5f, 0);
        public float LabelH = 1.3f;
        public Transform Content;
        public NPC Npc;
    }

    /// <summary>
    /// Baut das 3D-Modell jeder Station. Lokaler Ursprung = Bodenmitte, Vorderseite zeigt nach +Z.
    /// "Content" ist ein leerer Knoten, in den die Station dynamische Dinge legt
    /// (Kisten im Regal, Pakete auf dem Tisch ...).
    /// </summary>
    public static class StationKit
    {
        public static StationKitResult Build(Transform root, StationType type, StationOpts o)
        {
            switch (type)
            {
                case StationType.Pc: return Pc(root, o.Stage);
                case StationType.Dock: return Dock(root, o.Stage);
                case StationType.Regal: return Regal(root, o);
                case StationType.Pack: return Pack(root);
                case StationType.Label: return LabelPrinter(root);
                case StationType.Ship: return Ship(root, o.Stage);
                case StationType.Fold: return Fold(root);
                case StationType.Conveyor: return ConveyorIn(root);
                case StationType.DinerPass: return DinerPass(root);
                case StationType.DinerTable: return DinerTable(root, o);
                case StationType.NpcTalk: return Kalle(root);
                case StationType.EndDay: return EndDay(root, o.Stage);
                case StationType.Stand: return Stand(root);
                case StationType.ReturnTray: return ReturnTray(root);
                case StationType.ReturnDesk: return ReturnDesk(root);
                case StationType.ReturnBin: return ReturnBin(root);
                case StationType.Pallet: return PalletPlace(root);
                case StationType.Monitor: return Monitor(root, o.Stage);
                case StationType.Sign: return ZoneSign(root, o);
            }
            Props.Box(root, Vector3.one, Mats.Std(Color.magenta), new Vector3(0, 0.5f, 0));
            return new StationKitResult();
        }

        private static Transform Content(Transform root, Vector3 pos) => Props.Node(root, "Content", pos).transform;

        // ---- PC ------------------------------------------------------------------------------------
        private static StationKitResult Pc(Transform root, int stage)
        {
            if (stage == 0)
            {
                Props.Table(root, 1.7f, 0.75f, 0.92f, Mats.Wood(), Mats.DarkMetal());
                Props.Box(root, new Vector3(1.7f, 0.9f, 0.03f), Mats.Std(new Color(0.55f, 0.45f, 0.32f), 0.9f), new Vector3(0, 1.5f, -0.36f), default, 0f);
                for (int i = 0; i < 6; i++)
                    Props.Box(root, new Vector3(0.04f, 0.22f, 0.03f), Mats.Std(i % 2 == 0 ? new Color(0.7f, 0.2f, 0.2f) : new Color(0.25f, 0.25f, 0.28f), 0.5f),
                        new Vector3(-0.6f + i * 0.2f, 1.55f, -0.33f), default, 0f);
                Laptop(root, new Vector3(-0.15f, 0.93f, 0.02f));
                Props.Cyl(root, 0.07f, 0.09f, 0.03f, Mats.DarkMetal(), new Vector3(0.55f, 0.935f, -0.15f));
                Props.Box(root, new Vector3(0.03f, 0.4f, 0.03f), Mats.DarkMetal(), new Vector3(0.55f, 1.13f, -0.15f), new Vector3(0, 0, 10));
                Props.Cyl(root, 0.05f, 0.1f, 0.1f, Mats.Std(new Color(0.9f, 0.3f, 0.2f), 0.5f), new Vector3(0.5f, 1.33f, -0.08f), new Vector3(35, 0, 0));
                Props.PointLight(root, new Vector3(0.45f, 1.2f, 0.05f), new Color(1f, 0.85f, 0.65f), 0.8f, 2.2f);
                Props.Cyl(root, 0.045f, 0.04f, 0.1f, Mats.Std(new Color(0.95f, 0.95f, 0.95f), 0.4f), new Vector3(0.3f, 0.97f, 0.15f));
                return new StationKitResult { Size = new Vector3(1.7f, 1f, 0.75f), Center = new Vector3(0, 0.5f, 0), LabelH = 1.55f };
            }
            Props.Table(root, 1.8f, 0.85f, 0.76f, Mats.Std(new Color(0.92f, 0.92f, 0.9f), 0.5f), Mats.DarkMetal());
            Props.Box(root, new Vector3(0.05f, 0.14f, 0.05f), Mats.DarkMetal(), new Vector3(0, 0.83f, -0.2f));
            Props.Box(root, new Vector3(0.8f, 0.48f, 0.04f), Mats.Std(new Color(0.08f, 0.08f, 0.09f), 0.3f), new Vector3(0, 1.12f, -0.22f), default, 0.01f);
            Props.Box(root, new Vector3(0.74f, 0.42f, 0.01f), Mats.Emit(new Color(0.3f, 0.6f, 0.95f), 1.3f), new Vector3(0, 1.12f, -0.195f), default, 0f, false);
            Props.Box(root, new Vector3(0.44f, 0.02f, 0.15f), Mats.Std(new Color(0.15f, 0.15f, 0.17f), 0.5f), new Vector3(0, 0.77f, 0.12f), default, 0.005f);
            var chair = Props.Chair(root, Mats.Std(new Color(0.15f, 0.15f, 0.18f), 0.6f));
            chair.transform.localPosition = new Vector3(0.1f, 0, 0.75f);
            chair.transform.localRotation = Quaternion.Euler(0, 180, 0);
            return new StationKitResult { Size = new Vector3(1.8f, 1f, 0.85f), Center = new Vector3(0, 0.5f, 0), LabelH = 1.55f };
        }

        private static void Laptop(Transform root, Vector3 pos)
        {
            var body = Mats.Std(new Color(0.7f, 0.72f, 0.75f), 0.3f, 0.7f);
            Props.Box(root, new Vector3(0.38f, 0.02f, 0.26f), body, pos + new Vector3(0, 0.01f, 0), default, 0.006f);
            var lid = Props.Node(root, "Lid", pos + new Vector3(0, 0.02f, -0.13f));
            lid.transform.localRotation = Quaternion.Euler(-12f, 0, 0);
            Props.Box(lid.transform, new Vector3(0.38f, 0.25f, 0.012f), body, new Vector3(0, 0.125f, 0), default, 0.004f);
            Props.Box(lid.transform, new Vector3(0.34f, 0.21f, 0.004f), Mats.Emit(new Color(0.35f, 0.65f, 1f), 1.5f), new Vector3(0, 0.125f, 0.008f), default, 0f, false);
            Props.Box(root, new Vector3(0.3f, 0.004f, 0.11f), Mats.Std(new Color(0.2f, 0.2f, 0.22f), 0.6f), pos + new Vector3(0, 0.022f, 0.03f), default, 0f, false);
        }

        // ---- Wareneingang --------------------------------------------------------------------------
        private static StationKitResult Dock(Transform root, int stage)
        {
            float w = stage == 0 ? 1.5f : 2.8f;
            float d = stage == 0 ? 1.4f : 2.2f;
            var stripeA = Mats.Std(new Color(0.95f, 0.78f, 0.1f), 0.6f);
            var stripeB = Mats.Std(new Color(0.1f, 0.1f, 0.1f), 0.6f);
            int count = (int)(w / 0.25f);
            for (int i = 0; i < count; i++)
            {
                var m = i % 2 == 0 ? stripeA : stripeB;
                Props.Box(root, new Vector3(0.25f, 0.01f, 0.08f), m, new Vector3(-w / 2f + 0.125f + i * 0.25f, 0.006f, d / 2f), default, 0f, false);
                Props.Box(root, new Vector3(0.25f, 0.01f, 0.08f), m, new Vector3(-w / 2f + 0.125f + i * 0.25f, 0.006f, -d / 2f), default, 0f, false);
            }
            var pals = stage == 0 ? new[] { Vector3.zero } : new[] { new Vector3(-0.65f, 0, 0), new Vector3(0.65f, 0, 0) };
            foreach (var pp in pals) Props.Pallet(root).transform.localPosition = pp;
            float signH = stage == 0 ? 1.6f : 2.2f;
            var sign = Props.SignBoard(root, "WARENEINGANG", new Color(0.95f, 0.78f, 0.1f), new Color(0.1f, 0.1f, 0.1f), new Vector2(1f, 0.22f));
            sign.transform.localPosition = new Vector3(0, signH, -d / 2f + 0.05f);
            sign.transform.localScale = Vector3.one * (stage == 0 ? 0.8f : 1.2f);
            Props.Box(root, new Vector3(0.05f, signH, 0.05f), Mats.DarkMetal(), new Vector3(w / 2f - 0.05f, signH / 2f, -d / 2f));
            return new StationKitResult { Size = new Vector3(w, 0.7f, d), Center = new Vector3(0, 0.35f, 0), LabelH = 1.3f, Content = Content(root, new Vector3(0, 0.15f, 0)) };
        }

        // ---- Regal ------------------------------------------------------------------------------------
        private static StationKitResult Regal(Transform root, StationOpts o)
        {
            var p = GameData.Products[o.ProductIndex];
            var pc = p.Color.ToColor();
            if (o.Stage == 0)
            {
                var up = Mats.Std(new Color(0.3f, 0.32f, 0.35f), 0.45f, 0.6f);
                var shelf = Mats.Std(new Color(0.62f, 0.64f, 0.66f), 0.4f, 0.7f);
                foreach (float sx in new[] { -0.88f, 0.88f })
                foreach (float sz in new[] { -0.24f, 0.24f })
                    Props.Box(root, new Vector3(0.04f, 2f, 0.04f), up, new Vector3(sx, 1f, sz));
                for (int i = 0; i < 4; i++) Props.Box(root, new Vector3(1.8f, 0.03f, 0.54f), shelf, new Vector3(0, 0.1f + i * 0.5f, 0));
                Props.Box(root, new Vector3(0.6f, 0.12f, 0.01f), Mats.Std(pc, 0.5f), new Vector3(0, 0.52f, 0.275f), default, 0f, false);
                Label3D.Create(root, p.Name, 30f, pc.grayscale > 0.6f ? new Color(0.1f, 0.1f, 0.1f) : Color.white, new Vector3(0, 0.52f, 0.285f), false);
                return new StationKitResult { Size = new Vector3(1.8f, 2f, 0.55f), Center = new Vector3(0, 1f, 0), LabelH = 2.35f, Content = Content(root, Vector3.zero) };
            }
            var blue = Mats.Std(new Color(0.16f, 0.32f, 0.62f), 0.5f, 0.5f);
            var orange = Mats.Std(new Color(0.95f, 0.45f, 0.1f), 0.5f, 0.4f);
            foreach (float sx in new[] { -1.98f, 1.98f })
            {
                foreach (float sz in new[] { -0.5f, 0.5f }) Props.Box(root, new Vector3(0.08f, 3.6f, 0.08f), blue, new Vector3(sx, 1.8f, sz));
                for (int i = 0; i < 6; i++)
                    Props.Box(root, new Vector3(0.03f, 0.03f, 1f), blue, new Vector3(sx, 0.3f + i * 0.6f, 0), new Vector3(i % 2 == 0 ? 35 : -35, 0, 0), 0f);
            }
            for (int lvl = 0; lvl < 3; lvl++)
            {
                float y = 0.15f + lvl * 1.15f;
                foreach (float sz in new[] { -0.5f, 0.5f }) Props.Box(root, new Vector3(4f, 0.1f, 0.06f), orange, new Vector3(0, y, sz));
                foreach (float sx in new[] { -1f, 1f })
                {
                    var pal = Props.Pallet(root);
                    pal.transform.localPosition = new Vector3(sx, y + 0.05f - 0.15f, 0);
                    pal.transform.localScale = new Vector3(1.3f, 1f, 0.95f);
                }
            }
            var sign = Props.SignBoard(root, p.Name.ToUpperInvariant(), pc.Darkened(0.2f), Color.white, new Vector2(2.4f, 0.4f));
            sign.transform.localPosition = new Vector3(0, 3.85f, 0.45f);
            return new StationKitResult { Size = new Vector3(4f, 3.6f, 1.1f), Center = new Vector3(0, 1.8f, 0), LabelH = 4.4f, Content = Content(root, Vector3.zero) };
        }

        // ---- Packtisch --------------------------------------------------------------------------------
        private static StationKitResult Pack(Transform root)
        {
            Props.Table(root, 1.6f, 0.8f, 0.92f, Mats.Std(new Color(0.85f, 0.83f, 0.78f), 0.6f), Mats.DarkMetal());
            Props.Box(root, new Vector3(0.8f, 0.005f, 0.55f), Mats.Std(new Color(0.2f, 0.45f, 0.3f), 0.8f), new Vector3(-0.3f, 0.925f, 0.05f), default, 0f, false);
            Props.Box(root, new Vector3(0.18f, 0.06f, 0.08f), Mats.Std(new Color(0.8f, 0.15f, 0.15f), 0.5f), new Vector3(-0.62f, 0.95f, -0.25f));
            Props.Cyl(root, 0.06f, 0.06f, 0.05f, Mats.Std(new Color(0.8f, 0.7f, 0.45f), 0.4f), new Vector3(-0.62f, 1.01f, -0.25f), new Vector3(90, 0, 0));
            Props.Cyl(root, 0.14f, 0.14f, 0.7f, Mats.Std(new Color(0.8f, 0.9f, 1f, 0.6f), 0.2f), new Vector3(0, 1.05f, -0.28f), new Vector3(0, 0, 90));
            Props.Box(root, new Vector3(0.12f, 0.015f, 0.03f), Mats.Std(new Color(0.95f, 0.7f, 0.1f), 0.4f), new Vector3(-0.15f, 0.935f, 0.2f));
            return new StationKitResult { Size = new Vector3(1.6f, 1f, 0.8f), Center = new Vector3(0, 0.5f, 0), LabelH = 1.6f, Content = Content(root, new Vector3(0.45f, 0.925f, 0.05f)) };
        }

        // ---- Labeldrucker -------------------------------------------------------------------------------
        private static StationKitResult LabelPrinter(Transform root)
        {
            Props.Table(root, 1f, 0.65f, 0.92f, Mats.Std(new Color(0.3f, 0.32f, 0.36f), 0.5f, 0.4f), Mats.DarkMetal());
            var body = Mats.Std(new Color(0.92f, 0.92f, 0.9f), 0.4f);
            Props.Box(root, new Vector3(0.42f, 0.24f, 0.36f), body, new Vector3(-0.1f, 1.04f, 0), default, 0.03f);
            Props.Box(root, new Vector3(0.44f, 0.05f, 0.3f), Mats.Std(new Color(0.2f, 0.2f, 0.22f), 0.5f), new Vector3(-0.1f, 1.18f, -0.01f), default, 0.01f);
            Props.Box(root, new Vector3(0.32f, 0.01f, 0.12f), Mats.Std(Color.white, 0.6f), new Vector3(-0.1f, 0.99f, 0.23f), new Vector3(-20, 0, 0), 0f);
            Props.Box(root, new Vector3(0.06f, 0.03f, 0.01f), Mats.Emit(new Color(0.2f, 1f, 0.3f), 3f), new Vector3(0.05f, 1.1f, 0.181f), default, 0f, false);
            Props.Cyl(root, 0.07f, 0.07f, 0.2f, Mats.Std(Color.white, 0.6f), new Vector3(0.3f, 1f, 0), new Vector3(0, 0, 90));
            Props.Box(root, new Vector3(0.14f, 0.1f, 0.02f), Mats.Emit(new Color(0.4f, 0.75f, 1f), 1f), new Vector3(-0.22f, 1.1f, 0.181f), default, 0f, false);
            return new StationKitResult { Size = new Vector3(1f, 1.2f, 0.65f), Center = new Vector3(0, 0.6f, 0), LabelH = 1.65f };
        }

        // ---- Versand ------------------------------------------------------------------------------------
        private static StationKitResult Ship(Transform root, int stage)
        {
            if (stage == 0)
            {
                var yellow = Mats.Std(new Color(0.98f, 0.78f, 0.1f), 0.45f);
                Props.Box(root, new Vector3(1f, 1.35f, 0.75f), yellow, new Vector3(0, 0.75f, 0), default, 0.03f);
                Props.Box(root, new Vector3(1.06f, 0.08f, 0.8f), Mats.Std(new Color(0.2f, 0.2f, 0.22f), 0.5f), new Vector3(0, 1.46f, 0), default, 0.02f);
                Props.Box(root, new Vector3(0.7f, 0.12f, 0.02f), Mats.Std(new Color(0.1f, 0.1f, 0.1f), 0.4f), new Vector3(0, 1.15f, 0.38f), default, 0f);
                Props.Box(root, new Vector3(1f, 0.16f, 0.01f), Mats.Std(new Color(0.85f, 0.12f, 0.12f), 0.5f), new Vector3(0, 0.75f, 0.38f), default, 0f, false);
                Label3D.Create(root, "PaketBlitz", 72f, new Color(0.12f, 0.12f, 0.12f), new Vector3(0, 0.45f, 0.39f), false);
                Props.Box(root, new Vector3(0.9f, 0.08f, 0.65f), Mats.DarkMetal(), new Vector3(0, 0.04f, 0));
                return new StationKitResult { Size = new Vector3(1f, 1.5f, 0.75f), Center = new Vector3(0, 0.75f, 0), LabelH = 1.9f };
            }
            var wire = Mats.Std(new Color(0.7f, 0.72f, 0.75f), 0.35f, 0.8f);
            Props.Box(root, new Vector3(1.2f, 0.06f, 0.8f), wire, new Vector3(0, 0.18f, 0));
            foreach (float fx in new[] { -0.5f, 0.5f })
            foreach (float fz in new[] { -0.32f, 0.32f })
                Props.Cyl(root, 0.07f, 0.07f, 0.05f, Mats.Std(new Color(0.1f, 0.1f, 0.1f)), new Vector3(fx, 0.07f, fz), new Vector3(90, 0, 0));
            for (int i = 0; i < 7; i++)
            {
                Props.Box(root, new Vector3(0.02f, 1.5f, 0.02f), wire, new Vector3(-0.6f + i * 0.2f, 0.95f, -0.4f), default, 0f);
                Props.Box(root, new Vector3(0.02f, 1.5f, 0.02f), wire, new Vector3(-0.6f + i * 0.2f, 0.95f, 0.4f), default, 0f);
            }
            for (int i = 0; i < 5; i++)
            {
                Props.Box(root, new Vector3(0.02f, 1.5f, 0.02f), wire, new Vector3(-0.6f, 0.95f, -0.4f + i * 0.2f), default, 0f);
                Props.Box(root, new Vector3(0.02f, 1.5f, 0.02f), wire, new Vector3(0.6f, 0.95f, -0.4f + i * 0.2f), default, 0f);
            }
            for (int j = 0; j < 4; j++)
            {
                Props.Box(root, new Vector3(1.2f, 0.02f, 0.02f), wire, new Vector3(0, 0.3f + j * 0.45f, 0.4f), default, 0f);
                Props.Box(root, new Vector3(1.2f, 0.02f, 0.02f), wire, new Vector3(0, 0.3f + j * 0.45f, -0.4f), default, 0f);
            }
            var sign = Props.SignBoard(root, "VERSAND", new Color(0.2f, 0.7f, 0.35f), Color.white, new Vector2(1f, 0.25f));
            sign.transform.localPosition = new Vector3(0, 1.9f, 0);
            return new StationKitResult { Size = new Vector3(1.2f, 1.7f, 0.8f), Center = new Vector3(0, 0.85f, 0), LabelH = 2.35f, Content = Content(root, new Vector3(0, 0.21f, 0)) };
        }

        // ---- Falttisch -----------------------------------------------------------------------------------
        private static StationKitResult Fold(Transform root)
        {
            Props.Table(root, 1.4f, 0.75f, 0.9f, Mats.Planks(new Color(0.78f, 0.64f, 0.46f), 0.15f), Mats.DarkMetal());
            var half = Props.Node(root, "Half", new Vector3(0.38f, 0.9f, 0));
            Props.Box(half.transform, new Vector3(0.36f, 0.26f, 0.3f), Mats.Cardboard(), new Vector3(0, 0.13f, 0), default, 0.01f);
            Props.Box(half.transform, new Vector3(0.36f, 0.01f, 0.16f), Mats.Cardboard(), new Vector3(0, 0.3f, 0.2f), new Vector3(-55, 0, 0), 0f);
            return new StationKitResult { Size = new Vector3(1.4f, 1f, 0.75f), Center = new Vector3(0, 0.5f, 0), LabelH = 1.55f, Content = Content(root, new Vector3(-0.22f, 0.9f, 0)) };
        }

        // ---- Förderband-Einlauf ------------------------------------------------------------------------------
        private static StationKitResult ConveyorIn(Transform root)
        {
            var frame = Mats.Std(new Color(0.2f, 0.45f, 0.25f), 0.5f, 0.4f);
            Props.Box(root, new Vector3(1f, 0.8f, 0.9f), frame, new Vector3(0, 0.4f, 0), default, 0.02f);
            var belt = Props.Box(root, new Vector3(0.9f, 0.04f, 0.8f), Mats.Conveyor(), new Vector3(0, 0.82f, 0), default, 0f);
            belt.AddComponent<BeltScroller>();
            foreach (float sz in new[] { -0.43f, 0.43f })
                Props.Box(root, new Vector3(1f, 0.12f, 0.04f), Mats.Std(new Color(0.95f, 0.8f, 0.1f), 0.5f), new Vector3(0, 0.88f, sz));
            Props.Box(root, new Vector3(0.12f, 0.06f, 0.02f), Mats.Emit(new Color(0.2f, 1f, 0.3f), 3f), new Vector3(0.35f, 0.65f, 0.451f), default, 0f, false);
            return new StationKitResult { Size = new Vector3(1f, 0.95f, 0.9f), Center = new Vector3(0, 0.47f, 0), LabelH = 1.5f };
        }

        // ---- Imbiss --------------------------------------------------------------------------------------------
        private static StationKitResult DinerPass(Transform root)
        {
            var steel = Mats.Std(new Color(0.78f, 0.8f, 0.82f), 0.25f, 0.9f);
            foreach (float sx in new[] { -0.55f, 0.55f }) Props.Box(root, new Vector3(0.04f, 0.55f, 0.04f), steel, new Vector3(sx, 1.28f, 0));
            Props.Box(root, new Vector3(1.2f, 0.03f, 0.5f), steel, new Vector3(0, 1.03f, 0));
            Props.Box(root, new Vector3(1.2f, 0.06f, 0.18f), steel, new Vector3(0, 1.56f, 0));
            Props.Box(root, new Vector3(1f, 0.02f, 0.1f), Mats.Emit(new Color(1f, 0.45f, 0.15f), 3f), new Vector3(0, 1.52f, 0), default, 0f, false);
            Props.Sphere(root, 0.05f, Mats.Std(new Color(0.9f, 0.75f, 0.3f), 0.2f, 0.9f), new Vector3(0.45f, 1.07f, 0.15f));
            return new StationKitResult { Size = new Vector3(1.2f, 1.7f, 0.6f), Center = new Vector3(0, 0.85f, 0), LabelH = 1.95f, Content = Content(root, new Vector3(0, 1.045f, 0)) };
        }

        private static StationKitResult DinerTable(Transform root, StationOpts o)
        {
            var red = Mats.Std(new Color(0.78f, 0.14f, 0.16f), 0.45f);
            Props.Table(root, 1f, 0.75f, 0.76f, Mats.Std(new Color(0.95f, 0.94f, 0.9f), 0.35f), Mats.Metal());
            foreach (float sz in new[] { -0.62f, 0.62f })
            {
                Props.Box(root, new Vector3(1f, 0.45f, 0.45f), red, new Vector3(0, 0.225f, sz), default, 0.04f);
                Props.Box(root, new Vector3(1f, 0.55f, 0.12f), red, new Vector3(0, 0.72f, sz + (sz > 0 ? 0.2f : -0.2f)), default, 0.04f);
            }
            Props.Cyl(root, 0.03f, 0.03f, 0.12f, Mats.Std(new Color(0.9f, 0.9f, 0.9f), 0.3f), new Vector3(0.3f, 0.83f, 0.1f));
            Props.Cyl(root, 0.03f, 0.03f, 0.12f, Mats.Std(new Color(0.85f, 0.2f, 0.15f), 0.3f), new Vector3(0.36f, 0.83f, 0.1f));
            Label3D.Create(root, o.TableId.ToString(), 80f, new Color(0.15f, 0.15f, 0.15f), new Vector3(-0.3f, 0.9f, 0), true);
            return new StationKitResult { Size = new Vector3(1f, 0.8f, 0.8f), Center = new Vector3(0, 0.4f, 0), LabelH = 1.4f, Content = Content(root, new Vector3(0, 0.77f, 0)) };
        }

        private static StationKitResult Kalle(Transform root)
        {
            var go = Props.Node(root, "Kalle");
            var npc = go.AddComponent<NPC>();
            npc.Setup(new Look
            {
                Skin = new Color(0.93f, 0.75f, 0.6f), Hair = new Color(0.2f, 0.15f, 0.1f), Shirt = new Color(0.92f, 0.92f, 0.9f),
                Pants = new Color(0.2f, 0.2f, 0.22f), Apron = true, Mustache = true, ChefHat = true,
            });
            return new StationKitResult { Size = new Vector3(0.7f, 1.9f, 0.7f), Center = new Vector3(0, 0.95f, 0), LabelH = 2.55f, Npc = npc };
        }

        // ---- Feierabend ---------------------------------------------------------------------------------------
        private static StationKitResult EndDay(Transform root, int stage)
        {
            if (stage == 0)
            {
                Props.Box(root, new Vector3(1.4f, 0.22f, 2f), Mats.Std(new Color(0.9f, 0.9f, 0.88f), 0.9f), new Vector3(0, 0.11f, 0), default, 0.06f);
                Props.Box(root, new Vector3(1.42f, 0.06f, 1.3f), Mats.Std(new Color(0.25f, 0.35f, 0.6f), 0.9f), new Vector3(0, 0.25f, 0.32f), default, 0.03f);
                Props.Box(root, new Vector3(0.6f, 0.12f, 0.35f), Mats.Std(new Color(0.97f, 0.97f, 0.97f), 0.9f), new Vector3(0, 0.28f, -0.7f), default, 0.05f);
                Props.Box(root, new Vector3(0.16f, 0.1f, 0.07f), Mats.Std(new Color(0.8f, 0.15f, 0.15f), 0.4f), new Vector3(0.6f, 0.05f, -1.1f));
                Props.Box(root, new Vector3(0.1f, 0.04f, 0.01f), Mats.Emit(new Color(1f, 0.2f, 0.2f), 3f), new Vector3(0.6f, 0.06f, -1.064f), default, 0f, false);
                return new StationKitResult { Size = new Vector3(1.4f, 0.45f, 2f), Center = new Vector3(0, 0.22f, 0), LabelH = 0.9f };
            }
            Props.Box(root, new Vector3(0.12f, 1.25f, 0.12f), Mats.DarkMetal(), new Vector3(0, 0.625f, 0));
            Props.Box(root, new Vector3(0.45f, 0.55f, 0.25f), Mats.Std(new Color(0.85f, 0.83f, 0.78f), 0.5f), new Vector3(0, 1.45f, 0), default, 0.03f);
            Props.Box(root, new Vector3(0.3f, 0.12f, 0.01f), Mats.Emit(new Color(0.3f, 1f, 0.45f), 2f), new Vector3(0, 1.58f, 0.126f), default, 0f, false);
            Props.Box(root, new Vector3(0.12f, 0.02f, 0.05f), Mats.Std(new Color(0.15f, 0.15f, 0.15f)), new Vector3(0, 1.32f, 0.13f));
            return new StationKitResult { Size = new Vector3(0.5f, 1.75f, 0.4f), Center = new Vector3(0, 0.87f, 0), LabelH = 2.1f };
        }

        // ---- Verkaufsstand ------------------------------------------------------------------------------------
        private static StationKitResult Stand(Transform root)
        {
            Props.MarketStall(root, Game.Sim != null ? Game.Sim.BrandColor.ToColor() : new Color(0.9f, 0.26f, 0.26f));
            return new StationKitResult { Size = new Vector3(2.3f, 1f, 1f), Center = new Vector3(0, 0.5f, 0), LabelH = 2.8f, Content = Content(root, new Vector3(0, 0.96f, 0.05f)) };
        }
    
        // ---- v3.0: Retouren ---------------------------------------------------------------------------------
        public static readonly Color ReturnRed = new Color(0.86f, 0.16f, 0.14f);

        /// <summary>Kleines Regalfach "RETOUREN" neben dem Wareneingang.</summary>
        private static StationKitResult ReturnTray(Transform root)
        {
            var frame = Mats.Std(new Color(0.3f, 0.32f, 0.35f), 0.45f, 0.6f);
            var board = Mats.Std(new Color(0.62f, 0.64f, 0.66f), 0.4f, 0.7f);
            foreach (float sx in new[] { -0.46f, 0.46f })
            foreach (float sz in new[] { -0.22f, 0.22f })
                Props.Box(root, new Vector3(0.04f, 1.1f, 0.04f), frame, new Vector3(sx, 0.55f, sz));
            foreach (float y in new[] { 0.08f, 0.55f, 1.08f }) Props.Box(root, new Vector3(0.96f, 0.03f, 0.48f), board, new Vector3(0, y, 0));
            Props.Box(root, new Vector3(0.96f, 0.1f, 0.01f), Mats.Std(ReturnRed, 0.5f), new Vector3(0, 1.0f, 0.245f), default, 0f, false);
            var sign = Props.SignBoard(root, "RETOUREN", ReturnRed, Color.white, new Vector2(0.9f, 0.2f));
            sign.transform.localPosition = new Vector3(0, 1.38f, 0);
            return new StationKitResult { Size = new Vector3(0.96f, 1.2f, 0.5f), Center = new Vector3(0, 0.6f, 0), LabelH = 1.7f, Content = Content(root, Vector3.zero) };
        }

        /// <summary>Prüftisch am Retourenplatz (links): Retoure als B-Ware einlagern.</summary>
        private static StationKitResult ReturnDesk(Transform root)
        {
            Props.Table(root, 1.2f, 0.7f, 0.9f, Mats.Std(new Color(0.88f, 0.88f, 0.85f), 0.5f), Mats.DarkMetal());
            Props.Box(root, new Vector3(0.7f, 0.005f, 0.45f), Mats.Std(new Color(0.25f, 0.5f, 0.75f), 0.8f), new Vector3(-0.15f, 0.905f, 0.05f), default, 0f, false);
            // Lupe + Klemmbrett
            Props.Cyl(root, 0.06f, 0.06f, 0.012f, Mats.Glass(), new Vector3(0.35f, 0.93f, -0.1f));
            Props.Box(root, new Vector3(0.02f, 0.02f, 0.12f), Mats.DarkMetal(), new Vector3(0.35f, 0.93f, -0.02f));
            Props.Box(root, new Vector3(0.22f, 0.01f, 0.3f), Mats.Std(new Color(0.55f, 0.4f, 0.25f), 0.7f), new Vector3(0.38f, 0.91f, 0.15f));
            Props.Box(root, new Vector3(0.18f, 0.004f, 0.24f), Mats.Std(Color.white, 0.6f), new Vector3(0.38f, 0.918f, 0.16f), default, 0f, false);
            var sign = Props.SignBoard(root, "B-WARE", new Color(0.2f, 0.55f, 0.3f), Color.white, new Vector2(0.8f, 0.2f));
            sign.transform.localPosition = new Vector3(0, 1.5f, -0.3f);
            foreach (float sx in new[] { -0.38f, 0.38f }) Props.Box(root, new Vector3(0.03f, 0.6f, 0.03f), Mats.DarkMetal(), new Vector3(sx, 1.2f, -0.3f));
            return new StationKitResult { Size = new Vector3(1.2f, 1f, 0.7f), Center = new Vector3(0, 0.5f, 0), LabelH = 1.85f, Content = Content(root, new Vector3(-0.15f, 0.91f, 0.05f)) };
        }

        /// <summary>Roter Container am Retourenplatz (rechts): Retoure entsorgen.</summary>
        private static StationKitResult ReturnBin(Transform root)
        {
            var red = Mats.Std(ReturnRed, 0.5f, 0.3f);
            Props.Box(root, new Vector3(0.8f, 0.85f, 0.65f), red, new Vector3(0, 0.45f, 0), default, 0.03f);
            Props.Box(root, new Vector3(0.84f, 0.05f, 0.7f), Mats.Std(new Color(0.2f, 0.2f, 0.22f), 0.5f), new Vector3(0, 0.9f, -0.02f), new Vector3(-12, 0, 0), 0.01f);
            foreach (float sx in new[] { -0.3f, 0.3f })
                Props.Cyl(root, 0.05f, 0.05f, 0.04f, Mats.Std(new Color(0.1f, 0.1f, 0.1f)), new Vector3(sx, 0.05f, 0.22f), new Vector3(0, 0, 90));
            Label3D.Create(root, "ENTSORGEN", 34f, Color.white, new Vector3(0, 0.55f, 0.33f), false);
            return new StationKitResult { Size = new Vector3(0.8f, 0.95f, 0.65f), Center = new Vector3(0, 0.47f, 0), LabelH = 1.35f };
        }

        // ---- v3.0: Palettenplatz ------------------------------------------------------------------------------
        /// <summary>Anzahl Palettenplätze (je laufendem Großauftrag einer).</summary>
        public const int PalletSlots = 4;
        public const float PalletSpacing = 1.3f;

        public static Vector3 PalletSlotPos(int i) => new Vector3((i - (PalletSlots - 1) / 2f) * PalletSpacing, 0, 0);

        private static StationKitResult PalletPlace(Transform root)
        {
            var yellow = Mats.Std(new Color(0.95f, 0.78f, 0.1f), 0.6f);
            float w = PalletSlots * PalletSpacing + 0.2f;
            // Bodenmarkierung
            foreach (float sz in new[] { -0.6f, 0.6f }) Props.Box(root, new Vector3(w, 0.01f, 0.08f), yellow, new Vector3(0, 0.006f, sz), default, 0f, false);
            for (int i = 0; i <= PalletSlots; i++)
                Props.Box(root, new Vector3(0.08f, 0.01f, 1.2f), yellow, new Vector3(-w / 2f + 0.1f + i * PalletSpacing, 0.006f, 0), default, 0f, false);
            // Schild auf Pfosten
            Props.Box(root, new Vector3(0.06f, 2.1f, 0.06f), Mats.DarkMetal(), new Vector3(-w / 2f - 0.1f, 1.05f, -0.55f));
            var sign = Props.SignBoard(root, "B2B · SPEDITION", new Color(0.18f, 0.35f, 0.7f), Color.white, new Vector2(1.5f, 0.3f));
            sign.transform.localPosition = new Vector3(-w / 2f + 0.65f, 2.0f, -0.55f);
            return new StationKitResult { Size = new Vector3(w, 0.5f, 1.2f), Center = new Vector3(0, 0.25f, 0), LabelH = 2.5f, Content = Content(root, Vector3.zero) };
        }

        // ---- v3.0: Bestell-Monitor ---------------------------------------------------------------------------
        /// <summary>Wandmonitor. Ursprung = Bildschirmmitte; Content hält die Textzeilen.</summary>
        private static StationKitResult Monitor(Transform root, int stage)
        {
            float w = stage == 0 ? 1.3f : 2.4f, h = stage == 0 ? 0.78f : 1.35f;
            Props.Box(root, new Vector3(w + 0.08f, h + 0.08f, 0.06f), Mats.Std(new Color(0.06f, 0.06f, 0.07f), 0.3f), Vector3.zero, default, 0.015f);
            Props.Box(root, new Vector3(w, h, 0.01f), Mats.Emit(new Color(0.05f, 0.09f, 0.16f), 1f), new Vector3(0, 0, 0.032f), default, 0f, false);
            if (stage != 0)
                foreach (float sx in new[] { -w * 0.35f, w * 0.35f })
                    Props.Box(root, new Vector3(0.02f, 3f, 0.02f), Mats.DarkMetal(), new Vector3(sx, h / 2f + 1.5f, 0), default, 0f, false);
            var content = Content(root, new Vector3(0, 0, 0.045f));
            return new StationKitResult { Size = new Vector3(w, h, 0.12f), Center = Vector3.zero, LabelH = h / 2f + 0.3f, Content = content };
        }

        // ---- v3.0: Zonen-Schild ------------------------------------------------------------------------------
        private static StationKitResult ZoneSign(Transform root, StationOpts o)
        {
            string text = string.IsNullOrEmpty(o.Text) ? "ZONE" : o.Text;
            float big = o.Stage == 0 ? 1f : 1.8f;
            var bg = o.Color;
            var fg = bg.grayscale > 0.55f ? new Color(0.08f, 0.08f, 0.08f) : Color.white;
            var sign = Props.SignBoard(root, text, bg, fg, new Vector2(0.24f * big * Mathf.Max(4, text.Length) * 0.62f, 0.3f * big), SignFont.Condensed);
            sign.transform.localPosition = Vector3.zero;
            if (o.Stage != 0)
            {
                float half = 0.24f * big * Mathf.Max(4, text.Length) * 0.62f * 0.4f;
                foreach (float sx in new[] { -half, half })
                    Props.Box(root, new Vector3(0.02f, 3f, 0.02f), Mats.DarkMetal(), new Vector3(sx, 0.15f * big + 1.5f, 0), default, 0f, false);
            }
            return new StationKitResult { Size = Vector3.zero, Center = Vector3.zero, LabelH = 0f };
        }
    }
}
