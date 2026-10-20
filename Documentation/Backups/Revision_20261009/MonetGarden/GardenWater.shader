Shader "MonetGarden/LocalWaterRipples"
{
    Properties { _Tint("Ripple colour", Color) = (.62,.81,.72,1) _Speed("Speed",Float)=.6 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Tint; float _Speed; CBUFFER_END
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V Vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o; }
            half4 Frag(V i):SV_Target
            {
                float2 p=i.uv-.5;
                float distance=length(p);
                float ring=pow(saturate(sin(distance*95-_Time.y*_Speed*6)),18);
                float edge=1-smoothstep(.22,.48,distance);
                float alpha=ring*edge*.18;
                clip(alpha-.015);
                return half4(_Tint.rgb,alpha*_Tint.a);
            }
            ENDHLSL
        }
    }
}
