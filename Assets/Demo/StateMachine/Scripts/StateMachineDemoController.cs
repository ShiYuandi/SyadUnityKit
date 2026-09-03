using Syad.UnityKit.StateMachine;
using UnityEngine;

/// <summary>
/// 创建并驱动状态机的 Demo 入口。
/// Controller 决定何时切换，具体状态只实现自己的生命周期。
/// </summary>
public sealed class StateMachineDemoController : MonoBehaviour
{
    private StateMachine<DemoState> _stateMachine;
    private IdleDemoState _idleState;
    private RunningDemoState _runningState;

    private string _currentStateName = "未初始化";
    private string _lastLifecycle = "无";
    private float _runningSeconds;

    private GUIStyle _titleStyle;
    private GUIStyle _textStyle;
    private GUIStyle _statusStyle;

    private void Awake()
    {
        _stateMachine = new StateMachine<DemoState>();
        _idleState = new IdleDemoState(this);
        _runningState = new RunningDemoState(this);

        _stateMachine.Initialize(_idleState);
    }

    private void Update()
    {
        HandleDebugInput();
        _stateMachine.Update();
    }

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            _stateMachine.Stop();
            _currentStateName = "已停止";
            return;
        }

        if (Input.GetKeyDown(KeyCode.I) && _stateMachine.CurrentState == null)
        {
            _stateMachine.Initialize(_idleState);
            return;
        }

        if (_stateMachine.CurrentState == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            _stateMachine.ChangeState(_stateMachine.CurrentState);
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        if (_stateMachine.CurrentState == _idleState)
        {
            _stateMachine.ChangeState(_runningState);
        }
        else
        {
            _stateMachine.ChangeState(_idleState);
        }
    }

    /// <summary>由具体状态在 Enter 时通知 Demo 展示层。</summary>
    public void NotifyStateEntered(string stateName)
    {
        _currentStateName = stateName;
        _lastLifecycle = stateName + ".Enter";
        Debug.Log("StateMachine Demo：" + _lastLifecycle);
    }

    /// <summary>由具体状态在 Exit 时通知 Demo 展示层。</summary>
    public void NotifyStateExited(string stateName)
    {
        _lastLifecycle = stateName + ".Exit";
        Debug.Log("StateMachine Demo：" + _lastLifecycle);
    }

    /// <summary>更新运行状态自己维护的计时结果。</summary>
    public void SetRunningSeconds(float seconds)
    {
        _runningSeconds = seconds;
    }

    private void OnDestroy()
    {
        if (_stateMachine != null)
        {
            _stateMachine.Stop();
        }
    }

    private void OnGUI()
    {
        EnsureGuiStyles();

        const float width = 620f;
        const float height = 330f;
        Rect panelRect = new Rect(24f, 24f, width, height);
        GUI.Box(panelRect, GUIContent.none);

        float x = panelRect.x + 24f;
        float y = panelRect.y + 18f;
        float contentWidth = panelRect.width - 48f;

        GUI.Label(
            new Rect(x, y, contentWidth, 36f),
            "SYAD StateMachine Demo",
            _titleStyle);
        y += 48f;

        GUI.Label(
            new Rect(x, y, contentWidth, 30f),
            "当前状态：" + _currentStateName,
            _statusStyle);
        y += 34f;
        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "最近生命周期：" + _lastLifecycle,
            _textStyle);
        y += 30f;
        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "本次运行时间：" + _runningSeconds.ToString("0.00") + " 秒",
            _textStyle);
        y += 42f;

        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "Space：切换空闲 / 运行    R：重新进入当前状态",
            _textStyle);
        y += 30f;
        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "S：停止状态机    I：停止后重新初始化",
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
            fontSize = 17
        };
        _statusStyle = new GUIStyle(_textStyle)
        {
            fontStyle = FontStyle.Bold
        };
    }
}
