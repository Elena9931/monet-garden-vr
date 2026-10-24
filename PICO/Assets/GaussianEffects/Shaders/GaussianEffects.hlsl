#ifndef GSPLAT_MIGRATED_GAUSSIAN_EFFECTS_INCLUDED
#define GSPLAT_MIGRATED_GAUSSIAN_EFFECTS_INCLUDED

float _EffectProgress;
float _RiseHeight;
float _DriftStrength;
float _DissolveStrength;
float _GlowIntensity;
float4 _GlowColor;

float _UniformPointSize;
float _UniformPointAlpha;
float _UniformPointBrightness;
float _UniformFloatAmplitude;
float _UniformFloatFrequency;
float _UniformFloatRandomness;
float3 _UniformFloatWorldUpLocal;
float _UniformTwinkleIntensity;
float _UniformTwinkleFrequency;
float _UniformWaveAmplitude;
float _UniformWaveFrequency;
float _UniformWaveLength;
float3 _UniformWaveDirectionLocal;

float4 _DigitalColor;
float _DigitalBrightness;
float _DigitalRainSpeed;
float _DigitalRainHeight;
float _DigitalPointSize;
float _DigitalColumnScale;
float _DigitalColumnSnapStrength;
float _DigitalTrailLength;
float _DigitalHeadBrightness;
float _DigitalTailBrightness;
float _DigitalLineLength;
float _DigitalLineThickness;
float _DigitalLineStretchStrength;

int _LocalRevealEnabled;
float3 _LocalRevealCenter;
float _LocalRevealRadius;
float _LocalRevealSoftness;
float _LocalRevealStrength;

int _EffectVolumeActive;
float4 _EffectVolumeCenterRadius;
float4 _EffectVolumeMotion;
float4 _EffectVolumeDissolveGlow;
float4 _EffectVolumeColor;
float4 _EffectVolumeWaveTwinkle;

