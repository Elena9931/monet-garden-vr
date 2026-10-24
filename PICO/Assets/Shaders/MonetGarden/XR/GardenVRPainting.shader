Shader "MonetGarden/VRPaintingReveal" {Properties{_MainTex("Specified original",2D)="white"{} _Progress("Sequence",Range(0,1))=0} SubShader{Tags{"Queue"="Transparent+50"} ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha Pass{HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"
sampler2D _MainTex;float _Progress;
struct A{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_OUTPUT(V,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(a.vertex);o.uv=a.uv;o.color=a.color;return o;}
float h(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float n(float2 p){float2 b=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(h(b),h(b+float2(1,0)),f.x),lerp(h(b+float2(0,1)),h(b+1),f.x),f.y);}
half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float gate=n(i.uv*40)*.3+.1*abs(i.uv.y-.52);float alpha=_Progress>=.999?1:smoothstep(gate-.06,gate+.09,saturate((_Progress-.5)/.45));return tex2D(_MainTex,i.uv)*float4(1,1,1,alpha*i.color.a);}
ENDHLSL}}}
