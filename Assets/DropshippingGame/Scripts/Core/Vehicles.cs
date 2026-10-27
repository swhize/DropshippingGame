using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>Ein fahrbares Fahrzeug (Daten). Bisher nur das Start-Moped; weitere Stufen als neue Einträge.</summary>
    public sealed class VehicleDef
    {
        public string Id = "", Name = "";
        /// <summary>Höchstgeschwindigkeit in km/h.</summary>
        public float MaxKmh;
        /// <summary>Beschleunigung in m/s² (bei Vollgas aus dem Stand).</summary>
        public float Accel;
        /// <summary>Plätze für Pakete (Paket, etikettiertes Paket, Retoure) in der Heckbox.</summary>
        public int ParcelSlots;
        /// <summary>Plätze für Lieferkisten (eine Kiste belegt die ganze Box).</summary>
        public int CrateSlots;
    }

    /// <summary>
    /// Regeln fürs Moped (reine Logik ohne Unity, getestet in Tests/EditMode/VehicleTests.cs): was in die
    /// Heckbox passt, Höchstgeschwindigkeit, Lenkeinschlag, Schräglage und Motor-Tonhöhe.
    /// </summary>
    public static class VehicleRules
    {
        public const string StarterId = "moped";
        /// <summary>Radstand des Mopeds in Metern (für die Lenkung).</summary>
        public const float Wheelbase = 1.25f;

        public static readonly VehicleDef[] Defs =
        {
            new VehicleDef { Id = StarterId, Name = "Mofa „Blitz 35“", MaxKmh = 35f, Accel = 3.4f, ParcelSlots = 4, CrateSlots = 1 },
        };

        /// <summary>Fahrzeug nach Id, unbekannte Ids (alte/kaputte Spielstände) = Start-Moped.</summary>
        public static VehicleDef Def(string id)
        {
            foreach (var d in Defs)
                if (d.Id == id) return d;
            return Defs[0];
        }

        public static float KmhToMs(float kmh) => kmh / 3.6f;
        public static float MsToKmh(float ms) => ms * 3.6f;

        /// <summary>Zählt als Paket für die Heckbox (Paket, etikettiertes Paket, Retoure)?</summary>
        public static bool IsParcel(ItemData it) =>
            it != null && (it.Kind == ItemKind.Package || it.Kind == ItemKind.Labeled || it.Kind == ItemKind.Return);

        /// <summary>Darf grundsätzlich in die Heckbox (Pakete und Lieferkisten, keine Teller/Einzelartikel)?</summary>
        public static bool Accepts(ItemData it) => IsParcel(it) || (it != null && it.Kind == ItemKind.Crate);

        public static bool HasCrate(IList<ItemData> box)
        {
            if (box == null) return false;
            foreach (var it in box)
                if (it != null && it.Kind == ItemKind.Crate) return true;
            return false;
        }

        /// <summary>Passt dieser Gegenstand noch in die Box? Eine Kiste nur in die leere Box, sonst bis zu ParcelSlots Pakete.</summary>
        public static bool CanLoad(IList<ItemData> box, ItemData it, VehicleDef def)
        {
            if (!Accepts(it) || def == null) return false;
            int count = box != null ? box.Count : 0;
            if (HasCrate(box)) return false;
            if (it.Kind == ItemKind.Crate) return def.CrateSlots > 0 && count == 0;
            return count < def.ParcelSlots;
        }

        /// <summary>Freie Paketplätze (0, wenn eine Kiste drin ist).</summary>
        public static int FreeParcelSlots(IList<ItemData> box, VehicleDef def)
        {
            if (def == null || HasCrate(box)) return 0;
            return Math.Max(0, def.ParcelSlots - (box != null ? box.Count : 0));
        }

        /// <summary>Maximaler Lenkeinschlag in Grad: im Stand eng, bei Höchstgeschwindigkeit nur noch sanft.</summary>
        public static float SteerLimitDeg(float speed01)
        {
            float t = Mathx.Clamp((float)Math.Pow(Mathx.Clamp(speed01, 0f, 1f), 0.8), 0f, 1f);
            return 36f + (11f - 36f) * t;
        }

        /// <summary>Gierrate in Grad/s aus Geschwindigkeit (m/s, negativ = rückwärts) und Lenkwinkel (Grad).</summary>
        public static float YawRateDeg(float speed, float steerDeg) =>
            (float)(speed * Math.Tan(steerDeg * Math.PI / 180.0) / Wheelbase * 180.0 / Math.PI);

        /// <summary>Schräglage in Grad (positiv = nach rechts) für Kurvenfahrt: atan(v·ω/g), begrenzt auf ±28°.</summary>
        public static float LeanDeg(float speed, float yawRateDeg)
        {
            double w = yawRateDeg * Math.PI / 180.0;
            double a = Math.Atan(speed * w / 9.81) * 180.0 / Math.PI;
            return Mathx.Clamp((float)a, -28f, 28f);
        }

        /// <summary>Neue Geschwindigkeit nach einem Zeitschritt (Gas 0..1, Bremse 0..1). Rückwärts nur langsam schieben.</summary>
        public static float StepSpeed(float v, float throttle, float brake, float dt, VehicleDef def)
        {
            if (def == null || dt <= 0f) return v;
            float vmax = KmhToMs(def.MaxKmh);
            throttle = Mathx.Clamp(throttle, 0f, 1f);
            brake = Mathx.Clamp(brake, 0f, 1f);
            if (throttle > 0.01f && v >= -0.05f)
            {
                float k = Mathx.Clamp(v / vmax, 0f, 1.2f);
                v += def.Accel * throttle * (1.06f - k * k) * dt;
            }
            else if (throttle > 0.01f) v = MoveTowards(v, 0f, 6f * dt);
            if (brake > 0.01f)
            {
                if (v > 0.25f) v = Math.Max(0f, v - 8.5f * brake * dt);
                else if (throttle <= 0.01f) v = MoveTowards(v, -1.6f * brake, 2.2f * dt);
            }
            // Rollwiderstand + Luft
            if (throttle <= 0.01f || v < 0f)
            {
                float drag = (0.35f + 0.012f * v * v) * dt;
                if (brake <= 0.01f || v > 0f) v = Math.Abs(v) <= drag ? 0f : v - Math.Sign(v) * drag;
            }
            return Mathx.Clamp(v, -1.6f, vmax);
        }

        /// <summary>Tonhöhe des Motor-Loops (AudioSource.pitch) aus Geschwindigkeit 0..1 und Gas 0..1.</summary>
        public static float EnginePitch(float speed01, float throttle)
        {
            speed01 = Mathx.Clamp(speed01, 0f, 1.1f);
            throttle = Mathx.Clamp(throttle, 0f, 1f);
            return 0.78f + 1.05f * speed01 + 0.18f * throttle * (1f - speed01 * 0.5f);
        }

        private static float MoveTowards(float a, float b, float step) =>
            Math.Abs(b - a) <= step ? b : a + Math.Sign(b - a) * step;
    }

    /// <summary>Zustand des eigenen Mopeds im Spielstand: Standort und Inhalt der Heckbox.</summary>
    public sealed class VehicleState
    {
        public string Id = VehicleRules.StarterId;
        /// <summary>false = steht am Stammplatz vor der Garage/Halle (neues Spiel, alter Spielstand).</summary>
        public bool Placed;
        public V3 Pos;
        public float RotY;
        /// <summary>Heckbox (unten zuerst).</summary>
        public List<ItemData> Box = new List<ItemData>();
        /// <summary>Hinweis-Nachricht „Dein Moped steht vor der Garage“ schon geschickt?</summary>
        public bool HintSent;

        public Dictionary<string, object> ToJson()
        {
            var box = new List<object>();
            foreach (var it in Box)
                if (it != null) box.Add(it.ToJson());
            return new Dictionary<string, object>
            {
                { "id", Id }, { "placed", Placed }, { "pos", Pos.ToJson() }, { "rot", (double)RotY }, { "box", box }, { "hint", HintSent },
            };
        }

        public static VehicleState FromJson(object o)
        {
            var v = new VehicleState();
            var d = o as Dictionary<string, object>;
            if (d == null) return v;
            v.Id = J.S(d, "id", VehicleRules.StarterId);
            if (string.IsNullOrEmpty(v.Id)) v.Id = VehicleRules.StarterId;
            v.Pos = V3.FromJson(J.Get(d, "pos"));
            v.RotY = J.F(d, "rot");
            bool finite = !(float.IsNaN(v.Pos.x) || float.IsNaN(v.Pos.y) || float.IsNaN(v.Pos.z) ||
                            float.IsInfinity(v.Pos.x) || float.IsInfinity(v.Pos.y) || float.IsInfinity(v.Pos.z) || float.IsNaN(v.RotY));
            v.Placed = J.B(d, "placed") && finite && !v.Pos.IsZero;
            if (!finite) v.RotY = 0f;
            v.HintSent = J.B(d, "hint");
            var def = VehicleRules.Def(v.Id);
            foreach (var b in J.A(d, "box"))
            {
                ItemData it;
                try
                {
                    it = ItemData.FromJson(b);
                }
                catch (Exception)
                {
                    continue;
                }
                if (VehicleRules.CanLoad(v.Box, it, def)) v.Box.Add(it);
            }
            return v;
        }
    }

    public sealed partial class Sim
    {
        /// <summary>Das eigene Moped (gibt es ab der Business-Phase, steht anfangs vor der Garage).</summary>
        public VehicleState Moped = new VehicleState();
        public VehicleDef MopedDef => VehicleRules.Def(Moped.Id);

        /// <summary>Lädt einen Gegenstand in die Heckbox. false = passt nicht (voll, Kiste drin, falsche Art).</summary>
        public bool LoadIntoMoped(ItemData it)
        {
            if (!VehicleRules.CanLoad(Moped.Box, it, MopedDef)) return false;
            Moped.Box.Add(it);
            return true;
        }

        /// <summary>Nimmt den obersten Gegenstand aus der Box (oder null).</summary>
        public ItemData TakeFromMoped()
        {
            int n = Moped.Box.Count;
            if (n == 0) return null;
            var it = Moped.Box[n - 1];
            Moped.Box.RemoveAt(n - 1);
            return it;
        }

        /// <summary>Anzahl Gegenstände dieser Art in der Box.</summary>
        public int CountInMoped(ItemKind kind)
        {
            int c = 0;
            foreach (var it in Moped.Box)
                if (it != null && it.Kind == kind) c++;
            return c;
        }

        /// <summary>Entnimmt alle etikettierten Pakete (für die Abgabe an der Packstation direkt aus der Box).</summary>
        public List<ItemData> TakeLabeledFromMoped()
        {
            var list = new List<ItemData>();
            for (int i = 0; i < Moped.Box.Count; i++)
            {
                var it = Moped.Box[i];
                if (it == null || it.Kind != ItemKind.Labeled) continue;
                list.Add(it);
                Moped.Box.RemoveAt(i);
                i--;
            }
            return list;
        }

        /// <summary>Hinweis-Nachricht zum Moped fällig? Erst nach dem Tutorial (wie die anderen Onboarding-Tipps).</summary>
        public bool MopedHintDue => OnboardingActive && !Moped.HintSent;

        /// <summary>Schickt einmalig die Moped-Nachricht. keyLabel = Taste fürs Benutzen ("E" / "X").</summary>
        public bool SendMopedHint(string keyLabel)
        {
            if (!MopedHintDue) return false;
            Moped.HintSent = true;
            string k = string.IsNullOrEmpty(keyLabel) ? "E" : keyLabel;
            Events.AddMail("Opa Heinz", "Dein Moped steht vor der Garage",
                "Hab dir mein altes Mofa vor die Garage gestellt. Dein Moped steht vor der Garage – " + k + " zum Aufsteigen. " +
                "Hinten in die Box passen 4 Pakete oder eine Kiste: Pakete in der Hand, " + k + " am Moped. An der Packstation " +
                "gibst du die Pakete direkt aus der Box ab. Fahr vorsichtig, 35 Sachen reichen!", "bolt");
            return true;
        }

        /// <summary>Hook aus <see cref="ResetState"/>.</summary>
        private void ResetVehicleState()
        {
            Moped = new VehicleState();
        }

        /// <summary>Hook aus <see cref="ToJson"/>.</summary>
        private void AddVehicleState(Dictionary<string, object> state)
        {
            state["moped"] = Moped.ToJson();
        }

        /// <summary>Hook aus <see cref="FromJson"/> (vor dem Abgleich der Bestellzettel). Alte Spielstände: Moped am Stammplatz, Box leer.</summary>
        private void ReadVehicleState(Dictionary<string, object> s)
        {
            Moped = VehicleState.FromJson(J.Get(s, "moped"));
        }

        /// <summary>Bestellnummern der Pakete in der Heckbox (damit ihre Zettel nicht als verwaist gelten).</summary>
        private void AddMopedOrderIds(ICollection<int> ids)
        {
            foreach (var it in Moped.Box)
                if (it != null && it.OrderId > 0) ids.Add(it.OrderId);
        }
    }
}
