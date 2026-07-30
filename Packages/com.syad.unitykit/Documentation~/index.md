# SYAD Unity Kit 文档

稳定版本：`0.3.0`。

SYAD Unity Kit 当前包含以下运行时模块：

- `Syad.UnityKit.UI`：页面目录、层级、生命周期、缓存与释放管理。
- `Syad.UnityKit.Input`：强类型输入命令转发、启停与全局冷却。
- `Syad.UnityKit.RuntimeConfig`：从显式根目录读取 UTF-8 文本和强类型 JSON 配置。
- `Syad.UnityKit.RuntimeSettings`：提供零编码 Bootstrap，并校验和应用分辨率、光标与画质设置。
- `Syad.UnityKit.Windows`：控制 Windows Player 的窗口位置、样式和置顶状态。

## 文档入口

- [框架安装与完整 API 说明](../README.md)
- [InputRouter 详细原理与练习](InputRouter详解.md)
- [RuntimeConfig 详细原理与练习](RuntimeConfig详解.md)
- [RuntimeSettings 与 Windows 窗口控制](RuntimeSettings详解.md)
- [版本变更记录](../CHANGELOG.md)
- [MIT 许可证](../LICENSE.md)

仓库中的 `Assets/Demo/UI`、`Assets/Demo/Input` 和 `Assets/Demo/RuntimeConfig` 提供可运行示例。通过 Git URL 安装 UPM 包时，不会把这些 Demo 自动导入目标项目。
