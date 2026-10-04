using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Einkaufsviertel auf der Südseite der Hauptstraße (x −60 … −125): Elektromarkt „MediaMarkd“
    /// (Einkaufswagen schieben, Preisscanner, Kasse), Tierbedarf „Fressnix“ (Tiere adoptieren, Futter,
    /// Spielzeug) und der verschlossene Klamottenladen („Demnächst – DLC“). Verwaltet außerdem den
    /// geschobenen Wagen, den Scanner in der Hand und die Haustiere in der Welt.
    /// </summary>
    public sealed class ShoppingDistrict : MonoBehaviour
    {
        public static ShoppingDistrict Instance;

        // Rect(x0, z0, Breite, Tiefe)
        public static readonly Rect ElectroRect = new Rect(-95f, -34f, 33f, 25f);
        public static readonly Rect PetRect = new Rect(-115f, -27f, 16f, 18f);
        public static readonly Rect ClothesRect = new Rect(-124f, -21f, 7f, 12f);
        /// <summary>Teleport-Punkt (Admin): Gehweg vor MediaMarkd.</summary>
        public static readonly Vector3 SpawnShops = new Vector3(-80f, 0.05f, -6.2f);

        private static readonly Color MmRed = new Color(0.86f, 0.1f, 0.12f);
        private static readonly Color FnGreen = new Color(0.18f, 0.55f, 0.3f);
        private static readonly Color FnOrange = new Color(1f, 0.62f, 0.15f);

        private Transform _static, _dyn;
        private Sim _sim;
        private bool _menu;
        private readonly List<ShopBay> _bays = new List<ShopBay>();
        private readonly List<ShopCart> _carts = new List<ShopCart>();
        private readonly Dictionary<string, PetActor> _pets = new Dictionary<string, PetActor>();
        private ShopCart _pushed;
        private GameObject _scannerHeld, _scannerOnStand;
        private Label3D _megaBoard, _megaFacade;
        private bool _inElectro, _inPet;
        private int _tagDay = -1;

        public bool PushingCart => _pushed != null;
        public bool HasScanner { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        public void Build(bool menuMode)
        {
            Instance = this;
            _menu = menuMode;
            _sim = Game.Sim;
            _static = Props.Node(transform, "ShopsStatic").transform;
            _dyn = Props.Node(transform, "ShopsDynamic").transform;
            try
            {
                Forecourt();
                Electro();
                PetShop();
                Clothes();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            var batch = new List<GameObject>();
            foreach (var mf in _static.GetComponentsInChildren<MeshFilter>(true))
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable) batch.Add(mf.gameObject);
            if (batch.Count > 0) StaticBatchingUtility.Combine(batch.ToArray(), _static.gameObject);
            if (_sim != null && !menuMode)
            {
                _sim.ShopChanged += OnShopChanged;
                _sim.PetsChanged += SyncPets;
                _sim.DayStarted += OnDayStarted;
                SyncPets();
            }
            RefreshTags();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_scannerHeld != null) Destroy(_scannerHeld);
            if (_sim == null) return;
            _sim.ShopChanged -= OnShopChanged;
            _sim.PetsChanged -= SyncPets;
            _sim.DayStarted -= OnDayStarted;
        }

        // =====================================================================================
        // Bau-Helfer
        // =====================================================================================
        /// <summary>Achsparallele Wand mit Öffnungen (x = von, y = bis, z = Höhe der Öffnung).</summary>
        private void Wall(Vector3 a, Vector3 b, float height, float thick, Material mat, params Vector3[] openings)
        {
            bool alongX = Mathf.Abs(a.z - b.z) < 0.01f;
            float start = alongX ? Mathf.Min(a.x, b.x) : Mathf.Min(a.z, b.z);
            float stop = alongX ? Mathf.Max(a.x, b.x) : Mathf.Max(a.z, b.z);
            var cuts = new List<Vector3>(openings ?? new Vector3[0]);
            cuts.Sort((p, q) => p.x.CompareTo(q.x));
            float cursor = start;
            foreach (var o in cuts)
            {
                if (o.x > cursor) Seg(alongX, a, cursor, o.x, 0f, height, thick, mat);
                Seg(alongX, a, o.x, o.y, o.z, height, thick, mat);
                cursor = o.y;
            }
            if (cursor < stop) Seg(alongX, a, cursor, stop, 0f, height, thick, mat);
        }

        private void Seg(bool alongX, Vector3 a, float from, float to, float y0, float y1, float thick, Material mat)
        {
            float len = to - from, h = y1 - y0;
            if (len <= 0.01f || h <= 0.01f) return;
            float mid = (from + to) / 2f;
            if (alongX) Props.Solid(_static, new Vector3(len, h, thick), mat, new Vector3(mid, y0 + h / 2f, a.z), default, 0.015f);
            else Props.Solid(_static, new Vector3(thick, h, len), mat, new Vector3(a.x, y0 + h / 2f, mid), default, 0.015f);
        }

        /// <summary>Glasfront entlang X: Sockel, Glas (mit Kollision), Band darüber in Fassadenfarbe.</summary>
        private void GlassFront(float x0, float x1, float z, float glassTop, float height, Material band, float doorFrom, float doorTo, float doorH)
        {
            var sockel = Mats.Std(new Color(0.25f, 0.26f, 0.28f), 0.6f);
            foreach (var r in new[] { new Vector2(x0, doorFrom), new Vector2(doorTo, x1) })
            {
                float len = r.y - r.x;
                if (len <= 0.05f) continue;
                float mid = (r.x + r.y) / 2f;
                Props.Solid(_static, new Vector3(len, 0.5f, 0.3f), sockel, new Vector3(mid, 0.25f, z));
                Props.Box(_static, new Vector3(len, glassTop - 0.5f, 0.04f), Mats.Window(false), new Vector3(mid, 0.5f + (glassTop - 0.5f) / 2f, z + 0.02f), default, 0f, false);
                Props.Collider(_static, new Vector3(len, glassTop - 0.5f, 0.2f), new Vector3(mid, 0.5f + (glassTop - 0.5f) / 2f, z));
                for (float x = r.x + 2.5f; x < r.y - 0.5f; x += 2.5f)
                    Props.Box(_static, new Vector3(0.08f, glassTop - 0.5f, 0.12f), Mats.DarkMetal(), new Vector3(x, 0.5f + (glassTop - 0.5f) / 2f, z));
            }
            Props.Solid(_static, new Vector3(x1 - x0, height - glassTop, 0.3f), band, new Vector3((x0 + x1) / 2f, glassTop + (height - glassTop) / 2f, z));
            // Türrahmen
            Props.Box(_static, new Vector3(doorTo - doorFrom + 0.2f, 0.12f, 0.32f), Mats.DarkMetal(), new Vector3((doorFrom + doorTo) / 2f, doorH, z));
        }

        private void Roof(Rect r, float h)
        {
            Props.Box(_static, new Vector3(r.width + 0.4f, 0.3f, r.height + 0.4f), Mats.RoofMat(new Color(0.36f, 0.37f, 0.4f)),
                new Vector3(r.x + r.width / 2f, h + 0.15f, r.y + r.height / 2f), default, 0.03f);
        }

        private void FloorOf(Rect r, Material m)
        {
            Props.Box(_static, new Vector3(r.width - 0.2f, 0.025f, r.height - 0.2f), m, new Vector3(r.x + r.width / 2f, 0.02f, r.y + r.height / 2f), default, 0f, false);
        }

        private void Lamp(Vector3 pos, float len, float energy, float range)
        {
            var fix = Props.CeilingLight(_static, len);
            fix.transform.localPosition = pos;
            Props.PointLight(transform, pos - new Vector3(0f, 0.9f, 0f), new Color(1f, 0.97f, 0.92f), energy, range);
        }

        /// <summary>Knoten mit Front = lokal +Z.</summary>
        private Transform NodeAt(Transform parent, string name, Vector3 pos, float rotY) => Props.Node(parent, name, pos, rotY).transform;

        /// <summary>Regalgestell (Front +Z), gibt die Höhen der Böden zurück.</summary>
        private static float[] ShelfFrame(Transform t, float w, float h, Color color)
        {
            var mat = Mats.Std(color, 0.6f);
            var board = Mats.Std(new Color(0.92f, 0.92f, 0.9f), 0.5f);
            Props.Box(t, new Vector3(w, h, 0.05f), mat, new Vector3(0f, h / 2f, -0.25f), default, 0.01f);
            foreach (float x in new[] { -w / 2f, w / 2f }) Props.Box(t, new Vector3(0.05f, h, 0.55f), mat, new Vector3(x, h / 2f, 0f), default, 0.01f);
            float[] ys = { 0.12f, 0.6f, 1.08f, 1.56f };
            foreach (float y in ys) Props.Box(t, new Vector3(w - 0.05f, 0.04f, 0.5f), board, new Vector3(0f, y, 0.02f), default, 0.005f);
            return ys;
        }

        // =====================================================================================
        // Vorplatz
        // =====================================================================================
        private void Forecourt()
        {
            var paving = Mats.Sidewalk(new Color(0.7f, 0.68f, 0.64f), new Color(0.6f, 0.58f, 0.55f));
            Props.Box(_static, new Vector3(65f, 0.03f, 2.3f), paving, new Vector3(-92.5f, 0.013f, -8.15f), default, 0f, false);
            Props.Box(_static, new Vector3(4f, 0.03f, 25f), paving, new Vector3(-97f, 0.013f, -21.5f), default, 0f, false);
            for (int i = 0; i < 4; i++) Props.Bench(_static).transform.localPosition = new Vector3(-97f, 0f, -14f - i * 4.5f);
            Props.TrashBin(_static, MmRed).transform.localPosition = new Vector3(-75f, 0f, -7.9f);
            Props.TrashBin(_static, FnGreen).transform.localPosition = new Vector3(-104f, 0f, -7.9f);
        }

        // =====================================================================================
        // MediaMarkd
        // =====================================================================================
        private void Electro()
        {
            var r = ElectroRect;
            float x0 = r.x, x1 = r.x + r.width, z0 = r.y, z1 = r.y + r.height;
            const float h = 6f;
            var wall = Mats.Plaster(new Color(0.9f, 0.9f, 0.88f));
            var red = Mats.Std(MmRed, 0.5f);
            Wall(new Vector3(x0, 0, z0 + 0.15f), new Vector3(x1, 0, z0 + 0.15f), h, 0.3f, wall);
            Wall(new Vector3(x0 + 0.15f, 0, z0), new Vector3(x0 + 0.15f, 0, z1), h, 0.3f, wall);
            Wall(new Vector3(x1 - 0.15f, 0, z0), new Vector3(x1 - 0.15f, 0, z1), h, 0.3f, wall);
            GlassFront(x0, x1, z1 - 0.15f, 3.2f, h, red, -80f, -77f, 3.0f);
            Roof(r, h);
            FloorOf(r, Mats.Tiles(0.6f));
            Label3D.Create(transform, ShopData.ElectroName.ToUpperInvariant(), 300f, Color.white, new Vector3(-78.5f, 4.9f, z1 + 0.05f), false, 0f, false, 3f);
            Label3D.Create(transform, ShopData.ElectroSlogan, 90f, new Color(1f, 0.9f, 0.3f), new Vector3(-78.5f, 3.75f, z1 + 0.05f), false, 0f, false, 2f);
            _megaFacade = Label3D.Create(transform, "", 70f, Color.white, new Vector3(-88f, 3.75f, z1 + 0.05f), false, 0f, false, 2f);
            Props.PointLight(transform, new Vector3(-78.5f, 5.2f, z1 + 1.5f), new Color(1f, 0.4f, 0.35f), 3f, 9f);

            foreach (float lx in new[] { -88f, -72f })
            foreach (float lz in new[] { -15f, -27f })
                Lamp(new Vector3(lx, h - 0.1f, lz), 3f, 3.2f, 16f);

            // Schilder
            var aisle = Props.SignBoard(transform, "GADGETS & LICHT", MmRed, Color.white, new Vector2(4f, 0.6f));
            aisle.transform.localPosition = new Vector3(-85f, 3.3f, -17.6f);
            var aisle2 = Props.SignBoard(transform, "AUDIO, VIDEO & DROHNEN", MmRed, Color.white, new Vector2(4.6f, 0.6f));
            aisle2.transform.localPosition = new Vector3(-85f, 3.3f, -24.1f);

            // Knaller-Tafel
            var board = NodeAt(transform, "MegaBoard", new Vector3(-78.5f, 0f, -14.6f), 0f);
            Props.Box(board, new Vector3(4.2f, 1.6f, 0.1f), Mats.Std(new Color(0.08f, 0.08f, 0.1f), 0.4f), new Vector3(0f, 3.6f, 0f), default, 0.02f);
            Props.Box(board, new Vector3(4.2f, 0.4f, 0.12f), red, new Vector3(0f, 4.6f, 0f), default, 0.02f);
            Label3D.Create(board, "KNALLER DES TAGES", 70f, Color.white, new Vector3(0f, 4.6f, 0.08f), false, 0f, false, 2f);
            _megaBoard = Label3D.Create(board, "", 54f, new Color(1f, 0.85f, 0.2f), new Vector3(0f, 3.55f, 0.07f), false, 0f, false, 2.4f, 30);
            foreach (float bx in new[] { -1.9f, 1.9f }) Props.Box(board, new Vector3(0.05f, 2f, 0.05f), Mats.DarkMetal(), new Vector3(bx, 5f, 0f));

            // Deko: TV-Wand (Ostwand) und Weiße Ware hinten
            var tvWall = NodeAt(transform, "TvWall", new Vector3(x1 - 0.35f, 0f, -24f), -90f);
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 2; j++)
            {
                var c = Color.HSVToRGB((i * 0.21f + j * 0.37f) % 1f, 0.6f, 1f);
                Props.Box(tvWall, new Vector3(1.6f, 0.95f, 0.08f), Mats.Std(new Color(0.05f, 0.05f, 0.06f), 0.3f), new Vector3(-3.3f + i * 2.2f, 1.5f + j * 1.3f, 0f), default, 0.02f);
                Props.Box(tvWall, new Vector3(1.48f, 0.83f, 0.01f), Mats.Emit(c, 1.2f), new Vector3(-3.3f + i * 2.2f, 1.5f + j * 1.3f, 0.045f), default, 0f, false);
            }
            Label3D.Create(tvWall, "TV & HIFI – nur zum Angucken", 60f, MmRed, new Vector3(0f, 3.4f, 0.06f), false);
            var white = Mats.Std(new Color(0.95f, 0.95f, 0.96f), 0.3f);
            for (int i = 0; i < 6; i++)
            {
                var m = NodeAt(_static, "Washer", new Vector3(-90f + i * 1.1f, 0f, z0 + 0.8f), 0f);
                Props.Solid(m, new Vector3(0.95f, 0.95f, 0.7f), white, new Vector3(0f, 0.475f, 0f), default, 0.03f);
                Props.Cyl(m, 0.28f, 0.28f, 0.03f, Mats.Std(new Color(0.3f, 0.35f, 0.42f), 0.2f, 0.4f), new Vector3(0f, 0.45f, 0.36f), new Vector3(90, 0, 0));
            }
            Label3D.Create(transform, "WEISSE WARE · ab Level 99", 60f, MmRed, new Vector3(-87.2f, 1.6f, z0 + 0.32f), false);

            if (_menu) return;

            // Regale: Reihe A (z −19) und B (z −25.5), je 4 Fächer, Front +Z
            var ware = ShopData.ItemsOf(ShopData.Electro);
            for (int i = 0; i < ware.Count && i < 8; i++)
            {
                float x = -88.6f + (i % 4) * 2.4f;
                float z = i < 4 ? -19f : -25.5f;
                Bay(ware[i], new Vector3(x, 0f, z), 0f, new Color(0.25f, 0.27f, 0.32f));
            }
            // Rückseiten der Gondeln (Deko)
            foreach (float z in new[] { -19.6f, -26.1f })
            {
                var back = NodeAt(_static, "GondolaBack", new Vector3(-84.99f, 0f, z), 180f);
                ShelfFrame(back, 9.6f, 1.8f, new Color(0.25f, 0.27f, 0.32f));
                for (int k = 0; k < 18; k++)
                    Props.Box(back, new Vector3(0.35f, 0.3f, 0.3f), Mats.Std(Color.HSVToRGB(k * 0.13f % 1f, 0.35f, 0.85f), 0.6f),
                        new Vector3(-4.4f + (k % 9) * 1.1f, 0.77f + (k / 9) * 0.48f, 0.02f), default, 0.01f);
            }

            // Einkaufswagen am Eingang
            for (int i = 0; i < 3; i++)
            {
                var home = new Vector3(-83.6f - i * 0.75f, 0f, -10.6f);
                var go = new GameObject("ShopCart");
                go.transform.SetParent(_dyn, false);
                go.transform.position = home;
                go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                var cart = go.AddComponent<ShopCart>();
                cart.Setup(home, 180f);
                _carts.Add(cart);
            }
            Label3D.Create(transform, "WAGEN", 50f, MmRed, new Vector3(-84.4f, 1.6f, -10.0f), true, 8f);

            // Preisscanner-Ständer
            var sNode = NodeAt(_dyn, "ScannerStand", new Vector3(-75.2f, 0f, -10.9f), 180f);
            Props.Box(sNode, new Vector3(0.12f, 1.1f, 0.12f), Mats.DarkMetal(), new Vector3(0f, 0.55f, 0f));
            Props.Box(sNode, new Vector3(0.5f, 0.06f, 0.35f), red, new Vector3(0f, 1.12f, 0f), default, 0.01f);
            _scannerOnStand = ScannerModel(sNode, new Vector3(0f, 1.2f, 0f));
            Label3D.Create(sNode, "PREISSCANNER", 40f, Color.white, new Vector3(0f, 1.55f, 0f), true, 6f);
            sNode.gameObject.AddComponent<ShopScannerStand>().Setup();

            // Kassen
            foreach (float kx in new[] { -71f, -66.5f })
            {
                var k = NodeAt(_dyn, "Checkout", new Vector3(kx, 0f, -12.6f), 90f);
                Props.Box(k, new Vector3(2.6f, 0.95f, 0.8f), Mats.Std(new Color(0.2f, 0.21f, 0.24f), 0.5f), new Vector3(0f, 0.475f, 0f), default, 0.02f);
                Props.Box(k, new Vector3(2.6f, 0.04f, 0.82f), Mats.Std(new Color(0.12f, 0.12f, 0.13f), 0.3f), new Vector3(0f, 0.97f, 0f), default, 0.005f);
                if (Props.AssetAt(k, "logistics.cash_register", new Vector3(0.7f, 0.99f, -0.1f), 0f, 0.5f) == null)
                    Props.Box(k, new Vector3(0.4f, 0.3f, 0.35f), Mats.Std(new Color(0.1f, 0.1f, 0.1f), 0.4f), new Vector3(0.7f, 1.14f, -0.1f));
                var cashier = NodeAt(k, "Cashier", new Vector3(0.6f, 0f, -0.95f), 0f);
                Staffer(cashier, MmRed, 11);
                k.gameObject.AddComponent<ShopCheckoutDesk>().Setup(ShopData.Electro, new Vector3(2.6f, 1.1f, 0.9f), new Vector3(0f, 0.55f, 0f));
                var sign = Props.SignBoard(transform, "KASSE", MmRed, Color.white, new Vector2(1.4f, 0.45f));
                sign.transform.localPosition = new Vector3(kx, 2.8f, -12.6f);
            }
        }

        /// <summary>Kleines Handscanner-Modell.</summary>
        private static GameObject ScannerModel(Transform parent, Vector3 pos)
        {
            var n = Props.Node(parent, "Scanner", pos);
            var t = n.transform;
            Props.Box(t, new Vector3(0.07f, 0.06f, 0.2f), Mats.Std(new Color(0.15f, 0.15f, 0.17f), 0.4f), new Vector3(0f, 0.04f, 0f), default, 0.01f, false);
            Props.Box(t, new Vector3(0.05f, 0.12f, 0.05f), Mats.Std(new Color(0.85f, 0.65f, 0.1f), 0.5f), new Vector3(0f, -0.03f, -0.06f), new Vector3(15, 0, 0), 0.01f, false);
            Props.Box(t, new Vector3(0.05f, 0.002f, 0.06f), Mats.Emit(new Color(0.3f, 1f, 0.5f), 1.5f), new Vector3(0f, 0.071f, -0.02f), default, 0f, false);
            Props.Box(t, new Vector3(0.06f, 0.03f, 0.005f), Mats.Emit(new Color(1f, 0.15f, 0.1f), 2f), new Vector3(0f, 0.04f, 0.101f), default, 0f, false);
            return n;
        }

        /// <summary>Verkäufer:in hinter der Theke (Modell oder prozedural), Blick +Z.</summary>
        private static void Staffer(Transform parent, Color shirt, int seed)
        {
            var look = new Look { Shirt = shirt, Seed = seed, Skin = CharacterKit.SkinTones[seed % 4], Hair = CharacterKit.HairColors[seed % 5] };
            if (CharacterKit.BuildModel(parent, look) == null) CharacterKit.Build(parent, look);
        }

        private void Bay(StoreItemDef it, Vector3 pos, float rotY, Color frame)
        {
            var t = NodeAt(_dyn, "Bay " + it.Id, pos, rotY);
            float[] ys = ShelfFrame(t, 2.3f, 1.8f, frame);
            var col = it.Color.ToColor();
            for (int b = 1; b < ys.Length; b++)
            {
                int per = it.Kind == "ware" && GameData.IsProduct(it.Product) && GameData.Product(it.Product).Size == 2 ? 3 : 4;
                for (int k = 0; k < per; k++)
                {
                    float x = -0.85f + k * (1.7f / Mathf.Max(1, per - 1));
                    var p = new Vector3(x, ys[b] + 0.02f, 0.05f);
                    switch (it.Kind)
                    {
                        case "ware":
                        {
                            var item = ItemKit.Build(t, new ItemData { Kind = ItemKind.Item, Product = it.Product }, false);
                            float sc = per == 3 ? 2.1f : 1.6f;
                            item.transform.localScale = Vector3.one * sc;
                            item.transform.localPosition = p + new Vector3(0f, ItemKit.ItemSize.y * sc / 2f, 0f);
                            break;
                        }
                        case "futter":
                            Props.Box(t, new Vector3(0.32f, 0.4f, 0.16f), Mats.Std(col, 0.7f), p + new Vector3(0f, 0.2f, 0f), new Vector3(0, (k * 7) % 11 - 5, 0), 0.03f);
                            Props.Box(t, new Vector3(0.2f, 0.12f, 0.005f), Mats.Std(Color.white, 0.6f), p + new Vector3(0f, 0.22f, 0.083f), default, 0f, false);
                            break;
                        case "gadget":
                            if (it.Id == "fn_ball") Props.Sphere(t, 0.09f, Mats.Std(col, 0.4f), p + new Vector3(0f, 0.09f, 0f));
                            else if (it.Id == "fn_bett") Props.Cyl(t, 0.25f, 0.22f, 0.12f, Mats.Std(col, 0.9f), p + new Vector3(0f, 0.06f, 0f));
                            else if (it.Id == "fn_halsband") Props.Cyl(t, 0.1f, 0.1f, 0.03f, Mats.Emit(col, 1.2f), p + new Vector3(0f, 0.02f, 0f));
                            else Props.Box(t, new Vector3(0.25f, 0.35f, 0.25f), Mats.Std(col, 0.8f), p + new Vector3(0f, 0.175f, 0f), default, 0.02f);
                            break;
                    }
                }
            }
            if (it.Id == "fn_kratzbaum")
            {
                var tree = NodeAt(t, "Kratzbaum", new Vector3(0f, 0f, 0.75f), 0f);
                var sisal = Mats.Std(new Color(0.78f, 0.68f, 0.5f), 0.95f);
                Props.Box(tree, new Vector3(0.6f, 0.06f, 0.6f), Mats.Std(col, 0.9f), new Vector3(0f, 0.03f, 0f));
                Props.Cyl(tree, 0.07f, 0.07f, 1.2f, sisal, new Vector3(0f, 0.6f, 0f));
                Props.Box(tree, new Vector3(0.45f, 0.06f, 0.45f), Mats.Std(col, 0.9f), new Vector3(0f, 1.2f, 0f));
            }
            // Kopfschild + Preisschild
            Props.Box(t, new Vector3(2.3f, 0.32f, 0.05f), Mats.Std(it.Store == ShopData.Electro ? MmRed : FnGreen, 0.5f), new Vector3(0f, 2.0f, -0.2f), default, 0.01f);
            Label3D.Create(t, it.Name, 46f, Color.white, new Vector3(0f, 2.0f, -0.17f), false, 0f, false, 1.4f);
            var tag = Label3D.Create(t, "", 40f, new Color(1f, 0.95f, 0.2f), new Vector3(0f, 0.38f, 0.32f), false, 10f, false, 2f);
            Props.Box(t, new Vector3(1.3f, 0.18f, 0.02f), Mats.Std(new Color(0.1f, 0.1f, 0.12f), 0.5f), new Vector3(0f, 0.38f, 0.3f), default, 0f, false);
            var bay = t.gameObject.AddComponent<ShopBay>();
            bay.Tag = tag;
            bay.Setup(it.Id, new Vector3(2.3f, 2.1f, 0.8f), new Vector3(0f, 1.05f, 0.1f));
            _bays.Add(bay);
        }

        // =====================================================================================
        // Fressnix
        // =====================================================================================
        private void PetShop()
        {
            var r = PetRect;
            float x0 = r.x, x1 = r.x + r.width, z0 = r.y, z1 = r.y + r.height;
            const float h = 4.5f;
            var wall = Mats.Plaster(new Color(0.95f, 0.93f, 0.85f));
            var green = Mats.Std(FnGreen, 0.55f);
            Wall(new Vector3(x0, 0, z0 + 0.15f), new Vector3(x1, 0, z0 + 0.15f), h, 0.3f, wall);
            Wall(new Vector3(x0 + 0.15f, 0, z0), new Vector3(x0 + 0.15f, 0, z1), h, 0.3f, wall);
            Wall(new Vector3(x1 - 0.15f, 0, z0), new Vector3(x1 - 0.15f, 0, z1), h, 0.3f, wall);
            GlassFront(x0, x1, z1 - 0.15f, 2.8f, h, green, -108.5f, -106.5f, 2.6f);
            Roof(r, h);
            FloorOf(r, Mats.WoodFloor(new Color(0.65f, 0.5f, 0.35f)));
            Label3D.Create(transform, ShopData.PetShopName.ToUpperInvariant(), 220f, FnOrange, new Vector3(-107f, 3.75f, z1 + 0.05f), false, 0f, false, 3f);
            Label3D.Create(transform, ShopData.PetShopSlogan, 60f, Color.white, new Vector3(-107f, 3.05f, z1 + 0.05f), false, 0f, false, 1.6f);
            Props.PointLight(transform, new Vector3(-107f, 4.2f, z1 + 1.4f), new Color(1f, 0.75f, 0.4f), 2.2f, 7f);
            Lamp(new Vector3(-107f, h - 0.1f, -14f), 2f, 2.6f, 11f);
            Lamp(new Vector3(-107f, h - 0.1f, -22f), 2f, 2.6f, 11f);
            // Pfotenabdrücke am Boden
            var paw = Mats.Std(new Color(0.35f, 0.25f, 0.18f), 0.8f);
            for (int i = 0; i < 6; i++) Props.Box(_static, new Vector3(0.18f, 0.005f, 0.2f), paw, new Vector3(-107.5f + (i % 2) * 0.35f, 0.035f, -10f - i * 0.8f), default, 0f, false);

            if (_menu) return;

            // Tierheim-Ecke (Westwand), Tiere zum Adoptieren
            var corner = Props.SignBoard(transform, "TIERHEIM-ECKE · ADOPTIEREN", FnOrange, Color.white, new Vector2(4f, 0.5f));
            corner.transform.localPosition = new Vector3(x0 + 0.35f, 2.9f, -19.5f);
            corner.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var pens = new[] { "fn_hund", "fn_katze" };
            for (int i = 0; i < pens.Length; i++)
            {
                var it = ShopData.Item(pens[i]);
                var pos = new Vector3(x0 + 1.4f, 0f, -21.8f + i * 4.2f);
                var pen = NodeAt(_dyn, "Pen " + it.Id, pos, 90f);
                var fence = Mats.Std(new Color(0.85f, 0.75f, 0.55f), 0.8f);
                Props.Box(pen, new Vector3(2.6f, 0.04f, 1.8f), Mats.Std(new Color(0.55f, 0.75f, 0.45f), 0.95f), new Vector3(0f, 0.04f, 0f), default, 0f, false);
                Props.Box(pen, new Vector3(2.6f, 0.7f, 0.05f), fence, new Vector3(0f, 0.35f, 0.9f));
                foreach (float sx in new[] { -1.3f, 1.3f }) Props.Box(pen, new Vector3(0.05f, 0.7f, 1.8f), fence, new Vector3(sx, 0.35f, 0f));
                var actor = PetActor.Spawn(pen, it.Product, pen.TransformPoint(new Vector3(0.2f, 0f, 0f)), 90f + 180f + 90f, true);
                actor.transform.localRotation = Quaternion.identity;
                var tag = Label3D.Create(pen, "", 44f, Color.white, new Vector3(0f, 1.25f, 0.9f), true, 9f);
                var bay = pen.gameObject.AddComponent<ShopBay>();
                bay.Tag = tag;
                bay.Setup(it.Id, new Vector3(2.6f, 1.2f, 2f), new Vector3(0f, 0.6f, 0f));
                _bays.Add(bay);
            }

            // Regale: Ostwand (Front −X) und Rückwand (Front +Z)
            var east = new[] { "fn_futter", "fn_premium", "fn_brunnen", "fn_ball" };
            for (int i = 0; i < east.Length; i++)
                Bay(ShopData.Item(east[i]), new Vector3(x1 - 0.6f, 0f, -15.4f - i * 2.5f), -90f, new Color(0.3f, 0.5f, 0.35f));
            var back = new[] { "fn_kratzbaum", "fn_halsband", "fn_bett" };
            for (int i = 0; i < back.Length; i++)
                Bay(ShopData.Item(back[i]), new Vector3(-108.5f + i * 2.5f, 0f, z0 + 0.6f), 0f, new Color(0.3f, 0.5f, 0.35f));

            // Kasse
            var k = NodeAt(_dyn, "PetCheckout", new Vector3(-102.3f, 0f, -12.2f), 0f);
            Props.Box(k, new Vector3(2.4f, 0.95f, 0.7f), Mats.Std(FnGreen, 0.6f), new Vector3(0f, 0.475f, 0f), default, 0.02f);
            Props.Box(k, new Vector3(2.5f, 0.05f, 0.8f), Mats.Planks(new Color(0.7f, 0.52f, 0.33f), 0.15f), new Vector3(0f, 0.975f, 0f), default, 0.005f);
            if (Props.AssetAt(k, "logistics.cash_register", new Vector3(-0.6f, 1f, -0.05f), 180f, 0.45f) == null)
                Props.Box(k, new Vector3(0.4f, 0.3f, 0.35f), Mats.Std(new Color(0.1f, 0.1f, 0.1f), 0.4f), new Vector3(-0.6f, 1.15f, -0.05f));
            var cashier = NodeAt(k, "Cashier", new Vector3(0.2f, 0f, -0.85f), 0f);
            Staffer(cashier, FnGreen, 23);
            k.gameObject.AddComponent<ShopCheckoutDesk>().Setup(ShopData.PetShop, new Vector3(2.5f, 1.1f, 0.9f), new Vector3(0f, 0.55f, 0f));
            var ks = Props.SignBoard(transform, "KASSE", FnOrange, Color.white, new Vector2(1.2f, 0.4f));
            ks.transform.localPosition = new Vector3(-102.3f, 2.6f, -12.6f);
        }

        // =====================================================================================
        // Klamottenladen (verschlossen, DLC)
        // =====================================================================================
        private void Clothes()
        {
            var r = ClothesRect;
            float x0 = r.x, x1 = r.x + r.width, z0 = r.y, z1 = r.y + r.height;
            const float h = 4.2f;
            var wall = Mats.Brick(new Color(0.45f, 0.3f, 0.5f), new Color(0.8f, 0.78f, 0.75f));
            Wall(new Vector3(x0, 0, z0 + 0.15f), new Vector3(x1, 0, z0 + 0.15f), h, 0.3f, wall);
            Wall(new Vector3(x0 + 0.15f, 0, z0), new Vector3(x0 + 0.15f, 0, z1), h, 0.3f, wall);
            Wall(new Vector3(x1 - 0.15f, 0, z0), new Vector3(x1 - 0.15f, 0, z1), h, 0.3f, wall);
            Wall(new Vector3(x0, 0, z1 - 0.15f), new Vector3(x1, 0, z1 - 0.15f), h, 0.3f, wall);
            Roof(r, h);
            var paper = Mats.Std(new Color(0.93f, 0.88f, 0.75f), 0.95f);
            Props.Box(_static, new Vector3(2.2f, 1.6f, 0.03f), paper, new Vector3(-122.4f, 1.6f, z1 + 0.02f), default, 0f, false);
            Label3D.Create(transform, "DEMNÄCHST", 60f, new Color(0.6f, 0.1f, 0.5f), new Vector3(-122.4f, 1.7f, z1 + 0.04f), false);
            Label3D.Create(transform, "(als DLC)", 40f, new Color(0.3f, 0.3f, 0.3f), new Vector3(-122.4f, 1.3f, z1 + 0.04f), false);
            Label3D.Create(transform, ShopData.ClothesName.ToUpperInvariant(), 120f, new Color(1f, 0.85f, 0.95f), new Vector3((x0 + x1) / 2f, 3.5f, z1 + 0.05f), false, 0f, false, 2f);
            // Tür + Brett davor
            var door = NodeAt(_dyn, "LockedDoor", new Vector3(-119.4f, 0f, z1 + 0.02f), 0f);
            Props.Box(door, new Vector3(1.2f, 2.3f, 0.08f), Mats.Std(new Color(0.25f, 0.18f, 0.12f), 0.7f), new Vector3(0f, 1.15f, 0f), default, 0.02f);
            Props.Box(door, new Vector3(1.6f, 0.2f, 0.05f), Mats.Planks(new Color(0.6f, 0.45f, 0.28f), 0.2f), new Vector3(0f, 1.3f, 0.07f), new Vector3(0, 0, 18f), 0.01f);
            Props.Box(door, new Vector3(0.7f, 0.4f, 0.02f), Mats.Std(new Color(0.9f, 0.15f, 0.15f), 0.5f), new Vector3(0f, 1.8f, 0.08f), default, 0f, false);
            Label3D.Create(door, "ZU", 60f, Color.white, new Vector3(0f, 1.8f, 0.1f), false);
            door.gameObject.AddComponent<LockedShopDoor>().Setup(new Vector3(1.4f, 2.4f, 0.4f), new Vector3(0f, 1.2f, 0.1f));
        }

        // =====================================================================================
        // Laufzeit: Wagen, Scanner, Läden betreten/verlassen, Tiere
        // =====================================================================================
        public void StartPushing(ShopCart cart)
        {
            if (cart == null || _pushed != null) return;
            _pushed = cart;
            cart.SetPushed(true);
            cart.RefreshContent();
            Game.Sound("metal_clank", 0.1f, -10f);
        }

        public void ReleaseCart(bool toHome)
        {
            if (_pushed == null) return;
            var c = _pushed;
            _pushed = null;
            c.SetPushed(false);
            if (toHome) c.ReturnHome();
            c.RefreshContent();
        }

        public void SetScanner(bool on)
        {
            if (HasScanner == on) return;
            HasScanner = on;
            if (_scannerOnStand != null) _scannerOnStand.SetActive(!on);
            if (_scannerHeld != null) Destroy(_scannerHeld);
            _scannerHeld = null;
            var player = Game.Player;
            if (on && player != null && player.Cam != null)
            {
                _scannerHeld = ScannerModel(player.Cam.transform, new Vector3(-0.28f, -0.3f, 0.5f));
                _scannerHeld.transform.localRotation = Quaternion.Euler(-8f, 12f, 0f);
                foreach (var rr in _scannerHeld.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Game.Sound(on ? "scanner" : "place", 0.05f, -6f);
        }

        private static bool Inside(Rect r, Vector3 p) => p.x > r.x + 0.1f && p.x < r.x + r.width - 0.1f && p.z > r.y + 0.1f && p.z < r.y + r.height - 0.1f;

        private void Update()
        {
            if (_menu || _sim == null) return;
            var player = Game.Player;
            if (player == null) return;
            var pos = player.transform.position;
            bool inE = Inside(ElectroRect, pos), inP = Inside(PetRect, pos);
            if (_inElectro && !inE) LeaveStore(ShopData.Electro);
            if (_inPet && !inP) LeaveStore(ShopData.PetShop);
            _inElectro = inE;
            _inPet = inP;

            if (_pushed != null)
            {
                bool locked = Game.Root != null && Game.Root.InputLocked;
                if (!locked && GameInput.DropDown && player.Held == null)
                {
                    ReleaseCart(false);
                    return;
                }
                if (player.Held != null)
                {
                    ReleaseCart(false);
                    return;
                }
                var fwd = player.transform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
                fwd.Normalize();
                float dist = 1.0f;
                if (Physics.Raycast(pos + Vector3.up * 0.5f, fwd, out var hit, 1.5f, ~(1 << 2), QueryTriggerInteraction.Ignore))
                    dist = Mathf.Clamp(hit.distance - 0.5f, 0.35f, 1.0f);
                var target = pos + fwd * dist;
                target.y = 0f;
                var t = _pushed.transform;
                t.position = Vector3.Lerp(t.position, target, Mathf.Min(1f, Time.deltaTime * 14f));
                t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(fwd, Vector3.up), Mathf.Min(1f, Time.deltaTime * 10f));
            }
            if (_tagDay != _sim.Day) RefreshTags();
        }

        private void LeaveStore(string store)
        {
            if (_sim.CartCount(store) > 0)
            {
                _sim.CartClear(store);
                Game.Notify("Piep piep! Unbezahlte Ware – alles zurück ins Regal.", "bad");
                Game.Sound("error", 0.05f, -2f);
            }
            if (store == ShopData.Electro)
            {
                if (_pushed != null)
                {
                    ReleaseCart(true);
                    Game.Notify("Der Wagen bleibt im Laden.", "info");
                }
                if (HasScanner)
                {
                    SetScanner(false);
                    Game.Notify("Den Preisscanner hast du am Eingang zurückgelegt.", "info");
                }
            }
        }

        private void OnShopChanged()
        {
            if (_pushed != null) _pushed.RefreshContent();
            RefreshTags();
        }

        private void OnDayStarted(int day)
        {
            ReleaseCart(true);
            foreach (var c in _carts) if (c != null) c.ReturnHome();
            SetScanner(false);
            RefreshTags();
        }

        /// <summary>Preisschilder (Aktion / Preis / ausverkauft) und Knaller-Tafeln.</summary>
        private void RefreshTags()
        {
            var sim = _sim;
            if (sim == null) return;
            _tagDay = sim.Day;
            foreach (var b in _bays)
            {
                if (b == null || b.Tag == null) continue;
                var it = ShopData.Item(b.ItemId);
                if (it == null) continue;
                var deal = sim.ShopDealFor(it.Id);
                string text;
                if (sim.ShopLeft(it.Id) <= 0 && sim.CartQty(it.Id) == 0) text = "AUSVERKAUFT";
                else if (it.Store == ShopData.Electro)
                    text = deal != null ? "AKTION −" + Mathf.RoundToInt(deal.Discount * 100f) + " %" : "Preis: scannen!";
                else text = (deal != null ? "AKTION " : "") + Fmt.Money(sim.ShopPrice(it.Id));
                b.Tag.SetText(text);
                b.Tag.SetColor(deal != null ? new Color(1f, 0.35f, 0.25f) : new Color(1f, 0.95f, 0.6f));
            }
            var mega = sim.ShopDealsToday(ShopData.Electro);
            string megaText = "";
            if (mega.Count > 0)
            {
                var it = ShopData.Item(mega[0].ItemId);
                if (it != null) megaText = it.Name + "\n−" + Mathf.RoundToInt(mega[0].Discount * 100f) + " % · nur " + mega[0].Limit + "× !";
            }
            if (_megaBoard != null) _megaBoard.SetText(megaText);
            if (_megaFacade != null) _megaFacade.SetText(megaText.Replace("\n", " "));
        }

        private void SyncPets()
        {
            if (_sim == null || _menu) return;
            var want = new HashSet<string>();
            foreach (var p in _sim.Pets) want.Add(p.Species);
            var remove = new List<string>();
            foreach (var kv in _pets)
                if (!want.Contains(kv.Key) || kv.Value == null) remove.Add(kv.Key);
            foreach (var k in remove)
            {
                if (_pets[k] != null) Destroy(_pets[k].gameObject);
                _pets.Remove(k);
            }
            var parent = Game.World != null ? Game.World.transform : transform;
            foreach (var p in _sim.Pets)
            {
                if (_pets.ContainsKey(p.Species)) continue;
                Vector3 pos;
                var player = Game.Player;
                if (p.Follow && player != null) pos = player.transform.position - player.transform.forward * 1.3f;
                else pos = PetActor.GarageHome;
                pos.y = 0f;
                _pets[p.Species] = PetActor.Spawn(parent, p.Species, pos, 0f, false);
            }
        }
    }
}
