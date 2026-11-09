Shader "AllSorted/CabinetLit"
{
    Properties
    {
        _BaseColor("Color",Color)=(.91,.88,.81,1)
        _Metallic("Metallic",Range(0,1))=0
        _Smoothness("Smoothness",Range(0,1))=.4
        _AOStrength("Cavity AO",Range(0,1))=.65
        _AOColor("Inner edge tint",Color)=(.25,.32,.34,1)
        _RimColor("Bevel rim",Color)=(.8,.86,.94,1)
        _RimStrength("Bevel rim strength",Range(0,.3))=.2
        _Role("Frame 0 Shelf 1 Back 2",Float)=0
        _TopShadowStrength("Local upper recess shadow",Range(0,1))=0
        [ToggleOff] _ReceiveShadows("Receive shadows",Float)=1
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _AOColor, _RimColor;
            half _Metallic, _Smoothness, _AOStrength, _RimStrength, _Role, _ReceiveShadows, _TopShadowStrength;
        CBUFFER_END
        // The single active board publishes local rectangles once on entry.
        // No per-renderer property blocks; all three materials remain SRP batchable.
        float4 _CabinetCellRects[16];
        float _CabinetCellCount;
        struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;half4 color:COLOR;};
        struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float3 positionOS:TEXCOORD2;half ao:TEXCOORD3;};
        Varyings CabinetVertex(Attributes input)
        {
            Varyings o;VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
            o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.positionOS=input.positionOS.xyz;
            o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.ao=input.color.r;return o;
        }
        half CavityAO(float3 p)
        {
            half cavity=0;
            [loop] for(int i=0;i<_CabinetCellCount;i++) {
                float4 r=_CabinetCellRects[i];
                float2 edge=min(p.xy-r.xy,r.zw-p.xy);
                if(min(edge.x,edge.y)>=-.012) {
                    half edges=exp2(-max(min(edge.x,edge.y),0)*11);
                    half corners=exp2(-max(edge.x,0)*8)*exp2(-max(edge.y,0)*8);
                    cavity=max(cavity,edges*.70+corners*.12);
                }
            }
            return saturate(cavity)*smoothstep(-.55,.85,p.z);
        }
        half UpperRecessShadow(float3 p)
        {
            half shadow=0;
            [loop]for(int c=0;c<_CabinetCellCount;c++){
                float4 r=_CabinetCellRects[c];
                if(p.x>=r.x && p.x<=r.z && p.y>=r.y && p.y<=r.w){
                    // A broad soft shadow under the ceiling, fading down the back.
                    half band=1-smoothstep(.06,.58,r.w-p.y);
                    half side=smoothstep(0,.09,min(p.x-r.x,r.z-p.x));
                    shadow=max(shadow,band*lerp(.85,1,side));
                }
            }
            return shadow*_TopShadowStrength*smoothstep(.50,.95,p.z);
        }
        half4 CabinetFragment(Varyings i):SV_Target
        {
            half3 normal=NormalizeNormalPerPixel(i.normalWS);
            half ao=max(1-i.ao,CavityAO(i.positionOS));
            ao=saturate(ao*_AOStrength);
            SurfaceData surface=(SurfaceData)0;
            surface.albedo=_BaseColor.rgb*lerp(half3(1,1,1),_AOColor.rgb,ao*.26);
            surface.metallic=_Metallic;surface.smoothness=_Smoothness;surface.alpha=1;
            surface.occlusion=1-ao;surface.normalTS=half3(0,0,1);
            if(_Role>1.5)surface.albedo*=1-UpperRecessShadow(i.positionOS);
            // Keep shelf undersides and front lip a little darker than the top.
            if(_Role>.5 && _Role<1.5)surface.albedo*=lerp(.77,1,saturate(normal.y*.5+.5));
            InputData data=(InputData)0;
            data.positionWS=i.positionWS;data.normalWS=normal;data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
            data.shadowCoord=TransformWorldToShadowCoord(i.positionWS);data.bakedGI=SampleSH(normal);
            data.vertexLighting=VertexLighting(i.positionWS,normal);
            data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);data.shadowMask=half4(1,1,1,1);
            // A bevel-facing term, not a glow on the broad front plane.
            surface.emission=_RimColor.rgb*pow(1-saturate(dot(normal,data.viewDirectionWS)),3)*_RimStrength;
            half4 color=UniversalFragmentPBR(data,surface);
            color.rgb=MixFog(color.rgb,ComputeFogFactor(i.positionCS.z));return color;
        }
        half4 CabinetDepth(Varyings i):SV_Target{return 0;}
        half4 CabinetNormals(Varyings i):SV_Target
        {
            float3 normal=NormalizeNormalPerPixel(i.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct=PackNormalOctQuadEncode(normal);
                return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
            #else
                return half4(normal,0);
            #endif
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex CabinetVertex
            #pragma fragment CabinetFragment
            #pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ColorMask R
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex CabinetVertex
            #pragma fragment CabinetDepth
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags {"LightMode"="DepthNormals"}
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex CabinetVertex
            #pragma fragment CabinetNormals
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
