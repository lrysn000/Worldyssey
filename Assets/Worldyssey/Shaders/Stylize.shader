// 全局风格化的后期部分，由 StylizeRendererFeature 调用：
// Pass 0：把相机画面点采样到低分辨率，同时在低分辨率网格上做描边（外轮廓看深度差，内线看法线差），
//         线条颜色取像素自身颜色压暗、提饱和，所以和物体同色系；线条正好 1 个低分辨率像素宽。
// Pass 1：把低分辨率画面最近邻放大回屏幕，并按相机对齐像素网格时丢掉的零头偏移，保持相机运动平滑。
Shader "Hidden/Worldyssey/Stylize"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

        // xy：低分辨率宽高，zw：一个低分辨率像素在 UV 中的大小
        float4 _StylizeLowResSize;
        // x：深度阈值，y：法线阈值，z：是否描边
        float4 _StylizeEdgeParams;
        // x：外轮廓压暗，y：内线压暗，z：额外饱和度
        float4 _StylizeLineParams;
        float3 _StylizeNormalEdgeBias;
        // 相机对齐像素网格丢掉的零头，单位为低分辨率像素
        float2 _StylizeSnapOffset;
        // 暂时压低描边（0~1，未设置时为 0 即正常描边）。传送相片盖住画面时使用：相片里已经有旧世界的线，
        // 不能再按新世界的深度在相片上叠线
        float _StylizeOutlineSuppress;

        // 视线方向的线性深度（世界单位），正交和透视都适用
        float EyeDepthAt(float2 uv)
        {
            float raw = SampleSceneDepth(uv);
            if (unity_OrthoParams.w > 0.5)
            {
                #if UNITY_REVERSED_Z
                raw = 1.0 - raw;
                #endif
                return lerp(_ProjectionParams.y, _ProjectionParams.z, raw);
            }

            return LinearEyeDepth(raw, _ZBufferParams);
        }

        half3 LineColor(half3 color, half darken)
        {
            half luma = dot(color, half3(0.299, 0.587, 0.114));
            half3 saturated = lerp(luma.xxx, color, 1.0 + _StylizeLineParams.z);
            return max(saturated, 0.0) * darken;
        }

        half4 FragPixelate(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;
            half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, uv);
            if (_StylizeEdgeParams.z < 0.5)
            {
                return color;
            }

            float depth = EyeDepthAt(uv);

            // 天空和远处背景（深度在远裁剪面附近）不描边
            if (depth > _ProjectionParams.z * 0.99)
            {
                return color;
            }

            float3 normal = SampleSceneNormals(uv);
            float2 texel = _StylizeLowResSize.zw;
            float2 offsets[4] = { float2(texel.x, 0), float2(-texel.x, 0), float2(0, texel.y), float2(0, -texel.y) };
            float depthEdge = 0.0;
            float normalEdge = 0.0;

            [unroll]
            for (int i = 0; i < 4; i++)
            {
                float2 neighborUv = uv + offsets[i];
                float depthDiff = EyeDepthAt(neighborUv) - depth;

                // 外轮廓：邻居明显更远，说明自己是靠前那一侧，线只画在前景物体上
                depthEdge += step(_StylizeEdgeParams.x, depthDiff);

                // 内线：法线差够大，且邻居没有明显更近（不是别的物体挡在前面），并只取偏好方向的一侧，避免双线
                float3 normalDiff = normal - SampleSceneNormals(neighborUv);
                float sideIndicator = smoothstep(-0.01, 0.01, dot(normalDiff, _StylizeNormalEdgeBias));
                float notOccluded = step(-_StylizeEdgeParams.x * 0.5, depthDiff);
                normalEdge += step(_StylizeEdgeParams.y, length(normalDiff)) * sideIndicator * notOccluded;
            }

            float strength = 1.0 - saturate(_StylizeOutlineSuppress);
            depthEdge = saturate(depthEdge) * strength;
            normalEdge = saturate(normalEdge) * (1.0 - saturate(depthEdge)) * strength;
            color.rgb = lerp(color.rgb, LineColor(color.rgb, _StylizeLineParams.x), depthEdge);
            color.rgb = lerp(color.rgb, LineColor(color.rgb, _StylizeLineParams.y), normalEdge);
            return color;
        }

        half4 FragUpscale(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord + _StylizeSnapOffset * _StylizeLowResSize.zw;
            return SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, uv);
        }
        ENDHLSL

        Pass
        {
            Name "PixelateAndOutline"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragPixelate
            ENDHLSL
        }

        Pass
        {
            Name "Upscale"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUpscale
            ENDHLSL
        }
    }
}
