using System;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Verbindet Spielereignisse aus <see cref="Sim"/> mit Feedback: Paket versendet (+X € mit Münzen,
    /// Bewertungs-Sterne + Kassen-Ding), Level-Up (Bildschirm-Konfetti, FOV-Kick), Perk/Skill (goldene Funken),
    /// Ziel/Wochenziel/Großauftrag (Konfetti), Rekordtag in der Tagesabrechnung (Konfetti-Regen + Stempel),
    /// Kasse in den Läden (Münzregen), Verkaufsstand-Verkauf. Dazu Einstiege für UI-Aktionen (TikTok gepostet,
    /// Artikel in den Einkaufswagen). Ein Haken in GameRoot.HookSim: <c>FeelDirector.Attach(sim)</c>.
    /// </summary>
    public static class FeelDirector
    {
        private static Sim _sim;
        private static int _reviews, _perks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _sim = null;
            _reviews = 0;
            _perks = 0;
        }

        /// <summary>True, solange das Feedback-System Ereignisse verarbeitet (sonst greifen alte Fallbacks).</summary>
        public static bool Active => _sim != null && Feel.Enabled;

        public static void Attach(Sim sim)
        {
            try
            {
                Detach();
                _sim = sim;
                if (sim == null) return;
                FeelRunner.Ensure();
                _reviews = sim.ReviewCount;
                _perks = PerkSum(sim);
                sim.PackageShipped += OnShipped;
                sim.LevelUp += OnLevelUp;
                sim.DayEnded += OnDayEnded;
                sim.ProgressChanged += OnProgress;
                sim.SoundRequested += OnSound;
                sim.SkillLearned += OnSkill;
                sim.GoalCompleted += OnGoal;
                sim.ChallengeCompleted += OnChallenge;
                sim.ContractCompleted += OnContract;
                sim.StandSale += OnStandSale;
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
        }

        public static void Detach()
        {
            var s = _sim;
            _sim = null;
            if (s == null) return;
            s.PackageShipped -= OnShipped;
            s.LevelUp -= OnLevelUp;
            s.DayEnded -= OnDayEnded;
            s.ProgressChanged -= OnProgress;
            s.SoundRequested -= OnSound;
            s.SkillLearned -= OnSkill;
            s.GoalCompleted -= OnGoal;
            s.ChallengeCompleted -= OnChallenge;
            s.ContractCompleted -= OnContract;
            s.StandSale -= OnStandSale;
        }

        private static int PerkSum(Sim s)
        {
            int n = 0;
            if (s?.PerkRanks == null) return 0;
            foreach (var kv in s.PerkRanks) n += kv.Value;
            return n;
        }

        // ---- Versand & Bewertung ---------------------------------------------------------------------
        private static void OnShipped(ItemData pkg, int reward, V3 where)
        {
            try
            {
                var s = _sim;
                if (s == null) return;
                var pos = where.IsZero ? Feel.InFrontOfCamera(1.6f, 0f) : where.ToVector3() + Vector3.up * 0.4f;
                FeelPops.MoneyPop(pos, reward);
                if (s.ReviewCount < _reviews) _reviews = s.ReviewCount;
                if (s.ReviewCount > _reviews && s.Reviews != null && s.Reviews.Count > 0)
                {
                    _reviews = s.ReviewCount;
                    int stars = Mathf.Clamp(s.Reviews[0].Stars, 1, 5);
                    var at = pos + Vector3.up * 0.75f;
                    FeelTween.Call(0.45f, () =>
                    {
                        FeelFx.Stars(at, stars, stars >= 3);
                        if (stars >= 4)
                        {
                            FeelFx.Sparkle(at, 8);
                            FeelAudio.Play("ding", 1f, 0.04f, -3f);
                        }
                        else if (stars <= 2) FeelAudio.Play("bad", 1f, 0.04f, -10f);
                        else FeelAudio.Play("pop", 1.1f, 0.04f, -6f);
                    });
                }
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
        }

        private static void OnStandSale(string pid, int price)
        {
            FeelFx.Coins(WorldBuilder.StandPos + new Vector3(0f, 1.6f, 0f), 4, 0.8f);
            FeelAudio.Play("coin", FeelAudio.Ladder(), 0.04f, -8f, 0f, 0.08f);
        }

        // ---- Fortschritt -----------------------------------------------------------------------------
        private static void OnLevelUp(int level)
        {
            UiConfetti.Burst(new Vector2(0.5f, 0.42f), 60, null, 1.1f);
            UiConfetti.Burst(new Vector2(0.12f, 0.95f), 22, null, 1f, 0.12f);
            UiConfetti.Burst(new Vector2(0.88f, 0.95f), 22, null, 1f, 0.12f);
            CameraFeel.Kick(3f);
            FeelFx.Sparkle(Feel.InFrontOfCamera(1.6f, 0.1f), 14, null, 1.2f);
            FeelAudio.Play("popper", 1f, 0.05f, -2f);
        }

        private static void OnProgress()
        {
            var s = _sim;
            if (s == null) return;
            int sum = PerkSum(s);
            bool bought = sum > _perks;
            _perks = sum;
            // Nur echte Käufe feiern (Laptop/Handy offen), nicht das Laden eines Spielstands.
            bool viaUi = s.PcOpen || (Game.UI != null && Game.UI.Phone != null && Game.UI.Phone.IsOpen);
            if (!bought || !viaUi) return;
            UiConfetti.Burst(new Vector2(0.5f, 0.5f), 34, UiConfetti.GoldSet, 0.8f);
            FeelAudio.Play("perk", 1f, 0.03f, -4f);
        }

        private static void OnSkill(SkillDef sk)
        {
            UiConfetti.Burst(new Vector2(0.5f, 0.5f), 30, UiConfetti.GoldSet, 0.8f);
            FeelAudio.Play("perk", 1.06f, 0.03f, -4f);
        }

        private static void OnGoal(GoalDef g) => SmallCelebration();
        private static void OnChallenge(WeeklyChallenge c) => SmallCelebration();
        private static void OnContract(Contract c) => SmallCelebration();

        private static void SmallCelebration()
        {
            UiConfetti.Burst(new Vector2(0.5f, 0.2f), 36, null, 0.8f);
            FeelAudio.Play("popper", 1.1f, 0.05f, -6f);
        }

        // ---- Tagesabrechnung --------------------------------------------------------------------------
        private static void OnDayEnded(DaySummary d)
        {
            try
            {
                var s = _sim;
                if (s == null || d == null || d.Bankrupt) return;
                var hist = s.History;
                if (hist == null || hist.Count == 0) return;
                int bestProfit = int.MinValue, bestShipped = 0;
                foreach (var h in hist)
                {
                    if (h == null) continue;
                    bestProfit = Math.Max(bestProfit, h.Profit);
                    bestShipped = Math.Max(bestShipped, h.Shipped);
                }
                string badge = null;
                if (d.Profit > 0 && d.Profit > bestProfit) badge = "NEUER REKORD: " + Fmt.SignedMoney(d.Profit);
                else if (d.Shipped >= 3 && d.Shipped > bestShipped) badge = "REKORD: " + d.Shipped + " PAKETE";
                if (badge == null) return;
                // Nach dem Hochzählen der Bon-Zeilen (Echtzeit – die Abrechnung pausiert das Spiel).
                string text = badge;
                Anim.Delay(1.5f, () => Feel.Safe(() =>
                {
                    UiConfetti.Rain(90);
                    UiConfetti.Burst(new Vector2(0.08f, 1f), 26, null, 1.2f);
                    UiConfetti.Burst(new Vector2(0.92f, 1f), 26, null, 1.2f);
                    UiConfetti.Badge(text, new Color(1f, 0.84f, 0.3f), 0.1f, 2.8f);
                    FeelAudio.Play("popper", 1f, 0.05f, -2f);
                    FeelAudio.Play("perk", 1f, 0.02f, -4f, 0f, 0f);
                }), true);
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
        }

        // ---- Sounds aus der Simulation als Signal -------------------------------------------------------
        private static void OnSound(string name, float pitch, float vol)
        {
            if (name != "register") return;
            // Kasse in MediaMarkd/Fressnix: Münzregen + Funken über dem Kassenbon
            FeelFx.Coins(Feel.InFrontOfCamera(1.1f, 0.25f), 10, 0.8f);
            UiConfetti.Burst(new Vector2(0.5f, 0.62f), 18, UiConfetti.GoldSet, 0.6f);
            FeelAudio.Play("coin_cascade", 1f, 0.05f, -4f, 0f);
            CameraFeel.Kick(1f);
        }

        // ---- Einstiege aus der Oberfläche ---------------------------------------------------------------
        /// <summary>TikTok-Video gepostet: Herzen-Konfetti und Plopp-Kaskade.</summary>
        public static void TikTokPosted()
        {
            Feel.Safe(() =>
            {
                UiConfetti.Burst(new Vector2(0.5f, 0.78f), 46, UiConfetti.Hearts, 1.1f);
                UiConfetti.Burst(new Vector2(0.3f, 0.85f), 16, UiConfetti.Hearts, 0.9f, 0.18f);
                UiConfetti.Burst(new Vector2(0.7f, 0.85f), 16, UiConfetti.Hearts, 0.9f, 0.3f);
                for (int i = 0; i < 4; i++)
                {
                    float pitch = Mathf.Pow(2f, (i * 3) / 12f);
                    Anim.Delay(0.09f * i, () => FeelAudio.Play("pop", pitch, 0.02f, -6f, 0f, 0f), true);
                }
                FeelAudio.Play("ding", 1.2f, 0.02f, -6f);
            });
        }

        /// <summary>Artikel in den Einkaufswagen/Korb (Scanner-Piep, Funkeln, Preis-Pop).</summary>
        public static void CartAdded(Component bay, int price, bool electro)
        {
            if (bay == null) return;
            Feel.Safe(() =>
            {
                var col = bay.GetComponent<Collider>();
                var pos = col != null ? col.bounds.center : bay.transform.position + Vector3.up;
                FeelFx.Sparkle(pos, 6, Color.white, 0.6f);
                FeelPops.Spawn(pos + Vector3.up * 0.3f, "+1 · " + Fmt.Money(price), FeelPops.Info, 0.7f);
                FeelAudio.Play(electro ? "scanner" : "pop", electro ? 1f : FeelAudio.Ladder(), 0.03f, -6f);
            });
        }
    }
}
