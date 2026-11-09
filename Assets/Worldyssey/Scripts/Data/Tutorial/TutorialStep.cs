using System;
using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 一条教学提示：什么时候出现、显示什么、指向哪里、什么时候算完成。
    /// </summary>
    [Serializable]
    public class TutorialStep
    {
        [Tooltip("提示文字。方括号里的按键（例如 [F]）会用按键颜色显示")]
        [TextArea(2, 4)]
        [SerializeField] private string _text = string.Empty;

        [Tooltip("上一步完成后，等这个条件发生才出现；None 表示立即出现")]
        [SerializeField] private TutorialTrigger _startOn = TutorialTrigger.None;

        [Tooltip("这个条件发生后提示消失、进入下一步；None 表示一直显示到关卡结束")]
        [SerializeField] private TutorialTrigger _completeOn = TutorialTrigger.MovedOrTurned;

        [Tooltip("完成条件为 AfterSeconds 时显示的秒数")]
        [Min(0f)]
        [SerializeField] private float _seconds = 4f;

        [SerializeField] private TutorialTarget _target;

        public TutorialStep()
        {
        }

        public TutorialStep(string text, TutorialTrigger startOn, TutorialTrigger completeOn, TutorialTarget target, float seconds = 4f)
        {
            _text = text;
            _startOn = startOn;
            _completeOn = completeOn;
            _target = target;
            _seconds = seconds;
        }

        public string Text => _text;
        public TutorialTrigger StartOn => _startOn;
        public TutorialTrigger CompleteOn => _completeOn;
        public float Seconds => _seconds;
        public TutorialTarget Target => _target;
    }
}
