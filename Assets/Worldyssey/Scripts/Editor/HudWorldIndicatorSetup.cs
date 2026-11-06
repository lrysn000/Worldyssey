using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Data;
using Worldyssey.Gameplay;
using Worldyssey.UI;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// HUD 改版：左上角卡片收窄为“关卡名 + 步数”，删掉旧的世界名、冲突、通关文字，
    /// 在画面正上方新建世界指示器（季节图标 + 世界名，冲突时图标变灰加叉号）。
    /// 世界图标会顺带设成 Sprite 导入。可以重复运行：指示器已存在时只更新引用和图标。
    /// </summary>
    public static class HudWorldIndicatorSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Build HUD World Indicator";
        private const string IndicatorName = "WorldIndicator";
        private const string InfoCardName = "InfoCard";
        private const int SlotCount = 4;

        private const float InfoWidth = 560f;
        private const float InfoHeight = 132f;
        private const float IndicatorWidth = 360f;
        private const float IndicatorHeight = 144f;

        // 图标行顶部留出像素边框和季节装饰的位置；当前图标切换时会放大弹回（UiMotionConfig.WorldPopScale），
        // 尺寸按放大后的大小留余量，免得弹出时顶出卡片上沿
        private const float IconRowTop = 28f;
        private const float IconRowHeight = 64f;
        private const float CurrentIconSize = 56f;
        private const float OtherIconSize = 36f;
        private const float WorldNameTop = 98f;
        private const float WorldNameHeight = 32f;
        private const float WorldNameFontSize = 22f;

        // 旧 HUD 留下的物体：世界名、冲突、通关文字和通关底板
        private static readonly string[] ObsoleteNames = { "WorldName", "Conflict", "Status", "statusTextBackground" };

        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        [MenuItem(MenuPath)]
        public static void BuildFromMenu()
        {
            GameFlow gameFlow = UiThemeSceneSetup.Setup();
            if (gameFlow == null)
            {
                return;
            }

            var hud = UiThemeSceneSetup.GetReference<HudPanel>(new SerializedObject(gameFlow), "_hudPanel");
            var scope = hud != null ? hud.GetComponent<UiThemeScope>() : null;
            if (scope == null)
            {
                Debug.LogError($"[{nameof(HudWorldIndicatorSetup)}] 找不到 HUD 或它的主题作用域，先运行 Worldyssey/Setup/Restyle UI (Honeti)", gameFlow);
                return;
            }

            var themeSet = GameUiBuilder.FindAsset<UiThemeSet>(null);
            Apply(hud, scope, new UiStyleKit.Context(themeSet.NeutralTheme));
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            Debug.Log($"[{nameof(HudWorldIndicatorSetup)}] HUD 改版完成，记得保存场景", hud);
        }

        /// <summary>
        /// 对 <paramref name="hud"/> 执行改版。换皮工具在新场景里也会调用这里，保证两条路径结果一致。
        /// </summary>
        internal static void Apply(HudPanel hud, UiThemeScope scope, UiStyleKit.Context context)
        {
            RemoveObsolete(hud.transform);
            LayoutInfoCard(hud, scope, context);

            Sprite[] icons = UiStyleKit.LoadWorldIcons();
            Transform existing = hud.transform.Find(IndicatorName);
            WorldIndicatorPanel indicator = existing != null ? existing.GetComponent<WorldIndicatorPanel>() : null;
            if (indicator == null)
            {
                indicator = CreateIndicator(hud.transform, icons, scope, context);
            }

            LayoutIndicator(indicator, scope, context);
            LinkIndicator(indicator, hud, icons);
        }

        private static void RemoveObsolete(Transform hud)
        {
            for (int i = 0; i < ObsoleteNames.Length; i++)
            {
                Transform obsolete = hud.Find(ObsoleteNames[i]);
                if (obsolete != null)
                {
                    Undo.DestroyObjectImmediate(obsolete.gameObject);
                }
            }
        }

        private static void LayoutInfoCard(HudPanel hud, UiThemeScope scope, UiStyleKit.Context context)
        {
            var position = new Vector2(UiStyleKit.Padding, -UiStyleKit.Padding);
            var size = new Vector2(InfoWidth, InfoHeight);
            Transform card = hud.transform.Find(InfoCardName);
            if (card == null)
            {
                card = UiStyleKit.CreateCard(hud.transform, InfoCardName, TopLeft, position, size, UiColorRole.PanelBackground, scope, context);
                card.SetAsFirstSibling();
            }
            else
            {
                Undo.RecordObject(card, UiStyleKit.UndoName);
                GameUiBuilder.Place((RectTransform)card, TopLeft, position, size);
            }

            float textWidth = InfoWidth - UiStyleKit.Padding * 2f;
            var serialized = new SerializedObject(hud);
            UiRestyleTool.PlaceText(serialized, "_levelNameText", TopLeft, new Vector2(48f, -36f), new Vector2(textWidth, 48f), 36f,
                UiColorRole.Header, true, scope, context);
            UiRestyleTool.PlaceText(serialized, "_stepCountText", TopLeft, new Vector2(48f, -88f), new Vector2(textWidth, 40f), 28f,
                UiColorRole.Text, false, scope, context);

            // 卡片变窄后长关卡名放不下，允许自动缩小字号
            var levelName = UiThemeSceneSetup.GetReference<TMP_Text>(serialized, "_levelNameText");
            if (levelName != null)
            {
                levelName.enableAutoSizing = true;
                levelName.fontSizeMin = 22f;
                levelName.fontSizeMax = 36f;
                levelName.enableWordWrapping = false;
            }
        }

        private static WorldIndicatorPanel CreateIndicator(Transform hud, Sprite[] icons, UiThemeScope scope, UiStyleKit.Context context)
        {
            RectTransform card = UiStyleKit.CreateCard(hud, IndicatorName, TopCenter, new Vector2(0f, -UiStyleKit.Padding),
                new Vector2(IndicatorWidth, IndicatorHeight), UiColorRole.PanelBackground, scope, context);
            var indicator = card.gameObject.AddComponent<WorldIndicatorPanel>();

            RectTransform row = GameUiBuilder.CreateRect("Icons", card);
            GameUiBuilder.Place(row, TopCenter, new Vector2(0f, -IconRowTop), new Vector2(IndicatorWidth - UiStyleKit.Padding * 2f, IconRowHeight));

            Sprite conflictMark = context.Skin != null ? context.Skin.CrossIcon : null;
            if (conflictMark == null)
            {
                Debug.LogWarning($"[{nameof(HudWorldIndicatorSetup)}] 界面套件里没有叉号图标，冲突标记会显示为方块，可在 UiSkin 里设置后重新运行");
            }

            var slots = new Object[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                slots[i] = CreateSlot(row, i, icons[i], conflictMark);
            }

            UiThemeSceneSetup.SetReferenceArray(indicator, "_slots", slots);

            TMP_Text worldName = GameUiBuilder.CreateText(card, "WorldName", "World", WorldNameFontSize, TextAlignmentOptions.Center, TopCenter,
                new Vector2(0f, -WorldNameTop), new Vector2(IndicatorWidth - UiStyleKit.Padding * 2f, WorldNameHeight));
            UiStyleKit.StyleText(worldName, UiColorRole.Header, scope, context, true);
            GameUiBuilder.SetReference(indicator, "_worldNameText", worldName);
            return indicator;
        }

        /// <summary>已有的指示器也按当前常量重新排版，改尺寸后重跑菜单即可生效。</summary>
        private static void LayoutIndicator(WorldIndicatorPanel indicator, UiThemeScope scope, UiStyleKit.Context context)
        {
            var card = (RectTransform)indicator.transform;
            Undo.RecordObject(card, UiStyleKit.UndoName);
            GameUiBuilder.Place(card, TopCenter, new Vector2(0f, -UiStyleKit.Padding), new Vector2(IndicatorWidth, IndicatorHeight));

            float innerWidth = IndicatorWidth - UiStyleKit.Padding * 2f;
            var row = card.Find("Icons") as RectTransform;
            if (row != null)
            {
                Undo.RecordObject(row, UiStyleKit.UndoName);
                GameUiBuilder.Place(row, TopCenter, new Vector2(0f, -IconRowTop), new Vector2(innerWidth, IconRowHeight));
            }

            var serialized = new SerializedObject(indicator);
            serialized.FindProperty("_currentIconSize").floatValue = CurrentIconSize;
            serialized.FindProperty("_otherIconSize").floatValue = OtherIconSize;
            serialized.ApplyModifiedProperties();

            UiRestyleTool.PlaceText(serialized, "_worldNameText", TopCenter, new Vector2(0f, -WorldNameTop), new Vector2(innerWidth, WorldNameHeight),
                WorldNameFontSize, UiColorRole.Header, true, scope, context);
        }

        private static WorldIconSlot CreateSlot(Transform row, int index, Sprite icon, Sprite conflictMark)
        {
            RectTransform rect = GameUiBuilder.CreateRect($"Slot{index}", row);
            GameUiBuilder.Place(rect, Center, Vector2.zero, new Vector2(80f, 80f));
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;

            // 叉号贴在图标右下角，用锚点按比例占位，图标缩放时一起缩放
            RectTransform markRect = GameUiBuilder.CreateRect("ConflictMark", rect);
            markRect.anchorMin = new Vector2(0.5f, -0.05f);
            markRect.anchorMax = new Vector2(1.05f, 0.5f);
            markRect.offsetMin = Vector2.zero;
            markRect.offsetMax = Vector2.zero;
            Image mark = markRect.gameObject.AddComponent<Image>();
            mark.sprite = conflictMark;
            mark.preserveAspect = true;
            mark.raycastTarget = false;
            markRect.gameObject.SetActive(false);

            var slot = rect.gameObject.AddComponent<WorldIconSlot>();
            GameUiBuilder.SetReference(slot, "_icon", image);
            GameUiBuilder.SetReference(slot, "_conflictMark", mark);
            return slot;
        }

        private static void LinkIndicator(WorldIndicatorPanel indicator, HudPanel hud, Sprite[] icons)
        {
            var levelSession = UiThemeSceneSetup.GetReference<LevelSession>(new SerializedObject(hud), "_levelSession");
            GameUiBuilder.SetReference(indicator, "_levelSession", levelSession);
            GameUiBuilder.SetReference(indicator, "_levelView", Object.FindObjectOfType<LevelView>());
            GameUiBuilder.SetReference(indicator, "_functionalPalette", GameUiBuilder.FindAsset<UiFunctionalPalette>(null));
            GameUiBuilder.SetReference(indicator, "_motionConfig", GameUiBuilder.FindAsset<UiMotionConfig>(null));
            UiThemeSceneSetup.SetReferenceArray(indicator, "_seasonIcons", icons);
        }
    }
}
