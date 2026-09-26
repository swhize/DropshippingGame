using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Pitch Day als Minispiel: drei Juroren stellen je eine Frage, du wählst eine von drei Antworten.
    /// Jede Antwort stützt sich auf eine deiner echten Kennzahlen (Bewertung, Umsatz, Lager ...).
    /// Wer die Antwort wählt, die zu seinem Unternehmen passt, bekommt den Großauftrag.
    /// </summary>
    public sealed class PitchGame
    {
        public sealed class Answer
        {
            public string Text, Stat, StatLabel;
            public float Strength;
        }

        public sealed class Question
        {
            public string Juror, Text;
            public Answer[] Answers;
        }

        public sealed class Result
        {
            public float Score;
            public int Money;
            public string Title, Text;
            public Effects Effects;
        }

        private sealed class QTemplate
        {
            public string Juror, Text;
            public (string text, string stat)[] Answers;
        }

        private static readonly QTemplate[] Pool =
        {
            new QTemplate
            {
                Juror = "Frau Dr. Brandt, Einkauf", Text = "Warum sollten wir ausgerechnet Ihre Marke ins Regal stellen?",
                Answers = new[]
                {
                    ("Unsere Kunden lieben uns – schauen Sie sich die Bewertungen an.", "rating"),
                    ("Wir haben schon {shipped} Pakete verschickt. Die Nachfrage ist da.", "shipped"),
                    ("Weil ich heute extra einen Anzug trage. Einen richtigen.", "luck"),
                },
            },
            new QTemplate
            {
                Juror = "Herr Okafor, Logistik", Text = "Wie wollen Sie 500 Filialen zuverlässig beliefern?",
                Answers = new[]
                {
                    ("Mit meinem eigenen Lager und einem eingespielten Team.", "company"),
                    ("Unsere Lieferanten liefern blitzschnell, auch in großen Mengen.", "level"),
                    ("Notfalls fahre ich selbst. Mit dem Fahrrad.", "luck"),
                },
            },
            new QTemplate
            {
                Juror = "Frau Petrović, Marketing", Text = "Was unterscheidet Sie von BilligBoy24?",
                Answers = new[]
                {
                    ("Qualität statt Ramsch. Unsere Ware hält.", "quality"),
                    ("Wir sind günstiger als der Markt – und trotzdem profitabel.", "price"),
                    ("{brand} hat Stil. Die Leute kennen uns.", "brand"),
                },
            },
            new QTemplate
            {
                Juror = "Herr Lindqvist, Finanzen", Text = "Zeigen Sie mir Zahlen. Wie läuft das Geschäft?",
                Answers = new[]
                {
                    ("Wir haben bisher {earned} umgesetzt.", "earned"),
                    ("Wir wachsen jeden Tag. Schauen Sie sich die Kurve an.", "trend"),
                    ("Genug, um im Winter nicht zu frieren.", "luck"),
                },
            },
            new QTemplate
            {
                Juror = "Frau Weber, Nachhaltigkeit", Text = "Wie gehen Sie mit Beschwerden und Rücksendungen um?",
                Answers = new[]
                {
                    ("Schnell und freundlich. Deshalb bleiben unsere Bewertungen gut.", "rating"),
                    ("Unsere Retourenquote liegt bei nur {returnrate}. Wir prüfen jede Retoure.", "returns"),
                    ("Beschwerden? Hatten wir noch nie. Glaube ich.", "luck"),
                },
            },
        };

        private static readonly Dictionary<string, string> StatLabels = new Dictionary<string, string>
        {
            { "rating", "Bewertung" }, { "shipped", "verschickte Pakete" }, { "luck", "Glück" }, { "company", "Lager & Team" },
            { "level", "Firmenlevel" }, { "quality", "Warenqualität" }, { "price", "Preise" }, { "brand", "Marke & Bekanntheit" },
            { "earned", "Umsatz" }, { "trend", "Wachstum" }, { "returns", "Retourenquote" },
        };

        private readonly Sim _sim;
        public readonly List<Question> Questions = new List<Question>();
        public readonly List<float> Scores = new List<float>();
        public int Round => Scores.Count;
        public bool Finished => Scores.Count >= Questions.Count;

        public PitchGame(Sim sim, int rounds = 3)
        {
            _sim = sim;
            var idx = new List<int>();
            for (int i = 0; i < Pool.Length; i++) idx.Add(i);
            for (int r = 0; r < rounds && idx.Count > 0; r++)
            {
                int k = sim.Rng.Index(idx.Count);
                var t = Pool[idx[k]];
                idx.RemoveAt(k);
                var answers = new Answer[t.Answers.Length];
                for (int a = 0; a < answers.Length; a++)
                {
                    string stat = t.Answers[a].stat;
                    answers[a] = new Answer
                    {
                        Text = FillText(t.Answers[a].text), Stat = stat, StatLabel = StatLabels[stat],
                        Strength = stat == "luck" ? sim.Rng.Range(0.15f, 0.9f) : StatValue(stat),
                    };
                }
                Questions.Add(new Question { Juror = t.Juror, Text = t.Text, Answers = answers });
            }
        }

        private string FillText(string s) => s
            .Replace("{shipped}", _sim.TotalShipped.ToString())
            .Replace("{earned}", Fmt.Money(_sim.TotalEarned))
            .Replace("{brand}", _sim.BrandName)
            .Replace("{returnrate}", Fmt.Dec(_sim.ReturnRateTotal() * 100f, 1) + " %");

        /// <summary>Wie gut die eigene Firma bei dieser Kennzahl dasteht (0..1).</summary>
        public float StatValue(string stat)
        {
            var s = _sim;
            switch (stat)
            {
                case "rating": return Mathx.Clamp01((s.Reputation - 2.5f) / 2.5f);
                case "shipped": return Mathx.Clamp01(s.TotalShipped / 300f);
                case "earned": return Mathx.Clamp01(s.TotalEarned / 20000f);
                case "company": return Mathx.Clamp01(s.LocationStage * 0.5f + s.Staff.Count / 8f);
                case "level": return Mathx.Clamp01(s.Level / 10f);
                case "quality":
                {
                    float q = 0f;
                    int n = 0;
                    foreach (var p in GameData.Products)
                    {
                        if (s.StockQty(p.Id) <= 0) continue;
                        q += s.StockQuality(p.Id);
                        n++;
                    }
                    return n == 0 ? 0.2f : Mathx.Clamp01((q / n - 0.6f) / 1.0f);
                }
                case "price":
                {
                    int listed = 0, cheap = 0;
                    foreach (var p in GameData.Products)
                    {
                        if (!s.IsListed(p.Id) || !s.ProductAvailable(p.Id)) continue;
                        listed++;
                        if (s.CurrentSalePrice(p.Id) <= s.Market.MarketPrice(p.Id)) cheap++;
                    }
                    return listed == 0 ? 0f : (float)cheap / listed;
                }
                case "brand": return Mathx.Clamp01((s.BrandNamed ? 0.4f : 0f) + s.Awareness / 1.5f * 0.6f + (s.Reputation >= 4f ? 0.1f : 0f));
                case "returns": return s.TotalShipped < 10 ? 0.35f : Mathx.Clamp01(1f - s.ReturnRateTotal() * 8f);
                case "trend":
                {
                    var h = s.History;
                    if (h.Count < 2) return 0.25f;
                    float last = h[h.Count - 1].Revenue;
                    float prev = h[Math.Max(0, h.Count - 4)].Revenue;
                    return Mathx.Clamp01(0.5f + (last - prev) / Math.Max(100f, prev) * 0.5f);
                }
            }
            return 0.5f;
        }

        /// <summary>Gibt die Reaktion der Jury zurück.</summary>
        public string Choose(int answerIndex)
        {
            if (Finished) return "";
            var q = Questions[Round];
            var a = q.Answers[Mathx.Clamp(answerIndex, 0, q.Answers.Length - 1)];
            float score = Mathx.Clamp01(a.Strength + _sim.Rng.Range(-0.08f, 0.08f));
            Scores.Add(score);
            if (score >= 0.7f) return "Die Jury nickt anerkennend. Jemand macht sich eine Notiz.";
            if (score >= 0.45f) return "Solide Antwort. Die Jury bleibt freundlich, aber vorsichtig.";
            if (score >= 0.25f) return "Eine Jurorin zieht die Augenbraue hoch.";
            return "Stille. Irgendwo hustet jemand.";
        }

        public Result Evaluate()
        {
            float avg = 0f;
            foreach (var s in Scores) avg += s;
            avg = Scores.Count > 0 ? avg / Scores.Count : 0f;
            var res = new Result { Score = avg };
            if (avg >= 0.58f)
            {
                res.Money = Mathx.RoundToInt(500 + avg * 1500 + _sim.Level * 120);
                res.Title = "Deal! MegaMarkt nimmt dich ins Sortiment.";
                res.Text = "Standing Ovations. Du bekommst einen Großauftrag über " + Fmt.Money(res.Money) + ".";
                res.Effects = new Effects { Money = res.Money, Rep = 0.1f, Awareness = 0.1f };
            }
            else if (avg >= 0.38f)
            {
                res.Money = Mathx.RoundToInt(200 + avg * 500);
                res.Title = "Ein kleiner Testauftrag.";
                res.Text = "Die Jury ist noch nicht ganz überzeugt, bestellt aber eine Testlieferung über " + Fmt.Money(res.Money) + ".";
                res.Effects = new Effects { Money = res.Money, Awareness = 0.04f };
            }
            else
            {
                res.Money = 0;
                res.Title = "Kein Deal.";
                res.Text = "'Wir melden uns.' Sie melden sich nicht. Nächstes Mal mit besseren Zahlen!";
                res.Effects = new Effects();
            }
            return res;
        }
    }
}
