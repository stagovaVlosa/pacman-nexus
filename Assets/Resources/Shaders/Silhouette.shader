// X-ray outline for characters hidden behind buildings (URP, unlit).
// Drawn only where the sphere is BEHIND something already drawn (ZTest Greater):
// invisible in the open, a colored glowing shape when a building is in front.
// Lives in Resources so Shader.Find keeps working in builds.
Shader "Nexus/Silhouette"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.8, 0.2, 0.75)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+50" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Silhouette"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Greater
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(p.positionWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float facing = saturate(dot(normalize(i.normalWS), normalize(i.viewWS)));
                half4 c = _Color;
                // solid-ish center, bright rim
                c.rgb *= 1.0 + (1.0 - facing) * 1.5;
                c.a *= 0.45 + (1.0 - facing) * 0.55;
                return c;
            }
            ENDHLSL
        }
    }
}
