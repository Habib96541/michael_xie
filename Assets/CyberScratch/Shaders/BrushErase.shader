Shader "CyberScratch/BrushErase"
{
    Properties
    {
        _MainTex ("Brush Stamp Texture", 2D) = "white" {}
        _Strength ("Erase Strength", Range(0, 1)) = 1.0
        _UseTextureAlpha ("Use Texture Alpha", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend Zero OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Strength;
            float _UseTextureAlpha;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float alpha = (_UseTextureAlpha > 0.5) ? tex2D(_MainTex, i.uv).a : 1.0;
                return fixed4(0, 0, 0, alpha * _Strength);
            }
            ENDCG
        }
    }
}
