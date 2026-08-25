using Syad.UnityKit.Networking;
using UnityEngine;

/// <summary>
/// UDP Demo 的展示层。它只显示接收组件状态，并作为 Inspector UnityEvent 的绑定目标。
/// </summary>
public sealed class UdpReceiverDemoPresenter : MonoBehaviour
{
    [SerializeField]
    private UdpReceiverBehaviour _receiver;

    private string _eventMessage = "尚未收到";
    private string _eventError = "无";
    private int _eventCount;
    private GUIStyle _titleStyle;
    private GUIStyle _textStyle;
    private GUIStyle _statusStyle;

    /// <summary>在场景 Inspector 中绑定到 UDP 组件的“收到文本消息”。</summary>
    public void HandleMessage(string message)
    {
        _eventMessage = message;
        _eventCount++;
        _eventError = "无";
    }

    /// <summary>在场景 Inspector 中绑定到 UDP 组件的“发生错误”。</summary>
    public void HandleError(string error)
    {
        _eventError = error;
    }

    private void OnGUI()
    {
        EnsureGuiStyles();

        const float width = 760f;
        const float height = 490f;
        Rect panelRect = new Rect(24f, 24f, width, height);
        GUI.Box(panelRect, GUIContent.none);

        float x = panelRect.x + 24f;
        float y = panelRect.y + 18f;
        float contentWidth = panelRect.width - 48f;

        GUI.Label(
            new Rect(x, y, contentWidth, 36f),
            "SYAD UDP Receiver Demo",
            _titleStyle);
        y += 48f;

        if (_receiver == null)
        {
            GUI.Label(
                new Rect(x, y, contentWidth, 30f),
                "没有配置 UdpReceiverBehaviour。",
                _statusStyle);
            return;
        }

        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "监听地址：0.0.0.0:"
            + _receiver.ListenPort
            + "    状态："
            + (_receiver.IsListening ? "正在监听" : "已停止"),
            _statusStyle);
        y += 38f;

        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "发送端："
            + (string.IsNullOrEmpty(_receiver.LastRemoteEndPoint)
                ? "无"
                : _receiver.LastRemoteEndPoint),
            _textStyle);
        y += 32f;

        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "Socket 收包数："
            + _receiver.ReceivedMessageCount
            + "    UnityEvent 次数："
            + _eventCount
            + "    队列丢弃："
            + _receiver.DroppedMessageCount,
            _textStyle);
        y += 40f;

        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "UnityEvent 收到的完整文本：",
            _statusStyle);
        y += 30f;
        GUI.TextArea(
            new Rect(x, y, contentWidth, 105f),
            _eventMessage ?? string.Empty,
            _textStyle);
        y += 118f;

        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "最近错误：" + _eventError,
            _textStyle);
        y += 38f;

        GUI.Label(
            new Rect(x, y, contentWidth, 52f),
            "请从外部程序向 127.0.0.1:"
            + _receiver.ListenPort
            + " 发送一个 UTF-8 UDP 数据包。PowerShell 命令见本 Demo 的 README。",
            _textStyle);
    }

    private void EnsureGuiStyles()
    {
        if (_titleStyle != null)
        {
            return;
        }

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold
        };
        _textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            wordWrap = true
        };
        _statusStyle = new GUIStyle(_textStyle)
        {
            fontStyle = FontStyle.Bold
        };
    }
}
