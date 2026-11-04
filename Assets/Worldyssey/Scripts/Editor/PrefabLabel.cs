using System;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 调色板报告里的一个模型：名称（季节/预制体名）和类别（草、地面、墙、树、装饰）。
    /// </summary>
    internal readonly struct PrefabLabel : IEquatable<PrefabLabel>
    {
        public readonly string Name;
        public readonly string Category;

        public PrefabLabel(string name, string category)
        {
            Name = name;
            Category = category;
        }

        public bool Equals(PrefabLabel other)
        {
            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            return obj is PrefabLabel other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }
    }
}
