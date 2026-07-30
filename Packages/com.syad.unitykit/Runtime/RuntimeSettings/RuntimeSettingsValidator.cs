namespace Syad.UnityKit.RuntimeSettings
{
    /// <summary>在运行设置影响 Unity 或操作系统之前检查配置值。</summary>
    public static class RuntimeSettingsValidator
    {
        /// <summary>
        /// 校验设置对象。qualityLevelCount 应传入 QualitySettings.names.Length。
        /// </summary>
        public static bool TryValidate(
            ApplicationRuntimeSettings settings,
            int qualityLevelCount,
            out string error)
        {
            if (settings == null)
            {
                error = "运行设置对象不能为空。";
                return false;
            }

            if (settings.window == null)
            {
                error = "运行设置缺少 window 对象。";
                return false;
            }

            if (settings.cursor == null)
            {
                error = "运行设置缺少 cursor 对象。";
                return false;
            }

            if (settings.window.enabled
                && (settings.window.width <= 0 || settings.window.height <= 0))
            {
                error = "窗口宽度和高度必须大于 0。";
                return false;
            }

            if (settings.qualityLevel < -1)
            {
                error = "qualityLevel 不能小于 -1。";
                return false;
            }

            if (settings.qualityLevel >= 0
                && settings.qualityLevel >= qualityLevelCount)
            {
                error = "qualityLevel 超出当前项目的画质等级范围："
                    + settings.qualityLevel;
                return false;
            }

            error = null;
            return true;
        }
    }
}
