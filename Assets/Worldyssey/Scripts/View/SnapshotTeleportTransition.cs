using System;
using System.Collections;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.View
{
    /// <summary>
    /// 截图缩放转场：玩家钻进传送箱子后截下当前画面，相机瞬间推近到伙伴箱子，让箱子的轮廓正好塞进画面（内切），
    /// 截图分两层显示（见 <see cref="SnapshotPhoto"/>），这一帧和截图一模一样。随后相机拉回：
    /// 箱子轮廓以外的截图先淡出，剩下的截图贴在箱子上等比缩小，最后淡出，箱子轻轻一压，像上一个世界被装进了箱子。
    /// 由 <see cref="StepPlayer"/> 在传送时调用，打断时调用 <see cref="Cancel"/>。
    /// </summary>
    public class SnapshotTeleportTransition : MonoBehaviour
    {
        private static readonly int OutlineSuppressId = Shader.PropertyToID("_StylizeOutlineSuppress");

        [SerializeField] private LevelView _levelView;
        [SerializeField] private CameraRig _cameraRig;
        [SerializeField] private GameplayConfig _gameplayConfig;

        [Tooltip("截图材质（Worldyssey/SnapshotProjection 着色器），运行时复制使用，不修改资源本身")]
        [SerializeField] private Material _shellMaterial;

        private RenderTexture _snapshot;
        private SnapshotPhoto _photo;
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
                _photo = new SnapshotPhoto(transform, _shellMaterial);
            }
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void OnDestroy()
        {
            ReleaseSnapshot();
            _photo?.Destroy();
        }

        /// <summary>
        /// 播放整段转场，由调用方的协程 yield。<paramref name="applyTeleportedState"/> 在截图之后、画面被截图盖住时调用，
        /// 用来切换到新世界。<paramref name="pedestal"/> 是传送底座，<paramref name="towardBox"/> 是从玩家指向底座的方向。
        /// <paramref name="isBoxSquashed"/> 为 false 时截图淡出时不压箱子（回程箱子另有合成动画负责）。
        /// </summary>
        public IEnumerator Play(GridPos pedestal, Direction towardBox, int targetWorld, Action applyTeleportedState, bool isBoxSquashed = true)
        {
            EntityView player = _levelView.PlayerView;
            Vector3 intoBox = _levelView.DirectionToLocal(towardBox);
            float cellSize = _levelView.CellSize;
            Vector3 pedestalWorld = _levelView.GridToWorld(pedestal);
            Vector3 boxCenter = pedestalWorld + Vector3.up * (cellSize * 0.5f);

            // 前半段：钻进箱子、相机推近
            float enterDuration = _gameplayConfig.TeleportEnterDuration;
            player.PlayVanish(intoBox, enterDuration);
            yield return _cameraRig.ZoomToward(boxCenter, _gameplayConfig.TeleportCameraZoom, enterDuration);

            // 截图：玩家已完全进入箱子。相机单独渲染一帧到贴图，不含界面
            Camera viewCamera = _cameraRig.ViewCamera;
            Capture(viewCamera);
            applyTeleportedState();

            // 起始机位：箱子轮廓内切于画面。截图按这个机位投影，所以这一帧和截图一模一样，看不出世界已经换了
            var shell = new Bounds(boxCenter, Vector3.one * (_gameplayConfig.SnapshotShellSize * cellSize));
            Quaternion rotation = _cameraRig.CurrentRotation;
            float size = CameraFraming.FitOrthoSize(viewCamera.aspect, rotation, shell.extents, 0f, 0f);
            _cameraRig.SnapToward(boxCenter, size);

            // 外圈略大于画面，避免像素对齐造成的边缘缝隙
            var outerSize = new Vector2(size * 2f * viewCamera.aspect, size * 2f) * 1.02f;
            _photo.Show(shell, rotation, outerSize, _snapshot, viewCamera.projectionMatrix * viewCamera.worldToCameraMatrix,
                _levelView.GetWorldColor(targetWorld));

            // 后半段：拉回正常机位，玩家从箱子里走出来
            float pullBack = _gameplayConfig.TeleportPullBackDuration;
            float emergeStart = _gameplayConfig.SnapshotEmergeStart;
            player.PlayEmerge(intoBox, pullBack * (1f - emergeStart), pullBack * emergeStart);
            _cameraRoutine = StartCoroutine(_cameraRig.PullBack(pullBack));

            EntityView partnerBox = FindBoxAt(pedestalWorld, cellSize);
            bool hasSquashed = false;
            float elapsed = 0f;
            while (elapsed < pullBack)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / pullBack);
                float outerAlpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, _gameplayConfig.SnapshotOuterFadeEnd, progress));
                float shellAlpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_gameplayConfig.SnapshotFadeStart, 1f, progress));
                _photo.SetFade(shellAlpha, outerAlpha, _gameplayConfig.SnapshotTintStrength);

                // 外圈截图里已经有旧世界的描边，盖住画面时不能再按新世界的深度叠线；外圈淡出后箱子上的线正好成为箱子的轮廓
                Shader.SetGlobalFloat(OutlineSuppressId, outerAlpha);

                if (isBoxSquashed && !hasSquashed && progress >= _gameplayConfig.SnapshotFadeStart)
                {
                    hasSquashed = true;
                    if (partnerBox != null)
                    {
                        partnerBox.PlaySquash(Vector3.up, _gameplayConfig.SnapshotBoxSquash, _gameplayConfig.SnapshotBoxSquashDuration);
                    }
                }

                yield return null;
            }

            HidePhoto();
            _cameraRoutine = null;
        }

        /// <summary>
        /// 立即结束：隐藏截图、停止相机动画。相机复位由调用方负责（<see cref="CameraRig.ResetZoom"/>）。
        /// </summary>
        public void Cancel()
        {
            if (_cameraRoutine != null)
            {
                StopCoroutine(_cameraRoutine);
                _cameraRoutine = null;
            }

            HidePhoto();
        }

        /// <summary>底座上当前显示的传送箱子（传送后就是伙伴箱子），找不到时返回 null。</summary>
        private EntityView FindBoxAt(Vector3 pedestalWorld, float cellSize)
        {
            float maxDistance = cellSize * 0.5f;
            for (int i = 0; i < _levelView.PortalBoxViewCount; i++)
            {
                PortalBoxView view = _levelView.GetPortalBoxView(i);
                Vector3 offset = view.transform.position - pedestalWorld;
                offset.y = 0f;
                if (view.gameObject.activeInHierarchy && offset.sqrMagnitude <= maxDistance * maxDistance)
                {
                    return view;
                }
            }

            return null;
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

        private void HidePhoto()
        {
            _photo?.Hide();
            Shader.SetGlobalFloat(OutlineSuppressId, 0f);
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
