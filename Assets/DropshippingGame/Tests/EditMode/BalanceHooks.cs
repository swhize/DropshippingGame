using System;
using DropshippingGame.Core;

namespace DropshippingGame.Tests
{
    /// <summary>Bindet Schwierigkeit und Meta-Ereignisse an den Balance-Bot.</summary>
    internal static class DifficultyHook
    {
        public static void Apply(Sim gm, string difficulty) => gm.NextDifficulty = difficulty;

        public static void Subscribe(Sim gm, Action reward, Action rescue)
        {
            gm.AchievementUnlocked += _ => reward();
            gm.MamaRescued += _ => rescue();
        }
    }

    /// <summary>Exploit-Varianten: ein schlichter Durchschnittsspieler plus genau ein Trick.</summary>
    public static class ExploitRunner
    {
        public static readonly string[] Names =
        {
            "basis", "ads_stack", "fakes", "crypto", "festival_max", "mediamarkd", "loan_invest", "tiktok_spam", "price_gouge",
        };

        public static BalanceBot Make(string name, int seed)
        {
            var b = new BalanceBot(PlayerType.Average, seed);
            b.FakebookPerDay = 0;
            b.GugelPerDay = 0;
            b.FakeSitesWanted = 0;
            b.InvestAsset = "";
            b.TikToksPerDay = 1;
            b.FestivalStock = 40;
            switch (name)
            {
                case "ads_stack":
                    b.OldAds = true;
                    b.FakebookPerDay = 3;
                    b.FakebookBudget = 100;
                    b.GugelPerDay = 3;
                    b.GugelBudget = 120;
                    b.PriceCap = 2.0f;
                    break;
                case "fakes":
                    b.FakeProfilesWanted = GameData.MaxFakeProfiles;
                    b.FakeSitesWanted = GameData.MaxFakeSites;
                    b.PriceCap = 2.0f;
                    break;
                case "crypto":
                    b.InvestAsset = "MOON";
                    b.InvestShare = 0.8f;
                    break;
                case "festival_max":
                    b.FestivalStock = GameData.FestivalCapacity;
                    break;
                case "mediamarkd":
                    b.MediaMarkdArbitrage = true;
                    break;
                case "loan_invest":
                    b.LoanToInvest = true;
                    b.InvestAsset = "KART";
                    break;
                case "tiktok_spam":
                    b.TikToksPerDay = GameData.TikTokPerDay;
                    b.TikTokQuality = 0.25f;
                    b.PriceCap = 1.5f;
                    break;
                case "price_gouge":
                    b.PriceCap = 2.5f;
                    break;
            }
            return b;
        }
    }
}
