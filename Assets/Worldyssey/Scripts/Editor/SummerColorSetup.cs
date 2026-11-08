using UnityEditor;
using UnityEngine;
using Worldyssey.Data;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 把 WorldPalette 里的夏季色从青色换成叶绿：原来的青色和冬季中蓝色相接近，传送箱子很难分辨。
    /// 通过 SerializedObject 修改，支持撤销；可以重复运行。
    /// </summary>
    public static class SummerColorSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Summer Palette Color";
        private const int SummerIndex = 1;
        private static readonly Color SummerColor = new Color(0.42f, 0.72f, 0.24f, 1f);

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(WorldPalette)}");
            if (guids.Length == 0)
            {
                Debug.LogError($"[{nameof(SummerColorSetup)}] 找不到 WorldPalette 资源，未做任何修改");
                return;
            }

            foreach (string guid in guids)
            {
                var palette = AssetDatabase.LoadAssetAtPath<WorldPalette>(AssetDatabase.GUIDToAssetPath(guid));
                if (palette != null)
                {
                    ApplySummerColor(palette);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static void ApplySummerColor(WorldPalette palette)
        {
            var serialized = new SerializedObject(palette);
            SerializedProperty colors = serialized.FindProperty("_colors");
            if (colors == null || colors.arraySize <= SummerIndex)
            {
                Debug.LogError($"[{nameof(SummerColorSetup)}] {palette.name} 的颜色数组缺少夏季项，未修改", palette);
                return;
            }

            colors.GetArrayElementAtIndex(SummerIndex).colorValue = SummerColor;
            serialized.ApplyModifiedProperties();
            Debug.Log($"[{nameof(SummerColorSetup)}] {palette.name} 的夏季色已改为叶绿", palette);
        }
    }
}
