using System;
using DropshippingGame.Core;
using DropshippingGame.UI;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Demo-Version (z. B. für das Steam Next Fest): nur mit Scripting-Define <c>DS_DEMO</c> aktiv
    /// (Menü Dropshipping › Build › Windows (Demo) setzt es nur für diesen Build).
    /// Begrenzung: die ersten <see cref="DayLimit"/> Spieltage. Nach dem Tagesabschluss von Tag
    /// <see cref="DayLimit"/> geht es nicht weiter – stattdessen erscheint der Abschluss-Bildschirm mit
    /// „Auf die Wunschliste!“. Die Demo hat einen eigenen Produktnamen und damit einen eigenen Spielstand-Ordner.
    /// </summary>
    public static class DemoMode
    {
#if DS_DEMO
        public static readonly bool Enabled = true;
#else
        public static readonly bool Enabled = false;
#endif

        /// <summary>Spielbare Tage in der Demo.</summary>
        public const int DayLimit = 5;

        /// <summary>
        /// Steam-Seite des Vollspiels. Vor dem Next Fest die echte App-ID in <see cref="SteamIds.FullGame"/> eintragen.
        /// </summary>
        public static string StoreUrl => SteamIds.FullGame > 0
            ? "https://store.steampowered.com/app/" + SteamIds.FullGame + "/"
            : "https://store.steampowered.com/";

        /// <summary>true, wenn nach Tag <paramref name="day"/> Schluss ist (nur in der Demo).</summary>
        public static bool ReachedEnd(int day) => Enabled && day >= DayLimit;

        /// <summary>Wunschliste öffnen: bevorzugt im Steam-Client, sonst im Browser.</summary>
        public static void OpenStorePage()
        {
            try
            {
                if (SteamIds.FullGame > 0 && Platform.IsOnline) Application.OpenURL("steam://store/" + SteamIds.FullGame);
                else Application.OpenURL(StoreUrl);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Demo] Shop-Seite nicht geöffnet: " + e.Message);
            }
        }

        /// <summary>Abschluss-Bildschirm der Demo. Jedes Schließen führt ins Hauptmenü.</summary>
        public static void ShowEnd(ModalView modal, Sim sim, Action toMenu)
        {
            if (modal == null) { toMenu?.Invoke(); return; }
            Game.Sound("levelup");
            var body = modal.Open("Danke fürs Spielen der Demo!", DayLimit + " Tage Hustle – und das war erst der Anfang.", 580, new[]
            {
                new ModalButton("Auf die Wunschliste!", OpenStorePage, "accent", "heart") { KeepOpen = true },
                new ModalButton("Zum Hauptmenü", null, "ghost", "home"),
            }, true, "demo-end", toMenu, "trophy");
            if (sim != null)
                modal.Text(body, "Deine Bilanz: " + Fmt.Thousands(sim.TotalShipped) + " Pakete verschickt, " + Fmt.Money(sim.TotalEarned) +
                                 " umgesetzt, " + Fmt.Rating(sim.Reputation) + " Sterne.");
            modal.Text(body, "Im fertigen Spiel geht es weiter: Lagerhalle und Personal, Großaufträge, TikTok-Hypes, Börse und Krypto, " +
                             "Straßenfest, Shitstorms – bis zum Imperium mit 100.000 € Umsatz und darüber hinaus.");
            modal.Text(body, "Setz das Spiel auf deine Steam-Wunschliste – dann sagt Steam dir Bescheid, sobald es erscheint.", "muted");
        }
    }

    /// <summary>Steam-App-IDs an einer Stelle (0 = noch nicht vergeben). Siehe docs/RELEASE.md.</summary>
    public static class SteamIds
    {
        /// <summary>App-ID des Vollspiels (Steamworks › App-Verwaltung).</summary>
        public const uint FullGame = 0;
        /// <summary>App-ID der Demo (eigene App, im Steamworks-Backend unter „Demo“ anlegen).</summary>
        public const uint Demo = 0;
        /// <summary>Valves Testspiel „Spacewar“ – Rückfall für Tests, solange keine eigene ID existiert.</summary>
        public const uint TestSpacewar = 480;

        /// <summary>ID, mit der dieser Build an Steam meldet.</summary>
        public static uint Current
        {
            get
            {
                uint id = DemoMode.Enabled ? Demo : FullGame;
                return id > 0 ? id : TestSpacewar;
            }
        }
    }
}
