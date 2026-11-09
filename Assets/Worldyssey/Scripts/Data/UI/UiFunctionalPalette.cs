using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 功能色：确认、取消、禁用。所有主题共用一份，保证按钮含义不随季节变化，玩家不需要重新辨认。
    /// </summary>
    [CreateAssetMenu(fileName = "UiFunctionalPalette", menuName = "Worldyssey/UI/Functional Palette")]
    public class UiFunctionalPalette : ScriptableObject
    {
        [SerializeField] private Color _confirm = new Color(0.30f, 0.72f, 0.47f, 1f);
        [SerializeField] private Color _cancel = new Color(0.93f, 0.42f, 0.36f, 1f);
        [SerializeField] private Color _disabled = new Color(0.70f, 0.72f, 0.75f, 1f);

        public Color Confirm => _confirm;
        public Color Cancel => _cancel;
        public Color Disabled => _disabled;
    }
}
