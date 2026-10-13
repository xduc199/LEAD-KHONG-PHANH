Shader "LEAD KHONG PHANH/Environment/Procedural Sky"
{
    Properties
    {
        [Header(Sky Colors)]
        _SkyTopColor ("Sky Top", Color) = (0.055, 0.12, 0.24, 1)
        _SkyMiddleColor ("Sky Middle", Color) = (0.16, 0.32, 0.55, 1)
        _HorizonColor ("Horizon", Color) = (0.78, 0.55, 0.36, 1)
        _GroundColor ("Ground", Color) = (0.025, 0.035, 0.05, 1)

        [Header(Atmosphere)]
        _HorizonSharpness ("Horizon Sharpness", Range(0.2, 8.0)) = 2.0
        _HorizonGlow ("Horizon Glow", Range(0.0, 3.0)) = 0.8
        _AtmosphereStrength ("Atmosphere Strength", Range(0.0, 3.0)) = 1.0

        [Header(Sun)]
        _SunDirection ("Sun Direction", Vector) = (0.35, 0.45, 0.75, 0)
        _SunColor ("Sun Color", Color) = (1.0, 0.68, 0.35, 1)
        _SunIntensity ("Sun Intensity", Range(0.0, 10.0)) = 2.5
        _SunSize ("Sun Size", Range(0.001, 0.2)) = 0.035
        _SunSoftness ("Sun Softness", Range(0.001, 0.2)) = 0.045
        _SunHalo ("Sun Halo", Range(0.0, 10.0)) = 2.0
        _SunHaloSize ("Sun Halo Size", Range(0.01, 2.0)) = 0.35

        [Header(Twilight)]
        _TwilightColor ("Twilight Color", Color) = (0.65, 0.18, 0.32, 1)
        _TwilightStrength ("Twilight Strength", Range(0.0, 3.0)) = 0.8

        [Header(Night)]
        _NightColor ("Night Color", Color) = (0.008, 0.015, 0.045, 1)
        _NightStrength ("Night Strength", Range(0.0, 1.0)) = 0.65
        _StarsStrength ("Stars Strength", Range(0.0, 2.0)) = 0.35

        [Header(Clouds)]
        _CloudStrength ("Cloud Strength", Range(0.0, 1.0)) = 0.15
        _CloudScale ("Cloud Scale", Range(0.5, 20.0)) = 4.0
        _CloudSoftness ("Cloud Softness", Range(0.01, 1.0)) = 0.35

        [Header(Color)]
        _Exposure ("Exposure", Range(0.1, 4.0)) = 1.0
        _Saturation ("Saturation", Range(0.0, 2.0)) = 1.0

        [Header(Time Of Day)]
        _TimeOfDay ("Time Of Day", Range(0.0, 1.0)) = 0.22

        [Header(Rotation)]
        _Rotation ("Sky Rotation", Range(0.0, 360.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            float4 _SkyTopColor;
            float4 _SkyMiddleColor;
            float4 _HorizonColor;
            float4 _GroundColor;

            float _HorizonSharpness;
            float _HorizonGlow;
            float _AtmosphereStrength;

            float4 _SunDirection;
            float4 _SunColor;
            float _SunIntensity;
            float _SunSize;
            float _SunSoftness;
            float _SunHalo;
            float _SunHaloSize;

            float4 _TwilightColor;
            float _TwilightStrength;

            float4 _NightColor;
            float _NightStrength;
            float _StarsStrength;

            float _CloudStrength;
            float _CloudScale;
            float _CloudSoftness;

            float _Exposure;
            float _Saturation;

            float _TimeOfDay;
            float _Rotation;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);

                float3 direction = normalize(v.vertex.xyz);

                float angle = radians(_Rotation);

                float s = sin(angle);
                float c = cos(angle);

                float3 rotatedDirection;

                rotatedDirection.x =
                    direction.x * c -
                    direction.z * s;

                rotatedDirection.y =
                    direction.y;

                rotatedDirection.z =
                    direction.x * s +
                    direction.z * c;

                o.direction = normalize(rotatedDirection);

                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);

                return frac(p.x * p.y);
            }

            float Noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(
                    lerp(a, b, f.x),
                    lerp(c, d, f.x),
                    f.y
                );
            }

            float FBM(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                value += Noise2D(p);
                p *= 2.03;
                amplitude *= 0.5;

                value += Noise2D(p) * amplitude;
                p *= 2.01;
                amplitude *= 0.5;

                value += Noise2D(p) * amplitude;

                return value;
            }

            float3 ApplySaturation(float3 color, float saturation)
            {
                float luminance =
                    dot(
                        color,
                        float3(0.2126, 0.7152, 0.0722)
                    );

                return lerp(
                    luminance.xxx,
                    color,
                    saturation
                );
            }

            float3 GetSkyGradient(float3 dir)
            {
                float vertical = saturate(dir.y * 0.5 + 0.5);

                float horizonMask =
                    pow(
                        saturate(1.0 - abs(dir.y)),
                        _HorizonSharpness
                    );

                float3 sky;

                if (vertical < 0.5)
                {
                    float t = smoothstep(
                        0.0,
                        0.5,
                        vertical
                    );

                    sky = lerp(
                        _GroundColor.rgb,
                        _HorizonColor.rgb,
                        t
                    );
                }
                else
                {
                    float t = smoothstep(
                        0.5,
                        1.0,
                        vertical
                    );

                    sky = lerp(
                        _SkyMiddleColor.rgb,
                        _SkyTopColor.rgb,
                        t
                    );
                }

                float atmosphere =
                    horizonMask *
                    _HorizonGlow *
                    _AtmosphereStrength;

                sky +=
                    _HorizonColor.rgb *
                    atmosphere;

                return sky;
            }

            float GetSunDisc(float3 dir)
            {
                float3 sunDir =
                    normalize(_SunDirection.xyz);

                float sunDot =
                    dot(dir, sunDir);

                float inner =
                    smoothstep(
                        1.0 - _SunSize,
                        1.0,
                        sunDot
                    );

                float outer =
                    smoothstep(
                        1.0 - _SunSize - _SunSoftness,
                        1.0 - _SunSize,
                        sunDot
                    );

                return max(inner, outer * 0.35);
            }

            float GetSunHalo(float3 dir)
            {
                float3 sunDir =
                    normalize(_SunDirection.xyz);

                float sunDot =
                    saturate(dot(dir, sunDir));

                float halo =
                    pow(
                        sunDot,
                        1.0 / max(_SunHaloSize, 0.001)
                    );

                return halo * _SunHalo;
            }

            float GetTwilight(float3 dir)
            {
                float3 sunDir =
                    normalize(_SunDirection.xyz);

                float sunHeight =
                    sunDir.y;

                float horizon =
                    1.0 -
                    abs(dir.y);

                float sunHorizon =
                    exp(
                        -abs(sunHeight) * 8.0
                    );

                return saturate(
                    horizon *
                    sunHorizon
                );
            }

            float GetStars(float3 dir)
            {
                if (dir.y <= 0.05)
                    return 0.0;

                float2 uv;

                uv.x =
                    atan2(dir.z, dir.x)
                    * 0.15915494
                    + 0.5;

                uv.y =
                    asin(saturate(dir.y))
                    * 0.31830989
                    + 0.5;

                uv *= 180.0;

                float n =
                    Hash21(floor(uv));

                float stars =
                    step(0.9975, n);

                float horizonFade =
                    smoothstep(
                        0.05,
                        0.45,
                        dir.y
                    );

                return stars *
                    horizonFade *
                    _StarsStrength;
            }

            float3 ApplyClouds(
                float3 dir,
                float3 baseSky
            )
            {
                if (_CloudStrength <= 0.001)
                    return baseSky;

                float2 cloudUV;

                cloudUV.x =
                    atan2(dir.z, dir.x)
                    * 0.15915494;

                cloudUV.y =
                    dir.y * 0.5 + 0.5;

                cloudUV *= _CloudScale;

                float cloud =
                    FBM(cloudUV);

                cloud =
                    smoothstep(
                        0.5 - _CloudSoftness,
                        0.5 + _CloudSoftness,
                        cloud
                    );

                float horizonFade =
                    smoothstep(
                        0.05,
                        0.8,
                        dir.y
                    );

                cloud *= horizonFade;

                float3 cloudColor =
                    lerp(
                        baseSky,
                        float3(1.0, 1.0, 1.0),
                        cloud * _CloudStrength
                    );

                return cloudColor;
            }

            float3 ApplyTimeOfDay(
                float3 color,
                float3 dir
            )
            {
                /*
                    TimeOfDay

                    0.00 = midnight
                    0.20 = sunrise
                    0.35 = morning
                    0.50 = noon
                    0.65 = afternoon
                    0.80 = sunset
                    1.00 = midnight
                */

                float t =
                    _TimeOfDay;

                float night =
                    smoothstep(
                        0.0,
                        0.20,
                        0.10 - t
                    );

                night +=
                    smoothstep(
                        0.80,
                        1.0,
                        t
                    );

                float sunset =
                    smoothstep(
                        0.60,
                        0.75,
                        t
                    )
                    *
                    (1.0 -
                    smoothstep(
                        0.75,
                        0.90,
                        t
                    ));

                float sunrise =
                    smoothstep(
                        0.10,
                        0.25,
                        t
                    )
                    *
                    (1.0 -
                    smoothstep(
                        0.25,
                        0.40,
                        t
                    ));

                float twilight =
                    saturate(
                        sunset +
                        sunrise
                    );

                color =
                    lerp(
                        color,
                        color * _NightColor.rgb,
                        saturate(
                            night *
                            _NightStrength
                        )
                    );

                color =
                    lerp(
                        color,
                        color +
                        _TwilightColor.rgb *
                        _TwilightStrength,
                        twilight
                    );

                return color;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir =
                    normalize(i.direction);

                float3 color =
                    GetSkyGradient(dir);

                color =
                    ApplyClouds(
                        dir,
                        color
                    );

                float sun =
                    GetSunDisc(dir);

                float halo =
                    GetSunHalo(dir);

                color +=
                    _SunColor.rgb *
                    sun *
                    _SunIntensity;

                color +=
                    _SunColor.rgb *
                    halo;

                float twilight =
                    GetTwilight(dir);

                color +=
                    _TwilightColor.rgb *
                    twilight *
                    _TwilightStrength;

                float stars =
                    GetStars(dir);

                color +=
                    stars.xxx;

                color =
                    ApplyTimeOfDay(
                        color,
                        dir
                    );

                color *= _Exposure;

                color =
                    ApplySaturation(
                        color,
                        _Saturation
                    );

                color =
                    max(color, 0.0);

                return float4(
                    color,
                    1.0
                );
            }

            ENDCG
        }
    }

    FallBack Off
}