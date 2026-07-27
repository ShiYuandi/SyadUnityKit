# SYAD Unity Kit

SYAD Unity Kit 是一套刻意保持小型、显式和易于理解的 Unity 运行时基础框架。稳定版 `0.1.0` 包含 UI 生命周期与页面管理内核；开发分支还包含尚未发布的输入命令路由模块。游戏状态、业务数据和“收到输入后应该做什么”仍由具体项目负责。

支持 Unity **2019.4 LTS 及以上版本**，包括 Unity 2022.3 LTS。

## 安装

在 Unity 中打开 **Window > Package Manager**，点击左上角的 **+**，选择 **Add package from git URL**，输入：

```text
https://github.com/ShiYuandi/SyadUnityKit.git?path=/Packages/com.syad.unitykit#v0.1.0
```

也可以在目标项目的 `Packages/manifest.json` 的 `dependencies` 中加入：

```json
"com.syad.unitykit": "https://github.com/ShiYuandi/SyadUnityKit.git?path=/Packages/com.syad.unitykit#v0.1.0"
```

地址末尾的 `#v0.1.0` 表示锁定到稳定的 `0.1.0` 版本。更新框架时，应把它改成需要安装的新版本标签。

卸载时，通过 Package Manager 点击 **Remove**，或者从 `manifest.json` 中删除该依赖。卸载前应先移除场景和 Prefab 上依赖本框架的组件。

## 设计边界

- 使用具体 `UIView` 类型标识页面，不使用字符串页面名。
- `UIRoot` 通过 Inspector 或代码显式配置，不使用 `GameObject.Find("Canvas")`。
- `UIService` 只负责创建、显示、隐藏、缓存和释放视图。
- View 通过明确的方法接收数据，通过普通 C# 事件发出用户操作意图。
- 项目级 Controller 根据业务状态决定显示哪个 View。
- 不提供全局 EventBus、Service Locator、单例或反射自动注册。

## 输入命令路由（开发中）

不同项目可能使用键盘、UDP、RFID、Kinect 或串口，但业务层真正关心的通常是“返回”“向左”“确认”等命令。`InputRouter<TCommand>` 用一个显式创建的实例统一转发这些强类型命令，并提供输入开关和全局冷却。

它不会读取具体设备、解析通信协议或切换页面，这些工作仍由项目代码负责。所有命令共享同一段冷却时间，适合防止页面切换动画期间连续触发。

`TryDispatch()` 应从 Unity 主线程调用。如果 UDP、串口等接收器在后台线程工作，应先把解析结果放入线程安全队列，再在 `Update()` 中取出并提交；`InputRouter<TCommand>` 不会自动切换线程。

```csharp
using Syad.UnityKit.Input;
using UnityEngine;

public enum ExhibitCommand
{
    Back,
    Left,
    Right,
    Confirm
}

public sealed class ExhibitInputController : MonoBehaviour
{
    private InputRouter<ExhibitCommand> _inputRouter;

    private void Awake()
    {
        // 每次成功输入后，0.5 秒内忽略后续输入。
        _inputRouter = new InputRouter<ExhibitCommand>(0.5f);
        _inputRouter.CommandReceived += HandleCommand;
    }

    private void Update()
    {
        // 键盘只是一个调试输入源。
        if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _inputRouter.TryDispatch(ExhibitCommand.Left);
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
        {
            _inputRouter.TryDispatch(ExhibitCommand.Right);
        }
    }

    // UDP、RFID 等输入源解析完成后，也调用同一个入口。
    public void OnUdpMessageReceived(string message)
    {
        if (message == "left")
        {
            _inputRouter.TryDispatch(ExhibitCommand.Left);
        }
    }

    private void HandleCommand(ExhibitCommand command)
    {
        // 在这里把命令交给项目状态机或 UI Controller。
        Debug.Log("收到输入命令：" + command);
    }

    private void OnDestroy()
    {
        if (_inputRouter != null)
        {
            _inputRouter.CommandReceived -= HandleCommand;
            _inputRouter.Dispose();
        }
    }
}
```

常用接口：

```csharp
// 尝试转发命令；处于禁用或冷却状态时返回 false。
bool accepted = inputRouter.TryDispatch(command);

// 临时停止和恢复输入。
inputRouter.SetEnabled(false);
inputRouter.SetEnabled(true);

// 清除剩余冷却时间，使下一条命令可以立即通过。
inputRouter.ResetCooldown();

// 释放并清理全部订阅；释放后不能继续使用。
inputRouter.Dispose();
```

`InputRouter<TCommand>` 是项目局部对象，不是全局事件总线。应由项目的组合入口或输入控制器持有，并在该对象销毁时调用 `Dispose()`。

## 场景配置

在 Canvas 下创建下面的结构：

