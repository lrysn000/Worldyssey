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
    /// 关卡教学：生成 TutorialLibrary（已有内容时不覆盖）、导入箭头图，在 HUD 下重建提示条、箭头和 TutorialDirector 并接到 GameFlow。
    /// 可以重复运行；HUD 下的 Tutorial 节点每次都会重建，教学文字保存在 TutorialLibrary 里不受影响。
    /// </summary>
    public static class TutorialSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Build Tutorial";
        private const string ResetMenuPath = "Worldyssey/Setup/Reset Tutorial Texts";
        private const string LibraryPath = ProjectPaths.Config + "/TutorialLibrary.asset";
        private const string ArrowPath = ProjectPaths.Textures + "/UI/Tutorial/TutorialArrow.png";
        private const string RootName = "Tutorial";

        // 提示条放在 HUD 传送提示（底边 110）的下方，两者同时出现也不重叠
        private const float HintBottom = 16f;
        private const float HintWidth = 1240f;
        private const float HintHeight = 88f;
        private const float HintFontSize = 30f;
        private static readonly Vector2 HintTextInset = new Vector2(28f, 10f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

        [MenuItem(MenuPath)]
        public static void Build()
        {
            TutorialLibrary library = GetOrCreateLibrary(false);
            Sprite arrow = ImportArrow();
            var gameFlow = Object.FindObjectOfType<GameFlow>(true);
            if (library == null || arrow == null || gameFlow == null)
            {
                Debug.LogError($"[{nameof(TutorialSetup)}] 缺少 TutorialLibrary、箭头图 {ArrowPath} 或场景里的 GameFlow，先打开游戏场景");
                return;
            }

            var flow = new SerializedObject(gameFlow);
            var hud = UiThemeSceneSetup.GetReference<HudPanel>(flow, "_hudPanel");
            var levelSession = UiThemeSceneSetup.GetReference<LevelSession>(flow, "_levelSession");
            var levelView = levelSession != null ? UiThemeSceneSetup.GetReference<LevelView>(new SerializedObject(levelSession), "_levelView") : null;
            var cameraRig = Object.FindObjectOfType<CameraRig>(true);
            var scope = hud != null ? hud.GetComponent<UiThemeScope>() : null;
            var themeSet = GameUiBuilder.FindAsset<UiThemeSet>(null);
            if (hud == null || levelView == null || cameraRig == null || cameraRig.ViewCamera == null || scope == null || themeSet == null)
            {
                Debug.LogError($"[{nameof(TutorialSetup)}] 找不到 HUD（及其主题作用域）、LevelView、CameraRig 相机或 UiThemeSet，未做修改", gameFlow);
                return;
            }

            var context = new UiStyleKit.Context(themeSet.NeutralTheme);
            RectTransform root = RebuildRoot(hud.transform);
            TutorialHintPanel hint = CreateHint(root, scope, context);
            TutorialMarker marker = CreateMarker(root, arrow, cameraRig.ViewCamera, scope, context);

            var director = root.gameObject.AddComponent<TutorialDirector>();
            GameUiBuilder.SetReference(director, "_levelSession", levelSession);
            GameUiBuilder.SetReference(director, "_levelView", levelView);
            GameUiBuilder.SetReference(director, "_library", library);
            GameUiBuilder.SetReference(director, "_hintPanel", hint);
            GameUiBuilder.SetReference(director, "_marker", marker);
            GameUiBuilder.SetReference(director, "_worldIndicator", hud.GetComponentInChildren<WorldIndicatorPanel>(true));
            GameUiBuilder.SetReference(gameFlow, "_tutorialDirector", director);

            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            Selection.activeObject = library;
            Debug.Log($"[{nameof(TutorialSetup)}] 教学已接好，记得保存场景；文字和触发条件在 {LibraryPath} 里改", library);
        }

        [MenuItem(ResetMenuPath)]
        public static void ResetTexts()
        {
            if (!EditorUtility.DisplayDialog("Reset Tutorial Texts", "用默认内容覆盖 TutorialLibrary 里的全部关卡教学？", "覆盖", "取消"))
            {
                return;
            }

            TutorialLibrary library = GetOrCreateLibrary(true);
            Debug.Log($"[{nameof(TutorialSetup)}] 已写入默认教学内容", library);
        }

        private static TutorialLibrary GetOrCreateLibrary(bool isOverwriting)
        {
            var library = AssetDatabase.LoadAssetAtPath<TutorialLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<TutorialLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            // 已经有内容时保留，避免覆盖在 Inspector 里改过的文字
            if (isOverwriting || !library.HasTutorials)
            {
                Undo.RecordObject(library, "Tutorial Texts");
                library.SetTutorials(TutorialContent.Build());
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
            }

            return library;
        }

        private static Sprite ImportArrow()
        {
            if (AssetImporter.GetAtPath(ArrowPath) is not TextureImporter importer)
            {
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = UiStyleKit.IconPixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath);
        }

        private static RectTransform RebuildRoot(Transform hud)
        {
            Transform existing = hud.Find(RootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            RectTransform root = GameUiBuilder.CreateRect(RootName, hud);
            Undo.RegisterCreatedObjectUndo(root.gameObject, UiStyleKit.UndoName);
            GameUiBuilder.Stretch(root);

            // 画在 HUD 其他元素上面
            root.SetAsLastSibling();
            return root;
        }

        private static TutorialHintPanel CreateHint(RectTransform root, UiThemeScope scope, UiStyleKit.Context context)
        {
            RectTransform card = UiStyleKit.CreateCard(root, "HintBar", BottomCenter, new Vector2(0f, HintBottom), new Vector2(HintWidth, HintHeight),
                UiColorRole.PanelBackground, scope, context);
            card.gameObject.AddComponent<CanvasGroup>();

            RectTransform textRect = GameUiBuilder.CreateRect("Text", card);
            GameUiBuilder.Stretch(textRect);
            textRect.offsetMin = HintTextInset;
            textRect.offsetMax = -HintTextInset;
            var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = HintFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            UiStyleKit.StyleText(text, UiColorRole.Text, scope, context, false);

            var panel = card.gameObject.AddComponent<TutorialHintPanel>();
            GameUiBuilder.SetReference(panel, "_text", text);
            return panel;
        }

        private static TutorialMarker CreateMarker(RectTransform root, Sprite arrow, Camera viewCamera, UiThemeScope scope, UiStyleKit.Context context)
        {
            RectTransform markerRect = GameUiBuilder.CreateRect("Marker", root);
            GameUiBuilder.Stretch(markerRect);
            markerRect.pivot = new Vector2(0.5f, 0.5f);

            RectTransform arrowRect = GameUiBuilder.CreateRect("Arrow", markerRect);
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(0.5f, 0.5f);

            // 与界面素材一致：1 个图片像素 = UiPixelScale 个界面单位
            int pixelScale = context.Skin != null ? context.Skin.UiPixelScale : 4;
            arrowRect.sizeDelta = arrow.rect.size * pixelScale;
            var image = arrowRect.gameObject.AddComponent<Image>();
            image.sprite = arrow;
            image.raycastTarget = false;
            UiStyleKit.Theme(image, UiColorRole.Accent, scope, context);

            var marker = markerRect.gameObject.AddComponent<TutorialMarker>();
            GameUiBuilder.SetReference(marker, "_camera", viewCamera);
            GameUiBuilder.SetReference(marker, "_arrow", arrowRect);
            return marker;
        }
    }
}
