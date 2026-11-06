using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Data;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 给界面加装饰层（素材来自 <see cref="UiSkin"/>）：主要面板加 底纹 + 双层外框 + 角饰，对话框标题栏换成标题条，
    /// 按钮加按下下沉效果，关卡卡片和编辑器画笔的选中状态加四角括号。装饰层放在面板最底下（角饰在最上面），布局和引用不变。
    /// 已有的装饰层会先删掉再生成，可以重复运行。
    /// </summary>
    internal static class PixelUiDecorator
    {
        private const string PatternName = "Deco_Pattern";
        private const string FrameName = "Deco_Frame";
        private const string OrnamentPrefix = "Deco_Ornament_";
        private const string CardSelectionName = "SelectionFrame";

        // 完整装饰（带角饰）的面板，以及只加外框和底纹的大面板
        private static readonly HashSet<string> FullPanels = new HashSet<string>
        {
            "InfoCard", "WorldIndicator", "DetailCard", "Card", "ToolsCard", "SideCard", "CenterCard",
        };

        private static readonly HashSet<string> LightPanels = new HashSet<string> { "CardsArea", "TopBar" };

        /// <summary>装饰当前场景，返回装饰的面板数。</summary>
        internal static int DecorateScene(UiStyleKit.Context context, UiThemeSet themeSet)
        {
            int panels = 0;
            foreach (RectTransform rect in Object.FindObjectsOfType<RectTransform>(true))
            {
                bool isFull = FullPanels.Contains(rect.name);
                if ((isFull || LightPanels.Contains(rect.name)) && rect.TryGetComponent(out ThemedGraphic _))
                {
                    DecoratePanel(rect, isFull, context, themeSet);
                    panels++;
                }
                else if (rect.name == "Header" && rect.parent != null && rect.parent.name == "Card" && rect.TryGetComponent(out Image header))
                {
                    UiStyleKit.SetSliced(header, context.Skin.TitleBar);
                }
            }

            foreach (Button button in Object.FindObjectsOfType<Button>(true))
            {
                AddPress(button, context, true);
            }

            foreach (EditorToolbar toolbar in Object.FindObjectsOfType<EditorToolbar>(true))
            {
                foreach (Button tool in toolbar.GetComponentsInChildren<Button>(true))
                {
                    AddSelectCorners(tool.transform, EditorToolbar.SelectCornersName, context, 1, false);
                }
            }

            return panels;
        }

        /// <summary>界面预制体：按钮加按下效果，关卡卡片的选中描边换成四角括号。</summary>
        internal static void DecoratePrefab(GameObject root, UiStyleKit.Context context)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                AddPress(button, context, false);
            }

            Transform selection = root.transform.Find(CardSelectionName);
            if (selection != null && root.GetComponent<LevelCard>() != null)
            {
                // 原来是一整圈描边图，换成四个角；选中时由 LevelCard 开关这个物体，运行时代码不用改
                if (selection.TryGetComponent(out ThemedGraphic oldTheme))
                {
                    Object.DestroyImmediate(oldTheme);
                }

                if (selection.TryGetComponent(out Image oldImage))
                {
                    Object.DestroyImmediate(oldImage);
                }

                RebuildCorners((RectTransform)selection, context, 0);
            }
        }

        private static void DecoratePanel(RectTransform panel, bool hasOrnaments, UiStyleKit.Context context, UiThemeSet themeSet)
        {
            RemoveChildren(panel, child => child.StartsWith("Deco_"));
            UiSkin skin = context.Skin;
            int scale = skin.UiPixelScale;
            var fill = panel.GetComponent<Image>();
            UiStyleKit.SetSliced(fill, context.PanelSprite);
            UiThemeScope scope = panel.GetComponentInParent<UiThemeScope>(true);

            // 外框和底纹用比底色深的颜色：底色本身是标题色的（顶栏）就用正文色
            UiColorRole frameRole = panel.GetComponent<ThemedGraphic>().Role == UiColorRole.Header ? UiColorRole.Text : UiColorRole.Header;

            // 底纹是固定的淡浅灰，不随主题变色，避免干扰文字；深色的顶栏不加底纹
            bool hasPattern = skin.HasPattern && frameRole != UiColorRole.Text;
            if (hasPattern)
            {
                Image pattern = CreateLayer(panel, PatternName, 4f * scale);
                pattern.sprite = skin.Pattern;
                pattern.type = Image.Type.Tiled;
                pattern.pixelsPerUnitMultiplier = UiStyleKit.PixelMultiplier(skin.Pattern, scale);
                pattern.color = skin.PatternColor;
                pattern.transform.SetSiblingIndex(0);
            }

            Image frame = CreateLayer(panel, FrameName, 0f);
            UiStyleKit.SetSliced(frame, skin.Frame);
            frame.fillCenter = false;
            UiStyleKit.Theme(frame, frameRole, scope, context);
            frame.transform.SetSiblingIndex(hasPattern ? 1 : 0);

            if (hasOrnaments)
            {
                // 上面两个角：季节角饰（中性时为铆钉）；下面两个角：只有中性时显示铆钉
                AddOrnament(panel, "TL", false, false, skin.SeasonOrnaments, skin, themeSet, scope);
                AddOrnament(panel, "TR", true, false, skin.SeasonOrnaments, skin, themeSet, scope);
                AddOrnament(panel, "BL", false, true, new Sprite[4], skin, themeSet, scope);
                AddOrnament(panel, "BR", true, true, new Sprite[4], skin, themeSet, scope);
            }
        }

        private static void AddOrnament(RectTransform panel, string corner, bool isRight, bool isBottom, Sprite[] seasonSprites,
            UiSkin skin, UiThemeSet themeSet, UiThemeScope scope)
        {
            RectTransform rect = CreateCornerRect(panel, OrnamentPrefix + corner, isRight, isBottom);
            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            var ornament = rect.gameObject.AddComponent<SeasonOrnament>();
            var serialized = new SerializedObject(ornament);
            serialized.FindProperty("_scope").objectReferenceValue = scope;
            serialized.FindProperty("_themeSet").objectReferenceValue = themeSet;
            serialized.FindProperty("_neutralSprite").objectReferenceValue = skin.NeutralOrnament;
            serialized.FindProperty("_pixelScale").intValue = skin.UiPixelScale;
            SerializedProperty sprites = serialized.FindProperty("_seasonSprites");
            sprites.arraySize = seasonSprites.Length;
            for (int i = 0; i < seasonSprites.Length; i++)
            {
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = seasonSprites[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // 编辑器里先按中性的样子显示，运行时由组件按主题切换
            image.sprite = skin.NeutralOrnament;
            rect.SetAsLastSibling();
        }

        private static void AddPress(Button button, UiStyleKit.Context context, bool isSceneObject)
        {
            if (!(button.targetGraphic is Image image) || image.sprite != context.ButtonSprite || context.Skin.ButtonPressed == null)
            {
                return;
            }

            if (!button.TryGetComponent(out PixelButtonPress press))
            {
                press = isSceneObject ? Undo.AddComponent<PixelButtonPress>(button.gameObject) : button.gameObject.AddComponent<PixelButtonPress>();
            }

            var serialized = new SerializedObject(press);
            serialized.FindProperty("_image").objectReferenceValue = image;
            serialized.FindProperty("_normalSprite").objectReferenceValue = context.ButtonSprite;
            serialized.FindProperty("_pressedSprite").objectReferenceValue = context.Skin.ButtonPressed;
            serialized.FindProperty("_contentOffset").floatValue = context.Skin.UiPixelScale;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>在 <paramref name="parent"/> 下建（或重建）一组四角括号，向外伸出 <paramref name="outsetPixels"/> 个素材像素，默认隐藏。</summary>
        private static void AddSelectCorners(Transform parent, string name, UiStyleKit.Context context, int outsetPixels, bool isActive)
        {
            RemoveChildren(parent, child => child == name);
            RectTransform container = GameUiBuilder.CreateRect(name, parent);
            GameUiBuilder.Stretch(container);
            RebuildCorners(container, context, outsetPixels);
            container.gameObject.SetActive(isActive);
        }

        private static void RebuildCorners(RectTransform container, UiStyleKit.Context context, int outsetPixels)
        {
            RemoveChildren(container, child => child.StartsWith("Corner_"));
            UiSkin skin = context.Skin;
            int scale = skin.UiPixelScale;
            float outset = outsetPixels * scale;
            UiThemeScope scope = container.GetComponentInParent<UiThemeScope>(true);
            bool[] rights = { false, true, false, true };
            bool[] bottoms = { false, false, true, true };
            for (int i = 0; i < 4; i++)
            {
                RectTransform rect = CreateCornerRect(container, "Corner_" + i, rights[i], bottoms[i]);
                rect.sizeDelta = skin.SelectCorner.rect.size * scale;
                rect.anchoredPosition = new Vector2(rights[i] ? outset : -outset, bottoms[i] ? -outset : outset);
                Image image = rect.gameObject.AddComponent<Image>();
                image.sprite = skin.SelectCorner;
                image.raycastTarget = false;
                UiStyleKit.Theme(image, UiColorRole.Accent, scope, context);
            }
        }

        /// <summary>
        /// 角上的小图：轴心在素材左上角，右侧和下侧用负缩放镜像，这样只需要一张左上角的素材，并且总是从角落向内延伸。
        /// </summary>
        private static RectTransform CreateCornerRect(Transform parent, string name, bool isRight, bool isBottom)
        {
            RectTransform rect = GameUiBuilder.CreateRect(name, parent);
            var anchor = new Vector2(isRight ? 1f : 0f, isBottom ? 0f : 1f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.localScale = new Vector3(isRight ? -1f : 1f, isBottom ? -1f : 1f, 1f);
            return rect;
        }

        private static Image CreateLayer(RectTransform parent, string name, float inset)
        {
            RectTransform rect = GameUiBuilder.CreateRect(name, parent);
            GameUiBuilder.Stretch(rect);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void SetKeepAlpha(ThemedGraphic themed)
        {
            var serialized = new SerializedObject(themed);
            serialized.FindProperty("_keepAlpha").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveChildren(Transform parent, System.Predicate<string> isMatch)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (!isMatch(child.name))
                {
                    continue;
                }

                // 预制体内容在预览场景里，不进撤销记录
                if (EditorSceneManager.IsPreviewScene(child.gameObject.scene))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
                else
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }
    }
}
