using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Data;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 界面换皮工具共用的样式方法：Honeti 圆角素材、主题着色组件、字体。
    /// </summary>
    internal static class UiStyleKit
    {
        internal const string UndoName = "Restyle UI";
        internal const float Padding = 24f;
        internal const float HeaderHeight = 96f;

        private const string PanelSpritePath = ProjectPaths.ThirdParty + "/Honeti/UltimateFlatGUI/ThemeCommon/Textures/Panels/UniversalPanel20.png";
        private const string FontSearch = "t:TMP_FontAsset Poppins";

        /// <summary>
        /// 一次换皮共用的素材：编辑器预览用的主题、面板素材、字体（可为空）。
        /// </summary>
        internal sealed class Context
        {
            public readonly UiTheme Preview;
            public readonly Sprite PanelSprite;
            public readonly TMP_FontAsset Font;

            public Context(UiTheme preview)
            {
                Preview = preview;
                PanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
                if (PanelSprite == null)
                {
                    Debug.LogWarning($"[{nameof(UiStyleKit)}] 找不到面板素材 {PanelSpritePath}，改用纯色方块");
                }

                // 字体是可选的：项目里有 Poppins 的 TMP 字体资源就用，没有就保留默认字体
                string[] guids = AssetDatabase.FindAssets(FontSearch);
                Font = guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
            }
        }

        internal static UiThemeScope EnsureScope(Component panel, UiTheme initialTheme)
        {
            var scope = panel.GetComponent<UiThemeScope>();
            if (scope == null)
            {
                scope = Undo.AddComponent<UiThemeScope>(panel.gameObject);
            }

            var serialized = new SerializedObject(scope);
            SerializedProperty initial = serialized.FindProperty("_initialTheme");
            if (initial.objectReferenceValue == null)
            {
                initial.objectReferenceValue = initialTheme;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return scope;
        }

        /// <summary>
        /// 给图形挂上（或更新）主题着色。<paramref name="scope"/> 为空时运行时自动找父级作用域，用于预制体。
        /// </summary>
        internal static ThemedGraphic Theme(Graphic graphic, UiColorRole role, UiThemeScope scope, Context context)
        {
            var themed = graphic.GetComponent<ThemedGraphic>();
            if (themed == null)
            {
                themed = Undo.AddComponent<ThemedGraphic>(graphic.gameObject);
            }

            var serialized = new SerializedObject(themed);
            serialized.FindProperty("_role").enumValueIndex = (int)role;
            serialized.FindProperty("_scope").objectReferenceValue = scope;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // 运行时才会着色，这里先按预览主题上色，编辑器里看到的就接近实际效果
            Undo.RecordObject(graphic, UndoName);
            graphic.color = context.Preview.GetColor(role);
            return themed;
        }

        internal static void StyleText(TMP_Text text, UiColorRole role, UiThemeScope scope, Context context, bool isBold)
        {
            Theme(text, role, scope, context);
            if (context.Font != null)
            {
                text.font = context.Font;
            }

            text.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
        }

        internal static void StyleButton(Button button, UiColorRole role, UiThemeScope scope, Context context)
        {
            if (button.targetGraphic is Image image)
            {
                SetSliced(image, context.PanelSprite);
                Theme(image, role, scope, context);
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                StyleText(label, UiColorRole.HeaderText, scope, context, true);
            }
        }

        /// <summary>
        /// 新建一块圆角底板。以 <paramref name="anchor"/> 同时作为锚点和轴心放置。
        /// </summary>
        internal static RectTransform CreateCard(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size,
            UiColorRole role, UiThemeScope scope, Context context)
        {
            RectTransform card = GameUiBuilder.CreateRect(name, parent);
            Undo.RegisterCreatedObjectUndo(card.gameObject, UndoName);
            GameUiBuilder.Place(card, anchor, position, size);
            Image image = card.gameObject.AddComponent<Image>();
            SetSliced(image, context.PanelSprite);

            // 底板只做背景，不拦截点击
            image.raycastTarget = false;
            Theme(image, role, scope, context);
            return card;
        }

        /// <summary>
        /// 在卡片顶部新建标题栏，左右和顶部各留半个边距。
        /// </summary>
        internal static RectTransform CreateHeaderBar(RectTransform card, UiThemeScope scope, Context context)
        {
            RectTransform header = CreateCard(card, "Header", new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, UiColorRole.Header, scope, context);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = Vector2.one;
            header.anchoredPosition = new Vector2(0f, -Padding * 0.5f);
            header.sizeDelta = new Vector2(-Padding, HeaderHeight);
            return header;
        }

        /// <summary>
        /// 把标题文字放进标题栏，左对齐。
        /// </summary>
        internal static void PutTitleInHeader(TMP_Text title, RectTransform header, UiThemeScope scope, Context context)
        {
            Undo.SetTransformParent(title.transform, header, UndoName);
            GameUiBuilder.Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(32f, 0f);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.fontSize = 44f;
            StyleText(title, UiColorRole.HeaderText, scope, context, true);
        }

        /// <summary>
        /// 面板根节点下直接挂着的文字（不含按钮里的标签）。
        /// </summary>
        internal static List<TMP_Text> GetLooseTexts(Transform root)
        {
            var texts = new List<TMP_Text>();
            for (int i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i).TryGetComponent(out TMP_Text text))
                {
                    texts.Add(text);
                }
            }

            return texts;
        }

        internal static void SetSliced(Image image, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Undo.RecordObject(image, UndoName);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }
    }
}
