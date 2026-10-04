using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// Zugriff auf die importierten Assets unter <c>Assets/DropshippingGame/Resources</c>
    /// (Modelle, PBR-Oberflächen, Sounds, Musik, Schriften, HDRIs) – beschrieben durch
    /// <c>Resources/asset_manifest.json</c>. Alles wird gecacht. Fehlt etwas, liefern alle Aufrufe
    /// <c>null</c>/<c>false</c> (nie eine Exception), damit die Welt prozedural weiterbauen kann.
    ///
    /// Konventionen (siehe docs/ASSETS.md):
    /// * <see cref="Model"/> liefert einen Wurzelknoten: Ursprung = Bodenmitte des Modells, Vorderseite
    ///   zeigt nach lokal +Z (bei Fahrzeugen: Fahrtrichtung +Z). <c>rotY</c> dreht zusätzlich.
    /// * Figuren (Kategorie "character") werden über die Höhe skaliert, alle anderen über die größte Kante.
    /// * Materialien werden bei Bedarf in URP-Lit (opak) umgebaut und geteilt.
    /// </summary>
    public static class AssetLib
    {
        // =============================================================================================
        // Daten aus dem Manifest
        // =============================================================================================

        /// <summary>Beschreibung eines Modells aus dem Manifest.</summary>
        public sealed class ModelInfo
        {
            public string Id = "";
            public string Category = "";
            /// <summary>Resources-Pfad ohne Endung.</summary>
            public string Path = "";
            /// <summary>Echte Größe in Metern (größte Kante bzw. Höhe bei FitAxis "y").</summary>
            public float Meters = 1f;
            /// <summary>"max" (größte Kante, Standard) oder "x"/"y"/"z".</summary>
            public string FitAxis = "max";
            /// <summary>Drehung um Y (Grad), damit die Vorderseite nach +Z zeigt.</summary>
            public float ForwardRotY;
            /// <summary>"bottom" (Bodenmitte, Standard) oder "wall" (Rückseite bei z = 0, unten bei y = 0).</summary>
            public string Anchor = "bottom";
            /// <summary>Optionale Textur (Resources-Pfad), die als Grundfarbe erzwungen wird.</summary>
            public string Texture = "";
            /// <summary>Glätte für die umgebauten Materialien (0 = matt).</summary>
            public float Smoothness = 0.2f;
            /// <summary>Größe nach dem Import (Meter, Unity-Achsen, vor ForwardRotY) – nur für ModelBounds.</summary>
            public Vector3 ImportSize = Vector3.one;
            public string Credit = "";
            public string Desc = "";
            public string[] Tags = new string[0];
            /// <summary>Animationsclips (Legacy), leer bei statischen Modellen.</summary>
            public string[] Clips = new string[0];
        }

        private sealed class SurfaceInfo
        {
            public string Id = "";
            public string Prefix = "";
            public Vector2 Tile = Vector2.one;
            public float Smoothness = 0.3f;
            public float NormalScale = 1f;
            public Color Tint = Color.white;
        }

        private static readonly Dictionary<string, ModelInfo> Models = new Dictionary<string, ModelInfo>();
        private static readonly Dictionary<string, List<string>> ByCategory = new Dictionary<string, List<string>>();
        private static readonly Dictionary<string, SurfaceInfo> Surfaces = new Dictionary<string, SurfaceInfo>();
        private static readonly Dictionary<string, string> FontPaths = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> HdriPaths = new Dictionary<string, string>();

        private static readonly Dictionary<string, UnityEngine.Object> LoadCache = new Dictionary<string, UnityEngine.Object>();
        private static readonly HashSet<string> Missing = new HashSet<string>();
        private static readonly Dictionary<string, List<AudioClip>> ClipVariants = new Dictionary<string, List<AudioClip>>();
        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Material> SurfaceCache = new Dictionary<string, Material>();
        private static readonly HashSet<string> Warned = new HashSet<string>();
        private static readonly List<string> Empty = new List<string>();

        private static bool _loaded;
        private static Material _tplModel, _tplSurface;
        private static bool _tplTried;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Models.Clear();
            ByCategory.Clear();
            Surfaces.Clear();
            FontPaths.Clear();
            HdriPaths.Clear();
            LoadCache.Clear();
            Missing.Clear();
            ClipVariants.Clear();
            MaterialCache.Clear();
            SurfaceCache.Clear();
            Warned.Clear();
            _loaded = false;
            _tplTried = false;
            _tplModel = null;
            _tplSurface = null;
        }

        private static void Warn(string key, string msg)
        {
            if (Warned.Add(key)) Debug.LogWarning("[AssetLib] " + msg);
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            // Standard-Schriften, auch ohne Manifest
            FontPaths["ui"] = "Fonts/UI-Regular";
            FontPaths["ui_bold"] = "Fonts/UI-Bold";
            FontPaths["display"] = "Fonts/Display-Bold";
            FontPaths["mono"] = "Fonts/Mono-Regular";
            FontPaths["mono_bold"] = "Fonts/Mono-Bold";
            try
            {
                var ta = Resources.Load<TextAsset>("asset_manifest");
                if (ta == null)
                {
                    Warn("manifest", "Resources/asset_manifest.json fehlt – nur Direktzugriffe möglich.");
                    return;
                }
                if (!Json.TryParse(ta.text, out object parsed))
                {
                    Warn("manifest-parse", "asset_manifest.json ist kein gültiges JSON.");
                    return;
                }
                var root = J.Obj(parsed);
                foreach (object o in J.A(root, "models")) ReadModel(J.Obj(o));
                foreach (object o in J.A(root, "surfaces")) ReadSurface(J.Obj(o));
                foreach (var kv in J.O(root, "fonts"))
                    if (kv.Value is string s && s.Length > 0) FontPaths[kv.Key] = s;
                foreach (var kv in J.O(root, "hdri"))
                    if (kv.Value is string s && s.Length > 0) HdriPaths[kv.Key] = s;
            }
            catch (Exception e)
            {
                Warn("manifest-ex", "Manifest konnte nicht gelesen werden: " + e.Message);
            }
        }

        private static Vector3 V3(List<object> a, Vector3 def)
        {
            if (a == null || a.Count < 3) return def;
            return new Vector3(J.F(a[0]), J.F(a[1]), J.F(a[2]));
        }

        private static string[] Strings(List<object> a)
        {
            var res = new List<string>();
            if (a != null)
                foreach (object o in a)
                    if (o is string s)
                        res.Add(s);
            return res.ToArray();
        }

        private static void ReadModel(Dictionary<string, object> d)
        {
            string id = J.S(d, "id");
            string path = J.S(d, "path");
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(path)) return;
            var m = new ModelInfo
            {
                Id = id,
                Category = J.S(d, "category"),
                Path = path,
                Meters = Mathf.Max(0.01f, J.F(d, "meters", 1f)),
                FitAxis = J.S(d, "fitAxis", "max"),
                ForwardRotY = J.F(d, "forwardRotY"),
                Anchor = J.S(d, "anchor", "bottom"),
                Texture = J.S(d, "texture"),
                Smoothness = J.F(d, "smoothness", 0.2f),
                ImportSize = V3(J.A(d, "size"), Vector3.one),
                Credit = J.S(d, "credit"),
                Desc = J.S(d, "desc"),
                Tags = Strings(J.A(d, "tags")),
                Clips = Strings(J.A(J.O(d, "anim"), "clips")),
            };
            Models[id] = m;
            if (!ByCategory.TryGetValue(m.Category, out var list))
            {
                list = new List<string>();
                ByCategory[m.Category] = list;
            }
            if (!list.Contains(id)) list.Add(id);
        }

        private static void ReadSurface(Dictionary<string, object> d)
        {
            string id = J.S(d, "id");
            if (string.IsNullOrEmpty(id)) return;
            var tile = J.A(d, "tile");
            var tint = J.A(d, "tint");
            var s = new SurfaceInfo
            {
                Id = id,
                Prefix = J.S(d, "path", "Textures/Surfaces/" + id + "/" + id),
                Tile = tile.Count >= 2 ? new Vector2(Mathf.Max(0.01f, J.F(tile[0], 1f)), Mathf.Max(0.01f, J.F(tile[1], 1f))) : Vector2.one,
                Smoothness = J.F(d, "smoothness", 0.3f),
                NormalScale = J.F(d, "normalScale", 1f),
                Tint = tint.Count >= 3 ? new Color(J.F(tint[0], 1f), J.F(tint[1], 1f), J.F(tint[2], 1f), 1f) : Color.white,
            };
            Surfaces[id] = s;
        }

        // =============================================================================================
        // Laden mit Cache
        // =============================================================================================
        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path)) return null;
            string key = typeof(T).Name + ":" + path;
            if (LoadCache.TryGetValue(key, out var cached) && cached != null) return cached as T;
            if (Missing.Contains(key)) return null;
            T res = null;
            try
            {
                res = Resources.Load<T>(path);
            }
            catch (Exception)
            {
                res = null;
            }
            if (res == null) Missing.Add(key);
            else LoadCache[key] = res;
            return res;
        }

        // =============================================================================================
        // Modelle
        // =============================================================================================

        /// <summary>Ist ein Modell mit dieser ID im Manifest und als Datei vorhanden?</summary>
        public static bool HasModel(string id)
        {
            try
            {
                EnsureLoaded();
                if (string.IsNullOrEmpty(id) || !Models.TryGetValue(id, out var info)) return false;
                return Load<GameObject>(info.Path) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Manifest-Eintrag eines Modells (oder null).</summary>
        public static ModelInfo Info(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(id) && Models.TryGetValue(id, out var info) ? info : null;
        }

        /// <summary>Alle Modell-IDs einer Kategorie (z. B. "vehicle", "character", "decor").</summary>
        public static IReadOnlyList<string> Ids(string category)
        {
            EnsureLoaded();
            if (category != null && ByCategory.TryGetValue(category, out var list)) return list;
            return Empty;
        }

        /// <summary>Alle Kategorien des Manifests.</summary>
        public static IReadOnlyList<string> Categories()
        {
            EnsureLoaded();
            return new List<string>(ByCategory.Keys);
        }

        /// <summary>
        /// Baut ein Modell: Ursprung = Bodenmitte, Vorderseite nach lokal +Z, danach um rotY gedreht.
        /// fit &gt; 0: größte Kante (bei Figuren: Höhe) wird auf fit Meter skaliert, sonst Maße aus dem Manifest.
        /// </summary>
        public static GameObject Model(string id, Transform parent, Vector3 localPos, float rotY = 0f,
                                       float fit = -1f, bool collider = false, bool castShadows = true)
        {
            GameObject root = null;
            try
            {
                EnsureLoaded();
                if (string.IsNullOrEmpty(id) || !Models.TryGetValue(id, out var info)) return null;
                var prefab = Load<GameObject>(info.Path);
                if (prefab == null)
                {
                    Warn("model:" + id, "Modell '" + id + "' fehlt unter Resources/" + info.Path);
                    return null;
                }

                // Erst am Ursprung ohne Eltern aufbauen, damit Weltgrenzen = lokale Grenzen sind.
                root = new GameObject(id);
                var holder = new GameObject("Model").transform;
                holder.SetParent(root.transform, false);
                holder.localRotation = Quaternion.Euler(0f, info.ForwardRotY, 0f);
                var inst = UnityEngine.Object.Instantiate(prefab, holder, false);
                inst.name = prefab.name;

                PrepareRenderers(inst, info, castShadows);

                if (!TryGetBounds(root.transform, out Bounds b))
                {
                    b = new Bounds(new Vector3(0f, info.ImportSize.y * 0.5f, 0f), info.ImportSize);
                }
                float measured = Measure(b.size, info);
                float target = fit > 0f ? fit : info.Meters;
                float s = measured > 1e-5f ? target / measured : 1f;
                if (float.IsNaN(s) || float.IsInfinity(s) || s <= 0f) s = 1f;
                holder.localScale = new Vector3(s, s, s);
                Vector3 offset = new Vector3(-b.center.x, -b.min.y, -b.center.z);
                if (info.Anchor == "wall") offset.z = -b.min.z;
                holder.localPosition = offset * s;

                if (collider)
                {
                    var bc = root.AddComponent<BoxCollider>();
                    Vector3 size = b.size * s;
                    bc.size = size;
                    bc.center = new Vector3(0f, size.y * 0.5f, info.Anchor == "wall" ? size.z * 0.5f : 0f);
                }

                if (parent != null) root.transform.SetParent(parent, false);
                root.transform.localPosition = localPos;
                root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
                root.transform.localScale = Vector3.one;
                return root;
            }
            catch (Exception e)
            {
                Warn("model-ex:" + id, "Modell '" + id + "' konnte nicht gebaut werden: " + e.Message);
                if (root != null) UnityEngine.Object.Destroy(root);
                return null;
            }
        }

        private static float Measure(Vector3 size, ModelInfo info)
        {
            switch (info.FitAxis)
            {
                case "x": return size.x;
                case "y": return size.y;
                case "z": return size.z;
                default: return Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            }
        }

        /// <summary>
        /// Grenzen des fertig skalierten Modells in seinem Wurzel-Raum (vor rotY), so wie
        /// <see cref="Model"/> es mit fit = -1 bauen würde. Unbekannt: Bounds der Größe 0.
        /// </summary>
        public static Bounds ModelBounds(string id)
        {
            try
            {
                EnsureLoaded();
                if (string.IsNullOrEmpty(id) || !Models.TryGetValue(id, out var info)) return new Bounds(Vector3.zero, Vector3.zero);
                Vector3 sz = info.ImportSize;
                int quarter = Mathf.RoundToInt(info.ForwardRotY / 90f) & 3;
                if (quarter == 1 || quarter == 3) sz = new Vector3(sz.z, sz.y, sz.x);
                float measured = Measure(sz, info);
                float s = measured > 1e-5f ? info.Meters / measured : 1f;
                Vector3 size = sz * s;
                var center = new Vector3(0f, size.y * 0.5f, info.Anchor == "wall" ? size.z * 0.5f : 0f);
                return new Bounds(center, size);
            }
            catch (Exception)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }
        }

        private static bool TryGetBounds(Transform root, out Bounds bounds)
        {
            bounds = new Bounds();
            bool any = false;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                Bounds rb = r.bounds;
                if (rb.size.sqrMagnitude <= 0f) continue;
                if (!any)
                {
                    bounds = rb;
                    any = true;
                }
                else bounds.Encapsulate(rb);
            }
            return any;
        }

        private static void PrepareRenderers(GameObject inst, ModelInfo info, bool castShadows)
        {
            // Kameras/Lichter aus der Datei sind unerwünscht (Import-Regeln filtern sie normalerweise schon).
            foreach (var cam in inst.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.Destroy(cam);
            foreach (var l in inst.GetComponentsInChildren<Light>(true)) UnityEngine.Object.Destroy(l);

            UnityEngine.Texture forced = string.IsNullOrEmpty(info.Texture) ? null : Load<Texture2D>(info.Texture);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                r.receiveShadows = true;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var fixedMat = FixMaterial(mats[i], forced, info.Smoothness);
                    if (fixedMat != mats[i])
                    {
                        mats[i] = fixedMat;
                        changed = true;
                    }
                }
                if (changed) r.sharedMaterials = mats;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
            }

            var anim = inst.GetComponentInChildren<Animation>(true);
            if (anim != null)
            {
                anim.cullingType = AnimationCullingType.BasedOnRenderers;
                if (info.Clips.Length > 0 && anim.GetClip(info.Clips[0]) != null)
                {
                    anim.clip = anim.GetClip(info.Clips[0]);
                    anim.playAutomatically = true;
                }
            }
        }

        // =============================================================================================
        // Materialien
        // =============================================================================================
        private static void InitTemplates()
        {
            if (_tplTried) return;
            _tplTried = true;
            _tplModel = Resources.Load<Material>("AssetLib/ModelLit");
            if (_tplModel == null) _tplModel = Resources.Load<Material>("DSMaterials/Lit");
            _tplSurface = Resources.Load<Material>("AssetLib/SurfaceLit");
            if (_tplSurface == null) _tplSurface = Resources.Load<Material>("DSMaterials/LitNormal");
        }

        private static Material NewMaterial(bool surface)
        {
            InitTemplates();
            Material tpl = surface ? (_tplSurface != null ? _tplSurface : _tplModel) : _tplModel;
            if (tpl != null) return new Material(tpl);
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            return sh != null ? new Material(sh) : null;
        }

        private static bool IsUrpShader(Shader sh) =>
            sh != null && sh.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal) && sh.isSupported;

        private static UnityEngine.Texture GetTex(Material m, string prop) =>
            m != null && m.HasProperty(prop) ? m.GetTexture(prop) : null;

        private static Color GetCol(Material m, Color def)
        {
            if (m == null) return def;
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
            return def;
        }

        private static bool IsTransparentUrp(Material m) =>
            m != null && m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f;

        /// <summary>
        /// Stellt sicher, dass ein importiertes Material in URP opak gerendert wird. Nicht-URP-Shader
        /// (z. B. "Standard" vom ersten Import vor der URP-Einrichtung) werden als URP-Lit nachgebaut.
        /// </summary>
        private static Material FixMaterial(Material src, UnityEngine.Texture forced, float smoothness)
        {
            string key = (src != null ? src.GetInstanceID() : 0) + "|" + (forced != null ? forced.GetInstanceID() : 0);
            if (MaterialCache.TryGetValue(key, out var cached) && cached != null) return cached;
            Material result = src;
            UnityEngine.Texture baseTex = GetTex(src, "_BaseMap");
            if (baseTex == null) baseTex = GetTex(src, "_MainTex");
            bool needsTex = forced != null && baseTex != forced;
            bool ok = src != null && IsUrpShader(src.shader) && !IsTransparentUrp(src) && !needsTex;
            if (!ok)
            {
                var m = NewMaterial(false);
                if (m != null)
                {
                    UnityEngine.Texture tex = forced != null ? forced : baseTex;
                    Color c = GetCol(src, Color.white);
                    c.a = 1f;
                    // Mit Textur (Farbpalette/Atlas) nur weiß multiplizieren, sonst würde doppelt eingefärbt.
                    if (forced != null) c = Color.white;
                    m.name = (src != null ? src.name : "Material") + " (URP)";
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                    if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                    if (tex != null)
                    {
                        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
                    }
                    if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
                    if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", Mathf.Clamp01(smoothness));
                    if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                    if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0f);
                    m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.DisableKeyword("_ALPHATEST_ON");
                    m.renderQueue = -1;
                    result = m;
                }
            }
            MaterialCache[key] = result;
            return result;
        }

        /// <summary>
        /// Färbt ein mit <see cref="Model"/> gebautes Objekt ein (Farbe wird mit der Textur multipliziert,
        /// Materialien werden dafür kopiert). Für Paletten-Texturen (Kenney/KayKit) eher sparsam einsetzen.
        /// </summary>
        public static bool Tint(GameObject root, Color color)
        {
            if (root == null) return false;
            try
            {
                bool any = false;
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.materials;
                    foreach (var m in mats)
                    {
                        if (m == null) continue;
                        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
                        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
                        any = true;
                    }
                    r.materials = mats;
                }
                return any;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =============================================================================================
        // Animation (Legacy, siehe docs/ASSETS.md)
        // =============================================================================================

        /// <summary>
        /// Spielt einen Animationsclip eines Modells mit Überblendung (Legacy-Animation, Clips loopen je
        /// nach Import-Regel). Beispiel: <c>AssetLib.PlayAnim(npc, "walk")</c>. false, wenn es den Clip nicht gibt.
        /// </summary>
        public static bool PlayAnim(GameObject root, string clip, float fade = 0.15f, float speed = 1f)
        {
            if (root == null || string.IsNullOrEmpty(clip)) return false;
            try
            {
                var anim = root.GetComponentInChildren<Animation>(true);
                if (anim == null) return false;
                if (anim.GetClip(clip) == null)
                {
                    // Fallback: Take-Namen wie "root|walk|Animation Base Layer" per Teilstring finden.
                    string found = null;
                    foreach (AnimationState st in anim)
                    {
                        if (st == null || st.name == null) continue;
                        foreach (var part in st.name.Split('|'))
                            if (string.Equals(part.Trim(), clip, StringComparison.OrdinalIgnoreCase)) { found = st.name; break; }
                        if (found != null) break;
                    }
                    if (found == null) return false;
                    clip = found;
                }
                var state = anim[clip];
                if (state != null) state.speed = speed;
                if (fade > 0f) anim.CrossFade(clip, fade);
                else anim.Play(clip);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Namen aller Animationsclips eines gebauten Modells.</summary>
        public static IReadOnlyList<string> AnimClips(GameObject root)
        {
            var res = new List<string>();
            if (root == null) return res;
            var anim = root.GetComponentInChildren<Animation>(true);
            if (anim == null) return res;
            foreach (AnimationState st in anim)
                if (st != null) res.Add(st.name);
            return res;
        }

        // =============================================================================================
        // PBR-Oberflächen
        // =============================================================================================

        /// <summary>Empfohlene Kachelgröße (Meter entlang U) einer Oberfläche aus dem Manifest, sonst 1.</summary>
        public static float SurfaceTileMeters(string id)
        {
            EnsureLoaded();
            return id != null && Surfaces.TryGetValue(id, out var s) ? s.Tile.x : 1f;
        }

        /// <summary>
        /// URP-Lit-Material mit Grundfarbe, Normal-, Glätte- (aus *_roughness, Alpha = Glätte) und AO-Map.
        /// tileMeters = Größe einer Texturwiederholung in Metern (bei nicht quadratischen Texturen entlang U;
        /// V passt sich an). Werte &lt;= 0 nehmen die Empfehlung aus dem Manifest. Erwartet UVs in Metern
        /// (wie MeshKit). Gecacht pro (id, tileMeters).
        /// </summary>
        public static Material Surface(string id, float tileMeters = 1f)
        {
            try
            {
                EnsureLoaded();
                if (string.IsNullOrEmpty(id)) return null;
                Surfaces.TryGetValue(id, out var info);
                if (info == null) info = new SurfaceInfo { Id = id, Prefix = "Textures/Surfaces/" + id + "/" + id };
                float tu = tileMeters > 0f ? tileMeters : info.Tile.x;
                float tv = tu * (info.Tile.y / Mathf.Max(0.001f, info.Tile.x));
                string key = id + "@" + Mathf.RoundToInt(tu * 1000f);
                if (SurfaceCache.TryGetValue(key, out var cached) && cached != null) return cached;

                var baseTex = Load<Texture2D>(info.Prefix + "_basecolor");
                if (baseTex == null)
                {
                    Warn("surface:" + id, "Oberfläche '" + id + "' fehlt (" + info.Prefix + "_basecolor).");
                    return null;
                }
                var normal = Load<Texture2D>(info.Prefix + "_normal");
                var rough = Load<Texture2D>(info.Prefix + "_roughness");
                var ao = Load<Texture2D>(info.Prefix + "_ao");

                var m = NewMaterial(true);
                if (m == null) return null;
                m.name = "Surface_" + id;
                var scale = new Vector2(1f / Mathf.Max(0.01f, tu), 1f / Mathf.Max(0.01f, tv));
                SetTex(m, "_BaseMap", baseTex, scale);
                SetTex(m, "_MainTex", baseTex, scale);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", info.Tint);
                if (m.HasProperty("_Color")) m.SetColor("_Color", info.Tint);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);

                if (normal != null && m.HasProperty("_BumpMap"))
                {
                    SetTex(m, "_BumpMap", normal, scale);
                    if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", info.NormalScale);
                    m.EnableKeyword("_NORMALMAP");
                }
                else m.DisableKeyword("_NORMALMAP");

                if (rough != null && HasAlpha(rough) && m.HasProperty("_MetallicGlossMap"))
                {
                    // *_roughness: R = 0 (Metallic), G = Rauheit, A = Glätte -> direkt als Metallic-Map
                    SetTex(m, "_MetallicGlossMap", rough, scale);
                    m.EnableKeyword("_METALLICSPECGLOSSMAP");
                    if (m.HasProperty("_SmoothnessTextureChannel")) m.SetFloat("_SmoothnessTextureChannel", 0f);
                    if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 1f);
                    if (m.HasProperty("_GlossMapScale")) m.SetFloat("_GlossMapScale", 1f);
                }
                else
                {
                    m.DisableKeyword("_METALLICSPECGLOSSMAP");
                    if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", info.Smoothness);
                    if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", info.Smoothness);
                }

                if (ao != null && m.HasProperty("_OcclusionMap"))
                {
                    SetTex(m, "_OcclusionMap", ao, scale);
                    if (m.HasProperty("_OcclusionStrength")) m.SetFloat("_OcclusionStrength", 1f);
                    m.EnableKeyword("_OCCLUSIONMAP");
                }
                else m.DisableKeyword("_OCCLUSIONMAP");

                if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0f);
                m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                SurfaceCache[key] = m;
                return m;
            }
            catch (Exception e)
            {
                Warn("surface-ex:" + id, "Oberfläche '" + id + "' konnte nicht gebaut werden: " + e.Message);
                return null;
            }
        }

        private static void SetTex(Material m, string prop, UnityEngine.Texture tex, Vector2 scale)
        {
            if (m == null || !m.HasProperty(prop)) return;
            m.SetTexture(prop, tex);
            m.SetTextureScale(prop, scale);
        }

        private static bool HasAlpha(Texture2D t)
        {
            try
            {
                return UnityEngine.Experimental.Rendering.GraphicsFormatUtility.HasAlphaChannel(t.format);
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>Beliebige Textur aus Resources (Pfad ohne Endung), gecacht.</summary>
        public static Texture2D Texture(string path)
        {
            try
            {
                return Load<Texture2D>(path);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>HDRI als Cubemap ("day", "sunset", "night") – für Skybox/Cubemap oder Reflexionen.</summary>
        public static Cubemap Hdri(string name)
        {
            try
            {
                EnsureLoaded();
                if (string.IsNullOrEmpty(name)) return null;
                string path = HdriPaths.TryGetValue(name, out var p) ? p : "HDRI/" + name;
                return Load<Cubemap>(path);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // =============================================================================================
        // Audio
        // =============================================================================================

        /// <summary>Soundeffekt Audio/SFX/&lt;name&gt; – zufällige Variante aus name, name_2, name_3 …</summary>
        public static AudioClip Sfx(string name) => Pick("Audio/SFX/", name);

        /// <summary>Umgebungs-Loop Audio/Ambience/&lt;name&gt; (street, diner, warehouse, night, park).</summary>
        public static AudioClip Ambience(string name) => Pick("Audio/Ambience/", name);

        /// <summary>Musikstück Audio/Music/&lt;name&gt; (menu, work_1 … work_3, diner, evening); Varianten name_2 … werden zufällig gewählt.</summary>
        public static AudioClip Music(string name) => Pick("Audio/Music/", name);

        /// <summary>Anzahl der Varianten eines Soundeffekts (0 = nicht vorhanden).</summary>
        public static int SfxVariants(string name)
        {
            var list = Variants("Audio/SFX/", name);
            return list != null ? list.Count : 0;
        }

        private static AudioClip Pick(string folder, string name)
        {
            try
            {
                var list = Variants(folder, name);
                if (list == null || list.Count == 0) return null;
                return list.Count == 1 ? list[0] : list[UnityEngine.Random.Range(0, list.Count)];
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static List<AudioClip> Variants(string folder, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string key = folder + name;
            if (ClipVariants.TryGetValue(key, out var list)) return list;
            list = new List<AudioClip>();
            var first = Load<AudioClip>(folder + name);
            if (first != null) list.Add(first);
            for (int i = 2; i <= 12; i++)
            {
                var c = Load<AudioClip>(folder + name + "_" + i);
                if (c == null) break;
                list.Add(c);
            }
            ClipVariants[key] = list;
            return list;
        }

        // =============================================================================================
        // Schriften
        // =============================================================================================

        /// <summary>Schrift: "ui", "ui_bold", "display", "mono", "mono_bold" (oder direkter Resources-Pfad).</summary>
        public static UnityEngine.Font Font(string key)
        {
            try
            {
                EnsureLoaded();
                if (string.IsNullOrEmpty(key)) return null;
                string path = FontPaths.TryGetValue(key, out var p) ? p : (key.Contains("/") ? key : "Fonts/" + key);
                return Load<UnityEngine.Font>(path);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
