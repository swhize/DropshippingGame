using System;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Schwebende Zahlen/Wörter in der Welt („+12 €“, „×3“, „+1 Karton“): gepoolte <see cref="Label3D"/>,
    /// ploppen mit Überschwingen auf, steigen und blenden aus. Höchstens <see cref="Capacity"/> gleichzeitig –
    /// ist der Pool voll, wird der älteste wiederverwendet.
    /// </summary>
    public static class FeelPops
    {
        private struct Pop
        {
            public Label3D Label;
            public bool Active;
            public float T, Dur, Rise, Size, Delay;
            public Vector3 Pos;
            public Color Color;
        }

        public const int Capacity = 14;
        private static readonly Pop[] Pool = new Pop[Capacity];
        private static int _next;

        public static readonly Color Money = new Color(0.42f, 0.9f, 0.5f);
        public static readonly Color Info = new Color(1f, 0.93f, 0.7f);
        public static readonly Color Muted = new Color(0.78f, 0.8f, 0.86f);
        public static readonly Color Bad = new Color(1f, 0.5f, 0.45f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            for (int i = 0; i < Capacity; i++) Pool[i] = default;
            _next = 0;
        }

        /// <summary>Zeigt einen Pop. size 1 = Standard (wie SpawnFloatText). Gibt false zurück, wenn nicht möglich.</summary>
        public static bool Spawn(Vector3 pos, string text, Color color, float size = 1f, float delay = 0f, float rise = 1.1f, float duration = 1.35f)
        {
            if (!Feel.Enabled || string.IsNullOrEmpty(text)) return false;
            try
            {
                var root = FeelRunner.Root;
                if (root == null) return false;
                int idx = -1;
                for (int i = 0; i < Capacity; i++)
                {
                    int k = (_next + i) % Capacity;
                    if (!Pool[k].Active)
                    {
                        idx = k;
                        break;
                    }
                }
                if (idx < 0) idx = _next;
                _next = (idx + 1) % Capacity;
                ref var p = ref Pool[idx];
                if (p.Label == null || !p.Label.Alive)
                {
                    p.Label = Label3D.Create(root, text, 64f, color, pos, true, 0f, true, 1.6f);
                    p.Label.gameObject.layer = 2;
                }
                else p.Label.SetText(text);
                p.Label.SetColor(color);
                p.Active = true;
                p.T = 0f;
                p.Delay = Mathf.Max(0f, delay);
                p.Dur = Mathf.Max(0.3f, duration);
                p.Rise = rise;
                p.Size = Mathf.Clamp(size, 0.3f, 3f);
                p.Pos = pos;
                p.Color = color;
                var tr = p.Label.transform;
                tr.position = pos;
                tr.localScale = Vector3.zero;
                p.Label.gameObject.SetActive(true);
                return true;
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
                return false;
            }
        }

        /// <summary>Geld-Pop: „+12 €“ grün, etwas größer, mit Münzen.</summary>
        public static bool MoneyPop(Vector3 pos, int amount, bool coins = true, float delay = 0f)
        {
            bool ok = Spawn(pos, (amount >= 0 ? "+" : "−") + Fmt.Money(Mathf.Abs(amount)), amount >= 0 ? Money : Bad, 1.15f, delay);
            if (ok && coins && amount > 0) FeelFx.Coins(pos, Mathf.Clamp(3 + amount / 12, 3, 12));
            return ok;
        }

        internal static void Tick(float dt)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (!Pool[i].Active) continue;
                ref var p = ref Pool[i];
                if (p.Label == null || !p.Label.Alive)
                {
                    p.Active = false;
                    p.Label = null;
                    continue;
                }
                if (p.Delay > 0f)
                {
                    p.Delay -= dt;
                    continue;
                }
                p.T += dt;
                float t = p.T / p.Dur;
                var tr = p.Label.transform;
                if (t >= 1f)
                {
                    p.Active = false;
                    p.Label.gameObject.SetActive(false);
                    continue;
                }
                float pop = t < 0.14f ? FeelTween.Eval(FeelEase.OutBack, t / 0.14f) : 1f;
                tr.localScale = Vector3.one * (p.Size * pop);
                tr.position = p.Pos + new Vector3(0f, p.Rise * FeelTween.Eval(FeelEase.OutCubic, t), 0f);
                p.Label.SetAlpha(t < 0.55f ? 1f : 1f - (t - 0.55f) / 0.45f);
            }
        }
    }
}
