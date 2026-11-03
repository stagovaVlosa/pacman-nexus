// Additive glow for neon effects (URP, unlit).
// _FresnelPower > 0: glow only at the silhouette edges (rim glow on glass).
// _FresnelPower = 0: flat glow everywhere (light cones, particles, ground glow).
// _MainTex alpha shapes the glow (e.g. a soft radial gradient for a ground glow).
// _ZTest = Always draws it on top of everything (markers that must show through buildings).
// Lives in Resources so Shader.Find keeps working in builds.
Shader "Nexus/Glow"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.7, 0, 1)
        _Intensity ("Intensity", Float) = 1
        _FresnelPower ("Fresnel Power", Float) = 0
        _MainTex ("Mask", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull [_Cull]
            ZTest [_ZTest]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _FresnelPower;
                float4 _MainTex_ST;
            CBUFFER_END
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; float2 uv : TEXCOORD2; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(posWS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half f = 1;
                if (_FresnelPower > 0)
                {
                    half ndv = abs(dot(normalize(i.normalWS), normalize(i.viewWS)));
                    f = pow(1 - ndv, _FresnelPower);
                }
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                return half4(_Color.rgb * (f * mask * _Intensity), 0);
            }
            ENDHLSL
        }
    }
}
