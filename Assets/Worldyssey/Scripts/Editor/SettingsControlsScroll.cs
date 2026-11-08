using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Data;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 设置面板的按键说明加上关卡编辑器部分后放不下，把它放进一个可用滚轮滚动的区域，位置和大小不变，
    /// 右侧加一条可拖动的滚动条（内容放得下时自动隐藏）。可以重复运行。
    /// </summary>
    public static class SettingsControlsScroll
    {
        private const string MenuPath = "Worldyssey/Setup/Make Settings Controls Scrollable";
        private const string ScrollName = "ControlsScroll";
        private const string ScrollbarName = "Scrollbar";
        private const float ScrollSensitivity = 30f;

        // 滚动条宽度和与文字的间距，按界面像素比例 4 取整，与像素边框粗细一致
        private const float ScrollbarWidth = 16f;
        private const float ScrollbarGap = 12f;

        [MenuItem(MenuPath)]
        public static void Upgrade()
        {
            var panel = Object.FindObjectOfType<SettingsPanel>(true);
            var text = panel != null ? UiThemeSceneSetup.GetReference<TMP_Text>(new SerializedObject(panel), "_controlsText") : null;
            if (text == null)
            {
                Debug.LogError($"[{nameof(SettingsControlsScroll)}] 找不到设置面板的按键说明文字，未做任何修改");
                return;
            }

            if (text.transform.parent != null && text.transform.parent.name == ScrollName)
            {
                AddScrollbar(panel, text.transform.parent.GetComponent<ScrollRect>(), text.rectTransform);
                return;
            }

            RectTransform textRect = text.rectTransform;
            RectTransform viewport = GameUiBuilder.CreateRect(ScrollName, textRect.parent);
            Undo.RegisterCreatedObjectUndo(viewport.gameObject, UiStyleKit.UndoName);
            viewport.anchorMin = textRect.anchorMin;
            viewport.anchorMax = textRect.anchorMax;
            viewport.pivot = textRect.pivot;
            viewport.anchoredPosition = textRect.anchoredPosition;
            viewport.sizeDelta = textRect.sizeDelta;
            viewport.SetSiblingIndex(textRect.GetSiblingIndex());
            viewport.gameObject.AddComponent<RectMask2D>();

            // 透明底图只用来接收滚轮
            Image hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;

            Undo.SetTransformParent(textRect, viewport, UiStyleKit.UndoName);
            Undo.RecordObject(textRect, UiStyleKit.UndoName);
            Undo.RecordObject(text, UiStyleKit.UndoName);
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            var fitter = Undo.AddComponent<ContentSizeFitter>(text.gameObject);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = ScrollSensitivity;
            scroll.viewport = viewport;
            scroll.content = textRect;
            AddScrollbar(panel, scroll, textRect);
        }

        private static void AddScrollbar(SettingsPanel panel, ScrollRect scroll, RectTransform textRect)
        {
            var themeSet = GameUiBuilder.FindAsset<UiThemeSet>(null);
            if (scroll == null || themeSet == null)
            {
                Debug.LogError($"[{nameof(SettingsControlsScroll)}] 找不到滚动区域或 UiThemeSet，未添加滚动条", panel);
                return;
            }

            if (scroll.transform.Find(ScrollbarName) != null)
            {
                Debug.Log($"[{nameof(SettingsControlsScroll)}] 按键说明已有滚动条，无需处理", scroll);
                return;
            }

            var context = new UiStyleKit.Context(themeSet.NeutralTheme);
            UiThemeScope scope = panel.GetComponent<UiThemeScope>();

            // 轨道贴在滚动区域右边，上下铺满
            RectTransform track = GameUiBuilder.CreateRect(ScrollbarName, scroll.transform);
            Undo.RegisterCreatedObjectUndo(track.gameObject, UiStyleKit.UndoName);
            track.anchorMin = new Vector2(1f, 0f);
            track.anchorMax = Vector2.one;
            track.pivot = new Vector2(1f, 0.5f);
            track.anchoredPosition = Vector2.zero;
            track.sizeDelta = new Vector2(ScrollbarWidth, 0f);
            Image trackImage = track.gameObject.AddComponent<Image>();
            UiStyleKit.Theme(trackImage, UiColorRole.InnerFrame, scope, context);

            RectTransform slidingArea = GameUiBuilder.CreateRect("SlidingArea", track);
            GameUiBuilder.Stretch(slidingArea);
            RectTransform handle = GameUiBuilder.CreateRect("Handle", slidingArea);
            GameUiBuilder.Stretch(handle);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            UiStyleKit.Theme(handleImage, UiColorRole.Accent, scope, context);

            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;

            // 不参与键盘导航，免得方向键把焦点移到滚动条上
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };

            Undo.RecordObject(scroll, UiStyleKit.UndoName);
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // 文字右边让出滚动条的位置，避免被盖住
            Undo.RecordObject(textRect, UiStyleKit.UndoName);
            textRect.offsetMax = new Vector2(-(ScrollbarWidth + ScrollbarGap), textRect.offsetMax.y);

            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Debug.Log($"[{nameof(SettingsControlsScroll)}] 按键说明已可滚动并带滚动条，记得保存场景", panel);
        }
    }
}
