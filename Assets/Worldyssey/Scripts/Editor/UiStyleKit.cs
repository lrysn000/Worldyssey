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
    /// 界面换皮工具共用的样式方法：像素风素材与字体（来自 <see cref="UiSkin"/>）、主题着色组件。
    /// </summary>
    internal static class UiStyleKit
    {
        internal const string UndoName = "Restyle UI";
        internal const float Padding = 24f;
        internal const float HeaderHeight = 96f;

        // 自制像素图标与两套像素素材一致：16 像素 = 1 单位
        private const float IconPixelsPerUnit = 16f;

        internal const string WorldIconFolder = ProjectPaths.Textures + "/UI/WorldIcons";

        // 与 WorldTheme 的数值顺序一致：春、夏、秋、冬
        private static readonly string[] WorldIconNames = { "WorldIcon_Spring", "WorldIcon_Summer", "WorldIcon_Autumn", "WorldIcon_Winter" };

        // 画布的 Reference Pixels Per Unit（Unity 默认 100），九宫格边框的显示大小以它为基准
        private const float CanvasReferencePixelsPerUnit = 100f;

        // 当前换皮使用的像素比例，Context 创建时更新；SetSliced 按它计算边框大小
        private static int s_pixelScale = 4;

        /// <summary>
        /// 一次换皮共用的素材：编辑器预览用的主题、界面套件（素材、字体，可为空时退回默认）。
        /// </summary>
        internal sealed class Context
        {
            public readonly UiTheme Preview;
            public readonly UiSkin Skin;
            public readonly Sprite PanelSprite;
            public readonly Sprite ButtonSprite;
            public readonly TMP_FontAsset Font;
            public readonly TMP_FontAsset TitleFont;

            public Context(UiTheme preview)
            {
                Preview = preview;
                string[] guids = AssetDatabase.FindAssets("t:" + nameof(UiSkin));
                Skin = guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<UiSkin>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
                if (Skin == null)
                {
                    Debug.LogWarning($"[{nameof(UiStyleKit)}] 项目中没有 {nameof(UiSkin)}，先运行 Worldyssey/UI/Apply Pixel UI Skin 生成；暂时使用纯色方块和默认字体");
                    return;
                }

                PanelSprite = Skin.Panel;
                ButtonSprite = Skin.Button != null ? Skin.Button : Skin.Panel;
                Font = Skin.BodyFont;
                TitleFont = Skin.TitleFont != null ? Skin.TitleFont : Skin.BodyFont;
                s_pixelScale = Skin.UiPixelScale;
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

        /// <summary>
        /// 文字着色并换字体。像素字体不能用合成的粗体（笔画会糊），所以“加粗”改用标题字体。
        /// </summary>
        internal static void StyleText(TMP_Text text, UiColorRole role, UiThemeScope scope, Context context, bool isBold)
        {
            Theme(text, role, scope, context);
            ApplyFont(text, context, isBold);
        }

        internal static void ApplyFont(TMP_Text text, Context context, bool isTitle)
        {
            TMP_FontAsset font = isTitle ? context.TitleFont : context.Font;
            if (font != null)
            {
                text.font = font;
                text.fontStyle = FontStyles.Normal;
            }
            else
            {
                text.fontStyle = isTitle ? FontStyles.Bold : FontStyles.Normal;
            }

            // 像素字体没有省略号“…”，TMP 会去默认的 SDF 字体里借这个字，运行时生成的备用材质在退出游戏时被销毁，
            // 文字随后刷新就会报 MissingReferenceException（而且省略号的字体也对不上），所以统一改为直接截断
            if (font != null && text.overflowMode == TextOverflowModes.Ellipsis)
            {
                text.overflowMode = TextOverflowModes.Truncate;
            }

            if (font != null && context.Skin != null && context.Skin.IsFontSizeSnapped)
            {
                SnapFontSize(text, font, context.Skin.MinTextScale);
            }
        }

        /// <summary>
        /// 像素字体只有在设计尺寸（生成字体时的取样尺寸）的整数倍下，每个字形像素才正好对应整数个屏幕像素，笔画粗细一致。
        /// 自动缩放会算出任意字号，所以关掉，改用它原来的上限并取整；单行文字放不下时直接截断。
        /// </summary>
        private static void SnapFontSize(TMP_Text text, TMP_FontAsset font, int minScale)
        {
            float native = font.faceInfo.pointSize;
            if (native <= 0f)
            {
                return;
            }

            float size = text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
            if (text.enableAutoSizing)
            {
                text.enableAutoSizing = false;
                if (!text.enableWordWrapping)
                {
                    text.overflowMode = TextOverflowModes.Truncate;
                }
            }

            int multiple = Mathf.Max(minScale, Mathf.RoundToInt(size / native));
            text.fontSize = multiple * native;
        }

        internal static void StyleButton(Button button, UiColorRole role, UiThemeScope scope, Context context)
        {
            if (button.targetGraphic is Image image)
            {
                SetSliced(image, context.ButtonSprite);
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

        /// <summary>
        /// 换成九宫格素材，并让素材的 1 个像素显示为 <paramref name="pixelScale"/> 个界面单位（0 表示用套件的默认比例），
        /// 所有面板的边框粗细一致。
        /// </summary>
        internal static void SetSliced(Image image, Sprite sprite, int pixelScale = 0)
        {
            if (sprite == null)
            {
                return;
            }

            Undo.RecordObject(image, UndoName);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = PixelMultiplier(sprite, pixelScale > 0 ? pixelScale : s_pixelScale);
        }

        internal static float PixelMultiplier(Sprite sprite, int pixelScale)
        {
            return CanvasReferencePixelsPerUnit / (sprite.pixelsPerUnit * Mathf.Max(1, pixelScale));
        }

        /// <summary>
        /// 读取四张世界图标（春、夏、秋、冬），缺的位置为 null。
        /// </summary>
        internal static Sprite[] LoadWorldIcons()
        {
            var icons = new Sprite[WorldIconNames.Length];
            for (int i = 0; i < WorldIconNames.Length; i++)
            {
                icons[i] = LoadIconSprite($"{WorldIconFolder}/{WorldIconNames[i]}.png");
            }

            return icons;
        }

        /// <summary>
        /// 读取一张白色剪影图标，并确保它以 Sprite 导入（透明背景、无 mipmap）。找不到时返回 null 并警告。
        /// </summary>
        internal static Sprite LoadIconSprite(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                Debug.LogWarning($"[{nameof(UiStyleKit)}] 找不到图标 {path}");
                return null;
            }

            // 像素图标：点过滤、不压缩，放大时边缘保持锐利
            bool isReady = importer.textureType == TextureImporterType.Sprite && !importer.mipmapEnabled && importer.alphaIsTransparency
                && importer.filterMode == FilterMode.Point && importer.textureCompression == TextureImporterCompression.Uncompressed
                && Mathf.Approximately(importer.spritePixelsPerUnit, IconPixelsPerUnit);
            if (!isReady)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = IconPixelsPerUnit;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
