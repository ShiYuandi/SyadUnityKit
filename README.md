# SYAD Unity Kit

SYAD Unity Kit 是一套小型、显式、便于理解和扩展的 Unity 运行时基础框架。`0.4.0` 提供 UI 生命周期管理、强类型输入源与命令路由、零编码 UDP 文本接收、运行时配置读取，以及 Windows Player 现场运行设置。

支持 Unity **2019.4 LTS 及以上版本**，并保持与 Unity 2022.3 LTS 兼容。

## 包含模块

| 模块 | 命名空间 | 主要能力 |
|---|---|---|
| UI | `Syad.UnityKit.UI` | 页面目录、层级、生命周期、缓存、重复实例和释放策略 |
| Input | `Syad.UnityKit.Input` | 强类型命令转发、输入启停、全局冷却和冷却重置 |
| Networking | `Syad.UnityKit.Networking` | 后台接收 UDP UTF-8 文本并在 Unity 主线程触发事件 |
| RuntimeConfig | `Syad.UnityKit.RuntimeConfig` | 从显式根目录读取 UTF-8 文本和强类型 JSON 配置 |
| RuntimeSettings | `Syad.UnityKit.RuntimeSettings` | 零编码加载并应用分辨率、光标和画质设置 |
| Windows | `Syad.UnityKit.Windows` | Windows Player 窗口位置、无边框和置顶控制 |

框架不提供全局 EventBus、Service Locator、单例或反射自动注册。具体设备读取、通信协议、业务状态和页面导航仍由项目代码负责。

详细资料：

- [包使用文档](Packages/com.syad.unitykit/README.md)
- [InputRouter 详解](Packages/com.syad.unitykit/Documentation~/InputRouter详解.md)
- [Input 与 UDP 一键创建模板](Packages/com.syad.unitykit/Documentation~/模板创建工具详解.md)
- [UDP Receiver 详解](Packages/com.syad.unitykit/Documentation~/UdpReceiver详解.md)
- [RuntimeConfig 详解](Packages/com.syad.unitykit/Documentation~/RuntimeConfig详解.md)
- [RuntimeSettings 与 Windows 窗口控制](Packages/com.syad.unitykit/Documentation~/RuntimeSettings详解.md)
- [版本变更记录](Packages/com.syad.unitykit/CHANGELOG.md)

## 通过 Git 安装

### 环境要求

- Unity 2019.4 LTS 或更高版本。
- 本机已经安装 Git，并且 Unity 可以调用 Git。
- 项目已经使用 Unity Package Manager。

### 方式一：通过 Package Manager 安装

1. 在 Unity 中打开 **Window > Package Manager**。
2. 点击左上角的 **+**。
3. 选择 **Add package from git URL**。
4. 粘贴下面的地址：

```text
https://github.com/ShiYuandi/SyadUnityKit.git?path=/Packages/com.syad.unitykit#v0.4.0
```

5. 点击 **Add**，等待 Unity 下载并编译完成。

安装成功后，Package Manager 中会显示 **SYAD Unity Kit**。

### 方式二：修改 manifest.json

打开目标项目的 `Packages/manifest.json`，在 `dependencies` 中加入：

```json
{
  "dependencies": {
    "com.syad.unitykit": "https://github.com/ShiYuandi/SyadUnityKit.git?path=/Packages/com.syad.unitykit#v0.4.0"
  }
}
```

如果文件中已经有其他依赖，只添加 `com.syad.unitykit` 这一行，并注意上一行末尾需要有逗号。

### 验证安装

在项目脚本中加入：

```csharp
using Syad.UnityKit.Input;
using Syad.UnityKit.UI;
```

能够正常编译，并且 **Assets > Create > SYAD Unity Kit > UI > 视图目录** 菜单可用，即表示安装成功。

## 更新版本

框架使用 Git 标签发布稳定版本。更新时，把安装地址末尾的标签改为目标版本：

```text
#v0.4.0
```

例如安装 `0.4.0`：

```text
https://github.com/ShiYuandi/SyadUnityKit.git?path=/Packages/com.syad.unitykit#v0.4.0
```

建议项目始终指定明确标签，不要直接依赖开发分支，以免框架更新导致项目意外变化。

## 卸载

通过 Package Manager 选中 **SYAD Unity Kit**，点击 **Remove**。

如果通过 `manifest.json` 安装，则删除下面这一项并保存：

```json
"com.syad.unitykit": "https://github.com/ShiYuandi/SyadUnityKit.git?path=/Packages/com.syad.unitykit#v0.4.0"
```

卸载前请先移除场景和 Prefab 上依赖本框架的组件，否则 Unity 会显示 Missing Script。

## 快速开始

### UI

使用 UI 模块需要：

1. 在 Canvas 下创建并配置 `UIRoot`。
2. 创建继承自 `UIView` 的项目页面。
3. 创建 `UIViewCatalog` 并登记页面 Prefab。
4. 在项目组合入口中创建 `UIService`。
5. 通过 `Show`、`Hide`、`TryGet`、`Release` 和 `Dispose` 管理页面。

完整示例和 API 说明请阅读 [框架使用文档](Packages/com.syad.unitykit/README.md)。

### Input

推荐先保存场景，然后执行：

```text
Tools > SYAD Unity Kit > Input > 创建输入系统模板
```

