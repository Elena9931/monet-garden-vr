// Optional scene-controlled GPU choreography. Original source buffers are never written.
float _MonetSplatActive,_MonetSplatProgress,_MonetSplatSpread,_MonetAssemblyDensity;
float3 _MonetAssemblyCenter,_MonetAssemblyRight,_MonetAssemblyUp;
sampler2D _MonetAssemblyPainting;
void ApplyMonetParticleSequence(inout float3 p,inout float3 scale,inout float4 color,float3 original,float3 originalScale,float4 originalColor){
 if(_MonetSplatActive<.5)return;
 float t=saturate(_MonetSplatProgress)*7;float3 h=hash3(original);float pointPhase=smoothstep(0,1.5,t);scale=lerp(originalScale,scale,pointPhase);p=lerp(original,p,pointPhase);color=lerp(originalColor,color,pointPhase);
 float dispersion=smoothstep(1.5,3.5,t);float collection=smoothstep(3.5,5.5,t);
 float3 dir=normalize(h-.5+float3(.001,.002,.003));float3 scattered=original+dir*_MonetSplatSpread*dispersion*(.4+h.y*1.6);
 float a=dispersion*(.35+h.z*.6);float3 relative=scattered-_MonetAssemblyCenter;float2 spun=float2(relative.x*cos(a)-relative.z*sin(a),relative.x*sin(a)+relative.z*cos(a));scattered.xz=lerp(scattered.xz,_MonetAssemblyCenter.xz+spun,dispersion*.5);
 float2 uv=h.xy;float3 target=_MonetAssemblyCenter+_MonetAssemblyRight*(uv.x-.5)+_MonetAssemblyUp*(uv.y-.5);
 target+=normalize(_MonetAssemblyCenter-original+float3(.001,0,0))*(h.z-.5)*.025*(1-collection);
 p=lerp(scattered,target,collection);
 float retained=1-smoothstep(_MonetAssemblyDensity-.015,_MonetAssemblyDensity+.015,h.z);float thinning=lerp(1,retained,smoothstep(2.5,4.5,t));
 float4 pigment=tex2Dlod(_MonetAssemblyPainting,float4(uv,0,0));color.rgb=lerp(color.rgb,pigment.rgb,collection);
 color.a*=thinning*(1-smoothstep(5.5,7,t));scale=lerp(scale,float3(.0016,.0016,.0016),collection);
}
