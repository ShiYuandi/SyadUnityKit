using Syad.UnityKit.UI;
using UnityEngine;

public sealed class MainSystem : MonoBehaviour
{
    [SerializeField] private UIRoot _uiRoot;
    [SerializeField] private UIViewCatalog _catalog;

    private UIService _uiService;
    private UIView _currentView;

    private void Awake()
    {
        _uiService = new UIService(_uiRoot, _catalog);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            ShowView<StartView>();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            ShowView<View2>();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            ShowView<View3>();
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            CloseCurrentView();
        }
    }

    /// <summary>
    /// 显示任意支持关闭请求的页面，并保证同一时间只显示一个主页面。
    /// </summary>
    private void ShowView<TView>() where TView : UIView, ICloseRequestView
    {
        CloseCurrentView();

        TView view = _uiService.Show<TView>();

        // 缓存页面会被重复使用，先退订再订阅可以避免回调被重复绑定。
        view.CloseRequested -= HandleCloseRequested;
        view.CloseRequested += HandleCloseRequested;

        _currentView = view;
    }

    /// <summary>统一处理所有页面发出的关闭请求。</summary>
    private void HandleCloseRequested(UIView view)
    {
        if (view == null)
        {
            return;
        }

        ICloseRequestView closeRequestView = view as ICloseRequestView;
        if (closeRequestView != null)
        {
            closeRequestView.CloseRequested -= HandleCloseRequested;
        }

        if (_currentView == view)
        {
            _currentView = null;
        }

        _uiService.Hide(view);
    }

    /// <summary>关闭当前显示的主页面。</summary>
    private void CloseCurrentView()
    {
        if (_currentView == null)
        {
            return;
        }

        UIView view = _currentView;
        _currentView = null;

        ICloseRequestView closeRequestView = view as ICloseRequestView;
        if (closeRequestView != null)
        {
            closeRequestView.CloseRequested -= HandleCloseRequested;
        }

        _uiService.Hide(view);
    }

    private void OnDestroy()
    {
        // 释放由这个 UIService 创建和缓存的全部页面。
        if (_uiService != null)
        {
            _uiService.Dispose();
        }
    }
}
