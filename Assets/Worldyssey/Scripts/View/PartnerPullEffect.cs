using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.View
{
    /// <summary>
    /// 传送后回程箱子（伙伴箱子）在目标世界底座上“合成”的表现：
    /// 新生成（<see cref="PartnerSpawnedEvent"/>）时，6 块缩小的箱子从四周的一圈聚拢到中心；
    /// 被拉过来（<see cref="PartnerMovedEvent"/>）时，箱子在原位置碎成 6 块，沿弧线飞到底座再聚拢。
    /// 碎块排成整齐的一圈、同时出发同时到达、一起转同一个角度，避免杂乱；合成后真箱子出现并压一下。
    /// 碎块直接用箱体的网格和材质绘制，不创建物体。只是表现，不影响输入；撤销、重开时由 <see cref="StepPlayer"/> 取消。
    /// </summary>
    public class PartnerPullEffect : MonoBehaviour
    {
        private const int PieceCount = 6;
        private const float FadeInPortion = 0.15f;
        private const float SpinDegrees = 90f;

        // 被拉过来时：碎块在原位置组成箱子的那一圈半径、途中散开的幅度（格）
        private const float PulledStartRadius = 0.22f;
        private const float PulledBulge = 0.35f;

        [SerializeField] private GameplayConfig _gameplayConfig;
        [SerializeField] private LevelView _levelView;

        private readonly Vector3[] _directions = new Vector3[PieceCount];
        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _routine;
        private PortalBoxView _hiddenBox;
        private bool _hasReferences;

        private void Awake()
        {
            _hasReferences = CheckReference(_gameplayConfig, nameof(_gameplayConfig)) & CheckReference(_levelView, nameof(_levelView));
            _propertyBlock = new MaterialPropertyBlock();

            // 碎块方向固定为水平面上均分的一圈，每次播放都一样整齐
            for (int i = 0; i < PieceCount; i++)
            {
                float angle = (i + 0.5f) * (360f / PieceCount) * Mathf.Deg2Rad;
                _directions[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }
        }

        private void OnDisable()
        {
            Cancel();
        }

        /// <summary>这一步是否会让回程箱子出现在底座上（新生成或被拉过来），即是否会播放合成。</summary>
        public static bool HasPartnerArrival(StepResult result)
        {
            if (result == null)
            {
                return false;
            }

            for (int i = 0; i < result.Events.Count; i++)
            {
                if (result.Events[i] is PartnerSpawnedEvent || result.Events[i] is PartnerMovedEvent)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 回程箱子新生成或被拉过来时，立即隐藏它，等 <paramref name="delay"/> 秒后播放合成；没有这类事件时什么都不做。
        /// 调用时世界已经切换、箱子已在底座上。
        /// </summary>
        public void TryPlay(StepResult result, float delay)
        {
            Cancel();
            if (!_hasReferences || result == null)
            {
                return;
            }

            for (int i = 0; i < result.Events.Count; i++)
            {
                if (result.Events[i] is PartnerSpawnedEvent spawned)
                {
                    Begin(spawned.PortalIndex, null, delay);
                    return;
                }

                if (result.Events[i] is PartnerMovedEvent moved)
                {
                    Begin(moved.PortalIndex, moved.From, delay);
                    return;
                }
            }
        }

        /// <summary>立即停止，碎块消失，箱子直接显示在终点。</summary>
        public void Cancel()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            RevealBox();
        }

        private void Begin(int portalIndex, GridPos? from, float delay)
        {
            PortalBoxView box = _levelView.GetPortalBoxView(portalIndex);
            Renderer body = box != null ? box.BodyRenderer : null;
            MeshFilter filter = body != null ? body.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null)
            {
                Debug.LogError($"[{nameof(PartnerPullEffect)}] 传送箱子的箱体不是普通网格渲染器，无法播放合成", this);
                return;
            }

            // 在隐藏前记下箱子中心和网格相对中心的变换：碎块按“以箱子中心为原点”的矩阵缩放、旋转
            Vector3 center = body.bounds.center;
            Transform root = box.transform;
            Matrix4x4 centerFrame = Matrix4x4.TRS(center, root.rotation, root.lossyScale);
            Matrix4x4 meshInCenter = centerFrame.inverse * body.localToWorldMatrix;
            Vector3 start = from.HasValue ? _levelView.GridToWorld(from.Value) + (center - root.position) : center;
            body.GetPropertyBlock(_propertyBlock);

            box.SetBodyVisible(false);
            _hiddenBox = box;
            _routine = StartCoroutine(PlayRoutine(filter.sharedMesh, body, meshInCenter, root, start, center, from.HasValue, delay));
        }

        private IEnumerator PlayRoutine(Mesh mesh, Renderer body, Matrix4x4 meshInCenter, Transform root,
            Vector3 start, Vector3 end, bool isPulled, float delay)
        {
            float elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            float cell = _levelView.CellSize;
            float duration = isPulled ? _gameplayConfig.PartnerPullDuration : _gameplayConfig.AssembleDuration;
            Vector3 peak = (start + end) * 0.5f + Vector3.up * (_gameplayConfig.PartnerPullArcHeight * cell);
            Vector2 pieceScale = _gameplayConfig.AssemblePieceScale;
            Material[] materials = body.sharedMaterials;
            int layer = body.gameObject.layer;

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                Vector3 center;
                float radius;
                float height;
                float scale = Mathf.Lerp(isPulled ? pieceScale.y : pieceScale.x, pieceScale.y, eased);
                if (isPulled)
                {
                    // 沿弧线飞行，途中略微散开，到达时收拢
                    float u = 1f - eased;
                    center = u * u * start + 2f * u * eased * peak + eased * eased * end;
                    radius = PulledStartRadius * (1f - eased) + PulledBulge * Mathf.Sin(Mathf.PI * eased);
                    height = 0f;
                }
                else
                {
                    // 从四周的一圈聚拢，开头由小变大地出现
                    center = end;
                    radius = _gameplayConfig.AssembleRadius * (1f - eased);
                    height = _gameplayConfig.AssembleHeight * (1f - eased);
                    scale *= Mathf.Clamp01(progress / FadeInPortion);
                }

                Quaternion rotation = root.rotation * Quaternion.Euler(0f, SpinDegrees * eased, 0f);
                DrawPieces(mesh, materials, layer, meshInCenter, root.lossyScale * scale, rotation, center, radius * cell, height * cell);
                yield return null;
            }

            _routine = null;
            RevealBox();
            // 合成完成，真箱子出现并压一下，像落了地
            if (root.TryGetComponent(out EntityView view))
            {
                view.PlaySquash(Vector3.up, _gameplayConfig.SnapshotBoxSquash, _gameplayConfig.SnapshotBoxSquashDuration);
            }
        }

        private void DrawPieces(Mesh mesh, Material[] materials, int layer, Matrix4x4 meshInCenter, Vector3 scale,
            Quaternion rotation, Vector3 center, float radius, float height)
        {
            for (int i = 0; i < PieceCount; i++)
            {
                Vector3 position = center + _directions[i] * radius + Vector3.up * height;
                Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale) * meshInCenter;
                for (int sub = 0; sub < materials.Length && sub < mesh.subMeshCount; sub++)
                {
                    Graphics.DrawMesh(mesh, matrix, materials[sub], layer, null, sub, _propertyBlock, ShadowCastingMode.On, true);
                }
            }
        }

        private void RevealBox()
        {
            if (_hiddenBox != null)
            {
                _hiddenBox.SetBodyVisible(true);
                _hiddenBox = null;
            }
        }

        private bool CheckReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(PartnerPullEffect)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
