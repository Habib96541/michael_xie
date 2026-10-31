Shader "BrickRush/Trail"
{
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Blend SrcAlpha One ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes {float3 positionOS:POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            struct Varyings {float4 positionCS:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            Varyings Vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS);o.color=v.color;o.uv=v.uv;return o;}
            half4 Frag(Varyings i):SV_Target{return half4(i.color.rgb,i.color.a*saturate(1-abs(i.uv.y-.5)*2));}
            ENDHLSL
        }
    }
}
