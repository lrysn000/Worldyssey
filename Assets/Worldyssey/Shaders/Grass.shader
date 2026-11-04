// 草：随风摆动，玩家和箱子经过时被推开、压扁，离开后慢慢回弹。
// 颜色由根部色和顶部色按高度渐变，贴图可选（留空为纯色）。互动数据由 GrassInteractorFeeder 每帧设置。
Shader "Worldyssey/Grass"
{
    Properties
    {
        [Header(Color)]
        _BaseMap ("Texture (Optional)", 2D) = "white" {}
        _RootColor ("Root Color", Color) = (0.25, 0.45, 0.18, 1)
        _TipColor ("Tip Color", Color) = (0.6, 0.85, 0.38, 1)
        _ColorVariation ("Patch Variation", Range(0, 0.5)) = 0.12
        _BendHeight ("Bend Height (m)", Float) = 0.35

        [Header(Wind)]
        _WindDirection ("Wind Direction (xz)", Vector) = (1, 0, 0.35, 0)
        _WindStrength ("Wind Strength", Range(0, 0.5)) = 0.07
        _WindSpeed ("Wind Speed", Range(0, 10)) = 1.6
        _WindFrequency ("Wind Frequency", Range(0, 5)) = 0.7
        _GustStrength ("Gust Strength", Range(0, 0.3)) = 0.03

        [Header(Interaction)]
        _PushStrength ("Push Strength", Range(0, 1)) = 0.3
        _SquashStrength ("Squash Strength", Range(0, 1)) = 0.75
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "GrassCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half4 color : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = GrassDisplace(TransformObjectToWorld(input.positionOS.xyz), input.positionOS.y);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                half heightT = saturate(input.positionOS.y / max(_BendHeight, 0.001));
                half variation = 1.0 + (GrassColorNoise(positionWS.xz) - 0.5) * 2.0 * _ColorVariation;
                output.color = lerp(_RootColor, _TipColor, heightT) * variation;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * input.color.rgb;
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                // 草叶很薄、双面渲染，用包裹光照避免背光面一片死黑
                half diffuse = saturate(dot(normalWS, mainLight.direction)) * 0.6 + 0.4;
                half3 lighting = mainLight.color * diffuse * mainLight.shadowAttenuation + SampleSH(normalWS);
                half3 color = MixFog(albedo * lighting, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "GrassCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // 由 URP 在渲染阴影时设置
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                float3 positionWS = GrassDisplace(TransformObjectToWorld(input.positionOS.xyz), input.positionOS.y);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirection = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirection = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 ShadowFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "GrassCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            float4 DepthVert(Attributes input) : SV_POSITION
            {
                float3 positionWS = GrassDisplace(TransformObjectToWorld(input.positionOS.xyz), input.positionOS.y);
                return TransformWorldToHClip(positionWS);
            }

            half DepthFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
