using System;
using System.Collections;
using Syad.UnityKit.RuntimeConfig;
using Syad.UnityKit.Windows;
using UnityEngine;

namespace Syad.UnityKit.RuntimeSettings
{
    /// <summary>
    /// 开箱即用地读取并应用 StreamingAssets 中的运行设置。
    /// 简单项目只需把本组件添加到启动场景，不需要另写连接代码。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    [AddComponentMenu("SYAD Unity Kit/Runtime Settings Bootstrap")]
    public sealed class RuntimeSettingsBootstrap : MonoBehaviour
    {
        public const string DefaultConfigRelativePath =
            "Configs/runtime-settings.json";

        [SerializeField]
        [Tooltip("相对于 StreamingAssets 的 JSON 配置路径。")]
        private string _configRelativePath = DefaultConfigRelativePath;

        [SerializeField]
        [Tooltip("组件启动时自动读取并应用配置。")]
        private bool _applyOnAwake = true;

        [SerializeField]
        [Tooltip("是否允许在运行时按指定按键重新读取配置。启用后会输出按键绑定和触发日志。")]
        private bool _enableReloadKey = false;

        [SerializeField]
        [Tooltip("重新读取配置使用的按键。")]
        private KeyCode _reloadKey = KeyCode.R;

        [SerializeField]
        [Tooltip("成功读取或应用后是否输出日志。")]
        private bool _logSuccess = true;

        private RuntimeConfigLoader _loader;
        private WindowsWindowController _windowsWindowController;
        private Coroutine _applyWindowsWindowRoutine;

        /// <summary>当前配置文件的相对路径。</summary>
        public string ConfigRelativePath
        {
            get { return _configRelativePath; }
        }

        /// <summary>最近一次成功读取并通过校验的设置。</summary>
        public ApplicationRuntimeSettings CurrentSettings { get; private set; }

        /// <summary>最近一次读取是否成功。</summary>
        public bool IsLoaded { get; private set; }

        /// <summary>最近一次设置是否已经应用；Editor 只读取模式下为 false。</summary>
        public bool IsApplied { get; private set; }

        /// <summary>最近一次失败原因；没有错误时为 null。</summary>
        public string LastError { get; private set; }

        private void Awake()
        {
            _windowsWindowController = new WindowsWindowController();

            if (_enableReloadKey)
            {
                Debug.Log(
                    "SYAD RuntimeSettings：已绑定重新读取配置按键 ["
                    + _reloadKey
                    + "]，配置路径："
                    + _configRelativePath,
                    this);
            }

            if (_applyOnAwake)
            {
                ReloadAndApply();
            }
        }

        private void Update()
        {
            if (_enableReloadKey
                && UnityEngine.Input.GetKeyDown(_reloadKey))
            {
                Debug.Log(
                    "SYAD RuntimeSettings：检测到重新读取配置按键 ["
                    + _reloadKey
                    + "]，开始重新加载："
                    + _configRelativePath,
                    this);
                ReloadAndApply();
            }
        }

        /// <summary>重新读取、校验并应用配置。</summary>
        public void ReloadAndApply()
        {
            StopPendingWindowsWindowApply();
            CurrentSettings = null;
            IsLoaded = false;
            IsApplied = false;
            LastError = null;

            try
            {
                if (_loader == null)
                {
                    _loader = RuntimeConfigLoader.CreateForStreamingAssets();
                }

                ApplicationRuntimeSettings loadedSettings;
                string error;
                if (!_loader.TryLoadJson(
                        _configRelativePath,
                        out loadedSettings,
                        out error))
                {
                    ReportFailure("读取运行设置失败：" + error);
                    return;
                }

                if (!RuntimeSettingsValidator.TryValidate(
                        loadedSettings,
                        QualitySettings.names.Length,
                        out error))
                {
                    ReportFailure("运行设置校验失败：" + error);
                    return;
                }

                CurrentSettings = loadedSettings;
                IsLoaded = true;

                if (Application.isEditor)
                {
                    ReportSuccess("运行设置读取成功；Editor 中未应用。");
                    return;
                }

                if (!RuntimeSettingsApplier.TryApply(loadedSettings, out error))
                {
                    ReportFailure("应用运行设置失败：" + error);
                    return;
                }

                IsApplied = true;

                if (!WindowsWindowController.IsSupported
                    || !loadedSettings.window.enabled
                    || loadedSettings.window.fullscreen)
                {
                    ReportSuccess("运行设置已应用。");
                    return;
                }

                _applyWindowsWindowRoutine =
                    StartCoroutine(ApplyWindowsWindowAfterResolution(loadedSettings.window));
            }
            catch (Exception exception)
            {
                ReportFailure("运行设置处理失败：" + exception.Message);
            }
        }

        private IEnumerator ApplyWindowsWindowAfterResolution(
            WindowRuntimeSettings windowSettings)
        {
            // Screen.SetResolution 不是立即完成的，等待两帧后再修改原生窗口。
            yield return null;
            yield return null;

            string error;
            if (!_windowsWindowController.TryApply(windowSettings, out error))
            {
                IsApplied = false;
                ReportFailure("应用 Windows 窗口设置失败：" + error);
            }
            else
            {
                IsApplied = true;
                ReportSuccess("运行设置已应用，包括 Windows 窗口设置。");
            }

            _applyWindowsWindowRoutine = null;
        }

        private void StopPendingWindowsWindowApply()
        {
            if (_applyWindowsWindowRoutine == null)
            {
                return;
            }

            StopCoroutine(_applyWindowsWindowRoutine);
            _applyWindowsWindowRoutine = null;
        }

        private void ReportFailure(string error)
        {
            LastError = error;
            Debug.LogError("SYAD RuntimeSettings：" + error, this);
        }

        private void ReportSuccess(string message)
        {
            LastError = null;
            if (_logSuccess)
            {
                Debug.Log("SYAD RuntimeSettings：" + message, this);
            }
        }
    }
}
