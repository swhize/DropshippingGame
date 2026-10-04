using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Umrechnung zwischen den Unity-freien Core-Typen und Unity-Typen.</summary>
    public static class Conv
    {
        public static Color ToColor(this RGBA c) => new Color(c.r, c.g, c.b, c.a);
        public static RGBA ToRGBA(this Color c) => new RGBA(c.r, c.g, c.b, c.a);
        public static Vector3 ToVector3(this V3 v) => new Vector3(v.x, v.y, v.z);
        public static V3 ToV3(this Vector3 v) => new V3(v.x, v.y, v.z);

        public static Color Hex(string hex) => RGBA.Hex(hex).ToColor();

        public static Color Darkened(this Color c, float amount) => new Color(c.r * (1f - amount), c.g * (1f - amount), c.b * (1f - amount), c.a);
        public static Color Lightened(this Color c, float amount) => new Color(c.r + (1f - c.r) * amount, c.g + (1f - c.g) * amount, c.b + (1f - c.b) * amount, c.a);
        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
