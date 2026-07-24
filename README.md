# SYAD Unity Kit

SYAD Unity Kit 是一套小型、显式、便于理解和扩展的 Unity 运行时基础框架。目前 `0.1.0` 版本提供 UI 生命周期、页面目录、层级管理、缓存与释放策略。

支持 Unity **2019.4 LTS 及以上版本**，并保持与 Unity 2022.3 LTS 兼容。

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
https://github.com/ShiYuandi/SyadUnityKit.git?path=/Packages/com.syad.unitykit#v0.1.0
```

5. 点击 **Add**，等待 Unity 下载并编译完成。

安装成功后，Package Manager 中会显示 **SYAD Unity Kit**。

### 方式二：修改 manifest.json

打开目标项目的 `Packages/manifest.json`，在 `dependencies` 中加入：

```json
{
  "dependencies": {
    "com.syad.unitykit": "https://github.com/ShiYuandi/SyadUnityKit.git?path=/Packages/com.syad.unitykit#v0.1.0"
  }
}
```

如果文件中已经有其他依赖，只添加 `com.syad.unitykit` 这一行，并注意上一行末尾需要有逗号。

### 验证安装

在项目脚本中加入：

```csharp
using Syad.UnityKit.UI;
```

能够正常编译，并且 **Assets > Create > SYAD Unity Kit > UI > 视图目录** 菜单可用，即表示安装成功。

## 更新版本

框架使用 Git 标签发布稳定版本。更新时，把安装地址末尾的标签改为目标版本：

```text
#v0.1.0
```

例如未来更新到 `0.1.1`：

```text
https://github.com/ShiYuandi/SyadUnityKit.git?path=/Packages/com.syad.unitykit#v0.1.1
```

建议项目始终指定明确标签，不要直接依赖开发分支，以免框架更新导致项目意外变化。

## 卸载

通过 Package Manager 选中 **SYAD Unity Kit**，点击 **Remove**。

如果通过 `manifest.json` 安装，则删除下面这一项并保存：

```json
"com.syad.unitykit": "https://github.com/ShiYuandi/SyadUnityKit.git?path=/Packages/com.syad.unitykit#v0.1.0"
```

卸载前请先移除场景和 Prefab 上依赖本框架的组件，否则 Unity 会显示 Missing Script。

## 快速开始

安装后需要：

1. 在 Canvas 下创建并配置 `UIRoot`。
2. 创建继承自 `UIView` 的项目页面。
3. 创建 `UIViewCatalog` 并登记页面 Prefab。
4. 在项目组合入口中创建 `UIService`。
5. 通过 `Show`、`Hide`、`TryGet`、`Release` 和 `Dispose` 管理页面。

完整示例和 API 说明请阅读 [框架使用文档](Packages/com.syad.unitykit/README.md)。

## 仓库结构

```text
SyadUnityKit
├─ Assets                         示例场景与项目级验证代码
├─ Packages
│  └─ com.syad.unitykit           可复用的 UPM 框架源码
├─ ProjectSettings                Unity 项目设置
└─ README.md
```

`Assets` 中的 `StartView`、`test` 和示例场景用于演示与验证，不属于框架内核。通过 Git URL 安装时，Unity 只会安装 `Packages/com.syad.unitykit`，不会把整个示例工程导入目标项目。

## 本地开发

维护框架源码时：

1. 在 `Packages/com.syad.unitykit` 中修改通用代码。
2. 在 `Assets` 中编写示例验证真实使用流程。
3. 使用 Unity Test Framework 运行包测试。
4. 更新 `package.json` 版本号与 `CHANGELOG.md`。
5. 提交 Git，并创建与包版本一致的标签，例如 `v0.1.0`。

## 许可证

使用 MIT License，详见 [LICENSE.md](Packages/com.syad.unitykit/LICENSE.md)。
