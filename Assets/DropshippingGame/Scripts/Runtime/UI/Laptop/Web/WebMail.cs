using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// iWolke Mail (Comic, iCloud-artig): Ordner, Mail-Liste und Lesebereich als Sprechblase.
    /// Das bestehende Ereignis-/Mailsystem, Entscheidungen als Buttons.
    /// Routen: "" (Posteingang) · "pending" (Entscheidungen) · "unread" (Ungelesen).
    /// </summary>
    public sealed class WebMail : WebApp
    {
        public override string AppId => "mail";
        public override string SkinClass => "iw";
        public override WebSkin Skin => WebSkin.Comic;
        protected override string Domain => "iwolke.de/mail";

        private static int _selected = -1;

        public override void Build()
        {
            var s = S;
            if (s == null) return;
            var ev = s.Events;
            string folder = Route ?? "";

            var iw = Row(Root, 0f, "iw-wrap");
            iw.style.alignItems = Align.Stretch;

            // Ordner
            var f = Wd(Col(iw, 4f, "iw-folders"), 190f);
            H(f, "iWolke", "iw-logo");
            Folder(f, "Posteingang", ev.Mails.Count.ToString(), "", folder);
            Folder(f, "Entscheidungen", ev.PendingCount() > 0 ? ev.PendingCount().ToString() : "", "pending", folder);
            Folder(f, "Ungelesen", ev.UnreadCount() > 0 ? ev.UnreadCount().ToString() : "", "unread", folder);
            T(f, "", "iw-gap");
            FakeFolder(f, "Lieferanten", "");
            FakeFolder(f, "Gesendet", "");
            FakeFolder(f, "Werbung", (38 + s.Day).ToString());
            

            // Liste
            var ls = Wd(Col(iw, 0f, "iw-list"), 320f);
            int shown = 0;
            Mail firstVisible = null;
            foreach (var m in ev.Mails)
            {
                if (!Matches(m, folder)) continue;
                if (firstVisible == null) firstVisible = m;
                if (++shown > 30) break;
            }
            if (_selected == -1 || ev.Find(_selected) == null || !Matches(ev.Find(_selected), folder))
            {
                _selected = firstVisible != null ? firstVisible.Id : -1;
                if (folder == "")
                    foreach (var m in ev.Mails)
                    {
                        if (!m.Pending) continue;
                        _selected = m.Id;
                        break;
                    }
            }
            shown = 0;
            foreach (var m in ev.Mails)
            {
                if (!Matches(m, folder)) continue;
                if (++shown > 30) break;
                int id = m.Id;
                var item = Press(ls, () =>
                {
                    _selected = id;
                    Rebuild();
                }, "iw-item");
                item.EnableInClassList("on", m.Id == _selected);
                item.EnableInClassList("unread", !m.Read || m.Pending);
                var top = Row(item, 6f);
                if (!m.Read || m.Pending) Div(top, "iw-dot");
                Flex(B(top, m.Sender ?? "", "iw-from"));
                T(top, "Tag " + m.Day, "iw-time");
                B(item, (m.Pending ? "[!] " : "") + (m.Title ?? ""), "iw-subject");
                T(item, Preview(m.Text), "iw-preview");
                UIX.PassThrough(item);
            }
            if (shown == 0)
            {
                var e = Col(ls, 6f, "iw-empty");
                H(e, "Leer!", "iw-empty-title");
                
            }

            // Lesebereich
            var rd = Flex(Col(iw, 10f, "iw-read"));
            var mail = _selected >= 0 ? ev.Find(_selected) : null;
            if (mail == null)
            {
                H(rd, "Keine Mail ausgewählt", "iw-subject-big");
                
                return;
            }
            ev.MarkRead(mail.Id);
            H(rd, mail.Title ?? "", "iw-subject-big");
            var who = Row(rd, 10f, "iw-who");
            var av = Div(who, "iw-av");
            av.style.backgroundColor = W.Hex(AvatarColor(mail.Sender));
            H(av, string.IsNullOrEmpty(mail.Sender) ? "?" : mail.Sender.Substring(0, 1).ToUpperInvariant(), "iw-av-text");
            var wv = Col(who, 1f);
            B(wv, mail.Sender ?? "", "iw-text");
            T(wv, "an: info@" + W.Slug(s.BrandName) + ".de · Tag " + mail.Day + ", " + Fmt.Clock(mail.Time) + " Uhr", "iw-small");

            var bubble = Col(rd, 6f, "iw-bubble");
            T(bubble, mail.Text ?? "", "iw-text");

            if (mail.Pending)
            {
                B(rd, "Deine Entscheidung:", "iw-text");
                var actions = Div(rd, "iw-actions");
                for (int i = 0; i < mail.Choices.Count; i++)
                {
                    var c = mail.Choices[i];
                    int ci = i;
                    int mailId = mail.Id;
                    string txt = c.Label + (c.Cost > 0 ? " · " + Fmt.Money(c.Cost) : "") + (!string.IsNullOrEmpty(c.Minigame) ? " · Minispiel" : "");
                    BtnIf(actions, c.Cost <= s.Money, txt, () =>
                    {
                        S.Events.Choose(mailId, ci);
                        Rebuild();
                    }, "iw-btn", i == 0 ? "main" : "");
                }
                T(rd, "20 Uhr: automatisch die vorsichtigste Antwort.", "iw-small");
            }
            else if (!string.IsNullOrEmpty(mail.Result))
            {
                var res = Col(rd, 4f, "iw-result");
                H(res, "ERGEBNIS", "iw-result-title");
                if (!string.IsNullOrEmpty(mail.Chosen)) T(res, "Du hast gewählt: " + mail.Chosen, "iw-small");
                T(res, mail.Result, "iw-text");
            }
            else
            {
                var actions = Div(rd, "iw-actions");
                Btn(actions, "Antworten", () => Toast("Antwort geschrieben, gelöscht, neu geschrieben, nicht gesendet. Wie immer."), "iw-btn");
                Btn(actions, "Weiterleiten", () => Toast("An wen denn? Du hast keine Kollegen. Noch."), "iw-btn");
            }
        }

        private void Folder(VisualElement parent, string name, string count, string route, string current)
        {
            var b = Press(parent, () => Nav(route), "iw-folder");
            b.EnableInClassList("on", current == route);
            var r = Row(b, 6f);
            Flex(B(r, name, "iw-folder-text"));
            B(r, count, "iw-folder-count");
            UIX.PassThrough(b);
        }

        private void FakeFolder(VisualElement parent, string name, string count)
        {
            var b = Press(parent, () => Toast(name == "Werbung" ? "38 Mails von „Mega Vision Official“. Alle mit „Dear friend!!!“." : "Leer. Wie der Kühlschrank."), "iw-folder", "fake");
            var r = Row(b, 6f);
            Flex(B(r, name, "iw-folder-text"));
            B(r, count, "iw-folder-count");
            UIX.PassThrough(b);
        }

        private static bool Matches(Mail m, string folder)
        {
            if (m == null) return false;
            if (folder == "pending") return m.Pending;
            if (folder == "unread") return !m.Read || m.Pending;
            return true;
        }

        private static string Preview(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string t = text.Replace("\n", " ").Replace("<b>", "").Replace("</b>", "");
            return t.Length > 70 ? t.Substring(0, 70) + " …" : t;
        }

        private static string AvatarColor(string sender)
        {
            string[] cols = { "#D9E8FF", "#FFE0C8", "#D8F5C8", "#FFD6F2", "#FFF3B0", "#E3E3FF" };
            return cols[W.Hash(sender ?? "") % cols.Length];
        }
    }
}
