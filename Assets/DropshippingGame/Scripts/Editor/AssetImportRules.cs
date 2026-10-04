using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace DropshippingGame.EditorTools
{
    /// <summary>
    /// Import-Regeln für die Drittanbieter-Assets unter Assets/DropshippingGame/Resources/.
    /// - Modelle: keine Kameras/Lichter, Materialien per MaterialDescription als URP/Lit (opak),
    ///   Charaktere (Models/kenney_mini_characters) mit Legacy-Animation (Animation-Komponente,
    ///   kein AnimatorController nötig; loopende Clips: idle, walk, sprint, sit, drive, holding-*),
    ///   alle anderen Modelle ohne Animation/Rig.
    /// - Texturen: *_normal -> NormalMap, *_roughness/*_ao linear, max. 1024/2048, Mipmaps; HDRI -> Cube.
    /// - Audio: Music/Ambience streamen, SFX DecompressOnLoad.
    /// Nur Pfade unterhalb von Resources/ und nur unsere Unterordner werden angefasst.
    /// </summary>
    public sealed class AssetImportRules : AssetPostprocessor
    {
        private const string Root = "Assets/DropshippingGame/Resources/";
        private const string CharDir = Root + "Models/kenney_mini_characters/";
        private const string PetDir = Root + "Models/kenney_cube_pets/";

        private static readonly string[] LoopClips =
        {
            "idle", "walk", "sprint", "sit", "drive", "static", "crouch",
            "run", "eat", "dance",
            "holding-both", "holding-right", "holding-left", "wheelchair-sit", "wheelchair-move"
        };

        public override int GetPostprocessOrder() { return 100; }

        private static bool Ours(string path)
        {
            return !string.IsNullOrEmpty(path) && path.Replace('\\', '/').StartsWith(Root, StringComparison.Ordinal);
        }

        // ------------------------------------------------------------------ Modelle
        private void OnPreprocessModel()
        {
            if (!Ours(assetPath)) return;
            var mi = assetImporter as ModelImporter;
            if (mi == null) return;
            try
            {
                mi.importCameras = false;
                mi.importLights = false;
                mi.importVisibility = true;
                mi.useFileScale = true;
                mi.globalScale = 1f;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
                mi.isReadable = false;
                mi.importBlendShapes = false;

                string np = assetPath.Replace('\\', '/');
                bool character = np.StartsWith(CharDir, StringComparison.Ordinal) || np.StartsWith(PetDir, StringComparison.Ordinal);
                if (character)
                {
                    mi.animationType = ModelImporterAnimationType.Legacy;
                    mi.importAnimation = true;
                    mi.generateAnimations = ModelImporterGenerateAnimations.InRoot;
                }
                else
                {
                    mi.animationType = ModelImporterAnimationType.None;
                    mi.importAnimation = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AssetImportRules] Model " + assetPath + ": " + e.Message);
            }
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!Ours(assetPath) || root == null) return;
            // Clip-Namen normalisieren ("root|walk|Animation Base Layer" -> "walk") und Loop setzen.
            var anim = root.GetComponent<Animation>();
            if (anim == null) return;
            try
            {
                foreach (AnimationClip clip in AnimationUtility.GetAnimationClips(root))
                {
                    if (clip == null) continue;
                    string shortName = ShortClipName(clip.name);
                    bool loop = Array.IndexOf(LoopClips, shortName) >= 0;
                    clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
                }
                if (anim.clip == null)
                {
                    foreach (AnimationClip clip in AnimationUtility.GetAnimationClips(root))
                        if (clip != null && ShortClipName(clip.name) == "idle") { anim.clip = clip; break; }
                }
                anim.playAutomatically = true;
                anim.cullingType = AnimationCullingType.BasedOnRenderers;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AssetImportRules] Anim " + assetPath + ": " + e.Message);
            }
        }

        private void OnPreprocessAnimation()
        {
            if (!Ours(assetPath)) return;
            var mi = assetImporter as ModelImporter;
            if (mi == null || mi.animationType != ModelImporterAnimationType.Legacy) return;
            try
            {
                var clips = mi.defaultClipAnimations;
                if (clips == null || clips.Length == 0) return;
                for (int i = 0; i < clips.Length; i++)
                {
                    string n = ShortClipName(clips[i].name);
                    clips[i].name = n;
                    bool loop = Array.IndexOf(LoopClips, n) >= 0;
                    clips[i].loopTime = loop;
                    clips[i].wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
                }
                mi.clipAnimations = clips;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AssetImportRules] Clips " + assetPath + ": " + e.Message);
            }
        }

        internal static string ShortClipName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var parts = name.Split('|');
            foreach (var p in parts)
            {
                string t = p.Trim();
                if (t.Length == 0 || t == "root" || t == "Root" || t.StartsWith("Animation Base Layer", StringComparison.Ordinal)) continue;
                return t;
            }
            return name;
        }

        private void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] animations)
        {
            if (!Ours(assetPath) || material == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return; // URP noch nicht aktiv: AssetLib konvertiert zur Laufzeit
            material.shader = shader;

            Color c = Color.white;
            Vector4 v;
            if (description.TryGetProperty("DiffuseColor", out v)) c = new Color(v.x, v.y, v.z, 1f);
            TexturePropertyDescription tex;
            Texture2D baseTex = null;
            if (description.TryGetProperty("DiffuseColor", out tex) && tex.texture != null) baseTex = tex.texture as Texture2D;
            if (baseTex == null && tex.path != null) baseTex = FindTextureNear(tex.path);
            if (baseTex == null) baseTex = FindTextureNear(null);
            if (baseTex != null)
            {
                material.SetTexture("_BaseMap", baseTex);
                c = Color.white;
            }
            material.SetColor("_BaseColor", c);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.2f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = -1;
        }

        /// <summary>Sucht die Pack-Textur relativ zum Modell (Textures/colormap.png oder *_texture.png daneben).</summary>
        private Texture2D FindTextureNear(string hint)
        {
            try
            {
                string dir = Path.GetDirectoryName(assetPath).Replace('\\', '/');
                if (!string.IsNullOrEmpty(hint))
                {
                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/" + Path.GetFileName(hint));
                    if (t != null) return t;
                    t = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/Textures/" + Path.GetFileName(hint));
                    if (t != null) return t;
                }
                foreach (var f in new[] { dir + "/Textures/colormap.png" })
                {
                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>(f);
                    if (t != null) return t;
                }
                foreach (var f in Directory.GetFiles(dir, "*.png"))
                {
                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>(f.Replace('\\', '/'));
                    if (t != null) return t;
                }
            }
            catch (Exception) { }
            return null;
        }

        // ------------------------------------------------------------------ Texturen
        private void OnPreprocessTexture()
        {
            if (!Ours(assetPath)) return;
            var ti = assetImporter as TextureImporter;
            if (ti == null) return;
            string p = assetPath.Replace('\\', '/');
            string name = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
            try
            {
                if (p.StartsWith(Root + "HDRI/", StringComparison.Ordinal))
                {
                    ti.textureShape = TextureImporterShape.TextureCube;
                    ti.sRGBTexture = false;
                    ti.mipmapEnabled = true;
                    ti.maxTextureSize = 1024;
                    return;
                }
                ti.textureType = TextureImporterType.Default;
                ti.mipmapEnabled = true;
                ti.isReadable = false;
                ti.wrapMode = TextureWrapMode.Repeat;
                if (name.EndsWith("_normal", StringComparison.Ordinal))
                {
                    ti.textureType = TextureImporterType.NormalMap;
                    ti.maxTextureSize = 1024;
                }
                else if (name.EndsWith("_roughness", StringComparison.Ordinal) || name.EndsWith("_ao", StringComparison.Ordinal))
                {
                    ti.sRGBTexture = false;
                    ti.alphaSource = TextureImporterAlphaSource.FromInput;
                    ti.maxTextureSize = 1024;
                }
                else if (p.StartsWith(Root + "Models/", StringComparison.Ordinal))
                {
                    // Palettentexturen der Low-Poly-Packs: scharfe Farbfelder
                    ti.filterMode = FilterMode.Bilinear;
                    ti.mipmapEnabled = false;
                    ti.maxTextureSize = 1024;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                }
                else
                {
                    ti.maxTextureSize = 1024;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AssetImportRules] Texture " + assetPath + ": " + e.Message);
            }
        }

        // ------------------------------------------------------------------ Audio
        private void OnPreprocessAudio()
        {
            if (!Ours(assetPath)) return;
            var ai = assetImporter as AudioImporter;
            if (ai == null) return;
            string p = assetPath.Replace('\\', '/');
            try
            {
                var s = ai.defaultSampleSettings;
                bool stream = p.StartsWith(Root + "Audio/Music/", StringComparison.Ordinal) ||
                              p.StartsWith(Root + "Audio/Ambience/", StringComparison.Ordinal);
                if (stream)
                {
                    s.loadType = AudioClipLoadType.Streaming;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = 0.6f;
                    s.preloadAudioData = false;
                    ai.loadInBackground = true;
                }
                else if (p.StartsWith(Root + "Audio/", StringComparison.Ordinal))
                {
                    s.loadType = AudioClipLoadType.DecompressOnLoad;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = 0.7f;
                    s.preloadAudioData = true;
                    ai.loadInBackground = false;
                }
                else return;
                ai.defaultSampleSettings = s;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AssetImportRules] Audio " + assetPath + ": " + e.Message);
            }
        }
    }
}
