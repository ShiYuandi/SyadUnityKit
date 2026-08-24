# UDP 文本接收模块设计

日期：2026-08-24

目标版本：SYAD Unity Kit `0.4.0`

当前稳定版本：`0.3.0`

## 一、目标

为经常使用外部程序、识别程序和传感器的 Unity 现场项目提供一个开箱即用的 UDP 文本接收组件。

使用者只需在场景对象上添加 `UdpReceiverBehaviour`，在 Inspector 中填写端口并绑定 UnityEvent，即可在 Unity 主线程收到外部程序发来的 UTF-8 文本，不需要自己处理 `UdpClient`、后台线程或主线程切换。

第一版需要直接兼容 `YCP031EarchProject` 的实际发送方式：该程序把国家名称编码为 UTF-8，并以一个完整 UDP 数据包发送到 `127.0.0.1:15000`。

## 二、范围

第一版包含：

- UDP 接收，不提供 UDP 发送；
- `MonoBehaviour` 场景组件，不提供纯 C# 接收器；
- 一个 UDP 数据包对应一条文本消息；
- 严格 UTF-8 解码；
- 后台线程阻塞接收；
- 线程安全待处理队列；
- Unity 主线程触发 `UnityEvent<string>`；
- 自动启动、停止和资源释放；
- 端口、队列和日志配置；
- 接收状态、统计信息和最近错误；
- 独立 Demo、中文文档和 PlayMode 测试。

第一版不包含：

- 串口、TCP、WebSocket；
- UDP 发送、广播和组播；
- JSON、CSV、传感器协议或国家名称解析；
- 自动调用 `InputRouter`；
- 自动修改 UI 或项目状态；
- 二进制业务协议；
- 消息可靠性、确认、重传或顺序保证。

## 三、模块位置

运行时代码：

```text
Packages/com.syad.unitykit/Runtime/Networking/
├─ UdpReceiverBehaviour.cs
└─ UdpTextEvent.cs
```

命名空间：

```csharp
Syad.UnityKit.Networking
```

代码继续使用现有的 `Syad.UnityKit.Runtime` 程序集，不新增程序集定义。

测试与文档：

```text
Packages/com.syad.unitykit/Tests/Runtime/UdpReceiverBehaviourTests.cs
Packages/com.syad.unitykit/Documentation~/UdpReceiver详解.md
```

Demo：

```text
Assets/Demo/UDP/
├─ Scenes/UdpReceiverDemo.unity
├─ Scripts/UdpReceiverDemoPresenter.cs
└─ README.md
```

## 四、公开组件

主要公开类型：

```csharp
public sealed class UdpReceiverBehaviour : MonoBehaviour
```

为确保 Unity 2019.4 能稳定序列化带参数事件，定义一个明确的事件子类：

```csharp
[Serializable]
public sealed class UdpTextEvent : UnityEvent<string>
{
}
```

组件提供两个 Inspector 事件：

```text
OnMessageReceived(string message)
OnError(string error)
```

公开控制方法：

```csharp
void StartListening();
void StopListening();
```

控制方法采用幂等规则：重复开始不会创建第二个接收线程，重复停止不会报错。停止后允许再次开始。

公开只读状态：

```csharp
bool IsListening
int ListenPort
long ReceivedMessageCount
long DroppedMessageCount
string LastMessage
string LastRemoteEndPoint
string LastError
```

## 五、Inspector 配置

第一版提供以下序列化字段：

```text
监听端口                 默认 15000
启动时自动监听           默认 true
输出启动和停止日志       默认 true
输出每条接收日志         默认 false
最大待处理消息数         默认 1024
每帧最多处理消息数       默认 100
收到文本消息             UnityEvent<string>
发生错误                 UnityEvent<string>
```

监听地址固定为 `0.0.0.0`，即监听本机全部网卡。第一版不向 Inspector 暴露网卡绑定选择，避免增加现场配置负担。

端口合法范围为 `1` 到 `65535`。队列容量和每帧处理数量必须大于零。

## 六、数据流与线程模型

```text
外部程序发送 UTF-8 UDP 数据包
            ↓
后台接收线程调用 UdpClient.Receive
            ↓
复制数据、发送端 IP 和端口
            ↓
放入有容量上限的线程安全队列
            ↓
UdpReceiverBehaviour.Update
            ↓
严格 UTF-8 解码
            ↓
更新统计和诊断状态
            ↓
在 Unity 主线程触发 OnMessageReceived
```

后台线程不得调用任何 Unity API，包括 `Debug.Log`、`UnityEvent`、`GameObject`、UI 或 `InputRouter`。

停止监听时关闭 `UdpClient`，使阻塞中的 `Receive` 退出；随后等待接收线程结束并释放资源。由主动停止引起的关闭异常不作为运行错误上报。

组件在 `OnEnable` 中根据配置自动启动，在 `OnDisable` 和 `OnDestroy` 中停止监听。再次启用 GameObject 后，仅在“启动时自动监听”开启时重新开始。

