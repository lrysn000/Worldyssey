using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.Data
{
    /// <summary>
    /// 区分传送箱子配对的符号：第 n 对用第 n 个符号，同一对的两个箱子印同一个符号。
    /// 符号是白色像素图，显示时乘上颜色：3D 箱子顶面用 <see cref="BoxSymbolColor"/>（默认白色），编辑器里用格子底色的对比色。
    /// </summary>
    [CreateAssetMenu(fileName = "PairSymbolSet", menuName = "Worldyssey/Pair Symbol Set")]
    public class PairSymbolSet : ScriptableObject
    {
        [Tooltip("按配对编号排列的符号。数量应等于配对上限 LevelLimits.MaxPairCount")]
        [SerializeField] private Sprite[] _symbols = new Sprite[LevelLimits.MaxPairCount];

        [Tooltip("3D 箱子顶面符号和传送拖尾的颜色")]
        [SerializeField] private Color _boxSymbolColor = Color.white;

        public Color BoxSymbolColor => _boxSymbolColor;

        /// <summary>配对 <paramref name="pairIndex"/> 的符号。超出数量时循环使用，编号非法或没有符号时返回 null。</summary>
        public Sprite GetSymbol(int pairIndex)
        {
            return pairIndex >= 0 && _symbols != null && _symbols.Length > 0 ? _symbols[pairIndex % _symbols.Length] : null;
        }

    }
}
