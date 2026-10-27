using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Verpeilte Gangarten für Passanten (harmlos und albern).</summary>
    public enum WalkStyle
    {
        Normal = 0,
        Wobbly = 1,   // wackelt hin und her
        ZigZag = 2,   // läuft Schlangenlinien
        Sleepy = 3,   // schlurft müde, Kopf hängt, "Zzz"
        Hyper = 4,    // rennt, hüpft
        Dancer = 5,   // tanzt beim Warten
        Phone = 6,    // starrt aufs Handy, rempelt Laternen an
    }

    /// <summary>
    /// Optik der Gangart: hängt die Figur unter einen eigenen Drehpunkt und bewegt nur diesen
    /// (die Laufwege des NPC bleiben unverändert). Wird von <see cref="StreetLife"/> verteilt.
    /// </summary>
    public sealed class GoofyWalk : MonoBehaviour
    {
        public WalkStyle Style;
        private NPC _npc;
        private Transform _pivot;
        private float _t, _say, _bonkCd, _bonk;
        private Transform _phone;

        private static readonly string[] SleepyLines = { "Zzz ...", "*gähn*", "Erst mal Kaffee ...", "Ist schon Montag?" };
        private static readonly string[] HyperLines = { "Keine Zeit!", "Juhuu!", "Schneller!", "Zu viel Espresso!" };
        private static readonly string[] WobblyLines = { "Huch!", "Wo war ich?", "Hoppla!", "Alles dreht sich ... vom Karussell!" };
        private static readonly string[] BonkLines = { "Autsch!", "Wer stellt da 'ne Laterne hin?!", "Aua! Mein Kopf!", "Entschuldigung ... Laterne." };
        private static readonly string[] DanceLines = { "Das ist mein Lied!", "♪ Dum dum dum ♪", "Party!" };

        /// <summary>Würfelt eine Gangart: ~30 % der Passanten sind verpeilt.</summary>
        public static WalkStyle Roll(System.Random rng)
        {
            if (rng == null || rng.NextDouble() > 0.3) return WalkStyle.Normal;
            return (WalkStyle)rng.Next(1, 7);
        }

        /// <summary>Tempo-Faktor für die Gangart.</summary>
        public static float SpeedFactor(WalkStyle s)
        {
            switch (s)
            {
                case WalkStyle.Sleepy: return 0.5f;
                case WalkStyle.Hyper: return 2.0f;
                case WalkStyle.Phone: return 0.8f;
                case WalkStyle.Wobbly: return 0.85f;
                default: return 1f;
            }
        }

        public static GoofyWalk Attach(NPC npc, WalkStyle style)
        {
            if (npc == null || style == WalkStyle.Normal) return null;
            var g = npc.gameObject.AddComponent<GoofyWalk>();
            g.Init(npc, style);
            return g;
        }

        private void Init(NPC npc, WalkStyle style)
        {
            _npc = npc;
            Style = style;
            Transform visual = npc.Model != null ? npc.Model.transform : (npc.Rig != null ? npc.Rig.Root : null);
            if (visual == null || visual.parent != transform) return;
            _pivot = new GameObject("GoofyPivot").transform;
            _pivot.SetParent(transform, false);
            visual.SetParent(_pivot, false);
            _t = Random.value * 10f;
            _say = Random.Range(4f, 15f);
            if (style == WalkStyle.Phone)
            {
                _phone = Props.Box(_pivot, new Vector3(0.08f, 0.14f, 0.015f), Mats.Emit(new Color(0.5f, 0.75f, 1f), 1.5f), new Vector3(0.12f, 1.1f, 0.3f), new Vector3(-40f, 0, 0), 0f, false).transform;
            }
        }

        /// <summary>Kurz tanzen (z. B. als Reaktion auf das Spieler-Emote).</summary>
        public void DanceFor(float seconds)
        {
            _dance = Mathf.Max(_dance, seconds);
        }

        private float _dance;

        private void Update()
        {
            if (_pivot == null || _npc == null) return;
            float dt = Time.deltaTime;
            _t += dt;
            _say -= dt;
            _bonkCd -= dt;
            _dance -= dt;
            bool walking = _npc.IsWalking;
            Vector3 pos = Vector3.zero;
            Vector3 rot = Vector3.zero;
            if (_dance > 0f || (Style == WalkStyle.Dancer && !walking))
            {
                pos.y = Mathf.Abs(Mathf.Sin(_t * 6f)) * 0.12f;
                rot.y = Mathf.Sin(_t * 3f) * 35f;
                rot.z = Mathf.Sin(_t * 6f) * 8f;
                if (_say <= 0f) { _say = Random.Range(10f, 20f); _npc.Say(DanceLines[Random.Range(0, DanceLines.Length)], 2.5f); }
            }
            else switch (Style)
            {
                case WalkStyle.Wobbly:
                    rot.z = Mathf.Sin(_t * 2.3f) * 14f + Mathf.Sin(_t * 5.1f) * 4f;
                    pos.x = Mathf.Sin(_t * 1.7f) * 0.25f;
                    if (_say <= 0f) { _say = Random.Range(12f, 25f); _npc.Say(WobblyLines[Random.Range(0, WobblyLines.Length)], 2.5f); }
                    break;
                case WalkStyle.ZigZag:
                    if (walking)
                    {
                        pos.x = Mathf.Sin(_t * 2.6f) * 0.55f;
                        rot.y = Mathf.Cos(_t * 2.6f) * 30f;
                    }
                    break;
                case WalkStyle.Sleepy:
                    rot.x = 12f + Mathf.Sin(_t * 0.8f) * 6f;
                    pos.y = -0.04f;
                    if (_say <= 0f) { _say = Random.Range(8f, 18f); _npc.Say(SleepyLines[Random.Range(0, SleepyLines.Length)], 3f); }
                    break;
                case WalkStyle.Hyper:
                    if (walking) pos.y = Mathf.Abs(Mathf.Sin(_t * 9f)) * 0.18f;
                    else rot.y = Mathf.Sin(_t * 10f) * 12f;
                    if (_say <= 0f) { _say = Random.Range(8f, 16f); _npc.Say(HyperLines[Random.Range(0, HyperLines.Length)], 2f); }
                    break;
                case WalkStyle.Phone:
                    rot.x = 10f;
                    if (walking && _bonkCd <= 0f) CheckBonk();
                    break;
            }
            if (_bonk > 0f)
            {
                _bonk -= dt;
                rot.x -= Mathf.Sin(_bonk * 12f) * 18f * _bonk;
                pos.z -= 0.15f * _bonk;
            }
            _pivot.localPosition = Vector3.Lerp(_pivot.localPosition, pos, Mathf.Min(1f, dt * 10f));
            _pivot.localRotation = Quaternion.Slerp(_pivot.localRotation, Quaternion.Euler(rot), Mathf.Min(1f, dt * 8f));
        }

        private void CheckBonk()
        {
            Vector3 p = transform.position;
            foreach (var l in CityServices.LampPoints)
            {
                Vector3 d = l - p;
                d.y = 0f;
                if (d.sqrMagnitude < 1.1f * 1.1f && Vector3.Dot(d, transform.forward) > 0f)
                {
                    _bonkCd = 15f;
                    _bonk = 1f;
                    _npc.Stop(1.6f);
                    _npc.Say(BonkLines[Random.Range(0, BonkLines.Length)], 2.5f);
                    if (Game.Audio != null) Game.Audio.PlayAt("metal_clank", l, -10f);
                    return;
                }
            }
            _bonkCd = 0.3f;
        }
    }
}
