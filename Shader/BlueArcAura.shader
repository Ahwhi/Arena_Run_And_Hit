Shader "UI/BlueArcAura"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite",2D) = "white" {}
        _Color("Base Tint", Color) = (1,1,1,1)

        [Header(Aura)]
        _AuraColor("Aura Color", Color) = (0.35,0.55,1.0,1)
        _AuraIntensity("Aura Intensity", Range(0,4)) = 1.7
        _SmokeAlpha("Smoke Alpha", Range(0,1)) = 0.85
        _NoiseScale("Noise Scale", Range(1,20)) = 7.0
        _NoiseSpeed("Noise Speed", Range(0,5)) = 1.2

        [Header(Ring)]
        _Radius("Radius", Range(0.0, 0.5)) = 0.22
        _Thickness("Ring Thickness", Range(0.0, 0.5)) = 0.06
        _RingGlow("Ring Glow", Range(0,6)) = 2.2

        [Header(Arcs)]
        _ArcColor("Arc Color", Color) = (0.55,0.8,1.2,1)
        _ArcCount("Arc Count", Range(1,12)) = 4
        _ArcWidth("Arc Angular Width", Range(0.01,0.6)) = 0.2
        _ArcGlow("Arc Glow", Range(0,8)) = 3.2
        _ArcJitter("Arc Jitter", Range(0,1)) = 0.55
        _ArcSpeed("Arc Rotate Speed", Range(-8,8)) = 2.0

        [Header(Common)]
        _TimeScale("Time Scale", Range(0,3)) = 1.0
        _CenterShift("Center Shift (x,y)", Vector) = (0.0, 0.0, 0, 0)
        _Soft("Edge Softness", Range(0.0001, 0.05)) = 0.01
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        // 부드러운 가산 + UI 알파 유지
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float4 _AuraColor;
            float _AuraIntensity;
            float _SmokeAlpha;
            float _NoiseScale;
            float _NoiseSpeed;

            float _Radius;
            float _Thickness;
            float _RingGlow;

            float4 _ArcColor;
            float _ArcCount;
            float _ArcWidth;
            float _ArcGlow;
            float _ArcJitter;
            float _ArcSpeed;

            float _TimeScale;
            float2 _CenterShift;
            float _Soft;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // ---- small hash & noise (fbm) ----
            float hash21(float2 p)
            {
                p = frac(p*float2(123.34, 345.45));
                p += dot(p, p+34.345);
                return frac(p.x*p.y);
            }

            float2 hash22(float2 p)
            {
                float n = sin(dot(p,float2(41,289)));
                return frac(float2(262144.0, 32768.0)*n);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f*f*(3.0-2.0*f);

                float a = hash21(i);
                float b = hash21(i+float2(1,0));
                float c = hash21(i+float2(0,1));
                float d = hash21(i+float2(1,1));
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            float fbm(float2 p)
            {
                float s = 0.0;
                float a = 0.5;
                float2x2 m = float2x2(1.6,1.2,-1.2,1.6);
                [unroll(5)]
                for(int i=0;i<5;i++)
                {
                    s += a*noise(p);
                    p = mul(m,p)+0.5;
                    a *= 0.5;
                }
                return s;
            }

            // distance to ring band
            float sdRing(float r, float R, float T)
            {
                float d = abs(r - R) - T*0.5;
                return d;
            }

            // gaussian-ish glow from distance
            float glow(float d, float g)
            {
                return exp(-max(d,0)*g*8.0);
            }

            // angle wrap [-pi,pi]
            float angDiff(float a, float b)
            {
                float d = a - b;
                d = fmod(d + 3.14159265, 6.2831853) - 3.14159265;
                return abs(d);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sprite sample for masking (optional)
                fixed4 baseTex = tex2D(_MainTex, i.uv);
                float2 uv = i.uv;

                // pivot center (0.5,0.5) + optional shift
                float2 p = uv - 0.5 - _CenterShift;
                float t = (_Time.y + _Time.w) * _TimeScale; // stable UI time

                float r = length(p);
                float a = atan2(p.y, p.x); // [-pi,pi]

                // --- SMOKEY AURA ---
                float2 nUV = p * _NoiseScale + float2(0.0, t*_NoiseSpeed);
                float smoke = fbm(nUV);
                // radial falloff outside ring
                float outer = smoothstep(_Radius+_Thickness*0.5, _Radius+_Thickness*0.5+0.18, r);
                float inner = 1.0 - smoothstep(_Radius-0.06, _Radius+0.02, r);
                float auraMask = saturate(outer + inner*0.35);
                float aura = auraMask * smoke * _SmokeAlpha;

                // --- MAIN RING + glow ---
                float dRing = sdRing(r, _Radius, _Thickness);
                float ringCore = 1.0 - smoothstep(_Soft, _Soft*2.0, abs(dRing));
                float ringGlow = glow(abs(dRing), _RingGlow);

                // --- LIGHTNING ARCS (angular bands with jitter) ---
                float arcs = 0.0;
                int N = (int)round(_ArcCount);
                [loop]
                for(int k=0;k<12;k++)
                {
                    if(k>=N) break;
                    float seed = (k+1)*37.7;
                    float ang0 = ( (k/(float)max(N,1)) + t*_ArcSpeed*0.05 + hash21(float2(seed,seed)) ) * 6.2831853;

                    // jaggedness along angle + radius
                    float jag = (noise(float2(a*2.5 + seed, t*3.0)) - 0.5) * _ArcJitter;
                    float rWarp = r + jag*0.04;

                    // angular width
                    float ad = angDiff(a, ang0);
                    float band = 1.0 - smoothstep(_ArcWidth*0.5, _ArcWidth*0.5+0.12, ad);

                    // only show near ring band
                    float dr = abs(sdRing(rWarp, _Radius, _Thickness*0.65));
                    float radial = 1.0 - smoothstep(_Soft*1.2, _Soft*3.2, dr);

                    // glow
                    float g = glow(dr + ad*0.05, _ArcGlow);
                    arcs += band * radial * g;
                }
                arcs = saturate(arcs);

                // --- COMPOSE ---
                float3 col = 0;

                // aura (soft additive)
                col += _AuraColor.rgb * aura * _AuraIntensity;

                // ring
                float ringVal = max(ringCore*0.85, ringGlow*0.55);
                col += _AuraColor.rgb * ringVal;

                // arcs on top
                col += _ArcColor.rgb * arcs;

                // final alpha: keep UI integrity but allow glow to “bleed”
                float alpha = saturate(baseTex.a * i.color.a + aura*0.5 + ringCore*0.8 + arcs*0.9);

                return fixed4(col, alpha) * i.color;
            }
            ENDCG
        }
    }
}
