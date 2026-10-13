// =============================================================================
// LEAD KHÔNG PHANH - Hexagon Energy Shield
// ShieldHex.shader   (URP / Unity 6)
//
// Khiên năng lượng dạng "từ trường lục giác" màu xanh dương nhạt:
//   - Fresnel dome: viền sáng rực khi nhìn nghiêng, trong suốt ở giữa
//   - Hex grid: lưới lục giác thật (axial hex distance field), không phải ảnh
//   - Edge glow: viền lục giác sáng hơn phần thân
//   - Hit ripple: sóng lục giác lan ra từ điểm va chạm, đổi màu sang hitColor
//
// Dùng cho 1 mesh Sphere (hoặc Icosphere) bao quanh xe. Double-sided để nhìn
// từ trong ra cũng thấy lưới.
// =============================================================================

Shader "LEADKhongPhanh/ShieldHex"
{
    Properties
    {
        [HDR] _ShieldColor ("Shield Color (light blue)", Color) = (0.45, 0.80, 1.0, 1)
        [HDR] _HitColor    ("Hit Color", Color) = (0.85, 0.95, 1.0, 1)

        _Intensity      ("Master Intensity", Range(0, 2)) = 1
        _HexAlpha       ("Hex Grid Weight", Range(0,1)) = 1
        _FresnelAlpha   ("Fresnel Weight", Range(0,1)) = 1
        _EdgeAlpha      ("Edge Glow Weight", Range(0,1)) = 1

        _HexScale       ("Hex Scale", Range(1, 30)) = 9
        _HexLineWidth   ("Hex Line Width", Range(0.01, 0.6)) = 0.14
        _HexScroll      ("Hex Scroll Speed", Range(0, 3)) = 0.35
        _HexJitter      ("Hex Cell Flicker", Range(0, 1)) = 0.25

        _FresnelPower   ("Fresnel Power", Range(0.5, 8)) = 2.6
        _FresnelMin     ("Fresnel Min (center opacity)", Range(0, 1)) = 0.04
        _EdgeSharpness  ("Edge Glow Sharpness", Range(1, 12)) = 5

        _HitPoint       ("Hit Point (local, normalized)", Vector) = (0,0,1,0)
        _HitTime        ("Hit Time", Float) = -999
        _HitStrength    ("Hit Strength", Range(0,1)) = 0
        _HitSpeed       ("Hit Ripple Speed", Range(0.1, 6)) = 2.2
        _HitWidth       ("Hit Ripple Width", Range(0.02, 1)) = 0.22

        _TimeOverride   ("Time Override", Float) = 0

        [HideInInspector] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent+50"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ShieldHexUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShieldColor;
                float4 _HitColor;
                float  _Intensity;
                float  _HexAlpha;
                float  _FresnelAlpha;
                float  _EdgeAlpha;
                float  _HexScale;
                float  _HexLineWidth;
                float  _HexScroll;
                float  _HexJitter;
                float  _FresnelPower;
                float  _FresnelMin;
                float  _EdgeSharpness;
                float4 _HitPoint;
                float  _HitTime;
                float  _HitStrength;
                float  _HitSpeed;
                float  _HitWidth;
                float  _TimeOverride;
                float  _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                float3 localPos   : TEXCOORD2;
                float3 localNrm   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs vni = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = vpi.positionCS;
                OUT.normalWS = vni.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(vpi.positionWS);
                OUT.localPos = normalize(IN.positionOS.xyz);
                OUT.localNrm = normalize(IN.normalOS);

                return OUT;
            }

            // ---- axial hex grid distance field (trên mặt cầu, chiếu theo lat/long) ----
            float2 hexCenter(float2 id)
            {
                float2 c = id;
                c.x += (fmod(id.y, 2.0) == 1.0) ? 0.5 : 0.0;
                return c;
            }

            float hexDist(float2 p)
            {
                p = abs(p);
                float c = dot(p, normalize(float2(1.0, 1.73205)));
                c = max(c, p.x);
                return c;
            }

            // trả về (distanceToEdge, cellRandom)
            float2 hexGrid(float2 uv, float scale)
            {
                uv *= scale;
                float2 r = float2(1.0, 1.73205);
                float2 h = r * 0.5;

                float2 a = fmod(uv, r) - h;
                float2 b = fmod(uv - h, r) - h;

                float2 gv = dot(a, a) < dot(b, b) ? a : b;
                float2 id = uv - gv;

                float d = hexDist(gv);

                float rnd = frac(sin(dot(id, float2(41.3, 289.1))) * 43758.5453);

                return float2(0.5 - d, rnd);
            }

            float hash13(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _TimeOverride > 0.0 ? _TimeOverride : _Time.y;

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                // ---- Fresnel: viền sáng khi nhìn nghiêng, trong ở giữa ----
                float NdotV = saturate(dot(N, V));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                fresnel = lerp(_FresnelMin, 1.0, fresnel);

                // ---- Hex grid: chiếu local-normal (dạng cầu) thành UV lưới ----
                float2 sphUV;
                sphUV.x = atan2(IN.localPos.x, IN.localPos.z) / (2.0 * PI);
                sphUV.y = acos(clamp(IN.localPos.y, -1.0, 1.0)) / PI;

                sphUV.x += t * _HexScroll * 0.10;
                sphUV.y -= t * _HexScroll * 0.06;

                float2 hex = hexGrid(sphUV, _HexScale);
                float hexLine = smoothstep(0.0, _HexLineWidth, hex.x);
                float cellFlicker = 1.0 - _HexJitter * (0.5 - 0.5 * sin(t * 3.0 + hex.y * 30.0));

                float hexMask = (1.0 - hexLine) * cellFlicker;
                float edgeMask = (1.0 - smoothstep(0.0, _HexLineWidth * 0.35, hex.x)) - hexMask;
                edgeMask = saturate(edgeMask * _EdgeSharpness);

                // ---- Hit ripple: sóng lan từ điểm va chạm ----
                float hitFactor = 0.0;
                if (_HitStrength > 0.0001)
                {
                    float age = t - _HitTime;
                    float radius = age * _HitSpeed;
                    float d = distance(normalize(IN.localPos), normalize(_HitPoint.xyz));
                    float ring = 1.0 - saturate(abs(d - radius) / _HitWidth);
                    float fade = saturate(1.0 - age / 1.4);
                    hitFactor = ring * fade * _HitStrength;
                }

                // ---- Compose ----
                float3 baseCol = _ShieldColor.rgb;
                float3 col = baseCol * (0.35 + fresnel * _FresnelAlpha * 0.9);

                col = lerp(col, baseCol * 1.6, hexMask * _HexAlpha * 0.55);
                col += baseCol * 1.8 * edgeMask * _EdgeAlpha;

                col = lerp(col, _HitColor.rgb * 2.2, saturate(hitFactor));

                float alpha = saturate(fresnel * _FresnelAlpha * 0.8
                                       + hexMask * _HexAlpha * 0.35
                                       + edgeMask * _EdgeAlpha
                                       + hitFactor * 1.5);

                alpha *= _Intensity;
                col *= _Intensity;

                clip(alpha - 0.003);
                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
