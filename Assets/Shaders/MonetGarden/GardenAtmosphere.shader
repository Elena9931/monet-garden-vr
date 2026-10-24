Shader "MonetGarden/Atmosphere" { SubShader { Tags {"Queue"="Transparent" "RenderPipeline"="UniversalPipeline"} Pass { Cull Front ZWrite Off Blend SrcAlpha One
HLSLPROGRAM
#pragma vertex Vert
#pragma fragment Frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 n:TEXCOORD0;float3 world:TEXCOORD1;};V Vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.world=TransformObjectToWorld(i.p.xyz);return o;}half4 Frag(V i):SV_Target{float3 n=normalize(i.n),v=normalize(_WorldSpaceCameraPos-i.world);float rim=pow(saturate(1-abs(dot(n,v))),5);float warm=saturate(dot(n,normalize(float3(.5,.55,-.7))));return half4(lerp(half3(.12,.37,.7),half3(.8,.6,.22),pow(warm,4)),rim*.22);}
ENDHLSL }} }
