Shader "AllSorted/LockDoor"
{
    Properties {
        _BaseMap("Supplied door image",2D)="white"{}
        _DoorAspect("Door aspect",Float)=2.47
        _CounterBlueTop("Counter top",Color)=(.16,.66,1,1)
        _CounterBlueBottom("Counter bottom",Color)=(.12,.23,.80,1)
    }
    SubShader {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10"}
        Pass {
            Name "LockDoor" Tags {"LightMode"="SRPDefaultUnlit"}
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;half4 _CounterBlueTop,_CounterBlueBottom;float _DoorAspect;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
            half4 frag(V i):SV_Target{
                // Horizontal three-slice keeps the central lock circular while the
                // wooden side panels expand to fit a wide shelf. Source pixels unchanged.
                float ratio=max(1,_DoorAspect/(948.0/658.0));float halfCenter=.28/ratio;
                float x=i.uv.x-.5;float a=abs(x);float u;
                if(a<halfCenter)u=.5+x*ratio;
                else u=.5+sign(x)*(.28+(a-halfCenter)*(.22/(.5-halfCenter)));
                float2 uv=float2(u,i.uv.y)*float2(948.0/1024.0,658.0/1024.0)+float2(38.0/1024.0,220.0/1024.0);
                half4 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv);
                // A reusable live count is composed over the printed sample '2'.
                float2 d=uv-float2(.5,.555);float radius=length(d);
                half mask=1-smoothstep(.068,.071,radius);
                half3 blue=lerp(_CounterBlueBottom.rgb,_CounterBlueTop.rgb,saturate((d.y+.07)/.14));
                color.rgb=lerp(color.rgb,blue,mask);return color;
            }
            ENDHLSL
        }
    }
}
