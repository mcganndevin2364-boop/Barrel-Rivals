Shader "Barrel Rivals/Tailored Leather"
{
    Properties
    {
        _BaseMap("Shared leather albedo", 2D) = "white" {}
        _BumpMap("Shared leather normal", 2D) = "bump" {}
        _MetallicGlossMap("Shared leather surface", 2D) = "white" {}
        _BaseColor("Leather dye", Color) = (1,1,1,1)
        _ThreadColor("Fixed flax thread", Color) = (.52,.36,.18,1)
        _BumpScale("Leather grain strength", Range(0,1)) = .07
        _Smoothness("Leather smoothness", Range(0,1)) = .3
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #define _SPECULAR_SETUP 1
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor, _ThreadColor;
            half _BumpScale, _Smoothness;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            half4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half4 tangentWS : TEXCOORD2;
            float2 uv : TEXCOORD3;
            half4 color : COLOR;
            half4 fogAndVertexLight : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings SurfaceVertex(Attributes input)
        {
            Varyings o=(Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input,o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
            VertexNormalInputs basis=GetVertexNormalInputs(input.normalOS,input.tangentOS);
            o.positionCS=p.positionCS; o.positionWS=p.positionWS;
            o.normalWS=basis.normalWS; o.tangentWS=half4(basis.tangentWS,input.tangentOS.w*GetOddNegativeScale());
            o.uv=TRANSFORM_TEX(input.uv,_BaseMap); o.color=input.color;
            o.fogAndVertexLight.x=ComputeFogFactor(p.positionCS.z);
            o.fogAndVertexLight.yzw=VertexLighting(p.positionWS,basis.normalWS);
            return o;
        }
        half3 SurfaceNormal(Varyings input)
        {
            half3 n=NormalizeNormalPerPixel(input.normalWS);
            half3 t=SafeNormalize(input.tangentWS.xyz-n*dot(input.tangentWS.xyz,n));
            half3 b=cross(n,t)*input.tangentWS.w;
            half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,input.uv),_BumpScale*(1-saturate(input.color.a)));
            return NormalizeNormalPerPixel(mul(normalTS,half3x3(t,b,n)));
        }
        ENDHLSL
        Pass
        {
            Name "SurfaceForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma editor_sync_compilation
            #pragma vertex SurfaceVertex
            #pragma fragment SurfaceFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            half4 SurfaceFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half thread=saturate(input.color.a);
                InputData data=(InputData)0;
                data.positionWS=input.positionWS;
                data.normalWS=SurfaceNormal(input);
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.positionWS);
                data.shadowCoord=TransformWorldToShadowCoord(input.positionWS);
                data.bakedGI=SampleSH(data.normalWS);
                data.vertexLighting=input.fogAndVertexLight.yzw;
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
                data.shadowMask=half4(1,1,1,1);
                SurfaceData surface=(SurfaceData)0;
                // Alpha is the sewn-thread mask, never opacity. Fixed thread color is
                // independent of the six leather dyes and does not sample coarse leather grain.
                half3 leather=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb;
                // Compress large grain contrast for soft riding leather at hand distance.
                leather=lerp(leather,half3(.24,.13,.068),.28);
                surface.albedo=lerp(leather*_BaseColor.rgb*input.color.rgb,_ThreadColor.rgb,thread);
                half roughnessMap=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,input.uv).a;
                surface.smoothness=lerp(_Smoothness*(.6+.4*roughnessMap),.18h,thread);
                surface.specular=.028h.xxx;
                surface.normalTS=half3(0,0,1);surface.occlusion=1;surface.alpha=1;
                half4 result=UniversalFragmentPBR(data,surface);
                result.rgb=MixFog(result.rgb,input.fogAndVertexLight.x);
                return half4(result.rgb,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Back ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma editor_sync_compilation
            #pragma vertex SurfaceShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection;float3 _LightPosition;
            Varyings SurfaceShadowVertex(Attributes input)
            {
                Varyings o=SurfaceVertex(input);float3 direction=_LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    direction=normalize(_LightPosition-o.positionWS);
                #endif
                o.positionCS=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,direction)));
                return o;
            }
            half4 ShadowFragment(Varyings input):SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Back ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.0
            #pragma editor_sync_compilation
            #pragma vertex SurfaceVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing
            half4 DepthFragment(Varyings input):SV_Target{return input.positionCS.z;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma editor_sync_compilation
            #pragma vertex SurfaceVertex
            #pragma fragment NormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 NormalsFragment(Varyings input):SV_Target
            {
                half3 n=SurfaceNormal(input);
                #if defined(_GBUFFER_NORMALS_OCT)
                    return half4(PackFloat2To888(saturate(PackNormalOctQuadEncode(n)*.5+.5)),0);
                #else
                    return half4(n,0);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
