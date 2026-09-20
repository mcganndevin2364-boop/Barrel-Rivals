Shader "Barrel Rivals/Distant Terrain"
{
    Properties
    {
        _BaseMap("Reviewed rock albedo", 2D) = "white" {}
        _GroundMap("Reviewed soil albedo", 2D) = "white" {}
        _RockTint("Rock tint", Color) = (.91,.92,.95,1)
        _GroundTint("Sage ground tint", Color) = (.56,.61,.52,1)
        _DryTint("Dry ground tint", Color) = (.76,.70,.60,1)
        _RockScale("Rock repeats per metre", Float) = .0555556
        _GroundScale("Soil repeats per metre", Float) = .0769231
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _RockTint;
            half4 _GroundTint;
            half4 _DryTint;
            float _RockScale;
            float _GroundScale;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            half4 color : COLOR;
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half4 cover : TEXCOORD2;
            half fog : TEXCOORD3;
        };
        Varyings TerrainVertex(Attributes input)
        {
            Varyings output;
            VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
            output.positionCS=p.positionCS;
            output.positionWS=p.positionWS;
            output.normalWS=TransformObjectToWorldNormal(input.normalOS);
            output.cover=input.color;
            output.fog=ComputeFogFactor(p.positionCS.z);
            return output;
        }
        ENDHLSL
        Pass
        {
            Name "DistantTerrainForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex TerrainVertex
            #pragma fragment TerrainFragment
            #pragma multi_compile_fog
            // This backdrop is outside the arena shadow range and explicitly uses
            // one sun plus ambient SH. It neither casts nor receives arena shadows.
            #define _ENVIRONMENTREFLECTIONS_OFF 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_GroundMap); SAMPLER(sampler_GroundMap);
            half4 TerrainFragment(Varyings input) : SV_Target
            {
                half3 normal=NormalizeNormalPerPixel(input.normalWS);
                half3 weights=pow(abs(normal),4);
                weights/=max(dot(weights,half3(1,1,1)),.0001h);
                float3 p=input.positionWS*_RockScale;
                half3 rock=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.zy).rgb*weights.x
                    +SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*weights.y
                    +SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*weights.z;
                half3 soil=SAMPLE_TEXTURE2D(_GroundMap,sampler_GroundMap,input.positionWS.xz*_GroundScale).rgb;
                // The arena soil is strongly orange; distant cover needs restrained
                // mineral/sage hues instead of amplifying that source saturation.
                half soilLuma=dot(soil,half3(.2126h,.7152h,.0722h));
                soil=lerp(soilLuma.xxx,soil,.12h);
                half rockLuma=dot(rock,half3(.2126h,.7152h,.0722h));
                rock=lerp(rockLuma.xxx,rock,.40h);
                half dryness=smoothstep(.32h,.70h,input.cover.r);
                half3 ground=soil*lerp(_GroundTint.rgb,_DryTint.rgb,dryness);
                half exposed=max(1-smoothstep(.60h,.89h,normal.y),input.cover.a*.76h);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=lerp(ground,rock*_RockTint.rgb,exposed)*lerp(.76h,1.20h,input.cover.g);
                surface.alpha=1; surface.occlusion=1; surface.smoothness=.06h;
                surface.normalTS=half3(0,0,1);
                InputData lighting=(InputData)0;
                lighting.positionWS=input.positionWS;
                lighting.normalWS=normal;
                lighting.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.bakedGI=SampleSH(normal);
                lighting.shadowMask=half4(1,1,1,1);
                lighting.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
                half4 color=UniversalFragmentPBR(lighting,surface);
                color.rgb=MixFog(color.rgb,input.fog);
                return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Back ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex TerrainVertex
            #pragma fragment DepthFragment
            half4 DepthFragment(Varyings input) : SV_Target { return input.positionCS.z; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
