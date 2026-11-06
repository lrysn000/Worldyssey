using UnityEngine;

namespace Worldyssey.UI
{
    /// <summary>
    /// 编辑器网格一格的显示内容，由 <see cref="EditorGridView"/> 算好后交给 <see cref="EditorGridCell"/>。
    /// 图标为空表示不显示；实体颜色 alpha 为 0 表示没有实体。
    /// </summary>
    public struct EditorCellVisual
    {
        public Color Frame;
        public Color Terrain;
        public Sprite TerrainIcon;
        public Color TerrainIconColor;
        public Color Entity;
        public Sprite EntityIcon;

        /// <summary>实体图标绕 Z 轴的角度，用来表示玩家朝向。</summary>
        public float EntityAngle;

        /// <summary>其他世界在这一格的独有墙或传送箱子，画成角上的淡色小图标。</summary>
        public Sprite GhostIcon;
        public Color GhostColor;

        public string Label;
    }
}
