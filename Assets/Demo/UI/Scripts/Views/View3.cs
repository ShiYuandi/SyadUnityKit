using System.Collections;
using System;
using UnityEngine;
using Syad.UnityKit.UI;
using UnityEngine.UI;

public sealed class View3 : UIView, ICloseRequestView
{
    [SerializeField] private Button _startButton;

    // 页面不直接隐藏自己，只向外部发出关闭请求。
    public event Action<UIView> CloseRequested;

    protected override void OnCreated()
    {
        // 每个页面实例只初始化一次
        _startButton.onClick.AddListener(HandleView3Clicked);
    }

    protected override void OnReleased()
    {
        // 页面释放时清理按钮监听
        _startButton.onClick.RemoveListener(HandleView3Clicked);

        // 清理外部对象对这个页面事件的订阅
        CloseRequested = null;
    }

    private void HandleView3Clicked()
    {
        Debug.Log("View3：用户点击了开始按钮");

        if (CloseRequested != null)
        {
            CloseRequested(this);
        }
    }
}
