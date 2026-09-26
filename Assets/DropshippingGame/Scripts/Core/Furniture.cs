using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>Vom Spieler verschobene Position eines Möbelstücks (Boden-Koordinaten x/z, Drehung um y in Grad).</summary>
    public sealed class FurniturePlacement
    {
        public float X, Z, RotY;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "x", (double)X }, { "z", (double)Z }, { "rot", (double)RotY },
        };

        /// <summary>null bei ungültigen Daten (fehlende/NaN-Werte).</summary>
        public static FurniturePlacement FromJson(object o)
        {
            var d = J.Obj(o);
            if (d.Count == 0 || !d.ContainsKey("x") || !d.ContainsKey("z")) return null;
            float x = J.F(d, "x"), z = J.F(d, "z"), r = J.F(d, "rot");
            if (!FurnitureLayout.Finite(x) || !FurnitureLayout.Finite(z)) return null;
            if (!FurnitureLayout.Finite(r)) r = 0f;
            return new FurniturePlacement { X = x, Z = z, RotY = FurnitureLayout.NormalizeAngle(r) };
        }
    }

    /// <summary>Gedrehtes Rechteck auf dem Boden (Grundfläche eines Möbelstücks).</summary>
    public struct Footprint
    {
        public float CX, CZ, HalfX, HalfZ, RotY;
        public string Name;
        /// <summary>Flache Objekte (Teppich) dürfen unter anderen liegen.</summary>
        public bool Flat;

        public Footprint(float cx, float cz, float halfX, float halfZ, float rotY, string name = "", bool flat = false)
        {
            CX = cx;
            CZ = cz;
            HalfX = Math.Max(0f, halfX);
            HalfZ = Math.Max(0f, halfZ);
            RotY = rotY;
            Name = name ?? "";
            Flat = flat;
        }
    }

    /// <summary>Achsparalleles Rechteck (Raum, Sperrzone vor Türen).</summary>
    public struct FloorRect
    {
        public float MinX, MinZ, MaxX, MaxZ;

        public FloorRect(float minX, float minZ, float maxX, float maxZ)
        {
            MinX = Math.Min(minX, maxX);
            MaxX = Math.Max(minX, maxX);
            MinZ = Math.Min(minZ, maxZ);
            MaxZ = Math.Max(minZ, maxZ);
        }

        public FloorRect Inset(float m) => new FloorRect(MinX + m, MinZ + m, MaxX - m, MaxZ - m);
    }

    /// <summary>Regeln für den Bau-/Verschiebemodus: Raster, Drehung, Gültigkeit.</summary>
    public static class FurnitureLayout
    {
        public const float Grid = 0.25f;
        public const float RotStep = 15f;
        /// <summary>Kleine Toleranz, damit sich berührende Möbel nicht als Überschneidung gelten.</summary>
        public const float Tolerance = 0.06f;

        public static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

        public static float Snap(float v, float grid = Grid)
        {
            if (!Finite(v)) return 0f;
            if (grid <= 0f) return v;
            return (float)Math.Round(v / grid, MidpointRounding.AwayFromZero) * grid;
        }

        /// <summary>Winkel in [0, 360).</summary>
        public static float NormalizeAngle(float deg)
        {
            if (!Finite(deg)) return 0f;
            float r = deg % 360f;
            if (r < 0f) r += 360f;
            if (r >= 360f - 0.0001f) r = 0f;
            return r;
        }

        public static float SnapAngle(float deg, float step = RotStep) => NormalizeAngle(Snap(NormalizeAngle(deg), step));

        /// <summary>Ecken des Rechtecks (Unity-Konvention: Drehung um y im Uhrzeigersinn von oben gesehen).</summary>
        public static void Corners(Footprint f, float[] xs, float[] zs)
        {
            double a = f.RotY * Math.PI / 180.0;
            float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
            // lokale x-Achse -> (cos, -sin), lokale z-Achse -> (sin, cos)
            int i = 0;
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                float lx = sx * f.HalfX, lz = sz * f.HalfZ;
                xs[i] = f.CX + lx * c + lz * s;
                zs[i] = f.CZ - lx * s + lz * c;
                i++;
            }
        }

        /// <summary>Überschneiden sich zwei gedrehte Rechtecke (Trennachsen-Test, mit Toleranz)?</summary>
        public static bool Overlaps(Footprint a, Footprint b, float tolerance = Tolerance)
        {
            var sa = Shrink(a, tolerance);
            var sb = Shrink(b, tolerance);
            if (sa.HalfX <= 0f || sa.HalfZ <= 0f || sb.HalfX <= 0f || sb.HalfZ <= 0f) return false;
            var ax = new float[4];
            var az = new float[4];
            var bx = new float[4];
            var bz = new float[4];
            Corners(sa, ax, az);
            Corners(sb, bx, bz);
            foreach (float rot in new[] { sa.RotY, sb.RotY })
            {
                double r = rot * Math.PI / 180.0;
                float c = (float)Math.Cos(r), s = (float)Math.Sin(r);
                // Achsen: lokale x und z des jeweiligen Rechtecks
                if (Separated(ax, az, bx, bz, c, -s) || Separated(ax, az, bx, bz, s, c)) return false;
            }
            return true;
        }

        private static Footprint Shrink(Footprint f, float t) =>
            new Footprint(f.CX, f.CZ, f.HalfX - t, f.HalfZ - t, f.RotY, f.Name, f.Flat);

        private static bool Separated(float[] ax, float[] az, float[] bx, float[] bz, float nx, float nz)
        {
            float aMin = float.MaxValue, aMax = float.MinValue, bMin = float.MaxValue, bMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                float pa = ax[i] * nx + az[i] * nz;
                float pb = bx[i] * nx + bz[i] * nz;
                aMin = Math.Min(aMin, pa);
                aMax = Math.Max(aMax, pa);
                bMin = Math.Min(bMin, pb);
                bMax = Math.Max(bMax, pb);
            }
            return aMax <= bMin || bMax <= aMin;
        }

        public static bool Overlaps(Footprint a, FloorRect r)
        {
            var rf = new Footprint((r.MinX + r.MaxX) / 2f, (r.MinZ + r.MaxZ) / 2f, (r.MaxX - r.MinX) / 2f, (r.MaxZ - r.MinZ) / 2f, 0f);
            return Overlaps(a, rf, 0f);
        }

        /// <summary>Liegt das Rechteck komplett im Raum?</summary>
        public static bool Inside(Footprint f, FloorRect room)
        {
            var xs = new float[4];
            var zs = new float[4];
            Corners(f, xs, zs);
            for (int i = 0; i < 4; i++)
                if (xs[i] < room.MinX - 0.001f || xs[i] > room.MaxX + 0.001f || zs[i] < room.MinZ - 0.001f || zs[i] > room.MaxZ + 0.001f)
                    return false;
            return true;
        }

        /// <summary>
        /// Prüft einen Platz. Gibt null zurück, wenn er gültig ist, sonst den Grund (deutscher Hinweistext).
        /// Flache Objekte (Teppich) dürfen andere überlappen und werden selbst überlappt.
        /// </summary>
        public static string Validate(Footprint f, FloorRect room, IList<Footprint> others, IList<FloorRect> keepOut)
        {
            if (!Finite(f.CX) || !Finite(f.CZ) || !Finite(f.RotY)) return "Ungültige Position";
            if (!Inside(f, room)) return "Muss im Gebäude stehen";
            if (keepOut != null)
                foreach (var k in keepOut)
                    if (Overlaps(f, k)) return "Tür / Durchgang frei halten";
            if (others != null && !f.Flat)
                foreach (var o in others)
                    if (!o.Flat && Overlaps(f, o)) return string.IsNullOrEmpty(o.Name) ? "Platz belegt" : "Stößt an: " + o.Name;
            return null;
        }
    }

    public sealed partial class Sim
    {
        /// <summary>Vom Spieler verschobene Möbel/Stationen (Schlüssel siehe WorldBuilder, z. B. "st:1:Regal:3").</summary>
        public readonly Dictionary<string, FurniturePlacement> Furniture = new Dictionary<string, FurniturePlacement>();

        public void SetFurniture(string key, float x, float z, float rotY)
        {
            if (string.IsNullOrEmpty(key) || !FurnitureLayout.Finite(x) || !FurnitureLayout.Finite(z)) return;
            Furniture[key] = new FurniturePlacement { X = x, Z = z, RotY = FurnitureLayout.NormalizeAngle(rotY) };
        }

        public FurniturePlacement GetFurniture(string key) =>
            !string.IsNullOrEmpty(key) && Furniture.TryGetValue(key, out var p) ? p : null;

        private Dictionary<string, object> FurnitureToJson()
        {
            var d = new Dictionary<string, object>();
            foreach (var kv in Furniture)
                if (kv.Value != null) d[kv.Key] = kv.Value.ToJson();
            return d;
        }

        /// <summary>Fehlt "furniture" (ältere Spielstände), bleibt alles am Standardplatz.</summary>
        private void ReadFurniture(Dictionary<string, object> s)
        {
            Furniture.Clear();
            foreach (var kv in J.O(s, "furniture"))
            {
                var p = FurniturePlacement.FromJson(kv.Value);
                if (p != null && !string.IsNullOrEmpty(kv.Key)) Furniture[kv.Key] = p;
            }
        }
    }
}
