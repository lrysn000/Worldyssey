using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 持有运行时生成的网格，物体销毁时一并销毁网格。运行时 new 出来的 Mesh 不会随物体自动释放，
    /// 反复进出关卡会造成内存泄漏。
    /// </summary>
    public class RuntimeMeshOwner : MonoBehaviour
    {
        private Mesh _mesh;

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        public void Initialize(Mesh mesh)
        {
            _mesh = mesh;
        }
    }
}
