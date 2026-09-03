namespace Syad.UnityKit.StateMachine
{
    /// <summary>
    /// 定义状态进入、逐帧更新和退出的基础生命周期。
    /// 具体状态由项目实现，框架不读取输入或处理业务逻辑。
    /// </summary>
    public interface IState
    {
        /// <summary>状态成为当前状态时调用。</summary>
        void Enter();

        /// <summary>由状态机的 Update 方法逐帧转发。</summary>
        void Update();

        /// <summary>状态即将不再是当前状态时调用。</summary>
        void Exit();
    }
}
