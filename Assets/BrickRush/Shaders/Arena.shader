Shader "BrickRush/Arena"
{
    Properties { _MainTex("Texture",2D)="white"{} }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            struct Attributes {float3 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            Varyings Vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS);o.uv=v.uv;return o;}
            half4 Frag(Varyings i):SV_Target
            {
                float2 uv=i.uv;
                half3 color=lerp(half3(.018,.035,.075),half3(.035,.070,.12),uv.y);
                float2 cells=uv*float2(16,24);float2 grid=abs(frac(cells-.5)-.5)/max(fwidth(cells),.001);
                float g=1-min(min(grid.x,grid.y),1);
                color+=half3(.09,.23,.3)*g*.16;
                float pulse=.75+.25*sin(_Time.y*.35);
                color+=half3(.075,.012,.18)*exp(-length((uv-float2(.17,.82))*float2(3,2))*3)*pulse;
                color+=half3(.005,.07,.065)*exp(-length((uv-float2(.85,.25))*float2(4,2))*3);
                float scanLine=pow(saturate(1-abs(uv.y-.18-sin(uv.x*3+_Time.y*.1)*.03)*40),8);
                color+=half3(.02,.05,.055)*scanLine;
                color*=.7+.3*(1-length(uv-.5));return half4(SRGBToLinear(color),1);
            }
            ENDHLSL
        }
    }
}
