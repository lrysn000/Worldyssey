using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 关卡编辑器网格格子预制体（P_EditorGridCell）的生成与换皮。
    /// </summary>
    internal static class EditorGridCellPrefabBuilder
    {
        private const string CellPrefabPath = ProjectPaths.UiPrefabs + "/P_EditorGridCell.prefab";

        // 网格格子的圆角缩小倍数：格子只有几十像素，用面板素材原本的圆角会太圆
        private const float CellCornerMultiplier = 4f;

        /// <summary>
        /// 网格格子预制体：没有就新建；已有就只把外框、地形、实体换成小圆角素材，文字和结构不动。
        /// </summary>
        internal static EditorGridCell GetOrUpdate(UiStyleKit.Context context)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath) == null)
            {
                CreateCellPrefab();
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(CellPrefabPath);
            var cell = contents.GetComponent<EditorGridCell>();
            var serialized = new SerializedObject(cell);
            string[] imageFields = { "_frame", "_terrain", "_entity" };
            for (int i = 0; i < imageFields.Length; i++)
            {
                var image = UiThemeSceneSetup.GetReference<Image>(serialized, imageFields[i]);
                if (image != null && context.PanelSprite != null)
                {
                    image.sprite = context.PanelSprite;
                    image.type = Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = CellCornerMultiplier;
                }
            }

            var label = UiThemeSceneSetup.GetReference<TMP_Text>(serialized, "_label");
            if (label != null && context.Font != null)
            {
                label.font = context.Font;
                label.fontStyle = FontStyles.Bold;
            }

            PrefabUtility.SaveAsPrefabAsset(contents, CellPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            return AssetDatabase.LoadAssetAtPath<EditorGridCell>(CellPrefabPath);
        }

        private static void CreateCellPrefab()
        {
            AssetFolderUtility.EnsureFolder(ProjectPaths.UiPrefabs);
            var root = new GameObject("P_EditorGridCell", typeof(RectTransform));
            var frame = root.AddComponent<Image>();
            Image terrain = CreateInsetImage(root.transform, "Terrain", 3f);
            Image entity = CreateInsetImage(root.transform, "Entity", 9f);
            RectTransform labelRect = GameUiBuilder.CreateRect("Label", root.transform);
            GameUiBuilder.Stretch(labelRect);
            var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.enableAutoSizing = true;
            label.fontSizeMin = 8f;
            label.fontSizeMax = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            var cell = root.AddComponent<EditorGridCell>();
            GameUiBuilder.SetReference(cell, "_frame", frame);
            GameUiBuilder.SetReference(cell, "_terrain", terrain);
            GameUiBuilder.SetReference(cell, "_entity", entity);
            GameUiBuilder.SetReference(cell, "_label", label);
            PrefabUtility.SaveAsPrefabAsset(root, CellPrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>
        /// 四边各缩进 <paramref name="inset"/> 像素的图像。只有最外层的外框接收鼠标事件。
        /// </summary>
        private static Image CreateInsetImage(Transform parent, string name, float inset)
        {
            RectTransform rect = GameUiBuilder.CreateRect(name, parent);
            GameUiBuilder.Stretch(rect);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
    }
}
