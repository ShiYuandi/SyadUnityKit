using System;
using UnityEngine.Events;

namespace Syad.UnityKit.Networking
{
    /// <summary>
    /// 可在 Unity 2019.4 Inspector 中序列化的字符串事件。
    /// UDP 组件使用它在 Unity 主线程通知项目代码。
    /// </summary>
    [Serializable]
    public sealed class UdpTextEvent : UnityEvent<string>
    {
    }
}
