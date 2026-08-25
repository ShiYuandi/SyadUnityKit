using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Syad.UnityKit.Networking
{
    /// <summary>
    /// 在后台线程接收 UDP 数据包，并在 Unity 主线程把 UTF-8 文本交给 UnityEvent。
    /// 一个 UDP 数据包对应一条文本消息；本组件不解析任何项目业务协议。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SYAD Unity Kit/Networking/UDP Receiver")]
    public sealed class UdpReceiverBehaviour : MonoBehaviour
    {
        private const int MinimumPort = 1;
        private const int MaximumPort = 65535;
        private const int StopThreadTimeoutMilliseconds = 1000;

        private static readonly UTF8Encoding StrictUtf8 =
            new UTF8Encoding(false, true);

        [Header("监听设置")]
        [SerializeField]
        [Tooltip("本机监听端口，合法范围是 1～65535。组件会监听本机全部 IPv4 网卡。")]
        private int _listenPort = 15000;

        [SerializeField]
        [Tooltip("组件启用时是否自动开始监听，禁用时会自动停止。")]
        private bool _startOnEnable = true;

        [Header("队列保护")]
        [SerializeField]
        [Min(1)]
        [Tooltip("后台线程最多保留多少条等待主线程处理的消息。队列满时丢弃最旧消息。")]
        private int _maxPendingMessages = 1024;

        [SerializeField]
        [Min(1)]
        [Tooltip("每帧最多把多少条消息交给 UnityEvent，避免突发流量阻塞主线程。")]
        private int _maxMessagesPerFrame = 100;

        [Header("日志")]
        [SerializeField]
        [Tooltip("开始监听和停止监听时是否输出日志。")]
        private bool _logLifecycle = true;

        [SerializeField]
        [Tooltip("是否为每一条成功接收的文本输出日志。高频传感器数据不建议开启。")]
        private bool _logReceivedMessages = false;

        [Header("事件")]
        [SerializeField]
        [Tooltip("成功收到并解码 UTF-8 文本后，在 Unity 主线程触发。")]
        private UdpTextEvent _onMessageReceived = new UdpTextEvent();

        [SerializeField]
        [Tooltip("监听或解码发生错误后，在 Unity 主线程触发。")]
        private UdpTextEvent _onError = new UdpTextEvent();

        private readonly object _lifecycleLock = new object();
        private readonly object _messageQueueLock = new object();
        private readonly object _errorQueueLock = new object();
        private readonly Queue<PendingDatagram> _pendingMessages =
            new Queue<PendingDatagram>();
        private readonly Queue<string> _pendingErrors = new Queue<string>();

        private UdpClient _udpClient;
        private Thread _receiveThread;
        private volatile bool _stopRequested;
        private volatile bool _isListening;
        private int _activeListenPort;
        private long _receivedMessageCount;
        private long _droppedMessageCount;
        private int _overflowWarningPending;

        /// <summary>当前是否已经成功启动并仍在监听。</summary>
        public bool IsListening
        {
            get { return _isListening; }
        }

        /// <summary>Inspector 中配置的本机监听端口。</summary>
        public int ListenPort
        {
            get { return _isListening ? _activeListenPort : _listenPort; }
        }

        /// <summary>当前监听会话从 Socket 收到的数据包总数。</summary>
        public long ReceivedMessageCount
        {
            get { return Interlocked.Read(ref _receivedMessageCount); }
        }

        /// <summary>当前监听会话因队列已满而丢弃的数据包数量。</summary>
        public long DroppedMessageCount
        {
            get { return Interlocked.Read(ref _droppedMessageCount); }
        }

        /// <summary>最近一条成功解码并交给项目的文本。</summary>
        public string LastMessage { get; private set; }

        /// <summary>最近一条成功消息的发送端 IP 和端口。</summary>
        public string LastRemoteEndPoint { get; private set; }

        /// <summary>当前监听会话最近一次错误；没有错误时为 null。</summary>
        public string LastError { get; private set; }

        /// <summary>成功收到文本时触发的 Inspector 事件。</summary>
        public UdpTextEvent OnMessageReceived
        {
            get { return _onMessageReceived; }
        }

        /// <summary>发生错误时触发的 Inspector 事件。</summary>
        public UdpTextEvent OnError
        {
            get { return _onError; }
        }

        private void OnEnable()
        {
            if (Application.isPlaying && _startOnEnable)
            {
                StartListening();
            }
        }

        private void Update()
        {
            ReportPendingOverflowWarning();
            DispatchPendingErrors();
            DispatchPendingMessages();
        }

        private void OnDisable()
        {
            StopListening();
        }

        private void OnDestroy()
        {
            StopListening();
        }

        /// <summary>
        /// 使用 Inspector 中的端口开始监听。重复调用不会创建第二个接收线程。
        /// </summary>
        public void StartListening()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("SYAD UDP：只会在 Play Mode 或构建后的 Player 中监听。", this);
                return;
            }

            lock (_lifecycleLock)
            {
                if (_isListening
                    || (_receiveThread != null && _receiveThread.IsAlive))
                {
                    return;
                }
            }

            string validationError;
            if (!TryValidateConfiguration(out validationError))
            {
                ResetSessionState();
                ReportErrorOnMainThread(validationError);
                return;
            }

            string startError = null;

            lock (_lifecycleLock)
            {
                if (_isListening
                    || (_receiveThread != null && _receiveThread.IsAlive))
                {
                    return;
                }

                DisposeStaleClientInternal();
                ResetSessionState();
                _stopRequested = false;

                try
                {
                    int activePort = _listenPort;
                    UdpClient client = new UdpClient(AddressFamily.InterNetwork);
                    client.ExclusiveAddressUse = true;
                    client.Client.SetSocketOption(
                        SocketOptionLevel.Socket,
                        SocketOptionName.ReuseAddress,
                        false);
                    client.Client.Bind(
                        new IPEndPoint(IPAddress.Any, activePort));

                    Thread thread = new Thread(
                        () => ReceiveLoop(client, activePort));
                    thread.IsBackground = true;
                    thread.Name = "SYAD UDP Receiver " + activePort;

                    _udpClient = client;
                    _receiveThread = thread;
                    _activeListenPort = activePort;
                    _isListening = true;
                    thread.Start();
                }
                catch (Exception exception)
                {
                    _isListening = false;
                    _stopRequested = true;
                    DisposeStaleClientInternal();
                    _receiveThread = null;
                    startError = BuildStartError(exception);
                }
            }

            if (startError != null)
            {
                ReportErrorOnMainThread(startError);
                return;
            }

            if (_logLifecycle)
            {
                Debug.Log(
                    "SYAD UDP：开始监听 0.0.0.0:"
                    + _listenPort
                    + "，等待 UTF-8 文本。",
                    this);
            }
        }

        /// <summary>
        /// 停止监听并清空尚未交给主线程的消息。重复调用不会报错。
        /// </summary>
        public void StopListening()
        {
            UdpClient client;
            Thread thread;
            bool wasRunning;
            int activePort;

            lock (_lifecycleLock)
            {
                wasRunning = _isListening
                    || _udpClient != null
                    || (_receiveThread != null && _receiveThread.IsAlive);
                activePort = _activeListenPort > 0
                    ? _activeListenPort
                    : _listenPort;

                _stopRequested = true;
                _isListening = false;
                client = _udpClient;
                thread = _receiveThread;
                _udpClient = null;
                _receiveThread = null;
                _activeListenPort = 0;
            }

            CloseClient(client);

            bool threadStopped = true;
            if (thread != null
                && thread != Thread.CurrentThread
                && thread.IsAlive)
            {
                threadStopped = thread.Join(StopThreadTimeoutMilliseconds);
            }

            ClearPendingQueues();

            if (!threadStopped)
            {
                ReportErrorOnMainThread(
                    "SYAD UDP：停止监听端口 "
                    + activePort
                    + " 时，接收线程未能在限定时间内退出。");
            }

            if (wasRunning && _logLifecycle)
            {
                Debug.Log("SYAD UDP：已停止监听端口 " + activePort + "。", this);
            }
        }

        private void ReceiveLoop(UdpClient client, int activePort)
        {
            try
            {
                while (!_stopRequested)
                {
                    IPEndPoint remoteEndPoint = null;
                    byte[] data;

                    try
                    {
                        data = client.Receive(ref remoteEndPoint);
                    }
                    catch (ObjectDisposedException)
                    {
                        if (!_stopRequested)
                        {
                            EnqueueError(
                                "SYAD UDP：监听端口 "
                                + activePort
                                + " 时接收器意外关闭。");
                        }

                        break;
                    }
                    catch (SocketException exception)
                    {
                        if (!_stopRequested)
                        {
                            EnqueueError(
                                "SYAD UDP：监听端口 "
                                + activePort
                                + " 时接收失败："
                                + exception.Message);
                        }

                        break;
                    }
                    catch (Exception exception)
                    {
                        if (!_stopRequested)
                        {
                            EnqueueError(
                                "SYAD UDP：监听端口 "
                                + activePort
                                + " 时发生未预期错误："
                                + exception.Message);
                        }

                        break;
                    }

                    Interlocked.Increment(ref _receivedMessageCount);
                    EnqueueMessage(
                        new PendingDatagram(
                            data,
                            FormatRemoteEndPoint(remoteEndPoint)));
                }
            }
            finally
            {
                _isListening = false;
                CloseClient(client);
            }
        }

        private void EnqueueMessage(PendingDatagram datagram)
        {
            lock (_messageQueueLock)
            {
                int queueLimit = Math.Max(1, _maxPendingMessages);
                if (_pendingMessages.Count >= queueLimit)
                {
                    _pendingMessages.Dequeue();
                    Interlocked.Increment(ref _droppedMessageCount);
                    Interlocked.Exchange(ref _overflowWarningPending, 1);
                }

                _pendingMessages.Enqueue(datagram);
            }
        }

        private void EnqueueError(string error)
        {
            lock (_errorQueueLock)
            {
                _pendingErrors.Enqueue(error);
            }
        }

        private void DispatchPendingMessages()
        {
            int processedCount = 0;
            int limit = Math.Max(1, _maxMessagesPerFrame);

            while (processedCount < limit)
            {
                PendingDatagram datagram;
                lock (_messageQueueLock)
                {
                    if (_pendingMessages.Count == 0)
                    {
                        return;
                    }

                    datagram = _pendingMessages.Dequeue();
                }

                processedCount++;
                string message;

                try
                {
                    message = StrictUtf8.GetString(datagram.Data);
                }
                catch (DecoderFallbackException exception)
                {
                    ReportErrorOnMainThread(
                        "SYAD UDP：来自 "
                        + datagram.RemoteEndPoint
                        + " 的数据不是有效 UTF-8："
                        + exception.Message);
                    continue;
                }

                LastMessage = message;
                LastRemoteEndPoint = datagram.RemoteEndPoint;

                if (_logReceivedMessages)
                {
                    Debug.Log(
                        "SYAD UDP：收到来自 "
                        + datagram.RemoteEndPoint
                        + " 的文本："
                        + message,
                        this);
                }

                if (_onMessageReceived != null)
                {
                    _onMessageReceived.Invoke(message);
                }
            }
        }

        private void DispatchPendingErrors()
        {
            while (true)
            {
                string error;
                lock (_errorQueueLock)
                {
                    if (_pendingErrors.Count == 0)
                    {
                        return;
                    }

                    error = _pendingErrors.Dequeue();
                }

                ReportErrorOnMainThread(error);
            }
        }

        private void ReportPendingOverflowWarning()
        {
            if (Interlocked.Exchange(ref _overflowWarningPending, 0) == 0)
            {
                return;
            }

            Debug.LogWarning(
                "SYAD UDP：待处理队列已满，已丢弃最旧消息。"
                + "当前会话累计丢弃 "
                + DroppedMessageCount
                + " 条。",
                this);
        }

        private void ReportErrorOnMainThread(string error)
        {
            LastError = error;
            Debug.LogError(error, this);

            if (_onError != null)
            {
                _onError.Invoke(error);
            }
        }

        private bool TryValidateConfiguration(out string error)
        {
            if (_listenPort < MinimumPort || _listenPort > MaximumPort)
            {
                error = "SYAD UDP：监听端口必须在 1～65535 之间，当前值为 "
                    + _listenPort
                    + "。";
                return false;
            }

            if (_maxPendingMessages <= 0)
            {
                error = "SYAD UDP：最大待处理消息数必须大于零。";
                return false;
            }

            if (_maxMessagesPerFrame <= 0)
            {
                error = "SYAD UDP：每帧最多处理消息数必须大于零。";
                return false;
            }

            error = null;
            return true;
        }

        private string BuildStartError(Exception exception)
        {
            SocketException socketException = exception as SocketException;
            if (socketException != null
                && socketException.SocketErrorCode
                    == SocketError.AddressAlreadyInUse)
            {
                return "SYAD UDP：无法监听端口 "
                    + _listenPort
                    + "，该端口已被其他程序占用。";
            }

            return "SYAD UDP：无法监听端口 "
                + _listenPort
                + "："
                + exception.Message;
        }

        private void ResetSessionState()
        {
            ClearPendingQueues();
            Interlocked.Exchange(ref _receivedMessageCount, 0L);
            Interlocked.Exchange(ref _droppedMessageCount, 0L);
            LastMessage = null;
            LastRemoteEndPoint = null;
            LastError = null;
        }

        private void ClearPendingQueues()
        {
            lock (_messageQueueLock)
            {
                _pendingMessages.Clear();
            }

            lock (_errorQueueLock)
            {
                _pendingErrors.Clear();
            }

            Interlocked.Exchange(ref _overflowWarningPending, 0);
        }

        private void DisposeStaleClientInternal()
        {
            UdpClient staleClient = _udpClient;
            _udpClient = null;
            CloseClient(staleClient);
        }

        private static void CloseClient(UdpClient client)
        {
            if (client == null)
            {
                return;
            }

            try
            {
                client.Close();
            }
            catch (Exception)
            {
                // 关闭操作必须可重复，资源已经释放时不再上报第二个错误。
            }
        }

        private static string FormatRemoteEndPoint(IPEndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null)
            {
                return "未知发送端";
            }

            return remoteEndPoint.Address + ":" + remoteEndPoint.Port;
        }

        private sealed class PendingDatagram
        {
            public readonly byte[] Data;
            public readonly string RemoteEndPoint;

            public PendingDatagram(byte[] data, string remoteEndPoint)
            {
                Data = data;
                RemoteEndPoint = remoteEndPoint;
            }
        }
    }
}
