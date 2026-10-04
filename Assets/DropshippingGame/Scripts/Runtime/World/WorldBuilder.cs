using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Baut die komplette Spielwelt zur Laufzeit: Straße, Kalles Imbiss, Garage, Lagerhalle,
    /// Park, Skyline, Laternen, Verkehr, Passanten. Verwaltet außerdem Stationen je Ausbaustufe,
    /// Deko/Lifestyle-Objekte, Personal-Figuren, Lieferwagen, Förderband und den Verkaufsstand.
    /// Koordinaten wie im Godot-Original (die Welt ist dadurch gespiegelt - spielt sich identisch).
    /// </summary>
    public sealed class WorldBuilder : MonoBehaviour
    {
        public static readonly Rect DinerRect = new Rect(-44, -19, 16, 12);
        public static readonly Rect GarageRect = new Rect(-22, -17, 12, 10);
        public static readonly Rect WarehouseRect = new Rect(-2, -31, 36, 24);

        public static readonly Vector3 SpawnDiner = new Vector3(-35.2f, 0.05f, -9.2f);
        public static readonly Vector3 SpawnGarage = new Vector3(-16f, 0.05f, -8.2f);
        public static readonly Vector3 SpawnWarehouse = new Vector3(17f, 0.05f, -9f);
        public static readonly Vector3[] VanStop = { new Vector3(-16f, 0, -2.2f), new Vector3(26.5f, 0, -2.2f) };
        public static readonly Vector3 BeltStart = new Vector3(21.8f, 0.9f, -15.5f);
        public static readonly Vector3 BeltEnd = new Vector3(7.2f, 0.9f, -15.5f);
        public static readonly Vector3 StandPos = new Vector3(-21.2f, 0, -5.55f);

        private static readonly (int id, Vector3 pos)[] DinerTables =
        {
            (1, new Vector3(-42f, 0, -9.8f)), (2, new Vector3(-38.8f, 0, -9.8f)), (3, new Vector3(-31.3f, 0, -9.8f)), (4, new Vector3(-31.3f, 0, -12.6f)),
        };

        /// <summary>Deko/Lifestyle-Plätze je Standort: (Stufe, Position, Drehung). Stufe -1 = draußen, immer sichtbar.</summary>
        private static readonly Dictionary<string, (int stage, Vector3 pos, float rot)[]> DecorSlots = new Dictionary<string, (int, Vector3, float)[]>
        {
            { "pflanze", new[] { (0, new Vector3(-21.4f, 0, -16.4f), 0f), (1, new Vector3(-1.4f, 0, -15.5f), 0f) } },
            { "poster", new[] { (0, new Vector3(-21.83f, 1.8f, -14.6f), 90f), (1, new Vector3(-1.8f, 1.8f, -11.5f), 90f) } },
            { "stehlampe", new[] { (0, new Vector3(-13.6f, 0, -16.4f), 0f), (1, new Vector3(4.3f, 0, -15.4f), 0f) } },
            { "teppich", new[] { (0, new Vector3(-16f, 0, -11.2f), 0f), (1, new Vector3(1.8f, 0, -10.2f), 0f) } },
            { "billy", new[] { (0, new Vector3(-21.62f, 0, -10.8f), 90f), (1, new Vector3(-1.62f, 0, -8.6f), 90f) } },
            { "whiteboard", new[] { (0, new Vector3(-14.4f, 1.6f, -16.83f), 0f), (1, new Vector3(1.6f, 1.8f, -15.84f), 0f) } },
            { "kaffee", new[] { (0, new Vector3(-16.3f, 0, -16.45f), 0f), (1, new Vector3(33.3f, 0, -22f), -90f) } },
            { "sofa", new[] { (1, new Vector3(30f, 0, -13.2f), 180f) } },
            { "sneaker", new[] { (0, new Vector3(-18.62f, 0, -16.5f), 0f), (1, new Vector3(-1.4f, 0, -12.6f), 90f) } },
            { "gamingstuhl", new[] { (0, new Vector3(-19.9f, 0, -15.5f), 180f) } },
            { "neon", new[] { (0, new Vector3(-12.9f, 2.4f, -16.83f), 0f), (1, new Vector3(15.3f, 5.8f, -30.75f), 0f) } },
            { "auto", new[] { (-1, new Vector3(-6f, 0, -10.5f), 90f) } },
            { "sportwagen", new[] { (-1, new Vector3(-6f, 0, -16f), 90f) } },
        };

        public Atmosphere Atmos;
        public bool MenuMode;
        private Transform _static, _stations, _decor, _dynamic, _customers, _staff, _belt;
        private readonly List<Label3D> _brandLabels = new List<Label3D>();
        private readonly List<NPC> _pedestrians = new List<NPC>();
        private readonly Dictionary<int, GameObject> _beltBoxes = new Dictionary<int, GameObject>();
        private Transform _gate;
        private Collider _gateCol;
        private GameObject _saleSign, _penthouse;
        private float _trafficAcc;
        private System.Random _rng;
        private Station _stand;
        private Label3D _boardTitle, _boardSub;
        private Renderer _boardPanel;
        private float _bagCheck;

        public IEnumerable<NPC> Pedestrians => _pedestrians;

        public void Build(bool menuMode)
        {
            MenuMode = menuMode;
            _rng = new System.Random(4242);
            NpcNav.ClearPoints();
            _static = Props.Node(transform, "Static").transform;
            Atmos = gameObject.AddComponent<Atmosphere>();
            Atmos.Setup(transform);
            GroundAndStreet();
            Diner();
            Garage();
            Warehouse();
            Park();
            // Große Stadt (CityBuilder); alte Kulisse nur als Rückfall
            try { _penthouse = CityBuilder.Build(this, S, menuMode); }
            catch (System.Exception e)
            {
                Debug.LogWarning("CityBuilder: " + e);
                Background();
                Bounds();
            }
            _stations = Props.Node(transform, "Stations").transform;
            _decor = Props.Node(transform, "Decor").transform;
            _dynamic = Props.Node(transform, "Dynamic").transform;
            _customers = Props.Node(transform, "Customers").transform;
            _staff = Props.Node(transform, "Staff").transform;
            _belt = Props.Node(transform, "Belt").transform;
            Pedestrians_();
            gameObject.AddComponent<CityTraffic>().Setup(_dynamic);
            if (!menuMode) gameObject.AddComponent<GarbageTruck>().Setup(_dynamic);
            if (!menuMode) gameObject.AddComponent<StreetFestival>().Setup();
            try { gameObject.AddComponent<FinanceDistrict>().Setup(menuMode); } catch (System.Exception e) { Debug.LogWarning("Finanzviertel: " + e.Message); }
            // Statische Geometrie zusammenfassen: deutlich weniger Draw Calls.
            // Nur lesbare (prozedurale) Meshes: importierte Modelle sind nicht lesbar und würden Fehler werfen.
            var batch = new List<GameObject>();
            foreach (var mf in _static.GetComponentsInChildren<MeshFilter>(true))
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable) batch.Add(mf.gameObject);
            if (batch.Count > 0) StaticBatchingUtility.Combine(batch.ToArray(), _static.gameObject);
            RebuildStations();
            RefreshWorld(false);
            Atmos.SetTimeOfDay(12f);
            Atmos.RenderReflections();
        }

        private Transform S => _static;

        // =====================================================================================
        // Boden, Straße
        // =====================================================================================
        private void GroundAndStreet()
        {
            Props.Collider(transform, new Vector3(400, 1, 400), new Vector3(0, -0.5f, 0));
            Props.Box(S, new Vector3(400, 0.02f, 400), Mats.Grass(), new Vector3(0, -0.012f, 0), default, 0f);
            Props.Box(S, new Vector3(400, 0.02f, 8), Mats.Asphalt(), new Vector3(0, 0.004f, 0), default, 0f);
            var walk = Mats.Sidewalk(new Color(0.66f, 0.65f, 0.63f), new Color(0.56f, 0.55f, 0.53f));
            Props.Box(S, new Vector3(400, 0.03f, 3f), walk, new Vector3(0, 0.012f, -5.5f), default, 0f);
            Props.Box(S, new Vector3(400, 0.03f, 3f), walk, new Vector3(0, 0.012f, 5.5f), default, 0f);
            var curb = Mats.Std(new Color(0.7f, 0.7f, 0.68f), 0.8f);
            Props.Box(S, new Vector3(400, 0.1f, 0.18f), curb, new Vector3(0, 0.05f, -4f));
            Props.Box(S, new Vector3(400, 0.1f, 0.18f), curb, new Vector3(0, 0.05f, 4f));
            var lot = Mats.Concrete(new Color(0.5f, 0.5f, 0.49f), new Color(0.4f, 0.4f, 0.4f), 0.25f, 0f, 0.4f);
            Props.Box(S, new Vector3(120, 0.02f, 30), lot, new Vector3(-4, 0.008f, -22), default, 0f);
            var white = Mats.Std(new Color(0.92f, 0.92f, 0.9f), 0.6f);
            for (int i = 0; i < 34; i++) Props.Box(S, new Vector3(3f, 0.01f, 0.14f), white, new Vector3(-100 + i * 6f, 0.017f, 0), default, 0f, false);
            Props.Box(S, new Vector3(400, 0.01f, 0.12f), white, new Vector3(0, 0.017f, -3.65f), default, 0f, false);
            Props.Box(S, new Vector3(400, 0.01f, 0.12f), white, new Vector3(0, 0.017f, 3.65f), default, 0f, false);
            for (int i = 0; i < 7; i++)
            {
                Props.Box(S, new Vector3(0.5f, 0.012f, 3.6f), white, new Vector3(-38f + i, 0.018f, -1.9f), default, 0f, false);
                Props.Box(S, new Vector3(0.5f, 0.012f, 3.6f), white, new Vector3(-38f + i, 0.018f, 1.9f), default, 0f, false);
            }
            foreach (float x in new[] { -48f, -32f, -16f, 0f, 16f, 32f, 44f })
            {
                var lp = Props.LampPost(transform, out var light);
                lp.transform.localPosition = new Vector3(x, 0, 6.8f);
                NpcNav.AddPoint(transform.TransformPoint(new Vector3(x, 0, 6.8f)));
                lp.transform.localRotation = Quaternion.Euler(0, 180, 0);
                Atmos.StreetLights.Add(light);
            }
            foreach (float x in new[] { -25f, -4f, 36f })
            {
                var lp = Props.LampPost(transform, out var light);
                lp.transform.localPosition = new Vector3(x, 0, -6.85f);
                NpcNav.AddPoint(transform.TransformPoint(new Vector3(x, 0, -6.85f)));
                Atmos.StreetLights.Add(light);
            }
            StreetDressing();
        }

        private static readonly Vector3 BoardPos = new Vector3(20f, 0f, 8.4f);

        /// <summary>Freizuhaltende Kreise im Park (x, z, Radius): Plakatwand, Kübel, Sonnenschirm.</summary>
        private static readonly Vector3[] ParkReserved = { new Vector3(20f, 8.4f, 4.5f), new Vector3(-30f, 10.4f, 2.4f), new Vector3(24f, 10.4f, 2.4f), new Vector3(-8f, 20f, 2.6f) };

        /// <summary>Hydranten, Mülleimer, Verkehrsschilder und die Plakatwand mit der eigenen Marke.</summary>
        private void StreetDressing()
        {
            foreach (var x in new[] { -46.5f, -9f, 40f })
            {
                Props.AssetAt(S, "street.firehydrant", new Vector3(x, 0, 6.9f), 180f, 0.8f);
                NpcNav.AddPoint(transform.TransformPoint(new Vector3(x, 0, 6.9f)));
            }
            Props.AssetAt(S, "street.firehydrant", new Vector3(-27.5f, 0, -6.9f), 0f, 0.8f);
            NpcNav.AddPoint(transform.TransformPoint(new Vector3(-27.5f, 0, -6.9f)));
            foreach (var x in new[] { -52f, -2f, 22f })
            {
                Props.AssetAt(S, "street.trafficlight_a", new Vector3(x, 0, 6.95f), 180f, 3.4f);
                NpcNav.AddPoint(transform.TransformPoint(new Vector3(x, 0, 6.95f)));
            }
            Props.AssetAt(S, "street.construction_cone", new Vector3(46.2f, 0, -3.4f), 20f, 0.6f);
            Props.AssetAt(S, "street.construction_cone", new Vector3(46.9f, 0, -2.6f), -10f, 0.6f);
            // Plakatwand am Parkrand, Blick zur Straße (Marke sichtbar in der Welt, GAME_IDEAS #8)
            var board = Props.Billboard(transform, new Vector2(7f, 3.2f), new Color(0.15f, 0.15f, 0.17f), out _boardTitle, out _boardSub, out _boardPanel, null);
            board.transform.localPosition = new Vector3(BoardPos.x, 0, BoardPos.z);
            board.transform.localRotation = Quaternion.Euler(0, 180, 0);
            foreach (var l in board.GetComponentsInChildren<Light>(true)) Atmos.NightLights.Add((l, 3.2f));
            foreach (float px in new[] { -2.1f, 2.1f }) Props.Collider(transform, new Vector3(0.3f, 3.8f, 0.3f), new Vector3(BoardPos.x + px, 1.9f, BoardPos.z + 0.12f));
        }

        // =====================================================================================
        // Gebäude-Helfer
        // =====================================================================================
        /// <summary>Achsenparallele Wand von a nach b (gleiches z ODER gleiches x) mit Öffnungen (von, bis, Höhe).</summary>
        private void Wall(Vector3 a, Vector3 b, float height, float thick, Material mat, params Vector3[] openings)
        {
            WallBand(a, b, 0f, height, thick, mat, openings, true);
        }

        /// <summary>Wandband von y0 bis y1. Öffnungen: x = von, y = bis, z = Höhe der Öffnung.</summary>
        private void WallBand(Vector3 a, Vector3 b, float y0, float y1, float thick, Material mat, Vector3[] openings, bool openingsFromGround = false)
        {
            bool alongX = Mathf.Abs(a.z - b.z) < 0.01f;
            float start = alongX ? Mathf.Min(a.x, b.x) : Mathf.Min(a.z, b.z);
            float stop = alongX ? Mathf.Max(a.x, b.x) : Mathf.Max(a.z, b.z);
            var cuts = new List<Vector3>(openings ?? new Vector3[0]);
            cuts.Sort((p, q) => p.x.CompareTo(q.x));
            var segs = new List<Vector3>();
            var bottoms = new List<float>();
            float cursor = start;
            foreach (var o in cuts)
            {
                if (o.x > cursor)
                {
                    segs.Add(new Vector3(cursor, o.x, 0));
                    bottoms.Add(y0);
                }
                segs.Add(new Vector3(o.x, o.y, 0));
                bottoms.Add(openingsFromGround ? o.z : Mathf.Max(y0, o.z));
                cursor = o.y;
            }
            if (cursor < stop)
            {
                segs.Add(new Vector3(cursor, stop, 0));
                bottoms.Add(y0);
            }
            for (int i = 0; i < segs.Count; i++)
            {
                float len = segs[i].y - segs[i].x;
                float bottom = bottoms[i];
                float h = y1 - bottom;
                if (len <= 0.01f || h <= 0.01f) continue;
                float mid = (segs[i].x + segs[i].y) / 2f;
                if (alongX) Props.Solid(S, new Vector3(len, h, thick), mat, new Vector3(mid, bottom + h / 2f, a.z), default, 0.015f);
                else Props.Solid(S, new Vector3(thick, h, len), mat, new Vector3(a.x, bottom + h / 2f, mid), default, 0.015f);
            }
        }

        /// <summary>Fenster in einer Wand: Außen- und Innenscheibe + Fensterbank. normal zeigt nach außen.</summary>
        private void Window(Vector3 center, Vector2 size, Vector3 normal)
        {
            bool sideways = Mathf.Abs(normal.x) > 0.5f;
            var rot = sideways ? new Vector3(0, 90, 0) : Vector3.zero;
            Props.Box(S, new Vector3(size.x, size.y, 0.02f), Mats.Window(false), center + normal * 0.17f, rot, 0f, false);
            Props.Box(S, new Vector3(size.x, size.y, 0.02f), Mats.Window(true), center - normal * 0.17f, rot, 0f, false);
            var frame = Mats.Std(new Color(0.9f, 0.9f, 0.88f), 0.5f);
            var sill = sideways ? new Vector3(0.1f, 0.08f, size.x + 0.12f) : new Vector3(size.x + 0.12f, 0.08f, 0.1f);
            Props.Box(S, sill, frame, center + normal * 0.2f - new Vector3(0, size.y / 2f + 0.04f, 0));
            // Rahmen oben und seitlich
            var top = sideways ? new Vector3(0.06f, 0.06f, size.x + 0.08f) : new Vector3(size.x + 0.08f, 0.06f, 0.06f);
            Props.Box(S, top, frame, center + normal * 0.18f + new Vector3(0, size.y / 2f + 0.03f, 0));
        }

        private void Roof(Rect r, float height, Material mat)
        {
            Props.Box(S, new Vector3(r.width + 0.5f, 0.3f, r.height + 0.5f), mat, new Vector3(r.x + r.width / 2f, height + 0.15f, r.y + r.height / 2f), default, 0.03f);
            var trim = Mats.Std(new Color(0.2f, 0.21f, 0.23f), 0.6f);
            Props.Box(S, new Vector3(r.width + 0.6f, 0.35f, 0.12f), trim, new Vector3(r.x + r.width / 2f, height + 0.3f, r.y + r.height + 0.25f));
        }

        private void Floor(Rect r, Material mat)
        {
            Props.Box(S, new Vector3(r.width - 0.2f, 0.025f, r.height - 0.2f), mat, new Vector3(r.x + r.width / 2f, 0.02f, r.y + r.height / 2f), default, 0f, false);
        }

        private void CeilingLamp(Vector3 pos, float length, Color color, float energy, float range)
        {
            var fix = Props.CeilingLight(S, length);
            fix.transform.localPosition = pos;
            Props.PointLight(transform, pos - new Vector3(0, 1f, 0), color, energy * 2.2f, range);
        }

        // =====================================================================================
        // Kalles Imbiss
        // =====================================================================================
        private void Diner()
        {
            var r = DinerRect;
            float x0 = r.x, x1 = r.x + r.width, z0 = r.y, z1 = r.y + r.height;
            const float h = 4.2f;
            var brick = Mats.Brick(new Color(0.64f, 0.3f, 0.22f), new Color(0.8f, 0.77f, 0.7f));
            Wall(new Vector3(x0, 0, z1 - 0.15f), new Vector3(x1, 0, z1 - 0.15f), h, 0.3f, brick, new Vector3(-36.2f, -34.2f, 2.5f));
            Wall(new Vector3(x0, 0, z0 + 0.15f), new Vector3(x1, 0, z0 + 0.15f), h, 0.3f, brick);
            Wall(new Vector3(x0 + 0.15f, 0, z0), new Vector3(x0 + 0.15f, 0, z1), h, 0.3f, brick);
            Wall(new Vector3(x1 - 0.15f, 0, z0), new Vector3(x1 - 0.15f, 0, z1), h, 0.3f, brick);
            Roof(r, h, Mats.RoofMat(new Color(0.35f, 0.37f, 0.4f)));
            Floor(r, Mats.Tiles(0.45f));
            foreach (float wx in new[] { -40.5f, -31f }) Window(new Vector3(wx, 1.8f, z1 - 0.15f), new Vector2(4.2f, 1.5f), Vector3.forward);
            // Markise + Neonschild
            Props.Box(S, new Vector3(16.4f, 0.12f, 1.4f), Mats.Std(new Color(0.78f, 0.14f, 0.16f), 0.6f), new Vector3(-36, 3.1f, z1 + 0.6f), new Vector3(-12, 0, 0));
            for (int i = 0; i < 8; i++)
                Props.Box(S, new Vector3(1f, 0.13f, 1.41f), Mats.Std(new Color(0.95f, 0.95f, 0.93f), 0.6f), new Vector3(x0 + 0.5f + i * 2f, 3.101f, z1 + 0.6f), new Vector3(-12, 0, 0), 0f);
            Label3D.Create(transform, "KALLES IMBISS", 220f, new Color(1f, 0.32f, 0.22f), new Vector3(-36, 3.75f, z1 + 0.05f), false, 0f, false, 3.2f, 0, SignFont.Script);
            Props.PointLight(transform, new Vector3(-36, 3.6f, z1 + 1.2f), new Color(1f, 0.35f, 0.25f), 3f, 7f);
            var aboard = Props.Node(transform, "ABoard", new Vector3(-33f, 0, -5.8f), 20f);
            Props.Box(aboard.transform, new Vector3(0.7f, 1f, 0.05f), Mats.Std(new Color(0.12f, 0.12f, 0.12f), 0.7f), new Vector3(0, 0.5f, 0.18f), new Vector3(-10, 0, 0));
            Label3D.Create(aboard.transform, "DÖNER 5,50\nPOMMES 3,-\nCURRYWURST 4,-", 40f, Color.white, new Vector3(0, 0.55f, 0.21f), false, 0f, false, 1f, 0, SignFont.Condensed);
            // Theke
            Props.Solid(S, new Vector3(10.5f, 1f, 0.8f), Mats.Std(new Color(0.75f, 0.16f, 0.18f), 0.5f), new Vector3(-38.25f, 0.5f, -15.2f));
            Props.Box(S, new Vector3(10.7f, 0.06f, 0.95f), Mats.Std(new Color(0.85f, 0.86f, 0.88f), 0.25f, 0.8f), new Vector3(-38.25f, 1.03f, -15.2f));
            foreach (float sx in new[] { -42f, -40.4f, -36.8f }) Props.Stool(S).transform.localPosition = new Vector3(sx, 0, -14.3f);
            // Küche
            var steel = Mats.Std(new Color(0.75f, 0.77f, 0.8f), 0.3f, 0.85f);
            Props.Solid(S, new Vector3(10f, 0.95f, 0.9f), steel, new Vector3(-38.5f, 0.475f, -18.4f));
            Props.Box(S, new Vector3(1.6f, 0.05f, 0.7f), Mats.Std(new Color(0.1f, 0.1f, 0.1f), 0.4f), new Vector3(-41f, 0.98f, -18.4f));
            foreach (float fx in new[] { -38.2f, -37.2f })
            {
                Props.Box(S, new Vector3(0.7f, 0.2f, 0.6f), steel, new Vector3(fx, 1.05f, -18.4f));
                Props.Box(S, new Vector3(0.55f, 0.05f, 0.45f), Mats.Std(new Color(0.85f, 0.6f, 0.2f), 0.3f), new Vector3(fx, 1.14f, -18.4f), default, 0f);
            }
            Props.Box(S, new Vector3(6f, 0.6f, 1f), steel, new Vector3(-39f, 3.2f, -18.4f));
            Props.Solid(S, new Vector3(1f, 2.1f, 0.8f), Mats.Std(new Color(0.9f, 0.9f, 0.92f), 0.3f, 0.3f), new Vector3(-43.3f, 1.05f, -17.6f));
            DinerDressing();
            var menu = Props.SignBoard(transform, "KALLES KARTE", new Color(0.1f, 0.1f, 0.1f), new Color(1f, 0.85f, 0.3f), new Vector2(3.4f, 0.5f), SignFont.Retro);
            menu.transform.localPosition = new Vector3(-38, 2.6f, z0 + 0.32f);
            Label3D.Create(transform, "Döner ....... 5,50\nCurrywurst .. 4,00\nPommes ...... 3,00\nSchnitzel ... 8,90", 48f, new Color(0.95f, 0.95f, 0.9f), new Vector3(-38, 1.95f, z0 + 0.33f), false, 0f, false, 1f, 0, SignFont.Condensed);
            foreach (float lx in new[] { -41f, -36f, -31f }) CeilingLamp(new Vector3(lx, h - 0.1f, -12f), 1.2f, new Color(1f, 0.85f, 0.65f), 1.4f, 8f);
            CeilingLamp(new Vector3(-38.5f, h - 0.1f, -17.2f), 2f, new Color(1f, 0.95f, 0.85f), 1.1f, 6f);
        }

        /// <summary>Töpfe, Pfannen, Teller und Soßen in der Küche und auf der Theke.</summary>
        private void DinerDressing()
        {
            const float counterTop = 0.95f;
            Props.AssetAt(S, "diner.pot_a", new Vector3(-42.4f, counterTop, -18.45f), 10f, 0.42f);
            Props.AssetAt(S, "diner.pan_a", new Vector3(-40.6f, counterTop + 0.05f, -18.4f), 80f, 0.45f);
            Props.AssetAt(S, "diner.cuttingboard", new Vector3(-36f, counterTop, -18.35f), 0f, 0.5f);
            Props.AssetAt(S, "diner.dishrack_plates", new Vector3(-35f, counterTop, -18.45f), 0f, 0.42f);
            Props.AssetAt(S, "diner.jar_a_large", new Vector3(-34.2f, counterTop, -18.55f), 0f, 0.28f);
            Props.AssetAt(S, "food.pizza_box", new Vector3(-33.9f, counterTop, -18.1f), 12f, 0.34f);
            const float theke = 1.06f;
            Props.AssetAt(S, "food.bottle_ketchup", new Vector3(-42.6f, theke, -15.1f), 0f, 0.2f);
            Props.AssetAt(S, "food.bottle_musterd", new Vector3(-42.4f, theke, -15.1f), 0f, 0.2f);
            Props.AssetAt(S, "diner.menu", new Vector3(-40.2f, theke, -14.95f), 180f, 0.28f);
            Props.AssetAt(S, "food.cup_coffee", new Vector3(-37f, theke, -15f), 30f, 0.1f);
            Props.AssetAt(S, "food.soda", new Vector3(-41.7f, theke, -15f), 0f, 0.16f);
        }

        // =====================================================================================
        // Garage
        // =====================================================================================
        private void Garage()
        {
            var r = GarageRect;
            float x0 = r.x, x1 = r.x + r.width, z0 = r.y, z1 = r.y + r.height;
            const float h = 3.6f;
            var block = Mats.Brick(new Color(0.62f, 0.62f, 0.6f), new Color(0.5f, 0.5f, 0.48f), new Vector2(0.4f, 0.2f));
            Wall(new Vector3(x0, 0, z1 - 0.15f), new Vector3(x1, 0, z1 - 0.15f), h, 0.3f, block, new Vector3(-19.5f, -12.5f, 3f));
            Wall(new Vector3(x0, 0, z0 + 0.15f), new Vector3(x1, 0, z0 + 0.15f), h, 0.3f, block);
            Wall(new Vector3(x0 + 0.15f, 0, z0), new Vector3(x0 + 0.15f, 0, z1), h, 0.3f, block);
            Wall(new Vector3(x1 - 0.15f, 0, z0), new Vector3(x1 - 0.15f, 0, z1), h, 0.3f, block);
            Roof(r, h, Mats.RoofMat(new Color(0.42f, 0.44f, 0.47f)));
            Floor(r, Mats.Concrete(new Color(0.56f, 0.56f, 0.54f), new Color(0.44f, 0.44f, 0.43f), 0.5f, 0f, 0.5f));
            // aufgerolltes Tor
            Props.Cyl(S, 0.25f, 0.25f, 7.2f, Mats.Std(new Color(0.55f, 0.57f, 0.6f), 0.5f, 0.6f), new Vector3(-16, 3.25f, z1 - 0.45f), new Vector3(0, 0, 90));
            Props.Box(S, new Vector3(7.2f, 0.35f, 0.05f), Mats.Shutter(new Color(0.7f, 0.72f, 0.75f)), new Vector3(-16, 2.85f, z1 - 0.2f), default, 0f);
            Window(new Vector3(x0 + 0.15f, 1.9f, -13f), new Vector2(1.6f, 1f), Vector3.left);
            foreach (float lx in new[] { -18f, -13.8f }) CeilingLamp(new Vector3(lx, h - 0.08f, -12f), 1.4f, new Color(0.92f, 0.96f, 1f), 1.5f, 8f);
            var brand = Label3D.Create(transform, "GARAGE", 170f, Color.white, new Vector3(-16, 3.3f, z1 + 0.03f), false, 0f, false, 1f, 0, SignFont.Stencil);
            _brandLabels.Add(brand);
            var spot = new GameObject("Spot").AddComponent<Light>();
            spot.transform.SetParent(transform, false);
            spot.transform.localPosition = new Vector3(-16, 3.9f, z1 + 0.8f);
            spot.transform.localRotation = Quaternion.Euler(60, 180, 0);
            spot.type = LightType.Spot;
            spot.intensity = 3f;
            spot.range = 6f;
            spot.spotAngle = 70f;
            spot.color = new Color(1f, 0.9f, 0.75f);
            // Palette unter der Matratze (bleibt flach und prozedural, damit die Matratze nicht darin versinkt)
            Props.UseAssets = false;
            var pal = Props.Pallet(S);
            Props.UseAssets = true;
            pal.transform.localPosition = new Vector3(-12.3f, 0, -16.2f);
        }

        // =====================================================================================
        // Lagerhalle
        // =====================================================================================
        private void Warehouse()
        {
            var r = WarehouseRect;
            float x0 = r.x, x1 = r.x + r.width, z0 = r.y, z1 = r.y + r.height;
            const float h = 7.5f;
            var plinth = Mats.Concrete(new Color(0.62f, 0.62f, 0.6f), new Color(0.52f, 0.52f, 0.5f), 0.6f, 0f, 0.25f);
            var sheet = Mats.MetalSheet(new Color(0.36f, 0.45f, 0.56f), 18f);
            var gateOpen = new Vector3(14f, 20f, 4.6f);
            Wall(new Vector3(x0, 0, z1 - 0.15f), new Vector3(x1, 0, z1 - 0.15f), 1.2f, 0.35f, plinth, new Vector3(14f, 20f, 1.2f));
            Wall(new Vector3(x0, 0, z0 + 0.15f), new Vector3(x1, 0, z0 + 0.15f), 1.2f, 0.35f, plinth);
            Wall(new Vector3(x0 + 0.15f, 0, z0), new Vector3(x0 + 0.15f, 0, z1), 1.2f, 0.35f, plinth);
            Wall(new Vector3(x1 - 0.15f, 0, z0), new Vector3(x1 - 0.15f, 0, z1), 1.2f, 0.35f, plinth);
            WallBand(new Vector3(x0, 0, z1 - 0.15f), new Vector3(x1, 0, z1 - 0.15f), 1.2f, h, 0.3f, sheet, new[] { gateOpen });
            WallBand(new Vector3(x0, 0, z0 + 0.15f), new Vector3(x1, 0, z0 + 0.15f), 1.2f, h, 0.3f, sheet, null);
            WallBand(new Vector3(x0 + 0.15f, 0, z0), new Vector3(x0 + 0.15f, 0, z1), 1.2f, h, 0.3f, sheet, null);
            WallBand(new Vector3(x1 - 0.15f, 0, z0), new Vector3(x1 - 0.15f, 0, z1), 1.2f, h, 0.3f, sheet, null);
            Roof(r, h, Mats.RoofMat(new Color(0.4f, 0.42f, 0.45f)));
            Floor(r, Mats.Concrete(new Color(0.6f, 0.6f, 0.58f), new Color(0.52f, 0.52f, 0.5f), 0.3f, 0f, 0.2f));
            foreach (float wx in new[] { 6f, 27f }) Window(new Vector3(wx, 5.4f, z1 - 0.15f), new Vector2(10f, 1f), Vector3.forward);
            foreach (float wz in new[] { -24f, -14f })
            {
                Window(new Vector3(x1 - 0.15f, 5.4f, wz), new Vector2(7f, 1f), Vector3.right);
                Window(new Vector3(x0 + 0.15f, 5.4f, wz), new Vector2(7f, 1f), Vector3.left);
            }
            // Sicherheitslinien am Boden
            var yellow = Mats.Std(new Color(0.95f, 0.78f, 0.12f), 0.6f);
            foreach (float lz in new[] { -17.8f, -22.6f }) Props.Box(S, new Vector3(30f, 0.012f, 0.1f), yellow, new Vector3(17.5f, 0.035f, lz), default, 0f, false);
            Props.Box(S, new Vector3(0.1f, 0.012f, 16f), yellow, new Vector3(12f, 0.035f, -15f), default, 0f, false);
            foreach (float lx in new[] { 4f, 16f, 28f })
            foreach (float lz in new[] { -12f, -24f })
                CeilingLamp(new Vector3(lx, h - 0.2f, lz), 2.4f, new Color(0.95f, 0.97f, 1f), 1.7f, 15f);
            // Tor (Rolltor)
            _gate = Props.Node(transform, "Gate", new Vector3(17f, 2.3f, z1 - 0.15f)).transform;
            Props.Box(_gate, new Vector3(6f, 4.6f, 0.12f), Mats.Shutter(new Color(0.72f, 0.74f, 0.76f)), Vector3.zero, default, 0f);
            var gc = _gate.gameObject.AddComponent<BoxCollider>();
            gc.size = new Vector3(6f, 4.6f, 0.4f);
            _gateCol = gc;
            _saleSign = Props.SignBoard(transform, "ZU VERKAUFEN · 4.500 €", new Color(0.9f, 0.15f, 0.15f), Color.white, new Vector2(5f, 0.8f));
            _saleSign.transform.localPosition = new Vector3(6.5f, 2.6f, z1 + 0.05f);
            var brand = Label3D.Create(transform, "LAGERHALLE", 300f, Color.white, new Vector3(17f, 6.3f, z1 + 0.05f), false, 0f, false, 1f, 0, SignFont.Stencil);
            _brandLabels.Add(brand);
            // Büro-Trennwand
            var low = Mats.Std(new Color(0.85f, 0.85f, 0.82f), 0.6f);
            Props.Solid(S, new Vector3(6.85f, 1.1f, 0.12f), low, new Vector3(1.575f, 0.55f, -16f));
            Props.Solid(S, new Vector3(0.12f, 1.1f, 5.5f), low, new Vector3(5f, 0.55f, -13.25f));
            Props.Box(S, new Vector3(6.85f, 1.3f, 0.04f), Mats.Glass(), new Vector3(1.575f, 1.75f, -16f), default, 0f, false);
            Props.Box(S, new Vector3(0.04f, 1.3f, 5.5f), Mats.Glass(), new Vector3(5f, 1.75f, -13.25f), default, 0f, false);
            // Pausenecke
            var vm = Props.VendingMachine(S);
            vm.transform.localPosition = new Vector3(33.2f, 0, -18f);
            vm.transform.localRotation = Quaternion.Euler(0, -90, 0);
            Props.Collider(transform, new Vector3(0.8f, 1.9f, 0.9f), new Vector3(33.2f, 0.95f, -18f));
            var bt = Props.Table(S, 1.2f, 0.8f, 0.76f, Mats.Std(new Color(0.92f, 0.92f, 0.9f), 0.4f), Mats.DarkMetal());
            bt.transform.localPosition = new Vector3(29.8f, 0, -21f);
            foreach (float cz in new[] { -21.7f, -20.3f })
            {
                var ch = Props.Chair(S, Mats.Std(new Color(0.2f, 0.45f, 0.7f), 0.6f));
                ch.transform.localPosition = new Vector3(29.8f, 0, cz);
                ch.transform.localRotation = Quaternion.Euler(0, cz < -21f ? 0f : 180f, 0);
            }
            float palY = 0f;
            for (int i = 0; i < 3; i++)
            {
                var pl = Props.Pallet(S);
                pl.transform.localPosition = new Vector3(31.5f, palY, -28.5f);
                float ph = Props.LocalBounds(pl).size.y;
                palY += ph > 0.05f && ph < 0.6f ? ph : 0.15f;
            }
            if (Props.AssetAt(S, "logistics.pallet_small_decorated_b", new Vector3(31.5f, 0, -27.1f), 90f) != null)
                Props.Collider(transform, new Vector3(1.2f, 1.8f, 1.2f), new Vector3(31.5f, 0.9f, -27.1f));
            if (Props.AssetAt(S, "logistics.pallet_small_decorated_a", new Vector3(31.5f, 0, -25.8f), 0f) != null)
                Props.Collider(transform, new Vector3(1.2f, 0.9f, 1.2f), new Vector3(31.5f, 0.45f, -25.8f));
            WarehouseDressing();
            Props.Cyl(S, 0.1f, 0.1f, 0.5f, Mats.Std(new Color(0.85f, 0.1f, 0.1f), 0.4f), new Vector3(-1.72f, 1f, -20f));
        }

        /// <summary>Spinde, Werkbank, Kartons und Warnleuchten an freien Wänden der Halle.</summary>
        private void WarehouseDressing()
        {
            // Spinde an der Ostwand neben der Pausenecke (Front nach Westen)
            for (int i = 0; i < 3; i++)
                Props.AssetAt(S, i == 1 ? "logistics.locker_decorated" : "logistics.locker", new Vector3(33.35f, 0, -24.9f + i * 0.62f), -90f, 1.8f);
            Props.Collider(transform, new Vector3(0.66f, 1.8f, 1.9f), new Vector3(33.35f, 0.9f, -24.28f));
            // Werkbank an der Westwand im Lagerbereich (Front nach Osten)
            if (Props.AssetAt(S, "logistics.workbench_decorated", new Vector3(-1.2f, 0, -23.5f), 90f, 1.8f) != null)
                Props.Collider(transform, new Vector3(0.9f, 1f, 1.8f), new Vector3(-1.2f, 0.5f, -23.5f));
            // Kartons in der Ecke
            Props.AssetAt(S, "logistics.box_large", new Vector3(-1.1f, 0, -29.9f), 6f);
            Props.AssetAt(S, "logistics.box_wide", new Vector3(-1.2f, 0.55f, -29.9f), -8f);
            Props.AssetAt(S, "logistics.box_small", new Vector3(0.1f, 0, -30.2f), 20f);
            Props.AssetAt(S, "logistics.warning_orange", new Vector3(20.6f, 0, -6.9f), 0f, 1.1f);
            Props.AssetAt(S, "logistics.warning_orange", new Vector3(13.4f, 0, -6.9f), 0f, 1.1f);
        }

        // =====================================================================================
        // Park, Hintergrund, Grenzen
        // =====================================================================================
        private void Park()
        {
            var path = Mats.Sidewalk(new Color(0.78f, 0.72f, 0.62f), new Color(0.62f, 0.58f, 0.52f));
            Props.Box(S, new Vector3(106, 0.025f, 2.4f), path, new Vector3(-4, 0.01f, 12.5f), default, 0f, false);
            for (int i = 0; i < 4; i++) Props.Box(S, new Vector3(2f, 0.025f, 5f), path, new Vector3(-40f + i * 26f, 0.01f, 8.8f), default, 0f, false);
            foreach (float bx in new[] { -44f, -20f, 4f, 28f })
            {
                var b = Props.Bench(S);
                b.transform.localPosition = new Vector3(bx, 0, 14.4f);
                b.transform.localRotation = Quaternion.Euler(0, 180, 0);
                Props.Collider(transform, new Vector3(1.8f, 0.9f, 0.5f), new Vector3(bx, 0.45f, 14.4f));
                Props.TrashBin(S).transform.localPosition = new Vector3(bx + 1.5f, 0, 14.4f);
            }
            int placed = 0, tries = 0;
            while (placed < 30 && tries < 400)
            {
                tries++;
                float x = Range(-55f, 47f), z = Range(8.2f, 28.5f);
                if (Mathf.Abs(z - 12.5f) < 2.2f || Mathf.Abs(z - 14.4f) < 1f) continue;
                if (x > -46f && x < 8f && z < 11.3f) continue;
                bool blocked = false;
                foreach (var rv in ParkReserved)
                    if ((x - rv.x) * (x - rv.x) + (z - rv.y) * (z - rv.y) < rv.z * rv.z) blocked = true;
                if (blocked) continue;
                var tree = Props.Tree(S, Range(0.85f, 1.3f), _rng.Next(0, 6));
                tree.transform.localPosition = new Vector3(x, 0, z);
                tree.transform.localRotation = Quaternion.Euler(0, Range(0f, 360f), 0);
                Props.Collider(transform, new Vector3(0.4f, 3f, 0.4f), new Vector3(x, 1.5f, z));
                placed++;
            }
            for (int i = 0; i < 16; i++) Props.Bush(S, Range(0.7f, 1.1f)).transform.localPosition = new Vector3(-54f + i * 6.5f + Range(-1f, 1f), 0, 7.6f);
            Props.Fence(S, 104f).transform.localPosition = new Vector3(-5, 0, 29.8f);
            var dump = Props.Dumpster(S);
            dump.transform.localPosition = new Vector3(-30f, 0, -21f);
            Props.Collider(transform, new Vector3(2.4f, 1.4f, 1.5f), new Vector3(-30f, 0.7f, -21f));
            // Parasols und Blumenkübel im Park, Sitzbank-Ecke
            Props.AssetAt(S, "street.detail_parasol_a", new Vector3(-8f, 0, 20f), 0f, 3f);
            Props.AssetAt(S, "nature.planter", new Vector3(-30f, 0, 10.4f), 0f, 2.2f);
            Props.AssetAt(S, "nature.planter", new Vector3(24f, 0, 10.4f), 0f, 2.2f);
            for (int i = 0; i < 2; i++) Props.Pallet(S).transform.localPosition = new Vector3(-24.5f, i * 0.3f, -20f);
        }

        private float Range(float a, float b) => a + (float)_rng.NextDouble() * (b - a);

        private void Background()
        {
            if (BackgroundModels()) return;
            var rng = new System.Random(99);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            Color[] walls = { new Color(0.72f, 0.66f, 0.58f), new Color(0.6f, 0.62f, 0.66f), new Color(0.75f, 0.55f, 0.45f), new Color(0.55f, 0.58f, 0.52f), new Color(0.82f, 0.8f, 0.74f) };
            var roof = Mats.Std(new Color(0.25f, 0.25f, 0.27f), 0.7f);
            float x = -120f;
            int variant = 0;
            while (x < 120f)
            {
                float w = R(10f, 18f);
                float hgt = R(9f, 30f);
                float cx = x + w / 2f;
                if (Mathf.Abs(cx - 18f) < 9f) hgt = 46f;
                var mat = Mats.Facade(walls[rng.Next(walls.Length)], (variant++ % 5) + 1, 0.4f);
                Props.Box(S, new Vector3(w - 1f, hgt, 12f), mat, new Vector3(cx, hgt / 2f, 42f), default, 0f, true, true);
                Props.Box(S, new Vector3(w - 0.6f, 0.4f, 12.4f), roof, new Vector3(cx, hgt + 0.2f, 42f));
                if (hgt >= 46f)
                {
                    _penthouse = Props.Box(transform, new Vector3(w - 0.9f, 3f, 12.1f), Mats.Emit(new Color(1f, 0.78f, 0.3f), 3f), new Vector3(cx, hgt - 2f, 42f), default, 0f, false);
                    _penthouse.SetActive(false);
                }
                float h2 = R(8f, 22f);
                var mat2 = Mats.Facade(walls[rng.Next(walls.Length)], (variant++ % 5) + 1, 0.35f);
                Props.Box(S, new Vector3(w - 1f, h2, 12f), mat2, new Vector3(cx, h2 / 2f, -44f), default, 0f, true, true);
                x += w;
            }
            foreach (float side in new[] { -1f, 1f })
            foreach (float zz in new[] { -20f, 20f })
            {
                float hh = R(10f, 20f);
                var mat3 = Mats.Facade(walls[rng.Next(walls.Length)], (variant++ % 5) + 1, 0.45f);
                Props.Box(S, new Vector3(20f, hh, 22f), mat3, new Vector3(side > 0 ? 75f : -78f, hh / 2f, zz), default, 0f, true, true);
            }
        }

        /// <summary>
        /// Stadtkulisse aus Kenney-City-Modellen: vorne (Nordseite hinter dem Park) Geschäftshäuser, dahinter
        /// Hochhäuser; im Süden hinter der Halle eine zweite Reihe. Das Hochhaus bei x = 18 bekommt das Penthouse.
        /// false, wenn die Modelle fehlen (dann baut <see cref="Background"/> prozedural).
        /// </summary>
        private bool BackgroundModels()
        {
            if (!AssetLib.HasModel("city.building_a_2") || !AssetLib.HasModel("city.building_skyscraper_d")) return false;
            var rng = new System.Random(99);
            string[] front = { "city.building_a_2", "city.building_b_2", "city.building_c_2", "city.building_d_2", "city.building_e_2", "city.building_f_2",
                "city.building_g_2", "city.building_h_2", "city.building_i", "city.building_j", "city.building_k", "city.building_l", "city.building_n" };
            string[] tall = { "city.building_skyscraper_a", "city.building_skyscraper_b", "city.building_skyscraper_c", "city.building_skyscraper_e", "city.building_m" };
            string[] low = { "city.low_detail_building_a", "city.low_detail_building_b", "city.low_detail_building_c", "city.low_detail_building_d", "city.low_detail_building_e",
                "city.low_detail_building_f", "city.low_detail_building_g", "city.low_detail_building_h", "city.low_detail_building_i", "city.low_detail_building_j",
                "city.low_detail_building_k", "city.low_detail_building_l", "city.low_detail_building_m", "city.low_detail_building_wide_a", "city.low_detail_building_wide_b" };

            // Reihe 1: Nordseite, Front zur Straße (-Z)
            Row(front, rng, -118f, 118f, 41f, 180f, 1.15f, 18f);
            // Reihe 2: Hochhäuser dahinter
            Row(tall, rng, -120f, 120f, 60f, 180f, 1f, 18f);
            var tower = Props.AssetAt(S, "city.building_skyscraper_d", new Vector3(18f, 0, 46f), 180f, 48f);
            if (tower != null)
            {
                var b = Props.LocalBounds(tower);
                _penthouse = Props.Box(transform, new Vector3(b.size.x * 1.02f, 3f, b.size.z * 1.02f), Mats.Emit(new Color(1f, 0.78f, 0.3f), 3f),
                    new Vector3(18f, Mathf.Max(8f, b.max.y - 5f), 46f - b.center.z), default, 0f, false);
                _penthouse.SetActive(false);
            }
            // Reihe 3: Südseite hinter der Halle (Front nach +Z)
            Row(low, rng, -120f, 120f, -44f, 0f, 1.6f, -1000f);
            Row(tall, rng, -120f, 120f, -62f, 0f, 1f, -1000f);
            // Seitliche Abschlüsse
            foreach (float side in new[] { -1f, 1f })
            foreach (float zz in new[] { -20f, 20f })
                Props.AssetAt(S, low[rng.Next(low.Length)], new Vector3(side > 0 ? 72f : -76f, 0, zz), side > 0 ? -90f : 90f, 18f);
            return true;
        }

        /// <summary>Eine Häuserreihe entlang X (Lücke bei skipX für das Penthouse-Hochhaus).</summary>
        private void Row(string[] ids, System.Random rng, float xFrom, float xTo, float z, float rotY, float scale, float skipX)
        {
            float x = xFrom;
            int guard = 0;
            while (x < xTo && guard++ < 80)
            {
                string id = ids[rng.Next(ids.Length)];
                var info = AssetLib.Info(id);
                if (info == null) continue;
                float fit = info.Meters * scale;
                var mb = AssetLib.ModelBounds(id);
                float k = info.Meters > 0.01f ? fit / info.Meters : 1f;
                float w = Mathf.Max(4f, mb.size.x * k);
                float cx = x + w / 2f + 0.3f;
                if (Mathf.Abs(cx - skipX) < 7f)
                {
                    x = skipX + 7f;
                    continue;
                }
                Props.AssetAt(S, id, new Vector3(cx, 0, z), rotY, fit);
                x += w + 0.6f + (float)rng.NextDouble() * 1.5f;
            }
        }

        private void Bounds()
        {
            Props.Collider(transform, new Vector3(0.5f, 6, 70), new Vector3(-57f, 3, -2f));
            Props.Collider(transform, new Vector3(0.5f, 6, 70), new Vector3(49f, 3, -2f));
            Props.Collider(transform, new Vector3(110, 6, 0.5f), new Vector3(-4f, 3, -34f));
            Props.Collider(transform, new Vector3(110, 6, 0.5f), new Vector3(-4f, 3, 30f));
            foreach (float bx in new[] { -56.2f, 48.2f })
            foreach (float bz in new[] { -5.5f, -2f, 2f, 5.5f })
            {
                var b = Props.Barrier(S);
                b.transform.localPosition = new Vector3(bx, 0, bz);
                b.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
            Props.Fence(S, 110f).transform.localPosition = new Vector3(-4, 0, -33.8f);
        }

        // =====================================================================================
        // Stationen, Deko, Kunden
        // =====================================================================================
        private struct Def
        {
            public StationType Type;
            public Vector3 Pos;
            public float Rot;
            public StationOpts Opts;
        }

        private static Def D(StationType t, Vector3 pos, float rot, int stage = 0, int product = 0, int table = 0) =>
            new Def { Type = t, Pos = pos, Rot = rot, Opts = new StationOpts { Stage = stage, ProductIndex = product, TableId = table } };

        private static Def Sign(string text, Vector3 pos, float rot, Color color, int stage) =>
            new Def { Type = StationType.Sign, Pos = pos, Rot = rot, Opts = new StationOpts { Stage = stage, Text = text, Color = color } };

        private static readonly Color SignYellow = new Color(0.95f, 0.78f, 0.1f);
        private static readonly Color SignBlue = new Color(0.16f, 0.32f, 0.62f);
        private static readonly Color SignGreen = new Color(0.2f, 0.62f, 0.35f);
        private static readonly Color SignRed = new Color(0.86f, 0.16f, 0.14f);
        private static readonly Color SignB2B = new Color(0.18f, 0.35f, 0.7f);

        private List<Def> StationDefs()
        {
            var sim = Game.Sim;
            var defs = new List<Def>
            {
                D(StationType.NpcTalk, new Vector3(-39.5f, 0, -16.6f), 0f),
                D(StationType.DinerPass, new Vector3(-34.2f, 0, -15.2f), 0f),
            };
            foreach (var t in DinerTables) defs.Add(D(StationType.DinerTable, t.pos, 0f, 0, 0, t.id));
            if (sim.LocationStage == 0)
            {
                defs.Add(D(StationType.Pc, new Vector3(-20f, 0, -16.35f), 0f));
                defs.Add(D(StationType.Label, new Vector3(-17.6f, 0, -16.45f), 0f));
                for (int i = 0; i < GameData.GarageProducts; i++)
                    defs.Add(D(StationType.Regal, new Vector3(-10.75f, 0, -14.2f + i * 2.3f), -90f, 0, i));
                defs.Add(D(StationType.Pack, new Vector3(-15.2f, 0, -12.6f), 0f));
                defs.Add(D(StationType.Fold, new Vector3(-21.3f, 0, -12.6f), 90f));
                defs.Add(D(StationType.Dock, new Vector3(-20.4f, 0, -9f), 90f));
                defs.Add(D(StationType.Ship, new Vector3(-11.2f, 0, -5.4f), -90f));
                defs.Add(D(StationType.EndDay, new Vector3(-12.4f, 0, -15.8f), 0f));
                // v3.0: Retouren (Fach neben dem Wareneingang, Prüftisch + Container am Tor), B2B-Palettenplatz
                // im Hof zwischen Imbiss und Garage, Bestell-Monitor an der Rückwand, Zonenschilder.
                defs.Add(D(StationType.ReturnTray, new Vector3(-20.4f, 0, -10.6f), 90f));
                defs.Add(D(StationType.ReturnDesk, new Vector3(-12.1f, 0, -7.75f), 180f));
                defs.Add(D(StationType.ReturnBin, new Vector3(-11.05f, 0, -7.78f), 180f));
                defs.Add(D(StationType.Pallet, new Vector3(-25f, 0, -12.6f), 90f));
                defs.Add(D(StationType.Monitor, new Vector3(-17.3f, 2.3f, -16.8f), 0f));
                defs.Add(Sign("LAGER", new Vector3(-10.22f, 2.75f, -11.9f), -90f, SignBlue, 0));
                defs.Add(Sign("PACKEN", new Vector3(-15.2f, 2.8f, -13.3f), 0f, SignGreen, 0));
                defs.Add(Sign("RETOUREN", new Vector3(-11.6f, 2.5f, -7.22f), 180f, SignRed, 0));
            }
            else
            {
                defs.Add(D(StationType.Pc, new Vector3(1.5f, 0, -13.2f), 90f, 1));
                defs.Add(D(StationType.EndDay, new Vector3(12.6f, 0, -7.7f), 180f, 1));
                // Reihe A an der Rückwand (Produkte 1-6), Reihe B gegenüber (Produkte 7-10) - Gang dazwischen
                for (int i = 0; i < GameData.Products.Length; i++)
                {
                    if (i < 6) defs.Add(D(StationType.Regal, new Vector3(2.8f + i * 5f, 0, -29.3f), 0f, 1, i));
                    else
                    {
                        float[] rowB = { 7.8f, 12.8f, 22.8f, 27.8f };
                        defs.Add(D(StationType.Regal, new Vector3(rowB[i - 6], 0, -24.9f), 180f, 1, i));
                    }
                }
                defs.Add(D(StationType.Fold, new Vector3(8f, 0, -20f), 0f));
                defs.Add(D(StationType.Pack, new Vector3(13f, 0, -20f), 0f));
                defs.Add(D(StationType.Label, new Vector3(17.5f, 0, -20f), 0f));
                defs.Add(D(StationType.Dock, new Vector3(26.5f, 0, -10.2f), 0f, 1));
                defs.Add(D(StationType.Ship, new Vector3(6f, 0, -13.2f), 90f, 1));
                if (sim.HasUpgrade("conveyor")) defs.Add(D(StationType.Conveyor, new Vector3(22.5f, 0, -15.5f), -90f));
                // v3.0: Retourenecke rechts vorne am Tor, B2B-Palettenplatz links hinten, Monitor über dem Packtisch.
                defs.Add(D(StationType.ReturnTray, new Vector3(29.2f, 0, -9.8f), 0f));
                defs.Add(D(StationType.ReturnDesk, new Vector3(31f, 0, -7.85f), 180f));
                defs.Add(D(StationType.ReturnBin, new Vector3(32.4f, 0, -7.8f), 180f));
                defs.Add(D(StationType.Pallet, new Vector3(1.4f, 0, -22f), 90f));
                defs.Add(D(StationType.Monitor, new Vector3(13f, 3.2f, -21.2f), 0f, 1));
                defs.Add(Sign("WARENEINGANG", new Vector3(26.5f, 4.6f, -10.2f), 0f, SignYellow, 1));
                defs.Add(Sign("LAGER", new Vector3(17.8f, 5.2f, -27.1f), 0f, SignBlue, 1));
                defs.Add(Sign("PACKEN", new Vector3(13f, 5.3f, -20.2f), 0f, SignGreen, 1));
                defs.Add(Sign("VERSAND", new Vector3(6f, 3.7f, -13.2f), 90f, SignGreen, 1));
                defs.Add(Sign("RETOUREN", new Vector3(30.6f, 3.8f, -9f), 0f, SignRed, 1));
                defs.Add(Sign("B2B", new Vector3(1.4f, 4.2f, -22f), 90f, SignB2B, 1));
            }
            if (sim.HasUpgrade("stand")) defs.Add(D(StationType.Stand, StandPos, 0f));
            // PaketBlitz: Packstationen in der Stadt + Schalter in der Filiale (gratis Abgabe)
            foreach (var pd in PostService.StationDefs()) defs.Add(D(StationType.Ship, pd.pos, pd.rot, pd.stage));
            return defs;
        }

        // =====================================================================================
        // Verschiebbare Möbel (Taste B)
        // =====================================================================================
        /// <summary>Innenraum je Ausbaustufe (0 = Garage, 1 = Lagerhalle) für den Verschiebe-Modus.</summary>
        public static FloorRect RoomFor(int stage)
        {
            var r = stage >= 1 ? WarehouseRect : GarageRect;
            return new FloorRect(r.xMin, r.yMin, r.xMax, r.yMax);
        }

        private static readonly FloorRect[] GarageKeepOut =
        {
            new FloorRect(-19.7f, -8.8f, -12.3f, -6.5f), // Tor
        };

        private static readonly FloorRect[] WarehouseKeepOut =
        {
            new FloorRect(13.8f, -9.4f, 20.2f, -6.5f), // Rolltor
            new FloorRect(4.3f, -10.6f, 5.7f, -7f), // Durchgang ins Büro
        };

        /// <summary>Bereiche, die frei bleiben müssen (Türen, Durchgänge).</summary>
        public static IList<FloorRect> KeepOutsFor(int stage) => stage >= 1 ? WarehouseKeepOut : GarageKeepOut;

        /// <summary>Stationen, die verschoben werden dürfen. Wareneingang, Versand, Förderband, Monitor,
        /// Schilder, Matratze/Stempeluhr und der Verkaufsstand bleiben fest (Lieferwagen, Wände, Band).</summary>
        private static bool IsMovableStation(StationType t, int locationStage)
        {
            switch (t)
            {
                case StationType.Pc:
                case StationType.Label:
                case StationType.Regal:
                case StationType.Pack:
                case StationType.Fold:
                case StationType.ReturnTray:
                case StationType.ReturnDesk:
                case StationType.ReturnBin:
                    return true;
                case StationType.Pallet:
                    return locationStage >= 1; // in der Garage steht der Palettenplatz draußen im Hof
            }
            return false;
        }

        public static string StationKey(int stage, StationType t, int productIndex) => "st:" + stage + ":" + t + ":" + productIndex;
        public static string DecorKey(string id, int stage) => "deco:" + id + ":" + stage;

        private static readonly Dictionary<string, (string label, bool flat)> MovableDecor = new Dictionary<string, (string, bool)>
        {
            { "pflanze", ("Pflanze", false) }, { "stehlampe", ("Stehlampe", false) }, { "teppich", ("Teppich", true) },
            { "billy", ("Bücherregal", false) }, { "kaffee", ("Kaffeemaschine", false) }, { "sofa", ("Sofa", false) },
            { "sneaker", ("Sneaker-Vitrine", false) }, { "gamingstuhl", ("Gaming-Stuhl", false) },
        };

        /// <summary>Gespeicherten Platz übernehmen (Höhe bleibt wie im Standard-Layout).</summary>
        private static void ApplyPlacement(Transform t, string key)
        {
            var p = Game.Sim?.GetFurniture(key);
            if (p == null || t == null) return;
            t.localPosition = new Vector3(p.X, t.localPosition.y, p.Z);
            t.localRotation = Quaternion.Euler(0f, p.RotY, 0f);
        }

        public void RebuildStations()
        {
            for (int i = _stations.childCount - 1; i >= 0; i--) Destroy(_stations.GetChild(i).gameObject);
            _stand = null;
            int stage = Game.Sim != null ? Game.Sim.LocationStage : 0;
            foreach (var d in StationDefs())
            {
                var go = Props.Node(_stations, d.Type.ToString(), d.Pos, d.Rot);
                var st = go.AddComponent<Station>();
                st.Setup(d.Type, d.Opts);
                if (!MenuMode && IsMovableStation(d.Type, stage) && st.Kit != null)
                {
                    string key = StationKey(stage, d.Type, d.Type == StationType.Regal ? d.Opts.ProductIndex : 0);
                    ApplyPlacement(go.transform, key);
                    go.AddComponent<Movable>().Setup(key, st.Title, stage, false, st.Kit.Size, st.Kit.Center);
                }
                if (d.Type == StationType.NpcTalk && st.Npc != null && Game.Player != null) st.Npc.LookAtTarget = Game.Player.transform;
                if (d.Type == StationType.Stand) _stand = st;
            }
            RefreshBelt();
        }

        public void RefreshWorld(bool animate)
        {
            RefreshBrand();
            RefreshDecor();
            RefreshGate(animate);
            RefreshCustomers();
            RefreshBelt();
            if (_penthouse != null) _penthouse.SetActive(Game.Sim.LifestyleOwned.Contains("penthouse"));
        }

        private void RefreshBrand()
        {
            var sim = Game.Sim;
            _brandLabels.RemoveAll(x => x == null);
            foreach (var l in _brandLabels)
            {
                l.SetText(sim.BrandName.ToUpperInvariant());
                l.SetColor(Color.Lerp(Color.white, sim.BrandColor.ToColor(), 0.35f));
            }
            if (_brandLabels.Count > 1) _brandLabels[1].gameObject.SetActive(sim.LocationStage >= 1);
            RefreshBillboard();
        }

        /// <summary>Plakatwand: Markenname in Markenfarbe, Spruch je nach Bekanntheit/Bewertung.</summary>
        private void RefreshBillboard()
        {
            var sim = Game.Sim;
            if (sim == null || _boardTitle == null || _boardSub == null) return;
            Color brand = sim.BrandColor.ToColor();
            if (_boardPanel != null) _boardPanel.sharedMaterial = Mats.Std(brand, 0.6f);
            float lum = brand.r * 0.3f + brand.g * 0.59f + brand.b * 0.11f;
            Color text = lum > 0.6f ? new Color(0.08f, 0.08f, 0.1f) : Color.white;
            _boardTitle.SetText(sim.BrandName.ToUpperInvariant());
            _boardTitle.SetColor(text);
            string[] slogans =
            {
                "Bald auch in deiner Stadt.", "Jetzt online bestellen!", "Schon " + Mathf.Max(1, sim.TotalShipped) + " Pakete verschickt!",
                "Die ganze Stadt redet drüber.",
            };
            int idx = sim.Awareness >= 0.8f ? 3 : (sim.TotalShipped >= 50 ? 2 : (sim.Awareness >= 0.2f ? 1 : 0));
            _boardSub.SetText(slogans[idx]);
            _boardSub.SetColor(text);
        }

        /// <summary>Anteil der Passanten mit Markentüte (Bekanntheit + Bewertung).</summary>
        private void RefreshBags()
        {
            var sim = Game.Sim;
            if (sim == null) return;
            float share = Mathf.Clamp01((sim.Awareness - 0.15f) * 0.45f) * Mathf.Clamp01((sim.Reputation - 3f) / 1.5f);
            Color brand = sim.BrandColor.ToColor();
            for (int i = 0; i < _pedestrians.Count; i++)
            {
                var npc = _pedestrians[i];
                if (npc == null) continue;
                // stabil pro Figur: Figur i trägt eine Tüte, wenn ihr Anteil unter share liegt
                float slot = ((i * 37) % 100) / 100f;
                npc.SetBag(slot < share, brand);
            }
        }

        private void RefreshGate(bool animate)
        {
            bool open = Game.Sim.LocationStage >= 1;
            _saleSign.SetActive(!open);
            float targetY = open ? 6.3f : 2.3f;
            float targetScale = open ? 0.15f : 1f;
            _gateCol.enabled = !open;
            if (animate && open && Mathf.Abs(_gate.localPosition.y - targetY) > 0.1f)
            {
                Game.Sound("door");
                float y0 = _gate.localPosition.y, s0 = _gate.localScale.y;
                Anim.Run(2.4f, t =>
                {
                    if (_gate == null) return;
                    _gate.localPosition = new Vector3(_gate.localPosition.x, Mathf.Lerp(y0, targetY, t), _gate.localPosition.z);
                    _gate.localScale = new Vector3(1f, Mathf.Lerp(s0, targetScale, t), 1f);
                }, null, Ease.InOutSine);
            }
            else
            {
                _gate.localPosition = new Vector3(_gate.localPosition.x, targetY, _gate.localPosition.z);
                _gate.localScale = new Vector3(1f, targetScale, 1f);
            }
        }

        private void RefreshDecor()
        {
            for (int i = _decor.childCount - 1; i >= 0; i--) Destroy(_decor.GetChild(i).gameObject);
            var sim = Game.Sim;
            var owned = new HashSet<string>(sim.DecorOwned);
            owned.UnionWith(sim.LifestyleOwned);
            foreach (var id in owned)
            {
                if (!DecorSlots.TryGetValue(id, out var slots)) continue;
                foreach (var slot in slots)
                {
                    if (slot.stage == 1 && sim.LocationStage < 1) continue;
                    var node = DecorNode(id);
                    if (node == null) continue;
                    node.transform.localPosition = slot.pos;
                    node.transform.localRotation = Quaternion.Euler(0, slot.rot, 0);
                    // Bodenmöbel lassen sich verschieben (Wandbilder, Neon, Whiteboard und Autos bleiben fest).
                    if (!MenuMode && slot.stage >= 0 && Mathf.Abs(slot.pos.y) < 0.01f && MovableDecor.TryGetValue(id, out var md))
                    {
                        string key = DecorKey(id, slot.stage);
                        ApplyPlacement(node.transform, key);
                        node.AddComponent<Movable>().Setup(key, md.label, slot.stage, md.flat, null, null, true);
                    }
                }
            }
        }

        private GameObject DecorNode(string id)
        {
            var p = _decor;
            switch (id)
            {
                case "pflanze": return Props.Plant(p, 1.4f);
                case "poster": return Props.Poster(p, "HUSTLE\nHARDER", new Color(0.12f, 0.12f, 0.14f));
                case "stehlampe": return Props.FloorLamp(p);
                case "teppich": return Props.Rug(p, new Color(0.55f, 0.2f, 0.25f));
                case "billy": return Props.Billy(p);
                case "whiteboard": return Props.Whiteboard(p);
                case "kaffee": return Props.CoffeeMachine(p);
                case "sofa": return Props.Sofa(p, new Color(0.35f, 0.22f, 0.15f));
                case "sneaker":
                {
                    var n = Props.Node(p, "Sneaker");
                    Props.Box(n.transform, new Vector3(0.5f, 0.9f, 0.4f), Mats.Std(new Color(0.95f, 0.95f, 0.95f), 0.4f), new Vector3(0, 0.45f, 0));
                    Props.ShoeDisplay(n.transform).transform.localPosition = new Vector3(0, 0.92f, 0);
                    return n;
                }
                case "gamingstuhl": return Props.GamingChair(p);
                case "neon": return Props.NeonSign(p, "HUSTLE", new Color(1f, 0.3f, 0.8f));
                case "auto": return Props.Car(p, new Color(0.3f, 0.45f, 0.35f), false, "vehicle.kombi");
                case "sportwagen": return Props.Car(p, new Color(0.9f, 0.12f, 0.1f), true, "vehicle.sportscar");
            }
            return null;
        }

        private void RefreshCustomers()
        {
            for (int i = _customers.childCount - 1; i >= 0; i--) Destroy(_customers.GetChild(i).gameObject);
            var sim = Game.Sim;
            var rng = new System.Random(77 + sim.Day);
            var tables = sim.StoryStage == "diner" ? new[] { 2, 3, 4 } : new[] { 1, 4 };
            foreach (var t in DinerTables)
            {
                if (System.Array.IndexOf(tables, t.id) < 0) continue;
                var look = CharacterKit.RandomLook(rng);
                look.Sitting = true;
                var go = Props.Node(_customers, "Guest", t.pos + new Vector3(0, 0, 0.62f), 180f);
                go.AddComponent<NPC>().Setup(look);
                // Essen auf dem Teller vor dem Gast
                string[] dishes = { "food.food_burger", "food.food_dinner", "food.food_stew" };
                if (Props.AssetAt(_customers, dishes[rng.Next(dishes.Length)], t.pos + new Vector3(0.05f, 0.77f, 0.24f), 180f, 0.3f) == null)
                    Props.AssetAt(_customers, "food.plate_dinner", t.pos + new Vector3(0.05f, 0.77f, 0.24f), 180f, 0.28f);
                Props.AssetAt(_customers, rng.Next(2) == 0 ? "food.soda" : "food.mug", t.pos + new Vector3(-0.3f, 0.77f, 0.22f), 0f, 0.13f);
            }
        }

        // =====================================================================================
        // Förderband
        // =====================================================================================
        private void RefreshBelt()
        {
            if (_belt == null) return;
            for (int i = _belt.childCount - 1; i >= 0; i--) Destroy(_belt.GetChild(i).gameObject);
            _beltBoxes.Clear();
            var sim = Game.Sim;
            if (sim.LocationStage < 1 || !sim.HasUpgrade("conveyor")) return;
            float length = BeltStart.x - BeltEnd.x;
            float cx = (BeltStart.x + BeltEnd.x) / 2f;
            var frame = Mats.Std(new Color(0.2f, 0.45f, 0.25f), 0.5f, 0.4f);
            Props.Collider(_belt, new Vector3(length, 0.9f, 0.8f), new Vector3(cx, 0.45f, BeltStart.z));
            Props.Box(_belt, new Vector3(length, 0.1f, 0.8f), frame, new Vector3(cx, 0.75f, BeltStart.z));
            var belt = Props.Box(_belt, new Vector3(length, 0.04f, 0.66f), Mats.Conveyor(), new Vector3(cx, 0.82f, BeltStart.z), default, 0f);
            belt.AddComponent<BeltScroller>();
            foreach (float sz in new[] { -0.4f, 0.4f })
                Props.Box(_belt, new Vector3(length, 0.1f, 0.04f), Mats.Std(new Color(0.95f, 0.8f, 0.1f), 0.5f), new Vector3(cx, 0.88f, BeltStart.z + sz));
            int legs = (int)(length / 2f);
            for (int i = 0; i <= legs; i++)
            {
                float lx = BeltEnd.x + i * (length / legs);
                foreach (float sz in new[] { -0.32f, 0.32f })
                    Props.Box(_belt, new Vector3(0.08f, 0.72f, 0.08f), Mats.DarkMetal(), new Vector3(lx, 0.36f, BeltStart.z + sz));
            }
            SyncConveyor();
        }

        public void SyncConveyor()
        {
            if (_belt == null) return;
            var sim = Game.Sim;
            var alive = new HashSet<int>();
            foreach (var e in sim.ConveyorQueue)
            {
                alive.Add(e.Id);
                if (_beltBoxes.ContainsKey(e.Id)) continue;
                var box = ItemKit.Build(_belt, e.Pkg, false);
                box.transform.localPosition = BeltStart + new Vector3(0, 0.15f, 0);
                _beltBoxes[e.Id] = box;
                float secs = Mathf.Max(0.5f, (e.DoneAt - sim.BClock()) / GameData.MinutesPerSecond);
                Anim.MoveLocal(box.transform, BeltEnd + new Vector3(0, 0.15f, 0), secs);
            }
            var dead = new List<int>();
            foreach (var kv in _beltBoxes)
                if (!alive.Contains(kv.Key)) dead.Add(kv.Key);
            foreach (int id in dead)
            {
                if (_beltBoxes[id] != null) Destroy(_beltBoxes[id]);
                _beltBoxes.Remove(id);
            }
        }

        // =====================================================================================
        // Personal-Figuren
        // =====================================================================================
        public void RefreshStaff()
        {
            for (int i = _staff.childCount - 1; i >= 0; i--) Destroy(_staff.GetChild(i).gameObject);
            var sim = Game.Sim;
            if (sim.LocationStage < 1) return;
            var rng = new System.Random(555);
            var roleSeen = new Dictionary<string, int>();
            foreach (var s in sim.Staff)
            {
                var role = GameData.StaffRole(s.Role);
                roleSeen.TryGetValue(s.Role, out int n);
                roleSeen[s.Role] = n + 1;
                var off = new Vector3(n * 0.8f, 0, 0);
                Vector3[] pts;
                switch (s.Role)
                {
                    case "lager":
                        pts = new[] { new Vector3(26.5f, 0, -12f), new Vector3(26f, 0, -18.5f), new Vector3(17.8f, 0, -22.6f), new Vector3(17.8f, 0, -27.4f), new Vector3(22.8f, 0, -27.4f), new Vector3(17.8f, 0, -27.4f), new Vector3(17.8f, 0, -22.6f), new Vector3(26f, 0, -18.5f) };
                        break;
                    case "packer":
                        pts = new[] { new Vector3(13f, 0, -21.3f), new Vector3(17.8f, 0, -22.6f), new Vector3(17.8f, 0, -27.4f), new Vector3(12.8f, 0, -27.4f), new Vector3(17.8f, 0, -27.4f), new Vector3(17.8f, 0, -22.6f) };
                        break;
                    case "versand":
                        pts = new[] { new Vector3(13f, 0, -18.9f), new Vector3(17.5f, 0, -18.9f), new Vector3(6.6f, 0, -17.2f), new Vector3(6.8f, 0, -14.2f), new Vector3(6.6f, 0, -17.2f) };
                        break;
                    default:
                        pts = new[] { new Vector3(2.9f, 0, -14.6f) };
                        break;
                }
                for (int i = 0; i < pts.Length; i++) pts[i] += off;
                var look = CharacterKit.RandomLook(rng);
                look.Shirt = role.Shirt.ToColor();
                look.Vest = s.Role != "social";
                var go = Props.Node(_staff, s.Name);
                var npc = go.AddComponent<NPC>();
                npc.Setup(look, pts, 1.6f);
                npc.PauseAtPoints = 0.8f;
                Label3D.Create(go.transform, s.Name + " · " + role.Name, 30f, new Color(1f, 1f, 1f, 0.9f), new Vector3(0, 2.1f, 0), true, 12f);
            }
        }

        // =====================================================================================
        // Leben auf der Straße
        // =====================================================================================
        /// <summary>Passanten: zufällige Routen, Pausen, Spawn/Despawn am Rand (siehe <see cref="StreetLife"/>).</summary>
        private void Pedestrians_()
        {
            _pedestrians.Clear();
            try
            {
                var life = gameObject.AddComponent<StreetLife>();
                life.Setup(_dynamic, transform, _pedestrians, MenuMode ? 9 : 24);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("StreetLife: " + e.Message);
            }
        }

        private float _standCheck;

        private void Update()
        {
            _trafficAcc -= Time.deltaTime;
            if (_trafficAcc <= 0f)
            {
                _trafficAcc = Random.Range(5f, 11f);
                if (!StreetFestival.BlocksTraffic && !CityTraffic.Active) SpawnCar();
            }
            _bagCheck -= Time.deltaTime;
            if (_bagCheck <= 0f)
            {
                _bagCheck = 5f;
                RefreshBags();
            }
            _standCheck -= Time.deltaTime;
            if (_standCheck <= 0f)
            {
                _standCheck = 0.25f;
                StandCustomers();
            }
        }

        /// <summary>Passanten, die am Verkaufsstand vorbeikommen, bleiben stehen und kaufen vielleicht.</summary>
        private void StandCustomers()
        {
            var sim = Game.Sim;
            if (_stand == null || sim == null || !sim.InGame || !sim.StandOpen) return;
            Vector3 sp = _stand.transform.position;
            foreach (var npc in _pedestrians)
            {
                if (npc == null || !npc.IsWalking) continue;
                Vector3 d = npc.transform.position - sp;
                d.y = 0f;
                if (d.sqrMagnitude > 2.4f * 2.4f) continue;
                if (Time.time - npc.LastStandVisit < 25f) continue;
                npc.LastStandVisit = Time.time;
                if (Random.value > 0.55f) continue;
                npc.Stop(2.6f, _stand.transform);
                if (sim.StandTryBuy(out string pid, out int price, out bool tooExpensive))
                {
                    npc.Say(GameData.StandShouts[Random.Range(0, GameData.StandShouts.Length)]);
                    SpawnFloatText(npc.transform.position + new Vector3(0, 2.2f, 0), "+" + Fmt.Money(price), new Color(0.42f, 0.88f, 0.52f));
                }
                else if (tooExpensive) npc.Say("Puh, zu teuer ...");
            }
        }

        private void SpawnCar()
        {
            Color[] colors =
            {
                new Color(0.8f, 0.1f, 0.1f), new Color(0.15f, 0.3f, 0.6f), new Color(0.9f, 0.9f, 0.9f), new Color(0.1f, 0.1f, 0.12f),
                new Color(0.55f, 0.57f, 0.6f), new Color(0.9f, 0.7f, 0.1f), new Color(0.2f, 0.5f, 0.3f),
            };
            var car = Props.Car(_dynamic, colors[Random.Range(0, colors.Length)]);
            bool east = Random.value < 0.5f;
            float z = east ? 2f : -2f;
            float fromX = east ? -110f : 110f;
            car.transform.localPosition = new Vector3(fromX, 0, z);
            car.transform.localRotation = Quaternion.Euler(0, east ? 0f : 180f, 0);
            float dur = Random.Range(14f, 18f);
            var tr = car.transform;
            Anim.Run(dur, t =>
            {
                if (tr != null) tr.localPosition = new Vector3(Mathf.Lerp(fromX, -fromX, t), 0, z);
            }, () =>
            {
                if (tr != null) Destroy(tr.gameObject);
            });
        }

        /// <summary>Lieferwagen fährt vor die aktuelle Anlieferung, hält kurz und fährt weiter.</summary>
        public void SendVan()
        {
            var stop = VanStop[Mathf.Clamp(Game.Sim.LocationStage, 0, 1)];
            string brandName = Game.Sim != null && !string.IsNullOrEmpty(Game.Sim.BrandName) ? Game.Sim.BrandName : "";
            var van = Props.Van(_dynamic, new Color(0.95f, 0.78f, 0.1f), brandName.Length > 0 ? "PaketBlitz\nfür " + brandName : "PaketBlitz",
                new Color(0.12f, 0.12f, 0.14f));
            var tr = van.transform;
            tr.localPosition = new Vector3(stop.x + 60f, 0, stop.z);
            tr.localRotation = Quaternion.Euler(0, 180, 0);
            float startX = stop.x + 60f;
            Anim.Run(3.2f, t =>
            {
                if (tr != null) tr.localPosition = new Vector3(Mathf.Lerp(startX, stop.x, t), 0, stop.z);
            }, () =>
            {
                if (tr == null) return;
                if (Game.Audio != null) Game.Audio.PlayAt("truck", tr.position, -2f);
                Anim.Run(4.5f, t =>
                {
                    if (tr != null) tr.localPosition = new Vector3(Mathf.Lerp(stop.x, stop.x - 80f, t), 0, stop.z);
                }, () =>
                {
                    if (tr != null) Destroy(tr.gameObject);
                }, Ease.InQuad, false, null, 2.2f);
            }, Ease.OutQuad);
        }

        public void SpawnFloatText(Vector3 pos, string text, Color color)
        {
            var l = Label3D.Create(_dynamic, text, 64f, color, pos, true, 0f, true, 1.6f);
            var tr = l.transform;
            Anim.Run(1.4f, t =>
            {
                if (tr == null) return;
                tr.localPosition = pos + new Vector3(0, 1.2f * Anim.Apply(Ease.OutCubic, t), 0);
                l.SetAlpha(t < 0.3f ? 1f : 1f - (t - 0.3f) / 0.7f);
            }, () =>
            {
                if (tr != null) Destroy(tr.gameObject);
            });
        }

        public Vector3 SpawnPoint()
        {
            var sim = Game.Sim;
            if (sim.StoryStage == "diner") return SpawnDiner;
            return sim.LocationStage == 0 ? SpawnGarage : SpawnWarehouse;
        }

        /// <summary>Blickrichtung beim Start (in die Garage bzw. Halle hinein).</summary>
        public float SpawnYaw()
        {
            return 180f;
        }
    }
}
