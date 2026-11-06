using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worldyssey.Data;
using Worldyssey.Gameplay;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 接入游戏鼠标指针：把三张指针贴图设为 Cursor 类型导入，生成 <see cref="CursorSet"/>，并在 GameFlow 物体上挂 <see cref="GameCursor"/>。
    /// 可以重复运行，已有的资源和组件只更新引用。
    /// </summary>
    public static class GameCursorSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Setup Game Cursor";
        private const string CursorFolder = ProjectPaths.Textures + "/UI/Cursors";
        private const string CursorSetPath = ProjectPaths.Config + "/CursorSet.asset";
        private static readonly Vector2 CursorHotspot = new Vector2(2f, 2f);

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var gameFlow = Object.FindObjectOfType<GameFlow>();
            if (gameFlow == null)
            {
                Debug.LogError($"[{nameof(GameCursorSetup)}] 当前场景没有 {nameof(GameFlow)}，未做任何修改");
                return;
            }

            CursorSet cursorSet = GetOrCreateCursorSet();
            var serialized = new SerializedObject(cursorSet);
            serialized.FindProperty("_normal").objectReferenceValue = LoadCursor("Cursor_Normal");
            serialized.FindProperty("_hover").objectReferenceValue = LoadCursor("Cursor_Hover");
            serialized.FindProperty("_pressed").objectReferenceValue = LoadCursor("Cursor_Pressed");

            // 像素指针的箭头尖端在左上角第一个像素（放大 4 倍后约为 2,2）
            serialized.FindProperty("_hotspot").vector2Value = CursorHotspot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cursorSet);
            AssetDatabase.SaveAssets();

            if (!gameFlow.TryGetComponent(out GameCursor cursor))
            {
                cursor = Undo.AddComponent<GameCursor>(gameFlow.gameObject);
            }

            GameUiBuilder.SetReference(cursor, "_cursorSet", cursorSet);
            EditorSceneManager.MarkSceneDirty(gameFlow.gameObject.scene);
            Selection.activeObject = cursorSet;
            Debug.Log($"[{nameof(GameCursorSetup)}] 鼠标指针已接入，判定点和软件指针开关在 {CursorSetPath}，记得保存场景", cursorSet);
        }

        private static CursorSet GetOrCreateCursorSet()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CursorSet>(CursorSetPath);
            if (existing != null)
            {
                return existing;
            }

            AssetFolderUtility.EnsureFolder(ProjectPaths.Config);
            var created = ScriptableObject.CreateInstance<CursorSet>();
            AssetDatabase.CreateAsset(created, CursorSetPath);
            return created;
        }

        /// <summary>
        /// 指针贴图必须是 Cursor 类型、可读写、不压缩、无 mipmap，否则 SetCursor 会报错或显示模糊。
        /// </summary>
        private static Texture2D LoadCursor(string fileName)
        {
            string path = $"{CursorFolder}/{fileName}.png";
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                Debug.LogWarning($"[{nameof(GameCursorSetup)}] 找不到指针贴图 {path}");
                return null;
            }

            bool isReady = importer.textureType == TextureImporterType.Cursor && importer.isReadable && !importer.mipmapEnabled
                && importer.textureCompression == TextureImporterCompression.Uncompressed && importer.alphaIsTransparency
                && importer.filterMode == FilterMode.Point;
            if (!isReady)
            {
                importer.textureType = TextureImporterType.Cursor;
                importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
