Shader "MonetGarden/LocalFoliageSway"
{
    Properties { _Tint("Leaf colour",Color)=(.2,.4,.12,1) _Sway("Sway distance",Float)=.02 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Tint; float _Sway; CBUFFER_END
            struct A {float4 positionOS:POSITION; float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            V Vert(A a)
            {
                V o;
                float3 p=a.positionOS.xyz;
                float3 anchor=TransformObjectToWorld(float3(0,0,0));
                p.x+=sin(_Time.y*1.3+anchor.x*3+anchor.z)*_Sway*a.uv.y;
                o.positionCS=TransformObjectToHClip(p);o.uv=a.uv;return o;
            }
            half4 Frag(V i):SV_Target {return half4(_Tint.rgb*lerp(.75,1.15,i.uv.y),1);}
            ENDHLSL
        }
    }
}
