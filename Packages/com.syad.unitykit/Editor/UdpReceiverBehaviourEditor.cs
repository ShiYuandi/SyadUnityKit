using Syad.UnityKit.Networking;
using UnityEditor;
using UnityEngine;

namespace Syad.UnityKit.Editor
{
    /// <summary>为 UDP 零编码接收组件提供中文 Inspector 和运行状态。</summary>
    [CustomEditor(typeof(UdpReceiverBehaviour))]
    [CanEditMultipleObjects]
    public sealed class UdpReceiverBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty _listenPort;
        private SerializedProperty _startOnEnable;
        private SerializedProperty _maxPendingMessages;
        private SerializedProperty _maxMessagesPerFrame;
        private SerializedProperty _logLifecycle;
        private SerializedProperty _logReceivedMessages;
        private SerializedProperty _onMessageReceived;
        private SerializedProperty _onError;

        private void OnEnable()
        {
            _listenPort = serializedObject.FindProperty("_listenPort");
            _startOnEnable = serializedObject.FindProperty("_startOnEnable");
            _maxPendingMessages = serializedObject.FindProperty("_maxPendingMessages");
            _maxMessagesPerFrame = serializedObject.FindProperty("_maxMessagesPerFrame");
            _logLifecycle = serializedObject.FindProperty("_logLifecycle");
            _logReceivedMessages = serializedObject.FindProperty("_logReceivedMessages");
            _onMessageReceived = serializedObject.FindProperty("_onMessageReceived");
            _onError = serializedObject.FindProperty("_onError");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "组件监听本机全部 IPv4 网卡。一个 UDP 数据包会作为一条 UTF-8 文本，"
                + "在 Unity 主线程触发“收到文本消息”。",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                _listenPort,
                new GUIContent("监听端口"));
            EditorGUILayout.PropertyField(
                _startOnEnable,
                new GUIContent("启用时自动监听"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("队列保护", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _maxPendingMessages,
                new GUIContent("最大待处理消息数"));
            EditorGUILayout.PropertyField(
                _maxMessagesPerFrame,
                new GUIContent("每帧最多处理消息数"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _logLifecycle,
                new GUIContent("输出启动和停止日志"));
            EditorGUILayout.PropertyField(
                _logReceivedMessages,
                new GUIContent("输出每条接收日志"));

            if (_logReceivedMessages.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "高频传感器数据会产生大量日志，正式运行时建议关闭。",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(
                _onMessageReceived,
                new GUIContent("收到文本消息"));
            EditorGUILayout.PropertyField(
                _onError,
                new GUIContent("发生错误"));

            serializedObject.ApplyModifiedProperties();

            if (!Application.isPlaying || targets.Length != 1)
            {
                return;
            }

            DrawRuntimeStatus((UdpReceiverBehaviour)target);
        }

        private static void DrawRuntimeStatus(UdpReceiverBehaviour receiver)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("运行状态", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "监听状态",
                receiver.IsListening ? "正在监听" : "已停止");
            EditorGUILayout.LabelField(
                "收到数据包",
                receiver.ReceivedMessageCount.ToString());
            EditorGUILayout.LabelField(
                "队列丢弃",
                receiver.DroppedMessageCount.ToString());
            EditorGUILayout.LabelField(
                "最近发送端",
                string.IsNullOrEmpty(receiver.LastRemoteEndPoint)
                    ? "无"
                    : receiver.LastRemoteEndPoint);
            EditorGUILayout.LabelField(
                "最近文本",
                string.IsNullOrEmpty(receiver.LastMessage)
                    ? "无"
                    : receiver.LastMessage);

            if (!string.IsNullOrEmpty(receiver.LastError))
            {
                EditorGUILayout.HelpBox(receiver.LastError, MessageType.Error);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("开始监听"))
            {
                receiver.StartListening();
            }

            if (GUILayout.Button("停止监听"))
            {
                receiver.StopListening();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
