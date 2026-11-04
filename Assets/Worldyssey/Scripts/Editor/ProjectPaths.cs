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

        // 配置
        internal const string Config = Root + "/Config";
        internal const string Seasons = Config + "/Seasons";

        // 预制体
        internal const string Prefabs = Root + "/Prefabs";
        internal const string EnvironmentPrefabs = Prefabs + "/Environment";
        internal const string GameplayPrefabs = Prefabs + "/Player_Box_Goal";
        internal const string PlaceholderPrefabs = Prefabs + "/Placeholder";
        internal const string EffectPrefabs = Prefabs + "/Effects";
        internal const string UiPrefabs = Prefabs + "/UI";

        // 关卡与界面
        internal const string Levels = Root + "/Levels";
        internal const string LevelSources = Levels + "/Source";
        internal const string Ui = Root + "/UI";

        // 第三方素材包
        internal const string ThirdParty = Root + "/Packages";
    }
}
