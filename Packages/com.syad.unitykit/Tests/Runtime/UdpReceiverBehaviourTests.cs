using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Syad.UnityKit.Networking;
using UnityEngine;
using UnityEngine.TestTools;

namespace Syad.UnityKit.Tests
{
    public sealed class UdpReceiverBehaviourTests
    {
        private readonly List<GameObject> _createdObjects =
            new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                GameObject gameObject = _createdObjects[i];
                if (gameObject == null)
                {
                    continue;
                }

                UdpReceiverBehaviour receiver =
                    gameObject.GetComponent<UdpReceiverBehaviour>();
                if (receiver != null)
                {
                    receiver.StopListening();
                }

                UnityEngine.Object.DestroyImmediate(gameObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void StartListening_InvalidPortReportsChineseError()
        {
            UdpReceiverBehaviour receiver = CreateReceiver(0);
            string receivedError = null;
            receiver.OnError.AddListener(error => receivedError = error);

            const string expectedError =
                "SYAD UDP：监听端口必须在 1～65535 之间，当前值为 0。";
            LogAssert.Expect(LogType.Error, expectedError);

            receiver.StartListening();

            Assert.That(receiver.IsListening, Is.False);
            Assert.That(receiver.LastError, Is.EqualTo(expectedError));
            Assert.That(receivedError, Is.EqualTo(expectedError));
        }

        [Test]
        public void StartListening_ValidPortEntersListeningState()
        {
            UdpReceiverBehaviour receiver = CreateReceiver(GetAvailablePort());

            receiver.StartListening();

            Assert.That(receiver.IsListening, Is.True);
        }

        [Test]
        public void StartListening_CalledTwiceKeepsSameThread()
        {
            UdpReceiverBehaviour receiver = CreateReceiver(GetAvailablePort());
            receiver.StartListening();
            Thread firstThread = GetPrivateField<Thread>(receiver, "_receiveThread");

            receiver.StartListening();
            Thread secondThread = GetPrivateField<Thread>(receiver, "_receiveThread");

            Assert.That(receiver.IsListening, Is.True);
            Assert.That(secondThread, Is.SameAs(firstThread));
        }

        [UnityTest]
        public IEnumerator Receive_Utf8DatagramInvokesEventWithCompleteText()
        {
            int port = GetAvailablePort();
            UdpReceiverBehaviour receiver = CreateReceiver(port);
            string receivedMessage = null;
            receiver.OnMessageReceived.AddListener(
                message => receivedMessage = message);
            receiver.StartListening();

            Send(port, Encoding.UTF8.GetBytes("中国\n完整消息"));

            yield return WaitUntil(
                () => receivedMessage != null,
                "等待 UDP UTF-8 文本超时。");

            Assert.That(receivedMessage, Is.EqualTo("中国\n完整消息"));
            Assert.That(receiver.LastMessage, Is.EqualTo(receivedMessage));
            Assert.That(receiver.ReceivedMessageCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Receive_EventRunsOnUnityMainThread()
        {
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            int callbackThreadId = -1;
            int port = GetAvailablePort();
            UdpReceiverBehaviour receiver = CreateReceiver(port);
            receiver.OnMessageReceived.AddListener(
                message => callbackThreadId = Thread.CurrentThread.ManagedThreadId);
            receiver.StartListening();

            Send(port, Encoding.UTF8.GetBytes("main-thread"));

            yield return WaitUntil(
                () => callbackThreadId >= 0,
                "等待 UDP 主线程事件超时。");

            Assert.That(callbackThreadId, Is.EqualTo(mainThreadId));
        }

        [UnityTest]
        public IEnumerator Receive_RecordsRemoteAddressAndPort()
        {
            int port = GetAvailablePort();
            UdpReceiverBehaviour receiver = CreateReceiver(port);
            receiver.StartListening();

            using (UdpClient sender = new UdpClient())
            {
                byte[] data = Encoding.UTF8.GetBytes("endpoint");
                sender.Send(data, data.Length, IPAddress.Loopback.ToString(), port);
                int senderPort = ((IPEndPoint)sender.Client.LocalEndPoint).Port;

                yield return WaitUntil(
                    () => receiver.LastRemoteEndPoint != null,
                    "等待 UDP 发送端信息超时。");

                Assert.That(
                    receiver.LastRemoteEndPoint,
                    Is.EqualTo("127.0.0.1:" + senderPort));
            }
        }

        [Test]
        public void StopListening_StopsAndCanBeCalledTwice()
        {
            UdpReceiverBehaviour receiver = CreateReceiver(GetAvailablePort());
            receiver.StartListening();

            receiver.StopListening();
            receiver.StopListening();

            Assert.That(receiver.IsListening, Is.False);
        }

        [Test]
        public void StopThenStart_CanListenAgain()
        {
            UdpReceiverBehaviour receiver = CreateReceiver(GetAvailablePort());
            receiver.StartListening();
            receiver.StopListening();

            receiver.StartListening();

            Assert.That(receiver.IsListening, Is.True);
        }

        [UnityTest]
        public IEnumerator QueueOverflow_DropsOldestMessages()
        {
            int port = GetAvailablePort();
            UdpReceiverBehaviour receiver = CreateReceiver(
                port,
                maxPendingMessages: 2,
                maxMessagesPerFrame: 1);
            receiver.StartListening();

            using (UdpClient sender = new UdpClient())
            {
                for (int i = 0; i < 5; i++)
                {
                    byte[] data = Encoding.UTF8.GetBytes("message-" + i);
                    sender.Send(
                        data,
                        data.Length,
                        IPAddress.Loopback.ToString(),
                        port);
                }
            }

            DateTime deadline = DateTime.UtcNow.AddSeconds(2);
            while (receiver.ReceivedMessageCount < 5
                && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(5);
            }

            Assert.That(receiver.ReceivedMessageCount, Is.EqualTo(5));
            Assert.That(receiver.DroppedMessageCount, Is.EqualTo(3));

            LogAssert.Expect(
                LogType.Warning,
                "SYAD UDP：待处理队列已满，已丢弃最旧消息。"
                + "当前会话累计丢弃 3 条。");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Receive_InvalidUtf8ReportsErrorWithoutMessageEvent()
        {
            int port = GetAvailablePort();
            UdpReceiverBehaviour receiver = CreateReceiver(port);
            int messageCount = 0;
            string receivedError = null;
            receiver.OnMessageReceived.AddListener(message => messageCount++);
            receiver.OnError.AddListener(error => receivedError = error);
            receiver.StartListening();

            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    "^SYAD UDP：来自 127\\.0\\.0\\.1:[0-9]+ 的数据不是有效 UTF-8："));
            Send(port, new byte[] { 0xC3, 0x28 });

            yield return WaitUntil(
                () => receivedError != null,
                "等待非法 UTF-8 错误超时。");

            Assert.That(messageCount, Is.EqualTo(0));
            StringAssert.Contains("不是有效 UTF-8", receivedError);
        }

        [Test]
        public void DisableGameObject_AutomaticallyStopsListening()
        {
            UdpReceiverBehaviour receiver = CreateReceiver(GetAvailablePort());
            receiver.StartListening();

            receiver.gameObject.SetActive(false);

            Assert.That(receiver.IsListening, Is.False);
        }

        [UnityTest]
        public IEnumerator EnableGameObject_AutomaticallyStartsWhenConfigured()
        {
            int port = GetAvailablePort();
            GameObject gameObject = new GameObject("UDP Auto Start Test");
            gameObject.SetActive(false);
            _createdObjects.Add(gameObject);

            UdpReceiverBehaviour receiver =
                gameObject.AddComponent<UdpReceiverBehaviour>();
            SetPrivateField(receiver, "_listenPort", port);
            SetPrivateField(receiver, "_startOnEnable", true);
            SetPrivateField(receiver, "_logLifecycle", false);

            gameObject.SetActive(true);
            yield return null;

            Assert.That(receiver.IsListening, Is.True);
        }

        private UdpReceiverBehaviour CreateReceiver(
            int port,
            int maxPendingMessages = 1024,
            int maxMessagesPerFrame = 100)
        {
            GameObject gameObject = new GameObject("UDP Receiver Test");
            gameObject.SetActive(false);
            _createdObjects.Add(gameObject);

            UdpReceiverBehaviour receiver =
                gameObject.AddComponent<UdpReceiverBehaviour>();
            SetPrivateField(receiver, "_listenPort", port);
            SetPrivateField(receiver, "_startOnEnable", false);
            SetPrivateField(receiver, "_maxPendingMessages", maxPendingMessages);
            SetPrivateField(receiver, "_maxMessagesPerFrame", maxMessagesPerFrame);
            SetPrivateField(receiver, "_logLifecycle", false);
            SetPrivateField(receiver, "_logReceivedMessages", false);

            gameObject.SetActive(true);
            return receiver;
        }

        private static IEnumerator WaitUntil(
            Func<bool> condition,
            string timeoutMessage)
        {
            float deadline = Time.realtimeSinceStartup + 2f;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, timeoutMessage);
        }

        private static int GetAvailablePort()
        {
            using (UdpClient temporaryClient = new UdpClient(0))
            {
                return ((IPEndPoint)temporaryClient.Client.LocalEndPoint).Port;
            }
        }

        private static void Send(int port, byte[] data)
        {
            using (UdpClient sender = new UdpClient())
            {
                sender.Send(
                    data,
                    data.Length,
                    IPAddress.Loopback.ToString(),
                    port);
            }
        }

        private static void SetPrivateField<T>(
            UdpReceiverBehaviour receiver,
            string fieldName,
            T value)
        {
            FieldInfo field = typeof(UdpReceiverBehaviour).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "没有找到测试字段：" + fieldName);
            field.SetValue(receiver, value);
        }

        private static T GetPrivateField<T>(
            UdpReceiverBehaviour receiver,
            string fieldName)
        {
            FieldInfo field = typeof(UdpReceiverBehaviour).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "没有找到测试字段：" + fieldName);
            return (T)field.GetValue(receiver);
        }
    }
}
