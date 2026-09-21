Shader "Barrel Rivals/Horse Fiber"
{
    Properties
    {
        [MainTexture] _BaseMap("Original strand color and coverage", 2D) = "white" {}
        [MainColor] _BaseColor("Coat tint", Color) = (1,1,1,1)
        _FiberTint("Fiber reflection tint", Color) = (.80,.80,.78,1)
        [HideInInspector] _AlphaClip("Alpha clip", Float) = 1
        _Cutoff("Coverage cutoff", Range(0,1)) = .36
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling", Float) = 0
        [Enum(Off,0,On,1)] _AlphaToMask("MSAA coverage", Float) = 1
        _PrimaryStrength("Narrow reflection", Range(0,1)) = .10
        _SecondaryStrength("Broad reflection", Range(0,1)) = .055
        _PrimaryExponent("Narrow exponent", Range(4,128)) = 64
        _SecondaryExponent("Broad exponent", Range(4,64)) = 18
        _PrimaryShift("Narrow tilt", Range(-.5,.5)) = .08
        _SecondaryShift("Broad tilt", Range(-.5,.5)) = -.16
        _Scatter("Edge transmission", Range(0,1)) = .28
        _RootShade("Root ambient visibility", Range(0,1)) = .68
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _FiberTint;
            half _AlphaClip, _Cutoff, _Cull, _AlphaToMask;
            half _PrimaryStrength, _SecondaryStrength, _PrimaryExponent, _SecondaryExponent;
            half _PrimaryShift, _SecondaryShift, _Scatter, _RootShade;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            float2 attachment : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half3 fiberWS : TEXCOORD2;
            float3 uvProgress : TEXCOORD3;
            half4 fogAndVertexLight : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings FiberVertex(Attributes input)
        {
            Varyings output=(Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input,output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
            VertexNormalInputs basis=GetVertexNormalInputs(input.normalOS,input.tangentOS);
            output.positionCS=p.positionCS;output.positionWS=p.positionWS;
            output.normalWS=basis.normalWS;
            // Atlas V decreases from root to tip. U runs ACROSS each lock;
            // using mesh tangent alone would rotate the highlight by 90 degrees.
            output.fiberWS=-basis.bitangentWS;
            output.uvProgress=float3(TRANSFORM_TEX(input.uv,_BaseMap),input.attachment.y);
            output.fogAndVertexLight.x=ComputeFogFactor(p.positionCS.z);
            #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                output.fogAndVertexLight.yzw=VertexLighting(p.positionWS,basis.normalWS);
            #endif
            return output;
        }
        half4 ReadFiber(float2 uv)
        {
            half4 sample=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv);
            // Coverage is an atlas property; tint never changes the strand silhouette.
            clip(sample.a-_Cutoff);
            return sample;
        }
        half ReflectionLobe(half3 direction,half3 halfVector,half exponent)
        {
            half alignment=dot(direction,halfVector);
            return pow(saturate(1-alignment*alignment),exponent*.5h);
        }
        half3 FiberLight(Light light,half3 normal,half3 fiber,half3 view,
            half3 albedo,half detail,half progress)
        {
            half ndl=dot(normal,light.direction);
            half longitudinal=dot(fiber,light.direction);
            half diffuse=sqrt(saturate(1-longitudinal*longitudinal))*saturate((ndl+.35h)/1.35h);
            half3 halfway=SafeNormalize(light.direction+view);
            // Existing mip-filtered color detail breaks up the lobes along actual locks;
            // no screen-space noise, new texture, extra geometry or temporal flicker.
            half shift=(detail-.07h)*.38h;
            half first=ReflectionLobe(SafeNormalize(fiber+normal*(_PrimaryShift+shift)),halfway,_PrimaryExponent);
            half second=ReflectionLobe(SafeNormalize(fiber+normal*(_SecondaryShift+shift)),halfway,_SecondaryExponent);
            half visibility=saturate(ndl+.3h);
            half strandGlint=saturate(.07h+detail*7);
            half3 specular=_FiberTint.rgb*(first*_PrimaryStrength+second*_SecondaryStrength)*visibility*strandGlint;
            half transmission=_Scatter*saturate(-ndl)*pow(saturate(dot(-light.direction,view)),3);
            transmission*=lerp(.25h,1.0h,saturate(progress));
            return ((diffuse+transmission)*albedo+specular)*light.color*light.distanceAttenuation*light.shadowAttenuation;
        }
        ENDHLSL
        Pass
        {
            Name "FiberForward"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull] ZWrite On AlphaToMask [_AlphaToMask]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma editor_sync_compilation
            #pragma vertex FiberVertex
            #pragma fragment FiberFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            half4 FiberFragment(Varyings input,FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 texel=ReadFiber(input.uvProgress.xy);
                half3 normal=NormalizeNormalPerPixel(input.normalWS)*IS_FRONT_VFACE(frontFace,1,-1);
                half3 fiber=SafeNormalize(input.fiberWS);
                half3 view=GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 albedo=texel.rgb*_BaseColor.rgb;
                half detail=dot(texel.rgb,half3(.2126h,.7152h,.0722h));
                half ambientVisibility=lerp(_RootShade,1,smoothstep(0,.7h,input.uvProgress.z));
                half3 color=albedo*SampleSH(normal)*ambientVisibility;
                Light main=GetMainLight(TransformWorldToShadowCoord(input.positionWS),input.positionWS,half4(1,1,1,1));
                color+=FiberLight(main,normal,fiber,view,albedo,detail,input.uvProgress.z);
                #if defined(_ADDITIONAL_LIGHTS)
                    uint count=GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(count)
                        Light extra=GetAdditionalLight(lightIndex,input.positionWS,half4(1,1,1,1));
                        color+=FiberLight(extra,normal,fiber,view,albedo,detail,input.uvProgress.z);
                    LIGHT_LOOP_END
                #endif
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    color+=albedo*input.fogAndVertexLight.yzw;
                #endif
                color=MixFog(color,input.fogAndVertexLight.x);
                half coverage=saturate((texel.a-_Cutoff)/max(fwidth(texel.a),.0001h)+.5h);
                return half4(color,coverage);
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
            #pragma vertex FiberShadowVertex
            #pragma fragment FiberShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection;
            float3 _LightPosition;
            Varyings FiberShadowVertex(Attributes input)
            {
                Varyings output=FiberVertex(input);
                float3 direction=_LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    direction=normalize(_LightPosition-output.positionWS);
                #endif
                output.positionCS=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(output.positionWS,output.normalWS,direction)));
                return output;
            }
            half4 FiberShadowFragment(Varyings input) : SV_Target
            { ReadFiber(input.uvProgress.xy);return 0; }
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
            #pragma vertex FiberVertex
            #pragma fragment FiberDepthFragment
            #pragma multi_compile_instancing
            half4 FiberDepthFragment(Varyings input) : SV_Target
            { ReadFiber(input.uvProgress.xy);return input.positionCS.z; }
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
            #pragma vertex FiberVertex
            #pragma fragment FiberNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 FiberNormalsFragment(Varyings input,FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                ReadFiber(input.uvProgress.xy);
                half3 normal=NormalizeNormalPerPixel(input.normalWS)*IS_FRONT_VFACE(frontFace,1,-1);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct=PackNormalOctQuadEncode(normal);
                    return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
                #else
                    return half4(normal,0);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
