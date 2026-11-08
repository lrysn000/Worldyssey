using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;
using Worldyssey.UI;
using Worldyssey.View;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 把 <see cref="TutorialTarget"/> 换算成箭头位置：场景对象取模型位置，格子和终点取格子中心，世界图标取界面元素。
    /// 目标暂时看不见（例如传送箱子在别的世界）时隐藏箭头。
    /// </summary>
    public sealed class TutorialTargetPointer
    {
        private readonly LevelSession _levelSession;
        private readonly LevelView _levelView;
        private readonly WorldIndicatorPanel _worldIndicator;
        private readonly TutorialMarker _marker;

        public TutorialTargetPointer(LevelSession levelSession, LevelView levelView, WorldIndicatorPanel worldIndicator, TutorialMarker marker)
        {
            _levelSession = levelSession;
            _levelView = levelView;
            _worldIndicator = worldIndicator;
            _marker = marker;
        }

        /// <summary>
        /// 让箭头指向 <paramref name="target"/>，<paramref name="heightInCells"/> 是场景箭头比目标高出的格数。
        /// </summary>
        public void Point(TutorialTarget target, float heightInCells)
        {
            if (target.Kind == TutorialTargetKind.WorldIcon)
            {
                RectTransform slot = _worldIndicator != null ? _worldIndicator.GetSlotRect(target.Index) : null;
                if (slot != null)
                {
                    _marker.PointAtUi(slot);
                }
                else
                {
                    _marker.Hide();
                }

                return;
            }

            if (TryGetWorldPoint(target, out Vector3 point))
            {
                _marker.PointAtWorld(point + Vector3.up * (heightInCells * _levelView.CellSize));
            }
            else
            {
                _marker.Hide();
            }
        }

        private bool TryGetWorldPoint(TutorialTarget target, out Vector3 point)
        {
            point = Vector3.zero;
            if (!_levelSession.HasLevel)
            {
                return false;
            }

            int index = target.Index;
            switch (target.Kind)
            {
                case TutorialTargetKind.Player:
                    return TryGetViewPoint(_levelView.PlayerView != null ? _levelView.PlayerView.transform : null, out point);
                case TutorialTargetKind.MappedBox:
                    return index < _levelView.MappedBoxViewCount && TryGetViewPoint(_levelView.GetMappedBoxView(index).transform, out point);
                case TutorialTargetKind.PortalBox:
                    return index < _levelView.PortalBoxViewCount && TryGetViewPoint(_levelView.GetPortalBoxView(index).transform, out point);
                case TutorialTargetKind.Goal:
                    var goals = _levelSession.Session.Rules.Map.GoalPositions;
                    if (index >= goals.Count)
                    {
                        return false;
                    }

                    point = _levelView.GridToWorld(goals[index]);
                    return true;
                case TutorialTargetKind.Cell:
                    point = _levelView.GridToWorld(new GridPos(target.Cell.x, target.Cell.y));
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetViewPoint(Transform view, out Vector3 point)
        {
            // 不在当前世界的模型是隐藏的，这时不指
            bool isVisible = view != null && view.gameObject.activeInHierarchy;
            point = isVisible ? view.position : Vector3.zero;
            return isVisible;
        }
    }
}
