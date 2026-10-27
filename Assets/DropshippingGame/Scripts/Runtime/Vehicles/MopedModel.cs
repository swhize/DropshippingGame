using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame.Vehicles
{
    /// <summary>Teile des Moped-Modells, die zur Laufzeit bewegt oder umgeschaltet werden.</summary>
    public sealed class MopedParts
    {
        /// <summary>Optik-Wurzel (Schräglage, Nicken, Federn). Ursprung am Boden zwischen den Rädern.</summary>
        public Transform Body;
        /// <summary>Alles Feste am Moped (für die Hervorhebung beim Anvisieren).</summary>
        public Transform Shell;
        /// <summary>Lenkkopf: dreht um die (leicht geneigte) Lenkachse.</summary>
        public Transform Steer;
        public Transform FrontSpin, RearSpin;
        public Transform Needle;
        public Transform GripL, GripR;
        /// <summary>Grundstellung von Lenker und Rädern (importiertes Modell: Knoten haben eigene Drehungen).</summary>
        public Quaternion SteerBase = Quaternion.identity, FrontBase = Quaternion.identity, RearBase = Quaternion.identity;
        /// <summary>Radius für die Raddrehung.</summary>
        public float WheelRadius = MopedModel.WheelR;
        /// <summary>Drehachse der Räder (lokal im Radknoten, Vorzeichen so, dass positiv = vorwärts rollen) und Lenkachse.</summary>
        public Vector3 FrontAxis = Vector3.right, RearAxis = Vector3.right, SteerAxis = Vector3.up;
        public GameObject Kickstand;
        /// <summary>Material-Plätze von Scheinwerfer- und Rücklichtglas (an/aus/Bremse werden umgeschaltet).</summary>
        public readonly List<MatSlot> Lens = new List<MatSlot>(), Tail = new List<MatSlot>();
        public Material LensOn, TailOn, TailBrake;
        public Light HeadLight;
        /// <summary>true = importiertes CC0-Modell "vehicle.moped", false = prozedurales Modell.</summary>
        public bool FromAsset;
        /// <summary>Heckbox (eigene Hervorhebung) und Ablage für die Ladung (Boden der Box).</summary>
        public Transform Box, Cargo;
        /// <summary>Fahrerfigur (nur in der Verfolgerkamera sichtbar).</summary>
        public Transform Rider;
        /// <summary>Arme in der Ego-Sicht (Ärmel + Hand), werden jedes Bild zu den Griffen ausgerichtet.</summary>
        public Transform ArmL, ArmR;
        public Label3D[] BrandLabels = new Label3D[0];
    }

    /// <summary>Ein Material-Platz eines Renderers (Index in sharedMaterials) mit seinem Aus-Material.</summary>
    public sealed class MatSlot
    {
        public Renderer R;
        public int Index;
        public Material Off;
        private Material _cur;

        /// <summary>Setzt das Material (null = Aus-Material), nur bei Änderung.</summary>
        public void Set(Material m)
        {
            if (m == null) m = Off;
            if (R == null || m == null || m == _cur) return;
            _cur = m;
            var mats = R.sharedMaterials;
            if (Index < 0 || Index >= mats.Length) return;
            mats[Index] = m;
            R.sharedMaterials = mats;
        }

        public static void SetAll(List<MatSlot> slots, Material m)
        {
            foreach (var s in slots) s.Set(m);
        }
    }

    /// <summary>
    /// Baut das Moped: bevorzugt das CC0-Modell "vehicle.moped" (Vespa-Stil, Knoten RareWheel / SteeringWheel /
    /// FrontWheel für Räder und Lenkung, Lack „Body…“ in Markenfarbe), sonst prozedural (Roller mit Beinschild,
    /// Sitzbank, Speichenrädern, Tacho). Immer dazu: Gepäckträger-Heckbox in Markenfarbe mit Logo, Licht,
    /// Ständer, Fahrerfigur und Ego-Arme. Front = lokal +Z, Ursprung am Boden mittig zwischen den Rädern.
    /// </summary>
    public static class MopedModel
    {
        public const float WheelR = 0.25f;
        public const float AxleZ = 0.62f;
        /// <summary>Augenhöhe in der Ego-Sicht (über dem Boden, leicht erhöht) und Abstand hinter der Mitte.</summary>
        public static readonly Vector3 EyeLocal = new Vector3(0f, 1.66f, -0.42f);
        /// <summary>Schultern in der Ego-Sicht (für die Arme zu den Griffen).</summary>
        public static readonly Vector3 ShoulderL = new Vector3(-0.21f, 1.33f, -0.3f), ShoulderR = new Vector3(0.21f, 1.33f, -0.3f);
        private const float Rake = -9.3f;
        private static readonly Vector3 SteerHead = new Vector3(0f, 0.99f, 0.5f);

        public static MopedParts Build(Transform root, Color brand, string brandName)
        {
            var p = new MopedParts();
            p.Body = Props.Node(root, "Body").transform;
            p.Shell = Props.Node(p.Body, "Shell").transform;
            var s = p.Shell;

            var paint = Mats.Std(brand, 0.28f, 0.15f);
            var paintDark = Mats.Std(brand.Darkened(0.35f), 0.4f, 0.1f);
            var cream = Mats.Std(new Color(0.93f, 0.91f, 0.86f), 0.35f);
            var rubber = Mats.Std(new Color(0.06f, 0.06f, 0.07f), 0.85f);
            var leather = Mats.Std(new Color(0.09f, 0.08f, 0.08f), 0.55f);
            var chrome = Mats.Std(new Color(0.82f, 0.84f, 0.88f), 0.12f, 0.95f);
            var dark = Mats.DarkMetal();

            p.LensOn = Mats.Emit(new Color(1f, 0.96f, 0.84f), 3.2f);
            p.TailOn = Mats.Emit(new Color(1f, 0.08f, 0.06f), 1.4f);
            p.TailBrake = Mats.Emit(new Color(1f, 0.12f, 0.08f), 4.5f);
            Vector3 boxPos = new Vector3(0f, 0.85f, -0.8f);
            bool asset = BuildAsset(p, s, brand, ref boxPos);
            if (!asset) BuildChassis(p, s, paint, paintDark, cream, rubber, leather, chrome, dark);

            // ---- Gepäckträger + Heckbox ------------------------------------------------------------------
            p.Box = Props.Node(p.Body, "Heckbox", boxPos).transform;
            BuildBox(p, paint, paintDark, brand, brandName);

            // ---- Ständer (links) ---------------------------------------------------------------------------
            var standPivot = Props.Node(s, "Kickstand", new Vector3(-0.12f, 0.25f, -0.12f));
            standPivot.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            Props.Box(standPivot.transform, new Vector3(0.025f, 0.28f, 0.025f), dark, new Vector3(0f, -0.13f, 0f), default, 0f);
            p.Kickstand = standPivot;

            // ---- Lenkung (prozedural) -------------------------------------------------------------------------
            if (!asset)
            {
                var headBase = Props.Node(s, "SteerHead", SteerHead);
                headBase.transform.localRotation = Quaternion.Euler(Rake, 0f, 0f);
                p.Steer = Props.Node(headBase.transform, "Steer").transform;
                BuildSteer(p, paint, cream, rubber, chrome, dark);
            }

            // ---- Fahrer (Verfolgerkamera) und Ego-Arme -----------------------------------------------------
            BuildRider(p, brand);
            p.ArmL = Arm(p.Body, "ArmL");
            p.ArmR = Arm(p.Body, "ArmR");
            p.ArmL.gameObject.SetActive(false);
            p.ArmR.gameObject.SetActive(false);
            return p;
        }

        private static void BuildChassis(MopedParts p, Transform s, Material paint, Material paintDark, Material cream, Material rubber,
            Material leather, Material chrome, Material dark)
        {
            // ---- Hinterrad, Rahmen, Heck --------------------------------------------------------------------
            p.RearSpin = Wheel(s, new Vector3(0f, WheelR, -AxleZ), rubber, chrome, paint);
            // Trittbrett, Heckverkleidung mit Zierleiste
            Props.Box(s, new Vector3(0.34f, 0.07f, 0.56f), rubber, new Vector3(0f, 0.31f, 0.1f), default, 0.02f);
            Props.Box(s, new Vector3(0.36f, 0.05f, 0.58f), paintDark, new Vector3(0f, 0.26f, 0.1f), default, 0.015f);
            Props.Box(s, new Vector3(0.42f, 0.34f, 0.78f), paint, new Vector3(0f, 0.53f, -0.42f), default, 0.08f);
            Props.Box(s, new Vector3(0.36f, 0.2f, 0.34f), paint, new Vector3(0f, 0.58f, -0.86f), new Vector3(-12f, 0f, 0f), 0.07f);
            Props.Box(s, new Vector3(0.43f, 0.035f, 0.8f), cream, new Vector3(0f, 0.43f, -0.42f), default, 0.01f);
            // Motor + Auspuff (rechts)
            Props.Box(s, new Vector3(0.2f, 0.2f, 0.42f), dark, new Vector3(0.09f, 0.3f, -0.5f), default, 0.03f);
            Props.Cyl(s, 0.045f, 0.045f, 0.5f, dark, new Vector3(0.2f, 0.24f, -0.66f), new Vector3(90f, 0f, 0f), 12);
            Props.Cyl(s, 0.05f, 0.05f, 0.08f, chrome, new Vector3(0.2f, 0.24f, -0.94f), new Vector3(90f, 0f, 0f), 12);
            // Beinschild + Front
            Props.Box(s, new Vector3(0.46f, 0.66f, 0.05f), paint, new Vector3(0f, 0.64f, 0.4f), new Vector3(-6f, 0f, 0f), 0.04f);
            Props.Box(s, new Vector3(0.3f, 0.5f, 0.16f), paint, new Vector3(0f, 0.6f, 0.5f), new Vector3(-8f, 0f, 0f), 0.07f);
            Props.Box(s, new Vector3(0.04f, 0.36f, 0.02f), cream, new Vector3(0f, 0.62f, 0.585f), new Vector3(-8f, 0f, 0f), 0.01f);
            // Sitzbank
            Props.Box(s, new Vector3(0.34f, 0.11f, 0.68f), leather, new Vector3(0f, 0.755f, -0.33f), default, 0.05f);
            Props.Box(s, new Vector3(0.35f, 0.02f, 0.69f), cream, new Vector3(0f, 0.7f, -0.33f), default, 0.005f);

            // Gepäckträger
            Props.Box(s, new Vector3(0.4f, 0.03f, 0.4f), chrome, new Vector3(0f, 0.835f, -0.8f), default, 0.008f);
            foreach (float sx in new[] { -0.17f, 0.17f })
                Props.Box(s, new Vector3(0.025f, 0.2f, 0.025f), chrome, new Vector3(sx, 0.74f, -0.72f), new Vector3(20f, 0f, 0f), 0f);
            // ---- Rücklicht, Kennzeichen -------------------------------------------------------------
            var tailOff = Mats.Std(new Color(0.45f, 0.05f, 0.05f), 0.3f);
            var tail = Props.Box(s, new Vector3(0.16f, 0.06f, 0.035f), tailOff, new Vector3(0f, 0.64f, -1.06f), new Vector3(-12f, 0f, 0f), 0.01f, false);
            p.Tail.Add(new MatSlot { R = tail.GetComponent<Renderer>(), Index = 0, Off = tailOff });
            Props.Box(s, new Vector3(0.14f, 0.1f, 0.01f), Mats.Std(new Color(0.95f, 0.95f, 0.92f), 0.4f), new Vector3(0f, 0.41f, -1.0f), default, 0.003f, false);
            Props.Box(s, new Vector3(0.03f, 0.03f, 0.16f), Mats.DarkMetal(), new Vector3(0f, 0.46f, -0.93f), default, 0f, false);
            // Label3D ohne Billboard ist von +Z lesbar; Drehung 0 = von hinten (−Z) lesbar.
            var plate = Label3D.Create(s, "35 DSH", 8f, new Color(0.08f, 0.08f, 0.1f), new Vector3(0f, 0.41f, -1.008f), false, 25f);
            if (plate != null) plate.transform.localRotation = Quaternion.identity;
        }

        private static Transform Wheel(Transform parent, Vector3 pos, Material rubber, Material chrome, Material accent)
        {
            var node = Props.Node(parent, "Wheel", pos).transform;
            var spin = Props.Node(node, "Spin").transform;
            Props.Cyl(spin, WheelR, WheelR, 0.1f, rubber, Vector3.zero, new Vector3(0f, 0f, 90f), 20);
            Props.Cyl(spin, WheelR * 0.66f, WheelR * 0.66f, 0.106f, chrome, Vector3.zero, new Vector3(0f, 0f, 90f), 16);
            Props.Cyl(spin, 0.05f, 0.05f, 0.13f, Mats.DarkMetal(), Vector3.zero, new Vector3(0f, 0f, 90f), 10);
            // Speichen (fünf Streben), damit man das Drehen sieht
            for (int i = 0; i < 5; i++)
                Props.Box(spin, new Vector3(0.112f, 0.028f, WheelR * 1.25f), accent, Vector3.zero, new Vector3(i * 36f, 0f, 0f), 0f, false);
            return spin;
        }

        private static void BuildBox(MopedParts p, Material paint, Material paintDark, Color brand, string brandName)
        {
            var b = p.Box;
            const float w = 0.5f, d = 0.48f, h = 0.32f, th = 0.025f;
            Props.Box(b, new Vector3(w, th, d), paintDark, new Vector3(0f, th / 2f, 0f), default, 0.008f);
            Props.Box(b, new Vector3(w, h, th), paint, new Vector3(0f, h / 2f, d / 2f - th / 2f), default, 0.008f);
            Props.Box(b, new Vector3(w, h, th), paint, new Vector3(0f, h / 2f, -d / 2f + th / 2f), default, 0.008f);
            Props.Box(b, new Vector3(th, h, d), paint, new Vector3(w / 2f - th / 2f, h / 2f, 0f), default, 0.008f);
            Props.Box(b, new Vector3(th, h, d), paint, new Vector3(-w / 2f + th / 2f, h / 2f, 0f), default, 0.008f);
            // Kantenschutz oben
            var rim = Mats.Std(new Color(0.12f, 0.12f, 0.13f), 0.5f);
            Props.Box(b, new Vector3(w + 0.012f, 0.025f, 0.035f), rim, new Vector3(0f, h, d / 2f - th / 2f), default, 0.006f, false);
            Props.Box(b, new Vector3(w + 0.012f, 0.025f, 0.035f), rim, new Vector3(0f, h, -d / 2f + th / 2f), default, 0.006f, false);
            Props.Box(b, new Vector3(0.035f, 0.025f, d), rim, new Vector3(w / 2f - th / 2f, h, 0f), default, 0.006f, false);
            Props.Box(b, new Vector3(0.035f, 0.025f, d), rim, new Vector3(-w / 2f + th / 2f, h, 0f), default, 0.006f, false);
            // Markenschild auf beiden Seiten und hinten
            var labels = new System.Collections.Generic.List<Label3D>();
            Color text = brand.grayscale > 0.6f ? new Color(0.1f, 0.1f, 0.12f) : Color.white;
            string name = string.IsNullOrEmpty(brandName) ? "MeinShop" : brandName;
            if (name.Length > 14) name = name.Substring(0, 14);
            // Schriftgröße passend zur Seitenbreite (Zeichenbreite ≈ size · 0,0024 m)
            float size = Mathf.Clamp(0.4f / (Mathf.Max(1, name.Length) * 0.0024f), 8f, 30f);
            // Drehung: 0 = von −Z lesbar (hinten), −90 = von +X (rechts), 90 = von −X (links)
            foreach (var (pos, rot) in new[] { (new Vector3(0f, h * 0.5f, -d / 2f - 0.004f), 0f), (new Vector3(w / 2f + 0.004f, h * 0.5f, 0f), -90f), (new Vector3(-w / 2f - 0.004f, h * 0.5f, 0f), 90f) })
            {
                var l = Label3D.Create(b, name, size, text, pos, false, 30f, false, 1f, 0, SignFont.Display)?.MakeUpper();
                if (l == null) continue;
                l.transform.localRotation = Quaternion.Euler(0f, rot, 0f);
                labels.Add(l);
            }
            p.BrandLabels = labels.ToArray();
            p.Cargo = Props.Node(b, "Cargo", new Vector3(0f, th, 0f)).transform;
            // Kollider der Heckbox (eigener Anvisier-Bereich)
            var col = b.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(w + 0.02f, h + 0.06f, d + 0.02f);
            col.center = new Vector3(0f, h / 2f + 0.02f, 0f);
        }

        private static void BuildSteer(MopedParts p, Material paint, Material cream, Material rubber, Material chrome, Material dark)
        {
            var st = p.Steer;
            float fork = SteerHead.y - WheelR; // Länge Lenkkopf → Vorderachse (ca.)
            fork /= Mathf.Cos(Rake * Mathf.Deg2Rad);
            foreach (float fx in new[] { -0.075f, 0.075f })
                Props.Box(st, new Vector3(0.035f, fork, 0.05f), chrome, new Vector3(fx, -fork / 2f, 0f), default, 0.01f);
            p.FrontSpin = Wheel(st, new Vector3(0f, -fork, 0f), rubber, chrome, paint);
            Props.Box(st, new Vector3(0.14f, 0.04f, 0.4f), paint, new Vector3(0f, -fork + WheelR + 0.07f, 0.02f), new Vector3(-6f, 0f, 0f), 0.015f);
            // Lenker mit Griffen, Hebeln und Spiegeln
            var barY = 0.12f;
            Props.Box(st, new Vector3(0.6f, 0.035f, 0.035f), dark, new Vector3(0f, barY, -0.06f), default, 0.01f);
            foreach (float side in new[] { -1f, 1f })
            {
                var grip = Props.Box(st, new Vector3(0.11f, 0.045f, 0.045f), rubber, new Vector3(side * 0.31f, barY, -0.06f), default, 0.015f);
                if (side < 0f) p.GripL = grip.transform;
                else p.GripR = grip.transform;
                Props.Box(st, new Vector3(0.12f, 0.012f, 0.02f), chrome, new Vector3(side * 0.27f, barY + 0.01f, 0.0f), new Vector3(0f, side * -12f, 0f), 0.003f, false);
                Props.Box(st, new Vector3(0.012f, 0.2f, 0.012f), chrome, new Vector3(side * 0.24f, barY + 0.1f, -0.04f), new Vector3(0f, 0f, side * -14f), 0f, false);
                var mirror = Props.Cyl(st, 0.045f, 0.045f, 0.012f, dark, new Vector3(side * 0.27f, barY + 0.21f, -0.04f), new Vector3(90f, 0f, 0f), 14);
                Props.Cyl(mirror.transform, 0.038f, 0.038f, 0.014f, chrome, Vector3.zero, default, 14);
            }
            // Lenkerverkleidung mit Scheinwerfer
            Props.Box(st, new Vector3(0.28f, 0.15f, 0.2f), paint, new Vector3(0f, barY - 0.02f, 0.04f), new Vector3(-6f, 0f, 0f), 0.06f);
            var lensOff = Mats.Std(new Color(0.86f, 0.87f, 0.84f), 0.1f, 0.2f);
            var lens = Props.Cyl(st, 0.07f, 0.07f, 0.03f, lensOff, new Vector3(0f, barY - 0.02f, 0.145f), new Vector3(90f, 0f, 0f), 18);
            p.Lens.Add(new MatSlot { R = lens.GetComponent<Renderer>(), Index = 0, Off = lensOff });
            Props.Cyl(st, 0.08f, 0.08f, 0.026f, chrome, new Vector3(0f, barY - 0.02f, 0.138f), new Vector3(90f, 0f, 0f), 18);
            p.HeadLight = HeadLight(st);
            p.HeadLight.transform.localPosition = new Vector3(0f, barY - 0.02f, 0.2f);
            p.HeadLight.transform.localRotation = Quaternion.Euler(9f - Rake, 0f, 0f);
            // Tacho (zum Fahrer geneigt) mit Nadel
            var dial = Props.Node(st, "Tacho", new Vector3(0f, barY + 0.07f, -0.0f));
            dial.transform.localRotation = Quaternion.Euler(-38f, 0f, 0f);
            Props.Cyl(dial.transform, 0.07f, 0.07f, 0.02f, dark, Vector3.zero, default, 20);
            Props.Cyl(dial.transform, 0.058f, 0.058f, 0.022f, Mats.Emit(new Color(0.92f, 0.93f, 0.9f), 0.45f), Vector3.zero, default, 20);
            for (int i = 0; i <= 7; i++)
            {
                float ang = -120f + i * (240f / 7f);
                var tick = Props.Node(dial.transform, "Tick", new Vector3(0f, 0.012f, 0f));
                tick.transform.localRotation = Quaternion.Euler(0f, ang, 0f);
                Props.Box(tick.transform, new Vector3(0.004f, 0.002f, 0.012f), Mats.Std(new Color(0.1f, 0.1f, 0.12f), 0.5f), new Vector3(0f, 0f, 0.045f), default, 0f, false);
            }
            var needle = Props.Node(dial.transform, "Needle", new Vector3(0f, 0.013f, 0f));
            Props.Box(needle.transform, new Vector3(0.005f, 0.003f, 0.05f), Mats.Emit(new Color(1f, 0.2f, 0.1f), 1.6f), new Vector3(0f, 0f, 0.022f), default, 0f, false);
            Props.Cyl(dial.transform, 0.008f, 0.008f, 0.03f, dark, Vector3.zero, default, 8);
            needle.transform.localRotation = Quaternion.Euler(0f, -120f, 0f);
            p.Needle = needle.transform;
            foreach (var r in st.GetComponentsInChildren<Renderer>(true))
                if (r.transform.IsChildOf(dial.transform)) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // =====================================================================================
        // Importiertes Modell (CC0, "vehicle.moped")
        // =====================================================================================
        private static Light HeadLight(Transform parent)
        {
            var go = new GameObject("HeadLight");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = new Color(1f, 0.95f, 0.82f);
            l.intensity = 6f;
            l.range = 22f;
            l.spotAngle = 62f;
            l.innerSpotAngle = 30f;
            l.shadows = LightShadows.None;
            l.enabled = false;
            return l;
        }

        /// <summary>
        /// Lädt das Modell, färbt den Lack („Body…“) in der Markenfarbe, verdrahtet Räder/Lenker und misst Griffe,
        /// Scheinwerfer und Gepäckträger aus. false = kein Modell (oder ohne bewegliche Knoten) → prozedural.
        /// </summary>
        private static bool BuildAsset(MopedParts p, Transform s, Color brand, ref Vector3 boxPos)
        {
            GameObject model = null;
            try
            {
                model = AssetLib.Model("vehicle.moped", s, Vector3.zero, 0f);
                if (model == null) return false;
                var rear = FindDeep(model.transform, "RareWheel") ?? FindDeep(model.transform, "RearWheel");
                var steer = FindDeep(model.transform, "SteeringWheel");
                var front = FindDeep(model.transform, "FrontWheel");
                if (rear == null || steer == null || front == null)
                {
                    model.SetActive(false);
                    Object.Destroy(model);
                    return false;
                }
                p.FromAsset = true;
                model.name = "MopedModel";
                // Mitte zwischen den Achsen auf z = 0 (wie das prozedurale Modell und die Fahrphysik)
                float mid = (p.Body.InverseTransformPoint(rear.position).z + p.Body.InverseTransformPoint(front.position).z) * 0.5f;
                if (Mathf.Abs(mid) < 0.5f) model.transform.localPosition -= new Vector3(0f, 0f, mid);
                p.Steer = steer;
                p.SteerBase = steer.localRotation;
                p.FrontSpin = front;
                p.FrontBase = front.localRotation;
                p.RearSpin = rear;
                p.RearBase = rear.localRotation;

                // Lack in Markenfarbe, Lichtgläser merken
                var paint = Mats.Std(brand, 0.3f, 0.1f);
                var handles = new List<Renderer>();
                var lensR = new List<Renderer>();
                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = mats[i];
                        if (m == null) continue;
                        string n = m.name ?? "";
                        if (n.StartsWith("Body"))
                        {
                            mats[i] = paint;
                            changed = true;
                        }
                        else if (n.StartsWith("Headlight"))
                        {
                            p.Lens.Add(new MatSlot { R = r, Index = i, Off = m });
                            lensR.Add(r);
                        }
                        else if (n.StartsWith("BrakeLight")) p.Tail.Add(new MatSlot { R = r, Index = i, Off = m });
                        else if (n.StartsWith("Handle") && r.transform.IsChildOf(steer) && !r.transform.IsChildOf(front)) handles.Add(r);
                    }
                    if (changed) r.sharedMaterials = mats;
                }

                var body = p.Body;
                // Radradius aus dem Hinterrad; Achsen: dünnste Ausdehnung des Rads = Radachse
                var rb = LocalBounds(body, rear.GetComponentsInChildren<Renderer>(true));
                if (rb.HasValue) p.WheelRadius = Mathf.Clamp(rb.Value.extents.y, 0.15f, 0.4f);
                p.RearAxis = SpinAxis(rear, body);
                p.FrontAxis = SpinAxis(front, body);
                var up = body.InverseTransformDirection(steer.TransformDirection(Vector3.up));
                p.SteerAxis = up.y >= 0f ? Vector3.up : Vector3.down;
                // Griffe: Lenkergriffe („Handle“), sonst äußere Enden des Lenkers
                Bounds? hb = LocalBounds(body, handles);
                if (!hb.HasValue)
                {
                    var steerR = new List<Renderer>();
                    foreach (var r in steer.GetComponentsInChildren<Renderer>(true))
                        if (!r.transform.IsChildOf(front)) steerR.Add(r);
                    var sb = LocalBounds(body, steerR);
                    if (sb.HasValue) hb = new Bounds(new Vector3(sb.Value.center.x, sb.Value.max.y - 0.12f, sb.Value.center.z), new Vector3(sb.Value.size.x - 0.1f, 0.04f, 0.04f));
                }
                if (hb.HasValue)
                {
                    var h = hb.Value;
                    p.GripL = Anchor(steer, body.TransformPoint(new Vector3(h.min.x + 0.05f, h.center.y, h.center.z)), "GripL");
                    p.GripR = Anchor(steer, body.TransformPoint(new Vector3(h.max.x - 0.05f, h.center.y, h.center.z)), "GripR");
                }
                // Scheinwerfer-Licht am Lenker (dreht mit)
                var lb = LocalBounds(body, lensR);
                Vector3 lightLocal = lb.HasValue ? lb.Value.center + new Vector3(0f, 0f, lb.Value.extents.z + 0.04f) : new Vector3(0f, 1.0f, 0.72f);
                p.HeadLight = HeadLight(steer);
                p.HeadLight.transform.position = body.TransformPoint(lightLocal);
                p.HeadLight.transform.rotation = body.rotation * Quaternion.Euler(9f, 0f, 0f);
                // Heckbox auf den Gepäckträger: höchster Punkt hinter dem Hinterrad
                var rearAxle = body.InverseTransformPoint(rear.position);
                var backR = new List<Renderer>();
                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.transform.IsChildOf(steer) || r.transform.IsChildOf(rear)) continue;
                    var b = LocalBounds(body, new[] { r });
                    if (b.HasValue && b.Value.center.z < rearAxle.z + 0.15f && b.Value.size.z < 1.2f) backR.Add(r);
                }
                var back = LocalBounds(body, backR);
                float top = back.HasValue ? back.Value.max.y : 0.86f;
                if (top < 0.7f || top > 1.1f) top = 0.86f;
                boxPos = new Vector3(0f, top + 0.005f, Mathf.Clamp(rearAxle.z - 0.06f, -0.95f, -0.45f));
                return true;
            }
            catch (System.Exception e)
            {
                SafeLog.Warn("moped.asset", e);
                if (model != null)
                {
                    model.SetActive(false);
                    Object.Destroy(model);
                }
                p.FromAsset = false;
                p.Lens.Clear();
                p.Tail.Clear();
                p.Steer = p.FrontSpin = p.RearSpin = p.GripL = p.GripR = null;
                p.SteerBase = p.FrontBase = p.RearBase = Quaternion.identity;
                p.WheelRadius = WheelR;
                if (p.HeadLight != null) Object.Destroy(p.HeadLight.gameObject);
                p.HeadLight = null;
                return false;
            }
        }

        /// <summary>Radachse im Radknoten: kürzeste Seite des Rad-Hüllquaders, so ausgerichtet, dass positiv vorwärts rollt (Body +X).</summary>
        private static Vector3 SpinAxis(Transform wheel, Transform body)
        {
            var b = LocalBounds(wheel, wheel.GetComponentsInChildren<Renderer>(true));
            Vector3 axis = Vector3.right;
            if (b.HasValue)
            {
                var e = b.Value.extents;
                axis = e.x <= e.y && e.x <= e.z ? Vector3.right : (e.y <= e.z ? Vector3.up : Vector3.forward);
            }
            var inBody = body.InverseTransformDirection(wheel.TransformDirection(axis));
            return inBody.x >= 0f ? axis : -axis;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(name)) return t;
            return null;
        }

        /// <summary>Leerer Ankerpunkt unter parent an einer Weltposition (Skalierung des Modells egal).</summary>
        private static Transform Anchor(Transform parent, Vector3 world, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = world;
            return go.transform;
        }

        /// <summary>Hüllquader der Meshes im Raum von space (aus den Mesh-Bounds, auch bei nicht lesbaren Meshes).</summary>
        private static Bounds? LocalBounds(Transform space, IEnumerable<Renderer> renderers)
        {
            bool any = false;
            var b = new Bounds();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var m = space.worldToLocalMatrix * r.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    var w = m.MultiplyPoint3x4(c);
                    if (!any)
                    {
                        b = new Bounds(w, Vector3.zero);
                        any = true;
                    }
                    else b.Encapsulate(w);
                }
            }
            return any ? b : (Bounds?)null;
        }

        private static void BuildRider(MopedParts p, Color brand)
        {
            var node = Props.Node(p.Body, "Rider", new Vector3(0f, 0.37f, -0.32f)).transform;
            p.Rider = node;
            try
            {
                var look = new Look
                {
                    Skin = CharacterKit.SkinTones[1], Hair = CharacterKit.HairColors[1], Shirt = brand,
                    Pants = CharacterKit.PantsColors[0], Cap = true, CapColor = brand.Darkened(0.4f), Sitting = true, Seed = 7,
                };
                var rig = CharacterKit.Build(node, look);
                if (rig != null)
                {
                    if (rig.Torso != null) rig.Torso.localRotation = Quaternion.Euler(14f, 0f, 0f);
                    if (rig.ArmL != null) rig.ArmL.localRotation = Quaternion.Euler(-62f, 0f, 0f);
                    if (rig.ArmR != null) rig.ArmR.localRotation = Quaternion.Euler(-62f, 0f, 0f);
                    if (rig.HipL != null) rig.HipL.localRotation = Quaternion.Euler(-80f, -8f, 0f);
                    if (rig.HipR != null) rig.HipR.localRotation = Quaternion.Euler(-80f, 8f, 0f);
                }
            }
            catch (System.Exception e)
            {
                SafeLog.Warn("moped.rider", e);
            }
            node.gameObject.SetActive(false);
        }

        /// <summary>Ego-Arm: Ärmel (Länge 1, wird skaliert) + Hand am Ende (+Z).</summary>
        private static Transform Arm(Transform parent, string name)
        {
            var arm = Props.Node(parent, name).transform;
            var sleeve = Props.Node(arm, "Sleeve").transform;
            Props.Box(sleeve, new Vector3(0.1f, 0.1f, 1f), Mats.Std(new Color(0.2f, 0.22f, 0.28f), 0.8f), new Vector3(0f, 0f, 0.5f), default, 0.01f, false);
            var hand = Props.Node(arm, "Hand").transform;
            Props.Box(hand, new Vector3(0.095f, 0.075f, 0.11f), Mats.Std(new Color(0.93f, 0.76f, 0.62f), 0.7f), Vector3.zero, default, 0.02f, false);
            foreach (var r in arm.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            return arm;
        }

        /// <summary>Richtet einen Ego-Arm von der Schulter (Body-lokal) zum Griff (Welt) aus.</summary>
        public static void AimArm(Transform arm, Transform body, Vector3 shoulderLocal, Vector3 gripWorld)
        {
            if (arm == null || body == null) return;
            Vector3 from = body.TransformPoint(shoulderLocal);
            Vector3 dir = gripWorld - from;
            float len = dir.magnitude;
            if (len < 0.05f) return;
            arm.position = from;
            arm.rotation = Quaternion.LookRotation(dir / len, body.up);
            var sleeve = arm.Find("Sleeve");
            if (sleeve != null) sleeve.localScale = new Vector3(1f, 1f, Mathf.Max(0.05f, len - 0.05f));
            var hand = arm.Find("Hand");
            if (hand != null) hand.localPosition = new Vector3(0f, 0f, len - 0.02f);
        }

        /// <summary>Ladung in der Box neu aufbauen: bis zu 4 Pakete im 2×2-Raster, eine Kiste mittig.</summary>
        public static void BuildCargo(Transform cargo, System.Collections.Generic.IList<ItemData> items)
        {
            if (cargo == null) return;
            for (int i = cargo.childCount - 1; i >= 0; i--) Object.Destroy(cargo.GetChild(i).gameObject);
            if (items == null) return;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null) continue;
                GameObject go;
                try
                {
                    go = ItemKit.Build(cargo, it, false);
                }
                catch (System.Exception e)
                {
                    SafeLog.Warn("moped.cargo", e);
                    continue;
                }
                var size = ItemKit.Bounds(it);
                float scale;
                Vector3 pos;
                if (it.Kind == ItemKind.Crate)
                {
                    scale = Mathf.Min(1f, 0.47f / size.x, 0.45f / size.z, 0.42f / size.y);
                    pos = new Vector3(0f, size.y * scale / 2f, 0f);
                }
                else
                {
                    int cell = i % 4;
                    scale = Mathf.Min(1f, 0.21f / size.x, 0.21f / size.z, 0.27f / size.y);
                    pos = new Vector3(cell % 2 == 0 ? -0.115f : 0.115f, size.y * scale / 2f, cell < 2 ? 0.11f : -0.11f);
                }
                go.transform.localScale = Vector3.one * scale;
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(0f, (i % 2 == 0 ? 4f : -6f), 0f);
                foreach (var l in go.GetComponentsInChildren<Label3D>(true)) Object.Destroy(l.gameObject);
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            }
        }
    }
}
