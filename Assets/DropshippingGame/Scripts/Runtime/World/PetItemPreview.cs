using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Admin: stellt alle Tierbedarf-Modelle (Fressnix + Deko) auf Sockeln vor den Spieler (Sichtprüfung).</summary>
    public static class PetItemPreview
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
            var basePos = pt.position + fwd * 2.2f;
            if (Physics.Raycast(basePos + Vector3.up * 1.5f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore))
                basePos.y = hit.point.y;
            _root = new GameObject("PetItemPreview");
            _root.transform.position = basePos;
            _root.transform.rotation = Quaternion.LookRotation(-fwd, Vector3.up);
            var ids = new List<string>(PetItemModels.ShopIds());
            ids.AddRange(PetItemModels.Extras);
            ids.Add("napf@1");
            ids.Add("napf@0");
            var ped = Mats.Std(new Color(0.95f, 0.95f, 0.95f), 0.5f);
            const int perRow = 7;
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                int row = i / perRow, col = i % perRow;
                float x = (col - (perRow - 1) / 2f) * 0.6f;
                float h = 0.6f + row * 0.35f;
                var node = Props.Node(_root.transform, id, new Vector3(x, 0f, -row * 0.7f));
                Props.Box(node.transform, new Vector3(0.5f, h, 0.5f), ped, new Vector3(0, h / 2f, 0), default, 0.01f);
                // Große Teile (Kratzbaum, Bett, Katzenklo) verkleinert, Rest in echter Größe
                float nat = PetItemModels.NaturalSize(id);
                var m = PetItemModels.Build(node.transform, id, nat > 0.48f ? 0.48f : 0f, true);
                m.transform.localPosition = new Vector3(0, h, 0);
                var it = ShopData.Item(id);
                string label = it != null ? it.Short : id;
                Label3D.Create(node.transform, label, 26f, Color.white, new Vector3(0, h + 0.6f, 0), true, 6f);
            }
        }
    }
}
