Shader "LEAD KHONG PHANH/Curve World/URP Lit DEFORM V8"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0

        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1.0

        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionMap("Emission Map", 2D) = "white" {}

        [HideInInspector] _Surface("__surface", Float) = 0
        [HideInInspector] _Blend("__blend", Float) = 0
        [HideInInspector] _Cull("__cull", Float) = 2
        [HideInInspector] _AlphaClip("__clip", Float) = 0
        [HideInInspector] _SrcBlend("__src", Float) = 1
        [HideInInspector] _DstBlend("__dst", Float) = 0
        [HideInInspector] _ZWrite("__zw", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex CurveWorldLitVertex
            #pragma fragment LitPassFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED

            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SCREENSPACEREFLECTIONS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            #include "CurveWorldCommon.hlsl"

            Varyings CurveWorldLitVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 originalPositionWS =
                    TransformObjectToWorld(input.positionOS.xyz);

                float3 normalWS =
                    TransformObjectToWorldNormal(input.normalOS);

                float3 tangentWS =
                    TransformObjectToWorldDir(input.tangentOS.xyz);

                CurveWorldData curveData =
                    CurveWorld_Evaluate(originalPositionWS);

                normalWS =
                    CurveWorld_ApplyNormalFromData(
                        normalWS,
                        curveData);

                tangentWS =
                    CurveWorld_ApplyTangentFromData(
                        tangentWS,
                        curveData);

                float3 positionWS =
                    curveData.positionWS;

                VertexPositionInputs vertexInput =
                    GetVertexPositionInputs(
                        input.positionOS.xyz);

                vertexInput.positionWS =
                    positionWS;

                vertexInput.positionCS =
                    TransformWorldToHClip(positionWS);

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    half3 vertexLight =
                        VertexLighting(
                            positionWS,
                            normalWS);
                #else
                    half3 vertexLight = 0;
                #endif

                half fogFactor = 0;

                #if !defined(_FOG_FRAGMENT)
                    fogFactor =
                        ComputeFogFactor(
                            vertexInput.positionCS.z);
                #endif

                output.uv =
                    TRANSFORM_TEX(
                        input.texcoord,
                        _BaseMap);

                output.normalWS =
                    normalWS;

                #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)

                    real tangentSign =
                        input.tangentOS.w *
                        GetOddNegativeScale();

                    half4 tangentWS4 =
                        half4(
                            tangentWS,
                            tangentSign);

                    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
                        output.tangentWS =
                            tangentWS4;
                    #endif

                    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)

                        half3 viewDirWS =
                            GetWorldSpaceNormalizeViewDir(
                                positionWS);

                        output.viewDirTS =
                            GetViewDirectionTangentSpace(
                                tangentWS4,
                                output.normalWS,
                                viewDirWS);

                    #endif

                #endif

                OUTPUT_LIGHTMAP_UV(
                    input.staticLightmapUV,
                    unity_LightmapST,
                    output.staticLightmapUV);

                #ifdef DYNAMICLIGHTMAP_ON

                    output.dynamicLightmapUV =
                        input.dynamicLightmapUV.xy *
                        unity_DynamicLightmapST.xy +
                        unity_DynamicLightmapST.zw;

                #endif

                OUTPUT_SH4(
                    positionWS,
                    output.normalWS.xyz,
                    GetWorldSpaceNormalizeViewDir(
                        positionWS),
                    output.vertexSH,
                    output.probeOcclusion);

                #ifdef _ADDITIONAL_LIGHTS_VERTEX

                    output.fogFactorAndVertexLight =
                        half4(
                            fogFactor,
                            vertexLight);

                #else

                    output.fogFactor =
                        fogFactor;

                #endif

                #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)

                    output.positionWS =
                        positionWS;

                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)

                    output.shadowCoord =
                        GetShadowCoord(vertexInput);

                #endif

                output.positionCS =
                    vertexInput.positionCS;

                return output;
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex CurveWorldShadowVertex
            #pragma fragment ShadowPassFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            #include "CurveWorldCommon.hlsl"

            Varyings CurveWorldShadowVertex(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                #if defined(_ALPHATEST_ON)
                    output.uv =
                        TRANSFORM_TEX(
                            input.texcoord,
                            _BaseMap);
                #endif

                float3 originalPositionWS =
                    TransformObjectToWorld(
                        input.positionOS.xyz);

                float3 normalWS =
                    TransformObjectToWorldNormal(
                        input.normalOS);

                CurveWorldData curveData =
                    CurveWorld_Evaluate(
                        originalPositionWS);

                normalWS =
                    CurveWorld_ApplyNormalFromData(
                        normalWS,
                        curveData);

                float3 positionWS =
                    curveData.positionWS;

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW

                    float3 lightDirectionWS =
                        normalize(
                            _LightPosition -
                            positionWS);

                #else

                    float3 lightDirectionWS =
                        _LightDirection;

                #endif

                float4 positionCS =
                    TransformWorldToHClip(
                        ApplyShadowBias(
                            positionWS,
                            normalWS,
                            lightDirectionWS));

                positionCS =
                    ApplyShadowClamping(
                        positionCS);

                output.positionCS =
                    positionCS;

                return output;
            }

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"

            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex CurveWorldDepthVertex
            #pragma fragment CurveWorldDepthFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "CurveWorldCommon.hlsl"

            struct CurveDepthAttributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct CurveDepthVaryings
            {
                float4 positionCS : SV_POSITION;

                #if defined(_ALPHATEST_ON)
                    float2 uv : TEXCOORD0;
                #endif

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CurveDepthVaryings CurveWorldDepthVertex(
                CurveDepthAttributes input)
            {
                CurveDepthVaryings output =
                    (CurveDepthVaryings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 originalPositionWS =
                    TransformObjectToWorld(
                        input.positionOS.xyz);

                CurveWorldData curveData =
                    CurveWorld_Evaluate(
                        originalPositionWS);

                output.positionCS =
                    TransformWorldToHClip(
                        curveData.positionWS);

                #if defined(_ALPHATEST_ON)
                    output.uv =
                        TRANSFORM_TEX(
                            input.texcoord,
                            _BaseMap);
                #endif

                return output;
            }

            half4 CurveWorldDepthFragment(
                CurveDepthVaryings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(_ALPHATEST_ON)

                    half4 baseMap =
                        SAMPLE_TEXTURE2D(
                            _BaseMap,
                            sampler_BaseMap,
                            input.uv);

                    half alpha =
                        baseMap.a *
                        _BaseColor.a;

                    clip(alpha - _Cutoff);

                #endif

                return 0;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
