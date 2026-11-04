using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.View
{
    /// <summary>
    /// 截图缩放转场：玩家钻进传送箱子后，把当前画面截下来投影到伙伴箱子外的外壳上，
    /// 相机瞬移到外壳跟前让它铺满画面（看起来和截图一模一样），再拉回正常机位，像从上一个世界的“箱子”里穿越出来。
    /// 由 <see cref="StepPlayer"/> 在传送时调用，打断时调用 <see cref="Cancel"/>。
    /// </summary>
    public class SnapshotTeleportTransition : MonoBehaviour
    {
        private const string ShellName = "SnapshotShell";
        private static readonly int SnapshotTexId = Shader.PropertyToID("_SnapshotTex");
        private static readonly int SnapshotVpId = Shader.PropertyToID("_SnapshotVP");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int TintStrengthId = Shader.PropertyToID("_TintStrength");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        [SerializeField] private LevelView _levelView;
        [SerializeField] private CameraRig _cameraRig;
        [SerializeField] private GameplayConfig _gameplayConfig;

        [Tooltip("快照外壳的材质（Worldyssey/SnapshotProjection 着色器），运行时复制一份使用，不修改资源本身")]
        [SerializeField] private Material _shellMaterial;

        private RenderTexture _snapshot;
        private Material _shellInstance;
        private Transform _shell;
        private Coroutine _cameraRoutine;
        private bool _hasReferences;

        /// <summary>引用齐全、可以播放。</summary>
        public bool IsAvailable => _hasReferences && _cameraRig.ViewCamera != null;

        private void Awake()
        {
            _hasReferences = CheckReference(_levelView, nameof(_levelView))
                & CheckReference(_cameraRig, nameof(_cameraRig))
                & CheckReference(_gameplayConfig, nameof(_gameplayConfig))
                & CheckReference(_shellMaterial, nameof(_shellMaterial));
            if (_hasReferences)
            {
                CreateShell();
            }
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void OnDestroy()
        {
            ReleaseSnapshot();
            if (_shellInstance != null)
            {
                Destroy(_shellInstance);
            }
        }

        /// <summary>
        /// 播放整段转场，由调用方的协程 yield。<paramref name="applyTeleportedState"/> 在截图之后、画面被外壳盖住时调用，
        /// 用来切换到新世界。<paramref name="pedestal"/> 是传送底座，<paramref name="towardBox"/> 是从玩家指向底座的方向。
        /// </summary>
        public IEnumerator Play(GridPos pedestal, Direction towardBox, int targetWorld, Action applyTeleportedState)
        {
            EntityView player = _levelView.PlayerView;
            Vector3 intoBox = _levelView.DirectionToLocal(towardBox);
            float cellSize = _levelView.CellSize;
            Vector3 boxCenter = _levelView.GridToWorld(pedestal) + Vector3.up * (cellSize * 0.5f);

            // 前半段：钻进箱子、相机推近（与原来的转场一致）
            float enterDuration = _gameplayConfig.TeleportEnterDuration;
            player.PlayVanish(intoBox, enterDuration);
            yield return _cameraRig.ZoomToward(boxCenter, _gameplayConfig.TeleportCameraZoom, enterDuration);

            // 截图：玩家已完全进入箱子。相机单独渲染一帧到贴图，不含界面
            Camera viewCamera = _cameraRig.ViewCamera;
            Capture(viewCamera);
            applyTeleportedState();

            // 起始机位：外壳铺满画面；外壳按起始机位投影截图，所以这一帧和截图一模一样，看不出世界已经换了
            var shellBounds = new Bounds(boxCenter, Vector3.one * (_gameplayConfig.SnapshotShellSize * cellSize));
            _cameraRig.SnapToward(boxCenter, SnapshotFraming.FindCoveringDistance(shellBounds, _cameraRig.CurrentRotation, viewCamera));
            ShowShell(shellBounds, viewCamera.projectionMatrix * viewCamera.worldToCameraMatrix, _levelView.GetWorldColor(targetWorld));

            // 后半段：拉回正常机位，玩家从箱子里走出来，外壳最后淡出。
            // 不播放“独有墙从地下升起”的展开动画：截图转场全程看得见新世界，墙现场升起很违和，直接以完整的样子出现
            float pullBack = _gameplayConfig.TeleportPullBackDuration;
            float emergeStart = _gameplayConfig.SnapshotEmergeStart;
            player.PlayEmerge(intoBox, pullBack * (1f - emergeStart), pullBack * emergeStart);
            _cameraRoutine = StartCoroutine(_cameraRig.PullBack(pullBack));

            float fadeStart = _gameplayConfig.SnapshotFadeStart;
            float elapsed = 0f;
            while (elapsed < pullBack)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / pullBack);
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fadeStart, 1f, progress));
                SetShellFade(alpha);
                yield return null;
            }

            HideShell();
            _cameraRoutine = null;
        }

        /// <summary>
        /// 立即结束：隐藏外壳、停止相机动画。相机复位由调用方负责（<see cref="CameraRig.ResetZoom"/>）。
        /// </summary>
        public void Cancel()
        {
            if (_cameraRoutine != null)
            {
                StopCoroutine(_cameraRoutine);
                _cameraRoutine = null;
            }

            HideShell();
        }

        private void Capture(Camera viewCamera)
        {
            int width = Mathf.Max(1, viewCamera.pixelWidth);
            int height = Mathf.Max(1, viewCamera.pixelHeight);
            if (_snapshot == null || _snapshot.width != width || _snapshot.height != height)
            {
                ReleaseSnapshot();
                _snapshot = new RenderTexture(width, height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default)
                {
                    name = "TeleportSnapshot",
                    wrapMode = TextureWrapMode.Clamp,
                };
            }

            RenderTexture previous = viewCamera.targetTexture;
            viewCamera.targetTexture = _snapshot;
            viewCamera.Render();
            viewCamera.targetTexture = previous;
        }

        private void CreateShell()
        {
            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shell.name = ShellName;
            Destroy(shell.GetComponent<Collider>());
            shell.transform.SetParent(transform, false);
            _shellInstance = new Material(_shellMaterial) { name = _shellMaterial.name + " (Instance)" };
            var shellRenderer = shell.GetComponent<MeshRenderer>();
            shellRenderer.sharedMaterial = _shellInstance;
            shellRenderer.shadowCastingMode = ShadowCastingMode.Off;
            shellRenderer.receiveShadows = false;
            shell.SetActive(false);
            _shell = shell.transform;
        }

        private void ShowShell(Bounds bounds, Matrix4x4 viewProjection, Color tint)
        {
            _shell.position = bounds.center;
            _shell.rotation = Quaternion.identity;
            _shell.localScale = bounds.size;
            _shellInstance.SetTexture(SnapshotTexId, _snapshot);
            _shellInstance.SetMatrix(SnapshotVpId, viewProjection);
            _shellInstance.SetColor(TintId, tint);
            SetShellFade(1f);
            _shell.gameObject.SetActive(true);
        }

        private void SetShellFade(float alpha)
        {
            _shellInstance.SetFloat(AlphaId, alpha);

            // 季节色随外壳一起淡出
            _shellInstance.SetFloat(TintStrengthId, _gameplayConfig.SnapshotTintStrength * alpha);
        }

        private void HideShell()
        {
            if (_shell != null)
            {
                _shell.gameObject.SetActive(false);
            }
        }

        private void ReleaseSnapshot()
        {
            if (_snapshot == null)
            {
                return;
            }

            _snapshot.Release();
            Destroy(_snapshot);
            _snapshot = null;
        }

        private bool CheckReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(SnapshotTeleportTransition)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
