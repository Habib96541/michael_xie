Shader "Bull Ride/Rodeo Shadow"
{
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil { Ref 1 ReadMask 1 WriteMask 1 Comp NotEqual Pass Replace }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 vert(float4 vertex : POSITION) : SV_POSITION
            {
                float3 world = mul(unity_ObjectToWorld, vertex).xyz;
                world -= float3(.53, .84, .32) * ((world.y - .015) / .84);
                return mul(UNITY_MATRIX_VP, float4(world, 1));
            }
            fixed4 frag() : SV_Target { return fixed4(.035, .024, .023, .55); }
            ENDCG
        }
    }
}

