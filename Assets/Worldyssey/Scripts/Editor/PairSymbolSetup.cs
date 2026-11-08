using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Worldyssey.Core;
using Worldyssey.Data;
using Worldyssey.UI;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 配对符号一键接入：创建 <see cref="PairSymbolSet"/>、在传送箱子预制体顶面加符号层、
    /// 编辑器网格格子和配对列表项加符号位，并把配置接到场景里用到的地方。可以重复运行。
    /// </summary>
    public static class PairSymbolSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Add Pair Symbols";
        private const string SymbolFolder = ProjectPaths.Textures + "/UI/PairSymbols";
        private const string SetPath = ProjectPaths.Config + "/PairSymbolSet.asset";
        private const string SymbolChildName = "PairSymbol";
        private const string IconChildName = "Icon";
        private const string SpriteMaterialName = "Sprites-Default.mat";

        // 符号边长占箱子顶面宽度的比例；高出顶面一点点，避免与箱面闪烁
        private const float SymbolSizeRatio = 0.5f;
        private const float SurfaceLift = 0.005f;
        private const float ListIconSize = 32f;
        private const float ListIconInset = 16f;

        // 配对 0~5 依次使用的形状。菱形和圆形在小尺寸下难以分辨，方形和箱子、终点的外形相近，都不用
        private static readonly string[] SymbolOrder = { "Circle", "Triangle", "Star", "Heart", "Cross", "Moon" };

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var config = GameUiBuilder.FindAsset<LevelVisualConfig>(null);
            var themeSet = GameUiBuilder.FindAsset<UiThemeSet>(null);
            if (config == null || config.PortalBoxPrefab == null || themeSet == null)
            {
                Debug.LogError($"[{nameof(PairSymbolSetup)}] 找不到 LevelVisualConfig、传送箱子预制体或 UiThemeSet，未做任何修改");
                return;
            }

            PairSymbolSet symbols = GetOrCreateSet();
            if (symbols == null)
            {
                return;
            }

            GameUiBuilder.SetReference(config, "_pairSymbols", symbols);
            AddBoxSymbolLayer(config.PortalBoxPrefab);
            AddListIconLayer();

            // 编辑器网格：格子预制体补符号位，场景里的网格接上符号配置
            EditorGridCellPrefabBuilder.GetOrUpdate(new UiStyleKit.Context(themeSet.NeutralTheme));
            foreach (EditorGridView grid in Object.FindObjectsOfType<EditorGridView>(true))
            {
                EditorGridIcons.Assign(grid);
            }

            foreach (EditorPairList pairList in Object.FindObjectsOfType<EditorPairList>(true))
            {
                GameUiBuilder.SetReference(pairList, "_pairSymbols", symbols);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeObject = symbols;
            Debug.Log($"[{nameof(PairSymbolSetup)}] 配对符号已接入，配置在 {SetPath}，记得保存场景", symbols);
        }

        private static PairSymbolSet GetOrCreateSet()
        {
            if (SymbolOrder.Length != LevelLimits.MaxPairCount)
            {
                Debug.LogError($"[{nameof(PairSymbolSetup)}] 符号顺序表有 {SymbolOrder.Length} 个，与配对上限 {LevelLimits.MaxPairCount} 不一致，未做任何修改");
                return null;
            }

            // 按顺序表找图：文件名形如 PairSymbol_3_Star，只看下划线后的形状名，序号不影响结果
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SymbolFolder });
            var sprites = new Object[SymbolOrder.Length];
            for (int i = 0; i < SymbolOrder.Length; i++)
            {
                string path = FindSymbolPath(guids, SymbolOrder[i]);
                sprites[i] = path != null ? UiStyleKit.LoadIconSprite(path) : null;
                if (sprites[i] == null)
                {
                    Debug.LogError($"[{nameof(PairSymbolSetup)}] {SymbolFolder} 里找不到符号 {SymbolOrder[i]}，未做任何修改");
                    return null;
                }
            }

            var set = AssetDatabase.LoadAssetAtPath<PairSymbolSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<PairSymbolSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }

            UiThemeSceneSetup.SetReferenceArray(set, "_symbols", sprites);
            EditorUtility.SetDirty(set);
            return set;
        }

        private static string FindSymbolPath(string[] guids, string shape)
        {
            string suffix = "_" + shape + ".png";
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith(suffix, System.StringComparison.Ordinal))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>在传送箱子预制体的顶面加一个朝上的符号层，并接到 <see cref="PortalBoxView"/>。</summary>
        private static void AddBoxSymbolLayer(PortalBoxView prefab)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            var view = contents.GetComponent<PortalBoxView>();
            var body = UiThemeSceneSetup.GetReference<Renderer>(new SerializedObject(view), "_bodyRenderer");
            if (body == null)
            {
                Debug.LogError($"[{nameof(PairSymbolSetup)}] 传送箱子预制体没有设置 _bodyRenderer，无法确定顶面位置", prefab);
                PrefabUtility.UnloadPrefabContents(contents);
                return;
            }

            Transform root = contents.transform;
            Transform existing = root.Find(SymbolChildName);
            SpriteRenderer symbol = existing != null ? existing.GetComponent<SpriteRenderer>() : null;
            if (symbol == null)
            {
                symbol = new GameObject(SymbolChildName).AddComponent<SpriteRenderer>();
                symbol.transform.SetParent(root, false);
            }

            // 用箱体包围盒确定顶面中心和宽度（预制体内容在独立场景里，包围盒是有效的）
            Bounds bounds = body.bounds;
            Vector3 topCenter = root.InverseTransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
            float spriteWorldSize = 8f / UiStyleKit.IconPixelsPerUnit;
            float scale = bounds.size.x * SymbolSizeRatio / spriteWorldSize / Mathf.Max(root.lossyScale.x, 0.0001f);

            // 精灵默认朝 -Z，绕 X 转 90° 后朝上
            symbol.transform.localPosition = topCenter + Vector3.up * SurfaceLift;
            symbol.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            symbol.transform.localScale = Vector3.one * scale;
            // 用内置的不受光照精灵材质：在 3D 渲染器下稳定可见，颜色就是设置的颜色
            symbol.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>(SpriteMaterialName);
            symbol.shadowCastingMode = ShadowCastingMode.Off;
            symbol.receiveShadows = false;
            symbol.enabled = false;

            var serialized = new SerializedObject(view);
            serialized.FindProperty("_pairSymbol").objectReferenceValue = symbol;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        /// <summary>在列表项预制体左侧加一个小图标位（默认隐藏，只有配对列表会设置图标）。</summary>
        private static void AddListIconLayer()
        {
            LevelButton prefab = GameUiBuilder.GetOrCreateLevelButtonPrefab();
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            var button = contents.GetComponent<LevelButton>();
            var serialized = new SerializedObject(button);
            if (UiThemeSceneSetup.GetReference<Image>(serialized, "_icon") == null)
            {
                RectTransform rect = GameUiBuilder.CreateRect(IconChildName, contents.transform);
                var left = new Vector2(0f, 0.5f);
                rect.anchorMin = left;
                rect.anchorMax = left;
                rect.pivot = left;
                rect.anchoredPosition = new Vector2(ListIconInset, 0f);
                rect.sizeDelta = new Vector2(ListIconSize, ListIconSize);
                Image icon = rect.gameObject.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.enabled = false;

                // 跟随主题着色，和名字同色；选中时由 LevelButton 换成浅色
                var themed = rect.gameObject.AddComponent<ThemedGraphic>();
                var themedSerialized = new SerializedObject(themed);
                themedSerialized.FindProperty("_role").enumValueIndex = (int)UiColorRole.Text;
                themedSerialized.ApplyModifiedPropertiesWithoutUndo();

                serialized.FindProperty("_icon").objectReferenceValue = icon;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }

            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
