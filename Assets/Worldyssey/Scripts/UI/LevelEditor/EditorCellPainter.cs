using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 算出编辑器网格每一格的显示内容：地形色块和图标、实体图标、其他世界的叠影，以及画笔悬停预览的颜色。
    /// 配色来自 <see cref="EditorGridPalette"/>；独有墙用世界的季节色，传送箱子用目标世界的季节色。
    /// </summary>
    internal sealed class EditorCellPainter
    {
        private const string PortalLabelPrefix = "P";
        private const float GhostAlpha = 0.8f;
        private static readonly Color Hidden = new Color(0f, 0f, 0f, 0f);

        private readonly EditorGridPalette _palette;
        private readonly WorldPalette _worldPalette;
        private readonly EditorGridIconSet _icons;

        public EditorCellPainter(EditorGridPalette palette, WorldPalette worldPalette, EditorGridIconSet icons)
        {
            _palette = palette;
            _worldPalette = worldPalette;
            _icons = icons;
        }

        public EditorCellVisual Paint(LevelDraft draft, int world, GridPos pos, Color frame)
        {
            var visual = new EditorCellVisual
            {
                Frame = frame,
                Terrain = TerrainColor(draft, world, pos),
                TerrainIcon = TerrainIcon(draft, world, pos),
                Entity = Hidden,
                Label = string.Empty,
            };
            visual.TerrainIconColor = IconColorOn(visual.Terrain);

            int portal = draft.FindPortal(world, pos);
            if (world == draft.StartWorld && pos == draft.PlayerPosition)
            {
                // 朝向按 上、右、下、左 排列，图标尖端朝上，顺时针每档转 90°（UI 中顺时针为负角度）
                visual.Entity = _palette.Player;
                visual.EntityIcon = _icons.Player;
                visual.EntityAngle = -90f * (int)draft.PlayerFacing;
            }
            else if (draft.FindMappedBox(pos) >= 0)
            {
                visual.Entity = _palette.MappedBox;
                visual.EntityIcon = _icons.Box;
            }
            else if (portal >= 0)
            {
                int pairIndex = draft.Portals[portal].Pair;
                visual.Entity = SeasonColor(draft, TargetWorld(draft, pairIndex, world), _palette.PortalBoxFallback);
                visual.EntityIcon = _icons.Portal;
                visual.PairIcon = _icons.PairSymbols != null ? _icons.PairSymbols.GetSymbol(pairIndex) : null;
                visual.PairIconColor = IconColorOn(visual.Entity);

                // 有符号时用符号，没有配置符号时退回编号文字
                visual.Label = visual.PairIcon != null ? string.Empty : PortalLabelPrefix + pairIndex;
            }

            // 格子上有实体时隐藏地形图标，只留地形底色（如终点的黄色），避免两个图标叠在一起看不清
            if (visual.Entity.a > 0f)
            {
                visual.TerrainIcon = null;
            }

            PaintGhost(draft, world, pos, ref visual);
            return visual;
        }

        /// <summary>画笔在当前世界画下去后的颜色，用于悬停预览。橡皮擦显示为空地。</summary>
        public Color PreviewColor(LevelDraft draft, int world, EditorTool tool, int selectedPair)
        {
            switch (tool)
            {
                case EditorTool.SharedWall:
                    return _palette.SharedWall;
                case EditorTool.Pedestal:
                    return _palette.Pedestal;
                case EditorTool.Goal:
                    return _palette.Goal;
                case EditorTool.UniqueWall:
                    return SeasonColor(draft, world, _palette.UniqueWallFallback);
                case EditorTool.MappedBox:
                    return _palette.MappedBox;
                case EditorTool.Player:
                    return _palette.Player;
                case EditorTool.PortalBox:
                    return selectedPair >= 0 && selectedPair < draft.Pairs.Count
                        ? SeasonColor(draft, TargetWorld(draft, selectedPair, world), _palette.PortalBoxFallback)
                        : _palette.PortalBoxFallback;
                default:
                    return _palette.Floor;
            }
        }

        /// <summary>
        /// 图标用底色的深浅变化：浅色底上画深一档，深色底上画浅一档，和色块同色系又看得清。
        /// </summary>
        public static Color IconColorOn(Color background)
        {
            float luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
            Color icon = luminance > 0.5f ? background * 0.55f : Color.Lerp(background, Color.white, 0.55f);
            icon.a = 1f;
            return icon;
        }

        /// <summary>
        /// 其他世界在这一格有独有墙或传送箱子时，在角上画一个对应世界季节色的小图标。
        /// 当前世界这一格本身是墙时不画（反正走不进去，也不会冲突）。
        /// </summary>
        private void PaintGhost(LevelDraft draft, int world, GridPos pos, ref EditorCellVisual visual)
        {
            if (draft.GetSharedCell(pos) == SharedCell.Wall || draft.IsUniqueWall(world, pos))
            {
                return;
            }

            for (int other = 0; other < draft.WorldCount; other++)
            {
                if (other == world)
                {
                    continue;
                }

                if (draft.IsUniqueWall(other, pos))
                {
                    SetGhost(ref visual, _icons.UniqueWall, SeasonColor(draft, other, _palette.UniqueWallFallback));
                    return;
                }

                int portal = draft.FindPortal(other, pos);
                if (portal >= 0)
                {
                    SetGhost(ref visual, _icons.Portal, SeasonColor(draft, other, _palette.PortalBoxFallback));
                    return;
                }
            }
        }

        private static void SetGhost(ref EditorCellVisual visual, Sprite icon, Color color)
        {
            color.a = GhostAlpha;
            visual.GhostIcon = icon;
            visual.GhostColor = color;
        }

        private static int TargetWorld(LevelDraft draft, int pairIndex, int world)
        {
            DraftPair pair = draft.Pairs[pairIndex];
            return pair.WorldA == world ? pair.WorldB : pair.WorldA;
        }

        private Color TerrainColor(LevelDraft draft, int world, GridPos pos)
        {
            SharedCell shared = draft.GetSharedCell(pos);
            if (shared == SharedCell.Wall)
            {
                return _palette.SharedWall;
            }

            if (draft.IsUniqueWall(world, pos))
            {
                return SeasonColor(draft, world, _palette.UniqueWallFallback);
            }

            switch (shared)
            {
                case SharedCell.Pedestal:
                    return _palette.Pedestal;
                case SharedCell.Goal:
                    return _palette.Goal;
                default:
                    return _palette.Floor;
            }
        }

        private Sprite TerrainIcon(LevelDraft draft, int world, GridPos pos)
        {
            SharedCell shared = draft.GetSharedCell(pos);
            if (shared == SharedCell.Wall)
            {
                return _icons.Wall;
            }

            if (draft.IsUniqueWall(world, pos))
            {
                return _icons.UniqueWall;
            }

            switch (shared)
            {
                case SharedCell.Pedestal:
                    return _icons.Pedestal;
                case SharedCell.Goal:
                    return _icons.Goal;
                default:
                    return null;
            }
        }

        private Color SeasonColor(LevelDraft draft, int world, Color fallback)
        {
            return world >= 0 && world < draft.WorldCount && _worldPalette.TryGetColor((int)draft.GetWorldTheme(world), out Color color)
                ? color
                : fallback;
        }
    }
}
