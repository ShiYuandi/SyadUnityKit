# RuntimeSettings 与 Windows 窗口控制

RuntimeSettings 建立在 `RuntimeConfigLoader` 之上，用于把已经读取的配置显式应用到 Unity Player。

它没有改变 RuntimeConfig 的职责：读取器仍然只读取文件。开箱即用组件或项目入口决定是否以及何时应用配置。

## 推荐的零编码用法

安装 UPM 包后，在 Unity 菜单中选择：

```text
Tools > SYAD Unity Kit > Runtime Settings > 一键创建运行设置
```

该菜单会在当前场景创建 `Runtime Settings Bootstrap` 对象，并在项目中生成：

```text
Assets/StreamingAssets/Configs/runtime-settings.json
```

如果配置已经存在，菜单不会覆盖。保存场景并把它加入 Build Settings 后即可构建，不需要创建自己的 C# 脚本。

组件提供中文 Inspector：

- `配置相对路径`：相对于 StreamingAssets 的 JSON 路径；
- `启动时自动应用`：在 Awake 阶段自动处理配置；
- `允许按键重新加载`：运行时按指定按键重新读取；启用后，程序启动和按键触发时都会输出明确日志；
- `输出成功日志`：控制成功信息，错误始终输出。

按键绑定提示与按键触发提示不会被“输出成功日志”关闭，避免维护人员忘记某个按键已经被运行设置组件占用。

Editor 中固定只读取和校验配置，不提供应用开关。分辨率、光标、画质和 Windows 原生窗口设置只会在构建后的 Player 中应用。

如果只手动添加组件，默认配置路径是 `Configs/runtime-settings.json`。

## 现场部署推荐流程

1. 在启动场景执行一次“一键创建运行设置”，然后保存场景。
2. 在 Inspector 中启用重新加载按键，默认使用 `R`。
3. 构建 Windows Player，并在现场启动程序。
4. 根据屏幕修改 Player 目录中的 `程序名_Data/StreamingAssets/Configs/runtime-settings.json`。
5. 回到程序按 `R`，通过日志确认配置已重新读取并应用，不需要重新构建。

Editor 只用于检查 JSON 是否能够读取和通过校验。分辨率、窗口位置、无边框、置顶和鼠标行为必须在构建后的 Player 中验证。

现场调试完成后可以继续保留重新加载按键。程序启动时会输出按键绑定和配置路径，方便以后维护时确认该按键的用途。

## 模块关系

```text
RuntimeConfigLoader
→ ApplicationRuntimeSettings
→ RuntimeSettingsValidator
→ RuntimeSettingsApplier
→ WindowsWindowController（仅 Windows Player）

RuntimeSettingsBootstrap 负责按正确顺序组合以上能力。
```

- `RuntimeConfigLoader`：读取 UTF-8 文本和 JSON。
- `ApplicationRuntimeSettings`：定义通用运行设置字段。
- `RuntimeSettingsValidator`：在产生副作用前检查配置。
- `RuntimeSettingsApplier`：调用 Unity API 修改分辨率、光标和画质。
- `WindowsWindowController`：调用 Win32 API 修改窗口位置、边框和置顶。

## 配置格式

```json
{
  "window": {
    "enabled": true,
    "x": 100,
    "y": 100,
    "width": 1920,
    "height": 1080,
    "fullscreen": false,
    "borderless": false,
    "alwaysOnTop": false,
    "bringToFrontOnce": false
  },
  "cursor": {
    "hidden": false,
    "confineToWindow": false
  },
  "qualityLevel": -1
}
```

字段含义：

