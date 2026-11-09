Shader "AllSorted/CardFace"
{
    Properties
    {
        _MainTex ("Face", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _Radius ("Corner radius", Range(0,0.25)) = 0.09
        _Face ("Face mask", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Pass
        {
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST, _Color;
            float _Cutoff, _Radius, _Face;
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                Varyings o;
                o.position = UnityObjectToClipPos(vertex);
                o.uv = uv;
                return o;
            }
            fixed4 frag(Varyings i) : SV_Target
            {
                float2 q = abs(i.uv-.5)-(.5-_Radius);
                float mask = 1-step(_Radius,length(max(q,0)));
                fixed4 c = tex2D(_MainTex, TRANSFORM_TEX(i.uv,_MainTex)) * _Color;
                clip(c.a * lerp(1,mask,_Face) - _Cutoff);
                return fixed4(c.rgb,1);
            }
            ENDHLSL
        }
    }
}
