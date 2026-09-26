using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Mini-Event Straßenfest/Flohmarkt in der Welt: Banner über der Straße, Marktstand mit Markise
    /// auf dem Gehweg gegenüber, Packtisch vor der eigenen Tür und (in der Festphase) viele Besucher,
    /// die zum Stand laufen, schauen, feilschen oder kaufen. Die Regeln liegen in Sim.Festival.cs.
    /// Alles prozedural (keine Assets nötig). Fragt den Sim-Zustand ab, statt Ereignisse zu abonnieren,
    /// damit ein neuer Spielstand/neue Sim nichts kaputt macht.
    /// </summary>
    public sealed class StreetFestival : MonoBehaviour
    {
        /// <summary>Während Aufbau und Fest fahren keine Autos (Straße gesperrt).</summary>
        public static bool BlocksTraffic;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => BlocksTraffic = false;

        public static readonly Vector3 StallPos = new Vector3(-6f, 0f, 5.45f);
        private static readonly Vector3[] PackTablePos = { new Vector3(-12.6f, 0f, -5.7f), new Vector3(4.2f, 0f, -5.8f) };
        private const float BannerX = -2.6f;
        private const int MaxVisitors = 16;
        private const int SlotCount = 5;

        private sealed class Visitor
        {
            public NPC Npc;
            /// <summary>0 = zum Stand/Warten, 1 = schaut am Stand, 2 = geht, 3 = bummelt</summary>
            public int State;
            public int Slot = -1;
            public Vector3 Target;
            public float Timer;
            public float Patience;
            public string FollowUp;
            public float FollowUpAt;
        }

        private Transform _root, _goods, _visitorsRoot;
        private FestivalInteract _stall, _packTable;
        private Label3D _bannerA, _bannerB, _stallSign, _stallInfo, _packInfo;
        private readonly List<Visitor> _visitors = new List<Visitor>();
        private readonly Visitor[] _slots = new Visitor[SlotCount];
        private System.Random _rng = new System.Random(2718);
        private FestivalPhase _shownPhase = (FestivalPhase)(-1);
        private int _shownStage = -1;
        private string _goodsSig = "";
        private float _poll, _spawnAcc;

        public void Setup()
        {
            _root = Props.Node(transform, "StreetFestival").transform;
            _visitorsRoot = Props.Node(transform, "FestivalVisitors").transform;
            _root.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            BlocksTraffic = false;
        }

        private void Update()
        {
            var sim = Game.Sim;
            if (_root == null) return;
            bool show = sim != null && sim.InGame && sim.StoryStage == "business" && sim.FestivalActive && sim.FestivalDay == sim.Day;
            FestivalPhase phase = show ? sim.Festival : FestivalPhase.None;
            BlocksTraffic = show;

            _poll -= Time.deltaTime;
            if (_poll <= 0f || phase != _shownPhase)
            {
                _poll = 0.25f;
                SyncVisuals(sim, phase);
            }
            UpdateVisitors(sim, phase, Time.deltaTime);
        }

        // =====================================================================================
        // Aufbau der Kulisse
        // =====================================================================================
        private void SyncVisuals(Sim sim, FestivalPhase phase)
        {
            bool show = phase == FestivalPhase.Prep || phase == FestivalPhase.Live;
            int stage = sim != null ? Mathf.Clamp(sim.LocationStage, 0, 1) : 0;
            if (show && (_shownPhase != FestivalPhase.Prep && _shownPhase != FestivalPhase.Live || stage != _shownStage))
            {
                BuildScene(sim, stage);
                _shownStage = stage;
            }
            _shownPhase = phase;
            _root.gameObject.SetActive(show);
            if (!show)
            {
                ClearVisitors(sim == null || !sim.InGame);
                return;
            }
            string name = string.IsNullOrEmpty(sim.FestivalName) ? "Straßenfest" : sim.FestivalName;
            string upper = name.ToUpperInvariant().Replace("ß", "SS");
            if (_bannerA != null) _bannerA.SetText(upper);
            if (_bannerB != null) _bannerB.SetText(phase == FestivalPhase.Prep ? "AUFBAU · START " + Fmt.Clock(sim.FestivalLiveStart) : "HEUTE BIS " + Fmt.Clock(sim.FestivalLiveEnd));
            if (_stallSign != null) _stallSign.SetText(string.IsNullOrEmpty(sim.BrandName) ? "MEIN STAND" : sim.BrandName.ToUpperInvariant());
            if (_stallInfo != null)
                _stallInfo.SetText(sim.FestivalTotal() > 0 ? sim.FestivalTotal() + " Artikel" : (phase == FestivalPhase.Prep ? "Noch leer – Kisten bringen!" : "Ausverkauft"));
            if (_packInfo != null)
                _packInfo.SetText(phase == FestivalPhase.Prep ? "FESTKISTEN PACKEN" : "NACHSCHUB PACKEN");
            RefreshGoods(sim);
        }

        private void BuildScene(Sim sim, int stage)
        {
            for (int i = _root.childCount - 1; i >= 0; i--) Destroy(_root.GetChild(i).gameObject);
            _goodsSig = "";
            var wood = Mats.Std(new Color(0.55f, 0.38f, 0.22f), 0.85f);
            var darkWood = Mats.Std(new Color(0.36f, 0.24f, 0.14f), 0.85f);
            var metal = Mats.Std(new Color(0.75f, 0.76f, 0.78f), 0.35f, 0.6f);
            Color brand = sim != null ? sim.BrandColor.ToColor() : new Color(0.9f, 0.3f, 0.3f);
            var red = Mats.Std(new Color(0.86f, 0.18f, 0.2f), 0.7f);
            var white = Mats.Std(new Color(0.96f, 0.95f, 0.92f), 0.7f);
            var brandMat = Mats.Std(brand, 0.6f);

            // ---- Banner quer über die Straße --------------------------------------------------
            var banner = Props.Node(_root, "Banner", new Vector3(BannerX, 0, 0)).transform;
            foreach (float z in new[] { -4.45f, 4.45f })
            {
                Props.Cyl(banner, 0.07f, 0.07f, 5.2f, metal, new Vector3(0, 2.6f, z));
                Props.Collider(banner, new Vector3(0.2f, 5.2f, 0.2f), new Vector3(0, 2.6f, z));
            }
            Props.Box(banner, new Vector3(0.05f, 1.1f, 8.8f), red, new Vector3(0, 4.45f, 0), default, 0f);
            Props.Box(banner, new Vector3(0.06f, 0.12f, 8.8f), white, new Vector3(0, 3.95f, 0), default, 0f, false);
            Props.Box(banner, new Vector3(0.06f, 0.12f, 8.8f), white, new Vector3(0, 4.95f, 0), default, 0f, false);
            _bannerA = Label3D.Create(banner, "STRASSENFEST", 150f, Color.white, new Vector3(0, 4.55f, 0), true, 60f, false, 1.4f);
            _bannerB = Label3D.Create(banner, "", 60f, new Color(1f, 0.92f, 0.55f), new Vector3(0, 4.12f, 0), true, 40f);

            // ---- Wimpelketten entlang der Straße ---------------------------------------------
            Color[] flagCols = { new Color(0.9f, 0.2f, 0.2f), new Color(0.98f, 0.8f, 0.15f), new Color(0.2f, 0.55f, 0.9f), new Color(0.25f, 0.75f, 0.35f), brand };
            foreach (float z in new[] { -4.4f, 4.4f })
            {
                for (int i = 0; i < 22; i++)
                {
                    float x = BannerX - 12f + i * 0.55f;
                    float sag = Mathf.Sin(i / 21f * Mathf.PI) * 0.5f;
                    Props.Box(_root, new Vector3(0.28f, 0.3f, 0.01f), Mats.Std(flagCols[i % flagCols.Length], 0.7f), new Vector3(x, 3.9f - sag, z),
                        new Vector3(0, 0, 45f), 0f, false);
                }
            }

            // ---- Marktstand (Front zeigt zur Straße, also -z) ---------------------------------
            var stallGo = Props.Node(_root, "Marktstand", StallPos);
            var st = stallGo.transform;
            Props.Box(st, new Vector3(3.2f, 0.9f, 0.8f), wood, new Vector3(0, 0.45f, 0));
            Props.Box(st, new Vector3(3.3f, 0.06f, 0.9f), darkWood, new Vector3(0, 0.93f, 0));
            Props.Box(st, new Vector3(3.22f, 0.3f, 0.02f), brandMat, new Vector3(0, 0.72f, -0.41f), default, 0f, false);
            foreach (float x in new[] { -1.55f, 1.55f })
                foreach (float z in new[] { -0.4f, 0.4f })
                    Props.Box(st, new Vector3(0.08f, 2.5f, 0.08f), darkWood, new Vector3(x, 1.25f, z));
            // Markise mit Streifen, leicht zur Straße geneigt.
            var awning = Props.Node(st, "Markise", new Vector3(0, 2.5f, -0.1f)).transform;
            awning.localRotation = Quaternion.Euler(-14f, 0, 0);
            for (int i = 0; i < 8; i++)
                Props.Box(awning, new Vector3(0.42f, 0.04f, 1.5f), i % 2 == 0 ? red : white, new Vector3(-1.47f + i * 0.42f, 0, 0), default, 0f);
            for (int i = 0; i < 8; i++)
                Props.Box(awning, new Vector3(0.42f, 0.2f, 0.03f), i % 2 == 0 ? red : white, new Vector3(-1.47f + i * 0.42f, -0.1f, -0.75f), default, 0f, false);
            _stallSign = Label3D.Create(st, "MEIN STAND", 80f, Color.white, new Vector3(0, 2.95f, -0.2f), true, 30f, false, 1.3f);
            _stallInfo = Label3D.Create(st, "", 46f, new Color(1f, 0.9f, 0.5f), new Vector3(0, 1.55f, -0.5f), true, 14f);
            _goods = Props.Node(st, "Ware", new Vector3(0, 0.96f, 0)).transform;
            // Kisten hinter dem Stand
            Props.Box(st, ItemKit.CrateSize, Mats.Cardboard(), new Vector3(-1.1f, 0.23f, 0.8f), new Vector3(0, 12f, 0), 0.015f);
            Props.Box(st, ItemKit.CrateSize, Mats.Cardboard(), new Vector3(0.9f, 0.23f, 0.85f), new Vector3(0, -8f, 0), 0.015f);
            Props.Collider(st, new Vector3(3.3f, 1.2f, 1.0f), new Vector3(0, 0.6f, 0));
            _stall = stallGo.AddComponent<FestivalInteract>();
            _stall.Setup(this, true);

            // ---- Packtisch vor der eigenen Tür ------------------------------------------------
            var packGo = Props.Node(_root, "Packtisch", PackTablePos[stage]);
            var pt = packGo.transform;
            Props.Box(pt, new Vector3(1.6f, 0.06f, 0.8f), Mats.Std(new Color(0.85f, 0.85f, 0.83f), 0.6f), new Vector3(0, 0.82f, 0));
            foreach (float x in new[] { -0.72f, 0.72f })
                Props.Box(pt, new Vector3(0.05f, 0.8f, 0.7f), metal, new Vector3(x, 0.4f, 0));
            Props.Box(pt, ItemKit.CrateSize * 0.8f, Mats.Cardboard(), new Vector3(-0.35f, 1.03f, 0), new Vector3(0, 8f, 0), 0.012f);
            Props.Box(pt, new Vector3(0.5f, 0.02f, 0.4f), Mats.Cardboard(), new Vector3(0.4f, 0.86f, 0.05f), new Vector3(0, -15f, 0), 0f);
            Props.Cyl(pt, 0.06f, 0.06f, 0.05f, Mats.Std(new Color(0.8f, 0.7f, 0.45f), 0.4f), new Vector3(0.45f, 0.88f, -0.2f), new Vector3(90f, 0, 0));
            _packInfo = Label3D.Create(pt, "FESTKISTEN PACKEN", 50f, new Color(1f, 0.85f, 0.35f), new Vector3(0, 1.75f, 0), true, 25f, false, 1.2f);
            Props.Collider(pt, new Vector3(1.6f, 1.2f, 0.8f), new Vector3(0, 0.6f, 0));
            _packTable = packGo.AddComponent<FestivalInteract>();
            _packTable.Setup(this, false);
        }

        private void RefreshGoods(Sim sim)
        {
            if (_goods == null || sim == null) return;
            var sb = new System.Text.StringBuilder();
            foreach (var p in GameData.Products)
            {
                int q = sim.FestivalQty(p.Id);
                if (q > 0) sb.Append(p.Id).Append(Mathf.Min(6, (q + 4) / 5)).Append(',');
            }
            string sig = sb.ToString();
            if (sig == _goodsSig) return;
            _goodsSig = sig;
            for (int i = _goods.childCount - 1; i >= 0; i--) Destroy(_goods.GetChild(i).gameObject);
            int slot = 0;
            foreach (var p in GameData.Products)
            {
                int q = sim.FestivalQty(p.Id);
                int n = Mathf.Min(6, (q + 4) / 5);
                for (int b = 0; b < n && slot < 24; b++, slot++)
                {
                    GameObject item;
                    try
                    {
                        item = ItemKit.Build(_goods, new ItemData { Kind = ItemKind.Item, Product = p.Id }, false);
                    }
                    catch (System.Exception)
                    {
                        item = Props.Box(_goods, ItemKit.ItemSize, Mats.Std(p.Color.ToColor(), 0.6f)).gameObject;
                    }
                    int row = slot / 12, col = slot % 12;
                    item.transform.localPosition = new Vector3(-1.4f + col * 0.255f, 0.07f + row * 0.14f, -0.15f + row * 0.2f);
                    item.transform.localRotation = Quaternion.Euler(0, (slot * 17) % 20 - 10, 0);
                }
            }
        }

        // =====================================================================================
        // Interaktion (von FestivalInteract aufgerufen)
        // =====================================================================================
        private string _packProduct = "";

        public string StallPrompt(PlayerController player)
        {
            var sim = Game.Sim;
            if (sim == null || player == null) return "";
            var held = player.Held;
            if (held != null && held.Kind == ItemKind.Crate)
                return "Kiste auspacken (" + held.Quantity + "× " + ProductName(held.Product) + " · Stand " + sim.FestivalTotal() + "/" + GameData.FestivalCapacity + ")";
            if (held != null) return "Hände voll – bring eine Kiste";
            if (sim.Festival == FestivalPhase.Prep) return "Stand: " + sim.FestivalTotal() + " Artikel · Start in " + sim.FestivalCountdown();
            return "Stand: " + sim.FestivalTotal() + " Artikel · " + sim.FestivalStats.Sold + " verkauft (" + Fmt.Money(sim.FestivalStats.Revenue) + ")";
        }

        public void StallInteract(PlayerController player)
        {
            var sim = Game.Sim;
            if (sim == null || player == null) return;
            var held = player.Held;
            if (held == null || held.Kind != ItemKind.Crate)
            {
                sim.Notify("Pack am Packtisch vor deiner Tür eine Festkiste (oder nimm eine Lieferkiste) und bring sie her.", "info");
                return;
            }
            string pname = ProductName(held.Product);
            int put = sim.FestivalAddCrate(held);
            if (put <= 0) return;
            Game.Sound("place");
            if (held.Quantity <= 0) player.ClearHands();
            else player.Hold(held);
            sim.Notify(put + "× " + pname + " am Stand ausgelegt.", "good");
            SyncVisuals(sim, _shownPhase);
        }

        public string PackPrompt(PlayerController player)
        {
            var sim = Game.Sim;
            if (sim == null || player == null) return "";
            if (player.Held != null) return "Hände frei machen zum Packen";
            if (!IsProductOk(sim, _packProduct)) _packProduct = sim.FestivalNextPackProduct();
            if (string.IsNullOrEmpty(_packProduct)) return "Lager ist leer – nichts zum Einpacken";
            int n = Mathf.Min(GameData.FestivalCrateSize, sim.StockQty(_packProduct));
            string next = sim.FestivalNextPackProduct(_packProduct);
            string other = next != "" && next != _packProduct ? " · danach: " + ProductName(next) : "";
            return "Festkiste packen: " + n + "× " + ProductName(_packProduct) + " (" + sim.StockQty(_packProduct) + " auf Lager)" + other;
        }

        public void PackInteract(PlayerController player)
        {
            var sim = Game.Sim;
            if (sim == null || player == null) return;
            if (player.Held != null)
            {
                sim.Notify("Zum Packen brauchst du freie Hände.", "info");
                return;
            }
            if (!IsProductOk(sim, _packProduct)) _packProduct = sim.FestivalNextPackProduct();
            if (string.IsNullOrEmpty(_packProduct))
            {
                sim.Notify("Nichts auf Lager. Bestell Ware im Laptop oder bring Lieferkisten direkt zum Stand.", "info");
                return;
            }
            var crate = sim.FestivalPackCrate(_packProduct);
            if (crate == null) return;
            player.Hold(crate);
            Game.Sound("pickup");
            // Nächstes Mal das nächste Produkt anbieten, damit der Stand bunt wird.
            _packProduct = sim.FestivalNextPackProduct(_packProduct);
        }

        private static bool IsProductOk(Sim sim, string pid) => !string.IsNullOrEmpty(pid) && GameData.IsProduct(pid) && sim.StockQty(pid) > 0;
        private static string ProductName(string pid) => !string.IsNullOrEmpty(pid) && GameData.IsProduct(pid) ? GameData.Product(pid).Name : "?";

        // =====================================================================================
        // Besucher
        // =====================================================================================
        private float Rand(float a, float b) => a + (b - a) * (float)_rng.NextDouble();

        private void UpdateVisitors(Sim sim, FestivalPhase phase, float dt)
        {
            if (phase != FestivalPhase.Live)
            {
                // Nach dem Fest laufen die letzten Besucher noch weg.
                for (int i = _visitors.Count - 1; i >= 0; i--)
                {
                    var v = _visitors[i];
                    if (v.Npc == null)
                    {
                        _visitors.RemoveAt(i);
                        continue;
                    }
                    if (v.State != 2) Leave(v);
                    if (v.Npc.IsAt(v.Target, 0.4f)) Remove(i);
                }
                return;
            }
            // Spawnen: mehr Besucher mit Bekanntheit und Bewertung.
            _spawnAcc -= dt;
            if (_spawnAcc <= 0f)
            {
                _spawnAcc = Rand(1.1f, 2.2f) / Mathf.Max(0.3f, sim.FestivalCrowdFactor());
                if (_visitors.Count < MaxVisitors) Spawn();
            }
            for (int i = _visitors.Count - 1; i >= 0; i--)
            {
                var v = _visitors[i];
                if (v.Npc == null)
                {
                    FreeSlot(v);
                    _visitors.RemoveAt(i);
                    continue;
                }
                if (v.FollowUp != null && Time.time >= v.FollowUpAt)
                {
                    v.Npc.Say(v.FollowUp, 2.5f);
                    v.FollowUp = null;
                }
                switch (v.State)
                {
                    case 0:
                        if (v.Slot < 0 && !TryTakeSlot(v))
                        {
                            v.Patience -= dt;
                            if (v.Patience <= 0f) Leave(v);
                            else if (v.Npc.IsAt(v.Target, 0.3f)) Wander(v);
                        }
                        else if (v.Npc.IsAt(v.Target, 0.12f))
                        {
                            v.State = 1;
                            v.Timer = Rand(1.6f, 2.8f);
                            v.Npc.Stop(v.Timer + 1f, _stall != null ? _stall.transform : null);
                            if (_rng.NextDouble() < 0.35) v.Npc.Say(GameData.FestivalLookShouts[_rng.Next(GameData.FestivalLookShouts.Length)], 1.8f);
                        }
                        break;
                    case 1:
                        v.Timer -= dt;
                        if (v.Timer <= 0f)
                        {
                            Decide(sim, v);
                            Leave(v);
                        }
                        break;
                    case 2:
                        if (v.Npc.IsAt(v.Target, 0.4f)) Remove(i);
                        break;
                    case 3:
                        v.Timer -= dt;
                        v.Patience -= dt;
                        if (v.Timer <= 0f)
                        {
                            if (v.Patience <= 0f) Leave(v);
                            else if (!TryTakeSlot(v)) Wander(v);
                        }
                        break;
                }
            }
        }

        private void Spawn()
        {
            bool west = _rng.NextDouble() < 0.5;
            var start = new Vector3(west ? -42f : 38f, 0, Rand(-3f, 3f));
            var go = Props.Node(_visitorsRoot, "Festbesucher", start);
            NPC npc;
            try
            {
                npc = go.AddComponent<NPC>();
                npc.Setup(CharacterKit.RandomLook(_rng), null, Rand(1.1f, 1.6f));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Festbesucher konnte nicht gebaut werden: " + e.Message);
                Destroy(go);
                return;
            }
            go.transform.localPosition = start;
            var v = new Visitor { Npc = npc, State = 0, Patience = Rand(10f, 22f) };
            _visitors.Add(v);
            if (!TryTakeSlot(v)) Wander(v);
        }

        private Vector3 SlotPos(int i)
        {
            float x = StallPos.x - 1.2f + i * (2.4f / (SlotCount - 1));
            return new Vector3(x, 0, StallPos.z - 1.0f);
        }

        private bool TryTakeSlot(Visitor v)
        {
            int start = _rng.Next(SlotCount);
            for (int k = 0; k < SlotCount; k++)
            {
                int i = (start + k) % SlotCount;
                if (_slots[i] != null && _slots[i].Npc != null) continue;
                _slots[i] = v;
                v.Slot = i;
                v.State = 0;
                v.Target = SlotPos(i);
                v.Npc.HoldAt(v.Target);
                return true;
            }
            return false;
        }

        private void FreeSlot(Visitor v)
        {
            if (v.Slot >= 0 && v.Slot < SlotCount && _slots[v.Slot] == v) _slots[v.Slot] = null;
            v.Slot = -1;
        }

        private void Wander(Visitor v)
        {
            v.State = 3;
            v.Timer = Rand(2f, 4.5f);
            v.Target = new Vector3(StallPos.x + Rand(-7f, 7f), 0, Rand(-2.8f, 2.6f));
            v.Npc.HoldAt(v.Target);
        }

        private void Leave(Visitor v)
        {
            FreeSlot(v);
            v.State = 2;
            bool west = v.Npc.transform.localPosition.x < StallPos.x ? _rng.NextDouble() < 0.75 : _rng.NextDouble() < 0.25;
            v.Target = new Vector3(west ? -44f : 40f, 0, Rand(-3f, 3f));
            v.Npc.HoldAt(v.Target);
        }

        private void Remove(int i)
        {
            var v = _visitors[i];
            FreeSlot(v);
            if (v.Npc != null) Destroy(v.Npc.gameObject);
            _visitors.RemoveAt(i);
        }

        private void ClearVisitors(bool immediate)
        {
            for (int i = _visitors.Count - 1; i >= 0; i--)
            {
                if (immediate) Remove(i);
                else if (_visitors[i].Npc != null && _visitors[i].State != 2) Leave(_visitors[i]);
            }
            for (int i = 0; i < SlotCount; i++) _slots[i] = null;
        }

        private void Decide(Sim sim, Visitor v)
        {
            FestivalVisit r;
            try
            {
                r = sim.FestivalVisitor();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Festbesuch fehlgeschlagen: " + e.Message);
                return;
            }
            var npc = v.Npc;
            Vector3 head = npc.transform.position + new Vector3(0, 2.2f, 0);
            switch (r.Outcome)
            {
                case FestivalOutcome.Bought:
                    npc.Say(GameData.FestivalBuyShouts[_rng.Next(GameData.FestivalBuyShouts.Length)], 2.5f);
                    FloatText(head, "+" + Fmt.Money(r.Price * Mathf.Max(1, r.Qty)) + (r.Qty > 1 ? " (" + r.Qty + "×)" : ""), new Color(0.42f, 0.88f, 0.52f));
                    npc.SetBag(true, sim.BrandColor.ToColor());
                    break;
                case FestivalOutcome.HaggledBought:
                    npc.Say(GameData.FestivalHaggleShouts[_rng.Next(GameData.FestivalHaggleShouts.Length)] + " " + Fmt.Money(r.Offer) + "?", 1.6f);
                    v.FollowUp = "Deal!";
                    v.FollowUpAt = Time.time + 1.4f;
                    FloatText(head, "+" + Fmt.Money(r.Price) + " (gefeilscht)", new Color(0.95f, 0.82f, 0.35f));
                    npc.SetBag(true, sim.BrandColor.ToColor());
                    break;
                case FestivalOutcome.HaggleFailed:
                    npc.Say("Für " + Fmt.Money(r.Offer) + "? Nein? Schade.", 2.5f);
                    break;
                case FestivalOutcome.TooExpensive:
                    npc.Say("Puh, viel zu teuer ...", 2.2f);
                    break;
                case FestivalOutcome.Empty:
                    npc.Say("Schon ausverkauft?", 2.2f);
                    break;
                default:
                    if (_rng.NextDouble() < 0.4) npc.Say("Nichts für mich.", 1.8f);
                    break;
            }
        }

        private static void FloatText(Vector3 pos, string text, Color c)
        {
            if (Game.World != null) Game.World.SpawnFloatText(pos, text, c);
        }
    }

    /// <summary>Interaktionspunkt des Straßenfests (Marktstand oder Packtisch).</summary>
    public sealed class FestivalInteract : MonoBehaviour, IInteractable
    {
        private StreetFestival _owner;
        private bool _isStall;
        private readonly Highlighter _hl = new Highlighter();

        public void Setup(StreetFestival owner, bool isStall)
        {
            _owner = owner;
            _isStall = isStall;
            _hl.Collect(transform);
        }

        public string Title => _isStall ? "Marktstand" : "Festkisten-Packtisch";

        public string Prompt(PlayerController player)
        {
            if (_owner == null) return "";
            return _isStall ? _owner.StallPrompt(player) : _owner.PackPrompt(player);
        }

        public void Interact(PlayerController player)
        {
            if (_owner == null) return;
            if (_isStall) _owner.StallInteract(player);
            else _owner.PackInteract(player);
        }

        public void SetHighlighted(bool on) => _hl.Set(on);
    }
}
