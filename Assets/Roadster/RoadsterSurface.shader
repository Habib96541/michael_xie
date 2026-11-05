Shader "Roadster/Highway Surface"
{
    Properties
    {
        [MainColor] _BaseColor("Base color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Color map", 2D) = "white" {}
        [Toggle(_ROADSTER_TEXTURED)] _UseColorMap("Use color map", Float) = 0
        _Shine("Shine", Range(0, 1)) = 0
        _Glow("Glow", Range(0, 2)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            Name "Highway Surface"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local_fragment _ _ROADSTER_TEXTURED
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Shine;
                half _Glow;
            CBUFFER_END
            half _RoadsterGrayscale;
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half3 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = SRGBToLinear(input.color.rgb) * _BaseColor.rgb;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                const half3 sun = half3(-0.41, 0.29, 0.865);
                const half3 fill = half3(0.55, 0.60, -0.58);
                half3 n = normalize(input.normalWS);
                float3 eye = _WorldSpaceCameraPos - input.positionWS;
                half3 v = normalize(eye);
                half diffuse = saturate(dot(n, sun));
                half sky = saturate(n.y * 0.7 + 0.3);
                half spec = pow(saturate(dot(n, normalize(sun + v))), 28);
                half rim = pow(1 - saturate(dot(n, v)), 3) * sky;
                half3 light = half3(0.29, 0.24, 0.34) + sky * half3(0.25, 0.25, 0.37)
                    + diffuse * half3(0.97, 0.56, 0.35) + saturate(dot(n, fill)) * half3(0.37, 0.28, 0.32);
                #if defined(_ROADSTER_TEXTURED)
                    half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                #else
                    half3 baseColor = input.color;
                #endif
                half3 color = baseColor * (light + _Glow);
                color += (spec * half3(1, 0.59, 0.34) + rim * half3(0.12, 0.17, 0.27)) * _Shine;
                half fog = 1 - exp(-max(0, length(eye) - 20) * 0.0069);
                color = lerp(color, half3(0.54, 0.32, 0.37), fog);
                half gray = dot(color, half3(0.2126, 0.7152, 0.0722));
                return half4(lerp(color, gray.xxx, _RoadsterGrayscale), 1);
            }
            ENDHLSL
        }
    }
}
