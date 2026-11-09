using Worldyssey.Core;

namespace Worldyssey.View
{
    /// <summary>
    /// 由格子坐标、世界编号和用途决定种子的伪随机数。同一关每次生成的变体、朝向、草的位置完全一样，
    /// 撤销、重开、重进关卡时画面不会跳变。
    /// </summary>
    public struct CellRandom
    {
        private uint _state;

        public CellRandom(GridPos pos, int world, int salt)
        {
            // 先把几个输入混合成种子，再过一遍 murmur3 的收尾混合，避免相邻格子得到相近的序列
            uint seed = (uint)pos.X * 73856093u ^ (uint)pos.Y * 19349663u ^ (uint)(world + 1) * 83492791u ^ (uint)salt * 2654435761u;
            seed ^= seed >> 16;
            seed *= 0x85EBCA6Bu;
            seed ^= seed >> 13;
            seed *= 0xC2B2AE35u;
            seed ^= seed >> 16;
            _state = seed != 0 ? seed : 0x9E3779B9u;
        }

        /// <summary>[0, <paramref name="count"/>) 内的整数；count 不大于 0 时返回 0。</summary>
        public int Range(int count)
        {
            return count <= 0 ? 0 : (int)(Next() % (uint)count);
        }

        /// <summary>[<paramref name="min"/>, <paramref name="max"/>) 内的浮点数。</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * Value();
        }

        /// <summary>[0, 1) 内的浮点数。</summary>
        public float Value()
        {
            return (Next() & 0xFFFFFFu) / 16777216f;
        }

        private uint Next()
        {
            // xorshift32
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }
    }
}
