namespace Worldyssey.Data
{
    /// <summary>
    /// 界面元素的颜色角色。元素只声明自己是什么角色，具体颜色由当前主题决定。
    /// </summary>
    /// <remarks>值会被序列化到场景和预制体里，新增角色只能加在末尾，不能调整已有的数值。</remarks>
    public enum UiColorRole
    {
        Header = 0,
        PanelBackground = 1,
        InnerFrame = 2,
        Accent = 3,
        Text = 4,
        HeaderText = 5,
        Icon = 6,
        Confirm = 7,
        Cancel = 8,
        Disabled = 9,
    }
}
