namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 编辑器工具用到的项目文件夹路径，集中在这一处。挪动文件夹后只需改这里，所有工具跟着生效。
    /// </summary>
    internal static class ProjectPaths
    {
        internal const string Root = "Assets/Worldyssey";

        // 美术资源
        internal const string Art = Root + "/Art";
        internal const string Materials = Art + "/Materials";
        internal const string Textures = Art + "/Textures";
        internal const string Models = Art + "/Models";
        internal const string EnvironmentMaterials = Materials + "/Environment";
        internal const string TileMaterials = Materials + "/Tiles";

        // 配置
        internal const string Config = Root + "/Config";
        internal const string Seasons = Config + "/Seasons";

        // 预制体
        internal const string Prefabs = Root + "/Prefabs";
        internal const string EnvironmentPrefabs = Prefabs + "/Environment";

        // 场景里实际使用的玩法物件（地板、墙、目标、底座、箱子、玩家）
        internal const string GameplayPrefabs = Prefabs + "/Gameplay";

        // 外部导入、等待 GameplayPrefabSetup 处理的玩法模型预制体。和上面分开，免得处理时把生成好的预制体也改掉
        internal const string ImportedGameplayPrefabs = GameplayPrefabs + "/Imported";
        internal const string EffectPrefabs = Prefabs + "/Effects";
        internal const string UiPrefabs = Prefabs + "/UI";

        // 关卡与界面
        internal const string Levels = Root + "/Levels";
        internal const string LevelSources = Levels + "/Source";
        internal const string Ui = Root + "/UI";

        // 第三方素材包
        internal const string ThirdParty = Root + "/ThirdParty";
    }
}
