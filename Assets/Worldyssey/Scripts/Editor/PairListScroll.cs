using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 关卡编辑器的配对列表：一行一项、可用滚轮滚动。新建界面时由 <see cref="LevelEditorUiBuilder"/> 调用；
    /// 已有场景用菜单把旧的两列网格换成滚动列表。
    /// </summary>
    public static class PairListScroll
    {
        private const string MenuPath = "Worldyssey/Setup/Upgrade Editor Pair List";
        private const string ScrollName = "PairScroll";
        private const float Spacing = 12f;
        private const int Padding = 8;
        private const float ScrollSensitivity = 30f;

        [MenuItem(MenuPath)]
        public static void UpgradeFromMenu()
        {
            var pairList = Object.FindObjectOfType<EditorPairList>(true);
            if (pairList == null)
            {
                Debug.LogError($"[{nameof(PairListScroll)}] 当前场景没有 {nameof(EditorPairList)}，未做任何修改");
                return;
            }

            var serialized = new SerializedObject(pairList);
            var oldRoot = UiThemeSceneSetup.GetReference<RectTransform>(serialized, "_listRoot");
            if (oldRoot == null || oldRoot.GetComponent<GridLayoutGroup>() == null)
            {
                Debug.Log($"[{nameof(PairListScroll)}] 配对列表已经是滚动列表，无需升级", pairList);
                return;
            }

            ScrollRect scroll = Create(oldRoot.parent, oldRoot.anchoredPosition, oldRoot.sizeDelta);
            scroll.transform.SetSiblingIndex(oldRoot.GetSiblingIndex());
            Link(pairList, scroll);
            Undo.DestroyObjectImmediate(oldRoot.gameObject);
            EditorSceneManager.MarkSceneDirty(pairList.gameObject.scene);
            Debug.Log($"[{nameof(PairListScroll)}] 配对列表已改为一行一项的滚动列表，记得保存场景", pairList);
        }

        /// <summary>
        /// 在 <paramref name="parent"/> 左上角坐标系里建一个滚动列表，返回 ScrollRect，列表项放进它的 content。
        /// </summary>
        internal static ScrollRect Create(Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform viewport = GameUiBuilder.CreateRect(ScrollName, parent);
            Undo.RegisterCreatedObjectUndo(viewport.gameObject, UiStyleKit.UndoName);
            var topLeft = new Vector2(0f, 1f);
            viewport.anchorMin = topLeft;
            viewport.anchorMax = topLeft;
            viewport.pivot = topLeft;
            viewport.anchoredPosition = position;
            viewport.sizeDelta = size;
            viewport.gameObject.AddComponent<RectMask2D>();

            // 透明底图只用来接收滚轮：鼠标停在列表项之间的空隙时也能滚动
            Image hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;

            RectTransform content = GameUiBuilder.CreateRect("Content", viewport);
            content.anchorMin = topLeft;
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            // 宽度撑满一行，高度用列表项预制体自己的高度
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(Padding, Padding, Padding, Padding);
            layout.spacing = Spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = ScrollSensitivity;
            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }

        /// <summary>把滚动列表接到配对列表组件上。</summary>
        internal static void Link(EditorPairList pairList, ScrollRect scroll)
        {
            GameUiBuilder.SetReference(pairList, "_listRoot", scroll.content);
            GameUiBuilder.SetReference(pairList, "_scrollRect", scroll);
        }
    }
}
