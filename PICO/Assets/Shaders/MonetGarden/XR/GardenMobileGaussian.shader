Shader "MonetGarden/MobileGaussian" {
SubShader {Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"} Pass {
Blend One OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
HLSLPROGRAM
#pragma target 4.5
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"
#define SH_BANDS_0
#include "GardenMobileGaussianMath.hlsl"
float3 hash3(float3 p){return frac(sin(float3(dot(p,float3(12.98,78.2,37.7)),dot(p,float3(39.3,11.1,83.1)),dot(p,float3(73.2,52.3,9.2))))*43758.5453);}
#include "../../../GaussianEffects/Shaders/MonetParticleSequence.hlsl"
StructuredBuffer<float3> _PositionBuffer,_ScaleBuffer;
StructuredBuffer<float4> _RotationBuffer,_ColorBuffer;
StructuredBuffer<uint> _OrderBuffer;
float4x4 _MATRIX_M;float _SizeMultiplier,_PointSize,_SceneOpacity;
struct A{uint vertexID:SV_VertexID;
#if !defined(UNITY_INSTANCING_ENABLED) && !defined(UNITY_STEREO_INSTANCING_ENABLED)
uint instanceID:SV_InstanceID;
#endif
UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 colour:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
V vert(A v){V o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(V,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)
uint id=_OrderBuffer[unity_InstanceID];
#else
uint id=_OrderBuffer[v.instanceID];
#endif
float2 corners[6]={float2(-1,-1),float2(1,1),float2(1,-1),float2(-1,-1),float2(-1,1),float2(1,1)};
SplatSource source;source.id=id;source.order=id;source.cornerUV=corners[v.vertexID%6];
float3 p=_PositionBuffer[id],s=_ScaleBuffer[id]*_SizeMultiplier;float4 c=_ColorBuffer[id];c.rgb=c.rgb*SH_C0+.5;
float3 orig=p,origScale=s;float4 origColour=c;float points=smoothstep(0,1.5,_MonetSplatProgress*7)*step(.5,_MonetSplatActive);s=lerp(s,_PointSize.xxx,points);
ApplyMonetParticleSequence(p,s,c,orig,origScale,origColour);
float4x4 mv=mul(UNITY_MATRIX_V,_MATRIX_M);float4 cv=mul(mv,float4(p,1));if(cv.z>-.01){o.pos=discardVec;return o;}
SplatCenter center;center.view=cv.xyz;center.proj=mul(UNITY_MATRIX_P,cv);center.projMat00=UNITY_MATRIX_P[0][0];center.modelView=mv;
float4 quat=lerp(_RotationBuffer[id],float4(1,0,0,0),points);SplatCorner corner;
if(!InitCorner(source,CalcCovariance(quat,s),center,corner)){o.pos=discardVec;return o;}
ClipCorner(corner,max(c.a,.004));o.pos=center.proj+float4(corner.offset.x,_ProjectionParams.x*corner.offset.y,0,0);o.uv=corner.uv;c.a*=_SceneOpacity;o.colour=c;return o;}
half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float a=dot(i.uv,i.uv);clip(1-a);float alpha=exp(-a*4)*i.colour.a;clip(alpha-1.0/255);float3 rgb=max(i.colour.rgb,0);
#ifndef UNITY_COLORSPACE_GAMMA
rgb=GammaToLinearSpace(rgb);
#endif
return half4(rgb*alpha,alpha);}
ENDHLSL }} }
