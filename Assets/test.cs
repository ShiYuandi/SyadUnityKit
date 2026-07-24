using Syad.UnityKit.UI;
using UnityEngine;

public sealed class test : MonoBehaviour
{
    [SerializeField] private UIRoot _uiRoot;
    [SerializeField] private UIViewCatalog _catalog;

    private UIService _uiService;
    private StartView _startView;

    private void Awake()
    {
        _uiService = new UIService(_uiRoot, _catalog);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShowStartView();
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            _uiService.Hide<StartView>();
        }
    }

    private void ShowStartView()
    {
        // 按下空格时，让 UIService 创建或重新显示 StartView。
        _startView = _uiService.Show<StartView>();

        // StartView 会被缓存并重复显示，所以先退订再订阅，避免重复绑定。
        _startView.StartRequested -= HandleStartRequested;
        _startView.StartRequested += HandleStartRequested;
    }

    private void HandleStartRequested()
    {
        Debug.Log("test：收到开始请求，隐藏 StartView");

        if (_startView != null)
        {
            _uiService.Hide(_startView);
        }
    }

    private void OnDestroy()
    {
        if (_startView != null)
        {
            _startView.StartRequested -= HandleStartRequested;
        }

        // 释放由这个 UIService 创建和缓存的全部页面。
        if (_uiService != null)
        {
            _uiService.Dispose();
        }
    }
}
