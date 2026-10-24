Shader "MonetGarden/FrostedGuide" {
Properties { _MainTex("UI",2D)="white"{} }
SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off ZTest Always
Pass { HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
sampler2D _MonetGuideBackground; float4 _MonetGuideBackground_TexelSize;
struct A{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 screen:TEXCOORD1;float4 color:COLOR;};
V vert(A i){V o;o.pos=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.screen=ComputeScreenPos(o.pos);o.color=i.color;return o;}
half4 frag(V i):SV_Target{float2 p=i.uv-.5;float2 q=abs(p)-float2(.45,.465);float d=length(max(q,0))+min(max(q.x,q.y),0)-.035;float mask=1-smoothstep(-.004,.002,d);float2 uv=i.screen.xy/i.screen.w;half3 blur=0;for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)blur+=tex2D(_MonetGuideBackground,uv+float2(x,y)*.004).rgb/9;half3 c=lerp(blur,half3(.88,.94,.9),.8);float border=1-smoothstep(.001,.006,abs(d+.005));c=lerp(c,1,border*.65);return half4(c,mask*.92*i.color.a);}
ENDHLSL }} }
