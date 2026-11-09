using UnityEngine;
using UnityEngine.Rendering;

namespace Worldyssey.View
{
    /// <summary>
    /// 传送转场里显示截图的两层：与箱子同大的外壳（截图贴在它看得见的几个面上），以及正对相机、铺满起始画面的外圈平面。
    /// 两层用同一个起始机位投影截图，内容无缝衔接；外圈先淡出，截图就只剩在箱子轮廓里，跟着箱子等比缩小，不会变形。
    /// </summary>
    internal sealed class SnapshotPhoto
    {
        private static readonly int SnapshotTexId = Shader.PropertyToID("_SnapshotTex");
        private static readonly int SnapshotVpId = Shader.PropertyToID("_SnapshotVP");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int TintStrengthId = Shader.PropertyToID("_TintStrength");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        private readonly Transform _shell;
        private readonly Transform _outer;
        private readonly Material _shellMaterial;
        private readonly Material _outerMaterial;

        /// <summary>按 <paramref name="source"/>（SnapshotProjection 材质）各复制一份材质，不修改资源本身。</summary>
        public SnapshotPhoto(Transform parent, Material source)
        {
            _shellMaterial = new Material(source) { name = source.name + " (Shell)" };
            _outerMaterial = new Material(source) { name = source.name + " (Outer)" };

            // 两层重叠的部分内容相同，外圈后画只是为了顺序确定
            _outerMaterial.renderQueue = source.renderQueue + 1;
            _shell = CreateLayer(parent, "SnapshotShell", PrimitiveType.Cube, _shellMaterial);

            // Unity 的 Quad 正面朝 -Z，旋转到相机朝向后正好面对相机
            _outer = CreateLayer(parent, "SnapshotOuter", PrimitiveType.Quad, _outerMaterial);
        }

        /// <summary>
        /// 显示两层。<paramref name="viewProjection"/> 是起始机位的投影×视图矩阵，<paramref name="outerSize"/> 是起始画面在外壳中心处的宽高。
        /// </summary>
        public void Show(Bounds shell, Quaternion cameraRotation, Vector2 outerSize, Texture snapshot, Matrix4x4 viewProjection, Color tint)
        {
            _shell.SetPositionAndRotation(shell.center, Quaternion.identity);
            _shell.localScale = shell.size;
            _outer.SetPositionAndRotation(shell.center, cameraRotation);
            _outer.localScale = new Vector3(outerSize.x, outerSize.y, 1f);
            SetupMaterial(_shellMaterial, snapshot, viewProjection, tint);
            SetupMaterial(_outerMaterial, snapshot, viewProjection, tint);
            _shell.gameObject.SetActive(true);
            _outer.gameObject.SetActive(true);
        }

        /// <summary>分别设置外壳和外圈的不透明度；季节色强度随各自的不透明度一起淡出。</summary>
        public void SetFade(float shellAlpha, float outerAlpha, float tintStrength)
        {
            _shellMaterial.SetFloat(AlphaId, shellAlpha);
            _shellMaterial.SetFloat(TintStrengthId, tintStrength * shellAlpha);
            _outerMaterial.SetFloat(AlphaId, outerAlpha);
            _outerMaterial.SetFloat(TintStrengthId, tintStrength * outerAlpha);
            _outer.gameObject.SetActive(outerAlpha > 0f);
        }

        public void Hide()
        {
            _shell.gameObject.SetActive(false);
            _outer.gameObject.SetActive(false);
        }

        public void Destroy()
        {
            Object.Destroy(_shellMaterial);
            Object.Destroy(_outerMaterial);
        }

        private static Transform CreateLayer(Transform parent, string name, PrimitiveType type, Material material)
        {
            GameObject layer = GameObject.CreatePrimitive(type);
            layer.name = name;
            Object.Destroy(layer.GetComponent<Collider>());
            layer.transform.SetParent(parent, false);
            var renderer = layer.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            layer.SetActive(false);
            return layer.transform;
        }

        private static void SetupMaterial(Material material, Texture snapshot, Matrix4x4 viewProjection, Color tint)
        {
            material.SetTexture(SnapshotTexId, snapshot);
            material.SetMatrix(SnapshotVpId, viewProjection);
            material.SetColor(TintId, tint);
        }
    }
}
