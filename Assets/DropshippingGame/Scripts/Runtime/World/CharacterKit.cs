using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Aussehen einer Figur.</summary>
    public sealed class Look
    {
        public Color Skin = CharacterKit.SkinTones[0];
        public Color Hair = CharacterKit.HairColors[0];
        public Color Shirt = CharacterKit.Shirts[0];
        public Color Pants = CharacterKit.PantsColors[0];
        public bool LongHair, Apron, Vest, Mustache, Cap, Sitting, ChefHat;
        public Color CapColor = new Color(0.85f, 0.2f, 0.2f);
        /// <summary>Modell-ID (character.*) oder null = automatisch aus dem Aussehen; "" = immer prozedural.</summary>
        public string ModelId;
        /// <summary>Zufallswert für die Modellwahl (stabil pro Figur).</summary>
        public int Seed;
    }

    /// <summary>Gelenke einer Figur für die Animation.</summary>
    public sealed class CharacterRig
    {
        public Transform Root, HipL, HipR, KneeL, KneeR, ArmL, ArmR, Torso, Head;
        public bool Sitting;
    }

    /// <summary>
    /// Low-Poly-Figuren (Schedule-I-Stil) aus abgerundeten Blöcken: Beine mit Knie, Arme, Kopf, Haare,
    /// optional Schürze / Warnweste / Kappe / Schnurrbart / Kochmütze. Blickrichtung ist +Z.
    /// </summary>
    public static class CharacterKit
    {
        public static readonly Color[] SkinTones = { new Color(0.95f, 0.8f, 0.66f), new Color(0.87f, 0.68f, 0.52f), new Color(0.72f, 0.52f, 0.38f), new Color(0.5f, 0.35f, 0.25f) };
        public static readonly Color[] HairColors = { new Color(0.12f, 0.09f, 0.07f), new Color(0.35f, 0.22f, 0.12f), new Color(0.75f, 0.6f, 0.35f), new Color(0.55f, 0.2f, 0.1f), new Color(0.6f, 0.6f, 0.62f) };
        public static readonly Color[] Shirts =
        {
            new Color(0.2f, 0.35f, 0.65f), new Color(0.7f, 0.2f, 0.22f), new Color(0.25f, 0.55f, 0.35f), new Color(0.85f, 0.75f, 0.3f),
            new Color(0.4f, 0.4f, 0.45f), new Color(0.6f, 0.35f, 0.65f), new Color(0.9f, 0.9f, 0.9f), new Color(0.15f, 0.15f, 0.17f),
        };
        public static readonly Color[] PantsColors = { new Color(0.18f, 0.22f, 0.35f), new Color(0.2f, 0.2f, 0.22f), new Color(0.4f, 0.33f, 0.25f), new Color(0.3f, 0.32f, 0.38f) };

        public static Look RandomLook(System.Random rng)
        {
            return new Look
            {
                Skin = SkinTones[rng.Next(SkinTones.Length)],
                Hair = HairColors[rng.Next(HairColors.Length)],
                Shirt = Shirts[rng.Next(Shirts.Length)],
                Pants = PantsColors[rng.Next(PantsColors.Length)],
                LongHair = rng.NextDouble() < 0.4,
                Seed = rng.Next(0, 100000),
            };
        }

        private static readonly string[] Passersby =
        {
            "character.female_a", "character.female_b", "character.female_c", "character.female_d", "character.female_e",
            "character.male_a", "character.male_c", "character.male_d", "character.male_f",
        };

        /// <summary>
        /// Passendes animiertes Modell (Kenney Mini Characters) für ein Aussehen: Kochmütze = Kalle,
        /// Warnweste = Lagerpersonal, sonst Passant/Kundin nach Seed. null = prozedural bauen.
        /// </summary>
        public static string ModelFor(Look o)
        {
            if (o == null) return null;
            if (o.ModelId != null) return o.ModelId.Length == 0 ? null : o.ModelId;
            if (o.ChefHat || o.Apron) return "character.cook";
            if (o.Vest) return (o.Seed & 1) == 0 ? "character.worker_1" : "character.worker_2";
            int seed = o.Seed != 0 ? o.Seed : Mathf.Abs(Mathf.RoundToInt(o.Shirt.r * 997f + o.Hair.g * 577f + o.Skin.b * 331f + o.Pants.r * 173f));
            return Passersby[seed % Passersby.Length];
        }

        /// <summary>
        /// Baut das animierte Modell einer Figur (Front +Z, Füße auf 0). null, wenn es fehlt oder
        /// (bei sitzenden Figuren) keinen Sitz-Clip hat - dann bleibt die prozedurale Figur.
        /// </summary>
        public static GameObject BuildModel(Transform parent, Look o)
        {
            string id = ModelFor(o);
            if (id == null || !Props.UseAssets || !AssetLib.HasModel(id)) return null;
            var go = AssetLib.Model(id, parent, Vector3.zero, 0f, -1f, false, true);
            if (go == null) return null;
            bool ok = o.Sitting ? AssetLib.PlayAnim(go, "sit", 0f) : AssetLib.PlayAnim(go, "idle", 0f);
            if (o.Sitting && !ok)
            {
                Object.Destroy(go);
                return null;
            }
            if (o.Sitting) go.transform.localPosition = new Vector3(0f, 0.05f, -0.1f);
            return go;
        }

        public static CharacterRig Build(Transform parent, Look o)
        {
            var root = Props.Node(parent, "Character");
            var rig = new CharacterRig { Root = root.transform };
            var skin = Mats.Std(o.Skin, 0.7f);
            var shirt = Mats.Std(o.Shirt, 0.8f);
            var pants = Mats.Std(o.Pants, 0.85f);
            var hair = Mats.Std(o.Hair, 0.9f);
            var shoe = Mats.Std(new Color(0.1f, 0.1f, 0.11f), 0.6f);

            for (int side = -1; side <= 1; side += 2)
            {
                var hip = Props.Node(root.transform, "Hip", new Vector3(0.1f * side, 0.92f, 0));
                Props.Box(hip.transform, new Vector3(0.16f, 0.46f, 0.18f), pants, new Vector3(0, -0.23f, 0), default, 0.03f);
                var knee = Props.Node(hip.transform, "Knee", new Vector3(0, -0.46f, 0));
                Props.Box(knee.transform, new Vector3(0.15f, 0.42f, 0.16f), pants, new Vector3(0, -0.21f, 0), default, 0.03f);
                Props.Box(knee.transform, new Vector3(0.16f, 0.08f, 0.27f), shoe, new Vector3(0, -0.43f, 0.04f), default, 0.02f);
                if (side < 0)
                {
                    rig.HipL = hip.transform;
                    rig.KneeL = knee.transform;
                }
                else
                {
                    rig.HipR = hip.transform;
                    rig.KneeR = knee.transform;
                }
            }

            var torso = Props.Node(root.transform, "Torso", new Vector3(0, 0.92f, 0));
            rig.Torso = torso.transform;
            Props.Box(torso.transform, new Vector3(0.44f, 0.62f, 0.25f), shirt, new Vector3(0, 0.31f, 0), default, 0.05f);
            if (o.Apron) Props.Box(torso.transform, new Vector3(0.42f, 0.72f, 0.02f), Mats.Std(new Color(0.95f, 0.95f, 0.93f), 0.8f), new Vector3(0, 0.18f, 0.135f), default, 0f);
            if (o.Vest)
            {
                Props.Box(torso.transform, new Vector3(0.46f, 0.5f, 0.27f), Mats.Std(new Color(1f, 0.55f, 0.1f), 0.6f), new Vector3(0, 0.36f, 0), default, 0.04f);
                Props.Box(torso.transform, new Vector3(0.47f, 0.05f, 0.28f), Mats.Emit(new Color(0.9f, 0.95f, 0.9f), 0.6f), new Vector3(0, 0.3f, 0), default, 0f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = Props.Node(torso.transform, "Shoulder", new Vector3(0.29f * side, 0.58f, 0));
                Props.Box(shoulder.transform, new Vector3(0.12f, 0.58f, 0.14f), shirt, new Vector3(0, -0.29f, 0), default, 0.03f);
                Props.Box(shoulder.transform, new Vector3(0.11f, 0.11f, 0.12f), skin, new Vector3(0, -0.63f, 0), default, 0.03f);
                if (side < 0) rig.ArmL = shoulder.transform;
                else rig.ArmR = shoulder.transform;
            }

            var head = Props.Node(torso.transform, "Head", new Vector3(0, 0.7f, 0));
            rig.Head = head.transform;
            var ht = head.transform;
            Props.Box(ht, new Vector3(0.1f, 0.08f, 0.1f), skin, new Vector3(0, 0.02f, 0));
            Props.Box(ht, new Vector3(0.27f, 0.29f, 0.26f), skin, new Vector3(0, 0.2f, 0), default, 0.05f);
            var eye = Mats.Std(new Color(0.08f, 0.08f, 0.1f), 0.4f);
            Props.Box(ht, new Vector3(0.04f, 0.045f, 0.01f), eye, new Vector3(-0.065f, 0.23f, 0.131f), default, 0f, false);
            Props.Box(ht, new Vector3(0.04f, 0.045f, 0.01f), eye, new Vector3(0.065f, 0.23f, 0.131f), default, 0f, false);
            Props.Box(ht, new Vector3(0.28f, 0.08f, 0.28f), hair, new Vector3(0, 0.37f, -0.005f), default, 0.03f);
            Props.Box(ht, new Vector3(0.28f, 0.16f, 0.06f), hair, new Vector3(0, 0.28f, -0.12f), default, 0.02f);
            if (o.LongHair) Props.Box(ht, new Vector3(0.3f, 0.32f, 0.08f), hair, new Vector3(0, 0.16f, -0.13f), default, 0.03f);
            if (o.Mustache) Props.Box(ht, new Vector3(0.14f, 0.035f, 0.02f), hair, new Vector3(0, 0.14f, 0.135f), default, 0f, false);
            if (o.Cap)
            {
                var cap = Mats.Std(o.CapColor, 0.7f);
                Props.Box(ht, new Vector3(0.29f, 0.09f, 0.29f), cap, new Vector3(0, 0.39f, 0), default, 0.03f);
                Props.Box(ht, new Vector3(0.24f, 0.02f, 0.14f), cap, new Vector3(0, 0.35f, 0.19f), default, 0f);
            }
            if (o.ChefHat)
            {
                var white = Mats.Std(new Color(0.98f, 0.98f, 0.97f), 0.8f);
                Props.Box(ht, new Vector3(0.26f, 0.22f, 0.26f), white, new Vector3(0, 0.48f, 0), default, 0.06f);
                Props.Box(ht, new Vector3(0.3f, 0.06f, 0.3f), white, new Vector3(0, 0.37f, 0), default, 0.02f);
            }
            if (o.Sitting) SetSitting(rig, true);
            return rig;
        }

        public static void SetSitting(CharacterRig rig, bool sitting)
        {
            float hip = sitting ? -90f : 0f;
            float knee = sitting ? 90f : 0f;
            rig.HipL.localRotation = Quaternion.Euler(hip, 0, 0);
            rig.HipR.localRotation = Quaternion.Euler(hip, 0, 0);
            rig.KneeL.localRotation = Quaternion.Euler(knee, 0, 0);
            rig.KneeR.localRotation = Quaternion.Euler(knee, 0, 0);
            rig.Root.localPosition = new Vector3(rig.Root.localPosition.x, sitting ? -0.46f : 0f, rig.Root.localPosition.z);
            rig.Sitting = sitting;
        }

        /// <summary>Lauf-/Idle-Animation. phase läuft mit der Zeit, amount 0 = stehen, 1 = voll laufen.</summary>
        public static void Animate(CharacterRig rig, float phase, float amount, float t)
        {
            if (rig == null || rig.HipL == null) return;
            float swing = Mathf.Sin(phase) * 0.55f * amount * Mathf.Rad2Deg;
            if (!rig.Sitting)
            {
                rig.HipL.localRotation = Quaternion.Euler(swing, 0, 0);
                rig.HipR.localRotation = Quaternion.Euler(-swing, 0, 0);
                rig.KneeL.localRotation = Quaternion.Euler(Mathf.Max(0f, -Mathf.Sin(phase)) * 0.8f * amount * Mathf.Rad2Deg, 0, 0);
                rig.KneeR.localRotation = Quaternion.Euler(Mathf.Max(0f, Mathf.Sin(phase)) * 0.8f * amount * Mathf.Rad2Deg, 0, 0);
            }
            float breathe = Mathf.Sin(t * 1.3f) * 0.03f * Mathf.Rad2Deg;
            rig.ArmL.localRotation = Quaternion.Euler(-swing * 0.8f + breathe, 0, 0);
            rig.ArmR.localRotation = Quaternion.Euler(swing * 0.8f - breathe, 0, 0);
            rig.Torso.localPosition = new Vector3(0, 0.92f + Mathf.Abs(Mathf.Sin(phase)) * 0.04f * amount + Mathf.Sin(t * 2f) * 0.004f, 0);
        }
    }
}
