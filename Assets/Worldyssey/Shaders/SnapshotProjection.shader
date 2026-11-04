// 传送转场的快照外壳：把上一个世界的截图，从“起始机位”投影到伙伴箱子外面的外壳上。
// 起始机位时外壳铺满画面，每个像素正好对应截图上同一个位置，所以第一帧和截图完全一样；
// 相机拉远时截图像贴在箱子上一样变小，四周露出新世界。不受雾影响，可整体淡出并叠一层季节色。
Shader "Worldyssey/SnapshotProjection"
{
    Properties
    {
        _SnapshotTex ("Snapshot", 2D) = "black" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0
        _Alpha ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_SnapshotTex);
            SAMPLER(sampler_SnapshotTex);

            CBUFFER_START(UnityPerMaterial)
                float4x4 _SnapshotVP;
                half4 _Tint;
                half _TintStrength;
                half _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 projected : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);

                // _SnapshotVP 是起始机位的 投影 × 视图 矩阵（非 GPU 版本），裁剪坐标换算成 0~1 的截图 UV
                output.projected = mul(_SnapshotVP, float4(positionWS, 1.0));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.projected.xy / input.projected.w * 0.5 + 0.5;
                half4 color = SAMPLE_TEXTURE2D(_SnapshotTex, sampler_SnapshotTex, uv);
                color.rgb = lerp(color.rgb, _Tint.rgb, _TintStrength);
                return half4(color.rgb, _Alpha);
            }
            ENDHLSL
        }
    }
}
