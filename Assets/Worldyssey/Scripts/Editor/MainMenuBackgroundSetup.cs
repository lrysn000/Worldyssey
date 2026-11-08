using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 主菜单滚动背景：修正背景图的导入设置（Repeat、不压缩、不缩小、无 Mipmap），
    /// 在主菜单最底层放一个全屏 RawImage 并挂上 <see cref="MenuBackgroundScroller"/>。
    /// 可以重复运行：背景物体已存在时只更新贴图，不覆盖已调好的滚动速度。
    /// </summary>
    public static class MainMenuBackgroundSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Main Menu Scrolling Background";
        private const string TexturePath = ProjectPaths.Textures + "/UI/MainMenu/FourSeasons_PixelPanorama_v3_Seamless.png";
        private const string BackgroundName = "ScrollingBackground";
        private const int MaxTextureSize = 4096;

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            Texture2D texture = ApplyImportSettings();
            if (texture == null)
            {
                return;
            }

            MainMenuPanel menu = Object.FindObjectOfType<MainMenuPanel>(true);
            if (menu == null)
            {
                Debug.LogError($"[{nameof(MainMenuBackgroundSetup)}] 当前场景里找不到 MainMenuPanel，先打开游戏场景");
                return;
            }

            RawImage background = GetOrCreateBackground(menu.transform);
            Undo.RecordObject(background, "Main Menu Background");
            background.texture = texture;
            background.color = Color.white;
            background.raycastTarget = false;

            if (background.GetComponent<MenuBackgroundScroller>() == null)
            {
                Undo.AddComponent<MenuBackgroundScroller>(background.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
            Selection.activeObject = background.gameObject;
            Debug.Log($"[{nameof(MainMenuBackgroundSetup)}] 主菜单滚动背景已设置，记得保存场景；滚动速度在 {BackgroundName} 的 {nameof(MenuBackgroundScroller)} 上调整", background);
        }

        private static Texture2D ApplyImportSettings()
        {
            if (AssetImporter.GetAtPath(TexturePath) is not TextureImporter importer)
            {
                Debug.LogError($"[{nameof(MainMenuBackgroundSetup)}] 找不到背景图 {TexturePath}，未做任何修改");
                return null;
            }

            // Repeat 让 UV 偏移能循环；原图宽 2172，默认 2048 上限会把它缩小；压缩会在大块平涂色上出现色块噪点
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.maxTextureSize = MaxTextureSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        private static RawImage GetOrCreateBackground(Transform menu)
        {
            Transform existing = menu.Find(BackgroundName);
            if (existing != null && existing.TryGetComponent(out RawImage found))
            {
                existing.SetAsFirstSibling();
                return found;
            }

            var go = new GameObject(BackgroundName, typeof(RectTransform), typeof(RawImage));
            Undo.RegisterCreatedObjectUndo(go, "Main Menu Background");
            var rect = (RectTransform)go.transform;
            rect.SetParent(menu, false);
            GameUiBuilder.Stretch(rect);

            // 放在最底层，标题和按钮画在它上面
            rect.SetAsFirstSibling();
            return go.GetComponent<RawImage>();
        }
    }
}
