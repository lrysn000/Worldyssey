using System.IO;
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
    /// 把现有界面整体换成像素风套件：生成像素字体、生成或补全 <see cref="UiSkin"/>，
    /// 再把当前场景和界面预制体里旧的 Honeti 圆角素材换成套件素材、文字换成像素字体。只换素材、字体和边框粗细，布局不动。
    /// 可以重复运行：改了 UiSkin（素材、字体、像素比例）后再运行一次即可更新。
    /// </summary>
    public static class PixelUiReskin
    {
        private const string MenuPath = "Worldyssey/UI/Apply Pixel UI Skin";
        private const string UndoName = "Apply Pixel UI Skin";
        // 换皮前的素材：最早的 Honeti 圆角套件，以及上一版像素套件用过的底板
        private static readonly string[] OldKitMarkers =
        {
            "/UltimateFlatGUI/", "/Pixel UI/Sprites/UI/Rounded/", "/PixelArtGUI2/Textures/Panels/UniversalPanel1",
        };

        // 自制图标所在的文件夹，换皮时统一改为点过滤导入
        private static readonly string[] IconFolders =
        {
            UiStyleKit.WorldIconFolder,
            ProjectPaths.Textures + "/UI/EditorBrushes",
            ProjectPaths.Textures + "/UI/StatusIcons",
        };

        [MenuItem(MenuPath)]
        public static void Apply()
        {
            UiSkin skin = PixelUiSkinDefaults.GetOrCreateSkin();
            var themeSet = GameUiBuilder.FindAsset<UiThemeSet>(null);
            if (skin == null || themeSet == null)
            {
                return;
            }

            RefreshIconImports();
            var context = new UiStyleKit.Context(themeSet.NeutralTheme);
            int changed = ReskinHierarchy(Object.FindObjectsOfType<Image>(true), Object.FindObjectsOfType<TMP_Text>(true), context, true);
            int prefabs = ReskinPrefabs(context);
            int panels = PixelUiDecorator.DecorateScene(context, themeSet);
            AddPixelCanvasScalers();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeObject = skin;
            Debug.Log($"[{nameof(PixelUiReskin)}] 像素风换皮完成：场景中更新 {changed} 处、装饰面板 {panels} 个，预制体 {prefabs} 个。素材、字体、像素比例在 UiSkin 里调整后可再次运行，记得保存场景", skin);
        }

        private static int ReskinHierarchy(Image[] images, TMP_Text[] texts, UiStyleKit.Context context, bool isSceneObject)
        {
            int changed = 0;
            foreach (Image image in images)
            {
                changed += ReskinImage(image, context, isSceneObject) ? 1 : 0;
            }

            foreach (TMP_Text text in texts)
            {
                if (isSceneObject)
                {
                    Undo.RecordObject(text, UndoName);
                }

                // 原本加粗的文字（标题、按钮）用标题字体；已经换过的按当前字体判断，重复运行结果不变
                bool isTitle = (text.fontStyle & FontStyles.Bold) != 0 || (context.TitleFont != null && context.TitleFont != context.Font && text.font == context.TitleFont);
                UiStyleKit.ApplyFont(text, context, isTitle);
                changed++;
            }

            return changed;
        }

        private static bool ReskinImage(Image image, UiStyleKit.Context context, bool isSceneObject)
        {
            Sprite current = image.sprite;
            if (current == null)
            {
                return false;
            }

            Sprite target = FindReplacement(image, current, context, out bool isSliced);
            if (target == null)
            {
                return false;
            }

            // 编辑器网格的格子只有几十像素，用小一号的像素比例
            int scale = image.GetComponentInParent<EditorGridCell>(true) != null ? context.Skin.SmallPixelScale : 0;
            if (isSceneObject)
            {
                Undo.RecordObject(image, UndoName);
            }

            if (isSliced)
            {
                UiStyleKit.SetSliced(image, target, scale);
            }
            else
            {
                image.sprite = target;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }

            return true;
        }

        /// <summary>旧素材按文件名对应到套件素材；已经是套件底板的也返回它本身，用来按新的像素比例刷新边框。</summary>
        private static Sprite FindReplacement(Image image, Sprite current, UiStyleKit.Context context, out bool isSliced)
        {
            isSliced = true;
            UiSkin skin = context.Skin;
            if (current == context.PanelSprite || current == context.ButtonSprite || current == skin.SliderTrack)
            {
                return current;
            }

            string path = AssetDatabase.GetAssetPath(current);
            if (!IsOldKit(path))
            {
                return null;
            }

            if (!path.Contains("/UltimateFlatGUI/"))
            {
                // 上一版像素套件的底板：同样按是否可点击分配
                return IsClickable(image) ? context.ButtonSprite : context.PanelSprite;
            }

            switch (Path.GetFileNameWithoutExtension(path))
            {
                case "UniversalPanel20":
                    return IsClickable(image) ? context.ButtonSprite : context.PanelSprite;
                case "SliderBg":
                    return skin.SliderTrack;
                case "SliderHandle":
                    return skin.SliderHandle;
            }

            isSliced = false;
            switch (Path.GetFileNameWithoutExtension(path))
            {
                case "check":
                    return skin.CheckIcon;
                case "arrow_down":
                    return skin.ArrowDownIcon;
                case "x":
                    return skin.CrossIcon;
                default:
                    return null;
            }
        }

        private static bool IsOldKit(string path)
        {
            foreach (string marker in OldKitMarkers)
            {
                if (path.Contains(marker))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>按钮、开关、下拉框、关卡卡片等可点击控件的底图用按钮素材，其余用面板素材。</summary>
        private static bool IsClickable(Image image)
        {
            Selectable selectable = image.GetComponentInParent<Selectable>(true);
            return selectable != null && selectable.targetGraphic == image;
        }

        private static int ReskinPrefabs(UiStyleKit.Context context)
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ProjectPaths.UiPrefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                int changed = ReskinHierarchy(contents.GetComponentsInChildren<Image>(true), contents.GetComponentsInChildren<TMP_Text>(true), context, false);
                PixelUiDecorator.DecoratePrefab(contents, context);
                if (changed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    count++;
                }

                PrefabUtility.UnloadPrefabContents(contents);
            }

            return count;
        }

        /// <summary>所有画布加上整数倍缩放，像素素材和像素字在 1080p 以外的分辨率下也保持清晰。</summary>
        private static void AddPixelCanvasScalers()
        {
            foreach (CanvasScaler scaler in Object.FindObjectsOfType<CanvasScaler>(true))
            {
                if (!scaler.TryGetComponent(out PixelCanvasScaler _))
                {
                    Undo.AddComponent<PixelCanvasScaler>(scaler.gameObject);
                }
            }
        }

        private static void RefreshIconImports()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", IconFolders))
            {
                UiStyleKit.LoadIconSprite(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
    }
}
