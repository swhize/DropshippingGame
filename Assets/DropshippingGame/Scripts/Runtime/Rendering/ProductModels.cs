using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// Erkennbare 3D-Modelle aller Produkte (Kopfhörer, Drohne, Wackeldackel ...). Bevorzugt ein Asset
    /// "product.&lt;id&gt;" aus dem Manifest, sonst ein prozedurales Modell aus Grundformen.
    /// Pro Produkt wird einmal eine (inaktive) Vorlage gebaut und danach nur noch instanziert –
    /// Meshes und Materialien sind geteilt (MeshKit/Mats-Caches).
    /// Ergebnis: Knoten, dessen Begrenzungsbox-Mitte im Ursprung liegt (bottom = true: Boden im Ursprung),
    /// größte Kante = targetSize. Vorderseite zeigt nach +Z.
    /// </summary>
    public static class ProductModels
    {
        private sealed class Template
        {
            public GameObject Root;
            public Bounds Bounds;
        }

        private static readonly Dictionary<string, Template> Templates = new Dictionary<string, Template>();
        private static readonly Dictionary<string, Mesh> TorusCache = new Dictionary<string, Mesh>();
        private static Transform _store;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Templates.Clear();
            TorusCache.Clear();
            _store = null;
        }

        /// <summary>Baut das Produktmodell. targetSize = größte Kante in Metern (≤ 0: echte Größe).</summary>
        public static GameObject Build(Transform parent, string productId, float targetSize, bool bottom = false)
        {
            var tpl = Get(productId);
            float max = Mathf.Max(tpl.Bounds.size.x, Mathf.Max(tpl.Bounds.size.y, tpl.Bounds.size.z));
            float scale = targetSize > 0f && max > 0.0001f ? targetSize / max : 1f;
            return Place(parent, tpl, scale, bottom);
        }

        /// <summary>Wie Build, aber so skaliert, dass das Modell in die Box passt (Mitte im Ursprung, Boden auf -box.y/2).</summary>
        public static GameObject BuildFitted(Transform parent, string productId, Vector3 box)
        {
            var tpl = Get(productId);
            var s = tpl.Bounds.size;
            float scale = Mathf.Min(box.x / Mathf.Max(s.x, 0.0001f), Mathf.Min(box.y / Mathf.Max(s.y, 0.0001f), box.z / Mathf.Max(s.z, 0.0001f)));
            var go = Place(parent, tpl, scale, true);
            go.transform.localPosition = new Vector3(0f, -box.y / 2f, 0f);
            return go;
        }

        /// <summary>Echte Größe (größte Kante, Meter) des Modells.</summary>
        public static float NaturalSize(string productId)
        {
            var s = Get(productId).Bounds.size;
            return Mathf.Max(s.x, Mathf.Max(s.y, s.z));
        }

        private static GameObject Place(Transform parent, Template tpl, float scale, bool bottom)
        {
            var root = new GameObject("Product");
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
                var s = new GameObject("ProductModelTemplates");
                s.SetActive(false);
                if (Application.isPlaying) Object.DontDestroyOnLoad(s);
                _store = s.transform;
            }
            var root = new GameObject(id);
            root.transform.SetParent(_store, false);
            bool asset = false;
            try
            {
                string key = "product." + id;
                if (Props.UseAssets && AssetLib.HasModel(key))
                    asset = AssetLib.Model(key, root.transform, Vector3.zero) != null;
            }
            catch (System.Exception) { asset = false; }
            if (!asset)
            {
                ProductDef p = GameData.IsProduct(id) ? GameData.Product(id) : null;
                BuildProcedural(root.transform, id, p != null ? p.Color.ToColor() : new Color(0.9f, 0.8f, 0.3f));
            }
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (r.bounds.size.sqrMagnitude < 0.0004f) r.shadowCastingMode = ShadowCastingMode.Off;
            t = new Template { Root = root, Bounds = MeshBounds(root.transform) };
            if (t.Bounds.size.sqrMagnitude < 1e-8f) t.Bounds = new Bounds(Vector3.zero, Vector3.one * 0.15f);
            Templates[id] = t;
            return t;
        }

        /// <summary>Begrenzung aus den Meshes (funktioniert auch bei inaktiven Objekten).</summary>
        private static Bounds MeshBounds(Transform root)
        {
            var b = new Bounds();
            bool any = false;
            var inv = root.worldToLocalMatrix;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var m = inv * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(c);
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                    else b.Encapsulate(p);
                }
            }
            foreach (var sm in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (sm.sharedMesh == null) continue;
                var mb = sm.sharedMesh.bounds;
                var m = inv * sm.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(c);
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        // ---- Bausteine ----------------------------------------------------------------------------
        private static GameObject B(Transform p, Vector3 size, Material m, Vector3 pos, Vector3 rot = default, float bevel = -1f) =>
            Props.Box(p, size, m, pos, rot, bevel);

        private static GameObject C(Transform p, float r, float h, Material m, Vector3 pos, Vector3 rot = default, int seg = 14) =>
            Props.Cyl(p, r, r, h, m, pos, rot, seg);

        private static GameObject Cone(Transform p, float rTop, float rBot, float h, Material m, Vector3 pos, Vector3 rot = default, int seg = 14) =>
            Props.Cyl(p, rTop, rBot, h, m, pos, rot, seg);

        private static GameObject S(Transform p, float r, Material m, Vector3 pos, Vector3 scale = default, int seg = 12, int rings = 6)
        {
            var go = Props.Sphere(p, r, m, pos, seg, rings);
            if (scale != default) go.transform.localScale = scale;
            return go;
        }

        /// <summary>Torus(-Segment) in der XY-Ebene (Achse = Z), Bogen startet bei startDeg und läuft arcDeg.</summary>
        private static GameObject Torus(Transform p, float R, float r, Material m, Vector3 pos, Vector3 rot = default,
            float arcDeg = 360f, float startDeg = 0f, int seg = 20, int sides = 8)
        {
            var go = new GameObject("Torus");
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(rot);
            go.AddComponent<MeshFilter>().sharedMesh = TorusMesh(R, r, arcDeg, startDeg, seg, sides);
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        private static Mesh TorusMesh(float R, float r, float arcDeg, float startDeg, int seg, int sides)
        {
            string key = R.ToString("F4") + "|" + r.ToString("F4") + "|" + arcDeg.ToString("F1") + "|" + startDeg.ToString("F1") + "|" + seg + "|" + sides;
            if (TorusCache.TryGetValue(key, out var cached) && cached != null) return cached;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = (startDeg + arcDeg * i / seg) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var center = dir * R;
                for (int j = 0; j <= sides; j++)
                {
                    float b = j / (float)sides * Mathf.PI * 2f;
                    var nn = dir * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                    v.Add(center + nn * r);
                    n.Add(nn);
                    uv.Add(new Vector2(i / (float)seg, j / (float)sides));
                }
            }
            int row = sides + 1;
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a0 = i * row + j, a1 = a0 + 1, b0 = a0 + row, b1 = b0 + 1;
                    AddTri(v, n, tri, a0, b0, a1);
                    AddTri(v, n, tri, a1, b0, b1);
                }
            // Endkappen bei offenen Bögen
            if (arcDeg < 359.9f)
            {
                for (int end = 0; end < 2; end++)
                {
                    float a = (startDeg + (end == 0 ? 0f : arcDeg)) * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    var tangent = new Vector3(-dir.y, dir.x, 0f) * (end == 0 ? -1f : 1f);
                    int c = v.Count;
                    v.Add(dir * R); n.Add(tangent); uv.Add(new Vector2(0.5f, 0.5f));
                    for (int j = 0; j <= sides; j++)
                    {
                        float b = j / (float)sides * Mathf.PI * 2f;
                        v.Add(dir * R + (dir * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b)) * r);
                        n.Add(tangent);
                        uv.Add(new Vector2(0.5f + Mathf.Cos(b) * 0.5f, 0.5f + Mathf.Sin(b) * 0.5f));
                    }
                    for (int j = 0; j < sides; j++) AddTri(v, n, tri, c, c + 1 + j, c + 2 + j);
                }
            }
            var mesh = new Mesh { name = "torus:" + key };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            TorusCache[key] = mesh;
            return mesh;
        }

        /// <summary>Dreieck mit korrekter Ausrichtung (Unity: im Uhrzeigersinn = Vorderseite).</summary>
        private static void AddTri(List<Vector3> v, List<Vector3> n, List<int> tri, int a, int b, int c)
        {
            var fn = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (Vector3.Dot(fn, n[a] + n[b] + n[c]) < 0f) { tri.Add(a); tri.Add(c); tri.Add(b); }
            else { tri.Add(a); tri.Add(b); tri.Add(c); }
        }

        // ---- Prozedurale Modelle (Maße in Metern, Ursprung ~ Bodenmitte) --------------------------
        private static void BuildProcedural(Transform t, string id, Color col)
        {
            var main = Mats.Std(col, 0.45f);
            var dark = Mats.Std(new Color(0.11f, 0.11f, 0.13f), 0.5f);
            var grey = Mats.Std(new Color(0.55f, 0.57f, 0.6f), 0.35f, 0.6f);
            var white = Mats.Std(new Color(0.95f, 0.95f, 0.94f), 0.4f);
            switch (id)
            {
                case "huelle": Huelle(t, main, dark, grey); break;
                case "led": Led(t, col, white, dark); break;
                case "massage": Massage(t, main, dark, grey, col); break;
                case "kopfhoerer": Kopfhoerer(t, main, dark, grey); break;
                case "ringlicht": Ringlicht(t, dark, grey); break;
                case "katzenbrunnen": Katzenbrunnen(t, main, white); break;
                case "haltung": Haltung(t, main, dark, col); break;
                case "smartwatch": Smartwatch(t, main, dark, grey); break;
                case "beamer": Beamer(t, main, dark, grey); break;
                case "drohne": Drohne(t, main, dark, grey); break;
                case "toaster": Toaster(t, main, dark, grey); break;
                case "bartglitzer": Bartglitzer(t, col, white); break;
                case "giesskanne": Giesskanne(t, main, dark, grey); break;
                case "wackeldackel": Wackeldackel(t, col, dark); break;
                default:
                    B(t, new Vector3(0.15f, 0.1f, 0.12f), main, new Vector3(0, 0.05f, 0));
                    break;
            }
        }

        private static void Huelle(Transform t, Material main, Material dark, Material grey)
        {
            // Handyhülle auf kleinem Ständer, Rückseite mit Kameraausschnitt zeigt nach vorne.
            var n = Props.Node(t, "Case", new Vector3(0, 0.012f, 0));
            n.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            var nt = n.transform;
            B(nt, new Vector3(0.078f, 0.158f, 0.012f), main, new Vector3(0, 0.079f, 0), default, 0.005f);
            B(nt, new Vector3(0.07f, 0.15f, 0.004f), dark, new Vector3(0, 0.079f, -0.005f), default, 0.002f);
            B(nt, new Vector3(0.03f, 0.034f, 0.005f), dark, new Vector3(-0.017f, 0.13f, 0.007f), default, 0.004f);
            var lens = Mats.Glass(new Color(0.15f, 0.2f, 0.3f, 0.9f));
            C(nt, 0.006f, 0.004f, lens, new Vector3(-0.022f, 0.138f, 0.01f), new Vector3(90, 0, 0), 10);
            C(nt, 0.006f, 0.004f, lens, new Vector3(-0.022f, 0.122f, 0.01f), new Vector3(90, 0, 0), 10);
            var logo = Mats.Std(Color.white, 0.3f);
            C(nt, 0.01f, 0.002f, logo, new Vector3(0, 0.07f, 0.0065f), new Vector3(90, 0, 0), 12);
            B(t, new Vector3(0.06f, 0.012f, 0.05f), grey, new Vector3(0, 0.006f, -0.005f), default, 0.003f);
        }

        private static void Led(Transform t, Color col, Material white, Material dark)
        {
            // Rolle mit aufgewickeltem Streifen, leuchtende Punkte, loses Ende am Boden.
            var spool = Props.Node(t, "Spool", new Vector3(0, 0.07f, 0));
            var st = spool.transform;
            C(st, 0.07f, 0.006f, white, new Vector3(0, 0, 0.022f), new Vector3(90, 0, 0), 20);
            C(st, 0.07f, 0.006f, white, new Vector3(0, 0, -0.022f), new Vector3(90, 0, 0), 20);
            C(st, 0.052f, 0.04f, Mats.Std(new Color(0.92f, 0.92f, 0.9f), 0.35f), Vector3.zero, new Vector3(90, 0, 0), 20);
            C(st, 0.016f, 0.05f, dark, Vector3.zero, new Vector3(90, 0, 0), 10);
            Color[] glow = { col, new Color(1f, 0.3f, 0.5f), new Color(0.3f, 0.7f, 1f), new Color(0.4f, 1f, 0.5f) };
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                var g = Mats.Emit(glow[i % glow.Length], 2.5f);
                S(st, 0.005f, g, new Vector3(Mathf.Cos(a) * 0.053f, Mathf.Sin(a) * 0.053f, (i % 2 == 0 ? 0.008f : -0.008f)), default, 6, 4);
            }
            // loses Ende
            B(t, new Vector3(0.012f, 0.003f, 0.12f), Mats.Std(new Color(0.92f, 0.92f, 0.9f), 0.35f), new Vector3(0.0f, 0.0015f, 0.09f), default, 0f);
            for (int i = 0; i < 5; i++)
                S(t, 0.004f, Mats.Emit(glow[i % glow.Length], 2.5f), new Vector3(0, 0.004f, 0.045f + i * 0.022f), default, 6, 4);
            B(t, new Vector3(0.02f, 0.012f, 0.03f), dark, new Vector3(0, 0.006f, 0.16f), default, 0.003f);
        }

        private static void Massage(Transform t, Material main, Material dark, Material grey, Color col)
        {
            // Griff senkrecht, Kopf waagerecht, Massagekopf vorne.
            C(t, 0.026f, 0.022f, dark, new Vector3(0, 0.011f, 0), default, 14);
            Cone(t, 0.017f, 0.02f, 0.13f, main, new Vector3(0, 0.087f, 0), default, 14);
            for (int i = 0; i < 3; i++) C(t, 0.0205f, 0.008f, dark, new Vector3(0, 0.05f + i * 0.025f, 0), default, 14);
            C(t, 0.035f, 0.15f, main, new Vector3(0, 0.18f, 0.01f), new Vector3(90, 0, 0), 16);
            C(t, 0.036f, 0.012f, dark, new Vector3(0, 0.18f, -0.06f), new Vector3(90, 0, 0), 16);
            C(t, 0.012f, 0.05f, grey, new Vector3(0, 0.18f, 0.105f), new Vector3(90, 0, 0), 10);
            S(t, 0.028f, Mats.Std(col.Darkened(0.55f), 0.7f), new Vector3(0, 0.18f, 0.14f), default, 12, 8);
            var led = Mats.Emit(new Color(0.3f, 0.9f, 1f), 2f);
            for (int i = 0; i < 4; i++) B(t, new Vector3(0.006f, 0.006f, 0.004f), led, new Vector3(-0.012f + i * 0.008f, 0.205f, -0.03f), default, 0f);
        }

        private static void Kopfhoerer(Transform t, Material main, Material dark, Material grey)
        {
            const float R = 0.085f, cy = 0.05f;
            // Bügel: Bogen über den Kopf, gepolstert
            Torus(t, R, 0.009f, main, new Vector3(0, cy, 0), default, 180f, 0f, 22, 8);
            Torus(t, R - 0.008f, 0.008f, dark, new Vector3(0, cy, 0), default, 120f, 30f, 16, 8);
            // Gleiter
            B(t, new Vector3(0.008f, 0.04f, 0.016f), grey, new Vector3(R, cy - 0.012f, 0), default, 0.002f);
            B(t, new Vector3(0.008f, 0.04f, 0.016f), grey, new Vector3(-R, cy - 0.012f, 0), default, 0.002f);
            // Ohrmuscheln (Achse X)
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * (R - 0.004f);
                C(t, 0.042f, 0.03f, main, new Vector3(x, cy - 0.05f, 0), new Vector3(0, 0, 90), 18);
                C(t, 0.03f, 0.032f, grey, new Vector3(x + s * 0.002f, cy - 0.05f, 0), new Vector3(0, 0, 90), 14);
                Torus(t, 0.03f, 0.011f, dark, new Vector3(x - s * 0.018f, cy - 0.05f, 0), new Vector3(0, 90, 0), 360f, 0f, 16, 6);
            }
            var led = Mats.Emit(new Color(0.3f, 0.6f, 1f), 3f);
            S(t, 0.004f, led, new Vector3(R + 0.012f, cy - 0.07f, 0.02f), default, 6, 4);
        }

        private static void Ringlicht(Transform t, Material dark, Material grey)
        {
            var glow = Mats.Emit(new Color(1f, 0.97f, 0.9f), 2.5f);
            float h = 0.3f;
            Torus(t, 0.1f, 0.012f, glow, new Vector3(0, h, 0), default, 360f, 0f, 28, 8);
            Torus(t, 0.1f, 0.0135f, dark, new Vector3(0, h, -0.004f), default, 360f, 0f, 28, 8).transform.localScale = new Vector3(1f, 1f, 0.6f);
            // Handyhalter in der Mitte
            B(t, new Vector3(0.006f, 0.1f, 0.006f), grey, new Vector3(0, h - 0.05f, 0), default, 0f);
            B(t, new Vector3(0.05f, 0.09f, 0.008f), dark, new Vector3(0, h, 0.004f), default, 0.003f);
            B(t, new Vector3(0.044f, 0.082f, 0.002f), Mats.Emit(new Color(0.4f, 0.6f, 0.95f), 1.2f), new Vector3(0, h, 0.009f), default, 0f);
            // Stange + Stativ
            C(t, 0.006f, h - 0.1f, grey, new Vector3(0, (h - 0.1f) / 2f + 0.05f, -0.004f), default, 8);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f;
                var leg = Props.Node(t, "Leg", new Vector3(0, 0.05f, -0.004f), a);
                C(leg.transform, 0.004f, 0.08f, dark, new Vector3(0, -0.025f, 0.03f), new Vector3(52, 0, 0), 6);
            }
        }

        private static void Katzenbrunnen(Transform t, Material main, Material white)
        {
            var water = Mats.Glass(new Color(0.45f, 0.75f, 0.95f, 0.55f));
            Cone(t, 0.11f, 0.1f, 0.07f, main, new Vector3(0, 0.035f, 0), default, 24);
            Torus(t, 0.105f, 0.008f, main, new Vector3(0, 0.07f, 0), new Vector3(90, 0, 0), 360f, 0f, 24, 6);
            C(t, 0.1f, 0.004f, water, new Vector3(0, 0.066f, 0), default, 24);
            // Kuppel mit Blüte
            S(t, 0.055f, white, new Vector3(0, 0.07f, 0), new Vector3(1f, 0.8f, 1f), 16, 8);
            C(t, 0.012f, 0.05f, white, new Vector3(0, 0.13f, 0), default, 10);
            var petal = Mats.Std(new Color(0.98f, 0.98f, 0.98f), 0.3f);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                S(t, 0.012f, petal, new Vector3(Mathf.Cos(a) * 0.016f, 0.155f, Mathf.Sin(a) * 0.016f), new Vector3(1f, 0.4f, 1f), 8, 4);
            }
            // Wasserstrahl
            Cone(t, 0.01f, 0.018f, 0.04f, water, new Vector3(0, 0.175f, 0), default, 10);
            Cone(t, 0.004f, 0.006f, 0.1f, water, new Vector3(0.02f, 0.11f, 0.03f), new Vector3(20, 0, -10), 8);
            // Fischpfote-Aufkleber
            S(t, 0.012f, Mats.Std(Color.white, 0.4f), new Vector3(0, 0.035f, 0.102f), new Vector3(1f, 1f, 0.2f), 8, 4);
        }

        private static void Haltung(Transform t, Material main, Material dark, Color col)
        {
            // Mini-Torso (Büste) mit überkreuzten Gurten auf dem Rücken (zeigt nach vorne).
            var bust = Mats.Std(new Color(0.85f, 0.8f, 0.74f), 0.75f);
            C(t, 0.035f, 0.02f, Mats.Std(new Color(0.3f, 0.25f, 0.2f), 0.6f), new Vector3(0, 0.01f, 0), default, 14);
            C(t, 0.008f, 0.04f, Mats.Std(new Color(0.3f, 0.25f, 0.2f), 0.6f), new Vector3(0, 0.04f, 0), default, 8);
            S(t, 0.065f, bust, new Vector3(0, 0.12f, 0), new Vector3(1.25f, 1.1f, 0.7f), 16, 8);
            C(t, 0.02f, 0.03f, bust, new Vector3(0, 0.2f, 0), default, 10);
            // Schulterschlaufen
            Torus(t, 0.035f, 0.007f, main, new Vector3(-0.045f, 0.175f, 0), new Vector3(0, 90, 0), 360f, 0f, 16, 6);
            Torus(t, 0.035f, 0.007f, main, new Vector3(0.045f, 0.175f, 0), new Vector3(0, 90, 0), 360f, 0f, 16, 6);
            // Kreuz auf dem Rücken (+Z = Rücken zum Betrachter)
            B(t, new Vector3(0.016f, 0.13f, 0.006f), main, new Vector3(0, 0.12f, 0.047f), new Vector3(0, 0, 32f), 0.002f);
            B(t, new Vector3(0.016f, 0.13f, 0.006f), main, new Vector3(0, 0.12f, 0.047f), new Vector3(0, 0, -32f), 0.002f);
            B(t, new Vector3(0.04f, 0.04f, 0.008f), Mats.Std(col.Darkened(0.4f), 0.6f), new Vector3(0, 0.12f, 0.05f), default, 0.003f);
            B(t, new Vector3(0.15f, 0.018f, 0.006f), dark, new Vector3(0, 0.07f, 0.043f), default, 0.002f);
        }

        private static void Smartwatch(Transform t, Material main, Material dark, Material grey)
        {
            // Uhr auf Präsentationskissen, Display leuchtet.
            const float R = 0.034f, cy = 0.04f;
            var band = Torus(t, R, 0.006f, main, new Vector3(0, cy, 0), new Vector3(0, 90, 0), 360f, 0f, 22, 6);
            band.transform.localScale = new Vector3(1f, 1f, 2.6f); // flaches Armband (lokal Z -> Breite)
            B(t, new Vector3(0.046f, 0.054f, 0.014f), grey, new Vector3(0, cy, R + 0.004f), default, 0.008f);
            B(t, new Vector3(0.04f, 0.048f, 0.003f), dark, new Vector3(0, cy, R + 0.011f), default, 0.006f);
            var screen = Mats.Emit(new Color(0.25f, 0.85f, 0.95f), 1.6f);
            C(t, 0.012f, 0.002f, screen, new Vector3(0, cy + 0.004f, R + 0.0128f), new Vector3(90, 0, 0), 14);
            B(t, new Vector3(0.022f, 0.004f, 0.002f), Mats.Emit(Color.white, 1.5f), new Vector3(0, cy - 0.014f, R + 0.0128f), default, 0f);
            C(t, 0.004f, 0.006f, grey, new Vector3(0.026f, cy + 0.006f, R + 0.004f), new Vector3(0, 0, 90), 8);
            // Kissen
            B(t, new Vector3(0.06f, 0.022f, 0.05f), Mats.Std(new Color(0.15f, 0.15f, 0.17f), 0.9f), new Vector3(0, 0.011f, 0), default, 0.008f);
        }

        private static void Beamer(Transform t, Material main, Material dark, Material grey)
        {
            B(t, new Vector3(0.15f, 0.065f, 0.13f), main, new Vector3(0, 0.0405f, 0), default, 0.014f);
            B(t, new Vector3(0.152f, 0.012f, 0.132f), Mats.Std(new Color(0.92f, 0.92f, 0.9f), 0.4f), new Vector3(0, 0.068f, 0), default, 0.005f);
            C(t, 0.026f, 0.02f, dark, new Vector3(0.03f, 0.04f, 0.072f), new Vector3(90, 0, 0), 18);
            C(t, 0.018f, 0.004f, Mats.Emit(new Color(0.75f, 0.85f, 1f), 1.8f), new Vector3(0.03f, 0.04f, 0.083f), new Vector3(90, 0, 0), 16);
            Torus(t, 0.024f, 0.003f, grey, new Vector3(0.03f, 0.04f, 0.083f), default, 360f, 0f, 18, 6);
            for (int i = 0; i < 4; i++)
                B(t, new Vector3(0.03f, 0.004f, 0.003f), dark, new Vector3(-0.04f, 0.025f + i * 0.01f, 0.066f), default, 0f);
            for (int i = 0; i < 3; i++)
                C(t, 0.006f, 0.004f, grey, new Vector3(-0.04f + i * 0.012f, 0.0755f, -0.03f), default, 8);
            C(t, 0.008f, 0.008f, dark, new Vector3(0.055f, 0.004f, 0.045f), default, 8);
            C(t, 0.008f, 0.008f, dark, new Vector3(-0.055f, 0.004f, 0.045f), default, 8);
            C(t, 0.008f, 0.008f, dark, new Vector3(0f, 0.004f, -0.05f), default, 8);
        }

        private static void Drohne(Transform t, Material main, Material dark, Material grey)
        {
            float y = 0.05f;
            B(t, new Vector3(0.07f, 0.03f, 0.11f), main, new Vector3(0, y, 0), default, 0.01f);
            B(t, new Vector3(0.055f, 0.012f, 0.08f), dark, new Vector3(0, y + 0.018f, -0.005f), default, 0.005f);
            // Gimbal-Kamera
            S(t, 0.014f, dark, new Vector3(0, y - 0.022f, 0.045f), default, 10, 6);
            C(t, 0.007f, 0.004f, Mats.Glass(new Color(0.2f, 0.3f, 0.5f, 0.9f)), new Vector3(0, y - 0.022f, 0.058f), new Vector3(90, 0, 0), 10);
            var rotors = new List<Transform>();
            var blade = Mats.Std(new Color(0.85f, 0.85f, 0.88f, 0.9f), 0.4f);
            for (int i = 0; i < 4; i++)
            {
                float a = 45f + i * 90f;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var arm = Props.Node(t, "Arm", new Vector3(0, y, 0), a);
                B(arm.transform, new Vector3(0.012f, 0.01f, 0.11f), dark, new Vector3(0, 0, 0.06f), default, 0.003f);
                var tip = new Vector3(0, y, 0) + dir * 0.115f;
                C(t, 0.011f, 0.018f, grey, tip + new Vector3(0, 0.006f, 0), default, 10);
                C(t, 0.003f, 0.05f, dark, tip + new Vector3(0, -0.025f, 0), default, 6);
                var rot = Props.Node(t, "Rotor", tip + new Vector3(0, 0.017f, 0), i * 37f);
                B(rot.transform, new Vector3(0.11f, 0.002f, 0.012f), blade, Vector3.zero, default, 0f);
                B(rot.transform, new Vector3(0.012f, 0.002f, 0.11f), blade, Vector3.zero, default, 0f);
                rotors.Add(rot.transform);
                S(t, 0.003f, Mats.Emit(i < 2 ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.2f, 0.2f), 3f), tip + new Vector3(0, -0.004f, 0), default, 6, 4);
            }
            var anim = t.gameObject.AddComponent<ProductAnim>();
            anim.Spin = rotors.ToArray();
        }

        private static void Toaster(Transform t, Material main, Material dark, Material grey)
        {
            B(t, new Vector3(0.2f, 0.13f, 0.12f), main, new Vector3(0, 0.07f, 0), default, 0.025f);
            B(t, new Vector3(0.21f, 0.008f, 0.125f), grey, new Vector3(0, 0.008f, 0), default, 0.004f);
            B(t, new Vector3(0.15f, 0.006f, 0.022f), dark, new Vector3(0, 0.135f, 0.025f), default, 0.002f);
            B(t, new Vector3(0.15f, 0.006f, 0.022f), dark, new Vector3(0, 0.135f, -0.025f), default, 0.002f);
            // Handy steckt im Schlitz (Selfie!)
            B(t, new Vector3(0.075f, 0.12f, 0.009f), dark, new Vector3(-0.03f, 0.16f, -0.025f), default, 0.005f);
            B(t, new Vector3(0.066f, 0.105f, 0.002f), Mats.Emit(new Color(0.95f, 0.65f, 0.4f), 1.2f), new Vector3(-0.03f, 0.165f, -0.0198f), default, 0f);
            // Toast mit eingebranntem Selfie
            var toast = Mats.Std(new Color(0.85f, 0.62f, 0.32f), 0.9f);
            var burnt = Mats.Std(new Color(0.35f, 0.2f, 0.1f), 0.9f);
            B(t, new Vector3(0.09f, 0.085f, 0.012f), toast, new Vector3(0.045f, 0.17f, 0.025f), new Vector3(0, 0, -6f), 0.01f);
            S(t, 0.006f, burnt, new Vector3(0.033f, 0.18f, 0.032f), new Vector3(1f, 1f, 0.3f), 8, 4);
            S(t, 0.006f, burnt, new Vector3(0.056f, 0.178f, 0.032f), new Vector3(1f, 1f, 0.3f), 8, 4);
            B(t, new Vector3(0.03f, 0.005f, 0.004f), burnt, new Vector3(0.044f, 0.158f, 0.032f), new Vector3(0, 0, -6f), 0f);
            // Hebel + Drehknopf
            B(t, new Vector3(0.015f, 0.025f, 0.012f), dark, new Vector3(0.105f, 0.1f, 0), default, 0.003f);
            C(t, 0.012f, 0.008f, grey, new Vector3(0.06f, 0.05f, 0.062f), new Vector3(90, 0, 0), 12);
        }

        private static void Bartglitzer(Transform t, Color col, Material white)
        {
            var glass = Mats.Glass(new Color(0.85f, 0.9f, 1f, 0.35f));
            var glitter = Mats.Std(col, 0.15f, 0.9f);
            C(t, 0.028f, 0.045f, glitter, new Vector3(0, 0.025f, 0), default, 16);
            C(t, 0.032f, 0.06f, glass, new Vector3(0, 0.03f, 0), default, 16);
            C(t, 0.034f, 0.016f, Mats.Std(col.Darkened(0.35f), 0.3f, 0.6f), new Vector3(0, 0.068f, 0), default, 16);
            C(t, 0.0325f, 0.02f, white, new Vector3(0, 0.03f, 0), default, 16);
            var spark = Mats.Emit(Color.Lerp(col, Color.white, 0.5f), 3.5f);
            Vector3[] sp = { new Vector3(0.02f, 0.095f, 0.01f), new Vector3(-0.018f, 0.105f, -0.006f), new Vector3(0.004f, 0.12f, 0.012f), new Vector3(0.03f, 0.115f, -0.01f), new Vector3(-0.026f, 0.088f, 0.016f) };
            for (int i = 0; i < sp.Length; i++)
            {
                B(t, new Vector3(0.012f, 0.003f, 0.003f), spark, sp[i], new Vector3(0, 0, 45f + i * 20f), 0f);
                B(t, new Vector3(0.003f, 0.012f, 0.003f), spark, sp[i], new Vector3(0, 0, 45f + i * 20f), 0f);
            }
        }

        private static void Giesskanne(Transform t, Material main, Material dark, Material grey)
        {
            Cone(t, 0.05f, 0.06f, 0.12f, main, new Vector3(0, 0.06f, 0), default, 18);
            C(t, 0.052f, 0.01f, grey, new Vector3(0, 0.12f, 0), default, 18);
            C(t, 0.035f, 0.008f, dark, new Vector3(0, 0.126f, 0), default, 14);
            // Tülle nach vorne (+Z) mit Brause
            Cone(t, 0.008f, 0.014f, 0.14f, main, new Vector3(0, 0.1f, 0.095f), new Vector3(55, 0, 0), 10);
            Cone(t, 0.022f, 0.01f, 0.025f, grey, new Vector3(0, 0.145f, 0.155f), new Vector3(55, 0, 0), 12);
            // Griff hinten
            Torus(t, 0.045f, 0.008f, main, new Vector3(0, 0.1f, -0.045f), new Vector3(0, 90, 0), 200f, -10f, 16, 6);
            // "Smart": Display + Antenne
            B(t, new Vector3(0.035f, 0.025f, 0.004f), dark, new Vector3(0.03f, 0.07f, 0.045f), new Vector3(0, 33, 0), 0.003f);
            B(t, new Vector3(0.029f, 0.019f, 0.002f), Mats.Emit(new Color(0.4f, 1f, 0.6f), 1.6f), new Vector3(0.0315f, 0.07f, 0.0475f), new Vector3(0, 33, 0), 0f);
            C(t, 0.002f, 0.05f, dark, new Vector3(-0.03f, 0.15f, 0f), default, 6);
            S(t, 0.006f, Mats.Emit(new Color(0.2f, 0.6f, 1f), 3f), new Vector3(-0.03f, 0.178f, 0f), default, 8, 4);
        }

        private static void Wackeldackel(Transform t, Color col, Material dark)
        {
            var fur = Mats.Std(col, 0.85f);
            var furDark = Mats.Std(col.Darkened(0.45f), 0.85f);
            // Sockel
            B(t, new Vector3(0.13f, 0.012f, 0.06f), Mats.Std(new Color(0.2f, 0.2f, 0.22f), 0.5f), new Vector3(0, 0.006f, 0), default, 0.004f);
            // Körper lang entlang X, Beine kurz
            S(t, 0.03f, fur, new Vector3(0, 0.05f, 0), new Vector3(2.0f, 0.9f, 0.9f), 14, 8);
            for (int i = 0; i < 4; i++)
                C(t, 0.008f, 0.03f, fur, new Vector3(i < 2 ? 0.04f : -0.04f, 0.026f, i % 2 == 0 ? 0.015f : -0.015f), default, 8);
            Cone(t, 0.002f, 0.006f, 0.04f, fur, new Vector3(-0.07f, 0.065f, 0), new Vector3(0, 0, 50f), 6);
            // Halsband mit Bluetooth-LED
            Torus(t, 0.019f, 0.003f, Mats.Std(new Color(0.15f, 0.35f, 0.9f), 0.4f), new Vector3(0.05f, 0.065f, 0), new Vector3(0, 90, 20f), 360f, 0f, 14, 5);
            S(t, 0.004f, Mats.Emit(new Color(0.3f, 0.6f, 1f), 3f), new Vector3(0.056f, 0.06f, 0.018f), default, 6, 4);
            // Wackelkopf an Feder
            var head = Props.Node(t, "Head", new Vector3(0.055f, 0.075f, 0));
            var ht = head.transform;
            C(ht, 0.003f, 0.02f, Mats.Std(new Color(0.7f, 0.7f, 0.72f), 0.3f, 0.8f), new Vector3(0, 0.005f, 0), default, 6);
            S(ht, 0.024f, fur, new Vector3(0.008f, 0.025f, 0), new Vector3(1.1f, 1f, 1f), 12, 8);
            S(ht, 0.012f, fur, new Vector3(0.034f, 0.018f, 0), new Vector3(1.6f, 0.9f, 0.9f), 10, 6);
            S(ht, 0.006f, dark, new Vector3(0.053f, 0.02f, 0), default, 8, 4);
            S(ht, 0.004f, dark, new Vector3(0.024f, 0.033f, 0.013f), default, 6, 4);
            S(ht, 0.004f, dark, new Vector3(0.024f, 0.033f, -0.013f), default, 6, 4);
            S(ht, 0.012f, furDark, new Vector3(0.0f, 0.02f, 0.024f), new Vector3(0.8f, 1.8f, 0.35f), 8, 4);
            S(ht, 0.012f, furDark, new Vector3(0.0f, 0.02f, -0.024f), new Vector3(0.8f, 1.8f, 0.35f), 8, 4);
            // Dackel schaut nach vorne (+Z): ganzes Tier um -90° drehen
            var all = new List<Transform>();
            for (int i = 0; i < t.childCount; i++) all.Add(t.GetChild(i));
            var turn = Props.Node(t, "Turn", Vector3.zero, -90f);
            foreach (var c in all) c.SetParent(turn.transform, false);
            var anim = t.gameObject.AddComponent<ProductAnim>();
            anim.Bob = ht;
        }
    }

    /// <summary>Kleine, billige Animationen der Produktmodelle (Rotoren drehen, Dackelkopf wackelt).</summary>
    public sealed class ProductAnim : MonoBehaviour
    {
        public Transform[] Spin;
        public Transform Bob;
        private Quaternion _bobBase;
        private float _phase;
        private Renderer _vis;

        private void Start()
        {
            if (Bob != null) _bobBase = Bob.localRotation;
            _phase = Random.value * 10f;
            _vis = GetComponentInChildren<Renderer>();
        }

        private void Update()
        {
            if (_vis != null && !_vis.isVisible) return;
            float tm = Time.time + _phase;
            if (Spin != null)
                for (int i = 0; i < Spin.Length; i++)
                    if (Spin[i] != null) Spin[i].Rotate(0f, (i % 2 == 0 ? 1f : -1f) * 900f * Time.deltaTime, 0f, Space.Self);
            if (Bob != null)
                Bob.localRotation = _bobBase * Quaternion.Euler(Mathf.Sin(tm * 7f) * 6f, 0f, Mathf.Sin(tm * 5.3f) * 12f);
        }
    }
}
