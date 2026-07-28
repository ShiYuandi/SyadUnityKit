# Input Demo

打开 `Scenes/InputDemo.unity` 并进入 Play Mode。

## 脚本职责

```text
KeyboardInputSource.Update
负责监听原始键盘输入
        ↓ CommandDetected
InputRouterDemoController.SubmitCommand
负责调用 InputRouter.TryDispatch
        ↓ CommandReceived
InputRouterDemoController.HandleCommand
负责处理通过门控的命令
```

- `InputDemoCommand.cs`：定义 Demo 使用的强类型命令。
- `KeyboardInputSource.cs`：只监听键盘并发出命令或调试请求。
- `InputRouterDemoController.cs`：创建和释放 InputRouter，提交命令并处理结果。

`KeyboardInputSource` 不知道冷却规则，也不知道命令最终执行什么业务。`InputRouterDemoController` 不直接监听具体按键。

## 操作

- `←`：提交 `Left` 命令。
- `→`：提交 `Right` 命令。
- `Space`：提交 `Confirm` 命令。
- `T`：启用或禁用 InputRouter；恢复输入时同时重置冷却。
- `R`：重置冷却，使下一条命令可以立即通过。

`T` 和 `R` 控制路由器本身，因此它们作为独立调试请求直接交给 Controller，不进入被它们控制的 InputRouter。

场景默认设置一秒全局冷却。快速连续提交命令时，第一条命令通过，冷却期间的后续命令会被拒绝。

场景使用 `OnGUI` 显示调试信息，目的是让 Input 示例不依赖 SYAD UI 模块。正式项目应使用自己的 UI 或日志系统展示状态。
