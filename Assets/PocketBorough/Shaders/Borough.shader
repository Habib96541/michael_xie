Shader "PocketBorough/City"
{
    Properties { _Tint("Tint",Color)=(1,1,1,1) _Ground("Cut ground",Float)=0 _ClipBelow("Clip sinking geometry",Float)=1 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct A { float4 pos:POSITION; float3 normal:NORMAL; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos:SV_POSITION; float3 world:TEXCOORD0; half3 normal:TEXCOORD1; half4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint; float _Ground; float _ClipBelow;
            CBUFFER_END
            float4 _BoroughHoles[8];
            V vert(A i){ UNITY_SETUP_INSTANCE_ID(i); V o; o.world=TransformObjectToWorld(i.pos.xyz);o.pos=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normal);o.color=half4(SRGBToLinear(i.color.rgb)*_Tint.rgb,i.color.a);return o; }
            half4 frag(V i):SV_Target
            {
                if(_ClipBelow>.5)clip(i.world.y+.08);
                if(_Ground>.5){ for(int k=0;k<8;k++){float2 delta=i.world.xz-_BoroughHoles[k].xy;clip(dot(delta,delta)-_BoroughHoles[k].z*_BoroughHoles[k].z);}}
                Light l=GetMainLight(TransformWorldToShadowCoord(i.world));half ndl=saturate(dot(normalize(i.normal),l.direction));
                half shade=.68+.32*ndl*l.shadowAttenuation;
                return half4(i.color.rgb*shade,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 pos:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};struct V{float4 pos:SV_POSITION;float y:TEXCOORD0;};
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;float _Ground;float _ClipBelow;
            CBUFFER_END
            V vert(A i){UNITY_SETUP_INSTANCE_ID(i);V o;float3 p=TransformObjectToWorld(i.pos.xyz);o.pos=TransformWorldToHClip(p);o.y=p.y;return o;}
            half4 frag(V i):SV_Target{if(_ClipBelow>.5)clip(i.y+.08);return 0;}
            ENDHLSL
        }
    }
}
