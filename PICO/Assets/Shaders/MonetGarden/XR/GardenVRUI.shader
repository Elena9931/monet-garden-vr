Shader "MonetGarden/VRUI" {
 Properties { [PerRendererData]_MainTex("Texture",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _StencilComp("Stencil Comparison",Float)=8 _Stencil("Stencil",Float)=0 _StencilOp("Stencil Operation",Float)=0 _StencilWriteMask("Stencil Write Mask",Float)=255 _StencilReadMask("Stencil Read Mask",Float)=255 _ColorMask("Color Mask",Float)=15 }
 SubShader { Tags{"Queue"="Overlay" "RenderType"="Transparent"} Stencil{Ref[_Stencil] Comp[_StencilComp] Pass[_StencilOp] ReadMask[_StencilReadMask] WriteMask[_StencilWriteMask]}
 Cull Off Lighting Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha ColorMask[_ColorMask]
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
 #include "UnityCG.cginc"
 #include "UnityUI.cginc"
 sampler2D _MainTex;float4 _Color,_ClipRect,_TextureSampleAdd;
 struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float4 local:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_OUTPUT(V,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.local=a.vertex;o.pos=UnityObjectToClipPos(a.vertex);o.uv=a.uv;o.color=a.color*_Color;return o;}
 half4 frag(V i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);half4 c=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
 #ifdef UNITY_UI_CLIP_RECT
 c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
 #endif
 return c;}
 ENDHLSL }
 }
}