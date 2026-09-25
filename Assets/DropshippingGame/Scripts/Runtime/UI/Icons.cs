using System;
using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Zeichnet alle Oberflächen-Symbole per Code (weiß auf transparent, einfärbbar per
    /// -unity-background-image-tint-color). Koordinaten im 24er-Raster wie SVG-Icons,
    /// Strichstärke 2. Ersetzt die Emojis aus der Godot-Version.
    /// </summary>
    public static class Icons
    {
        private const int Size = 64;
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Cache.Clear();

        private abstract class Prim
        {
            /// <summary>Deckkraft 0..1 an Punkt (x, y) im 24er-Raster; aa = Breite eines Pixels.</summary>
            public abstract float Alpha(float x, float y, float aa);
        }

        private sealed class Seg : Prim
        {
            public float X1, Y1, X2, Y2, W = 2f;

            public override float Alpha(float x, float y, float aa)
            {
                float dx = X2 - X1, dy = Y2 - Y1;
                float l2 = dx * dx + dy * dy;
                float t = l2 > 0 ? Mathf.Clamp01(((x - X1) * dx + (y - Y1) * dy) / l2) : 0f;
                float px = X1 + t * dx - x, py = Y1 + t * dy - y;
                float d = Mathf.Sqrt(px * px + py * py);
                return Mathf.Clamp01((W / 2f - d) / aa + 0.5f);
            }
        }

        private sealed class Ring : Prim
        {
            public float Cx, Cy, R, W = 2f, A0 = -1000f, A1 = 1000f;

            public override float Alpha(float x, float y, float aa)
            {
                float dx = x - Cx, dy = y - Cy;
                if (A0 > -999f)
                {
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (!InArc(ang)) return 0f;
                }
                float d = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - R);
                return Mathf.Clamp01((W / 2f - d) / aa + 0.5f);
            }

            private bool InArc(float ang)
            {
                float a = Mathf.Repeat(ang - A0, 360f);
                float span = Mathf.Repeat(A1 - A0, 360f);
                if (span < 0.01f) span = 360f;
                return a <= span;
            }
        }

        private sealed class Disc : Prim
        {
            public float Cx, Cy, R;

            public override float Alpha(float x, float y, float aa)
            {
                float d = Mathf.Sqrt((x - Cx) * (x - Cx) + (y - Cy) * (y - Cy)) - R;
                return Mathf.Clamp01(-d / aa + 0.5f);
            }
        }

        private sealed class RRect : Prim
        {
            public float X, Y, W, H, R, Stroke = 2f;
            public bool Fill;

            public override float Alpha(float x, float y, float aa)
            {
                float cx = X + W / 2f, cy = Y + H / 2f;
                float qx = Mathf.Abs(x - cx) - (W / 2f - R), qy = Mathf.Abs(y - cy) - (H / 2f - R);
                float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                float sd = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - R;
                if (Fill) return Mathf.Clamp01(-sd / aa + 0.5f);
                return Mathf.Clamp01((Stroke / 2f - Mathf.Abs(sd)) / aa + 0.5f);
            }
        }

        private sealed class Poly : Prim
        {
            public Vector2[] P;

            public override float Alpha(float x, float y, float aa)
            {
                bool inside = false;
                float minD = float.MaxValue;
                for (int i = 0, j = P.Length - 1; i < P.Length; j = i++)
                {
                    var a = P[i];
                    var b = P[j];
                    if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                    var ab = b - a;
                    float t = Mathf.Clamp01(Vector2.Dot(new Vector2(x, y) - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    minD = Mathf.Min(minD, (a + ab * t - new Vector2(x, y)).magnitude);
                }
                float sd = inside ? -minD : minD;
                return Mathf.Clamp01(-sd / aa + 0.5f);
            }
        }

        private sealed class Builder
        {
            public readonly List<Prim> Prims = new List<Prim>();

            public Builder L(float x1, float y1, float x2, float y2, float w = 2f)
            {
                Prims.Add(new Seg { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, W = w });
                return this;
            }

            /// <summary>Linienzug aus Punktpaaren.</summary>
            public Builder P(params float[] xy)
            {
                for (int i = 0; i + 3 < xy.Length; i += 2) L(xy[i], xy[i + 1], xy[i + 2], xy[i + 3]);
                return this;
            }

            public Builder C(float cx, float cy, float r, float w = 2f)
            {
                Prims.Add(new Ring { Cx = cx, Cy = cy, R = r, W = w });
                return this;
            }

            public Builder A(float cx, float cy, float r, float a0, float a1, float w = 2f)
            {
                Prims.Add(new Ring { Cx = cx, Cy = cy, R = r, W = w, A0 = a0, A1 = a1 });
                return this;
            }

            public Builder D(float cx, float cy, float r)
            {
                Prims.Add(new Disc { Cx = cx, Cy = cy, R = r });
                return this;
            }

            public Builder R(float x, float y, float w, float h, float r = 1.5f, bool fill = false)
            {
                Prims.Add(new RRect { X = x, Y = y, W = w, H = h, R = r, Fill = fill });
                return this;
            }

            public Builder F(params float[] xy)
            {
                var pts = new Vector2[xy.Length / 2];
                for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
                Prims.Add(new Poly { P = pts });
                return this;
            }
        }

        private static readonly Dictionary<string, Action<Builder>> Defs = new Dictionary<string, Action<Builder>>
        {
            { "home", b => b.P(3, 11, 12, 4, 21, 11).P(5.5f, 9.5f, 5.5f, 20, 18.5f, 20, 18.5f, 9.5f).P(10, 20, 10, 15, 14, 15, 14, 20) },
            { "mail", b => b.R(3, 5, 18, 14).P(4, 7, 12, 13, 20, 7) },
            { "cart", b => b.P(2.5f, 4, 4.8f, 4, 7.2f, 14.5f, 17.5f, 14.5f, 20, 8, 6, 8).D(9, 19, 1.6f).D(17, 19, 1.6f) },
            { "box", b => b.P(3, 7.5f, 12, 3, 21, 7.5f, 21, 16.5f, 12, 21, 3, 16.5f, 3, 7.5f).P(3, 7.5f, 12, 12, 21, 7.5f).L(12, 12, 12, 21) },
            { "package", b => b.R(3.5f, 7, 17, 13, 1.5f).L(3.5f, 11, 20.5f, 11).P(10, 7, 10, 14, 14, 14, 14, 7).L(7, 3.5f, 17, 3.5f) },
            { "globe", b => b.C(12, 12, 9).L(3, 12, 21, 12).A(12, 12, 9, -90, 90).A(19.5f, 12, 10.5f, 145, 215).A(4.5f, 12, 10.5f, -35, 35) },
            { "mega", b => b.P(3, 10, 3, 14, 6, 14, 13, 18.5f, 13, 5.5f, 6, 10, 3, 10).A(14.5f, 12, 3, -60, 60).A(14.5f, 12, 6, -55, 55) },
            { "tag", b => b.P(3, 12, 3, 4, 11, 4, 21, 14, 14, 21, 3, 12).D(7.5f, 8, 1.7f) },
            { "store", b => b.P(4, 9.5f, 5.5f, 4, 18.5f, 4, 20, 9.5f).L(4, 9.5f, 20, 9.5f).A(7, 9.5f, 3, 0, 180).A(12, 9.5f, 2, 0, 180).A(17, 9.5f, 3, 0, 180).P(5.5f, 12.5f, 5.5f, 20, 18.5f, 20, 18.5f, 12.5f) },
            { "trend", b => b.P(3, 17, 9, 11, 13, 15, 21, 7).P(15, 7, 21, 7, 21, 13) },
            { "trend_down", b => b.P(3, 7, 9, 13, 13, 9, 21, 17).P(15, 17, 21, 17, 21, 11) },
            { "users", b => b.C(9, 8, 3.5f).A(9, 20, 6.5f, 180, 360).A(15.5f, 8, 3.5f, -90, 90).A(18, 20, 5, 250, 360) },
            { "user", b => b.C(12, 8, 4).A(12, 21, 7.5f, 180, 360) },
            { "building", b => b.L(3.5f, 20.5f, 20.5f, 20.5f).P(5, 20.5f, 5, 9.5f, 12, 5, 19, 9.5f, 19, 20.5f).P(9.5f, 20.5f, 9.5f, 14.5f, 14.5f, 14.5f, 14.5f, 20.5f) },
            { "bank", b => b.P(3, 9, 12, 4, 21, 9).L(5, 10.5f, 5, 17.5f).L(9.7f, 10.5f, 9.7f, 17.5f).L(14.3f, 10.5f, 14.3f, 17.5f).L(19, 10.5f, 19, 17.5f).L(3, 20, 21, 20) },
            { "bars", b => b.L(5, 20, 5, 12).L(11, 20, 11, 5).L(17, 20, 17, 15).L(3, 20, 21, 20) },
            { "trophy", b => b.P(8, 4, 16, 4, 16, 9).A(12, 9, 4, 0, 180).L(8, 9, 8, 4).A(8, 7.5f, 3, 90, 270).A(16, 7.5f, 3, -90, 90).L(12, 13, 12, 17).L(8.5f, 20, 15.5f, 20).L(9.5f, 17, 14.5f, 17) },
            { "lock", b => b.R(5, 10.5f, 14, 10).A(12, 7.5f, 4, 180, 360).L(8, 7.5f, 8, 10.5f).L(16, 7.5f, 16, 10.5f) },
            { "check", b => b.P(4.5f, 12.5f, 9.5f, 17.5f, 19.5f, 6.5f) },
            { "close", b => b.L(6, 6, 18, 18).L(18, 6, 6, 18) },
            { "plus", b => b.L(12, 5, 12, 19).L(5, 12, 19, 12) },
            { "minus", b => b.L(5, 12, 19, 12) },
            { "star", b => b.F(12, 2.8f, 14.8f, 8.9f, 21.4f, 9.6f, 16.4f, 14, 17.8f, 20.6f, 12, 17.3f, 6.2f, 20.6f, 7.6f, 14, 2.6f, 9.6f, 9.2f, 8.9f) },
            { "star_o", b => b.P(12, 3.5f, 14.5f, 9.2f, 20.6f, 9.8f, 16, 13.9f, 17.3f, 20, 12, 16.9f, 6.7f, 20, 8, 13.9f, 3.4f, 9.8f, 9.5f, 9.2f, 12, 3.5f) },
            { "gear", b => b.C(12, 12, 3).C(12, 12, 7.5f, 2.5f).L(12, 2.5f, 12, 5).L(12, 19, 12, 21.5f).L(2.5f, 12, 5, 12).L(19, 12, 21.5f, 12).L(5.3f, 5.3f, 7, 7).L(17, 17, 18.7f, 18.7f).L(5.3f, 18.7f, 7, 17).L(17, 7, 18.7f, 5.3f) },
            { "play", b => b.F(7, 4.5f, 19, 12, 7, 19.5f) },
            { "pause", b => b.R(6, 5, 4, 14, 1, true).R(14, 5, 4, 14, 1, true) },
            { "warning", b => b.P(12, 3.5f, 21.5f, 20, 2.5f, 20, 12, 3.5f).L(12, 9.5f, 12, 14).D(12, 17, 1.2f) },
            { "heart", b => b.D(8.3f, 9.3f, 4.3f).D(15.7f, 9.3f, 4.3f).F(4.2f, 11, 19.8f, 11, 12, 20.5f) },
            { "clock", b => b.C(12, 12, 9).P(12, 7, 12, 12, 15.5f, 14) },
            { "truck", b => b.R(2.5f, 6, 11.5f, 10, 1).P(14, 9, 18, 9, 21.5f, 12.5f, 21.5f, 16, 14, 16).D(7, 17.5f, 2).D(17, 17.5f, 2) },
            { "bolt", b => b.F(13.5f, 2.5f, 5, 13.5f, 11, 13.5f, 10, 21.5f, 19, 10, 13, 10) },
            { "coin", b => b.C(12, 12, 9).L(12, 6.5f, 12, 17.5f).A(12, 10, 2.8f, 90, 330).A(12, 14, 2.8f, 270, 150) },
            { "search", b => b.C(10.5f, 10.5f, 6.5f).L(15.5f, 15.5f, 20.5f, 20.5f) },
            { "thumb", b => b.P(7, 11, 7, 20, 4, 20, 4, 11, 7, 11, 11, 3.5f, 13, 4.5f, 12.5f, 9.5f, 19, 9.5f, 20.5f, 11.5f, 18.5f, 19, 16.5f, 20, 7, 20) },
            { "paper", b => b.P(6, 3, 14, 3, 19, 8, 19, 21, 6, 21, 6, 3).P(14, 3, 14, 8, 19, 8).L(9, 12, 16, 12).L(9, 16, 16, 16) },
            { "phone", b => b.R(7, 2.5f, 10, 19, 2).L(10.5f, 18, 13.5f, 18) },
            { "bulb", b => b.A(12, 9.5f, 6, 150, 390).P(8.5f, 14.5f, 8.5f, 17, 15.5f, 17, 15.5f, 14.5f).L(9.5f, 20, 14.5f, 20) },
            { "drop", b => b.A(12, 14.5f, 6, -30, 210).P(6.8f, 11.5f, 12, 3, 17.2f, 11.5f) },
            { "ring", b => b.C(12, 10.5f, 7).C(12, 10.5f, 4.5f, 1.4f).L(12, 17.5f, 12, 21).L(8.5f, 21, 15.5f, 21) },
            { "headphones", b => b.A(12, 13, 8, 180, 360).R(3, 13, 4.5f, 7, 1.5f).R(16.5f, 13, 4.5f, 7, 1.5f) },
            { "screen", b => b.R(3, 5, 18, 12, 1.5f).L(9, 20, 15, 20).L(12, 17, 12, 20) },
            { "drone", b => b.R(9, 10, 6, 4, 1).L(9, 11, 5, 7).L(15, 11, 19, 7).L(9, 13, 5, 17).L(15, 13, 19, 17).C(5, 7, 2.2f, 1.5f).C(19, 7, 2.2f, 1.5f).C(5, 17, 2.2f, 1.5f).C(19, 17, 2.2f, 1.5f) },
            { "fire", b => b.P(12, 21, 7.5f, 18, 6.5f, 13, 9, 9, 10, 12, 12, 3, 16, 9, 17.5f, 14, 16.5f, 18, 12, 21) },
            { "angry", b => b.C(12, 12, 9).L(8, 8.5f, 10.5f, 10).L(16, 8.5f, 13.5f, 10).A(12, 18.5f, 3.5f, 200, 340) },
            { "rocket", b => b.P(12, 2.5f, 16, 8, 16, 16, 8, 16, 8, 8, 12, 2.5f).P(8, 12, 5, 16, 8, 16).P(16, 12, 19, 16, 16, 16).L(10.5f, 19, 13.5f, 19).C(12, 10, 1.5f, 1.4f) },
            { "mic", b => b.R(9, 3, 6, 11, 3).A(12, 11, 6, 0, 180).L(12, 17, 12, 21).L(9, 21, 15, 21) },
            { "eye", b => b.A(12, 17, 9.5f, 215, 325).A(12, 7, 9.5f, 35, 145).C(12, 12, 2.8f) },
            { "shield", b => b.P(12, 3, 19.5f, 6, 19.5f, 11.5f).A(3.5f, 11.5f, 16, -50, 0).P(4.5f, 11.5f, 4.5f, 6, 12, 3).A(20.5f, 11.5f, 16, 180, 230) },
            { "factory", b => b.P(3, 20.5f, 3, 10, 8, 13, 8, 10, 13, 13, 13, 5, 17, 5, 17, 20.5f, 3, 20.5f).L(17, 20.5f, 21, 20.5f).L(21, 20.5f, 21, 9) },
            { "bag", b => b.R(4, 8, 16, 13, 2).A(12, 8, 3.5f, 180, 360) },
            { "music", b => b.P(9, 17.5f, 9, 5, 19, 3, 19, 15.5f).D(7, 17.5f, 2.3f).D(17, 15.5f, 2.3f) },
            { "chat", b => b.R(3, 4, 18, 13, 3).P(8, 17, 7, 21, 12, 17) },
            { "gamepad", b => b.R(2.5f, 7, 19, 11, 5).L(7, 10.5f, 7, 14.5f).L(5, 12.5f, 9, 12.5f).D(15.5f, 11, 1.2f).D(17.5f, 14, 1.2f) },
            { "server", b => b.R(4, 3.5f, 16, 7, 1.5f).R(4, 13.5f, 16, 7, 1.5f).D(8, 7, 1.1f).D(8, 17, 1.1f) },
            { "rack", b => b.L(4, 3, 4, 21).L(20, 3, 20, 21).L(4, 8, 20, 8).L(4, 14, 20, 14).L(4, 20, 20, 20) },
            { "calendar", b => b.R(3.5f, 5, 17, 16, 2).L(3.5f, 10, 20.5f, 10).L(8, 3, 8, 7).L(16, 3, 16, 7) },
            { "arrow_right", b => b.L(4, 12, 20, 12).P(14, 6, 20, 12, 14, 18) },
            { "chevron", b => b.P(9, 5, 16, 12, 9, 19) },
            { "save", b => b.P(4, 4, 16, 4, 20, 8, 20, 20, 4, 20, 4, 4).R(7.5f, 13, 9, 7, 1).L(8, 4, 8, 8.5f).L(8, 8.5f, 15, 8.5f).L(15, 8.5f, 15, 4) },
            { "exit", b => b.P(10, 4, 4, 4, 4, 20, 10, 20).L(9, 12, 20, 12).P(16, 8, 20, 12, 16, 16) },
            { "help", b => b.C(12, 12, 9).A(12, 9.5f, 3, 180, 450).L(12, 12.5f, 12, 14).D(12, 17, 1.2f) },
            { "sun", b => b.C(12, 12, 4.5f).L(12, 2, 12, 4).L(12, 20, 12, 22).L(2, 12, 4, 12).L(20, 12, 22, 12).L(4.9f, 4.9f, 6.3f, 6.3f).L(17.7f, 17.7f, 19.1f, 19.1f).L(4.9f, 19.1f, 6.3f, 17.7f).L(17.7f, 6.3f, 19.1f, 4.9f) },
            { "moon", b => b.A(12, 12, 8.5f, 60, 330).A(17.5f, 7.5f, 7, 110, 205) },
            { "list", b => b.L(9, 6, 20, 6).L(9, 12, 20, 12).L(9, 18, 20, 18).D(5, 6, 1.3f).D(5, 12, 1.3f).D(5, 18, 1.3f) },
            { "dot", b => b.D(12, 12, 5) },
            { "sparkle", b => b.F(12, 2, 14, 10, 22, 12, 14, 14, 12, 22, 10, 14, 2, 12, 10, 10) },
            { "wallet", b => b.R(3, 6, 18, 14, 2).P(3, 9, 17, 9).R(14, 11.5f, 7, 5, 1.5f).D(17, 14, 1) },
            { "brush", b => b.P(20, 3.5f, 11, 12.5f).P(11, 12.5f, 13.5f, 15).A(8.5f, 16.5f, 3.5f, 30, 200).P(5.2f, 17.7f, 4, 20.5f, 7.5f, 19.5f) },
            { "keyboard", b => b.R(2.5f, 6.5f, 19, 11, 2).L(6, 10, 7, 10).L(10, 10, 11, 10).L(14, 10, 15, 10).L(18, 10, 18.5f, 10).L(7, 14, 17, 14) },
            { "photo", b => b.R(3, 5, 18, 14, 2).P(3, 16, 8.5f, 11, 13, 15.5f, 16, 13, 21, 17).D(16, 9, 1.6f) },
            { "stand", b => b.P(3, 9, 5, 4, 19, 4, 21, 9, 3, 9).L(5, 9, 5, 20).L(19, 9, 19, 20).L(3, 14, 21, 14).L(3, 20, 21, 20) },
        };

        public static bool Has(string name) => Defs.ContainsKey(name);

        public static Texture2D Get(string name)
        {
            if (string.IsNullOrEmpty(name) || !Defs.ContainsKey(name)) name = "dot";
            if (Cache.TryGetValue(name, out var tex) && tex != null) return tex;
            var b = new Builder();
            Defs[name](b);
            var px = new Color32[Size * Size];
            float aa = 24f / Size;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size * 24f;
                    float v = 24f - (y + 0.5f) / Size * 24f;
                    float a = 0f;
                    foreach (var p in b.Prims)
                    {
                        a = Mathf.Max(a, p.Alpha(u, v, aa));
                        if (a >= 1f) break;
                    }
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true, false)
            {
                name = "icon_" + name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
            };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            Cache[name] = tex;
            return tex;
        }
    }
}
