using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 关卡编辑器网格的配色。独有墙不在这里：它用当前世界的季节色（<see cref="WorldPalette"/>），
    /// 画的时候一眼能看出在编辑哪个世界；传送箱子用它目标世界的季节色，与 3D 中的着色一致。
    /// </summary>
    [CreateAssetMenu(fileName = "EditorGridPalette", menuName = "Worldyssey/UI/Editor Grid Palette")]
    public class EditorGridPalette : ScriptableObject
    {
        [Header("外框")]
        [Tooltip("没有问题的格子的外框，也就是格子之间的缝")]
        [SerializeField] private Color _frame = new Color(0.82f, 0.84f, 0.87f, 1f);
        [SerializeField] private Color _errorFrame = new Color(0.93f, 0.42f, 0.36f, 1f);
        [SerializeField] private Color _warningFrame = new Color(0.96f, 0.70f, 0.28f, 1f);

        [Header("地形")]
        [SerializeField] private Color _floor = new Color(0.96f, 0.95f, 0.92f, 1f);
        [SerializeField] private Color _sharedWall = new Color(0.33f, 0.36f, 0.42f, 1f);
        [SerializeField] private Color _pedestal = new Color(0.62f, 0.80f, 0.82f, 1f);
        [SerializeField] private Color _goal = new Color(0.98f, 0.84f, 0.42f, 1f);

        [Tooltip("调色板里找不到当前世界颜色时，独有墙用这个颜色")]
        [SerializeField] private Color _uniqueWallFallback = new Color(0.40f, 0.47f, 0.62f, 1f);

        [Header("实体")]
        [SerializeField] private Color _player = new Color(0.25f, 0.45f, 0.80f, 1f);
        [SerializeField] private Color _mappedBox = new Color(0.72f, 0.48f, 0.30f, 1f);

        [Tooltip("调色板里找不到目标世界颜色时，传送箱子用这个颜色")]
        [SerializeField] private Color _portalBoxFallback = new Color(0.65f, 0.35f, 0.90f, 1f);

        public Color Frame => _frame;
        public Color ErrorFrame => _errorFrame;
        public Color WarningFrame => _warningFrame;
        public Color Floor => _floor;
        public Color SharedWall => _sharedWall;
        public Color Pedestal => _pedestal;
        public Color Goal => _goal;
        public Color UniqueWallFallback => _uniqueWallFallback;
        public Color Player => _player;
        public Color MappedBox => _mappedBox;
        public Color PortalBoxFallback => _portalBoxFallback;
    }
}
