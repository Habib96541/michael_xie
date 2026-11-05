Shader "Roadster/Marine Dusk"
{
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Pass
        {
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half _RoadsterGrayscale;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction);
                float height = max(0, d.y);
                const float3 sunDirection = float3(-0.382, 0.104, 0.9183);
                float sun = saturate(dot(d, sunDirection));
                float3 horizon = lerp(float3(0.71, 0.37, 0.34), float3(1.0, 0.63, 0.37), pow(sun, 7));
                float3 blue = lerp(float3(0.45, 0.36, 0.52), float3(0.085, 0.25, 0.43), smoothstep(0.08, 0.51, height));
                float3 color = lerp(horizon, blue, smoothstep(0.008, 0.38, height));
                color += pow(sun, 82) * float3(0.35, 0.16, 0.06);
                float cloud = exp(-abs(d.y - 0.21 - d.x * 0.056 - sin(d.x * 11) * 0.009) * 68);
                cloud *= smoothstep(-0.67, -0.37, d.x) * (1 - smoothstep(0.39, 0.71, d.x));
                color += cloud * float3(0.19, 0.086, 0.064);
                float disk = smoothstep(0.9990, 0.9992, sun);
                color = lerp(color, float3(1, 0.83, 0.59) * 1.4, disk);
                float gray = dot(color, float3(0.2126, 0.7152, 0.0722));
                return half4(lerp(color, gray.xxx, _RoadsterGrayscale), 1);
            }
            ENDHLSL
        }
    }
}
