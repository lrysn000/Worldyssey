using UnityEditor;
using UnityEngine;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 让底座和传送箱子尺寸配套：底座做成占满一格的扁圆台，传送箱子缩小一圈，
    /// 箱子放上底座后四边仍露出底座的边和侧面（激活时这一圈会发光）。
    /// 改的是 LevelVisualConfig 当前引用的两个预制体。可以重复运行，结果相同。
    /// </summary>
    public static class PedestalBoxSizeSetup
    {
        /// <summary>底座直径（格）。1 表示刚好占满一格。</summary>
        internal const float PedestalDiameter = 1f;

        /// <summary>底座高度（格），凸出地面，侧面在斜俯视角下可见。</summary>
        internal const float PedestalHeight = 0.12f;

        /// <summary>传送箱子边长（格）。比底座直径小 0.2，四边中间各露出 0.1。</summary>
        internal const float PortalBoxSize = 0.8f;

        private const string MenuPath = "Worldyssey/Scene Setup/Match Pedestal And Portal Box Size";
        private const string ModelName = "Model";

        [MenuItem(MenuPath)]
        public static void Apply()
        {
            var config = GameUiBuilder.FindAsset<LevelVisualConfig>(null);
            if (config == null || config.PedestalPrefab == null || config.PortalBoxPrefab == null)
            {
                Debug.LogError($"[{nameof(PedestalBoxSizeSetup)}] 找不到 LevelVisualConfig 或其中的底座、传送箱子预制体，未做修改");
                return;
            }

            bool isPedestalDone = EditPrefab(config.PedestalPrefab, ResizePedestal);
            bool isBoxDone = EditPrefab(config.PortalBoxPrefab.gameObject, ResizePortalBox);
            if (isPedestalDone && isBoxDone)
            {
                Debug.Log($"[{nameof(PedestalBoxSizeSetup)}] 底座直径 {PedestalDiameter}、高 {PedestalHeight}，传送箱子边长 {PortalBoxSize}", config);
            }
        }

        private static bool EditPrefab(GameObject prefab, System.Func<GameObject, bool> edit)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!edit(root))
                {
                    return false;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool ResizePedestal(GameObject root)
        {
            Transform model = root.transform.Find(ModelName);
            if (model == null || model.GetComponent<MeshFilter>() == null)
            {
                Debug.LogError($"[{nameof(PedestalBoxSizeSetup)}] 底座预制体 {root.name} 没有名为 {ModelName} 的网格子物体，未修改");
                return false;
            }

            // 按 Unity 自带圆柱计算：直径 1、高 2，所以 y 缩放取高度的一半，中心抬到半高
            model.localScale = new Vector3(PedestalDiameter, PedestalHeight * 0.5f, PedestalDiameter);
            model.localPosition = new Vector3(0f, PedestalHeight * 0.5f, 0f);
            return true;
        }

        private static bool ResizePortalBox(GameObject root)
        {
            Transform model = root.transform.Find(ModelName);
            if (model == null || model.localScale.x <= 0f)
            {
                Debug.LogError($"[{nameof(PedestalBoxSizeSetup)}] 传送箱子预制体 {root.name} 没有名为 {ModelName} 的子物体，未修改");
                return false;
            }

            // 箱体、四面提示板、顶面符号一起按比例缩放，相对位置不变；只动末端物体，FaceHint 这类容器保持原样
            float ratio = PortalBoxSize / model.localScale.x;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == root.transform || child.childCount > 0)
                {
                    continue;
                }

                child.localPosition *= ratio;
                child.localScale *= ratio;
            }

            return true;
        }
    }
}
