using System.Collections.Generic;
using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 全部关卡教学和卡关提示的文字与参数。由 Worldyssey/Setup/Build Tutorial 生成，之后可直接在 Inspector 里改文字。
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialLibrary", menuName = "Worldyssey/Tutorial Library")]
    public class TutorialLibrary : ScriptableObject
    {
        [Header("关卡教学")]
        [SerializeField] private List<LevelTutorial> _tutorials = new List<LevelTutorial>();

        [Tooltip("一条提示完成后，隔多久出现下一条（秒）")]
        [Range(0f, 3f)]
        [SerializeField] private float _stepGapSeconds = 0.6f;

        [Header("卡关提示")]
        [Tooltip("卡关提示显示多久（秒），之后回到当前教学步骤")]
        [Range(1f, 15f)]
        [SerializeField] private float _stuckHintSeconds = 5f;

        [Tooltip("想进被锁住的世界时的提示，{0} 为世界名")]
        [SerializeField] private string _lockedWorldFormat = "{0} is locked: a box is sitting on one of its walls or portals. Move it away first.";

        [Tooltip("箱子被推进墙角时的提示（每次进关最多一次）")]
        [SerializeField] private string _cornerText = "That box is stuck in a corner. Press [Z] to undo.";

        [Tooltip("短时间内连续撤销很多次时的提示（每次进关最多一次）")]
        [SerializeField] private string _undoBurstText = "Stuck? Press [R] to start the level over.";

        [Tooltip("在下面的时间窗内撤销达到这个次数就提示重来")]
        [Range(2, 30)]
        [SerializeField] private int _undoBurstCount = 8;

        [Tooltip("连续撤销的统计时间窗（秒）")]
        [Range(5f, 120f)]
        [SerializeField] private float _undoBurstWindow = 30f;

        [Header("箭头")]
        [Tooltip("场景箭头比目标高出多少格")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _markerHeight = 1.3f;

        public float StepGapSeconds => _stepGapSeconds;
        public float StuckHintSeconds => _stuckHintSeconds;
        public string LockedWorldFormat => _lockedWorldFormat;
        public string CornerText => _cornerText;
        public string UndoBurstText => _undoBurstText;
        public int UndoBurstCount => _undoBurstCount;
        public float UndoBurstWindow => _undoBurstWindow;
        public float MarkerHeight => _markerHeight;

        /// <summary>有没有任何关卡教学，编辑器工具据此判断是否需要写入默认内容。</summary>
        public bool HasTutorials => _tutorials.Count > 0;

        /// <summary>
        /// 整体替换关卡教学。仅供编辑器工具使用，运行时不要修改资源本身。
        /// </summary>
        public void SetTutorials(List<LevelTutorial> tutorials)
        {
            _tutorials = tutorials ?? new List<LevelTutorial>();
        }

        /// <summary>
        /// 查找某关的教学，没有时返回 false。
        /// </summary>
        public bool TryGetTutorial(string levelId, out LevelTutorial tutorial)
        {
            for (int i = 0; i < _tutorials.Count; i++)
            {
                if (_tutorials[i] != null && _tutorials[i].LevelId == levelId)
                {
                    tutorial = _tutorials[i];
                    return true;
                }
            }

            tutorial = null;
            return false;
        }
    }
}
