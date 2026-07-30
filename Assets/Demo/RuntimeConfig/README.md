# RuntimeConfig Demo

打开 `Scenes/RuntimeConfigDemo.unity` 并进入 Play Mode。

基础示例会从一键创建工具使用的默认路径读取 JSON：

```text
Assets/StreamingAssets/Configs/runtime-settings.json
```

运行时按 `R` 可以重新加载配置。你可以修改并保存 JSON，再回到 Game 窗口按 `R`，观察强类型配置对象中的内容更新。

为了避免修改 Unity 编辑器窗口，Editor 中只读取、校验和显示配置。构建后的 Player 会实际应用：

- 窗口宽度、高度和全屏状态；
- 鼠标显示与窗口范围限制；
- Unity 画质等级；
- Windows Player 的窗口位置、无边框和置顶状态。

## 文件职责

- `RuntimeSettingsBootstrap`：包内的开箱即用组件，负责读取、校验并应用配置。
- `RuntimeSettingsDemoPresenter.cs`：只负责显示组件状态，不再监听输入或加载配置。
- `runtime-settings.json`：部署人员可以修改的外部配置。

包中的职责仍然保持分离：

- `RuntimeConfigLoader` 只读取并转换 JSON；
- `RuntimeSettingsApplier` 应用 Unity 通用设置；
- `WindowsWindowController` 应用 Windows 原生窗口属性；
- Demo Controller 决定读取路径、调用顺序和错误处理。

Windows 原生窗口设置会等待 `Screen.SetResolution` 两帧后执行，避免 Unity 的分辨率刷新覆盖窗口位置和样式。

`qualityLevel` 使用 Unity 项目的画质索引，`-1` 表示保持当前画质。索引超出当前项目的 `QualitySettings.names` 范围时，配置校验会失败。

第一版 RuntimeConfig 使用 `System.IO`，适用于 Unity Editor 和可直接访问 StreamingAssets 文件的桌面平台。Android、WebGL 等 URL 或压缩包形式的 StreamingAssets 尚未支持。
