using System.Collections.Generic;
using System.Threading;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Audio: echte Aufnahmen aus Resources/Audio (über <see cref="AssetLib"/>, zufällige Varianten) mit
    /// synthetisiertem Fallback (<see cref="AudioSynth"/>, im Hintergrund-Thread).
    /// Musik läuft mit Überblendung (Arbeitsmusik rotiert work_1..3, abends "evening"), im Pausemenü klingt sie
    /// gedämpft "durch die Wand". Umgebungsgeräusche je Ort (Straße, Imbiss, Halle, Park, Nacht) werden
    /// überblendet; Schritte klingen je nach Untergrund.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> _sfx = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioClip> _music = new Dictionary<string, AudioClip>();
        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private readonly AudioSource[] _musicSources = new AudioSource[2];
        private readonly AudioLowPassFilter[] _lowpass = new AudioLowPassFilter[2];
        private readonly AudioSource[] _ambSources = new AudioSource[2];
        private readonly float[] _ambVol = { 0f, 0f };
        private int _ambActive;
        private string _ambCurrent = "";
        private float _ambCheck;
        private int _active;
        private string _current = "";
        private string _wanted = "";
        private readonly float[] _musicVol = { 0f, 0f };
        private float _muffle;
        private float _muffleTarget;
        private int _workTrack;
        private float _musicCheck;

        private readonly object _lock = new object();
        private readonly List<(string name, float[] data, bool music)> _pending = new List<(string, float[], bool)>();
        private Thread _thread;
        private volatile bool _abort;

        private static readonly string[] WorkTracks = { "work_1", "work_2", "work_3" };

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

                var ago = new GameObject("Ambience" + i);
                ago.transform.SetParent(transform, false);
                var a = ago.AddComponent<AudioSource>();
                a.loop = true;
                a.playOnAwake = false;
                a.volume = 0f;
                a.spatialBlend = 0f;
                _ambSources[i] = a;
            }
            _workTrack = Random.Range(0, WorkTracks.Length);
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
            // Zuerst die Musik fürs Menü, dann die Effekte, dann der Rest (alles nur Fallback für fehlende Dateien).
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

        // =============================================================================================
        // Effekte
        // =============================================================================================

        /// <summary>Clip zu einem Effektnamen: Datei (zufällige Variante) vor Synthese. "step" je nach Untergrund.</summary>
        private AudioClip SfxClip(string name, Vector3? where)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (name == "step")
            {
                string surf = SurfaceAt(where ?? ListenerPos());
                var sc = SafeSfx("step_" + surf);
                if (sc != null) return sc;
            }
            var clip = SafeSfx(name);
            if (clip != null) return clip;
            _sfx.TryGetValue(name, out clip);
            return clip;
        }

        private static AudioClip SafeSfx(string name)
        {
            try
            {
                return AssetLib.Sfx(name);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public void Play(string name, float pitchVar = 0.06f, float volumeDb = 0f)
        {
            var clip = SfxClip(name, null);
            if (clip == null) return;
            var s = FreeSource();
            s.spatialBlend = 0f;
            s.pitch = 1f + Random.Range(-pitchVar, pitchVar);
            // Echte Schritt-Aufnahmen sind lauter als der Synth-Tick: etwas leiser abspielen.
            float extra = name == "step" ? 4f : 0f;
            s.volume = Settings.SfxVolume * Mathf.Pow(10f, (volumeDb + extra) / 20f);
            s.clip = clip;
            s.Play();
        }

        /// <summary>Räumlicher Klang an einer Position (Lieferwagen, Tor).</summary>
        public void PlayAt(string name, Vector3 pos, float volumeDb = 0f)
        {
            var clip = SfxClip(name, pos);
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, pos, Settings.SfxVolume * Mathf.Pow(10f, volumeDb / 20f));
        }

        private AudioSource FreeSource()
        {
            foreach (var s in _pool)
                if (!s.isPlaying) return s;
            return _pool[0];
        }

        private static Vector3 ListenerPos()
        {
            if (Game.Player != null) return Game.Player.transform.position;
            var cam = Camera.main;
            return cam != null ? cam.transform.position : Vector3.zero;
        }

        private static bool In(Rect r, Vector3 p) => p.x >= r.xMin && p.x <= r.xMax && p.z >= r.yMin && p.z <= r.yMax;

        /// <summary>Untergrund an einer Weltposition: tile, wood, concrete, asphalt, grass.</summary>
        public static string SurfaceAt(Vector3 p)
        {
            if (In(WorldBuilder.DinerRect, p)) return "tile";
            if (In(WorldBuilder.GarageRect, p) || In(WorldBuilder.WarehouseRect, p)) return "concrete";
            if (Mathf.Abs(p.z) < 3.9f) return "asphalt";
            if (p.z > 7.4f && Mathf.Abs(p.z - 12.5f) > 1.2f) return "grass";
            return "concrete";
        }

        // =============================================================================================
        // Musik
        // =============================================================================================

        public void PlayMusic(string track)
        {
            _wanted = track ?? "";
            _musicCheck = 0f;
            UpdateMusic(true);
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

        private static bool IsNight()
        {
            var w = Game.World;
            if (w == null || w.Atmos == null || w.MenuMode) return false;
            float h = w.Atmos.Hours;
            return h >= 19.5f || h < 6f;
        }

        /// <summary>Welches Stück gerade laufen soll ("work" = Rotation, abends "evening").</summary>
        private string ResolveTrack()
        {
            if (string.IsNullOrEmpty(_wanted)) return "";
            if ((_wanted == "work" || _wanted == "diner") && IsNight() && HasMusicFile("evening")) return "evening";
            if (_wanted == "work")
            {
                for (int i = 0; i < WorkTracks.Length; i++)
                {
                    string t = WorkTracks[(_workTrack + i) % WorkTracks.Length];
                    if (HasMusicFile(t)) return t;
                }
            }
            return _wanted;
        }

        private readonly Dictionary<string, AudioClip> _musicFiles = new Dictionary<string, AudioClip>();

        private bool HasMusicFile(string name) => MusicFile(name) != null;

        private AudioClip MusicFile(string name)
        {
            if (_musicFiles.TryGetValue(name, out var c)) return c;
            try
            {
                c = AssetLib.Music(name);
            }
            catch (System.Exception)
            {
                c = null;
            }
            _musicFiles[name] = c;
            return c;
        }

        private AudioClip MusicClip(string track)
        {
            var file = MusicFile(track);
            if (file != null) return file;
            // Synth-Fallback: work_n / evening -> "work"
            string synth = track.StartsWith("work") || track == "evening" ? "work" : track;
            _music.TryGetValue(synth, out var clip);
            return clip;
        }

        private void UpdateMusic(bool force)
        {
            string track = ResolveTrack();
            if (track.Length == 0) return;
            if (track == _current && !force) return;
            if (track == _current) return;
            var clip = MusicClip(track);
            if (clip == null) return; // Synth noch nicht fertig - später erneut
            _current = track;
            _active = 1 - _active;
            var s = _musicSources[_active];
            s.clip = clip;
            // Arbeitsmusik rotiert: nicht loopen, am Ende kommt das nächste Stück.
            s.loop = !track.StartsWith("work_");
            s.volume = 0f;
            s.Play();
            _musicVol[_active] = 1f;
            _musicVol[1 - _active] = 0f;
        }

        // =============================================================================================
        // Umgebung
        // =============================================================================================

        /// <summary>Welcher Umgebungs-Loop am Hörer passt.</summary>
        private static string AmbienceAt(Vector3 p, bool night)
        {
            if (In(WorldBuilder.DinerRect, p)) return "diner";
            if (In(WorldBuilder.GarageRect, p) || In(WorldBuilder.WarehouseRect, p)) return "warehouse";
            if (night) return "night";
            if (p.z > 8f) return "park";
            return "street";
        }

        private void UpdateAmbience(float dt)
        {
            _ambCheck -= dt;
            if (_ambCheck <= 0f)
            {
                _ambCheck = 0.5f;
                string want = Game.World != null ? AmbienceAt(ListenerPos(), IsNight()) : "";
                if (want != _ambCurrent)
                {
                    AudioClip clip = null;
                    if (want.Length > 0)
                    {
                        try
                        {
                            clip = AssetLib.Ambience(want);
                            if (clip == null && want == "park") clip = AssetLib.Ambience("street");
                            if (clip == null && want == "night") clip = AssetLib.Ambience("street");
                        }
                        catch (System.Exception)
                        {
                            clip = null;
                        }
                    }
                    _ambCurrent = want;
                    if (clip == null || clip != _ambSources[_ambActive].clip)
                    {
                        _ambActive = 1 - _ambActive;
                        var s = _ambSources[_ambActive];
                        if (clip != null)
                        {
                            s.clip = clip;
                            s.volume = 0f;
                            s.time = Random.Range(0f, Mathf.Max(0f, clip.length - 1f));
                            s.Play();
                            _ambVol[_ambActive] = 1f;
                        }
                        else _ambVol[_ambActive] = 0f;
                        _ambVol[1 - _ambActive] = 0f;
                    }
                }
            }
            float baseVol = Settings.SfxVolume * 0.45f * (1f - _muffle * 0.6f);
            for (int i = 0; i < 2; i++)
            {
                var s = _ambSources[i];
                s.volume = Mathf.MoveTowards(s.volume, _ambVol[i] * baseVol, dt * 0.35f);
                if (_ambVol[i] <= 0f && s.volume <= 0.001f && s.isPlaying) s.Stop();
            }
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

            _musicCheck -= dt;
            if (_musicCheck <= 0f)
            {
                _musicCheck = 1f;
                // Rotierendes Stück zu Ende -> nächstes
                var cur = _musicSources[_active];
                if (_current.StartsWith("work_") && cur.clip != null && !cur.isPlaying && _musicVol[_active] > 0f)
                {
                    _workTrack = (_workTrack + 1) % WorkTracks.Length;
                    _current = "";
                }
                UpdateMusic(false);
            }

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
            UpdateAmbience(dt);
        }

        private void Register(string name, float[] data, bool music)
        {
            var clip = AudioClip.Create(name, data.Length, 1, AudioSynth.Rate, false);
            clip.SetData(data, 0);
            if (music)
            {
                _music[name] = clip;
                _musicCheck = 0f;
            }
            else _sfx[name] = clip;
        }
    }
}
