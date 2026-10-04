using DropshippingGame.Core;
using DropshippingGame.UI;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Regalfach in MediaMarkd / Fressnix: ein Artikel. Benutzen = in den Wagen / Korb.</summary>
    public sealed class ShopBay : MonoBehaviour, IInteractable
    {
        public string ItemId;
        public Label3D Tag;
        private readonly Highlighter _hl = new Highlighter();

        public void Setup(string itemId, Vector3 colSize, Vector3 colCenter)
        {
            ItemId = itemId;
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = colSize;
            col.center = colCenter;
            _hl.Collect(transform);
        }

        private StoreItemDef Item => ShopData.Item(ItemId);

        public string Title => Item != null ? Item.Name : "";

        /// <summary>Preistext (MediaMarkd nur mit Scanner, mit Großhandels-Vergleich).</summary>
        public static string PriceText(Sim sim, StoreItemDef it)
        {
            string s = Fmt.Money(sim.ShopPrice(it.Id));
            var deal = sim.ShopDealFor(it.Id);
            if (deal != null) s += " statt " + Fmt.Money(sim.ShopBasePrice(it.Id));
            if (it.Kind == "ware")
            {
                s += " (" + Fmt.Eur(sim.ShopUnitPrice(it.Id)) + "/Stk · Großhandel " + Fmt.Eur(sim.ShopWholesalePackPrice(it.Id) / Mathf.Max(1, it.Pack)) + ")";
                if (sim.ShopCheaperThanWholesale(it.Id)) s += " · GÜNSTIGER!";
            }
            return s;
        }

        public string Prompt(PlayerController player)
        {
            var sim = Game.Sim;
            var it = Item;
            var d = ShoppingDistrict.Instance;
            if (sim == null || it == null || d == null) return "";
            bool electro = it.Store == ShopData.Electro;
            if (electro && !d.PushingCart) return it.Name + " · Erst einen Einkaufswagen holen (Eingang)";
            string price = electro && !d.HasScanner ? "Preis? Scanner am Eingang" : PriceText(sim, it);
            string why = sim.ShopBlockReason(it.Id);
            if (why != "") return it.Name + " · " + price + " · " + why;
            int inCart = sim.CartQty(it.Id);
            string pack = it.Kind == "ware" ? " (" + it.Pack + " Stk)" : (it.Kind == "futter" ? " (" + it.Pack + " Portionen)" : "");
            return (electro ? "In den Wagen: " : "In den Korb: ") + it.Name + pack + " · " + price +
                   (inCart > 0 ? " · " + inCart + " drin" : "") + " · noch " + sim.ShopLeft(it.Id);
        }

        public void Interact(PlayerController player)
        {
            var sim = Game.Sim;
            var it = Item;
            var d = ShoppingDistrict.Instance;
            if (sim == null || it == null || d == null) return;
            if (it.Store == ShopData.Electro && !d.PushingCart)
            {
                Game.Notify("Ohne Einkaufswagen geht hier nichts. Die stehen am Eingang.", "info");
                return;
            }
            sim.CartAdd(it.Id);
        }

        public void SetHighlighted(bool on)
        {
            _hl.Set(on);
            var d = ShoppingDistrict.Instance;
            var it = Item;
            if (on && d != null && d.HasScanner && it != null && it.Store == ShopData.Electro) Game.Sound("scanner", 0.05f, -10f);
        }
    }

    /// <summary>Einkaufswagen: Benutzen = schieben, Ablegen-Taste = loslassen.</summary>
    public sealed class ShopCart : MonoBehaviour, IInteractable
    {
        public Vector3 HomePos;
        public float HomeRot;
        public bool Pushed;
        private readonly Highlighter _hl = new Highlighter();
        private Transform _content;
        private Label3D _label;
        private int _layer;

        public void Setup(Vector3 home, float rot)
        {
            HomePos = home;
            HomeRot = rot;
            _layer = gameObject.layer;
            var model = Props.Asset(transform, "CartModel", "logistics.shopping_cart", 1.05f);
            if (model == null)
            {
                var wire = Mats.Std(new Color(0.75f, 0.77f, 0.8f), 0.3f, 0.8f);
                Props.Box(transform, new Vector3(0.55f, 0.45f, 0.8f), Mats.Std(new Color(0.75f, 0.77f, 0.8f, 1f), 0.4f, 0.6f), new Vector3(0, 0.75f, 0));
                Props.Box(transform, new Vector3(0.6f, 0.04f, 0.04f), Mats.Std(new Color(0.85f, 0.1f, 0.1f), 0.5f), new Vector3(0, 1.05f, -0.45f));
                foreach (float x in new[] { -0.22f, 0.22f })
                foreach (float z in new[] { -0.32f, 0.32f })
                {
                    Props.Box(transform, new Vector3(0.03f, 0.5f, 0.03f), wire, new Vector3(x, 0.3f, z));
                    Props.Cyl(transform, 0.05f, 0.05f, 0.04f, Mats.DarkMetal(), new Vector3(x, 0.05f, z), new Vector3(0, 0, 90));
                }
            }
            _content = Props.Node(transform, "Content", new Vector3(0f, 0.62f, 0.05f)).transform;
            _hl.Collect(transform);
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(0.6f, 1.05f, 0.95f);
            col.center = new Vector3(0f, 0.52f, 0f);
            _label = Label3D.Create(transform, "", 34f, Color.white, new Vector3(0f, 1.35f, 0f), true, 6f);
            RefreshContent();
        }

        public string Title => "Einkaufswagen";

        public string Prompt(PlayerController player)
        {
            var d = ShoppingDistrict.Instance;
            if (d != null && d.PushingCart) return "Du schiebst schon einen Wagen";
            if (player.Held != null) return "Hände voll";
            return "Einkaufswagen schieben (" + GameInput.KeyLabel("drop") + ": loslassen)";
        }

        public void Interact(PlayerController player)
        {
            var d = ShoppingDistrict.Instance;
            if (d == null || d.PushingCart || player.Held != null) return;
            d.StartPushing(this);
        }

        public void SetHighlighted(bool on) => _hl.Set(on && !Pushed);

        public void SetPushed(bool on)
        {
            Pushed = on;
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = !on;
            SetLayer(transform, on ? 2 : _layer);
            if (on) _hl.Set(false);
        }

        private static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }

        public void ReturnHome()
        {
            transform.position = HomePos;
            transform.rotation = Quaternion.Euler(0f, HomeRot, 0f);
        }

        /// <summary>Inhalt sichtbar machen (kleine Kartons in Artikelfarbe) + Zähler.</summary>
        public void RefreshContent()
        {
            if (_content == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--) Destroy(_content.GetChild(i).gameObject);
            var sim = Game.Sim;
            if (sim == null || !Pushed)
            {
                if (_label != null) _label.SetText("");
                return;
            }
            int n = 0;
            foreach (var l in sim.CartLines(ShopData.Electro))
            {
                var it = ShopData.Item(l.ItemId);
                if (it == null) continue;
                for (int q = 0; q < l.Qty && n < ShopData.CartCapacity; q++, n++)
                {
                    float x = -0.15f + (n % 3) * 0.15f, z = -0.25f + (n / 3 % 4) * 0.16f, y = (n / 12) * 0.12f;
                    Props.Box(_content, new Vector3(0.13f, 0.11f, 0.13f), Mats.Std(it.Color.ToColor(), 0.6f), new Vector3(x, y, z), new Vector3(0, n * 13f, 0), 0.01f, false);
                }
            }
            int count = sim.CartCount(ShopData.Electro);
            if (_label != null) _label.SetText(count > 0 ? count + " Packungen · " + Fmt.Money(sim.CartTotal(ShopData.Electro)) : "Wagen leer");
        }
    }

    /// <summary>Ständer mit dem Preisscanner (MediaMarkd).</summary>
    public sealed class ShopScannerStand : MonoBehaviour, IInteractable
    {
        public GameObject Gun;
        private readonly Highlighter _hl = new Highlighter();

        public void Setup()
        {
            _hl.Collect(transform);
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(0.6f, 1.4f, 0.5f);
            col.center = new Vector3(0f, 0.7f, 0f);
        }

        public string Title => "Preisscanner";

        public string Prompt(PlayerController player)
        {
            var d = ShoppingDistrict.Instance;
            if (d == null) return "";
            return d.HasScanner ? "Preisscanner zurücklegen" : "Preisscanner nehmen (zeigt Preise + Großhandels-Vergleich)";
        }

        public void Interact(PlayerController player)
        {
            var d = ShoppingDistrict.Instance;
            if (d == null) return;
            d.SetScanner(!d.HasScanner);
        }

        public void SetHighlighted(bool on) => _hl.Set(on);
    }

    /// <summary>Kasse eines Ladens.</summary>
    public sealed class ShopCheckoutDesk : MonoBehaviour, IInteractable
    {
        public string Store;
        private readonly Highlighter _hl = new Highlighter();

        public void Setup(string store, Vector3 size, Vector3 center)
        {
            Store = store;
            _hl.Collect(transform);
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = size;
            col.center = center;
        }

        public string Title => "Kasse";

        public string Prompt(PlayerController player)
        {
            var sim = Game.Sim;
            if (sim == null) return "";
            int n = sim.CartCount(Store);
            if (n == 0) return "Kasse · " + (Store == ShopData.Electro ? "Wagen" : "Korb") + " ist leer";
            return "Bezahlen: " + n + " Packungen · " + Fmt.Money(sim.CartTotal(Store));
        }

        public void Interact(PlayerController player) => ShopCheckoutView.Show(Store);

        public void SetHighlighted(bool on) => _hl.Set(on);
    }

    /// <summary>Verschlossene Tür des Klamottenladens (DLC-Gag).</summary>
    public sealed class LockedShopDoor : MonoBehaviour, IInteractable
    {
        private readonly Highlighter _hl = new Highlighter();
        private int _tries;

        public void Setup(Vector3 size, Vector3 center)
        {
            _hl.Collect(transform);
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = size;
            col.center = center;
        }

        public string Title => ShopData.ClothesName;
        public string Prompt(PlayerController player) => "Abgeschlossen – " + ShopData.ClothesName + " eröffnet demnächst (DLC)";

        public void Interact(PlayerController player)
        {
            string[] lines =
            {
                "Abgeschlossen. Auf dem Zettel steht: „Demnächst – als DLC“.",
                "Rüttel, rüttel. Immer noch zu.",
                "Drinnen hängt ein Hoodie für 89 €. Gut, dass zu ist.",
                "Ein Schild: „Season Pass gibt's noch nicht. Bitte nicht klopfen.“",
            };
            Game.Notify(lines[_tries++ % lines.Length], "info");
            Game.Sound("latch", 0.1f, -4f);
        }

        public void SetHighlighted(bool on) => _hl.Set(on);
    }
}
