using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Kompass oben mittig (abschaltbar: Einstellungen › Gameplay): Himmelsrichtungen und Markierungen
    /// für wichtige Orte – Garage/Lagerhalle, nächste Packstation, PaketBlitz-Filiale, Bank, Börse,
    /// MediaMarkd, Fressnix – sowie das aktuelle Ziel (gleiche Station wie der Zielmarker in der Welt).
    /// Norden = +Z, Osten = +X (Einkaufsviertel im Westen, Finanzviertel im Osten).
    /// </summary>
    public sealed class HudCompass
    {
        private const float Width = 600f;
        private const float HalfFov = 90f;
        private const float PxPerDeg = Width / 2f / HalfFov;

        private sealed class Mark
        {
            public string Name, Icon;
            public Color Tint;
            public Func<Vector3?> Pos;
            /// <summary>Am Rand festhalten statt ausblenden (Ziel, Packstation).</summary>
            public bool Clamp;
            public float HideWithin;
            public VisualElement El;
            public Label Dist;
            public Vector3? Cached;
        }

        private readonly VisualElement _root, _layer;
        private readonly Label _name;
        private readonly List<VisualElement> _ticks = new List<VisualElement>();
        private readonly List<float> _tickDeg = new List<float>();
        private readonly List<Mark> _marks = new List<Mark>();
        private Mark _goal;
        private float _retarget;
        private float _lastYaw = 9999f;
        private Vector3 _lastPos = new Vector3(9999f, 0, 0);

        public HudCompass(VisualElement layer)
        {
            _layer = layer;
            _root = UIX.Div(layer, "compass");
            _root.pickingMode = PickingMode.Ignore;
            for (int d = 0; d < 360; d += 15)
            {
                VisualElement el;
                string letter = Letter(d);
                if (letter != null) el = UIX.Text(_root, letter, "compass-letter");
                else el = UIX.Div(_root, "compass-tick");
                if (letter == "N") el.style.color = Theme.Accent;
                _ticks.Add(el);
                _tickDeg.Add(d);
            }
            UIX.Div(_root, "compass-center");

            AddMark("Garage", "home", new Color(1f, 0.74f, 0.26f), () => Home(), false, 14f);
            AddMark("Packstation", "package", new Color(0.98f, 0.8f, 0.12f), NearestPackstation, true, 3f);
            AddMark("PaketBlitz-Filiale", "truck", new Color(0.98f, 0.8f, 0.12f), () => new Vector3(PostService.PostDoorX, 0, -9f), false, 6f);
            AddMark("Bank", "bank", new Color(0.55f, 0.75f, 1f), () => new Vector3(71f, 0, 12f), false, 6f);
            AddMark("Börse", "trend", new Color(0.55f, 0.75f, 1f), () => new Vector3(96f, 0, 12f), false, 6f);
            AddMark("MediaMarkd", "screen", new Color(1f, 0.45f, 0.4f), () => new Vector3(-78.5f, 0, -9f), false, 6f);
            AddMark("Fressnix", "heart", new Color(0.45f, 0.85f, 0.55f), () => new Vector3(-107.5f, 0, -9f), false, 6f);
            AddMark("Moped", "bolt", new Color(1f, 0.6f, 0.25f), Vehicles.Moped.CompassPos, false, 8f);
            _goal = AddMark("Ziel", "flag", new Color(1f, 0.85f, 0.3f), () => null, true, 2.5f);
            _goal.El.AddToClassList("compass-goal");

            _name = UIX.Text(layer, "", "compass-name");
            _name.pickingMode = PickingMode.Ignore;
            foreach (var el in _root.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
            UIX.Show(_root, false);
            UIX.Show(_name, false);
        }

        private static string Letter(int d)
        {
            switch (d)
            {
                case 0: return "N";
                case 45: return "NO";
                case 90: return "O";
                case 135: return "SO";
                case 180: return "S";
                case 225: return "SW";
                case 270: return "W";
                case 315: return "NW";
            }
            return null;
        }

        private Mark AddMark(string name, string icon, Color tint, Func<Vector3?> pos, bool clamp, float hideWithin)
        {
            var m = new Mark { Name = name, Icon = icon, Tint = tint, Pos = pos, Clamp = clamp, HideWithin = hideWithin };
            m.El = UIX.Col(_root, 0f, "compass-mark");
            UIX.Icon(m.El, Icons.Has(icon) ? icon : "dot", 16f, tint);
            m.Dist = UIX.Text(m.El, "", "compass-dist");
            _marks.Add(m);
            return m;
        }

        private static Vector3? Home()
        {
            var sim = Game.Sim;
            if (sim == null) return null;
            return sim.LocationStage >= 1 ? WorldBuilder.SpawnWarehouse : WorldBuilder.SpawnGarage;
        }

        private static Vector3? NearestPackstation()
        {
            var p = Game.Player;
            if (p == null) return null;
            var pos = p.transform.position;
            Vector3? best = null;
            float bestD = float.MaxValue;
            foreach (var ps in PostService.Packstations)
            {
                float d = (ps.pos - pos).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = ps.pos;
                }
            }
            return best;
        }

        public void Tick(float dt)
        {
            var sim = Game.Sim;
            var player = Game.Player;
            bool show = Settings.ShowCompass && sim != null && player != null && sim.InGame && sim.StoryStage == "business" &&
                        !(Game.World != null && Game.World.MenuMode);
            if (UIX.IsShown(_root) != show)
            {
                UIX.Show(_root, show);
                if (!show) UIX.Show(_name, false);
                _lastYaw = 9999f;
            }
            if (!show) return;

            _retarget -= dt;
            if (_retarget <= 0f)
            {
                _retarget = 0.35f;
                foreach (var m in _marks)
                    if (m != _goal)
                        m.Cached = m.Pos();
                Station target = null;
                try
                {
                    target = ObjectiveMarker.FindTarget(sim, player);
                }
                catch (Exception)
                {
                    target = null;
                }
                _goal.Cached = target != null ? target.transform.position : (Vector3?)null;
                _goal.Name = target != null ? "Ziel: " + target.Title : "Ziel";
                _lastYaw = 9999f;
            }

            float yaw = player.Cam != null ? player.Cam.transform.eulerAngles.y : player.transform.eulerAngles.y;
            var pos = player.transform.position;
            if (Mathf.Abs(Mathf.DeltaAngle(yaw, _lastYaw)) < 0.15f && (pos - _lastPos).sqrMagnitude < 0.04f) return;
            _lastYaw = yaw;
            _lastPos = pos;

            for (int i = 0; i < _ticks.Count; i++)
            {
                float rel = Mathf.DeltaAngle(yaw, _tickDeg[i]);
                bool vis = Mathf.Abs(rel) <= HalfFov + 2f;
                _ticks[i].style.display = vis ? DisplayStyle.Flex : DisplayStyle.None;
                if (vis) _ticks[i].style.left = Width / 2f + rel * PxPerDeg;
            }

            Mark centered = null;
            float centeredAbs = 7f;
            foreach (var m in _marks)
            {
                if (m.Cached == null)
                {
                    m.El.style.display = DisplayStyle.None;
                    continue;
                }
                var t = m.Cached.Value;
                var d = new Vector2(t.x - pos.x, t.z - pos.z);
                float dist = d.magnitude;
                if (dist < m.HideWithin)
                {
                    m.El.style.display = DisplayStyle.None;
                    continue;
                }
                float bearing = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                float rel = Mathf.DeltaAngle(yaw, bearing);
                bool outside = Mathf.Abs(rel) > HalfFov - 4f;
                if (outside && !m.Clamp)
                {
                    m.El.style.display = DisplayStyle.None;
                    continue;
                }
                m.El.style.display = DisplayStyle.Flex;
                float r = Mathf.Clamp(rel, -(HalfFov - 4f), HalfFov - 4f);
                m.El.style.left = Width / 2f + r * PxPerDeg;
                m.El.style.opacity = outside ? 0.5f : 1f;
                m.Dist.text = dist >= 1000f ? Fmt.Dec(dist / 1000f, 1) + " km" : Mathf.RoundToInt(dist) + " m";
                if (!outside && Mathf.Abs(rel) < centeredAbs)
                {
                    centeredAbs = Mathf.Abs(rel);
                    centered = m;
                }
            }
            // Ziel immer vorne zeichnen
            _goal.El.BringToFront();
            if (centered != null)
            {
                if (_name.text != centered.Name) _name.text = centered.Name;
                UIX.Show(_name, true);
            }
            else UIX.Show(_name, false);
        }
    }

    /// <summary>
    /// Untertitel (Einstellungen › Barrierefreiheit): gesprochene Zeilen der Figuren in der Nähe
    /// erscheinen zusätzlich unten mittig, mit Namen der Figur.
    /// </summary>
    public sealed class HudSubtitles
    {
        private const float Range = 14f;
        private readonly VisualElement _root;
        private readonly Label _text;
        private float _until;

        public HudSubtitles(VisualElement layer)
        {
            _root = UIX.Div(layer, "subtitle");
            var box = UIX.Div(_root, "subtitle-box");
            _text = UIX.Text(box, "", "subtitle-text");
            _text.enableRichText = true;
            _root.pickingMode = PickingMode.Ignore;
            box.pickingMode = PickingMode.Ignore;
            _text.pickingMode = PickingMode.Ignore;
            UIX.Show(_root, false);
            NPC.Spoke += OnSpoke;
        }

        private void OnSpoke(NPC npc, string line, float duration)
        {
            if (!Settings.Subtitles || npc == null || string.IsNullOrEmpty(line)) return;
            var p = Game.Player;
            if (p == null || (npc.transform.position - p.transform.position).sqrMagnitude > Range * Range) return;
            string who = string.IsNullOrEmpty(npc.DisplayName) ? "Passant:in" : npc.DisplayName;
            _text.text = "<color=" + Theme.Hex(Theme.Accent) + "><b>" + UiFmt.Safe(who) + ":</b></color> " + UiFmt.Safe(line);
            _until = Time.unscaledTime + Mathf.Clamp(duration, 2f, 8f) + 0.4f;
            UIX.Show(_root, true);
        }

        public void Tick(float dt)
        {
            if (!UIX.IsShown(_root)) return;
            var ui = Game.UI;
            bool covered = ui != null && ((ui.Dialogue != null && ui.Dialogue.Active) || (ui.Pause != null && ui.Pause.IsOpen));
            if (Time.unscaledTime > _until || !Settings.Subtitles || covered) UIX.Show(_root, false);
        }
    }
}
