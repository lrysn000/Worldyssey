using UnityEngine;

namespace Worldyssey.UI
{
    /// <summary>
    /// 界面面板基类：通过激活和隐藏自身 GameObject 来显示和关闭。
    /// </summary>
    public abstract class UiPanel : MonoBehaviour
    {
        public bool IsVisible => gameObject.activeSelf;

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        /// <summary>
        /// 检查必需引用，缺失时报错并写明对象名。返回是否齐全。
        /// </summary>
        protected bool CheckReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{GetType().Name}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
