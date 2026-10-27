using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Pausemenü › „Feedback &amp; Bug melden“: kopiert Version + Systeminfos in die Zwischenablage, öffnet den
    /// Log-Ordner und erklärt, wohin man den Bericht schickt. Es wird nichts automatisch hochgeladen.
    /// </summary>
    public static class BugReportView
    {
        public static void Build(VisualElement parent, Action onBack)
        {
            string text = BugReport.CopyToClipboard();
            // Streamer-Modus: keinen Explorer mit Benutzerpfad öffnen und keine Pfade/Systeminfos zeigen.
            bool streamer = Settings.StreamerMode;
            bool opened = !streamer && BugReport.OpenLogFolder();

            var head = UIX.Row(parent, 10f);
            UIX.Text(head, "Feedback & Bug melden", "h1");
            UIX.Spacer(head);
            UIX.Button(head, "Zurück", onBack, "ghost", false, "arrow_left");

            var card = UIX.Card(parent);
            var ch = UIX.Row(card, 8f);
            UIX.Icon(ch, "check", 16f, Theme.Good);
            UIX.Text(ch, "INFOS KOPIERT", "eyebrow");
            UIX.Text(card, "Version und Systeminfos liegen jetzt in deiner Zwischenablage" +
                           (opened ? ", der Ordner mit der Logdatei ist geöffnet." : (streamer ? ". (Streamer-Modus: Pfade werden nicht angezeigt.)" : ". Den Log-Ordner findest du hier:")), "help-step-text");
            if (!opened && !streamer) UIX.Text(card, BugReport.LogFolder, "mono", "small");

            string[] steps =
            {
                "Beschreibe kurz, <b>was du gemacht hast</b> und <b>was passiert ist</b> (bei Grafikfehlern gern mit Screenshot – F12).",
                "Füge die kopierten Infos ein (Strg + V).",
                "Hänge die Datei <b>Player.log</b> aus dem geöffneten Ordner an (bei Abstürzen auch <b>Player-prev.log</b>).",
                "Schick alles an: <b>" + BugReport.Contact + "</b>",
            };
            var how = UIX.Card(parent);
            for (int i = 0; i < steps.Length; i++)
            {
                var s = UIX.Div(how, "help-step");
                UIX.Text(s, (i + 1).ToString(), "help-step-num");
                UIX.Text(s, steps[i], "help-step-text");
            }

            if (!streamer)
            {
                var info = UIX.Card(parent);
                UIX.Text(info, "INHALT DER ZWISCHENABLAGE", "eyebrow");
                var pre = UIX.Text(info, (text ?? "").TrimEnd(), "mono", "small");
                pre.enableRichText = false;
                pre.style.whiteSpace = WhiteSpace.Normal;
            }

            var row = UIX.Row(parent, 8f);
            UIX.Button(row, "Erneut kopieren", () =>
            {
                BugReport.CopyToClipboard();
                Game.Notify("Infos in die Zwischenablage kopiert.", "info");
            }, "", false, "paper");
            UIX.Button(row, "Log-Ordner öffnen", () => BugReport.OpenLogFolder(), "", false, "inbox");
            UIX.Text(parent, "Datenschutz: Das Spiel sendet keine Daten. Du entscheidest selbst, was du teilst.", "small", "muted");
        }
    }
}
