using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.Data
{
    /// <summary>
    /// 内置关卡资源，包装一个 <see cref="LevelData"/>。
    /// </summary>
    [CreateAssetMenu(fileName = "Level_New", menuName = "Worldyssey/Level Asset")]
    public class LevelAsset : ScriptableObject
    {
        [SerializeField] private LevelData _level = new LevelData();

        public LevelData Level => _level;

        /// <summary>
        /// 整体替换关卡内容。仅供编辑器工具和导入流程使用，运行时不要修改资源本身。
        /// </summary>
        public void SetLevel(LevelData level)
        {
            if (level == null)
            {
                Debug.LogError($"[{nameof(LevelAsset)}] SetLevel 传入了 null，已忽略：{name}", this);
                return;
            }

            _level = level;
        }
    }
}
