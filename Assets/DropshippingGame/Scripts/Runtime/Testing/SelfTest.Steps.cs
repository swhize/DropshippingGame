using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DropshippingGame.Core;
using DropshippingGame.UI;
using DropshippingGame.Vehicles;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.Testing
{
    /// <summary>Die einzelnen Prüfschritte des Selbsttests (Reihenfolge = Ablauf).</summary>
    public sealed partial class SelfTest
    {
        private static StepDef S(string id, string title, float timeout, bool needsGame, Func<SelfTest, IEnumerator> body) =>
            new StepDef { Id = id, Title = title, Timeout = timeout, NeedsGame = needsGame, Body = body };

        private static readonly StepDef[] Steps =
        {
            S("menu", "Hauptmenü bereit", 40f, false, t => t.StepMenu()),
            S("newgame", "Neues Spiel (Profi-Start, Test-Spielstand)", 60f, false, t => t.StepNewGame()),
            S("coreloop", "Kernschleife: Einkauf, Kiste, Regal, Bestellung, Packtisch, Label, Packstation", 60f, true, t => t.StepCoreLoop()),
            S("laptop", "Laptop: alle Apps und Reiter (Spielbeginn)", 90f, true, t => t.StepLaptop(false)),
            S("menus", "Pausemenü, Einstellungen (alle Reiter), Hilfe", 60f, true, t => t.StepMenus()),
            S("unlock", "Alles freischalten (Lagerhalle, Max-Level) über das Admin-Panel", 40f, true, t => t.StepUnlock()),
            S("laptop_full", "Laptop: alle Apps und Reiter (alles freigeschaltet, mit Screenshots)", 150f, true, t => t.StepLaptop(true)),
            S("phone", "Handy: alle Apps (alles freigeschaltet)", 45f, true, t => t.StepPhone()),
            S("moped", "Moped: aufsteigen, 3 s fahren, absteigen", 45f, true, t => t.StepMoped()),
            S("mover", "Möbel verschieben: Modus an und aus", 30f, true, t => t.StepMover()),
            S("emote", "Emote-Rad öffnen und schließen", 20f, true, t => t.StepEmote()),
            S("tiktok", "TikTok-Aufnahme starten und abbrechen", 30f, true, t => t.StepTikTok()),
            S("birdseye", "Vogelperspektive an und aus", 20f, true, t => t.StepBirdsEye()),
            S("noclip", "Noclip an und aus", 20f, true, t => t.StepNoclip()),
            S("districts", "Stadtviertel besuchen (inkl. Straßenfest)", 150f, true, t => t.StepDistricts()),
            S("weather", "Wetter: Regen und klar", 30f, true, t => t.StepWeather()),
            S("daytime", "Tageszeiten: Morgen, Mittag, Goldene Stunde, Nacht", 30f, true, t => t.StepDaytime()),
            S("language", "Sprache Englisch: Oberfläche durchgehen, fehlende Übersetzungen", 150f, true, t => t.StepLanguage()),
            S("saveload", "Speichern und Laden (Test-Spielstand)", 60f, true, t => t.StepSaveLoad()),
            S("dayend", "Feierabend, Tagesabrechnung, nächster Tag", 60f, true, t => t.StepDayEnd()),
            S("perf", "Leistung: 20 s Spaziergang durch die Stadt", 60f, true, t => t.StepPerf()),
            S("finish", "Zurück ins Hauptmenü", 45f, false, t => t.StepFinish()),
        };

        private const string TestProduct = "huelle";

        // =====================================================================================
        // Helfer: Warten, Prüfen, Eingaben, Screenshots, Messen
        // =====================================================================================
        private static IEnumerator Wait(float seconds)
        {
            float end = Now + seconds;
            while (Now < end) yield return null;
        }

        private static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        /// <summary>Wartet, bis die Bedingung gilt – sonst bricht der Schritt mit <paramref name="failMessage"/> ab.</summary>
        private static IEnumerator Until(Func<bool> cond, float timeout, string failMessage)
        {
            float end = Now + timeout;
            while (!cond())
            {
                if (Now > end) throw new SelfTestFailure(failMessage + " (nach " + timeout.ToString("0.#") + " s)");
                yield return null;
            }
        }

        /// <summary>Wartet höchstens <paramref name="timeout"/> Sekunden auf die Bedingung (ohne Abbruch).</summary>
        private static IEnumerator UntilOrTimeout(Func<bool> cond, float timeout)
        {
            float end = Now + timeout;
            while (!cond() && Now < end) yield return null;
        }

        private static void Require(bool ok, string failMessage)
        {
            if (!ok) throw new SelfTestFailure(failMessage);
        }

        private void Check(bool ok, string okNote, string failMessage)
        {
            if (ok)
            {
                if (!string.IsNullOrEmpty(okNote)) _step.Note(okNote);
            }
            else _step.Fail(failMessage);
        }

        private void Soft(bool ok, string warnMessage)
        {
            if (!ok) _step.Warn(warnMessage);
        }

        /// <summary>Simuliert einen Tastendruck (Aktion aus der Tastenbelegung) für genau einen Frame.</summary>
        private static IEnumerator Press(string action)
        {
            GameInput.TestPressed.Add(action);
            try
            {
                yield return null;
            }
            finally
            {
                GameInput.TestPressed.Remove(action);
            }
        }

        private static string Slug(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char ch in (s ?? "").ToLowerInvariant())
            {
                if (ch >= 'a' && ch <= 'z' || ch >= '0' && ch <= '9') sb.Append(ch);
                else if (ch == 'ä') sb.Append("ae");
                else if (ch == 'ö') sb.Append("oe");
                else if (ch == 'ü') sb.Append("ue");
                else if (ch == 'ß') sb.Append("ss");
                else if (sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
            }
            return sb.ToString().Trim('_');
        }

        /// <summary>Screenshot (JPG, max. 1280 px breit) in den Berichtsordner; die Test-Einblendung wird dafür ausgeblendet.</summary>
        private IEnumerator Shot(string name)
        {
            if (Application.isBatchMode || string.IsNullOrEmpty(Folder)) yield break;
            string file = (++_shot).ToString("000") + "_" + Slug(name) + ".jpg";
            string path = Path.Combine(Folder, "screenshots", file);
            bool done = false;
            string error = null;
            SelfTestUI.SetOverlayHidden(true);
            StartCoroutine(Capture(path, err =>
            {
                error = err;
                done = true;
            }));
            float end = Now + 3f;
            while (!done && Now < end) yield return null;
            SelfTestUI.SetOverlayHidden(false);
            if (done && error == null) _step.Screenshots.Add("screenshots/" + file);
            else _step.Note("Screenshot „" + name + "“ nicht möglich" + (error != null ? ": " + error : " (Spielfenster nicht sichtbar?)"));
        }

        private static IEnumerator Capture(string path, Action<string> done)
        {
            yield return new WaitForEndOfFrame();
            Texture2D shot = null, small = null;
            string error = null;
            try
            {
                shot = ScreenCapture.CaptureScreenshotAsTexture();
                if (shot == null) error = "kein Bild";
                else
                {
                    small = Downscale(shot, 1280);
                    File.WriteAllBytes(path, (small ?? shot).EncodeToJPG(80));
                }
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            finally
            {
                if (shot != null) Destroy(shot);
                if (small != null) Destroy(small);
            }
            done(error);
        }

        /// <summary>Verkleinert auf höchstens <paramref name="maxWidth"/> Pixel Breite (2×2-Mittelung, CPU – farbtreu). null = nicht nötig.</summary>
        private static Texture2D Downscale(Texture2D src, int maxWidth)
        {
            int sw = src.width, sh = src.height;
            if (sw <= maxWidth) return null;
            int dw = maxWidth, dh = Mathf.Max(1, Mathf.RoundToInt(sh * (maxWidth / (float)sw)));
            var sp = src.GetPixels32();
            var dp = new Color32[dw * dh];
            float fx = sw / (float)dw, fy = sh / (float)dh;
            for (int y = 0; y < dh; y++)
            {
                int y0 = Mathf.Min(sh - 1, (int)(y * fy)), y1 = Mathf.Min(sh - 1, (int)((y + 0.5f) * fy));
                for (int x = 0; x < dw; x++)
                {
                    int x0 = Mathf.Min(sw - 1, (int)(x * fx)), x1 = Mathf.Min(sw - 1, (int)((x + 0.5f) * fx));
                    Color32 a = sp[y0 * sw + x0], b = sp[y0 * sw + x1], c = sp[y1 * sw + x0], d = sp[y1 * sw + x1];
                    dp[y * dw + x] = new Color32((byte)((a.r + b.r + c.r + d.r) >> 2), (byte)((a.g + b.g + c.g + d.g) >> 2),
                        (byte)((a.b + b.b + c.b + d.b) >> 2), 255);
                }
            }
            var t = new Texture2D(dw, dh, TextureFormat.RGB24, false);
            t.SetPixels32(dp);
            t.Apply(false);
            return t;
        }

        /// <summary>Misst Bildrate und GC-Speicher über <paramref name="seconds"/> Sekunden (Ergebnis in _lastPerf).</summary>
        private IEnumerator Sample(float seconds, string where, Action perFrame = null)
        {
            var dts = new List<float>(512);
            long gcSum = 0, gcMax = 0;
            int gcFrames = 0, gcSpikes = 0;
            int gc0 = GC.CollectionCount(0);
            yield return null;
            float end = Now + seconds;
            while (Now < end)
            {
                yield return null;
                dts.Add(Time.unscaledDeltaTime);
                perFrame?.Invoke();
                if (_gcRec.Valid)
                {
                    long gc = _gcRec.LastValue;
                    gcSum += gc;
                    gcFrames++;
                    if (gc > gcMax) gcMax = gc;
                    if (gc > 256 * 1024) gcSpikes++;
                }
            }
            var p = new PerfResult { Where = where ?? "" };
            if (dts.Count > 0)
            {
                float sum = 0f, max = 0f;
                int slow = 0;
                foreach (var d in dts)
                {
                    sum += d;
                    if (d > max) max = d;
                    if (d > 0.05f) slow++;
                }
                dts.Sort();
                float p99 = dts[Mathf.Clamp(Mathf.CeilToInt(dts.Count * 0.99f) - 1, 0, dts.Count - 1)];
                p.Frames = dts.Count;
                p.Seconds = sum;
                p.AvgFps = sum > 0f ? dts.Count / sum : 0f;
                p.MinFps = max > 0f ? 1f / max : 0f;
                p.Low1Fps = p99 > 0f ? 1f / p99 : 0f;
                p.SlowFrames = slow;
            }
            p.GcMeasured = gcFrames > 0;
            p.AvgGcKb = gcFrames > 0 ? gcSum / 1024f / gcFrames : 0f;
            p.MaxGcKb = gcMax / 1024f;
            p.GcSpikes = gcSpikes;
            p.GcCollections = GC.CollectionCount(0) - gc0;
            _lastPerf = p;
        }

        // ---- Welt: Stationen finden, davor stellen, benutzen -----------------------------------------
        private static Station FindStation(StationType type, string productId = null)
        {
            Station best = null;
            float bestD = float.MaxValue;
            var from = Game.Player != null ? Game.Player.transform.position : Vector3.zero;
            foreach (var s in Station.All)
            {
                if (s == null || !s.isActiveAndEnabled || s.Type != type) continue;
                if (productId != null && (type != StationType.Regal || s.ProductId != productId)) continue;
                float d = (s.transform.position - from).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = s;
                }
            }
            return best;
        }

        private static Station NearestPackstation(Vector3 from)
        {
            Station best = null;
            float bestD = float.MaxValue;
            foreach (var s in Station.All)
            {
                if (s == null || !s.isActiveAndEnabled || !PostService.IsFreeDrop(s) || s.Opts.Stage != PostService.StagePackstation) continue;
                float d = (s.transform.position - from).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = s;
                }
            }
            return best;
        }

        private static Bounds BoundsOf(GameObject go)
        {
            bool any = false;
            var b = new Bounds(go.transform.position + Vector3.up * 0.8f, Vector3.one * 0.5f);
            foreach (var c in go.GetComponentsInChildren<Collider>())
            {
                if (c == null || !c.enabled) continue;
                if (!any) b = c.bounds;
                else b.Encapsulate(c.bounds);
                any = true;
            }
            return b;
        }

        private const int RayMask = ~(1 << 2); // ohne „Ignore Raycast“ (Spieler)

        private static bool Free(Vector3 feet)
        {
            if (Physics.CheckCapsule(feet + Vector3.up * 0.45f, feet + Vector3.up * 1.55f, 0.3f, RayMask, QueryTriggerInteraction.Ignore)) return false;
            return Physics.Raycast(feet + Vector3.up * 0.4f, Vector3.down, 1.2f, RayMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Stellt den Spieler so vor ein Objekt, dass das Fadenkreuz es trifft (freier Platz, Sichtlinie innerhalb der
        /// Reichweite). false = kein guter Platz gefunden (Spieler steht dann trotzdem davor).
        /// </summary>
        private static bool PlaceFacing(PlayerController p, Bounds b, Transform orient, Func<Collider, bool> isTarget)
        {
            Physics.SyncTransforms();
            var c = b.center;
            float floor = orient != null ? orient.position.y : b.min.y;
            var f = orient != null ? orient.forward : Vector3.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 1e-4f) f = Vector3.forward;
            f.Normalize();
            var r = new Vector3(f.z, 0f, -f.x);
            Vector3[] dirs = { f, -f, r, -r, (f + r).normalized, (f - r).normalized, (-f + r).normalized, (-f - r).normalized };
            float[] gaps = { 0.75f, 1.2f, 0.45f, 1.7f };
            foreach (float gap in gaps)
            {
                foreach (var dir in dirs)
                {
                    float ext = Mathf.Abs(dir.x) * b.extents.x + Mathf.Abs(dir.z) * b.extents.z;
                    var feet = new Vector3(c.x, floor, c.z) + dir * (ext + gap);
                    if (!Free(feet)) continue;
                    var eye = feet + Vector3.up * PlayerController.StandCamY;
                    var aim = Vector3.Lerp(b.ClosestPoint(eye), c, 0.5f);
                    var to = aim - eye;
                    if (to.magnitude > PlayerController.Reach - 0.1f) continue;
                    if (!Physics.Raycast(eye, to.normalized, out var hit, PlayerController.Reach, RayMask, QueryTriggerInteraction.Collide)) continue;
                    if (!isTarget(hit.collider)) continue;
                    p.Teleport(feet + Vector3.up * 0.05f, 0f);
                    p.LookAt(aim);
                    return true;
                }
            }
            float e0 = Mathf.Abs(f.x) * b.extents.x + Mathf.Abs(f.z) * b.extents.z;
            p.Teleport(new Vector3(c.x, floor, c.z) + f * (e0 + 0.8f) + Vector3.up * 0.05f, 0f);
            p.LookAt(c);
            return false;
        }

        /// <summary>
        /// Benutzt ein Objekt wie der Spieler: davorstellen, anvisieren, Taste „Benutzen“ drücken. Trifft das
        /// Fadenkreuz nicht, wird Interact direkt aufgerufen und ein Hinweis notiert.
        /// </summary>
        private IEnumerator Use(IInteractable target, GameObject go, string what)
        {
            Require(target != null && go != null, what + ": nicht gefunden.");
            var p = Game.Player;
            Require(p != null, "Kein Spieler vorhanden.");
            var col = go.GetComponent<Collider>();
            var b = col != null ? col.bounds : BoundsOf(go);
            bool placed = PlaceFacing(p, b, go.transform, c => c != null && ReferenceEquals(c.GetComponentInParent<IInteractable>(), target));
            yield return Frames(3);
            bool aimed = ReferenceEquals(p.Focus, target) && Game.Root != null && !Game.Root.InputLocked;
            if (aimed)
            {
                yield return Press("interact");
                yield return Frames(1);
            }
            else
            {
                _step.Warn(what + ": Fadenkreuz trifft das Objekt nicht" + (placed ? "" : " (kein freier Platz davor gefunden)") + " – direkt benutzt.");
                target.Interact(p);
                yield return Frames(1);
            }
        }

        private IEnumerator UseStation(Station st, string what)
        {
            Require(st != null, what + ": Station nicht gefunden.");
            yield return Use(st, st.gameObject, what);
        }

        private static bool HasText(VisualElement root, string needle)
        {
            if (root == null || string.IsNullOrEmpty(needle)) return false;
            if (root is TextElement te && te.text != null && te.text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            foreach (var child in root.Children())
                if (HasText(child, needle))
                    return true;
            return false;
        }

        private static int CountElements(VisualElement root)
        {
            if (root == null) return 0;
            int n = 0;
            foreach (var child in root.Children()) n += 1 + CountElements(child);
            return n;
        }

        /// <summary>Prüft eine UI-Seite: Inhalt vorhanden, keine Fehlerseite, keine neuen Fehler im Log.</summary>
        private void CheckPage(VisualElement root, string what, int errorsBefore)
        {
            int n = CountElements(root);
            if (root == null || root.childCount == 0) _step.Fail(what + ": Seite ist leer.");
            else if (HasText(root, "hatte einen Fehler") || HasText(root, "kurz abgestürzt") || HasText(root, "konnte nicht geladen werden"))
                _step.Fail(what + ": zeigt eine Fehlerseite.");
            else if (HasText(root, "noch nicht fertig")) _step.Warn(what + ": Platzhalter „noch nicht fertig“.");
            if (_step.LogErrors.Count > errorsBefore) _step.Fail(what + ": " + (_step.LogErrors.Count - errorsBefore) + " Fehler im Log beim Öffnen.");
            _step.Note(what + ": " + n + " Elemente");
        }

        private static void TeleportOutside(PlayerController p, Vector3 pos, float yaw)
        {
            p.Teleport(pos, yaw);
            p.LookAt(pos + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 10f + Vector3.up * 1.5f);
        }

        // =====================================================================================
        // 1. Hauptmenü
        // =====================================================================================
        private IEnumerator StepMenu()
        {
            var ui = Game.UI;
            Require(ui != null && Game.Sim != null, "Oberfläche/Simulation nicht vorhanden (GameRoot fehlt).");
            if (Game.Sim.InGame)
            {
                _step.Note("Beim Start lief ein Spiel – das Hauptmenü wird am Ende geprüft.");
                yield break;
            }
            yield return Until(() => ui.Menu.IsOpen && !ui.Fader.Busy, 20f, "Hauptmenü ist nicht sichtbar");
            Check(ui.Menu.Content != null && ui.Menu.Content.childCount > 0, "Menü hat Inhalt", "Hauptmenü ist leer.");
            yield return Sample(1.5f, "Hauptmenü");
            _step.Note("Bildrate im Hauptmenü: " + _lastPerf.Text());
            yield return Shot("hauptmenue");
        }

        // =====================================================================================
        // 2. Neues Spiel
        // =====================================================================================
        private IEnumerator StepNewGame()
        {
            var ui = Game.UI;
            var sim = Game.Sim;
            Require(ui != null && sim != null && Game.Root != null, "GameRoot fehlt.");
            Require(IsTestDir(sim.SaveDir), "Sicherheitsstopp: Spielstand-Ordner zeigt nicht auf den Test-Ordner.");
            Require(ui.Menu.OnNewGame != null, "Hauptmenü hat keinen Start-Knopf (OnNewGame).");
            var oldPlayer = Game.Player;
            float t0 = Now;
            ui.Menu.OnNewGame("skip", 1);
            yield return Until(() => sim.InGame && Game.Player != null && Game.Player != oldPlayer && Game.World != null && !ui.Fader.Busy, 45f,
                "Spiel startet nicht (Welt oder Spieler fehlen – siehe Fehler im Log)");
            _step.Note("Ladezeit bis spielbereit: " + (Now - t0).ToString("0.0") + " s");
            Check(sim.StoryStage == "business", "Profi-Start: direkt im Business", "Falscher Spielabschnitt: " + sim.StoryStage);
            Check(sim.Slot == 1 && IsTestDir(sim.SaveDir), "Test-Spielstand 1 im Berichtsordner", "Spielstand-Ordner/Slot unerwartet.");
            sim.AdminTimeScale = 0f; // Uhr anhalten: reproduzierbarer Ablauf, kein Feierabend mitten im Test
            sim.AdminNoBankrupt = true;
            _step.Note("Spieluhr für den Test angehalten, Insolvenz aus.");
            yield return Wait(1f);
            var p = Game.Player;
            var pos = p.transform.position;
            Require(!float.IsNaN(pos.y) && pos.y > -2f, "Spieler steht unter der Welt (y = " + pos.y.ToString("0.0") + ").");
            float d = Vector3.Distance(pos, Game.World.SpawnPoint());
            Soft(d < 4f, "Spieler steht " + d.ToString("0.0") + " m vom Startpunkt entfernt.");
            int stations = 0;
            foreach (var s in Station.All)
                if (s != null)
                    stations++;
            Check(stations >= 5, stations + " Stationen in der Welt", "Zu wenige Stationen in der Welt (" + stations + ").");
            Check(Game.UI.Hud != null, null, "HUD fehlt.");
            yield return Sample(2f, "Garage");
            _step.Note("Bildrate in der Garage: " + _lastPerf.Text());
            yield return Shot("garage_start");
        }

        private bool IsTestDir(string dir)
        {
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(Folder)) return false;
            try
            {
                return Path.GetFullPath(dir).StartsWith(Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =====================================================================================
        // 3. Kernschleife
        // =====================================================================================
        private IEnumerator StepCoreLoop()
        {
            var sim = Game.Sim;
            var p = Game.Player;
            int pi = Array.FindIndex(GameData.Products, x => x.Id == TestProduct);
            Require(pi >= 0, "Testprodukt „" + TestProduct + "“ fehlt in GameData.");
            Require(sim.ProductAvailable(TestProduct), "Testprodukt ist nicht verfügbar (gesperrt oder kein Regal).");
            p.ClearHands();

            // Einkauf (wie Laptop › AllesExpress › Kaufen)
            int money0 = sim.Money;
            Require(sim.BuyBulk(pi, 0, 0), "Einkauf bei AllesExpress fehlgeschlagen (" + _lastToast + ").");
            _step.Note("Eingekauft für " + Fmt.Money(money0 - sim.Money));
            sim.AdminArriveDeliveries();
            yield return Frames(2);
            Require(sim.DockCrates.Count > 0, "Lieferung ist nicht am Wareneingang angekommen.");
            var crate = sim.DockCrates[0];
            Require(crate.Product == TestProduct, "Am Wareneingang liegt eine andere Kiste zuerst (" + crate.Product + ").");
            int qty = crate.Quantity;

            // Kiste aufnehmen
            yield return UseStation(FindStation(StationType.Dock), "Wareneingang");
            Require(p.Held != null && p.Held.Kind == ItemKind.Crate, "Kiste wurde am Wareneingang nicht aufgenommen.");
            yield return Shot("kiste_in_der_hand");

            // Ins Regal einräumen
            var regal = FindStation(StationType.Regal, TestProduct);
            int stock0 = sim.StockQty(TestProduct);
            yield return UseStation(regal, "Regal " + TestProduct);
            Require(p.Held == null, "Kiste ist noch in der Hand (nicht ins Regal eingeräumt).");
            Check(sim.StockQty(TestProduct) == stock0 + qty, "Regal: +" + qty + " auf Lager", "Lagerbestand stimmt nicht (" + sim.StockQty(TestProduct) + ", erwartet " + (stock0 + qty) + ").");

            // Bestellung kommt rein
            var order = sim.SpawnOrder(TestProduct, false);
            Require(order != null, "Bestellung konnte nicht erzeugt werden.");
            yield return Frames(2);

            // Artikel aus dem Regal nehmen
            yield return UseStation(regal, "Regal (Artikel entnehmen)");
            Require(p.Held != null && p.Held.Kind == ItemKind.Item, "Kein Artikel aus dem Regal entnommen.");
            int orderId = p.Held.OrderId;
            Check(orderId > 0, "Artikel gehört zu Bestellung #" + orderId, "Artikel hat keine Bestellnummer.");

            // Packtisch
            var pack = FindStation(StationType.Pack);
            yield return UseStation(pack, "Packtisch");
            Require(p.Held == null && sim.PackedCount() > 0, "Artikel wurde am Packtisch nicht verpackt.");
            yield return Wait(0.8f);
            yield return Shot("packtisch");
            yield return UseStation(pack, "Packtisch (Paket nehmen)");
            Require(p.Held != null && (p.Held.Kind == ItemKind.Package || p.Held.Kind == ItemKind.Labeled), "Fertiges Paket wurde nicht aufgenommen.");

            // Label
            if (p.Held.Kind == ItemKind.Package)
            {
                yield return UseStation(FindStation(StationType.Label), "Labeldrucker");
                Require(p.Held != null && p.Held.Kind == ItemKind.Labeled, "Paket hat kein Versandlabel bekommen.");
            }
            else _step.Note("Paket wurde automatisch etikettiert.");

            // Zur nächsten Packstation tragen und abgeben
            var ship = NearestPackstation(p.transform.position);
            Require(ship != null, "Keine Packstation in der Stadt gefunden.");
            int money1 = sim.Money, shipped0 = sim.TotalShipped;
            yield return UseStation(ship, "Packstation");
            yield return Frames(2);
            Require(p.Held == null, "Paket ist nach der Abgabe noch in der Hand.");
            Require(sim.TotalShipped == shipped0 + 1, "Versand wurde nicht gezählt.");
            Require(sim.Money > money1, "Kein Geld für das Paket bekommen.");
            _step.Note("Paket verschickt: +" + Fmt.Money(sim.Money - money1));
            bool open = sim.OrdersInWork.Exists(o => o.Id == orderId) || sim.OrderQueue.Exists(o => o.Id == orderId);
            Check(!open, "Bestellung #" + orderId + " abgeschlossen", "Bestellung #" + orderId + " ist noch offen.");
            yield return Wait(0.6f);
            yield return Shot("packstation_versendet");
        }

        // =====================================================================================
        // 4./7. Laptop
        // =====================================================================================
        private IEnumerator StepLaptop(bool shots)
        {
            var sim = Game.Sim;
            var lap = Game.UI.Laptop;
            bool prevUpcoming = LaptopView.ShowUpcoming;
            LaptopView.ShowUpcoming = true; // auch Apps zeigen, die erst später erscheinen (Aufträge, Retouren)
            try
            {
                var pc = FindStation(StationType.Pc);
                if (pc != null) yield return UseStation(pc, shots ? "Büro-PC" : "Laptop");
                else _step.Warn("Keine PC-Station gefunden.");
                if (!sim.PcOpen)
                {
                    _step.Warn("Laptop ging über die Station nicht auf – direkt geöffnet.");
                    sim.OpenPc();
                }
                yield return Until(() => lap.IsOpen, 3f, "Laptop öffnet sich nicht");
                yield return UntilOrTimeout(() => !lap.Booting, 4f);
                yield return Frames(2);
                int pages = 0;
                foreach (var app in new List<LaptopView.AppDef>(LaptopView.Apps))
                {
                    if (app == null) continue;
                    if (app.Visible != null && !SafeBool(app.Visible))
                    {
                        _step.Note("Laptop " + app.Id + ": derzeit ausgeblendet");
                        continue;
                    }
                    if (app.Tabs != null && app.Tabs.Count > 0)
                    {
                        foreach (var tab in new List<LaptopView.TabDef>(app.Tabs))
                        {
                            if (tab == null) continue;
                            if (tab.Visible != null && !SafeBool(tab.Visible))
                            {
                                _step.Note("Laptop " + app.Id + "/" + tab.Id + ": derzeit ausgeblendet");
                                continue;
                            }
                            yield return LaptopPage(app, tab, shots);
                            pages++;
                        }
                    }
                    else
                    {
                        yield return LaptopPage(app, null, shots);
                        pages++;
                    }
                }
                _step.Note(pages + " Laptop-Seiten geöffnet");
                sim.ClosePc();
                yield return Frames(3);
                Check(!lap.IsOpen && !Game.Root.InputLocked, "Laptop geschlossen", "Laptop schließt nicht sauber (Steuerung bleibt gesperrt).");
            }
            finally
            {
                LaptopView.ShowUpcoming = prevUpcoming;
            }
        }

        private static bool SafeBool(Func<bool> f)
        {
            try { return f(); }
            catch (Exception) { return false; }
        }

        private IEnumerator LaptopPage(LaptopView.AppDef app, LaptopView.TabDef tab, bool shots)
        {
            var lap = Game.UI.Laptop;
            string name = app.Id + (tab != null ? "/" + tab.Id : "");
            int errors = _step.LogErrors.Count;
            lap.OpenApp(app.Id, tab?.Id);
            yield return Frames(3);
            if (lap.Current != app.Id)
            {
                _step.Fail("Laptop " + name + ": öffnet nicht (zeigt „" + lap.Current + "“).");
                yield break;
            }
            if (tab != null && lap.CurrentTab != tab.Id) _step.Fail("Laptop " + name + ": Reiter öffnet nicht (zeigt „" + lap.CurrentTab + "“).");
            CheckPage(lap.AppRoot, "Laptop " + name, errors);
            if (shots) yield return Shot("laptop_" + name);
        }

        // =====================================================================================
        // 8. Handy
        // =====================================================================================
        private static readonly string[] PhoneNames = { "Bestellungen", "Nachrichten", "Nachbestellen", "Status" };

        private IEnumerator StepPhone()
        {
            var ph = Game.UI.Phone;
            yield return Press("phone");
            yield return Frames(2);
            if (!ph.IsOpen)
            {
                _step.Warn("Handy-Taste (" + GameInput.KeyboardLabel("phone") + ") wirkte nicht – Handy direkt geöffnet.");
                ph.Open();
            }
            Require(ph.IsOpen, "Handy öffnet sich nicht.");
            yield return Wait(0.35f);
            var apps = new List<int> { 0, 1, 2, 3 };
            for (int i = 0; i < PhoneView.ExtraApps.Count; i++)
            {
                var a = PhoneView.ExtraApps[i];
                if (a == null) continue;
                if (a.Visible != null && !SafeBool(a.Visible))
                {
                    _step.Note("Handy-App „" + a.Title + "“: derzeit ausgeblendet");
                    continue;
                }
                apps.Add(4 + i);
            }
            foreach (int idx in apps)
            {
                string title = idx < 4 ? PhoneNames[idx] : PhoneView.ExtraApps[idx - 4].Title;
                int errors = _step.LogErrors.Count;
                ph.SelectApp(idx);
                yield return Frames(3);
                CheckPage(ph.Content, "Handy " + title, errors);
                yield return Shot("handy_" + title);
            }
            yield return Press("phone");
            yield return Frames(2);
            if (ph.IsOpen)
            {
                _step.Warn("Handy-Taste schließt das Handy nicht – direkt geschlossen.");
                ph.Close(true);
            }
            yield return Wait(0.3f);
            Check(!Game.Root.InputLocked, null, "Steuerung bleibt nach dem Handy gesperrt.");
        }

        // =====================================================================================
        // 5. Pausemenü, Einstellungen, Hilfe
        // =====================================================================================
        private IEnumerator StepMenus()
        {
            _autoCloseModals = false;
            var ui = Game.UI;
            var pause = ui.Pause;
            pause.Open(); // wie Esc
            yield return Frames(3);
            Require(pause.IsOpen, "Pausemenü öffnet sich nicht.");
            int errors = _step.LogErrors.Count;
            CheckPage(pause.Card, "Pausemenü", errors);
            Check(Time.timeScale == 0f, "Spiel pausiert", "Spiel läuft im Pausemenü weiter (timeScale " + Time.timeScale + ").");
            yield return Shot("pausemenue");

            pause.ShowPage("settings");
            yield return Frames(3);
            for (int k = 0; k < 5; k++)
            {
                errors = _step.LogErrors.Count;
                if (k > 0)
                {
                    SettingsView.CycleTab(1);
                    yield return Frames(3);
                }
                CheckPage(pause.Card, "Einstellungen Reiter " + (k + 1), errors);
                yield return Shot("einstellungen_" + (k + 1));
            }

            errors = _step.LogErrors.Count;
            pause.ShowPage("help");
            yield return Frames(3);
            CheckPage(pause.Card, "Pausemenü › Hilfe", errors);
            yield return Shot("pause_hilfe");

            errors = _step.LogErrors.Count;
            pause.ShowPage("feedback");
            yield return Frames(3);
            CheckPage(pause.Card, "Pausemenü › Feedback", errors);
            yield return Shot("pause_feedback");

            pause.ShowPage("menu");
            yield return Frames(2);
            pause.Close();
            yield return Frames(3);
            Check(!pause.IsOpen && Time.timeScale > 0f, "Pausemenü geschlossen, Spiel läuft weiter", "Pausemenü schließt nicht sauber.");

            // Hilfe-Fenster über die Hilfe-Taste
            yield return Press("help");
            yield return Frames(3);
            if (!(ui.Modal.IsOpen && ui.Modal.Current == "help"))
            {
                _step.Warn("Hilfe-Taste (" + GameInput.KeyboardLabel("help") + ") wirkte nicht – Hilfe direkt geöffnet.");
                pause.OnHelp?.Invoke();
                yield return Frames(3);
            }
            Require(ui.Modal.IsOpen && ui.Modal.Current == "help", "Hilfe-Fenster öffnet sich nicht.");
            errors = _step.LogErrors.Count;
            CheckPage(ui.Modal.Card, "Hilfe-Fenster", errors);
            yield return Shot("hilfe_fenster");
            ui.Modal.Close();
            yield return Frames(3);
            Check(!ui.Modal.IsOpen && !Game.Root.InputLocked, null, "Hilfe-Fenster schließt nicht sauber.");
        }

        // =====================================================================================
        // 6. Alles freischalten
        // =====================================================================================
        private IEnumerator StepUnlock()
        {
            var sim = Game.Sim;
            sim.AdminUnlockEverything();
            yield return Wait(1.5f);
            Check(sim.Level == GameData.MaxLevel, "Level " + sim.Level, "Level wurde nicht auf das Maximum gesetzt.");
            Check(sim.LocationStage >= 1, "Umzug in die Lagerhalle", "Lagerhalle wurde nicht freigeschaltet.");
            Check(FindStation(StationType.Pc) != null, null, "Nach dem Umzug fehlt der PC.");
            Check(FindStation(StationType.Pack) != null, null, "Nach dem Umzug fehlt der Packtisch.");
            var p = Game.Player;
            p.Teleport(Game.World.SpawnPoint(), Game.World.SpawnYaw());
            yield return Wait(0.8f);
            yield return Shot("lagerhalle");
        }

        // =====================================================================================
        // 9. Moped
        // =====================================================================================
        private IEnumerator StepMoped()
        {
            var p = Game.Player;
            yield return Until(() => Moped.Instance != null, 5f, "Kein Moped in der Welt");
            TeleportOutside(p, new Vector3(0f, 0.05f, -5.2f), 0f);
            yield return Wait(0.3f);
            Moped.AdminBring(); // wie Admin › „Moped zu mir“
            yield return Wait(0.4f);
            var m = Moped.Instance;
            Require(m != null, "Moped verschwunden.");
            Soft(Vector3.Distance(m.transform.position, p.transform.position) < 5f, "Moped steht nicht neben dem Spieler.");
            p.ClearHands();
            yield return Use(m, m.gameObject, "Moped (aufsteigen)");
            yield return Frames(2);
            Require(p.Riding == m, "Aufsteigen hat nicht geklappt.");
            yield return Wait(0.3f);
            var start = m.transform.position;
            float maxSpeed = 0f;
            bool shot = false;
            GameInput.TestMove = new Vector2(0f, 1f);
            float end = Now + 3f;
            while (Now < end)
            {
                maxSpeed = Mathf.Max(maxSpeed, m.Speed);
                if (!shot && Now > end - 1.2f)
                {
                    shot = true;
                    yield return Shot("moped_fahrt");
                }
                yield return null;
            }
            GameInput.TestMove = new Vector2(0f, -1f); // bremsen
            yield return UntilOrTimeout(() => Mathf.Abs(m.Speed) < 0.6f, 3f);
            GameInput.TestMove = null;
            var delta = m.transform.position - start;
            delta.y = 0f;
            _step.Note("Gefahren: " + delta.magnitude.ToString("0.0") + " m, Spitze " + VehicleRules.MsToKmh(maxSpeed).ToString("0") + " km/h");
            Check(delta.magnitude > 1.5f, null, "Moped hat sich kaum bewegt (" + delta.magnitude.ToString("0.0") + " m).");

            yield return Press("interact"); // absteigen
            yield return UntilOrTimeout(() => p.Riding == null, 4f);
            if (p.Riding != null)
            {
                _step.Warn("Absteigen mit " + GameInput.KeyboardLabel("interact") + " dauerte zu lange – direkt abgestiegen.");
                m.Exit();
            }
            yield return Frames(2);
            Check(p.Riding == null, "Abgestiegen", "Spieler sitzt noch auf dem Moped.");
            Check(p.transform.position.y > m.transform.position.y - 1f, null, "Spieler steht nach dem Absteigen unter dem Boden.");
        }

        // =====================================================================================
        // 10.–14. Sondermodi
        // =====================================================================================
        private IEnumerator StepMover()
        {
            var sim = Game.Sim;
            var p = Game.Player;
            p.ClearHands();
            p.Teleport(Game.World.SpawnPoint(), Game.World.SpawnYaw());
            yield return Frames(2);
            Movable target = null;
            float best = float.MaxValue;
            foreach (var mv in Movable.All)
            {
                if (mv == null || !mv.isActiveAndEnabled || mv.Stage != sim.LocationStage) continue;
                float d = (mv.transform.position - p.transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    target = mv;
                }
            }
            Require(target != null, "Kein verschiebbares Möbelstück gefunden.");
            var orig = target.transform.position;
            bool placed = PlaceFacing(p, BoundsOf(target.gameObject), target.transform, c => c != null && c.GetComponentInParent<Movable>() == target);
            yield return Frames(3);
            yield return Press("build");
            yield return Frames(2);
            if (!p.Mover.Active)
            {
                _step.Warn("Taste " + GameInput.KeyboardLabel("build") + " startete den Verschiebe-Modus nicht" + (placed ? "" : " (kein guter Platz vor dem Möbel)") + " – direkt gestartet.");
                p.Mover.TryBegin(p);
                yield return Frames(2);
            }
            Require(p.Mover.Active, "Verschiebe-Modus startet nicht („" + target.Label + "“).");
            _step.Note("Möbel: " + target.Label);
            yield return Wait(0.5f);
            yield return Shot("moebel_verschieben");
            p.Mover.Cancel();
            yield return Frames(3);
            Check(!p.Mover.Active, "Verschiebe-Modus beendet", "Verschiebe-Modus lässt sich nicht abbrechen.");
            Check((target.transform.position - orig).sqrMagnitude < 0.01f, null, "Möbelstück steht nach dem Abbrechen nicht mehr an seinem Platz.");
        }

        private IEnumerator StepEmote()
        {
            var p = Game.Player;
            GameInput.TestHeld.Add("emote");
            yield return UntilOrTimeout(() => EmoteController.WheelOpen, 2f);
            bool opened = EmoteController.WheelOpen;
            if (opened)
            {
                yield return Wait(0.3f);
                yield return Shot("emote_rad");
            }
            GameInput.TestHeld.Remove("emote");
            yield return Frames(4);
            Require(opened, "Emote-Rad öffnet nicht (Taste " + GameInput.KeyboardLabel("emote") + " gehalten).");
            Check(!EmoteController.WheelOpen, "Rad geöffnet und geschlossen", "Emote-Rad schließt nicht.");
            var ec = p.GetComponent<EmoteController>();
            Require(ec != null, "EmoteController fehlt am Spieler.");
            ec.Play(EmoteController.Emote.Wave);
            yield return Wait(1.2f);
            ec.Stop();
            _step.Note("Emote „Winken“ abgespielt");
        }

        private IEnumerator StepTikTok()
        {
            var sim = Game.Sim;
            var p = Game.Player;
            if (sim.Level < GameData.TikTokLevel)
            {
                sim.AdminSetLevel(GameData.TikTokLevel);
                _step.Note("Level für TikTok angehoben.");
            }
            sim.AdminResetTikTok();
            p.ClearHands();
            p.Teleport(Game.World.SpawnPoint(), Game.World.SpawnYaw());
            yield return Wait(0.5f);
            bool ok = p.Recorder.Begin(p, TestProduct, TikTokFormats.Review);
            Require(ok, "Aufnahme startet nicht" + (_lastToast != "" ? " (Hinweis im Spiel: " + _lastToast + ")" : "") + ".");
            yield return Wait(1.2f);
            Check(p.Recorder.Active, "Sucher aktiv", "Aufnahme beendet sich von selbst.");
            yield return Shot("tiktok_sucher");
            p.Recorder.Cancel();
            yield return Frames(3);
            Check(!p.Recorder.Active, "Aufnahme abgebrochen", "Aufnahme lässt sich nicht abbrechen.");
        }

        private IEnumerator StepBirdsEye()
        {
            BirdsEyeView.Toggle();
            yield return Wait(1f);
            Require(BirdsEyeView.Active, "Vogelperspektive geht nicht an.");
            yield return Shot("vogelperspektive");
            BirdsEyeView.Toggle();
            yield return Frames(3);
            Check(!BirdsEyeView.Active, "Vogelperspektive an und aus", "Vogelperspektive geht nicht aus.");
            Check(!Game.Root.InputLocked, null, "Steuerung bleibt nach der Vogelperspektive gesperrt.");
        }

        private IEnumerator StepNoclip()
        {
            var p = Game.Player;
            TeleportOutside(p, new Vector3(-30f, 0.05f, -5.2f), 90f);
            yield return Frames(2);
            var a = p.transform.position;
            p.SetNoclip(true);
            yield return Frames(2);
            Require(PlayerController.Noclip, "Noclip geht nicht an.");
            GameInput.TestMove = new Vector2(0f, 1f);
            yield return Wait(1f);
            GameInput.TestMove = null;
            float d = Vector3.Distance(a, p.transform.position);
            Check(d > 2f, "Geflogen: " + d.ToString("0.0") + " m", "Noclip: keine Bewegung.");
            p.SetNoclip(false);
            yield return Frames(2);
            Check(!PlayerController.Noclip, "Noclip aus", "Noclip geht nicht aus.");
            p.Teleport(Game.World.SpawnPoint(), Game.World.SpawnYaw());
        }

        // =====================================================================================
        // 15. Stadtviertel
        // =====================================================================================
        private IEnumerator StepDistricts()
        {
            var sim = Game.Sim;
            var p = Game.Player;
            var places = new List<(string name, Vector3 pos, float yaw)>
            {
                ("Garage", WorldBuilder.SpawnGarage, 180f),
                ("Lagerhalle", WorldBuilder.SpawnWarehouse, 180f),
                ("Kalles Imbiss", WorldBuilder.SpawnDiner, 180f),
                ("Einkaufsviertel", WorldLots.Einkaufsviertel.StreetAccess + Vector3.up * 0.05f, 180f),
                ("Läden innen", ShoppingDistrict.SpawnShops, 180f),
                ("Finanzviertel", WorldLots.Finanzviertel.StreetAccess + Vector3.up * 0.05f, 0f),
                ("PaketBlitz-Filiale", new Vector3(PostService.PostDoorX, 0.05f, -5.5f), 180f),
                ("Packstation Parkecke", new Vector3(8f, 0.05f, 5f), 0f),
                ("Wohnviertel", new Vector3(-18f, 0.05f, 52f), 0f),
                ("Westende", new Vector3(-145f, 0.05f, 5.5f), 90f),
                ("Ostende", new Vector3(145f, 0.05f, -5.5f), -90f),
            };
            foreach (var place in places) yield return Visit(p, place.name, place.pos, place.yaw);

            // Straßenfest über das Admin-Panel auslösen (sofort live)
            bool fest = sim.AdminStartFestival(true);
            Check(fest, "Straßenfest gestartet", "Straßenfest ließ sich nicht starten (" + _lastToast + ").");
            yield return Wait(2f);
            if (fest) yield return Visit(p, "Straßenfest", new Vector3(StreetFestival.StallPos.x, 0.05f, StreetFestival.StallPos.z - 5f), 0f);
            if (sim.FestivalActive) sim.AdminEndFestival();
            yield return Frames(2);
            p.Teleport(Game.World.SpawnPoint(), Game.World.SpawnYaw());
        }

        private IEnumerator Visit(PlayerController p, string name, Vector3 pos, float yaw)
        {
            int errors = _step.LogErrors.Count;
            TeleportOutside(p, pos, yaw);
            yield return Wait(1.2f);
            var at = p.transform.position;
            if (float.IsNaN(at.y) || at.y < -5f) _step.Fail(name + ": Spieler fällt durch den Boden.");
            else if (Vector3.Distance(new Vector3(at.x, 0f, at.z), new Vector3(pos.x, 0f, pos.z)) > 6f) _step.Fail(name + ": Spieler wurde weggesetzt (Boden fehlt?).");
            yield return Sample(0.6f, name);
            _step.Note(name + ": Ø " + _lastPerf.AvgFps.ToString("0") + " FPS");
            if (_step.LogErrors.Count > errors) _step.Fail(name + ": Fehler im Log.");
            yield return Shot("viertel_" + name);
        }

        // =====================================================================================
        // 16./17. Wetter & Tageszeiten
        // =====================================================================================
        private IEnumerator StepWeather()
        {
            var p = Game.Player;
            var atmos = Game.World.Atmos;
            Require(atmos != null, "Keine Atmosphäre in der Welt.");
            TeleportOutside(p, new Vector3(2f, 0.05f, -5.2f), 60f);
            if (!Settings.Weather) _step.Warn("Wetter ist in den Einstellungen ausgeschaltet – Regen kann nicht geprüft werden.");
            Weather.ForceKind = (int)Weather.Kind.Rain;
            yield return Wait(1.5f);
            var w = atmos.Weather;
            Require(w != null, "Wetter-Komponente fehlt.");
            if (Settings.Weather) Check(w.Today == Weather.Kind.Rain && w.Rain > 0.05f, "Regen aktiv", "Regen lässt sich nicht erzwingen.");
            yield return Shot("wetter_regen");
            Weather.ForceKind = (int)Weather.Kind.Clear;
            yield return Wait(1.2f);
            Check(w.Today == Weather.Kind.Clear, "Klares Wetter", "Wetter wechselt nicht zurück auf klar.");
            yield return Shot("wetter_klar");
            Weather.ForceKind = _saved.WeatherForce;
        }

        private IEnumerator StepDaytime()
        {
            var sim = Game.Sim;
            var p = Game.Player;
            var atmos = Game.World.Atmos;
            Require(atmos != null, "Keine Atmosphäre in der Welt.");
            TeleportOutside(p, new Vector3(2f, 0.05f, -5.2f), 60f);
            float[] hours = { 8.5f, 12.5f, 18.6f };
            string[] names = { "Morgen", "Mittag", "Goldene Stunde" };
            for (int i = 0; i < hours.Length; i++)
            {
                sim.TimeMinutes = hours[i] * 60f; // Uhr steht (Test), GameRoot übernimmt das Licht
                yield return Wait(0.7f);
                Check(Mathf.Abs(atmos.Hours - hours[i]) < 0.1f, names[i] + ": " + Fmt.Clock(sim.TimeMinutes) + " Uhr", names[i] + ": Licht folgt der Uhrzeit nicht.");
                yield return Shot("tageszeit_" + names[i]);
            }
            // Nacht liegt außerhalb der Geschäftszeit: Licht direkt setzen (jeden Frame, GameRoot stellt sonst zurück).
            float end = Now + 0.6f;
            while (Now < end)
            {
                atmos.SetTimeOfDay(22f);
                yield return null;
            }
            atmos.SetTimeOfDay(22f);
            yield return Shot("tageszeit_nacht");
            atmos.SetTimeOfDay(sim.TimeMinutes / 60f);
            _step.Note("Nacht (22 Uhr) als Lichtstimmung gezeigt");
        }

        // =====================================================================================
        // 18. Sprache
        // =====================================================================================
        private IEnumerator StepLanguage()
        {
            var sim = Game.Sim;
            var ui = Game.UI;
            string orig = Settings.Language;
            var before = new HashSet<string>(Lang.Missing);
            if (orig == "en") _step.Note("Spiel ist bereits auf Englisch eingestellt.");
            try
            {
                Settings.SetLanguage("en");
                yield return Wait(0.6f);
                Require(Loc.English, "Englisch lässt sich nicht einschalten.");
                yield return Shot("en_hud");

                // Laptop: jede App (erster Reiter) und jeder Reiter kurz
                sim.OpenPc();
                yield return Until(() => ui.Laptop.IsOpen, 3f, "Laptop öffnet sich auf Englisch nicht");
                yield return UntilOrTimeout(() => !ui.Laptop.Booting, 4f);
                bool prevUpcoming = LaptopView.ShowUpcoming;
                LaptopView.ShowUpcoming = true;
                foreach (var app in new List<LaptopView.AppDef>(LaptopView.Apps))
                {
                    if (app == null || (app.Visible != null && !SafeBool(app.Visible))) continue;
                    if (app.Tabs != null && app.Tabs.Count > 0)
                    {
                        foreach (var tab in new List<LaptopView.TabDef>(app.Tabs))
                        {
                            if (tab == null || (tab.Visible != null && !SafeBool(tab.Visible))) continue;
                            ui.Laptop.OpenApp(app.Id, tab.Id);
                            yield return Frames(3);
                        }
                    }
                    else
                    {
                        ui.Laptop.OpenApp(app.Id);
                        yield return Frames(3);
                    }
                    if (app.Id == "home" || app.Id == "shop" || app.Id == "company") yield return Shot("en_laptop_" + app.Id);
                }
                LaptopView.ShowUpcoming = prevUpcoming;
                sim.ClosePc();
                yield return Frames(3);

                // Handy
                ui.Phone.Open();
                yield return Wait(0.3f);
                int phoneApps = 4 + PhoneView.ExtraApps.Count;
                for (int i = 0; i < phoneApps; i++)
                {
                    if (i >= 4)
                    {
                        var a = PhoneView.ExtraApps[i - 4];
                        if (a == null || (a.Visible != null && !SafeBool(a.Visible))) continue;
                    }
                    ui.Phone.SelectApp(i);
                    yield return Frames(3);
                    if (i == 0 || i == 3) yield return Shot("en_handy_" + i);
                }
                ui.Phone.Close(true);
                yield return Frames(2);

                // Pausemenü, Einstellungen, Hilfe
                _autoCloseModals = false;
                ui.Pause.Open();
                yield return Frames(3);
                yield return Shot("en_pause");
                ui.Pause.ShowPage("settings");
                yield return Frames(3);
                yield return Shot("en_einstellungen");
                for (int k = 1; k < 5; k++)
                {
                    SettingsView.CycleTab(1);
                    yield return Frames(3);
                }
                ui.Pause.ShowPage("help");
                yield return Frames(3);
                ui.Pause.ShowPage("feedback");
                yield return Frames(3);
                ui.Pause.Close();
                _autoCloseModals = true;
                yield return Wait(0.5f);
            }
            finally
            {
                if (Settings.Language != orig) Settings.SetLanguage(orig);
            }
            yield return Wait(0.5f);
            Check(Settings.Language == orig, "Sprache zurück auf „" + orig + "“", "Sprache ließ sich nicht zurückstellen.");

            var missing = new List<string>();
            foreach (var m in Lang.Missing)
                if (!before.Contains(m))
                    missing.Add(m);
            missing.Sort(StringComparer.Ordinal);
            Report.MissingTranslations = missing;
            if (missing.Count == 0) _step.Note("Keine fehlenden Übersetzungen gefunden");
            else
            {
                _step.Warn(missing.Count + " Texte ohne englische Übersetzung (vollständige Liste: missing_en.txt).");
                for (int i = 0; i < missing.Count && i < 15; i++) _step.Note("fehlt: " + StepResult.FirstLine(missing[i]));
            }
        }

        // =====================================================================================
        // 19. Speichern & Laden
        // =====================================================================================
        private IEnumerator StepSaveLoad()
        {
            var sim = Game.Sim;
            var ui = Game.UI;
            Require(IsTestDir(sim.SaveDir), "Sicherheitsstopp: Spielstand-Ordner zeigt nicht auf den Test-Ordner – es wird nichts gespeichert.");
            Require(sim.IsOpen, "Speichern geht nur während des Geschäftstags.");
            int money = sim.Money, day = sim.Day, shipped = sim.TotalShipped, level = sim.Level, stock = sim.StockQty(TestProduct), stage = sim.LocationStage;
            string brand = sim.BrandName;
            int slot = sim.Slot;
            Require(sim.SaveGame(false), "Speichern fehlgeschlagen (" + _lastToast + ").");
            string path = sim.SavePath(slot);
            Require(File.Exists(path), "Spielstand-Datei fehlt nach dem Speichern.");
            _step.Note("Gespeichert: " + Path.GetFileName(path) + " (" + (new FileInfo(path).Length / 1024) + " KB)");

            var oldPlayer = Game.Player;
            Require(ui.Menu.OnLoad != null, "Laden nicht verfügbar (OnLoad).");
            float t0 = Now;
            ui.Menu.OnLoad(slot);
            yield return Until(() => sim.InGame && Game.Player != null && Game.Player != oldPlayer && !ui.Fader.Busy, 40f, "Spielstand wird nicht geladen");
            _step.Note("Ladezeit: " + (Now - t0).ToString("0.0") + " s");
            sim.AdminTimeScale = 0f;
            yield return Wait(0.5f);
            Check(sim.Money == money, "Geld stimmt (" + Fmt.Money(money) + ")", "Geld nach dem Laden: " + Fmt.Money(sim.Money) + " statt " + Fmt.Money(money) + ".");
            Check(sim.Day == day, null, "Tag nach dem Laden: " + sim.Day + " statt " + day + ".");
            Check(sim.TotalShipped == shipped, null, "Verschickte Pakete nach dem Laden: " + sim.TotalShipped + " statt " + shipped + ".");
            Check(sim.Level == level, null, "Level nach dem Laden: " + sim.Level + " statt " + level + ".");
            Check(sim.StockQty(TestProduct) == stock, null, "Lagerbestand nach dem Laden: " + sim.StockQty(TestProduct) + " statt " + stock + ".");
            Check(sim.LocationStage == stage, null, "Standort nach dem Laden: " + sim.LocationStage + " statt " + stage + ".");
            Check(sim.BrandName == brand, null, "Markenname nach dem Laden: „" + sim.BrandName + "“ statt „" + brand + "“.");
            Check(!sim.LoadedFromBackup, null, "Spielstand wurde aus der Sicherung (.bak) geladen.");
            yield return Shot("nach_dem_laden");
        }

        // =====================================================================================
        // 20. Feierabend
        // =====================================================================================
        private IEnumerator StepDayEnd()
        {
            _autoCloseModals = false;
            var sim = Game.Sim;
            var ui = Game.UI;
            int day = sim.Day;
            var p = Game.Player;
            p.ClearHands();
            var bed = FindStation(StationType.EndDay);
            if (bed != null) yield return UseStation(bed, bed.Title);
            else
            {
                _step.Warn("Keine Feierabend-Station gefunden – Feierabend direkt angefragt.");
                sim.RequestEndDay();
            }
            yield return Until(() => ui.Modal.IsOpen && ui.Modal.Current == "confirm", 3f, "Rückfrage „Feierabend machen?“ erscheint nicht");
            yield return Shot("feierabend_rueckfrage");
            Require(ClickButton(ui.Modal.Card, "Ja, Tag beenden", "btn-accent"), "Knopf „Ja, Tag beenden“ nicht gefunden.");
            yield return Until(() => ui.BigModal.IsOpen && ui.BigModal.Current == "summary", 6f, "Tagesabrechnung erscheint nicht");
            Check(sim.DayOver, "Tag " + day + " beendet", "Tag ist nach der Abrechnung nicht vorbei.");
            yield return Wait(1.2f);
            yield return Shot("tagesabrechnung");
            Require(ClickButton(ui.BigModal.Card, "Nächster Tag", "btn-accent"), "Knopf „Nächster Tag“ nicht gefunden.");
            yield return Until(() => sim.Day == day + 1 && !sim.DayOver && !ui.Fader.Busy, 15f, "Nächster Tag beginnt nicht");
            _autoCloseModals = true;
            _step.Note("Tag " + sim.Day + " hat begonnen, " + Fmt.Clock(sim.TimeMinutes) + " Uhr");
            yield return Wait(0.8f);
            yield return Shot("naechster_tag");
        }

        /// <summary>Klickt einen Knopf (Text enthält <paramref name="text"/>, sonst erster mit <paramref name="cls"/>).</summary>
        private static bool ClickButton(VisualElement root, string text, string cls)
        {
            var all = new List<Button>();
            Collect(root, all);
            Button pick = all.Find(b => b.enabledInHierarchy && HasText(b, text));
            if (pick == null && cls != null) pick = all.Find(b => b.enabledInHierarchy && b.ClassListContains(cls));
            if (pick == null) return false;
            return Click(pick);
        }

        private static void Collect(VisualElement e, List<Button> into)
        {
            if (e == null) return;
            if (e is Button b) into.Add(b);
            foreach (var c in e.Children()) Collect(c, into);
        }

        /// <summary>Löst einen UI-Toolkit-Knopf aus wie ein Mausklick (Clickable.clicked).</summary>
        private static bool Click(Button b)
        {
            var clickable = b.clickable;
            if (clickable != null)
            {
                var field = typeof(Clickable).GetField("clicked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null && field.GetValue(clickable) is Action act)
                {
                    act();
                    return true;
                }
            }
            using (var e = NavigationSubmitEvent.GetPooled())
            {
                e.target = b;
                b.SendEvent(e);
            }
            return true;
        }

        // =====================================================================================
        // 21. Leistung
        // =====================================================================================
        private IEnumerator StepPerf()
        {
            var sim = Game.Sim;
            var p = Game.Player;
            sim.AdminTimeScale = 1f; // Uhr läuft wieder: Stadt, Verkehr und Simulation wie im echten Spiel
            TeleportOutside(p, new Vector3(-4f, 0.05f, -4.6f), 90f);
            yield return Wait(1f);
            var start = p.transform.position;
            // Geradeaus laufen; wer hängen bleibt (Laterne, Bank …), dreht ab wie ein Spaziergänger.
            var last = start;
            float checkAt = Now + 0.8f;
            int turns = 0;
            GameInput.TestMove = new Vector2(0f, 1f);
            yield return Sample(20f, "20 s Spaziergang (Hauptstraße)", () =>
            {
                if (Now < checkAt) return;
                checkAt = Now + 0.8f;
                var pos = p.transform.position;
                if ((pos - last).sqrMagnitude < 0.36f)
                {
                    p.Yaw += turns % 2 == 0 ? 110f : -150f;
                    turns++;
                }
                last = pos;
            });
            GameInput.TestMove = null;
            if (turns > 0) _step.Note("Beim Spaziergang " + turns + "× abgebogen (Hindernis)");
            sim.AdminTimeScale = 0f;
            var perf = _lastPerf;
            Report.Perf = perf;
            float walked = Vector3.Distance(start, p.transform.position);
            _step.Note(perf.Text());
            _step.Note("Gelaufen: " + walked.ToString("0") + " m");
            Soft(walked > 10f, "Spieler kam beim Spaziergang kaum voran (" + walked.ToString("0") + " m) – Hindernis?");
            Soft(perf.AvgFps >= 30f, "Durchschnittlich unter 30 FPS.");
            Soft(perf.MinFps >= 8f, "Starke Ruckler (schlechtester Frame unter 8 FPS).");
            yield return Shot("spaziergang_ende");
        }

        // =====================================================================================
        // 22. Zurück ins Hauptmenü
        // =====================================================================================
        private IEnumerator StepFinish()
        {
            var sim = Game.Sim;
            var ui = Game.UI;
            Require(sim != null && ui != null, "Oberfläche fehlt.");
            if (sim.InGame)
            {
                Require(IsTestDir(sim.SaveDir), "Sicherheitsstopp: Spielstand-Ordner zeigt nicht auf den Test-Ordner.");
                Require(ui.Pause.OnMainMenu != null, "Pausemenü hat keinen Weg ins Hauptmenü.");
                ui.Pause.OnMainMenu();
                yield return Until(() => !sim.InGame && ui.Menu.IsOpen && !ui.Fader.Busy, 25f, "Rückkehr ins Hauptmenü hängt");
            }
            yield return Wait(0.6f);
            Check(ui.Menu.IsOpen, "Hauptmenü sichtbar", "Hauptmenü ist nicht sichtbar.");
            yield return Shot("hauptmenue_ende");
        }
    }
}
