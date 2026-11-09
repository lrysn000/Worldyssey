using UnityEditor;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 编辑器工具共用的资源目录辅助方法。
    /// </summary>
    public static class AssetFolderUtility
    {
        /// <summary>
        /// 逐级创建 Assets 下的目录，已存在的跳过。
        /// </summary>
        public static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int lastSlash = folderPath.LastIndexOf('/');
            string parent = folderPath.Substring(0, lastSlash);
            string child = folderPath.Substring(lastSlash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
