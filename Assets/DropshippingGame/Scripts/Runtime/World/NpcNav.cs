using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Einfache Hindernisumgehung für NPCs ohne NavMesh: kleine, bodennahe Collider
    /// (Stationen, Bänke, Bäume, Mülltonnen, Laternen ...) werden als achsparallele
    /// Rechtecke (XZ) gesammelt. Kreuzt der direkte Weg ein Rechteck, läuft der NPC
    /// über die günstigste Ecke drumherum.
    /// </summary>
    public static class NpcNav
    {
        private struct Box
        {
            public float MinX, MaxX, MinZ, MaxZ;
        }

        /// <summary>Körperradius plus Sicherheitsabstand.</summary>
        public const float Radius = 0.4f;
        private const float MaxFootprint = 7f;

        private static readonly List<Box> Boxes = new List<Box>();
        private static readonly List<Vector2> Extra = new List<Vector2>();

        /// <summary>Zusätzliche runde Hindernisse ohne Collider (z.B. Laternen), als XZ-Mittelpunkt.</summary>
        public static void AddPoint(Vector3 worldPos)
        {
            Extra.Add(new Vector2(worldPos.x, worldPos.z));
        }

        public static void ClearPoints() => Extra.Clear();

        /// <summary>Sammelt alle Hindernisse unterhalb von <paramref name="root"/> neu ein.</summary>
        public static void Rebuild(Transform root, Transform exclude = null)
        {
            Boxes.Clear();
            if (root != null)
            {
                BoxCollider[] cols;
                try { cols = root.GetComponentsInChildren<BoxCollider>(false); }
                catch { cols = new BoxCollider[0]; }
                foreach (var c in cols)
                {
                    if (c == null || !c.enabled || c.isTrigger) continue;
                    if (c.GetComponentInParent<NPC>() != null) continue;
                    if (exclude != null && c.transform.IsChildOf(exclude)) continue;
                    UnityEngine.Bounds b;
                    try { b = c.bounds; }
                    catch { continue; }
                    // Nur Dinge, die man am Boden anrempeln würde; keine Böden, Decken, Riesenwände.
                    if (b.min.y > 1.2f || b.max.y < 0.25f) continue;
                    if (b.size.x > MaxFootprint || b.size.z > MaxFootprint) continue;
                    if (b.size.x < 0.01f || b.size.z < 0.01f) continue;
                    Add(b.min.x, b.max.x, b.min.z, b.max.z);
                }
            }
            foreach (var p in Extra) Add(p.x - 0.15f, p.x + 0.15f, p.y - 0.15f, p.y + 0.15f);
        }

        private static void Add(float minX, float maxX, float minZ, float maxZ)
        {
            Boxes.Add(new Box { MinX = minX - Radius, MaxX = maxX + Radius, MinZ = minZ - Radius, MaxZ = maxZ + Radius });
        }

        private static bool Inside(Box b, float x, float z, float shrink = 0f) =>
            x > b.MinX + shrink && x < b.MaxX - shrink && z > b.MinZ + shrink && z < b.MaxZ - shrink;

        /// <summary>Liegt ein Punkt (Welt) in einem Hindernis?</summary>
        public static bool Blocked(Vector3 p)
        {
            for (int i = 0; i < Boxes.Count; i++)
                if (Inside(Boxes[i], p.x, p.z)) return true;
            return false;
        }

        /// <summary>Segment-Rechteck-Test (Slab), liefert Eintrittsparameter 0..1.</summary>
        private static bool SegHits(Box b, float ax, float az, float bx, float bz, float shrink, out float tEnter)
        {
            tEnter = 0f;
            float t0 = 0f, t1 = 1f;
            float dx = bx - ax, dz = bz - az;
            if (!Slab(ax, dx, b.MinX + shrink, b.MaxX - shrink, ref t0, ref t1)) return false;
            if (!Slab(az, dz, b.MinZ + shrink, b.MaxZ - shrink, ref t0, ref t1)) return false;
            tEnter = t0;
            return t1 > t0;
        }

        private static bool Slab(float a, float d, float min, float max, ref float t0, ref float t1)
        {
            if (Mathf.Abs(d) < 1e-6f) return a > min && a < max;
            float ta = (min - a) / d, tb = (max - a) / d;
            if (ta > tb) { float s = ta; ta = tb; tb = s; }
            if (ta > t0) t0 = ta;
            if (tb < t1) t1 = tb;
            return t1 > t0;
        }

        /// <summary>
        /// Nächster Zwischenpunkt auf dem Weg von <paramref name="from"/> nach <paramref name="to"/> (Welt, y wird übernommen).
        /// Ist der Weg frei, kommt <paramref name="to"/> zurück.
        /// </summary>
        public static Vector3 Steer(Vector3 from, Vector3 to)
        {
            int hit = -1;
            float best = 2f;
            for (int i = 0; i < Boxes.Count; i++)
            {
                var b = Boxes[i];
                // Hindernisse, in denen Start oder Ziel liegt, ignorieren (sonst Festhängen).
                if (Inside(b, from.x, from.z) || Inside(b, to.x, to.z)) continue;
                if (SegHits(b, from.x, from.z, to.x, to.z, 0.02f, out float te) && te < best)
                {
                    best = te;
                    hit = i;
                }
            }
            if (hit < 0) return to;
            var h = Boxes[hit];
            const float pad = 0.12f;
            var corners = new[]
            {
                new Vector2(h.MinX - pad, h.MinZ - pad), new Vector2(h.MaxX + pad, h.MinZ - pad),
                new Vector2(h.MinX - pad, h.MaxZ + pad), new Vector2(h.MaxX + pad, h.MaxZ + pad),
            };
            float bestCost = float.MaxValue;
            Vector2 pick = corners[0];
            bool found = false;
            foreach (var c in corners)
            {
                // Ecke muss vom Start aus direkt erreichbar sein (nicht durch dasselbe Hindernis).
                if (SegHits(h, from.x, from.z, c.x, c.y, 0.05f, out _)) continue;
                if (Blocked(new Vector3(c.x, 0f, c.y))) continue;
                float cost = Vector2.Distance(new Vector2(from.x, from.z), c) + Vector2.Distance(c, new Vector2(to.x, to.z));
                if (cost < bestCost) { bestCost = cost; pick = c; found = true; }
            }
            if (!found)
            {
                // Notlösung: auch blockierte Ecken zulassen, Hauptsache nicht quer durch.
                foreach (var c in corners)
                {
                    if (SegHits(h, from.x, from.z, c.x, c.y, 0.05f, out _)) continue;
                    float cost = Vector2.Distance(new Vector2(from.x, from.z), c) + Vector2.Distance(c, new Vector2(to.x, to.z));
                    if (cost < bestCost) { bestCost = cost; pick = c; found = true; }
                }
            }
            if (!found) return to;
            return new Vector3(pick.x, to.y, pick.y);
        }
    }
}
