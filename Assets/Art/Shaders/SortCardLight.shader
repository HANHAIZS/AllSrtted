Shader "AllSorted/CardLight"
{
    Properties
    {
        _MainTex ("Effect source", 2D) = "white" {}
        _Color ("Tint and intensity", Color) = (1,1,1,1)
        _Shape ("Beam, flash, glow, sparkle", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+30" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Color;
            float _Shape;
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                Varyings o;
                o.position=UnityObjectToClipPos(vertex);
                o.uv=uv;
                return o;
            }
            fixed4 frag(Varyings i) : SV_Target
            {
                float2 p=abs(i.uv*2-1);
                float3 art=tex2D(_MainTex,i.uv).rgb;
                float envelope=saturate(1-length(p));
                float mask;
                // The supplied RGB art has a baked checker. Mask it geometrically
                // before using chroma (beams/flash) or luminance (glow/sparkle).
                if(_Shape<.5)
                    mask=(exp(-p.x*p.x*9)+saturate((art.b-art.r)*2)*saturate(1-p.x*2)) * smoothstep(0,.08,i.uv.y);
                else if(_Shape<1.5)
                    mask=envelope*envelope*(exp(-dot(p,p)*10)+saturate((art.r-art.b)*5))*.75;
                else if(_Shape<2.5)
                    mask=envelope*envelope*envelope*dot(art,float3(.3,.5,.2));
                else
                    mask=pow(saturate(1-sqrt(p.x)-sqrt(p.y)),1.4)*max(art.r,max(art.g,art.b));
                return fixed4(_Color.rgb*_Color.a*saturate(mask),0);
            }
            ENDHLSL
        }
    }
}
