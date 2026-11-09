Shader "AllSorted/CabinetBackdropShadow"
{
    Properties {
        _BaseColor("Shadow",Color)=(.015,.025,.065,.23)
        _ShadowOffset("Down-right offset",Vector)=(.22,-.33,0,0)
        _Softness("Soft edge",Range(.01,.3))=.09
    }
    SubShader {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-20"}
        Pass {
            Name "CabinetBackdropShadow" Tags {"LightMode"="SRPDefaultUnlit"}
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;float4 _ShadowOffset;float _Softness;
            CBUFFER_END
            float4 _CabinetCellRects[16];float _CabinetCellCount;
            struct A{float4 positionOS:POSITION;};struct V{float4 positionCS:SV_POSITION;float2 xy:TEXCOORD0;};
            V vert(A i){V o;float3 p=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(p);o.xy=p.xy-_ShadowOffset.xy;return o;}
            half4 frag(V i):SV_Target {
                float distance=100;
                [loop]for(int c=0;c<_CabinetCellCount;c++){
                    float4 r=_CabinetCellRects[c]+float4(-.135,-.163,.135,.11);
                    float2 q=abs(i.xy-(r.xy+r.zw)*.5)-(r.zw-r.xy)*.5;
                    distance=min(distance,length(max(q,0))+min(max(q.x,q.y),0));
                }
                return half4(_BaseColor.rgb,_BaseColor.a*(1-smoothstep(-_Softness,_Softness,distance)));
            }
            ENDHLSL
        }
    }
}
