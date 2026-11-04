using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 游戏鼠标指针：普通、悬停在可点击物体上、按下三种状态。贴图需以 Cursor 类型导入并开启读写。
    /// </summary>
    [CreateAssetMenu(fileName = "CursorSet", menuName = "Worldyssey/UI/Cursor Set")]
    public class CursorSet : ScriptableObject
    {
        [SerializeField] private Texture2D _normal;
        [SerializeField] private Texture2D _hover;
        [SerializeField] private Texture2D _pressed;

        [Tooltip("点击判定点在贴图中的像素位置（左上角为原点），对准箭头尖端")]
        [SerializeField] private Vector2 _hotspot = new Vector2(4f, 3f);

        [Tooltip("勾选后由 Unity 绘制指针：任何尺寸都能正常显示，但比系统指针慢一帧。系统指针显示偏小或模糊时再勾选")]
        [SerializeField] private bool _useSoftwareCursor;

        public Texture2D Normal => _normal;
        public Texture2D Hover => _hover;
        public Texture2D Pressed => _pressed;
        public Vector2 Hotspot => _hotspot;
        public CursorMode Mode => _useSoftwareCursor ? CursorMode.ForceSoftware : CursorMode.Auto;
    }
}
