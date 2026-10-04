using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Ein Ort, den Passanten besuchen (Schaufenster, Packstation, Hauseingang ...).</summary>
    public struct CityPoi
    {
        /// <summary>Gehweg-Zone (siehe <see cref="CityLayout"/>-Konstanten).</summary>
        public int Zone;
        /// <summary>Standpunkt auf dem Gehweg (Welt).</summary>
        public Vector3 Pos;
        /// <summary>Punkt, den man dabei anschaut (Welt).</summary>
        public Vector3 Look;
        public float MinPause, MaxPause;
    }

    /// <summary>
    /// Laufzeit-Daten der gebauten Stadt (für Passanten, Müllabfuhr, Verkehr). Wird bei jedem
    /// Weltaufbau von <see cref="CityBuilder"/> neu befüllt.
    /// </summary>
    public static class CityLayout
    {
        // Gehweg-Zonen der Passanten
        public const int ZoneMainSouth = 0, ZoneMainNorth = 1, ZonePark = 2, ZoneResSouth = 3, ZoneResNorth = 4;

        /// <summary>Gehweg-Bänder (zMin, zMax) je Zone, x-Bereich je Zone.</summary>
        public static readonly float[] LaneMinZ = { -6.45f, 4.55f, 11.75f, 45.8f, 58.3f };
        public static readonly float[] LaneMaxZ = { -4.55f, 6.45f, 13.25f, 47.7f, 60.2f };
        public static readonly float[] EdgeWestX = { -151f, -151f, -56f, -141f, -141f };
        public static readonly float[] EdgeEastX = { 151f, 151f, 47f, 131f, 131f };

        /// <summary>Schaufenster und andere Ziele der Stadt (zusätzlich zu den festen Zielen in StreetLife).</summary>
        public static readonly List<CityPoi> Pois = new List<CityPoi>();
        /// <summary>Namen der gebauten Läden (für Sprüche, Debug).</summary>
        public static readonly List<string> ShopNames = new List<string>();
        /// <summary>true, sobald die große Stadt gebaut wurde (sonst alte Kulisse).</summary>
        public static bool Built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Pois.Clear();
            ShopNames.Clear();
            Built = false;
        }

        public static void AddPoi(int zone, Vector3 pos, Vector3 look, float minPause = 2.5f, float maxPause = 7f)
        {
            Pois.Add(new CityPoi { Zone = zone, Pos = pos, Look = look, MinPause = minPause, MaxPause = maxPause });
        }
    }

    /// <summary>
    /// Baut die große Stadt um die Kernstraße herum: verlängerte Hauptstraße (x −150..150) mit Querstraßen,
    /// Zebrastreifen und Laternen, zufällig (pro Spielstand stabil) angeordnete Läden und Wohnhäuser
    /// mit lustigen Ladennamen, ein Wohnviertel hinter dem Park, geparkte Autos, Mülleimer, die
    /// PaketBlitz-Filiale, Skyline und Kartengrenzen. Reservierte Grundstücke (<see cref="WorldLots.Reserved"/>)
    /// bleiben frei. Modelle über AssetLib ("building.*", sonst "city.building_*"), sonst prozedural.
    /// </summary>
    public static class CityBuilder
    {
        private static readonly string[] FunnyShops =
        {
            "Haarmonie", "Hair Force One", "Kamm in", "Bäckerei Krümel", "Brotzeit", "Kaffeeklatsch", "Bohne & Söhne",
            "Döner Ehrlich", "Wurstcase", "Pizza Mamma Mia", "Wok'n'Roll", "Eiszeit", "Kiosk 24/7½", "Späti Spätzünder",
            "Blumen Rosenkavalier", "Optik Durchblick", "Copyshop Doppelt", "Waschsalon Schleudertrauma", "Drogerie Schaumschläger",
            "Buchladen Seitenwechsel", "Getränke Durstlöscher", "Speiche & Co.", "Handy-Doktor", "Muckibude", "Nagelneu",
            "Stichfest Tattoo", "Reisebüro Weitweg", "Schuh-Bidu", "Zweite Chance", "Pfandastisch", "Hustle Hub Coworking",
            "Krypto-Café Mondflug", "Escape Room Raus hier", "Bio Grünzeug", "Metzgerei Wurstbrot", "Schlüsseldienst Aufsperr",
            "Fahrschule Vollgas", "Yoga Om-Nom", "Karaoke Schiefton", "Sushi Fischers Fritz", "Nähstube Fadenschein",
            "Gemüse Rübezahl", "Senf dazu", "Uhren Tick Tack", "Kostüme Verkleidet", "Fotostudio Cheese", "Pommesbude Frittiererei",
            "Socken-Paradies", "Dönerwetter", "Schreibwaren Klecks",
        };

        private static readonly Color[] WallColors =
        {
            new Color(0.86f, 0.78f, 0.62f), new Color(0.78f, 0.55f, 0.45f), new Color(0.62f, 0.7f, 0.78f), new Color(0.84f, 0.84f, 0.8f),
            new Color(0.7f, 0.78f, 0.6f), new Color(0.9f, 0.72f, 0.5f), new Color(0.72f, 0.62f, 0.75f), new Color(0.95f, 0.88f, 0.7f),
            new Color(0.6f, 0.6f, 0.62f), new Color(0.85f, 0.6f, 0.58f),
        };

        private static readonly Color[] SignColors =
        {
            new Color(0.85f, 0.18f, 0.2f), new Color(0.15f, 0.35f, 0.7f), new Color(0.12f, 0.55f, 0.35f), new Color(0.95f, 0.75f, 0.1f),
            new Color(0.55f, 0.25f, 0.7f), new Color(0.1f, 0.1f, 0.12f), new Color(0.95f, 0.45f, 0.15f), new Color(0.1f, 0.6f, 0.7f),
        };

        private static readonly Color[] RoofColors =
        {
            new Color(0.62f, 0.25f, 0.18f), new Color(0.35f, 0.33f, 0.33f), new Color(0.45f, 0.28f, 0.2f), new Color(0.25f, 0.3f, 0.38f),
        };

        private static readonly Color[] CarColors =
        {
            new Color(0.8f, 0.1f, 0.1f), new Color(0.15f, 0.3f, 0.6f), new Color(0.9f, 0.9f, 0.9f), new Color(0.1f, 0.1f, 0.12f),
            new Color(0.55f, 0.57f, 0.6f), new Color(0.9f, 0.7f, 0.1f), new Color(0.2f, 0.5f, 0.3f), new Color(0.5f, 0.2f, 0.45f),
        };

        public static readonly string[] ParkedCarIds =
        {
            "vehicle.traffic_1", "vehicle.traffic_2", "vehicle.traffic_3", "vehicle.traffic_4", "vehicle.traffic_5", "vehicle.car_sedan",
            "vehicle.car_hatchback", "vehicle.car_stationwagon", "vehicle.sedan", "vehicle.suv", "vehicle.hatchback_sports", "vehicle.kombi",
        };

        private static Transform _s, _dyn, _signs, _world;
        private static System.Random _rng;
        private static CityLights _lights;
        private static WorldBuilder _wb;

        private static float R(float a, float b) => a + (float)_rng.NextDouble() * (b - a);
        private static bool Chance(float p) => _rng.NextDouble() < p;
        private static T Pick<T>(T[] arr) => arr[_rng.Next(arr.Length)];

        /// <summary>
        /// Baut die Stadt. Gibt das Penthouse-Licht zurück (Lifestyle "penthouse", anfangs inaktiv) oder null.
        /// Jeder Abschnitt ist einzeln abgesichert: ein Fehler lässt den Rest der Welt stehen.
        /// </summary>
        public static GameObject Build(WorldBuilder world, Transform staticRoot, bool menuMode)
        {
            _wb = world;
            _world = world.transform;
            _s = staticRoot;
            _dyn = Props.Node(_world, "City").transform;
            _signs = Props.Node(_dyn, "Signs").transform;
            int seed = 4242;
            if (!menuMode && Game.Sim != null) seed = Game.Sim.CitySeed;
            _rng = new System.Random(seed);
            CityLayout.Pois.Clear();
            CityLayout.ShopNames.Clear();
            CityServices.ClearBins();
            _lights = world.gameObject.AddComponent<CityLights>();
            GameObject penthouse = null;
            Safe("Grenzen", Bounds);
            Safe("Straßen", Streets);
            Safe("Laternen", Lamps);
            Safe("Post", PostOffice);
            Safe("Häuser", Buildings);
            Safe("Parkplätze", ParkedCars);
            Safe("Grün", Greenery);
            Safe("Mülleimer", Bins);
            Safe("Skyline", () => penthouse = Skyline());
            CityLayout.Built = true;
            _shopPool = null;
            return penthouse;
        }

        private static void Safe(string what, System.Action a)
        {
            try { a(); }
            catch (System.Exception e) { Debug.LogWarning("[CityBuilder] " + what + ": " + e); }
        }

        // =====================================================================================
        // Grenzen
        // =====================================================================================
        private static void Bounds()
        {
            float cz = (WorldLots.CityMinZ + WorldLots.CityMaxZ) * 0.5f;
            float lz = WorldLots.CityMaxZ - WorldLots.CityMinZ + 8f;
            Props.Collider(_world, new Vector3(1f, 8f, lz), new Vector3(WorldLots.CityMinX - 2.5f, 4f, cz));
            Props.Collider(_world, new Vector3(1f, 8f, lz), new Vector3(WorldLots.CityMaxX + 2.5f, 4f, cz));
            Props.Collider(_world, new Vector3(312f, 8f, 1f), new Vector3(0f, 4f, WorldLots.CityMinZ - 2f));
            Props.Collider(_world, new Vector3(312f, 8f, 1f), new Vector3(0f, 4f, WorldLots.CityMaxZ + 2f));
            // Absperrungen an den Straßenenden
            foreach (float bx in new[] { WorldLots.CityMinX - 1.2f, WorldLots.CityMaxX + 1.2f })
            foreach (float bz in new[] { -5.5f, -2f, 2f, 5.5f })
                Props.Barrier(_s).transform.localPosition = new Vector3(bx, 0, bz);
            foreach (var b in _s.GetComponentsInChildren<Transform>())
                if (b.name == "Barrier") b.localRotation = Quaternion.Euler(0, 90, 0);
            foreach (float bx in new[] { WorldLots.ResidentialFromX - 0.6f, WorldLots.ResidentialToX + 0.6f })
            foreach (float bz in new[] { 50f, 53f, 56f })
            {
                var b = Props.Barrier(_s);
                b.transform.localPosition = new Vector3(bx, 0, bz);
                b.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
            foreach (float sx in WorldLots.SideStreetsSouthX)
            foreach (float bx in new[] { -2.4f, 0f, 2.4f })
                Props.Barrier(_s).transform.localPosition = new Vector3(sx + bx, 0, WorldLots.SideSouthEndZ - 0.4f);
            // Zäune ganz außen
            var fs = Props.Fence(_s, 300f);
            fs.transform.localPosition = new Vector3(0, 0, WorldLots.CityMinZ - 1.5f);
            var fn = Props.Fence(_s, 300f);
            fn.transform.localPosition = new Vector3(0, 0, WorldLots.CityMaxZ + 1.5f);
            // Umleitungsschilder
            foreach (float side in new[] { -1f, 1f })
            {
                var sign = Props.SignBoard(_signs, "ENDE GELÄNDE\nUmleitung", new Color(0.95f, 0.8f, 0.1f), new Color(0.1f, 0.1f, 0.1f), new Vector2(2.6f, 1f));
                sign.transform.localPosition = new Vector3(side * (WorldLots.CityMaxX + 0.6f), 1.8f, 0f);
                sign.transform.localRotation = Quaternion.Euler(0, side > 0 ? -90f : 90f, 0);
            }
        }

        // =====================================================================================
        // Straßen
        // =====================================================================================
        private static Material _walk, _curb, _white, _asphalt;

        private static void Streets()
        {
            _asphalt = Mats.Asphalt();
            _walk = Mats.Sidewalk(new Color(0.66f, 0.65f, 0.63f), new Color(0.56f, 0.55f, 0.53f));
            _curb = Mats.Std(new Color(0.7f, 0.7f, 0.68f), 0.8f);
            _white = Mats.Std(new Color(0.92f, 0.92f, 0.9f), 0.6f);
            // Mittelstreifen der Hauptstraße über die alte Länge hinaus
            for (float x = -160f; x < -101f; x += 6f) Props.Box(_s, new Vector3(3f, 0.01f, 0.14f), _white, new Vector3(x, 0.017f, 0), default, 0f, false);
            for (float x = 104f; x <= 160f; x += 6f) Props.Box(_s, new Vector3(3f, 0.01f, 0.14f), _white, new Vector3(x, 0.017f, 0), default, 0f, false);
            // Zebrastreifen (der bei x = −35 existiert schon)
            foreach (float cx in WorldLots.CrosswalksX)
            {
                if (Mathf.Abs(cx + 35f) < 1f) continue;
                for (int i = 0; i < 7; i++)
                {
                    Props.Box(_s, new Vector3(0.5f, 0.012f, 3.6f), _white, new Vector3(cx - 3f + i, 0.018f, -1.9f), default, 0f, false);
                    Props.Box(_s, new Vector3(0.5f, 0.012f, 3.6f), _white, new Vector3(cx - 3f + i, 0.018f, 1.9f), default, 0f, false);
                }
                // Zebrastreifen-Schild auf beiden Seiten
                foreach (float sz in new[] { -6.9f, 6.9f })
                {
                    var pole = Props.Node(_s, "CrossSign", new Vector3(cx + 3.4f, 0, sz));
                    Props.Cyl(pole.transform, 0.05f, 0.05f, 2.6f, Mats.DarkMetal(), new Vector3(0, 1.3f, 0), default, 8);
                    Props.Box(pole.transform, new Vector3(0.6f, 0.6f, 0.04f), Mats.Std(new Color(0.15f, 0.35f, 0.75f), 0.5f), new Vector3(0, 2.45f, 0), default, 0.01f);
                    Props.Box(pole.transform, new Vector3(0.36f, 0.36f, 0.05f), Mats.Std(Color.white, 0.5f), new Vector3(0, 2.45f, 0), new Vector3(0, 0, 45f), 0f, false);
                    NpcNav.AddPoint(_world.TransformPoint(new Vector3(cx + 3.4f, 0, sz)));
                }
            }
            // Querstraßen
            foreach (float sx in WorldLots.SideStreetsX) SideStreet(sx, 7f, WorldLots.ResidentialZ - WorldLots.ResidentialHalfWidth, true);
            foreach (float sx in WorldLots.SideStreetsSouthX) SideStreet(sx, WorldLots.SideSouthEndZ, -7f, false);
            // Ampeln an den Kreuzungen
            foreach (float sx in WorldLots.SideStreetsX)
            {
                Props.AssetAt(_s, "street.trafficlight_a", new Vector3(sx - 4.6f, 0, 6.95f), 180f, 3.4f);
                NpcNav.AddPoint(_world.TransformPoint(new Vector3(sx - 4.6f, 0, 6.95f)));
            }
            foreach (float sx in WorldLots.SideStreetsSouthX)
            {
                Props.AssetAt(_s, "street.trafficlight_a", new Vector3(sx + 4.6f, 0, -6.95f), 0f, 3.4f);
                NpcNav.AddPoint(_world.TransformPoint(new Vector3(sx + 4.6f, 0, -6.95f)));
            }
            Residential();
        }

        private static void SideStreet(float sx, float z0, float z1, bool north)
        {
            float len = z1 - z0, cz = (z0 + z1) * 0.5f;
            float hw = WorldLots.SideHalfWidth, sw = WorldLots.SideSidewalk;
            Props.Box(_s, new Vector3(hw * 2f, 0.02f, len), _asphalt, new Vector3(sx, 0.012f, cz), default, 0f);
            float walkLen = north ? len - 2.5f : len;
            float walkCz = north ? z0 + walkLen * 0.5f : cz;
            foreach (float side in new[] { -1f, 1f })
            {
                Props.Box(_s, new Vector3(sw, 0.03f, walkLen), _walk, new Vector3(sx + side * (hw + sw * 0.5f), 0.014f, walkCz), default, 0f);
                Props.Box(_s, new Vector3(0.18f, 0.1f, len), _curb, new Vector3(sx + side * hw, 0.05f, cz));
            }
            for (float z = z0 + 3f; z < z1 - 2f; z += 6f)
                Props.Box(_s, new Vector3(0.14f, 0.01f, 3f), _white, new Vector3(sx, 0.024f, z), default, 0f, false);
            // Haltelinie an der Einmündung
            float stopZ = north ? z0 + 0.6f : z1 - 0.6f;
            Props.Box(_s, new Vector3(hw, 0.01f, 0.3f), _white, new Vector3(sx + (north ? hw * 0.5f : -hw * 0.5f), 0.024f, stopZ), default, 0f, false);
            // Straßenschild
            string[] names = { "Paketweg", "Hustlegasse", "Retourenring", "Kartonallee", "Lieferantenstr.", "Expressweg" };
            int idx = Mathf.Abs(Mathf.RoundToInt(sx * 7f + (north ? 1f : 3f))) % names.Length;
            var pole = Props.Node(_s, "StreetSign", new Vector3(sx + hw + sw + 0.3f, 0, north ? z0 + 1.2f : z1 - 1.2f));
            Props.Cyl(pole.transform, 0.05f, 0.05f, 2.8f, Mats.DarkMetal(), new Vector3(0, 1.4f, 0), default, 8);
            var sign = Props.SignBoard(_signs, names[idx], new Color(0.95f, 0.95f, 0.92f), new Color(0.1f, 0.1f, 0.1f), new Vector2(1.6f, 0.32f));
            sign.transform.localPosition = pole.transform.localPosition + new Vector3(0, 2.7f, 0);
            sign.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            NpcNav.AddPoint(_world.TransformPoint(pole.transform.localPosition));
        }

        private static void Residential()
        {
            float x0 = WorldLots.ResidentialFromX, x1 = WorldLots.ResidentialToX;
            float cx = (x0 + x1) * 0.5f, len = x1 - x0;
            float z = WorldLots.ResidentialZ, hw = WorldLots.ResidentialHalfWidth;
            Props.Box(_s, new Vector3(len, 0.02f, hw * 2f), _asphalt, new Vector3(cx, 0.012f, z), default, 0f);
            float wlen = len + 5f;
            Props.Box(_s, new Vector3(wlen, 0.03f, 2.5f), _walk, new Vector3(cx, 0.016f, z - hw - 1.25f), default, 0f);
            Props.Box(_s, new Vector3(wlen, 0.03f, 2.5f), _walk, new Vector3(cx, 0.016f, z + hw + 1.25f), default, 0f);
            Props.Box(_s, new Vector3(len, 0.1f, 0.18f), _curb, new Vector3(cx, 0.05f, z - hw));
            Props.Box(_s, new Vector3(len, 0.1f, 0.18f), _curb, new Vector3(cx, 0.05f, z + hw));
            // Parkstreifen-Linien und Mittelstreifen
            Props.Box(_s, new Vector3(len, 0.01f, 0.1f), _white, new Vector3(cx, 0.024f, z - 3f), default, 0f, false);
            Props.Box(_s, new Vector3(len, 0.01f, 0.1f), _white, new Vector3(cx, 0.024f, z + 3f), default, 0f, false);
            for (float x = x0 + 3f; x < x1 - 2f; x += 6f)
                Props.Box(_s, new Vector3(3f, 0.01f, 0.12f), _white, new Vector3(x, 0.024f, z), default, 0f, false);
            var sign = Props.SignBoard(_signs, "Lindenweg · Wohngebiet\nSchritttempo", new Color(0.15f, 0.35f, 0.7f), Color.white, new Vector2(2.4f, 0.8f));
            sign.transform.localPosition = new Vector3(-4f, 2.4f, z - hw - 2.3f);
            sign.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            var pole = Props.Node(_s, "Pole", new Vector3(-4f, 0, z - hw - 2.35f));
            Props.Cyl(pole.transform, 0.05f, 0.05f, 2.1f, Mats.DarkMetal(), new Vector3(0, 1.05f, 0), default, 8);
            NpcNav.AddPoint(_world.TransformPoint(pole.transform.localPosition));
        }

        // =====================================================================================
        // Laternen (Licht nur nachts und nur in der Nähe, siehe CityLights)
        // =====================================================================================
        private static void Lamps()
        {
            // Hauptstraße außerhalb der alten Kernstraße
            for (float x = -144f; x <= 146f; x += 16f)
            {
                if (x > -56f && x < 50f) continue;
                if (!WorldLots.OnSideStreet(x, 1f)) Lamp(new Vector3(x, 0, 6.8f), 180f);
                if (!WorldLots.OnSideStreet(x + 8f, 1f, true) && !(x + 8f > -50f && x + 8f < 40f)) Lamp(new Vector3(x + 8f, 0, -6.85f), 0f);
            }
            // Wohnstraße
            for (float x = WorldLots.ResidentialFromX + 6f; x < WorldLots.ResidentialToX - 3f; x += 18f)
            {
                if (!WorldLots.OnSideStreet(x, 1f)) Lamp(new Vector3(x, 0, WorldLots.ResidentialZ - WorldLots.ResidentialHalfWidth - 0.45f), 0f);
                Lamp(new Vector3(x + 9f, 0, WorldLots.ResidentialZ + WorldLots.ResidentialHalfWidth + 0.45f), 180f);
            }
            // Querstraßen
            foreach (float sx in WorldLots.SideStreetsX)
                for (float z = 15f; z < 44f; z += 14f) Lamp(new Vector3(sx + WorldLots.SideHalfWidth + 0.45f, 0, z), -90f);
            foreach (float sx in WorldLots.SideStreetsSouthX)
                for (float z = -15f; z > -46f; z -= 14f) Lamp(new Vector3(sx - WorldLots.SideHalfWidth - 0.45f, 0, z), 90f);
        }

        private static void Lamp(Vector3 pos, float rotY)
        {
            var lp = Props.LampPost(_s, out var light);
            lp.transform.localPosition = pos;
            lp.transform.localRotation = Quaternion.Euler(0, rotY, 0);
            NpcNav.AddPoint(_world.TransformPoint(pos));
            CityServices.LampPoints.Add(_world.TransformPoint(pos));
            if (light != null && _lights != null) _lights.Add(light);
        }

        // =====================================================================================
        // Häuser und Läden
        // =====================================================================================
        private static List<string> _shopPool;

        private static string NextShopName()
        {
            if (_shopPool == null || _shopPool.Count == 0)
            {
                _shopPool = new List<string>(FunnyShops);
                // mischen (Fisher-Yates)
                for (int i = _shopPool.Count - 1; i > 0; i--)
                {
                    int j = _rng.Next(i + 1);
                    var t = _shopPool[i];
                    _shopPool[i] = _shopPool[j];
                    _shopPool[j] = t;
                }
            }
            string n = _shopPool[_shopPool.Count - 1];
            _shopPool.RemoveAt(_shopPool.Count - 1);
            CityLayout.ShopNames.Add(n);
            return n;
        }

        private static void Buildings()
        {
            // Hauptstraße Nord (Front bei z = 9, Blick nach Süden)
            foreach (var r in new[] { new Vector2(-150f, -142f), new Vector2(-128f, -97f), new Vector2(-83f, -58.5f), new Vector2(112f, 118f), new Vector2(132f, 150f) })
                Row(r.x, r.y, 9f, 180f, 10f, 15f, true, CityLayout.ZoneMainNorth);
            // Hauptstraße Süd (Front bei z = −9, Blick nach Norden)
            foreach (var r in new[] { new Vector2(-150f, -142f), new Vector2(-59.5f, -46.5f), new Vector2(36.5f, 45.5f), new Vector2(97f, 118f), new Vector2(132f, 150f) })
                Row(r.x, r.y, -9f, 0f, 10f, 15f, true, CityLayout.ZoneMainSouth);
            // Wohnviertel: Südreihe (Front bei z = 44,5, Blick nach Norden), Nordreihe (Front bei z = 61,5, Blick nach Süden)
            foreach (var r in new[] { new Vector2(-128f, -97f), new Vector2(-83f, 45f), new Vector2(112f, 118f) })
                Row(r.x, r.y, 44.5f, 0f, 9f, 13f, false, CityLayout.ZoneResSouth);
            foreach (var r in new[] { new Vector2(-138f, -26f), new Vector2(-14f, 128f) })
                Row(r.x, r.y, 61.5f, 180f, 9f, 14f, false, CityLayout.ZoneResNorth);
            // Quartiersplatz mit Bank und Baum in der Lücke der Nordreihe (Packstation steht dort)
            var bench = Props.Bench(_s);
            bench.transform.localPosition = new Vector3(-17.5f, 0, 63f);
            bench.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            Props.Collider(_world, new Vector3(1.8f, 0.9f, 0.5f), new Vector3(-17.5f, 0.45f, 63f));
            var tree = Props.Tree(_s, 1.2f, 2);
            tree.transform.localPosition = new Vector3(-22f, 0, 65f);
            Props.Collider(_world, new Vector3(0.4f, 3f, 0.4f), new Vector3(-22f, 1.5f, 65f));
        }

        /// <summary>Häuserzeile entlang X zwischen xFrom und xTo (Front bei frontZ, Front nach außen per rotY).</summary>
        private static void Row(float xFrom, float xTo, float frontZ, float rotY, float dMin, float dMax, bool commercial, int zone)
        {
            float x = xFrom;
            int guard = 0;
            while (x < xTo - 5.5f && guard++ < 60)
            {
                float w = commercial ? R(8f, 13f) : R(8f, 12f);
                float rest = xTo - x;
                if (rest - w < 6f) w = rest;
                if (w > 16f) w = R(8f, 11f);
                float depth = R(dMin, dMax);
                float cx = x + w * 0.5f;
                bool towardPlusZ = rotY > 90f; // Haus erstreckt sich nach +Z
                float zMin = towardPlusZ ? frontZ : frontZ - depth;
                var rect = new Rect(x, zMin, w, depth);
                if (!WorldLots.Blocked(rect, 0.3f) && !WorldLots.OnSideStreet(cx, 0f) && !WorldLots.OnSideStreet(x + 0.3f, 0f) && !WorldLots.OnSideStreet(x + w - 0.3f, 0f))
                {
                    try { Building(cx, w - 0.4f, frontZ, rotY, depth, commercial, zone); }
                    catch (System.Exception e) { Debug.LogWarning("[CityBuilder] Haus: " + e.Message); }
                }
                x += w + (Chance(0.35f) ? R(0.6f, 2.2f) : 0.1f);
            }
        }

        private static void Building(float cx, float w, float frontZ, float rotY, float depth, bool commercial, int zone)
        {
            var node = Props.Node(_s, commercial ? "Shop" : "House", new Vector3(cx, 0, frontZ), rotY);
            var t = node.transform;
            bool shop = commercial ? Chance(0.8f) : Chance(0.08f);
            float height;
            if (!TryModelBuilding(t, w, depth, commercial, out height))
            {
                if (commercial) height = ShopHouse(t, w, depth, shop);
                else if (Chance(0.22f)) height = Apartment(t, w, depth);
                else height = FamilyHouse(t, w, depth);
            }
            // Ladenschild auch an Modellhäusern
            string name = shop ? NextShopName() : null;
            if (name != null) ShopFront(t, w, name, height);
            else if (!commercial) Doorstep(t, w);
            // Kollision über den ganzen Block (Hausmodelle haben keine eigene)
            Props.Collider(_world, new Vector3(Mathf.Abs(Mathf.Sin(rotY * Mathf.Deg2Rad)) > 0.5f ? depth : w, 6f, Mathf.Abs(Mathf.Sin(rotY * Mathf.Deg2Rad)) > 0.5f ? w : depth),
                t.TransformPoint(new Vector3(0, 3f, -depth * 0.5f)) - _world.position);
            // Ziel für Passanten: vor dem Haus auf dem Gehweg
            float laneZ = (CityLayout.LaneMinZ[zone] + CityLayout.LaneMaxZ[zone]) * 0.5f;
            float near = rotY > 90f ? CityLayout.LaneMaxZ[zone] - 0.2f : CityLayout.LaneMinZ[zone] + 0.2f;
            if (zone == CityLayout.ZoneMainSouth || zone == CityLayout.ZoneResSouth) near = rotY > 90f ? laneZ : CityLayout.LaneMinZ[zone] + 0.2f;
            if (zone == CityLayout.ZoneResSouth) near = CityLayout.LaneMinZ[zone] + 0.2f;
            if (zone == CityLayout.ZoneMainNorth || zone == CityLayout.ZoneResNorth) near = CityLayout.LaneMaxZ[zone] - 0.2f;
            if (zone == CityLayout.ZoneMainSouth) near = CityLayout.LaneMinZ[zone] + 0.2f;
            if (shop || Chance(0.3f))
                CityLayout.AddPoi(zone, _world.TransformPoint(new Vector3(cx + R(-w * 0.25f, w * 0.25f), 0, near)), _world.TransformPoint(new Vector3(cx, 1.5f, frontZ)),
                    shop ? 2.5f : 1.5f, shop ? 7f : 4f);
        }

        /// <summary>Modellhaus aus AssetLib ("building.*", sonst ab und zu "city.building_a..h").</summary>
        private static bool TryModelBuilding(Transform t, float w, float depth, bool commercial, out float height)
        {
            height = 0f;
            string id = null;
            var custom = AssetLib.Ids("building");
            if (custom != null && custom.Count > 0 && Chance(0.6f)) id = custom[_rng.Next(custom.Count)];
            else if (Chance(commercial ? 0.25f : 0.15f))
            {
                string[] kay = { "city.building_a", "city.building_b", "city.building_c", "city.building_d", "city.building_e", "city.building_f", "city.building_g", "city.building_h" };
                id = Pick(kay);
            }
            if (id == null || !AssetLib.HasModel(id)) return false;
            var info = AssetLib.Info(id);
            var mb = AssetLib.ModelBounds(id);
            if (info == null || mb.size.x < 0.5f || mb.size.z < 0.5f) return false;
            // So skalieren, dass Breite und Tiefe aufs Grundstück passen
            float k = Mathf.Min(w / mb.size.x, depth / mb.size.z);
            float fit = info.Meters * k;
            var m = Props.AssetAt(t, id, new Vector3(0, 0, -mb.size.z * k * 0.5f), 0f, fit);
            if (m == null) return false;
            height = Mathf.Max(3.5f, mb.size.y * k);
            return true;
        }

        private static Material FacadeMat(Color wall, float lit = 0.4f) => Mats.Facade(wall, _rng.Next(1, 6), lit);

        /// <summary>Geschäftshaus: Erdgeschoss als Laden (Putz/Klinker), darüber 1–4 Etagen mit Fenstern.</summary>
        private static float ShopHouse(Transform t, float w, float d, bool shop)
        {
            int floors = _rng.Next(1, 5);
            float h0 = 3.6f;
            float h = h0 + floors * 3.1f;
            Color wall = Pick(WallColors);
            var ground = Chance(0.45f) ? Mats.Brick(wall.Darkened(0.15f), new Color(0.8f, 0.78f, 0.72f)) : Mats.Plaster(wall.Lightened(0.15f));
            Props.Box(t, new Vector3(w, h0, d), ground, new Vector3(0, h0 * 0.5f, -d * 0.5f), default, 0f, true, true);
            Props.Box(t, new Vector3(w, h - h0, d), FacadeMat(wall), new Vector3(0, h0 + (h - h0) * 0.5f, -d * 0.5f), default, 0f, true, true);
            // Gesims zwischen Laden und Wohnungen
            Props.Box(t, new Vector3(w + 0.2f, 0.25f, 0.3f), Mats.Std(wall.Lightened(0.35f), 0.7f), new Vector3(0, h0, 0.1f), default, 0.02f);
            FlatRoof(t, w, d, h, wall);
            if (!shop)
            {
                // Hauseingang + Schaufenster ohne Laden (Wohnhaus mit Erdgeschoss)
                Door(t, new Vector3(R(-w * 0.3f, w * 0.3f), 0, 0), new Color(0.35f, 0.24f, 0.16f));
                Props.Box(t, new Vector3(w * 0.4f, 1.3f, 0.04f), Mats.Window(false), new Vector3(-w * 0.2f, 1.7f, 0.02f), default, 0f, false);
            }
            return h;
        }

        /// <summary>Wohnblock: 3–5 Etagen, Flachdach, Balkone.</summary>
        private static float Apartment(Transform t, float w, float d)
        {
            int floors = _rng.Next(3, 6);
            float h = floors * 3f + 0.4f;
            Color wall = Pick(WallColors);
            Props.Box(t, new Vector3(w, h, d), FacadeMat(wall, 0.5f), new Vector3(0, h * 0.5f, -d * 0.5f), default, 0f, true, true);
            Props.Box(t, new Vector3(w + 0.1f, 0.4f, d + 0.1f), Mats.Concrete(new Color(0.6f, 0.6f, 0.58f), new Color(0.5f, 0.5f, 0.48f)), new Vector3(0, 0.2f, -d * 0.5f), default, 0.02f);
            var rail = Mats.Std(Pick(SignColors).Lightened(0.3f), 0.6f);
            int cols = Mathf.Max(1, Mathf.FloorToInt(w / 3.6f));
            for (int f = 1; f < floors; f++)
                for (int c = 0; c < cols; c++)
                {
                    if (!Chance(0.6f)) continue;
                    float bx = -w * 0.5f + (c + 0.5f) * (w / cols);
                    float by = f * 3f + 0.4f;
                    Props.Box(t, new Vector3(2.2f, 0.12f, 0.9f), Mats.Concrete(), new Vector3(bx, by, 0.45f), default, 0.02f);
                    Props.Box(t, new Vector3(2.2f, 0.9f, 0.05f), rail, new Vector3(bx, by + 0.5f, 0.88f), default, 0.01f);
                    if (Chance(0.25f)) Props.Box(t, new Vector3(0.3f, 0.5f, 0.3f), Mats.Foliage(new Color(0.3f, 0.55f, 0.25f)), new Vector3(bx + 0.7f, by + 0.3f, 0.6f));
                }
            FlatRoof(t, w, d, h, wall);
            return h;
        }

        /// <summary>Einfamilien-/Reihenhaus mit Satteldach (Firstrichtung parallel zur Straße).</summary>
        private static float FamilyHouse(Transform t, float w, float d)
        {
            int floors = _rng.Next(2, 4);
            float h = floors * 3f + 0.3f;
            Color wall = Pick(WallColors);
            Props.Box(t, new Vector3(w, h, d), FacadeMat(wall, 0.45f), new Vector3(0, h * 0.5f, -d * 0.5f), default, 0f, true, true);
            Props.Box(t, new Vector3(w + 0.06f, 0.5f, d + 0.06f), Mats.Std(wall.Darkened(0.35f), 0.8f), new Vector3(0, 0.25f, -d * 0.5f), default, 0.02f);
            if (Chance(0.2f))
            {
                FlatRoof(t, w, d, h, wall);
                return h;
            }
            float pitch = R(28f, 40f);
            float rh = Mathf.Tan(pitch * Mathf.Deg2Rad) * (d * 0.5f + 0.4f);
            var roof = new GameObject("Roof");
            roof.transform.SetParent(t, false);
            roof.transform.localPosition = new Vector3(0, h, -d * 0.5f);
            roof.AddComponent<MeshFilter>().sharedMesh = Prism(w + 0.5f, d + 0.8f, rh);
            roof.AddComponent<MeshRenderer>().sharedMaterial = Mats.RoofMat(Pick(RoofColors));
            // Giebelwände in Hausfarbe (etwas eingerückt unter dem Dach)
            var gable = new GameObject("Gable");
            gable.transform.SetParent(t, false);
            gable.transform.localPosition = new Vector3(0, h, -d * 0.5f);
            gable.AddComponent<MeshFilter>().sharedMesh = Prism(w, d, rh * (d / (d + 0.8f)) - 0.05f);
            gable.AddComponent<MeshRenderer>().sharedMaterial = Mats.Plaster(wall);
            // Schornstein, Dachfenster
            if (Chance(0.7f))
                Props.Box(t, new Vector3(0.6f, rh + 0.8f, 0.6f), Mats.Brick(new Color(0.55f, 0.28f, 0.2f), new Color(0.75f, 0.72f, 0.68f)), new Vector3(R(-w * 0.3f, w * 0.3f), h + (rh + 0.8f) * 0.5f, -d * 0.7f));
            if (Chance(0.4f))
            {
                float dx = R(-w * 0.25f, w * 0.25f);
                Props.Box(t, new Vector3(1.4f, 1.1f, 1.2f), Mats.Plaster(wall), new Vector3(dx, h + 0.55f, -d * 0.18f), default, 0.02f);
                Props.Box(t, new Vector3(1f, 0.7f, 0.03f), Mats.Window(false), new Vector3(dx, h + 0.55f, -d * 0.18f + 0.61f), default, 0f, false);
                Props.Box(t, new Vector3(1.6f, 0.1f, 1.4f), Mats.RoofMat(Pick(RoofColors)), new Vector3(dx, h + 1.15f, -d * 0.18f), default, 0.01f);
            }
            return h + rh;
        }

        private static void FlatRoof(Transform t, float w, float d, float h, Color wall)
        {
            var dark = Mats.Std(new Color(0.24f, 0.24f, 0.26f), 0.8f);
            Props.Box(t, new Vector3(w + 0.2f, 0.25f, d + 0.2f), dark, new Vector3(0, h + 0.12f, -d * 0.5f), default, 0.02f);
            Props.Box(t, new Vector3(w + 0.25f, 0.5f, 0.2f), Mats.Std(wall.Lightened(0.25f), 0.7f), new Vector3(0, h + 0.35f, 0.05f), default, 0.02f);
            if (Chance(0.5f)) Props.Box(t, new Vector3(1.4f, 0.9f, 1f), Mats.Metal(), new Vector3(R(-w * 0.3f, w * 0.3f), h + 0.7f, -d * R(0.3f, 0.7f)), default, 0.03f);
            if (Chance(0.25f)) Props.Cyl(t, 0.03f, 0.03f, 2.2f, Mats.DarkMetal(), new Vector3(R(-w * 0.4f, w * 0.4f), h + 1.3f, -d * 0.6f), default, 6);
        }

        private static void Door(Transform t, Vector3 at, Color color)
        {
            Props.Box(t, new Vector3(1.2f, 2.3f, 0.08f), Mats.Std(new Color(0.85f, 0.85f, 0.82f), 0.6f), at + new Vector3(0, 1.15f, 0.02f), default, 0.01f);
            Props.Box(t, new Vector3(1f, 2.15f, 0.08f), Mats.Std(color, 0.6f), at + new Vector3(0, 1.08f, 0.05f), default, 0.01f);
            Props.Box(t, new Vector3(0.05f, 0.05f, 0.12f), Mats.Metal(), at + new Vector3(0.36f, 1.05f, 0.11f), default, 0f, false);
        }

        /// <summary>Ladenfront: Schaufenster, Tür, gestreifte Markise und Schild mit Namen.</summary>
        private static void ShopFront(Transform t, float w, string name, float height)
        {
            Color sc = Pick(SignColors);
            float ww = Mathf.Max(2f, w * 0.55f);
            float wx = -w * 0.5f + 0.6f + ww * 0.5f;
            var frame = Mats.Std(new Color(0.15f, 0.15f, 0.17f), 0.5f, 0.4f);
            Props.Box(t, new Vector3(ww + 0.2f, 2.3f, 0.06f), frame, new Vector3(wx, 1.5f, 0.02f), default, 0.01f);
            Props.Box(t, new Vector3(ww, 2.1f, 0.04f), Mats.Window(false), new Vector3(wx, 1.5f, 0.06f), default, 0f, false);
            // Auslage im Fenster (bunte Kästchen)
            for (int i = 0; i < 4; i++)
                Props.Box(t, new Vector3(0.35f, 0.3f, 0.2f), Mats.Std(Pick(SignColors).Lightened(0.3f), 0.6f), new Vector3(wx - ww * 0.35f + i * ww * 0.23f, 0.65f, 0.16f), default, 0.01f, false);
            Door(t, new Vector3(w * 0.5f - 1.1f, 0, 0.02f), sc.Darkened(0.3f));
            // Markise
            int stripes = Mathf.Max(3, Mathf.RoundToInt(w / 0.9f));
            float sw = w / stripes;
            var a = Mats.Std(sc, 0.6f);
            var b = Mats.Std(new Color(0.96f, 0.96f, 0.94f), 0.6f);
            if (Chance(0.75f))
                for (int i = 0; i < stripes; i++)
                    Props.Box(t, new Vector3(sw + 0.005f, 0.08f, 1.3f), i % 2 == 0 ? a : b, new Vector3(-w * 0.5f + (i + 0.5f) * sw, 3.05f, 0.6f), new Vector3(-12f, 0, 0), 0f);
            // Schild (nicht statisch: Text)
            var sign = Props.SignBoard(_signs, name.ToUpperInvariant(), sc, sc.r + sc.g * 1.4f + sc.b * 0.4f > 1.4f ? new Color(0.1f, 0.1f, 0.12f) : Color.white,
                new Vector2(Mathf.Min(w - 1f, 0.5f + name.Length * 0.32f), 0.62f));
            sign.transform.position = t.TransformPoint(new Vector3(0, 3.75f, 0.12f));
            sign.transform.rotation = t.rotation;
            // Kleinkram vor dem Laden
            if (Chance(0.35f))
            {
                var ab = Props.Node(_s, "ABoard");
                ab.transform.position = t.TransformPoint(new Vector3(wx, 0, 1.0f));
                ab.transform.rotation = t.rotation * Quaternion.Euler(0, R(-25f, 25f), 0);
                Props.Box(ab.transform, new Vector3(0.6f, 0.9f, 0.05f), Mats.Std(new Color(0.12f, 0.12f, 0.12f), 0.7f), new Vector3(0, 0.45f, 0.15f), new Vector3(-10, 0, 0));
                string[] deals = { "HEUTE\n2 FÜR 1", "NEU!\nJETZT\nHIER", "RABATT\n-10%", "FRISCH\nAUS DEM\nOFEN", "WIR\nSIND\nOFFEN" };
                var lab = Props.Node(_signs, "ABoardText");
                lab.transform.position = ab.transform.TransformPoint(new Vector3(0, 0.5f, 0.2f));
                lab.transform.rotation = ab.transform.rotation * Quaternion.Euler(-10, 0, 0);
                Label3D.Create(lab.transform, Pick(deals), 30f, Color.white, Vector3.zero, false);
            }
        }

        private static void Doorstep(Transform t, float w)
        {
            float dx = R(-w * 0.25f, w * 0.25f);
            Door(t, new Vector3(dx, 0, 0), Pick(SignColors).Darkened(0.2f));
            Props.Box(t, new Vector3(1.6f, 0.12f, 0.8f), Mats.Concrete(), new Vector3(dx, 0.06f, 0.4f), default, 0.02f);
            Props.Box(t, new Vector3(1.6f, 0.08f, 0.9f), Mats.Std(new Color(0.3f, 0.3f, 0.32f), 0.6f), new Vector3(dx, 2.55f, 0.45f), default, 0.01f);
            // Briefkasten, Hecke
            Props.Box(t, new Vector3(0.35f, 0.45f, 0.18f), Mats.Std(Chance(0.5f) ? new Color(0.95f, 0.8f, 0.1f) : new Color(0.2f, 0.2f, 0.22f), 0.5f), new Vector3(dx + 1.1f, 1.2f, 0.1f), default, 0.02f);
            var hedge = Mats.Foliage(new Color(0.22f, 0.45f, 0.2f));
            float left = dx - 1.2f - (-w * 0.5f), right = w * 0.5f - (dx + 1.6f);
            if (left > 0.8f && Chance(0.6f)) Props.Box(t, new Vector3(left, 0.75f, 0.45f), hedge, new Vector3(-w * 0.5f + left * 0.5f, 0.375f, 0.5f), default, 0.05f);
            if (right > 0.8f && Chance(0.6f)) Props.Box(t, new Vector3(right, 0.75f, 0.45f), hedge, new Vector3(w * 0.5f - right * 0.5f, 0.375f, 0.5f), default, 0.05f);
        }

        // ---- Satteldach als Prisma (First entlang X, Basis in Z, Spitze bei y = h) --------------------
        private static readonly Dictionary<string, Mesh> PrismCache = new Dictionary<string, Mesh>();

        public static Mesh Prism(float len, float baseW, float h)
        {
            string key = len.ToString("0.00") + "|" + baseW.ToString("0.00") + "|" + h.ToString("0.00");
            if (PrismCache.TryGetValue(key, out var cached) && cached != null) return cached;
            float L = len * 0.5f, B = baseW * 0.5f;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 nn, Vector2 ua, Vector2 ub, Vector2 uc)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), nn) < 0f)
                {
                    var tmp = b; b = c; c = tmp;
                    var tu = ub; ub = uc; uc = tu;
                }
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c);
                n.Add(nn); n.Add(nn); n.Add(nn);
                uv.Add(ua); uv.Add(ub); uv.Add(uc);
                tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
            }
            var lF = new Vector3(-L, 0, B); var lB = new Vector3(-L, 0, -B); var lT = new Vector3(-L, h, 0);
            var rF = new Vector3(L, 0, B); var rB = new Vector3(L, 0, -B); var rT = new Vector3(L, h, 0);
            float slope = Mathf.Sqrt(B * B + h * h);
            // Giebel
            Tri(lF, lB, lT, Vector3.left, new Vector2(B, 0), new Vector2(-B, 0), new Vector2(0, h));
            Tri(rF, rB, rT, Vector3.right, new Vector2(-B, 0), new Vector2(B, 0), new Vector2(0, h));
            // Dachflächen
            var nf = new Vector3(0, B, h).normalized;
            Tri(lF, rF, rT, nf, new Vector2(-L, 0), new Vector2(L, 0), new Vector2(L, slope));
            Tri(lF, rT, lT, nf, new Vector2(-L, 0), new Vector2(L, slope), new Vector2(-L, slope));
            var nb = new Vector3(0, B, -h).normalized;
            Tri(lB, rB, rT, nb, new Vector2(L, 0), new Vector2(-L, 0), new Vector2(-L, slope));
            Tri(lB, rT, lT, nb, new Vector2(L, 0), new Vector2(-L, slope), new Vector2(L, slope));
            // Unterseite
            Tri(lF, rF, rB, Vector3.down, new Vector2(-L, B), new Vector2(L, B), new Vector2(L, -B));
            Tri(lF, rB, lB, Vector3.down, new Vector2(-L, B), new Vector2(L, -B), new Vector2(-L, -B));
            var mesh = new Mesh { name = "Prism" };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            try { mesh.RecalculateTangents(); } catch (System.Exception) { }
            PrismCache[key] = mesh;
            return mesh;
        }

        // =====================================================================================
        // PaketBlitz-Filiale (begehbar, Schalter = Station, siehe PostService)
        // =====================================================================================
        public static readonly Rect PostBuilding = new Rect(62f, -25f, 23f, 16f);

        private static void PostOffice()
        {
            float x0 = PostBuilding.xMin, x1 = PostBuilding.xMax, z0 = PostBuilding.yMin, z1 = PostBuilding.yMax;
            const float h = 5f, th = 0.3f;
            var yellow = Mats.Plaster(new Color(0.98f, 0.8f, 0.15f));
            var red = Mats.Std(new Color(0.85f, 0.14f, 0.12f), 0.5f);
            float doorA = PostService.PostDoorX - 1.7f, doorB = PostService.PostDoorX + 1.7f;
            // Vorderwand mit Tür
            WallX(x0, doorA, z1 - th * 0.5f, 0f, h, th, yellow);
            WallX(doorB, x1, z1 - th * 0.5f, 0f, h, th, yellow);
            WallX(doorA, doorB, z1 - th * 0.5f, 2.7f, h, th, yellow);
            WallX(x0, x1, z0 + th * 0.5f, 0f, h, th, yellow);
            WallZ(x0 + th * 0.5f, z0, z1, h, th, yellow);
            WallZ(x1 - th * 0.5f, z0, z1, h, th, yellow);
            // Rotes Band und Dach
            Props.Box(_s, new Vector3(x1 - x0 + 0.1f, 0.5f, 0.06f), red, new Vector3((x0 + x1) * 0.5f, 3.4f, z1 + 0.02f), default, 0f);
            Props.Box(_s, new Vector3(x1 - x0 + 0.5f, 0.3f, z1 - z0 + 0.5f), Mats.RoofMat(new Color(0.3f, 0.3f, 0.32f)), new Vector3((x0 + x1) * 0.5f, h + 0.15f, (z0 + z1) * 0.5f), default, 0.02f);
            Props.Box(_s, new Vector3(x1 - x0 - 0.4f, 0.025f, z1 - z0 - 0.4f), Mats.Tiles(0.5f), new Vector3((x0 + x1) * 0.5f, 0.02f, (z0 + z1) * 0.5f), default, 0f, false);
            // Schaufenster
            foreach (float wx in new[] { 66f, 71f, 84f - 2.2f })
            {
                if (Mathf.Abs(wx - PostService.PostDoorX) < 3f) continue;
                Props.Box(_s, new Vector3(3.6f, 1.8f, 0.03f), Mats.Window(false), new Vector3(wx, 1.6f, z1 + 0.01f), default, 0f, false);
                Props.Box(_s, new Vector3(3.6f, 1.8f, 0.03f), Mats.Window(true), new Vector3(wx, 1.6f, z1 - th - 0.01f), default, 0f, false);
            }
            Label3D.Create(_signs, "PaketBlitz", 300f, new Color(0.85f, 0.12f, 0.1f), new Vector3(PostService.PostDoorX - 4.5f, 4.25f, z1 + 0.06f), false, 0f, false, 1.4f);
            Label3D.Create(_signs, "FILIALE", 160f, new Color(0.12f, 0.12f, 0.14f), new Vector3(PostService.PostDoorX + 4.5f, 4.25f, z1 + 0.06f), false, 0f, false, 1.2f);
            Label3D.Create(_signs, "Mo–Sa 8–20 Uhr · Pakete gratis abgeben", 40f, new Color(0.12f, 0.12f, 0.14f), new Vector3(PostService.PostDoorX, 2.95f, z1 + 0.06f), false);
            // Blitz-Logo
            Props.Box(_s, new Vector3(0.35f, 1.2f, 0.05f), red, new Vector3(PostService.PostDoorX, 4.2f, z1 + 0.05f), new Vector3(0, 0, 25f), 0.01f);
            // Innen: Regale mit Paketen hinter dem Schalter, Absperrbänder, Plakat, Licht
            var card = Mats.Cardboard();
            for (int s = 0; s < 3; s++)
            {
                float sx = PostService.CounterPos.x - 3f + s * 3f;
                Props.Solid(_s, new Vector3(2.4f, 2.2f, 0.5f), Mats.Std(new Color(0.55f, 0.57f, 0.6f), 0.5f, 0.5f), new Vector3(sx, 1.1f, z0 + 0.6f));
                for (int i = 0; i < 6; i++)
                    Props.Box(_s, new Vector3(R(0.3f, 0.55f), R(0.25f, 0.45f), 0.35f), card, new Vector3(sx - 0.8f + (i % 3) * 0.8f, 0.55f + (i / 3) * 0.9f + 0.2f, z0 + 0.7f), new Vector3(0, R(-8f, 8f), 0), 0.01f);
            }
            for (int i = 0; i < 4; i++)
            {
                var post = Props.Cyl(_s, 0.04f, 0.06f, 1f, Mats.Metal(), new Vector3(PostService.CounterPos.x - 2.5f + i * 1.6f, 0.5f, PostService.CounterPos.z + 2.4f), default, 8);
                post.name = "QueuePost";
            }
            Props.Box(_s, new Vector3(4.8f, 0.06f, 0.02f), red, new Vector3(PostService.CounterPos.x - 0.1f, 0.9f, PostService.CounterPos.z + 2.4f), default, 0f, false);
            var poster = Props.SignBoard(_signs, "Heute schon\ngehustlet?", new Color(0.12f, 0.12f, 0.14f), new Color(1f, 0.85f, 0.2f), new Vector2(1.6f, 1.1f));
            poster.transform.localPosition = new Vector3(x0 + th + 0.05f, 2.1f, (z0 + z1) * 0.5f);
            poster.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            foreach (float lx in new[] { 68f, 79f })
            {
                var fix = Props.CeilingLight(_s, 2f);
                fix.transform.localPosition = new Vector3(lx, h - 0.1f, (z0 + z1) * 0.5f);
                Props.PointLight(_world, new Vector3(lx, h - 1.1f, (z0 + z1) * 0.5f), new Color(1f, 0.95f, 0.85f), 2.4f, 9f);
            }
            // Mitarbeiterin am Schalter
            var clerkGo = Props.Node(_dyn, "PostClerk", PostService.CounterPos + new Vector3(0, 0, -1.3f), 0f);
            var look = CharacterKit.RandomLook(_rng);
            look.Shirt = new Color(0.98f, 0.8f, 0.15f);
            look.Sitting = false;
            look.Cap = true;
            look.CapColor = new Color(0.85f, 0.14f, 0.12f);
            var clerk = clerkGo.AddComponent<NPC>();
            clerk.Setup(look);
            if (Game.Player != null) clerk.LookAtTarget = Game.Player.transform;
            PostService.Clerk = clerk;
            Label3D.Create(clerkGo.transform, "Frau Blitz", 30f, new Color(1f, 0.92f, 0.6f), new Vector3(0, 2.05f, 0), true, 10f);
            // Hof mit Lieferwagen
            foreach (float vz in new[] { -15.5f, -21.5f })
            {
                var van = Props.Van(_dyn, new Color(0.98f, 0.78f, 0.1f), "PaketBlitz", new Color(0.85f, 0.12f, 0.1f));
                van.transform.localPosition = new Vector3(90.2f, 0, vz);
                van.transform.localRotation = Quaternion.Euler(0, -90f, 0);
                Props.Collider(_world, new Vector3(2.3f, 2.4f, 5f), new Vector3(90.2f, 1.2f, vz));
            }
            var lot = Mats.Concrete(new Color(0.5f, 0.5f, 0.49f), new Color(0.42f, 0.42f, 0.41f), 0.25f, 0f, 0.4f);
            Props.Box(_s, new Vector3(9f, 0.02f, 22f), lot, new Vector3(90.5f, 0.01f, -19f), default, 0f, false);
            Props.Box(_s, new Vector3(23f, 0.02f, 2f), Mats.Sidewalk(new Color(0.7f, 0.68f, 0.6f), new Color(0.6f, 0.58f, 0.52f)), new Vector3(73.5f, 0.01f, -8f), default, 0f, false);
            CityLayout.AddPoi(CityLayout.ZoneMainSouth, new Vector3(PostService.PostDoorX + 1.5f, 0, -6.2f), new Vector3(PostService.PostDoorX, 2f, -9f), 2f, 5f);
            CityLayout.ShopNames.Add("PaketBlitz Filiale");
        }

        private static void WallX(float xa, float xb, float z, float y0, float y1, float th, Material mat)
        {
            if (xb - xa < 0.02f || y1 - y0 < 0.02f) return;
            Props.Solid(_s, new Vector3(xb - xa, y1 - y0, th), mat, new Vector3((xa + xb) * 0.5f, (y0 + y1) * 0.5f, z), default, 0.015f);
        }

        private static void WallZ(float x, float za, float zb, float h, float th, Material mat)
        {
            Props.Solid(_s, new Vector3(th, h, zb - za), mat, new Vector3(x, h * 0.5f, (za + zb) * 0.5f), default, 0.015f);
        }

        // =====================================================================================
        // Geparkte Autos (Wohnstraße, Querstraßen-Buchten)
        // =====================================================================================
        private static void ParkedCars()
        {
            float z = WorldLots.ResidentialZ;
            foreach (float laneZ in new[] { z - 3.9f, z + 3.9f })
            {
                for (float x = WorldLots.ResidentialFromX + 5f; x < WorldLots.ResidentialToX - 4f; x += R(5.6f, 7.5f))
                {
                    if (WorldLots.OnSideStreet(x, 2.5f)) continue;
                    if (!Chance(0.5f)) continue;
                    ParkedCar(new Vector3(x, 0, laneZ), laneZ > z ? 0f : 180f);
                }
            }
            // Ein paar Autos vor den Läden an der Hauptstraße? Die Fahrbahn bleibt frei – stattdessen Buchten in den Querstraßen-Enden (Süd).
            foreach (float sx in WorldLots.SideStreetsSouthX)
                for (float pz = -40f; pz < -14f; pz += 6.5f)
                    if (Chance(0.45f)) ParkedCar(new Vector3(sx + WorldLots.SideHalfWidth - 1.2f, 0, pz), -90f);
        }

        /// <summary>Geparktes Auto (rotY: Fahrzeugfront entlang +X bei 0).</summary>
        public static GameObject ParkedCar(Vector3 pos, float rotY)
        {
            string id = Pick(ParkedCarIds);
            var car = Props.Car(_dyn, Pick(CarColors), false, AssetLib.HasModel(id) ? id : null);
            car.transform.localPosition = pos;
            car.transform.localRotation = Quaternion.Euler(0, rotY, 0);
            bool alongZ = Mathf.Abs(Mathf.Sin(rotY * Mathf.Deg2Rad)) > 0.5f;
            Props.Collider(_world, alongZ ? new Vector3(1.9f, 1.4f, 4.2f) : new Vector3(4.2f, 1.4f, 1.9f), pos + new Vector3(0, 0.7f, 0));
            return car;
        }

        // =====================================================================================
        // Grün: Straßenbäume, Hinterhöfe
        // =====================================================================================
        private static void Greenery()
        {
            // Straßenbäume an der Wohnstraße (zwischen den Laternen)
            for (float x = WorldLots.ResidentialFromX + 14f; x < WorldLots.ResidentialToX - 6f; x += 18f)
            {
                if (WorldLots.OnSideStreet(x, 2f)) continue;
                Tree(new Vector3(x, 0, WorldLots.ResidentialZ - WorldLots.ResidentialHalfWidth - 1.6f), 0.9f);
                if (Chance(0.7f)) Tree(new Vector3(x - 9f, 0, WorldLots.ResidentialZ + WorldLots.ResidentialHalfWidth + 1.6f), 0.9f);
            }
            // Hinterhöfe hinter der Südzeile und hinter der Nordreihe
            int placed = 0, tries = 0;
            while (placed < 45 && tries++ < 400)
            {
                bool south = Chance(0.5f);
                float x = R(-148f, 148f);
                float z = south ? R(-47f, -27f) : R(78f, 82f);
                var p = new Vector3(x, 0, z);
                if (WorldLots.IsReserved(p, 1f)) continue;
                if (south && WorldLots.OnSideStreet(x, 1f, true)) continue;
                if (south && x > -6f && x < 38f) continue; // hinter der Lagerhalle bleibt der Hof frei
                if (south && x > 58f && x < 96f && z > -31f) continue;
                Tree(p, R(0.85f, 1.3f));
                placed++;
            }
            // Kleine Grünflächen an den Querstraßen-Ecken
            foreach (float sx in new[] { -128.5f, 118.5f })
                for (int i = 0; i < 3; i++) Props.Bush(_s, R(0.7f, 1f)).transform.localPosition = new Vector3(sx + R(-1f, 1f) + (sx > 0 ? -1.5f : 1.5f), 0, -10f - i * 3f);
        }

        private static void Tree(Vector3 p, float scale)
        {
            var tree = Props.Tree(_s, scale, _rng.Next(0, 6));
            tree.transform.localPosition = p;
            tree.transform.localRotation = Quaternion.Euler(0, R(0f, 360f), 0);
            Props.Collider(_world, new Vector3(0.4f, 3f, 0.4f), p + new Vector3(0, 1.5f, 0));
        }

        // =====================================================================================
        // Mülleimer (die Müllabfuhr leert sie, siehe GarbageTruck)
        // =====================================================================================
        private static void Bins()
        {
            var binsRoot = Props.Node(_dyn, "Bins").transform;
            for (float x = -140f; x <= 140f; x += 28f)
            {
                if (!WorldLots.OnSideStreet(x + 3f, 1f)) Bin(binsRoot, new Vector3(x + 3f, 0, 6.7f), 180f, false);
                if (!WorldLots.OnSideStreet(x - 11f, 1f, true)) Bin(binsRoot, new Vector3(x - 11f, 0, -6.7f), 0f, false);
            }
            for (float x = WorldLots.ResidentialFromX + 10f; x < WorldLots.ResidentialToX - 5f; x += 36f)
            {
                if (WorldLots.OnSideStreet(x, 1f)) continue;
                Bin(binsRoot, new Vector3(x, 0, WorldLots.ResidentialZ - WorldLots.ResidentialHalfWidth - 0.5f), 0f, false);
                Bin(binsRoot, new Vector3(x + 18f, 0, WorldLots.ResidentialZ + WorldLots.ResidentialHalfWidth + 0.5f), 180f, false);
            }
            // Altpapier-Container der Firma (Garage-Hof und Lagerhalle)
            Bin(binsRoot, new Vector3(-23.8f, 0, -7.6f), 0f, true);
            Bin(binsRoot, new Vector3(35.2f, 0, -8.2f), 0f, true);
        }

        private static void Bin(Transform root, Vector3 pos, float rotY, bool business)
        {
            var node = Props.Node(root, business ? "PaperBin" : "Bin", pos, rotY);
            GameObject vis = null;
            if (business)
            {
                vis = Props.Node(node.transform, "Container");
                var blue = Mats.Std(new Color(0.15f, 0.32f, 0.7f), 0.55f);
                Props.Box(vis.transform, new Vector3(1.2f, 1.1f, 0.8f), blue, new Vector3(0, 0.6f, 0), default, 0.03f);
                Props.Box(vis.transform, new Vector3(1.25f, 0.08f, 0.85f), Mats.Std(new Color(0.1f, 0.2f, 0.45f), 0.5f), new Vector3(0, 1.19f, 0), default, 0.02f);
                foreach (float wx in new[] { -0.45f, 0.45f })
                    Props.Cyl(vis.transform, 0.07f, 0.07f, 0.06f, Mats.Std(new Color(0.1f, 0.1f, 0.1f)), new Vector3(wx, 0.07f, 0.3f), new Vector3(90, 0, 0), 10);
                Label3D.Create(vis.transform, "ALTPAPIER", 36f, Color.white, new Vector3(0, 0.75f, 0.41f), false);
                Props.Collider(_world, new Vector3(1.3f, 1.2f, 0.9f), pos + new Vector3(0, 0.6f, 0));
            }
            else
            {
                vis = Props.Asset(node.transform, "TrashModel", Chance(0.5f) ? "street.trash_a" : "street.trash_b", 1.05f);
                if (vis == null) vis = Props.TrashBin(node.transform, new Color(0.95f, 0.55f, 0.1f));
                Props.Collider(_world, new Vector3(0.6f, 1f, 0.6f), pos + new Vector3(0, 0.5f, 0));
            }
            NpcNav.AddPoint(_world.TransformPoint(pos));
            var bag = Props.Sphere(node.transform, 0.28f, Mats.Std(new Color(0.12f, 0.12f, 0.13f), 0.3f), new Vector3(0, business ? 1.25f : 1.0f, 0), 8, 5, true);
            bag.name = "Overflow";
            CityServices.RegisterBin(node.transform, bag.transform, business);
        }

        // =====================================================================================
        // Skyline hinter den Grenzen + Penthouse-Hochhaus
        // =====================================================================================
        private static GameObject Skyline()
        {
            GameObject penthouse = null;
            bool models = AssetLib.HasModel("city.building_skyscraper_d") && AssetLib.HasModel("city.low_detail_building_a");
            string[] tall = { "city.building_skyscraper_a", "city.building_skyscraper_b", "city.building_skyscraper_c", "city.building_skyscraper_e", "city.building_m" };
            string[] low = { "city.low_detail_building_a", "city.low_detail_building_b", "city.low_detail_building_c", "city.low_detail_building_d", "city.low_detail_building_e",
                "city.low_detail_building_f", "city.low_detail_building_g", "city.low_detail_building_h", "city.low_detail_building_i", "city.low_detail_building_j",
                "city.low_detail_building_k", "city.low_detail_building_l", "city.low_detail_building_m", "city.low_detail_building_wide_a", "city.low_detail_building_wide_b" };
            if (models)
            {
                SkyRow(low, -175f, 175f, 92f, 180f, 1.4f, 18f);
                SkyRow(tall, -180f, 180f, 108f, 180f, 1f, 18f);
                SkyRow(low, -175f, 175f, -60f, 0f, 1.5f, -1000f);
                SkyRow(tall, -180f, 180f, -76f, 0f, 1f, -1000f);
                for (float z = -50f; z <= 90f; z += 16f)
                {
                    Props.AssetAt(_s, low[_rng.Next(low.Length)], new Vector3(-168f, 0, z), 90f, 18f);
                    Props.AssetAt(_s, low[_rng.Next(low.Length)], new Vector3(168f, 0, z), -90f, 18f);
                }
                var tower = Props.AssetAt(_s, "city.building_skyscraper_d", new Vector3(18f, 0, 98f), 180f, 52f);
                if (tower != null)
                {
                    var b = Props.LocalBounds(tower);
                    penthouse = Props.Box(_world, new Vector3(b.size.x * 1.02f, 3f, b.size.z * 1.02f), Mats.Emit(new Color(1f, 0.78f, 0.3f), 3f),
                        new Vector3(18f, Mathf.Max(8f, b.max.y - 5f), 98f - b.center.z), default, 0f, false);
                    penthouse.SetActive(false);
                }
            }
            if (penthouse == null)
            {
                // Prozedurale Kulisse
                Color[] walls = { new Color(0.72f, 0.66f, 0.58f), new Color(0.6f, 0.62f, 0.66f), new Color(0.75f, 0.55f, 0.45f), new Color(0.55f, 0.58f, 0.52f), new Color(0.82f, 0.8f, 0.74f) };
                if (!models)
                    foreach (float rz in new[] { 96f, -64f })
                        for (float x = -175f; x < 175f;)
                        {
                            float w = R(12f, 20f), hh = R(14f, 40f);
                            Props.Box(_s, new Vector3(w - 1f, hh, 14f), Mats.Facade(Pick(walls), _rng.Next(1, 6), 0.45f), new Vector3(x + w * 0.5f, hh * 0.5f, rz), default, 0f, true, true);
                            x += w;
                        }
                Props.Box(_s, new Vector3(16f, 48f, 14f), Mats.Facade(new Color(0.6f, 0.62f, 0.66f), 3f, 0.5f), new Vector3(18f, 24f, 100f), default, 0f, true, true);
                penthouse = Props.Box(_world, new Vector3(16.2f, 3f, 14.2f), Mats.Emit(new Color(1f, 0.78f, 0.3f), 3f), new Vector3(18f, 46f, 100f), default, 0f, false);
                penthouse.SetActive(false);
            }
            return penthouse;
        }

        private static void SkyRow(string[] ids, float xFrom, float xTo, float z, float rotY, float scale, float skipX)
        {
            float x = xFrom;
            int guard = 0;
            while (x < xTo && guard++ < 120)
            {
                string id = ids[_rng.Next(ids.Length)];
                var info = AssetLib.Info(id);
                if (info == null)
                {
                    x += 10f;
                    continue;
                }
                float fit = info.Meters * scale;
                var mb = AssetLib.ModelBounds(id);
                float k = info.Meters > 0.01f ? fit / info.Meters : 1f;
                float w = Mathf.Max(4f, mb.size.x * k);
                float cx = x + w * 0.5f + 0.3f;
                if (Mathf.Abs(cx - skipX) < 9f)
                {
                    x = skipX + 9f;
                    continue;
                }
                Props.AssetAt(_s, id, new Vector3(cx, 0, z), rotY, fit);
                x += w + 0.6f + (float)_rng.NextDouble() * 1.5f;
            }
        }
    }

    /// <summary>
    /// Straßenlaternen der Stadt: nachts an (wie <see cref="Atmosphere"/>), aber nur in der Nähe der Kamera –
    /// sonst wären es zu viele Punktlichter.
    /// </summary>
    public sealed class CityLights : MonoBehaviour
    {
        private readonly List<Light> _lights = new List<Light>();
        private float _acc;
        public float Range = 48f;

        public void Add(Light l)
        {
            if (l != null) _lights.Add(l);
        }

        private void Update()
        {
            _acc -= Time.unscaledDeltaTime;
            if (_acc > 0f) return;
            _acc = 0.35f;
            float night = Mats.Night;
            var cam = Camera.main;
            Vector3 cp = cam != null ? cam.transform.position : Vector3.zero;
            float r2 = Range * Range;
            for (int i = _lights.Count - 1; i >= 0; i--)
            {
                var l = _lights[i];
                if (l == null)
                {
                    _lights.RemoveAt(i);
                    continue;
                }
                Vector3 d = l.transform.position - cp;
                d.y = 0f;
                bool on = night > 0.02f && d.sqrMagnitude < r2;
                if (l.enabled != on) l.enabled = on;
                if (on) l.intensity = night * 6f;
            }
        }
    }
}
