using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DropshippingGame.Core
{
    /// <summary>Farbe ohne Unity-Abhängigkeit (0..1 pro Kanal). Die Unity-Schicht wandelt in UnityEngine.Color um.</summary>
    [Serializable]
    public struct RGBA : IEquatable<RGBA>
    {
        public float r, g, b, a;

        public RGBA(float r, float g, float b, float a = 1f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static RGBA Hex(string hex)
        {
            hex = hex.TrimStart('#');
            float C(int i) => int.Parse(hex.Substring(i, 2), NumberStyles.HexNumber) / 255f;
            return new RGBA(C(0), C(2), C(4), hex.Length >= 8 ? C(6) : 1f);
        }

        public RGBA Lerp(RGBA to, float t)
        {
            t = Mathx.Clamp01(t);
            return new RGBA(r + (to.r - r) * t, g + (to.g - g) * t, b + (to.b - b) * t, a + (to.a - a) * t);
        }

        public RGBA Darkened(float amount) => new RGBA(r * (1f - amount), g * (1f - amount), b * (1f - amount), a);
        public RGBA Lightened(float amount) => new RGBA(r + (1f - r) * amount, g + (1f - g) * amount, b + (1f - b) * amount, a);
        public RGBA WithAlpha(float alpha) => new RGBA(r, g, b, alpha);

        public bool Approx(RGBA o, float eps = 0.002f) =>
            Math.Abs(r - o.r) < eps && Math.Abs(g - o.g) < eps && Math.Abs(b - o.b) < eps && Math.Abs(a - o.a) < eps;

        public string ToHex()
        {
            int C(float v) => (int)Math.Round(Mathx.Clamp01(v) * 255f);
            return "#" + C(r).ToString("X2") + C(g).ToString("X2") + C(b).ToString("X2");
        }

        public bool Equals(RGBA o) => Approx(o, 0.0001f);
        public override bool Equals(object obj) => obj is RGBA o && Equals(o);
        public override int GetHashCode() => (int)(r * 1000) ^ ((int)(g * 1000) << 10) ^ ((int)(b * 1000) << 20);

        public List<object> ToJson() => new List<object> { (double)r, (double)g, (double)b, (double)a };

        public static RGBA FromJson(object o, RGBA fallback)
        {
            if (o is List<object> l && l.Count >= 3)
                return new RGBA(J.F(l[0]), J.F(l[1]), J.F(l[2]), l.Count > 3 ? J.F(l[3]) : 1f);
            return fallback;
        }
    }

    /// <summary>Position ohne Unity-Abhängigkeit.</summary>
    [Serializable]
    public struct V3
    {
        public float x, y, z;

        public V3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static readonly V3 Zero = new V3(0, 0, 0);
        public bool IsZero => x == 0f && y == 0f && z == 0f;
        public List<object> ToJson() => new List<object> { (double)x, (double)y, (double)z };

        public static V3 FromJson(object o)
        {
            if (o is List<object> l && l.Count >= 3)
                return new V3(J.F(l[0]), J.F(l[1]), J.F(l[2]));
            return Zero;
        }
    }

    public static class Mathx
    {
        public static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static int RoundToInt(float v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);
        public static int RoundToInt(double v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);
    }

    /// <summary>Zufallszahlen (seedbar für Tests).</summary>
    public sealed class Rng
    {
        private Random _r;

        public Rng(int? seed = null)
        {
            _r = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public void Reseed(int seed) => _r = new Random(seed);
        public float Value() => (float)_r.NextDouble();
        public float Range(float a, float b) => a + (b - a) * (float)_r.NextDouble();

        /// <summary>Ganzzahl im geschlossenen Intervall [a, b].</summary>
        public int RangeInt(int a, int b) => _r.Next(a, b + 1);

        public int Index(int count) => count <= 0 ? 0 : _r.Next(count);
        public T Pick<T>(IList<T> list) => list[Index(list.Count)];

        public float Normal(float mean, float sd)
        {
            double u1 = 1.0 - _r.NextDouble();
            double u2 = _r.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + sd * (float)z;
        }
    }

    /// <summary>Deutsche Formatierung für Geld, Uhrzeit, Prozent, Bewertung.</summary>
    public static class Fmt
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Money(int v)
        {
            bool neg = v < 0;
            string s = Math.Abs((long)v).ToString(Inv);
            var sb = new StringBuilder();
            int count = 0;
            for (int i = s.Length - 1; i >= 0; i--)
            {
                sb.Insert(0, s[i]);
                count++;
                if (count % 3 == 0 && i > 0) sb.Insert(0, '.');
            }
            return (neg ? "−" : "") + sb + " €";
        }

        public static string Money(float v) => Money(Mathx.RoundToInt(v));
        public static string SignedMoney(int v) => (v > 0 ? "+" : "") + Money(v);
        public static string Eur(float v) => v.ToString("0.00", Inv).Replace('.', ',') + " €";
        public static string Dec(float v, int decimals = 1) => v.ToString("F" + decimals, Inv).Replace('.', ',');
        public static string Rating(float r) => Dec(r, 1);

        public static string Stars(float r)
        {
            int full = Mathx.RoundToInt(r);
            var sb = new StringBuilder();
            for (int i = 0; i < 5; i++) sb.Append(i < full ? '★' : '☆');
            return sb.ToString();
        }

        public static string Clock(float minutes)
        {
            int m = (int)minutes;
            return ((m / 60) % 24).ToString("00", Inv) + ":" + (m % 60).ToString("00", Inv);
        }

        public static string Pct(float v) => (v >= 0 ? "+" : "−") + Math.Abs(v * 100f).ToString("0.0", Inv).Replace('.', ',') + " %";

        public static string Thousands(int v)
        {
            if (v >= 1000000) return Dec(v / 1000000f, 1) + " Mio.";
            if (v >= 1000) return (v / 1000) + " Tsd.";
            return v.ToString(Inv);
        }
    }
}
