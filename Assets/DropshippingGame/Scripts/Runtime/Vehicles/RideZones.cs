using UnityEngine;

namespace DropshippingGame.Vehicles
{
    /// <summary>
    /// Wo das Moped fahren darf: Innenräume (Imbiss, Garage, Halle, Post, Bank, Börse, Läden) sind gesperrt –
    /// deren Türen sind breit genug für ein Moped, Wände allein reichen also nicht. Dazu die Bordsteinkanten
    /// (Straßen sind nur Optik, der Boden ist flach), an denen das Moped einen kleinen Hüpfer macht.
    /// </summary>
    public static class RideZones
    {
        /// <summary>Innenräume als Rechtecke (x, z, Breite, Tiefe), mit etwas Rand.</summary>
        private static Rect[] _indoor;

        private static Rect[] Indoor
        {
            get
            {
                if (_indoor != null) return _indoor;
                _indoor = new[]
                {
                    WorldBuilder.DinerRect, WorldBuilder.GarageRect, WorldBuilder.WarehouseRect,
                    CityBuilder.PostBuilding,
                    new Rect(62f, 12f, 18f, 16f), // Bank (FinanceDistrict BankX0..X1, BankZ0..Z1)
                    new Rect(84f, 12f, 24f, 24f), // Börse (FinanceDistrict ExX0..X1, ExZ0..Z1)
                    ShoppingDistrict.ElectroRect, ShoppingDistrict.PetRect, ShoppingDistrict.ClothesRect,
                };
                return _indoor;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _indoor = null;

        /// <summary>Liegt der Punkt in einem Innenraum (Moped darf nicht hinein)?</summary>
        public static bool IsIndoor(Vector3 p, float margin = 0.15f)
        {
            foreach (var r in Indoor)
                if (p.x > r.xMin - margin && p.x < r.xMax + margin && p.z > r.yMin - margin && p.z < r.yMax + margin) return true;
            return false;
        }

        // ---- Bordsteine ---------------------------------------------------------------------------------
        /// <summary>
        /// Überquert ein Rad zwischen a und b eine Bordsteinkante? Hauptstraße |z| = 4 (außer an Querstraßen),
        /// Querstraßen x = sx ± 4 (außerhalb der Hauptstraße), Wohnstraße z = 53 ± 5.
        /// </summary>
        public static bool CrossesCurb(Vector3 a, Vector3 b)
        {
            // Hauptstraße
            float hw = WorldLots.MainHalfWidth;
            if (Mathf.Abs(b.x) < WorldLots.CityMaxX && Crosses(Mathf.Abs(a.z), Mathf.Abs(b.z), hw) && Mathf.Sign(a.z) == Mathf.Sign(b.z) &&
                !NearSideStreet(b.x, b.z > 0f ? 0f : -1f))
                return true;
            // Querstraßen (Bordsteine längs, außerhalb der Hauptstraßen-Fahrbahn)
            if (Mathf.Abs(b.z) > hw + 0.2f)
            {
                foreach (float sx in WorldLots.SideStreetsX)
                {
                    bool south = b.z < 0f;
                    if (south && System.Array.IndexOf(WorldLots.SideStreetsSouthX, sx) < 0) continue;
                    if (south && b.z < WorldLots.SideSouthEndZ) continue;
                    if (!south && b.z > WorldLots.ResidentialZ + WorldLots.ResidentialHalfWidth + 3f) continue;
                    if (Crosses(a.x - sx, b.x - sx, WorldLots.SideHalfWidth) || Crosses(sx - a.x, sx - b.x, WorldLots.SideHalfWidth))
                        if (!InResidentialRoad(b)) return true;
                }
            }
            // Wohnstraße
            if (b.x > WorldLots.ResidentialFromX && b.x < WorldLots.ResidentialToX)
            {
                float rz = WorldLots.ResidentialZ, rh = WorldLots.ResidentialHalfWidth;
                if (Crosses(a.z - rz, b.z - rz, rh) || Crosses(rz - a.z, rz - b.z, rh))
                    if (!NearSideStreet(b.x, 1f)) return true;
            }
            return false;
        }

        /// <summary>Wert wandert über die Schwelle (in beide Richtungen).</summary>
        private static bool Crosses(float from, float to, float edge) => (from - edge) * (to - edge) < 0f;

        private static bool NearSideStreet(float x, float northSign)
        {
            var list = northSign < 0f ? WorldLots.SideStreetsSouthX : WorldLots.SideStreetsX;
            foreach (float sx in list)
                if (Mathf.Abs(x - sx) < WorldLots.SideHalfWidth) return true;
            return false;
        }

        private static bool InResidentialRoad(Vector3 p) =>
            Mathf.Abs(p.z - WorldLots.ResidentialZ) < WorldLots.ResidentialHalfWidth && p.x > WorldLots.ResidentialFromX && p.x < WorldLots.ResidentialToX;
    }
}
