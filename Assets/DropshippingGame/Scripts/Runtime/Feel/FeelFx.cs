using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// Prozedurale Partikel-Schübe (Staub, Funkeln, Konfetti, Münzen, Sterne) – komplett im Code erzeugt.
    /// Pro Art gibt es genau EIN ParticleSystem in Weltkoordinaten, das per <c>Emit</c> an beliebigen Stellen
    /// gleichzeitig ausstößt (Pooling ohne GameObjects pro Effekt). Obergrenzen: maxParticles je Art,
    /// Partikel-Budget pro Frame, weit entfernte Effekte werden ausgelassen.
    /// Shader "DS/Particle" (Resources/Shaders) mit Fallbacks; Texturen werden einmalig generiert.
    /// </summary>
    public static class FeelFx
    {
        private enum Tex
        {
            Soft,
            Star4,
            Square,
            Coin,
            Star5,
        }

        private sealed class Cfg
        {
            public string Name;
            public Tex Tex;
            public int Max;
            public float Gravity, Drag, LifeMin, LifeMax, SpeedMin, SpeedMax, SizeMin, SizeMax, UpBias, Spin, FadeStart = 0.6f, Intensity = 1f;
            public bool Mesh3D, RandomRot = true;
            /// <summary>Größe über Lebenszeit: Schlüssel (Zeit, Faktor).</summary>
            public float[] SizeKeys;
        }

        private const int KDust = 0, KSparkle = 1, KConfetti = 2, KCoins = 3, KStars = 4;

        private static readonly Cfg[] Cfgs =
        {
            new Cfg { Name = "Dust", Tex = Tex.Soft, Max = 160, Gravity = -0.04f, Drag = 3.2f, LifeMin = 0.45f, LifeMax = 0.85f, SpeedMin = 0.6f, SpeedMax = 1.6f,
                SizeMin = 0.1f, SizeMax = 0.22f, UpBias = 0.35f, Spin = 60f, FadeStart = 0.25f, SizeKeys = new[] { 0f, 0.55f, 1f, 1.5f } },
            new Cfg { Name = "Sparkle", Tex = Tex.Star4, Max = 220, Gravity = 0f, Drag = 2.4f, LifeMin = 0.35f, LifeMax = 0.75f, SpeedMin = 0.8f, SpeedMax = 2.2f,
                SizeMin = 0.05f, SizeMax = 0.13f, UpBias = 0.3f, Spin = 140f, FadeStart = 0.55f, Intensity = 2.2f, SizeKeys = new[] { 0f, 0.3f, 0.15f, 1f, 1f, 0f } },
            new Cfg { Name = "Confetti", Tex = Tex.Square, Max = 420, Gravity = 0.5f, Drag = 1.7f, LifeMin = 1.5f, LifeMax = 2.4f, SpeedMin = 2.2f, SpeedMax = 4.6f,
                SizeMin = 0.035f, SizeMax = 0.065f, UpBias = 1.1f, Spin = 720f, FadeStart = 0.8f, Mesh3D = true, SizeKeys = new[] { 0f, 1f, 1f, 1f } },
            new Cfg { Name = "Coins", Tex = Tex.Coin, Max = 160, Gravity = 1.7f, Drag = 0.6f, LifeMin = 0.7f, LifeMax = 1.0f, SpeedMin = 1.6f, SpeedMax = 3.0f,
                SizeMin = 0.06f, SizeMax = 0.095f, UpBias = 1.6f, Spin = 0f, FadeStart = 0.7f, Intensity = 1.25f, RandomRot = false, SizeKeys = new[] { 0f, 0.4f, 0.12f, 1f, 1f, 0.8f } },
            new Cfg { Name = "Stars", Tex = Tex.Star5, Max = 90, Gravity = -0.25f, Drag = 2.6f, LifeMin = 1.3f, LifeMax = 1.6f, SpeedMin = 0.8f, SpeedMax = 1.3f,
                SizeMin = 0.13f, SizeMax = 0.15f, UpBias = 0f, Spin = 0f, FadeStart = 0.7f, Intensity = 1.6f, RandomRot = false, SizeKeys = new[] { 0f, 0f, 0.12f, 1.25f, 0.25f, 1f, 1f, 0.9f } },
        };

        public static readonly Color[] ConfettiPalette =
        {
            new Color(1f, 0.32f, 0.38f), new Color(1f, 0.78f, 0.15f), new Color(0.25f, 0.8f, 0.5f),
            new Color(0.3f, 0.6f, 1f), new Color(0.75f, 0.45f, 1f), new Color(1f, 0.55f, 0.2f), Color.white,
        };

        public static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        public static readonly Color DustColor = new Color(0.78f, 0.72f, 0.62f, 0.55f);

        private const int FrameBudget = 260;
        private const float MaxDistance = 45f;

        private static ParticleSystem[] _sys;
        private static bool _init, _failed;
        private static int _budgetFrame = -1, _budgetLeft;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _sys = null;
            _init = false;
            _failed = false;
            _budgetFrame = -1;
        }

        // ---- Öffentliche Effekte ---------------------------------------------------------------------
        /// <summary>Staubwolke (Abstellen, Kiste öffnen, Karton schließen).</summary>
        public static void Dust(Vector3 pos, int count = 8, float scale = 1f) => Emit(KDust, pos, count, DustColor, false, scale, 0.08f);

        /// <summary>Funkeln (fertig!, Etikett, Freischaltung). tint null = Gold/Weiß.</summary>
        public static void Sparkle(Vector3 pos, int count = 10, Color? tint = null, float scale = 1f) =>
            Emit(KSparkle, pos, count, tint ?? Gold, tint == null, scale, 0.05f);

        /// <summary>Konfetti-Schuss nach oben.</summary>
        public static void Confetti(Vector3 pos, int count = 40, float scale = 1f) => Emit(KConfetti, pos, count, Color.white, true, scale, 0.1f);

        /// <summary>Münzen springen heraus und fallen.</summary>
        public static void Coins(Vector3 pos, int count = 8, float scale = 1f) => Emit(KCoins, pos, count, Color.white, false, scale, 0.05f);

        /// <summary>
        /// Bewertungs-Sterne: steigen fächerförmig auf. stars = Anzahl (1–5), gute Bewertung gold, schlechte grau.
        /// </summary>
        public static void Stars(Vector3 pos, int stars, bool good)
        {
            if (!Ready(pos)) return;
            stars = Mathf.Clamp(stars, 1, 5);
            var c = Cfgs[KStars];
            var ps = _sys[KStars];
            var col = good ? Gold : new Color(0.62f, 0.64f, 0.7f);
            var cam = Camera.main;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < stars && Spend(1); i++)
            {
                float k = stars == 1 ? 0f : (i / (float)(stars - 1)) * 2f - 1f;
                ep.position = pos + right * (k * 0.22f);
                ep.velocity = Vector3.up * (c.SpeedMax - Mathf.Abs(k) * 0.35f) + right * (k * 0.35f);
                ep.startLifetime = c.LifeMax - Mathf.Abs(k) * 0.1f;
                ep.startSize = c.SizeMax;
                ep.startColor = col;
                ep.rotation = 0f;
                ps.Emit(ep, 1);
            }
        }

        // ---- Intern ----------------------------------------------------------------------------------
        private static bool Spend(int n)
        {
            if (Time.frameCount != _budgetFrame)
            {
                _budgetFrame = Time.frameCount;
                _budgetLeft = FrameBudget;
            }
            if (_budgetLeft < n) return false;
            _budgetLeft -= n;
            return true;
        }

        private static bool Ready(Vector3 pos)
        {
            if (!Feel.Enabled || !Application.isPlaying) return false;
            if (!_init) Init();
            if (_failed || _sys == null) return false;
            return Feel.CameraDistanceSqr(pos) <= MaxDistance * MaxDistance;
        }

        private static void Emit(int k, Vector3 pos, int count, Color tint, bool palette, float scale, float radius)
        {
            try
            {
                if (count <= 0 || !Ready(pos)) return;
                var ps = _sys[k];
                if (ps == null) return;
                var c = Cfgs[k];
                var ep = new ParticleSystem.EmitParams();
                for (int i = 0; i < count && Spend(1); i++)
                {
                    var dir = UnityEngine.Random.onUnitSphere + Vector3.up * c.UpBias;
                    if (dir.sqrMagnitude < 1e-4f) dir = Vector3.up;
                    dir.Normalize();
                    ep.position = pos + UnityEngine.Random.insideUnitSphere * (radius * scale);
                    ep.velocity = dir * (UnityEngine.Random.Range(c.SpeedMin, c.SpeedMax) * Mathf.Sqrt(scale));
                    ep.startLifetime = UnityEngine.Random.Range(c.LifeMin, c.LifeMax);
                    ep.startSize = UnityEngine.Random.Range(c.SizeMin, c.SizeMax) * scale;
                    ep.startColor = palette ? PaletteFor(k) : tint;
                    if (c.Mesh3D) ep.rotation3D = new Vector3(UnityEngine.Random.value * 360f, UnityEngine.Random.value * 360f, UnityEngine.Random.value * 360f);
                    else ep.rotation = c.RandomRot ? UnityEngine.Random.value * 360f : 0f;
                    ps.Emit(ep, 1);
                }
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
        }

        private static Color PaletteFor(int k)
        {
            if (k == KSparkle) return UnityEngine.Random.value < 0.6f ? Gold : Color.white;
            return ConfettiPalette[UnityEngine.Random.Range(0, ConfettiPalette.Length)];
        }

        private static void Init()
        {
            _init = true;
            try
            {
                var root = FeelRunner.Root;
                if (root == null)
                {
                    _failed = true;
                    return;
                }
                _sys = new ParticleSystem[Cfgs.Length];
                for (int i = 0; i < Cfgs.Length; i++) _sys[i] = Build(root, Cfgs[i]);
            }
            catch (Exception e)
            {
                _failed = true;
                Feel.LogOnce(e);
            }
        }

        private static ParticleSystem Build(Transform root, Cfg c)
        {
            var go = new GameObject("FX_" + c.Name);
            go.transform.SetParent(root, false);
            go.layer = 2; // Ignore Raycast
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = c.Max;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 0.1f;
            main.startRotation3D = c.Mesh3D;
            main.gravityModifier = c.Gravity;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            var em = ps.emission;
            em.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, Mathf.Clamp(c.FadeStart, 0.01f, 0.99f)), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            if (c.SizeKeys != null && c.SizeKeys.Length >= 4)
            {
                var curve = new AnimationCurve();
                for (int i = 0; i + 1 < c.SizeKeys.Length; i += 2) curve.AddKey(c.SizeKeys[i], c.SizeKeys[i + 1]);
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
            }
            if (c.Drag > 0f)
            {
                var lv = ps.limitVelocityOverLifetime;
                lv.enabled = true;
                lv.limit = 100f;
                lv.dampen = 0f;
                lv.drag = c.Drag;
            }
            if (c.Spin > 0f)
            {
                float rad = c.Spin * Mathf.Deg2Rad;
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                if (c.Mesh3D)
                {
                    rot.separateAxes = true;
                    rot.x = new ParticleSystem.MinMaxCurve(-rad, rad);
                    rot.y = new ParticleSystem.MinMaxCurve(-rad * 0.5f, rad * 0.5f);
                    rot.z = new ParticleSystem.MinMaxCurve(-rad, rad);
                }
                else rot.z = new ParticleSystem.MinMaxCurve(-rad, rad);
            }

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MakeMaterial(c);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.maxParticleSize = 1.5f;
            if (c.Mesh3D)
            {
                r.renderMode = ParticleSystemRenderMode.Mesh;
                r.mesh = QuadMesh();
            }
            else r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            return ps;
        }

        private static Material MakeMaterial(Cfg c)
        {
            var tex = MakeTexture(c.Tex);
            Shader sh = Shader.Find("DS/Particle");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            Material m = sh != null ? new Material(sh) : new Material(Mats.Emit(Color.white, c.Intensity));
            m.name = "FX_" + c.Name;
            m.mainTexture = tex;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", c.Intensity);
            m.renderQueue = 3050;
            return m;
        }

        private static Mesh _quad;

        private static Mesh QuadMesh()
        {
            if (_quad != null) return _quad;
            _quad = new Mesh { name = "FX_Quad" };
            _quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            _quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            _quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _quad.RecalculateNormals();
            _quad.RecalculateBounds();
            return _quad;
        }

        // ---- Prozedurale Texturen ---------------------------------------------------------------------
        private static Texture2D MakeTexture(Tex kind)
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = "FX_" + kind,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[n * n];
            Vector2[] star = kind == Tex.Star5 ? StarPolygon(0.95f, 0.42f) : null;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                Color c = Color.white;
                float a;
                switch (kind)
                {
                    case Tex.Soft:
                        a = Mathf.Clamp01(1f - r);
                        a *= a;
                        break;
                    case Tex.Star4:
                    {
                        float core = Mathf.Exp(-r * r * 14f);
                        float arms = Mathf.Exp(-Mathf.Abs(u) * 20f) * Mathf.Exp(-Mathf.Abs(v) * 3f) + Mathf.Exp(-Mathf.Abs(v) * 20f) * Mathf.Exp(-Mathf.Abs(u) * 3f);
                        a = Mathf.Clamp01(core + arms) * Mathf.Clamp01((1f - r) * 4f);
                        break;
                    }
                    case Tex.Square:
                        a = Mathf.Clamp01((0.9f - Mathf.Max(Mathf.Abs(u), Mathf.Abs(v))) * 18f);
                        break;
                    case Tex.Coin:
                    {
                        a = Mathf.Clamp01((0.92f - r) * 24f);
                        var face = new Color(1f, 0.8f, 0.26f);
                        var rim = new Color(0.82f, 0.56f, 0.1f);
                        c = r > 0.72f ? rim : face;
                        if (Mathf.Abs(r - 0.56f) < 0.04f) c = rim;
                        float light = 1f + (v - u) * 0.18f;
                        c = new Color(c.r * light, c.g * light, c.b * light);
                        if (r < 0.3f && Mathf.Abs(u) < 0.07f) c = rim; // kleine Prägung
                        break;
                    }
                    default:
                    {
                        // 5-zackiger Stern, 2×2 Supersampling für glatte Kanten
                        int inside = 0;
                        for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float su = (x + 0.25f + sx * 0.5f) / n * 2f - 1f, sv = (y + 0.25f + sy * 0.5f) / n * 2f - 1f;
                            if (InPolygon(star, su, sv)) inside++;
                        }
                        a = inside / 4f;
                        break;
                    }
                }
                c.a = a;
                px[y * n + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        private static Vector2[] StarPolygon(float outer, float inner)
        {
            var p = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float rr = i % 2 == 0 ? outer : inner;
                p[i] = new Vector2(Mathf.Cos(ang) * rr, Mathf.Sin(ang) * rr - 0.05f);
            }
            return p;
        }

        private static bool InPolygon(Vector2[] poly, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                var a = poly[i];
                var b = poly[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }
    }
}
