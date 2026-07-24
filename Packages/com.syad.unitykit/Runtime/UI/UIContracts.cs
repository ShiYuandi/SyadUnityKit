namespace Syad.UnityKit.UI
{
    /// <summary>
    /// 定义 <see cref="UIRoot"/> 使用的标准 UI 层级顺序。
    /// </summary>
    public enum UILayer
    {
        Background = 0,
        Screen = 100,
        Popup = 200,
        Overlay = 300
    }

    /// <summary>
    /// 控制视图隐藏后的处理方式。
    /// </summary>
    public enum UICachePolicy
    {
        KeepAlive = 0,
        ReleaseOnHide = 1
    }

    /// <summary>
    /// 控制同一种具体视图类型是否允许同时存在多个实例。
    /// </summary>
    public enum UIDuplicatePolicy
    {
        SingleInstance = 0,
        AllowMultiple = 1
    }
}
