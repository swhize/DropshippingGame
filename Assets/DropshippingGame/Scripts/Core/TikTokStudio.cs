using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>Video-Formate für echte TikTok-Aufnahmen (Unboxing, POV, Review, Life-Hack).</summary>
    public static class TikTokFormats
    {
        public const string Unboxing = "unboxing";
        public const string Pov = "pov";
        public const string Review = "review";
        public const string Hack = "hack";

        public static readonly string[] All = { Unboxing, Pov, Review, Hack };

        public static bool IsFormat(string id) => Array.IndexOf(All, id) >= 0;

        public static string Name(string id)
        {
            switch (id)
            {
                case Unboxing: return "Unboxing";
                case Pov: return "POV";
                case Review: return "Review";
                case Hack: return "Life-Hack";
            }
            return "Video";
        }

        /// <summary>Kurze Anleitung: worauf es bei diesem Format ankommt.</summary>
        public static string Tip(string id)
        {
            switch (id)
            {
                case Unboxing: return "Nah ran, starker Hook in den ersten 2 Sekunden, Produkt auspacken (Aktion).";
                case Pov: return "Ruhige Kamera, einmal ums Produkt herum, Deko/Marke mit ins Bild.";
                case Review: return "Produkt mittig, gutes Licht, ruhig halten und lang genug (15 s+).";
                case Hack: return "Hook zuerst, dann mehrmals vorführen (Aktion), Produkt immer im Bild.";
            }
            return "";
        }

        /// <summary>Overlay-Text (Titel) für das gepostete Video.</summary>
        public static string Title(string id, string productName, float score)
        {
            string p = string.IsNullOrEmpty(productName) ? "das hier" : productName;
            bool great = score >= 0.7f;
            switch (id)
            {
                case Unboxing: return great ? "UNBOXING: " + p + " (gone right!)" : "Unboxing: " + p;
                case Pov: return "POV: du hast endlich " + p;
                case Review: return great ? "Ehrliche Review: " + p + " (5/5!)" : "Review: " + p + " – lohnt sich das?";
                case Hack: return "Life-Hack mit " + p + ", den JEDER braucht";
            }
            return p;
        }
    }

    /// <summary>
    /// Messwerte einer echten Aufnahme. Die Welt misst jeden Frame und fasst zusammen (Anteile 0..1
    /// der Aufnahmezeit); die Bewertung selbst passiert rein in <see cref="TikTokScoring"/>.
    /// </summary>
    public sealed class TikTokTake
    {
        /// <summary>Aufnahmedauer in Sekunden.</summary>
        public float Duration;
        /// <summary>Anteil der Zeit, in dem das Produkt im 9:16-Bild und nicht verdeckt war.</summary>
        public float VisibleFrac;
        /// <summary>Wie mittig das Produkt war (0..1, Durchschnitt über die sichtbare Zeit).</summary>
        public float Centered;
        /// <summary>Anteil der sichtbaren Zeit mit guter Entfernung.</summary>
        public float DistanceFrac;
        /// <summary>Anteil der Zeit mit ruhiger Kamera.</summary>
        public float SteadyFrac;
        /// <summary>Durchschnittliche Helligkeit am Produkt (0..1).</summary>
        public float Light;
        /// <summary>Abgedeckter Blickwinkel ums Produkt in Grad (Variation).</summary>
        public float AngleDegrees;
        /// <summary>Aktion (Auspacken/Schütteln/Zeigen) in den ersten Sekunden.</summary>
        public bool Hook;
        /// <summary>Anzahl Aktionen während der Aufnahme.</summary>
        public int Actions;
        /// <summary>Anteil der Zeit, in dem Marke/Deko (Poster, Neon, Schild) im Bild war.</summary>
        public float BrandFrac;
        /// <summary>Trend-Faktor des Produkts (1 = normal, bis ca. 2 = Hype).</summary>
        public float Hype = 1f;
    }

    /// <summary>Ein Kriterium der Bewertung (für die Aufschlüsselung auf der Ergebniskarte).</summary>
    public sealed class TikTokCriterion
    {
        public string Id, Label;
        /// <summary>Erfüllung 0..1.</summary>
        public float Value;
        /// <summary>Gewicht im gewählten Format.</summary>
        public float Weight;
        /// <summary>Verbesserungstipp, wenn schwach.</summary>
        public string Tip;
    }

    /// <summary>Ergebnis einer Aufnahme.</summary>
    public sealed class TikTokRating
    {
        /// <summary>Endwertung 0..1 (geht als "Treffer" an <see cref="Sim.TriggerTikTok"/>).</summary>
        public float Score;
        /// <summary>Qualität aus den Kriterien (ohne Boni).</summary>
        public float Quality;
        public float HypeBonus;
        public float FormatBonus;
        public bool TrendingFormat;
        public int PredictedViews;
        public string Grade = "";
        /// <summary>Hinweis auf Abzüge (Produkt nie im Bild, zu kurz).</summary>
        public string Penalty = "";
        public readonly List<TikTokCriterion> Criteria = new List<TikTokCriterion>();
    }

    /// <summary>Gepostetes Video (im TikTak-Feed, gespeichert).</summary>
    public sealed class TikTokVideo
    {
        public string Title = "", Product = "", Format = "";
        public float Score;
        public int Views, Likes, Comments, Day;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "title", Title }, { "product", Product }, { "format", Format }, { "score", (double)Score },
            { "views", Views }, { "likes", Likes }, { "comments", Comments }, { "day", Day },
        };

        public static TikTokVideo FromJson(object o)
        {
            var d = J.Obj(o);
            return new TikTokVideo
            {
                Title = J.S(d, "title"), Product = J.S(d, "product"), Format = J.S(d, "format"),
                Score = Mathx.Clamp01(J.F(d, "score")), Views = Math.Max(0, J.I(d, "views")),
                Likes = Math.Max(0, J.I(d, "likes")), Comments = Math.Max(0, J.I(d, "comments")), Day = J.I(d, "day"),
            };
        }
    }

    /// <summary>
    /// Reine Bewertungsregeln für echte TikTok-Aufnahmen. Die Unity-Seite misst nur und ruft
    /// <see cref="Rate"/> (Ergebnis) bzw. <see cref="FrameHints"/> (Live-Hinweise) auf.
    /// </summary>
    public static class TikTokScoring
    {
        public const float MinSeconds = 10f;
        public const float MaxSeconds = 20f;
        /// <summary>Früher stoppen geht erst ab so vielen Sekunden.</summary>
        public const float StopAfterSeconds = 3f;
        public const float HookSeconds = 2f;
        public const float GoodDistMin = 0.45f;
        public const float GoodDistMax = 2.0f;
        public const float SteadyDegPerSec = 70f;
        public const float LightMin = 0.45f;
        /// <summary>Ab dieser Helligkeit gibt es volle Punkte fürs Licht.</summary>
        public const float LightGood = 0.75f;
        public const float CenterMin = 0.55f;
        /// <summary>Ab dieser Mittigkeit (Durchschnitt) gibt es volle Punkte – ganz ruhig mittig hält niemand.</summary>
        public const float CenterGood = 0.8f;
        public const float AngleFull = 120f;
        public const float TrendingFormatBonus = 0.1f;
        public const float MaxHypeBonus = 0.15f;

        public static readonly string[] CriterionIds = { "visible", "center", "distance", "steady", "light", "angles", "duration", "hook", "action", "brand" };

        public static string Label(string id)
        {
            switch (id)
            {
                case "visible": return "Produkt im Bild";
                case "center": return "Bildmitte";
                case "distance": return "Abstand";
                case "steady": return "Ruhige Kamera";
                case "light": return "Licht";
                case "angles": return "Blickwinkel";
                case "duration": return "Länge";
                case "hook": return "Hook (erste 2 s)";
                case "action": return "Aktionen";
                case "brand": return "Marke/Deko im Bild";
            }
            return id;
        }

        private static string TipFor(string id)
        {
            switch (id)
            {
                case "visible": return "Halte das Produkt die ganze Zeit im Rahmen.";
                case "center": return "Produkt in die Bildmitte.";
                case "distance": return "Näher ran – etwa eine Armlänge.";
                case "steady": return "Langsamer schwenken.";
                case "light": return "Mehr Licht: Lampe, Ringlicht oder tagsüber drehen.";
                case "angles": return "Geh einmal ums Produkt herum.";
                case "duration": return "Mindestens " + (int)MinSeconds + " Sekunden aufnehmen.";
                case "hook": return "Gleich am Anfang eine Aktion (E) für den Hook.";
                case "action": return "Mehr Aktionen: auspacken, schütteln, zeigen.";
                case "brand": return "Poster, Neon oder Schild mit ins Bild.";
            }
            return "";
        }

        /// <summary>Gewicht eines Kriteriums im Format (1 = normal).</summary>
        public static float Weight(string format, string id)
        {
            switch (format)
            {
                case TikTokFormats.Unboxing:
                    if (id == "hook") return 2f;
                    if (id == "distance" || id == "action") return 1.5f;
                    if (id == "angles" || id == "brand") return 0.5f;
                    break;
                case TikTokFormats.Pov:
                    if (id == "steady" || id == "angles") return 1.8f;
                    if (id == "brand") return 1.5f;
                    if (id == "hook" || id == "action") return 0.5f;
                    break;
                case TikTokFormats.Review:
                    if (id == "center" || id == "light") return 1.6f;
                    if (id == "steady" || id == "duration") return 1.4f;
                    if (id == "hook") return 0.5f;
                    break;
                case TikTokFormats.Hack:
                    if (id == "action") return 2f;
                    if (id == "hook" || id == "visible") return 1.5f;
                    if (id == "brand") return 0.5f;
                    break;
            }
            return 1f;
        }

        /// <summary>Erfüllung eines Kriteriums (0..1) aus den Messwerten.</summary>
        public static float Value(string format, string id, TikTokTake t)
        {
            if (t == null) return 0f;
            switch (id)
            {
                case "visible": return Mathx.Clamp01(t.VisibleFrac / 0.85f);
                case "center": return Mathx.Clamp01(t.Centered / CenterGood);
                case "distance": return Mathx.Clamp01(t.DistanceFrac);
                case "steady": return Mathx.Clamp01(t.SteadyFrac);
                case "light": return Mathx.Clamp01(t.Light / LightGood);
                case "angles": return Mathx.Clamp01(t.AngleDegrees / AngleFull);
                case "duration":
                {
                    float d = Math.Max(0f, t.Duration);
                    float ideal = format == TikTokFormats.Review ? 15f : MinSeconds;
                    if (d >= ideal) return 1f;
                    if (d >= MinSeconds) return 0.8f + 0.2f * (d - MinSeconds) / Math.Max(0.01f, ideal - MinSeconds);
                    return 0.7f * d / MinSeconds;
                }
                case "hook": return t.Hook ? 1f : 0f;
                case "action": return Mathx.Clamp01(t.Actions / 3f);
                case "brand": return Mathx.Clamp01(t.BrandFrac / 0.3f);
            }
            return 0f;
        }

        /// <summary>Bonus (0..<see cref="MaxHypeBonus"/>) für ein Produkt mit Hype.</summary>
        public static float HypeBonus(float hype) => Mathx.Clamp((hype - 1f) * 0.25f, 0f, MaxHypeBonus);

        /// <summary>Bewertet eine Aufnahme. trendingFormat = aktuelles Trend-Format (Bonus, wenn gleich).</summary>
        public static TikTokRating Rate(TikTokTake t, string format, string trendingFormat)
        {
            var r = new TikTokRating();
            if (t == null) t = new TikTokTake();
            if (!TikTokFormats.IsFormat(format)) format = TikTokFormats.Review;
            float sum = 0f, wsum = 0f;
            foreach (var id in CriterionIds)
            {
                float w = Weight(format, id), v = Value(format, id, t);
                r.Criteria.Add(new TikTokCriterion { Id = id, Label = Label(id), Value = v, Weight = w, Tip = v < 0.6f ? TipFor(id) : "" });
                sum += w * v;
                wsum += w;
            }
            r.Quality = wsum > 0f ? sum / wsum : 0f;
            r.HypeBonus = HypeBonus(t.Hype);
            r.TrendingFormat = !string.IsNullOrEmpty(trendingFormat) && trendingFormat == format;
            r.FormatBonus = r.TrendingFormat ? TrendingFormatBonus : 0f;
            float score = r.Quality + r.HypeBonus + r.FormatBonus;
            if (t.VisibleFrac < 0.15f)
            {
                score = Math.Min(score, 0.15f);
                r.Penalty = "Das Produkt war kaum im Bild.";
            }
            if (t.Duration < StopAfterSeconds)
            {
                score *= Math.Max(0f, t.Duration) / StopAfterSeconds;
                r.Penalty = "Viel zu kurz.";
            }
            r.Score = Mathx.Clamp01(score);
            r.PredictedViews = PredictViews(r.Score, t.Hype);
            r.Grade = Grade(r.Score);
            return r;
        }

        /// <summary>Prognostizierte Aufrufe (deterministisch).</summary>
        public static int PredictViews(float score, float hype)
        {
            float s = Mathx.Clamp01(score);
            return Mathx.RoundToInt(400f + s * s * 250000f * (1f + HypeBonus(hype) * 2f));
        }

        public static string Grade(float score)
        {
            if (score >= 0.85f) return "Viral-Material";
            if (score >= 0.65f) return "Stark";
            if (score >= 0.45f) return "Ganz okay";
            if (score >= 0.25f) return "Naja";
            return "Cringe";
        }

        /// <summary>
        /// Live-Hinweise für einen Frame (kurz, Deutsch). Positive Hinweise mit ✓.
        /// center = 0..1 (1 = mittig), distance in Metern, rotSpeed in Grad/s, light 0..1.
        /// </summary>
        public static List<string> FrameHints(bool visible, float center, float distance, float rotSpeed, float light, float elapsed, bool hookDone)
        {
            var l = new List<string>();
            if (!visible) l.Add("Produkt nicht im Bild!");
            else
            {
                l.Add("Produkt im Bild ✓");
                if (center < CenterMin) l.Add("mehr zur Mitte");
                if (distance > GoodDistMax) l.Add("näher ran");
                else if (distance < GoodDistMin) l.Add("zu nah");
            }
            if (rotSpeed > SteadyDegPerSec) l.Add("wackelt");
            if (light < LightMin) l.Add("zu dunkel");
            if (!hookDone && elapsed < HookSeconds) l.Add("Jetzt Hook!");
            if (elapsed >= MinSeconds) l.Add("Länge ✓");
            return l;
        }
    }

    public sealed partial class Sim
    {
        /// <summary>Höchstens so viele eigene Videos merkt sich der TikTak-Feed.</summary>
        public const int TikTokVideoLimit = 12;

        /// <summary>Gepostete eigene Videos, neuestes zuerst (gespeichert).</summary>
        public readonly List<TikTokVideo> TikTokVideos = new List<TikTokVideo>();

        /// <summary>Trend-Format des Tages (wechselt täglich, verbraucht keinen Zufall).</summary>
        public string TrendingTikTokFormat()
        {
            int d = Math.Max(1, Day);
            return TikTokFormats.All[(d * 7 + 3) % TikTokFormats.All.Length];
        }

        /// <summary>Liegt dieses Produkt im Lager (zum Filmen)? Ein gehaltenes Stück prüft die Unity-Seite.</summary>
        public bool CanFilmProduct(string product) => GameData.IsProduct(product) && StockQty(product) > 0;

        /// <summary>
        /// Warum gerade kein TikTok geht (Deutsch, kurz) – null, wenn eins möglich ist.
        /// Reihenfolge: Story, Feierabend, Level, Tageslimit, Abklingzeit.
        /// </summary>
        public string TikTokBlocker()
        {
            if (StoryStage != "business") return "Erst die Schicht bei Kalle beenden.";
            if (DayOver) return "Feierabend – morgen wieder.";
            if (Level < GameData.TikTokLevel) return "TikTok gibt's ab Firmenlevel " + GameData.TikTokLevel + ".";
            if (TikToksLeftToday() <= 0) return "Tageslimit erreicht (" + GameData.TikTokPerDay + " pro Tag).";
            float wait = TikTokReadyAt - BClock();
            if (wait > 0f) return "Nächstes Video in " + Math.Max(1, (int)Math.Ceiling(wait)) + " min.";
            return null;
        }

        /// <summary>
        /// Wirkung eines TikToks (ohne es zu posten): Nachfrage-Faktor und Dauer in Minuten.
        /// Genau die Formel von <see cref="TriggerTikTok"/> – für die Vorschau auf der Ergebniskarte.
        /// </summary>
        public void TikTokEffect(float score, string product, out float mult, out float minutes)
        {
            float s = Mathx.Clamp(score, 0.05f, 1f);
            float power = TikTokPowerMult();
            if (GameData.IsProduct(product) && TrendMult(product) >= 1.3f) power *= GameData.TikTokTrendBonus;
            mult = 1f + (Mathx.Lerp(GameData.TikTokMinMult, GameData.TikTokMaxMult, s) - 1f) * power;
            minutes = Mathx.Lerp(GameData.TikTokMinMinutes, GameData.TikTokMaxMinutes, s) * power;
        }

        /// <summary>
        /// Postet eine echte Aufnahme: prüft Limit und Abklingzeit, löst <see cref="TriggerTikTok"/>
        /// mit der Wertung aus und legt das Video in den Feed. null, wenn gerade nicht erlaubt.
        /// </summary>
        public TikTokVideo PostTikTokVideo(TikTokRating rating, string product, string format)
        {
            if (rating == null || TikTokBlocker() != null) return null;
            string pid = GameData.IsProduct(product) ? product : "";
            float score = Mathx.Clamp01(rating.Score);
            TriggerTikTok(score, false, pid);
            int views = Math.Max(0, rating.PredictedViews);
            var v = new TikTokVideo
            {
                Title = TikTokFormats.Title(format, pid != "" ? GameData.Product(pid).Name : "", score),
                Product = pid, Format = TikTokFormats.IsFormat(format) ? format : "", Score = score,
                Views = views, Likes = views / 9, Comments = views / 120 + 3, Day = Day,
            };
            TikTokVideos.Insert(0, v);
            while (TikTokVideos.Count > TikTokVideoLimit) TikTokVideos.RemoveAt(TikTokVideos.Count - 1);
            RaiseEconomyChanged();
            return v;
        }

        private List<object> TikTokVideosToJson()
        {
            var l = new List<object>();
            foreach (var v in TikTokVideos) l.Add(v.ToJson());
            return l;
        }

        private void ReadTikTokVideos(Dictionary<string, object> s)
        {
            TikTokVideos.Clear();
            foreach (var o in J.A(s, "tiktok_videos"))
            {
                if (TikTokVideos.Count >= TikTokVideoLimit) break;
                TikTokVideos.Add(TikTokVideo.FromJson(o));
            }
        }
    }
}
