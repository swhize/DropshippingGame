// Hervorhebung beim Anvisieren: additive, pulsierende Randleuchte (Fresnel).
// Wird als zusätzliches Material über das Objekt gelegt. Funktioniert in URP und der Standard-Pipeline.
Shader "DS/Highlight"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.72, 0.25, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend One One
        ZWrite Off
        ZTest LEqual
        Cull Back
        Offset -1, -1

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 n : TEXCOORD0;
                float3 v : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.v = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float f = pow(1.0 - saturate(dot(normalize(i.n), normalize(i.v))), 2.2);
                float pulse = 0.75 + 0.25 * sin(_Time.y * 5.0);
                return fixed4(_Color.rgb * (f * 0.7 + 0.07) * pulse, 1);
            }
            ENDCG
        }
    }
}