- `window.enabled`：是否修改窗口。为 `false` 时保留 Player 当前分辨率和窗口属性。
- `x`、`y`：Windows 窗口左上角位置，允许负数以支持位于主屏幕左侧或上方的显示器。
- `width`、`height`：窗口或全屏分辨率，必须大于 0。
- `fullscreen`：是否全屏。为 `true` 时不再应用窗口位置和无边框设置。
- `borderless`：是否移除 Windows 窗口标题栏和可缩放边框。
- `alwaysOnTop`：是否把窗口设置为系统置顶窗口，但不会持续抢占输入焦点。
- `bringToFrontOnce`：应用设置时是否尝试把窗口前置一次。
- `cursor.hidden`：是否隐藏 Unity 光标。
- `cursor.confineToWindow`：是否把光标限制在 Player 窗口范围内。
- `qualityLevel`：Unity 画质等级索引，`-1` 表示保持当前等级。

## 底层 API（按需使用）

现场部署通常直接使用 `RuntimeSettingsBootstrap`，不需要编写下面的连接代码。只有项目需要把读取和应用插入自己的启动流程时，才需要直接组合底层 API。

项目入口先读取并校验：

```csharp
using Syad.UnityKit.RuntimeConfig;
using Syad.UnityKit.RuntimeSettings;
using UnityEngine;

RuntimeConfigLoader loader =
    RuntimeConfigLoader.CreateForStreamingAssets();

ApplicationRuntimeSettings settings;
string error;

if (!loader.TryLoadJson(
        "Configs/runtime-settings.json",
        out settings,
        out error))
{
    Debug.LogError("读取运行设置失败：" + error);
    return;
}

if (!RuntimeSettingsApplier.TryApply(settings, out error))
{
    Debug.LogError("应用运行设置失败：" + error);
    return;
}
```

`RuntimeSettingsApplier.TryApply` 会先调用校验器，因此无效分辨率或画质索引不会被应用。

## Windows 窗口设置

`Screen.SetResolution` 的更新不是立即完成的。项目应先应用通用设置，等待后续帧，再控制 Windows 原生窗口：

```csharp
using System.Collections;
using Syad.UnityKit.RuntimeSettings;
using Syad.UnityKit.Windows;
using UnityEngine;

private readonly WindowsWindowController _windowsWindow =
    new WindowsWindowController();

private IEnumerator ApplyWindowsWindow(WindowRuntimeSettings settings)
{
    yield return null;
    yield return null;

    if (!WindowsWindowController.IsSupported)
    {
        yield break;
    }

    string error;
    if (!_windowsWindow.TryApply(settings, out error))
    {
        Debug.LogError(error);
    }
}
```

该能力仅在构建后的 Windows Player 中启用。Unity Editor、macOS、Linux、Android 和 WebGL 中 `IsSupported` 返回 `false`，不会调用 `user32.dll`。

每个程序入口应自己创建并持有 `WindowsWindowController`，它不是单例或全局服务。实例会记住第一次读取到的窗口样式，因此在运行时把 `borderless` 从 `true` 改回 `false` 时能够恢复原始样式。

## 与旧 RuningMod 的区别

新实现没有复制旧脚本中的高风险行为：

- 不直接写 Unity 的注册表分辨率键；
- 不需要 `WindowHook.dll`；
- 不使用固定的三行 CSV 格式；
- 不静默吞掉配置解析异常；
- 不因为 Development Build 而跳过设置；
- 不在 `Update` 中不断抢占系统焦点；
- 不把配置读取、窗口控制和项目完成事件写进同一个 MonoBehaviour。

配置仍然属于具体项目，应放在项目的 `Assets/StreamingAssets` 中。UPM 包只提供固定且可复用的读取、校验和应用能力。

## Demo

仓库工程中的示例场景：

```text
Assets/Demo/RuntimeConfig/Scenes/RuntimeConfigDemo.unity
```

示例配置：

```text
Assets/StreamingAssets/Configs/runtime-settings.json
```

Editor 中只读取和展示，构建后的 Player 才会应用窗口设置。运行时修改 JSON 后按 `R` 可以重新读取和应用。基础 Presenter 不监听按键，重新加载完全由 `RuntimeSettingsBootstrap` 负责。
