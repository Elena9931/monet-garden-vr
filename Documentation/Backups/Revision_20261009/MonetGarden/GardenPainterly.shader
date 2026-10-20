Shader "MonetGarden/PainterlyTransition"
{
    Properties { _Painting("Selected original artwork", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            TEXTURE2D(_Painting); SAMPLER(sampler_Painting);
            float _Progress, _HasPainting, _PaintingAspect, _ViewAspect;
            float4 _SourceTexelSize;
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            half3 PaintedColour(float2 uv, float radius)
            {
                // Four overlapping quadrants preserve edges while merging detail into pigment areas.
                half3 best = 0;
                float bestVariance = 1e8;
                [unroll] for (int quadrant=0;quadrant<4;quadrant++)
                {
                    float2 sign = float2((quadrant&1)==0 ? -1:1, (quadrant&2)==0 ? -1:1);
                    half3 mean=0, squares=0;
                    [unroll] for(int y=0;y<3;y++) [unroll] for(int x=0;x<3;x++)
                    {
                        float2 offset=float2(x,y)*.5*radius*sign*_SourceTexelSize.xy;
                        half3 c=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+offset).rgb;
                        mean+=c;squares+=c*c;
                    }
                    mean/=9;
                    float variance=dot(abs(squares/9-mean*mean),float3(1,1,1));
                    if(variance<bestVariance){bestVariance=variance;best=mean;}
                }
                return best;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 raw = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float colour = smoothstep(0.0,0.3,_Progress);
                float brush = smoothstep(0.2,0.85,_Progress);
                float2 pixels = _SourceTexelSize.zw;
                float2 cell = float2(10,4.5) / pixels;
                float2 sampleUV = uv;
                float2 d = float2(2,1.2) * _SourceTexelSize.xy * brush;
                half3 soft = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,sampleUV).rgb * 0.28;
                soft += SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,sampleUV+float2(d.x,0)).rgb * 0.18;
                soft += SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,sampleUV-float2(d.x,0)).rgb * 0.18;
                soft += SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,sampleUV+float2(0,d.y)).rgb * 0.18;
                soft += SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,sampleUV-float2(0,d.y)).rgb * 0.18;
                soft = lerp(soft, PaintedColour(uv, 1.5+brush*4), brush*.86);
                half lum = dot(soft,half3(.2126,.7152,.0722));
                soft = lerp(lum.xxx,soft,1.0+colour*.18);
                soft = soft * lerp(half3(1,1,1),half3(1.055,1.025,.975),colour);
                half3 blocks = floor(soft*24+0.5)/24;
                soft = lerp(soft,blocks,brush*.22);
                float2 local = frac(uv/cell);
                float grain = hash(floor(uv * pixels / 1.5)) - .5;
                float stroke = sin((local.y + local.x * .22)*12.0 + hash(floor(uv/cell))*6.28);
                soft += (grain*.025 + stroke*.018)*brush;
                half3 colourResult = lerp(raw.rgb,soft,colour);
                float originalBlend = smoothstep(.7,1.0,_Progress)*_HasPainting;
                float2 imageUV = uv;
                if(_ViewAspect>_PaintingAspect) imageUV.x=(uv.x-.5)*_ViewAspect/max(_PaintingAspect,.01)+.5;
                else imageUV.y=(uv.y-.5)*_PaintingAspect/max(_ViewAspect,.01)+.5;
                half3 painting = half3(.93,.915,.87);
                if(all(imageUV>=0) && all(imageUV<=1)) painting = SAMPLE_TEXTURE2D(_Painting,sampler_Painting,imageUV).rgb;
                return half4(lerp(colourResult,painting,originalBlend),raw.a);
            }
            ENDHLSL
        }
    }
}
