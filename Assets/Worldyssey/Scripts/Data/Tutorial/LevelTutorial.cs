using System;
using System.Collections.Generic;
using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 一关的教学步骤，按顺序逐条出现。
    /// </summary>
    [Serializable]
    public class LevelTutorial
    {
        [Tooltip("关卡 id，与关卡 JSON 的 id 一致，例如 season-1-1")]
        [SerializeField] private string _levelId = string.Empty;

        [SerializeField] private List<TutorialStep> _steps = new List<TutorialStep>();

        public LevelTutorial()
        {
        }

        public LevelTutorial(string levelId, List<TutorialStep> steps)
        {
            _levelId = levelId;
            _steps = steps;
        }

        public string LevelId => _levelId;
        public IReadOnlyList<TutorialStep> Steps => _steps;
    }
}
