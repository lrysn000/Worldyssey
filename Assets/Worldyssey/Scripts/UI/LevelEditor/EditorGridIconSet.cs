using UnityEngine;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>编辑器网格用到的图标（画笔那一套），由 <see cref="EditorGridView"/> 从序列化字段组装。</summary>
    internal readonly struct EditorGridIconSet
    {
        public EditorGridIconSet(Sprite wall, Sprite uniqueWall, Sprite pedestal, Sprite goal, Sprite player, Sprite box, Sprite portal, PairSymbolSet pairSymbols)
        {
            Wall = wall;
            UniqueWall = uniqueWall;
            Pedestal = pedestal;
            Goal = goal;
            Player = player;
            Box = box;
            Portal = portal;
            PairSymbols = pairSymbols;
        }

        public Sprite Wall { get; }
        public Sprite UniqueWall { get; }
        public Sprite Pedestal { get; }
        public Sprite Goal { get; }
        public Sprite Player { get; }
        public Sprite Box { get; }
        public Sprite Portal { get; }

        /// <summary>可能为空：没有配置时传送箱子显示编号文字。</summary>
        public PairSymbolSet PairSymbols { get; }
    }
}
