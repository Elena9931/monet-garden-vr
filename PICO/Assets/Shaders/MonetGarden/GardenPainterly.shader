Shader "MonetGarden/PainterlyTransition"
{
    Properties {_Painting("User supplied original",2D)="white"{} _Reality("Frozen aligned view",2D)="black"{}}
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline"}
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            TEXTURE2D(_Painting);SAMPLER(sampler_Painting);
            TEXTURE2D(_Reality);SAMPLER(sampler_Reality);
            float _Progress,_HasPainting,_PaintingAspect,_ViewAspect;
            float4 _SourceTexelSize;
            float H(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float N(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(H(i),H(i+float2(1,0)),f.x),lerp(H(i+float2(0,1)),H(i+1),f.x),f.y);}
            half3 Painting(float2 uv)
            {
                if(any(uv<0)||any(uv>1))return half3(.025,.05,.105);
                return SAMPLE_TEXTURE2D(_Painting,sampler_Painting,uv).rgb;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 uv=input.texcoord;
                float t=saturate(_Progress);
                float dissolve=smoothstep(.03,.37,t);
                float2 grid=float2(95,58),cell=floor(uv*grid),local=frac(uv*grid)-.5;
                float seed=H(cell);
                float noise=N(uv*float2(24,20))*.6+N(uv*float2(135,95))*.4;
                float remain=1-smoothstep(noise-.065,noise+.065,dissolve);
                float edge=1-saturate(abs(noise-dissolve)/.05);
                float2 drift=float2(sin(seed*32+t*4),cos(seed*21+t*3))*.075*dissolve;
                half3 raw=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv).rgb;
                remain=1-smoothstep(.49,.8,t);
                edge=0;
                half3 background=half3(.015,.035,.08)+half3(.014,.021,.015)*N(uv*6);
                half3 result=lerp(background,raw*half3(1.025,1.02,.96),remain);
                result+=edge*half3(.48,.42,.16)*.19*(1-smoothstep(.34,.5,t));
                // Screen-space fragments retain source colours while floating away from their grid.
                float fragment=0;
                float brushShape=(1-smoothstep(.31,.48,abs(local.x)))*(1-smoothstep(.12,.29,abs(local.y)));
                result=lerp(result,raw,fragment*brushShape*.63);
                float2 imageUV=uv;
                if(_ViewAspect>_PaintingAspect)imageUV.x=(uv.x-.5)*_ViewAspect/max(_PaintingAspect,.01)+.5;
                else imageUV.y=(uv.y-.5)*_PaintingAspect/max(_ViewAspect,.01)+.5;
                float2 coarseUV=(floor(imageUV*float2(32,30))+.5)/float2(32,30);
                half3 coarse=Painting(coarseUV),fine=Painting(imageUV);
                float bridgePriority=1-smoothstep(.12,.42,abs(imageUV.y-.52));
                float arrival=N(imageUV*float2(47,53))*.55+(1-bridgePriority)*.2;
                float assembly=smoothstep(arrival-.07,arrival+.1,saturate((t-.5)/.28));
                float coarseAmount=1-smoothstep(.68,.9,t);
                half3 pigment=lerp(fine,coarse,coarseAmount*.16);
                float stroke=sin(local.y*24+local.x*4+seed*6.28)*.018;
                pigment+=stroke*(1-smoothstep(.82,.97,t));
                result=lerp(result,pigment,assembly*_HasPainting);
                // Final detail resolves spatially, rather than being a single global image crossfade.
                float reveal=smoothstep(noise*.2+.77,noise*.2+.8,t);
                result=lerp(result,fine,reveal*_HasPainting);
                if(t>=.999 && _HasPainting>0)result=fine;
                return half4(result,1);
            }
            ENDHLSL
        }
    }
}
