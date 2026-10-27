using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Zustand vor einer Stations-Aktion (was hielt der Spieler, wie viele Pakete lagen bereit ...).</summary>
    public struct FeelSnap
    {
        public bool Valid;
        public ItemKind Kind;
        public int Count, Packages, Labeled, Qty, Packed, Flat, Pack0, Pack1, Pack2, LabeledValue;
        public string Product;
    }

    /// <summary>
    /// „Juice“ der Kernhandlungen an den Stationen: vergleicht den Zustand vor und nach
    /// <see cref="Station.Interact"/> und spielt passende Sequenzen ab – ohne die Spiellogik anzufassen:
    /// Regal (entnehmen, Kiste auspacken), Packtisch (Karton klappt auf, Produkt fällt hinein, Laschen
    /// schließen, Klebeband zieht sich drüber), Labeldrucker (Etikett gleitet heraus und klatscht aufs Paket),
    /// Packstation (Fach öffnet, Paket rein, Tür knallt zu, Münzen), Falttisch, Förderband, Palette, Stand,
    /// Retouren. Alle Requisiten werden pro Station einmal gebaut und wiederverwendet.
    /// </summary>
    public static class StationFeel
    {
        /// <summary>Wenn false, spielen die Stationen ihre alten Einzel-Sounds selbst.</summary>
        public static bool Enabled => Feel.Enabled;

        private static readonly Vector3 PackPos = new Vector3(-0.3f, 0.925f, 0.05f);
        private static readonly Color TapeColor = new Color(0.86f, 0.74f, 0.46f, 1f);

        // ---- Zustand erfassen -----------------------------------------------------------------------
        public static FeelSnap Before(Station st, PlayerController p)
        {
            var snap = new FeelSnap();
            if (!Feel.Enabled || st == null || p == null) return snap;
            try
            {
                var sim = Game.Sim;
                if (sim == null) return snap;
                snap.Kind = p.Held?.Kind ?? ItemKind.None;
                snap.Count = p.CarryCount;
                snap.Product = p.Held?.Product;
                snap.Qty = p.Held?.Quantity ?? 0;
                foreach (var it in p.Carried())
                {
                    if (it.Kind == ItemKind.Package) snap.Packages++;
                    else if (it.Kind == ItemKind.Labeled)
                    {
                        snap.Labeled++;
                        snap.LabeledValue += it.Price;
                    }
                }
                snap.Packed = sim.PackedCount();
                snap.Flat = sim.FlatTotal();
                if (sim.Packaging != null && sim.Packaging.Length >= 3)
                {
                    snap.Pack0 = sim.Packaging[0];
                    snap.Pack1 = sim.Packaging[1];
                    snap.Pack2 = sim.Packaging[2];
                }
                snap.Valid = true;
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
            return snap;
        }

        /// <summary>Nach der Aktion: Unterschied auswerten und Effekt abspielen. Wirft nie.</summary>
        public static void After(Station st, PlayerController p, FeelSnap b)
        {
            if (!b.Valid || !Feel.Enabled || st == null || p == null) return;
            try
            {
                Dispatch(st, p, b);
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
        }

        private static void Dispatch(Station st, PlayerController p, FeelSnap b)
        {
            var sim = Game.Sim;
            if (sim == null) return;
            var kindNow = p.Held?.Kind ?? ItemKind.None;
            int countNow = p.CarryCount;
            int labeledNow = p.CountCarried(ItemKind.Labeled);
            switch (st.Type)
            {
                case StationType.Regal:
                    if (b.Kind == ItemKind.None && kindNow == ItemKind.Item) ShelfPick(st);
                    else if (b.Kind == ItemKind.Crate && countNow == 0) Unbox(st, b);
                    else if (b.Kind == ItemKind.Item && countNow == 0) Placed(st, W(st, new Vector3(0f, 1f, 0.35f)), 0.02f);
                    break;
                case StationType.Dock:
                    if (b.Kind == ItemKind.None && kindNow == ItemKind.Crate)
                    {
                        FeelFx.Dust(W(st, new Vector3(0f, 0.5f, 0.35f)), 7);
                        Squash(st, 0.04f);
                    }
                    else if (b.Count > 0 && countNow == 0) Placed(st, W(st, new Vector3(0f, 0.5f, 0.35f)), 0.04f);
                    break;
                case StationType.Pack:
                    if (b.Kind == ItemKind.Item && sim.PackedCount() > b.Packed && sim.PackedPackages.Count > 0)
                        PackSequence(st, sim.PackedPackages[sim.PackedPackages.Count - 1]);
                    else if (countNow > b.Count)
                    {
                        int taken = countNow - b.Count;
                        if (taken > 1 || b.Count > 0) FeelPops.Spawn(ContentTop(st, 0.35f), "×" + countNow, FeelPops.Info, 0.8f);
                        Squash(st, 0.06f);
                    }
                    else if (countNow < b.Count) Placed(st, ContentTop(st, 0.15f), 0.07f);
                    break;
                case StationType.Label:
                    if (labeledNow > b.Labeled) LabelSequence(st, labeledNow - b.Labeled);
                    break;
                case StationType.Ship:
                    if (labeledNow < b.Labeled) ShipSequence(st, b.Labeled - labeledNow);
                    break;
                case StationType.Fold:
                    if (sim.FlatTotal() < b.Flat) FoldSequence(st, sim, b);
                    break;
                case StationType.Conveyor:
                    if (labeledNow < b.Labeled)
                    {
                        int n = b.Labeled - labeledNow;
                        var top = W(st, new Vector3(0f, 0.95f, 0f));
                        FeelFx.Dust(top, 5, 0.8f);
                        FeelPops.Spawn(top + Vector3.up * 0.3f, n > 1 ? n + "× aufs Band" : "aufs Band", FeelPops.Muted, 0.75f);
                        FeelAudio.Play("pop", FeelAudio.Ladder(), 0.03f, -8f, 0.08f);
                        CameraFeel.Shake(0.08f, 0.15f);
                    }
                    break;
                case StationType.Stand:
                    if (b.Kind == ItemKind.Crate && (countNow == 0 || (p.Held != null && p.Held.Quantity < b.Qty)))
                    {
                        int put = countNow == 0 ? b.Qty : b.Qty - p.Held.Quantity;
                        var top = W(st, new Vector3(0f, 1.1f, 0.3f));
                        Placed(st, top, 0.08f);
                        if (put > 0) FeelPops.Spawn(top + Vector3.up * 0.25f, "+" + put + " am Stand", FeelPops.Info, 0.85f);
                    }
                    break;
                case StationType.Pallet:
                    if ((b.Kind == ItemKind.Crate || b.Kind == ItemKind.Item) && (countNow == 0 || (p.Held != null && p.Held.Quantity < b.Qty)))
                    {
                        int n = b.Kind == ItemKind.Item ? 1 : (countNow == 0 ? b.Qty : b.Qty - p.Held.Quantity);
                        var top = W(st, new Vector3(0f, 1.1f, 0f));
                        Placed(st, top, 0.06f);
                        FeelFx.Sparkle(top, 8);
                        if (n > 0) FeelPops.Spawn(top + Vector3.up * 0.3f, "+" + n + " Palette", FeelPops.Info, 0.85f);
                    }
                    break;
                case StationType.ReturnTray:
                    if (b.Count > 0 && countNow == 0) Placed(st, W(st, new Vector3(0f, 0.6f, 0.2f)), 0.06f);
                    break;
                case StationType.ReturnDesk:
                    if (b.Kind == ItemKind.Return && countNow == 0)
                    {
                        var top = W(st, new Vector3(0f, 1.05f, 0f));
                        FeelFx.Sparkle(top, 10);
                        FeelPops.Spawn(top + Vector3.up * 0.3f, "B-Ware eingelagert", FeelPops.Info, 0.8f);
                        FeelAudio.Play("stick", 1.05f, 0.06f, -4f);
                    }
                    break;
                case StationType.ReturnBin:
                    if (b.Kind == ItemKind.Return && countNow == 0)
                    {
                        var top = W(st, new Vector3(0f, 1.1f, 0f));
                        FeelFx.Dust(top, 10, 1.2f);
                        FeelPops.Spawn(top + Vector3.up * 0.3f, "Entsorgt", FeelPops.Muted, 0.8f);
                        CameraFeel.Shake(0.15f, 0.2f);
                    }
                    break;
                case StationType.DinerTable:
                    if (b.Kind == ItemKind.Plate && countNow == 0)
                    {
                        var top = W(st, new Vector3(0f, 0.95f, 0.1f));
                        FeelFx.Sparkle(top, 8);
                        FeelPops.Spawn(top + Vector3.up * 0.35f, "Guten Appetit!", FeelPops.Info, 0.8f);
                    }
                    break;
            }
        }

        // ---- Kleine Bausteine -----------------------------------------------------------------------
        private static Vector3 W(Station st, Vector3 local) => st.transform.TransformPoint(local);

        private static Vector3 ContentTop(Station st, float up)
        {
            if (st.Kit != null && st.Kit.Content != null) return st.Kit.Content.position + Vector3.up * up;
            float h = st.Kit != null ? st.Kit.LabelH * 0.6f : 1f;
            return st.transform.position + Vector3.up * (h + up);
        }

        private static void Squash(Station st, float amount)
        {
            if (st.Kit != null && st.Kit.Content != null) FeelTween.Squash(st.Kit.Content, amount, 0.35f);
        }

        private static void Placed(Station st, Vector3 at, float squash)
        {
            FeelFx.Dust(at, 5, 0.8f);
            Squash(st, squash);
        }

        private static void ShelfPick(Station st)
        {
            Squash(st, 0.02f);
            FeelFx.Dust(W(st, new Vector3(0f, 1.05f, 0.4f)), 3, 0.6f);
        }

        private static void Unbox(Station st, FeelSnap b)
        {
            var front = W(st, new Vector3(0f, 0.95f, 0.4f));
            FeelFx.Dust(front, 12, 1.2f);
            FeelFx.Sparkle(front + Vector3.up * 0.2f, 8);
            Squash(st, 0.05f);
            Color c = FeelPops.Info;
            string name = "";
            if (!string.IsNullOrEmpty(b.Product) && GameData.IsProduct(b.Product))
            {
                var pd = GameData.Product(b.Product);
                c = Color.Lerp(pd.Color.ToColor(), Color.white, 0.45f);
                name = " " + pd.Short;
            }
            if (b.Qty > 0) FeelPops.Spawn(front + Vector3.up * 0.45f, "+" + b.Qty + name, c, 1f);
            FeelAudio.Play("box_open", 1f, 0.06f, -2f);
            FeelAudio.Play("pop", 1.1f, 0.05f, -8f, 0.12f);
            CameraFeel.Shake(0.18f, 0.22f);
        }

        // =============================================================================================
        // Karton (Packtisch & Falttisch)
        // =============================================================================================
        private sealed class CartonRig
        {
            public Transform Root, Body, HF, HB, HL, HR, Tape, ProductSlot;
            public Vector3 Size;
            public readonly Dictionary<string, Transform> Products = new Dictionary<string, Transform>();
            public Transform HiddenReal;
            /// <summary>Laufende Sequenz – verspätete Aufrufe einer abgebrochenen Sequenz tun nichts.</summary>
            public int Seq;
        }

        private static readonly Dictionary<long, CartonRig> Cartons = new Dictionary<long, CartonRig>();
        private static readonly List<long> DeadKeys = new List<long>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Cartons.Clear();
            Lockers.Clear();
            Stickers.Clear();
        }

        private static CartonRig GetCarton(Station st, Vector3 size, int tag)
        {
            long key = ((long)st.GetInstanceID() << 8) ^ ((long)Mathf.RoundToInt(size.x * 100f) << 2) ^ tag;
            if (Cartons.TryGetValue(key, out var rig) && rig.Root != null) return rig;
            if (Cartons.Count > 48)
            {
                DeadKeys.Clear();
                foreach (var kv in Cartons)
                    if (kv.Value.Root == null) DeadKeys.Add(kv.Key);
                foreach (var k in DeadKeys) Cartons.Remove(k);
            }
            rig = BuildCarton(st.transform, size);
            Cartons[key] = rig;
            return rig;
        }

        private static CartonRig BuildCarton(Transform parent, Vector3 s)
        {
            var rig = new CartonRig { Size = s };
            var card = Mats.Cardboard();
            var tape = Mats.Std(TapeColor, 0.25f);
            const float th = 0.008f;
            rig.Root = Props.Node(parent, "FeelCarton").transform;
            rig.Body = Props.Node(rig.Root, "Body").transform;
            var body = rig.Body;
            Props.Box(body, new Vector3(s.x, th, s.z), card, new Vector3(0f, th / 2f, 0f), default, 0f);
            Props.Box(body, new Vector3(s.x, s.y, th), card, new Vector3(0f, s.y / 2f, s.z / 2f - th / 2f), default, 0f);
            Props.Box(body, new Vector3(s.x, s.y, th), card, new Vector3(0f, s.y / 2f, -s.z / 2f + th / 2f), default, 0f);
            Props.Box(body, new Vector3(th, s.y, s.z), card, new Vector3(-s.x / 2f + th / 2f, s.y / 2f, 0f), default, 0f);
            Props.Box(body, new Vector3(th, s.y, s.z), card, new Vector3(s.x / 2f - th / 2f, s.y / 2f, 0f), default, 0f);
            rig.HF = Props.Node(body, "HF", new Vector3(0f, s.y, s.z / 2f)).transform;
            Props.Box(rig.HF, new Vector3(s.x, th, s.z / 2f), card, new Vector3(0f, th / 2f, -s.z / 4f), default, 0f);
            rig.HB = Props.Node(body, "HB", new Vector3(0f, s.y, -s.z / 2f)).transform;
            Props.Box(rig.HB, new Vector3(s.x, th, s.z / 2f), card, new Vector3(0f, th / 2f, s.z / 4f), default, 0f);
            rig.HL = Props.Node(body, "HL", new Vector3(-s.x / 2f, s.y - th, 0f)).transform;
            Props.Box(rig.HL, new Vector3(s.x / 2f, th, s.z - 0.01f), card, new Vector3(s.x / 4f, th / 2f, 0f), default, 0f);
            rig.HR = Props.Node(body, "HR", new Vector3(s.x / 2f, s.y - th, 0f)).transform;
            Props.Box(rig.HR, new Vector3(s.x / 2f, th, s.z - 0.01f), card, new Vector3(-s.x / 4f, th / 2f, 0f), default, 0f);
            float len = s.z + 0.05f;
            rig.Tape = Props.Node(body, "Tape", new Vector3(0f, s.y + th + 0.002f, len / 2f)).transform;
            Props.Box(rig.Tape, new Vector3(s.x * 0.26f, 0.003f, len), tape, new Vector3(0f, 0f, -len / 2f), default, 0f, false);
            rig.ProductSlot = Props.Node(rig.Root, "Product").transform;
            rig.Root.gameObject.SetActive(false);
            return rig;
        }

        private static readonly Quaternion OpenF = Quaternion.Euler(118f, 0f, 0f), OpenB = Quaternion.Euler(-118f, 0f, 0f);
        private static readonly Quaternion OpenL = Quaternion.Euler(0f, 0f, 112f), OpenR = Quaternion.Euler(0f, 0f, -112f);

        private static void ResetCarton(CartonRig rig)
        {
            rig.Seq++;
            RevealHidden(rig);
            FeelTween.Kill(rig.Root, false);
            FeelTween.Kill(rig.Body, false);
            FeelTween.Kill(rig.HF, false);
            FeelTween.Kill(rig.HB, false);
            FeelTween.Kill(rig.HL, false);
            FeelTween.Kill(rig.HR, false);
            FeelTween.Kill(rig.Tape, false);
            foreach (var kv in rig.Products)
            {
                if (kv.Value == null) continue;
                FeelTween.Kill(kv.Value, false);
                kv.Value.gameObject.SetActive(false);
            }
            rig.Root.localScale = Vector3.one;
            rig.Body.localScale = Vector3.one;
            rig.Tape.localScale = Vector3.one;
            rig.Tape.gameObject.SetActive(true);
        }

        private static void RevealHidden(CartonRig rig)
        {
            if (rig.HiddenReal == null) return;
            FeelTween.Kill(rig.HiddenReal, true);
            rig.HiddenReal.gameObject.SetActive(true);
            rig.HiddenReal = null;
        }

        private static Transform ProductModel(CartonRig rig, string pid)
        {
            if (string.IsNullOrEmpty(pid) || !GameData.IsProduct(pid)) return null;
            if (rig.Products.TryGetValue(pid, out var t) && t != null) return t;
            if (rig.Products.Count >= 8)
            {
                foreach (var kv in rig.Products)
                    if (kv.Value != null)
                        UnityEngine.Object.Destroy(kv.Value.gameObject);
                rig.Products.Clear();
            }
            var go = ItemKit.Build(rig.ProductSlot, new ItemData { Kind = ItemKind.Item, Product = pid }, false);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(c);
            foreach (var l in go.GetComponentsInChildren<Label3D>(true)) UnityEngine.Object.Destroy(l.gameObject);
            t = go.transform;
            t.gameObject.SetActive(false);
            rig.Products[pid] = t;
            return t;
        }

        /// <summary>
        /// Packtisch: Karton ploppt auf, Produkt fällt hinein (Federn), Seitenlaschen, dann Längslaschen klappen zu,
        /// Klebeband zieht sich mit Ratsch-Geräusch drüber, Karton federt, Funken, dann hüpft er zum Stapel,
        /// wo das echte Paket erscheint.
        /// </summary>
        private static void PackSequence(Station st, ItemData pkg)
        {
            var size = ItemKit.Bounds(pkg);
            var rig = GetCarton(st, size, 1);
            ResetCarton(rig);
            var s = rig.Size;
            rig.Root.localPosition = PackPos;
            rig.Root.localRotation = Quaternion.Euler(0f, -6f, 0f);
            rig.Root.gameObject.SetActive(true);
            rig.HF.localRotation = OpenF;
            rig.HB.localRotation = OpenB;
            rig.HL.localRotation = OpenL;
            rig.HR.localRotation = OpenR;
            rig.Tape.localScale = new Vector3(1f, 1f, 0.001f);
            rig.Tape.gameObject.SetActive(false);
            FeelTween.PopIn(rig.Body, 0.2f, 0f, Vector3.one);
            FeelAudio.Play("fold", 1.15f, 0.05f, -8f);

            // Produkt fällt hinein
            var prod = ProductModel(rig, pkg.Product);
            if (prod != null)
            {
                float rest = 0.008f + ItemKit.ItemSize.y * 0.5f + 0.004f;
                prod.localPosition = new Vector3(0f, s.y + 0.32f, 0f);
                prod.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(-12f, 12f), 0f);
                FeelTween.Show(prod, 0.12f);
                FeelTween.Move(prod, new Vector3(0f, rest, 0f), 0.22f, FeelEase.InQuad, 0.12f);
                FeelTween.Hide(prod, 0.66f);
            }
            var top = rig.Root.TransformPoint(new Vector3(0f, s.y, 0f));
            FeelTween.Squash(rig.Body, 0.12f, 0.25f, 0.34f);
            FeelTween.Call(0.34f, () => FeelFx.Dust(top, 4, 0.6f));
            FeelAudio.Play("place", 1.1f, 0.05f, -6f, 0.34f);

            // Laschen schließen
            FeelTween.Rotate(rig.HL, Quaternion.identity, 0.13f, FeelEase.OutQuad, 0.42f);
            FeelTween.Rotate(rig.HR, Quaternion.identity, 0.13f, FeelEase.OutQuad, 0.45f);
            FeelAudio.Play("fold", 1.3f, 0.05f, -10f, 0.43f);
            FeelTween.Rotate(rig.HF, Quaternion.identity, 0.13f, FeelEase.OutBack, 0.53f);
            FeelTween.Rotate(rig.HB, Quaternion.identity, 0.13f, FeelEase.OutBack, 0.56f);
            FeelAudio.Play("fold", 1.45f, 0.05f, -10f, 0.55f);

            // Klebeband
            FeelTween.Show(rig.Tape, 0.68f);
            FeelTween.Scale(rig.Tape, Vector3.one, 0.3f, FeelEase.OutCubic, 0.68f);
            FeelAudio.Play("tape_rip", 1f, 0.08f, -4f, 0.66f, 0.1f);

            // Fertig!
            FeelTween.Squash(rig.Body, 0.22f, 0.35f, 1.0f);
            FeelTween.Call(1.0f, () =>
            {
                FeelFx.Sparkle(top + Vector3.up * 0.03f, 9);
                CameraFeel.Kick(-1.1f);
            });
            FeelAudio.Play("stick", 0.9f, 0.05f, -4f, 1.0f);

            // Echtes Paket (neuestes im Stapel) erst am Ende zeigen
            Transform real = null;
            var content = st.Kit != null ? st.Kit.Content : null;
            if (content != null && content.childCount > 0)
            {
                real = content.GetChild(content.childCount - 1);
                real.gameObject.SetActive(false);
                rig.HiddenReal = real;
            }
            Vector3 dest = PackPos + new Vector3(0.75f, 0f, 0f);
            Quaternion destRot = rig.Root.localRotation;
            if (real != null)
            {
                dest = st.transform.InverseTransformPoint(real.position) - new Vector3(0f, s.y / 2f, 0f);
                destRot = Quaternion.Inverse(st.transform.rotation) * real.rotation;
            }
            FeelTween.Move(rig.Root, dest, 0.28f, FeelEase.OutQuad, 1.22f, 0.12f);
            FeelTween.Rotate(rig.Root, destRot, 0.28f, FeelEase.OutQuad, 1.22f);
            int seq = rig.Seq;
            FeelTween.Call(1.5f, () =>
            {
                if (rig.Seq != seq) return;
                if (rig.Root != null) rig.Root.gameObject.SetActive(false);
                var r = rig.HiddenReal;
                rig.HiddenReal = null;
                if (r != null)
                {
                    r.gameObject.SetActive(true);
                    FeelTween.Squash(r, 0.16f, 0.3f);
                }
                FeelAudio.Play("place", 1.2f, 0.06f, -10f);
            });
        }

        /// <summary>Falttisch: flacher Karton richtet sich auf, Laschen falten ein, ploppt ins Lager.</summary>
        private static void FoldSequence(Station st, Sim sim, FeelSnap b)
        {
            int size = 0;
            if (sim.Packaging != null && sim.Packaging.Length >= 3)
            {
                if (sim.Packaging[1] > b.Pack1) size = 1;
                else if (sim.Packaging[2] > b.Pack2) size = 2;
            }
            var dims = ItemKit.PackageSizes[Mathf.Clamp(size, 0, ItemKit.PackageSizes.Length - 1)];
            var rig = GetCarton(st, dims, 2);
            ResetCarton(rig);
            var start = new Vector3(-0.22f, 0.9f + Mathf.Min(b.Flat, 12) * 0.014f + 0.01f, 0f);
            rig.Root.localPosition = start;
            rig.Root.localRotation = Quaternion.Euler(0f, 4f, 0f);
            rig.Root.gameObject.SetActive(true);
            rig.Tape.gameObject.SetActive(false);
            var flatOpen = Quaternion.Euler(170f, 0f, 0f);
            rig.HF.localRotation = flatOpen;
            rig.HB.localRotation = Quaternion.Euler(-170f, 0f, 0f);
            rig.HL.localRotation = Quaternion.Euler(0f, 0f, 170f);
            rig.HR.localRotation = Quaternion.Euler(0f, 0f, -170f);
            rig.Body.localScale = new Vector3(1f, 0.05f, 1f);
            FeelTween.Scale(rig.Body, Vector3.one, 0.3f, FeelEase.OutBack);
            FeelTween.Rotate(rig.HL, Quaternion.identity, 0.16f, FeelEase.OutQuad, 0.28f);
            FeelTween.Rotate(rig.HR, Quaternion.identity, 0.16f, FeelEase.OutQuad, 0.31f);
            FeelTween.Rotate(rig.HF, Quaternion.identity, 0.16f, FeelEase.OutBack, 0.4f);
            FeelTween.Rotate(rig.HB, Quaternion.identity, 0.16f, FeelEase.OutBack, 0.43f);
            FeelAudio.Play("fold", 1.25f, 0.06f, -8f, 0.42f);
            FeelTween.Squash(rig.Body, 0.2f, 0.3f, 0.6f);
            var top = rig.Root.TransformPoint(new Vector3(0f, dims.y, 0f));
            FeelTween.Call(0.6f, () =>
            {
                FeelFx.Dust(top, 5, 0.7f);
                FeelPops.Spawn(top + Vector3.up * 0.25f, "+1 Karton " + GameData.SizeName(size), FeelPops.Info, 0.85f);
            });
            FeelAudio.Play("pop", 1.2f, 0.05f, -6f, 0.62f);
            FeelTween.Move(rig.Root, start + new Vector3(0f, 0.35f, 0f), 0.25f, FeelEase.InBack, 0.85f);
            FeelTween.Scale(rig.Root, Vector3.zero, 0.25f, FeelEase.InBack, 0.85f);
            FeelTween.Hide(rig.Root, 1.12f);
        }

        // =============================================================================================
        // Labeldrucker
        // =============================================================================================
        private sealed class StickerRig
        {
            public Transform Root, Sticker;
        }

        private static readonly Dictionary<int, StickerRig> Stickers = new Dictionary<int, StickerRig>();

        private static StickerRig GetSticker(Station st)
        {
            int key = st.GetInstanceID();
            if (Stickers.TryGetValue(key, out var rig) && rig.Root != null) return rig;
            rig = new StickerRig();
            rig.Root = Props.Node(st.transform, "FeelLabel").transform;
            rig.Root.localPosition = new Vector3(-0.1f, 1.0f, 0.12f);
            rig.Root.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            rig.Sticker = Props.Node(rig.Root, "Sticker").transform;
            Props.Box(rig.Sticker, new Vector3(0.15f, 0.003f, 0.1f), Mats.Std(Color.white, 0.6f), Vector3.zero, default, 0f, false);
            var ink = Mats.Std(new Color(0.06f, 0.06f, 0.06f), 0.6f);
            for (int i = 0; i < 6; i++)
                Props.Box(rig.Sticker, new Vector3(0.004f + (i % 3) * 0.003f, 0.004f, 0.04f), ink, new Vector3(-0.045f + i * 0.016f, 0.001f, 0.015f), default, 0f, false);
            Props.Box(rig.Sticker, new Vector3(0.09f, 0.004f, 0.012f), ink, new Vector3(0f, 0.001f, -0.03f), default, 0f, false);
            rig.Root.gameObject.SetActive(false);
            Stickers[key] = rig;
            return rig;
        }

        /// <summary>Etikett gleitet surrend aus dem Drucker, fliegt aufs Paket und klatscht mit Squash drauf.</summary>
        private static void LabelSequence(Station st, int count)
        {
            var rig = GetSticker(st);
            FeelTween.Kill(rig.Root, false);
            FeelTween.Kill(rig.Sticker, false);
            rig.Root.gameObject.SetActive(true);
            rig.Sticker.localPosition = new Vector3(0f, 0f, -0.06f);
            rig.Sticker.localRotation = Quaternion.identity;
            rig.Sticker.localScale = Vector3.one;
            FeelTween.Move(rig.Sticker, new Vector3(0f, 0f, 0.1f), 0.38f, FeelEase.Linear);
            if (AssetLib.SfxVariants("printer") <= 0) FeelAudio.Play("print_whirr", 1f, 0.05f, -4f);
            var visual = st.transform.Find("Visual");
            if (visual != null) FeelTween.Shake(visual, 0.0025f, 0.4f);

            // Zum Paket in der Hand fliegen
            var target = HandFeel.HeldTop();
            var local = rig.Root.InverseTransformPoint(target);
            FeelTween.Move(rig.Sticker, local, 0.17f, FeelEase.InQuad, 0.42f, 0.06f);
            FeelTween.Scale(rig.Sticker, Vector3.one * 0.6f, 0.17f, FeelEase.InQuad, 0.42f);
            FeelTween.Hide(rig.Root, 0.6f);
            FeelTween.Call(0.6f, () =>
            {
                HandFeel.StickSquash(0.2f);
                FeelFx.Sparkle(HandFeel.HeldTop(), 5, Color.white, 0.5f);
                CameraFeel.Kick(-0.6f);
            });
            FeelAudio.Play("stick", 1f, 0.08f, -3f, 0.6f);
            if (count > 1) FeelPops.Spawn(W(st, new Vector3(-0.1f, 1.35f, 0.1f)), count + "× etikettiert", FeelPops.Info, 0.85f, 0.3f);
        }

        // =============================================================================================
        // Versand: Packstation-Fach / Abholung / Schalter
        // =============================================================================================
        private sealed class LockerRig
        {
            public Transform Root, Hinge, Parcel;
        }

        private static readonly Dictionary<int, LockerRig> Lockers = new Dictionary<int, LockerRig>();

        private static LockerRig GetLocker(Station st)
        {
            int key = st.GetInstanceID();
            if (Lockers.TryGetValue(key, out var rig) && rig.Root != null) return rig;
            rig = new LockerRig();
            rig.Root = Props.Node(st.transform, "FeelLocker").transform;
            Props.Box(rig.Root, new Vector3(0.5f, 0.4f, 0.012f), Mats.Std(new Color(0.07f, 0.07f, 0.08f), 0.9f), new Vector3(0f, 0f, 0.016f), default, 0f, false);
            rig.Hinge = Props.Node(rig.Root, "Hinge", new Vector3(-0.26f, 0f, 0.04f)).transform;
            Props.Box(rig.Hinge, new Vector3(0.52f, 0.42f, 0.02f), Mats.Std(new Color(0.92f, 0.72f, 0.09f), 0.5f), new Vector3(0.26f, 0f, 0f), default, 0.004f);
            Props.Box(rig.Hinge, new Vector3(0.03f, 0.1f, 0.02f), Mats.DarkMetal(), new Vector3(0.47f, 0f, 0.02f), default, 0.004f, false);
            rig.Parcel = Props.Node(rig.Root, "Parcel").transform;
            Props.Box(rig.Parcel, new Vector3(0.28f, 0.2f, 0.22f), Mats.Cardboard(), Vector3.zero, default, 0.01f);
            Props.Box(rig.Parcel, new Vector3(0.14f, 0.004f, 0.1f), Mats.Std(Color.white, 0.6f), new Vector3(-0.03f, 0.102f, 0.02f), default, 0f, false);
            rig.Root.gameObject.SetActive(false);
            Lockers[key] = rig;
            return rig;
        }

        private static void ShipSequence(Station st, int count)
        {
            int stage = st.Opts != null ? st.Opts.Stage : 0;
            if (stage == PostService.StagePackstation) LockerSequence(st);
            else if (stage == PostService.StageCounter)
            {
                var at = W(st, new Vector3(0.7f, 1.2f, 0f));
                FeelFx.Dust(at, 6, 0.8f);
                FeelAudio.Play("stamp", 1f, 0.08f, -2f, 0.1f);
                CameraFeel.Shake(0.1f, 0.15f);
            }
            else
            {
                var at = W(st, new Vector3(0f, 1.2f, 0.3f));
                FeelFx.Dust(at, 8, 1f);
                Squash(st, 0.08f);
                FeelAudio.Play("locker", 0.85f, 0.06f, -4f, 0.05f);
                CameraFeel.Shake(0.15f, 0.2f);
            }
            FeelAudio.Play(count > 1 ? "coin_cascade" : "coin", count > 1 ? 1f : FeelAudio.Ladder(), 0.05f, -4f, 0.45f, 0.12f);
        }

        /// <summary>Packstation: ein Fach klappt auf, das Paket rutscht hinein, die Tür knallt zu.</summary>
        private static void LockerSequence(Station st)
        {
            var rig = GetLocker(st);
            FeelTween.Kill(rig.Root, true);
            FeelTween.Kill(rig.Hinge, false);
            FeelTween.Kill(rig.Parcel, false);
            int c, r;
            do
            {
                c = UnityEngine.Random.Range(0, 4);
                r = UnityEngine.Random.Range(0, 3);
            } while (c == 1 && r == 2);
            rig.Root.localPosition = new Vector3(-0.85f + c * 0.57f, 0.35f + r * 0.47f, 0.31f);
            rig.Root.localRotation = Quaternion.identity;
            rig.Root.gameObject.SetActive(true);
            rig.Hinge.localRotation = Quaternion.identity;
            rig.Parcel.gameObject.SetActive(false);
            FeelTween.Rotate(rig.Hinge, Quaternion.Euler(0f, -105f, 0f), 0.22f, FeelEase.OutBack, 0.05f);
            FeelAudio.Play("locker", 1.25f, 0.06f, -8f, 0.05f);
            rig.Parcel.localPosition = new Vector3(0f, 0f, 0.42f);
            FeelTween.Show(rig.Parcel, 0.24f);
            FeelTween.Move(rig.Parcel, new Vector3(0f, 0f, -0.16f), 0.2f, FeelEase.InQuad, 0.24f);
            FeelTween.Hide(rig.Parcel, 0.46f);
            FeelTween.Rotate(rig.Hinge, Quaternion.identity, 0.12f, FeelEase.InQuad, 0.5f);
            var front = rig.Root.TransformPoint(new Vector3(0f, -0.15f, 0.12f));
            FeelTween.Call(0.62f, () =>
            {
                if (rig.Root != null) FeelTween.Shake(rig.Root, 0.006f, 0.2f);
                FeelFx.Dust(front, 6, 0.7f);
                CameraFeel.Shake(0.14f, 0.18f);
            });
            FeelAudio.Play("locker", 0.9f, 0.06f, 0f, 0.62f);
            FeelTween.Hide(rig.Root, 0.95f);
        }
    }
}
