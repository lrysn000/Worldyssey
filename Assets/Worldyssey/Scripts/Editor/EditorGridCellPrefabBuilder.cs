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
            // 实体图像运行时换成图标，这里只给外框和地形换圆角素材
            string[] imageFields = { "_frame", "_terrain" };
            for (int i = 0; i < imageFields.Length; i++)
            {
                var image = UiThemeSceneSetup.GetReference<Image>(serialized, imageFields[i]);
                if (image != null && context.PanelSprite != null)
                {
                    // 格子只有几十像素，用小一号的像素比例，否则边框会占满格子
                    UiStyleKit.SetSliced(image, context.PanelSprite, context.Skin.SmallPixelScale);
                }
            }

            // 地形图标：旧版格子没有，补一层放在地形和实体之间；按比例缩进，格子大小变化时跟着缩放
            if (UiThemeSceneSetup.GetReference<Image>(serialized, "_terrainIcon") == null)
            {
                RectTransform iconRect = GameUiBuilder.CreateRect("TerrainIcon", contents.transform);
                iconRect.anchorMin = new Vector2(0.2f, 0.2f);
                iconRect.anchorMax = new Vector2(0.8f, 0.8f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                var terrain = UiThemeSceneSetup.GetReference<Image>(serialized, "_terrain");
                iconRect.SetSiblingIndex(terrain != null ? terrain.transform.GetSiblingIndex() + 1 : 0);
                Image terrainIcon = iconRect.gameObject.AddComponent<Image>();
                terrainIcon.preserveAspect = true;
                terrainIcon.raycastTarget = false;
                terrainIcon.enabled = false;
                serialized.FindProperty("_terrainIcon").objectReferenceValue = terrainIcon;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            // 其他世界的叠影：角上的小图标，放在实体之上、文字之下，有箱子时也看得到
            if (UiThemeSceneSetup.GetReference<Image>(serialized, "_ghost") == null)
            {
                RectTransform ghostRect = GameUiBuilder.CreateRect("Ghost", contents.transform);
                ghostRect.anchorMin = new Vector2(0.04f, 0.04f);
                ghostRect.anchorMax = new Vector2(0.42f, 0.42f);
                ghostRect.offsetMin = Vector2.zero;
                ghostRect.offsetMax = Vector2.zero;
                var labelText = UiThemeSceneSetup.GetReference<TMP_Text>(serialized, "_label");
                ghostRect.SetSiblingIndex(labelText != null ? labelText.transform.GetSiblingIndex() : contents.transform.childCount - 1);
                Image ghost = ghostRect.gameObject.AddComponent<Image>();
                ghost.preserveAspect = true;
                ghost.raycastTarget = false;
                ghost.enabled = false;
                serialized.FindProperty("_ghost").objectReferenceValue = ghost;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            // 配对符号：右下角（叠影在左下角），替代原来的配对编号文字
            if (UiThemeSceneSetup.GetReference<Image>(serialized, "_pairMark") == null)
            {
                RectTransform markRect = GameUiBuilder.CreateRect("PairMark", contents.transform);
                markRect.anchorMin = new Vector2(0.58f, 0.04f);
                markRect.anchorMax = new Vector2(0.96f, 0.42f);
                markRect.offsetMin = Vector2.zero;
                markRect.offsetMax = Vector2.zero;
                var markLabel = UiThemeSceneSetup.GetReference<TMP_Text>(serialized, "_label");
                markRect.SetSiblingIndex(markLabel != null ? markLabel.transform.GetSiblingIndex() : contents.transform.childCount - 1);
                Image mark = markRect.gameObject.AddComponent<Image>();
                mark.preserveAspect = true;
                mark.raycastTarget = false;
                mark.enabled = false;
                serialized.FindProperty("_pairMark").objectReferenceValue = mark;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var entity = UiThemeSceneSetup.GetReference<Image>(serialized, "_entity");
            if (entity != null)
            {
                entity.sprite = null;
                entity.type = Image.Type.Simple;
                entity.preserveAspect = true;
            }

            // 文字只剩传送箱子的配对编号，放在右下角做角标，不挡图标
            var label = UiThemeSceneSetup.GetReference<TMP_Text>(serialized, "_label");
            if (label != null)
            {
                label.alignment = TextAlignmentOptions.BottomRight;
                label.fontSizeMax = 18f;
                label.margin = new Vector4(0f, 0f, 4f, 2f);
                UiStyleKit.ApplyFont(label, context, true);
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
