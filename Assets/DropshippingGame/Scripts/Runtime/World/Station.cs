using System.Collections.Generic;
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

        /// <summary>Alle aktiven Stationen (für Ziel-Markierungen).</summary>
        public static readonly List<Station> All = new List<Station>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        /// <summary>Nicht interaktive Stationen (Schilder) haben keinen Collider und kein Highlight.</summary>
        public bool Interactive => Type != StationType.Sign;

        public void Setup(StationType type, StationOpts opts)
        {
            Type = type;
            Opts = opts ?? new StationOpts();
            var visual = Props.Node(transform, "Visual");
            Kit = StationKit.Build(visual.transform, type, Opts) ?? new StationKitResult();
            if (Interactive)
            {
                var col = gameObject.AddComponent<BoxCollider>();
                col.size = Kit.Size;
                col.center = Kit.Center;
                _hl.Collect(visual.transform, Kit.Content);
            }
            if (Type == StationType.Monitor && Kit.Content != null)
                gameObject.AddComponent<OrderMonitor>().Setup(Kit.Content, Opts.Stage);
            All.Add(this);
            _sim = Game.Sim;
            if (_sim != null && Interactive)
            {
                _sim.EconomyChanged += Refresh;
                if (Type == StationType.ReturnTray || Type == StationType.Dock) _sim.ReturnsChanged += Refresh;
                if (Type == StationType.Pallet)
                {
                    _sim.ContractsChanged += Refresh;
                    _sim.ContractCompleted += OnContractCompleted;
                    _sim.ContractFailed += OnContractFailed;
                }
            }
            Refresh();
        }

        private void OnDestroy()
        {
            All.Remove(this);
            if (_sim == null) return;
            _sim.EconomyChanged -= Refresh;
            _sim.ReturnsChanged -= Refresh;
            _sim.ContractsChanged -= Refresh;
            _sim.ContractCompleted -= OnContractCompleted;
            _sim.ContractFailed -= OnContractFailed;
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
                    case StationType.ReturnTray: return "Retourenfach";
                    case StationType.ReturnDesk: return "Retourenplatz: Prüftisch";
                    case StationType.ReturnBin: return "Retourenplatz: Container";
                    case StationType.Pallet: return "Palettenplatz (B2B)";
                    case StationType.Monitor: return "Bestell-Monitor";
                }
                return "";
            }
        }

        public void SetHighlighted(bool on)
        {
            if (Interactive) _hl.Set(on);
        }

        private bool IsBusiness => Type <= StationType.Conveyor || Type == StationType.EndDay || Type == StationType.Stand ||
                                   (Type >= StationType.ReturnTray && Type <= StationType.Monitor);

        private static string ProductName(string pid) => GameData.IsProduct(pid) ? GameData.Product(pid).Name : "?";

        private static int LabeledValue(PlayerController player)
        {
            int sum = 0;
            foreach (var it in player.Carried()) if (it.Kind == ItemKind.Labeled) sum += it.Price;
            return sum;
        }

        private static bool RoomForMorePackages(PlayerController player) =>
            PlayerController.IsStackable(player.Held) && player.CarryCount < player.PackageCapacity;

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
                    if (kind == ItemKind.Return) return "Retoure zurücklegen";
                    if (kind != ItemKind.None) return "Hände voll";
                    if (gm.DockCrates.Count == 0 && gm.ReturnsAtDock > 0) return "Keine Kisten · Retouren liegen im Retourenfach";
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
                    if (kind == ItemKind.Item)
                    {
                        if (held.Product != pid) return "Hände voll – Artikel erst zum Packtisch";
                        if (held.OrderId > 0 && gm.PendingCountFor(pid) > 0)
                            return "Hände voll – erst zum Packtisch (2× " + GameInput.KeyLabel("interact") + ": zurücklegen)";
                        return "Zurücklegen";
                    }
                    if (kind == ItemKind.Package || kind == ItemKind.Labeled) return "Hände voll – erst Pakete wegbringen";
                    if (kind != ItemKind.None) return "Hände voll";
                    int pending = gm.PendingCountFor(pid);
                    int forContract = gm.ContractUnitsNeeded(pid);
                    if (gm.StockQty(pid) <= 0)
                        return forContract > 0 ? "Regal leer – Großauftrag braucht noch " + forContract + " (nachbestellen!)" : "Regal leer – im Laptop nachbestellen";
                    if (pending <= 0 && forContract > 0) return "Für Großauftrag entnehmen (noch " + forContract + ")";
                    if (pending <= 0) return p.Name + ": " + gm.StockQty(pid) + " auf Lager · keine Bestellung";
                    return p.Name + " entnehmen (" + pending + " offen)";
                }
                case StationType.Pack:
                    if (kind == ItemKind.Item)
                    {
                        if (held.ContractId > 0) return "Großauftrag-Artikel – ab zum Palettenplatz";
                        int size = GameData.Product(held.Product).Size;
                        return "Verpacken (Karton " + GameData.SizeName(size) + " · " + gm.Packaging[size] + " übrig)";
                    }
                    if ((kind == ItemKind.Package || kind == ItemKind.Labeled) && RoomForMorePackages(player) && gm.PackedCount() > 0)
                        return "Weiteres Paket nehmen (" + player.CarryCount + "/" + player.PackageCapacity + " in der Hand)";
                    if (kind == ItemKind.Package) return "Paket zurücklegen";
                    if (kind != ItemKind.None) return "Hände voll";
                    return gm.PackedCount() > 0 ? "Paket nehmen (" + gm.PackedCount() + " bereit)" : "Bring einen Artikel zum Verpacken";
                case StationType.Label:
                {
                    int unlabeled = player.CountCarried(ItemKind.Package);
                    if (unlabeled > 1) return "Alle " + unlabeled + " Pakete etikettieren";
                    if (unlabeled == 1) return "Versandlabel drucken";
                    if (kind == ItemKind.Labeled) return "Hat schon ein Label";
                }
                    return "Bring ein verpacktes Paket hierher";
                case StationType.Ship:
                {
                    int nl = player.CountCarried(ItemKind.Labeled);
                    if (nl > 1) return nl + " Pakete abgeben (+" + Fmt.Money(LabeledValue(player)) + ")";
                    if (nl == 1) return "Paket abgeben (+" + Fmt.Money(LabeledValue(player)) + ")";
                }
                    if (kind == ItemKind.Package) return "Erst ein Versandlabel drucken!";
                    return "Etikettierte Pakete hier abgeben";
                case StationType.Fold:
                    if (kind != ItemKind.None) return "Hände frei machen zum Falten";
                    return gm.FlatTotal() > 0 ? "Karton falten (" + gm.FlatTotal() + " ungefaltet)" : "Keine ungefalteten Kartons";
                case StationType.Conveyor:
                    if (player.CountCarried(ItemKind.Labeled) > 1) return "Alle " + player.CountCarried(ItemKind.Labeled) + " Pakete aufs Band legen";
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
                case StationType.ReturnTray:
                    if (kind == ItemKind.Return) return "Retoure zurücklegen";
                    if (kind != ItemKind.None) return "Hände voll";
                    if (gm.ReturnsAtDock <= 0)
                        return gm.ReturnsIncoming.Count > 0 ? "Retourenfach leer · " + gm.ReturnsIncoming.Count + " Retoure(n) unterwegs" : "Retourenfach leer";
                    return "Retoure aufnehmen (" + ProductName(gm.DockReturns[0].Product) + " · " + gm.ReturnsAtDock + " im Fach)";
                case StationType.ReturnDesk:
                    if (kind == ItemKind.Return)
                        return "Als B-Ware einlagern (" + GameData.QualityName(gm.BStockQuality(held)) + ")";
                    if (kind != ItemKind.None) return "Hier werden nur Retouren geprüft";
                    return gm.ReturnsAtDock > 0 ? "Hol eine Retoure aus dem Retourenfach (" + gm.ReturnsAtDock + " warten)" : "Retourenplatz – keine Retouren";
                case StationType.ReturnBin:
                    if (kind == ItemKind.Return) return "Retoure entsorgen (" + ProductName(held.Product) + ")";
                    return "Container für unverkäufliche Retouren";
                case StationType.Pallet:
                    return PalletPrompt(gm, held, kind);
                case StationType.Monitor:
                {
                    int open = gm.Tickets().Count;
                    int express = gm.ExpressPendingCount();
                    int overdue = gm.OverdueCount();
                    if (open == 0) return "Bestell-Monitor: keine offenen Bestellungen";
                    return "Bestell-Monitor: " + open + " offen" + (express > 0 ? " · " + express + " Express" : "") + (overdue > 0 ? " · " + overdue + " überfällig" : "");
                }
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
                    else if (kind == ItemKind.Return)
                    {
                        gm.PutBackReturn(held);
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
                    else if ((kind == ItemKind.Package || kind == ItemKind.Labeled) && RoomForMorePackages(player) && gm.PackedCount() > 0)
                    {
                        // Paketstapel: so viele fertige Pakete nehmen, wie noch in die Hände passen.
                        int taken = 0;
                        while (RoomForMorePackages(player) && gm.PackedCount() > 0)
                        {
                            var more = gm.PickupPackage();
                            if (more == null) break;
                            player.Push(more);
                            Anim.Delay(0.12f * taken, () => Game.Sound("pickup"));
                            taken++;
                        }
                        AutoLabel(player, gm);
                    }
                    else if (kind == ItemKind.Package)
                    {
                        gm.ReturnPackage(held);
                        player.PopTop();
                        Game.Sound("place");
                    }
                    else if (kind == ItemKind.Labeled) gm.Notify("Hände voll – erst die Pakete versenden.", "info");
                    else if (kind == ItemKind.None)
                    {
                        var pkg = gm.PickupPackage();
                        if (pkg != null)
                        {
                            player.Hold(pkg);
                            Game.Sound("pickup");
                            AutoLabel(player, gm);
                        }
                    }
                    else gm.Notify("Damit kannst du hier nichts anfangen.", "info");
                    break;
                case StationType.Label:
                    if (player.CountCarried(ItemKind.Package) > 0)
                    {
                        // Ein Druck etikettiert alle Pakete ohne Label im Stapel.
                        var items = player.Carried();
                        int printed = 0;
                        for (int i = 0; i < items.Count; i++)
                        {
                            if (items[i].Kind != ItemKind.Package) continue;
                            var labeled = items[i].Clone();
                            labeled.Kind = ItemKind.Labeled;
                            items[i] = labeled;
                            gm.OnLabeled(labeled);
                            if (printed == 0) Game.Sound("printer");
                            else Anim.Delay(0.15f * printed, () => Game.Sound("printer"));
                            printed++;
                        }
                        player.SetCarried(items);
                    }
                    else if (kind == ItemKind.Labeled) gm.Notify("Das Paket hat schon ein Versandlabel.", "info");
                    else gm.Notify("Bring ein verpacktes Paket zum Labeldrucker.", "info");
                    break;
                case StationType.Ship:
                    if (player.CountCarried(ItemKind.Labeled) > 0)
                    {
                        var items = player.Carried();
                        var keep = new List<ItemData>();
                        int shipped = 0;
                        foreach (var it in items)
                        {
                            if (it.Kind != ItemKind.Labeled)
                            {
                                keep.Add(it);
                                continue;
                            }
                            gm.ShipPackage(it, (transform.position + new Vector3(0, 1.8f + shipped * 0.35f, 0)).ToV3());
                            shipped++;
                        }
                        player.SetCarried(keep);
                        if (keep.Count > 0) gm.Notify("Pakete ohne Versandlabel bleiben in der Hand.", "info");
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
                    if (player.CountCarried(ItemKind.Labeled) > 0)
                    {
                        var items = player.Carried();
                        var keep = new List<ItemData>();
                        bool any = false;
                        foreach (var it in items)
                        {
                            if (it.Kind == ItemKind.Labeled && gm.ConveyorInsert(it)) any = true;
                            else keep.Add(it);
                        }
                        if (any)
                        {
                            player.SetCarried(keep);
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
                case StationType.ReturnTray:
                    if (kind == ItemKind.Return)
                    {
                        gm.PutBackReturn(held);
                        player.ClearHands();
                        Game.Sound("place");
                    }
                    else if (kind != ItemKind.None) gm.Notify("Hände sind voll.", "bad");
                    else
                    {
                        var ret = gm.PickupReturn();
                        if (ret != null)
                        {
                            player.Hold(ret);
                            Game.Sound("pickup");
                        }
                    }
                    break;
                case StationType.ReturnDesk:
                case StationType.ReturnBin:
                    if (kind == ItemKind.Return)
                    {
                        if (gm.ProcessReturn(held, Type == StationType.ReturnDesk)) player.ClearHands();
                    }
                    else if (gm.ReturnsAtDock > 0) gm.Notify("Nimm eine Retoure aus dem Retourenfach am Wareneingang und bring sie her.", "info");
                    else gm.Notify("Hier prüfst du Retouren: links als B-Ware einlagern, rechts entsorgen.", "info");
                    break;
                case StationType.Pallet:
                    if (kind == ItemKind.Crate || kind == ItemKind.Item)
                    {
                        int n = gm.ContractDeliver(held);
                        if (n > 0)
                        {
                            Game.Sound("place");
                            if (kind == ItemKind.Item || held.Quantity <= 0) player.ClearHands();
                            else player.Hold(held);
                        }
                    }
                    else if (kind != ItemKind.None) gm.Notify("Auf die Palette kommen nur Kisten und einzelne Artikel für Großaufträge.", "info");
                    else if (!gm.ContractsUnlocked) gm.Notify("Großaufträge von Firmen gibt es ab Firmenlevel " + GameData.ContractLevel + ".", "info");
                    else if (gm.ActiveContractCount() == 0) gm.Notify("Kein laufender Großauftrag. Angebote findest du in der App 'Aufträge'.", "info");
                    else gm.Notify("Bring ganze Kisten vom Wareneingang oder einzelne Artikel aus dem Regal hierher.", "info");
                    break;
                case StationType.Monitor:
                    gm.Notify("Alle Bestellungen siehst du auch auf dem Handy (" + GameData.Key("phone") + ").", "info");
                    break;
            }
        }

        private string PalletPrompt(Sim gm, ItemData held, ItemKind kind)
        {
            if (!gm.ContractsUnlocked) return "Palettenplatz – Großaufträge ab Firmenlevel " + GameData.ContractLevel;
            if (kind == ItemKind.Crate || kind == ItemKind.Item)
            {
                var c = gm.ContractFor(held);
                if (c != null) return "Auf die Palette: " + c.Company + " (noch " + c.Remaining + ")";
                if (kind == ItemKind.Item && held.OrderId > 0) return "Artikel gehört zu einer Bestellung – zum Packtisch";
                if (gm.ActiveContractCount() == 0) return "Kein laufender Großauftrag";
                return gm.ContractUnitsNeeded(held.Product) > 0 ? "Qualität reicht für den Auftrag nicht" : "Kein Großauftrag braucht " + ProductName(held.Product);
            }
            if (kind != ItemKind.None) return "Nur Kisten oder Artikel für Großaufträge";
            var active = gm.ActiveContracts();
            if (active.Count == 0) return "Palettenplatz – Aufträge in der App 'Aufträge' annehmen";
            var first = active[0];
            return first.Company + ": " + first.Delivered + "/" + first.Quantity + " " + ProductName(first.Product) + " · " + gm.ContractDeadlineText(first);
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
                    if (held.Product != pid)
                    {
                        gm.Notify("Hände voll – erst den Artikel in der Hand zum Packtisch bringen (oder ins Regal '" +
                                  GameData.Product(held.Product).Name + "' zurücklegen).", "info");
                        break;
                    }
                    // Früher legte ein zweiter Druck den Artikel kommentarlos zurück – bei mehreren offenen
                    // Bestellungen sah das aus wie "nimmt nichts raus". Jetzt: Hinweis, Zurücklegen erst
                    // bei erneutem Druck innerhalb kurzer Zeit (oder wenn keine weitere Bestellung wartet).
                    if (held.OrderId > 0 && gm.PendingCountFor(pid) > 0 && Time.unscaledTime > _returnArmedUntil)
                    {
                        _returnArmedUntil = Time.unscaledTime + 2.5f;
                        gm.Notify("Du trägst schon einen Artikel (#" + held.OrderId +
                                  ") – erst zum Packtisch, dann den nächsten holen. Nochmal " + GameInput.KeyLabel("interact") + " = zurücklegen.", "info");
                        break;
                    }
                    _returnArmedUntil = -1f;
                    gm.ReturnItem(held);
                    player.ClearHands();
                    Game.Sound("place");
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
                    gm.Notify(kind == ItemKind.Package || kind == ItemKind.Labeled
                        ? "Hände voll – erst die Pakete wegbringen (Label/Versand), dann neue Artikel holen."
                        : "Hände voll – damit kannst du am Regal nichts anfangen.", "info");
                    break;
            }
        }

        private float _returnArmedUntil = -1f;

        /// <summary>Hand-Labelgerät: alle Pakete in der Hand sofort etikettieren.</summary>
        private static void AutoLabel(PlayerController player, Sim gm)
        {
            if (gm == null || !gm.AutoLabelOnPickup || player.CountCarried(ItemKind.Package) == 0) return;
            var items = player.Carried();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Kind != ItemKind.Package) continue;
                var labeled = items[i].Clone();
                labeled.Kind = ItemKind.Labeled;
                items[i] = labeled;
                gm.OnLabeled(labeled);
            }
            player.SetCarried(items);
            Game.Sound("printer", 0.1f, -6f);
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
                case StationType.ReturnTray:
                {
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i < Mathf.Min(gm.DockReturns.Count, 6); i++) sb.Append(gm.DockReturns[i].Product).Append(',');
                    return sb.ToString();
                }
                case StationType.Pallet:
                {
                    var sb = new System.Text.StringBuilder();
                    var active = gm.ActiveContracts();
                    for (int i = 0; i < Mathf.Min(active.Count, StationKit.PalletSlots); i++)
                        sb.Append(active[i].Id).Append(':').Append(PalletBoxes(active[i])).Append('/').Append(active[i].Delivered).Append(',');
                    return sb.ToString() + gm.ContractsUnlocked;
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
                case StationType.ReturnTray: FillReturnTray(); break;
                case StationType.Pallet: FillPallet(); break;
            }
        }

        // ---- v3.0: Retourenfach ---------------------------------------------------------------------
        private void FillReturnTray()
        {
            var rets = S.DockReturns;
            int n = Mathf.Min(rets.Count, 6);
            for (int i = 0; i < n; i++)
            {
                var vis = ItemKit.Build(Kit.Content, rets[i], false);
                var b = ItemKit.Bounds(rets[i]);
                int shelf = i / 3, slot = i % 3;
                vis.transform.localPosition = new Vector3(-0.3f + slot * 0.3f, (shelf == 0 ? 0.095f : 0.565f) + b.y / 2f, 0);
                vis.transform.localRotation = Quaternion.Euler(0, (i * 23) % 24 - 12, 0);
                vis.transform.localScale = Vector3.one * Mathf.Min(1f, 0.28f / Mathf.Max(0.01f, b.x));
            }
            if (rets.Count > n)
                Label3D.Create(Kit.Content, "+" + (rets.Count - n), 40f, new Color(1f, 0.6f, 0.55f), new Vector3(0.38f, 1.2f, 0.2f), true, 10f);
        }

        // ---- v3.0: Palettenplatz --------------------------------------------------------------------
        private const int PalletMaxBoxes = 18;

        /// <summary>Sichtbare Kartons auf der Palette (wächst mit dem Fortschritt).</summary>
        private static int PalletBoxes(Contract c)
        {
            if (c == null || c.Quantity <= 0 || c.Delivered <= 0) return 0;
            return Mathf.Clamp(Mathf.CeilToInt(PalletMaxBoxes * (float)c.Delivered / c.Quantity), 1, PalletMaxBoxes);
        }

        private static Vector3 PalletBoxPos(int i) => new Vector3(-0.38f + (i % 3) * 0.38f, 0.15f + (i / 6) * 0.33f + 0.15f, -0.22f + ((i / 3) % 2) * 0.44f);

        private void FillPallet()
        {
            var gm = S;
            if (!gm.ContractsUnlocked)
            {
                Label3D.Create(Kit.Content, "GROSSAUFTRÄGE\nab Level " + GameData.ContractLevel, 44f, new Color(0.7f, 0.8f, 1f), new Vector3(0, 0.7f, 0), true, 14f);
                return;
            }
            var active = gm.ActiveContracts();
            if (active.Count == 0)
            {
                Label3D.Create(Kit.Content, "Kein Großauftrag", 40f, new Color(0.75f, 0.8f, 0.9f), new Vector3(0, 0.6f, 0), true, 12f);
                return;
            }
            for (int s = 0; s < Mathf.Min(active.Count, StationKit.PalletSlots); s++)
            {
                var c = active[s];
                var slot = Props.Node(Kit.Content, "Slot" + s, StationKit.PalletSlotPos(s)).transform;
                Props.Pallet(slot).transform.localScale = new Vector3(1f, 1f, 1f);
                var col = GameData.IsProduct(c.Product) ? GameData.Product(c.Product).Color.ToColor() : Color.white;
                int boxes = PalletBoxes(c);
                for (int i = 0; i < boxes; i++)
                {
                    var cr = Props.Crate(slot, col, new Vector3(0.36f, 0.3f, 0.4f));
                    cr.transform.localPosition = PalletBoxPos(i) - new Vector3(0, 0.15f, 0);
                    cr.transform.localRotation = Quaternion.Euler(0, (i * 11) % 8 - 4, 0);
                }
                if (boxes > 0 && boxes >= PalletMaxBoxes - 5)
                    Props.Box(slot, new Vector3(1.2f, 0.004f, 0.02f), Mats.Std(new Color(0.85f, 0.88f, 0.95f), 0.2f), new Vector3(0, 0.95f, 0.5f), default, 0f, false);
                bool urgent = gm.ContractDaysLeft(c) <= 0;
                var txtCol = urgent ? new Color(1f, 0.55f, 0.45f) : new Color(1f, 0.92f, 0.6f);
                string pname = GameData.IsProduct(c.Product) ? GameData.Product(c.Product).Short : c.Product;
                Label3D.Create(slot, c.Company + "\n" + c.Delivered + "/" + c.Quantity + " " + pname, 30f, txtCol,
                    new Vector3(0, 1.45f, 0), true, 14f, false, 1.2f, 18);
            }
        }

        private void OnContractCompleted(Contract c)
        {
            if (this == null || c == null || Game.World == null) return;
            // Der fertige Stapel wird verladen: Geister-Palette fährt zur Straße, die Spedition holt sie ab.
            var start = transform.position;
            var ghost = Props.Node(Game.World.transform, "PalletPickup", start, transform.eulerAngles.y).transform;
            Props.Pallet(ghost);
            var col = GameData.IsProduct(c.Product) ? GameData.Product(c.Product).Color.ToColor() : Color.white;
            for (int i = 0; i < PalletMaxBoxes; i++)
            {
                var cr = Props.Crate(ghost, col, new Vector3(0.36f, 0.3f, 0.4f));
                cr.transform.localPosition = PalletBoxPos(i) - new Vector3(0, 0.15f, 0);
            }
            SpeditionTruck.Pickup(ghost, start, c.Company);
        }

        private void OnContractFailed(Contract c)
        {
            if (this == null) return;
            _sig = "-";
            Refresh();
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
