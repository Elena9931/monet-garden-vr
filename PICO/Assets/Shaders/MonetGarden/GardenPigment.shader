Shader "MonetGarden/PigmentParticles"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 p:POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;};
            struct V {float4 p:SV_POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;};
            V Vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.c=a.c;o.uv=a.uv;return o;}
            half4 Frag(V i):SV_Target{float2 p=abs(i.uv-.5)*2;float shape=(1-smoothstep(.55,1,p.x))*(1-smoothstep(.45,1,p.y));return half4(i.c.rgb,i.c.a*shape);}
            ENDHLSL
        }
    }
}
