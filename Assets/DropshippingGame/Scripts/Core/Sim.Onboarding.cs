using System;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Onboarding der ersten Spielstunden: nach dem Tutorial erklärt Marvin neue Systeme nach und
    /// nach per Nachricht (<see cref="GameData.OnboardingTips"/>), höchstens eine alle
    /// <see cref="GameData.OnboardingGapMinutes"/> Geschäftsminuten – nie während des Tutorials.
    /// Außerdem der „Geschafft!“-Moment für das allererste verschickte Paket.
    /// Haken: <see cref="AdvanceMinutes"/> → <see cref="OnboardingUpdate"/>, TutorialCheck → <see cref="OnboardingAfterAction"/>.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Das allererste Paket dieses Spielstands ist raus (große Feier in der Oberfläche).</summary>
        public event Action FirstParcelShipped;

        private float _lastTipAt = -1000f;
        private bool _tipClockSet;
        private float _nextTipCheck;

        /// <summary>Wird die Onboarding-Nachricht <paramref name="key"/> gerade fällig? (öffentlich für Tests)</summary>
        public bool OnboardingDue(string key)
        {
            switch (key)
            {
                case "post": return true;
                case "phone": return true;
                case "dayend": return Day == 1 ? TimeMinutes >= GameData.DayEnd - 150f : Day >= 2;
                case "energy": return Energy < GameData.EnergyTiredBelow + 15f;
                case "waste": return WasteFill >= 0.5f;
                case "porto": return TotalShipped >= 5;
                case "city": return Day >= 2;
                case "garage": return Day >= 2 && TotalShipped >= 3;
                case "money_low": return Day >= 2 && Money < 40;
                case "tiktok": return Level >= GameData.TikTokLevel;
                case "ads": return FakebookUnlocked;
                case "finance": return Level >= GameData.TradingLevel;
                case "warehouse": return Level >= 4 && LocationStage == 0;
                case "staff": return StaffAllowed();
            }
            return false;
        }

        /// <summary>Darf gerade überhaupt eine Onboarding-Nachricht kommen?</summary>
        public bool OnboardingActive => StoryStage == "business" && TutorialStep < 0 && !DayOver;

        /// <summary>Nächste fällige, noch nicht erklärte Nachricht (oder null).</summary>
        public OnboardingTip NextOnboardingTip()
        {
            foreach (var t in GameData.OnboardingTips)
            {
                if (t == null || Explained.Contains(t.Key)) continue;
                bool due;
                try
                {
                    due = OnboardingDue(t.Key);
                }
                catch (Exception)
                {
                    due = false;
                }
                if (due) return t;
            }
            return null;
        }

        /// <summary>Schickt die nächste fällige Nachricht, wenn der Abstand eingehalten ist. true = gesendet.</summary>
        public bool OnboardingStep(bool ignoreGap = false)
        {
            if (!OnboardingActive) return false;
            float now = BClock();
            // Neues Spiel / geladener Stand: erste Nachricht frühestens nach ein paar Minuten (nicht beim Laden).
            if (!_tipClockSet || _lastTipAt > now)
            {
                _tipClockSet = true;
                _lastTipAt = now - GameData.OnboardingGapMinutes + 10f;
            }
            if (!ignoreGap && now - _lastTipAt < GameData.OnboardingGapMinutes) return false;
            var tip = NextOnboardingTip();
            if (tip == null) return false;
            if (!Explain(tip.Key, new[] { tip.Title, GameData.FillKeys(tip.Text) }, tip.Icon)) return false;
            _lastTipAt = now;
            return true;
        }

        /// <summary>Haken aus <see cref="AdvanceMinutes"/>: alle paar Minuten prüfen.</summary>
        private void OnboardingUpdate()
        {
            float now = BClock();
            if (_nextTipCheck - now > 10f) _nextTipCheck = now; // neues Spiel / geladener Stand
            if (now < _nextTipCheck) return;
            _nextTipCheck = now + 5f;
            OnboardingStep();
        }

        /// <summary>Haken aus TutorialCheck (nach jeder wichtigen Handlung).</summary>
        private void OnboardingAfterAction()
        {
            if (TotalShipped == 1 && StoryStage == "business" && !Explained.Contains("first_parcel"))
            {
                Explained.Add("first_parcel");
                Sound("levelup");
                FirstParcelShipped?.Invoke();
            }
        }

        /// <summary>Tutorial beendet: Wochenziele und Versand-Erklärung, danach geht's im Takt weiter.</summary>
        private void OnboardingTutorialFinished()
        {
            Explain("week", GameData.MailWeek, "trophy");
            OnboardingStep(true);
        }

        /// <summary>Produkt für die garantierte Tutorial-Bestellung: online und auf Lager (sonst Handyhülle).</summary>
        private string TutorialOrderProduct()
        {
            foreach (var p in GameData.Products)
                if (IsListed(p.Id) && StockQty(p.Id) > 0)
                    return p.Id;
            return "huelle";
        }

        /// <summary>Ist mindestens ein Produkt online, das auch im Regal liegt?</summary>
        public bool AnyListedInStock()
        {
            foreach (var p in GameData.Products)
                if (IsListed(p.Id) && StockQty(p.Id) > 0)
                    return true;
            return false;
        }
    }
}
