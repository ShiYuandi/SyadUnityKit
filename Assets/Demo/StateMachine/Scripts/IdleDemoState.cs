/// <summary>等待用户切换的空闲状态。</summary>
public sealed class IdleDemoState : DemoState
{
    public IdleDemoState(StateMachineDemoController controller)
        : base(controller)
    {
    }

    public override void Enter()
    {
        Controller.NotifyStateEntered("空闲状态");
    }

    public override void Update()
    {
        // 该状态当前没有需要逐帧执行的业务。
    }

    public override void Exit()
    {
        Controller.NotifyStateExited("空闲状态");
    }
}
