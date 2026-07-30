# RuntimeConfig 详解

`Syad.UnityKit.RuntimeConfig` 是 SYAD Unity Kit 的第三个运行时模块。它只解决一件事：从一个明确指定的本地目录中，安全地读取文本或 JSON 配置。

该模块自 `0.3.0` 起提供，适合读取现场部署参数和项目自己的本地业务配置。

## 为什么需要运行时配置

展馆、互动装置和长期部署的桌面程序经常需要在不重新打包 Unity 项目的情况下调整参数，例如：

- 窗口宽高和全屏选项；
- UDP 端口或服务器地址；
- 设备编号、展项编号；
- 调试开关和部署环境名称。

这些值适合放在 `StreamingAssets` 等外部文件中。程序启动时读取它们，再由项目代码决定如何应用。

RuntimeConfig 只负责“读取并转换配置”，不会直接修改窗口、连接网络或初始化设备。这样读取逻辑可以复用，业务行为仍然清晰地留在具体项目中。需要应用通用窗口、光标和画质设置时，可以显式调用 RuntimeSettings 模块。

## 最小使用流程

先定义一个与 JSON 字段对应的普通类。`JsonUtility` 读取的是字段，不是 C# 属性：

```csharp
using System;

[Serializable]
public sealed class ExhibitConfig
{
    public string applicationName;
    public int windowWidth;
    public int windowHeight;
    public bool fullscreen;
    public int udpPort;
}
```

在 `Assets/StreamingAssets/Configs/runtime-config.json` 中保存：

```json
{
  "applicationName": "展馆互动程序",
  "windowWidth": 1920,
  "windowHeight": 1080,
  "fullscreen": false,
  "udpPort": 15000
}
```

然后创建加载器并读取：

```csharp
using Syad.UnityKit.RuntimeConfig;
using UnityEngine;

public sealed class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        RuntimeConfigLoader loader =
            RuntimeConfigLoader.CreateForStreamingAssets();

        ExhibitConfig config =
            loader.LoadJson<ExhibitConfig>("Configs/runtime-config.json");

        Debug.Log("读取到 UDP 端口：" + config.udpPort);
    }
}
```

这里传给 `LoadJson` 的必须是相对于 `StreamingAssets` 的路径，不能传绝对路径。

## Load 与 TryLoad 的区别

两组 API 的读取能力相同，区别在于失败后的处理方式。

### Load：失败时抛出异常

```csharp
ExhibitConfig config =
    loader.LoadJson<ExhibitConfig>("Configs/runtime-config.json");
```

适合“缺少配置就无法继续启动”的情况。文件不存在、内容为空或路径不合法时，调用处会收到异常。

### TryLoad：失败时返回 false

```csharp
ExhibitConfig config;
string error;

if (!loader.TryLoadJson(
        "Configs/runtime-config.json",
        out config,
        out error))
{
    Debug.LogError("配置加载失败：" + error);
    return;
}

Debug.Log("配置加载成功：" + config.applicationName);
```

适合项目希望显示友好错误、使用默认配置或继续停留在启动页的情况。失败时 `config` 为 `null`，`error` 包含可记录到日志的原因。

文本配置也有对应方法：

```csharp
string text = loader.LoadText("Configs/message.txt");

string optionalText;
string error;
bool loaded = loader.TryLoadText(
    "Configs/message.txt",
    out optionalText,
    out error);
```

## 显式根目录

如果配置不在 `StreamingAssets`，可以直接指定根目录：

```csharp
RuntimeConfigLoader loader =
    new RuntimeConfigLoader(@"D:\ExhibitConfigs");

ExhibitConfig config =
    loader.LoadJson<ExhibitConfig>("HallA/runtime.json");
```

`RootDirectory` 可以查看规范化后的根目录，`GetFullPath` 可以查看某个相对路径最终对应的位置：

```csharp
Debug.Log(loader.RootDirectory);
Debug.Log(loader.GetFullPath("HallA/runtime.json"));
```

根目录可以暂时不存在；真正读取文件时才会报告文件不存在。

## 路径安全规则

加载器只允许读取根目录以内的相对路径：

```csharp
loader.LoadText("Configs/message.txt"); // 允许
loader.LoadText(@"D:\secret.txt");     // 拒绝绝对路径
loader.LoadText("../secret.txt");      // 拒绝离开根目录
```

这项限制可以防止调用者意外把一个外部路径交给配置加载器，也使“这个加载器能读取哪里”保持明确。

## JsonUtility 的限制

RuntimeConfig 第一版使用 Unity 自带的 `JsonUtility`，因此需要注意：

- 配置类型必须是普通 `class`，并标记 `[Serializable]`；
- 数据应放在可序列化字段中，不要只写 C# 属性；
- JSON 顶层应是对象；
- 字段名需要与 JSON 名称一致；
- 不适合直接解析字典或结构高度动态的 JSON。

如果未来真实项目明确需要字典、多态或复杂 JSON，再评估扩展方案。第一版不引入 Newtonsoft.Json，避免让一个简单配置模块增加额外依赖。

## StreamingAssets 平台限制

`CreateForStreamingAssets()` 内部使用 `Application.streamingAssetsPath` 和 `System.IO`。它适用于：

- Unity Editor；
- Windows、macOS、Linux 等 StreamingAssets 是普通文件目录的平台。

Android 的 StreamingAssets 通常位于 APK 压缩包中，WebGL 也不是普通本地目录，因此第一版会拒绝这类 URL 或压缩包路径。不要在这些平台上调用当前便捷入口。

未来如果确实需要这些平台，应增加基于 `UnityWebRequest` 的异步读取能力，而不是让同步文件 API 假装支持。

## 生命周期与职责

`RuntimeConfigLoader` 只是一个普通 C# 对象：

- 不继承 `MonoBehaviour`；
- 不启动线程或协程；
- 不持有文件句柄；
- 不需要调用 `Dispose()`；
- 可以按项目需要长期持有，也可以读取后立即丢弃。

推荐的职责关系是：

```text
RuntimeConfigLoader
→ 读取并转换配置
→ 项目 Bootstrap / Controller
→ 校验并应用窗口、网络或设备参数
```

例如 `RuntimeConfigLoader` 读取到 `udpPort` 后，不应该自己创建 UDP 服务；应由项目入口把端口交给网络模块。

## Demo 与练习

仓库开发工程中打开：

```text
Assets/Demo/RuntimeConfig/Scenes/RuntimeConfigDemo.unity
```

进入 Play Mode 后，场景会读取：

```text
Assets/StreamingAssets/Configs/runtime-settings.json
```

修改 JSON 并保存，再回到 Game 窗口按 `R`，可以观察重新读取后的字段。按键监听由 `RuntimeSettingsBootstrap` 负责，Presenter 只显示状态。Editor 中只显示值；构建后的 Player 会通过 RuntimeSettings 实际应用窗口、光标和画质设置。

建议练习：

1. 修改 `applicationName` 和 `udpPort`，按 `R` 观察结果。
2. 暂时改错文件名，观察 `TryLoadJson` 返回的错误。
3. 给数据类和 JSON 同时增加 `deviceId` 字段。
4. 在项目代码中校验端口是否处于 `1` 到 `65535`，理解“读取”和“业务校验”是两个职责。
