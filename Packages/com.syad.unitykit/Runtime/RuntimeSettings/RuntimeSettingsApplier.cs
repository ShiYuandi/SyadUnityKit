using UnityEngine;

namespace Syad.UnityKit.RuntimeSettings
{
    /// <summary>
    /// 应用 Unity 自身提供的分辨率、光标和画质设置。
    /// Windows 原生窗口位置、无边框和置顶由 WindowsWindowController 负责。
    /// </summary>
    public static class RuntimeSettingsApplier
    {
        /// <summary>校验并应用通用运行设置。</summary>
        public static bool TryApply(
            ApplicationRuntimeSettings settings,
            out string error)
        {
            if (!RuntimeSettingsValidator.TryValidate(
                    settings,
                    QualitySettings.names.Length,
                    out error))
            {
                return false;
            }

            if (settings.window.enabled)
            {
                Screen.SetResolution(
                    settings.window.width,
                    settings.window.height,
                    settings.window.fullscreen);
            }

            Cursor.lockState = settings.cursor.confineToWindow
                ? CursorLockMode.Confined
                : CursorLockMode.None;
            Cursor.visible = !settings.cursor.hidden;

            if (settings.qualityLevel >= 0
                && QualitySettings.GetQualityLevel() != settings.qualityLevel)
            {
                QualitySettings.SetQualityLevel(settings.qualityLevel, true);
            }

            error = null;
            return true;
        }
    }
}
