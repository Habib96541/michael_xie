Shader "Bull Ride/Dust"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            Output vert(Input i)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            fixed4 frag(Output i) : SV_Target
            {
                float2 p = (i.uv - .5) * 2;
                float fade = saturate(1 - dot(p,p));
                return fixed4(i.color.rgb,i.color.a * fade * fade);
            }
            ENDCG
        }
    }
}
