using System;
using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 教学箭头指向哪里。
    /// </summary>
    [Serializable]
    public struct TutorialTarget
    {
        [SerializeField] private TutorialTargetKind _kind;

        [Tooltip("箱子编号、传送箱子下标、终点序号或世界编号，见 TutorialTargetKind")]
        [Min(0)]
        [SerializeField] private int _index;

        [Tooltip("Kind 为 Cell 时使用的格子坐标，左下角为原点")]
        [SerializeField] private Vector2Int _cell;

        public TutorialTarget(TutorialTargetKind kind, int index, Vector2Int cell)
        {
            _kind = kind;
            _index = index;
            _cell = cell;
        }

        public static TutorialTarget None => default;

        public TutorialTargetKind Kind => _kind;
        public int Index => _index;
        public Vector2Int Cell => _cell;

        public static TutorialTarget MappedBox(int boxIndex)
        {
            return new TutorialTarget(TutorialTargetKind.MappedBox, boxIndex, Vector2Int.zero);
        }
    }
}
