Shader "UI/TabButton"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _GradTopColor    ("Grad Top", Color) = (1,1,1,1)
        _GradBottomColor ("Grad Bottom", Color) = (1,1,1,1)
        _GradBlend       ("Grad Blend (0~1)", Range(0,1)) = 0.35
        _GradAlphaInfluence ("Grad Alpha Influence", Range(0,1)) = 1.0
        _GradBias        ("Grad Y Bias (-1~1)", Range(-1,1)) = 0.0
        _GradScale       ("Grad Y Scale (0.1~4)", Range(0.1,4)) = 1.0
        _GradEase        ("Grad Ease (0=Linear,1=Quintic)", Range(0,1)) = 0.8

        _UnderlineColorTop    ("Line Top", Color) = (1,1,1,1)
        _UnderlineColorBottom ("Line Bottom", Color) = (1,1,1,1)
        _UnderlineHeight ("Line Height (0~1.0)", Range(0,1.0)) = 0.12
        _UnderlineSoft   ("Line Softness", Range(0.0001,2.0)) = 0.035
        _UnderlineOffset ("Line Y Offset (-2~2)", Range(-2,2)) = 0.0
        _UnderlineEase   ("Line Edge Ease (0~1)", Range(0,1)) = 0.8

        _ActiveAmt ("Active Amount", Range(0,1)) = 0
        _HoverAmt  ("Hover Amount", Range(0,1)) = 0
        _HoverGlow ("Hover Glow Strength", Range(0,1)) = 0.25

        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil ("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [HideInInspector]_ClipRect ("Clip Rect", Vector) = ( -32767, -32767, 32767, 32767)
    }
    SubShader
    {
        Tags{
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Stencil{
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UI"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t{
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 texcoord1: TEXCOORD1;
            };
            struct v2f{
                float4 pos      : SV_POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float2 maskUV   : TEXCOORD2;
            };

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color;

            fixed4 _GradTopColor, _GradBottomColor;
            float _GradBlend, _GradAlphaInfluence, _GradBias, _GradScale, _GradEase;

            fixed4 _UnderlineColorTop, _UnderlineColorBottom;
            float _UnderlineHeight, _UnderlineSoft, _UnderlineOffset, _UnderlineEase;

            float _ActiveAmt, _HoverAmt, _HoverGlow;
            float4 _ClipRect; float _UseUIAlphaClip;

            float smooth5(float x){ return x*x*x*(x*(6*x-15)+10); }

            v2f vert(appdata_t v){
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color    = v.color * _Color;
                o.worldPos = v.vertex;
                o.maskUV   = v.texcoord1.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // ===== Body gradient with bias/scale/ease =====
                // uv.y -> bias/scale -> [0,1]
                float ty = (i.uv.y - 0.5 - _GradBias * 0.5) * _GradScale + 0.5;
                ty = saturate(ty);
                // ease
                float tyEase = lerp(ty, smooth5(ty), _GradEase);
                fixed4 gcol  = lerp(_GradBottomColor, _GradTopColor, tyEase);

                // RGB blend
                col.rgb = lerp(col.rgb, col.rgb * gcol.rgb, _GradBlend);
                col.a *= lerp(1.0, gcol.a, _GradAlphaInfluence);

                float2 c = float2(0.5, 0.6);
                float d  = saturate(1.0 - distance(i.uv, c) * 1.8);
                d = lerp(d, smooth5(d), 0.75);
                col.rgb += col.rgb * d * _HoverGlow * _HoverAmt;

                float half = 0.5;
                float offY = _UnderlineOffset * half;
                float y0 = saturate(1.0 - _UnderlineHeight - offY);
                float y1 = saturate(1.0 - offY);

                float topEdge    = smoothstep(y0 - _UnderlineSoft, y0 + _UnderlineSoft, i.uv.y);
                float bottomEdge = 1.0 - smoothstep(y1 - _UnderlineSoft, y1 + _UnderlineSoft, i.uv.y);
                float uMask = saturate(topEdge * bottomEdge);
                uMask = lerp(uMask, smooth5(uMask), _UnderlineEase);

                float uy = 0.0;
                float denom = max(1e-4, (y1 - y0));
                uy = saturate((i.uv.y - y0) / denom);
                float uyEase = lerp(uy, smooth5(uy), _UnderlineEase);

                fixed4 ucol = lerp(_UnderlineColorBottom, _UnderlineColorTop, uyEase);

                float underlineAmt = saturate(_ActiveAmt + (1.0 - _ActiveAmt) * (0.65 * _HoverAmt));
                float uAlpha = uMask * underlineAmt * ucol.a;

                col.rgb = lerp(col.rgb, ucol.rgb, uAlpha);
                col.a   = 1.0 - (1.0 - col.a) * (1.0 - uAlpha);

                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);

            #if UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
            #endif
                return col;
            }
            ENDCG
        }
    }
}
