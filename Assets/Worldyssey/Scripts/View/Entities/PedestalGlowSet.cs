using System.Collections.Generic;
using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.View
{
    /// <summary>
    /// 底座的激活发光：当前世界里停着传送箱子的底座，按箱子通往的世界颜色发光，其余底座不亮。
    /// 底座有名为 <see cref="PatternChildName"/> 的刻纹子物体时只让刻纹发光，没有时整个底座发光。
    /// 用 MaterialPropertyBlock 改 _EmissionColor，不修改共享材质；颜色没变时不重新设置。
    /// </summary>
    public sealed class PedestalGlowSet
    {
        /// <summary>底座顶面刻纹子物体的名字。</summary>
        public const string PatternChildName = "Pattern";

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly List<GridPos> _positions = new List<GridPos>();
        private readonly List<Renderer[]> _renderers = new List<Renderer[]>();
        private readonly List<Color> _currentColors = new List<Color>();
        private readonly List<Color> _targetColors = new List<Color>();
        private MaterialPropertyBlock _propertyBlock;

        /// <summary>登记一个底座。只在生成关卡时调用，这里的数组分配不在每帧发生。</summary>
        public void Add(GridPos position, GameObject pedestal)
        {
            _positions.Add(position);
            Transform pattern = pedestal.transform.Find(PatternChildName);
            GameObject glowing = pattern != null ? pattern.gameObject : pedestal;
            _renderers.Add(glowing.GetComponentsInChildren<Renderer>(true));
            _currentColors.Add(Color.black);
            _targetColors.Add(Color.black);
        }

        public void Clear()
        {
            _positions.Clear();
            _renderers.Clear();
            _currentColors.Clear();
            _targetColors.Clear();
        }

        /// <summary>开始一轮刷新：先把所有底座的目标设为不亮，再用 <see cref="Light"/> 点亮需要亮的。</summary>
        public void ResetTargets()
        {
            for (int i = 0; i < _targetColors.Count; i++)
            {
                _targetColors[i] = Color.black;
            }
        }

        /// <summary>让 <paramref name="position"/> 上的底座以 <paramref name="emission"/> 发光。该格没有底座时忽略。</summary>
        public void Light(GridPos position, Color emission)
        {
            // 一关最多几个底座，线性查找比建字典更省
            for (int i = 0; i < _positions.Count; i++)
            {
                if (_positions[i] == position)
                {
                    _targetColors[i] = emission;
                    return;
                }
            }
        }

        /// <summary>把目标颜色应用到渲染器，只处理有变化的底座。</summary>
        public void Commit()
        {
            for (int i = 0; i < _positions.Count; i++)
            {
                if (_targetColors[i] == _currentColors[i])
                {
                    continue;
                }

                _currentColors[i] = _targetColors[i];
                Apply(_renderers[i], _targetColors[i]);
            }
        }

        private void Apply(Renderer[] renderers, Color emission)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                // 先读回已有的属性块，保留积雪遮罩（SnowExclusion）等其他组件写入的值
                renderers[i].GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(EmissionColorId, emission);
                renderers[i].SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
