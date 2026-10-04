using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Admin: stellt alle Produktmodelle (echte Größe) auf Sockeln vor den Spieler (Sichtprüfung).</summary>
    public static class ProductPreview
    {
        private static GameObject _root;

        public static void Clear()
        {
            if (_root != null) Object.Destroy(_root);
            _root = null;
        }

        public static void SpawnAllInFrontOfPlayer()
        {
            Clear();
            var player = Game.Player;
            if (player == null) return;
            var pt = player.transform;
            var fwd = Vector3.ProjectOnPlane(player.Cam != null ? player.Cam.transform.forward : pt.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.ProjectOnPlane(pt.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();
            // Boden unter dem Spieler suchen (Pivot kann auf Augenhöhe liegen).
            var basePos = pt.position + fwd * 1.8f;
            if (Physics.Raycast(basePos + Vector3.up * 1.5f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore))
                basePos.y = hit.point.y;
            _root = new GameObject("ProductPreview");
            _root.transform.position = basePos;
            _root.transform.rotation = Quaternion.LookRotation(-fwd, Vector3.up);
            var ped = Mats.Std(new Color(0.95f, 0.95f, 0.95f), 0.5f);
            int n = GameData.Products.Length;
            for (int i = 0; i < n; i++)
            {
                var p = GameData.Products[i];
                int row = i / 7, col = i % 7;
                float x = (col - 3) * 0.42f;
                float h = 0.75f + row * 0.3f;
                var node = Props.Node(_root.transform, p.Id, new Vector3(x, 0f, -row * 0.5f));
                Props.Box(node.transform, new Vector3(0.34f, h, 0.34f), ped, new Vector3(0, h / 2f, 0), default, 0.01f);
                var m = ProductModels.Build(node.transform, p.Id, 0f, true);
                m.transform.localPosition = new Vector3(0, h, 0);
                Label3D.Create(node.transform, p.Short, 26f, Color.white, new Vector3(0, h + 0.38f, 0), true, 6f);
            }
        }
    }
}
