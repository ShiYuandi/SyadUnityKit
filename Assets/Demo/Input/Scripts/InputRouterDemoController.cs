using Syad.UnityKit.Input;
using UnityEngine;

/// <summary>
/// InputRouter 的独立运行示例。
/// 本脚本只使用键盘和调试界面，不依赖 SYAD UI 模块。
/// </summary>
public sealed class InputRouterDemoController : MonoBehaviour
{
    [SerializeField] private KeyboardInputSource _keyboardInput;
    [SerializeField, Min(0f)] private float _cooldownSeconds = 1f;

    private InputRouter<InputDemoCommand> _inputRouter;
    private string _lastCommand = "无";
    private string _lastResult = "等待输入";
    private int _acceptedCount;
    private int _rejectedCount;

    private GUIStyle _titleStyle;
    private GUIStyle _textStyle;
    private GUIStyle _statusStyle;

    private void Awake()
    {
        if (_keyboardInput == null)
        {
            throw new MissingReferenceException("Input Demo Controller 没有配置 KeyboardInputSource。");
        }

        _inputRouter = new InputRouter<InputDemoCommand>(_cooldownSeconds);

        _keyboardInput.CommandDetected += SubmitCommand;
        _keyboardInput.ToggleRouterRequested += HandleToggleRouterRequested;
        _keyboardInput.ResetCooldownRequested += HandleResetCooldownRequested;
        _inputRouter.CommandReceived += HandleCommand;
    }

    private void SubmitCommand(InputDemoCommand command)
    {
        bool accepted = _inputRouter.TryDispatch(command);

        if (accepted)
        {
            _lastResult = "命令已通过：" + command;
            return;
        }

        _rejectedCount++;
        _lastResult = _inputRouter.IsEnabled
            ? "命令被冷却拦截：" + command
            : "命令被禁用状态拦截：" + command;

        Debug.Log("Input Demo：" + _lastResult);
    }

    private void HandleCommand(InputDemoCommand command)
    {
        _acceptedCount++;
        _lastCommand = command.ToString();
        Debug.Log("Input Demo：CommandReceived 收到命令 " + command);
    }

    private void HandleToggleRouterRequested()
    {
        bool nextEnabled = !_inputRouter.IsEnabled;
        _inputRouter.SetEnabled(nextEnabled, true);
        _lastResult = nextEnabled
            ? "输入路由器已启用，冷却已重置"
            : "输入路由器已禁用";
    }

    private void HandleResetCooldownRequested()
    {
        _inputRouter.ResetCooldown();
        _lastResult = "冷却已重置，下一条命令可立即通过";
    }

    private void OnGUI()
    {
        EnsureGuiStyles();

        const float width = 560f;
        const float height = 350f;
        Rect panelRect = new Rect(24f, 24f, width, height);
        GUI.Box(panelRect, GUIContent.none);

        float x = panelRect.x + 24f;
        float y = panelRect.y + 18f;
        float contentWidth = panelRect.width - 48f;

        GUI.Label(new Rect(x, y, contentWidth, 36f), "SYAD InputRouter Demo", _titleStyle);
        y += 48f;

        GUI.Label(new Rect(x, y, contentWidth, 28f), "← / →：提交 Left / Right 命令", _textStyle);
        y += 30f;
        GUI.Label(new Rect(x, y, contentWidth, 28f), "Space：提交 Confirm 命令", _textStyle);
        y += 30f;
        GUI.Label(new Rect(x, y, contentWidth, 28f), "T：启用或禁用输入    R：立即重置冷却", _textStyle);
        y += 44f;

        string enabledText = _inputRouter != null && _inputRouter.IsEnabled ? "已启用" : "已禁用";
        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "路由状态：" + enabledText + "    冷却时间：" + _cooldownSeconds.ToString("0.00") + " 秒",
            _statusStyle);
        y += 32f;

        GUI.Label(new Rect(x, y, contentWidth, 28f), "上一条通过的命令：" + _lastCommand, _textStyle);
        y += 30f;
        GUI.Label(new Rect(x, y, contentWidth, 28f), "最近结果：" + _lastResult, _textStyle);
        y += 30f;
        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "通过次数：" + _acceptedCount + "    拒绝次数：" + _rejectedCount,
            _textStyle);
    }

    private void OnDestroy()
    {
        if (_keyboardInput != null)
        {
            _keyboardInput.CommandDetected -= SubmitCommand;
            _keyboardInput.ToggleRouterRequested -= HandleToggleRouterRequested;
            _keyboardInput.ResetCooldownRequested -= HandleResetCooldownRequested;
        }

        if (_inputRouter == null)
        {
            return;
        }

        _inputRouter.CommandReceived -= HandleCommand;
        _inputRouter.Dispose();
        _inputRouter = null;
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
            fontSize = 17
        };

        _statusStyle = new GUIStyle(_textStyle)
        {
            fontStyle = FontStyle.Bold
        };
    }
}
