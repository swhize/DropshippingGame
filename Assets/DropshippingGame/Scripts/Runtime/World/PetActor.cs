using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Gelenke eines prozeduralen Haustiers (für die Lauf-/Schwanz-Animation).</summary>
    public sealed class PetRig
    {
        public Transform Root, Body, Head, Tail;
        public Transform[] Legs = new Transform[4];
    }

    /// <summary>
    /// Haustier in der Welt (Hund / Katze aus abgerundeten Blöcken, Kenney-Stil). Läuft dem Spieler
    /// hinterher oder wohnt in der Garage (schnuppert dort herum). Anvisieren: Streicheln (1× pro Tag,
    /// bringt Laune + Bekanntheit), geduckt + Benutzen: „Komm mit“ / „Bleib in der Garage“.
    /// Ohne Pet-Daten (Display = true) steht es nur animiert in der Tierheim-Ecke von Fressnix.
    /// </summary>
    public sealed class PetActor : MonoBehaviour, IInteractable
    {
        /// <summary>Wohnplatz in der Garage (Hund, Katze daneben).</summary>
        public static readonly Vector3 GarageHome = new Vector3(-14.4f, 0f, -10.3f);

        public string Species = "hund";
        public bool Display;
        private PetRig _rig;
        private readonly Highlighter _hl = new Highlighter();
        private Vector3 _wanderTarget;
        private float _wanderT, _phase, _speed, _barkT;
        private float _seed;

        private static Sim S => Game.Sim;

        public static PetActor Spawn(Transform parent, string species, Vector3 pos, float rotY, bool display)
        {
            var go = new GameObject("Pet " + species);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            var a = go.AddComponent<PetActor>();
            a.Setup(species, display);
            return a;
        }

        private void Setup(string species, bool display)
        {
            Species = species;
            Display = display;
            _seed = species == "katze" ? 1.7f : 0.3f;
            _rig = Build(transform, species);
            _hl.Collect(transform);
            if (!display)
            {
                var col = gameObject.AddComponent<BoxCollider>();
                col.isTrigger = true;
                bool cat = species == "katze";
                col.size = cat ? new Vector3(0.45f, 0.55f, 0.75f) : new Vector3(0.5f, 0.75f, 0.95f);
                col.center = new Vector3(0f, col.size.y / 2f, 0.05f);
            }
            _wanderTarget = transform.position;
        }

        private Pet Data => S != null ? S.PetOf(Species) : null;

        // ---- Modell ---------------------------------------------------------------------------------
        /// <summary>Baut Hund oder Katze. Front +Z, Füße auf y = 0.</summary>
        public static PetRig Build(Transform parent, string species)
        {
            bool cat = species == "katze";
            var rig = new PetRig();
            var root = Props.Node(parent, "PetModel").transform;
            rig.Root = root;
            var sp = ShopData.PetSpecies(species);
            var furCol = sp != null ? sp.Color.ToColor() : new Color(0.7f, 0.5f, 0.3f);
            var fur = Mats.Std(furCol, 0.85f);
            var light = Mats.Std(Color.Lerp(furCol, Color.white, 0.55f), 0.85f);
            var dark = Mats.Std(new Color(0.1f, 0.08f, 0.07f), 0.4f);
            var pink = Mats.Std(new Color(0.95f, 0.55f, 0.6f), 0.6f);
            float s = cat ? 0.78f : 1f;
            float legH = (cat ? 0.2f : 0.26f);
            float bodyY = legH + 0.12f * s;
            var body = Props.Node(root, "Body", new Vector3(0f, bodyY, 0f)).transform;
            rig.Body = body;
            Props.Box(body, new Vector3(0.26f * s, 0.24f * s, 0.52f * s), fur, Vector3.zero, default, 0.05f);
            Props.Box(body, new Vector3(0.2f * s, 0.06f * s, 0.4f * s), light, new Vector3(0f, -0.1f * s, 0.02f), default, 0.02f, false);
            // Beine (Drehpunkt oben)
            float lx = 0.085f * s, lz = 0.18f * s;
            int i = 0;
            foreach (float z in new[] { lz, -lz })
            foreach (float x in new[] { -lx, lx })
            {
                var pivot = Props.Node(root, "Leg", new Vector3(x, legH, z)).transform;
                Props.Box(pivot, new Vector3(0.075f * s, legH, 0.075f * s), fur, new Vector3(0f, -legH / 2f, 0f), default, 0.02f);
                Props.Box(pivot, new Vector3(0.08f * s, 0.04f, 0.09f * s), light, new Vector3(0f, -legH + 0.02f, 0.01f), default, 0.015f, false);
                rig.Legs[i++] = pivot;
            }
            // Kopf
            var head = Props.Node(body, "Head", new Vector3(0f, 0.15f * s, 0.3f * s)).transform;
            rig.Head = head;
            Props.Box(head, new Vector3(0.24f * s, 0.22f * s, 0.22f * s), fur, Vector3.zero, default, 0.05f);
            if (cat)
            {
                Props.Box(head, new Vector3(0.12f, 0.06f, 0.05f), light, new Vector3(0f, -0.04f, 0.1f), default, 0.02f, false);
                Props.Box(head, new Vector3(0.025f, 0.02f, 0.01f), pink, new Vector3(0f, -0.02f, 0.13f), default, 0f, false);
                foreach (float ex in new[] { -0.06f, 0.06f })
                {
                    Props.Box(head, new Vector3(0.06f, 0.08f, 0.03f), fur, new Vector3(ex, 0.11f, -0.01f), new Vector3(0, 0, ex > 0 ? -18f : 18f), 0.01f);
                    Props.Box(head, new Vector3(0.03f, 0.05f, 0.01f), pink, new Vector3(ex, 0.105f, 0.008f), new Vector3(0, 0, ex > 0 ? -18f : 18f), 0f, false);
                    Props.Box(head, new Vector3(0.035f, 0.045f, 0.01f), Mats.Std(new Color(0.55f, 0.85f, 0.3f), 0.3f), new Vector3(ex * 0.75f, 0.02f, 0.087f), default, 0f, false);
                    Props.Box(head, new Vector3(0.012f, 0.04f, 0.012f), dark, new Vector3(ex * 0.75f, 0.02f, 0.092f), default, 0f, false);
                    // Schnurrhaare
                    Props.Box(head, new Vector3(0.12f, 0.004f, 0.004f), light, new Vector3(ex * 1.4f, -0.04f, 0.11f), new Vector3(0, 0, ex > 0 ? 8f : -8f), 0f, false);
                }
            }
            else
            {
                Props.Box(head, new Vector3(0.14f, 0.1f, 0.14f), light, new Vector3(0f, -0.04f, 0.14f), default, 0.03f);
                Props.Box(head, new Vector3(0.06f, 0.045f, 0.04f), dark, new Vector3(0f, -0.005f, 0.215f), default, 0.012f, false);
                Props.Box(head, new Vector3(0.05f, 0.01f, 0.06f), pink, new Vector3(0f, -0.09f, 0.17f), new Vector3(10, 0, 0), 0f, false);
                foreach (float ex in new[] { -0.13f, 0.13f })
                {
                    Props.Box(head, new Vector3(0.05f, 0.14f, 0.1f), Mats.Std(furCol * 0.7f, 0.85f), new Vector3(ex, 0.01f, -0.02f), new Vector3(0, 0, ex > 0 ? 12f : -12f), 0.02f);
                    Props.Box(head, new Vector3(0.035f, 0.035f, 0.01f), dark, new Vector3(ex * 0.42f, 0.04f, 0.112f), default, 0f, false);
                }
                // Halsband
                Props.Box(body, new Vector3(0.22f, 0.05f, 0.05f), Mats.Std(new Color(0.85f, 0.15f, 0.15f), 0.5f), new Vector3(0f, 0.1f, 0.25f), default, 0.01f, false);
            }
            // Schwanz
            var tail = Props.Node(body, "Tail", new Vector3(0f, 0.08f * s, -0.26f * s)).transform;
            rig.Tail = tail;
            if (cat) Props.Box(tail, new Vector3(0.045f, 0.32f, 0.045f), fur, new Vector3(0f, 0.15f, -0.03f), new Vector3(-15f, 0, 0), 0.015f);
            else Props.Box(tail, new Vector3(0.05f, 0.05f, 0.2f), fur, new Vector3(0f, 0.05f, -0.08f), new Vector3(-35f, 0, 0), 0.015f);
            return rig;
        }

        // ---- Verhalten ---------------------------------------------------------------------------
        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _rig == null) return;
            var pet = Display ? null : Data;
            bool moving = false;
            if (!Display && pet != null)
            {
                var player = Game.Player;
                Vector3 target;
                float stopDist;
                if (pet.Follow && player != null)
                {
                    var p = player.transform;
                    var side = Species == "katze" ? -0.7f : 0.7f;
                    target = p.position - p.forward * 1.3f + p.right * side;
                    target.y = 0f;
                    stopDist = 0.35f;
                    float far = Vector3.Distance(Flat(transform.position), Flat(p.position));
                    if (far > 16f)
                    {
                        transform.position = target;
                        return;
                    }
                    _speed = far > 4f ? 7.6f : (far > 2.2f ? 4.8f : 2.2f);
                }
                else
                {
                    var home = GarageHome + (Species == "katze" ? new Vector3(1.1f, 0f, -0.3f) : Vector3.zero);
                    _wanderT -= dt;
                    if (_wanderT <= 0f || Vector3.Distance(Flat(_wanderTarget), Flat(home)) > 2f)
                    {
                        _wanderT = Random.Range(3f, 7f);
                        var r = Random.insideUnitCircle * 1.2f;
                        _wanderTarget = home + new Vector3(r.x, 0f, r.y);
                    }
                    if (Vector3.Distance(Flat(transform.position), Flat(home)) > 14f) transform.position = home;
                    target = _wanderTarget;
                    stopDist = 0.25f;
                    _speed = 1.1f;
                }
                var to = Flat(target) - Flat(transform.position);
                if (to.magnitude > stopDist)
                {
                    moving = true;
                    var step = to.normalized * Mathf.Min(to.magnitude, _speed * dt);
                    var pos = transform.position + step;
                    pos.y = 0f;
                    transform.position = pos;
                    var look = Quaternion.LookRotation(to.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, Mathf.Min(1f, dt * 8f));
                }
                else if (pet.Follow && player != null)
                {
                    var face = Flat(player.transform.position) - Flat(transform.position);
                    if (face.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face.normalized, Vector3.up), Mathf.Min(1f, dt * 3f));
                }
            }
            Animate(dt, moving, pet);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private void Animate(float dt, bool moving, Pet pet)
        {
            float t = Time.time + _seed;
            _phase += dt * (moving ? Mathf.Clamp(_speed * 3.2f, 5f, 18f) : 0f);
            for (int i = 0; i < 4; i++)
            {
                if (_rig.Legs[i] == null) continue;
                float sign = (i == 0 || i == 3) ? 1f : -1f;
                float a = moving ? Mathf.Sin(_phase) * 32f * sign : Mathf.MoveTowardsAngle(_rig.Legs[i].localEulerAngles.x, 0f, dt * 200f);
                _rig.Legs[i].localRotation = Quaternion.Euler(moving ? a : 0f, 0f, 0f);
            }
            bool happy = pet == null || pet.Happiness >= 0.5f;
            if (_rig.Tail != null)
            {
                float wag = Species == "katze" ? Mathf.Sin(t * 1.6f) * 18f : Mathf.Sin(t * (happy ? 14f : 3f)) * (happy ? 35f : 8f);
                _rig.Tail.localRotation = Quaternion.Euler(0f, wag, 0f);
            }
            if (_rig.Body != null)
            {
                float breathe = 1f + Mathf.Sin(t * 2.2f) * 0.015f;
                _rig.Body.localScale = new Vector3(1f, breathe, 1f);
                float bob = moving ? Mathf.Abs(Mathf.Sin(_phase)) * 0.025f : 0f;
                var lp = _rig.Body.localPosition;
                _rig.Body.localPosition = new Vector3(lp.x, BodyBase() + bob, lp.z);
            }
            if (_rig.Head != null)
            {
                float tilt = moving ? 0f : Mathf.Sin(t * 0.7f) * 12f;
                _rig.Head.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.9f) * 4f, tilt, 0f);
            }
            if (!Display && pet != null && pet.HungryDays > 0)
            {
                _barkT -= dt;
                if (_barkT <= 0f)
                {
                    _barkT = Random.Range(18f, 30f);
                    if (Game.Player != null && Vector3.Distance(Game.Player.transform.position, transform.position) < 6f)
                        Game.Sound("bad", 0.2f, -14f);
                }
            }
        }

        private float BodyBase()
        {
            bool cat = Species == "katze";
            float s = cat ? 0.78f : 1f;
            return (cat ? 0.2f : 0.26f) + 0.12f * s;
        }

        // ---- IInteractable ---------------------------------------------------------------------------
        public string Title
        {
            get
            {
                var p = Data;
                return p != null ? p.Name : (Species == "katze" ? "Katze" : "Hund");
            }
        }

        public string Prompt(PlayerController player)
        {
            var sim = S;
            var p = Data;
            if (sim == null || p == null) return "";
            string mood = sim.PetMoodText(p);
            string food = "Futter: " + sim.PetFoodTotal() + " Portionen";
            string toggle = GameInput.KeyLabel("crouch") + "+" + GameInput.KeyLabel("interact") + ": " + (p.Follow ? "Bleib in der Garage" : "Komm mit");
            if (GameInput.CrouchHeld) return p.Name + ": " + (p.Follow ? "Bleib in der Garage" : "Komm mit") + " · " + mood;
            if (p.PettedDay == sim.Day) return p.Name + " (" + mood + ") · " + food + " · " + toggle;
            return p.Name + " streicheln (" + mood + ") · " + food + " · " + toggle;
        }

        public void Interact(PlayerController player)
        {
            var sim = S;
            var p = Data;
            if (sim == null || p == null) return;
            if (GameInput.CrouchHeld)
            {
                sim.PetSetFollow(Species, !p.Follow);
                return;
            }
            sim.PetPet(Species);
        }

        public void SetHighlighted(bool on)
        {
            if (!Display) _hl.Set(on);
        }
    }
}
