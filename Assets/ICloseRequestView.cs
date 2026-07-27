using System;
using Syad.UnityKit.UI;

/// <summary>
/// 表示可以主动请求关闭的页面。
/// 页面只负责发出关闭意图，真正的隐藏操作由 MainSystem 处理。
/// </summary>
public interface ICloseRequestView
{
    event Action<UIView> CloseRequested;
}
