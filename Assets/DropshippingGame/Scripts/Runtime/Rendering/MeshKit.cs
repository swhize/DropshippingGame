using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Erzeugt und cached alle Grundkörper: Quader (optional mit abgefasten Kanten für einen
    /// hochwertigeren Look), Zylinder/Kegel und Kugeln (glatt oder facettiert für Laub).
    /// UV-Koordinaten sind in Metern, damit gekachelte Texturen auf jeder Größe gleich groß wirken.
    /// </summary>
    public static class MeshKit
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Cache.Clear();

        private sealed class Builder
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<int> T = new List<int>();
            public bool CapUvZero;

            private Vector2 Uv(Vector3 p, Vector3 n)
            {
                Vector3 a = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
                if (a.y >= a.x && a.y >= a.z) return CapUvZero ? new Vector2(0.02f, 0.02f) : new Vector2(p.x, p.z);
                if (a.x >= a.z) return new Vector2(p.z, p.y);
                return new Vector2(p.x, p.y);
            }

            /// <summary>Dreieck mit fester Flächennormale; Reihenfolge wird automatisch korrigiert.</summary>
            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f)
                {
                    var tmp = b;
                    b = c;
                    c = tmp;
                }
                int i = V.Count;
                V.Add(a);
                V.Add(b);
                V.Add(c);
                N.Add(normal);
                N.Add(normal);
                N.Add(normal);
                UV.Add(Uv(a, normal));
                UV.Add(Uv(b, normal));
                UV.Add(Uv(c, normal));
                T.Add(i);
                T.Add(i + 1);
                T.Add(i + 2);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f)
                {
                    var tmp = b;
                    b = d;
                    d = tmp;
                }
                int i = V.Count;
                V.Add(a);
                V.Add(b);
                V.Add(c);
                V.Add(d);
                for (int k = 0; k < 4; k++) N.Add(normal);
                UV.Add(Uv(a, normal));
                UV.Add(Uv(b, normal));
                UV.Add(Uv(c, normal));
                UV.Add(Uv(d, normal));
                T.Add(i);
                T.Add(i + 1);
                T.Add(i + 2);
                T.Add(i);
                T.Add(i + 2);
                T.Add(i + 3);
            }

            public Mesh Build(string name)
            {
                var m = new Mesh { name = name };
                if (V.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(V);
                m.SetNormals(N);
                m.SetUVs(0, UV);
                m.SetTriangles(T, 0);
                m.RecalculateBounds();
                m.RecalculateTangents();
                m.UploadMeshData(false);
                return m;
            }
        }

        private static string Key(string kind, params float[] v)
        {
            var sb = new System.Text.StringBuilder(kind);
            foreach (float f in v) sb.Append('|').Append(Mathf.RoundToInt(f * 1000f));
            return sb.ToString();
        }

        /// <summary>
        /// Quader mit Mittelpunkt im Ursprung. bevel &gt; 0 fast die Kanten ab (fängt Licht, wirkt hochwertiger).
        /// capUvZero: Ober-/Unterseite bekommen eine feste UV (für Fassaden ohne Fenster auf dem Dach).
        /// </summary>
        public static Mesh Box(Vector3 size, float bevel = 0f, bool capUvZero = false)
        {
            float minDim = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            bevel = Mathf.Min(bevel, minDim * 0.3f);
            if (bevel < 0.002f) bevel = 0f;
            string key = Key(capUvZero ? "boxc" : "box", size.x, size.y, size.z, bevel);
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var b = new Builder { CapUvZero = capUvZero };
            float hx = size.x / 2f, hy = size.y / 2f, hz = size.z / 2f;
            float e = bevel;
            // Hauptflächen (eingerückt um die Fase)
            b.Quad(new Vector3(hx, -hy + e, -hz + e), new Vector3(hx, hy - e, -hz + e), new Vector3(hx, hy - e, hz - e), new Vector3(hx, -hy + e, hz - e), Vector3.right);
            b.Quad(new Vector3(-hx, -hy + e, -hz + e), new Vector3(-hx, hy - e, -hz + e), new Vector3(-hx, hy - e, hz - e), new Vector3(-hx, -hy + e, hz - e), Vector3.left);
            b.Quad(new Vector3(-hx + e, hy, -hz + e), new Vector3(hx - e, hy, -hz + e), new Vector3(hx - e, hy, hz - e), new Vector3(-hx + e, hy, hz - e), Vector3.up);
            b.Quad(new Vector3(-hx + e, -hy, -hz + e), new Vector3(hx - e, -hy, -hz + e), new Vector3(hx - e, -hy, hz - e), new Vector3(-hx + e, -hy, hz - e), Vector3.down);
            b.Quad(new Vector3(-hx + e, -hy + e, hz), new Vector3(hx - e, -hy + e, hz), new Vector3(hx - e, hy - e, hz), new Vector3(-hx + e, hy - e, hz), Vector3.forward);
            b.Quad(new Vector3(-hx + e, -hy + e, -hz), new Vector3(hx - e, -hy + e, -hz), new Vector3(hx - e, hy - e, -hz), new Vector3(-hx + e, hy - e, -hz), Vector3.back);

            if (e > 0f)
            {
                // 12 Kanten-Fasen
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    for (int sy = -1; sy <= 1; sy += 2)
                    {
                        // Kanten entlang Z (zwischen X- und Y-Fläche)
                        b.Quad(new Vector3(sx * hx, sy * (hy - e), -hz + e), new Vector3(sx * (hx - e), sy * hy, -hz + e),
                            new Vector3(sx * (hx - e), sy * hy, hz - e), new Vector3(sx * hx, sy * (hy - e), hz - e), new Vector3(sx, sy, 0).normalized);
                    }
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        // Kanten entlang Y (zwischen X- und Z-Fläche)
                        b.Quad(new Vector3(sx * hx, -hy + e, sz * (hz - e)), new Vector3(sx * (hx - e), -hy + e, sz * hz),
                            new Vector3(sx * (hx - e), hy - e, sz * hz), new Vector3(sx * hx, hy - e, sz * (hz - e)), new Vector3(sx, 0, sz).normalized);
                    }
                }
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        // Kanten entlang X (zwischen Y- und Z-Fläche)
                        b.Quad(new Vector3(-hx + e, sy * hy, sz * (hz - e)), new Vector3(-hx + e, sy * (hy - e), sz * hz),
                            new Vector3(hx - e, sy * (hy - e), sz * hz), new Vector3(hx - e, sy * hy, sz * (hz - e)), new Vector3(0, sy, sz).normalized);
                    }
                }
                // 8 Ecken
                for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    b.Tri(new Vector3(sx * hx, sy * (hy - e), sz * (hz - e)), new Vector3(sx * (hx - e), sy * hy, sz * (hz - e)),
                        new Vector3(sx * (hx - e), sy * (hy - e), sz * hz), new Vector3(sx, sy, sz).normalized);
                }
            }
            var mesh = b.Build(key);
            Cache[key] = mesh;
            return mesh;
        }

        /// <summary>Zylinder/Kegelstumpf entlang Y, Mittelpunkt im Ursprung (wie in Godot).</summary>
        public static Mesh Cylinder(float rTop, float rBottom, float height, int segments = 16)
        {
            segments = Mathf.Max(3, segments);
            string key = Key("cyl", rTop, rBottom, height, segments);
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            float hh = height / 2f;
            float slope = (rBottom - rTop) / Mathf.Max(height, 0.0001f);
            float circ = Mathf.PI * 2f * Mathf.Max(rTop, rBottom);
            // Mantel (glatte Normalen)
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                float cx = Mathf.Cos(a), sz = Mathf.Sin(a);
                Vector3 normal = new Vector3(cx, slope, sz).normalized;
                v.Add(new Vector3(cx * rBottom, -hh, sz * rBottom));
                v.Add(new Vector3(cx * rTop, hh, sz * rTop));
                n.Add(normal);
                n.Add(normal);
                float u = i / (float)segments * circ;
                uv.Add(new Vector2(u, 0f));
                uv.Add(new Vector2(u, height));
            }
            for (int i = 0; i < segments; i++)
            {
                int k = i * 2;
                // Front = außen, Unity: im Uhrzeigersinn von außen gesehen
                t.Add(k);
                t.Add(k + 1);
                t.Add(k + 2);
                t.Add(k + 1);
                t.Add(k + 3);
                t.Add(k + 2);
            }
            // Deckel
            void Cap(float y, float r, Vector3 normal)
            {
                if (r <= 0.0001f) return;
                int center = v.Count;
                v.Add(new Vector3(0, y, 0));
                n.Add(normal);
                uv.Add(Vector2.zero);
                for (int i = 0; i <= segments; i++)
                {
                    float a = i / (float)segments * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                    n.Add(normal);
                    uv.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
                }
                for (int i = 0; i < segments; i++)
                {
                    int a0 = center + 1 + i, a1 = center + 2 + i;
                    if (normal.y > 0f)
                    {
                        t.Add(center);
                        t.Add(a1);
                        t.Add(a0);
                    }
                    else
                    {
                        t.Add(center);
                        t.Add(a0);
                        t.Add(a1);
                    }
                }
            }
            Cap(hh, rTop, Vector3.up);
            Cap(-hh, rBottom, Vector3.down);
            var mesh = new Mesh { name = key };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            FixWinding(mesh);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            Cache[key] = mesh;
            return mesh;
        }

        /// <summary>Kugel. flat = facettiert (Low-Poly-Laub), sonst glatt.</summary>
        public static Mesh Sphere(float radius, int segments = 16, int rings = 8, bool flat = false)
        {
            string key = Key(flat ? "sphf" : "sph", radius, segments, rings);
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var pts = new Vector3[rings + 1, segments + 1];
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                for (int s = 0; s <= segments; s++)
                {
                    float th = Mathf.PI * 2f * s / segments;
                    pts[r, s] = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th)) * radius;
                }
            }
            Mesh mesh;
            if (flat)
            {
                var b = new Builder();
                for (int r = 0; r < rings; r++)
                {
                    for (int s = 0; s < segments; s++)
                    {
                        Vector3 a = pts[r, s], bb = pts[r, s + 1], c = pts[r + 1, s + 1], d = pts[r + 1, s];
                        Vector3 center = (a + bb + c + d) / 4f;
                        if (r == 0) b.Tri(a, c, d, center.normalized);
                        else if (r == rings - 1) b.Tri(a, bb, c, center.normalized);
                        else
                        {
                            b.Tri(a, bb, c, ((a + bb + c) / 3f).normalized);
                            b.Tri(a, c, d, ((a + c + d) / 3f).normalized);
                        }
                    }
                }
                mesh = b.Build(key);
            }
            else
            {
                var v = new List<Vector3>();
                var n = new List<Vector3>();
                var uv = new List<Vector2>();
                var t = new List<int>();
                for (int r = 0; r <= rings; r++)
                {
                    for (int s = 0; s <= segments; s++)
                    {
                        v.Add(pts[r, s]);
                        n.Add(pts[r, s].normalized);
                        uv.Add(new Vector2((float)s / segments * Mathf.PI * 2f * radius, (float)r / rings * Mathf.PI * radius));
                    }
                }
                int row = segments + 1;
                for (int r = 0; r < rings; r++)
                {
                    for (int s = 0; s < segments; s++)
                    {
                        int a = r * row + s, bb = a + 1, c = a + row + 1, d = a + row;
                        t.Add(a);
                        t.Add(bb);
                        t.Add(c);
                        t.Add(a);
                        t.Add(c);
                        t.Add(d);
                    }
                }
                mesh = new Mesh { name = key };
                mesh.SetVertices(v);
                mesh.SetNormals(n);
                mesh.SetUVs(0, uv);
                mesh.SetTriangles(t, 0);
                FixWinding(mesh);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
            }
            Cache[key] = mesh;
            return mesh;
        }

        /// <summary>Dreht jedes Dreieck so, dass es zur (gespeicherten) Normale zeigt - schützt vor falscher Reihenfolge.</summary>
        private static void FixWinding(Mesh mesh)
        {
            var v = mesh.vertices;
            var n = mesh.normals;
            var t = mesh.triangles;
            bool changed = false;
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                Vector3 avgN = n[t[i]] + n[t[i + 1]] + n[t[i + 2]];
                Vector3 face = Vector3.Cross(b - a, c - a);
                if (face.sqrMagnitude > 1e-12f && Vector3.Dot(face, avgN) < 0f)
                {
                    int tmp = t[i + 1];
                    t[i + 1] = t[i + 2];
                    t[i + 2] = tmp;
                    changed = true;
                }
            }
            if (changed) mesh.triangles = t;
        }
    }
}
