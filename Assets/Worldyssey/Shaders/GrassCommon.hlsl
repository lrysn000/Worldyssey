// 草着色器各通道共用的部分：材质参数、全局互动数据、顶点变形。
// 所有通道用同一个顶点变形，画面、阴影、深度里的草才能一起摆动。
#ifndef WORLDYSSEY_GRASS_COMMON_INCLUDED
#define WORLDYSSEY_GRASS_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Shadows.hlsl 用到这里定义的 LerpWhiteTo；画面通道经 Lighting.hlsl 间接包含了它，投影通道没有，所以统一在这里包含
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

// 与 GrassInteractorFeeder.MaxInteractors 保持一致
#define GRASS_MAX_INTERACTORS 32

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _RootColor;
    half4 _TipColor;
    half _ColorVariation;
    float _BendHeight;
    float4 _WindDirection;
    float _WindStrength;
    float _WindSpeed;
    float _WindFrequency;
    float _GustStrength;
    float _PushStrength;
    float _SquashStrength;
CBUFFER_END

// 由 GrassInteractorFeeder 每帧设置：xyz 为世界坐标，w 为半径；强度随压痕回弹从 1 衰减到 0
float4 _GrassInteractors[GRASS_MAX_INTERACTORS];
float _GrassInteractorStrengths[GRASS_MAX_INTERACTORS];
float _GrassInteractorCount;

// 顶点离地高度归一化后的弯曲权重：根部 0 不动，越往上弯得越多
float GrassBendWeight(float heightOS)
{
    float t = saturate(heightOS / max(_BendHeight, 0.001));
    return t * t;
}

float3 GrassDisplace(float3 positionWS, float heightOS)
{
    float weight = GrassBendWeight(heightOS);

    // 风：沿风向推进的波，相位取决于世界坐标，所以相邻的草有先后，像一阵阵风吹过
    float2 windDirection = normalize(_WindDirection.xz + float2(0.0001, 0.0));
    float phase = dot(positionWS.xz, windDirection) * _WindFrequency - _Time.y * _WindSpeed;
    float sway = sin(phase) * _WindStrength + sin(phase * 2.3 + positionWS.x * 1.7 + positionWS.z * 1.3) * _GustStrength;
    float3 offset = float3(windDirection.x, 0.0, windDirection.y) * (sway * weight);

    // 互动：半径内的草朝外推开并压扁，越靠近中心变形越大
    int count = min((int)_GrassInteractorCount, GRASS_MAX_INTERACTORS);
    for (int i = 0; i < count; i++)
    {
        float4 interactor = _GrassInteractors[i];
        float2 delta = positionWS.xz - interactor.xz;
        float dist = length(delta);
        float influence = saturate(1.0 - dist / max(interactor.w, 0.001));
        influence = influence * influence * (3.0 - 2.0 * influence) * _GrassInteractorStrengths[i];
        float2 away = dist > 0.0001 ? delta / dist : float2(0.0, 0.0);
        offset.xz += away * (influence * _PushStrength * weight);
        offset.y -= influence * _SquashStrength * heightOS;
    }

    // 最多压到贴地，不会穿到地面以下
    offset.y = max(offset.y, -heightOS * 0.95);
    return positionWS + offset;
}

// 按世界坐标的低频噪声，让一片草地有成片的深浅变化
half GrassColorNoise(float2 positionXZ)
{
    float2 cell = floor(positionXZ * 0.35);
    float2 f = frac(positionXZ * 0.35);
    f = f * f * (3.0 - 2.0 * f);
    float a = frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);
    float b = frac(sin(dot(cell + float2(1, 0), float2(127.1, 311.7))) * 43758.5453);
    float c = frac(sin(dot(cell + float2(0, 1), float2(127.1, 311.7))) * 43758.5453);
    float d = frac(sin(dot(cell + float2(1, 1), float2(127.1, 311.7))) * 43758.5453);
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

#endif
