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
        public ItemData Held;
        public IInteractable Focus;
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
        }

        private void OnDestroy()
        {
            Settings.Changed -= ApplySettings;
            if (Focus != null) Focus.SetHighlighted(false);
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
        public void Hold(ItemData data)
        {
            Held = data;
            RebuildHeldVisual();
            Game.Root?.OnHeldChanged();
        }

        public void ClearHands()
        {
            Held = null;
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
            var size = ItemKit.Bounds(Held);
            bool twoHands = Held.Kind == ItemKind.Crate || Held.Kind == ItemKind.Package || Held.Kind == ItemKind.Labeled;
            var itemPos = twoHands ? new Vector3(0f, -0.36f, 0.72f) : new Vector3(0.26f, -0.3f, 0.55f);
            if (Held.Kind == ItemKind.Crate) itemPos = new Vector3(0f, -0.5f, 0.8f);
            var item = ItemKit.Build(_heldVisual.transform, Held, false);
            item.transform.localPosition = itemPos;
            item.transform.localRotation = Quaternion.Euler(0, twoHands ? 8f : 20f, 0);
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
            ClearHands();
        }

        public DroppedItem SpawnDropped(ItemData data, Vector3 pos, float rotY) =>
            DroppedItem.Spawn(Game.World != null ? Game.World.transform : null, data, pos + new Vector3(0, 0.05f, 0), rotY);

        // ---- Eingabe & Bewegung --------------------------------------------------------------------
        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            bool locked = Game.Root != null && Game.Root.InputLocked;
            if (!locked)
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
            if (Held != null && Held.Kind == ItemKind.Crate) speed *= 0.85f;
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

            UpdateFocus(locked);
            if (locked) return;
            if (GameInput.InteractDown && Focus != null) Focus.Interact(this);
            if (GameInput.DropDown) DropHeldItem();
            if (transform.position.y < -20f && Game.World != null) Teleport(Game.World.SpawnPoint(), Game.World.SpawnYaw());
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
        }

        /// <summary>Stößt Kisten und Pakete an, wenn man hineinläuft.</summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var body = hit.rigidbody;
            if (body == null || body.isKinematic) return;
            var push = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            body.AddForce(push * 0.6f, ForceMode.Impulse);
        }

        public PlayerSave ToSave() => new PlayerSave { Pos = transform.position.ToV3(), RotY = Yaw, Held = Held?.Clone() };
    }
}
