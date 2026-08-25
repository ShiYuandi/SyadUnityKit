# UDP Receiver Demo

该示例展示如何通过一个场景组件接收外部程序发送的 UTF-8 UDP 文本，不需要编写网络线程和主线程切换代码。

## 场景

打开：

```text
Assets/Demo/UDP/Scenes/UdpReceiverDemo.unity
```

进入 Play Mode 后，场景默认监听：

```text
0.0.0.0:15000
```

`0.0.0.0` 表示本机全部 IPv4 网卡。发送到 `127.0.0.1:15000` 的本机消息和发送到本机局域网 IP 的外部设备消息都可以被接收。

## PowerShell 本机测试

保持场景运行，在 PowerShell 中执行：

```powershell
$udp = New-Object System.Net.Sockets.UdpClient
$data = [Text.Encoding]::UTF8.GetBytes("中国")
$null = $udp.Send($data, $data.Length, "127.0.0.1", 15000)
$udp.Dispose()
```

Game 窗口会显示完整文本、发送端地址和接收数量。

## 接入 YCP031EarchProject

`YCP031EarchProject` 会把 OCR 识别出的国家名称编码为 UTF-8，并发送到 `127.0.0.1:15000`。保持本场景运行后启动该程序，识别结果会直接进入“收到文本消息”UnityEvent。

## Inspector 绑定

场景中的事件关系是：

```text
UdpReceiverBehaviour.收到文本消息
→ UdpReceiverDemoPresenter.HandleMessage(string)

UdpReceiverBehaviour.发生错误
→ UdpReceiverDemoPresenter.HandleError(string)
```

项目中可以用同样方式把事件绑定到自己的 Controller。UDP 组件只交付原始文本，不负责解析国家名称、温度、距离或其他业务协议。

## 局域网设备

外部设备不能把数据发到自己的 `127.0.0.1`。它必须把目标 IP 设置为运行 Unity Player 的电脑局域网 IP，并使用相同端口。

如果本机测试成功而局域网设备无法发送，请检查：

- 两台设备是否位于可互通的网络；
- Windows 防火墙是否允许 Unity Player 接收 UDP；
- 目标 IP 和端口是否正确；
- 端口是否已被另一个程序占用。
