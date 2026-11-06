using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Worldyssey.Data;

namespace Worldyssey.Rendering
{
    /// <summary>
    /// 全局风格化渲染功能：在后处理之前把画面降到低分辨率并描边，再最近邻放大回屏幕（UI 不经过这里，保持清晰）。
    /// 同时每帧把 <see cref="StylizeSettings"/> 写入着色器全局变量，卡通材质和描边都读取它们。只作用于游戏相机。
    /// </summary>
    public class StylizeRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private StylizeSettings _settings;

        [Tooltip("Hidden/Worldyssey/Stylize，放在这里保证打包时包含")]
        [SerializeField] private Shader _shader;

        [SerializeField] private RenderPassEvent _passEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        private Material _material;
        private StylizePass _pass;

        public override void Create()
        {
            if (_shader == null)
            {
                _shader = Shader.Find("Hidden/Worldyssey/Stylize");
            }

            if (_shader == null)
            {
                Debug.LogError($"[{nameof(StylizeRendererFeature)}] 找不到着色器 Hidden/Worldyssey/Stylize，风格化不会生效");
                return;
            }

            _material = CoreUtils.CreateEngineMaterial(_shader);
            _pass = new StylizePass(_material) { renderPassEvent = _passEvent };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_settings == null || _pass == null || renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            _settings.ApplyGlobals();
            if (!_settings.IsPixelationEnabled && !_settings.IsOutlineEnabled)
            {
                return;
            }

            _pass.Setup(_settings);
            _pass.ConfigureInput(_settings.IsOutlineEnabled
                ? ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal
                : ScriptableRenderPassInput.None);
            renderer.EnqueuePass(_pass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (_pass != null && renderingData.cameraData.cameraType == CameraType.Game)
            {
                _pass.SetTarget(renderer.cameraColorTargetHandle);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            CoreUtils.Destroy(_material);
        }
    }
}
