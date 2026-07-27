using System;
using UnityEngine;

namespace Syad.UnityKit.Input
{
    /// <summary>
    /// 把键盘、UDP、RFID 或其他输入源产生的结果统一转发为项目定义的强类型命令。
    /// 本类不读取具体设备，也不处理业务状态，只负责输入开关、冷却和命令转发。
    /// </summary>
    public sealed class InputRouter<TCommand> : IDisposable
    {
        private readonly Func<float> _timeProvider;
        private readonly float _cooldownSeconds;

        private float _nextAllowedTime;
        private bool _hasDispatched;
        private bool _isDisposed;

        /// <summary>输入命令通过门控后触发。</summary>
        public event Action<TCommand> CommandReceived;

        /// <summary>当前是否允许接收输入。</summary>
        public bool IsEnabled { get; private set; }

        /// <summary>每次成功转发命令后，需要等待的秒数。</summary>
        public float CooldownSeconds
        {
            get { return _cooldownSeconds; }
        }

        /// <summary>当前路由器是否已经释放。</summary>
        public bool IsDisposed
        {
            get { return _isDisposed; }
        }

        /// <summary>
        /// 创建输入路由器。默认使用不受 Time.timeScale 影响的运行时间。
        /// </summary>
        public InputRouter(float cooldownSeconds = 0f)
            : this(cooldownSeconds, () => Time.unscaledTime)
        {
        }

        /// <summary>
        /// 创建输入路由器，并显式提供时间来源。主要用于测试或自定义计时方式。
        /// </summary>
        public InputRouter(float cooldownSeconds, Func<float> timeProvider)
        {
            if (cooldownSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cooldownSeconds),
                    "输入冷却时间不能小于零。");
            }

            if (timeProvider == null)
            {
                throw new ArgumentNullException(nameof(timeProvider));
            }

            _cooldownSeconds = cooldownSeconds;
            _timeProvider = timeProvider;
            IsEnabled = true;
        }

        /// <summary>
        /// 尝试转发命令。返回 true 表示命令已通过门控并通知订阅者。
        /// 应从 Unity 主线程调用；本类不会替外部设备接收线程切换执行线程。
        /// </summary>
        public bool TryDispatch(TCommand command)
        {
            ThrowIfDisposed();

            if (!IsEnabled)
            {
                return false;
            }

            float currentTime = _timeProvider();
            if (_hasDispatched && currentTime < _nextAllowedTime)
            {
                return false;
            }

            // 先更新冷却状态，再通知订阅者，避免回调中重复提交命令绕过门控。
            _hasDispatched = true;
            _nextAllowedTime = currentTime + _cooldownSeconds;

            Action<TCommand> handler = CommandReceived;
            if (handler != null)
            {
                handler(command);
            }

            return true;
        }

        /// <summary>
        /// 设置是否允许输入。可选择在切换状态时同时清除剩余冷却时间。
        /// </summary>
        public void SetEnabled(bool enabled, bool resetCooldown = false)
        {
            ThrowIfDisposed();
            IsEnabled = enabled;

            if (resetCooldown)
            {
                ResetCooldownInternal();
            }
        }

        /// <summary>立即清除剩余冷却时间，使下一条命令可以通过。</summary>
        public void ResetCooldown()
        {
            ThrowIfDisposed();
            ResetCooldownInternal();
        }

        /// <summary>释放路由器并清理全部命令订阅。</summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            CommandReceived = null;
            IsEnabled = false;
            _isDisposed = true;
        }

        private void ResetCooldownInternal()
        {
            _hasDispatched = false;
            _nextAllowedTime = 0f;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(
                    typeof(InputRouter<TCommand>).Name,
                    "输入路由器已经释放，不能继续使用。");
            }
        }
    }
}
