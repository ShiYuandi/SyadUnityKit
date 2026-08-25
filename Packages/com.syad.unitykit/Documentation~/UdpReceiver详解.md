# UDP Receiver 详解

`Syad.UnityKit.Networking.UdpReceiverBehaviour` 是一个面向现场项目的零编码 UDP 文本接收组件。它负责后台接收、线程安全排队和 Unity 主线程通知，不负责解释项目协议。

该模块自 `0.4.0` 起正式提供。

## 一、快速使用

1. 在启动场景创建一个 GameObject。
2. 添加组件：

```text
Add Component > SYAD Unity Kit > Networking > UDP Receiver
```

3. 把“监听端口”设置为外部程序发送的目标端口，例如 `15000`。
4. 在“收到文本消息”中绑定项目脚本的公开方法：

```csharp
public void HandleUdpMessage(string message)
{
    Debug.Log("收到 UDP：" + message);
}
```

5. 进入 Play Mode。组件默认在启用时自动监听，禁用或销毁时自动停止。

项目脚本不需要创建 `UdpClient`、线程或主线程队列。

## 二、数据流程

```text
外部程序发送一个 UDP 数据包
          ↓
后台线程接收 byte[] 和发送端地址
          ↓
容量受限的线程安全队列
          ↓
UdpReceiverBehaviour.Update
          ↓
严格 UTF-8 解码
          ↓
Unity 主线程触发 UnityEvent<string>
          ↓
项目 Controller / Presenter
```

后台线程不会调用 Unity API，因此 UnityEvent、UI、GameObject 和 InputRouter 都只会在主线程接触消息。

## 三、消息边界

UDP 自己保留数据包边界，因此本模块采用：

```text
一个 UDP 数据包 = 一条文本消息
```

组件不会：

- 按换行符拆分；
- 自动删除首尾空格；
- 自动拼接多个数据包；
- 自动解析 JSON、CSV 或传感器协议。

发送端把下面字符串放入一个数据包：

```text
{"temperature":36.5}
```

接收事件会得到同一个完整字符串。是否反序列化由项目代码决定。

第一版使用严格 UTF-8。无法解码的数据会进入“发生错误”事件，不会进入正常消息事件。

## 四、Inspector 配置

### 监听端口

合法范围是 `1～65535`，默认 `15000`。同一端口通常不能同时被两个程序独占监听。

### 启用时自动监听

开启后，GameObject 启用时开始监听，禁用时停止。重新启用会开始一个新的监听会话，并重置统计数据。

### 最大待处理消息数

默认 `1024`。后台收包速度超过 Unity 主线程处理速度时，队列不会无限增长。

队列满后：

```text
丢弃最旧消息
保留最新消息
增加队列丢弃数量
输出限频警告
```

该策略适合温度、距离、压力等实时值。若业务要求每条指令都可靠到达，UDP 和本组件都不适合，应由项目选择带确认机制的协议。

### 每帧最多处理消息数

默认 `100`。它限制每帧触发 UnityEvent 的最大次数，避免突发流量长时间阻塞主线程。

### 日志

“输出启动和停止日志”适合现场部署，能够明确显示程序监听的端口。

“输出每条接收日志”只适合调试。高频传感器会产生大量日志，正式运行时应关闭。

## 五、运行状态

组件提供：

```csharp
bool IsListening
int ListenPort
long ReceivedMessageCount
long DroppedMessageCount
string LastMessage
string LastRemoteEndPoint
string LastError
```

`ReceivedMessageCount` 是 Socket 收到的数据包总数，包括之后被队列丢弃或无法解码的数据包。

`LastMessage` 和 `LastRemoteEndPoint` 只在文本成功解码并交给正常事件后更新。

Play Mode 中的中文 Inspector 会直接显示这些状态，并提供“开始监听”和“停止监听”按钮。

## 六、手动控制

默认自动监听已经能满足普通项目。需要由按钮或业务状态控制时，可以关闭“启用时自动监听”，再把 Unity 按钮绑定到：

```csharp
receiver.StartListening();
receiver.StopListening();
```

两个方法都允许重复调用。停止后可以再次开始。

修改 Inspector 端口不会切换一个正在运行的 Socket，应停止后再重新开始。

## 七、与 InputRouter 的关系

UDP 接收器处理连续通信，InputRouter 处理离散业务命令，它们不是同一个职责。

```text
UDP "distance:0.45"
→ UdpReceiverBehaviour
→ 项目解析距离 0.45
├─ 更新实时距离 UI
└─ 距离小于阈值时
   → InputRouter.TryDispatch(PersonApproaching)
```

不要把每一帧温度、距离或压力都当作带冷却的 InputRouter 命令。

## 八、本机测试

保持 Unity 场景监听 `15000`，在 PowerShell 执行：

```powershell
$udp = New-Object System.Net.Sockets.UdpClient
$data = [Text.Encoding]::UTF8.GetBytes("中国")
$null = $udp.Send($data, $data.Length, "127.0.0.1", 15000)
$udp.Dispose()
```

一个完整的 `中国` 字符串会进入 UnityEvent。

## 九、局域网设备

`127.0.0.1` 永远指向程序自己所在的设备，只能用于同一台电脑上的程序通信。

外部传感器或另一台电脑需要把目标地址设置为运行 Unity Player 的电脑局域网 IP，例如：

```text
192.168.1.100:15000
```

组件固定监听 `0.0.0.0`，即本机全部 IPv4 网卡。如果本机回环测试成功而外部设备无法发送，应检查：

- 目标 IP 和端口；
- 网络是否互通；
- Windows 防火墙；
- 端口是否被其他程序占用。

## 十、错误与 UDP 限制

组件会用中文报告端口无效、端口占用、Socket 接收失败和 UTF-8 解码失败。

UDP 本身可能：

- 丢包；
- 重复；
- 乱序；
- 数据包过大时被网络丢弃。

本模块不增加确认、重发和排序。它适合实时状态、识别结果和允许偶尔丢包的现场数据，不适合支付、存档等必须可靠交付的数据。

## 十一、Demo 和测试

示例场景：

```text
Assets/Demo/UDP/Scenes/UdpReceiverDemo.unity
```

测试文件：

```text
Packages/com.syad.unitykit/Tests/Runtime/UdpReceiverBehaviourTests.cs
```

UDP 模块包含 12 项 PlayMode 测试，覆盖端口校验、回环接收、主线程事件、发送端信息、停止与重启、队列溢出、非法 UTF-8 和 GameObject 生命周期。
