using UnityEditor;
using UnityEngine;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 一键生成占位预制体和 <see cref="LevelVisualConfig"/>。预制体根节点都在格子底面中心，模型作为子物体偏移，
    /// 这样 M8 替换美术时只需换子物体或换预制体引用。已存在的预制体和已填的配置引用都不会被覆盖。
    /// </summary>
    public static class PlaceholderPrefabCreator
    {
        private const string MenuPath = "Worldyssey/Scene Setup/Create Placeholder Prefabs";
        private const string PrefabFolder = ProjectPaths.PlaceholderPrefabs;
        private const string ConfigFolder = ProjectPaths.Config;
        private const string ConfigPath = ConfigFolder + "/LevelVisualConfig.asset";
        private const string MaterialFolder = ProjectPaths.Materials + "/Placeholder";

        private const string FloorName = "P_Floor";
        private const string SharedWallName = "P_SharedWall";
        private const string UniqueWallName = "P_UniqueWall";
        private const string GoalName = "P_Goal";
        private const string PedestalName = "P_Pedestal";
        private const string PlayerName = "P_Player";
        private const string MappedBoxName = "P_MappedBox";
        private const string PortalBoxName = "P_PortalBox";

        private const float BoxSize = 0.85f;

        // 传送箱子比底座小一圈，放上底座后四边能露出底座边缘（与 PedestalBoxSizeSetup 一致）
        private const float PortalBoxSize = PedestalBoxSizeSetup.PortalBoxSize;
        private const float HintPlateSize = 0.6f;
        private const float HintPlateThickness = 0.04f;

        [MenuItem(MenuPath)]
        public static void CreatePlaceholderPrefabs()
        {
            // 预制体依赖占位材质，先确保材质存在（已存在的会被跳过）
            PlaceholderMaterialCreator.CreatePlaceholderMaterials();
            AssetFolderUtility.EnsureFolder(PrefabFolder);
            AssetFolderUtility.EnsureFolder(ConfigFolder);

            GameObject floor = GetOrCreate(FloorName, () => CreateTile(
                FloorName, PrimitiveType.Cube, "M_Floor", new Vector3(1f, 0.1f, 1f), -0.05f));
            GameObject sharedWall = GetOrCreate(SharedWallName, () => CreateTile(
                SharedWallName, PrimitiveType.Cube, "M_SharedWall", Vector3.one, 0.5f));
            // 独有墙略矮一些，和共享墙在外形上也能区分
            GameObject uniqueWall = GetOrCreate(UniqueWallName, () => CreateTile(
                UniqueWallName, PrimitiveType.Cube, "M_UniqueWall", new Vector3(0.95f, 0.8f, 0.95f), 0.4f));
            GameObject goal = GetOrCreate(GoalName, () => CreateTile(
                GoalName, PrimitiveType.Cube, "M_Goal", new Vector3(0.8f, 0.02f, 0.8f), 0.01f));
            // Cylinder 原始高度为 2、直径为 1，y 缩放取高度的一半
            GameObject pedestal = GetOrCreate(PedestalName, () => CreateTile(
                PedestalName, PrimitiveType.Cylinder, "M_Pedestal",
                new Vector3(PedestalBoxSizeSetup.PedestalDiameter, PedestalBoxSizeSetup.PedestalHeight * 0.5f, PedestalBoxSizeSetup.PedestalDiameter),
                PedestalBoxSizeSetup.PedestalHeight * 0.5f));
            GameObject player = GetOrCreate(PlayerName, CreatePlayer);
            GameObject mappedBox = GetOrCreate(MappedBoxName, () => CreateEntity(
                MappedBoxName, PrimitiveType.Cube, "M_MappedBox", new Vector3(BoxSize, BoxSize, BoxSize), BoxSize * 0.5f));
            GameObject portalBox = GetOrCreate(PortalBoxName, CreatePortalBox);

            FillConfig(floor, sharedWall, uniqueWall, goal, pedestal, player, mappedBox, portalBox);
            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(PlaceholderPrefabCreator)}] 占位预制体与配置已就绪：{PrefabFolder}，{ConfigPath}");
        }

        private static GameObject GetOrCreate(string prefabName, System.Func<GameObject> createInstance)
        {
            string path = $"{PrefabFolder}/{prefabName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            GameObject instance = createInstance();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static GameObject CreateTile(string rootName, PrimitiveType shape, string materialName, Vector3 scale, float centerY)
        {
            var root = new GameObject(rootName);
            AddModel(root.transform, "Model", shape, materialName, scale, new Vector3(0f, centerY, 0f));
            return root;
        }

        private static GameObject CreateEntity(string rootName, PrimitiveType shape, string materialName, Vector3 scale, float centerY)
        {
            GameObject root = CreateTile(rootName, shape, materialName, scale, centerY);
            root.AddComponent<EntityView>();
            return root;
        }

        private static GameObject CreatePlayer()
        {
            // Capsule 原始高度为 2，缩放 0.45 得到 0.9 高
            GameObject root = CreateEntity(PlayerName, PrimitiveType.Capsule, "M_Player", new Vector3(0.6f, 0.45f, 0.6f), 0.45f);

            // 前方的小方块用来看清朝向，朝向 +z 即 Direction.Up
            AddModel(root.transform, "Nose", PrimitiveType.Cube, "M_SharedWall", new Vector3(0.2f, 0.2f, 0.2f), new Vector3(0f, 0.6f, 0.32f));
            return root;
        }

        private static GameObject CreatePortalBox()
        {
            var root = new GameObject(PortalBoxName);
            float centerY = PortalBoxSize * 0.5f;
            GameObject body = AddModel(root.transform, "Model", PrimitiveType.Cube, "M_PortalBox",
                new Vector3(PortalBoxSize, PortalBoxSize, PortalBoxSize), new Vector3(0f, centerY, 0f));

            var hintRoot = new GameObject("FaceHint");
            hintRoot.transform.SetParent(root.transform, false);
            PortalFaceHint faceHint = hintRoot.AddComponent<PortalFaceHint>();

            // 提示板贴在箱子四个侧面外侧一点，顺序与 Direction 枚举一致：Up(+z)、Right(+x)、Down(-z)、Left(-x)
            float offset = PortalBoxSize * 0.5f + HintPlateThickness * 0.5f + 0.01f;
            var faceScaleZ = new Vector3(HintPlateSize, HintPlateSize, HintPlateThickness);
            var faceScaleX = new Vector3(HintPlateThickness, HintPlateSize, HintPlateSize);
            Renderer[] faces =
            {
                AddModel(hintRoot.transform, "Face_Up", PrimitiveType.Cube, "M_HintEnterable", faceScaleZ, new Vector3(0f, centerY, offset)).GetComponent<Renderer>(),
                AddModel(hintRoot.transform, "Face_Right", PrimitiveType.Cube, "M_HintEnterable", faceScaleX, new Vector3(offset, centerY, 0f)).GetComponent<Renderer>(),
                AddModel(hintRoot.transform, "Face_Down", PrimitiveType.Cube, "M_HintEnterable", faceScaleZ, new Vector3(0f, centerY, -offset)).GetComponent<Renderer>(),
                AddModel(hintRoot.transform, "Face_Left", PrimitiveType.Cube, "M_HintEnterable", faceScaleX, new Vector3(-offset, centerY, 0f)).GetComponent<Renderer>(),
            };

            var hintSerialized = new SerializedObject(faceHint);
            SerializedProperty faceArray = hintSerialized.FindProperty("_faceRenderers");
            faceArray.arraySize = faces.Length;
            for (int i = 0; i < faces.Length; i++)
            {
                faceArray.GetArrayElementAtIndex(i).objectReferenceValue = faces[i];
            }

            hintSerialized.FindProperty("_enterableMaterial").objectReferenceValue = LoadMaterial("M_HintEnterable");
            hintSerialized.FindProperty("_blockedMaterial").objectReferenceValue = LoadMaterial("M_HintBlocked");
            hintSerialized.FindProperty("_conflictedMaterial").objectReferenceValue = LoadMaterial("M_HintConflicted");
            hintSerialized.ApplyModifiedPropertiesWithoutUndo();

            PortalBoxView view = root.AddComponent<PortalBoxView>();
            var viewSerialized = new SerializedObject(view);
            viewSerialized.FindProperty("_bodyRenderer").objectReferenceValue = body.GetComponent<Renderer>();
            viewSerialized.FindProperty("_faceHint").objectReferenceValue = faceHint;
            viewSerialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static GameObject AddModel(Transform parent, string modelName, PrimitiveType shape, string materialName, Vector3 scale, Vector3 localPosition)
        {
            GameObject model = GameObject.CreatePrimitive(shape);
            model.name = modelName;
            model.transform.SetParent(parent, false);
            model.transform.localPosition = localPosition;
            model.transform.localScale = scale;

            // 网格逻辑不依赖物理，去掉碰撞体避免无谓开销
            Object.DestroyImmediate(model.GetComponent<Collider>());

            Material material = LoadMaterial(materialName);
            if (material != null)
            {
                model.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            return model;
        }

        private static Material LoadMaterial(string materialName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{materialName}.mat");
            if (material == null)
            {
                Debug.LogError($"[{nameof(PlaceholderPrefabCreator)}] 找不到材质 {materialName}，将使用默认材质");
            }

            return material;
        }

        private static void FillConfig(
            GameObject floor,
            GameObject sharedWall,
            GameObject uniqueWall,
            GameObject goal,
            GameObject pedestal,
            GameObject player,
            GameObject mappedBox,
            GameObject portalBox)
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelVisualConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<LevelVisualConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            // 只填空着的引用：已手动替换过的预制体不会被覆盖
            var serialized = new SerializedObject(config);
            AssignIfEmpty(serialized, "_floorPrefab", floor);
            AssignIfEmpty(serialized, "_sharedWallPrefab", sharedWall);
            AssignIfEmpty(serialized, "_uniqueWallPrefab", uniqueWall);
            AssignIfEmpty(serialized, "_goalPrefab", goal);
            AssignIfEmpty(serialized, "_pedestalPrefab", pedestal);
            AssignIfEmpty(serialized, "_playerPrefab", player.GetComponent<EntityView>());
            AssignIfEmpty(serialized, "_mappedBoxPrefab", mappedBox.GetComponent<EntityView>());
            AssignIfEmpty(serialized, "_portalBoxPrefab", portalBox.GetComponent<PortalBoxView>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void AssignIfEmpty(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[{nameof(PlaceholderPrefabCreator)}] {nameof(LevelVisualConfig)} 中找不到字段 {propertyName}");
                return;
            }

            if (property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
