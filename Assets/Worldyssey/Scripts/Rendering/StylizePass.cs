using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Worldyssey.Data;

namespace Worldyssey.Rendering
{
    /// <summary>
    /// 风格化的两步：相机画面 → 低分辨率（点采样 + 描边）→ 放大回相机画面（最近邻 + 像素对齐零头补偿）。
    /// 关闭像素化时低分辨率等于原分辨率，只做描边。
    /// </summary>
    internal sealed class StylizePass : ScriptableRenderPass
    {
        private const int PixelatePass = 0;
        private const int UpscalePass = 1;
        private static readonly int LowResSizeId = Shader.PropertyToID("_StylizeLowResSize");

        private readonly Material _material;
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Stylize");
        private StylizeSettings _settings;
        private RTHandle _cameraColor;
        private RTHandle _lowRes;
        private Vector4 _lowResSize;

        public StylizePass(Material material)
        {
            _material = material;
        }

        public void Setup(StylizeSettings settings)
        {
            _settings = settings;
        }

        public void SetTarget(RTHandle cameraColor)
        {
            _cameraColor = cameraColor;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            int height = _settings.IsPixelationEnabled ? Mathf.Min(_settings.PixelHeight, descriptor.height) : descriptor.height;
            int width = Mathf.Max(1, Mathf.RoundToInt(height * (float)descriptor.width / descriptor.height));
            descriptor.width = width;
            descriptor.height = height;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref _lowRes, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_StylizeLowRes");
            _lowResSize = new Vector4(width, height, 1f / width, 1f / height);
            ConfigureTarget(_cameraColor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _cameraColor == null || _lowRes == null)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                cmd.SetGlobalVector(LowResSizeId, _lowResSize);
                Blitter.BlitCameraTexture(cmd, _cameraColor, _lowRes, _material, PixelatePass);
                Blitter.BlitCameraTexture(cmd, _lowRes, _cameraColor, _material, UpscalePass);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _lowRes?.Release();
            _lowRes = null;
        }
    }
}
