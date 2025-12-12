Shader "UI/LogoSparkle"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _Emission ("Emission Intensity", Range(0,10)) = 1

        [Toggle(_PULSE_ON)] _PulseOn ("Enable Pulse", Float) = 1
        _PulseAmp ("Pulse Amplitude", Range(0,2)) = 0.35
        _PulseSpeed ("Pulse Speed", Range(0,10)) = 1.2

        [Toggle(_SWEEP_ON)] _SweepOn ("Enable Sweep", Float) = 1
        _SweepIntensity ("Sweep Intensity", Range(0,4)) = 1.0
        _SweepWidth ("Sweep Width", Range(0.01,1)) = 0.25
        _SweepSharpness ("Sweep Sharpness", Range(0.1,16)) = 4
        _SweepSpeed ("Sweep Speed (UV units/s)", Range(-5,5)) = 0.6
        _SweepAngle ("Sweep Angle (deg)", Range(-180,180)) = 35

        [Toggle(_SPARKLE_ON)] _SparkleOn ("Enable Sparkle", Float) = 1
        _SparkleTex ("Sparkle Noise", 2D) = "gray" {}
        _SparkleScale ("Sparkle Scale", Range(0.5,8)) = 2.5
        _SparkleSpeed ("Sparkle Scroll Speed", Range(-5,5)) = 0.8
        _SparkleThreshold ("Sparkle Threshold", Range(0,1)) = 0.7
        _SparkleIntensity ("Sparkle Intensity", Range(0,4)) = 1.2

        [Toggle(_USE_MASK)] _UseMask ("Use Sparkle Mask", Float) = 0
        _MaskTex ("Mask (white=glow)", 2D) = "white" {}

        [Toggle(_EMISSION_A_TO_ALPHA)] _EmissionA2Alpha ("Emission A affects Alpha", Float) = 0
        _EmissionAlphaMul ("Emission->Alpha Multiplier", Range(0,2)) = 0.5

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
    }

    SubShader
    {
        Tags {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
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
            #pragma target 3.0

            #pragma shader_feature_local _PULSE_ON
            #pragma shader_feature_local _SWEEP_ON
            #pragma shader_feature_local _SPARKLE_ON
            #pragma shader_feature_local _USE_MASK
            #pragma shader_feature_local _EMISSION_A_TO_ALPHA
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            fixed4 _EmissionColor;
            float  _Emission;

            // Pulse
            float _PulseAmp;
            float _PulseSpeed;

            // Sweep
            float _SweepIntensity;
            float _SweepWidth;
            float _SweepSharpness;
            float _SweepSpeed;
            float _SweepAngle;

            sampler2D _SparkleTex;
            float4 _SparkleTex_ST;
            float _SparkleScale;
            float _SparkleSpeed;
            float _SparkleThreshold;
            float _SparkleIntensity;

            sampler2D _MaskTex;

            float _EmissionAlphaMul;

            float4 _ClipRect;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                o.worldPos = v.vertex;
                return o;
            }

            float2 rotateUV(float2 uv, float angleDeg)
            {
                float a = radians(angleDeg);
                float s = sin(a), c = cos(a);
                float2x2 m = float2x2(c, -s, s, c);
                uv -= 0.5;
                uv = mul(m, uv);
                uv += 0.5;
                return uv;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                #ifdef UNITY_UI_CLIP_RECT
                if (UnityGet2DClipping(i.worldPos, _ClipRect) > 0)
                    discard;
                #endif

                fixed4 baseCol = tex2D(_MainTex, i.uv) * i.color;
                clip(baseCol.a - 0.001);

                float emissive = _Emission;

                #if defined(_PULSE_ON)
                {
                    float pulse = (sin(_Time.y * _PulseSpeed * 6.2831853) * 0.5 + 0.5);
                    pulse = pow(pulse, 1.2);
                    emissive += pulse * _PulseAmp;
                }
                #endif

                #if defined(_SWEEP_ON)
                {
                    float2 suv = rotateUV(i.uv, _SweepAngle);
                    float bandCenter = frac(_Time.y * _SweepSpeed);
                    float px = frac(suv.x);
                    float d = abs(px - bandCenter);
                    d = min(d, 1.0 - d);
                    float sweep = exp(-pow(d / max(1e-4, _SweepWidth), _SweepSharpness));
                    emissive += sweep * _SweepIntensity;
                }
                #endif

                #if defined(_SPARKLE_ON)
                {
                    float2 nUV = i.uv * _SparkleScale;
                    nUV += float2(_Time.y * _SparkleSpeed, _Time.y * -_SparkleSpeed * 0.73);
                    float n = tex2D(_SparkleTex, nUV).r;
                    float spark = saturate((n - _SparkleThreshold) / max(1e-4, 1.0 - _SparkleThreshold));
                    spark *= (sin((i.uv.x + i.uv.y) * 20 + _Time.y * 7.0) * 0.5 + 0.5);
                    emissive += spark * _SparkleIntensity;
                }
                #endif

                #if defined(_USE_MASK)
                {
                    float m = tex2D(_MaskTex, i.uv).r;
                    emissive *= m;
                }
                #endif

                emissive *= baseCol.a;

                fixed3 rgb = baseCol.rgb + _EmissionColor.rgb * emissive;

                float a = baseCol.a;
                #if defined(_EMISSION_A_TO_ALPHA)
                {
                    float addA = _EmissionColor.a * emissive * _EmissionAlphaMul;
                    a = saturate(a + addA * baseCol.a);
                }
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(a - 0.001);
                #endif

                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
