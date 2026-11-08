using System.IO;
using UnityEditor;
using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 测试用的存档菜单：删除通关记录、打开存档目录。只删 save.json，设置和自建关卡保留。
    /// </summary>
    public static class SaveDebugMenu
    {
        private const string DeleteMenuPath = "Worldyssey/Debug/Delete Save";
        private const string OpenMenuPath = "Worldyssey/Debug/Open Save Folder";

        [MenuItem(DeleteMenuPath)]
        public static void DeleteSave()
        {
            // 运行中删除会被 GameFlow 在下次通关时用内存里的进度重新写回，必须先退出 Play
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning($"[{nameof(SaveDebugMenu)}] 请先退出 Play 模式再删除存档");
                return;
            }

            string path = Path.Combine(Application.persistentDataPath, SaveService.SaveFileName);
            if (!File.Exists(path))
            {
                Debug.Log($"[{nameof(SaveDebugMenu)}] 没有存档可删：{path}");
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete Save", $"删除全部通关记录？\n{path}\n\n设置和自建关卡不受影响。", "删除", "取消"))
            {
                return;
            }

            try
            {
                File.Delete(path);
                Debug.Log($"[{nameof(SaveDebugMenu)}] 已删除存档：{path}");
            }
            catch (IOException exception)
            {
                Debug.LogError($"[{nameof(SaveDebugMenu)}] 删除存档失败：{exception.Message}（{path}）");
            }
        }

        [MenuItem(OpenMenuPath)]
        public static void OpenFolder()
        {
            string folder = Application.persistentDataPath;
            Directory.CreateDirectory(folder);
            EditorUtility.RevealInFinder(folder);
        }
    }
}
