using System;

namespace Syad.UnityKit.RuntimeSettings
{
    /// <summary>
    /// 程序启动时可以从 JSON 读取并应用的通用运行设置。
    /// 具体项目仍可以在此基础上定义自己的网络、设备等配置类型。
    /// </summary>
    [Serializable]
    public sealed class ApplicationRuntimeSettings
    {
        public WindowRuntimeSettings window = new WindowRuntimeSettings();
        public CursorRuntimeSettings cursor = new CursorRuntimeSettings();

        /// <summary>-1 表示保持当前画质，其他值对应 QualitySettings 中的画质索引。</summary>
        public int qualityLevel = -1;
    }

    /// <summary>窗口尺寸以及 Windows Player 的窗口属性。</summary>
    [Serializable]
    public sealed class WindowRuntimeSettings
    {
        /// <summary>为 false 时不修改分辨率或窗口属性。</summary>
        public bool enabled = true;
        public int x;
        public int y;
        public int width = 1920;
        public int height = 1080;
        public bool fullscreen;

        /// <summary>以下属性只在 Windows Player 的窗口模式下生效。</summary>
        public bool borderless;
        public bool alwaysOnTop;
        public bool bringToFrontOnce;
    }

    /// <summary>Unity 光标相关设置。</summary>
    [Serializable]
    public sealed class CursorRuntimeSettings
    {
        public bool hidden;
        public bool confineToWindow;
    }
}
