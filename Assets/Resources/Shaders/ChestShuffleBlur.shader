Shader "Hidden/ChestShuffle/SoftBlur"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragHorizontal
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;
            fixed4 fragHorizontal(v2f_img i) : SV_Target
            {
                float2 d = float2(_MainTex_TexelSize.x * _BlurSize, 0);
                fixed4 c = tex2D(_MainTex, i.uv) * 0.40;
                c += tex2D(_MainTex, i.uv + d) * 0.24;
                c += tex2D(_MainTex, i.uv - d) * 0.24;
                c += tex2D(_MainTex, i.uv + d * 2.2) * 0.06;
                c += tex2D(_MainTex, i.uv - d * 2.2) * 0.06;
                return c;
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragVertical
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;
            fixed4 fragVertical(v2f_img i) : SV_Target
            {
                float2 d = float2(0, _MainTex_TexelSize.y * _BlurSize);
                fixed4 c = tex2D(_MainTex, i.uv) * 0.40;
                c += tex2D(_MainTex, i.uv + d) * 0.24;
                c += tex2D(_MainTex, i.uv - d) * 0.24;
                c += tex2D(_MainTex, i.uv + d * 2.2) * 0.06;
                c += tex2D(_MainTex, i.uv - d * 2.2) * 0.06;
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
