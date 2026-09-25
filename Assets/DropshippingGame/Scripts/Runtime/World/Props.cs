using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// Baukasten für alle 3D-Objekte der Welt (Möbel, Fahrzeuge, Bäume, Deko ...).
    /// Alles aus Grundformen mit abgefasten Kanten und prozeduralen Materialien - keine Modelldateien.
    /// Konvention wie im Godot-Original: Ursprung am Boden, Vorderseite zeigt nach +Z.
    /// </summary>
    public static class Props
    {
        // ---- Grundformen -------------------------------------------------------------------------
        public static GameObject Node(Transform parent, string name, Vector3 pos = default, float rotY = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            return go;
        }

        private static float AutoBevel(Vector3 size)
        {
            float m = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            if (m < 0.02f) return 0f;
            return Mathf.Clamp(m * 0.08f, 0.004f, 0.02f);
        }

        /// <summary>Quader. bevel &lt; 0 = automatisch (kleine Fase für einen hochwertigen Look).</summary>
        public static GameObject Box(Transform parent, Vector3 size, Material mat, Vector3 pos = default, Vector3 rotDeg = default,
            float bevel = -1f, bool shadows = true, bool capUvZero = false)
        {
            var go = new GameObject("Box");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(rotDeg);
            go.AddComponent<MeshFilter>().sharedMesh = MeshKit.Box(size, bevel < 0f ? AutoBevel(size) : bevel, capUvZero);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            if (!shadows) r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        public static GameObject Cyl(Transform parent, float rTop, float rBottom, float h, Material mat, Vector3 pos = default,
            Vector3 rotDeg = default, int segments = 16)
        {
            var go = new GameObject("Cyl");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(rotDeg);
            go.AddComponent<MeshFilter>().sharedMesh = MeshKit.Cylinder(rTop, rBottom, h, segments);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        public static GameObject Sphere(Transform parent, float r, Material mat, Vector3 pos = default, int segments = 16, int rings = 8, bool flat = false)
        {
            var go = new GameObject("Sphere");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = MeshKit.Sphere(r, segments, rings, flat);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Unsichtbarer statischer Kollisionskörper (Wände, Möbel ...).</summary>
        public static GameObject Collider(Transform parent, Vector3 size, Vector3 pos, Vector3 rotDeg = default)
        {
            var go = new GameObject("Collider");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(rotDeg);
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        /// <summary>Quader mit Kollision in einem Rutsch.</summary>
        public static GameObject Solid(Transform parent, Vector3 size, Material mat, Vector3 pos, Vector3 rotDeg = default, float bevel = -1f,
            bool capUvZero = false)
        {
            var go = Box(parent, size, mat, pos, rotDeg, bevel, true, capUvZero);
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        public static Label3D Label(Transform parent, string text, float size, Color color, Vector3 pos, bool billboard = true,
            float maxDistance = 0f, float glow = 1f)
        {
            return Label3D.Create(parent, text, size, color, pos, billboard, maxDistance, false, glow);
        }

        public static Light PointLight(Transform parent, Vector3 pos, Color color, float intensity, float range, bool shadows = false)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.renderMode = LightRenderMode.Auto;
            return l;
        }

        // ---- Möbel -----------------------------------------------------------------------------------
        public static GameObject Table(Transform parent, float w, float d, float h, Material top, Material leg)
        {
            var n = Node(parent, "Table");
            var t = n.transform;
            Box(t, new Vector3(w, 0.05f, d), top, new Vector3(0, h - 0.025f, 0));
            float ix = w / 2f - 0.07f, iz = d / 2f - 0.07f;
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Box(t, new Vector3(0.05f, h - 0.05f, 0.05f), leg, new Vector3(sx * ix, (h - 0.05f) / 2f, sz * iz));
            return n;
        }

        public static GameObject Chair(Transform parent, Material mat)
        {
            var n = Node(parent, "Chair");
            var t = n.transform;
            Box(t, new Vector3(0.44f, 0.05f, 0.44f), mat, new Vector3(0, 0.46f, 0));
            Box(t, new Vector3(0.44f, 0.48f, 0.05f), mat, new Vector3(0, 0.72f, -0.2f));
            foreach (float sx in new[] { -0.19f, 0.19f })
            foreach (float sz in new[] { -0.19f, 0.19f })
                Box(t, new Vector3(0.04f, 0.44f, 0.04f), Mats.DarkMetal(), new Vector3(sx, 0.22f, sz));
            return n;
        }

        public static GameObject Stool(Transform parent)
        {
            var n = Node(parent, "Stool");
            var t = n.transform;
            Cyl(t, 0.2f, 0.2f, 0.06f, Mats.Std(new Color(0.8f, 0.15f, 0.15f), 0.5f), new Vector3(0, 0.74f, 0));
            Cyl(t, 0.03f, 0.03f, 0.72f, Mats.Metal(), new Vector3(0, 0.36f, 0));
            Cyl(t, 0.18f, 0.2f, 0.03f, Mats.Metal(), new Vector3(0, 0.015f, 0));
            return n;
        }

        public static GameObject Pallet(Transform parent)
        {
            var n = Node(parent, "Pallet");
            var t = n.transform;
            var w = Mats.Planks(new Color(0.72f, 0.56f, 0.36f), 0.14f);
            for (int i = 0; i < 5; i++) Box(t, new Vector3(1.2f, 0.025f, 0.14f), w, new Vector3(0, 0.137f, -0.4f + i * 0.2f));
            foreach (float sx in new[] { -0.52f, 0f, 0.52f }) Box(t, new Vector3(0.1f, 0.1f, 1f), w, new Vector3(sx, 0.07f, 0));
            return n;
        }

        public static GameObject Crate(Transform parent, Color color, Vector3 size = default)
        {
            if (size == default) size = new Vector3(0.6f, 0.45f, 0.45f);
            var n = Node(parent, "Crate");
            var t = n.transform;
            Box(t, size, Mats.Cardboard(), new Vector3(0, size.y / 2f, 0));
            Box(t, new Vector3(size.x + 0.005f, 0.06f, size.z * 0.35f), Mats.Std(color, 0.6f), new Vector3(0, size.y * 0.55f, 0), default, 0f);
            Box(t, new Vector3(size.x * 0.18f, 0.005f, size.z + 0.005f), Mats.Std(new Color(0.8f, 0.72f, 0.5f), 0.4f), new Vector3(0, size.y + 0.002f, 0), default, 0f);
            return n;
        }

        public static GameObject Plant(Transform parent, float scale = 1f)
        {
            var n = Node(parent, "Plant");
            var t = n.transform;
            Cyl(t, 0.16f * scale, 0.12f * scale, 0.3f * scale, Mats.Std(new Color(0.85f, 0.82f, 0.78f), 0.6f), new Vector3(0, 0.15f * scale, 0));
            var leaf = Mats.Foliage(new Color(0.25f, 0.55f, 0.28f));
            Sphere(t, 0.28f * scale, leaf, new Vector3(0, 0.55f * scale, 0), 7, 4, true);
            Sphere(t, 0.2f * scale, leaf, new Vector3(0.12f * scale, 0.75f * scale, 0.05f * scale), 6, 3, true);
            Sphere(t, 0.18f * scale, leaf, new Vector3(-0.1f * scale, 0.8f * scale, -0.06f * scale), 6, 3, true);
            return n;
        }

        public static GameObject Tree(Transform parent, float scale = 1f, int variant = 0)
        {
            var n = Node(parent, "Tree");
            var t = n.transform;
            Cyl(t, 0.12f * scale, 0.18f * scale, 2.2f * scale, Mats.Std(new Color(0.36f, 0.25f, 0.16f), 0.9f), new Vector3(0, 1.1f * scale, 0), default, 7);
            Color[] greens = { new Color(0.24f, 0.48f, 0.22f), new Color(0.3f, 0.52f, 0.2f), new Color(0.2f, 0.42f, 0.26f) };
            var leaf = Mats.Foliage(greens[variant % 3]);
            if (variant % 2 == 0)
            {
                Sphere(t, 1.25f * scale, leaf, new Vector3(0, 2.9f * scale, 0), 7, 4, true);
                Sphere(t, 0.9f * scale, leaf, new Vector3(0.55f * scale, 3.5f * scale, 0.2f * scale), 6, 4, true);
                Sphere(t, 0.8f * scale, leaf, new Vector3(-0.5f * scale, 3.3f * scale, -0.3f * scale), 6, 4, true);
            }
            else
            {
                Cyl(t, 0f, 1.3f * scale, 2.2f * scale, leaf, new Vector3(0, 2.6f * scale, 0), default, 7);
                Cyl(t, 0f, 0.95f * scale, 1.8f * scale, leaf, new Vector3(0, 3.6f * scale, 0), default, 7);
            }
            return n;
        }

        public static GameObject Bush(Transform parent, float scale = 1f)
        {
            var n = Node(parent, "Bush");
            var t = n.transform;
            var leaf = Mats.Foliage(new Color(0.26f, 0.5f, 0.24f));
            Sphere(t, 0.6f * scale, leaf, new Vector3(0, 0.35f * scale, 0), 7, 4, true);
            Sphere(t, 0.45f * scale, leaf, new Vector3(0.45f * scale, 0.3f * scale, 0.1f), 6, 3, true);
            return n;
        }

        /// <summary>Straßenlaterne (Licht zeigt nach +Z). light = das Lampenlicht (nachts an).</summary>
        public static GameObject LampPost(Transform parent, out Light light)
        {
            var n = Node(parent, "LampPost");
            var t = n.transform;
            var pole = Mats.Std(new Color(0.16f, 0.17f, 0.19f), 0.4f, 0.6f);
            Cyl(t, 0.06f, 0.09f, 4.2f, pole, new Vector3(0, 2.1f, 0), default, 8);
            Box(t, new Vector3(0.08f, 0.08f, 1.1f), pole, new Vector3(0, 4.15f, 0.5f));
            Box(t, new Vector3(0.34f, 0.12f, 0.5f), pole, new Vector3(0, 4.08f, 1f));
            Box(t, new Vector3(0.28f, 0.03f, 0.42f), Mats.Lamp(new Color(1f, 0.85f, 0.6f)), new Vector3(0, 4.01f, 1f), default, 0f, false);
            light = PointLight(t, new Vector3(0, 3.8f, 1f), new Color(1f, 0.82f, 0.6f), 0f, 11f);
            light.enabled = false;
            return n;
        }

        public static GameObject Bench(Transform parent)
        {
            var n = Node(parent, "Bench");
            var t = n.transform;
            var w = Mats.Planks(new Color(0.55f, 0.36f, 0.22f), 0.1f);
            for (int i = 0; i < 3; i++) Box(t, new Vector3(1.8f, 0.04f, 0.12f), w, new Vector3(0, 0.45f, -0.14f + i * 0.14f));
            for (int i = 0; i < 2; i++) Box(t, new Vector3(1.8f, 0.12f, 0.04f), w, new Vector3(0, 0.66f + i * 0.16f, -0.26f));
            foreach (float sx in new[] { -0.8f, 0.8f }) Box(t, new Vector3(0.06f, 0.45f, 0.5f), Mats.DarkMetal(), new Vector3(sx, 0.225f, -0.05f));
            return n;
        }

        public static GameObject TrashBin(Transform parent, Color? color = null)
        {
            Color c = color ?? new Color(0.2f, 0.4f, 0.28f);
            var n = Node(parent, "TrashBin");
            var t = n.transform;
            Box(t, new Vector3(0.62f, 0.9f, 0.62f), Mats.Std(c, 0.6f), new Vector3(0, 0.45f, 0));
            Box(t, new Vector3(0.66f, 0.06f, 0.66f), Mats.Std(c.Darkened(0.3f), 0.6f), new Vector3(0, 0.93f, 0));
            return n;
        }

        public static GameObject Fence(Transform parent, float length, Color? color = null)
        {
            Color c = color ?? new Color(0.3f, 0.3f, 0.32f);
            var n = Node(parent, "Fence");
            var t = n.transform;
            var m = Mats.Std(c, 0.5f, 0.5f);
            int posts = (int)(length / 2f) + 1;
            for (int i = 0; i < posts; i++)
                Box(t, new Vector3(0.06f, 1.3f, 0.06f), m, new Vector3(-length / 2f + i * (length / Mathf.Max(posts - 1, 1)), 0.65f, 0));
            Box(t, new Vector3(length, 0.05f, 0.05f), m, new Vector3(0, 1.2f, 0));
            Box(t, new Vector3(length, 0.05f, 0.05f), m, new Vector3(0, 0.5f, 0));
            Box(t, new Vector3(length, 0.9f, 0.01f), Mats.Std(new Color(0.35f, 0.36f, 0.38f, 0.35f), 0.4f, 0.6f), new Vector3(0, 0.75f, 0), default, 0f, false);
            return n;
        }

        public static GameObject Barrier(Transform parent)
        {
            var n = Node(parent, "Barrier");
            var t = n.transform;
            for (int i = 0; i < 5; i++)
            {
                var c = i % 2 == 0 ? new Color(0.9f, 0.15f, 0.12f) : new Color(0.95f, 0.95f, 0.95f);
                Box(t, new Vector3(0.5f, 0.25f, 0.08f), Mats.Std(c, 0.5f), new Vector3(-1f + i * 0.5f, 0.85f, 0));
            }
            foreach (float sx in new[] { -1.1f, 1.1f }) Box(t, new Vector3(0.08f, 1f, 0.4f), Mats.Std(new Color(0.95f, 0.95f, 0.95f), 0.6f), new Vector3(sx, 0.5f, 0));
            return n;
        }

        private static void Wheel(Transform parent, float radius, float width, Vector3 pos)
        {
            var w = Cyl(parent, radius, radius, width, Mats.Std(new Color(0.06f, 0.06f, 0.07f), 0.8f), pos, new Vector3(90, 0, 0), 14);
            Cyl(w.transform, radius * 0.55f, radius * 0.55f, width + 0.01f, Mats.Metal(), Vector3.zero, default, 10);
        }

        /// <summary>Auto (Front zeigt in +X).</summary>
        public static GameObject Car(Transform parent, Color color, bool sporty = false)
        {
            var n = Node(parent, "Car");
            var t = n.transform;
            var paint = Mats.Std(color, 0.22f, 0.45f);
            var glass = Mats.Std(new Color(0.12f, 0.16f, 0.22f), 0.08f, 0.5f);
            float h = sporty ? 0.55f : 0.7f;
            float len = sporty ? 4.2f : 4f;
            Box(t, new Vector3(len, h, 1.8f), paint, new Vector3(0, 0.35f + h / 2f, 0), default, 0.08f);
            float cabLen = sporty ? 1.8f : 2.2f;
            float cabH = sporty ? 0.45f : 0.6f;
            Box(t, new Vector3(cabLen, cabH, 1.6f), glass, new Vector3(-0.2f, 0.35f + h + cabH / 2f, 0), default, 0.06f);
            Box(t, new Vector3(cabLen - 0.3f, 0.06f, 1.62f), paint, new Vector3(-0.2f, 0.35f + h + cabH, 0), default, 0.02f);
            foreach (float fx in new[] { 1.3f, -1.3f })
            foreach (float fz in new[] { 0.85f, -0.85f })
                Wheel(t, 0.36f, 0.26f, new Vector3(fx, 0.36f, fz));
            float front = len / 2f + 0.02f;
            Box(t, new Vector3(0.05f, 0.14f, 0.4f), Mats.Emit(new Color(1f, 0.95f, 0.8f), 3f), new Vector3(front, 0.62f, 0.6f), default, 0f, false);
            Box(t, new Vector3(0.05f, 0.14f, 0.4f), Mats.Emit(new Color(1f, 0.95f, 0.8f), 3f), new Vector3(front, 0.62f, -0.6f), default, 0f, false);
            Box(t, new Vector3(0.05f, 0.12f, 0.4f), Mats.Emit(new Color(1f, 0.1f, 0.1f), 2f), new Vector3(-front, 0.65f, 0.6f), default, 0f, false);
            Box(t, new Vector3(0.05f, 0.12f, 0.4f), Mats.Emit(new Color(1f, 0.1f, 0.1f), 2f), new Vector3(-front, 0.65f, -0.6f), default, 0f, false);
            if (sporty)
            {
                Box(t, new Vector3(0.25f, 0.05f, 1.7f), paint, new Vector3(-2f, 1.15f, 0));
                Box(t, new Vector3(0.06f, 0.25f, 0.06f), paint, new Vector3(-2f, 1f, 0.6f));
                Box(t, new Vector3(0.06f, 0.25f, 0.06f), paint, new Vector3(-2f, 1f, -0.6f));
            }
            return n;
        }

        /// <summary>Lieferwagen (Front zeigt in +X).</summary>
        public static GameObject Van(Transform parent, Color color, string text)
        {
            var n = Node(parent, "Van");
            var t = n.transform;
            var paint = Mats.Std(color, 0.3f, 0.3f);
            var glass = Mats.Std(new Color(0.12f, 0.16f, 0.22f), 0.08f, 0.5f);
            Box(t, new Vector3(3.6f, 2.2f, 2f), paint, new Vector3(-0.6f, 1.5f, 0), default, 0.08f);
            Box(t, new Vector3(1.4f, 1.5f, 1.95f), paint, new Vector3(1.9f, 1.15f, 0), default, 0.1f);
            Box(t, new Vector3(0.05f, 0.7f, 1.8f), glass, new Vector3(2.62f, 1.45f, 0), default, 0f);
            Box(t, new Vector3(0.9f, 0.6f, 0.02f), glass, new Vector3(1.9f, 1.45f, 0.99f), default, 0f);
            Box(t, new Vector3(0.9f, 0.6f, 0.02f), glass, new Vector3(1.9f, 1.45f, -0.99f), default, 0f);
            foreach (float fx in new[] { 1.8f, -1.6f })
            foreach (float fz in new[] { 0.92f, -0.92f })
                Wheel(t, 0.42f, 0.3f, new Vector3(fx, 0.42f, fz));
            Box(t, new Vector3(3.6f, 0.2f, 2.02f), Mats.Std(Color.white, 0.4f), new Vector3(-0.6f, 1f, 0), default, 0f);
            foreach (float side in new[] { 1f, -1f })
            {
                var holder = Node(t, "Side", new Vector3(-0.6f, 1.8f, side * 1.03f), side > 0 ? 0f : 180f);
                Label3D.Create(holder.transform, text, 150f, Color.white, Vector3.zero, false);
            }
            Box(t, new Vector3(0.05f, 0.16f, 0.4f), Mats.Emit(new Color(1f, 0.95f, 0.8f), 3f), new Vector3(2.62f, 0.75f, 0.7f), default, 0f, false);
            Box(t, new Vector3(0.05f, 0.16f, 0.4f), Mats.Emit(new Color(1f, 0.95f, 0.8f), 3f), new Vector3(2.62f, 0.75f, -0.7f), default, 0f, false);
            return n;
        }

        // ---- Deko & Lifestyle ------------------------------------------------------------------------
        public static GameObject Poster(Transform parent, string text, Color color)
        {
            var n = Node(parent, "Poster");
            var t = n.transform;
            Box(t, new Vector3(0.8f, 1.1f, 0.02f), Mats.Std(color, 0.7f), Vector3.zero, default, 0f);
            var l = Label3D.Create(t, text, 70f, Color.white, new Vector3(0, 0.1f, 0.02f), false, 0f, false, 1f, 8);
            return n;
        }

        public static GameObject Rug(Transform parent, Color color, Vector2 size = default)
        {
            if (size == default) size = new Vector2(2.4f, 1.6f);
            var n = Node(parent, "Rug");
            var t = n.transform;
            Box(t, new Vector3(size.x, 0.015f, size.y), Mats.Std(color, 0.95f), new Vector3(0, 0.008f, 0), default, 0f, false);
            Box(t, new Vector3(size.x - 0.3f, 0.017f, size.y - 0.3f), Mats.Std(color.Lightened(0.2f), 0.95f), new Vector3(0, 0.009f, 0), default, 0f, false);
            return n;
        }

        public static GameObject FloorLamp(Transform parent)
        {
            var n = Node(parent, "FloorLamp");
            var t = n.transform;
            Cyl(t, 0.18f, 0.2f, 0.04f, Mats.DarkMetal(), new Vector3(0, 0.02f, 0));
            Cyl(t, 0.02f, 0.02f, 1.6f, Mats.DarkMetal(), new Vector3(0, 0.8f, 0));
            Cyl(t, 0.12f, 0.25f, 0.3f, Mats.Emit(new Color(1f, 0.9f, 0.72f), 1.4f), new Vector3(0, 1.65f, 0));
            PointLight(t, new Vector3(0, 1.5f, 0), new Color(1f, 0.85f, 0.65f), 1.6f, 4f);
            return n;
        }

        public static GameObject Billy(Transform parent)
        {
            var n = Node(parent, "Billy");
            var t = n.transform;
            var w = Mats.Std(new Color(0.93f, 0.92f, 0.88f), 0.7f);
            foreach (float sx in new[] { -0.4f, 0.4f }) Box(t, new Vector3(0.03f, 2f, 0.3f), w, new Vector3(sx, 1f, 0));
            for (int i = 0; i < 5; i++) Box(t, new Vector3(0.8f, 0.03f, 0.3f), w, new Vector3(0, 0.05f + i * 0.47f, 0));
            Color[] cols = { new Color(0.7f, 0.2f, 0.2f), new Color(0.2f, 0.4f, 0.7f), new Color(0.85f, 0.7f, 0.2f), new Color(0.3f, 0.6f, 0.35f) };
            for (int i = 0; i < 7; i++) Box(t, new Vector3(0.05f, 0.28f, 0.22f), Mats.Std(cols[i % 4], 0.7f), new Vector3(-0.3f + i * 0.07f, 0.62f, 0));
            var p = Plant(t, 0.6f);
            p.transform.localPosition = new Vector3(0.2f, 1.46f, 0);
            return n;
        }

        public static GameObject Whiteboard(Transform parent)
        {
            var n = Node(parent, "Whiteboard");
            var t = n.transform;
            Box(t, new Vector3(1.6f, 1f, 0.04f), Mats.Std(new Color(0.97f, 0.97f, 0.97f), 0.3f), Vector3.zero);
            Box(t, new Vector3(1.66f, 0.04f, 0.06f), Mats.Metal(), new Vector3(0, -0.52f, 0.02f));
            Label3D.Create(t, "BUSINESSPLAN\n1. Kaufen\n2. Verkaufen\n3. ???\n4. PROFIT", 42f, new Color(0.15f, 0.25f, 0.6f), new Vector3(-0.2f, 0, 0.03f), false);
            Box(t, new Vector3(0.5f, 0.02f, 0.01f), Mats.Std(new Color(0.8f, 0.2f, 0.2f)), new Vector3(0.45f, 0.2f, 0.03f), new Vector3(0, 0, 25), 0f);
            Box(t, new Vector3(0.5f, 0.02f, 0.01f), Mats.Std(new Color(0.8f, 0.2f, 0.2f)), new Vector3(0.5f, -0.05f, 0.03f), new Vector3(0, 0, 40), 0f);
            return n;
        }

        public static GameObject CoffeeMachine(Transform parent)
        {
            var n = Node(parent, "CoffeeMachine");
            var t = n.transform;
            Table(t, 0.7f, 0.5f, 0.9f, Mats.Wood(), Mats.DarkMetal());
            Box(t, new Vector3(0.36f, 0.36f, 0.34f), Mats.Std(new Color(0.75f, 0.76f, 0.78f), 0.2f, 0.9f), new Vector3(0, 1.08f, 0));
            Box(t, new Vector3(0.3f, 0.05f, 0.3f), Mats.DarkMetal(), new Vector3(0, 1.28f, 0));
            Cyl(t, 0.05f, 0.04f, 0.08f, Mats.Std(new Color(0.95f, 0.95f, 0.95f), 0.3f), new Vector3(0.05f, 0.95f, 0.12f));
            Box(t, new Vector3(0.06f, 0.04f, 0.02f), Mats.Emit(new Color(0.3f, 1f, 0.4f), 2f), new Vector3(-0.1f, 1.15f, 0.18f), default, 0f, false);
            return n;
        }

        public static GameObject Sofa(Transform parent, Color color)
        {
            var n = Node(parent, "Sofa");
            var t = n.transform;
            var m = Mats.Std(color, 0.55f);
            Box(t, new Vector3(2f, 0.42f, 0.9f), m, new Vector3(0, 0.3f, 0), default, 0.05f);
            Box(t, new Vector3(2f, 0.55f, 0.22f), m, new Vector3(0, 0.72f, -0.34f), default, 0.05f);
            foreach (float sx in new[] { -0.93f, 0.93f }) Box(t, new Vector3(0.2f, 0.3f, 0.9f), m, new Vector3(sx, 0.6f, 0), default, 0.05f);
            foreach (float sx in new[] { -0.45f, 0.45f }) Box(t, new Vector3(0.82f, 0.12f, 0.62f), Mats.Std(color.Lightened(0.12f), 0.6f), new Vector3(sx, 0.57f, 0.06f), default, 0.04f);
            return n;
        }

        public static GameObject ShoeDisplay(Transform parent)
        {
            var n = Node(parent, "ShoeDisplay");
            var t = n.transform;
            Box(t, new Vector3(0.6f, 0.04f, 0.3f), Mats.Wood(), Vector3.zero);
            Box(t, new Vector3(0.62f, 0.34f, 0.32f), Mats.Glass(), new Vector3(0, 0.19f, 0), default, 0f, false);
            foreach (float sx in new[] { -0.12f, 0.12f })
            {
                Box(t, new Vector3(0.1f, 0.07f, 0.25f), Mats.Std(new Color(0.95f, 0.95f, 0.95f), 0.4f), new Vector3(sx, 0.06f, 0), default, 0.02f);
                Box(t, new Vector3(0.1f, 0.02f, 0.25f), Mats.Std(new Color(0.9f, 0.2f, 0.2f), 0.4f), new Vector3(sx, 0.025f, 0), default, 0f);
            }
            return n;
        }

        public static GameObject GamingChair(Transform parent)
        {
            var n = Node(parent, "GamingChair");
            var t = n.transform;
            var black = Mats.Std(new Color(0.08f, 0.08f, 0.09f), 0.5f);
            var accent = Mats.Emit(new Color(0.2f, 0.9f, 1f), 1.5f);
            Cyl(t, 0.3f, 0.3f, 0.05f, black, new Vector3(0, 0.08f, 0), default, 5);
            Cyl(t, 0.04f, 0.04f, 0.35f, Mats.Metal(), new Vector3(0, 0.28f, 0));
            Box(t, new Vector3(0.55f, 0.1f, 0.52f), black, new Vector3(0, 0.5f, 0), default, 0.03f);
            Box(t, new Vector3(0.52f, 0.8f, 0.1f), black, new Vector3(0, 0.95f, -0.24f), new Vector3(-8, 0, 0), 0.03f);
            Box(t, new Vector3(0.04f, 0.72f, 0.02f), accent, new Vector3(-0.2f, 0.95f, -0.18f), new Vector3(-8, 0, 0), 0f, false);
            Box(t, new Vector3(0.04f, 0.72f, 0.02f), accent, new Vector3(0.2f, 0.95f, -0.18f), new Vector3(-8, 0, 0), 0f, false);
            return n;
        }

        public static GameObject NeonSign(Transform parent, string text, Color color)
        {
            var n = Node(parent, "Neon");
            var t = n.transform;
            Box(t, new Vector3(2f, 0.6f, 0.04f), Mats.Std(new Color(0.05f, 0.05f, 0.06f), 0.3f), Vector3.zero);
            Label3D.Create(t, text, 120f, color, new Vector3(0, 0, 0.03f), false, 0f, false, 3.5f);
            PointLight(t, new Vector3(0, 0, 0.5f), color, 1.6f, 3.5f);
            return n;
        }

        public static GameObject VendingMachine(Transform parent)
        {
            var n = Node(parent, "Vending");
            var t = n.transform;
            Box(t, new Vector3(0.9f, 1.9f, 0.8f), Mats.Std(new Color(0.75f, 0.12f, 0.12f), 0.4f), new Vector3(0, 0.95f, 0), default, 0.03f);
            Box(t, new Vector3(0.6f, 1.2f, 0.02f), Mats.Emit(new Color(0.7f, 0.85f, 1f), 0.8f), new Vector3(-0.08f, 1.1f, 0.41f), default, 0f, false);
            for (int i = 0; i < 4; i++) Box(t, new Vector3(0.5f, 0.02f, 0.03f), Mats.Std(new Color(0.2f, 0.2f, 0.2f)), new Vector3(-0.08f, 0.65f + i * 0.28f, 0.42f), default, 0f);
            return n;
        }

        public static GameObject CeilingLight(Transform parent, float length)
        {
            var n = Node(parent, "CeilingLight");
            var t = n.transform;
            Box(t, new Vector3(length, 0.08f, 0.2f), Mats.DarkMetal(), Vector3.zero, default, -1f, false);
            Box(t, new Vector3(length - 0.1f, 0.03f, 0.14f), Mats.Lamp(new Color(0.9f, 0.95f, 1f), true), new Vector3(0, -0.05f, 0), default, 0f, false);
            return n;
        }

        public static GameObject SignBoard(Transform parent, string text, Color bg, Color fg, Vector2 size)
        {
            var n = Node(parent, "Sign");
            var t = n.transform;
            Box(t, new Vector3(size.x, size.y, 0.06f), Mats.Std(bg, 0.5f), Vector3.zero, default, 0.01f);
            float fontSize = Mathf.Min(110f, size.y * 0.7f / 0.004f);
            Label3D.Create(t, text, fontSize, fg, new Vector3(0, 0, 0.035f), false);
            var back = Node(t, "Back", new Vector3(0, 0, -0.035f), 180f);
            Label3D.Create(back.transform, text, fontSize, fg, Vector3.zero, false);
            return n;
        }

        /// <summary>Verkaufsstand mit Markise (Vorderseite +Z). Waren-Anzeige kommt über den Stand selbst.</summary>
        public static GameObject MarketStall(Transform parent, Color brand)
        {
            var n = Node(parent, "MarketStall");
            var t = n.transform;
            var wood = Mats.Planks(new Color(0.62f, 0.44f, 0.28f), 0.15f);
            Box(t, new Vector3(2.2f, 0.9f, 0.9f), wood, new Vector3(0, 0.45f, 0), default, 0.02f);
            Box(t, new Vector3(2.3f, 0.06f, 1f), Mats.Planks(new Color(0.72f, 0.54f, 0.34f), 0.2f), new Vector3(0, 0.93f, 0.02f), default, 0.01f);
            foreach (float sx in new[] { -1.05f, 1.05f })
            foreach (float sz in new[] { -0.4f, 0.4f })
                Box(t, new Vector3(0.06f, 1.4f, 0.06f), wood, new Vector3(sx, 1.6f, sz));
            for (int i = 0; i < 6; i++)
            {
                var c = i % 2 == 0 ? brand : Color.white;
                Box(t, new Vector3(0.4f, 0.05f, 1.25f), Mats.Std(c, 0.7f), new Vector3(-1f + i * 0.4f, 2.35f, 0.15f), new Vector3(-14f, 0, 0), 0f);
            }
            Box(t, new Vector3(2.3f, 0.22f, 0.04f), Mats.Std(brand.Darkened(0.2f), 0.6f), new Vector3(0, 2.15f, 0.76f), default, 0f);
            return n;
        }
    }
}
