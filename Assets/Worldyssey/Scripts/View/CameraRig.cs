using System.Collections;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.View
{
    /// <summary>
    /// 关卡相机：围绕关卡中心保持固定俯角，自动推拉到恰好完整看到整个关卡；支持每次 90° 旋转视角，
    /// 以及按住鼠标拖动平移（平移量限制在关卡范围内，换关卡时清零）。
    /// 旋转只影响画面，规则不知道它的存在；输入换算见 <see cref="ViewRotation"/>。
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private GameplayConfig _gameplayConfig;

        [Tooltip("没有关卡时使用的取景范围，避免菜单界面背后的相机位置随意")]
        [SerializeField] private Vector3 _defaultFocusSize = new Vector3(9f, 1f, 7f);

        private Bounds _focus;
        private Vector3 _focusOffset;
        private float _zoom = 1f;
        private float _yaw;
        private readonly CameraPanOffset _pan = new CameraPanOffset();
        private float _lastAspect;
        private Coroutine _rotateRoutine;

        /// <summary>相机从默认朝向起顺时针（俯视）转过的 90° 次数，0~3。旋转动画期间已是目标值。</summary>
        public int CameraQuarterTurns { get; private set; }

        public bool IsRotating => _rotateRoutine != null;

        /// <summary>被控制的相机。</summary>
        public Camera ViewCamera => _camera;

        /// <summary>正常取景时才能拖动：没在旋转，也没在推近（传送、入场的相机动画期间缩放或偏移不是默认值）。</summary>
        public bool CanPan => _rotateRoutine == null && _focusOffset == Vector3.zero && Mathf.Approximately(_zoom, 1f);

        /// <summary>当前视角的朝向（俯角加旋转）。</summary>
        public Quaternion CurrentRotation => Quaternion.Euler(_gameplayConfig != null ? _gameplayConfig.CameraPitch : 0f, _yaw, 0f);

        private void Awake()
        {
            if (_camera == null)
            {
                Debug.LogError($"[{nameof(CameraRig)}] {name} 缺少引用：{nameof(_camera)}", this);
            }

            if (_gameplayConfig == null)
            {
                Debug.LogError($"[{nameof(CameraRig)}] {name} 缺少引用：{nameof(_gameplayConfig)}", this);
            }

            _focus = new Bounds(Vector3.zero, _defaultFocusSize);
        }

        private void LateUpdate()
        {
            // 平移回中过渡中，或窗口比例变化（拖动窗口、切全屏）时重新取景；只比较一个浮点数，不分配内存
            bool isPanReturning = _pan.StepReturn(Time.deltaTime);
            if (isPanReturning || (_camera != null && !Mathf.Approximately(_camera.aspect, _lastAspect)))
            {
                ApplyPose();
            }
        }

        private void OnDisable()
        {
            CompleteRotation();
        }

        /// <summary>
        /// 对准新的关卡范围并立即取景。
        /// </summary>
        public void Frame(Bounds levelBounds)
        {
            _focus = levelBounds;
            _pan.Clear();
            ApplyPose();
        }

        /// <summary>
        /// 由调用方的协程 yield：在 <paramref name="duration"/> 秒内把焦点移向 <paramref name="worldPoint"/>，
        /// 距离缩放到 <paramref name="zoom"/> 倍。用于传送时推近传送箱子。偏移相对关卡中心：按键传送时平移量同时在回中，结束时正好对准目标。
        /// </summary>
        public IEnumerator ZoomToward(Vector3 worldPoint, float zoom, float duration)
        {
            return AnimateZoom(worldPoint - _focus.center, zoom, duration);
        }

        /// <summary>
        /// 立即把相机放到正常取景距离的 <paramref name="zoom"/> 倍处（对准关卡中心），用于关卡入场从远处推近。
        /// </summary>
        public void SetZoomImmediate(float zoom)
        {
            _focusOffset = Vector3.zero;
            _zoom = zoom;
            ApplyPose();
        }

        /// <summary>
        /// 由调用方的协程 yield：在 <paramref name="duration"/> 秒内回到正常取景。
        /// </summary>
        public IEnumerator ZoomBack(float duration)
        {
            return AnimateZoom(Vector3.zero, 1f, duration);
        }

        /// <summary>
        /// 朝向不变，立即把相机放到正对 <paramref name="worldPoint"/>、距离为 <paramref name="distance"/> 的位置。
        /// 用于截图转场的起始机位。
        /// </summary>
        public void SnapToward(Vector3 worldPoint, float distance)
        {
            if (_camera == null || _gameplayConfig == null)
            {
                return;
            }

            _pan.Clear();
            _focusOffset = worldPoint - _focus.center;
            float fitDistance = CalculateFitDistance(CurrentRotation);
            _zoom = fitDistance > 0f ? distance / fitDistance : 1f;
            ApplyPose();
        }

        /// <summary>
        /// 由调用方的协程 yield：从当前位置拉回正常取景。距离按指数插值，从极近处拉远时速度感均匀，
        /// 不会一开始就猛地拉开。
        /// </summary>
        public IEnumerator PullBack(float duration)
        {
            Vector3 startOffset = _focusOffset;
            float startZoom = Mathf.Max(_zoom, 0.0001f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                _focusOffset = Vector3.LerpUnclamped(startOffset, Vector3.zero, eased);
                _zoom = startZoom * Mathf.Pow(1f / startZoom, eased);
                ApplyPose();
                yield return null;
            }

            _focusOffset = Vector3.zero;
            _zoom = 1f;
            ApplyPose();
        }

        /// <summary>开始拖动：记下鼠标下方地面上的点，之后 <see cref="UpdatePan"/> 让这个点始终跟着鼠标。</summary>
        public void BeginPan(Vector2 screenPosition)
        {
            bool hasPoint = CameraFraming.TryGetGroundPoint(_camera, screenPosition, _focus.min.y, out Vector3 point);
            _pan.Grab(hasPoint, point);
        }

        /// <summary>拖动中每帧调用：让抓住的地面点回到鼠标下方。平移量限制在关卡范围内，焦点不会离开地图。</summary>
        public void UpdatePan(Vector2 screenPosition)
        {
            if (CameraFraming.TryGetGroundPoint(_camera, screenPosition, _focus.min.y, out Vector3 hit) && _pan.Drag(hit, _focus.extents))
            {
                ApplyPose();
            }
        }

        /// <summary>平滑回到关卡中心（时长见 GameplayConfig），并结束当前拖动。</summary>
        public void ReturnPanToCenter()
        {
            _pan.StartReturn(_gameplayConfig != null ? _gameplayConfig.PanReturnDuration : 0f);
        }

        /// <summary>
        /// 立即回到正常取景，用于打断传送过渡。
        /// </summary>
        public void ResetZoom()
        {
            _focusOffset = Vector3.zero;
            _zoom = 1f;
            ApplyPose();
        }

        /// <summary>
        /// 立即回到默认朝向，用于进入新关卡。
        /// </summary>
        public void ResetRotation()
        {
            CompleteRotation();
            CameraQuarterTurns = 0;
            _yaw = 0f;
            ApplyPose();
        }

        /// <summary>
        /// 旋转视角。<paramref name="sceneClockwise"/> 为 true 时画面中的场景顺时针转（相机逆时针转）。
        /// 正在旋转时忽略并返回 false。
        /// </summary>
        public bool TryRotate(bool sceneClockwise)
        {
            if (IsRotating || _gameplayConfig == null)
            {
                return false;
            }

            int cameraDelta = sceneClockwise ? -1 : 1;
            CameraQuarterTurns = ViewRotation.Normalize(CameraQuarterTurns + cameraDelta);
            float targetYaw = _yaw + cameraDelta * 90f;
            float duration = _gameplayConfig.ViewRotateDuration;
            if (duration <= 0f || !isActiveAndEnabled)
            {
                _yaw = targetYaw;
                ApplyPose();
                return true;
            }

            _rotateRoutine = StartCoroutine(RotateRoutine(targetYaw, duration));
            return true;
        }

        private IEnumerator AnimateZoom(Vector3 targetOffset, float targetZoom, float duration)
        {
            Vector3 startOffset = _focusOffset;
            float startZoom = _zoom;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                _focusOffset = Vector3.LerpUnclamped(startOffset, targetOffset, eased);
                _zoom = Mathf.LerpUnclamped(startZoom, targetZoom, eased);
                ApplyPose();
                yield return null;
            }

            _focusOffset = targetOffset;
            _zoom = targetZoom;
            ApplyPose();
        }

        private void CompleteRotation()
        {
            if (_rotateRoutine == null)
            {
                return;
            }

            StopCoroutine(_rotateRoutine);
            _rotateRoutine = null;
            _yaw = CameraQuarterTurns * 90f;
            ApplyPose();
        }

        private IEnumerator RotateRoutine(float targetYaw, float duration)
        {
            float startYaw = _yaw;
            AnimationCurve curve = _gameplayConfig.MoveCurve;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                _yaw = Mathf.LerpUnclamped(startYaw, targetYaw, curve != null ? curve.Evaluate(progress) : progress);
                ApplyPose();
                yield return null;
            }

            // 收尾时把角度规整到 0~360 内的整 90°，避免长时间旋转后角度累积
            _yaw = CameraQuarterTurns * 90f;
            ApplyPose();
            _rotateRoutine = null;
        }

        private void ApplyPose()
        {
            if (_camera == null || _gameplayConfig == null)
            {
                return;
            }

            _lastAspect = _camera.aspect;
            Quaternion rotation = Quaternion.Euler(_gameplayConfig.CameraPitch, _yaw, 0f);
            Vector3 forward = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;
            float distance = CalculateFitDistance(rotation) * _zoom;

            // 顶部留给 HUD：相机沿自身向上平移，关卡在画面里下移到剩余区域的中央。
            // 推近（传送截图、推向箱子）时按缩放比例减弱，贴近目标时目标仍在画面正中，与截图机位一致
            float tanHalfVertical = Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float shift = distance * tanHalfVertical * _gameplayConfig.CameraTopReserve * Mathf.Clamp01(_zoom);
            Transform cameraTransform = _camera.transform;
            cameraTransform.SetPositionAndRotation(_focus.center + _pan.Value + _focusOffset - forward * distance + up * shift, rotation);
        }

        private float CalculateFitDistance(Quaternion rotation)
        {
            return CameraFraming.FitDistance(_camera, rotation, _focus.extents, _gameplayConfig.CameraMargin, _gameplayConfig.CameraTopReserve);
        }
    }
}