```text
Canvas
└─ UIRoot
   ├─ Background
   ├─ Screen
   ├─ Popup
   └─ Overlay
```

1. 给 `UIRoot` 对象添加 `UIRoot` 组件。
2. 把四个子对象的 `RectTransform` 拖到组件的对应字段。
3. 通过 **Assets > Create > SYAD Unity Kit > UI > 视图目录** 创建 `UIViewCatalog`。
4. 把页面 Prefab 登记到 Catalog。Prefab 根对象必须挂载一个具体的 `UIView` 子类。
5. 在项目的组合入口中创建并持有 `UIService`。

## 创建 UIService

```csharp
using Syad.UnityKit.UI;
using UnityEngine;

public sealed class GameUIBootstrap : MonoBehaviour
{
    [SerializeField] private UIRoot _root;
    [SerializeField] private UIViewCatalog _catalog;

    public UIService UI { get; private set; }

    private void Awake()
    {
        UI = new UIService(_root, _catalog);
    }

    private void OnDestroy()
    {
        if (UI != null)
        {
            // 组合入口销毁时，释放该服务创建的全部页面。
            UI.Dispose();
        }
    }
}
```

## 编写 View

View 负责渲染数据，并把按钮点击转换为用户意图。View 不应查找全局游戏管理器，也不应直接修改游戏状态。

```csharp
using System;
using Syad.UnityKit.UI;
using UnityEngine;
using UnityEngine.UI;

public sealed class ResultView : UIView
{
    [SerializeField] private Text _scoreText;
    [SerializeField] private Button _continueButton;

    public event Action ContinueRequested;

    public void SetData(int score)
    {
        _scoreText.text = "得分：" + score;
    }

    protected override void OnCreated()
    {
        // 每个实例只初始化一次。
        _continueButton.onClick.AddListener(HandleContinueClicked);
    }

    protected override void OnReleased()
    {
        // 页面释放时清理自身持有的监听和事件。
        _continueButton.onClick.RemoveListener(HandleContinueClicked);
        ContinueRequested = null;
    }

    private void HandleContinueClicked()
    {
        if (ContinueRequested != null)
        {
            ContinueRequested();
        }
    }
}
```

## 显示并绑定数据

每次显示都可以重新绑定数据。缓存页面可能被重复显示，因此事件订阅应保持幂等：

```csharp
ResultView view = uiService.Show<ResultView>(candidate =>
{
    // bind 回调发生在页面激活和 OnShown 之前。
    candidate.SetData(score);
});

// 先退订再订阅，避免缓存页面重复显示后产生重复回调。
view.ContinueRequested -= HandleContinueRequested;
view.ContinueRequested += HandleContinueRequested;
```

`Show` 的 bind 回调用于每次显示时都可能变化的数据，不是“仅创建时执行一次”的初始化回调。如果订阅者的生命周期短于 View，应在订阅者自己的 `OnDestroy` 或 `Dispose` 中退订。View 应在 `OnReleased` 中清理自己拥有的事件。

## 常用接口

```csharp
// 创建或显示页面。
StartView view = uiService.Show<StartView>();

// 隐藏最新的可见 StartView。
uiService.Hide<StartView>();

// 隐藏指定实例。
uiService.Hide(view);

// 查询页面是否已经存在，默认也会查询隐藏的缓存页面。
StartView existing;
bool found = uiService.TryGet<StartView>(out existing);

// 忽略缓存策略，彻底释放指定页面。
uiService.Release(view);

// 释放当前服务持有的全部页面。
uiService.ReleaseAll();

// 关闭整个服务；调用后不能继续使用该 UIService。
uiService.Dispose();
```

## 管理策略

- `KeepAlive`：隐藏时停用并缓存实例，下次显示可以复用。
- `ReleaseOnHide`：隐藏时释放并销毁实例。
- `SingleInstance`：重复显示时复用当前被跟踪的单个实例。
- `AllowMultiple`：允许同时显示多个实例；存在隐藏的 `KeepAlive` 实例时优先复用。

`Hide<TView>()` 只隐藏最新的可见实例，`HideAll<TView>()` 会隐藏该类型的全部可见实例。缓存页面不再需要时，可以调用 `Release(view)` 提前释放。

## 推荐的项目职责关系

```text
用户输入
→ 项目状态机
→ 业务状态变化
→ 项目级 UI Controller
→ UIService / View 更新
```

固定 HUD、准星或玩法操作条可以继续直接放在场景中。Catalog 适合登记需要导航和动态管理的页面、弹窗，不要求所有 UI 对象都成为动态 View。

## 测试

包测试位于 `Tests/Runtime`。如果目标项目需要显示包测试，请在目标项目的 `Packages/manifest.json` 中加入：

```json
"testables": [
  "com.syad.unitykit"
]
```

然后通过 Unity Test Runner 运行 EditMode 测试。
