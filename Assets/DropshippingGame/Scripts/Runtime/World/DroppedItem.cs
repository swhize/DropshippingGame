using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Ein abgelegter Gegenstand mit echter Physik (fällt, kippt, lässt sich anstoßen).
    /// Behält alle Werte, kann wieder aufgenommen werden und wird mitgespeichert.
    /// </summary>
    public sealed class DroppedItem : MonoBehaviour, IInteractable
    {
        public ItemData Data;
        public Rigidbody Body;
        private readonly Highlighter _hl = new Highlighter();

        public static readonly List<DroppedItem> All = new List<DroppedItem>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        public static DroppedItem Spawn(Transform parent, ItemData data, Vector3 pos, float rotY)
        {
            var go = new GameObject("Dropped " + data.Kind);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, rotY, 0);
            var d = go.AddComponent<DroppedItem>();
            d.Setup(data);
            return d;
        }

        private void Setup(ItemData data)
        {
            Data = data.Clone();
            var size = ItemKit.Bounds(Data);
            var visual = ItemKit.Build(transform, Data);
            _hl.Collect(visual.transform);
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = size;
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = Data.Kind == ItemKind.Crate ? 8f : 1.5f;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            All.Add(this);
        }

        private void OnDestroy() => All.Remove(this);

        public string Title => Data.Describe();

        public string Prompt(PlayerController player) => player.Held != null ? "Hände voll" : "Aufheben: " + Data.Describe();

        public void Interact(PlayerController player)
        {
            if (player.Held != null)
            {
                Game.Notify("Hände sind schon voll.", "info");
                return;
            }
            player.Hold(Data);
            Game.Sound("pickup");
            Destroy(gameObject);
        }

        public void SetHighlighted(bool on) => _hl.Set(on);

        public WorldItemSave ToSave() => new WorldItemSave { Item = Data.Clone(), Pos = transform.position.ToV3(), RotY = transform.eulerAngles.y };
    }
}
