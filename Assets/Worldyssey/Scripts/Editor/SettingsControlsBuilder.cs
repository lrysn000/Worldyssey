using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Data;
using Worldyssey.Gameplay;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 在已有的设置面板里生成设置项：全屏开关、分辨率下拉框、三个音量滑条，以及按键提示，
    /// 套用 Honeti 素材和主题着色，并连好 SettingsPanel、GameSettingsController 和输入资源。已经生成过的会跳过。
    /// </summary>
    public static class SettingsControlsBuilder
    {
        private const string MenuPath = "Worldyssey/Setup/Build Settings Controls (Current Scene)";
        private const string UndoName = "Build Settings Controls";

        private const float RowHeight = 64f;
        private const float RowGap = 16f;
        private const float LabelWidth = 260f;
        private const float ControlWidth = 360f;
        private const float ControlsColumnX = 680f;

        private static readonly Vector2 CardSize = new Vector2(1180f, 760f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);

        [MenuItem(MenuPath)]
        public static void Build()
        {
            var panel = Object.FindObjectOfType<SettingsPanel>(true);
            var gameFlow = Object.FindObjectOfType<GameFlow>();
            var inputReader = Object.FindObjectOfType<PlayerInputReader>();
            var themeSet = GameUiBuilder.FindAsset<UiThemeSet>(null);
            Transform card = panel != null ? panel.transform.Find("Card") : null;
            Transform body = card != null ? card.Find("Body") : null;
            if (panel == null || gameFlow == null || themeSet == null || body == null || !panel.TryGetComponent(out UiThemeScope scope))
            {
                Debug.LogError($"[{nameof(SettingsControlsBuilder)}] 需要场景里有由 Add UI Theme + Settings Panel 生成的设置面板（含 Card/Body）、GameFlow 和主题资源，未做任何修改");
                return;
            }

            var panelData = new SerializedObject(panel);
            if (panelData.FindProperty("_masterSlider").objectReferenceValue != null)
            {
                Debug.Log($"[{nameof(SettingsControlsBuilder)}] 设置面板已经有设置项，跳过", panel);
                return;
            }

            var context = new UiStyleKit.Context(themeSet.NeutralTheme);
            ResizeCard((RectTransform)card);
            RemovePlaceholder(body);

            var resources = new DefaultControls.Resources
            {
                standard = context.PanelSprite,
                background = context.Skin != null && context.Skin.SliderTrack != null ? context.Skin.SliderTrack : context.PanelSprite,
                knob = context.Skin != null ? context.Skin.SliderHandle : null,
                checkmark = context.Skin != null ? context.Skin.CheckIcon : null,
            };

            float y = 0f;
            Toggle fullscreen = CreateToggleRow(body, UiText.Fullscreen, y, resources, scope, context);
            y += RowHeight + RowGap;
            TMP_Dropdown resolution = CreateDropdownRow(body, UiText.Resolution, y, resources, scope, context);
            y += RowHeight + RowGap;
            Slider master = CreateSliderRow(body, UiText.MasterVolume, y, resources, scope, context);
            y += RowHeight + RowGap;
            Slider music = CreateSliderRow(body, UiText.MusicVolume, y, resources, scope, context);
            y += RowHeight + RowGap;
            Slider sfx = CreateSliderRow(body, UiText.SfxVolume, y, resources, scope, context);

            TMP_Text controls = GameUiBuilder.CreateText(body, "Controls", UiText.ControlsHeader, 24f, TextAlignmentOptions.TopLeft, TopLeft,
                new Vector2(ControlsColumnX, -16f), new Vector2(420f, 470f));
            UiStyleKit.StyleText(controls, UiColorRole.Text, scope, context, false);

            if (!gameFlow.TryGetComponent(out GameSettingsController settings))
            {
                settings = Undo.AddComponent<GameSettingsController>(gameFlow.gameObject);
            }

            GameUiBuilder.SetReference(panel, "_settings", settings);
            GameUiBuilder.SetReference(panel, "_fullscreenToggle", fullscreen);
            GameUiBuilder.SetReference(panel, "_resolutionDropdown", resolution);
            GameUiBuilder.SetReference(panel, "_masterSlider", master);
            GameUiBuilder.SetReference(panel, "_musicSlider", music);
            GameUiBuilder.SetReference(panel, "_sfxSlider", sfx);
            GameUiBuilder.SetReference(panel, "_controlsText", controls);
            if (inputReader != null)
            {
                GameUiBuilder.SetReference(panel, "_inputActions", new SerializedObject(inputReader).FindProperty("_inputActions").objectReferenceValue);
            }

            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Selection.activeObject = panel;
            Debug.Log($"[{nameof(SettingsControlsBuilder)}] 设置项已生成，记得保存场景", panel);
        }

        private static Toggle CreateToggleRow(Transform body, string label, float y, DefaultControls.Resources resources, UiThemeScope scope, UiStyleKit.Context context)
        {
            CreateLabel(body, label, y, scope, context);
            GameObject toggleObject = DefaultControls.CreateToggle(resources);
            RectTransform rect = Attach(toggleObject, body, "FullscreenToggle", new Vector2(LabelWidth, -y - 8f), new Vector2(RowHeight - 16f, RowHeight - 16f));

            // 默认控件带一个旧版 Text 标签，这里标签由左边的文字负责，删掉它
            Object.DestroyImmediate(rect.Find("Label").gameObject);
            var background = (RectTransform)rect.Find("Background");
            GameUiBuilder.Stretch(background);
            UiStyleKit.SetSliced(background.GetComponent<Image>(), context.PanelSprite);
            UiStyleKit.Theme(background.GetComponent<Image>(), UiColorRole.PanelBackground, scope, context);
            var checkmark = (RectTransform)background.Find("Checkmark");
            GameUiBuilder.Stretch(checkmark);
            checkmark.offsetMin = new Vector2(6f, 6f);
            checkmark.offsetMax = new Vector2(-6f, -6f);
            UiStyleKit.Theme(checkmark.GetComponent<Image>(), UiColorRole.Accent, scope, context);
            return toggleObject.GetComponent<Toggle>();
        }

        private static Slider CreateSliderRow(Transform body, string label, float y, DefaultControls.Resources resources, UiThemeScope scope, UiStyleKit.Context context)
        {
            CreateLabel(body, label, y, scope, context);
            GameObject sliderObject = DefaultControls.CreateSlider(resources);
            RectTransform rect = Attach(sliderObject, body, label.Replace(" ", string.Empty) + "Slider",
                new Vector2(LabelWidth, -y - RowHeight * 0.25f), new Vector2(ControlWidth, RowHeight * 0.5f));
            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            UiStyleKit.Theme(rect.Find("Background").GetComponent<Image>(), UiColorRole.PanelBackground, scope, context);
            UiStyleKit.Theme(rect.Find("Fill Area/Fill").GetComponent<Image>(), UiColorRole.Accent, scope, context);
            Image handle = rect.Find("Handle Slide Area/Handle").GetComponent<Image>();
            ((RectTransform)handle.transform).sizeDelta = new Vector2(RowHeight * 0.6f, 0f);
            UiStyleKit.Theme(handle, UiColorRole.Header, scope, context);
            return slider;
        }

        private static TMP_Dropdown CreateDropdownRow(Transform body, string label, float y, DefaultControls.Resources resources, UiThemeScope scope, UiStyleKit.Context context)
        {
            CreateLabel(body, label, y, scope, context);
            var tmpResources = new TMP_DefaultControls.Resources
            {
                standard = context.PanelSprite,
                background = context.PanelSprite,
                dropdown = context.Skin != null ? context.Skin.ArrowDownIcon : null,
                checkmark = resources.checkmark,
                knob = context.PanelSprite,
            };
            GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(tmpResources);
            RectTransform rect = Attach(dropdownObject, body, "ResolutionDropdown", new Vector2(LabelWidth, -y), new Vector2(ControlWidth, RowHeight - 8f));
            UiStyleKit.SetSliced(dropdownObject.GetComponent<Image>(), context.PanelSprite);
            UiStyleKit.Theme(dropdownObject.GetComponent<Image>(), UiColorRole.PanelBackground, scope, context);
            StyleDropdownText(rect.Find("Label"), 26f, scope, context);
            UiStyleKit.Theme(rect.Find("Arrow").GetComponent<Image>(), UiColorRole.Icon, scope, context);

            var template = (RectTransform)rect.Find("Template");
            template.sizeDelta = new Vector2(template.sizeDelta.x, 300f);
            UiStyleKit.Theme(template.GetComponent<Image>(), UiColorRole.InnerFrame, scope, context);
            Transform item = template.Find("Viewport/Content/Item");
            ((RectTransform)item).sizeDelta = new Vector2(0f, 44f);
            UiStyleKit.Theme(item.Find("Item Background").GetComponent<Image>(), UiColorRole.InnerFrame, scope, context);
            UiStyleKit.Theme(item.Find("Item Checkmark").GetComponent<Image>(), UiColorRole.Accent, scope, context);
            StyleDropdownText(item.Find("Item Label"), 24f, scope, context);
            Transform scrollbar = template.Find("Scrollbar");
            UiStyleKit.Theme(scrollbar.GetComponent<Image>(), UiColorRole.PanelBackground, scope, context);
            UiStyleKit.Theme(scrollbar.Find("Sliding Area/Handle").GetComponent<Image>(), UiColorRole.Header, scope, context);
            return dropdownObject.GetComponent<TMP_Dropdown>();
        }

        private static void CreateLabel(Transform body, string label, float y, UiThemeScope scope, UiStyleKit.Context context)
        {
            TMP_Text text = GameUiBuilder.CreateText(body, label.Replace(" ", string.Empty) + "Label", label, 28f, TextAlignmentOptions.MidlineLeft, TopLeft,
                new Vector2(24f, -y - 16f), new Vector2(LabelWidth - 32f, RowHeight));
            UiStyleKit.StyleText(text, UiColorRole.Text, scope, context, false);
        }

        private static void StyleDropdownText(Transform textTransform, float fontSize, UiThemeScope scope, UiStyleKit.Context context)
        {
            var text = textTransform.GetComponent<TMP_Text>();
            text.fontSize = fontSize;
            UiStyleKit.StyleText(text, UiColorRole.Text, scope, context, false);
        }

        /// <summary>
        /// 把默认控件挂到内容区，左上角对齐放置。内容区上方留出与标题栏的间距。
        /// </summary>
        private static RectTransform Attach(GameObject control, Transform body, string controlName, Vector2 position, Vector2 size)
        {
            Undo.RegisterCreatedObjectUndo(control, UndoName);
            control.name = controlName;
            var rect = (RectTransform)control.transform;
            rect.SetParent(body, false);
            GameUiBuilder.Place(rect, TopLeft, position + new Vector2(0f, -16f), size);
            return rect;
        }

        private static void ResizeCard(RectTransform card)
        {
            Undo.RecordObject(card, UndoName);
            card.sizeDelta = CardSize;
        }

        private static void RemovePlaceholder(Transform body)
        {
            Transform placeholder = body.Find("Placeholder");
            if (placeholder != null)
            {
                Undo.DestroyObjectImmediate(placeholder.gameObject);
            }
        }
    }
}
