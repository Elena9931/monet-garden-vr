Shader "MonetGarden/LocalWaterRipples"
{
    Properties {_Tint("Ripple colour",Color)=(.62,.81,.72,.5) _Speed("Wind rate",Float)=.4}
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Tint;float _Speed;CBUFFER_END
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
            V Vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.world=TransformObjectToWorld(a.p.xyz);return o;}
            half4 Frag(V i):SV_Target
            {
                float2 p=i.uv-.5;float d=length(p);float t=_Time.y*_Speed;
                float edge=1-smoothstep(.17,.45,d);
                float wind=sin(i.world.x*23+i.world.z*19-t*3)+sin(i.world.x*37-i.world.z*14+t*1.7);
                float rings=pow(saturate(sin(d*72-t*4)),18)*( .35+.2*sin(t*.43+i.world.z));
                float highlight=pow(saturate(wind*.27+.3),6)*.25;
                float alpha=(rings*.1+highlight)*edge*_Tint.a;
                return half4(lerp(_Tint.rgb,half3(.88,.86,.65),highlight),alpha);
            }
            ENDHLSL
        }
    }
}
