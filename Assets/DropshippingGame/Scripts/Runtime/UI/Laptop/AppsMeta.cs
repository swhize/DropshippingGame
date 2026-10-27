using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Registriert Firma › Erfolge und Firma › Statistik und verbindet Erfolge mit der Plattform (Steam).</summary>
    public static class MetaApps
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            LaptopView.RegisterTab("company", new LaptopView.TabDef
            {
                Id = "achievements", Title = "Erfolge", Icon = "trophy", Make = () => new AppAchievements(),
            }, "logistics");
            LaptopView.RegisterTab("company", new LaptopView.TabDef { Id = "stats", Title = "Statistik", Icon = "bars", Make = () => new AppStats() }, "achievements");
            LaptopView.Aliases["erfolge"] = "company/achievements";
            LaptopView.Aliases["statistik"] = "company/stats";
        }
    }

    /// <summary>
    /// Leitet Erfolge aus dem Kern an die Plattform weiter (Steam-API-Namen wie "ACH_FIRST_PARCEL") und meldet
    /// nach dem Laden alle bereits freigeschalteten erneut (offline erspielte werden so nachgetragen).
    /// </summary>
    public static class AchievementBridge
    {
        private static Sim _attached;

        public static void Attach(Sim sim)
        {
            if (sim == null || ReferenceEquals(sim, _attached)) return;
            _attached = sim;
            sim.AchievementUnlocked += a =>
            {
                if (a != null) Platform.UnlockAchievement(a.Id);
            };
            sim.AchievementsLoaded += ids =>
            {
                foreach (var id in ids) Platform.UnlockAchievement(id);
            };
            sim.DayStarted += _ =>
            {
                Platform.SetStat("STAT_PARCELS", sim.TotalShipped);
                Platform.SetStat("STAT_REVENUE", sim.TotalEarned);
                Platform.SetStat("STAT_DAYS", sim.Day);
            };
        }
    }

    /// <summary>Firma › Erfolge: alle Erfolge mit Fortschritt, geheime erst nach dem Freischalten.</summary>
    public sealed class AppAchievements : LaptopApp
    {
        public override string Lead => "Kleine und große Meilensteine deiner Firma. Manche sind geheim – die findest du nur durch Ausprobieren.";

        public override void Build()
        {
            var s = S;
            int total = AchievementData.All.Length, got = s.AchievementCount();
            var top = Columns(Root);
            Stat(top, "FREIGESCHALTET", got + " / " + total, got > 0 ? "good" : null);
            Stat(top, "QUOTE", Mathf.RoundToInt(got * 100f / Mathf.Max(1, total)) + " %");
            Stat(top, "SCHWIERIGKEIT", s.DifficultyInfo.Name);
            UIX.Bar(Root, got / (float)Mathf.Max(1, total), Theme.LaptopGood, 8f);

            var list = new List<AchievementDef>(AchievementData.All);
            // Freigeschaltete zuerst (neueste oben), dann nach Fortschritt.
            list.Sort((a, b) =>
            {
                bool ha = s.HasAchievement(a.Id), hb = s.HasAchievement(b.Id);
                if (ha != hb) return ha ? -1 : 1;
                if (ha) return s.Achievements[b.Id].CompareTo(s.Achievements[a.Id]);
                return s.AchievementProgress(b).CompareTo(s.AchievementProgress(a));
            });
            var grid = UIX.Wrap(Root);
            foreach (var a in list)
            {
                bool has = s.HasAchievement(a.Id);
                bool secret = a.Hidden && !has;
                var c = UIX.Card(grid, null, has ? "card-hi" : null);
                c.style.width = 300;
                var h = UIX.Row(c, 10f);
                string icon = secret ? "lock" : (Icons.Has(a.Icon) ? a.Icon : "trophy");
                UIX.Round(UIX.Swatch(h, has ? Theme.LaptopGood : Theme.LaptopAccent, 38f, Icons.Has(icon) ? icon : "star"), 12f);
                var v = Grow(UIX.Col(h, 0f));
                UIX.Text(v, secret ? "Geheimer Erfolg" : a.Name, "h3");
                UIX.Text(v, has ? "Freigeschaltet an Tag " + s.Achievements[a.Id] : (secret ? "???" : Mathf.RoundToInt(s.AchievementProgress(a) * 100f) + " %"), "small");
                P(c, secret ? "Wird verraten, sobald du ihn geschafft hast." : a.Desc);
                if (!has && !secret && a.Progress != null) UIX.Bar(c, s.AchievementProgress(a), Theme.LaptopTeal, 6f);
            }
        }
    }

    /// <summary>Firma › Statistik: Gesamtzahlen seit Spielbeginn.</summary>
    public sealed class AppStats : LaptopApp
    {
        public override string Lead => "Alles, was deine Firma seit dem ersten Paket geschafft hat.";

        public override void Build()
        {
            var s = S;
            var top = Columns(Root);
            Stat(top, "UMSATZ", Fmt.Money(s.TotalEarned), "good");
            Stat(top, "PAKETE", Fmt.Thousands(s.TotalShipped));
            Stat(top, "TAG", s.Day.ToString());
            Stat(top, "ERFOLGE", s.AchievementCount() + " / " + AchievementData.All.Length);
            var g = s.NextGoal() ?? s.EndlessGoal();
            if (g != null)
            {
                var gc = Card(Root, s.EndingSeen ? "Endlos-Modus · nächstes Ziel" : "Nächstes Ziel", g.Title);
                P(gc, g.Desc);
                UIX.Bar(gc, Mathf.Clamp01(s.GoalValue(g) / Mathf.Max(0.001f, g.Target)), Theme.LaptopAccent, 8f);
            }
            string group = null;
            VisualElement card = null;
            int row = 0;
            foreach (var l in s.StatsLines())
            {
                if (l[0] != group)
                {
                    group = l[0];
                    card = Card(Root, group);
                    row = 0;
                }
                var r = UIX.Row(card, 12f, "table-row");
                if (row++ == 0) r.AddToClassList("first");
                Grow(UIX.Text(r, l[1], "muted"));
                UIX.Text(r, l[2], "h3");
            }
        }
    }
}
