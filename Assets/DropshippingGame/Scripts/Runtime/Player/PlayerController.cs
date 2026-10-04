using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// Ego-Perspektive: Bewegung (CharacterController), Blick, Zielen per Fadenkreuz (Raycast) mit
    /// Hervorhebung, Tragen mit sichtbaren Armen, Ablegen mit Physik, Kopfwippen und Schrittgeräusche.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public const float SpeedWalk = 4.6f;
        public const float SpeedSprint = 7.2f;
        public const float SpeedCrouch = 2.2f;
        public const float JumpVelocity = 5.2f;
        public const float Gravity = 16f;
        public const float PitchLimit = 86f;
        public const float StandHeight = 1.8f;
        public const float CrouchHeight = 1.1f;
        public const float StandCamY = 1.62f;
        public const float CrouchCamY = 0.95f;
        public const float Reach = 3.2f;
        private const int IgnoreRaycastLayer = 2;

        public Camera Cam;
        /// <summary>Oberster gehaltener Gegenstand (der, mit dem gearbeitet wird).</summary>
        public ItemData Held;
        /// <summary>
        /// v3.0 Paketstapel: weitere Pakete UNTER <see cref="Held"/> (unten zuerst). Nur Pakete
        /// (mit oder ohne Etikett) lassen sich stapeln; Kapazität siehe <see cref="PackageCapacity"/>.
        /// </summary>
        public readonly List<ItemData> Stack = new List<ItemData>();
        private ObjectiveMarker _marker;
        public IInteractable Focus;
        /// <summary>Verschiebe-Modus für Möbel (Taste B).</summary>
        public readonly FurnitureMover Mover = new FurnitureMover();
        public string PromptText = "";

        private CharacterController _cc;
        private Transform _handAnchor;
        private GameObject _heldVisual;
        private Vector3 _velocity;
        private float _pitch;
        private float _bobT, _stepAcc;
        private float _camBaseY = StandCamY;
        private Vector2 _sway;
        private bool _wasGrounded = true;

        public bool IsEmpty => Held == null;

        public static PlayerController Create(Transform parent)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            go.layer = IgnoreRaycastLayer;
            var p = go.AddComponent<PlayerController>();
            p.Init();
            return p;
        }

        private void Init()
        {
            _cc = GetComponent<CharacterController>();
            _cc.height = StandHeight;
            _cc.radius = 0.32f;
            _cc.center = new Vector3(0, StandHeight / 2f, 0);
            _cc.stepOffset = 0.35f;
            _cc.slopeLimit = 50f;
            _cc.skinWidth = 0.04f;
            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0, StandCamY, 0);
            camGo.tag = "MainCamera";
            Cam = camGo.AddComponent<Camera>();
            Cam.nearClipPlane = 0.05f;
            Cam.farClipPlane = 400f;
            Cam.fieldOfView = Settings.Fov;
            camGo.AddComponent<AudioListener>();
            PostFX.SetupCamera(Cam);
            _handAnchor = new GameObject("Hands").transform;
            _handAnchor.SetParent(camGo.transform, false);
            Settings.Changed += ApplySettings;
            _marker = ObjectiveMarker.Create(transform.parent, this);
            gameObject.AddComponent<EmoteController>();
        }

        private void OnDestroy()
        {
            Settings.Changed -= ApplySettings;
            if (Mover.Active) Mover.Cancel();
            if (_marker != null) Destroy(_marker.gameObject);
            if (Focus != null && !(Focus is Object fo && fo == null)) Focus.SetHighlighted(false);
        }

        private void ApplySettings()
        {
            if (Cam != null) Cam.fieldOfView = Settings.Fov;
            PostFX.SetupCamera(Cam);
        }

        public float Yaw
        {
            get => transform.eulerAngles.y;
            set => transform.rotation = Quaternion.Euler(0, value, 0);
        }

        public void Teleport(Vector3 pos, float yaw)
        {
            _cc.enabled = false;
            transform.position = pos;
            Yaw = yaw;
            _pitch = 0f;
            Cam.transform.localRotation = Quaternion.identity;
            _velocity = Vector3.zero;
            _cc.enabled = true;
        }

        // ---- Gehaltener Gegenstand mit Armen --------------------------------------------------------
        /// <summary>Ersetzt den obersten Gegenstand (der Stapel darunter bleibt).</summary>
        public void Hold(ItemData data)
        {
            if (data == null)
            {
                PopTop();
                return;
            }
            if (!IsStackable(data)) Stack.Clear();
            Held = data;
            HeldChanged();
        }

        /// <summary>Leert beide Hände komplett (inklusive Paketstapel).</summary>
        public void ClearHands()
        {
            Held = null;
            Stack.Clear();
            HeldChanged();
        }

        // ---- Paketstapel (GAME_IDEAS #5) ------------------------------------------------------------
        public static bool IsStackable(ItemData d) => d != null && (d.Kind == ItemKind.Package || d.Kind == ItemKind.Labeled);

        /// <summary>Anzahl getragener Gegenstände (0, 1 oder Stapelhöhe).</summary>
        public int CarryCount => Held == null ? 0 : 1 + Stack.Count;

        /// <summary>
        /// Wie viele Pakete auf einmal getragen werden können: 1, +1 mit Skill "Starke Arme",
        /// +1 mit "Prozess-Flow" (Logistik-Endknoten).
        /// </summary>
        public int PackageCapacity
        {
            get
            {
                var sim = Game.Sim;
                if (sim == null) return 1;
                int cap = 1;
                if (sim.HasSkill("l_arme")) cap++;
                if (sim.HasSkill("l_flow")) cap++;
                return cap;
            }
        }

        /// <summary>Passt dieses Paket noch oben auf den Stapel?</summary>
        public bool CanStack(ItemData d) => Held != null && IsStackable(Held) && IsStackable(d) && CarryCount < PackageCapacity;

        /// <summary>Legt einen Gegenstand oben auf (leere Hände: einfach halten).</summary>
        public void Push(ItemData d)
        {
            if (d == null) return;
            if (Held == null || !CanStack(d))
            {
                Hold(d);
                return;
            }
            Stack.Add(Held);
            Held = d;
            HeldChanged();
        }

        /// <summary>Entfernt den obersten Gegenstand; der nächste im Stapel rückt nach.</summary>
        public void PopTop()
        {
            if (Stack.Count > 0)
            {
                Held = Stack[Stack.Count - 1];
                Stack.RemoveAt(Stack.Count - 1);
            }
            else Held = null;
            HeldChanged();
        }

        /// <summary>Alle getragenen Gegenstände, unten zuerst, oben (Held) zuletzt. Neue Liste.</summary>
        public List<ItemData> Carried()
        {
            var list = new List<ItemData>(Stack);
            if (Held != null) list.Add(Held);
            return list;
        }

        /// <summary>Setzt den ganzen Stapel neu (unten zuerst). Leere Liste = Hände leer.</summary>
        public void SetCarried(List<ItemData> items)
        {
            Stack.Clear();
            Held = null;
            if (items != null)
                foreach (var it in items)
                {
                    if (it == null) continue;
                    if (Held != null) Stack.Add(Held);
                    Held = it;
                }
            HeldChanged();
        }

        /// <summary>Wie viele getragene Gegenstände dieser Art (z. B. etikettierte Pakete)?</summary>
        public int CountCarried(ItemKind kind)
        {
            int n = 0;
            foreach (var it in Carried()) if (it.Kind == kind) n++;
            return n;
        }

        private void HeldChanged()
        {
            RebuildHeldVisual();
            Game.Root?.OnHeldChanged();
        }

        private void RebuildHeldVisual()
        {
            if (_heldVisual != null) Destroy(_heldVisual);
            _heldVisual = null;
            if (Held == null) return;
            _heldVisual = new GameObject("Held");
            _heldVisual.transform.SetParent(_handAnchor, false);
            var bottom = Stack.Count > 0 && Stack[0] != null ? Stack[0] : Held;
            var size = ItemKit.Bounds(bottom);
            bool twoHands = Held.Kind == ItemKind.Crate || Held.Kind == ItemKind.Package || Held.Kind == ItemKind.Labeled || Held.Kind == ItemKind.Return;
            var itemPos = twoHands ? new Vector3(0f, -0.36f, 0.72f) : new Vector3(0.26f, -0.3f, 0.55f);
            if (Held.Kind == ItemKind.Crate) itemPos = new Vector3(0f, -0.5f, 0.8f);
            // Stapel: unten zuerst, jedes Paket auf dem vorigen (leicht verdreht).
            var carried = Carried();
            float cursor = itemPos.y - size.y / 2f;
            for (int i = 0; i < carried.Count; i++)
            {
                var data = carried[i];
                var b = ItemKit.Bounds(data);
                var item = ItemKit.Build(_heldVisual.transform, data, false);
                item.transform.localPosition = new Vector3(itemPos.x, cursor + b.y / 2f, itemPos.z);
                item.transform.localRotation = Quaternion.Euler(0, (twoHands ? 8f : 20f) + (i % 2 == 0 ? 0f : 7f), 0);
                cursor += b.y + 0.004f;
            }
            var sleeve = Mats.Std(new Color(0.2f, 0.22f, 0.28f), 0.8f);
            var skin = Mats.Std(new Color(0.93f, 0.76f, 0.62f), 0.7f);
            if (twoHands)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var hand = itemPos + new Vector3(side * (size.x / 2f + 0.03f), -size.y * 0.15f, -0.02f);
                    var shoulder = new Vector3(side * 0.34f, -0.72f, 0.08f);
                    Arm(shoulder, hand, sleeve, skin);
                }
            }
            else Arm(new Vector3(0.36f, -0.7f, 0.05f), itemPos + new Vector3(0.04f, -size.y * 0.5f - 0.03f, -0.03f), sleeve, skin);
            foreach (var r in _heldVisual.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            foreach (var l in _heldVisual.GetComponentsInChildren<Label3D>()) Destroy(l.gameObject);
        }

        private void Arm(Vector3 from, Vector3 to, Material sleeve, Material skin)
        {
            var dir = to - from;
            float length = dir.magnitude;
            var arm = new GameObject("Arm").transform;
            arm.SetParent(_heldVisual.transform, false);
            arm.localPosition = (from + to) / 2f;
            arm.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            Props.Box(arm, new Vector3(0.1f, 0.1f, length), sleeve, Vector3.zero, default, 0.02f, false);
            Props.Box(arm, new Vector3(0.09f, 0.07f, 0.11f), skin, new Vector3(0, 0, length / 2f + 0.03f), default, 0.02f, false);
        }

        // ---- Ablegen -------------------------------------------------------------------------------
        public void DropHeldItem()
        {
            if (Held == null) return;
            var fwd = Cam.transform.forward;
            var pos = Cam.transform.position + fwd * 0.7f + new Vector3(0, -0.35f, 0);
            // Nicht in Wände hinein ablegen
            if (Physics.Raycast(Cam.transform.position, fwd, out var hit, 0.9f, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                pos = hit.point - fwd * 0.35f + new Vector3(0, -0.2f, 0);
            var item = DroppedItem.Spawn(Game.World != null ? Game.World.transform : null, Held, pos, Yaw);
            item.Body.AddForce(fwd * 2.2f + _velocity * 0.5f + new Vector3(0, 0.8f, 0), ForceMode.VelocityChange);
            Game.Sound("drop");
            PopTop();
        }

        public DroppedItem SpawnDropped(ItemData data, Vector3 pos, float rotY) =>
            DroppedItem.Spawn(Game.World != null ? Game.World.transform : null, data, pos + new Vector3(0, 0.05f, 0), rotY);

        // ---- Eingabe & Bewegung --------------------------------------------------------------------
        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            bool locked = Game.Root != null && Game.Root.InputLocked;
            if (!locked && !EmoteController.WheelOpen)
            {
                var look = GameInput.Look;
                Yaw += look.x;
                _pitch = Mathf.Clamp(_pitch - look.y, -PitchLimit, PitchLimit);
                Cam.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
                _sway += new Vector2(look.x, look.y) * 0.004f;
            }

            Vector2 mv = locked ? Vector2.zero : GameInput.Move;
            bool crouching = !locked && GameInput.CrouchHeld;
            bool sprinting = !locked && GameInput.SprintHeld && !crouching;
            var inputDir = transform.forward * mv.y + transform.right * mv.x;
            inputDir.y = 0f;
            if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

            float targetH = crouching ? CrouchHeight : StandHeight;
            if (!Mathf.Approximately(_cc.height, targetH))
            {
                if (!crouching && Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.up, StandHeight - 0.4f, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                    targetH = _cc.height; // unter einem Hindernis nicht aufstehen
                _cc.height = targetH;
                _cc.center = new Vector3(0, targetH / 2f, 0);
            }
            _camBaseY = Mathf.Lerp(_camBaseY, _cc.height < StandHeight - 0.1f ? CrouchCamY : StandCamY, 10f * dt);

            float speed = crouching ? SpeedCrouch : (sprinting ? SpeedSprint : SpeedWalk);
            if (Game.Sim != null) speed *= Game.Sim.CarrySpeedMult(Held?.Kind ?? ItemKind.None);
            var target = inputDir * speed;
            bool grounded = _cc.isGrounded;
            float accel = grounded ? 12f : 3f;
            _velocity.x = Mathf.MoveTowards(_velocity.x, target.x, accel * speed * dt);
            _velocity.z = Mathf.MoveTowards(_velocity.z, target.z, accel * speed * dt);
            if (grounded)
            {
                if (_velocity.y < 0f) _velocity.y = -2f;
                if (!locked && GameInput.JumpDown) _velocity.y = JumpVelocity;
            }
            else _velocity.y -= Gravity * dt;
            var flags = _cc.Move(_velocity * dt);
            if ((flags & CollisionFlags.Above) != 0 && _velocity.y > 0f) _velocity.y = 0f;

            // Landung + Kopfwippen + Schritte
            grounded = _cc.isGrounded;
            if (grounded && !_wasGrounded) Game.Sound("step", 0.1f, -6f);
            _wasGrounded = grounded;
            float hspeed = new Vector2(_velocity.x, _velocity.z).magnitude;
            bool moving = grounded && hspeed > 0.6f;
            if (moving)
            {
                _bobT += dt * hspeed * 1.9f;
                _stepAcc += dt * hspeed;
                if (_stepAcc >= 1.55f)
                {
                    _stepAcc = 0f;
                    Game.Sound("step", 0.15f, -15f);
                }
            }
            bool bob = moving && Settings.HeadBob;
            float bobY = bob ? Mathf.Sin(_bobT) * 0.035f : 0f;
            float bobX = bob ? Mathf.Cos(_bobT * 0.5f) * 0.02f : 0f;
            Cam.transform.localPosition = new Vector3(bobX, _camBaseY + bobY, 0);
            _sway = Vector2.Lerp(_sway, Vector2.zero, Mathf.Min(1f, dt * 8f));
            _handAnchor.localPosition = new Vector3(-_sway.x * 0.6f, -_sway.y * 0.6f + bobY * 0.5f, 0);

            if (Mover.Active)
            {
                ClearFocus();
                if (!locked) Mover.Tick(this);
                PromptText = Mover.Prompt;
                return;
            }
            UpdateFocus(locked);
            if (locked) return;
            if (GameInput.BuildDown && Mover.TryBegin(this))
            {
                ClearFocus();
                PromptText = Mover.Prompt;
                return;
            }
            if (GameInput.InteractDown && Focus != null) Focus.Interact(this);
            if (GameInput.DropDown) DropHeldItem();
            if (transform.position.y < -20f && Game.World != null) Teleport(Game.World.SpawnPoint(), Game.World.SpawnYaw());
        }

        private void ClearFocus()
        {
            if (Focus != null && !(Focus is Object o && o == null)) Focus.SetHighlighted(false);
            Focus = null;
        }

        private void UpdateFocus(bool locked)
        {
            IInteractable target = null;
            if (!locked && Physics.Raycast(Cam.transform.position, Cam.transform.forward, out var hit, Reach, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Collide))
                target = hit.collider.GetComponentInParent<IInteractable>();
            if (!ReferenceEquals(target, Focus))
            {
                if (Focus != null && !(Focus is Object o && o == null)) Focus.SetHighlighted(false);
                Focus = target;
                Focus?.SetHighlighted(true);
            }
            if (Focus is Object uo && uo == null) Focus = null;
            PromptText = Focus != null ? Focus.Prompt(this) : "";
            if (!locked)
            {
                string hint = FurnitureMover.HintFor(Cam);
                if (!string.IsNullOrEmpty(hint)) PromptText = string.IsNullOrEmpty(PromptText) ? hint : PromptText + "  ·  " + hint;
            }
        }

        /// <summary>Stößt Kisten und Pakete an, wenn man hineinläuft.</summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var body = hit.rigidbody;
            if (body == null || body.isKinematic) return;
            var push = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            body.AddForce(push * 0.6f, ForceMode.Impulse);
        }

        /// <summary>
        /// Spielstand: Der Spielstand kennt nur einen gehaltenen Gegenstand. Weitere Pakete im Stapel
        /// werden als am Boden liegende Gegenstände neben dem Spieler gespeichert (gehen nicht verloren).
        /// GameRoot ruft dies nach dem Einsammeln der Welt-Gegenstände auf.
        /// </summary>
        public PlayerSave ToSave()
        {
            var sim = Game.Sim;
            if (sim != null && sim.WorldItems != null)
            {
                for (int i = 0; i < Stack.Count; i++)
                {
                    var it = Stack[i];
                    if (it == null) continue;
                    var pos = transform.position + transform.forward * 0.6f + new Vector3(0, 0.3f + i * 0.3f, 0);
                    sim.WorldItems.Add(new WorldItemSave { Item = it.Clone(), Pos = pos.ToV3(), RotY = Yaw });
                }
            }
            return new PlayerSave { Pos = transform.position.ToV3(), RotY = Yaw, Held = Held?.Clone() };
        }
    }
}
