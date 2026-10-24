Shader "MonetGarden/ImpressionistEarth"
{
    Properties { _MainTex("Painted geographic surface",2D)="white"{} }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;float2 uv:TEXCOORD2;};
            V Vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.uv=a.uv;return o;}
            float H(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            half4 Frag(V i):SV_Target
            {
                float3 n=normalize(i.normalWS),v=normalize(_WorldSpaceCameraPos-i.positionWS);
                float diffuse=saturate(dot(n,normalize(float3(.5,.55,-.7))));
                float2 cell=floor(i.uv*float2(420,210));
                float brush=sin((frac(i.uv.y*210)+frac(i.uv.x*420)*.35)*14+H(cell)*6.28);
                float fibre=sin(i.uv.x*5600+sin(i.uv.y*1300)*.8)*sin(i.uv.y*3400);
                half3 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb;
                c*=.62+.49*diffuse+brush*.025+fibre*.009;
                float rim=pow(1-saturate(dot(n,v)),3);
                c+=rim*half3(.06,.17,.26)*(.3+diffuse);
                c+=pow(diffuse,16)*half3(.14,.105,.045);
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
