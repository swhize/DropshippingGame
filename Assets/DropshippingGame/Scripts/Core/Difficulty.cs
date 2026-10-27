using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>Schwierigkeitsgrad (Wahl beim neuen Spiel, im Spielstand gespeichert).</summary>
    public sealed class DifficultyDef
    {
        public string Id, Name, Icon, Desc;
        /// <summary>Faktor auf die Nachfrage.</summary>
        public float DemandMult = 1f;
        /// <summary>Faktor auf Miete, Löhne und Einkaufspreise.</summary>
        public float CostMult = 1f;
        /// <summary>Gewicht schlechter Bewertungen (größer = strengere Kundschaft).</summary>
        public float ReviewHarshness = 1f;
        public int StartMoney = GameData.StartCapital;
        /// <summary>Wie oft Mama vor der Pleite rettet.</summary>
        public int MamaRescues = 1;
    }

    public static partial class GameData
    {
        public const string DefaultDifficulty = "normal";

        public static readonly DifficultyDef[] Difficulties =
        {
            new DifficultyDef
            {
                Id = "entspannt", Name = "Entspannt", Icon = "heart", DemandMult = 1.15f, CostMult = 0.85f, ReviewHarshness = 0.7f, StartMoney = 300, MamaRescues = 2,
                Desc = "Mehr Bestellungen, günstigere Kosten, nachsichtige Kundschaft. Mama springt zweimal ein.",
            },
            new DifficultyDef
            {
                Id = "normal", Name = "Normal", Icon = "star",
                Desc = "So ist das Spiel gedacht. Fehler kosten, aber Mama hilft einmal aus der Pleite.",
            },
            new DifficultyDef
            {
                Id = "hart", Name = "Hart", Icon = "fire", DemandMult = 0.88f, CostMult = 1.15f, ReviewHarshness = 1.35f, StartMoney = 100, MamaRescues = 0,
                Desc = "Weniger Kundschaft, höhere Kosten, strenge Bewertungen – und kein Notgroschen von Mama.",
            },
        };

        public static DifficultyDef Difficulty(string id)
        {
            foreach (var d in Difficulties)
                if (d.Id == id) return d;
            return Difficulties[1];
        }

        public static bool IsDifficulty(string id)
        {
            foreach (var d in Difficulties)
                if (d.Id == id) return true;
            return false;
        }
    }

    public sealed partial class Sim
    {
        /// <summary>Schwierigkeit dieses Spielstands ("entspannt", "normal", "hart").</summary>
        public string Difficulty = GameData.DefaultDifficulty;

        /// <summary>
        /// Wahl aus dem Menü für das nächste <see cref="NewGame"/> (bleibt über <see cref="ResetState"/> erhalten).
        /// </summary>
        public string NextDifficulty = GameData.DefaultDifficulty;

        public DifficultyDef DifficultyInfo => GameData.Difficulty(Difficulty);
        public float DifficultyDemandMult() => DifficultyInfo.DemandMult;
        public float DifficultyCostMult() => DifficultyInfo.CostMult;
        public float DifficultyReviewHarshness() => DifficultyInfo.ReviewHarshness;

        /// <summary>Aus <see cref="NewGame"/>: Schwierigkeit übernehmen und Startgeld setzen.</summary>
        private void ApplyDifficultyOnNewGame(string mode)
        {
            Difficulty = GameData.IsDifficulty(NextDifficulty) ? NextDifficulty : GameData.DefaultDifficulty;
            MamaRescuesLeft = DifficultyInfo.MamaRescues;
            if (mode != "intro") Money = DifficultyInfo.StartMoney;
        }

        /// <summary>Nach dem Imbiss-Intro: Startgeld je Schwierigkeit.</summary>
        public int StartMoneyForDifficulty() => DifficultyInfo.StartMoney;
    }
}
