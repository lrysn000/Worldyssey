using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.Data
{
    /// <summary>
    /// 每个世界（季节）的代表色，是季节色的唯一来源：传送箱子、传送遮罩、UI 强调色都从这里取。
    /// </summary>
    [CreateAssetMenu(fileName = "WorldPalette", menuName = "Worldyssey/World Palette")]
    public class WorldPalette : ScriptableObject
    {
        [Tooltip("按世界编号排列：0 春、1 夏、2 秋、3 冬。冬季用中蓝而非冰白，保证传送箱子发光在浅色地面上看得清")]
        [SerializeField] private Color[] _colors =
        {
            new Color(0.90f, 0.48f, 0.60f, 1f),
            new Color(0.15f, 0.64f, 0.66f, 1f),
            new Color(0.86f, 0.50f, 0.16f, 1f),
            new Color(0.36f, 0.60f, 0.82f, 1f),
        };

        /// <summary>
        /// 取某个世界的代表色，编号越界时返回 false。
        /// </summary>
        public bool TryGetColor(int world, out Color color)
        {
            if (_colors != null && world >= 0 && world < _colors.Length)
            {
                color = _colors[world];
                return true;
            }

            color = default;
            return false;
        }

        private void OnValidate()
        {
            if (_colors == null || _colors.Length < LevelLimits.MaxWorldCount)
            {
                Debug.LogError($"[{nameof(WorldPalette)}] {name} 的颜色少于 {LevelLimits.MaxWorldCount} 个，部分世界会取不到颜色", this);
            }
        }
    }
}
