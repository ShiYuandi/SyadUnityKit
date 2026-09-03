# StateMachine 设计方案

## 一、目标

为 SYAD Unity Kit 增加一个小型、显式的通用状态机模块，可用于角色、敌人、小游戏流程、页面流程和设备连接流程。

第一版参考 `RPC-Udemy-Course` 项目的状态机结构，只负责当前状态和 `Enter / Update / Exit` 生命周期，不把任何具体项目业务放入 UPM Runtime。

## 二、设计边界

状态机内核只负责：

- 设置初始状态；
- 保存当前状态；
- 转发每帧 `Update()`；
- 按照固定顺序切换状态；
- 停止时退出当前状态。

状态机内核不负责：

- 读取 Unity 键盘、UDP 或其他设备；
- 显示或隐藏 UI；
- 自动查找、注册或创建状态；
- 使用全局 EventBus 通知状态变化；
- 维护状态转换表或自动判断转换条件；
- 依赖 `MonoBehaviour`、`ScriptableObject` 或反射；
- 提供异步状态、并行状态或分层状态机。

## 三、公开 API

命名空间：

```csharp
Syad.UnityKit.StateMachine
```

状态接口：

```csharp
public interface IState
{
    void Enter();
    void Update();
    void Exit();
}
```

通用状态机：

```csharp
public sealed class StateMachine<TState>
    where TState : class, IState
{
    public TState CurrentState { get; }

    public void Initialize(TState startState);
    public void ChangeState(TState newState);
    public void Update();
    public void Stop();
}
```

`TState` 是项目自己的状态基类或接口类型。例如：

```csharp
StateMachine<PlayerState>
StateMachine<EnemyState>
StateMachine<FishingState>
```

使用项目专用基类时，基类实现框架的 `IState`，各具体状态再继承该基类。

## 四、生命周期

### Initialize

```text
CurrentState = startState
→ CurrentState.Enter()
```

`Initialize()` 用于第一次启动状态机。已经初始化时不应再次调用；需要普通切换时使用 `ChangeState()`，需要重新启动时先调用 `Stop()`。

### Update

```text
StateMachine.Update()
→ CurrentState.Update()
```

当前没有状态时，`Update()` 直接返回。

框架不传入 `deltaTime`。具体项目状态若需要计时，可自行读取 `Time.deltaTime`、`Time.unscaledDeltaTime` 或项目注入的时间服务。

### ChangeState

```text
旧状态.Exit()
→ CurrentState = newState
→ 新状态.Enter()
```

即使 `newState` 与当前状态是同一个实例，也会完整执行：

```text
Exit()
→ Enter()
```

这表示使用者可以通过再次切换到当前状态，明确要求它重新进入。

### Stop

```text
CurrentState.Exit()
→ CurrentState = null
```

没有当前状态时，`Stop()` 直接返回，因此可以安全重复调用。

## 五、错误处理

- `Initialize(null)` 抛出 `ArgumentNullException`；
- 已有当前状态时再次调用 `Initialize()` 抛出 `InvalidOperationException`；
- 未初始化时调用 `ChangeState()` 抛出 `InvalidOperationException`；
- `ChangeState(null)` 抛出 `ArgumentNullException`；
- `Update()` 和 `Stop()` 在未初始化状态下安全返回；
- 状态自己在 `Enter / Update / Exit` 中抛出的异常原样向上传递，状态机不隐藏业务错误。

## 六、项目使用方式

```csharp
using Syad.UnityKit.StateMachine;
using UnityEngine;

public sealed class PlayerController : MonoBehaviour
{
    private StateMachine<PlayerState> _stateMachine;

    private void Awake()
    {
        _stateMachine = new StateMachine<PlayerState>();

        PlayerIdleState idleState =
            new PlayerIdleState(this, _stateMachine);

        _stateMachine.Initialize(idleState);
    }

    private void Update()
    {
        _stateMachine.Update();
    }

    private void OnDestroy()
    {
        _stateMachine.Stop();
    }
}
```

状态对象通过构造函数显式接收 Controller、状态机或项目上下文，框架不使用全局单例帮它查找依赖。

## 七、测试范围

第一版至少覆盖：

1. `Initialize()` 设置当前状态并调用一次 `Enter()`；
2. `Update()` 只转发给当前状态；
3. `ChangeState()` 按 `Exit → 更换 → Enter` 的顺序执行；
4. 切换到同一实例时仍执行 `Exit → Enter`；
5. `Stop()` 退出状态并清空 `CurrentState`；
6. 未初始化时 `Update()` 和 `Stop()` 不抛出异常；
7. 空状态和错误调用顺序会抛出明确异常；
8. `Stop()` 后可以再次通过 `Initialize()` 启动。

## 八、第一版交付内容

- `Runtime/StateMachine/IState.cs`；
- `Runtime/StateMachine/StateMachine.cs`；
- 包内 PlayMode 测试；
- 一个独立的 StateMachine Demo 场景和中文示例代码；
- StateMachine Editor 一键模板，生成项目状态基类、空闲状态和控制器；
- 用户使用文档与 README 入口。

模板只生成最小可运行骨架，不预先放入“运行状态”或其他项目业务；用户可以在 `Assets` 中继续扩展。
