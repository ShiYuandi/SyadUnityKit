using System;

namespace Syad.UnityKit.StateMachine
{
    /// <summary>
    /// 管理一个当前状态，并统一转发 Enter、Update 和 Exit 生命周期。
    /// 状态机不依赖 MonoBehaviour，项目需要从自己的 Update 中显式驱动它。
    /// </summary>
    public sealed class StateMachine<TState>
        where TState : class, IState
    {
        /// <summary>当前正在运行的状态；尚未初始化或已经停止时为 null。</summary>
        public TState CurrentState { get; private set; }

        /// <summary>
        /// 使用初始状态启动状态机。
        /// 状态机已经运行时应使用 ChangeState，或先 Stop 后重新初始化。
        /// </summary>
        public void Initialize(TState startState)
        {
            if (startState == null)
            {
                throw new ArgumentNullException(nameof(startState));
            }

            if (CurrentState != null)
            {
                throw new InvalidOperationException(
                    "状态机已经初始化。请使用 ChangeState 切换状态，或先调用 Stop。");
            }

            CurrentState = startState;
            CurrentState.Enter();
        }

        /// <summary>
        /// 退出当前状态并进入新状态。
        /// 即使传入当前状态本身，也会完整执行一次 Exit 和 Enter。
        /// </summary>
        public void ChangeState(TState newState)
        {
            if (newState == null)
            {
                throw new ArgumentNullException(nameof(newState));
            }

            if (CurrentState == null)
            {
                throw new InvalidOperationException(
                    "状态机尚未初始化。请先调用 Initialize。");
            }

            CurrentState.Exit();
            CurrentState = newState;
            CurrentState.Enter();
        }

        /// <summary>更新当前状态；尚未初始化或已经停止时不执行任何操作。</summary>
        public void Update()
        {
            if (CurrentState == null)
            {
                return;
            }

            CurrentState.Update();
        }

        /// <summary>退出并清空当前状态；可以安全重复调用。</summary>
        public void Stop()
        {
            if (CurrentState == null)
            {
                return;
            }

            CurrentState.Exit();
            CurrentState = null;
        }
    }
}
