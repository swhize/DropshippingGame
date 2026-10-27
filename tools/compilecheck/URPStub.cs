// Minimal API stubs for URP / Core RP (only what the game uses) – used ONLY by tools/compilecheck, never by Unity.
namespace UnityEngine.Rendering
{
    public class VolumeParameter<T> { public T value; public bool overrideState; public void Override(T x) { value = x; } }
    public class ClampedFloatParameter : VolumeParameter<float> { }
    public class MinFloatParameter : VolumeParameter<float> { }
    public class FloatParameter : VolumeParameter<float> { }
    public class ColorParameter : VolumeParameter<UnityEngine.Color> { }
    public class VolumeComponent : UnityEngine.ScriptableObject { public bool active; }
    public class VolumeProfile : UnityEngine.ScriptableObject { public T Add<T>(bool overrides = false) where T : VolumeComponent { return null; } public bool TryGet<T>(out T c) where T : VolumeComponent { c = null; return false; } }
    public class Volume : UnityEngine.MonoBehaviour { public bool isGlobal; public float priority; public float weight; public VolumeProfile profile; public VolumeProfile sharedProfile; }
}
namespace UnityEngine.Rendering.Universal
{
    using UnityEngine.Rendering;
    public enum TonemappingMode { None, Neutral, ACES }
    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode> { }
    public sealed class Tonemapping : VolumeComponent { public TonemappingModeParameter mode; }
    public sealed class Bloom : VolumeComponent { public MinFloatParameter threshold, intensity, clamp; public ClampedFloatParameter scatter; public ColorParameter tint; }
    public sealed class ColorAdjustments : VolumeComponent { public FloatParameter postExposure; public ClampedFloatParameter contrast, hueShift, saturation; public ColorParameter colorFilter; }
    public sealed class Vignette : VolumeComponent { public ColorParameter color; public ClampedFloatParameter intensity, smoothness; }
    public enum ShadowResolution { _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048, _4096 = 4096 }
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }
    public enum AntialiasingQuality { Low, Medium, High }
    public class UniversalAdditionalCameraData : UnityEngine.MonoBehaviour { public bool renderPostProcessing; public AntialiasingMode antialiasing; public AntialiasingQuality antialiasingQuality; public bool renderShadows; public CameraRenderType renderType; }
    public enum CameraRenderType { Base, Overlay }
    public static class CameraExtensions { public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this UnityEngine.Camera c) { return null; } }
    public class PostProcessData : UnityEngine.ScriptableObject { }
    public class ScriptableRendererFeature : UnityEngine.ScriptableObject { }
    public class ScriptableRendererData : UnityEngine.ScriptableObject { public System.Collections.Generic.List<ScriptableRendererFeature> rendererFeatures = new System.Collections.Generic.List<ScriptableRendererFeature>(); }
    public class UniversalRendererData : ScriptableRendererData { public PostProcessData postProcessData; }
    public class UniversalRenderPipelineAsset : RenderPipelineAsset
    {
        public static UniversalRenderPipelineAsset Create(ScriptableRendererData d = null) { return null; }
        public float shadowDistance; public int shadowCascadeCount; public int msaaSampleCount; public float renderScale;
        public bool supportsHDR; public bool supportsCameraDepthTexture; public bool supportsCameraOpaqueTexture;
        public ShadowResolution mainLightShadowmapResolution; public bool supportsSoftShadows; public float cascadeBorder;
        public int additionalLightsPerObjectLimit; public bool useSRPBatcher; public bool supportsDynamicBatching;
        protected override RenderPipeline CreatePipeline() { return null; }
    }
}
