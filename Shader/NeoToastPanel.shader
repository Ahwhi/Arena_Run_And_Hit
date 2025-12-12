Shader "UI/NeoToastPanel" {
    Properties{
        [PerRendererData]_MainTex("Sprite",2D)="white"{}
        _Color("Tint", Color) = (1,1,1,1)

        // Gradient
        _UseTexture("Use Base Texture (0/1)", Float) = 0
        _GradTop("Gradient Top", Color) = (0.10,0.12,0.16,0.92)
        _GradBottom("Gradient Bottom", Color) = (0.08,0.09,0.12,0.92)
        _GradAngle("Gradient Angle (deg)", Range(0,360)) = 0

        // Shape
        _CornerRadius("Corner Radius (0~0.5, normalized)", Range(0,0.5)) = 0.20
        _CornerRadiusPx("Corner Radius (px, overrides)", Range(0,64)) = 0
        _EdgeSoft("Edge Softness (px)", Range(0,2)) = 1.0

        // Outline (px)
        _OutlineWidth("Outline Width (px)", Range(0,6)) = 1.2
        _OutlineColor("Outline Color", Color) = (1,1,1,0.10)

        // Shadow (px, outside only)
        _ShadowColor("Shadow Color", Color) = (0,0,0,0.65)
        _ShadowOffset("Shadow Offset (px)", Vector) = (0,-4,0,0)
        _ShadowBlur("Shadow Blur (px)", Range(0,16)) = 6

        // UI common
        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil ("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask ("Color Mask", Float) = 15
    }
    SubShader{
        Tags{ "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off Lighting Off ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 luv:TEXCOORD1; float4 col:COLOR; };

            sampler2D _MainTex; float4 _MainTex_TexelSize;
            float4 _Color;
            float _UseTexture; float4 _GradTop,_GradBottom; float _GradAngle;

            float _CornerRadius, _CornerRadiusPx, _EdgeSoft;
            float _OutlineWidth; float4 _OutlineColor;
            float4 _ShadowColor; float4 _ShadowOffset; float _ShadowBlur;

            v2f vert(appdata v){
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                o.luv = v.uv;
                o.col = v.color * _Color;
                return o;
            }

            void uvPixelScale(float2 uv, out float ux, out float uy){
                float2 dudx = ddx(uv);
                float2 dudy = ddy(uv);
                ux = length(float2(dudx.x, dudy.x)) + 1e-6;
                uy = length(float2(dudx.y, dudy.y)) + 1e-6;
            }

            float sdRoundRectPx(float2 ppx, float2 b, float r){
                float2 q = abs(ppx) - (b - r);
                return length(max(q,0)) + min(max(q.x,q.y),0) - r;
            }

            float4 grad(float2 uv, float angDeg){
                float a = radians(angDeg);
                float2 dir = float2(cos(a), sin(a));
                float t = saturate(dot(uv-0.5, dir) + 0.5);
                return lerp(_GradBottom, _GradTop, t);
            }

            float4 frag(v2f i):SV_Target{
                float4 baseCol = (_UseTexture>0.5)? tex2D(_MainTex,i.uv) : grad(i.luv,_GradAngle);
                baseCol *= i.col;

                float ux, uy; uvPixelScale(i.luv, ux, uy);
                float2 halfSizePx = float2(0.5/ux, 0.5/uy);
                float2 ppx = (i.luv - 0.5) / float2(ux, uy);

                float rpx = (_CornerRadiusPx > 0.0)
                            ? _CornerRadiusPx
                            : saturate(_CornerRadius) * 2.0 * min(halfSizePx.x, halfSizePx.y);

                float sd = sdRoundRectPx(ppx, halfSizePx, rpx);

                float feather = max(1.0, _EdgeSoft);
                float ow      = _OutlineWidth;

                float fillA    = 1.0 - smoothstep(0.0, feather, sd);
                float aInner   = 1.0 - smoothstep(-ow - feather, -ow + feather, sd);
                float aOuter   =      smoothstep(   -feather,        feather,  sd);
                float outlineA = aInner * aOuter;

                float shadowA = 0.0;
                if (_ShadowBlur > 0.0 || any(_ShadowOffset.xy != 0)) {
                    float2 ppxSh = ppx - _ShadowOffset.xy;
                    float sdSh   = sdRoundRectPx(ppxSh, halfSizePx, rpx) - _ShadowBlur;
                    float outside = smoothstep(0.0, feather*2.0, sd);
                    float shFeather = max(1.0, _ShadowBlur);
                    shadowA = (1.0 - smoothstep(0.0, shFeather*2.0, sdSh)) * outside;
                }

                // ÇÕ¼º: shadow -> fill -> outline
                float4 shadow  = _ShadowColor;  shadow.a *= shadowA;
                float4 outline = _OutlineColor; outline.a *= outlineA;
                float4 fill    = baseCol;       fill.a   *= fillA;

                float4 col = 0;
                col = lerp(col, shadow,  shadow.a);
                col = lerp(col, fill,    fill.a);
                col = lerp(col, outline, outline.a);

                col.rgb *= col.a;
                return col;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
