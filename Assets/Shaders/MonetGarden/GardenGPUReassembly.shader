Shader "MonetGarden/GPUReassembly" {
Properties {_Painting("Original painting",2D)="white"{} }
SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent"} Pass {Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
HLSLPROGRAM
#pragma vertex Vert
#pragma fragment Frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D(_Painting);SAMPLER(sampler_Painting);float _Progress,_Dispersion,_ViewAspect,_PaintingAspect;
struct A{float4 p:POSITION;float2 corner:TEXCOORD0;float2 seed:TEXCOORD1;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
float H(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
V Vert(A i){V o;float t=_Progress*7,s=H(i.seed);float collect=smoothstep(3.5,5.5,t);float width=min(_ViewAspect,_PaintingAspect),height=width/_PaintingAspect;float2 target=(i.seed-.5)*float2(width*2,height*2);float2 origin=(float2(H(i.seed+2),H(i.seed+7))-.5)*float2(_ViewAspect*2,2);float2 swirl=float2(sin(s*33+t*.6),cos(s*24+t*.5))*.7*_Dispersion*sin(saturate((t-1.5)/4)*3.14159)*(1-collect);float2 p=lerp(origin,target,collect)+swirl;float size=lerp(.011,.006,collect)*(.55+s*.8);float3 local=float3(p+i.corner*size,(s-.5)*1.6*(1-collect));o.p=TransformObjectToHClip(local);o.uv=i.corner;half3 c=SAMPLE_TEXTURE2D_LOD(_Painting,sampler_Painting,i.seed,0).rgb;float fade=smoothstep(1.3,2.3,t)*(1-smoothstep(5.5,7,t));o.color=half4(c,fade*.72);return o;}
half4 Frag(V i):SV_Target{float r=dot(i.uv,i.uv);clip(1-r);return half4(i.color.rgb,i.color.a*exp(-r*2.5));}
ENDHLSL }} }