inline void ApplyMigratedGaussianEffect(
    inout float3 center,
    inout float3 scales,
    inout float4 rgba,
    int effectType,
    float t,
    float glitterDensity)
{
    float3 localPos = center;
    float3 splatScales = scales;

    if (effectType == 17)
    {
        float3 hashVal = hash3(localPos);
        float oscillation = sin(t * 2.0f + hashVal.y * 6.2831853f) * 0.5f + 0.5f;

        float randomDelay = hashVal.x * 0.35f;
        float flyProgress = saturate((_EffectProgress - randomDelay) / max(1.0f - randomDelay, 0.0001f));
        flyProgress = smoothstep(0.0f, 1.0f, flyProgress);

        float randomSpeed = 0.35f + frac(sin(dot(localPos, float3(12.0f, 78.0f, 45.0f))) * 43758.0f);
        float3 moveDirection = normalize(float3(
            (hashVal.x - 0.5f) * max(_DriftStrength, 0.0f) * 2.0f,
            -_RiseHeight,
            (hashVal.z - 0.5f) * max(_DriftStrength, 0.0f) * 2.0f
        ) + float3(0.0001f, 0.0001f, 0.0001f));

        float moveAmount = flyProgress * randomSpeed;
        float verticalAmount = abs(_RiseHeight) * moveAmount;
        float lateralAmount = _DriftStrength * moveAmount * (0.6f + 0.4f * oscillation);
        center += moveDirection * (verticalAmount + lateralAmount);

        float shrinkFactor = smoothstep(0.0f, 1.0f, saturate(moveAmount * _DissolveStrength));
        scales = lerp(splatScales, float3(0.003f, 0.003f, 0.003f), shrinkFactor);

        if (hashVal.z < glitterDensity && flyProgress > 0.0f)
        {
            float glow = 0.0f;
            glow += sin(t * (5.0f + hashVal.x * 10.0f) + hashVal.x * 6.28318f) * 0.5f + 0.5f;
            glow += sin(t * (3.0f + hashVal.y * 8.0f) + hashVal.y * 6.28318f) * 0.5f + 0.5f;
            glow += sin(t * (2.0f + hashVal.z * 6.0f) + hashVal.z * 6.28318f) * 0.5f + 0.5f;
            glow = pow(glow / 3.0f, 2.0f);
            float3 finalGlitterColor = float3(1.5f, 1.8f, 2.0f) * glow;
            rgba.rgb = lerp(rgba.rgb, finalGlitterColor, shrinkFactor);
            float fadeOut = smoothstep(0.7f, 1.0f, shrinkFactor);
            rgba.a *= 1.0f - fadeOut * _DissolveStrength;
        }
        else
        {
            rgba.a *= 1.0f - saturate(moveAmount * 0.5f * _DissolveStrength);
        }

        if (flyProgress > 0.0f)
        {
            float glowPulse = 0.6f + 0.4f * oscillation;
            rgba.rgb += _GlowColor.rgb * max(0.0f, _GlowIntensity) * glowPulse * flyProgress;
        }
    }
    else if (effectType == 18)
    {
        // Emotional Atmosphere is implemented by ApplyGaussianEffectVolume below.
    }
    else if (effectType == 19)
    {
        float3 hashVal = hash3(localPos);
        float amplitude = max(_UniformFloatAmplitude, 0.0f);
        float frequency = max(_UniformFloatFrequency, 0.0f);
        float randomness = saturate(_UniformFloatRandomness);
        float phase = hashVal.x * 6.2831853f * randomness;
        float randomFactor = lerp(0.65f, 1.35f, hashVal.y);
        float localAmplitude = amplitude * lerp(1.0f, randomFactor, randomness);
        center += _UniformFloatWorldUpLocal * (sin(t * frequency + phase) * localAmplitude);

        float waveAmplitude = max(_UniformWaveAmplitude, 0.0f);
        float waveFrequency = max(_UniformWaveFrequency, 0.0f);
        float waveLength = max(_UniformWaveLength, 0.001f);
        float3 waveDirection = dot(_UniformWaveDirectionLocal, _UniformWaveDirectionLocal) < 0.000001f
            ? float3(1.0f, 0.0f, 0.0f)
            : normalize(_UniformWaveDirectionLocal);
        float wavePhase = dot(localPos, waveDirection) / waveLength;
        float waveOffset = sin(wavePhase * 6.2831853f + t * waveFrequency) * waveAmplitude;
        center += _UniformFloatWorldUpLocal * waveOffset;

        float pointSize = max(_UniformPointSize, 0.001f);
        scales = float3(pointSize, pointSize, pointSize);
        rgba.a *= saturate(_UniformPointAlpha);
        rgba.rgb *= max(_UniformPointBrightness, 0.0f);

        float twinkle = 1.0f + sin(t * max(_UniformTwinkleFrequency, 0.0f) + hashVal.z * 6.2831853f)
            * saturate(_UniformTwinkleIntensity);
        rgba.rgb *= max(twinkle, 0.0f);
    }
    else if (effectType == 20)
    {
        float pointSize = max(_DigitalPointSize, 0.001f);
        float brightness = max(_DigitalBrightness, 0.0f);
        float rainHeight = max(_DigitalRainHeight, 0.001f);
        float columnScale = max(_DigitalColumnScale, 0.001f);
        float snapStrength = saturate(_DigitalColumnSnapStrength);
        float trailLength = max(_DigitalTrailLength, 0.001f);
        float headBrightness = max(_DigitalHeadBrightness, 0.0f);
        float tailBrightness = max(_DigitalTailBrightness, 0.0f);
        float lineLength = max(_DigitalLineLength, 0.001f);
        float lineThickness = max(_DigitalLineThickness, 0.001f);
        float stretchStrength = saturate(_DigitalLineStretchStrength);
        float seed = hash3(localPos).x;
        float fall = frac(t * _DigitalRainSpeed + seed) * rainHeight;
        float columnX = floor(center.x * columnScale) / columnScale;
        float columnZ = floor(center.z * columnScale) / columnScale;
        float columnId = floor(center.x * columnScale);
        float columnSeed = hash3(float3(columnId, columnId * 0.37f, 12.7f)).x;
        float headY = rainHeight * 0.5f - frac(t * _DigitalRainSpeed + columnSeed) * rainHeight;

        center.x = lerp(center.x, columnX, snapStrength);
        center.z = lerp(center.z, columnZ, snapStrength * 0.15f);
        center.y -= fall;
        center.y += rainHeight * 0.5f;

        float distanceBehindHead = center.y - headY;
        float tail = saturate(1.0f - distanceBehindHead / trailLength) * step(0.0f, distanceBehindHead);
        float head = 1.0f - smoothstep(0.0f, trailLength * 0.15f, abs(center.y - headY));
        float trailBrightness = max(tail * tailBrightness, head * headBrightness);

        float3 pointScales = float3(pointSize, pointSize, pointSize);
        float3 lineScales = float3(lineThickness, lineLength, lineThickness);
        scales = lerp(pointScales, lineScales, stretchStrength);
        rgba.rgb = max(_DigitalColor.rgb, float3(0.0f, 0.0f, 0.0f)) * brightness
            * max(trailBrightness, tailBrightness);
        rgba.a = max(rgba.a, 0.0f);
    }
}

