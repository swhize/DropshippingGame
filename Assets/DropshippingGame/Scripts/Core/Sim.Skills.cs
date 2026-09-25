using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>
    /// F5 Hustle-Skills: 1 Skillpunkt zum Start + 1 je Level-Aufstieg (+ Boni aus Ereignissen).
    /// Drei Äste (Logistik, Vertrieb, Marketing) à 4 Stufen; Stufe n braucht Stufe n-1 im selben Ast
    /// und ein Mindestlevel. Die Effekte werden hier als Faktoren bereitgestellt und in den Formeln
    /// abgefragt; Welt-Effekte (Lauftempo beim Tragen) liest die Welt über <see cref="CarrySpeedMult"/>.
    /// </summary>
    public sealed partial class Sim
    {
        public event Action<SkillDef> SkillLearned;
        /// <summary>Skills wurden gelernt oder zurückgesetzt.</summary>
        public event Action SkillsChanged;

        /// <summary>Gelernte Skills (IDs).</summary>
        public HashSet<string> Skills = new HashSet<string>();
        /// <summary>Zusätzliche Skillpunkte aus Ereignissen (Seminar ...).</summary>
        public int BonusSkillPoints;

        public bool HasSkill(string id) => Skills.Contains(id);

        public int SkillPointsTotal() => GameData.StartSkillPoints + Math.Max(0, Level - 1) + BonusSkillPoints;

        /// <summary>Noch nicht verteilte Skillpunkte.</summary>
        public int SkillPointsAvailable() => Math.Max(0, SkillPointsTotal() - Skills.Count);

        /// <summary>"learned", "level" (Level zu niedrig), "requires" (Vorstufe fehlt), "points" (kein Punkt frei), "available" oder "unknown".</summary>
        public string SkillState(string id)
        {
            var s = GameData.Skill(id);
            if (s == null) return "unknown";
            if (Skills.Contains(id)) return "learned";
            if (Level < s.Level) return "level";
            if (s.Tier > 1)
            {
                var prev = PreviousSkill(s);
                if (prev != null && !Skills.Contains(prev.Id)) return "requires";
            }
            if (SkillPointsAvailable() <= 0) return "points";
            return "available";
        }

        /// <summary>Die Vorstufe im selben Ast (oder null bei Stufe 1).</summary>
        public static SkillDef PreviousSkill(SkillDef s)
        {
            if (s == null || s.Tier <= 1) return null;
            foreach (var o in GameData.Skills)
                if (o.Branch == s.Branch && o.Tier == s.Tier - 1) return o;
            return null;
        }

        /// <summary>Skills eines Astes, nach Stufe sortiert.</summary>
        public static List<SkillDef> SkillsOfBranch(string branch)
        {
            var l = new List<SkillDef>();
            foreach (var s in GameData.Skills)
                if (s.Branch == branch) l.Add(s);
            l.Sort((a, b) => a.Tier.CompareTo(b.Tier));
            return l;
        }

        public bool LearnSkill(string id)
        {
            string st = SkillState(id);
            var s = GameData.Skill(id);
            switch (st)
            {
                case "available":
                    break;
                case "learned":
                    return false;
                case "level":
                    Notify("Dafür brauchst du Firmenlevel " + s.Level + ".", "bad");
                    return false;
                case "requires":
                    Notify("Lern zuerst „" + PreviousSkill(s).Name + "“.", "bad");
                    return false;
                case "points":
                    Notify("Kein Skillpunkt frei. Den nächsten gibt's beim Level-Aufstieg.", "bad");
                    return false;
                default:
                    return false;
            }
            Skills.Add(id);
            Notify("Skill gelernt: " + s.Name + "!", "good");
            Sound("levelup", 0.02f, -4f);
            SkillLearned?.Invoke(s);
            SkillsChanged?.Invoke();
            RaiseEconomyChanged();
            return true;
        }

        /// <summary>Kosten, um alle Skills zurückzusetzen ("Umschulung").</summary>
        public int RespecCost() => GameData.RespecCostPerLevel * Level;

        /// <summary>Setzt alle Skills gegen Geld zurück (Punkte werden wieder frei).</summary>
        public bool ResetSkills()
        {
            if (Skills.Count == 0) return false;
            int cost = RespecCost();
            if (Money < cost)
            {
                Notify("Die Umschulung kostet " + Fmt.Money(cost) + ". So viel hast du nicht.", "bad");
                Sound("error");
                return false;
            }
            Spend(cost, "other");
            Skills.Clear();
            Notify("Umschulung abgeschlossen – alle Skillpunkte sind wieder frei.", "info");
            SkillsChanged?.Invoke();
            RaiseEconomyChanged();
            return true;
        }

        public void AddSkillPoints(int n)
        {
            if (n == 0) return;
            BonusSkillPoints = Math.Max(0, BonusSkillPoints + n);
            SkillsChanged?.Invoke();
            RaiseEconomyChanged();
        }

        // =====================================================================================
        // Effekte (werden von den Formeln abgefragt)
        // =====================================================================================
        /// <summary>
        /// Welt-Flag: Faktor für das Lauftempo, während der Spieler etwas trägt. Ohne Skill bremst eine
        /// Kiste auf 85 %, mit "Starke Arme" gibt es keine Bremse und +10 % mit vollen Händen.
        /// </summary>
        public float CarrySpeedMult(ItemKind kind)
        {
            if (kind == ItemKind.None) return 1f;
            if (HasSkill("l_arme")) return 1.1f;
            return kind == ItemKind.Crate ? 0.85f : 1f;
        }

        /// <summary>Lagerplatz-Faktor ("Lager-Tetris").</summary>
        public float CapacityMult() => HasSkill("l_tetris") ? 1.3f : 1f;

        /// <summary>Lieferzeit-Faktor ("Kurze Wege").</summary>
        public float LeadTimeMult() => HasSkill("l_wege") ? 0.7f : 1f;

        /// <summary>Arbeitstempo des Personals ("Prozess-Flow").</summary>
        public float StaffSpeedMult() => HasSkill("l_flow") ? 1.25f : 1f;

        /// <summary>Laufzeit eines Pakets auf dem Förderband in Minuten.</summary>
        public float ConveyorMinutes() => HasSkill("l_flow") ? 3f : 6f;

        /// <summary>Einkaufspreis-Faktor ("Feilschen").</summary>
        public float PurchasePriceMult() => HasSkill("v_feilschen") ? 0.9f : 1f;

        /// <summary>Retourenquoten-Faktor ("Kundenflüsterer").</summary>
        public float ReturnRateMult() => HasSkill("v_kulanz") ? 0.65f : 1f;

        /// <summary>Vergütungsfaktor für Großaufträge ("Networking").</summary>
        public float ContractPayMult() => HasSkill("v_netzwerk") ? 1.2f : 1f;

        public int ContractSlotBonus() => HasSkill("v_netzwerk") ? 1 : 0;

        /// <summary>Zusätzliche Plätze in der Warteschlange ("Stammkundschaft").</summary>
        public int QueueBonus() => HasSkill("v_stamm") ? 3 : 0;

        /// <summary>Gewicht schlechter Bewertungen ("Stammkundschaft").</summary>
        public float BadReviewWeightMult() => HasSkill("v_stamm") ? 0.5f : 1f;

        /// <summary>Stärke/Dauer-Faktor für TikToks ("Content Creator").</summary>
        public float TikTokPowerMult() => HasSkill("m_content") ? 1.25f : 1f;

        /// <summary>Abklingzeit zwischen TikToks in Minuten ("Viral-Gen").</summary>
        public float TikTokCooldownMinutes() => GameData.TikTokCooldown * (HasSkill("m_viral") ? 0.6f : 1f);

        /// <summary>Kostenfaktor für Werbekampagnen ("Viral-Gen").</summary>
        public float AdCostMult() => HasSkill("m_viral") ? 0.75f : 1f;

        /// <summary>Tatsächliche Kosten einer Werbeform (mit Skill-Rabatt).</summary>
        public int AdCost(int tierIndex)
        {
            var t = GameData.AdTiers[Mathx.Clamp(tierIndex, 0, GameData.AdTiers.Length - 1)];
            return Math.Max(1, Mathx.RoundToInt(t.Cost * AdCostMult()));
        }

        /// <summary>Genauigkeit des Trendradars (0..1, "Trendradar").</summary>
        public float TrendForecastAccuracy() => HasSkill("m_radar") ? GameData.TrendAccuracySkill : GameData.TrendAccuracyBase;

        /// <summary>Wie viele Tage im Voraus der Trendradar einen neuen Hype erkennt.</summary>
        public int TrendSightDays() => HasSkill("m_radar") ? 3 : 1;

        /// <summary>"Trendsetter": gute TikToks lösen einen Hype aus.</summary>
        public bool TikTokStartsTrends => HasSkill("m_trendsetter");
    }
}
