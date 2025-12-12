Shader "UI/GlassInScroll"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite",2D)="white"{}
        _TintColor("Tint",Color)=(1,1,1,0.34)
        _CornerRadius("Corner Radius (0~0.5)",Range(0,0.5))=0.28
        _EdgeSoft("Edge Softness",Range(0,0.02))=0.006

        _BorderWidth("Border Width",Range(0,0.02))=0.003
        _BorderColor("Border Color",Color)=(1,1,1,0.12)

        _UseBackground("Use Background (0/1)",Float)=1
        _BlurSize("Base Blur Amount",Range(0,8))=3.5
        _FrostStrength("Frost Strength",Range(0,1))=0.48

        _DiffusionStrength("Diffusion Strength",Range(0,2))=0.75
        _DiffusionRadius("Diffusion Radius",Range(0,12))=6.0
        _DiffusionAniso("Diffusion Anisotropy (0=±ÕÀÏ,1=±æ°Ô)",Range(0,1))=0.35
        _DiffusionThreshold("Bright Threshold",Range(0,1))=0.55
        _DiffusionSoftness("Threshold Softness",Range(0,0.5))=0.18
        _RefractAmount("Tiny Refraction",Range(0,2))=0.15

        _FlowDir("Flow Dir (x,y)",Vector)=(1,0,0,0)
        _FlowSpeed("Flow Speed",Range(0,4))=1.25
        _FlowWidth("Flow Width",Range(0,0.5))=0.14
        _FlowFeather("Flow Feather",Range(0,0.25))=0.05
        _FlowIntensity("Flow Intensity",Range(0,2))=0.38
        _FlowColor("Flow Color",Color)=(1,1,1,1)
        _FlowNoiseAmp("Flow Noise Amp",Range(0,0.05))=0.012
        _FlowNoiseScale("Flow Noise Scale",Range(1,32))=12
        _FlowGradient("Flow Gradient",Range(0,1))=0.35

        _SpecularStrength("Specular Strength",Range(0,1))=0.22
        _SpecularWidth("Specular Width",Range(0,0.5))=0.22
        _SpecularColor("Specular Color", Color) = (1,1,1,1)

        _ShadowColor("Outer Shadow",Color)=(0,0,0,0.40)
        _ShadowOffset("Shadow Offset",Vector)=(0,-0.02,0,0)
        _ShadowSoft("Shadow Softness",Range(0,0.05))=0.02

        // UI Stencil (Mask/ScrollRect)
    [HideInInspector]_StencilComp("Stencil Comparison", Float) = 8
    [HideInInspector]_Stencil("Stencil ID", Float) = 0
    [HideInInspector]_StencilOp("Stencil Operation", Float) = 0
    [HideInInspector]_StencilWriteMask("Stencil Write Mask", Float) = 255
    [HideInInspector]_StencilReadMask("Stencil Read Mask", Float) = 255

    [HideInInspector]_ColorMask("Color Mask", Float) = 15
    [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off

        GrabPass{"_GrabTex"}

        Pass
        {
            Name "GlassInScroll"

    Stencil
    {
        Ref [_Stencil]
        Comp [_StencilComp]
        Pass [_StencilOp]
        ReadMask [_StencilReadMask]
        WriteMask [_StencilWriteMask]
    }

    ColorMask [_ColorMask]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex, _GrabTex;

            sampler2D _SceneTex;
            float4 _SceneTex_TexelSize;

            float4 _MainTex_TexelSize;
            float4 _TintColor, _BorderColor, _FlowColor, _ShadowColor, _SpecularColor;

            float _CornerRadius, _EdgeSoft, _BorderWidth;
            float _UseBackground, _BlurSize, _FrostStrength;

            float _DiffusionStrength, _DiffusionRadius, _DiffusionAniso, _DiffusionThreshold, _DiffusionSoftness, _RefractAmount;

            float2 _FlowDir; float _FlowSpeed, _FlowWidth, _FlowFeather, _FlowIntensity, _FlowNoiseAmp, _FlowNoiseScale, _FlowGradient;

            float _SpecularStrength, _SpecularWidth;
            float2 _ShadowOffset; float _ShadowSoft;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 sp:TEXCOORD1; };

            v2f vert(appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.sp=ComputeScreenPos(o.pos); return o; }

            float sdRoundRect(float2 uv,float r){
                float2 p=uv-0.5;
                float2 b=0.5-r;
                float2 d=abs(p)-b;
                return length(max(d,0.0))+min(max(d.x,d.y),0.0)-r;
            }

            float hash21(float2 p){
                p=frac(p*float2(123.34,345.45));
                p+=dot(p,p+34.345);
                return frac(p.x*p.y);
            }

            void flowSpace(float2 uv,float2 dir,out float s01,out float y01){
                float2 p=uv-0.5;
                dir = (dir.x==0 && dir.y==0) ? float2(1,0) : normalize(dir);
                float2 g=float2(-dir.y,dir.x);
                float x=dot(p,dir), y=dot(p,g);
                float ext=0.5*(abs(dir.x)+abs(dir.y));
                s01=saturate((x/(2*ext))+0.5);
                y01=saturate((y/(2*ext))+0.5);
            }

            float gauss(float x,float sigma){ float k = x/sigma; return exp(-0.5*k*k); }

            float3 SampleScene(float2 uv)
            {
                if (_SceneTex_TexelSize.z > 0.0 && _SceneTex_TexelSize.w > 0.0)
                    return tex2D(_SceneTex, uv).rgb;
                else
                    return tex2D(_GrabTex, uv).rgb;
            }

            float3 sampleDiffusion(float2 suv, float2 px, float2 dir, float radius, float aniso, float thr, float soft)
            {
                dir = (dir.x==0 && dir.y==0) ? float2(1,0) : normalize(dir);
                float2 g = float2(-dir.y, dir.x);

                float3 acc = 0; float wsum = 0;
                [unroll] for(int k=1;k<=6;k++){
                    float t = k / 6.0;
                    float rLong = radius * lerp(1.0, 1.8, aniso) * t;
                    float rShort= radius * lerp(1.0, 0.6, aniso) * t;

                    float2 offA = (dir * rLong) * px;
                    float2 offB = (g   * rShort) * px;

                    float2 offs[4] = { offA, -offA, offB, -offB };
                    [unroll] for(int j=0;j<4;j++){
                        float2 uvS = suv + offs[j];
                        float3 c = SampleScene(uvS);

                        float lum = dot(c, float3(0.2126,0.7152,0.0722));
                        float m = smoothstep(thr-soft, thr+soft, lum);

                        float dist = length(offs[j] / px);
                        float w = gauss(dist, radius*0.66) * m;

                        acc += c * w;
                        wsum += w;
                    }
                }

                return (wsum > 1e-5) ? (acc / wsum) : SampleScene(suv);
            }

            fixed4 frag(v2f i):SV_Target
            {
                float2 uv = i.uv;
                float2 suv = i.sp.xy / i.sp.w;

                float sdf = sdRoundRect(uv, _CornerRadius);
                float aSDF = saturate(1.0 - sdf/max(_EdgeSoft,1e-5));

                float2 shUV = suv + _ShadowOffset * _MainTex_TexelSize.xy;
                float shA = smoothstep(0.0, _ShadowSoft, -sdf);
                float4 sh = float4(_ShadowColor.rgb, _ShadowColor.a * shA);

                float3 baseBG = _TintColor.rgb;
                if (_UseBackground > 0.5)
                {
                    float2 bo = _MainTex_TexelSize.xy * _BlurSize;

                    float3 b=0;
                    b += SampleScene(suv + bo*float2(-1,-1));
                    b += SampleScene(suv + bo*float2( 1,-1));
                    b += SampleScene(suv + bo*float2(-1, 1));
                    b += SampleScene(suv + bo*float2( 1, 1));
                    b += SampleScene(suv);
                    b *= 0.2;

                    float3 diff = sampleDiffusion(suv, _MainTex_TexelSize.xy, _FlowDir, _DiffusionRadius, _DiffusionAniso, _DiffusionThreshold, _DiffusionSoftness);

                    float2 refrOff = (uv-0.5) * _MainTex_TexelSize.xy * _RefractAmount;
                    float3 refr = SampleScene(suv + refrOff);

                    float3 frost = lerp(refr, b, 0.65);
                    frost = lerp(frost, diff, _DiffusionStrength);

                    baseBG = lerp(frost, _TintColor.rgb, _FrostStrength);
                }

                float2 dir = (_FlowDir.x==0&&_FlowDir.y==0)?float2(1,0):normalize(_FlowDir);
                float s01, y01; flowSpace(uv, dir, s01, y01);
                float t = _Time.y * _FlowSpeed;
                float wave = 0.5 + 0.5 * sin((s01*6.28318) + t);
                float n = hash21(float2(s01,y01) * _FlowNoiseScale);
                float band = smoothstep(0.5 - _FlowWidth - wave*_FlowFeather,
                                        0.5 + _FlowWidth + wave*_FlowFeather,
                                        wave + n * _FlowNoiseAmp);
                band *= (1.0 - _FlowGradient) + y01 * _FlowGradient;
                float3 flowRGB = _FlowColor.rgb * band * _FlowIntensity;

                float rim = pow(saturate(1.0 - abs(sdf/_SpecularWidth)), 3.0);
                float3 rimRGB = _SpecularColor.rgb * rim * _SpecularStrength;

                float bmask = (_BorderWidth>1e-5) ? smoothstep(_BorderWidth+_EdgeSoft, _EdgeSoft, abs(sdf)) : 0.0;

                float3 col = baseBG + flowRGB + rimRGB;
                col = lerp(col, _BorderColor.rgb, bmask * _BorderColor.a);
                float alpha = max(_TintColor.a * aSDF, bmask * _BorderColor.a);

                float4 outC = float4(col, alpha);
                float4 res = outC + float4(_ShadowColor.rgb, _ShadowColor.a * shA) * (1 - outC.a);
                res.a = saturate(outC.a + (_ShadowColor.a * shA) * (1 - outC.a));


                return res;
            }
            ENDCG
        }
    }
}
