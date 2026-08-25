using System;
using UnityEngine;

namespace Syad.UnityKit.Input
{
    /// <summary>
    /// 强类型输入源基类。具体输入源只负责发现命令，不处理冷却和业务。
    /// </summary>
    /// <typeparam name="TCommand">项目使用的命令类型。</typeparam>
    public abstract class InputSource<TCommand> : MonoBehaviour
    {
        /// <summary>输入源检测到命令时触发。</summary>
        public event Action<TCommand> CommandDetected;

        /// <summary>供具体输入源提交已经转换完成的项目命令。</summary>
        protected void RaiseCommand(TCommand command)
        {
            Action<TCommand> handler = CommandDetected;
            if (handler != null)
            {
                handler(command);
            }
        }

        /// <summary>输入源销毁时清理全部命令订阅。</summary>
        protected virtual void OnDestroy()
        {
            CommandDetected = null;
        }
    }
}
