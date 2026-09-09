Shader "LEAD KHONG PHANH/Photon Video Black Key"
{
    Properties
    {
        [MainTexture] _MainTex ("Video Texture", 2D) = "black" {}

        _BlackThreshold ("Black Threshold", Range(0, 1)) = 0.08
        _BlackSoftness ("Black Softness", Range(0.001, 0.5)) = 0.08
        _Brightness ("Brightness", Range(0, 5)) = 1.5
        _AlphaMultiplier ("Alpha Multiplier", Range(0, 3)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "PhotonVideo"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;
                float _BlackThreshold;
                float _BlackSoftness;
                float _Brightness;
                float _AlphaMultiplier;

            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv =
                    TRANSFORM_TEX(IN.uv, _MainTex);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 video =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        IN.uv
                    );

                // Luminance của pixel video
                half luminance =
                    dot(
                        video.rgb,
                        half3(0.2126, 0.7152, 0.0722)
                    );

                // Đen -> alpha 0
                half alpha =
                    smoothstep(
                        _BlackThreshold,
                        _BlackThreshold + _BlackSoftness,
                        luminance
                    );

                alpha *= _AlphaMultiplier;

                half3 finalColor =
                    video.rgb * _Brightness;

                return half4(
                    finalColor,
                    saturate(alpha)
                );
            }

            ENDHLSL
        }
    }
}