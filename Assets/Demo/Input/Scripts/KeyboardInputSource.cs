using System;
using UnityEngine;

/// <summary>
/// Input Demo 的键盘输入源，只负责把原始按键转换成项目命令或调试请求。
/// 它不知道命令是否会通过冷却，也不知道命令最终执行什么业务。
/// </summary>
public sealed class KeyboardInputSource : MonoBehaviour
{
    public event Action<InputDemoCommand> CommandDetected;
    public event Action ToggleRouterRequested;
    public event Action ResetCooldownRequested;

    private void Update()
    {
        if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
        {
            RaiseCommandDetected(InputDemoCommand.Left);
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
        {
            RaiseCommandDetected(InputDemoCommand.Right);
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
        {
            RaiseCommandDetected(InputDemoCommand.Confirm);
        }

        // T 和 R 控制路由器本身，不属于需要经过路由器的业务命令。
        if (UnityEngine.Input.GetKeyDown(KeyCode.T))
        {
            Action handler = ToggleRouterRequested;
            if (handler != null)
            {
                handler();
            }
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.R))
        {
            Action handler = ResetCooldownRequested;
            if (handler != null)
            {
                handler();
            }
        }
    }

    private void RaiseCommandDetected(InputDemoCommand command)
    {
        Action<InputDemoCommand> handler = CommandDetected;
        if (handler != null)
        {
            handler(command);
        }
    }

    private void OnDestroy()
    {
        CommandDetected = null;
        ToggleRouterRequested = null;
        ResetCooldownRequested = null;
    }
}
