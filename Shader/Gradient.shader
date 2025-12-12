Shader "UI/GradientPanel"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        _UseTexture("Use Base Texture (0/1)", Float) = 0
        _GradMode("Gradient Mode (0=Linear,1=Corner)", Float) = 0
        _LinearAngle("Linear Angle (deg)", Range(0,360)) = 0
        _ColorA("From", Color) = (1,0,0,1)
        _ColorB("To", Color)   = (1,1,0,1)
        _CornerBL("Corner BL", Color) = (1,1,1,1)
        _CornerBR("Corner BR", Color) = (1,1,1,1)
        _CornerTL("Corner TL", Color) = (1,1,1,1)
        _CornerTR("Corner TR", Color) = (1,1,1,1)

        _UseRound("Use Rounded (0/1)", Float) = 1
        _RoundRadiusUV("Corner Radius (UV 0..0.5)", Range(0,0.5)) = 0.1
        _EdgeSoftUV("Edge Softness (UV)", Range(0,0.05)) = 0.003

        _OutlineEnable("Enable Outline (0/1)", Float) = 1
        _OutlineWidthUV("Outline Width (UV)", Range(0,0.1)) = 0.02
        _OutlineColor("Outline Color", Color) = (1,1,1,1)

        _EmissionEnable("Enable Emission (0/1)", Float) = 1
        [HDR]_EmissionColor("Emission Color", Color) = (0.5,0.8,1,1)
        _EmissionBoost("Emission Boost", Float) = 1

        _ForceVisible("Force Visible (0/1)", Float) = 1
    }

    SubShader
    {
        Tags{ "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZTest [unity_GUIZTestMode]
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGBA

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex: POSITION;
                float4 color : COLOR;
                float2 uv    : TEXCOORD0;
            };
            struct v2f{
                float4 pos   : SV_POSITION;
                float4 color : COLOR;
                float2 uv    : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float _UseTexture, _GradMode, _LinearAngle;
            fixed4 _ColorA, _ColorB, _CornerBL, _CornerBR, _CornerTL, _CornerTR;

            float _UseRound, _RoundRadiusUV, _EdgeSoftUV;
            float _OutlineEnable, _OutlineWidthUV;
            fixed4 _OutlineColor;

            float _EmissionEnable, _EmissionBoost;
            fixed4 _EmissionColor;

            float _ForceVisible;

            v2f vert(appdata v){
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 LinearGradient(float2 uv){
                float rad = radians(_LinearAngle);
                float2 dir = float2(cos(rad), sin(rad));
                float t = saturate(dot(uv - 0.5, dir) + 0.5);
                return lerp(_ColorA, _ColorB, t);
            }
            fixed4 CornerGradient(float2 uv){
                fixed4 bottom = lerp(_CornerBL, _CornerBR, uv.x);
                fixed4 top    = lerp(_CornerTL, _CornerTR, uv.x);
                return lerp(bottom, top, uv.y);
            }

            float sdRoundedRectUV(float2 uv, float radiusUV)
            {
                float2 p = uv * 2.0 - 1.0;
                float2 r = float2(1.0 - radiusUV*2.0, 1.0 - radiusUV*2.0);
                float2 d = abs(p) - r;
                float outside = length(max(d,0.0)) - radiusUV*2.0;
                float inside  = min(max(d.x,d.y),0.0);
                return outside + inside;
            }

            fixed4 frag(v2f i):SV_Target
            {
                float2 uv01 = saturate(i.uv);

                fixed4 grad = (_GradMode < 0.5) ? LinearGradient(uv01) : CornerGradient(uv01);
                fixed4 tex  = tex2D(_MainTex, uv01);
                fixed4 col  = grad * lerp(fixed4(1,1,1,1), tex, saturate(_UseTexture));
                col *= i.color;

                if (_ForceVisible > 0.5){
                    col.a = 1;
                    return col;
                }

                float edge = 1.0;
                float sd = 0.0;
                if (_UseRound > 0.5)
                {
                    sd = sdRoundedRectUV(uv01, _RoundRadiusUV);
                    edge = smoothstep(0.0, max(_EdgeSoftUV, 1e-5), -sd);
                    col.a *= edge;
                }

                if (_OutlineEnable > 0.5 && _OutlineWidthUV > 0.0)
{
    float sdBase = (_UseRound > 0.5) ? sd : sdRoundedRectUV(uv01, 0.0);
    float aa = max(_EdgeSoftUV, 1e-5);
    float outlineMask = 1.0 - smoothstep(_OutlineWidthUV - aa, _OutlineWidthUV + aa, abs(sdBase));
    outlineMask = saturate(outlineMask);

    col.rgb = lerp(col.rgb, _OutlineColor.rgb, outlineMask * _OutlineColor.a);

    float speed = 1.5;
    float pulse = 0.6 + 0.4 * sin(_Time.y * speed);

    if (_EmissionEnable > 0.5 && outlineMask > 0.0)
        col.rgb += _EmissionColor.rgb * _EmissionColor.a * _EmissionBoost * outlineMask * pulse;
}

                return col;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
