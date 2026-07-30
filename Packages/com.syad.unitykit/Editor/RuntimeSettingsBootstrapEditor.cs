using Syad.UnityKit.RuntimeSettings;
using UnityEditor;
using UnityEngine;

namespace Syad.UnityKit.Editor
{
    /// <summary>为开箱即用组件提供中文 Inspector。</summary>
    [CustomEditor(typeof(RuntimeSettingsBootstrap))]
    [CanEditMultipleObjects]
    public sealed class RuntimeSettingsBootstrapEditor : UnityEditor.Editor
    {
        private SerializedProperty _configRelativePath;
        private SerializedProperty _applyOnAwake;
        private SerializedProperty _enableReloadKey;
        private SerializedProperty _reloadKey;
        private SerializedProperty _logSuccess;

        private void OnEnable()
        {
            _configRelativePath = serializedObject.FindProperty("_configRelativePath");
            _applyOnAwake = serializedObject.FindProperty("_applyOnAwake");
            _enableReloadKey = serializedObject.FindProperty("_enableReloadKey");
            _reloadKey = serializedObject.FindProperty("_reloadKey");
            _logSuccess = serializedObject.FindProperty("_logSuccess");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "组件会从 StreamingAssets 读取 JSON。"
                + "Editor 固定只读取和校验，构建后的 Player 才应用窗口设置。",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                _configRelativePath,
                new GUIContent("配置相对路径"));
            EditorGUILayout.PropertyField(
                _applyOnAwake,
                new GUIContent("启动时自动应用"));
            EditorGUILayout.PropertyField(
                _enableReloadKey,
                new GUIContent("允许按键重新加载"));

            if (_enableReloadKey.boolValue)
            {
                EditorGUILayout.PropertyField(
                    _reloadKey,
                    new GUIContent("重新加载按键"));
                EditorGUILayout.HelpBox(
                    "程序启动时会输出当前按键绑定；按键真正触发时也会输出重新加载日志。"
                    + "这两条提示不受“输出成功日志”开关影响。",
                    MessageType.Info);
            }

            EditorGUILayout.PropertyField(
                _logSuccess,
                new GUIContent("输出成功日志"));

            serializedObject.ApplyModifiedProperties();

            if (!Application.isPlaying || targets.Length != 1)
            {
                return;
            }

            RuntimeSettingsBootstrap bootstrap =
                (RuntimeSettingsBootstrap)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "读取状态",
                bootstrap.IsLoaded ? "成功" : "尚未成功");
            EditorGUILayout.LabelField(
                "应用状态",
                bootstrap.IsApplied ? "已应用" : "未应用");

            if (!string.IsNullOrEmpty(bootstrap.LastError))
            {
                EditorGUILayout.HelpBox(bootstrap.LastError, MessageType.Error);
            }

            if (GUILayout.Button("立即重新读取并应用"))
            {
                bootstrap.ReloadAndApply();
            }
        }
    }
}
