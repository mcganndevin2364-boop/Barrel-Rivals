Shader "Barrel Rivals/Reins Ghost Hair"
{
    Properties
    {
        [MainTexture] _BaseMap("Source strand mask", 2D) = "white" {}
        [MainColor] _BaseColor("Ghost tint and opacity", Color) = (0.35, 0.88, 0.77, 0.28)
        _AlphaClip("Alpha clip", Float) = 1
        _Cutoff("Source strand cutoff", Range(0,1)) = 0.36
        _FoundationCoverage("Source mane foundation", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Source culling", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "GhostStrands"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]
            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Assets/_Project/Art/Reins/Hair/HorseHairCoverage.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _AlphaClip;
                half _Cutoff;
                half _Cull, _FoundationCoverage;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 attachment : TEXCOORD1;
                float2 foundation : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uvProgress : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uvProgress=float4(TRANSFORM_TEX(input.uv,_BaseMap),input.attachment.y,input.foundation.x);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half mask=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uvProgress.xy).a;
                mask=HorseHairCoverage(mask,input.uvProgress.z,input.uvProgress.w,_FoundationCoverage);
                // Do not clip the already-faded ghost alpha: it may be lower than the source cutoff.
                clip(mask-_Cutoff);
                return half4(_BaseColor.rgb,mask*_BaseColor.a);
            }
            ENDHLSL
        }
    }
}
