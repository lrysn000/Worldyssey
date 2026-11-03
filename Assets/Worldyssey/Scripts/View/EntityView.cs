using System.Collections;
using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 玩家、箱子等会移动的实体模型，负责自身的位置和朝向补间。
    /// </summary>
    public class EntityView : MonoBehaviour
    {
        private Coroutine _tween;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;

        public bool IsTweening => _tween != null;

        private void OnDisable()
        {
            // 禁用时协程会被打断，先落到终点，避免模型停在半路
            CompleteTween();
        }

        /// <summary>
        /// 立即移动到目标位置和朝向（相对父节点），会中止正在进行的补间。
        /// </summary>
        public void SnapTo(Vector3 localPosition, Quaternion localRotation)
        {
            StopTween();
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
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

            StopTween();
            _targetPosition = localPosition;
            _targetRotation = localRotation;
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

            StopTween();
            transform.localPosition = _targetPosition;
            transform.localRotation = _targetRotation;
        }

        private void StopTween()
        {
            if (_tween == null)
            {
                return;
            }

            StopCoroutine(_tween);
            _tween = null;
        }

        private IEnumerator TweenRoutine(float duration, AnimationCurve curve)
        {
            Vector3 startPosition = transform.localPosition;
            Quaternion startRotation = transform.localRotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = curve != null ? curve.Evaluate(progress) : progress;
                transform.localPosition = Vector3.LerpUnclamped(startPosition, _targetPosition, eased);
                transform.localRotation = Quaternion.SlerpUnclamped(startRotation, _targetRotation, eased);
                yield return null;
            }

            transform.localPosition = _targetPosition;
            transform.localRotation = _targetRotation;
            _tween = null;
        }
    }
}
