using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Kartenrand der Hauptstraße: an beiden Enden ein Tunnelportal ("Stadttunnel"). Autos fahren auf
    /// x = ±175 los bzw. verschwinden dort, Passanten kommen am Rand – im dunklen Tunnelinneren sieht man
    /// weder Spawn noch Despawn, und die Straße endet sichtbar statt ins Nichts zu laufen.
    /// </summary>
    public static class CityEdges
    {
        private const float PortalX = 153f;     // Vorderkante des Portals (Betrag)
        private const float Depth = 24f;        // Tiefe des dunklen Inneren (deckt die Spawnpunkte bei ±175 ab)
        private const float OpenHalfZ = 7.2f;   // Öffnung: Fahrbahn + Gehwege
        private const float OpenH = 5.2f;
        private const float WallHalfZ = 11f;
        private const float WallH = 9.5f;

        public static void Build(Transform staticRoot, Transform signs)
        {
            if (staticRoot == null) return;
            foreach (float side in new[] { -1f, 1f })
            {
                try { Portal(staticRoot, signs, side); }
                catch (System.Exception e) { Debug.LogWarning("[CityEdges] " + e.Message); }
            }
        }

        private static void Portal(Transform root, Transform signs, float side)
        {
            var node = new GameObject(side < 0 ? "TunnelWest" : "TunnelEast").transform;
            node.SetParent(root, false);
            var concrete = Mats.Concrete(new Color(0.6f, 0.58f, 0.55f), new Color(0.47f, 0.46f, 0.44f));
            var dark = Mats.Std(new Color(0.015f, 0.015f, 0.02f), 1f);
            var trim = Mats.Std(new Color(0.85f, 0.62f, 0.12f), 0.7f);
            float fx = side * PortalX;
            float cx = side * (PortalX + Depth * 0.5f);
            float wallT = 1.2f;
            float wx = side * (PortalX + wallT * 0.5f);

            // Dunkles Tunnelinneres: geschlossener Block direkt hinter der Öffnung – alles darin ist unsichtbar.
            Props.Box(node, new Vector3(Depth, OpenH, OpenHalfZ * 2f), dark, new Vector3(cx, OpenH * 0.5f, 0f), default, 0f, false);
            // Portalwand: Sturz über der Öffnung und seitliche Pfeiler
            Props.Box(node, new Vector3(wallT, WallH - OpenH, WallHalfZ * 2f), concrete, new Vector3(wx, OpenH + (WallH - OpenH) * 0.5f, 0f));
            foreach (float zs in new[] { -1f, 1f })
            {
                float pw = WallHalfZ - OpenHalfZ;
                Props.Box(node, new Vector3(wallT, OpenH, pw), concrete, new Vector3(wx, OpenH * 0.5f, zs * (OpenHalfZ + pw * 0.5f)));
                // Hang/Erdwall links und rechts, damit das Portal im Gelände sitzt
                Props.Box(node, new Vector3(Depth, WallH, 14f), Mats.Std(new Color(0.33f, 0.45f, 0.25f), 0.95f),
                    new Vector3(cx, WallH * 0.5f - 0.4f, zs * (WallHalfZ + 7f)), default, 0f);
            }
            // Erdwall über dem Tunnel
            Props.Box(node, new Vector3(Depth - wallT, 3.5f, WallHalfZ * 2f), Mats.Std(new Color(0.33f, 0.45f, 0.25f), 0.95f),
                new Vector3(side * (PortalX + wallT + (Depth - wallT) * 0.5f), WallH - 1.2f, 0f), default, 0f);
            // Gelb-schwarze Warnkante am Sturz
            Props.Box(node, new Vector3(0.12f, 0.35f, OpenHalfZ * 2f), trim, new Vector3(fx - side * 0.06f, OpenH + 0.2f, 0f), default, 0f, false);
            // Leuchtband innen (wirkt wie Tunnelbeleuchtung, ohne echte Lichter)
            Props.Box(node, new Vector3(0.1f, 0.12f, OpenHalfZ * 1.6f), Mats.Emit(new Color(1f, 0.78f, 0.45f), 2.2f),
                new Vector3(fx + side * 0.4f, OpenH - 0.25f, 0f), default, 0f, false);

            // Schild über der Einfahrt
            if (signs != null)
            {
                var sign = Props.SignBoard(signs, side < 0 ? "STADTTUNNEL\n← Autobahn" : "STADTTUNNEL\nAutobahn →",
                    new Color(0.12f, 0.36f, 0.72f), new Color(0.96f, 0.96f, 0.96f), new Vector2(4.2f, 1.3f), SignFont.Condensed);
                sign.transform.localPosition = new Vector3(fx - side * 0.7f, OpenH + 2.1f, 0f);
                sign.transform.localRotation = Quaternion.Euler(0, side > 0 ? -90f : 90f, 0);
            }
            // Kollision: der Spieler kann nicht in den Tunnel laufen (Autos/NPCs bewegen sich ohne Physik)
            Props.Collider(node, new Vector3(0.6f, OpenH, OpenHalfZ * 2f), new Vector3(side * (PortalX + 0.6f), OpenH * 0.5f, 0f));
        }
    }
}
