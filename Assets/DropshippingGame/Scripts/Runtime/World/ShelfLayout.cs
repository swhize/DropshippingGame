using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Standard-Plätze der Regale je Standort. Garage: Regale für <see cref="GameData.GarageShelves"/>
    /// (3 an der Wand, 2 als Insel). Lagerhalle: Reihe A (6 große Regale), Reihe B (4), dazu kleine
    /// Wandregale an der Westwand für alle weiteren Produkte.
    /// </summary>
    public static class ShelfLayout
    {
        public struct Slot
        {
            public int Product;
            public Vector3 Pos;
            public float Rot;
            /// <summary>Optik: 0 = kleines Regal, 1 = großes Schwerlastregal.</summary>
            public int Visual;
        }

        private static readonly Vector3[] GaragePos =
        {
            new Vector3(-10.75f, 0, -14.2f), new Vector3(-10.75f, 0, -11.9f), new Vector3(-10.75f, 0, -9.6f),
            new Vector3(-13.6f, 0, -13.5f), new Vector3(-13.6f, 0, -11.2f),
        };

        private static readonly float[] GarageRot = { -90f, -90f, -90f, 90f, 90f };
        private static readonly float[] RowB = { 7.8f, 12.8f, 22.8f, 27.8f };
        private static readonly float[] WestWallZ = { -29.4f, -27.2f, -25f, -22.8f, -20.6f };

        public static List<Slot> For(int stage)
        {
            var l = new List<Slot>();
            if (stage <= 0)
            {
                for (int i = 0; i < GameData.GarageShelves.Length && i < GaragePos.Length; i++)
                {
                    string id = GameData.GarageShelves[i];
                    if (!GameData.IsProduct(id)) continue;
                    l.Add(new Slot { Product = GameData.ProductIndex(id), Pos = GaragePos[i], Rot = GarageRot[i], Visual = 0 });
                }
                return l;
            }
            int west = 0;
            for (int i = 0; i < GameData.Products.Length; i++)
            {
                if (i < 6) l.Add(new Slot { Product = i, Pos = new Vector3(2.8f + i * 5f, 0, -29.3f), Rot = 0f, Visual = 1 });
                else if (i < 10) l.Add(new Slot { Product = i, Pos = new Vector3(RowB[i - 6], 0, -24.9f), Rot = 180f, Visual = 1 });
                else
                {
                    float z = west < WestWallZ.Length ? WestWallZ[west] : -20.6f + (west - WestWallZ.Length + 1) * 2.2f;
                    l.Add(new Slot { Product = i, Pos = new Vector3(-1.45f, 0, z), Rot = 90f, Visual = 0 });
                    west++;
                }
            }
            return l;
        }
    }
}
