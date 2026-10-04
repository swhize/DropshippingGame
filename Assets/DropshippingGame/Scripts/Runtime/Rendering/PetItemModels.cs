using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// Erkennbare 3D-Modelle aller Tierbedarf-Artikel (Fressnix) und Deko für die Tier-Ecke:
    /// Futtersack mit Pfote, Nassfutter-Dosen, Quietsch-Ball, Kratzbaum, Kuschelbett, Napf (mit Füllstand),
    /// Kauknochen, Seil, Frisbee, Katzenklo ... Gleiche Logik wie <see cref="ProductModels"/>:
    /// bevorzugt ein Asset "petitem.&lt;id&gt;" aus dem Manifest, sonst prozedural. Pro ID eine (inaktive)
    /// Vorlage, danach nur Instanzen. Ergebnis: Mitte (bzw. Boden bei bottom) im Ursprung,
    /// größte Kante = targetSize (≤ 0: echte Größe). Vorderseite +Z.
    /// IDs: StoreItemDef-IDs ("fn_futter" ...) oder Deko-IDs aus <see cref="Extras"/>; Napf mit Füllstand: "napf@0".."napf@3".
    /// </summary>
    public static class PetItemModels
    {
        /// <summary>Zusätzliche Deko-Modelle, die es (noch) nicht zu kaufen gibt.</summary>
        public static readonly string[] Extras =
        {
            "napf@3", "wassernapf", "knochen", "seil", "frisbee", "leine", "katzenklo", "leckerli", "pulli",
        };

        private sealed class Template
        {
            public GameObject Root;
            public Bounds Bounds;
        }

        private static readonly Dictionary<string, Template> Templates = new Dictionary<string, Template>();
        private static Transform _store;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Templates.Clear();
            _store = null;
        }

        /// <summary>Alle Fressnix-Artikel mit Modell (ohne Tiere selbst).</summary>
        public static List<string> ShopIds()
        {
            var l = new List<string>();
            foreach (var it in ShopData.ItemsOf(ShopData.PetShop))
                if (it.Kind != "tier") l.Add(it.Id);
            return l;
        }

        /// <summary>Napf-ID passend zum Futtervorrat (0 = leer … 3 = voll).</summary>
        public static string BowlFor(int food) => "napf@" + (food <= 0 ? 0 : food < 5 ? 1 : food < 12 ? 2 : 3);

        /// <summary>Baut das Modell. targetSize = größte Kante in Metern (≤ 0: echte Größe).</summary>
        public static GameObject Build(Transform parent, string itemId, float targetSize, bool bottom = true)
        {
            var tpl = Get(itemId);
            float max = Mathf.Max(tpl.Bounds.size.x, Mathf.Max(tpl.Bounds.size.y, tpl.Bounds.size.z));
            float scale = targetSize > 0f && max > 0.0001f ? targetSize / max : 1f;
            return Place(parent, tpl, scale, bottom);
        }

        /// <summary>Wie Build, aber so skaliert, dass das Modell in die Box passt (Boden auf -box.y/2).</summary>
        public static GameObject BuildFitted(Transform parent, string itemId, Vector3 box)
        {
            var tpl = Get(itemId);
            var s = tpl.Bounds.size;
            float scale = Mathf.Min(box.x / Mathf.Max(s.x, 0.0001f), Mathf.Min(box.y / Mathf.Max(s.y, 0.0001f), box.z / Mathf.Max(s.z, 0.0001f)));
            var go = Place(parent, tpl, scale, true);
            go.transform.localPosition = new Vector3(0f, -box.y / 2f, 0f);
            return go;
        }

        /// <summary>Echte Größe (größte Kante, Meter).</summary>
        public static float NaturalSize(string itemId)
        {
            var s = Get(itemId).Bounds.size;
            return Mathf.Max(s.x, Mathf.Max(s.y, s.z));
        }

        private static GameObject Place(Transform parent, Template tpl, float scale, bool bottom)
        {
            var root = new GameObject("PetItem");
            root.transform.SetParent(parent, false);
            var inst = Object.Instantiate(tpl.Root, root.transform, false);
            inst.name = "Model";
            var c = tpl.Bounds.center;
            if (bottom) c.y = tpl.Bounds.min.y;
            inst.transform.localScale = Vector3.one * scale;
            inst.transform.localPosition = -c * scale;
            inst.transform.localRotation = Quaternion.identity;
            inst.SetActive(true);
            return root;
        }

        private static Template Get(string id)
        {
            if (id == null) id = "";
            if (Templates.TryGetValue(id, out var t) && t != null && t.Root != null) return t;
            if (_store == null)
            {
                var s = new GameObject("PetItemModelTemplates");
                s.SetActive(false);
                if (Application.isPlaying) Object.DontDestroyOnLoad(s);
                _store = s.transform;
            }
            var root = new GameObject(id);
            root.transform.SetParent(_store, false);
            bool asset = false;
            try
            {
                string key = "petitem." + id;
                if (Props.UseAssets && AssetLib.HasModel(key))
                    asset = AssetLib.Model(key, root.transform, Vector3.zero) != null;
            }
            catch (System.Exception) { asset = false; }
            if (!asset)
            {
                try { BuildProcedural(root.transform, id); }
                catch (System.Exception e)
                {
                    Debug.LogWarning("PetItemModels: " + id + " → " + e.Message);
                    B(root.transform, new Vector3(0.2f, 0.2f, 0.2f), Mats.Std(Color.magenta, 0.6f), new Vector3(0, 0.1f, 0));
                }
            }
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (r.bounds.size.sqrMagnitude < 0.0004f) r.shadowCastingMode = ShadowCastingMode.Off;
            t = new Template { Root = root, Bounds = ProductModels.MeshBounds(root.transform) };
            if (t.Bounds.size.sqrMagnitude < 1e-8f) t.Bounds = new Bounds(Vector3.zero, Vector3.one * 0.15f);
            Templates[id] = t;
            return t;
        }

        // ---- Bausteine ----------------------------------------------------------------------------
        private static GameObject B(Transform p, Vector3 size, Material m, Vector3 pos, Vector3 rot = default, float bevel = -1f) =>
            Props.Box(p, size, m, pos, rot, bevel);

        private static GameObject C(Transform p, float r, float h, Material m, Vector3 pos, Vector3 rot = default, int seg = 16) =>
            Props.Cyl(p, r, r, h, m, pos, rot, seg);

        private static GameObject Cone(Transform p, float rTop, float rBot, float h, Material m, Vector3 pos, Vector3 rot = default, int seg = 16) =>
            Props.Cyl(p, rTop, rBot, h, m, pos, rot, seg);

        private static GameObject S(Transform p, float r, Material m, Vector3 pos, Vector3 scale = default, int seg = 12, int rings = 6)
        {
            var go = Props.Sphere(p, r, m, pos, seg, rings);
            if (scale != default) go.transform.localScale = scale;
            return go;
        }

        private static GameObject Ring(Transform p, float R, float r, Material m, Vector3 pos, Vector3 rot = default, int seg = 24, int sides = 8) =>
            ProductModels.Torus(p, R, r, m, pos, rot, 360f, 0f, seg, sides);

        private static Color ItemColor(string id, Color fallback)
        {
            var it = ShopData.Item(id);
            return it != null ? it.Color.ToColor() : fallback;
        }

        /// <summary>Pfotenabdruck in der XY-Ebene (Blick +Z), Mitte bei pos.</summary>
        private static void Paw(Transform t, Material m, Vector3 pos, float s)
        {
            S(t, 0.022f * s, m, pos + new Vector3(0f, -0.008f * s, 0f), new Vector3(1.2f, 1f, 0.25f), 10, 4);
            for (int i = 0; i < 4; i++)
            {
                float x = (-0.027f + i * 0.018f) * s;
                float y = (i == 0 || i == 3 ? 0.016f : 0.027f) * s;
                S(t, 0.0085f * s, m, pos + new Vector3(x, y, 0f), new Vector3(1f, 1.2f, 0.3f), 8, 4);
            }
        }

        private static void BuildProcedural(Transform t, string id)
        {
            var white = Mats.Std(new Color(0.96f, 0.96f, 0.94f), 0.45f);
            var dark = Mats.Std(new Color(0.12f, 0.12f, 0.14f), 0.5f);
            if (id.StartsWith("napf@"))
            {
                int lvl = 0;
                int.TryParse(id.Substring(5), out lvl);
                Bowl(t, new Color(0.85f, 0.2f, 0.2f), Mathf.Clamp(lvl, 0, 3), false);
                return;
            }
            switch (id)
            {
                case "fn_futter": FoodBag(t, ItemColor(id, new Color(0.85f, 0.65f, 0.3f)), white, dark); break;
                case "fn_premium":
                case "dosen": Cans(t, ItemColor("fn_premium", new Color(0.75f, 0.25f, 0.4f)), white); break;
                case "fn_ball": Ball(t, ItemColor(id, new Color(0.95f, 0.3f, 0.3f))); break;
                case "fn_kratzbaum": CatTree(t, ItemColor(id, new Color(0.75f, 0.68f, 0.55f))); break;
                case "fn_halsband": Collar(t, ItemColor(id, new Color(0.3f, 0.8f, 0.95f)), true); break;
                case "fn_bett": Bed(t, ItemColor(id, new Color(0.55f, 0.45f, 0.85f))); break;
                case "fn_brunnen": ProductModels.Build(t, "katzenbrunnen", 0f, true); break;
                case "fn_gps": Gps(t, dark); break;
                case "fn_automat": Feeder(t, white, dark); break;
                case "fn_laser": Laser(t, ItemColor(id, new Color(0.95f, 0.2f, 0.25f)), dark); break;
                case "napf": Bowl(t, new Color(0.85f, 0.2f, 0.2f), 3, false); break;
                case "wassernapf": Bowl(t, new Color(0.25f, 0.55f, 0.9f), 3, true); break;
                case "knochen": Bone(t); break;
                case "seil": Rope(t); break;
                case "frisbee": Frisbee(t); break;
                case "leine": Leash(t, dark); break;
                case "katzenklo": Litter(t, white); break;
                case "leckerli": TreatJar(t); break;
                case "pulli": Sweater(t); break;
                default:
                    B(t, new Vector3(0.2f, 0.2f, 0.2f), Mats.Std(ItemColor(id, new Color(0.8f, 0.7f, 0.5f)), 0.7f), new Vector3(0, 0.1f, 0));
                    break;
            }
        }

        // ---- Modelle ------------------------------------------------------------------------------
        private static void FoodBag(Transform t, Color col, Material white, Material dark)
        {
            var bag = Mats.Std(col, 0.75f);
            // Sack: unten breit, oben zusammengefaltet
            B(t, new Vector3(0.3f, 0.36f, 0.14f), bag, new Vector3(0, 0.18f, 0), default, 0.035f);
            B(t, new Vector3(0.29f, 0.06f, 0.07f), bag, new Vector3(0, 0.38f, 0), default, 0.015f);
            B(t, new Vector3(0.3f, 0.025f, 0.075f), Mats.Std(col * 0.75f, 0.7f), new Vector3(0, 0.405f, 0), default, 0.008f);
            // Etikett mit großer Pfote
            B(t, new Vector3(0.22f, 0.2f, 0.005f), white, new Vector3(0, 0.2f, 0.071f), default, 0f);
            Paw(t, Mats.Std(new Color(0.35f, 0.22f, 0.15f), 0.6f), new Vector3(0f, 0.22f, 0.075f), 2.2f);
            // Kroketten unten aufs Etikett
            var kib = Mats.Std(new Color(0.55f, 0.33f, 0.16f), 0.8f);
            for (int i = 0; i < 5; i++)
                S(t, 0.012f, kib, new Vector3(-0.05f + i * 0.025f, 0.125f + (i % 2) * 0.008f, 0.075f), new Vector3(1f, 0.8f, 0.4f), 8, 4);
            // Markenstreifen
            B(t, new Vector3(0.3f, 0.03f, 0.005f), Mats.Std(new Color(0.2f, 0.55f, 0.3f), 0.5f), new Vector3(0, 0.33f, 0.071f), default, 0f);
        }

        private static void Cans(Transform t, Color col, Material white)
        {
            var metal = Mats.Std(new Color(0.75f, 0.76f, 0.78f), 0.3f, 0.8f);
            var label = Mats.Std(col, 0.5f);
            var fish = Mats.Std(new Color(0.98f, 0.75f, 0.2f), 0.5f);
            Vector3[] pos = { new Vector3(-0.045f, 0f, 0f), new Vector3(0.045f, 0f, 0f), new Vector3(0f, 0.045f, 0f) };
            foreach (var p in pos)
            {
                C(t, 0.042f, 0.045f, metal, p + new Vector3(0, 0.0225f, 0), default, 18);
                C(t, 0.0425f, 0.03f, label, p + new Vector3(0, 0.0225f, 0), default, 18);
                Ring(t, 0.03f, 0.003f, metal, p + new Vector3(0, 0.046f, 0), new Vector3(90, 0, 0), 16, 4);
                // Fisch auf dem Etikett
                S(t, 0.012f, fish, p + new Vector3(0, 0.0225f, 0.042f), new Vector3(1.4f, 0.8f, 0.3f), 8, 4);
                Cone(t, 0.0f, 0.008f, 0.012f, fish, p + new Vector3(-0.022f, 0.0225f, 0.042f), new Vector3(0, 0, 90), 6);
            }
        }

        private static void Ball(Transform t, Color col)
        {
            S(t, 0.045f, Mats.Std(col, 0.35f), new Vector3(0, 0.045f, 0), default, 18, 10);
            Ring(t, 0.0455f, 0.005f, Mats.Std(Color.white, 0.4f), new Vector3(0, 0.045f, 0), new Vector3(0, 0, 25), 28, 6);
            Ring(t, 0.0455f, 0.005f, Mats.Std(new Color(1f, 0.85f, 0.2f), 0.4f), new Vector3(0, 0.045f, 0), new Vector3(0, 90, -25), 28, 6);
        }

        private static void CatTree(Transform t, Color col)
        {
            var plush = Mats.Std(col, 0.95f);
            var sisal = Mats.Std(new Color(0.8f, 0.7f, 0.5f), 0.95f);
            B(t, new Vector3(0.6f, 0.06f, 0.5f), plush, new Vector3(0, 0.03f, 0), default, 0.02f);
            C(t, 0.06f, 0.75f, sisal, new Vector3(-0.18f, 0.43f, 0), default, 12);
            C(t, 0.06f, 1.15f, sisal, new Vector3(0.18f, 0.63f, -0.08f), default, 12);
            // Höhle
            B(t, new Vector3(0.34f, 0.28f, 0.3f), plush, new Vector3(-0.12f, 0.95f, 0.04f), default, 0.03f);
            C(t, 0.08f, 0.02f, Mats.Std(new Color(0.15f, 0.12f, 0.1f), 0.9f), new Vector3(-0.12f, 0.93f, 0.19f), new Vector3(90, 0, 0), 16);
            // Liegeflächen
            C(t, 0.18f, 0.05f, plush, new Vector3(0.18f, 1.23f, -0.08f), default, 20);
            B(t, new Vector3(0.3f, 0.05f, 0.25f), plush, new Vector3(0.1f, 0.55f, 0.05f), default, 0.02f);
            // Spielball an der Schnur
            C(t, 0.003f, 0.2f, Mats.Std(Color.white, 0.6f), new Vector3(0.22f, 0.43f, 0.12f), default, 4);
            S(t, 0.025f, Mats.Std(new Color(0.95f, 0.3f, 0.5f), 0.9f), new Vector3(0.22f, 0.32f, 0.12f), default, 10, 6);
        }

        private static void Collar(Transform t, Color col, bool led)
        {
            var band = led ? Mats.Emit(col, 1.4f) : Mats.Std(col, 0.6f);
            Ring(t, 0.075f, 0.009f, band, new Vector3(0, 0.009f, 0), new Vector3(90, 0, 0), 28, 6);
            var metal = Mats.Std(new Color(0.85f, 0.85f, 0.8f), 0.25f, 0.9f);
            B(t, new Vector3(0.03f, 0.02f, 0.012f), metal, new Vector3(0, 0.009f, 0.075f), default, 0.003f);
            // Marke
            C(t, 0.015f, 0.003f, Mats.Std(new Color(0.95f, 0.8f, 0.2f), 0.3f, 0.8f), new Vector3(0, 0.002f, 0.1f), default, 12);
        }

        private static void Bed(Transform t, Color col)
        {
            var plush = Mats.Std(col, 0.95f);
            C(t, 0.32f, 0.05f, plush, new Vector3(0, 0.025f, 0), default, 28);
            Ring(t, 0.28f, 0.07f, plush, new Vector3(0, 0.08f, 0), new Vector3(90, 0, 0), 32, 10);
            C(t, 0.24f, 0.05f, Mats.Std(Color.Lerp(col, Color.white, 0.6f), 0.95f), new Vector3(0, 0.06f, 0), default, 24);
            Paw(t, Mats.Std(col * 0.8f, 0.9f), new Vector3(0, 0.1f, 0.35f), 1.5f);
        }

        private static void Gps(Transform t, Material dark)
        {
            Collar(t, new Color(0.15f, 0.15f, 0.18f), false);
            B(t, new Vector3(0.06f, 0.04f, 0.035f), dark, new Vector3(0, 0.02f, 0.085f), default, 0.008f);
            C(t, 0.003f, 0.06f, dark, new Vector3(0.022f, 0.06f, 0.085f), default, 6);
            S(t, 0.006f, Mats.Emit(new Color(0.3f, 1f, 0.4f), 2f), new Vector3(-0.015f, 0.03f, 0.103f), default, 8, 4);
            B(t, new Vector3(0.03f, 0.012f, 0.003f), Mats.Emit(new Color(0.3f, 0.7f, 1f), 1.2f), new Vector3(0.006f, 0.018f, 0.103f), default, 0f);
        }

        private static void Feeder(Transform t, Material white, Material dark)
        {
            B(t, new Vector3(0.24f, 0.07f, 0.28f), white, new Vector3(0, 0.035f, 0.02f), default, 0.02f);
            B(t, new Vector3(0.2f, 0.26f, 0.18f), white, new Vector3(0, 0.2f, -0.03f), default, 0.03f);
            // Sichtfenster mit Kroketten
            B(t, new Vector3(0.12f, 0.12f, 0.005f), Mats.Glass(new Color(0.7f, 0.85f, 1f, 0.5f)), new Vector3(0, 0.24f, 0.062f), default, 0f);
            B(t, new Vector3(0.11f, 0.06f, 0.004f), Mats.Std(new Color(0.55f, 0.33f, 0.16f), 0.8f), new Vector3(0, 0.21f, 0.058f), default, 0f);
            // Display + Napf
            B(t, new Vector3(0.08f, 0.03f, 0.004f), Mats.Emit(new Color(0.3f, 0.8f, 1f), 1.5f), new Vector3(0, 0.12f, 0.062f), default, 0f);
            Cone(t, 0.07f, 0.055f, 0.03f, dark, new Vector3(0, 0.085f, 0.1f), default, 18);
            C(t, 0.055f, 0.005f, Mats.Std(new Color(0.55f, 0.33f, 0.16f), 0.8f), new Vector3(0, 0.095f, 0.1f), default, 16);
            // WLAN-Antenne (Satire: braucht App + Abo)
            C(t, 0.004f, 0.08f, dark, new Vector3(0.08f, 0.37f, -0.08f), default, 6);
            S(t, 0.008f, Mats.Emit(new Color(1f, 0.3f, 0.3f), 2f), new Vector3(0.08f, 0.41f, -0.08f), default, 8, 4);
        }

        private static void Laser(Transform t, Color col, Material dark)
        {
            var body = Mats.Std(new Color(0.75f, 0.76f, 0.8f), 0.3f, 0.8f);
            C(t, 0.012f, 0.14f, body, new Vector3(0, 0.012f, 0), new Vector3(90, 0, 0), 12);
            C(t, 0.013f, 0.02f, dark, new Vector3(0, 0.012f, -0.06f), new Vector3(90, 0, 0), 12);
            B(t, new Vector3(0.01f, 0.006f, 0.012f), Mats.Std(col, 0.4f), new Vector3(0, 0.025f, 0.02f), default, 0.002f);
            S(t, 0.008f, Mats.Emit(col, 3f), new Vector3(0, 0.012f, 0.072f), default, 8, 4);
            // Clip
            B(t, new Vector3(0.005f, 0.004f, 0.06f), body, new Vector3(0, 0.026f, -0.03f), default, 0f);
        }

        private static void Bowl(Transform t, Color col, int level, bool water)
        {
            var m = Mats.Std(col, 0.35f);
            Cone(t, 0.11f, 0.085f, 0.06f, m, new Vector3(0, 0.03f, 0), default, 24);
            Ring(t, 0.106f, 0.008f, m, new Vector3(0, 0.06f, 0), new Vector3(90, 0, 0), 28, 6);
            C(t, 0.08f, 0.004f, Mats.Std(col * 0.6f, 0.5f), new Vector3(0, 0.022f, 0), default, 20);
            if (!water) Paw(t, Mats.Std(Color.white, 0.4f), new Vector3(0, 0.03f, 0.1f), 0.8f);
            if (level <= 0) return;
            if (water)
            {
                C(t, 0.095f, 0.004f, Mats.Glass(new Color(0.45f, 0.75f, 0.95f, 0.6f)), new Vector3(0, 0.05f, 0), default, 24);
                return;
            }
            var kib = Mats.Std(new Color(0.55f, 0.33f, 0.16f), 0.85f);
            float y = 0.025f + level * 0.01f;
            C(t, 0.085f + level * 0.004f, 0.006f, kib, new Vector3(0, y, 0), default, 20);
            int n = level * 6;
            for (int i = 0; i < n; i++)
            {
                float a = i * 2.4f, r = 0.015f + (i % 5) * 0.014f;
                S(t, 0.011f, kib, new Vector3(Mathf.Cos(a) * r, y + 0.006f + (i % 3) * 0.004f, Mathf.Sin(a) * r), new Vector3(1f, 0.7f, 1f), 6, 4);
            }
            if (level >= 3) S(t, 0.05f, kib, new Vector3(0, y, 0), new Vector3(1f, 0.35f, 1f), 12, 6);
        }

        private static void Bone(Transform t)
        {
            var m = Mats.Std(new Color(0.95f, 0.9f, 0.78f), 0.7f);
            C(t, 0.017f, 0.14f, m, new Vector3(0, 0.022f, 0), new Vector3(0, 0, 90), 12);
            foreach (float x in new[] { -0.075f, 0.075f })
                foreach (float z in new[] { -0.018f, 0.018f })
                    S(t, 0.024f, m, new Vector3(x, 0.024f, z), default, 12, 6);
        }

        private static void Rope(Transform t)
        {
            var a = Mats.Std(new Color(0.9f, 0.2f, 0.2f), 0.95f);
            var b = Mats.Std(new Color(0.95f, 0.95f, 0.9f), 0.95f);
            var c = Mats.Std(new Color(0.2f, 0.45f, 0.9f), 0.95f);
            for (int i = 0; i < 3; i++)
            {
                var m = i == 0 ? a : (i == 1 ? b : c);
                float off = (i - 1) * 0.008f;
                C(t, 0.009f, 0.22f, m, new Vector3(0, 0.02f + off * 0.5f, off), new Vector3(0, 0, 90 + (i - 1) * 3), 8);
            }
            foreach (float x in new[] { -0.12f, 0.12f })
            {
                S(t, 0.03f, b, new Vector3(x, 0.03f, 0), default, 10, 6);
                Cone(t, 0.025f, 0.012f, 0.05f, a, new Vector3(x * 1.35f, 0.025f, 0), new Vector3(0, 0, x > 0 ? -90 : 90), 8);
            }
        }

        private static void Frisbee(Transform t)
        {
            var m = Mats.Std(new Color(1f, 0.55f, 0.1f), 0.35f);
            Cone(t, 0.1f, 0.12f, 0.018f, m, new Vector3(0, 0.009f, 0), default, 28);
            Ring(t, 0.115f, 0.008f, m, new Vector3(0, 0.012f, 0), new Vector3(90, 0, 0), 32, 6);
            // Pfote flach obendrauf
            var w = Mats.Std(Color.white, 0.4f);
            S(t, 0.035f, w, new Vector3(0, 0.019f, -0.012f), new Vector3(1.2f, 0.12f, 1f), 10, 4);
            for (int i = 0; i < 4; i++)
                S(t, 0.013f, w, new Vector3(-0.043f + i * 0.029f, 0.019f, i == 0 || i == 3 ? 0.026f : 0.043f), new Vector3(1f, 0.15f, 1.2f), 8, 4);
        }

        private static void Leash(Transform t, Material dark)
        {
            var red = Mats.Std(new Color(0.85f, 0.15f, 0.15f), 0.6f);
            for (int i = 0; i < 4; i++)
                Ring(t, 0.08f - i * 0.006f, 0.006f, red, new Vector3(0, 0.006f + i * 0.011f, 0), new Vector3(90, 0, 0), 24, 6);
            // Griff + Karabiner
            Ring(t, 0.035f, 0.009f, dark, new Vector3(0.1f, 0.03f, 0), new Vector3(0, 90, 0), 16, 6);
            B(t, new Vector3(0.025f, 0.012f, 0.012f), Mats.Std(new Color(0.8f, 0.8f, 0.8f), 0.25f, 0.9f), new Vector3(-0.09f, 0.01f, 0.03f), default, 0.003f);
        }

        private static void Litter(Transform t, Material white)
        {
            var m = Mats.Std(new Color(0.4f, 0.7f, 0.85f), 0.45f);
            B(t, new Vector3(0.48f, 0.14f, 0.36f), m, new Vector3(0, 0.07f, 0), default, 0.03f);
            B(t, new Vector3(0.42f, 0.02f, 0.3f), Mats.Std(new Color(0.85f, 0.8f, 0.7f), 0.98f), new Vector3(0, 0.125f, 0), default, 0.005f);
            // Klümpchen (Satire)
            var lump = Mats.Std(new Color(0.6f, 0.55f, 0.48f), 0.98f);
            S(t, 0.018f, lump, new Vector3(0.08f, 0.135f, -0.05f), new Vector3(1.3f, 0.6f, 1f), 8, 4);
            S(t, 0.014f, lump, new Vector3(-0.1f, 0.135f, 0.06f), new Vector3(1.2f, 0.6f, 1f), 8, 4);
            // Schaufel
            B(t, new Vector3(0.09f, 0.005f, 0.1f), white, new Vector3(0.17f, 0.15f, 0.09f), new Vector3(0, 20, 8), 0.003f);
            B(t, new Vector3(0.02f, 0.01f, 0.1f), white, new Vector3(0.2f, 0.16f, 0.0f), new Vector3(0, 20, 8), 0.003f);
        }

        private static void TreatJar(Transform t)
        {
            var glass = Mats.Glass(new Color(0.85f, 0.95f, 1f, 0.4f));
            C(t, 0.07f, 0.17f, glass, new Vector3(0, 0.085f, 0), default, 20);
            var treat = Mats.Std(new Color(0.75f, 0.45f, 0.2f), 0.8f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.1f;
                B(t, new Vector3(0.05f, 0.016f, 0.018f), treat, new Vector3(Mathf.Cos(a) * 0.03f, 0.015f + i * 0.012f, Mathf.Sin(a) * 0.03f), new Vector3(0, i * 40f, i * 15f), 0.006f);
            }
            var lid = Mats.Std(new Color(0.85f, 0.2f, 0.25f), 0.4f);
            C(t, 0.075f, 0.025f, lid, new Vector3(0, 0.18f, 0), default, 20);
            // Knochen-Griff
            var bone = Mats.Std(new Color(0.96f, 0.92f, 0.8f), 0.6f);
            C(t, 0.008f, 0.06f, bone, new Vector3(0, 0.2f, 0), new Vector3(0, 0, 90), 8);
            foreach (float x in new[] { -0.032f, 0.032f }) S(t, 0.012f, bone, new Vector3(x, 0.2f, 0), default, 8, 4);
        }

        private static void Sweater(Transform t)
        {
            var knit = Mats.Std(new Color(0.85f, 0.2f, 0.25f), 0.98f);
            var white = Mats.Std(new Color(0.96f, 0.96f, 0.94f), 0.95f);
            B(t, new Vector3(0.24f, 0.03f, 0.32f), knit, new Vector3(0, 0.015f, 0), default, 0.012f);
            foreach (float x in new[] { -0.085f, 0.085f })
                C(t, 0.03f, 0.08f, knit, new Vector3(x, 0.015f, 0.17f), new Vector3(90, 0, 0), 10);
            Ring(t, 0.06f, 0.012f, white, new Vector3(0, 0.02f, 0.16f), new Vector3(90, 0, 0), 20, 6);
            for (int i = 0; i < 3; i++)
                B(t, new Vector3(0.245f, 0.032f, 0.02f), white, new Vector3(0, 0.016f, -0.1f + i * 0.06f), default, 0.005f);
            // "INFLUENCER"-Stern
            S(t, 0.022f, Mats.Std(new Color(1f, 0.85f, 0.2f), 0.5f), new Vector3(0, 0.034f, 0.05f), new Vector3(1f, 0.2f, 1f), 5, 3);
        }
    }
}