工具会在 `Assets/SyadUnityKit/Generated/Input` 生成命令枚举、`SyadInputSource` 桥接类、`KeyboardInputSource`、`UdpInputSource` 和 `SyadInputController`。框架的 `InputSource<TCommand>` 负责通用事件和命令提交，项目侧 `SyadInputSource` 只指定 `SyadInputCommand` 类型。控制器只订阅输入源数组，新增设备不需要增加控制器字段。生成代码属于你的项目，可以直接修改，重复执行菜单不会覆盖。

该模板菜单属于 `0.4.0`，安装 `v0.4.0` 后即可使用。

也可以手动定义项目命令，再创建并持有 `InputRouter<TCommand>`：

```csharp
using Syad.UnityKit.Input;

public enum ExhibitCommand
{
    Left,
    Right,
    Confirm
}

InputRouter<ExhibitCommand> inputRouter =
    new InputRouter<ExhibitCommand>(0.5f);

inputRouter.CommandReceived += HandleCommand;
inputRouter.TryDispatch(ExhibitCommand.Left);
```

具体输入源负责把键盘、UDP、RFID、Kinect 等原始信号转换为命令；`InputSource<TCommand>` 提供统一的命令事件；InputRouter 负责启停、冷却和转发；项目 Controller 负责实际业务。详细说明请阅读 [《InputRouter 详解》](Packages/com.syad.unitykit/Documentation~/InputRouter详解.md)。

### UDP Receiver

推荐先保存场景，然后执行：

```text
Tools > SYAD Unity Kit > Networking > 创建 UDP 接收器
```

工具会生成 `SyadUdpReceiverController.cs`，并自动创建带有 `UdpReceiverBehaviour` 和处理控制器的场景对象。如果 Input 系统已经存在，还会添加 `UdpInputSource` 并更新输入源数组。两个菜单可以按任意顺序执行。也可以手动在场景中添加：

```text
Add Component > SYAD Unity Kit > Networking > UDP Receiver
```

Inspector 填写监听端口，例如 `15000`，再把“收到文本消息”绑定到项目脚本的 `public void HandleMessage(string message)`。组件在后台线程接收完整 UDP 数据包，并在 Unity 主线程交付严格 UTF-8 文本。

该模块不负责 UDP 发送、串口、JSON 解析或自动调用 InputRouter。完整说明请阅读 [《UDP Receiver 详解》](Packages/com.syad.unitykit/Documentation~/UdpReceiver详解.md)和[《Input 与 UDP 模板创建工具详解》](Packages/com.syad.unitykit/Documentation~/模板创建工具详解.md)。

### RuntimeConfig 与现场运行设置

一般项目不需要为运行设置编写代码。安装包后执行：

```text
Tools > SYAD Unity Kit > Runtime Settings > 一键创建运行设置
```

工具会在当前场景添加 `RuntimeSettingsBootstrap`，并创建默认 JSON。保存场景后直接构建即可。

下面是需要读取其他业务配置时使用的底层 API：

先定义带有 `[Serializable]` 的配置类，再从指定根目录读取 JSON：

```csharp
using Syad.UnityKit.RuntimeConfig;

RuntimeConfigLoader loader =
    RuntimeConfigLoader.CreateForStreamingAssets();

ExhibitConfig config =
    loader.LoadJson<ExhibitConfig>("Configs/runtime-config.json");
```

RuntimeConfig 只负责读取并转换配置，不负责修改窗口、启动网络或初始化设备。`RuntimeSettingsBootstrap` 会自动组合 `RuntimeSettingsApplier` 和 `WindowsWindowController`。

第一版适用于 Unity Editor 和可通过 `System.IO` 直接访问 StreamingAssets 的桌面平台，暂不支持 Android、WebGL。详细说明请阅读 [《RuntimeConfig 详解》](Packages/com.syad.unitykit/Documentation~/RuntimeConfig详解.md)和[《RuntimeSettings 与 Windows 窗口控制》](Packages/com.syad.unitykit/Documentation~/RuntimeSettings详解.md)。

现场部署时，修改 Player 目录中的 `程序名_Data/StreamingAssets/Configs/runtime-settings.json`，再按 Inspector 中设置的重新加载按键即可应用，不需要重新构建。Editor 只读取和校验，窗口行为必须在构建后的 Windows Player 中验证。

## 仓库结构

```text
SyadUnityKit
├─ Assets
│  └─ Demo
│     ├─ UI                       UI 页面管理示例
│     ├─ Input                    输入源与命令路由示例
│     ├─ RuntimeConfig            零编码运行设置示例
│     └─ UDP                      UDP 文本接收示例
├─ Packages
│  └─ com.syad.unitykit
│     ├─ Runtime                  可复用的 UPM 框架源码
│     ├─ Tests                    PlayMode 自动化测试
│     └─ Documentation~           详细文档
├─ ProjectSettings                Unity 项目设置
└─ README.md
```

`Assets/Demo` 中的场景和脚本用于演示与验证，不属于框架内核。通过 Git URL 安装时，Unity 只会安装 `Packages/com.syad.unitykit`，不会把整个示例工程导入目标项目。

## 本地开发

维护框架源码时：

1. 在 `Packages/com.syad.unitykit` 中修改通用代码。
2. 在 `Assets` 中编写示例验证真实使用流程。
3. 使用 Unity Test Framework 运行包测试。
4. 更新 `package.json` 版本号与 `CHANGELOG.md`。
5. 提交 Git，并创建与包版本一致的版本标签。

## 许可证

使用 MIT License，详见 [LICENSE.md](LICENSE.md)。UPM 包目录中同时保留一份相同的许可证，确保通过 Git 安装后仍能查看授权协议。
