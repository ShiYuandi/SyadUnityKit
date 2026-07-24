using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Syad.UnityKit.UI;
using UnityEngine.UI;

public sealed class StartView : UIView
{
    [SerializeField] private Button _startButton;

    // StartView 不关心是谁处理开始操作，只负责发出用户意图。
    public event Action StartRequested;

    protected override void OnCreated()
    {
        // 每个页面实例只初始化一次
        _startButton.onClick.AddListener(HandleStartClicked);
    }

    protected override void OnReleased()
    {
        // 页面释放时清理按钮监听
        _startButton.onClick.RemoveListener(HandleStartClicked);

        // 清理外部对象对这个页面事件的订阅
        StartRequested = null;
    }

    private void HandleStartClicked()
    {
        Debug.Log("StartView：用户点击了开始按钮");

        if (StartRequested != null)
        {
            StartRequested();
        }
    }
}
