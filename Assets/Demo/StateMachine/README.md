# StateMachine Demo

打开 `Scenes/StateMachineDemo.unity` 并进入 Play Mode。

## 脚本职责

```text
StateMachineDemoController.Update
读取演示按键并驱动 StateMachine.Update
        ↓
StateMachine<DemoState>
只负责当前状态和生命周期转发
        ↓
IdleDemoState / RunningDemoState
实现项目自己的 Enter、Update 和 Exit
```

- `DemoState.cs`：Demo 的项目级状态基类，实现框架 `IState`。
- `IdleDemoState.cs`：空闲状态。
- `RunningDemoState.cs`：运行状态，在项目状态中自行使用 `Time.deltaTime`。
- `StateMachineDemoController.cs`：创建状态机和状态，决定何时切换，并在销毁时停止状态机。

## 操作

- `Space`：在空闲状态和运行状态之间切换。
- `R`：重新进入当前状态，可观察 `Exit → Enter`。
- `S`：停止状态机。
- `I`：状态机停止后，重新从空闲状态初始化。

场景使用 `OnGUI` 显示调试信息，因此不依赖 SYAD UI 模块。
