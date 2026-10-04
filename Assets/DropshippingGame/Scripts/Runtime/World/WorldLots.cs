using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Stadtplan zum Nachschlagen für alle Welt-Systeme (lokale Koordinaten der Welt = Weltkoordinaten,
    /// die Welt liegt im Ursprung). Hauptstraße entlang X bei z = 0 (Fahrbahn z −4..4, Gehwege z ±4..7),
    /// spielbarer Bereich x −150..150, z −50..83. Rechtecke: <c>Rect(xMin, zMin, Breite, Tiefe)</c>.
    ///
    /// <b>Reservierte Grundstücke</b> (werden von <see cref="CityBuilder"/> frei gelassen – andere Systeme bauen dort):
    /// <list type="bullet">
    /// <item><see cref="Finanzviertel"/>: Nordseite x 60..110, z 8..45 (Bank + Börse).</item>
    /// <item><see cref="Einkaufsviertel"/>: Südseite x −125..−60, z −45..−8 (Elektromarkt + Tierladen).</item>
    /// </list>
    /// Jedes Grundstück hat einen Eingangspunkt (Mitte der Straßenfront, etwas auf dem Grundstück),
    /// eine Blickrichtung nach außen (zur Straße) und einen Gehweg-Anschluss (Punkt auf dem Gehweg davor).
    /// </summary>
    public static class WorldLots
    {
        /// <summary>Ein Grundstück mit Zugang.</summary>
        public sealed class Lot
        {
            public string Id;
            public string Name;
            /// <summary>Fläche in XZ: x = xMin, y = zMin, width = Ausdehnung X, height = Ausdehnung Z.</summary>
            public Rect Area;
            /// <summary>Eingang (Mitte der Straßenfront, 0,5 m auf dem Grundstück).</summary>
            public Vector3 Entrance;
            /// <summary>Drehung (Grad um Y), mit der eine Fassade zur Straße zeigt (Front = lokal +Z).</summary>
            public float FacingRotY;
            /// <summary>Punkt auf dem Gehweg vor dem Eingang (für Wege von Passanten, Ziel-Markierungen ...).</summary>
            public Vector3 StreetAccess;
            /// <summary>Empfohlene Teilflächen (z. B. Bank / Börse), jeweils mit eigenem Eingang.</summary>
            public Lot[] Parts = new Lot[0];

            public Vector3 Center => new Vector3(Area.center.x, 0f, Area.center.y);
            public bool Contains(Vector3 p, float margin = 0f) =>
                p.x >= Area.xMin - margin && p.x <= Area.xMax + margin && p.z >= Area.yMin - margin && p.z <= Area.yMax + margin;
        }

        // ---- Straßenraster -----------------------------------------------------------------------------
        public const float MainStreetZ = 0f;
        /// <summary>Halbe Fahrbahnbreite der Hauptstraße.</summary>
        public const float MainHalfWidth = 4f;
        /// <summary>Gehwege der Hauptstraße: |z| von 4 bis 7.</summary>
        public const float MainSidewalkOuter = 7f;
        /// <summary>Fahrspuren der Hauptstraße: z = +2 Richtung +X (Osten), z = −2 Richtung −X (Westen).</summary>
        public const float LaneEastZ = 2f, LaneWestZ = -2f;

        /// <summary>Spielbarer Bereich (unsichtbare Grenzen knapp außerhalb).</summary>
        public const float CityMinX = -150f, CityMaxX = 150f, CityMinZ = -50f, CityMaxZ = 83f;

        /// <summary>Querstraßen (Nord-Süd), Mittellinie x. Fahrbahn ±<see cref="SideHalfWidth"/>, Gehwege je 2,5 m.</summary>
        public static readonly float[] SideStreetsX = { -135f, -90f, 52f, 125f };
        /// <summary>Querstraßen, die auch nach Süden führen (bis <see cref="SideSouthEndZ"/>).</summary>
        public static readonly float[] SideStreetsSouthX = { -135f, 52f, 125f };
        public const float SideHalfWidth = 4f;
        public const float SideSidewalk = 2.5f;
        public const float SideSouthEndZ = -48f;

        /// <summary>Wohnstraße im Norden (hinter dem Park), entlang X bei z = 53, Fahrbahn z 48..58 mit Parkstreifen.</summary>
        public const float ResidentialZ = 53f;
        public const float ResidentialHalfWidth = 5f;
        public const float ResidentialFromX = -139f, ResidentialToX = 129f;
        /// <summary>Gehwege der Wohnstraße: z 45,5..48 (Süd) und 58..60,5 (Nord).</summary>
        public const float ResidentialSidewalkSouthZ = 46.75f, ResidentialSidewalkNorthZ = 59.25f;

        /// <summary>Zebrastreifen über die Hauptstraße (Mitte x).</summary>
        public static readonly float[] CrosswalksX = { -144f, -98f, -35f, 0f, 45f, 80f, 118f };

        // ---- Reservierte Grundstücke -------------------------------------------------------------------
        /// <summary>Finanzviertel (Bank + Börse), Nordseite, Front nach Süden zur Hauptstraße.</summary>
        public static readonly Lot Finanzviertel = new Lot
        {
            Id = "finanzviertel", Name = "Finanzviertel",
            Area = new Rect(60f, 8f, 50f, 37f),
            Entrance = new Vector3(85f, 0f, 8.5f), FacingRotY = 180f, StreetAccess = new Vector3(85f, 0f, 5.5f),
            Parts = new[]
            {
                new Lot { Id = "bank", Name = "Bank", Area = new Rect(60f, 8f, 25f, 37f), Entrance = new Vector3(72.5f, 0f, 8.5f), FacingRotY = 180f, StreetAccess = new Vector3(72.5f, 0f, 5.5f) },
                new Lot { Id = "boerse", Name = "Börse", Area = new Rect(85f, 8f, 25f, 37f), Entrance = new Vector3(97.5f, 0f, 8.5f), FacingRotY = 180f, StreetAccess = new Vector3(97.5f, 0f, 5.5f) },
            },
        };

        /// <summary>Einkaufsviertel (Elektromarkt + Tierladen), Südseite, Front nach Norden zur Hauptstraße.</summary>
        public static readonly Lot Einkaufsviertel = new Lot
        {
            Id = "einkaufsviertel", Name = "Einkaufsviertel",
            Area = new Rect(-125f, -45f, 65f, 37f),
            Entrance = new Vector3(-92.5f, 0f, -8.5f), FacingRotY = 0f, StreetAccess = new Vector3(-92.5f, 0f, -5.5f),
            Parts = new[]
            {
                new Lot { Id = "elektromarkt", Name = "Elektromarkt", Area = new Rect(-92.5f, -45f, 32.5f, 37f), Entrance = new Vector3(-76.25f, 0f, -8.5f), FacingRotY = 0f, StreetAccess = new Vector3(-76.25f, 0f, -5.5f) },
                new Lot { Id = "tierladen", Name = "Tierladen", Area = new Rect(-125f, -45f, 32.5f, 37f), Entrance = new Vector3(-108.75f, 0f, -8.5f), FacingRotY = 0f, StreetAccess = new Vector3(-108.75f, 0f, -5.5f) },
            },
        };

        // ---- Eigene Grundstücke der Stadt ----------------------------------------------------------------
        /// <summary>PaketBlitz-Filiale, Südseite.</summary>
        public static readonly Lot PostOffice = new Lot
        {
            Id = "post", Name = "PaketBlitz-Filiale",
            Area = new Rect(60f, -30f, 35f, 22f),
            Entrance = new Vector3(77.5f, 0f, -8.5f), FacingRotY = 0f, StreetAccess = new Vector3(77.5f, 0f, -5.5f),
        };

        /// <summary>Wohnviertel nördlich des Parks (ohne das Finanzviertel).</summary>
        public static readonly Rect Residential = new Rect(-150f, 30f, 300f, 50f);

        /// <summary>Alle Grundstücke, auf denen die Stadt nichts bauen darf.</summary>
        public static readonly Lot[] Reserved = { Finanzviertel, Einkaufsviertel };

        /// <summary>Bestehende Gebäude der Kernstadt (Imbiss, Garage, Halle) als Sperrflächen.</summary>
        public static readonly Rect[] CoreBuildings =
        {
            new Rect(-46f, -21f, 20f, 14f), // Kalles Imbiss + Rand
            new Rect(-30f, -24f, 22f, 17f), // Hof, Garage
            new Rect(-4f, -34f, 40f, 27f),  // Lagerhalle
            new Rect(-57f, 7.5f, 106f, 22.5f), // Park
        };

        /// <summary>Liegt ein Punkt in einem reservierten Grundstück?</summary>
        public static bool IsReserved(Vector3 p, float margin = 0f)
        {
            foreach (var l in Reserved)
                if (l.Contains(p, margin)) return true;
            return false;
        }

        /// <summary>Überschneidet ein Rechteck (XZ) ein reserviertes Grundstück oder ein Kerngebäude?</summary>
        public static bool Blocked(Rect r, float margin = 0.5f)
        {
            foreach (var l in Reserved)
                if (Overlaps(r, l.Area, margin)) return true;
            foreach (var c in CoreBuildings)
                if (Overlaps(r, c, margin)) return true;
            if (Overlaps(r, PostOffice.Area, margin)) return true;
            return false;
        }

        public static bool Overlaps(Rect a, Rect b, float margin = 0f) =>
            a.xMin < b.xMax + margin && a.xMax > b.xMin - margin && a.yMin < b.yMax + margin && a.yMax > b.yMin - margin;

        /// <summary>Liegt x auf einer Querstraße (inkl. Gehwege und Rand)?</summary>
        public static bool OnSideStreet(float x, float extra = 0.5f, bool south = false)
        {
            var list = south ? SideStreetsSouthX : SideStreetsX;
            foreach (float sx in list)
                if (Mathf.Abs(x - sx) < SideHalfWidth + SideSidewalk + extra) return true;
            return false;
        }

        // ---- Straßenanschlüsse (für Wege, Lieferungen, Spawns) ----------------------------------------
        /// <summary>Punkte auf den Gehwegen, an denen man von der Hauptstraße in die Querstraßen abbiegt.</summary>
        public static IEnumerable<Vector3> StreetAccessPoints()
        {
            foreach (float sx in SideStreetsX)
            {
                yield return new Vector3(sx - SideHalfWidth - SideSidewalk * 0.5f, 0f, 5.5f);
                yield return new Vector3(sx + SideHalfWidth + SideSidewalk * 0.5f, 0f, 5.5f);
            }
            foreach (float sx in SideStreetsSouthX)
            {
                yield return new Vector3(sx - SideHalfWidth - SideSidewalk * 0.5f, 0f, -5.5f);
                yield return new Vector3(sx + SideHalfWidth + SideSidewalk * 0.5f, 0f, -5.5f);
            }
        }

        /// <summary>Nächster Gehweg-Punkt an der Hauptstraße (Nord- oder Südseite je nach z).</summary>
        public static Vector3 NearestMainSidewalk(Vector3 p)
        {
            float x = Mathf.Clamp(p.x, CityMinX, CityMaxX);
            return new Vector3(x, 0f, p.z >= 0f ? 5.5f : -5.5f);
        }

        /// <summary>Haltepunkt am Straßenrand für Fahrzeuge vor einem Punkt (Fahrspur der passenden Seite).</summary>
        public static Vector3 CurbStop(Vector3 p) => new Vector3(Mathf.Clamp(p.x, CityMinX, CityMaxX), 0f, p.z >= 0f ? LaneEastZ : LaneWestZ);
    }
}
