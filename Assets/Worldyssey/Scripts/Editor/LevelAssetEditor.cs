using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// <see cref="LevelAsset"/> 的 Inspector：顶部列出校验结果，中间按世界画出地图预览，并提供导出 JSON。
    /// </summary>
    [CustomEditor(typeof(LevelAsset))]
    public class LevelAssetEditor : UnityEditor.Editor
    {
        private const float CellSize = 14f;
        private const float CellGap = 1f;
        private const int MaxPreviewSize = 64;
        private const string SourceFolder = "Assets/Worldyssey/Levels/Source";

        private static readonly Color FloorColor = new Color(0.55f, 0.55f, 0.52f);
        private static readonly Color SharedWallColor = new Color(0.18f, 0.19f, 0.22f);
        private static readonly Color UniqueWallColor = new Color(0.40f, 0.47f, 0.62f);
        private static readonly Color GoalColor = new Color(0.95f, 0.80f, 0.25f);
        private static readonly Color PedestalColor = new Color(0.30f, 0.72f, 0.75f);
        private static readonly Color MappedBoxColor = new Color(0.88f, 0.45f, 0.20f);
        private static readonly Color PlayerColor = new Color(0.20f, 0.50f, 0.95f);
        private static readonly Color PortalBoxColor = new Color(0.65f, 0.35f, 0.90f);

        private LevelValidationReport _report;
        private GUIStyle _cellLabelStyle;

        private void OnEnable()
        {
            Revalidate();
        }

        public override void OnInspectorGUI()
        {
            var asset = (LevelAsset)target;

            DrawReport();
            EditorGUILayout.Space();
            if (GUILayout.Button("导出 JSON…"))
            {
                Export(asset);
            }

            EditorGUILayout.Space();
            DrawPreview(asset.Level);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                Revalidate();
            }
        }

        private void Revalidate()
        {
            var asset = target as LevelAsset;
            _report = asset != null ? LevelValidator.Validate(asset.Level) : null;
        }

        private void DrawReport()
        {
            if (_report == null)
            {
                return;
            }

            if (_report.Issues.Count == 0)
            {
                EditorGUILayout.HelpBox("校验通过", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"校验结果：{_report.ErrorCount} 个错误，{_report.WarningCount} 个警告（世界编号从 0 开始）", EditorStyles.boldLabel);
            IReadOnlyList<LevelIssue> issues = _report.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                LevelIssue issue = issues[i];
                string location = issue.Position.HasValue ? $" @ {issue.Position.Value}" : string.Empty;
                EditorGUILayout.HelpBox(issue.Message + location, issue.IsError ? MessageType.Error : MessageType.Warning);
            }
        }

        private void DrawPreview(LevelData level)
        {
            if (level.Width <= 0 || level.Height <= 0 || level.Width > MaxPreviewSize || level.Height > MaxPreviewSize)
            {
                return;
            }

            _cellLabelStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };

            for (int world = 0; world < level.Worlds.Count; world++)
            {
                WorldData worldData = level.Worlds[world];
                string worldName = worldData != null ? worldData.Name : "(空)";
                string startMark = world == level.StartWorld ? "（起始）" : string.Empty;
                EditorGUILayout.LabelField($"世界 {world}：{worldName}{startMark}", EditorStyles.boldLabel);

                Rect area = GUILayoutUtility.GetRect(level.Width * CellSize, level.Height * CellSize, GUILayout.ExpandWidth(false));
                DrawWorld(level, world, area);
            }
        }

        private void DrawWorld(LevelData level, int world, Rect area)
        {
            IReadOnlyList<string> sharedRows = level.SharedRows;
            IReadOnlyList<string> wallRows = level.Worlds[world] != null ? level.Worlds[world].WallRows : null;

            for (int row = 0; row < level.Height; row++)
            {
                string shared = row < sharedRows.Count ? sharedRows[row] : null;
                string walls = wallRows != null && row < wallRows.Count ? wallRows[row] : null;
                for (int x = 0; x < level.Width; x++)
                {
                    char sharedCell = shared != null && x < shared.Length ? shared[x] : ' ';
                    bool isUniqueWall = walls != null && x < walls.Length && walls[x] == LevelMap.UniqueWallChar;
                    EditorGUI.DrawRect(CellRect(area, x, row), TerrainColor(sharedCell, isUniqueWall));
                }
            }

            IReadOnlyList<GridPos> boxes = level.MappedBoxes;
            for (int i = 0; i < boxes.Count; i++)
            {
                DrawMarker(area, level.Height, boxes[i], MappedBoxColor, string.Empty);
            }

            IReadOnlyList<PortalBoxData> portals = level.PortalBoxes;
            for (int i = 0; i < portals.Count; i++)
            {
                if (portals[i] != null && portals[i].World == world)
                {
                    DrawMarker(area, level.Height, portals[i].Position, PortalBoxColor, portals[i].Pair.ToString());
                }
            }

            if (world == level.StartWorld)
            {
                DrawMarker(area, level.Height, level.Player.Position, PlayerColor, "@");
            }
        }

        private void DrawMarker(Rect area, int height, GridPos pos, Color color, string label)
        {
            int row = height - 1 - pos.Y;
            Rect cell = CellRect(area, pos.X, row);
            if (!area.Contains(cell.center))
            {
                return;
            }

            var inner = new Rect(cell.x + 2f, cell.y + 2f, cell.width - 4f, cell.height - 4f);
            EditorGUI.DrawRect(inner, color);
            if (!string.IsNullOrEmpty(label))
            {
                GUI.Label(cell, label, _cellLabelStyle);
            }
        }

        private static Rect CellRect(Rect area, int x, int row)
        {
            return new Rect(area.x + x * CellSize, area.y + row * CellSize, CellSize - CellGap, CellSize - CellGap);
        }

        private static Color TerrainColor(char sharedCell, bool isUniqueWall)
        {
            if (sharedCell == LevelMap.SharedWallChar)
            {
                return SharedWallColor;
            }

            if (isUniqueWall)
            {
                return UniqueWallColor;
            }

            switch (sharedCell)
            {
                case LevelMap.PedestalChar:
                    return PedestalColor;
                case LevelMap.GoalChar:
                    return GoalColor;
                default:
                    return FloorColor;
            }
        }

        private void Export(LevelAsset asset)
        {
            if (_report != null && _report.HasErrors
                && !EditorUtility.DisplayDialog("导出关卡", "关卡有校验错误，导出的文件将无法导入。仍要导出吗？", "导出", "取消"))
            {
                return;
            }

            string fileName = LevelJsonImporter.ToSafeFileName(asset.Level.Id);
            string path = EditorUtility.SaveFilePanel("导出关卡 JSON", SourceFolder, fileName, "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            // 不带 BOM，方便在其他工具里比对
            File.WriteAllText(path, LevelJsonSerializer.ToJson(asset.Level), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log($"[{nameof(LevelAssetEditor)}] 已导出：{path}", asset);
        }
    }
}
