using System.Collections.Generic;
using System.Text;
using DropshippingGame.Core;
using DropshippingGame.UI;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Finanzviertel (Nordseite der Hauptstraße, x 60..110, z 8..45): Kiezbank (begehbar, Schalter mit
    /// Bankberater, Geldautomat draußen) und Börse (Parkett mit LED-Kurswand, Laufband, hektischen
    /// Maklern und Handelsterminal). Alles prozedural. Regeln: Core/Market.Exchange.cs.
    /// Fragt den Sim-Zustand ab (kein Abo), damit ein neuer Spielstand nichts kaputt macht.
    /// </summary>
    public sealed class FinanceDistrict : MonoBehaviour
    {
        // Bank: x 62..80, z 12..28 (Tür bei x 71). Börse: x 84..108, z 12..36 (Tür bei x 96).
        private const float BankX0 = 62f, BankX1 = 80f, BankZ0 = 12f, BankZ1 = 28f, BankH = 4.6f;
        private const float ExX0 = 84f, ExX1 = 108f, ExZ0 = 12f, ExZ1 = 36f, ExH = 7f;
        public static readonly Vector3 Entrance = new Vector3(85f, 0.05f, 9.5f);

        private sealed class Broker
        {
            public NPC Npc;
            public float SayT, MoveT, Hop;
            public Transform Visual;
            public Vector3 VisualBase;
        }

        private Transform _root, _static;
        private readonly List<Broker> _brokers = new List<Broker>();
        private readonly List<Label3D[]> _boardRows = new List<Label3D[]>();
        private readonly List<string> _boardIds = new List<string>();
        private Label3D _ticker, _tickerOut, _newsLine, _frenzyLabel;
        private NPC _banker;
        private System.Random _rng = new System.Random(6060);
        private float _boardT, _tickT, _bankerT;
        private int _tickPos;
        private string _tickText = "";
        private bool _menu;

        public void Setup(bool menuMode)
        {
            _menu = menuMode;
            _root = Props.Node(transform, "Finanzviertel").transform;
            _static = Props.Node(_root, "Static").transform;
            try
            {
                Plaza();
                Bank();
                Exchange();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Finanzviertel unvollständig: " + e.Message);
            }
            try
            {
                var batch = new List<GameObject>();
                foreach (var mf in _static.GetComponentsInChildren<MeshFilter>(true))
                    if (mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable) batch.Add(mf.gameObject);
                if (batch.Count > 0) StaticBatchingUtility.Combine(batch.ToArray(), _static.gameObject);
            }
            catch (System.Exception) { }
        }

        private float Rand(float a, float b) => a + (b - a) * (float)_rng.NextDouble();

        // =====================================================================================
        // Bauen
        // =====================================================================================
        /// <summary>Wand entlang X (z fest) oder Z (x fest) mit Kollision; Tür (von, bis, Höhe) optional.</summary>
        private void WallX(float x0, float x1, float z, float h, Material m, float doorFrom = 0f, float doorTo = 0f, float doorH = 0f)
        {
            if (doorTo > doorFrom)
            {
                SolidX(x0, doorFrom, z, 0f, h, m);
                SolidX(doorTo, x1, z, 0f, h, m);
                SolidX(doorFrom, doorTo, z, doorH, h, m);
            }
            else SolidX(x0, x1, z, 0f, h, m);
        }

        private void SolidX(float x0, float x1, float z, float y0, float y1, Material m)
        {
            if (x1 - x0 < 0.01f || y1 - y0 < 0.01f) return;
            Props.Solid(_static, new Vector3(x1 - x0, y1 - y0, 0.3f), m, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, z));
        }

        private void WallZ(float z0, float z1, float x, float h, Material m)
        {
            Props.Solid(_static, new Vector3(0.3f, h, z1 - z0), m, new Vector3(x, h / 2f, (z0 + z1) / 2f));
        }

        private void Plaza()
        {
            var paving = Mats.Sidewalk(new Color(0.74f, 0.72f, 0.68f), new Color(0.6f, 0.58f, 0.55f));
            Props.Box(_static, new Vector3(50f, 0.03f, 5f), paving, new Vector3(85f, 0.015f, 9.5f), default, 0f, false);
            // Goldener Bulle (aus Kisten, versteht sich)
            var gold = Mats.Std(new Color(0.95f, 0.75f, 0.25f), 0.3f, 0.9f);
            var bull = Props.Node(_root, "GoldenerBulle", new Vector3(82f, 0, 9.6f), -90f).transform;
            Props.Box(bull, new Vector3(0.6f, 0.2f, 1.6f), Mats.Std(new Color(0.3f, 0.3f, 0.32f), 0.6f), new Vector3(0, 0.1f, 0));
            Props.Box(bull, new Vector3(0.5f, 0.45f, 1.1f), gold, new Vector3(0, 0.6f, 0));
            Props.Box(bull, new Vector3(0.36f, 0.34f, 0.36f), gold, new Vector3(0, 0.75f, 0.68f));
            Props.Box(bull, new Vector3(0.5f, 0.06f, 0.06f), gold, new Vector3(0, 0.95f, 0.75f), new Vector3(0, 0, 20f));
            foreach (float x in new[] { -0.17f, 0.17f })
            foreach (float z in new[] { -0.4f, 0.4f })
                Props.Box(bull, new Vector3(0.1f, 0.38f, 0.1f), gold, new Vector3(x, 0.38f, z));
            Props.Collider(bull, new Vector3(0.7f, 1.2f, 1.7f), new Vector3(0, 0.6f, 0));
            Label3D.Create(bull, "DER GOLDENE BULLE\n(Anfassen bringt Rendite. Nicht.)", 22f, new Color(1f, 0.9f, 0.6f), new Vector3(0, 1.45f, 0), true, 10f);
            NpcNav.AddPoint(transform.TransformPoint(new Vector3(82f, 0, 9.6f)));
            var sign = Props.SignBoard(_root, "FINANZVIERTEL", new Color(0.12f, 0.16f, 0.28f), new Color(1f, 0.85f, 0.4f), new Vector2(3.4f, 0.6f));
            sign.transform.localPosition = new Vector3(61f, 2.6f, 8.2f);
            sign.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Props.Box(_static, new Vector3(0.12f, 2.4f, 0.12f), Mats.DarkMetal(), new Vector3(61f, 1.2f, 8.2f));
        }

        private void Bank()
        {
            var wall = Mats.Plaster(new Color(0.86f, 0.84f, 0.78f));
            var trim = Mats.Std(new Color(0.15f, 0.25f, 0.45f), 0.5f);
            float door0 = 69.8f, door1 = 72.2f;
            WallX(BankX0, BankX1, BankZ0, BankH, wall, door0, door1, 2.6f);
            WallX(BankX0, BankX1, BankZ1, BankH, wall);
            WallZ(BankZ0, BankZ1, BankX0, BankH, wall);
            WallZ(BankZ0, BankZ1, BankX1, BankH, wall);
            Props.Box(_static, new Vector3(BankX1 - BankX0 + 0.6f, 0.4f, BankZ1 - BankZ0 + 0.6f), Mats.RoofMat(new Color(0.3f, 0.3f, 0.33f)),
                new Vector3((BankX0 + BankX1) / 2f, BankH + 0.2f, (BankZ0 + BankZ1) / 2f));
            Props.Box(_static, new Vector3(BankX1 - BankX0, 0.03f, BankZ1 - BankZ0), Mats.Tiles(0.6f), new Vector3((BankX0 + BankX1) / 2f, 0.02f, (BankZ0 + BankZ1) / 2f), default, 0f, false);
            Props.Box(_static, new Vector3(BankX1 - BankX0 + 0.4f, 0.5f, 0.1f), trim, new Vector3((BankX0 + BankX1) / 2f, BankH - 0.6f, BankZ0 - 0.2f));
            var name = Label3D.Create(_root, "KIEZBANK", 150f, new Color(1f, 0.9f, 0.55f), new Vector3(71f, BankH - 0.6f, BankZ0 - 0.27f), false, 0f, false, 1.6f);
            name.transform.localRotation = Quaternion.identity;
            Label3D.Create(_root, "Seit 2019. Gefühlt.", 40f, Color.white, new Vector3(71f, 3.1f, BankZ0 - 0.2f), false).transform.localRotation = Quaternion.identity;
            // Fenster (Glasstreifen außen)
            foreach (float x in new[] { 65f, 77f })
                Props.Box(_static, new Vector3(3f, 1.6f, 0.05f), Mats.Window(true), new Vector3(x, 1.9f, BankZ0 - 0.17f), default, 0f, false);
            // Licht innen
            for (int i = 0; i < 3; i++)
            {
                var cl = Props.CeilingLight(_root, 4f);
                cl.transform.localPosition = new Vector3(66f + i * 5f, BankH - 0.1f, 20f);
            }
            Props.PointLight(_root, new Vector3(71f, BankH - 0.6f, 17f), new Color(1f, 0.95f, 0.85f), 2.2f, 12f);
            Props.PointLight(_root, new Vector3(71f, BankH - 0.6f, 24f), new Color(1f, 0.95f, 0.85f), 2.2f, 12f);
            // Schalter
            var wood = Mats.Std(new Color(0.45f, 0.3f, 0.18f), 0.6f);
            var counter = Props.Node(_root, "Bankschalter", new Vector3(71f, 0, 23f)).transform;
            Props.Box(counter, new Vector3(8f, 1.1f, 0.8f), wood, new Vector3(0, 0.55f, 0));
            Props.Box(counter, new Vector3(8.2f, 0.06f, 1f), Mats.Std(new Color(0.9f, 0.88f, 0.84f), 0.3f), new Vector3(0, 1.13f, 0));
            Props.Box(counter, new Vector3(8f, 0.9f, 0.04f), Mats.Glass(), new Vector3(0, 1.6f, 0.1f), default, 0f, false);
            Props.Collider(counter, new Vector3(8f, 2.1f, 0.9f), new Vector3(0, 1.05f, 0));
            counter.gameObject.AddComponent<FinanceInteract>().Setup(FinanceInteract.Kind.Bank, "Bankschalter");
            Label3D.Create(counter, "SCHALTER 1 VON 1", 40f, new Color(0.2f, 0.3f, 0.6f), new Vector3(0, 2.3f, 0.2f), true, 12f);
            // Bankberater
            if (!_menu)
            {
                var go = Props.Node(_root, "Bankberater", new Vector3(71f, 0, 24.3f), 180f);
                try
                {
                    _banker = go.AddComponent<NPC>();
                    var look = CharacterKit.RandomLook(_rng);
                    look.Shirt = new Color(0.2f, 0.25f, 0.4f);
                    look.Vest = true;
                    _banker.Setup(look, null, 1f);
                    go.transform.localRotation = Quaternion.Euler(0, 180, 0);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Bankberater fehlt: " + e.Message);
                }
            }
            // Wartebereich, Pflanze, Absperrband
            var bench = Props.Bench(_root);
            bench.transform.localPosition = new Vector3(64f, 0, 16f);
            bench.transform.localRotation = Quaternion.Euler(0, 90, 0);
            var plant = Props.Plant(_root, 1.2f);
            plant.transform.localPosition = new Vector3(78.8f, 0, 13.2f);
            var metal = Mats.Metal();
            for (int i = 0; i < 4; i++) Props.Cyl(_root, 0.04f, 0.06f, 1f, metal, new Vector3(68.5f + i * 1.4f, 0.5f, 20.5f));
            Props.Box(_root, new Vector3(4.2f, 0.06f, 0.03f), Mats.Std(new Color(0.7f, 0.1f, 0.15f), 0.6f), new Vector3(70.6f, 0.9f, 20.5f), default, 0f, false);
            var poster = Props.Poster(_root, "Sparen ist das neue Ausgeben!", new Color(0.2f, 0.5f, 0.85f));
            poster.transform.localPosition = new Vector3(BankX0 + 0.2f, 1.8f, 22f);
            poster.transform.localRotation = Quaternion.Euler(0, 90, 0);
            // Geldautomat draußen links der Tür
            var atm = Props.Node(_root, "Geldautomat", new Vector3(66.5f, 0, BankZ0 - 0.5f), 180f).transform;
            Props.Box(atm, new Vector3(0.9f, 1.8f, 0.6f), Mats.Std(new Color(0.2f, 0.3f, 0.5f), 0.4f, 0.3f), new Vector3(0, 0.9f, 0));
            Props.Box(atm, new Vector3(0.5f, 0.35f, 0.02f), Mats.Emit(new Color(0.3f, 0.8f, 1f), 1.2f), new Vector3(0, 1.35f, 0.31f), default, 0f, false);
            Props.Box(atm, new Vector3(0.5f, 0.2f, 0.1f), Mats.DarkMetal(), new Vector3(0, 1.0f, 0.33f), new Vector3(30, 0, 0));
            Label3D.Create(atm, "GELD", 50f, Color.white, new Vector3(0, 1.68f, 0.31f), false).transform.localRotation = Quaternion.Euler(0, 180, 0);
            Props.Collider(atm, new Vector3(0.9f, 1.8f, 0.6f), new Vector3(0, 0.9f, 0));
            atm.gameObject.AddComponent<FinanceInteract>().Setup(FinanceInteract.Kind.Atm, "Geldautomat");
        }

        private void Exchange()
        {
            var stone = Mats.Plaster(new Color(0.9f, 0.88f, 0.82f));
            float door0 = 94.6f, door1 = 97.4f;
            WallX(ExX0, ExX1, ExZ0, ExH, stone, door0, door1, 3.2f);
            WallX(ExX0, ExX1, ExZ1, ExH, stone);
            WallZ(ExZ0, ExZ1, ExX0, ExH, stone);
            WallZ(ExZ0, ExZ1, ExX1, ExH, stone);
            Props.Box(_static, new Vector3(ExX1 - ExX0 + 0.6f, 0.5f, ExZ1 - ExZ0 + 0.6f), Mats.RoofMat(new Color(0.35f, 0.33f, 0.3f)),
                new Vector3((ExX0 + ExX1) / 2f, ExH + 0.25f, (ExZ0 + ExZ1) / 2f));
            Props.Box(_static, new Vector3(ExX1 - ExX0, 0.03f, ExZ1 - ExZ0), Mats.WoodFloor(new Color(0.5f, 0.35f, 0.22f)), new Vector3((ExX0 + ExX1) / 2f, 0.02f, (ExZ0 + ExZ1) / 2f), default, 0f, false);
            // Säulen-Front + Giebel
            var col = Mats.Std(new Color(0.95f, 0.94f, 0.9f), 0.7f);
            foreach (float x in new[] { 86f, 89.5f, 93f, 99f, 102.5f, 106f })
            {
                Props.Cyl(_static, 0.35f, 0.4f, ExH - 0.6f, col, new Vector3(x, (ExH - 0.6f) / 2f, ExZ0 - 1.4f));
                Props.Collider(_static, new Vector3(0.7f, 2f, 0.7f), new Vector3(x, 1f, ExZ0 - 1.4f));
            }
            Props.Box(_static, new Vector3(ExX1 - ExX0 + 0.4f, 0.8f, 2f), col, new Vector3(96f, ExH - 0.2f, ExZ0 - 1.1f));
            Props.Box(_static, new Vector3(ExX1 - ExX0 + 0.8f, 0.2f, 2.4f), col, new Vector3(96f, 0.1f, ExZ0 - 1.2f));
            var title = Label3D.Create(_root, "BÖRSE", 220f, new Color(0.15f, 0.2f, 0.35f), new Vector3(96f, ExH - 0.2f, ExZ0 - 2.12f), false, 0f, false, 1f);
            title.transform.localRotation = Quaternion.identity;
            // Laufband außen über der Tür
            Props.Box(_static, new Vector3(9f, 0.5f, 0.1f), Mats.Std(new Color(0.03f, 0.03f, 0.04f), 0.4f), new Vector3(96f, 4.2f, ExZ0 - 0.2f));
            _tickerOut = Label3D.Create(_root, "", 55f, new Color(0.4f, 1f, 0.5f), new Vector3(96f, 4.2f, ExZ0 - 0.27f), false, 0f, false, 2.5f);
            _tickerOut.transform.localRotation = Quaternion.identity;
            // Innenlicht
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 2; j++)
                Props.PointLight(_root, new Vector3(89f + i * 7f, ExH - 1f, 18f + j * 10f), new Color(1f, 0.96f, 0.88f), 2.6f, 14f);
            // LED-Kurswand an der Rückwand (lesbar von -Z => Label ohne Drehung zeigt zur +Z-Seite des Parents)
            var wallNode = Props.Node(_root, "Kurswand", new Vector3(96f, 0, ExZ1 - 0.2f), 180f).transform;
            Props.Box(wallNode, new Vector3(20f, 4.4f, 0.12f), Mats.Std(new Color(0.02f, 0.02f, 0.03f), 0.3f), new Vector3(0, 4.3f, 0));
            Label3D.Create(wallNode, "KURSE LIVE", 90f, new Color(1f, 0.8f, 0.3f), new Vector3(0, 6.15f, 0.08f), false, 0f, false, 2.5f);
            var all = Market.AllAssets;
            int perCol = (all.Length + 1) / 2;
            for (int i = 0; i < all.Length; i++)
            {
                int c = i / perCol, r = i % perCol;
                float x0 = -9f + c * 10f;
                float y = 5.6f - r * 0.48f;
                var a = Label3D.Create(wallNode, all[i].Id, 60f, new Color(1f, 0.85f, 0.4f), new Vector3(x0 + 1.2f, y, 0.08f), false, 0f, false, 2.2f);
                var p = Label3D.Create(wallNode, "", 60f, Color.white, new Vector3(x0 + 4.3f, y, 0.08f), false, 0f, false, 2.2f);
                var ch = Label3D.Create(wallNode, "", 60f, Color.green, new Vector3(x0 + 7.6f, y, 0.08f), false, 0f, false, 2.4f);
                _boardRows.Add(new[] { a, p, ch });
                _boardIds.Add(all[i].Id);
            }
            _newsLine = Label3D.Create(wallNode, "", 46f, new Color(1f, 1f, 1f), new Vector3(0, 2.45f, 0.08f), false, 0f, false, 2f, 70);
            _frenzyLabel = Label3D.Create(wallNode, "", 44f, new Color(1f, 0.5f, 0.3f), new Vector3(7.5f, 6.15f, 0.08f), false, 0f, false, 2f);
            // Laufband innen über der Wand
            Props.Box(wallNode, new Vector3(20f, 0.5f, 0.1f), Mats.Std(new Color(0.03f, 0.03f, 0.04f), 0.4f), new Vector3(0, 1.75f, 0.02f));
            _ticker = Label3D.Create(wallNode, "", 60f, new Color(0.4f, 1f, 0.5f), new Vector3(0, 1.75f, 0.09f), false, 0f, false, 2.5f);
            // Händlertische (Parkett)
            var desk = Mats.Std(new Color(0.3f, 0.3f, 0.34f), 0.5f);
            foreach (var pos in new[] { new Vector3(90f, 0, 28f), new Vector3(102f, 0, 28f), new Vector3(90f, 0, 21f), new Vector3(102f, 0, 21f) })
            {
                var d = Props.Node(_root, "Händlertisch", pos).transform;
                Props.Box(d, new Vector3(2.6f, 0.9f, 1.2f), desk, new Vector3(0, 0.45f, 0));
                for (int k = 0; k < 3; k++)
                    Props.Box(d, new Vector3(0.6f, 0.4f, 0.04f), Mats.Emit(k % 2 == 0 ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.95f, 0.3f, 0.25f), 1.1f),
                        new Vector3(-0.8f + k * 0.8f, 1.15f, 0f), new Vector3(-10, 0, 0), 0f, false);
                Props.Collider(d, new Vector3(2.6f, 1.2f, 1.2f), new Vector3(0, 0.6f, 0));
            }
            // Handelsterminal beim Eingang
            var term = Props.Node(_root, "Handelsterminal", new Vector3(89f, 0, 15f), 180f).transform;
            Props.Box(term, new Vector3(0.8f, 1.1f, 0.5f), Mats.DarkMetal(), new Vector3(0, 0.55f, 0));
            Props.Box(term, new Vector3(0.75f, 0.5f, 0.05f), Mats.Emit(new Color(0.25f, 0.6f, 1f), 1.4f), new Vector3(0, 1.4f, 0.05f), new Vector3(-15, 0, 0), 0f, false);
            Props.Collider(term, new Vector3(0.8f, 1.7f, 0.6f), new Vector3(0, 0.85f, 0));
            Label3D.Create(term, "HANDELN", 40f, new Color(0.6f, 0.85f, 1f), new Vector3(0, 1.95f, 0), true, 10f);
            term.gameObject.AddComponent<FinanceInteract>().Setup(FinanceInteract.Kind.Terminal, "Handelsterminal");
            // Zweites Terminal hinten rechts
            var term2 = Props.Node(_root, "Handelsterminal2", new Vector3(104f, 0, 15f), 180f).transform;
            Props.Box(term2, new Vector3(0.8f, 1.1f, 0.5f), Mats.DarkMetal(), new Vector3(0, 0.55f, 0));
            Props.Box(term2, new Vector3(0.75f, 0.5f, 0.05f), Mats.Emit(new Color(0.25f, 0.6f, 1f), 1.4f), new Vector3(0, 1.4f, 0.05f), new Vector3(-15, 0, 0), 0f, false);
            Props.Collider(term2, new Vector3(0.8f, 1.7f, 0.6f), new Vector3(0, 0.85f, 0));
            term2.gameObject.AddComponent<FinanceInteract>().Setup(FinanceInteract.Kind.Terminal, "Handelsterminal");
            if (!_menu) SpawnBrokers();
        }

        private void SpawnBrokers()
        {
            Color[] shirts = { new Color(0.95f, 0.95f, 0.95f), new Color(0.7f, 0.8f, 0.95f), new Color(0.95f, 0.85f, 0.5f) };
            for (int i = 0; i < 7; i++)
            {
                var start = RandomFloorPoint();
                var go = Props.Node(_root, "Makler", start);
                try
                {
                    var npc = go.AddComponent<NPC>();
                    var look = CharacterKit.RandomLook(_rng);
                    look.Shirt = shirts[i % shirts.Length];
                    look.Vest = i % 2 == 0;
                    npc.Setup(look, null, 1.6f);
                    go.transform.localPosition = start;
                    var b = new Broker { Npc = npc, SayT = Rand(0.5f, 4f), MoveT = Rand(0.2f, 2f) };
                    b.Visual = npc.Model != null ? npc.Model.transform : (npc.Rig != null && npc.Rig.Root != null ? npc.Rig.Root : null);
                    if (b.Visual != null) b.VisualBase = b.Visual.localPosition;
                    _brokers.Add(b);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Makler konnte nicht gebaut werden: " + e.Message);
                    Destroy(go);
                }
            }
        }

        private Vector3 RandomFloorPoint()
        {
            // Parkett ohne Tische: Gänge zwischen den Tischen
            float x = Rand(86.5f, 105.5f), z = Rand(16.5f, 33f);
            if (Mathf.Abs(z - 28f) < 1.2f || Mathf.Abs(z - 21f) < 1.2f) z += 1.8f;
            return new Vector3(x, 0, z);
        }

        // =====================================================================================
        // Laufzeit
        // =====================================================================================
        private void Update()
        {
            var sim = Game.Sim;
            if (sim == null || _root == null) return;
            float dt = Time.deltaTime;
            var m = sim.Market;
            _boardT -= dt;
            if (_boardT <= 0f)
            {
                _boardT = 0.5f;
                UpdateBoard(m);
            }
            _tickT -= dt;
            if (_tickT <= 0f)
            {
                _tickT = 0.14f;
                ScrollTicker();
            }
            UpdateBrokers(m, dt);
            if (_banker != null && Game.Player != null)
            {
                _banker.LookAtTarget = Game.Player.transform;
                _bankerT -= dt;
                if (_bankerT <= 0f)
                {
                    _bankerT = Rand(9f, 16f);
                    if ((Game.Player.transform.position - _banker.transform.position).sqrMagnitude < 64f)
                        _banker.Say(FinanceText.BankerLines[_rng.Next(FinanceText.BankerLines.Length)], 4f);
                }
            }
        }

        private void UpdateBoard(Market m)
        {
            for (int i = 0; i < _boardRows.Count; i++)
            {
                string id = _boardIds[i];
                if (!m.Prices.ContainsKey(id)) continue;
                float c = m.ChangePct(id);
                var row = _boardRows[i];
                row[1].SetText(Price(m.Prices[id]));
                row[2].SetText((c >= 0 ? "+" : "-") + Fmt.Dec(Mathf.Abs(c * 100f), 1) + "%");
                row[2].SetColor(c >= 0 ? new Color(0.35f, 1f, 0.45f) : new Color(1f, 0.35f, 0.3f));
            }
            if (_newsLine != null) _newsLine.SetText(m.News.Count > 0 ? "EILMELDUNG: " + m.News[0].Text : "Keine Nachrichten. Verdächtig ruhig.");
            if (_frenzyLabel != null) _frenzyLabel.SetText("HEKTIK " + Mathf.RoundToInt(m.Frenzy * 100f) + "%");
            // Laufband-Text neu zusammensetzen
            var sb = new StringBuilder();
            foreach (var a in Market.AllAssets)
            {
                float c = m.ChangePct(a.Id);
                sb.Append(a.Id).Append(' ').Append(Price(m.Prices[a.Id])).Append(c >= 0 ? " +" : " -").Append(Fmt.Dec(Mathf.Abs(c * 100f), 1)).Append("%   ");
            }
            if (m.News.Count > 0) sb.Append("+++ ").Append(m.News[0].Text).Append(" +++   ");
            _tickText = sb.ToString();
        }

        private static string Price(float p) => p >= 100f ? Fmt.Dec(p, 1) : Fmt.Dec(p, p < 1f ? 3 : 2);

        private void ScrollTicker()
        {
            if (string.IsNullOrEmpty(_tickText)) return;
            const int window = 46;
            string loop = _tickText + _tickText;
            _tickPos = (_tickPos + 1) % _tickText.Length;
            string s = loop.Substring(_tickPos, Mathf.Min(window, loop.Length - _tickPos));
            if (_ticker != null) _ticker.SetText(s);
            if (_tickerOut != null) _tickerOut.SetText(s.Length > 30 ? s.Substring(0, 30) : s);
        }

        private void UpdateBrokers(Market m, float dt)
        {
            float f = Mathf.Clamp01(m.Frenzy);
            foreach (var b in _brokers)
            {
                if (b.Npc == null) continue;
                b.Npc.Speed = Mathf.Lerp(1.3f, 3.6f, f);
                b.MoveT -= dt;
                if (b.MoveT <= 0f || b.Npc.IsAt(b.Npc.transform.localPosition, 0f) && !b.Npc.IsWalking && b.MoveT < 0.5f)
                {
                    b.MoveT = Mathf.Lerp(Rand(3f, 6f), Rand(0.8f, 2f), f);
                    b.Npc.HoldAt(RandomFloorPoint());
                }
                b.SayT -= dt;
                if (b.SayT <= 0f)
                {
                    b.SayT = Mathf.Lerp(Rand(3.5f, 7f), Rand(0.8f, 1.8f), f);
                    b.Npc.Say(BrokerLine(m, f), Mathf.Lerp(2.6f, 1.4f, f));
                    b.Hop = 0.6f;
                }
                // Gestikulieren: Hüpfen + Wackeln, stärker bei Hektik
                if (b.Visual != null)
                {
                    b.Hop = Mathf.Max(0f, b.Hop - dt);
                    float t = Time.time * (6f + f * 10f) + b.GetHashCode() % 10;
                    float amp = (b.Hop > 0f ? 0.12f : 0f) + f * 0.05f;
                    b.Visual.localPosition = b.VisualBase + new Vector3(0, Mathf.Abs(Mathf.Sin(t)) * amp, 0);
                    b.Visual.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 0.7f) * 15f * (f + (b.Hop > 0f ? 0.6f : 0f)), Mathf.Sin(t) * 6f * f);
                }
            }
        }

        private string BrokerLine(Market m, float frenzy)
        {
            if (_rng.NextDouble() < 0.3)
            {
                var mover = m.BiggestMover(15, out float ch);
                if (mover != null && Mathf.Abs(ch) > 0.01f)
                    return string.Format(ch > 0 ? FinanceText.BrokerUp : FinanceText.BrokerDown, mover.Id);
            }
            if (frenzy > 0.55f && _rng.NextDouble() < frenzy) return FinanceText.BrokerPanic[_rng.Next(FinanceText.BrokerPanic.Length)];
            return FinanceText.BrokerBabble[_rng.Next(FinanceText.BrokerBabble.Length)];
        }
    }

    /// <summary>Interaktionspunkt im Finanzviertel: Bankschalter, Geldautomat oder Handelsterminal.</summary>
    public sealed class FinanceInteract : MonoBehaviour, IInteractable
    {
        public enum Kind { Bank, Atm, Terminal }

        private Kind _kind;
        private string _title = "";
        private readonly Highlighter _hl = new Highlighter();

        public void Setup(Kind kind, string title)
        {
            _kind = kind;
            _title = title;
            _hl.Collect(transform);
        }

        public string Title => _title;

        public string Prompt(PlayerController player)
        {
            var sim = Game.Sim;
            if (sim == null) return "";
            switch (_kind)
            {
                case Kind.Bank: return "Mit dem Bankberater sprechen (Konten, Kredit, Sparen)";
                case Kind.Atm: return "Geldautomat · Konto " + Fmt.Money(sim.Money);
                default: return "Handeln: Aktien, ETFs, Krypto · Depot " + Fmt.Money(sim.Market.PortfolioValue());
            }
        }

        public void Interact(PlayerController player)
        {
            if (Game.Sim == null) return;
            if (Game.Sim.StoryStage != "business")
            {
                Game.Notify("Geschlossen. Erst mal Geld verdienen.", "info");
                return;
            }
            Game.Sound("click");
            if (_kind == Kind.Terminal) FinanceUI.OpenExchange();
            else FinanceUI.OpenBank(_kind == Kind.Atm);
        }

        public void SetHighlighted(bool on) => _hl.Set(on);
    }
}
