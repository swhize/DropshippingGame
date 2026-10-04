using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// TikTok-Studio (Unity-Seite der echten Aufnahme): Auswahl von Produkt und Format, Start der
    /// Aufnahme (<see cref="TikTokRecorder"/>), Ergebniskarte mit Aufschlüsselung und Posten.
    /// Einstiege: Handy-App „TikTak“, Laptop › TikTak › Video aufnehmen und das Ringlicht am
    /// Schreibtisch (<see cref="TikTokRingLight"/>). Die Regeln selbst stehen in der Core
    /// (<see cref="TikTokScoring"/>, <see cref="Sim.PostTikTokVideo"/>).
    /// Die letzte Aufnahme bleibt als Entwurf bis Tagesende erhalten (nicht im Spielstand).
    /// </summary>
    public static class TikTokStudio
    {
        public const string AppTitle = "TikTak";

        /// <summary>Gewähltes Produkt (Id) und Format – gemeinsam für Handy, Laptop und Ringlicht.</summary>
        public static string Product = "";
        public static string Format = "";

        private sealed class Take
        {
            public string Product = "", Format = "";
            public TikTokRating Rating;
            public int Day, Slot;
        }

        private static Take _draft;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Product = "";
            Format = "";
            _draft = null;
        }

        // =====================================================================================
        // Auswahl
        // =====================================================================================
        /// <summary>Produkt, das der Spieler gerade in der Hand hat (Einzelstück oder Kiste), sonst "".</summary>
        public static string HeldProduct()
        {
            var p = Game.Player;
            var h = p != null ? p.Held : null;
            if (h == null || (h.Kind != ItemKind.Item && h.Kind != ItemKind.Crate)) return "";
            return GameData.IsProduct(h.Product) ? h.Product : "";
        }

        /// <summary>Was sich filmen lässt: zuerst das gehaltene Produkt, dann alles mit Lagerbestand.</summary>
        public static List<string> Filmable(Sim sim)
        {
            var list = new List<string>();
            string held = HeldProduct();
            if (held != "") list.Add(held);
            if (sim == null) return list;
            foreach (var p in GameData.Products)
                if (p != null && !list.Contains(p.Id) && sim.CanFilmProduct(p.Id))
                    list.Add(p.Id);
            return list;
        }

        /// <summary>Hält Produkt und Format gültig (gehaltenes Produkt bzw. größter Hype zuerst, Format = Trend).</summary>
        public static void EnsureSelection(Sim sim)
        {
            if (sim == null) return;
            var list = Filmable(sim);
            if (list.Count == 0) Product = "";
            else if (!list.Contains(Product ?? ""))
            {
                string held = HeldProduct();
                string best = list[0];
                if (held == "")
                    foreach (var id in list)
                        if (sim.TrendMult(id) > sim.TrendMult(best) + 0.01f)
                            best = id;
                Product = best;
            }
            if (!TikTokFormats.IsFormat(Format)) Format = sim.TrendingTikTokFormat();
        }

        public static string ProductName(string id) => GameData.IsProduct(id) ? GameData.Product(id).Name : "Produkt";

        /// <summary>Kurzinfo zu einem filmbaren Produkt: Hand/Lager und Hype.</summary>
        public static string ProductSub(Sim sim, string id)
        {
            if (sim == null || !GameData.IsProduct(id)) return "";
            string s = id == HeldProduct() ? "in der Hand" : sim.StockQty(id) + " im Lager";
            float hype = sim.TrendMult(id);
            if (hype >= 1.3f) s += " · Hype ×" + Fmt.Dec(hype, 1);
            return s;
        }

        public static string FormatLabel(Sim sim, string format)
        {
            string n = TikTokFormats.Name(format);
            return sim != null && sim.TrendingTikTokFormat() == format ? n + " · TREND" : n;
        }

        /// <summary>Warum gerade nicht gefilmt werden kann (inkl. „kein Produkt“), sonst null.</summary>
        public static string RecordBlocker(Sim sim)
        {
            if (sim == null) return "Gerade nicht möglich.";
            string b = sim.TikTokBlocker();
            if (b != null) return b;
            if (Filmable(sim).Count == 0) return "Nichts zum Filmen: erst Ware einlagern oder ein Produkt in die Hand nehmen.";
            return null;
        }

        // =====================================================================================
        // Aufnahme starten / Entwurf / Posten
        // =====================================================================================
        /// <summary>Schließt Handy/Laptop/Auswahl und startet die Aufnahme mit der aktuellen Auswahl.</summary>
        public static bool StartRecording()
        {
            var sim = Game.Sim;
            var player = Game.Player;
            var ui = Game.UI;
            if (sim == null || player == null) return false;
            EnsureSelection(sim);
            string block = RecordBlocker(sim);
            if (block != null)
            {
                sim.Notify(block, "info");
                Game.Sound("error");
                return false;
            }
            if (ui != null)
            {
                if (ui.Phone != null) ui.Phone.Close(true);
                if (ui.Modal != null && ui.Modal.IsOpen && (ui.Modal.Current ?? "").StartsWith("tiktok", StringComparison.Ordinal)) ui.Modal.Close();
            }
            if (sim.PcOpen) sim.ClosePc();
            bool ok = false;
            try
            {
                ok = player.Recorder.Begin(player, Product, Format);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            if (!ok) sim.Notify("Aufnahme konnte nicht starten. Such dir etwas mehr Platz.", "info");
            return ok;
        }

        public static bool HasDraft
        {
            get
            {
                var sim = Game.Sim;
                if (_draft == null || sim == null) return false;
                if (_draft.Day != sim.Day || _draft.Slot != sim.Slot || sim.DayOver)
                {
                    _draft = null;
                    return false;
                }
                return true;
            }
        }

        public static void ClearDraft() => _draft = null;

        public static void PostDraft()
        {
            var sim = Game.Sim;
            if (sim == null || !HasDraft) return;
            var d = _draft;
            var v = sim.PostTikTokVideo(d.Rating, d.Product, d.Format);
            if (v == null)
            {
                sim.Notify("Posten geht gerade nicht: " + (sim.TikTokBlocker() ?? "später nochmal versuchen."), "bad");
                Game.Sound("error");
                return;
            }
            _draft = null;
            Game.Sound("levelup", 0.05f, -6f);
            var ui = Game.UI;
            if (ui != null)
            {
                ui.Hud?.ShowBanner("Video ist online!", v.Title + " · ≈ " + Fmt.Thousands(v.Views) + " Aufrufe", "TIKTAK");
                ui.Phone?.MarkDirty();
            }
        }

        private static void Retake()
        {
            if (_draft != null)
            {
                Product = _draft.Product;
                Format = _draft.Format;
            }
            StartRecording();
        }

        // =====================================================================================
        // Ergebniskarte
        // =====================================================================================
        public static void ShowResult(string product, string format, TikTokRating rating)
        {
            var sim = Game.Sim;
            if (sim == null || rating == null) return;
            _draft = new Take { Product = product ?? "", Format = format ?? "", Rating = rating, Day = sim.Day, Slot = sim.Slot };
            Product = product ?? "";
            Format = format ?? "";
            var ui = Game.UI;
            if (ui == null || ui.Modal == null)
            {
                sim.Notify("Aufnahme fertig: " + Mathf.RoundToInt(rating.Score * 100f) + " %. Posten über Handy › TikTak.", "info");
                return;
            }
            string block = sim.TikTokBlocker();
            var buttons = new List<ModalButton>();
            if (block == null) buttons.Add(new ModalButton("Posten", PostDraft, "accent", "send"));
            buttons.Add(new ModalButton("Nochmal aufnehmen", Retake, block == null ? "" : "accent", "refresh"));
            buttons.Add(new ModalButton("Später", null, "ghost", "clock"));
            var body = ui.Modal.Open("Dein TikTok ist im Kasten", TikTokFormats.Name(format) + " · " + ProductName(product), 640, buttons, true, "tiktok_result", null, "music");
            try
            {
                BuildResult(body, sim, product, format, rating, block);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UIX.Text(body, "Wertung: " + Mathf.RoundToInt(rating.Score * 100f) + " %");
            }
        }

        private static Color ScoreColor(float v) => v >= 0.65f ? Theme.Good : (v >= 0.4f ? Theme.Accent : Theme.Bad);

        private static void BuildResult(VisualElement body, Sim sim, string product, string format, TikTokRating r, string block)
        {
            var head = UIX.Row(body, 16f);
            head.style.alignItems = Align.Center;
            var big = UIX.Text(head, Mathf.RoundToInt(r.Score * 100f) + " %");
            big.style.fontSize = 46;
            big.style.unityFontStyleAndWeight = FontStyle.Bold;
            big.style.color = ScoreColor(r.Score);
            var hv = UIX.Col(head, 2f);
            hv.style.flexShrink = 1;
            UIX.Text(hv, r.Grade, "h2");
            UIX.Text(hv, "Prognose: ≈ " + Fmt.Thousands(r.PredictedViews) + " Aufrufe", "muted");

            var chips = UIX.Row(body, 6f);
            chips.style.flexWrap = Wrap.Wrap;
            UIX.Chip(chips, "Qualität " + UiFmt.Percent(r.Quality), "eye");
            if (r.TrendingFormat) UIX.Chip(chips, "Trend-Format +" + UiFmt.Percent(r.FormatBonus), "fire", Theme.Accent);
            if (r.HypeBonus > 0.001f) UIX.Chip(chips, "Hype +" + UiFmt.Percent(r.HypeBonus), "trend", Theme.Accent);

            if (!string.IsNullOrEmpty(r.Penalty))
            {
                var pen = UIX.Text(body, r.Penalty);
                pen.style.color = Theme.Bad;
                pen.style.whiteSpace = WhiteSpace.Normal;
            }

            var card = UIX.Card(body, "Aufschlüsselung");
            foreach (var c in r.Criteria)
            {
                if (c == null) continue;
                var row = UIX.Row(card, 8f);
                row.style.alignItems = Align.Center;
                var lbl = UIX.Text(row, c.Label);
                lbl.style.width = 160;
                lbl.style.flexShrink = 0;
                var bar = UIX.Bar(row, c.Value, ScoreColor(c.Value), 7f);
                bar.style.flexGrow = 1;
                bar.style.flexShrink = 1;
                var pct = UIX.Text(row, UiFmt.Percent(c.Value), "muted");
                pct.style.width = 48;
                pct.style.flexShrink = 0;
                pct.style.unityTextAlign = TextAnchor.MiddleRight;
                string w = c.Weight >= 1.45f ? "wichtig" : (c.Weight <= 0.55f ? "Nebensache" : "");
                var wl = UIX.Text(row, w, "small");
                wl.style.width = 78;
                wl.style.flexShrink = 0;
                if (c.Weight >= 1.45f) wl.style.color = Theme.Accent;
            }

            // Die drei wichtigsten Verbesserungen (Gewicht × fehlender Anteil)
            var tips = new List<TikTokCriterion>();
            foreach (var c in r.Criteria)
                if (c != null && !string.IsNullOrEmpty(c.Tip))
                    tips.Add(c);
            tips.Sort((a, b) => (b.Weight * (1f - b.Value)).CompareTo(a.Weight * (1f - a.Value)));
            if (tips.Count > 0)
            {
                var tc = UIX.Card(body, "Nächstes Mal besser");
                for (int i = 0; i < tips.Count && i < 3; i++)
                {
                    var t = UIX.Text(tc, "• " + tips[i].Tip);
                    t.style.whiteSpace = WhiteSpace.Normal;
                }
            }

            sim.TikTokEffect(r.Score, product, out float mult, out float minutes);
            var eff = UIX.Text(body, "Posten bringt ×" + Fmt.Dec(mult, 1) + " Nachfrage für " + UiFmt.Duration(minutes) + ". Heute noch " +
                                     sim.TikToksLeftToday() + " von " + GameData.TikTokPerDay + " Videos.", "muted");
            eff.style.whiteSpace = WhiteSpace.Normal;
            if (block != null)
            {
                var b = UIX.Text(body, "Posten gerade nicht möglich: " + block + " Die Aufnahme bleibt bis Feierabend als Entwurf (Handy › TikTak).");
                b.style.color = Theme.Bad;
                b.style.whiteSpace = WhiteSpace.Normal;
            }
            else
            {
                var hint = UIX.Text(body, "„Später“ hebt die Aufnahme bis Feierabend als Entwurf auf (Handy › TikTak).", "small");
                hint.style.whiteSpace = WhiteSpace.Normal;
            }
        }

        // =====================================================================================
        // Auswahl als Fenster (Ringlicht) und als Handy-App
        // =====================================================================================
        /// <summary>Auswahlfenster (vom Ringlicht am Schreibtisch).</summary>
        public static void OpenPicker()
        {
            var sim = Game.Sim;
            var ui = Game.UI;
            if (sim == null || ui == null || ui.Modal == null) return;
            string block = RecordBlocker(sim);
            if (block != null)
            {
                sim.Notify(block, "info");
                Game.Sound("error");
                return;
            }
            EnsureSelection(sim);
            var body = ui.Modal.Open("TikTok aufnehmen", "Creator-Ecke · Produkt und Format wählen", 600, new[]
            {
                new ModalButton("Aufnahme starten", () => StartRecording(), "accent", "play"),
                new ModalButton("Abbrechen", null, "ghost", "close"),
            }, true, "tiktok_pick", null, "music");
            Action rebuild = null;
            rebuild = () =>
            {
                body.Clear();
                BuildPicker(body, sim, rebuild, false);
            };
            rebuild();
        }

        /// <summary>Produkt- und Format-Auswahl mit UIX-Bausteinen (Handy und Fenster).</summary>
        public static void BuildPicker(VisualElement parent, Sim sim, Action refresh, bool phone)
        {
            EnsureSelection(sim);
            string sectionCls = phone ? "phone-section" : "eyebrow";
            UIX.Text(parent, "PRODUKT", sectionCls);
            var pw = UIX.Wrap(parent);
            foreach (var id in Filmable(sim))
            {
                string pid = id;
                var b = UIX.Button(pw, GameData.Product(pid).Short + " · " + ProductSub(sim, pid), () =>
                {
                    Product = pid;
                    Game.Sound("click", 0.05f, -8f);
                    refresh?.Invoke();
                }, pid == Product ? "accent" : "ghost");
                b.AddToClassList("btn-sm");
            }
            UIX.Text(parent, "FORMAT", sectionCls);
            var fw = UIX.Wrap(parent);
            foreach (var f in TikTokFormats.All)
            {
                string fid = f;
                var b = UIX.Button(fw, FormatLabel(sim, fid), () =>
                {
                    Format = fid;
                    Game.Sound("click", 0.05f, -8f);
                    refresh?.Invoke();
                }, fid == Format ? "accent" : "ghost", false, sim.TrendingTikTokFormat() == fid ? "fire" : null);
                b.AddToClassList("btn-sm");
            }
            var tip = UIX.Text(parent, TikTokFormats.Tip(Format), phone ? "phone-item-sub" : "muted");
            tip.style.whiteSpace = WhiteSpace.Normal;
            var how = UIX.Text(parent, "So geht's: Das Produkt steht vor dir, du filmst in Ego-Sicht 10–20 Sekunden. " +
                                       GameInput.KeyLabel("rec_action") + " = Aktion (gleich am Anfang = Hook), " +
                                       GameInput.KeyLabel("rec_stop") + " = Stopp, " + GameInput.KeyLabel("rec_cancel") + " = Abbrechen. " +
                                       "Bewertet werden Bildmitte, Abstand, ruhige Kamera, Licht, Blickwinkel, Länge und Deko im Bild.",
                phone ? "phone-item-sub" : "small");
            how.style.whiteSpace = WhiteSpace.Normal;
        }

        /// <summary>Registriert die Handy-App „TikTak“ (einmalig, ab Firmenlevel <see cref="GameData.TikTokLevel"/>).</summary>
        public static void RegisterPhoneApp()
        {
            if (PhoneView.ExtraApps.Exists(a => a != null && a.Title == AppTitle)) return;
            PhoneView.ExtraApps.Add(new PhoneView.PhoneApp
            {
                Title = AppTitle, TabLabel = AppTitle, Icon = "music",
                Visible = () =>
                {
                    var s = Game.Sim;
                    return s != null && s.Level >= GameData.TikTokLevel;
                },
                Badge = () => HasDraft ? "1" : "",
                Build = BuildPhone,
            });
        }

        private static void BuildPhone(PhoneView view, VisualElement content)
        {
            var sim = Game.Sim;
            if (sim == null || content == null) return;
            Action refresh = () => view?.Rebuild();
            var sub = UIX.Text(content, "Heute noch " + sim.TikToksLeftToday() + " von " + GameData.TikTokPerDay + " · Trend-Format: " +
                                        TikTokFormats.Name(sim.TrendingTikTokFormat()), "phone-sub");
            sub.style.whiteSpace = WhiteSpace.Normal;

            if (HasDraft)
            {
                var d = _draft;
                var card = UIX.Col(content, 6f, "phone-item");
                card.style.alignItems = Align.Stretch;
                UIX.Text(card, "ENTWURF", "phone-section").style.marginTop = 0;
                UIX.Text(card, TikTokFormats.Name(d.Format) + " · " + ProductName(d.Product), "phone-item-title");
                UIX.Text(card, Mathf.RoundToInt(d.Rating.Score * 100f) + " % · " + d.Rating.Grade + " · ≈ " + Fmt.Thousands(d.Rating.PredictedViews) + " Aufrufe", "phone-item-sub");
                var br = UIX.Row(card, 6f);
                string block = sim.TikTokBlocker();
                var post = UIX.Button(br, "Posten", () =>
                {
                    PostDraft();
                    refresh();
                }, "accent", block != null, "send");
                post.style.flexGrow = 1;
                var drop = UIX.Button(br, "Verwerfen", () =>
                {
                    ClearDraft();
                    refresh();
                }, "ghost");
                drop.style.flexGrow = 1;
                if (block != null) UIX.Text(card, block, "phone-item-sub").style.whiteSpace = WhiteSpace.Normal;
            }

            string rb = RecordBlocker(sim);
            if (rb != null) UIX.Empty(content, sim.TikToksLeftToday() <= 0 ? "calendar" : "hourglass", "Gerade keine Aufnahme", rb);
            else
            {
                BuildPicker(content, sim, refresh, true);
                var start = UIX.Button(content, "Aufnahme starten", () => StartRecording(), "accent", false, "play");
                start.style.marginTop = 10;
            }

            if (sim.TikTokVideos.Count > 0)
            {
                UIX.Text(content, "DEINE VIDEOS", "phone-section");
                int n = 0;
                foreach (var v in sim.TikTokVideos)
                {
                    if (v == null) continue;
                    if (++n > 4) break;
                    var item = UIX.Col(content, 2f, "phone-item");
                    item.style.alignItems = Align.Stretch;
                    UIX.Ellipsis(UIX.Text(item, v.Title, "phone-item-title"));
                    UIX.Text(item, "Tag " + v.Day + " · " + Mathf.RoundToInt(v.Score * 100f) + " % · " + Fmt.Thousands(v.Views) + " Aufrufe", "phone-item-sub");
                }
            }
        }
    }
}
