using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 生成四季的天气粒子预制体：春花瓣、夏萤火虫、秋落叶、冬雪。只是默认起点，生成后在预制体上直接调。
    /// 已存在的预制体不会覆盖。放在 Prefabs/Effects，不放进 Environment，免得被当成外围装饰收进季节组。
    /// </summary>
    internal static class SeasonParticleCreator
    {
        private const string PrefabFolder = "Assets/Worldyssey/Prefabs/Effects";
        private const string ParticleMaterialPath = "Assets/Worldyssey/Materials/Placeholder/M_Particle.mat";
        private const string LeafPrefabPath = "Assets/Worldyssey/Packages/CS_StylizedPlantPack/Prefabs/Leaves/Leaf_1.prefab";

        // 覆盖范围：以关卡中心为原点的正方形，足够盖住常见尺寸的关卡和外围两圈
        private const float AreaSize = 26f;

        /// <summary>
        /// 生成（或读取已有的）四季粒子，按 春、夏、秋、冬 顺序返回。
        /// </summary>
        internal static ParticleSystem[] CreateAll()
        {
            AssetFolderUtility.EnsureFolder(PrefabFolder);
            var material = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
            if (material == null)
            {
                Debug.LogWarning($"[{nameof(SeasonParticleCreator)}] 找不到 {ParticleMaterialPath}，请先运行 Setup Visual Effects；粒子会用默认材质");
            }

            return new[]
            {
                GetOrCreate("P_Season_Spring", material, ConfigurePetals),
                GetOrCreate("P_Season_Summer", material, ConfigureFireflies),
                GetOrCreate("P_Season_Autumn", material, ConfigureLeaves),
                GetOrCreate("P_Season_Winter", material, ConfigureSnow),
            };
        }

        private static ParticleSystem GetOrCreate(string prefabName, Material material, System.Action<ParticleSystem> configure)
        {
            string path = $"{PrefabFolder}/{prefabName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<ParticleSystem>(path);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject(prefabName);
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.prewarm = true;
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(AreaSize, 0.1f, AreaSize);
            var particleRenderer = root.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            configure(particles);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<ParticleSystem>();
        }

        private static void ConfigurePetals(ParticleSystem particles)
        {
            particles.transform.localPosition = Vector3.up * 7f;
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = 14f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.72f, 0.8f), new Color(1f, 0.95f, 0.97f));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.02f;
            main.maxParticles = 400;
            SetRate(particles, 20f);
            SetDrift(particles, 0.3f, 0.4f);
            ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        }

        private static void ConfigureFireflies(ParticleSystem particles)
        {
            // 贴近地面漂浮；颜色超过 1，配合泛光发亮
            particles.transform.localPosition = Vector3.up * 1f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.scale = new Vector3(AreaSize * 0.6f, 1.5f, AreaSize * 0.6f);
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = 6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.08f);
            main.startColor = new Color(0.8f, 1f, 0.4f) * 4f;
            main.maxParticles = 80;
            SetRate(particles, 6f);
            SetDrift(particles, 0f, 0.5f);

            // 不透明材质没有透明度，用大小的升降来表现闪烁
            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f)));
        }

        private static void ConfigureLeaves(ParticleSystem particles)
        {
            particles.transform.localPosition = Vector3.up * 7f;
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = 14f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.55f, 0.15f), new Color(0.8f, 0.28f, 0.1f));
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.05f;
            main.maxParticles = 200;
            SetRate(particles, 10f);
            SetDrift(particles, 0.4f, 0.5f);
            ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
            rotation.y = new ParticleSystem.MinMaxCurve(-2f, 2f);
            rotation.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

            Mesh leaf = LoadLeafMesh();
            if (leaf != null)
            {
                var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
                particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                particleRenderer.mesh = leaf;
            }
        }

        private static void ConfigureSnow(ParticleSystem particles)
        {
            particles.transform.localPosition = Vector3.up * 7f;
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = 12f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.startColor = Color.white;
            main.gravityModifier = 0.03f;
            main.maxParticles = 800;
            SetRate(particles, 50f);
            SetDrift(particles, 0.1f, 0.2f);
        }

        private static void SetRate(ParticleSystem particles, float rate)
        {
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = rate;
        }

        /// <summary>
        /// 一点横向的风加上噪声扰动，让粒子不是直上直下。
        /// </summary>
        private static void SetDrift(ParticleSystem particles, float wind, float noiseStrength)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(wind);
            velocity.y = new ParticleSystem.MinMaxCurve(0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = noiseStrength > 0f;
            noise.strength = noiseStrength;
            noise.frequency = 0.3f;
        }

        /// <summary>
        /// 落叶用植物包的叶子模型。粒子使用的网格必须可读，否则打包后不显示，这里顺便开启 Read/Write。
        /// </summary>
        private static Mesh LoadLeafMesh()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LeafPrefabPath);
            MeshFilter filter = prefab != null ? prefab.GetComponentInChildren<MeshFilter>(true) : null;
            if (filter == null || filter.sharedMesh == null)
            {
                Debug.LogWarning($"[{nameof(SeasonParticleCreator)}] 找不到叶子模型 {LeafPrefabPath}，落叶改用方形粒子");
                return null;
            }

            if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(filter.sharedMesh)) is ModelImporter importer && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();

                // 重新导入后旧的网格引用会失效，重新读取
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LeafPrefabPath);
                filter = prefab.GetComponentInChildren<MeshFilter>(true);
            }

            return filter.sharedMesh;
        }
    }
}
