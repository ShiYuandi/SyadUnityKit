using System;
using UnityEngine;

namespace Syad.UnityKit.UI
{
    /// <summary>
    /// 由 <see cref="UIService"/> 管理的视图基类。
    /// 业务视图应通过明确的 SetData/Bind 方法接收数据，并通过事件发出用户操作意图。
    /// </summary>
    public abstract class UIView : MonoBehaviour
    {
        public bool IsCreated { get; private set; }

        public bool IsVisible { get; private set; }

        public bool IsReleased { get; private set; }

        internal void Create()
        {
            ThrowIfReleased();

            if (IsCreated)
            {
                return;
            }

            IsCreated = true;
            OnCreated();
        }

        internal void BeforeShow()
        {
            ThrowIfReleased();

            if (!IsCreated)
            {
                Create();
            }

            IsVisible = true;
        }

        internal void AfterShow()
        {
            ThrowIfReleased();

            if (!IsVisible)
            {
                throw new InvalidOperationException("UIView 必须先完成显示准备，才能执行显示回调。");
            }

            OnShown();
        }

        internal bool BeforeHide()
        {
            if (IsReleased || !IsVisible)
            {
                return false;
            }

            IsVisible = false;
            OnHidden();
            return true;
        }

        internal void Release()
        {
            if (IsReleased)
            {
                return;
            }

            IsReleased = true;
            OnReleased();
        }

        /// <summary>服务创建视图后调用一次。</summary>
        protected virtual void OnCreated()
        {
        }

        /// <summary>每次请求显示时调用，包括对可见单实例视图的重复显示请求。</summary>
        protected virtual void OnShown()
        {
        }

        /// <summary>可见视图被隐藏时调用。</summary>
        protected virtual void OnHidden()
        {
        }

        /// <summary>服务销毁视图前调用一次。</summary>
        protected virtual void OnReleased()
        {
        }

        private void ThrowIfReleased()
        {
            if (IsReleased)
            {
                throw new InvalidOperationException("已经释放的 UIView 不能再次使用。");
            }
        }
    }
}
