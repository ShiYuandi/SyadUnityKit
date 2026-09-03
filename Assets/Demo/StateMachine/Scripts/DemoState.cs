using Syad.UnityKit.StateMachine;

/// <summary>
/// StateMachine Demo 的项目级状态基类。
/// 它可以保存 Demo Controller 等项目依赖，框架内核不需要了解这些对象。
/// </summary>
public abstract class DemoState : IState
{
    protected StateMachineDemoController Controller { get; private set; }

    protected DemoState(StateMachineDemoController controller)
    {
        Controller = controller;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}
