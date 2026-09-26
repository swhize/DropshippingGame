using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// Verschiebe-Modus: Möbelstück anvisieren, B drücken - es folgt dem Fadenkreuz auf dem Boden
    /// (Raster 0,25 m), R/Mausrad dreht, ein grüner/roter Umriss zeigt, ob der Platz frei ist.
    /// Linksklick/E stellt ab, Esc/B bricht ab (Objekt springt zurück). Der Platz wird im Spielstand gemerkt.
    /// </summary>
    public sealed class FurnitureMover
    {
        private const int IgnoreRaycastLayer = 2;
        private const float PickRange = 4.5f;
        private const float MinDist = 1.1f;
        private const float MaxDist = 5.5f;
        private const float WallInset = 0.3f;

        private bool _moving;
        private Movable _target;
        private Station _station;
        private Vector3 _origPos;
        private Quaternion _origRot;
        private float _rot;
        private Vector3 _pos;
        private string _reason;
        private readonly List<Collider> _disabled = new List<Collider>();
        private GameObject _ghost;
        private readonly List<Footprint> _others = new List<Footprint>();

        public bool Active => _moving;
        public bool Valid => Active && _reason == null;

        public string Prompt
        {
            get
            {
                if (!Active || _target == null) return "";
                string name = string.IsNullOrEmpty(_target.Label) ? "Möbel" : _target.Label;
                string place = GameInput.KeyLabel("place"), rotate = GameInput.KeyLabel("rotate"), back = GameInput.KeyLabel("back");
                string state = _reason == null ? "Platz frei" : _reason;
                return name + " verschieben – " + state + "\n" + place + ": abstellen · " + rotate + ": drehen · " + back + ": abbrechen";
            }
        }

        /// <summary>Kurzer Zusatz für den Hinweistext, wenn ein verschiebbares Objekt anvisiert wird.</summary>
        public static string HintFor(Camera cam)
        {
            if (!Allowed()) return "";
            var m = PickTarget(cam);
            return m != null ? GameInput.KeyLabel("build") + ": " + (string.IsNullOrEmpty(m.Label) ? "Verschieben" : m.Label + " verschieben") : "";
        }

        private static Movable PickTarget(Camera cam)
        {
            if (cam == null) return null;
            if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, PickRange, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Collide))
                return null;
            return hit.collider != null ? hit.collider.GetComponentInParent<Movable>() : null;
        }

        private static bool Allowed()
        {
            var sim = Game.Sim;
            return sim != null && sim.StoryStage == "business" && !sim.DayOver;
        }

        /// <summary>Startet den Modus für das anvisierte Objekt. false, wenn nichts Verschiebbares im Blick ist.</summary>
        public bool TryBegin(PlayerController player)
        {
            if (Active || player == null) return false;
            if (!Allowed()) return false;
            var m = PickTarget(player.Cam);
            if (m == null)
            {
                Game.Sim?.Notify("Ziele auf ein Möbelstück, um es zu verschieben.", "info");
                return false;
            }
            _moving = true;
            _target = m;
            _station = m.GetComponent<Station>();
            _origPos = m.transform.position;
            _origRot = m.transform.rotation;
            _rot = FurnitureLayout.NormalizeAngle(m.transform.eulerAngles.y);
            _pos = _origPos;
            _reason = null;
            _disabled.Clear();
            foreach (var c in m.GetComponentsInChildren<Collider>(true))
            {
                if (c == null || !c.enabled) continue;
                c.enabled = false;
                _disabled.Add(c);
            }
            if (_station != null) _station.SetHighlighted(false);
            CreateGhost();
            Game.Sound("pickup");
            return true;
        }

        public void Tick(PlayerController player)
        {
            if (!Active) return;
            if (_target == null || player == null || !Allowed())
            {
                Cancel();
                return;
            }
            if (GameInput.BuildDown)
            {
                Cancel();
                return;
            }
            float dRot = GameInput.BuildRotateDegrees;
            if (Mathf.Abs(dRot) > 0.01f)
            {
                _rot = FurnitureLayout.SnapAngle(_rot + dRot);
                Game.Sound("click", 0.1f, -8f);
            }
            _rot = FurnitureLayout.NormalizeAngle(_rot);

            var floor = FloorPoint(player);
            var fp0 = _target.FootprintAt(Vector3.zero, _rot);
            // Mittelpunkt der Grundfläche unters Fadenkreuz, Drehpunkt aufs Raster
            float px = FurnitureLayout.Snap(floor.x - fp0.CX), pz = FurnitureLayout.Snap(floor.z - fp0.CZ);
            _pos = new Vector3(px, _origPos.y, pz);
            _target.transform.SetPositionAndRotation(_pos, Quaternion.Euler(0f, _rot, 0f));

            var fp = _target.FootprintAt(_pos, _rot);
            _reason = Validate(fp, player);
            UpdateGhost(fp);

            if (GameInput.BuildConfirmDown) Confirm();
        }

        private static Vector3 FloorPoint(PlayerController player)
        {
            var cam = player.Cam.transform;
            var origin = cam.position;
            var dir = cam.forward;
            var flatFwd = new Vector3(dir.x, 0f, dir.z);
            if (flatFwd.sqrMagnitude < 0.0001f) flatFwd = player.transform.forward;
            flatFwd.Normalize();
            Vector3 p;
            if (dir.y < -0.05f)
            {
                float t = -origin.y / dir.y;
                p = origin + dir * t;
            }
            else p = player.transform.position + flatFwd * MaxDist;
            var basePos = player.transform.position;
            var delta = new Vector3(p.x - basePos.x, 0f, p.z - basePos.z);
            float dist = delta.magnitude;
            if (dist < 0.0001f) delta = flatFwd;
            else delta /= dist;
            dist = Mathf.Clamp(dist, MinDist, MaxDist);
            return basePos + delta * dist;
        }

        private string Validate(Footprint fp, PlayerController player)
        {
            var world = Game.World;
            var room = WorldBuilder.RoomFor(_target.Stage).Inset(WallInset);
            _others.Clear();
            foreach (var m in Movable.All)
            {
                if (m == null || m == _target || !m.isActiveAndEnabled) continue;
                _others.Add(m.CurrentFootprint);
            }
            string reason = FurnitureLayout.Validate(fp, room, _others, WorldBuilder.KeepOutsFor(_target.Stage));
            if (reason != null) return reason;
            if (!fp.Flat)
            {
                var pp = player.transform.position;
                if (FurnitureLayout.Overlaps(fp, new Footprint(pp.x, pp.z, 0.4f, 0.4f, 0f), 0f)) return "Du stehst im Weg";
                string blocked = PhysicsBlocked(fp);
                if (blocked != null) return blocked;
            }
            return world == null ? "Keine Welt" : null;
        }

        /// <summary>Feste Hindernisse (Wände, Automaten, Trennwände, andere Stationen ohne Movable) per Physik prüfen.</summary>
        private string PhysicsBlocked(Footprint fp)
        {
            const float bottom = 0.2f;
            float h = Mathf.Clamp(_target.Height, 0.3f, 2.4f);
            var center = new Vector3(fp.CX, bottom + (h - bottom) / 2f + _origPos.y, fp.CZ);
            var half = new Vector3(Mathf.Max(0.05f, fp.HalfX - 0.08f), Mathf.Max(0.05f, (h - bottom) / 2f), Mathf.Max(0.05f, fp.HalfZ - 0.08f));
            var hits = Physics.OverlapBox(center, half, Quaternion.Euler(0f, fp.RotY, 0f), ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore);
            if (hits == null) return null;
            foreach (var c in hits)
            {
                if (c == null || c.isTrigger) continue;
                if (c.transform.IsChildOf(_target.transform)) continue;
                if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) continue; // lose Kisten/Pakete werden geschoben
                if (c.GetComponentInParent<Movable>() != null) continue; // über Grundflächen geprüft
                if (c.GetComponentInParent<NPC>() != null) continue;
                if (c.GetComponentInParent<DroppedItem>() != null) continue;
                if (c.GetComponentInParent<CharacterController>() != null) continue;
                var st = c.GetComponentInParent<Station>();
                return st != null && !string.IsNullOrEmpty(st.Title) ? "Stößt an: " + st.Title : "Platz belegt";
            }
            return null;
        }

        private void Confirm()
        {
            if (!Active) return;
            if (_reason != null)
            {
                Game.Sound("error");
                Game.Sim?.Notify("Hier geht das nicht: " + _reason, "bad");
                return;
            }
            Game.Sim?.SetFurniture(_target.Key, _pos.x, _pos.z, _rot);
            Game.Sound("place");
            Finish();
        }

        /// <summary>Bricht ab und stellt das Objekt an seinen alten Platz zurück.</summary>
        public void Cancel()
        {
            if (_target != null) _target.transform.SetPositionAndRotation(_origPos, _origRot);
            Finish();
        }

        private void Finish()
        {
            foreach (var c in _disabled)
                if (c != null) c.enabled = true;
            _disabled.Clear();
            _moving = false;
            _target = null;
            _station = null;
            _reason = null;
            if (_ghost != null) Object.Destroy(_ghost);
            _ghost = null;
        }

        // ---- Vorschau -------------------------------------------------------------------------------
        private static Material _ok, _bad;

        private void CreateGhost()
        {
            if (_ghost != null) Object.Destroy(_ghost);
            _ghost = new GameObject("MoveGhost") { layer = IgnoreRaycastLayer };
            if (Game.World != null) _ghost.transform.SetParent(Game.World.transform, true);
            var box = Props.Box(_ghost.transform, Vector3.one, GhostMat(true), Vector3.zero, default, 0f, false);
            box.name = "Volume";
            box.layer = IgnoreRaycastLayer;
            var plate = Props.Box(_ghost.transform, Vector3.one, GhostMat(true), Vector3.zero, default, 0f, false);
            plate.name = "Plate";
            plate.layer = IgnoreRaycastLayer;
        }

        private static Material GhostMat(bool ok)
        {
            if (ok && _ok != null) return _ok;
            if (!ok && _bad != null) return _bad;
            var m = Mats.Glass(ok ? new Color(0.2f, 0.9f, 0.35f, 0.22f) : new Color(0.95f, 0.2f, 0.15f, 0.28f));
            if (ok) _ok = m;
            else _bad = m;
            return m;
        }

        private void UpdateGhost(Footprint fp)
        {
            if (_ghost == null) CreateGhost();
            if (_ghost == null) return;
            var mat = GhostMat(_reason == null);
            float h = Mathf.Clamp(_target.Height, 0.05f, 3f);
            _ghost.transform.SetPositionAndRotation(new Vector3(fp.CX, _origPos.y, fp.CZ), Quaternion.Euler(0f, fp.RotY, 0f));
            _ghost.transform.localScale = Vector3.one;
            var vol = _ghost.transform.Find("Volume");
            if (vol != null)
            {
                vol.localPosition = new Vector3(0f, h / 2f, 0f);
                vol.localScale = new Vector3(fp.HalfX * 2f + 0.04f, h + 0.02f, fp.HalfZ * 2f + 0.04f);
                SetMat(vol, mat);
            }
            var plate = _ghost.transform.Find("Plate");
            if (plate != null)
            {
                plate.localPosition = new Vector3(0f, 0.03f, 0f);
                plate.localScale = new Vector3(fp.HalfX * 2f + 0.1f, 0.02f, fp.HalfZ * 2f + 0.1f);
                SetMat(plate, mat);
            }
        }

        private static void SetMat(Transform t, Material m)
        {
            var r = t.GetComponent<MeshRenderer>();
            if (r == null) return;
            if (r.sharedMaterial != m) r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
