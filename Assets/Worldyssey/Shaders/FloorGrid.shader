// 关卡地面的格子衬线：整块可玩区域一张平面，片元里算出离最近格线的距离画线。
// 线宽按“低分辨率像素”算（像素化后的画面像素），不管关卡多大、相机拉多远，降采样后线都至少 1 像素、不会断断续续。
// 正片叠底：线的颜色 = 地面自己的颜色 × _Color，在每个季节都是同色调的深一档，阴影里也跟着变暗。
// 不写深度、没有 DepthNormals，不会被描边后处理当成边缘。
Shader "Worldyssey/FloorGrid"
{
    Properties
    {
        _Color ("Multiply Color", Color) = (0.72, 0.72, 0.72, 1)
        _LineWidth ("Line Width (low-res pixels)", Range(0.5, 4)) = 1
    }

    SubShader
    {
        // 排在普通不透明物体之后（地面已画好才能叠底）、透明裁剪的草之前（草可以盖住线）
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+10" "RenderPipeline" = "UniversalPipeline" }
        Blend DstColor Zero
        ZWrite Off
        ZTest LEqual
        Offset -1, -1
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _LineWidth;
            CBUFFER_END

            // 风格化后处理设置的低分辨率画面尺寸 (宽, 高, 1/宽, 1/高)，没有后处理时为 0
            float4 _StylizeLowResSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 cell : TEXCOORD0;     // 格子坐标：整数处是格线
                float2 gridSize : TEXCOORD1; // 可玩区域的格数（宽, 高），整张平面相同
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 cell : TEXCOORD0;
                float2 gridSize : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.cell = input.cell;
                output.gridSize = input.gridSize;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 一个低分辨率像素等于几个屏幕像素
                float pixelScale = _StylizeLowResSize.y > 0.0 ? _ScreenParams.y / _StylizeLowResSize.y : 1.0;
                float halfWidth = _LineWidth * pixelScale * 0.5;

                float2 cellsPerPixel = max(fwidth(input.cell), 1e-5);
                float2 margin = cellsPerPixel * halfWidth;

                // 只画在可玩区域内（外圈装饰地带不画），边界线完整保留
                if (any(input.cell < -margin) || any(input.cell > input.gridSize + margin))
                {
                    discard;
                }

                float2 distancePixels = abs(input.cell - round(input.cell)) / cellsPerPixel;
                if (min(distancePixels.x, distancePixels.y) > halfWidth)
                {
                    discard;
                }

                return _Color;
            }
            ENDHLSL
        }
    }
}
