#ifndef WORLDYSSEY_TOON_COMMON_INCLUDED
#define WORLDYSSEY_TOON_COMMON_INCLUDED

// 卡通明暗的全局参数，由 StylizeSettings.ApplyGlobals 每帧写入
// _ToonShadowTint：暗面颜色倍数；_ToonShadowParams：x 分界，y 过渡宽度，z 暗面受环境光影响程度
half4 _ToonShadowTint;
float4 _ToonShadowParams;

// 两档明暗：受光强度（N·L × 投影衰减）低于分界为暗面。投影并入暗面，所以影子和背光面是同一种颜色。
// 暗面不是简单变黑，而是固有色 × 偏冷的暗面色，并混入环境光（季节天光），保持和物体同一色系。
half3 ToonShade(half3 albedo, half lightAmount, half3 lightColor, half3 ambient)
{
    half softness = max(_ToonShadowParams.y, 0.0001);
    half lit = smoothstep(_ToonShadowParams.x - softness, _ToonShadowParams.x + softness, lightAmount);
    half3 shadowColor = albedo * lerp(_ToonShadowTint.rgb, ambient, _ToonShadowParams.z);
    half3 litColor = albedo * lightColor;
    return lerp(shadowColor, litColor, lit);
}

// 雾的浓度。URP 自带的 ComputeFogFactor 按透视投影的裁剪空间深度换算，正交相机下结果不对，
// 这里直接用视线方向的距离计算，正交和透视都正确
half ToonFogFactor(float3 positionWS)
{
    float eyeDepth = -TransformWorldToView(positionWS).z;
    return ComputeFogFactorZ0ToFar(eyeDepth);
}

#endif
