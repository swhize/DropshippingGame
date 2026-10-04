using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Regeln für die PaketBlitz-Post: Abgabe an Packstationen/Filiale ist kostenlos, die bequeme
    /// "Abholung" direkt an der Garage bzw. im Versandkäfig der Halle kostet eine kleine Gebühr pro Paket.
    /// Während des Tutorials ist die Abholung gratis (damit der Einstieg nicht bestraft).
    /// Reine Logik ohne Unity – getestet in Tests/EditMode/CityTests.cs.
    /// </summary>
    public static class PostRules
    {
        /// <summary>Gebühr pro Paket für die Abholung an der Garage.</summary>
        public const int GaragePickupFee = 3;
        /// <summary>Gebühr pro Paket im Versandkäfig der Lagerhalle (Firmentarif).</summary>
        public const int WarehousePickupFee = 2;

        /// <summary>Gebühr pro Paket für die Abholung je Ausbaustufe (0 = Garage, 1 = Lagerhalle).</summary>
        public static int PickupFeePerParcel(int locationStage) => locationStage >= 1 ? WarehousePickupFee : GaragePickupFee;

        /// <summary>Gesamtgebühr für <paramref name="parcels"/> Pakete. 0 bei keinem Paket oder im Tutorial.</summary>
        public static int PickupFee(int parcels, int locationStage, bool tutorialActive)
        {
            if (parcels <= 0 || tutorialActive) return 0;
            return parcels * PickupFeePerParcel(locationStage);
        }

        /// <summary>Läuft gerade das Tutorial (Abholung dann gratis)?</summary>
        public static bool TutorialActive(Sim sim) =>
            sim != null && sim.TutorialStep >= 0 && sim.TutorialStep < GameData.Tutorial.Length;
    }

    public sealed partial class Sim
    {
        private int _citySeed;

        /// <summary>
        /// Zufallssaat für die Stadt (Häuser, Läden, geparkte Autos). Bleibt pro Spielstand stabil und wird
        /// gespeichert. Ein neues Spiel bekommt beim ersten Zugriff eine neue Saat; alte Spielstände ohne
        /// Saat bekommen eine stabile Saat aus Slot und Markenname. Verbraucht keinen <see cref="Rng"/>-Zufall.
        /// </summary>
        public int CitySeed
        {
            get
            {
                if (_citySeed == 0) _citySeed = FreshCitySeed();
                return _citySeed;
            }
            set => _citySeed = value == 0 ? 1 : value;
        }

        /// <summary>Stabile, positive Saat aus Text und Zahl (ohne string.GetHashCode, das pro Prozess variiert).</summary>
        public static int StableSeed(string text, int salt)
        {
            unchecked
            {
                uint h = 2166136261u;
                string s = text ?? "";
                for (int i = 0; i < s.Length; i++)
                {
                    h ^= s[i];
                    h *= 16777619u;
                }
                h ^= (uint)salt;
                h *= 16777619u;
                int v = (int)(h & 0x7fffffff);
                return v == 0 ? 1 : v;
            }
        }

        private static int FreshCitySeed()
        {
            unchecked
            {
                long t = DateTime.UtcNow.Ticks;
                int v = (int)((t ^ (t >> 29) ^ Environment.TickCount) & 0x7fffffff);
                return v == 0 ? 1 : v;
            }
        }

        /// <summary>Hook aus <see cref="ResetState"/>: neues Spiel = neue Stadt.</summary>
        private void ResetCityState()
        {
            _citySeed = 0;
        }

        /// <summary>Hook aus <see cref="ToJson"/>.</summary>
        private void AddCityState(Dictionary<string, object> state)
        {
            state["city_seed"] = CitySeed;
        }

        /// <summary>Hook aus <see cref="FromJson"/>. Alte Spielstände (ohne Saat): stabil aus Slot + Marke.</summary>
        private void ReadCityState(Dictionary<string, object> s)
        {
            int seed = J.I(s, "city_seed", 0);
            _citySeed = seed > 0 ? seed : StableSeed(J.S(s, "brand_name", BrandName), Slot);
        }
    }
}
