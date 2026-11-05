Shader "Bull Ride/Rodeo Surface"
{
    Properties { _Style ("Surface style", Float) = 0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Style;
            float4 _RideSpot;
            struct Input { float4 position : POSITION; float3 normal : NORMAL; float4 color : COLOR; };
            struct Output { float4 position : SV_POSITION; float3 normal : TEXCOORD0; float3 world : TEXCOORD1; float4 color : COLOR; };

            Output vert(Input v)
            {
                Output o;
                o.position = UnityObjectToClipPos(v.position);
                o.world = mul(unity_ObjectToWorld, v.position).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                return o;
            }

            float grain(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f*f*(3-2*f);
                float4 dots = dot(cell, float2(127.1, 311.7)) + float4(0, 127.1, 311.7, 438.8);
                float4 h = frac(sin(dots) * 43758.5453);
                return lerp(lerp(h.x,h.y,f.x),lerp(h.z,h.w,f.x),f.y);
            }

            fixed4 frag(Output i) : SV_Target
            {
                if (_Style == 6) return fixed4(i.color.rgb,1);
                float3 n = normalize(i.normal);
                float3 eye = normalize(_WorldSpaceCameraPos - i.world);
                float3 key = normalize(float3(.47,.83,.31));
                float nd = max(0, dot(n, key));
                float rim = pow(1 - saturate(dot(n, eye)), 2) * max(0,dot(n, normalize(float3(-.8,.36,-.3))));
                float radius = length((i.world.xz - _RideSpot.xz) * float2(1,.91));
                float spot = 1 - smoothstep(2.6, 12.5, radius);
                float3 baseColor = i.color.rgb;
                float3 ambient = lerp(float3(.22,.24,.29),float3(.40,.42,.44),n.y*.5+.5);
                float3 color = baseColor * (ambient + (.34+.61*spot)*nd*float3(1.22,.96,.70));
                color += rim * float3(.13,.18,.25) * (.47+.53*spot);
                if (_Style == 1)
                {
                    float2 xz = i.world.xz;
                    float dirt = .76 + .23*grain(xz*.52) + .21*grain(xz*3.9) + .13*grain(xz*38);
                    color *= dirt * (.50+.50*spot);
                    color += spot * float3(.075,.042,.019);
                }
                else if (_Style == 2)
                    color *= .84 + .23*grain(float2(i.world.y*20, (i.world.x+i.world.z)*1.8));
                else if (_Style == 5 || _Style == 7)
                {
                    float spec = pow(max(0,dot(n,normalize(key+eye))),_Style==5?24:14);
                    color += spec * (.18+.18*spot) * (_Style==5 ? float3(.80,.84,.92) : baseColor*.42);
                }
                else if (_Style == 8)
                    color = baseColor * (.42 + .21*nd) + rim*.025;
                else if (_Style == 9)
                    color = baseColor * (.68 + .13*spot);
                float fog = smoothstep(13,49,length(_WorldSpaceCameraPos-i.world))*.71;
                return fixed4(lerp(color,float3(.045,.051,.063),fog),1);
            }
            ENDCG
        }
    }
}