`StopListening()` 会清空尚未交给主线程的消息和错误，避免重新启动后处理上一次监听会话留下的数据。每次成功开始新的监听会话时，接收数量、丢弃数量、最后消息、最后发送端和最近错误全部重置。

## 七、数据和队列规则

一个 UDP 数据包严格对应一条消息：

- 不按换行符拆分；
- 不调用 `Trim()`；
- 不自动解析 JSON；
- 保留合法 UTF-8 文本中的空格和换行；
- 非法 UTF-8 不触发正常消息事件，而是形成错误通知。

队列满时丢弃最旧消息并保留最新消息，同时增加 `DroppedMessageCount`。该策略面向温度、压力、距离等实时现场数据，最新状态比积压的旧状态更重要。

每帧最多处理指定数量的消息，避免突发 UDP 流量长时间占用 Unity 主线程。

`ReceivedMessageCount` 表示当前监听会话中从 Socket 收到的数据包总数，包括之后因队列溢出被丢弃或因 UTF-8 无效而上报错误的数据包。`DroppedMessageCount` 只统计队列溢出时被丢弃的数据包。`LastMessage` 和 `LastRemoteEndPoint` 只在成功解码并触发正常消息事件时更新。

UDP 本身可能丢包、重复或乱序。本模块不承诺可靠投递，也不在框架层增加确认和重传协议。

## 八、错误处理

以下错误不得导致 Unity 程序崩溃：

- 端口无效；
- 端口已被占用；
- 创建或绑定 `UdpClient` 失败；
- 后台接收异常；
- 严格 UTF-8 解码失败；
- 无法正常停止接收线程。

错误处理流程：

```text
后台或主线程发现错误
→ 记录待上报错误
→ 必要时停止监听
→ Update 更新 LastError
→ 输出中文错误日志
→ 主线程触发 OnError(string)
```

队列溢出属于可恢复警告，不停止监听。警告需要限频，避免高流量时刷满日志。

错误信息应包含实际端口和必要上下文，例如：

```text
SYAD UDP：无法监听端口 15000，该端口可能已被其他程序占用。
```

## 九、与现有模块的关系

UDP 模块只交付原始文本：

```text
UDP Receiver
→ 项目解析器或 Controller
├─ 实时数值 → UI Presenter
└─ 业务触发 → InputRouter.TryDispatch(command)
```

例如距离消息应先由项目解析并更新界面；只有进入指定距离范围时，才转换成 `PersonApproaching` 等离散命令交给 `InputRouter`。

UDP 模块不得引用 UI、InputRouter、RuntimeConfig 或具体项目协议，从而保持独立和可复用。

## 十、Demo

Demo 默认监听 `0.0.0.0:15000`，通过 Inspector 把 `OnMessageReceived` 绑定到 `UdpReceiverDemoPresenter.HandleMessage(string)`。

Presenter 仅显示：

- 当前监听状态；
- 监听端口；
- 最新文本；
- 最近发送端；
- 接收数量；
- 丢弃数量；
- 最近错误。

Demo README 提供 PowerShell 本机发送命令，并说明可以直接运行 `YCP031EarchProject` 验证国家名称接收。Demo 中不新增可复用 UDP 发送模块。

## 十一、测试

PlayMode 测试至少覆盖：

1. 非法端口不能启动并产生中文错误；
2. 启动后进入监听状态；
3. 重复启动不创建第二个接收器；
4. 回环地址发送 UTF-8 后收到完整文本；
5. `UnityEvent` 在 Unity 主线程触发；
6. 正确记录发送端 IP 和端口；
7. 停止后退出监听；
8. 重复停止不报错；
9. 停止后可以重新开始；
10. 队列满时丢弃最旧消息并累计数量；
11. 非法 UTF-8 进入错误事件，不进入消息事件；
12. GameObject 禁用或销毁后停止监听。

网络测试仅使用本机回环地址，不依赖外部设备或互联网。测试应动态选择可用端口，避免使用固定端口造成并行测试冲突。

## 十二、平台和兼容性

- 保持 Unity 2019.4 LTS 兼容；
- 保持 Unity 2022.3 LTS 兼容；
- 使用 Unity 现有 .NET API，不引入第三方网络库；
- 主要面向 Windows Editor 和 Windows Player；
- 局域网接收可能需要用户允许 Windows 防火墙访问；
- `127.0.0.1` 只表示本机，外部设备必须向运行 Unity Player 的电脑局域网 IP 发送。

## 十三、发布策略

实现阶段先把功能记录在 `CHANGELOG.md` 的“未发布”部分，稳定包版本暂时保持 `0.3.0`。

代码、Demo、文档和测试全部验证后，再由维护者决定何时把包版本更新为 `0.4.0`，创建 `v0.4.0` 标签并发布 GitHub Release。
