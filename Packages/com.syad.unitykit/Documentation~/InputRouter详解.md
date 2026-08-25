# InputRouter 详解

本文介绍 `Syad.UnityKit.Input.InputRouter<TCommand>` 的设计目的、基本用法、生命周期、冷却机制以及与键盘、UDP、RFID、Kinect 等输入源的协作方式。

适用版本：SYAD Unity Kit `0.2.0` 及以上。

## 目录

- [一、InputRouter 解决什么问题](#一inputrouter-解决什么问题)
- [二、它不是什么](#二它不是什么)
- [三、整体输入流程](#三整体输入流程)
- [四、TCommand 是什么](#四tcommand-是什么)
- [五、创建 InputRouter](#五创建-inputrouter)
- [六、CommandReceived 事件](#六commandreceived-事件)
- [七、TryDispatch 的执行过程](#七trydispatch-的执行过程)
- [八、输入冷却](#八输入冷却)
- [九、启用、禁用和重置冷却](#九启用禁用和重置冷却)
- [十、Dispose 和生命周期](#十dispose-和生命周期)
- [十一、完整键盘示例](#十一完整键盘示例)
- [十二、接入 UDP 等外部输入](#十二接入-udp-等外部输入)
- [十三、后台线程注意事项](#十三后台线程注意事项)
- [十四、携带参数的命令](#十四携带参数的命令)
- [十五、与 UI Controller 配合](#十五与-ui-controller-配合)
- [十六、为什么它不是全局 EventBus](#十六为什么它不是全局-eventbus)
- [十七、常见误区](#十七常见误区)
- [十八、API 速查](#十八api-速查)
- [十九、当前测试覆盖](#十九当前测试覆盖)
- [二十、建议练习顺序](#二十建议练习顺序)

## 一、InputRouter 解决什么问题

在互动项目中，同一个业务操作经常可以由多种设备触发：

```text
键盘 LeftArrow
UDP 消息 "left"
RFID 卡片编号 1
Kinect 向左手势
```

这些原始信号可能都表示同一个项目操作：

```text
向左切换
```

如果每个输入源都直接调用业务方法，代码可能逐渐变成：

```csharp
private void Update()
{
    if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
    {
        GoLeft();
    }
}

private void OnUdpMessage(string message)
{
    if (message == "left")
    {
        GoLeft();
    }
}

private void OnRfidReceived(int cardId)
{
    if (cardId == 1)
    {
        GoLeft();
    }
}
```

随着项目变大，输入管理类还可能同时承担：

- 读取键盘；
- 解析通信消息；
- 控制页面；
- 修改业务状态；
- 播放动画；
- 输入防抖。

最终容易形成职责过多的 `InputManager`。

`InputRouter<TCommand>` 把其中可以复用的部分抽出来：

> 接收已经解析好的项目命令，经过启用状态和冷却检查后，将命令通知给项目控制器。

## 二、它不是什么

`InputRouter<TCommand>` 不是 Unity New Input System 的替代品，也不会自动读取设备。

它不负责：

- 调用 `UnityEngine.Input.GetKeyDown()`；
- 配置 Unity Input Action；
- 解析 UDP 或串口协议；
- 识别 Kinect 手势；
- 决定按键映射；
- 打开或关闭 UI 页面；
- 修改游戏状态；
- 保存业务数据。

可以这样区分：

```text
UnityEngine.Input / UDP / RFID / Kinect
负责获得原始输入

项目输入适配代码
负责把原始输入转换成项目命令

InputRouter<TCommand>
负责命令门控、冷却和转发

项目 Controller 或状态机
负责收到命令后执行什么业务
```

## 三、整体输入流程

```text
键盘 LeftArrow ─┐
UDP "left"     ├──→ DemoCommand.Left
RFID 卡片      ┘
                          ↓
                InputRouter.TryDispatch()
                          ↓
                检查是否启用、是否冷却
                          ↓
                    CommandReceived
                          ↓
                  项目 Controller 处理
```

不同设备只负责产生统一命令。后面的业务逻辑不需要知道命令来自键盘还是外部硬件。

### InputSource 与 SyadInputSource 的关系

框架 Runtime 还提供：

```csharp
public abstract class InputSource<TCommand> : MonoBehaviour
```

它统一声明 `CommandDetected` 事件，并向具体输入源提供受保护的 `RaiseCommand()`。它不读取设备、不处理冷却，也不执行业务。

一键创建工具会在项目 `Assets` 中生成一个很薄的桥接类：

```csharp
public abstract class SyadInputSource
    : InputSource<SyadInputCommand>
{
}
```

这样框架无需知道项目有哪些命令，项目又可以在 Inspector 中继续使用 `SyadInputSource[]`。新增 RFID、触摸或其他设备时，应继承项目侧 `SyadInputSource`，在识别出命令后调用 `RaiseCommand()`。

完整职责关系是：

```text
具体设备输入源
→ InputSource<TCommand>.CommandDetected
→ 项目输入控制器
→ InputRouter<TCommand>.TryDispatch
→ 项目业务
```

## 四、TCommand 是什么

类定义为：

```csharp
public sealed class InputRouter<TCommand> : IDisposable
```

`TCommand` 是一个泛型类型参数，表示当前路由器处理的命令类型。

先定义项目命令：

```csharp
public enum ExhibitCommand
{
    Back,
    Left,
    Right,
    Confirm
}
```

然后创建：

```csharp
InputRouter<ExhibitCommand> inputRouter;
```

对于这个实例，所有 `TCommand` 都代表 `ExhibitCommand`。因此：

```csharp
public bool TryDispatch(TCommand command)
```

可以理解为：

```csharp
public bool TryDispatch(ExhibitCommand command)
```

只能提交对应类型的命令：

```csharp
inputRouter.TryDispatch(ExhibitCommand.Left);
inputRouter.TryDispatch(ExhibitCommand.Confirm);
```

以下代码无法编译：

```csharp
inputRouter.TryDispatch(123);
inputRouter.TryDispatch("left");
```

### 为什么推荐枚举而不是字符串

字符串容易出现大小写和拼写问题：

```csharp
"left"
"Left"
"lef"
```

枚举由编译器检查：

```csharp
ExhibitCommand.Left
```

原始通信消息仍然可以是字符串，但应先在项目代码中解析成枚举，再交给路由器。

## 五、创建 InputRouter

### 不使用冷却

```csharp
private InputRouter<ExhibitCommand> _inputRouter;

private void Awake()
{
    _inputRouter = new InputRouter<ExhibitCommand>();
}
```

默认冷却时间是 `0` 秒，因此连续命令都可以通过。

### 设置冷却

```csharp
private void Awake()
{
    _inputRouter = new InputRouter<ExhibitCommand>(0.5f);
}
```

这表示每次成功提交命令后，接下来的 `0.5` 秒内拒绝其他命令。

### 冷却时间不能为负数

```csharp
new InputRouter<ExhibitCommand>(-1f);
```

会抛出 `ArgumentOutOfRangeException`，因为负数冷却没有明确含义。

### 自定义时间来源

InputRouter 还有一个构造函数：

```csharp
public InputRouter(float cooldownSeconds, Func<float> timeProvider)
```

普通项目通常不需要使用。它主要用于测试，让测试代码可以手动控制时间，而不需要真实等待一秒。

默认时间来源是：

```csharp
Time.unscaledTime
```

它不受 `Time.timeScale` 影响。即使项目暂停，输入冷却仍然能够正常结束。

## 六、CommandReceived 事件

路由器通过事件通知订阅者：

```csharp
public event Action<TCommand> CommandReceived;
```

当 `TCommand` 是 `ExhibitCommand` 时，可以理解成：

```csharp
public event Action<ExhibitCommand> CommandReceived;
```

订阅事件：

```csharp
private void Awake()
{
    _inputRouter = new InputRouter<ExhibitCommand>(0.5f);
    _inputRouter.CommandReceived += HandleCommand;
}
```

处理命令：

```csharp
private void HandleCommand(ExhibitCommand command)
{
    Debug.Log("收到命令：" + command);
}
```

提交命令：

```csharp
_inputRouter.TryDispatch(ExhibitCommand.Left);
```

如果命令通过门控，最终会执行：

```csharp
HandleCommand(ExhibitCommand.Left);
```

## 七、TryDispatch 的执行过程

方法定义：

```csharp
public bool TryDispatch(TCommand command)
```

`Try` 表示这次提交不保证成功。

返回值含义：

```text
true  命令通过门控
false 命令因为禁用或冷却被拒绝
```

使用返回值：

```csharp
bool accepted = _inputRouter.TryDispatch(ExhibitCommand.Left);

if (accepted)
{
    Debug.Log("输入通过");
}
else
{
    Debug.Log("输入被忽略");
}
```

内部流程：

```text
TryDispatch(command)
        ↓
路由器已经 Dispose？
├─ 是：抛出异常
└─ 否
        ↓
IsEnabled？
├─ false：返回 false
└─ true
        ↓
还在冷却？
├─ 是：返回 false
└─ 否
        ↓
记录下一次允许输入的时间
        ↓
触发 CommandReceived(command)
        ↓
返回 true
```

路由器会先更新冷却状态，再通知订阅者。这样即使订阅者在回调中再次提交命令，也不能绕过当前冷却。

即使没有订阅者，只要命令通过门控，`TryDispatch()` 仍然返回 `true`。这个返回值表示“路由器接受了命令”，不表示“一定存在业务处理者”。

## 八、输入冷却

假设冷却时间为一秒：

```csharp
_inputRouter = new InputRouter<ExhibitCommand>(1f);
```

执行过程：

```text
0.0 秒：提交 Left
→ 接受
→ 下一次允许时间是 1.0 秒

0.2 秒：提交 Right
→ 拒绝

0.9 秒：提交 Confirm
→ 拒绝

1.0 秒：提交 Right
→ 接受
→ 下一次允许时间是 2.0 秒
```

所有命令共享同一段冷却时间。

`Left` 被接受后，冷却期间的 `Right`、`Confirm` 也会被拒绝。这适合页面切换、硬件触发和动画播放期间的整体输入锁定。

当前第一版不提供“每种命令分别冷却”。如果未来真实项目反复需要这种规则，再考虑增加，而不是提前扩展。

## 九、启用、禁用和重置冷却

### IsEnabled

创建后默认允许输入：

```csharp
inputRouter.IsEnabled == true
```

外部代码只能读取，不能直接修改。

### 禁用输入

```csharp
_inputRouter.SetEnabled(false);
```

禁用后，`TryDispatch()` 返回 `false`，也不会触发 `CommandReceived`。

适合：

- 正在播放不能打断的动画；
- 正在加载场景；
- 等待服务器响应；
- 硬件正在复位；
- 当前业务状态不允许交互。

### 恢复输入

```csharp
_inputRouter.SetEnabled(true);
```

默认不会清除之前剩余的冷却时间。

### 恢复并清除冷却

```csharp
_inputRouter.SetEnabled(true, true);
```

第二个参数表示同时清除冷却，使下一条命令可以立即通过。

### 单独清除冷却

```csharp
_inputRouter.ResetCooldown();
```

`ResetCooldown()` 不会改变 `IsEnabled`。如果路由器仍然处于禁用状态，命令依旧不能通过。

## 十、Dispose 和生命周期

`InputRouter<TCommand>` 实现 `IDisposable`：

```csharp
public sealed class InputRouter<TCommand> : IDisposable
```

使用结束后调用：

```csharp
_inputRouter.Dispose();
```

它会：

- 清除 `CommandReceived` 的全部订阅；
- 设置 `IsEnabled = false`；
- 设置 `IsDisposed = true`。

推荐生命周期：

```text
Awake
→ 创建路由器
→ 订阅 CommandReceived

运行期间
→ TryDispatch 命令

OnDestroy
→ 取消订阅
→ Dispose
```

标准写法：

```csharp
private void OnDestroy()
{
    if (_inputRouter != null)
    {
        _inputRouter.CommandReceived -= HandleCommand;
        _inputRouter.Dispose();
    }
}
```

`Dispose()` 可以重复调用，不会重复报错。但释放后调用 `TryDispatch()`、`SetEnabled()` 或 `ResetCooldown()` 会抛出 `ObjectDisposedException`。

## 十一、完整键盘示例

```csharp
using Syad.UnityKit.Input;
using UnityEngine;

public enum DemoCommand
{
    Left,
    Right,
    Confirm
}

public sealed class InputRouterDemoController : MonoBehaviour
{
    private InputRouter<DemoCommand> _inputRouter;

    private void Awake()
    {
        // 每次成功输入后冷却一秒。
        _inputRouter = new InputRouter<DemoCommand>(1f);
        _inputRouter.CommandReceived += HandleCommand;
    }

    private void Update()
    {
        if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SubmitCommand(DemoCommand.Left);
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
        {
            SubmitCommand(DemoCommand.Right);
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
        {
            SubmitCommand(DemoCommand.Confirm);
        }
    }

    private void SubmitCommand(DemoCommand command)
    {
        bool accepted = _inputRouter.TryDispatch(command);

        if (accepted)
        {
            Debug.Log("输入通过：" + command);
        }
        else
        {
            Debug.Log("输入被冷却或禁用：" + command);
        }
    }

    private void HandleCommand(DemoCommand command)
    {
        switch (command)
        {
            case DemoCommand.Left:
                Debug.Log("业务处理：向左");
                break;

            case DemoCommand.Right:
                Debug.Log("业务处理：向右");
                break;

            case DemoCommand.Confirm:
                Debug.Log("业务处理：确认");
                break;
        }
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

这里仍然由项目代码读取键盘，InputRouter 只接收 `DemoCommand`。

## 十二、接入 UDP 等外部输入

假设 UDP 消息包含：

```text
left
right
confirm
```

项目代码先解析字符串：

```csharp
private void HandleUdpMessage(string message)
{
    switch (message)
    {
        case "left":
            _inputRouter.TryDispatch(DemoCommand.Left);
            break;

        case "right":
            _inputRouter.TryDispatch(DemoCommand.Right);
            break;

        case "confirm":
            _inputRouter.TryDispatch(DemoCommand.Confirm);
            break;
    }
}
```

键盘和 UDP 最终进入同一个入口：

```text
键盘 LeftArrow
→ DemoCommand.Left
→ TryDispatch

UDP "left"
→ DemoCommand.Left
→ TryDispatch
```

后面的业务处理不需要区分来源。

## 十三、后台线程注意事项

`TryDispatch()` 应从 Unity 主线程调用。

某些 UDP 或串口接收器会在后台线程收到消息。如果直接在后台线程执行：

```csharp
_inputRouter.TryDispatch(command);
```

那么 `CommandReceived` 的订阅者也会在后台线程执行。订阅者一旦操作 GameObject、UI、Animator 等 Unity 对象，就可能产生线程问题。

推荐流程：

```text
UDP 后台线程收到消息
        ↓
放入线程安全队列
        ↓
Unity 主线程 Update 取出消息
        ↓
解析成项目命令
        ↓
InputRouter.TryDispatch(command)
```

InputRouter 第一版不会自动切换线程，因为不同项目使用的网络库和主线程调度方案不同。

## 十四、携带参数的命令

简单项目推荐使用枚举。如果命令需要携带卡片编号、内容索引等数据，可以使用结构体。

```csharp
public enum ExhibitCommandType
{
    SelectItem,
    Back
}

public struct ExhibitCommand
{
    public ExhibitCommandType Type;
    public int ItemIndex;
}
```

创建路由器：

```csharp
InputRouter<ExhibitCommand> inputRouter;
```

提交：

```csharp
ExhibitCommand command = new ExhibitCommand
{
    Type = ExhibitCommandType.SelectItem,
    ItemIndex = 3
};

inputRouter.TryDispatch(command);
```

处理：

```csharp
private void HandleCommand(ExhibitCommand command)
{
    if (command.Type == ExhibitCommandType.SelectItem)
    {
        Debug.Log("选择编号：" + command.ItemIndex);
    }
}
```

第一阶段建议先使用枚举，熟悉后再根据真实需求使用结构体。

## 十五、与 UI Controller 配合

InputRouter 不直接依赖 UI，但项目 Controller 可以同时组合 Input 和 UI。

定义 UI Demo 命令：

```csharp
public enum UIViewDemoCommand
{
    ShowStartView,
    ShowView2,
    ShowView3,
    CloseCurrentView
}
```

键盘映射为命令：

```csharp
if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
{
    _inputRouter.TryDispatch(UIViewDemoCommand.ShowStartView);
}
```

Controller 处理：

```csharp
private void HandleCommand(UIViewDemoCommand command)
{
    switch (command)
    {
        case UIViewDemoCommand.ShowStartView:
            ShowView<StartView>();
            break;

        case UIViewDemoCommand.ShowView2:
            ShowView<View2>();
            break;

        case UIViewDemoCommand.ShowView3:
            ShowView<View3>();
            break;

        case UIViewDemoCommand.CloseCurrentView:
            CloseCurrentView();
            break;
    }
}
```

职责关系：

```text
键盘或硬件
→ 项目命令
→ InputRouter
→ UI Demo Controller
→ UIService
→ UIView
```

为了单独理解 Input 模块，建议先在独立 Input Demo 场景中只输出日志，不立即依赖 UI。

## 十六、为什么它不是全局 EventBus

InputRouter 使用普通 C# 事件，但它不是全局事件总线：

- 没有静态实例；
- 没有 `InputRouter.Instance`；
- 不使用任意字符串事件名；
- 一个实例只处理一种 `TCommand`；
- 由项目入口显式创建；
- 由创建者显式释放。

例如：

```csharp
private InputRouter<DemoCommand> _inputRouter;
```

这个路由器只属于持有它的 Controller，不会自动影响整个项目。

## 十七、常见误区

### 误区一：认为 InputRouter 会自动监听按键

错误理解：

```text
创建 InputRouter 后，它会自动监听 LeftArrow
```

实际情况：

```text
项目代码监听 LeftArrow
→ 主动调用 TryDispatch(DemoCommand.Left)
```

### 误区二：把原始 UDP 字符串直接当业务命令到处传递

不推荐：

```csharp
inputRouter.TryDispatch("LEFT_COMMAND_01");
```

推荐先解析：

```csharp
inputRouter.TryDispatch(DemoCommand.Left);
```

### 误区三：在 CommandReceived 中继续解析硬件协议

协议解析应该发生在提交之前。`CommandReceived` 收到的应当已经是项目可以理解的命令。

### 误区四：认为 SetEnabled(false) 会清除冷却

禁用只会拒绝输入，不会默认清除冷却。需要同时清除时使用：

```csharp
inputRouter.SetEnabled(false, true);
```

或者：

```csharp
inputRouter.ResetCooldown();
```

### 误区五：Dispose 后继续使用

`Dispose()` 是终止状态。释放后不能重新启用，应创建新的 InputRouter 实例。

### 误区六：从后台线程直接操作 Unity 对象

InputRouter 不切换线程。外部设备消息应先回到 Unity 主线程，再调用 `TryDispatch()`。

## 十八、API 速查

### 创建

```csharp
InputRouter<DemoCommand> inputRouter =
    new InputRouter<DemoCommand>(1f);
```

### 订阅

```csharp
inputRouter.CommandReceived += HandleCommand;
```

### 提交

```csharp
bool accepted = inputRouter.TryDispatch(DemoCommand.Left);
```

### 禁用

```csharp
inputRouter.SetEnabled(false);
```

### 恢复

```csharp
inputRouter.SetEnabled(true);
```

### 恢复并清除冷却

```csharp
inputRouter.SetEnabled(true, true);
```

### 清除冷却

```csharp
inputRouter.ResetCooldown();
```

### 查询状态

```csharp
bool enabled = inputRouter.IsEnabled;
bool disposed = inputRouter.IsDisposed;
float cooldown = inputRouter.CooldownSeconds;
```

### 释放

```csharp
inputRouter.CommandReceived -= HandleCommand;
inputRouter.Dispose();
```

## 十九、当前测试覆盖

自动化测试位于：

```text
Packages/com.syad.unitykit/Tests/Runtime/InputRouterTests.cs
```

当前覆盖：

- 第一条命令能够正常转发；
- 冷却期间拒绝后续命令；
- 冷却结束后恢复输入；
- 禁用状态拒绝命令；
- 恢复后能够继续接收；
- `ResetCooldown()` 可以立即清除冷却；
- `Dispose()` 清理订阅并禁止继续使用；
- 负数冷却时间会抛出异常。

这些测试属于运行时测试程序集，应在 Unity Test Runner 的 PlayMode 页面运行。

## 二十、建议练习顺序

### 第一步：只使用键盘和日志

创建独立 Input Demo 场景：

```text
LeftArrow  → DemoCommand.Left
RightArrow → DemoCommand.Right
Space      → DemoCommand.Confirm
```

在 `HandleCommand()` 中只输出日志。

### 第二步：观察冷却

设置一秒冷却，快速连续按键，比较 `TryDispatch()` 返回的 `true` 和 `false`。

### 第三步：练习启用和禁用

增加按键：

```text
T → 切换 IsEnabled
R → ResetCooldown
```

### 第四步：模拟外部输入

编写一个普通方法接收字符串：

```csharp
SimulateUdpMessage("left");
```

把字符串解析成 `DemoCommand.Left` 后提交。

### 第五步：再与 UI Controller 组合

等 Input 模块的行为清楚后，再让命令调用 `ShowView<TView>()` 或 `CloseCurrentView()`。

## 总结

`InputRouter<TCommand>` 的边界可以用一句话概括：

> 它不负责输入设备发出了什么，也不负责项目接下来做什么；它只负责把已经解析好的强类型命令，经过启用状态和冷却检查后，统一交给项目 Controller。
