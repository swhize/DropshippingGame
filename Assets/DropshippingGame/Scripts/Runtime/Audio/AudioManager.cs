using System.Collections.Generic;
using System.Threading;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Audio: alle Soundeffekte und Musikstücke werden beim Start per Code synthetisiert
    /// (<see cref="AudioSynth"/>, im Hintergrund-Thread) - keine Audiodateien im Projekt.
    /// Musik läuft mit Überblendung, im Pausemenü klingt sie gedämpft "durch die Wand".
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> _sfx = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioClip> _music = new Dictionary<string, AudioClip>();
        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private readonly AudioSource[] _musicSources = new AudioSource[2];
        private AudioLowPassFilter[] _lowpass = new AudioLowPassFilter[2];
        private int _active;
        private string _current = "";
        private string _wanted = "";
        private readonly float[] _musicVol = { 0f, 0f };
        private float _muffle;
        private float _muffleTarget;

        private readonly object _lock = new object();
        private readonly List<(string name, float[] data, bool music)> _pending = new List<(string, float[], bool)>();
        private Thread _thread;
        private volatile bool _abort;

        public static AudioManager Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<AudioManager>();
            a.Init();
            return a;
        }

        private void Init()
        {
            for (int i = 0; i < 16; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                _pool.Add(s);
            }
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Music" + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.loop = true;
                s.playOnAwake = false;
                s.volume = 0f;
                _musicSources[i] = s;
                _lowpass[i] = go.AddComponent<AudioLowPassFilter>();
                _lowpass[i].cutoffFrequency = 22000f;
            }
            ApplyVolumes();
            Settings.Changed += ApplyVolumes;
            _thread = new Thread(GenerateAll) { IsBackground = true, Name = "AudioSynth" };
            _thread.Start();
        }

        private void OnDestroy()
        {
            _abort = true;
            Settings.Changed -= ApplyVolumes;
        }

        private void GenerateAll()
        {
            // Zuerst die Musik fürs Menü, dann die Effekte, dann der Rest.
            var order = new List<(string, bool)> { ("menu", true) };
            foreach (var n in AudioSynth.SfxNames) order.Add((n, false));
            foreach (var t in AudioSynth.MusicNames)
                if (t != "menu") order.Add((t, true));
            foreach (var (name, music) in order)
            {
                if (_abort) return;
                float[] data;
                try
                {
                    data = AudioSynth.Render(name, music);
                }
                catch (System.Exception)
                {
                    continue;
                }
                lock (_lock) _pending.Add((name, data, music));
            }
        }

        public bool IsReady => _sfx.Count >= AudioSynth.SfxNames.Length;

        private void ApplyVolumes()
        {
            AudioListener.volume = Settings.MasterVolume;
            foreach (var s in _pool) s.volume = Settings.SfxVolume;
        }

        public void Play(string name, float pitchVar = 0.06f, float volumeDb = 0f)
        {
            if (!_sfx.TryGetValue(name, out var clip)) return;
            var s = FreeSource();
            s.spatialBlend = 0f;
            s.pitch = 1f + Random.Range(-pitchVar, pitchVar);
            s.volume = Settings.SfxVolume * Mathf.Pow(10f, volumeDb / 20f);
            s.clip = clip;
            s.Play();
        }

        /// <summary>Räumlicher Klang an einer Position (Lieferwagen, Tor).</summary>
        public void PlayAt(string name, Vector3 pos, float volumeDb = 0f)
        {
            if (!_sfx.TryGetValue(name, out var clip)) return;
            AudioSource.PlayClipAtPoint(clip, pos, Settings.SfxVolume * Mathf.Pow(10f, volumeDb / 20f));
        }

        private AudioSource FreeSource()
        {
            foreach (var s in _pool)
                if (!s.isPlaying) return s;
            return _pool[0];
        }

        public void PlayMusic(string track)
        {
            _wanted = track;
            if (_music.ContainsKey(track)) CrossfadeTo(track);
        }

        public void StopMusic()
        {
            _wanted = "";
            _current = "";
            _musicVol[0] = 0f;
            _musicVol[1] = 0f;
        }

        /// <summary>Dämpft die Musik (Pausemenü, Laptop).</summary>
        public void SetMuffled(bool on) => _muffleTarget = on ? 1f : 0f;

        private void CrossfadeTo(string track)
        {
            if (track == _current) return;
            _current = track;
            _active = 1 - _active;
            var s = _musicSources[_active];
            s.clip = _music[track];
            s.volume = 0f;
            s.Play();
            _musicVol[_active] = 1f;
            _musicVol[1 - _active] = 0f;
        }

        private void Update()
        {
            if (_pending.Count > 0)
            {
                List<(string name, float[] data, bool music)> items;
                lock (_lock)
                {
                    items = new List<(string, float[], bool)>(_pending);
                    _pending.Clear();
                }
                foreach (var it in items) Register(it.name, it.data, it.music);
            }
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < 2; i++)
            {
                var s = _musicSources[i];
                float target = _musicVol[i] * Settings.MusicVolume;
                s.volume = Mathf.MoveTowards(s.volume, target, dt * 0.5f);
                if (_musicVol[i] <= 0f && s.volume <= 0.001f && s.isPlaying) s.Stop();
            }
            _muffle = Mathf.MoveTowards(_muffle, _muffleTarget, dt * 2.5f);
            float cutoff = Mathf.Lerp(22000f, 750f, _muffle);
            _lowpass[0].cutoffFrequency = cutoff;
            _lowpass[1].cutoffFrequency = cutoff;
        }

        private void Register(string name, float[] data, bool music)
        {
            var clip = AudioClip.Create(name, data.Length, 1, AudioSynth.Rate, false);
            clip.SetData(data, 0);
            if (music)
            {
                _music[name] = clip;
                if (_wanted == name) CrossfadeTo(name);
            }
            else _sfx[name] = clip;
        }
    }
}
