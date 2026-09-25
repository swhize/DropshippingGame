using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>Phase eines Produkt-Trends.</summary>
    public enum TrendPhase
    {
        /// <summary>Stabil, Hype um 1,0.</summary>
        Normal = 0,
        /// <summary>Im Aufwind.</summary>
        Rising = 1,
        /// <summary>Höhepunkt des Hypes.</summary>
        Peak = 2,
        /// <summary>Hype flaut ab.</summary>
        Falling = 3,
        /// <summary>Tot – niemand will es gerade.</summary>
        Dead = 4,
    }

    /// <summary>Hype-Zustand eines Produkts. Der Hype-Wert (0,5-2,0) multipliziert die Nachfrage.</summary>
    public sealed class TrendState
    {
        public string Product = "";
        public TrendPhase Phase = TrendPhase.Normal;
        /// <summary>Verbleibende Tage der Phase inklusive heute.</summary>
        public int DaysLeft = 5;
        /// <summary>Gesamtlänge der aktuellen Phase in Tagen.</summary>
        public int PhaseDays = 5;
        /// <summary>Hype am Anfang des heutigen Verlaufs (ab <see cref="StartAt"/>).</summary>
        public float Start = 1f;
        /// <summary>Uhrzeit (Minuten), ab der <see cref="Start"/> gilt – normalerweise 08:00.</summary>
        public float StartAt = GameData.DayStart;
        /// <summary>Hype zum Tagesende (Ziel des heutigen Verlaufs).</summary>
        public float Hype = 1f;
        /// <summary>Hype beim Eintritt in die aktuelle Phase.</summary>
        public float From = 1f;
        /// <summary>Plan des (nächsten) Zyklus: Höhe des Peaks, Tiefpunkt, Dauer der Phasen.</summary>
        public float Peak = 1.8f, Floor = 0.62f;
        public int RiseDays = 2, PeakDays = 1, FallDays = 2, DeadDays = 1;
        /// <summary>Kurze deutsche Begründung ("Ein Streamer hat ... in die Kamera gehalten.").</summary>
        public string Reason = "";
        /// <summary>Hype am Ende der letzten Tage (ältester zuerst, max. <see cref="GameData.TrendHistoryDays"/>).</summary>
        public List<float> History = new List<float>();

        public TrendState CloneState()
        {
            var c = (TrendState)MemberwiseClone();
            c.History = new List<float>(History);
            return c;
        }

        public Dictionary<string, object> ToJson()
        {
            var h = new List<object>();
            foreach (var v in History) h.Add((double)v);
            return new Dictionary<string, object>
            {
                { "phase", (int)Phase }, { "days_left", DaysLeft }, { "phase_days", PhaseDays }, { "start", (double)Start },
                { "start_at", (double)StartAt }, { "hype", (double)Hype }, { "from", (double)From }, { "peak", (double)Peak },
                { "floor", (double)Floor }, { "rise_days", RiseDays }, { "peak_days", PeakDays }, { "fall_days", FallDays },
                { "dead_days", DeadDays }, { "reason", Reason ?? "" }, { "history", h },
            };
        }

        public static TrendState FromJson(string product, object o)
        {
            var d = J.Obj(o);
            var s = new TrendState
            {
                Product = product, Phase = (TrendPhase)Mathx.Clamp(J.I(d, "phase"), 0, 4), DaysLeft = Math.Max(1, J.I(d, "days_left", 5)),
                PhaseDays = Math.Max(1, J.I(d, "phase_days", 5)), Start = J.F(d, "start", 1f), StartAt = J.F(d, "start_at", GameData.DayStart),
                Hype = J.F(d, "hype", 1f), From = J.F(d, "from", 1f), Peak = J.F(d, "peak", 1.8f), Floor = J.F(d, "floor", 0.62f),
                RiseDays = Math.Max(1, J.I(d, "rise_days", 2)), PeakDays = Math.Max(1, J.I(d, "peak_days", 1)),
                FallDays = Math.Max(1, J.I(d, "fall_days", 2)), DeadDays = Math.Max(0, J.I(d, "dead_days", 1)), Reason = J.S(d, "reason", ""),
            };
            foreach (var v in J.A(d, "history")) s.History.Add(J.F(v, 1f));
            s.Start = Mathx.Clamp(s.Start, GameData.TrendMin, GameData.TrendMax);
            s.Hype = Mathx.Clamp(s.Hype, GameData.TrendMin, GameData.TrendMax);
            return s;
        }
    }

    /// <summary>Trendradar-Prognose für ein Produkt (mit Unsicherheit).</summary>
    public sealed class TrendForecast
    {
        public string Product = "";
        public TrendPhase Phase;
        /// <summary>Aktueller Hype.</summary>
        public float Now;
        /// <summary>Prognostizierter Hype am Ende der nächsten Tage (Index 0 = morgen).</summary>
        public float[] Values = new float[0];
        /// <summary>Unsicherheitsband je Tag.</summary>
        public float[] Low = new float[0], High = new float[0];
        /// <summary>Genauigkeit des Radars (0..1, höher = besser).</summary>
        public float Accuracy;
        /// <summary>Kurzlabel: "stabil", "Hype im Anflug", "steigend", "Hype!", "fallend", "tot".</summary>
        public string Label = "";
        /// <summary>Handlungstipp auf Deutsch.</summary>
        public string Hint = "";
        /// <summary>Icon-Name (trend, fire, trend_down, warning, dot).</summary>
        public string Icon = "dot";
    }

    /// <summary>
    /// Trends/Hype je Produkt: Zyklen aus stabil → steigend → Peak → fallend → tot → stabil über
    /// mehrere Tage. Der Hype multipliziert die Nachfrage und verläuft innerhalb eines Tages
    /// fließend vom Start- zum Zielwert. Der Trendradar sagt die nächsten Tage mit Rauschen voraus
    /// (genauer mit dem Skill "Trendradar"). Ereignisse und Skills können Hypes auslösen.
    /// Die Prognose verbraucht keine Zufallszahlen (Anzeige ändert den Spielverlauf nicht).
    /// </summary>
    public sealed class TrendSystem
    {
        private readonly Sim _sim;
        public readonly Dictionary<string, TrendState> States = new Dictionary<string, TrendState>();

        public TrendSystem(Sim sim)
        {
            _sim = sim;
        }

        private Rng Rng => _sim.Rng;

        public void Reset()
        {
            States.Clear();
            foreach (var p in GameData.Products)
            {
                var st = new TrendState { Product = p.Id };
                PlanCycle(st);
                SetPhase(st, TrendPhase.Normal, Rng.RangeInt(3, 10), false);
                st.Start = st.Hype = st.From = 1f;
                st.Reason = "";
                States[p.Id] = st;
            }
        }

        public TrendState State(string id)
        {
            if (!States.TryGetValue(id, out var st))
            {
                st = new TrendState { Product = id };
                States[id] = st;
            }
            return st;
        }

        public TrendPhase Phase(string id) => State(id).Phase;

        /// <summary>Aktueller Hype-Faktor (fließend über den Tag).</summary>
        public float Mult(string id)
        {
            var st = State(id);
            float span = GameData.DayEnd - st.StartAt;
            float t = span > 1f ? Mathx.Clamp01((_sim.TimeMinutes - st.StartAt) / span) : 1f;
            return Mathx.Clamp(Mathx.Lerp(st.Start, st.Hype, t), GameData.TrendMin, GameData.TrendMax);
        }

        public string Label(string id) => GameData.TrendPhaseNames[(int)Phase(id)];

        public int HypedCount()
        {
            int n = 0;
            foreach (var kv in States)
                if (kv.Value.Phase == TrendPhase.Rising || kv.Value.Phase == TrendPhase.Peak) n++;
            return n;
        }

        // ---- Tageswechsel ------------------------------------------------------------------------
        /// <summary>Wird zu Beginn jedes neuen Tages aufgerufen.</summary>
        public void NewDay()
        {
            foreach (var p in GameData.Products)
            {
                var st = State(p.Id);
                st.History.Add(st.Hype);
                while (st.History.Count > GameData.TrendHistoryDays) st.History.RemoveAt(0);
                st.Start = st.Hype;
                st.StartAt = GameData.DayStart;
                st.DaysLeft--;
                if (st.DaysLeft <= 0) NextPhase(st, true);
                st.Hype = Target(st, true);
            }
        }

        private void NextPhase(TrendState st, bool live)
        {
            switch (st.Phase)
            {
                case TrendPhase.Normal:
                    if (live && HypedCount() >= GameData.MaxHypedProducts)
                    {
                        st.DaysLeft = Rng.RangeInt(1, 3);
                        return;
                    }
                    SetPhase(st, TrendPhase.Rising, st.RiseDays, live);
                    break;
                case TrendPhase.Rising:
                    SetPhase(st, TrendPhase.Peak, st.PeakDays, live);
                    break;
                case TrendPhase.Peak:
                    SetPhase(st, TrendPhase.Falling, st.FallDays, live);
                    break;
                case TrendPhase.Falling:
                    if (st.DeadDays > 0) SetPhase(st, TrendPhase.Dead, st.DeadDays, live);
                    else EnterNormal(st, live);
                    break;
                default:
                    EnterNormal(st, live);
                    break;
            }
        }

        private void EnterNormal(TrendState st, bool live)
        {
            if (live)
            {
                PlanCycle(st);
                SetPhase(st, TrendPhase.Normal, Rng.RangeInt(5, 14), true);
            }
            else SetPhase(st, TrendPhase.Normal, 99, false);
        }

        /// <summary>Würfelt den nächsten Zyklus vorab aus (damit der Trendradar ihn sehen kann).</summary>
        private void PlanCycle(TrendState st)
        {
            st.RiseDays = Rng.RangeInt(2, 3);
            st.PeakDays = Rng.RangeInt(1, 2);
            st.FallDays = Rng.RangeInt(2, 3);
            st.DeadDays = Rng.Value() < 0.35f ? 0 : Rng.RangeInt(1, 2);
            st.Peak = Rng.Range(1.6f, 2.0f);
            st.Floor = Rng.Range(0.55f, 0.7f);
        }

        private void SetPhase(TrendState st, TrendPhase phase, int days, bool live)
        {
            var old = st.Phase;
            st.Phase = phase;
            st.DaysLeft = Math.Max(1, days);
            st.PhaseDays = st.DaysLeft;
            st.From = st.Start;
            if (!live) return;
            st.Reason = ReasonFor(st.Product, phase);
            if (old != phase) _sim.OnTrendPhaseChanged(st.Product, phase);
        }

        /// <summary>Ziel-Hype am Ende des heutigen Tages (live = mit Zufallsrauschen in der stabilen Phase).</summary>
        private float Target(TrendState st, bool live)
        {
            int k = st.PhaseDays - st.DaysLeft + 1;
            float v;
            switch (st.Phase)
            {
                case TrendPhase.Rising:
                    v = Mathx.Lerp(st.From, st.Peak, k / (float)(st.PhaseDays + 1));
                    break;
                case TrendPhase.Peak:
                    v = st.Peak + (live ? Rng.Range(-0.03f, 0.03f) : 0f);
                    break;
                case TrendPhase.Falling:
                    v = Mathx.Lerp(st.From, 0.8f, k / (float)st.PhaseDays);
                    break;
                case TrendPhase.Dead:
                    v = st.Floor;
                    break;
                default:
                    v = Mathx.Lerp(st.Start, 1f, 0.5f) + (live ? Rng.Range(-0.04f, 0.04f) : 0f);
                    if (st.Start >= 0.85f && st.Start <= 1.15f) v = Mathx.Clamp(v, 0.88f, 1.12f);
                    break;
            }
            return Mathx.Clamp(v, GameData.TrendMin, GameData.TrendMax);
        }

        private string ReasonFor(string id, TrendPhase phase)
        {
            string[] pool;
            switch (phase)
            {
                case TrendPhase.Rising: pool = GameData.TrendReasonsRising; break;
                case TrendPhase.Peak: pool = GameData.TrendReasonsPeak; break;
                case TrendPhase.Falling: pool = GameData.TrendReasonsFalling; break;
                case TrendPhase.Dead: pool = GameData.TrendReasonsDead; break;
                default: return FillReason(GameData.TrendReasonNormal, id);
            }
            return FillReason(pool[Rng.Index(pool.Length)], id);
        }

        public static string HashTag(string id)
        {
            var p = GameData.Product(id);
            var sb = new System.Text.StringBuilder();
            foreach (char c in p.Short)
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            return sb + "Challenge";
        }

        public static string FillReason(string text, string id) =>
            (text ?? "").Replace("{p}", GameData.Product(id).Name).Replace("{tag}", HashTag(id));

        // ---- Auslöser (Ereignisse, Skills) --------------------------------------------------------
        /// <summary>
        /// Zwingt ein Produkt in eine Phase (z. B. Trend-Alarm, viraler Hit, Verriss).
        /// peak &gt; 0 setzt die Höhe des Hypes. Der Verlauf ändert sich ab sofort fließend.
        /// </summary>
        public void Trigger(string id, TrendPhase phase, float peak = 0f, string reason = "")
        {
            if (!GameData.IsProduct(id)) return;
            var st = State(id);
            float now = Mult(id);
            st.Start = now;
            st.StartAt = Math.Min(_sim.TimeMinutes, GameData.DayEnd - 1f);
            if (peak > 0f) st.Peak = Mathx.Clamp(peak, 1.2f, GameData.TrendMax);
            switch (phase)
            {
                case TrendPhase.Rising:
                    if (st.Peak < 1.4f) st.Peak = 1.8f;
                    st.RiseDays = Rng.RangeInt(1, 2);
                    st.PeakDays = Rng.RangeInt(1, 2);
                    st.FallDays = Rng.RangeInt(2, 3);
                    SetPhase(st, TrendPhase.Rising, st.RiseDays, true);
                    break;
                case TrendPhase.Peak:
                    SetPhase(st, TrendPhase.Peak, Rng.RangeInt(1, 2), true);
                    break;
                case TrendPhase.Falling:
                    SetPhase(st, TrendPhase.Falling, Rng.RangeInt(1, 2), true);
                    break;
                case TrendPhase.Dead:
                    st.Floor = Rng.Range(0.55f, 0.65f);
                    SetPhase(st, TrendPhase.Dead, Rng.RangeInt(1, 2), true);
                    break;
                default:
                    PlanCycle(st);
                    SetPhase(st, TrendPhase.Normal, Rng.RangeInt(5, 14), true);
                    break;
            }
            // Heute schon ein spürbarer Schritt in die neue Richtung.
            st.Hype = phase == TrendPhase.Rising ? Mathx.Clamp(Math.Max(Target(st, false), now + 0.25f), GameData.TrendMin, GameData.TrendMax)
                                                 : Target(st, false);
            if (!string.IsNullOrEmpty(reason)) st.Reason = FillReason(reason, id);
            _sim.RaiseTrendsUpdated();
        }

        // ---- Trendradar ------------------------------------------------------------------------------
        /// <summary>Prognose für die nächsten Tage (Standard: <see cref="GameData.TrendForecastDays"/>).</summary>
        public TrendForecast Forecast(string id, int days = GameData.TrendForecastDays)
        {
            var st = State(id);
            days = Mathx.Clamp(days, 1, 7);
            float acc = _sim.TrendForecastAccuracy();
            int sight = _sim.TrendSightDays();
            var f = new TrendForecast
            {
                Product = id, Phase = st.Phase, Now = Mult(id), Accuracy = acc,
                Values = new float[days], Low = new float[days], High = new float[days],
            };
            var sim = st.CloneState();
            int pi = GameData.ProductIndex(id);
            bool riseAhead = false;
            for (int d = 1; d <= days; d++)
            {
                sim.Start = sim.Hype;
                sim.DaysLeft--;
                if (sim.DaysLeft <= 0)
                {
                    if (sim.Phase == TrendPhase.Normal && d > sight) sim.DaysLeft = 1;
                    else
                    {
                        if (sim.Phase == TrendPhase.Normal) riseAhead = true;
                        NextPhase(sim, false);
                    }
                }
                float truth = Target(sim, false);
                sim.Hype = truth;
                float amp = (1f - acc) * 0.45f * (0.4f + 0.6f * d / days);
                float v = Mathx.Clamp(truth + amp * Noise(_sim.Day, pi, d), GameData.TrendMin, GameData.TrendMax);
                f.Values[d - 1] = v;
                f.Low[d - 1] = Mathx.Clamp(v - amp, GameData.TrendMin, GameData.TrendMax);
                f.High[d - 1] = Mathx.Clamp(v + amp, GameData.TrendMin, GameData.TrendMax);
            }
            float last = f.Values[days - 1];
            switch (st.Phase)
            {
                case TrendPhase.Rising:
                    f.Label = "steigend";
                    f.Icon = "trend";
                    f.Hint = "Der Hype wächst – jetzt Lager auffüllen!";
                    break;
                case TrendPhase.Peak:
                    f.Label = "Hype!";
                    f.Icon = "fire";
                    f.Hint = "Höhepunkt: Die Leute kaufen fast jeden Preis. Aber bald ist es vorbei.";
                    break;
                case TrendPhase.Falling:
                    f.Label = "fallend";
                    f.Icon = "trend_down";
                    f.Hint = "Der Hype flaut ab – nicht mehr zu viel nachkaufen.";
                    break;
                case TrendPhase.Dead:
                    f.Label = "tot";
                    f.Icon = "warning";
                    f.Hint = "Keiner will's gerade. Preis senken oder aussitzen.";
                    break;
                default:
                    if (riseAhead || last > f.Now + 0.2f)
                    {
                        f.Label = "Hype im Anflug";
                        f.Icon = "trend";
                        f.Hint = "Der Trendradar sieht etwas kommen. Vorräte anlegen?";
                    }
                    else
                    {
                        f.Label = "stabil";
                        f.Icon = "dot";
                        f.Hint = acc >= 0.8f ? "Keine Auffälligkeiten." : "Keine Auffälligkeiten – soweit das Radar es erkennt.";
                    }
                    break;
            }
            return f;
        }

        /// <summary>Deterministisches Rauschen in [-1, 1] (verbraucht keine Zufallszahlen).</summary>
        public static float Noise(int a, int b, int c)
        {
            unchecked
            {
                uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ (uint)(c * 83492791) ^ 0x9E3779B9u;
                h ^= h >> 16;
                h *= 0x7feb352du;
                h ^= h >> 15;
                h *= 0x846ca68bu;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF * 2f - 1f;
            }
        }

        // ---- Speichern ------------------------------------------------------------------------------
        public Dictionary<string, object> ToJson()
        {
            var d = new Dictionary<string, object>();
            foreach (var kv in States) d[kv.Key] = kv.Value.ToJson();
            return d;
        }

        /// <summary>Lädt Trends. Fehlen sie (alter Spielstand), bleiben die frisch gewürfelten Startwerte.</summary>
        public void FromJson(Dictionary<string, object> d)
        {
            if (d == null || d.Count == 0) return;
            foreach (var p in GameData.Products)
                if (d.TryGetValue(p.Id, out object o) && o is Dictionary<string, object>)
                    States[p.Id] = TrendState.FromJson(p.Id, o);
        }
    }
}
