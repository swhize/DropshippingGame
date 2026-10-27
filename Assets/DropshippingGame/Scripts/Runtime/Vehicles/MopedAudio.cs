using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame.Vehicles
{
    /// <summary>
    /// Klänge des Mopeds: Motor-Loop (Tonhöhe nach Tempo), Hupe, Rumms. Echte Aufnahmen aus dem Manifest
    /// ("moped_engine", "moped_horn", "moped_bump"), sonst synthetisiert (<see cref="VehicleSynth"/>, gecacht).
    /// </summary>
    public sealed class MopedAudio
    {
        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();

        private readonly AudioSource _engine, _fx;
        private float _vol;
        private bool _running;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clips.Clear();

        public MopedAudio(GameObject host)
        {
            _engine = host.AddComponent<AudioSource>();
            _engine.playOnAwake = false;
            _engine.loop = true;
            _engine.spatialBlend = 0.7f;
            _engine.minDistance = 2.5f;
            _engine.maxDistance = 45f;
            _engine.rolloffMode = AudioRolloffMode.Linear;
            _engine.dopplerLevel = 0f;
            _engine.volume = 0f;
            _engine.clip = Clip("moped_engine");
            _fx = host.AddComponent<AudioSource>();
            _fx.playOnAwake = false;
            _fx.spatialBlend = 0.5f;
            _fx.minDistance = 3f;
            _fx.maxDistance = 50f;
            _fx.dopplerLevel = 0f;
        }

        public static AudioClip Clip(string name)
        {
            if (Clips.TryGetValue(name, out var c) && c != null) return c;
            try
            {
                c = AssetLib.Sfx(name);
            }
            catch (System.Exception)
            {
                c = null;
            }
            if (c == null)
            {
                try
                {
                    var data = VehicleSynth.Render(name);
                    c = AudioClip.Create(name, data.Length, 1, AudioSynth.Rate, false);
                    c.SetData(data, 0);
                }
                catch (System.Exception e)
                {
                    SafeLog.Warn("moped.audio", e);
                    c = null;
                }
            }
            Clips[name] = c;
            return c;
        }

        /// <summary>Motor an/aus (blendet weich).</summary>
        public void SetRunning(bool on)
        {
            _running = on;
            if (on && _engine.clip != null && !_engine.isPlaying)
            {
                _engine.time = Random.Range(0f, 0.9f);
                _engine.Play();
            }
        }

        /// <summary>Pro Bild: Tonhöhe und Lautstärke aus Tempo (0..1) und Gas (0..1). paused = Pausemenü.</summary>
        public void Tick(float dt, float speed01, float throttle, bool paused, bool inHead)
        {
            if (_engine == null) return;
            if (paused)
            {
                if (_engine.isPlaying) _engine.Pause();
                return;
            }
            float target = _running ? 0.32f + 0.22f * throttle + 0.18f * speed01 : 0f;
            _vol = Mathf.MoveTowards(_vol, target, dt * (_running ? 1.6f : 0.9f));
            if (_vol <= 0.001f && !_running)
            {
                if (_engine.isPlaying) _engine.Stop();
                return;
            }
            if (!_engine.isPlaying && _engine.clip != null) _engine.UnPause();
            if (!_engine.isPlaying && _engine.clip != null) _engine.Play();
            _engine.spatialBlend = inHead ? 0.15f : 0.75f;
            _engine.volume = _vol * Settings.SfxVolume;
            _engine.pitch = Mathf.Lerp(_engine.pitch <= 0.01f ? 1f : _engine.pitch, VehicleRules.EnginePitch(speed01, throttle), 1f - Mathf.Exp(-dt * 8f));
        }

        public void Play(string name, float volume = 1f, float pitchVar = 0.04f)
        {
            var c = Clip(name);
            if (c == null || _fx == null) return;
            _fx.pitch = 1f + Random.Range(-pitchVar, pitchVar);
            _fx.PlayOneShot(c, Mathf.Clamp01(volume) * Settings.SfxVolume);
        }
    }
}
