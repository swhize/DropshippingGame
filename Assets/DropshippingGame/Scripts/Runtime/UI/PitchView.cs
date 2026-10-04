using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Pitch-Day-Minispiel im Fenster: drei Juroren, drei Fragen. Jede Antwort stützt sich auf
    /// eine echte Kennzahl deiner Firma – wer seine Stärken kennt, holt den Großauftrag.
    /// </summary>
    public static class PitchView
    {
        public static void Run(ModalView modal, Sim sim, Mail mail, Action done)
        {
            if (modal == null || sim == null || mail == null) return;
            var game = new PitchGame(sim);
            var body = modal.Open("Pitch Day bei MegaMarkt", "Drei Juroren. Drei Fragen. Eine Chance.", 640, new[]
            {
                new ModalButton("Los geht's", () => Question(modal, sim, mail, game, done), "accent", "play"),
            }, true, "pitch", null, "mic");
            modal.Text(body, "Du stehst auf der Bühne, das Mikro knackt. Wähle Antworten, die zu deinen echten Zahlen passen – " +
                             "die Jury durchschaut leere Versprechen sofort.");
            var hint = UIX.Card(body);
            UIX.Text(hint, "DEINE STÄRKEN", "eyebrow");
            Strength(hint, "Bewertung", game.StatValue("rating"));
            Strength(hint, "Verschickte Pakete", game.StatValue("shipped"));
            Strength(hint, "Marke & Bekanntheit", game.StatValue("brand"));
            Strength(hint, "Lager & Team", game.StatValue("company"));
            Strength(hint, "Preise", game.StatValue("price"));
        }

        private static Color Tone(float value) => value >= 0.6f ? Theme.Good : (value >= 0.35f ? Theme.Accent : Theme.Bad);

        private static void Strength(VisualElement parent, string label, float value)
        {
            var r = UIX.Row(parent, 10f);
            var l = UIX.Text(r, label, "muted");
            l.style.width = 180;
            UIX.Bar(r, value, Tone(value), 7f);
            var p = UIX.Num(r, UiFmt.Percent(value), false, "small");
            p.style.width = 44;
            p.style.unityTextAlign = TextAnchor.MiddleRight;
        }

        private static void Question(ModalView modal, Sim sim, Mail mail, PitchGame game, Action done)
        {
            if (game.Finished)
            {
                Finish(modal, sim, mail, game, done);
                return;
            }
            var q = game.Questions[game.Round];
            var body = modal.Open("Frage " + (game.Round + 1) + " von " + game.Questions.Count, q.Juror, 640, null, true, "pitch", null, "user");
            var quote = UIX.Card(body, null, "card-hi");
            UIX.Text(quote, "„" + q.Text + "“", "h3");
            UIX.Text(body, "DEINE ANTWORT", "eyebrow").style.marginTop = 6;
            for (int i = 0; i < q.Answers.Length; i++)
            {
                var a = q.Answers[i];
                int idx = i;
                var b = UIX.PressCol(body, 2f, () =>
                {
                    string reaction = game.Choose(idx);
                    Reaction(modal, sim, mail, game, reaction, done);
                }, "btn", "btn-left");
                b.style.alignItems = Align.FlexStart;
                b.style.paddingTop = 10;
                b.style.paddingBottom = 10;
                UIX.Text(b, a.Text, "btn-label");
                UIX.Text(b, "stützt sich auf: " + a.StatLabel, "small");
                UIX.PassThrough(b);
            }
        }

        private static void Reaction(ModalView modal, Sim sim, Mail mail, PitchGame game, string reaction, Action done)
        {
            float score = game.Scores.Count > 0 ? game.Scores[game.Scores.Count - 1] : 0f;
            Game.Sound(score >= 0.6f ? "notify" : (score >= 0.35f ? "click" : "bad"), 0.02f, -4f);
            var body = modal.Open("Die Jury reagiert", null, 580, new[]
            {
                new ModalButton(game.Finished ? "Zum Ergebnis" : "Nächste Frage", () => Question(modal, sim, mail, game, done), "accent", "arrow_right"),
            }, true, "pitch", null, score >= 0.6f ? "thumb" : (score >= 0.35f ? "chat" : "angry"));
            modal.Text(body, reaction);
            var r = UIX.Row(body, 10f);
            UIX.Text(r, "Eindruck", "muted").style.width = 90;
            UIX.Bar(r, score, Tone(score), 9f);
            UIX.Num(r, UiFmt.Percent(score), true);
        }

        private static void Finish(ModalView modal, Sim sim, Mail mail, PitchGame game, Action done)
        {
            var res = game.Evaluate();
            sim.Events.ResolveMinigame(mail.Id, "Pitch gehalten", res.Title + " " + res.Text, res.Effects);
            sim.RaiseEconomyChanged();
            Game.Sound(res.Money > 0 ? "levelup" : "bad", 0f, -4f);
            var body = modal.Open(res.Title, "Gesamteindruck: " + Mathf.RoundToInt(res.Score * 100f) + " %", 580, new[]
            {
                new ModalButton("Super", null, "accent", "check"),
            }, true, "pitch", done, res.Money > 0 ? "trophy" : "trend_down");
            modal.Text(body, res.Text);
            if (res.Money > 0) UIX.KV(body, "Auftragswert", Fmt.Money(res.Money), Theme.Good);
        }
    }
}
