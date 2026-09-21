Shader "Barrel Rivals/Horse Surface"
{
    Properties
    {
        _Tint("Linear reflectance multiplier", Color) = (1,1,1,1)
        _Reflectance("Dielectric reflectance", Range(0,.08)) = .022
        _CoatSmoothness("Coat smoothness", Range(0,1)) = .44
        _BareSmoothness("Bare skin smoothness", Range(0,1)) = .64
        _MicroNormal("Short coat relief", Range(0,.1)) = .025
        _MicroScale("Coat grain UV scale", Vector) = (850,260,0,0)
        _CoatSheen("Directional coat reflection", Range(0,1)) = 0
        _CoatVariation("Short coat color variation", Range(0,.3)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #define _SPECULAR_SETUP 1
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            half _CoatSmoothness, _BareSmoothness, _MicroNormal, _Reflectance;
            half _CoatSheen, _CoatVariation;
            float4 _MicroScale;
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
            half3 tangentWS : TEXCOORD2;
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
            o.normalWS=basis.normalWS; o.tangentWS=basis.tangentWS;
            o.uv=input.uv; o.color=input.color;
            o.fogAndVertexLight.x=ComputeFogFactor(p.positionCS.z);
            o.fogAndVertexLight.yzw=VertexLighting(p.positionWS,basis.normalWS);
            return o;
        }
        float CoatNoise(float2 uv)
        {
            float2 cell=floor(uv),f=frac(uv);f=f*f*(3-2*f);
            float4 q=float4(dot(cell,float2(127.1,311.7)),dot(cell+float2(1,0),float2(127.1,311.7)),
                dot(cell+float2(0,1),float2(127.1,311.7)),dot(cell+1,float2(127.1,311.7)));
            q=frac(sin(q)*43758.5453);
            return lerp(lerp(q.x,q.y,f.x),lerp(q.z,q.w,f.x),f.y)*2-1;
        }
        half Grain(Varyings input)
        {
            float2 coordinate=input.uv*_MicroScale.xy;
            // Aperiodic short-coat relief. Fade subpixel detail before it aliases.
            float attenuation=1-smoothstep(.25,.85,max(fwidth(coordinate.x),fwidth(coordinate.y)));
            return CoatNoise(coordinate)*attenuation;
        }
        half3 SurfaceNormal(Varyings input,half grain)
        {
            half3 n=NormalizeNormalPerPixel(input.normalWS);
            half3 t=SafeNormalize(input.tangentWS-n*dot(input.tangentWS,n));
            return NormalizeNormalPerPixel(n+t*grain*_MicroNormal*(1-saturate(input.color.a)));
        }
        half3 CoatLight(Light light,half3 n,half3 t,half3 view,half amount)
        {
            if(amount<=.0001h)return 0;
            // Use the installed render pipeline's anisotropic GGX distribution. The
            // anatomical tangent is skinned with the horse; it is not a world-space stripe.
            half3 b=SafeNormalize(cross(n,t));half3 h=SafeNormalize(light.direction+view);
            float nl=saturate(dot(n,light.direction)),nv=max(.001,saturate(dot(n,view)));
            float dv=DV_SmithJointGGXAniso(dot(t,h),dot(b,h),saturate(dot(n,h)),
                dot(t,view),dot(b,view),nv,dot(t,light.direction),dot(b,light.direction),nl,.42,.22);
            half grazing=1-saturate(dot(light.direction,h));half grazing2=grazing*grazing;
            half f=lerp(.035h,1.0h,grazing2*grazing2*grazing);
            return light.color*(amount*dv*f*nl*light.distanceAttenuation*light.shadowAttenuation);
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
                half grain=Grain(input), bare=saturate(input.color.a);
                InputData data=(InputData)0;
                data.positionWS=input.positionWS;
                data.normalWS=SurfaceNormal(input,grain);
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.positionWS);
                data.shadowCoord=TransformWorldToShadowCoord(input.positionWS);
                data.bakedGI=SampleSH(data.normalWS);
                data.vertexLighting=input.fogAndVertexLight.yzw;
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
                data.shadowMask=half4(1,1,1,1);
                SurfaceData surface=(SurfaceData)0;
                // RGB is linear reflectance. A is the bare-surface mask, NEVER opacity.
                half detail=grain+CoatNoise(input.uv*float2(31,19))*.35h;
                surface.albedo=input.color.rgb*_Tint.rgb*(1+(grain*.035h+detail*_CoatVariation)*(1-bare));
                surface.albedo*=1-.035h*_CoatSheen*(1-bare);
                surface.smoothness=saturate(lerp(_CoatSmoothness,_BareSmoothness,bare)+grain*.04h*(1-bare));
                surface.specular=_Reflectance.xxx;
                surface.normalTS=half3(0,0,1);surface.occlusion=1;surface.alpha=1;
                half4 result=UniversalFragmentPBR(data,surface);
                half3 tangent=SafeNormalize(input.tangentWS-data.normalWS*dot(data.normalWS,input.tangentWS));
                half coat=_CoatSheen*(1-bare);
                result.rgb+=CoatLight(GetMainLight(data.shadowCoord,data.positionWS,data.shadowMask),data.normalWS,tangent,data.viewDirectionWS,coat);
                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount=GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        result.rgb+=CoatLight(GetAdditionalLight(lightIndex,data.positionWS,data.shadowMask),data.normalWS,tangent,data.viewDirectionWS,coat);
                    LIGHT_LOOP_END
                #endif
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
                half3 n=SurfaceNormal(input,Grain(input));
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
