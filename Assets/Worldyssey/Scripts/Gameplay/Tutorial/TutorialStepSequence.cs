using System.Collections.Generic;
using Worldyssey.Data;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 一关教学步骤的推进：等开始条件、到时间显示、等完成条件、隔一小段时间进入下一步。
    /// 不碰界面，只告诉调用方当前该显示哪一步；方法返回 true 表示显示的步骤变了，需要刷新提示条。
    /// </summary>
    public sealed class TutorialStepSequence
    {
        private IReadOnlyList<TutorialStep> _steps;
        private float _gapSeconds;
        private int _index;
        private bool _isStarted;
        private bool _isDisplayed;
        private float _showAt;
        private float _displayedAt;

        /// <summary>正在显示的步骤，没有时为 null。</summary>
        public TutorialStep DisplayedStep => _isDisplayed ? Current : null;

        private TutorialStep Current => _steps != null && _index < _steps.Count ? _steps[_index] : null;

        /// <summary>从第一步开始。<paramref name="steps"/> 为 null 表示这关没有教学。</summary>
        public void Begin(IReadOnlyList<TutorialStep> steps, float gapSeconds, float now)
        {
            _steps = steps;
            _gapSeconds = gapSeconds;
            _index = 0;
            Prepare(now);
        }

        public void Clear()
        {
            _steps = null;
            _isDisplayed = false;
        }

        /// <summary>
        /// 每帧调用：到时间且 <paramref name="canDisplay"/> 时显示当前步骤，按秒完成的步骤到时完成。
        /// </summary>
        public bool Tick(float now, bool canDisplay)
        {
            TutorialStep step = Current;
            if (step == null)
            {
                return false;
            }

            if (_isStarted && !_isDisplayed && now >= _showAt && canDisplay)
            {
                _isDisplayed = true;
                _displayedAt = now;
                return true;
            }

            if (_isDisplayed && step.CompleteOn == TutorialTrigger.AfterSeconds && now - _displayedAt >= step.Seconds)
            {
                return Complete(TutorialSignal.Direct(TutorialTrigger.AfterSeconds), now);
            }

            return false;
        }

        /// <summary>玩家做了一个动作。</summary>
        public bool Receive(TutorialSignal signal, float now)
        {
            TutorialStep step = Current;
            if (step == null)
            {
                return false;
            }

            if (!_isStarted)
            {
                // 开始条件和完成条件不用同一个动作判断，否则提示刚出现就消失
                _isStarted = signal.Matches(step.StartOn);
                return false;
            }

            // 还没来得及显示就做到了，说明玩家已经会了，直接跳过
            return signal.Matches(step.CompleteOn) && Complete(signal, now);
        }

        private bool Complete(TutorialSignal signal, float now)
        {
            bool wasDisplayed = _isDisplayed;
            _index++;
            Prepare(now + _gapSeconds);

            // 下一步的开始条件可能和这一步的完成发生在同一个动作里（例如传送时回程箱子同时出现）
            TutorialStep next = Current;
            if (next != null && !_isStarted)
            {
                _isStarted = signal.Matches(next.StartOn);
            }

            return wasDisplayed;
        }

        private void Prepare(float showAt)
        {
            _isDisplayed = false;
            TutorialStep step = Current;
            _isStarted = step != null && step.StartOn == TutorialTrigger.None;
            _showAt = showAt;
        }
    }
}
