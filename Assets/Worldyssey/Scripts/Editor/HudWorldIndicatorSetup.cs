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
        private const string ConflictMarkPath = ProjectPaths.ThirdParty + "/Honeti/UltimateFlatGUI/ThemeCommon/Textures/Icons/64/x.png";
        private const int SlotCount = 4;

        private const float InfoWidth = 400f;
        private const float InfoHeight = 132f;
        private const float IndicatorWidth = 360f;
        private const float IndicatorHeight = 160f;
        private const float IconRowHeight = 88f;

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
            GameUiBuilder.Place(row, TopCenter, new Vector2(0f, -16f), new Vector2(IndicatorWidth - UiStyleKit.Padding * 2f, IconRowHeight));

            Sprite conflictMark = AssetDatabase.LoadAssetAtPath<Sprite>(ConflictMarkPath);
            if (conflictMark == null)
            {
                Debug.LogWarning($"[{nameof(HudWorldIndicatorSetup)}] 找不到叉号素材 {ConflictMarkPath}，冲突标记会显示为方块，可在场景里手动替换");
            }

            var slots = new Object[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                slots[i] = CreateSlot(row, i, icons[i], conflictMark);
            }

            UiThemeSceneSetup.SetReferenceArray(indicator, "_slots", slots);

            TMP_Text worldName = GameUiBuilder.CreateText(card, "WorldName", "World", 30f, TextAlignmentOptions.Center, TopCenter,
                new Vector2(0f, -108f), new Vector2(IndicatorWidth - UiStyleKit.Padding * 2f, 40f));
            UiStyleKit.StyleText(worldName, UiColorRole.Header, scope, context, true);
            GameUiBuilder.SetReference(indicator, "_worldNameText", worldName);
            return indicator;
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
