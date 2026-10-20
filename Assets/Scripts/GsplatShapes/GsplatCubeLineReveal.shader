Shader "GsplatExtensions/URP Cube Line Reveal"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        [HDR] _RevealEdgeColor("Reveal Edge Color", Color) = (0.1,0.8,1,1)
        _RevealEdgeIntensity("Reveal Edge Intensity", Range(0,10)) = 2
        _RevealEdgeWidth("Reveal Edge Width", Float) = 0.35
        _CubeScale("Cube Scale", Float) = 1
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
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RevealEdgeColor;
                float _RevealEdgeIntensity;
                float _RevealEdgeWidth;
                float _RevealEnabled;
                float4 _ScanOrigin;
                float4 _ScanDirection;
                float _RevealFront;
                float _CubeScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float3 centerOS : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float centerDepth : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 scaledPositionOS = input.centerOS +
                    (input.positionOS.xyz - input.centerOS) *
                    max(0.0001, _CubeScale);
                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(scaledPositionOS);
                float3 centerWS = TransformObjectToWorld(input.centerOS);
                output.positionCS = positionInputs.positionCS;
                output.color = input.color;
                output.centerDepth = dot(
                    centerWS - _ScanOrigin.xyz,
                    normalize(_ScanDirection.xyz));
                output.fogFactor =
                    ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float revealedDistance =
                    _RevealFront - input.centerDepth;
                if (_RevealEnabled > 0.5)
                    clip(revealedDistance);

                half3 color = input.color.rgb * _BaseColor.rgb;
                float edge = 1 - smoothstep(
                    0,
                    max(0.0001, _RevealEdgeWidth),
                    revealedDistance);
                color += input.color.rgb *
                         _RevealEdgeColor.rgb *
                         edge *
                         _RevealEdgeIntensity;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
