// =============================================================================
// LEAD KHÔNG PHANH - Cinematic Nitro Exhaust
// NitroHeatDistortion.shader   (URP / Unity 6)
//
// Heat haze thật (bẻ cong hình ảnh phía sau), không phải một quad mờ.
//
// YÊU CẦU: bật "Opaque Texture" trong URP Asset (Universal Render Pipeline Asset
// -> Rendering -> Opaque Texture = ON). Nếu tắt, hiệu ứng sẽ vô hình.
// Trên mobile yếu có thể tắt Enable Heat Haze trong ExhaustNitroVFX.
// =============================================================================

Shader "LEADKhongPhanh/NitroHeatDistortion"
{
    Properties
    {
        _MaskTex    ("Mask (alpha)", 2D) = "white" {}
        _Strength   ("Distortion Strength", Range(0, 0.15)) = 0.035
        _NoiseScale ("Noise Scale", Range(0.5, 20)) = 6
        _Speed      ("Scroll Speed", Range(0, 10)) = 2.2
        _Fade       ("Global Fade", Range(0, 1)) = 1

        _Curvature  ("Curved World - Curvature", Float) = 0
        _Trimming   ("Curved World - Trimming", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent+200"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "NitroHeatHaze"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MaskTex_ST;
                float  _Strength;
                float  _NoiseScale;
                float  _Speed;
                float  _Fade;
                float  _Curvature;
                float  _Trimming;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
                float4 color       : COLOR;
            };

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

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 positionVS = TransformWorldToView(positionWS);

                positionVS.y -= _Curvature * positionVS.z * positionVS.z;
                positionVS.x += _Trimming  * positionVS.z * positionVS.z;

                OUT.positionCS = TransformWViewToHClip(positionVS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MaskTex);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y * _Speed;

                float n1 = vnoise(IN.uv * _NoiseScale + float2(0.0, -t));
                float n2 = vnoise(IN.uv * _NoiseScale * 1.7 + float2(t * 0.6, -t * 1.3));

                float mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv).a;
                mask *= IN.color.a * _Fade;

                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 0.0001);

                float2 offset = float2(n1 - 0.5, n2 - 0.5) * 2.0 * _Strength * mask;
                float3 scene = SampleSceneColor(saturate(screenUV + offset));

                clip(mask - 0.004);
                return half4(scene, saturate(mask));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
