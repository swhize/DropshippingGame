using System;
using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Ein Mülleimer der Stadt (oder der Altpapier-Container der Firma).</summary>
    public sealed class TrashBinInfo
    {
        public int Id;
        public Transform Node;
        public Transform Overflow;
        /// <summary>true = Altpapier-Container der Firma (Garage/Lagerhalle).</summary>
        public bool Business;
        /// <summary>Füllstand 0..1 (andere Systeme dürfen per <see cref="CityServices.AddFill"/> befüllen).</summary>
        public float Fill;
        public Vector3 Position => Node != null ? Node.position : Vector3.zero;
    }

    /// <summary>Daten einer Leerung (Hook für andere Systeme, z. B. Verpackungsmüll in der Core).</summary>
    public struct GarbageCollection
    {
        public TrashBinInfo Bin;
        /// <summary>Geleerter Füllstand (0..1) vor der Leerung.</summary>
        public float Amount;
        public bool Business;
    }

    /// <summary>
    /// Laufzeit-Hooks der Stadt: Mülleimer, Leerungs-Ereignis der Müllabfuhr, Laternenpositionen.
    /// <code>CityServices.GarbageCollected += c => { if (c.Business) ... };</code>
    /// </summary>
    public static class CityServices
    {
        public static readonly List<TrashBinInfo> Bins = new List<TrashBinInfo>();
        public static readonly List<Vector3> LampPoints = new List<Vector3>();

        /// <summary>Die Müllabfuhr hat einen Eimer geleert.</summary>
        public static event Action<GarbageCollection> GarbageCollected;
        /// <summary>Die Müllabfuhr hat ihre Runde beendet (Anzahl geleerter Eimer).</summary>
        public static event Action<int> GarbageRoundFinished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Bins.Clear();
            LampPoints.Clear();
            GarbageCollected = null;
            GarbageRoundFinished = null;
        }

        public static void ClearBins()
        {
            Bins.Clear();
            LampPoints.Clear();
        }

        public static TrashBinInfo RegisterBin(Transform node, Transform overflow, bool business)
        {
            var b = new TrashBinInfo { Id = Bins.Count + 1, Node = node, Overflow = overflow, Business = business, Fill = UnityEngine.Random.Range(0.2f, 0.9f) };
            Bins.Add(b);
            UpdateVisual(b);
            return b;
        }

        public static void AddFill(TrashBinInfo bin, float amount)
        {
            if (bin == null) return;
            bin.Fill = Mathf.Clamp01(bin.Fill + amount);
            UpdateVisual(bin);
        }

        public static TrashBinInfo NearestBin(Vector3 pos, bool businessOnly = false)
        {
            TrashBinInfo best = null;
            float bd = float.MaxValue;
            foreach (var b in Bins)
            {
                if (b.Node == null || (businessOnly && !b.Business)) continue;
                float d = (b.Position - pos).sqrMagnitude;
                if (d < bd) { bd = d; best = b; }
            }
            return best;
        }

        public static void UpdateVisual(TrashBinInfo b)
        {
            if (b == null || b.Overflow == null) return;
            b.Overflow.gameObject.SetActive(b.Fill > 0.7f);
            b.Overflow.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.2f, Mathf.InverseLerp(0.7f, 1f, b.Fill));
        }

        internal static void RaiseCollected(TrashBinInfo bin, float amount)
        {
            try { GarbageCollected?.Invoke(new GarbageCollection { Bin = bin, Amount = amount, Business = bin != null && bin.Business }); }
            catch (Exception e) { Debug.LogException(e); }
        }

        internal static void RaiseRoundFinished(int n)
        {
            try { GarbageRoundFinished?.Invoke(n); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
