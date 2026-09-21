Shader "Barrel Rivals/Worked Arena Footing"
{
    Properties
    {
        [MainTexture] _BaseMap("Reviewed dirt photograph", 2D) = "white" {}
        [MainColor] _BaseColor("Shared footing tint", Color) = (1,1,1,1)
        [Normal] _BumpMap("Reviewed soil normal", 2D) = "bump" {}
        _MetallicGlossMap("Reviewed roughness conversion", 2D) = "black" {}
        _DetailAlbedoMap("Original broad variation", 2D) = "gray" {}
        _TrackMap("Original worked-earth slopes / height / wear", 2D) = "gray" {}
        _PhotographScale("Photograph repeats per metre", Float) = .7692308
        _MacroScale("Broad variation repeats per metre", Float) = .0238095
        _TrackScale("Worked earth repeats per metre", Float) = .125
        _BumpScale("Photographic grain", Range(0,2)) = .7
        _Smoothness("Dry smoothness multiplier", Range(0,1)) = .34
        _TrackStrength("Cosmetic surface relief", Range(0,2)) = .8
        _Wetness("Manifest surface wetness", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
        TEXTURE2D(_DetailAlbedoMap); SAMPLER(sampler_DetailAlbedoMap);
        TEXTURE2D(_TrackMap); SAMPLER(sampler_TrackMap);
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _PhotographScale, _MacroScale, _TrackScale;
            half _BumpScale, _Smoothness, _TrackStrength, _Wetness, _Cull;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half4 fogAndVertexLight : TEXCOORD2;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings FootingVertex(Attributes input)
        {
            Varyings output=(Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);UNITY_TRANSFER_INSTANCE_ID(input,output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
            output.positionCS=p.positionCS;output.positionWS=p.positionWS;
            output.normalWS=TransformObjectToWorldNormal(input.normalOS);
            output.fogAndVertexLight.x=ComputeFogFactor(p.positionCS.z);
            #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                output.fogAndVertexLight.yzw=VertexLighting(p.positionWS,output.normalWS);
            #endif
            return output;
        }
        float2 TrackUV(float2 ground)
        {
            // Broad world-space drag arcs avoid a repeated wavy pattern in every 8m tile.
            float along=ground.y-22;
            return float2(ground.x+.006*along*along,ground.y)*_TrackScale;
        }
        void ReadFooting(float3 world,half3 geometricNormal,out SurfaceData surface,out half3 normal)
        {
            surface=(SurfaceData)0;
            // A short height-field view offset adds depth to the near-ground furrows.
            // Fade it before the horizon and bound the ray slope to avoid texture swimming.
            half3 view=GetWorldSpaceNormalizeViewDir(world);
            half4 first=SAMPLE_TEXTURE2D(_TrackMap,sampler_TrackMap,TrackUV(world.xz));
            half height=(first.b-.5h)*.08h*_TrackStrength;
            float2 ground=world.xz+view.xz*(height/max(view.y,.25h))*smoothstep(.12h,.32h,view.y);
            half broad=SAMPLE_TEXTURE2D(_DetailAlbedoMap,sampler_DetailAlbedoMap,ground*_MacroScale).r;
            half blend=smoothstep(.425h,.575h,broad);
            float2 p=ground*_PhotographScale;
            float2 a=p+float2(.31,-.23)*(broad-.5h);
            // A second, differently oriented scale removes the obvious photograph rows.
            // The same transform is used for color, roughness AND tangent-normal direction.
            const float c=-.7373689,s=.6754903;
            float2 b=float2(c*p.x-s*p.y,s*p.x+c*p.y)*.83+float2(3.71,8.23);
            half3 ca=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,a).rgb;
            half3 cb=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,b).rgb;
            half3 na=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,a),_BumpScale);
            half3 nb=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,b),_BumpScale);
            nb.xy=half2(c*nb.x+s*nb.y,-s*nb.x+c*nb.y);
            half2 grain=lerp(na.xy/max(na.z,.3h),nb.xy/max(nb.z,.3h),blend);
            half smoothA=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,a).a;
            half smoothB=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,b).a;
            half4 worked=SAMPLE_TEXTURE2D(_TrackMap,sampler_TrackMap,TrackUV(ground));
            half2 slope=(worked.rg-.5h)*3.2h*_TrackStrength;
            // Chain rule for the curved sampling domain; otherwise highlights turn the wrong way.
            slope.y+=.012h*(ground.y-22)*slope.x;
            normal=SafeNormalize(geometricNormal+half3(grain.x-slope.x,0,grain.y-slope.y));
            half impression=saturate((.5h-worked.b)*3.2h)*saturate(_TrackStrength);
            half scuff=worked.a*saturate(_TrackStrength);
            surface.albedo=lerp(ca,cb,blend)*_BaseColor.rgb*lerp(.86h,1.12h,saturate(broad*2-.5h));
            surface.albedo*=lerp(1,.90h,impression)*lerp(1,1.06h,scuff)*lerp(1,.80h,_Wetness);
            surface.smoothness=lerp(lerp(smoothA,smoothB,blend)*_Smoothness,.68h,_Wetness);
            surface.occlusion=lerp(1,.77h,impression);
            surface.normalTS=half3(0,0,1);surface.alpha=1;
        }
        ENDHLSL
        Pass
        {
            Name "FootingForward"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull] ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma editor_sync_compilation
            #pragma vertex FootingVertex
            #pragma fragment FootingFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            half4 FootingFragment(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                SurfaceData surface;half3 normal;
                ReadFooting(input.positionWS,NormalizeNormalPerPixel(input.normalWS),surface,normal);
                InputData lighting=(InputData)0;
                lighting.positionWS=input.positionWS;lighting.normalWS=normal;
                lighting.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord=TransformWorldToShadowCoord(input.positionWS);
                lighting.bakedGI=SampleSH(normal);
                lighting.vertexLighting=input.fogAndVertexLight.yzw;
                lighting.shadowMask=half4(1,1,1,1);
                lighting.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
                half4 color=UniversalFragmentPBR(lighting,surface);
                color.rgb=MixFog(color.rgb,input.fogAndVertexLight.x);return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull [_Cull] ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma editor_sync_compilation
            #pragma vertex FootingShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection,_LightPosition;
            Varyings FootingShadowVertex(Attributes input)
            {
                Varyings output=FootingVertex(input);float3 direction=_LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    direction=normalize(_LightPosition-output.positionWS);
                #endif
                output.positionCS=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(output.positionWS,output.normalWS,direction)));
                return output;
            }
            half4 ShadowFragment(Varyings input):SV_Target {return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull [_Cull] ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.0
            #pragma editor_sync_compilation
            #pragma vertex FootingVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing
            half4 DepthFragment(Varyings input):SV_Target {return input.positionCS.z;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            Cull [_Cull] ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma editor_sync_compilation
            #pragma vertex FootingVertex
            #pragma fragment NormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 NormalsFragment(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SurfaceData unused;half3 normal;
                ReadFooting(input.positionWS,NormalizeNormalPerPixel(input.normalWS),unused,normal);
                #if defined(_GBUFFER_NORMALS_OCT)
                    return half4(PackFloat2To888(saturate(PackNormalOctQuadEncode(normal)*.5+.5)),0);
                #else
                    return half4(normal,0);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
