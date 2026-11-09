using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 让网格布局固定每行 <see cref="_columns"/> 个，格子宽度按可用宽度平分（取整像素），高度保持不变。
    /// 用于选关卡片墙：屏幕变宽时卡片跟着变宽，而不是一行塞更多张。
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class GridColumnFitter : MonoBehaviour
    {
        [Min(1)]
        [SerializeField] private int _columns = 4;

        private GridLayoutGroup _grid;
        private RectTransform _rect;

        private void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            Fit();
        }

        private void OnRectTransformDimensionsChange()
        {
            // 尺寸变化时才调用，不是每帧；Awake 之前也可能被调用，需要判空
            if (_grid != null)
            {
                Fit();
            }
        }

        private void Fit()
        {
            float available = _rect.rect.width - _grid.padding.left - _grid.padding.right - _grid.spacing.x * (_columns - 1);
            if (available <= 0f)
            {
                return;
            }

            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = _columns;
            _grid.cellSize = new Vector2(Mathf.Floor(available / _columns), _grid.cellSize.y);
        }
    }
}
