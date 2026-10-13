// =============================================================================
// LEAD KHÔNG PHANH - Cinematic Nitro Exhaust
// NitroFlame.shader   (URP / Unity 6)
//
// - Gradient 3 tầng: blue/white core -> orange/yellow body -> red outer edge
// - Scrolling value-noise: flame tongues + distortion + erosion  ->  KHÔNG BAO
//   GIỜ nhìn thấy hình chữ nhật của texture / billboard
// - Vertex wobble: ngọn lửa uốn lượn hữu cơ, bất đối xứng
// - Tapered tip: thu nhọn dần về cuối
// - Curved World hook (_Curvature / _Trimming) để khớp với shader cong thế giới
//   của game (thêm material này vào danh sách curveMaterials trong CameraController)
//
// Dùng cho cả MeshRenderer (lưỡi lửa) lẫn ParticleSystemRenderer.
// =============================================================================

Shader "LEADKhongPhanh/NitroFlame"
{
    Properties
    {
        [MainTexture] _BaseMap       ("Flame Mask", 2D) = "white" {}

        [HDR] _ColorCore  ("Core Color (blue/white)", Color) = (0.55, 0.88, 1.0, 1)
        [HDR] _ColorMid   ("Mid Color (yellow)",      Color) = (1.0, 0.72, 0.20, 1)
        [HDR] _ColorOuter ("Outer Color (red)",       Color) = (1.0, 0.16, 0.02, 1)

        _Intensity   ("Intensity", Range(0, 4)) = 1
        _Exposure    ("Exposure",  Range(0, 4)) = 1.35
        _Seed        ("Seed", Float) = 0
        _Flicker     ("Flicker", Range(0,1)) = 0.5

        _ScrollSpeed ("Scroll Speed", Range(0, 20)) = 4.5
        _NoiseScale  ("Noise Scale",  Range(0.2, 12)) = 3.2
        _Distort     ("Distortion",   Range(0, 0.5)) = 0.13
        _Erosion     ("Erosion (flame tongues)", Range(0, 1)) = 0.42
        _TipFade     ("Tip Fade", Range(0.2, 5)) = 1.5
        _SoftEdge    ("Soft Edge", Range(0.001, 1)) = 0.35
        _CoreBoost   ("Core Boost", Range(0, 2)) = 0.85

        _WobbleAmp   ("Wobble Amplitude", Range(0, 0.6)) = 0.10
        _WobbleFreq  ("Wobble Frequency", Range(0, 20)) = 5.5
        _WobbleSpeed ("Wobble Speed", Range(0, 30)) = 8

        _Curvature   ("Curved World - Curvature", Float) = 0
        _Trimming    ("Curved World - Trimming", Float) = 0

        [HideInInspector] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"       = "Transparent"
            "Queue"            = "Transparent+100"
            "RenderPipeline"   = "UniversalPipeline"
            "IgnoreProjector"  = "True"
            "PreviewType"      = "Plane"
        }

        Pass
        {
            Name "NitroFlameUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One          // Additive
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _ColorCore;
                float4 _ColorMid;
                float4 _ColorOuter;
                float  _Intensity;
                float  _Exposure;
                float  _Seed;
                float  _Flicker;
                float  _ScrollSpeed;
                float  _NoiseScale;
                float  _Distort;
                float  _Erosion;
                float  _TipFade;
                float  _SoftEdge;
                float  _CoreBoost;
                float  _WobbleAmp;
                float  _WobbleFreq;
                float  _WobbleSpeed;
                float  _Curvature;
                float  _Trimming;
                float  _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            // ---- cheap value noise -------------------------------------------------
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                return vnoise(p) * 0.60
                     + vnoise(p * 2.13 + 7.7) * 0.28
                     + vnoise(p * 4.37 + 3.1) * 0.12;
            }
            // -----------------------------------------------------------------------

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 pos = IN.positionOS.xyz;

                // Lửa phụt theo LOCAL +Z -> uốn lượn trên X/Y, mạnh dần về ngọn.
                float t = _Time.y * _WobbleSpeed + _Seed * 13.7;
                float along = saturate(IN.uv.y);
                float amp = _WobbleAmp * along * along;

                pos.x += sin(pos.z * _WobbleFreq + t) * amp;
                pos.y += cos(pos.z * _WobbleFreq * 0.77 + t * 1.23) * amp * 0.75;

                float3 positionWS = TransformObjectToWorld(pos);
                float3 positionVS = TransformWorldToView(positionWS);

                // Curved World hook (để 0 nếu không dùng)
                positionVS.y -= _Curvature * positionVS.z * positionVS.z;
                positionVS.x += _Trimming  * positionVS.z * positionVS.z;

                OUT.positionCS = TransformWViewToHClip(positionVS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float v = saturate(uv.y);          // 0 = ống bô, 1 = ngọn lửa
                float t = _Time.y;

                // scrolling noise
                float2 np = float2(uv.x * _NoiseScale,
                                   uv.y * _NoiseScale * 0.65 - t * _ScrollSpeed) + _Seed * 3.77;
                float n = fbm(np);
                float n2 = fbm(np * 1.9 + float2(11.3, -t * _ScrollSpeed * 0.45));

                // distort UV -> phá vỡ đường biên hình học
                float2 duv = uv;
                duv.x += (n - 0.5) * _Distort * (0.25 + v * 1.4);
                duv.y += (n2 - 0.5) * _Distort * 0.5;

                float mask = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, saturate(duv)).a;

                // erosion -> tách thành nhiều flame tongues, mạnh dần về ngọn
                float erode = _Erosion * (0.18 + v * 1.25) * (0.65 + _Flicker * 0.7);
                mask = saturate((mask - erode * n2) / max(0.0001, 1.0 - erode));

                // tapered tip + soft edge ngang
                mask *= saturate(pow(saturate(1.0 - v), _TipFade) * 1.75);
                mask *= smoothstep(0.0, _SoftEdge, 1.0 - abs(uv.x * 2.0 - 1.0));

                // gradient nhiệt: đỏ (ngoài) -> vàng (thân) -> xanh/trắng (lõi, sát ống bô)
                float heat = saturate(mask * (1.45 - v * 0.95));
                float3 col = lerp(_ColorOuter.rgb, _ColorMid.rgb, saturate(heat * 1.7));

                float coreMask = saturate((heat - 0.60) * 2.9) * saturate(1.0 - v * 2.1);
                col = lerp(col, _ColorCore.rgb, coreMask * _CoreBoost);

                float alpha = saturate(mask * _Intensity) * IN.color.a;
                col *= IN.color.rgb * (_Exposure * (0.55 + _Intensity * 0.6));

                clip(alpha - 0.003);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
