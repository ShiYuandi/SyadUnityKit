# Input 与 UDP 一键创建模板设计

## 目标

为 SYAD Unity Kit 增加 Editor 菜单脚手架，让使用者可以在 Unity 中一键创建可修改的输入系统和 UDP 接收示例。创建结果属于使用项目的 `Assets`，不把项目业务代码写入 UPM 包的 Runtime 目录。

目标是减少第一次接入时的重复手工步骤，同时保留输入源、命令路由、UDP 接收和业务处理之间的解耦。

## 范围

本次增加两个 Editor 菜单：

```text
Tools > SYAD Unity Kit > Input > 创建输入系统模板
Tools > SYAD Unity Kit > Networking > 创建 UDP 接收器
```

两个菜单都支持 Unity 2019.4，并兼容 Unity 2022.3。生成的脚本使用中文注释、日志和公开说明。

本次不修改 `InputRouter<TCommand>`、`UdpReceiverBehaviour` 的公共 API，也不让 UPM Runtime 依赖项目生成脚本。

## 整体架构

```text
键盘输入源
    ↓
KeyboardInputSource.CommandDetected
    ↓
SyadInputController.SubmitCommand
    ↓
InputRouter<TCommand>.TryDispatch
    ↓
SyadInputController.HandleCommand
```

```text
UdpReceiverBehaviour
    ↓ Unity 主线程 UnityEvent<string>
SyadUdpReceiverController.HandleMessage
    ↓
项目自行解析文本、更新数据或转换为业务命令
```

`KeyboardInputSource` 不引用 `InputRouter`。`SyadUdpReceiverController` 也不引用 `InputRouter`。如果项目需要把 UDP 转换为命令，由项目控制器显式调用 `SyadInputController.SubmitCommand`，避免网络模块和输入模块互相依赖。

## 生成目录和文件

菜单第一次执行时创建以下目录：

```text
Assets/SyadUnityKit/Generated/
├─ Input/
│  ├─ SyadInputCommand.cs
│  ├─ KeyboardInputSource.cs
│  └─ SyadInputController.cs
└─ Networking/
   └─ SyadUdpReceiverController.cs
```

### SyadInputCommand.cs

生成一个可直接修改的项目命令枚举，默认包含：

```csharp
Left,
Right,
Confirm
```

它不属于 UPM 包，使用者可以增删命令名称。

### KeyboardInputSource.cs

这是独立的输入源 `MonoBehaviour`，默认监听左方向键、右方向键和空格键，并通过 `CommandDetected` 事件发送 `SyadInputCommand`。它不处理冷却、启停策略和业务。

### SyadInputController.cs

这是输入组合入口，负责：

- 创建并持有 `InputRouter<SyadInputCommand>`；
- 订阅 `KeyboardInputSource.CommandDetected`；
- 在 `SubmitCommand` 中调用 `TryDispatch`；
- 在 `HandleCommand` 中提供项目业务处理模板；
- 在销毁时退订输入事件并调用 `Dispose`；
- 提供中文日志和可修改的冷却时间字段。

该脚本只依赖 `Syad.UnityKit.Input` 和生成的命令枚举。

### SyadUdpReceiverController.cs

这是 UDP 接收器的项目侧处理模板，负责：

- 获取同一 GameObject 上的 `UdpReceiverBehaviour`；
- 在启用和禁用时订阅、退订消息和错误事件；
- 在 `HandleMessage(string)` 中输出最近消息和发送端；
- 在代码注释中标出传感器解析、业务判断和命令转换位置；
- 不直接创建 UDP 线程，不直接调用 `InputRouter`。

生成的控制器使用代码订阅事件，Inspector 的同一事件不自动添加重复绑定。使用者若改为 Inspector 绑定，应删除或注释对应的代码订阅。

## 场景对象创建

### Input

菜单创建名为 `SYAD Input System` 的 GameObject，并添加：

- `KeyboardInputSource`；
- `SyadInputController`。

脚本生成后执行 `AssetDatabase.Refresh`，再把生成的脚本类型挂到对象。对象会注册 Undo，创建完成后选中并 Ping。

### UDP

菜单创建名为 `SYAD UDP Receiver` 的 GameObject，并添加：

- `UdpReceiverBehaviour`；
- `SyadUdpReceiverController`。

UDP 使用组件默认端口 `15000`、自动监听和中文生命周期日志。控制器自动引用同一对象上的接收器。

## 幂等和覆盖规则

- 生成脚本路径已存在时，不覆盖原文件，并在 Console 输出提示；
- 场景中已存在同类型的生成控制器时，优先选中已有对象，不再创建重复对象；
- 只要缺少某个组件，就可以在已有对象上补齐该组件；
- 所有对象创建、组件添加和文件生成都尽可能支持 Undo；
- 菜单不会修改现有 `Assets/Demo/Input`、`Assets/Demo/UDP` 或 UPM Runtime 文件。

## 错误处理

- 当前场景未保存或场景无效时，菜单给出中文提示并停止创建；
- 生成脚本写入失败时，不创建不完整的场景对象，并输出具体路径；
- 生成脚本编译失败由 Unity Console 显示，菜单不会吞掉编译错误；
- 已存在文件不会被覆盖，避免用户修改被破坏。

## 测试和验收

Editor 验收：

1. 菜单存在且中文名称正确；
2. Input 菜单生成三个脚本、一个对象和两个组件；
3. UDP 菜单生成一个脚本、一个对象和两个组件；
4. 重复执行不会覆盖脚本或创建重复控制器；
5. 创建后脚本能够编译，场景对象引用正确；
6. Unity Undo 可以撤销对象创建。

运行时验收：

1. Input 场景中按左键、右键、空格能进入 `TryDispatch` 和 `HandleCommand`；
2. Input 控制器销毁后不残留事件订阅；
3. UDP 场景接收 UTF-8 文本并进入 `HandleMessage`；
4. UDP 控制器禁用和销毁后不残留事件订阅；
5. UDP 模块不需要手工创建线程或 `UdpClient`；
6. UDP 和 Input 两个控制器可以独立创建、独立使用。

## 文档更新

实现后更新：

- 根 `README.md` 的快速开始和菜单说明；
- 包 `README.md` 的 Editor 工具说明；
- `Documentation~/index.md`；
- 新增一篇中文“模板创建工具详解”文档；
- `CHANGELOG.md`，标记为后续版本的新增功能。

本功能不改变当前稳定版本 `0.3.0` 的 API。若随 UDP 功能一起发布，建议作为 `0.4.0` 的内容。
