using System;
using System.Collections.Generic;
using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 每帧把玩家和箱子的实时位置（补间中的表现位置）传给草着色器，让草被推开、压扁。
    /// 物体每移动一段距离留下一个压痕点，压痕在回弹时间内逐渐减弱，草像被踩过后慢慢立起来。
    /// 用全局着色器数组传递，草合并成一个网格后依然有效。
    /// </summary>
    public class GrassInteractorFeeder : MonoBehaviour
    {
        /// <summary>与 GrassCommon.hlsl 里的 GRASS_MAX_INTERACTORS 保持一致。</summary>
        public const int MaxInteractors = 32;

        private const int MaxTrailPoints = 64;
        private static readonly int InteractorsId = Shader.PropertyToID("_GrassInteractors");
        private static readonly int StrengthsId = Shader.PropertyToID("_GrassInteractorStrengths");
        private static readonly int CountId = Shader.PropertyToID("_GrassInteractorCount");

        [SerializeField] private LevelView _levelView;
        [SerializeField] private LevelVisualConfig _visualConfig;

        private readonly Vector4[] _interactors = new Vector4[MaxInteractors];
        private readonly float[] _strengths = new float[MaxInteractors];
        private readonly TrailPoint[] _trail = new TrailPoint[MaxTrailPoints];
        private readonly Dictionary<Transform, Vector3> _lastDropped = new Dictionary<Transform, Vector3>();
        private int _trailHead;
        private bool _hasReferences;

        private void Awake()
        {
            _hasReferences = CheckReference(_levelView, nameof(_levelView)) & CheckReference(_visualConfig, nameof(_visualConfig));
        }

        private void OnEnable()
        {
            if (_hasReferences)
            {
                _levelView.ShownWorldChanged += HandleShownWorldChanged;
            }
        }

        private void LateUpdate()
        {
            // 实体每帧都可能在补间中移动，位置只能逐帧读取；数量很少，且不分配内存
            if (!_hasReferences)
            {
                return;
            }

            EntityView player = _levelView.PlayerView;
            if (player == null)
            {
                Shader.SetGlobalFloat(CountId, 0f);
                return;
            }

            float now = Time.time;
            int count = AddCurrent(player.transform, _visualConfig.GrassPlayerRadius, 0, now);
            for (int i = 0; i < _levelView.MappedBoxViewCount; i++)
            {
                count = AddCurrent(_levelView.GetMappedBoxView(i).transform, _visualConfig.GrassBoxRadius, count, now);
            }

            for (int i = 0; i < _levelView.PortalBoxViewCount; i++)
            {
                PortalBoxView portal = _levelView.GetPortalBoxView(i);
                if (portal.gameObject.activeInHierarchy)
                {
                    count = AddCurrent(portal.transform, _visualConfig.GrassBoxRadius, count, now);
                }
            }

            count = AddTrail(count, now);
            Shader.SetGlobalVectorArray(InteractorsId, _interactors);
            Shader.SetGlobalFloatArray(StrengthsId, _strengths);
            Shader.SetGlobalFloat(CountId, count);
        }

        private void OnDisable()
        {
            if (_hasReferences)
            {
                _levelView.ShownWorldChanged -= HandleShownWorldChanged;
            }

            Shader.SetGlobalFloat(CountId, 0f);
        }

        private void HandleShownWorldChanged(int world)
        {
            // 每个世界的草是独立的一套，上一个世界的压痕不应该出现在新世界里
            Array.Clear(_trail, 0, _trail.Length);
            _trailHead = 0;
            _lastDropped.Clear();
        }

        private int AddCurrent(Transform entity, float radius, int count, float now)
        {
            Vector3 position = entity.position;
            if (count < MaxInteractors)
            {
                _interactors[count] = new Vector4(position.x, position.y, position.z, radius);
                _strengths[count] = 1f;
                count++;
            }

            float spacing = _visualConfig.GrassTrailSpacing;
            if (!_lastDropped.TryGetValue(entity, out Vector3 last) || (position - last).sqrMagnitude >= spacing * spacing)
            {
                _trail[_trailHead] = new TrailPoint(position, radius, now);
                _trailHead = (_trailHead + 1) % MaxTrailPoints;
                _lastDropped[entity] = position;
            }

            return count;
        }

        /// <summary>
        /// 从最新的压痕往回填，直到名额用完或压痕已经完全回弹。
        /// </summary>
        private int AddTrail(int count, float now)
        {
            float duration = _visualConfig.GrassRecoverDuration;
            for (int k = 0; k < MaxTrailPoints && count < MaxInteractors; k++)
            {
                TrailPoint point = _trail[(_trailHead - 1 - k + MaxTrailPoints) % MaxTrailPoints];
                float age = now - point.Time;
                if (!point.IsSet || age >= duration)
                {
                    // 环形缓冲从新到旧排列，遇到空位或已回弹的，后面只会更旧
                    break;
                }

                _interactors[count] = new Vector4(point.Position.x, point.Position.y, point.Position.z, point.Radius);
                _strengths[count] = 1f - age / duration;
                count++;
            }

            return count;
        }

        private bool CheckReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(GrassInteractorFeeder)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }

        private readonly struct TrailPoint
        {
            public readonly Vector3 Position;
            public readonly float Radius;
            public readonly float Time;
            public readonly bool IsSet;

            public TrailPoint(Vector3 position, float radius, float time)
            {
                Position = position;
                Radius = radius;
                Time = time;
                IsSet = true;
            }
        }
    }
}
