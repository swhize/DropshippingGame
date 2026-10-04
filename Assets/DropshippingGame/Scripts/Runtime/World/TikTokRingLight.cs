using DropshippingGame.Core;
using DropshippingGame.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// Kleines Ringlicht auf dem Schreibtisch („Creator-Ecke“): anvisieren + E öffnet die
    /// TikTok-Auswahl (<see cref="TikTokStudio.OpenPicker"/>). Wird von <see cref="Ensure"/>
    /// (GameRoot, 1× pro Sekunde) an jede Laptop-/PC-Station gehängt, sobald TikTok freigeschaltet
    /// ist – als Kind der Station, wandert es beim Verschieben des Schreibtischs mit und wird beim
    /// Neuaufbau der Stationen automatisch wieder aufgestellt. Nur Trigger-Collider (blockiert
    /// weder Spieler noch NPCs) und kein eigenes Licht (nur leuchtendes Material).
    /// </summary>
    public sealed class TikTokRingLight : MonoBehaviour, IInteractable
    {
        private readonly Highlighter _hl = new Highlighter();

        /// <summary>Stellt Ringlichter auf bzw. entfernt sie (Level, Story).</summary>
        public static void Ensure()
        {
            var sim = Game.Sim;
            bool want = sim != null && sim.InGame && sim.StoryStage == "business" && sim.Level >= GameData.TikTokLevel;
            foreach (var st in Station.All)
            {
                if (st == null || st.Type != StationType.Pc) continue;
                var existing = st.GetComponentInChildren<TikTokRingLight>(true);
                if (want && existing == null) Create(st);
                else if (!want && existing != null) Destroy(existing.gameObject);
            }
        }

        private static void Create(Station st)
        {
            // Tischhöhe laut StationKit.Pc: Garage 0,92 m, Lagerhalle/Büro 0,76 m.
            float top = st.Opts != null && st.Opts.Stage == 0 ? 0.925f : 0.765f;
            var go = new GameObject("TikTokRingLight");
            go.transform.SetParent(st.transform, false);
            go.transform.localPosition = new Vector3(-0.66f, top, -0.16f);
            go.transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
            var ring = go.AddComponent<TikTokRingLight>();
            ring.Build();
        }

        private void Build()
        {
            var t = transform;
            var metal = Mats.DarkMetal();
            Props.Cyl(t, 0.06f, 0.075f, 0.02f, metal, new Vector3(0f, 0.01f, 0f));
            Props.Box(t, new Vector3(0.016f, 0.36f, 0.016f), metal, new Vector3(0f, 0.19f, 0f), default, 0f, false);
            var glow = Mats.Emit(new Color(1f, 0.96f, 0.9f), 2.4f);
            const int seg = 12;
            const float radius = 0.1f;
            const float cy = 0.47f;
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var p = new Vector3(Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius, 0f);
                var b = Props.Box(t, new Vector3(0.056f, 0.018f, 0.018f), glow, p, new Vector3(0f, 0f, a * Mathf.Rad2Deg + 90f), 0f, false);
                var r = b != null ? b.GetComponent<MeshRenderer>() : null;
                if (r != null) r.shadowCastingMode = ShadowCastingMode.Off;
            }
            // Handyhalter in der Mitte
            Props.Box(t, new Vector3(0.05f, 0.09f, 0.008f), Mats.Std(new Color(0.06f, 0.06f, 0.07f), 0.3f), new Vector3(0f, cy, 0.012f), default, 0.003f, false);
            var col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.28f, 0.6f, 0.16f);
            col.center = new Vector3(0f, 0.3f, 0f);
            _hl.Collect(t);
        }

        public string Title => "Ringlicht (Creator-Ecke)";

        public string Prompt(PlayerController player)
        {
            var sim = Game.Sim;
            if (sim == null) return "";
            string block = TikTokStudio.RecordBlocker(sim);
            return block == null ? "TikTok aufnehmen" : "TikTok: " + block;
        }

        public void Interact(PlayerController player)
        {
            var sim = Game.Sim;
            if (sim == null) return;
            if (player != null && player.Recorder.Active) return;
            TikTokStudio.OpenPicker();
        }

        public void SetHighlighted(bool on) => _hl.Set(on);
    }
}
