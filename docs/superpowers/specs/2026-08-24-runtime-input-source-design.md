# Runtime InputSource 设计

## 背景

当前 `SyadInputSource` 位于项目生成目录，统一提供输入命令事件和安全触发方法。它直接依赖项目生成的 `SyadInputCommand`，因此这部分通用能力尚未进入 UPM Runtime。

本次调整把输入源的通用行为下沉到框架中，同时保留项目侧可挂载、可序列化的非泛型桥接类。现有键盘输入、UDP 输入、输入路由和项目业务之间的职责不变。

## 目标

- 在 `Syad.UnityKit.Input` 中新增强类型输入源基类 `InputSource<TCommand>`。
- 项目侧 `SyadInputSource` 只负责指定项目使用的 `SyadInputCommand` 类型。
- 保留现有 `SyadInputSource[]` Inspector 配置方式。
- 保持现有场景组件和序列化引用有效。
- 同时更新当前生成代码与包内生成模板。
- 兼容 Unity 2019.4 和 Unity 2022.3。

## 不在本次范围内

- 不把 `KeyboardInputSource` 或 `UdpInputSource` 移入 Runtime。
- 不修改 `InputRouter<TCommand>` 的门控、冷却和释放行为。
- 不引入单例、EventBus、Service Locator 或反射自动注册。
- 不使用字符串或 `object` 表示命令。
- 不改变 `SyadInputController` 的业务处理方式。

## 方案选择

采用“Runtime 泛型基类 + 项目侧非泛型桥接类”。

未采用接口方案，因为 Unity 2019.4 的 Inspector 不能直接序列化接口数组，会降低一键创建模板的易用性。未采用非泛型命令方案，因为它会失去编译期类型检查。

## 架构

```text
Packages/com.syad.unitykit/Runtime/Input
└─ InputSource<TCommand>
   ├─ CommandDetected
   ├─ RaiseCommand(TCommand)
   └─ OnDestroy()

Assets/SyadUnityKit/Generated/Input
└─ SyadInputSource : InputSource<SyadInputCommand>
   ├─ KeyboardInputSource
   └─ UdpInputSource
```

### Runtime：InputSource<TCommand>

`InputSource<TCommand>` 是抽象的泛型 `MonoBehaviour`，只承担以下职责：

- 声明 `Action<TCommand>` 类型的 `CommandDetected` 事件；
- 向具体输入源提供受保护的 `RaiseCommand(TCommand command)`；
- 在销毁时清除事件订阅，避免输入源销毁后继续持有订阅对象。

它不知道键盘、UDP、RFID 等设备，也不知道任何项目业务。

### 项目侧：SyadInputSource

项目生成的 `SyadInputSource` 保持原有类名、文件名和命名空间，只继承：

```csharp
InputSource<SyadInputCommand>
```

该桥接类不重复声明事件和触发方法。具体输入源继续继承 `SyadInputSource`，因此无需直接接触泛型参数，`SyadInputController` 也仍可在 Inspector 中使用 `SyadInputSource[]`。

## 数据流

```text
键盘、UDP 或其他设备数据
→ 具体 InputSource 识别项目命令
→ RaiseCommand(SyadInputCommand)
→ SyadInputController.SubmitCommand()
→ InputRouter<SyadInputCommand>.TryDispatch()
→ SyadInputController.HandleCommand()
→ 项目业务
```

`InputSource<TCommand>` 只负责“发现了一条命令”的通知。输入开关、冷却和统一转发仍由 `InputRouter<TCommand>` 负责；命令对应的业务仍由项目控制器负责。

## 生命周期与错误处理

- 没有事件订阅者时，`RaiseCommand` 安全返回，不抛出异常。
- 输入源销毁时清空 `CommandDetected`。
- 项目控制器仍应在自身销毁时主动退订输入源，这是明确的对称清理；输入源的销毁清理是额外保障。
- Runtime 基类不记录日志，因为它无法判断未订阅是否属于错误。
- 不捕获订阅者回调抛出的异常，避免框架隐藏项目业务错误。

## 场景与序列化兼容

现有场景引用的是项目侧具体组件及 `SyadInputSource[]` 字段。本次保留以下内容不变：

- `SyadUnityKit.Generated.SyadInputSource` 的完整类型名；
- `KeyboardInputSource` 和 `UdpInputSource` 的完整类型名；
- `SyadInputController._inputSources` 的字段名和字段类型。

因此不需要重新配置场景。实施后仍需在 Unity 2019.4 中重新导入脚本并打开测试场景，确认组件和数组引用没有变成 Missing。

## 模板同步

需要同步修改两处项目侧代码：

1. 当前项目的 `Assets/SyadUnityKit/Generated/Input/SyadInputSource.cs`；
2. UPM Editor 模板中的 `Templates~/Input/SyadInputSource.cs.txt`。

一键创建 Input 时，工具仍生成项目侧桥接类。这样具体项目可以拥有自己的命令枚举，又能复用 Runtime 的输入源行为。

## 测试与验收

Runtime 测试至少覆盖：

1. 具体非泛型测试子类能够继承 `InputSource<TCommand>` 并作为 Unity 组件创建；
2. `RaiseCommand` 能把同一个强类型命令传给订阅者；
3. 没有订阅者时调用 `RaiseCommand` 不报错；
4. 输入源销毁后清理事件订阅；
5. 现有 `InputRouter<TCommand>` 测试继续通过。

集成验证包括：

- Unity 2019.4 编译无错误；
- Runtime Test Runner 全部通过；
- 当前生成代码与包内模板都能编译；
- `New Scene` 中的键盘和 UDP 输入源组件没有丢失；
- `SyadInputController._inputSources` 数组引用保持有效；
- `git diff --check` 通过。

## 文档更新

实施时更新包 README、Input 详解、模板创建工具详解和 CHANGELOG，说明：

- `InputSource<TCommand>` 属于框架通用层；
- `SyadInputSource` 是项目命令类型的桥接层；
- 新增项目输入设备时，仍应继承项目侧 `SyadInputSource`。
