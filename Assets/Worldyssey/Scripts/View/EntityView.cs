using System.Collections;
using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 玩家、箱子等会移动的实体模型：负责位置和朝向的补间，以及回弹、挤压、弹跳等纯表现效果。
    /// </summary>
    /// <remarks>
    /// 逻辑位置（补间的终点）与效果偏移分开保存，每次变化后合成写入 Transform，
    /// 所以补间和各种效果可以同时进行而互不覆盖。实体只绕竖直轴转向，朝向用偏航角表示。
    /// </remarks>
    public class EntityView : MonoBehaviour
    {
        private Vector3 _position;
        private float _yaw;
        private Vector3 _targetPosition;
        private float _targetYaw;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _bumpOffset;
        private Vector3 _popOffset;
        private Vector3 _scaleMultiplier = Vector3.one;
        private Coroutine _tween;
        private Coroutine _bump;
        private Coroutine _pop;
        private Coroutine _squash;

        public bool IsTweening => _tween != null;

        protected virtual void Awake()
        {
            _baseScale = transform.localScale;
            _position = transform.localPosition;
            _yaw = transform.localEulerAngles.y;
        }

        private void OnDisable()
        {
            // 禁用时协程会被打断，先落到终点并清掉效果，避免模型停在半路或保持变形
            CompleteTween();
            StopEffects();
        }

        /// <summary>
        /// 立即移动到目标位置和朝向（相对父节点），会中止正在进行的补间。
        /// </summary>
        public void SnapTo(Vector3 localPosition, Quaternion localRotation)
        {
            StopRoutine(ref _tween);
            _position = localPosition;
            _yaw = localRotation.eulerAngles.y;
            Apply();
        }

        /// <summary>
        /// 在 <paramref name="duration"/> 秒内补间到目标位置和朝向（相对父节点）。
        /// </summary>
        public void TweenTo(Vector3 localPosition, Quaternion localRotation, float duration, AnimationCurve curve)
        {
            if (duration <= 0f || !isActiveAndEnabled)
            {
                SnapTo(localPosition, localRotation);
                return;
            }

            StopRoutine(ref _tween);
            _targetPosition = localPosition;
            _targetYaw = localRotation.eulerAngles.y;
            _tween = StartCoroutine(TweenRoutine(duration, curve));
        }

        /// <summary>
        /// 若正在补间，立即跳到终点。
        /// </summary>
        public void CompleteTween()
        {
            if (_tween == null)
            {
                return;
            }

            StopRoutine(ref _tween);
            _position = _targetPosition;
            _yaw = _targetYaw;
            Apply();
        }

        /// <summary>
        /// 朝 <paramref name="localDirection"/> 顶出 <paramref name="distance"/> 再弹回，用于撞墙反馈。
        /// </summary>
        public void PlayBump(Vector3 localDirection, float distance, float duration)
        {
            if (!CanPlayEffect(duration))
            {
                return;
            }

            StopRoutine(ref _bump);
            _bump = StartCoroutine(BumpRoutine(localDirection.normalized * distance, duration));
        }

        /// <summary>
        /// 沿 <paramref name="localDirection"/> 压扁再恢复，用于被推动的箱子。
        /// </summary>
        public void PlaySquash(Vector3 localDirection, float amount, float duration)
        {
            if (!CanPlayEffect(duration) || amount <= 0f)
            {
                return;
            }

            StopRoutine(ref _squash);
            _squash = StartCoroutine(SquashRoutine(localDirection, amount, duration));
        }

        /// <summary>
        /// 原地弹起再落下，用于箱子到达终点和通关庆祝。
        /// </summary>
        public void PlayPop(float height, float duration)
        {
            if (!CanPlayEffect(duration) || height <= 0f)
            {
                return;
            }

            StopRoutine(ref _pop);
            _pop = StartCoroutine(PopRoutine(height, duration));
        }

        /// <summary>
        /// 立即停止全部表现效果并复原，用于撤销、重开等需要画面立刻对齐状态的场合。
        /// </summary>
        public void StopEffects()
        {
            StopRoutine(ref _bump);
            StopRoutine(ref _squash);
            StopRoutine(ref _pop);
            _bumpOffset = Vector3.zero;
            _popOffset = Vector3.zero;
            _scaleMultiplier = Vector3.one;
            Apply();
        }

        private bool CanPlayEffect(float duration)
        {
            return duration > 0f && isActiveAndEnabled;
        }

        private void StopRoutine(ref Coroutine routine)
        {
            if (routine == null)
            {
                return;
            }

            StopCoroutine(routine);
            routine = null;
        }

        private void Apply()
        {
            transform.localPosition = _position + _bumpOffset + _popOffset;
            transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
            transform.localScale = Vector3.Scale(_baseScale, _scaleMultiplier);
        }

        private IEnumerator TweenRoutine(float duration, AnimationCurve curve)
        {
            Vector3 startPosition = _position;
            float startYaw = _yaw;

            // 取最短转向；恰好 180° 时两边一样远，固定顺时针转，避免方向随机
            float deltaYaw = Mathf.DeltaAngle(startYaw, _targetYaw);
            if (Mathf.Approximately(Mathf.Abs(deltaYaw), 180f))
            {
                deltaYaw = 180f;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = curve != null ? curve.Evaluate(progress) : progress;
                _position = Vector3.LerpUnclamped(startPosition, _targetPosition, eased);
                _yaw = startYaw + deltaYaw * eased;
                Apply();
                yield return null;
            }

            _position = _targetPosition;
            _yaw = _targetYaw;
            Apply();
            _tween = null;
        }

        private IEnumerator BumpRoutine(Vector3 offset, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _bumpOffset = offset * Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                Apply();
                yield return null;
            }

            _bumpOffset = Vector3.zero;
            Apply();
            _bump = null;
        }

        private IEnumerator SquashRoutine(Vector3 localDirection, float amount, float duration)
        {
            // 沿推动方向压短，另外两个方向稍微变宽，看起来像受力
            bool isAlongX = Mathf.Abs(localDirection.x) >= Mathf.Abs(localDirection.z);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float strength = amount * Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                float along = 1f - strength;
                float across = 1f + strength * 0.5f;
                _scaleMultiplier = isAlongX ? new Vector3(along, across, across) : new Vector3(across, across, along);
                Apply();
                yield return null;
            }

            _scaleMultiplier = Vector3.one;
            Apply();
            _squash = null;
        }

        private IEnumerator PopRoutine(float height, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _popOffset = Vector3.up * (height * Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI));
                Apply();
                yield return null;
            }

            _popOffset = Vector3.zero;
            Apply();
            _pop = null;
        }
    }
}
