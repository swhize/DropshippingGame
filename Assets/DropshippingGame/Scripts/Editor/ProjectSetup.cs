using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DropshippingGame.EditorTools
{
    /// <summary>
    /// Richtet das Projekt beim ersten Öffnen automatisch ein – ganz ohne Klicken:
    /// URP-Pipeline mit Renderer (inkl. Umgebungsverdeckung), Vorlage-Materialien für Builds,
    /// Farbraum, Spielszene samt Build-Einstellungen. Danach genügt ▶ Play.
    /// Menü: "Dropshipping → Projekt einrichten" führt alles erneut aus.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string Root = "Assets/DropshippingGame";
        private const string SettingsDir = Root + "/Settings";
        private const string PipelinePath = SettingsDir + "/DS_URP.asset";
        private const string RendererPath = SettingsDir + "/DS_URP_Renderer.asset";
        private const string MaterialDir = Root + "/Resources/DSMaterials";
        private const string ScenePath = "Assets/Scenes/Game.unity";
        private const string DoneKey = "DropshippingGame.SetupVersion";
        private const int SetupVersion = 1;

        static ProjectSetup()
        {
            EditorApplication.delayCall += AutoCheck;
        }

        private static void AutoCheck()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoCheck;
                return;
            }
            bool firstTime = !IsConfigured();
            if (firstTime)
            {
                Run(false);
                EditorUtility.DisplayDialog("Dropshipping Simulator",
                    "Das Projekt ist eingerichtet!\n\nDrücke oben in der Mitte auf ▶ (Play), um zu spielen.\n\n" +
                    "Tipp: Für Vollbild im Spiel-Fenster oben rechts \"Maximize On Play\" aktivieren.", "Los geht's");
                return;
            }
            // Leere, unbenannte Szene offen? Dann direkt die Spielszene öffnen.
            var active = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path) && !active.isDirty && File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath);
        }

        private static bool IsConfigured()
        {
            if (EditorPrefs.GetInt(DoneKey + "." + Application.dataPath, 0) < SetupVersion) return false;
            if (GraphicsSettings.defaultRenderPipeline == null) return false;
            if (!File.Exists(ScenePath)) return false;
            return AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/Lit.mat") != null;
        }

        [MenuItem("Dropshipping/Projekt einrichten", priority = 0)]
        public static void RunFromMenu()
        {
            Run(true);
            EditorUtility.DisplayDialog("Dropshipping Simulator", "Fertig. Drücke ▶ (Play) zum Spielen.", "OK");
        }

        [MenuItem("Dropshipping/Spiel starten", priority = 1)]
        public static void PlayGame()
        {
            if (!IsConfigured()) Run(false);
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Dropshipping/Spielstände-Ordner öffnen", priority = 20)]
        public static void OpenSaveFolder()
        {
            string dir = Path.Combine(Application.persistentDataPath, "saves");
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }

        public static void Run(bool force)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Dropshipping Simulator", "Grafik-Pipeline einrichten …", 0.1f);
                EnsureFolder(SettingsDir);
                EnsureFolder(MaterialDir);
                EnsureFolder("Assets/Scenes");
                var pipeline = SetupPipeline(force);

                EditorUtility.DisplayProgressBar("Dropshipping Simulator", "Materialien vorbereiten …", 0.45f);
                SetupMaterials();

                EditorUtility.DisplayProgressBar("Dropshipping Simulator", "Player-Einstellungen …", 0.6f);
                SetupPlayer();

                EditorUtility.DisplayProgressBar("Dropshipping Simulator", "Spielszene anlegen …", 0.8f);
                SetupScene();

                AssetDatabase.SaveAssets();
                EditorPrefs.SetInt(DoneKey + "." + Application.dataPath, SetupVersion);
                Debug.Log("[Dropshipping] Projekt eingerichtet. Pipeline: " + (pipeline != null ? pipeline.name : "keine"));
            }
            catch (Exception e)
            {
                Debug.LogError("[Dropshipping] Einrichtung fehlgeschlagen: " + e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ---- URP ------------------------------------------------------------------------------------
        private static UniversalRenderPipelineAsset SetupPipeline(bool force)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (asset == null || renderer == null || force)
            {
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(renderer, RendererPath);
                }
                if (renderer.postProcessData == null)
                {
                    renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                        "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                }
                AddAmbientOcclusion(renderer);
                EditorUtility.SetDirty(renderer);

                if (asset == null)
                {
                    asset = UniversalRenderPipelineAsset.Create(renderer);
                    AssetDatabase.CreateAsset(asset, PipelinePath);
                }
                ConfigurePipeline(asset);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
                QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                QualitySettings.vSyncCount = 1;
            }
            QualitySettings.SetQualityLevel(Mathf.Max(current, QualitySettings.names.Length - 1), true);
            return asset;
        }

        /// <summary>Werte per SerializedObject setzen – robust gegenüber Zugriffsrechten in verschiedenen URP-Versionen.</summary>
        private static void ConfigurePipeline(UniversalRenderPipelineAsset asset)
        {
            var so = new SerializedObject(asset);
            SetBool(so, "m_SupportsHDR", true);
            SetBool(so, "m_RequireDepthTexture", true);
            SetBool(so, "m_RequireOpaqueTexture", false);
            SetInt(so, "m_MSAA", 4);
            SetFloat(so, "m_RenderScale", 1f);
            SetBool(so, "m_MainLightShadowsSupported", true);
            SetInt(so, "m_MainLightShadowmapResolution", 4096);
            SetBool(so, "m_AdditionalLightShadowsSupported", true);
            SetInt(so, "m_AdditionalLightsShadowmapResolution", 2048);
            SetInt(so, "m_AdditionalLightsPerObjectLimit", 8);
            SetFloat(so, "m_ShadowDistance", 85f);
            SetInt(so, "m_ShadowCascadeCount", 3);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetBool(so, "m_SupportsTerrainHoles", false);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Screen Space Ambient Occlusion als Renderer-Feature (über Reflection, da je nach URP-Version intern).</summary>
        private static void AddAmbientOcclusion(UniversalRendererData renderer)
        {
            try
            {
                foreach (var f in renderer.rendererFeatures)
                    if (f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion") return;
                var type = typeof(UniversalRendererData).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
                if (type == null) return;
                var feature = ScriptableObject.CreateInstance(type) as ScriptableRendererFeature;
                if (feature == null) return;
                feature.name = "SSAO";
                AssetDatabase.AddObjectToAsset(feature, renderer);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);

                var fso = new SerializedObject(feature);
                SetFloat(fso, "m_Settings.Intensity", 0.9f);
                SetFloat(fso, "m_Settings.Radius", 0.28f);
                SetFloat(fso, "m_Settings.DirectLightingStrength", 0.3f);
                SetBool(fso, "m_Settings.Downsample", false);
                fso.ApplyModifiedPropertiesWithoutUndo();

                var so = new SerializedObject(renderer);
                var list = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                if (list == null || map == null) return;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Dropshipping] SSAO konnte nicht hinzugefügt werden (optional): " + e.Message);
            }
        }

        private static void SetBool(SerializedObject so, string prop, bool v)
        {
            var p = so.FindProperty(prop);
            if (p != null && p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v;
        }

        private static void SetInt(SerializedObject so, string prop, int v)
        {
            var p = so.FindProperty(prop);
            if (p == null) return;
            if (p.propertyType == SerializedPropertyType.Integer) p.intValue = v;
            else if (p.propertyType == SerializedPropertyType.Enum) p.intValue = v;
        }

        private static void SetFloat(SerializedObject so, string prop, float v)
        {
            var p = so.FindProperty(prop);
            if (p != null && p.propertyType == SerializedPropertyType.Float) p.floatValue = v;
        }

        // ---- Materialien ------------------------------------------------------------------------------
        /// <summary>
        /// Vorlage-Materialien in Resources: Das Spiel erzeugt alle Materialien zur Laufzeit. Diese
        /// Vorlagen sorgen dafür, dass Shader und Varianten (Normalen, Leuchten, Transparenz) auch
        /// in einem fertigen Build enthalten sind.
        /// </summary>
        private static void SetupMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var sky = Shader.Find("Skybox/Procedural");
            if (lit == null)
            {
                Debug.LogWarning("[Dropshipping] URP-Lit-Shader nicht gefunden – ist das URP-Paket installiert?");
                return;
            }
            MakeMaterial("Lit", lit, m => { });
            MakeMaterial("LitNormal", lit, m => m.EnableKeyword("_NORMALMAP"));
            MakeMaterial("LitEmission", lit, m =>
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
            });
            MakeMaterial("LitNormalEmission", lit, m =>
            {
                m.EnableKeyword("_NORMALMAP");
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
            });
            MakeMaterial("LitTransparent", lit, m =>
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            });
            if (unlit != null) MakeMaterial("Unlit", unlit, m => { });
            if (sky != null) MakeMaterial("Sky", sky, m => { });
            var text = Shader.Find("DS/Text3D");
            if (text != null) MakeMaterial("Text3D", text, m => { });
            var hl = Shader.Find("DS/Highlight");
            if (hl != null) MakeMaterial("Highlight", hl, m => { });
        }

        private static void MakeMaterial(string name, Shader shader, Action<Material> setup)
        {
            string path = MaterialDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = name };
                setup(m);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader)
            {
                m.shader = shader;
                setup(m);
                EditorUtility.SetDirty(m);
            }
        }

        // ---- Player -------------------------------------------------------------------------------------
        private static void SetupPlayer()
        {
            PlayerSettings.companyName = "Hustle Games";
            PlayerSettings.productName = "Dropshipping Simulator";
            if (PlayerSettings.colorSpace != ColorSpace.Linear) PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
        }

        // ---- Szene ----------------------------------------------------------------------------------------
        private static void SetupScene()
        {
            if (!File.Exists(ScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var go = new GameObject("Dropshipping Game");
                go.AddComponent<GameRoot>();
                RenderSettings.ambientMode = AmbientMode.Trilight;
                // Alles ist dynamisch (zur Laufzeit gebaut) – kein Lightmap-Backen nötig.
                string lightingPath = SettingsDir + "/DS_Lighting.lighting";
                var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(lightingPath);
                if (lighting == null)
                {
                    lighting = new LightingSettings { name = "DS_Lighting", bakedGI = false, realtimeGI = false };
                    AssetDatabase.CreateAsset(lighting, lightingPath);
                }
                Lightmapping.lightingSettings = lighting;
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else if (SceneManager.GetActiveScene().path != ScenePath)
            {
                var active = SceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(active.path) || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
