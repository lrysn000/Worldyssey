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
    /// 给当前场景里所有按钮、开关、下拉框，以及选关按钮预制体加上悬停放大，并把它们的轴心挪到中心（位置同步补偿，画面不变），
    /// 这样放大是从中心向四周，而不是朝一个角长出去。下拉框弹出列表里的选项、滑条不加。可以重复运行，新加的按钮再运行一次即可。
    /// </summary>
    public static class UiHoverScaleSetup
    {
        private const string MenuPath = "Worldyssey/Art/UI/Add Hover Scale To Buttons (Current Scene)";
        private const string ConfigPath = ProjectPaths.Config + "/UiMotionConfig.asset";
        private const string LevelButtonPrefabPath = ProjectPaths.UiPrefabs + "/P_LevelButton.prefab";
        private const string UndoName = "Add Hover Scale";

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            UiMotionConfig config = GetOrCreateConfig();
            int added = 0;
            foreach (Selectable selectable in Object.FindObjectsOfType<Selectable>(true))
            {
                if (IsTarget(selectable) && Attach(selectable.gameObject, config, true))
                {
                    added++;
                }
            }

            bool isPrefabChanged = AttachToPrefab(LevelButtonPrefabPath, config);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeObject = config;
            string prefabNote = isPrefabChanged ? "，选关按钮预制体也已加上" : string.Empty;
            Debug.Log($"[{nameof(UiHoverScaleSetup)}] 新加了 {added} 个悬停放大{prefabNote}。放大倍数和时长在 {ConfigPath} 里调，记得保存场景", config);
        }

        /// <summary>
        /// 按钮、开关、下拉框本身；下拉框弹出列表里的选项是运行时复制出来的，放大会和列表滚动区域冲突，跳过。
        /// </summary>
        private static bool IsTarget(Selectable selectable)
        {
            if (!(selectable is Button) && !(selectable is Toggle) && !(selectable is TMP_Dropdown))
            {
                return false;
            }

            Transform parent = selectable.transform.parent;
            return parent == null || parent.GetComponentInParent<TMP_Dropdown>(true) == null;
        }

        /// <summary>
        /// 加上组件并把轴心挪到中心。已经有组件的只补上配置引用。返回是否新加了组件。
        /// </summary>
        private static bool Attach(GameObject target, UiMotionConfig config, bool isSceneObject)
        {
            bool isNew = !target.TryGetComponent(out UiHoverScale hover);
            if (isNew)
            {
                hover = isSceneObject ? Undo.AddComponent<UiHoverScale>(target) : target.AddComponent<UiHoverScale>();
                CenterPivot((RectTransform)target.transform, isSceneObject);
            }

            var serialized = new SerializedObject(hover);
            serialized.FindProperty("_config").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return isNew;
        }

        /// <summary>
        /// 轴心挪到中心，同时按尺寸补偿位置，使控件在画面上的位置不变。
        /// </summary>
        private static void CenterPivot(RectTransform rect, bool isSceneObject)
        {
            Vector2 shift = Center - rect.pivot;
            if (shift == Vector2.zero)
            {
                return;
            }

            if (isSceneObject)
            {
                Undo.RecordObject(rect, UndoName);
            }

            Vector2 size = rect.rect.size;
            rect.pivot = Center;
            rect.anchoredPosition += Vector2.Scale(shift, size);
        }

        private static bool AttachToPrefab(string path, UiMotionConfig config)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Attach(root, config, false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        internal static UiMotionConfig GetOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<UiMotionConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<UiMotionConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }
    }
}
