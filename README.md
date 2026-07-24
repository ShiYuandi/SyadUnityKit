# SYAD Unity Kit

SYAD Unity Kit 是一个用于学习、开发和验证个人 Unity 框架的工程。目前版本为 `0.1.0`，第一阶段只包含轻量 UI 内核。

支持 Unity **2019.4 LTS 及以上版本**，并保持与 Unity 2022.3 LTS 兼容。

## 仓库结构

```text
SyadUnityKit
├─ Assets                         示例场景与项目级测试代码
├─ Packages
│  └─ com.syad.unitykit           可复用的 UPM 框架源码
├─ ProjectSettings                Unity 项目设置
└─ README.md
```

框架源码位于：

```text
Packages/com.syad.unitykit
```

`Assets` 中的 `StartView`、`test` 和示例场景用于学习与验证，不属于框架内核。

## 在其他项目中使用

最简单的方式是把整个目录：

```text
Packages/com.syad.unitykit
```

复制到目标项目的 `Packages` 目录。也可以在目标项目的 Package Manager 中选择 **Add package from disk**，然后选择包内的 `package.json`。

框架 API 的详细说明见 [包使用文档](Packages/com.syad.unitykit/README.md)。

## 开发与发布

1. 在 `Packages/com.syad.unitykit` 中修改通用框架代码。
2. 在 `Assets` 中编写项目示例，验证真实使用流程。
3. 使用 Unity Test Framework 运行包测试。
4. 更新 `package.json` 中的版本号和 `CHANGELOG.md`。
5. 提交 Git，并为发布版本创建与版本号一致的标签，例如 `v0.1.0`。

## 许可证

使用 MIT License，详见 [LICENSE.md](Packages/com.syad.unitykit/LICENSE.md)。
