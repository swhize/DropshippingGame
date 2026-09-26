using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Markiert ein Möbelstück / eine Station, die der Spieler im Verschiebe-Modus (Taste B) umstellen kann.
    /// Kennt ihren Speicher-Schlüssel, den Raum (Ausbaustufe) und ihre Grundfläche.
    /// </summary>
    public sealed class Movable : MonoBehaviour
    {
        public string Key = "";
        public string Label = "";
        /// <summary>0 = Garage, 1 = Lagerhalle.</summary>
        public int Stage;
        /// <summary>Flache Objekte (Teppich) dürfen unter anderen liegen.</summary>
        public bool Flat;
        public Vector3 LocalCenter;
        public Vector3 LocalSize = Vector3.one;

        public static readonly List<Movable> All = new List<Movable>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        /// <summary>
        /// Richtet das Objekt ein. Ohne explizite Größe werden die Grenzen der Renderer benutzt.
        /// addCollider: fügt einen Box-Collider hinzu, damit man das Objekt anvisieren kann (Deko hat oft keinen).
        /// </summary>
        public void Setup(string key, string label, int stage, bool flat, Vector3? size = null, Vector3? center = null, bool addCollider = false)
        {
            Key = key ?? "";
            Label = label ?? "";
            Stage = stage;
            Flat = flat;
            if (size.HasValue)
            {
                LocalSize = size.Value;
                LocalCenter = center ?? new Vector3(0f, size.Value.y / 2f, 0f);
            }
            else
            {
                var b = Props.LocalBounds(gameObject);
                LocalSize = b.size;
                LocalCenter = b.center;
            }
            // Grundfläche nie ganz null (sonst ließe sich das Objekt überall abstellen)
            LocalSize = new Vector3(Mathf.Max(0.3f, Mathf.Abs(LocalSize.x)), Mathf.Max(0.05f, Mathf.Abs(LocalSize.y)), Mathf.Max(0.3f, Mathf.Abs(LocalSize.z)));
            if (addCollider && !HasSolidCollider())
            {
                var col = gameObject.AddComponent<BoxCollider>();
                col.center = LocalCenter;
                // Flache Teppiche: Auslöser mit etwas Höhe, damit man sie anvisieren kann, ohne darüber zu stolpern.
                col.size = Flat ? new Vector3(LocalSize.x, Mathf.Max(0.12f, LocalSize.y), LocalSize.z) : LocalSize;
                col.isTrigger = Flat;
            }
            if (!All.Contains(this)) All.Add(this);
        }

        private bool HasSolidCollider()
        {
            foreach (var c in GetComponentsInChildren<Collider>(true))
                if (c != null && !c.isTrigger) return true;
            return false;
        }

        private void OnDestroy() => All.Remove(this);

        public float Height => LocalSize.y * Mathf.Abs(transform.lossyScale.y);

        /// <summary>Grundfläche, wenn der Drehpunkt bei pos steht und um rotY gedreht ist.</summary>
        public Footprint FootprintAt(Vector3 pos, float rotY)
        {
            var s = transform.lossyScale;
            var offset = Quaternion.Euler(0f, rotY, 0f) * new Vector3(LocalCenter.x * s.x, 0f, LocalCenter.z * s.z);
            return new Footprint(pos.x + offset.x, pos.z + offset.z, LocalSize.x * Mathf.Abs(s.x) / 2f, LocalSize.z * Mathf.Abs(s.z) / 2f, rotY, Label, Flat);
        }

        public Footprint CurrentFootprint => FootprintAt(transform.position, transform.eulerAngles.y);
    }
}
