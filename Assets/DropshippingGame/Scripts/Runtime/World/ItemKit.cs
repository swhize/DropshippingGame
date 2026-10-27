using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Baut die Darstellung eines tragbaren Gegenstands (Kiste, Einzelteil, Paket, etikettiertes
    /// Paket, Teller). Ursprung = Objektmitte. Genutzt in der Hand, am Boden, im Regal und auf dem Band.
    /// </summary>
    public static class ItemKit
    {
        public static readonly Vector3[] PackageSizes = { new Vector3(0.26f, 0.18f, 0.22f), new Vector3(0.34f, 0.24f, 0.3f), new Vector3(0.46f, 0.32f, 0.38f) };
        public static readonly Vector3 CrateSize = new Vector3(0.62f, 0.46f, 0.46f);
        public static readonly Vector3 ItemSize = new Vector3(0.2f, 0.13f, 0.15f);

        public static Vector3 Bounds(ItemData data)
        {
            switch (data.Kind)
            {
                case ItemKind.Crate: return CrateSize;
                case ItemKind.Item: return ItemSize;
                case ItemKind.Package:
                case ItemKind.Labeled: return PackageSizes[SizeIndex(data)];
                case ItemKind.Plate: return new Vector3(0.3f, 0.12f, 0.3f);
                case ItemKind.Return: return PackageSizes[ProductSizeIndex(data)];
            }
            return new Vector3(0.2f, 0.2f, 0.2f);
        }

        /// <summary>Kartongröße eines Pakets: tatsächlich benutzter Karton (v3.0: evtl. größer als passend).</summary>
        private static int SizeIndex(ItemData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Product) || !GameData.IsProduct(data.Product)) return 1;
            return Mathf.Clamp(data.EffectivePackSize, 0, PackageSizes.Length - 1);
        }

        private static int ProductSizeIndex(ItemData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Product) || !GameData.IsProduct(data.Product)) return 1;
            return Mathf.Clamp(GameData.Product(data.Product).Size, 0, PackageSizes.Length - 1);
        }

        public static GameObject Build(Transform parent, ItemData data, bool showLabel = true)
        {
            var root = new GameObject(data.Kind.ToString());
            root.transform.SetParent(parent, false);
            var t = root.transform;
            ProductDef product = string.IsNullOrEmpty(data.Product) || !GameData.IsProduct(data.Product) ? null : GameData.Product(data.Product);
            switch (data.Kind)
            {
                case ItemKind.Crate:
                {
                    var s = CrateSize;
                    Props.Box(t, s, Mats.Cardboard(), Vector3.zero, default, 0.015f);
                    Props.Box(t, new Vector3(s.x * 0.2f, 0.004f, s.z + 0.004f), Mats.Std(new Color(0.82f, 0.72f, 0.48f), 0.4f), new Vector3(0, s.y / 2f, 0), default, 0f, false);
                    Props.Box(t, new Vector3(s.x + 0.004f, s.y * 0.3f, s.z * 0.5f), Mats.Std(product != null ? product.Color.ToColor() : Color.white, 0.6f), Vector3.zero, default, 0f, false);
                    float q = data.Quality;
                    var qcol = q < 0.8f ? new Color(0.9f, 0.3f, 0.25f) : (q >= 1.3f ? new Color(0.95f, 0.8f, 0.2f) : new Color(0.85f, 0.85f, 0.85f));
                    Props.Box(t, new Vector3(0.12f, 0.08f, 0.004f), Mats.Std(qcol, 0.5f), new Vector3(-s.x * 0.3f, s.y * 0.25f, s.z / 2f), default, 0f, false);
                    if (product != null)
                    {
                        // Produktbild an der Seite: kleines Modell vor weißem Etikettfeld + Name.
                        Props.Box(t, new Vector3(0.16f, 0.16f, 0.004f), Mats.Std(Color.white, 0.6f), new Vector3(s.x * 0.22f, 0.02f, s.z / 2f + 0.002f), default, 0f, false);
                        var pic = ProductModels.Build(t, product.Id, 0.12f);
                        pic.transform.localPosition = new Vector3(s.x * 0.22f, 0.03f, s.z / 2f + 0.03f);
                        pic.transform.localScale = new Vector3(1f, 1f, 0.35f);
                        Label3D.Create(t, product.Short, 22f, new Color(0.15f, 0.12f, 0.1f), new Vector3(s.x * 0.22f, -0.07f, s.z / 2f + 0.006f), false);
                    }
                    if (showLabel && product != null)
                        Label3D.Create(t, data.Quantity + "× " + product.Short, 36f, Color.white, new Vector3(0, s.y / 2f + 0.12f, 0), true, 8f);
                    break;
                }
                case ItemKind.Item:
                {
                    if (product != null)
                    {
                        // Echtes Produktmodell (passt in die Item-Box, steht auf deren Boden).
                        ProductModels.BuildFitted(t, product.Id, ItemSize * 1.15f);
                        break;
                    }
                    var s = ItemSize;
                    var col = new Color(0.9f, 0.8f, 0.3f);
                    Props.Box(t, s, Mats.Std(new Color(0.97f, 0.97f, 0.96f), 0.5f), Vector3.zero, default, 0.01f);
                    Props.Box(t, new Vector3(s.x + 0.003f, s.y * 0.55f, s.z + 0.003f), Mats.Std(col, 0.45f), new Vector3(0, s.y * 0.1f, 0), default, 0f, false);
                    break;
                }
                case ItemKind.Package:
                case ItemKind.Labeled:
                {
                    var s = PackageSizes[SizeIndex(data)];
                    Props.Box(t, s, Mats.Cardboard(), Vector3.zero, default, 0.01f);
                    Props.Box(t, new Vector3(s.x + 0.003f, 0.004f, s.z * 0.22f), Mats.Std(new Color(0.8f, 0.7f, 0.45f), 0.35f), new Vector3(0, s.y / 2f, 0), default, 0f, false);
                    var brand = data.Color.ToColor();
                    Props.Box(t, new Vector3(s.x + 0.004f, s.y * 0.14f, s.z + 0.004f), Mats.Std(brand, 0.5f), new Vector3(0, -s.y * 0.32f, 0), default, 0f, false);
                    var logo = LogoMesh(t, data.Logo, brand, Mathf.Min(s.x, s.z) * 0.32f);
                    logo.transform.localPosition = new Vector3(s.x * 0.22f, s.y / 2f + 0.004f, -s.z * 0.22f);
                    if (data.Kind == ItemKind.Labeled)
                    {
                        float lw = s.x * 0.5f, ld = s.z * 0.5f;
                        var labelCol = data.Express ? new Color(1f, 0.82f, 0.8f) : Color.white;
                        if (data.Express)
                            Props.Box(t, new Vector3(lw, 0.005f, ld * 0.22f), Mats.Std(new Color(0.9f, 0.15f, 0.12f), 0.5f),
                                new Vector3(-s.x * 0.12f, s.y / 2f + 0.009f, s.z * 0.12f - ld * 0.36f), default, 0f, false);
                        Props.Box(t, new Vector3(lw, 0.004f, ld), Mats.Std(labelCol, 0.6f), new Vector3(-s.x * 0.12f, s.y / 2f + 0.006f, s.z * 0.12f), default, 0f, false);
                        for (int i = 0; i < 7; i++)
                        {
                            float bw = 0.004f + (i % 3) * 0.003f;
                            Props.Box(t, new Vector3(bw, 0.005f, ld * 0.45f), Mats.Std(new Color(0.05f, 0.05f, 0.05f)),
                                new Vector3(-s.x * 0.12f - lw * 0.35f + i * lw * 0.11f, s.y / 2f + 0.008f, s.z * 0.12f + ld * 0.18f), default, 0f, false);
                        }
                    }
                    break;
                }
                case ItemKind.Return:
                {
                    // Ramponierter Karton: leicht verzogen, kreuz und quer zugeklebt, roter RETOURE-Aufkleber.
                    var s = PackageSizes[ProductSizeIndex(data)];
                    var dented = new Color(0.62f, 0.5f, 0.34f);
                    Props.Box(t, s, Mats.Std(dented, 0.85f), Vector3.zero, new Vector3(2.5f, 0, -1.8f), 0.02f);
                    var tape = Mats.Std(new Color(0.78f, 0.7f, 0.5f), 0.3f);
                    Props.Box(t, new Vector3(s.x + 0.01f, 0.006f, s.z * 0.2f), tape, new Vector3(0, s.y / 2f + 0.004f, 0), new Vector3(0, 28, 0), 0f, false);
                    Props.Box(t, new Vector3(s.x + 0.01f, 0.006f, s.z * 0.2f), tape, new Vector3(0, s.y / 2f + 0.006f, 0), new Vector3(0, -34, 0), 0f, false);
                    Props.Box(t, new Vector3(s.x * 0.2f, s.y + 0.01f, s.z + 0.01f), tape, new Vector3(s.x * 0.18f, 0, 0), default, 0f, false);
                    // Knick an einer Ecke
                    Props.Box(t, new Vector3(s.x * 0.3f, 0.01f, s.z * 0.3f), Mats.Std(dented.Darkened(0.25f), 0.9f),
                        new Vector3(-s.x * 0.36f, s.y / 2f + 0.003f, -s.z * 0.36f), new Vector3(-18, 0, 12), 0f, false);
                    var red = Mats.Std(StationKit.ReturnRed, 0.45f);
                    Props.Box(t, new Vector3(s.x * 0.55f, s.y * 0.3f, 0.005f), red, new Vector3(-s.x * 0.12f, 0, s.z / 2f + 0.004f), default, 0f, false);
                    Props.Box(t, new Vector3(s.x * 0.5f, 0.005f, s.z * 0.35f), red, new Vector3(-s.x * 0.15f, s.y / 2f + 0.008f, s.z * 0.18f), default, 0f, false);
                    Label3D.Create(t, "RETOURE", Mathf.Clamp(s.x * 70f, 18f, 32f), Color.white, new Vector3(-s.x * 0.12f, 0, s.z / 2f + 0.008f), false);
                    if (showLabel && product != null)
                        Label3D.Create(t, "Retoure: " + product.Short, 32f, new Color(1f, 0.55f, 0.5f), new Vector3(0, s.y / 2f + 0.14f, 0), true, 8f);
                    break;
                }
                case ItemKind.Plate:
                {
                    Props.Cyl(t, 0.15f, 0.12f, 0.02f, Mats.Std(new Color(0.97f, 0.97f, 0.97f), 0.25f), new Vector3(0, -0.05f, 0), default, 20);
                    Props.Cyl(t, 0.08f, 0.08f, 0.03f, Mats.Std(new Color(0.85f, 0.6f, 0.3f), 0.7f), new Vector3(0, -0.025f, 0));
                    Props.Cyl(t, 0.085f, 0.085f, 0.025f, Mats.Std(new Color(0.4f, 0.22f, 0.12f), 0.7f), Vector3.zero);
                    Props.Cyl(t, 0.088f, 0.088f, 0.008f, Mats.Std(new Color(0.95f, 0.8f, 0.2f), 0.6f), new Vector3(0, 0.017f, 0));
                    Props.Cyl(t, 0.09f, 0.09f, 0.008f, Mats.Std(new Color(0.3f, 0.7f, 0.25f), 0.8f), new Vector3(0, 0.025f, 0));
                    Props.Sphere(t, 0.082f, Mats.Std(new Color(0.88f, 0.62f, 0.3f), 0.7f), new Vector3(0, 0.035f, 0), 12, 6);
                    for (int i = 0; i < 5; i++)
                        Props.Box(t, new Vector3(0.012f, 0.012f, 0.08f), Mats.Std(new Color(0.98f, 0.82f, 0.3f), 0.7f), new Vector3(0.11f, -0.03f, -0.06f + i * 0.03f), new Vector3(0, i * 12f, 0), 0f);
                    break;
                }
            }
            return root;
        }

        /// <summary>Markenlogo als flache 3D-Form (liegt auf einer Oberfläche, Oberseite = +Y).</summary>
        public static GameObject LogoMesh(Transform parent, int index, Color color, float size)
        {
            var n = Props.Node(parent, "Logo");
            var t = n.transform;
            var m = Mats.Std(color, 0.4f);
            const float th = 0.006f;
            switch (index)
            {
                case 0:
                    Props.Cyl(t, size * 0.5f, size * 0.5f, th, m, Vector3.zero, default, 20);
                    break;
                case 1:
                    Props.Box(t, new Vector3(size * 0.85f, th, size * 0.85f), m, Vector3.zero, default, 0f, false);
                    break;
                case 2:
                    Props.Box(t, new Vector3(size * 0.7f, th, size * 0.7f), m, Vector3.zero, new Vector3(0, 45, 0), 0f, false);
                    break;
                case 3:
                    Props.Box(t, new Vector3(size * 0.62f, th, size * 0.62f), m, Vector3.zero, default, 0f, false);
                    Props.Box(t, new Vector3(size * 0.62f, th, size * 0.62f), m, Vector3.zero, new Vector3(0, 45, 0), 0f, false);
                    break;
                case 4:
                    Props.Box(t, new Vector3(size * 0.22f, th, size * 0.6f), m, new Vector3(size * 0.08f, 0, -size * 0.18f), new Vector3(0, 25, 0), 0f, false);
                    Props.Box(t, new Vector3(size * 0.22f, th, size * 0.6f), m, new Vector3(-size * 0.08f, 0, size * 0.18f), new Vector3(0, 25, 0), 0f, false);
                    break;
                default:
                    Props.Cyl(t, size * 0.25f, size * 0.25f, th, m, new Vector3(-size * 0.17f, 0, -size * 0.1f), default, 16);
                    Props.Cyl(t, size * 0.25f, size * 0.25f, th, m, new Vector3(size * 0.17f, 0, -size * 0.1f), default, 16);
                    Props.Box(t, new Vector3(size * 0.5f, th, size * 0.5f), m, new Vector3(0, 0, size * 0.1f), new Vector3(0, 45, 0), 0f, false);
                    break;
            }
            return n;
        }
    }
}
