using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 修复换成 SDF 字体后被放大的字号：旧的取整规则把字号取成字体取样尺寸的倍数，SDF 字体取样尺寸很大（90），
    /// 字号因此变成 180。这里把使用 SDF 字体、字号明显超出文字框的文字改回设计字号。可以重复运行。
    /// </summary>
    public static class FontSizeRepair
    {
        private const string MenuPath = "Worldyssey/UI/Repair SDF Font Sizes";

        // 字号超过文字框高度的这个倍数，就认为是被错误放大的
        private const float OversizeRatio = 1.2f;

        // 没有设计字号时，按文字框高度估算
        private const float HeightToSize = 0.6f;
        private const float MinSize = 16f;
        private const float MaxSize = 40f;

        // 界面生成工具里的设计字号（按物体名）
        private static readonly Dictionary<string, float> DesignSizes = new Dictionary<string, float>
        {
            { "Steps", 28f },
            { "CoordinateText", 22f },
            { "EmptyText", 32f },
            { "Best", 28f },
            { "WorldName", 24f },
            { "Progress", 28f },
            { "Message", 30f },
            { "Status", 20f },
        };

        [MenuItem(MenuPath)]
        public static void Repair()
        {
            int sceneCount = RepairTexts(Object.FindObjectsOfType<TMP_Text>(true), true);
            int prefabCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ProjectPaths.UiPrefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                int count = RepairTexts(contents.GetComponentsInChildren<TMP_Text>(true), false);
                if (count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }

                PrefabUtility.UnloadPrefabContents(contents);
                prefabCount += count;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[{nameof(FontSizeRepair)}] 已恢复 SDF 字体字号：场景 {sceneCount} 处、预制体 {prefabCount} 处，记得保存场景");
        }

        private static int RepairTexts(TMP_Text[] texts, bool isSceneObject)
        {
            int fixedCount = 0;
            foreach (TMP_Text text in texts)
            {
                if (!(text.font is TMP_FontAsset font) || UiStyleKit.IsPixelFont(font))
                {
                    continue;
                }

                float height = text.rectTransform.rect.height;
                if (height <= 0f || text.fontSize <= height * OversizeRatio)
                {
                    continue;
                }

                if (isSceneObject)
                {
                    Undo.RecordObject(text, UiStyleKit.UndoName);
                }

                text.fontSize = DesignSizes.TryGetValue(text.name, out float size)
                    ? size
                    : Mathf.Clamp(Mathf.Round(height * HeightToSize), MinSize, MaxSize);
                fixedCount++;
            }

            return fixedCount;
        }
    }
}
