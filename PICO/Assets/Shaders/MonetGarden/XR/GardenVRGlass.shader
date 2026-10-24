Shader "MonetGarden/VRFrostedGuide"{Properties{_MainTex("UI",2D)="white"{}}SubShader{Tags{"Queue"="Transparent"}ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha Pass{HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"
struct A{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_OUTPUT(V,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(a.vertex);o.uv=a.uv;o.color=a.color;return o;}
half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 q=abs(i.uv-.5)-float2(.45,.465);float d=length(max(q,0))+min(max(q.x,q.y),0)-.035;float edge=1-smoothstep(.001,.006,abs(d+.005));float grain=frac(sin(dot(i.uv,float2(312.7,95.1)))*43758.5)*.012;return half4(lerp(float3(.86,.93,.9)+grain,1,edge*.6),(1-smoothstep(-.004,.002,d))*.86*i.color.a);}
ENDHLSL}}}
