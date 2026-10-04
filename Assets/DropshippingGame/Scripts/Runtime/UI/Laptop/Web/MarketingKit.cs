using System;
using System.Collections.Generic;
using System.Reflection;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Comic-Gesicht (Painter2D) für Fake-Leute und Fake-Profile – deterministisch aus einem Seed.</summary>
    public sealed class FaceAvatar : VisualElement
    {
        private static readonly Color[] Skins = { W.Hex("#F6D2B8"), W.Hex("#E8B48E"), W.Hex("#C68A60"), W.Hex("#8D5A3B"), W.Hex("#FFE0C8") };
        private static readonly Color[] Hairs = { W.Hex("#2B1B12"), W.Hex("#7A4A22"), W.Hex("#E8C15A"), W.Hex("#B23A2A"), W.Hex("#9A9AA2"), W.Hex("#4C6FFF") };
        private static readonly Color[] Bgs = { W.Hex("#DDE4FF"), W.Hex("#FFE0C8"), W.Hex("#D8F5C8"), W.Hex("#FFD6F2"), W.Hex("#FFF3B0"), W.Hex("#D0F4F0") };
        private readonly int _seed;

        public FaceAvatar(int seed, float size)
        {
            _seed = Math.Abs(seed);
            style.width = size;
            style.height = size;
            style.flexShrink = 0;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            var p = ctx.painter2D;
            if (p == null || r.width < 6f) return;
            float s = Mathf.Min(r.width, r.height) / 100f;
            Vector2 V(float x, float y) => new Vector2(x * s, y * s);
            var ink = new Color(0.09f, 0.09f, 0.09f);
            p.lineWidth = 3f * s;
            p.strokeColor = ink;
            p.fillColor = Bgs[_seed % Bgs.Length];
            p.BeginPath();
            p.Arc(V(50, 50), 48f * s, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Fill();
            var hair = Hairs[(_seed / 7) % Hairs.Length];
            int style2 = (_seed / 3) % 3;
            if (style2 == 1)
            {
                p.fillColor = hair;
                p.BeginPath();
                p.Arc(V(50, 22), 14f * s, Angle.Degrees(0f), Angle.Degrees(360f));
                p.Fill();
                p.Stroke();
            }
            p.fillColor = Skins[(_seed / 5) % Skins.Length];
            p.BeginPath();
            p.Arc(V(50, 56), 28f * s, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Fill();
            p.Stroke();
            if (style2 != 2)
            {
                p.fillColor = hair;
                p.BeginPath();
                p.Arc(V(50, 52), 29f * s, Angle.Degrees(180f), Angle.Degrees(360f));
                p.ClosePath();
                p.Fill();
                p.Stroke();
            }
            p.fillColor = ink;
            p.BeginPath();
            p.Arc(V(40, 58), 3.2f * s, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Fill();
            p.BeginPath();
            p.Arc(V(60, 58), 3.2f * s, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Fill();
            p.BeginPath();
            if (_seed % 4 == 0)
            {
                p.MoveTo(V(42, 72));
                p.LineTo(V(58, 72));
            }
            else p.Arc(V(50, 66), 9f * s, Angle.Degrees(25f), Angle.Degrees(155f));
            p.Stroke();
        }
    }

    /// <summary>
    /// „Produktfoto“: Studio-Hintergrund in Produktfarbe mit Lichtflecken, Bodenschatten,
    /// Vektor-Icon und Glitzer. Abzeichen („NEU“, „−20 %“) kommen über <see cref="MK.Photo"/> als Labels.
    /// </summary>
    public sealed class ProductPhoto : VisualElement
    {
        private readonly string _id;

        public ProductPhoto(string productId)
        {
            _id = productId ?? "";
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            var p = ctx.painter2D;
            if (p == null || r.width < 8f || r.height < 8f) return;
            try
            {
                var col = GameData.IsProduct(_id) ? GameData.Product(_id).Color.ToColor() : new Color(0.7f, 0.7f, 0.8f);
                p.fillColor = W.Lighten(col, 0.72f);
                Rect(p, 0, 0, r.width, r.height);
                p.fillColor = new Color(col.r, col.g, col.b, 0.18f);
                Rect(p, 0, r.height * 0.72f, r.width, r.height * 0.28f);
                p.fillColor = new Color(1f, 1f, 1f, 0.35f);
                float m = Mathf.Min(r.width, r.height);
                Circle(p, r.width * 0.25f, r.height * 0.2f, m * 0.38f);
                p.fillColor = new Color(1f, 1f, 1f, 0.25f);
                Circle(p, r.width * 0.85f, r.height * 0.85f, m * 0.3f);
                float size = m * 0.66f;
                var o = new Vector2((r.width - size) / 2f, (r.height - size) / 2f - m * 0.05f);
                p.fillColor = new Color(0f, 0f, 0f, 0.16f);
                Ellipse(p, r.width / 2f, o.y + size * 0.98f, size * 0.42f, size * 0.07f);
                ProductArt.Icon(p, _id, o, size / 120f, true);
                p.fillColor = Color.white;
                Star(p, r.width * 0.8f, r.height * 0.22f, m * 0.05f);
                Star(p, r.width * 0.16f, r.height * 0.7f, m * 0.035f);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void Rect(Painter2D p, float x, float y, float w, float h)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x, y));
            p.LineTo(new Vector2(x + w, y));
            p.LineTo(new Vector2(x + w, y + h));
            p.LineTo(new Vector2(x, y + h));
            p.ClosePath();
            p.Fill();
        }

        private static void Circle(Painter2D p, float x, float y, float rad)
        {
            p.BeginPath();
            p.Arc(new Vector2(x, y), rad, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Fill();
        }

        private static void Ellipse(Painter2D p, float cx, float cy, float rx, float ry)
        {
            const float k = 0.5523f;
            p.BeginPath();
            p.MoveTo(new Vector2(cx + rx, cy));
            p.BezierCurveTo(new Vector2(cx + rx, cy + ry * k), new Vector2(cx + rx * k, cy + ry), new Vector2(cx, cy + ry));
            p.BezierCurveTo(new Vector2(cx - rx * k, cy + ry), new Vector2(cx - rx, cy + ry * k), new Vector2(cx - rx, cy));
            p.BezierCurveTo(new Vector2(cx - rx, cy - ry * k), new Vector2(cx - rx * k, cy - ry), new Vector2(cx, cy - ry));
            p.BezierCurveTo(new Vector2(cx + rx * k, cy - ry), new Vector2(cx + rx, cy - ry * k), new Vector2(cx + rx, cy));
            p.ClosePath();
            p.Fill();
        }

        private static void Star(Painter2D p, float x, float y, float rad)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x, y - rad));
            p.LineTo(new Vector2(x + rad * 0.3f, y - rad * 0.3f));
            p.LineTo(new Vector2(x + rad, y));
            p.LineTo(new Vector2(x + rad * 0.3f, y + rad * 0.3f));
            p.LineTo(new Vector2(x, y + rad));
            p.LineTo(new Vector2(x - rad * 0.3f, y + rad * 0.3f));
            p.LineTo(new Vector2(x - rad, y));
            p.LineTo(new Vector2(x - rad * 0.3f, y - rad * 0.3f));
            p.ClosePath();
            p.Fill();
        }
    }

    /// <summary>Gemeinsame Bausteine der Marketing-Apps (Fakebook, Gugel, Mein Shop).</summary>
    public static class MK
    {
        /// <summary>Produktfoto mit Abzeichen (oben links, gedreht).</summary>
        public static VisualElement Photo(VisualElement parent, string pid, float height, params string[] badges)
        {
            var box = UIX.Div(parent, "mk-photo");
            if (height > 0) box.style.height = height;
            box.style.overflow = Overflow.Hidden;
            box.pickingMode = PickingMode.Ignore;
            box.Add(new ProductPhoto(pid));
            if (badges != null)
            {
                int i = 0;
                foreach (var b in badges)
                {
                    if (string.IsNullOrEmpty(b)) continue;
                    var l = W.Text(box, b, WebSkin.Round, WebFonts.Bold, "mk-badge", i == 0 ? "a" : "b");
                    l.style.top = 8 + i * 26;
                    l.pickingMode = PickingMode.Ignore;
                    i++;
                }
            }
            return box;
        }

        /// <summary>Name eines Produkts ("Hülle").</summary>
        public static string Short(string pid) => GameData.IsProduct(pid) ? GameData.Product(pid).Short : pid;

        public static string Name(string pid) => GameData.IsProduct(pid) ? GameData.Product(pid).Name : pid;

        /// <summary>Gelistete, freigeschaltete Produkte.</summary>
        public static List<string> Listed(Sim s)
        {
            var l = new List<string>();
            if (s == null) return l;
            foreach (var p in GameData.Products)
                if (s.ProductUnlocked(p.Id) && s.IsListed(p.Id)) l.Add(p.Id);
            return l;
        }

        public static string Num(float v) => Fmt.Thousands(Mathf.RoundToInt(v));

        public static string Cents(int c) => Fmt.Eur(c / 100f);

        /// <summary>Kennzahl-Kachel: großer Wert, kleines Label, Icon.</summary>
        public static Label Kpi(VisualElement parent, string icon, string value, string label, WebSkin skin, params string[] cls)
        {
            var c = UIX.Col(parent, 2f, "mk-kpi");
            foreach (var x in cls)
                if (!string.IsNullOrEmpty(x)) c.AddToClassList(x);
            c.style.flexGrow = 1;
            c.style.flexBasis = 0;
            var top = UIX.Row(c, 6f);
            UIX.Icon(top, icon, 16f, null, "mk-kpi-ic");
            W.Text(top, label, skin, WebFonts.Body, "mk-kpi-label");
            return W.Text(c, value, skin, WebFonts.Title, "mk-kpi-value");
        }

        /// <summary>Fortschrittsbalken (0..1).</summary>
        public static VisualElement Bar(VisualElement parent, float t, string cls = "mk-bar")
        {
            var b = UIX.Div(parent, cls);
            var f = UIX.Div(b, "mk-bar-fill");
            f.style.width = Length.Percent(Mathf.Clamp01(t) * 100f);
            return b;
        }

        public static readonly string[] PeopleNames =
        {
            "Kevin M.", "Jacqueline B.", "Uwe aus Bottrop", "Tante Gabi", "Mehmet Y.", "Laura S.", "Dieter S.", "Fitness-Ben", "Oma Gerda", "Sven K.",
            "Chantal R.", "Paul P.", "Nina W.", "Horst", "Mia Katzenlieb",
        };

        public static readonly string[] PeoplePosts =
        {
            "Montag. Wieder. Wer hat das erlaubt?", "Hab heute 10.000 Schritte gemacht. Zum Kühlschrank und zurück.",
            "Teilen, wenn du auch findest, dass früher alles besser war!!", "Suche Handwerker. Bitte kein Kevin.", "Mein Kuchen. 3 Stunden. Sieht aus wie ein Stein. Schmeckt auch so.",
            "Wer kennt den Ort? Bitte keine falschen Antworten.", "Urlaub! (Balkon)", "Sonnenuntergang. Gefiltert. 47 Mal.", "Bin jetzt Coach. Für was, weiß ich noch nicht.",
            "Achtung! Neuer Trick von Betrügern. Bitte an alle weiterleiten!!!", "Hat jemand mein Fahrrad gesehen? Blau. Oder grün.", "Heute ist Weltkatzentag. Wie jeden Tag.",
        };

        public static readonly string[] AdComments =
        {
            "Ist das seriös?", "Hab ich gekauft. Kam an! Glaub ich.", "Wo ist der Haken?", "@Sven guck mal, das brauchst du", "Zu teuer. Kauf ich trotzdem.",
            "Gesehen auf TikTak!", "Mein Nachbar hat das auch.", "Fake.",
        };

        public static readonly string[] FakeComments =
        {
            "Bestes Produkt EVER!!! 10/10", "Hat mein Leben verändert. Echt jetzt.", "Kaufe ich schon zum dritten Mal!", "Super Shop, super schnell, super echt!",
            "Ich bin ein ganz normaler Mensch und finde das toll.", "Meine ganze Familie liebt es (alle 4 Profile).",
        };
    }

    /// <summary>
    /// Empfohlener Preis aus der Marken-Logik, falls ein anderes Modul ihn in der Sim anbietet
    /// (per Reflexion, ohne harte Abhängigkeit). Sonst knapp unter Marktpreis.
    /// </summary>
    public static class BrandPrice
    {
        private static readonly string[] Names = { "SuggestedPrice", "BrandSuggestedPrice", "SuggestedShopPrice", "RecommendedPrice", "BrandPrice" };
        private static MethodInfo _m;
        private static bool _looked;

        public static bool FromBrand
        {
            get
            {
                Look();
                return _m != null;
            }
        }

        private static void Look()
        {
            if (_looked) return;
            _looked = true;
            try
            {
                foreach (var n in Names)
                {
                    var m = typeof(Sim).GetMethod(n, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                    if (m != null && (m.ReturnType == typeof(int) || m.ReturnType == typeof(float) || m.ReturnType == typeof(double)))
                    {
                        _m = m;
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("BrandPrice: " + e.Message);
            }
        }

        /// <summary>Preisvorschlag in Euro (immer ≥ 1).</summary>
        public static int Suggested(Sim s, string pid)
        {
            if (s == null || !GameData.IsProduct(pid)) return 1;
            Look();
            if (_m != null)
            {
                try
                {
                    var v = Convert.ToSingle(_m.Invoke(s, new object[] { pid }));
                    if (v >= 1f) return Mathf.RoundToInt(v);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("BrandPrice: " + e.Message);
                }
            }
            return Mathf.Max(1, Mathf.RoundToInt(s.Market.MarketPrice(pid) * 0.97f));
        }
    }

    /// <summary>Meldet Start-Bildschirm, Fakebook und Gugel im Laptop sowie die Handy-Apps an.</summary>
    public static class MarketingApps
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Register()
        {
            try
            {
                LaptopView.Register(new LaptopView.AppDef
                {
                    Id = "home", Title = "Start", Icon = "grid", Group = "BETRIEB", Make = () => new AppTabletHome(),
                    Fav = "H", FavColor = new Color(1f, 0.48f, 0.27f), Url = "hustle.os/start",
                });
                LaptopView.Register(new LaptopView.AppDef
                {
                    Id = "fakebook", Title = "Fakebook", Icon = "thumb", Group = "VERKAUF", Make = () => new WebFakebook(),
                    Fav = "f", FavColor = W.Hex("#3B5BDB"),
                    Badge = () => Game.Sim != null && Game.Sim.ActiveAdCount("fakebook") > 0 ? Game.Sim.ActiveAdCount("fakebook").ToString() : "",
                }, "tiktak");
                LaptopView.Register(new LaptopView.AppDef
                {
                    Id = "gugel", Title = "Gugel", Icon = "search", Group = "VERKAUF", Make = () => new WebGugel(),
                    Fav = "G", FavColor = W.Hex("#34A853"),
                    Badge = () => Game.Sim != null && Game.Sim.ActiveAdCount("gugel") > 0 ? Game.Sim.ActiveAdCount("gugel").ToString() : "",
                }, "fakebook");
                LaptopView.Aliases["facebook"] = "fakebook";
                LaptopView.Aliases["google"] = "gugel";
                LaptopView.Aliases["seo"] = "gugel/sites";

                if (!PhoneView.ExtraApps.Exists(a => a != null && a.Title == "Fakebook"))
                    PhoneView.ExtraApps.Add(new PhoneView.PhoneApp
                    {
                        Title = "Fakebook", TabLabel = "Fakeb.", Icon = "thumb", Visible = () => Game.Sim != null && Game.Sim.FakebookUnlocked,
                        Badge = () => Game.Sim != null && Game.Sim.ActiveAdCount("fakebook") > 0 ? Game.Sim.ActiveAdCount("fakebook").ToString() : "",
                        Build = PhoneMarketing.BuildFakebook,
                    });
                if (!PhoneView.ExtraApps.Exists(a => a != null && a.Title == "Gugel"))
                    PhoneView.ExtraApps.Add(new PhoneView.PhoneApp
                    {
                        Title = "Gugel", TabLabel = "Gugel", Icon = "search", Visible = () => Game.Sim != null && Game.Sim.GugelUnlocked,
                        Badge = () => Game.Sim != null && Game.Sim.ActiveAdCount("gugel") > 0 ? Game.Sim.ActiveAdCount("gugel").ToString() : "",
                        Build = PhoneMarketing.BuildGugel,
                    });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    /// <summary>Kompakte Handy-Versionen von Fakebook und Gugel.</summary>
    public static class PhoneMarketing
    {
        private static void Stats(VisualElement content, Sim s, string platform)
        {
            var ads = s.ActiveAds(platform);
            if (ads.Count == 0) UIX.Text(content, "Keine Anzeige aktiv.", "phone-sub");
            foreach (var c in ads)
            {
                var card = UIX.Col(content, 4f, "phone-item");
                card.style.alignItems = Align.Stretch;
                var r = UIX.Row(card, 8f);
                var p = GameData.Product(c.Product);
                UIX.Round(UIX.Swatch(r, p.Color.ToColor(), 34f, p.Icon), 10f);
                var mid = UIX.Col(r, 1f);
                mid.style.flexGrow = 1;
                UIX.Text(mid, p.Short + (c.IsGugel ? " · Platz " + c.Position : "") + "  ×" + Fmt.Dec(c.Mult, 2), "phone-item-title");
                UIX.Text(mid, MK.Num(c.Impressions) + " Views · " + MK.Num(c.Clicks) + " Klicks · " + Mathf.RoundToInt(c.Sales) + " Verk.", "phone-item-sub");
                int id = c.Id;
                UIX.Button(r, "", () => Game.Sim?.StopAdCampaign(id), "", false, "close");
                UIX.Bar(card, c.Budget > 0 ? c.Spent / c.Budget : 0f, Theme.LaptopAccent, 4f);
            }
        }

        private static string Best(Sim s, string platform)
        {
            string best = "";
            foreach (var pid in MK.Listed(s))
            {
                if (s.CanStartAd(platform, pid, 1) == "Läuft schon") continue;
                if (best == "" || s.StockQty(pid) > s.StockQty(best)) best = pid;
            }
            return best;
        }

        public static void BuildFakebook(PhoneView view, VisualElement content)
        {
            var s = Game.Sim;
            if (s == null) return;
            UIX.Text(content, "ANZEIGEN", "phone-section");
            Stats(content, s, "fakebook");
            string best = Best(s, "fakebook");
            if (best != "")
            {
                int target = 0;
                float fit = 0f;
                for (int i = 0; i < GameData.AdTargets.Length; i++)
                    if (Sim.AdTargetFit(best, i) > fit)
                    {
                        fit = Sim.AdTargetFit(best, i);
                        target = i;
                    }
                string why = s.CanStartAd("fakebook", best, 50);
                UIX.Button(content, "Schnell-Ad " + MK.Short(best) + " · 50 €", () =>
                {
                    Game.Sim?.StartFakebookAd(best, target, 0, 50);
                    view.MarkDirty();
                }, "accent", why != "", "thumb");
            }
            UIX.Text(content, "FAKE-PROFILE", "phone-section");
            var r = UIX.Row(content, 8f, "phone-item");
            UIX.Icon(r, "users", 18f);
            var t = UIX.Text(r, s.FakeProfiles + " / " + GameData.MaxFakeProfiles + " · Risiko " + UiFmt.Percent(s.FakeProfileRisk()), "phone-item-title");
            t.style.flexGrow = 1;
            UIX.Button(r, "+" + GameData.FakeProfileCost + " €", () =>
            {
                Game.Sim?.CreateFakeProfile();
                view.MarkDirty();
            }, "", s.FakeProfiles >= GameData.MaxFakeProfiles || s.Money < GameData.FakeProfileCost);
        }

        public static void BuildGugel(PhoneView view, VisualElement content)
        {
            var s = Game.Sim;
            if (s == null) return;
            UIX.Text(content, "ANZEIGEN", "phone-section");
            Stats(content, s, "gugel");
            string best = Best(s, "gugel");
            if (best != "")
            {
                int bid = GameData.GugelRivalBids(best, 0)[0] + 1;
                string why = s.CanStartAd("gugel", best, 60);
                UIX.Button(content, "Platz 1: „" + GameData.FillProduct(GameData.GugelKeywords[0].Text, best) + "“ · 60 €", () =>
                {
                    Game.Sim?.StartGugelAd(best, 0, bid, 60);
                    view.MarkDirty();
                }, "accent", why != "", "search");
            }
            UIX.Text(content, "KI-SEITEN", "phone-section");
            if (s.FakeSites.Count == 0) UIX.Text(content, "Noch keine. Am Laptop bei Gugel erstellen.", "phone-sub");
            foreach (var site in s.FakeSites)
            {
                var r = UIX.Row(content, 8f, "phone-item");
                UIX.Icon(r, "globe", 16f);
                UIX.Ellipsis(UIX.Text(r, SlopGen.Domain(site), "phone-item-sub")).style.flexShrink = 1;
            }
        }
    }
}