inline void ApplyGaussianEffectVolume(
    inout float3 center,
    inout float3 scales,
    inout float4 rgba,
    float t)
{
    if (_EffectVolumeActive <= 0)
        return;

    float3 localPos = center;
    float3 hashValue = hash3(localPos);
    float3 volumeCenter = _EffectVolumeCenterRadius.xyz;
    float volumeRadius = _EffectVolumeCenterRadius.w;
    float volumeProgress = _EffectVolumeMotion.x;
    float volumeSoftness = _EffectVolumeMotion.y;
    float volumeRiseHeight = _EffectVolumeMotion.z;
    float volumeDriftStrength = _EffectVolumeMotion.w;
    float volumeDissolveStrength = _EffectVolumeDissolveGlow.x;
    float volumeGlowIntensity = _EffectVolumeDissolveGlow.y;
    float volumeSparkDensity = _EffectVolumeDissolveGlow.z;
    float volumeIntensity = _EffectVolumeDissolveGlow.w;
    float3 volumeGlowColor = _EffectVolumeColor.rgb;
    float volumeWaveFrequency = _EffectVolumeWaveTwinkle.x;
    float volumeWaveSpeed = _EffectVolumeWaveTwinkle.y;
    float volumeTwinkleIntensity = _EffectVolumeWaveTwinkle.z;
    float volumeTwinkleFrequency = _EffectVolumeWaveTwinkle.w;

    float3 centered = localPos - volumeCenter;
    float distanceToCenter = length(centered);
    float radius = max(volumeRadius, 0.0001f);
    float softness = max(radius * volumeSoftness, 0.0001f);
    float localMask = 1.0f - smoothstep(radius, radius + softness, distanceToCenter);

    float spatialNoise = noise_scalar(localPos * 0.65f + float3(0.0f, t * 0.08f, 0.0f)) * 0.5f + 0.5f;
    float randomDelay = hashValue.x * 0.4f + spatialNoise * 0.2f;
    float phase = saturate((volumeProgress - randomDelay) / max(1.0f - randomDelay, 0.0001f));
    phase = smoothstep(0.0f, 1.0f, phase) * localMask;

    float breath = sin(t * (0.75f + hashValue.y * 0.35f) + distanceToCenter * 1.35f + hashValue.x * 6.2831853f);
    float breath01 = breath * 0.5f + 0.5f;
    float breathingMask = localMask * (0.35f + 0.65f * phase) * volumeIntensity;

    float3 turbulence = noise2_vec(localPos * (0.8f + volumeWaveFrequency * 0.25f)
        + t * (0.08f + volumeWaveSpeed * 0.2f));
    turbulence = turbulence * 2.0f - 1.0f;
    float3 lateral = normalize(float3(
        turbulence.x + hashValue.x - 0.5f,
        0.0f,
        turbulence.z + hashValue.z - 0.5f) + float3(0.0001f, 0.0f, 0.0001f));

    float riseCurve = phase * phase * (3.0f - 2.0f * phase);
    float rise = volumeRiseHeight * riseCurve * (0.65f + 0.5f * spatialNoise) * volumeIntensity;
    float swirl = sin(t * (0.6f + hashValue.z) + distanceToCenter * 2.0f);
    float lateralAmount = volumeDriftStrength * (0.35f + phase) * (0.5f + 0.5f * breath01) * volumeIntensity;

    center.y += rise;
    center.xz += lateral.xz * lateralAmount
        + float2(-centered.z, centered.x) * swirl * volumeDriftStrength * 0.03f * breathingMask;
    center += turbulence * volumeDriftStrength * 0.18f * breathingMask;
    scales *= 1.0f + breathingMask * (0.05f + 0.08f * breath01);

    float dissolveNoise = noise_scalar(localPos * 1.7f + t * 0.15f + hashValue * 4.0f) * 0.5f + 0.5f;
    float dissolve = saturate((phase * volumeDissolveStrength - dissolveNoise * 0.35f) / 0.75f);
    dissolve = smoothstep(0.0f, 1.0f, dissolve);
    scales = lerp(scales, float3(0.0015f, 0.0015f, 0.0015f), dissolve);
    rgba.a *= 1.0f - dissolve * (0.65f + 0.35f * phase);

    float isSpark = step(hashValue.z, volumeSparkDensity * localMask * (0.35f + 0.65f * phase));
    float twinkleSpeed = max(volumeTwinkleFrequency, 0.0f);
    float twinkle = sin(t * (twinkleSpeed + hashValue.x * twinkleSpeed * 2.0f) + hashValue.y * 6.2831853f);
    twinkle = pow(saturate(twinkle * 0.5f + 0.5f), 4.0f) * saturate(volumeTwinkleIntensity);

    float3 glowColor = max(volumeGlowColor, float3(0.0f, 0.0f, 0.0f));
    float3 adjustedColor = rgba.rgb;
    float glowPulse = (0.45f + 0.55f * breath01) * breathingMask;
    adjustedColor += glowColor * max(0.0f, volumeGlowIntensity) * glowPulse;

    if (isSpark > 0.5f)
    {
        float sparkSize = lerp(0.0015f, 0.006f, hashValue.y);
        scales = lerp(scales, float3(sparkSize, sparkSize, sparkSize), saturate(phase + twinkle));
        adjustedColor += glowColor * (1.5f + volumeGlowIntensity * 1.75f) * twinkle;
        rgba.a = max(rgba.a, twinkle * phase);
    }

    rgba.rgb = lerp(rgba.rgb, adjustedColor, saturate(localMask * (0.25f + phase)));
}

inline float ApplyLocalGaussianReveal(
    inout float3 center,
    inout float3 scales,
    inout float4 rgba,
    float3 originalCenter,
    float3 originalScales,
    float4 originalRgba)
{
    if (_LocalRevealEnabled == 0)
        return 0.0f;

    float radius = max(_LocalRevealRadius, 0.001f);
    float softness = max(_LocalRevealSoftness, 0.001f);
    float revealMask = (1.0f - smoothstep(radius, radius + softness, distance(center, _LocalRevealCenter)))
        * saturate(_LocalRevealStrength);

    center = lerp(center, originalCenter, revealMask);
    scales = lerp(scales, originalScales, revealMask);
    rgba = lerp(rgba, originalRgba, revealMask);
    return revealMask;
}

#endif
