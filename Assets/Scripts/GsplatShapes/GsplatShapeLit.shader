Shader "GsplatExtensions/URP Shape Lit"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _FlatColorBrightness("Flat Color Brightness", Range(0,2)) = 1
        _DirectionalLightStrength("Directional Light Strength", Range(0,2)) = 0
        _PointLightStrength("Point Light Strength", Range(0,4)) = 1
        _LightingDepth("Lighting Depth", Range(0,1)) = 0
        _EmissionMultiplier("Local Emission Multiplier", Range(0,10)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define GSPLAT_MAX_LOCAL_LIGHTS 8

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _FlatColorBrightness;
                float _DirectionalLightStrength;
                float _PointLightStrength;
                float _LightingDepth;
                float _EmissionMultiplier;
            CBUFFER_END

            int _GsplatShapeLightCount;
            float4x4 _GsplatShapeLightWorldToVolume[GSPLAT_MAX_LOCAL_LIGHTS];
            float4 _GsplatShapeLightCenter[GSPLAT_MAX_LOCAL_LIGHTS];
            float4 _GsplatShapeLightColor[GSPLAT_MAX_LOCAL_LIGHTS];
            float4 _GsplatShapeLightParams[GSPLAT_MAX_LOCAL_LIGHTS];

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                half fogFactor : TEXCOORD2;
                half3 vertexPointLight : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.color = input.color;
                output.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);
                output.vertexPointLight = 0;

#if defined(_ADDITIONAL_LIGHTS_VERTEX)
                uint lightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u;
                     lightIndex < lightCount;
                     lightIndex++)
                {
                    Light light = GetAdditionalLight(
                        lightIndex,
                        positionInputs.positionWS);
                    output.vertexPointLight +=
                        light.color *
                        light.distanceAttenuation *
                        light.shadowAttenuation;
                }
#endif
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 interpolatedNormal = normalize(input.normalWS);
                half3 normalWS = normalize(
                    cross(
                        ddy(input.positionWS),
                        ddx(input.positionWS)));
                normalWS *=
                    dot(normalWS, interpolatedNormal) < 0 ? -1 : 1;
                half3 baseColor = input.color.rgb * _BaseColor.rgb;

                float4 shadowCoord =
                    TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half mainNdotL =
                    saturate(dot(normalWS, mainLight.direction));
                half3 directionalLighting =
                    mainLight.color *
                    mainNdotL *
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation *
                    _DirectionalLightStrength;
                half3 pointLighting = input.vertexPointLight;

#if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u;
                     lightIndex < pixelLightCount;
                     lightIndex++)
                {
                    Light light = GetAdditionalLight(
                        lightIndex,
                        input.positionWS);
                    half normalLight =
                        saturate(dot(normalWS, light.direction));
                    half shapeFactor =
                        lerp(1, normalLight, _LightingDepth);
                    pointLighting +=
                        light.color *
                        light.distanceAttenuation *
                        light.shadowAttenuation *
                        shapeFactor;
                }
#endif
                pointLighting *= _PointLightStrength;

                half3 localSurface = 0;
                half3 localEmission = 0;

                [unroll]
                for (int index = 0;
                     index < GSPLAT_MAX_LOCAL_LIGHTS;
                     index++)
                {
                    if (index >= _GsplatShapeLightCount)
                        break;

                    float3 volumePosition = mul(
                        _GsplatShapeLightWorldToVolume[index],
                        float4(input.positionWS, 1)).xyz;
                    float isBox =
                        _GsplatShapeLightParams[index].z;
                    float sphereDistance =
                        length(volumePosition) * 2;
                    float boxDistance =
                        max(
                            abs(volumePosition.x),
                            max(
                                abs(volumePosition.y),
                                abs(volumePosition.z))) * 2;
                    float normalizedDistance =
                        lerp(sphereDistance, boxDistance, isBox);

                    float attenuation = pow(
                        saturate(1 - normalizedDistance),
                        _GsplatShapeLightParams[index].y);
                    float intensity =
                        _GsplatShapeLightParams[index].x;
                    float mode =
                        _GsplatShapeLightParams[index].w;
                    half3 color =
                        _GsplatShapeLightColor[index].rgb;

                    half3 toLight = normalize(
                        _GsplatShapeLightCenter[index].xyz -
                        input.positionWS);
                    half localNdotL =
                        saturate(dot(normalWS, toLight));
                    localNdotL =
                        lerp(1, localNdotL, _LightingDepth);

                    float surfaceEnabled =
                        (mode < 1.5 || mode > 2.5) ? 1.0 : 0.0;
                    float emissionEnabled =
                        mode > 1.5 ? 1.0 : 0.0;
                    localSurface +=
                        color *
                        attenuation *
                        intensity *
                        localNdotL *
                        surfaceEnabled;
                    localEmission +=
                        color *
                        attenuation *
                        intensity *
                        emissionEnabled;
                }

                half3 finalColor =
                    baseColor * _FlatColorBrightness +
                    baseColor *
                    (directionalLighting +
                     pointLighting +
                     localSurface) +
                    baseColor * localEmission * _EmissionMultiplier;
                finalColor = MixFog(
                    finalColor,
                    input.fogFactor);
                return half4(finalColor, input.color.a * _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
