using TMPro;
using UnityEditor;
using UnityEngine;
using Worldyssey.Data;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 给编辑器网格（关卡编辑器和选关小地图共用）接上地形图标、实体图标和画笔图标（悬停预览用），图标就是画笔那一套；
    /// 关卡编辑器的网格另外在中间区块左下角放一行坐标文字。
    /// </summary>
    internal static class EditorGridIcons
    {
        private const string CoordinateTextName = "CoordinateText";

        // 与 EditorTool 枚举顺序一致
        private static readonly string[] BrushNames =
        {
            "Brush_Floor", "Brush_Wall", "Brush_Pedestal", "Brush_Goal", "Brush_UniqueWall", "Brush_Box", "Brush_Player", "Brush_Portal", "Brush_Eraser",
        };

        internal static void Assign(EditorGridView grid)
        {
            string brushes = LevelEditorUiBuilder.BrushFolder;
            GameUiBuilder.SetReference(grid, "_wallIcon", UiStyleKit.LoadIconSprite(brushes + "/Brush_Wall.png"));
            GameUiBuilder.SetReference(grid, "_uniqueWallIcon", UiStyleKit.LoadIconSprite(brushes + "/Brush_UniqueWall.png"));
            GameUiBuilder.SetReference(grid, "_pedestalIcon", UiStyleKit.LoadIconSprite(brushes + "/Brush_Pedestal.png"));
            GameUiBuilder.SetReference(grid, "_goalIcon", UiStyleKit.LoadIconSprite(brushes + "/Brush_Goal.png"));
            GameUiBuilder.SetReference(grid, "_playerIcon", UiStyleKit.LoadIconSprite(brushes + "/Grid_Player.png"));
            GameUiBuilder.SetReference(grid, "_boxIcon", UiStyleKit.LoadIconSprite(brushes + "/Brush_Box.png"));
            GameUiBuilder.SetReference(grid, "_portalIcon", UiStyleKit.LoadIconSprite(brushes + "/Brush_Portal.png"));

            var toolIcons = new Object[BrushNames.Length];
            for (int i = 0; i < BrushNames.Length; i++)
            {
                toolIcons[i] = UiStyleKit.LoadIconSprite($"{brushes}/{BrushNames[i]}.png");
            }

            UiThemeSceneSetup.SetReferenceArray(grid, "_toolIcons", toolIcons);
            GameUiBuilder.SetReference(grid, "_pairSymbols", GameUiBuilder.FindAsset<PairSymbolSet>(null));
        }

        /// <summary>关卡编辑器的网格（非只读）：确保网格所在区块左下角有坐标文字，并接到网格上。</summary>
        internal static void EnsureCoordinateText(EditorGridView grid, UiStyleKit.Context context)
        {
            var serialized = new SerializedObject(grid);
            Transform card = grid.transform.parent;
            if (serialized.FindProperty("_isReadOnly").boolValue || card == null)
            {
                return;
            }

            Transform existing = card.Find(CoordinateTextName);
            TMP_Text text = existing != null ? existing.GetComponent<TMP_Text>() : null;
            if (text == null)
            {
                text = GameUiBuilder.CreateText(card, CoordinateTextName, string.Empty, 22f, TextAlignmentOptions.MidlineLeft,
                    Vector2.zero, new Vector2(24f, 24f), new Vector2(200f, 44f));
                text.enableWordWrapping = false;
                Undo.RegisterCreatedObjectUndo(text.gameObject, UiStyleKit.UndoName);
                UiStyleKit.StyleText(text, UiColorRole.Text, card.GetComponentInParent<UiThemeScope>(true), context, false);
            }

            GameUiBuilder.SetReference(grid, "_coordinateText", text);
        }
    }
}
