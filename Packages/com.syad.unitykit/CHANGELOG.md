# 变更日志

## 0.4.0 - 2026-08-25

- 新增 `Syad.UnityKit.Networking.UdpReceiverBehaviour` 零编码 UDP 文本接收组件。
- 支持后台线程接收 UDP 数据包，并在 Unity 主线程触发 `UnityEvent<string>`。
- 默认监听本机全部 IPv4 网卡，使用严格 UTF-8，并保留一个数据包一条消息的边界。
- 增加容量受限队列、每帧处理上限、队列溢出统计和中文诊断日志。
- 提供中文 Inspector、UDP Demo、详细文档和 12 项 PlayMode 测试。
- 第一版只提供 UDP 文本接收，不提供发送、串口、TCP、协议解析或可靠投递。
- 新增 Input 与 UDP Editor 脚手架菜单，可在项目 `Assets` 中生成中文可修改模板并自动创建场景对象。
- Input 模板将 `KeyboardInputSource`、命令枚举和 `SyadInputController` 分离，保持输入源与 `InputRouter` 解耦。
- 新增 `SyadInputSource` 基类和 `UdpInputSource` 适配器，`SyadInputController` 改用输入源数组，不再写死具体设备字段。
- 新增 Runtime `InputSource<TCommand>` 泛型基类，统一输入源事件、强类型命令提交与销毁清理。
- 项目侧 `SyadInputSource` 调整为 `InputSource<SyadInputCommand>` 的非泛型桥接类，保持 Unity Inspector 和现有场景引用方式不变。
- Input 与 UDP 创建菜单支持任意执行顺序，并自动连接场景中已有的键盘和 UDP 输入源。
- 模板工具不会覆盖已有项目脚本，也不会把项目业务代码写入 UPM Runtime。
- 新增 Runtime `InputSource<TCommand>` 测试，包内运行时测试总数达到 39 项。

### 0.4.0 发布边界

- UDP 模块只提供文本接收，不提供发送、串口、TCP、JSON 协议解析或可靠投递。
- `SyadUdpReceiverController` 使用运行时事件订阅；创建模板后无需手动向 Inspector 的 UnityEvent 添加同一个处理方法。
- UDP 和 Input 模板生成的脚本属于项目 `Assets`，使用者可以继续修改，不会被 UPM Runtime 自动覆盖。

## 0.3.0 - 2026-07-30

- 新增 `Syad.UnityKit.RuntimeConfig.RuntimeConfigLoader` 运行时配置加载器。
- 支持从显式根目录读取 UTF-8 文本和 `JsonUtility` 强类型 JSON 配置。
- 同时提供失败时抛出异常的 `Load` API，以及返回错误信息的 `TryLoad` API。
- 拒绝绝对文件路径和 `../` 目录越界，避免配置读取离开指定根目录。
- 提供桌面平台 `StreamingAssets` 便捷入口、RuntimeConfig Demo、中文文档和运行时测试。
- 新增 `ApplicationRuntimeSettings`、配置校验和 `RuntimeSettingsApplier`，显式应用分辨率、光标与画质设置。
- 新增 Windows Player 专用的 `WindowsWindowController`，支持窗口位置、无边框、置顶和单次前置。
- 新增开箱即用的 `RuntimeSettingsBootstrap` 组件和中文 Inspector，新项目不再需要编写连接代码。
- 新增 Editor 一键设置菜单，自动创建启动对象和默认的 `StreamingAssets/Configs/runtime-settings.json`。
- 启用配置重新加载按键后，启动和按键触发时都会输出绑定提示，且不受普通成功日志开关影响。
- 基础 RuntimeSettings Demo 移除重复按键监听，Presenter 只负责展示，重新加载由 Bootstrap 统一处理。
- Windows 窗口控制不写注册表、不依赖 `WindowHook.dll`，也不会在每帧强制抢占焦点。
- `RuntimeConfigLoader` 仍然只负责读取配置，不自动应用窗口、网络或设备参数。
- 第一版仅支持可由 `System.IO` 直接读取的本地路径，暂不支持 Android、WebGL 等 URL 或压缩包形式的 `StreamingAssets`。
- 新增 13 项 RuntimeConfig 与 RuntimeSettings 测试，包内运行时测试总数达到 24 项。

## 0.2.0 - 2026-07-28

- 新增 `Syad.UnityKit.Input.InputRouter<TCommand>` 输入命令路由。
- 支持将键盘、UDP、RFID 等项目输入源统一转换为强类型命令。
- 支持显式启用、禁用、输入冷却和冷却重置。
- 添加输入路由运行时测试与中文使用示例。
- 仓库添加独立 Input Demo，明确区分键盘输入源、命令路由和业务处理。
- 本版本没有修改 `0.1.0` UI 模块的公共 API。

## 0.1.0 - 2026-07-24

- 添加第一版 UI 运行时模块。
- 添加显式 UI 根节点、基于具体类型的视图目录和生命周期管理。
- 添加缓存策略与重复实例策略。
- 添加运行时测试和初版使用文档。
- 防止激活状态的 Prefab 在框架完成创建和数据绑定前提前触发显示逻辑。
- `AllowMultiple` 视图会优先复用隐藏的 `KeepAlive` 实例。
- 明确缓存视图应采用幂等事件订阅，避免重复回调。
