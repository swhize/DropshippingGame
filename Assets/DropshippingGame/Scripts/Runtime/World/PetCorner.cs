using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Tier-Ecke in der Garage (rund um <see cref="PetActor.GarageHome"/>): Futternapf (Füllstand = Futtervorrat),
    /// Wassernapf, Kuschelbett, Spielzeug, Futtervorrat und gekaufte Gadgets. Baut sich neu, sobald sich
    /// Tiere/Vorrat/Gadgets ändern. Die Tiere laufen ab und zu zu Napf oder Bett (<see cref="TryGetSpot"/>).
    /// </summary>
    public sealed class PetCorner : MonoBehaviour
    {
        public static PetCorner Instance;
        private Transform _content;
        private string _sig = null;
        private float _poll;
        private Vector3 _bowl, _bed, _catBed;
        private bool _hasFood, _catBedOn;

        public static PetCorner Create(Transform parent)
        {
            var go = new GameObject("PetCorner");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<PetCorner>();
            Instance = c;
            return c;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            _poll -= Time.deltaTime;
            if (_poll > 0f) return;
            _poll = 1.5f;
            var sim = Game.Sim;
            if (sim == null) return;
            string sig = Signature(sim);
            if (sig == _sig) return;
            _sig = sig;
            Rebuild(sim);
        }

        private static string Signature(Sim sim)
        {
            if (sim.Pets.Count == 0) return "none";
            var g = new List<string>(sim.PetGadgets);
            g.Sort();
            var sb = new System.Text.StringBuilder();
            foreach (var p in sim.Pets) sb.Append(p.Species).Append(',');
            sb.Append('|').Append(PetItemModels.BowlFor(sim.PetFoodTotal()));
            sb.Append('|').Append(sim.PetFood > 0).Append(sim.PetFoodPremium > 0);
            sb.Append('|').Append(string.Join(",", g));
            return sb.ToString();
        }

        /// <summary>Ein Platz, an dem das Tier gern sitzt (Napf, wenn Futter da ist, sonst Bett).</summary>
        public static bool TryGetSpot(string species, out Vector3 pos)
        {
            pos = default;
            var c = Instance;
            if (c == null || c._content == null || c._content.childCount == 0) return false;
            bool cat = species == "katze";
            if (c._hasFood && Random.value < 0.45f) pos = c._bowl + new Vector3(cat ? 0.25f : -0.2f, 0f, -0.35f);
            else pos = cat && c._catBedOn ? c._catBed : c._bed;
            return true;
        }

        private GameObject Put(string id, Vector3 world, float size, float rotY)
        {
            var go = PetItemModels.Build(_content, id, size, true);
            go.transform.position = new Vector3(world.x, 0.01f, world.z);
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            return go;
        }

        private void Rebuild(Sim sim)
        {
            if (_content != null) Destroy(_content.gameObject);
            _content = Props.Node(transform, "Content").transform;
            if (sim.Pets.Count == 0) return;
            bool cat = sim.HasPet("katze"), dog = sim.HasPet("hund");
            var h = PetActor.GarageHome;
            _bowl = h + new Vector3(0.2f, 0f, 0.9f);   // west neben dem Regal (-13.6/-11.2)
            _bed = h + new Vector3(-0.75f, 0f, -0.25f);
            _catBed = h + new Vector3(-1.2f, 0f, 0.6f);
            _hasFood = sim.PetFoodTotal() > 0;
            _catBedOn = cat;
            try
            {
                // Näpfe + Napf-Unterlage
                Props.Box(_content, new Vector3(0.75f, 0.01f, 0.36f), Mats.Std(new Color(0.3f, 0.65f, 0.4f), 0.9f),
                    new Vector3(_bowl.x + 0.15f, 0.006f, _bowl.z), default, 0f, false);
                Put(PetItemModels.BowlFor(sim.PetFoodTotal()), _bowl, 0.24f, 0f);
                Put("wassernapf", _bowl + new Vector3(0.32f, 0f, 0f), 0.24f, 0f);
                // Futtervorrat
                if (sim.PetFood > 0) Put("fn_futter", _bowl + new Vector3(-0.45f, 0f, 0.1f), 0.42f, 15f);
                if (sim.PetFoodPremium > 0) Put("fn_premium", _bowl + new Vector3(0.62f, 0f, 0.08f), 0.13f, -10f);
                Put("leckerli", _bowl + new Vector3(-0.42f, 0f, -0.3f), 0.2f, 0f);
                // Betten
                Put("fn_bett", _bed, sim.PetGadgets.Contains("fn_bett") ? 0.75f : 0.6f, 0f);
                if (cat) Put("fn_bett", _catBed, 0.5f, 20f);
                // Spielzeug
                Put("knochen", h + new Vector3(-0.2f, 0f, 0.3f), 0.15f, 35f);
                Put("seil", h + new Vector3(0.25f, 0f, -0.55f), 0.3f, -20f);
                if (dog) Put("frisbee", h + new Vector3(-1.5f, 0f, -0.4f), 0.24f, 0f);
                if (dog) Put("leine", h + new Vector3(-1.4f, 0f, -0.95f), 0.2f, 0f);
                if (cat) Put("katzenklo", h + new Vector3(-2.1f, 0f, 0.95f), 0.48f, 0f);
                if (sim.PetGadgets.Contains("fn_ball")) Put("fn_ball", h + new Vector3(0.35f, 0f, 0.1f), 0.09f, 0f);
                if (sim.PetGadgets.Contains("fn_laser")) Put("fn_laser", h + new Vector3(-0.5f, 0f, 0.55f), 0.14f, 60f);
                if (sim.PetGadgets.Contains("fn_halsband")) Put("fn_halsband", h + new Vector3(-1.0f, 0f, 0.0f), 0.16f, 0f);
                if (sim.PetGadgets.Contains("fn_gps")) Put("fn_gps", h + new Vector3(-0.95f, 0f, 0.35f), 0.17f, 30f);
                if (sim.PetGadgets.Contains("fn_automat")) Put("fn_automat", _bowl + new Vector3(0.15f, 0f, 0.42f), 0.38f, 180f);
                if (sim.PetGadgets.Contains("fn_kratzbaum")) Put("fn_kratzbaum", h + new Vector3(-2.1f, 0f, 0.05f), 1.3f, 90f);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
