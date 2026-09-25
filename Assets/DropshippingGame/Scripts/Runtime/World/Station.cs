using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Eine interaktive Station (Laptop, Regal, Packtisch ...). Der Spieler zielt mit dem
    /// Fadenkreuz darauf: Prompt() liefert den Hinweistext, Interact() führt die Aktion aus,
    /// SetHighlighted() lässt die Station aufleuchten. Dynamische Inhalte (Kisten im Regal,
    /// Pakete auf dem Tisch) werden bei jeder Wirtschaftsänderung aktualisiert.
    /// </summary>
    public sealed class Station : MonoBehaviour, IInteractable
    {
        public StationType Type;
        public StationOpts Opts = new StationOpts();
        public StationKitResult Kit;
        private readonly Highlighter _hl = new Highlighter();
        private string _sig = "-";
        private Sim _sim;

        private static Sim S => Game.Sim;

        public void Setup(StationType type, StationOpts opts)
        {
            Type = type;
            Opts = opts ?? new StationOpts();
            var visual = Props.Node(transform, "Visual");
            Kit = StationKit.Build(visual.transform, type, Opts);
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = Kit.Size;
            col.center = Kit.Center;
            _hl.Collect(visual.transform, Kit.Content);
            _sim = Game.Sim;
            if (_sim != null) _sim.EconomyChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_sim != null) _sim.EconomyChanged -= Refresh;
        }

        public string ProductId => GameData.Products[Opts.ProductIndex].Id;
        public NPC Npc => Kit?.Npc;

        public string Title
        {
            get
            {
                switch (Type)
                {
                    case StationType.Pc: return Opts.Stage == 0 ? "Laptop" : "Büro-PC";
                    case StationType.Dock: return "Wareneingang";
                    case StationType.Regal: return "Regal: " + GameData.Products[Opts.ProductIndex].Name;
                    case StationType.Pack: return "Packtisch";
                    case StationType.Label: return "Labeldrucker";
                    case StationType.Ship: return Opts.Stage == 0 ? "PaketBlitz-Abgabe" : "Versandkäfig";
                    case StationType.Fold: return "Falttisch";
                    case StationType.Conveyor: return "Förderband";
                    case StationType.DinerPass: return "Durchreiche";
                    case StationType.DinerTable: return "Tisch " + Opts.TableId;
                    case StationType.NpcTalk: return "Kalle (Chef)";
                    case StationType.EndDay: return Opts.Stage == 0 ? "Matratze" : "Stempeluhr";
                    case StationType.Stand: return "Verkaufsstand";
                }
                return "";
            }
        }

        public void SetHighlighted(bool on) => _hl.Set(on);

        private bool IsBusiness => Type <= StationType.Conveyor || Type == StationType.EndDay || Type == StationType.Stand;

        // ---- Hinweistext ------------------------------------------------------------------------
        public string Prompt(PlayerController player)
        {
            var gm = S;
            if (gm == null) return "";
            var held = player.Held;
            var kind = held?.Kind ?? ItemKind.None;
            if (IsBusiness && gm.StoryStage == "diner") return "Erst die Schicht bei Kalle beenden";
            switch (Type)
            {
                case StationType.Pc:
                    return Opts.Stage == 0 ? "Laptop öffnen" : "PC benutzen";
                case StationType.Dock:
                    if (kind == ItemKind.Crate) return "Kiste zurückstellen";
                    if (kind != ItemKind.None) return "Hände voll";
                    if (gm.DockCrates.Count == 0) return gm.TravelingDeliveries.Count == 0 ? "Keine Lieferung da" : "Lieferung ist unterwegs ...";
                    var c = gm.DockCrates[0];
                    return "Kiste aufnehmen (" + c.Quantity + "× " + GameData.Product(c.Product).Name + ")";
                case StationType.Regal:
                {
                    string pid = ProductId;
                    var p = GameData.Products[Opts.ProductIndex];
                    if (!gm.ProductUnlocked(pid)) return "Gesperrt – ab Firmenlevel " + p.UnlockLevel;
                    if (kind == ItemKind.Crate)
                    {
                        if (held.Product != pid) return "Falsches Regal (Kiste: " + GameData.Product(held.Product).Name + ")";
                        return "Kiste einräumen (+" + held.Quantity + ")";
                    }
                    if (kind == ItemKind.Item) return held.Product == pid ? "Zurücklegen" : "Falsches Regal";
                    if (kind != ItemKind.None) return "Hände voll";
                    int pending = gm.PendingCountFor(pid);
                    if (gm.StockQty(pid) <= 0) return "Regal leer – im Laptop nachbestellen";
                    if (pending <= 0) return p.Name + ": " + gm.StockQty(pid) + " auf Lager · keine Bestellung";
                    return p.Name + " entnehmen (" + pending + " offen)";
                }
                case StationType.Pack:
                    if (kind == ItemKind.Item)
                    {
                        int size = GameData.Product(held.Product).Size;
                        return "Verpacken (Karton " + GameData.SizeName(size) + " · " + gm.Packaging[size] + " übrig)";
                    }
                    if (kind == ItemKind.Package) return "Paket zurücklegen";
                    if (kind != ItemKind.None) return "Hände voll";
                    return gm.PackedCount() > 0 ? "Paket nehmen (" + gm.PackedCount() + " bereit)" : "Bring einen Artikel zum Verpacken";
                case StationType.Label:
                    if (kind == ItemKind.Package) return "Versandlabel drucken";
                    if (kind == ItemKind.Labeled) return "Hat schon ein Label";
                    return "Bring ein verpacktes Paket hierher";
                case StationType.Ship:
                    if (kind == ItemKind.Labeled) return "Paket abgeben (+" + Fmt.Money(held.Price) + ")";
                    if (kind == ItemKind.Package) return "Erst ein Versandlabel drucken!";
                    return "Etikettierte Pakete hier abgeben";
                case StationType.Fold:
                    if (kind != ItemKind.None) return "Hände frei machen zum Falten";
                    return gm.FlatTotal() > 0 ? "Karton falten (" + gm.FlatTotal() + " ungefaltet)" : "Keine ungefalteten Kartons";
                case StationType.Conveyor:
                    if (kind == ItemKind.Labeled) return "Aufs Förderband legen";
                    if (kind == ItemKind.Package) return "Erst etikettieren";
                    return "Etikettierte Pakete aufs Band legen";
                case StationType.DinerPass:
                    if (gm.StoryStage != "diner") return "Die Küche ist nicht mehr dein Problem";
                    if (kind == ItemKind.Plate) return "Bring den Teller zu Tisch " + held.Table;
                    if (gm.IntroStep != 1) return "Sprich zuerst mit Kalle";
                    return gm.DinerPlatesWaiting() > 0 ? "Teller nehmen" : "Keine Teller mehr";
                case StationType.DinerTable:
                    if (kind == ItemKind.Plate)
                        return held.Table == Opts.TableId ? "Teller servieren" : "Falscher Tisch (Teller für Tisch " + held.Table + ")";
                    return "Tisch " + Opts.TableId;
                case StationType.NpcTalk:
                    return "Mit Kalle sprechen";
                case StationType.EndDay:
                    return "Feierabend machen (Tag beenden)";
                case StationType.Stand:
                    if (!gm.HasUpgrade("stand")) return "Verkaufsstand – in der Ausbau-App kaufen";
                    if (kind == ItemKind.Crate) return "Kiste auf den Stand stellen (" + gm.StandTotal() + "/" + GameData.StandCapacity + ")";
                    return gm.StandTotal() > 0 ? "Stand: " + gm.StandTotal() + " Artikel – Passanten kaufen hier" : "Stell eine Kiste auf den Stand";
            }
            return "";
        }

        // ---- Aktion -----------------------------------------------------------------------------
        public void Interact(PlayerController player)
        {
            var gm = S;
            if (gm == null) return;
            var held = player.Held;
            var kind = held?.Kind ?? ItemKind.None;
            if (IsBusiness && gm.StoryStage == "diner")
            {
                gm.Notify("Die Schicht ist noch nicht vorbei. Kalle wartet!", "bad");
                return;
            }
            switch (Type)
            {
                case StationType.Pc:
                    gm.OpenPc();
                    break;
                case StationType.Dock:
                    if (kind == ItemKind.Crate)
                    {
                        gm.ReturnCrate(held);
                        player.ClearHands();
                        Game.Sound("place");
                    }
                    else if (kind != ItemKind.None) gm.Notify("Hände sind voll.", "bad");
                    else
                    {
                        var crate = gm.PickupCrate();
                        if (crate != null)
                        {
                            player.Hold(crate);
                            Game.Sound("pickup");
                        }
                    }
                    break;
                case StationType.Regal:
                    InteractRegal(player, kind, held);
                    break;
                case StationType.Pack:
                    if (kind == ItemKind.Item)
                    {
                        if (gm.WrapItem(held))
                        {
                            player.ClearHands();
                            Game.Sound("tape");
                        }
                    }
                    else if (kind == ItemKind.Package)
                    {
                        gm.ReturnPackage(held);
                        player.ClearHands();
                        Game.Sound("place");
                    }
                    else if (kind == ItemKind.None)
                    {
                        var pkg = gm.PickupPackage();
                        if (pkg != null)
                        {
                            player.Hold(pkg);
                            Game.Sound("pickup");
                        }
                    }
                    else gm.Notify("Damit kannst du hier nichts anfangen.", "info");
                    break;
                case StationType.Label:
                    if (kind == ItemKind.Package)
                    {
                        var labeled = held.Clone();
                        labeled.Kind = ItemKind.Labeled;
                        player.Hold(labeled);
                        gm.OnLabeled();
                        Game.Sound("printer");
                    }
                    else gm.Notify("Bring ein verpacktes Paket zum Labeldrucker.", "info");
                    break;
                case StationType.Ship:
                    if (kind == ItemKind.Labeled)
                    {
                        gm.ShipPackage(held, (transform.position + new Vector3(0, 1.8f, 0)).ToV3());
                        player.ClearHands();
                    }
                    else if (kind == ItemKind.Package)
                    {
                        gm.Notify("Ohne Versandlabel nimmt PaketBlitz nichts an!", "bad");
                        Game.Sound("error");
                    }
                    else gm.Notify("Hier gibst du etikettierte Pakete ab.", "info");
                    break;
                case StationType.Fold:
                    if (kind != ItemKind.None) gm.Notify("Zum Falten brauchst du freie Hände.", "info");
                    else gm.FoldCarton();
                    break;
                case StationType.Conveyor:
                    if (kind == ItemKind.Labeled)
                    {
                        if (gm.ConveyorInsert(held))
                        {
                            player.ClearHands();
                            Game.Sound("place");
                        }
                    }
                    else gm.Notify("Nur etikettierte Pakete aufs Band legen.", "info");
                    break;
                case StationType.DinerPass:
                    if (gm.StoryStage == "diner" && kind == ItemKind.None)
                    {
                        var plate = gm.DinerTakePlate();
                        if (plate != null)
                        {
                            player.Hold(plate);
                            Game.Sound("plate");
                        }
                    }
                    break;
                case StationType.DinerTable:
                    if (kind == ItemKind.Plate)
                    {
                        if (gm.DinerServe(Opts.TableId, held))
                        {
                            player.ClearHands();
                            Game.Sound("plate");
                        }
                        else gm.Notify("Das ist Tisch " + Opts.TableId + " – der Teller gehört an Tisch " + held.Table + ".", "bad");
                    }
                    break;
                case StationType.NpcTalk:
                    gm.RequestDialogue("kalle");
                    break;
                case StationType.EndDay:
                    gm.RequestEndDay();
                    break;
                case StationType.Stand:
                    if (!gm.HasUpgrade("stand"))
                    {
                        gm.Notify("Den Verkaufsstand kaufst du im Laptop unter 'Ausbau' (ab Level 2).", "info");
                        break;
                    }
                    if (kind == ItemKind.Crate)
                    {
                        int put = gm.StandAddCrate(held);
                        if (put > 0)
                        {
                            Game.Sound("place");
                            if (held.Quantity <= 0) player.ClearHands();
                            else player.Hold(held);
                            gm.Notify(put + "× " + GameData.Product(held.Product).Name + " auf den Stand gestellt.", "good");
                        }
                    }
                    else gm.Notify("Stell eine volle Kiste auf den Stand. Passanten kaufen zu deinem Webshop-Preis.", "info");
                    break;
            }
        }

        private void InteractRegal(PlayerController player, ItemKind kind, ItemData held)
        {
            var gm = S;
            string pid = ProductId;
            if (!gm.ProductUnlocked(pid))
            {
                gm.Notify("Dieses Produkt wird erst ab Firmenlevel " + GameData.Products[Opts.ProductIndex].UnlockLevel + " freigeschaltet.", "info");
                return;
            }
            switch (kind)
            {
                case ItemKind.Crate:
                    if (held.Product != pid)
                    {
                        gm.Notify("Falsches Regal! Die Kiste gehört zu '" + GameData.Product(held.Product).Name + "'.", "bad");
                        Game.Sound("error");
                        return;
                    }
                    if (gm.UnboxCrate(pid, held.Quantity, held.Quality))
                    {
                        player.ClearHands();
                        Game.Sound("place");
                    }
                    break;
                case ItemKind.Item:
                    if (held.Product == pid)
                    {
                        gm.ReturnItem(held);
                        player.ClearHands();
                        Game.Sound("place");
                    }
                    else gm.Notify("Das gehört in ein anderes Regal.", "bad");
                    break;
                case ItemKind.None:
                    var item = gm.PickItem(pid);
                    if (item != null)
                    {
                        player.Hold(item);
                        Game.Sound("pickup");
                    }
                    break;
                default:
                    gm.Notify("Damit kannst du hier nichts anfangen.", "info");
                    break;
            }
        }

        // ---- Dynamische Inhalte -------------------------------------------------------------------
        private string Signature()
        {
            var gm = S;
            switch (Type)
            {
                case StationType.Dock:
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var c in gm.DockCrates) sb.Append(c.Product).Append(',');
                    return sb.ToString();
                }
                case StationType.Regal: return RegalVisible().ToString();
                case StationType.Pack:
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var p in gm.PackedPackages) sb.Append(p.Color.ToHex()).Append(p.Product);
                    return sb.ToString();
                }
                case StationType.Fold: return Mathf.Min(gm.FlatTotal(), 12).ToString();
                case StationType.Ship: return Mathf.Min(gm.Daily.Shipped, 10).ToString();
                case StationType.DinerPass: return gm.DinerPlatesWaiting() + gm.StoryStage;
                case StationType.DinerTable: return gm.DinerTableState(Opts.TableId).ToString();
                case StationType.Stand:
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var p in GameData.Products)
                    {
                        int q = gm.StandStock.TryGetValue(p.Id, out int v) ? v : 0;
                        sb.Append(Mathf.Min(8, (q + 3) / 4)).Append(q > 0 ? gm.StandPrice(p.Id) : 0).Append(',');
                    }
                    return sb.ToString() + gm.HasUpgrade("stand");
                }
            }
            return "";
        }

        private int RegalVisible()
        {
            int qty = S.StockQty(ProductId);
            if (qty <= 0) return 0;
            if (Opts.Stage == 0) return Mathf.Clamp(Mathf.CeilToInt(qty / 10f), 1, 20);
            return Mathf.Clamp(Mathf.CeilToInt(qty / 25f), 1, 36);
        }

        public void Refresh()
        {
            if (this == null || Kit == null || Kit.Content == null || S == null) return;
            string sig = Signature();
            if (sig == _sig) return;
            _sig = sig;
            for (int i = Kit.Content.childCount - 1; i >= 0; i--) Destroy(Kit.Content.GetChild(i).gameObject);
            switch (Type)
            {
                case StationType.Dock: FillDock(); break;
                case StationType.Regal: FillRegal(); break;
                case StationType.Pack: FillPacked(); break;
                case StationType.Fold: FillFlat(); break;
                case StationType.Ship: FillShip(); break;
                case StationType.DinerPass: FillPlates(); break;
                case StationType.DinerTable: FillTable(); break;
                case StationType.Stand: FillStand(); break;
            }
        }

        private void FillDock()
        {
            var crates = S.DockCrates;
            var slots = new System.Collections.Generic.List<Vector3>();
            if (Opts.Stage == 0)
            {
                for (int layer = 0; layer < 3; layer++)
                foreach (float sx in new[] { -0.3f, 0.3f })
                    slots.Add(new Vector3(sx, layer * 0.46f, 0));
            }
            else
            {
                for (int layer = 0; layer < 2; layer++)
                foreach (float px in new[] { -0.65f, 0.65f })
                foreach (float sz in new[] { -0.24f, 0.24f })
                    slots.Add(new Vector3(px, layer * 0.46f, sz));
            }
            for (int i = 0; i < Mathf.Min(crates.Count, slots.Count); i++)
            {
                var cr = Props.Crate(Kit.Content, GameData.Product(crates[i].Product).Color.ToColor());
                cr.transform.localPosition = slots[i];
            }
        }

        private void FillRegal()
        {
            int n = RegalVisible();
            var col = GameData.Products[Opts.ProductIndex].Color.ToColor();
            var body = Mats.Std(col, 0.55f);
            var band = Mats.Std(new Color(0.97f, 0.97f, 0.96f), 0.5f);
            if (Opts.Stage == 0)
            {
                for (int i = 0; i < n; i++)
                {
                    int level = i / 5, slot = i % 5;
                    var pos = new Vector3(-0.72f + slot * 0.36f, 0.115f + level * 0.5f + 0.11f, 0);
                    Props.Box(Kit.Content, new Vector3(0.3f, 0.22f, 0.34f), body, pos, default, 0.012f);
                    Props.Box(Kit.Content, new Vector3(0.302f, 0.05f, 0.342f), band, pos + new Vector3(0, 0.04f, 0), default, 0f, false);
                }
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    int level = i / 12, rest = i % 12, pal = rest / 6, k = rest % 6;
                    float x = (pal == 0 ? -1f : 1f) + (-0.42f + (k % 3) * 0.42f);
                    float z = -0.22f + (k / 3) * 0.44f;
                    var pos = new Vector3(x, 0.15f + level * 1.15f + 0.05f + 0.18f, z);
                    Props.Box(Kit.Content, new Vector3(0.4f, 0.36f, 0.4f), body, pos, default, 0.015f);
                    Props.Box(Kit.Content, new Vector3(0.402f, 0.07f, 0.402f), band, pos + new Vector3(0, 0.06f, 0), default, 0f, false);
                }
            }
        }

        private void FillPacked()
        {
            var pkgs = S.PackedPackages;
            int shown = Mathf.Min(pkgs.Count, 6);
            for (int i = 0; i < shown; i++)
            {
                var pkg = pkgs[pkgs.Count - shown + i];
                var vis = ItemKit.Build(Kit.Content, pkg, false);
                var b = ItemKit.Bounds(pkg);
                int col = i % 2, layer = i / 2;
                vis.transform.localPosition = new Vector3(-0.12f + col * 0.26f, b.y / 2f + layer * 0.3f, 0);
                vis.transform.localRotation = Quaternion.Euler(0, (i * 17) % 20 - 10, 0);
            }
        }

        private void FillFlat()
        {
            int n = Mathf.Min(S.FlatTotal(), 12);
            for (int i = 0; i < n; i++)
                Props.Box(Kit.Content, new Vector3(0.62f, 0.012f, 0.5f), Mats.Cardboard(), new Vector3(0, 0.008f + i * 0.014f, 0), new Vector3(0, (i * 7) % 9 - 4, 0), 0f);
        }

        private void FillShip()
        {
            if (Opts.Stage == 0) return;
            int n = Mathf.Min(S.Daily.Shipped, 10);
            for (int i = 0; i < n; i++)
            {
                var pos = new Vector3(-0.35f + (i % 3) * 0.35f, 0.15f + (i / 6) * 0.3f, -0.18f + ((i / 3) % 2) * 0.36f);
                Props.Box(Kit.Content, new Vector3(0.3f, 0.26f, 0.3f), Mats.Cardboard(), pos, new Vector3(0, i * 23 % 30, 0), 0.01f);
            }
        }

        private void FillPlates()
        {
            int n = S.DinerPlatesWaiting();
            for (int i = 0; i < n; i++)
            {
                var plate = ItemKit.Build(Kit.Content, new ItemData { Kind = ItemKind.Plate });
                plate.transform.localPosition = new Vector3(-0.36f + i * 0.36f, 0.06f, 0);
            }
        }

        private void FillTable()
        {
            int state = S.DinerTableState(Opts.TableId);
            if (state == 1) Label3D.Create(Kit.Content, "Wartet auf Essen", 44f, new Color(1f, 0.75f, 0.3f), new Vector3(0, 1f, 0), true);
            else if (state == 2)
            {
                var plate = ItemKit.Build(Kit.Content, new ItemData { Kind = ItemKind.Plate });
                plate.transform.localPosition = new Vector3(0, 0.07f, 0.1f);
            }
        }

        private void FillStand()
        {
            var gm = S;
            if (!gm.HasUpgrade("stand"))
            {
                Label3D.Create(Kit.Content, "BALD HIER:\nDEIN STAND", 60f, new Color(1f, 0.85f, 0.4f), new Vector3(0, 0.5f, 0), true, 12f);
                return;
            }
            int slot = 0;
            foreach (var p in GameData.Products)
            {
                int q = gm.StandStock.TryGetValue(p.Id, out int v) ? v : 0;
                int boxes = Mathf.Min(8, (q + 3) / 4);
                for (int b = 0; b < boxes && slot < 16; b++, slot++)
                {
                    var item = ItemKit.Build(Kit.Content, new ItemData { Kind = ItemKind.Item, Product = p.Id });
                    item.transform.localPosition = new Vector3(-0.9f + (slot % 8) * 0.26f, 0.07f + (slot / 8) * 0.14f, -0.2f + (slot / 8) * 0.12f);
                    item.transform.localRotation = Quaternion.Euler(0, (slot * 13) % 16 - 8, 0);
                }
            }
            if (gm.StandTotal() > 0)
            {
                var sample = gm.StandStock;
                string pid = "";
                foreach (var p in GameData.Products)
                    if (sample.TryGetValue(p.Id, out int q) && q > 0)
                    {
                        pid = p.Id;
                        break;
                    }
                if (pid != "")
                    Label3D.Create(Kit.Content, "ab " + Fmt.Money(gm.StandPrice(pid)), 52f, new Color(1f, 0.9f, 0.5f), new Vector3(0, 0.6f, 0.4f), true, 10f);
            }
        }
    }
}
