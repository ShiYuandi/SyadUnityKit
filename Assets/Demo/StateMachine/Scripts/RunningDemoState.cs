using UnityEngine;

/// <summary>累计本次运行时间的演示状态。</summary>
public sealed class RunningDemoState : DemoState
{
    private float _elapsedSeconds;

    public RunningDemoState(StateMachineDemoController controller)
        : base(controller)
    {
    }

    public override void Enter()
    {
        _elapsedSeconds = 0f;
        Controller.NotifyStateEntered("运行状态");
        Controller.SetRunningSeconds(_elapsedSeconds);
    }

    public override void Update()
    {
        // 时间策略属于项目状态，通用状态机不强制传入 deltaTime。
        _elapsedSeconds += Time.deltaTime;
        Controller.SetRunningSeconds(_elapsedSeconds);
    }

    public override void Exit()
    {
        Controller.NotifyStateExited("运行状态");
    }
}
