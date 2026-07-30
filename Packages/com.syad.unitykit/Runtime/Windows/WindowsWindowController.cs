using System;
using Syad.UnityKit.RuntimeSettings;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Diagnostics;
using System.Runtime.InteropServices;
#endif

namespace Syad.UnityKit.Windows
{
    /// <summary>
    /// 控制 Windows Player 主窗口的位置、边框和置顶状态。
    /// 每个程序入口应显式创建并持有一个实例，不使用全局单例。
    /// </summary>
    public sealed class WindowsWindowController
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int GwlStyle = -16;
        private const long WsCaption = 0x00C00000L;
        private const long WsThickFrame = 0x00040000L;

        private const uint SwpNoActivate = 0x0010;
        private const uint SwpFrameChanged = 0x0020;
        private const uint SwpShowWindow = 0x0040;

        private static readonly IntPtr HwndTopMost = new IntPtr(-1);
        private static readonly IntPtr HwndNoTopMost = new IntPtr(-2);

        private IntPtr _windowHandle;
        private IntPtr _originalStyle;
        private bool _hasOriginalStyle;
#endif

        /// <summary>当前运行环境是否支持 Windows 原生窗口控制。</summary>
        public static bool IsSupported
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 将窗口设置应用到当前 Windows Player。
        /// 应在 Screen.SetResolution 生效后的后续帧调用。
        /// </summary>
        public bool TryApply(WindowRuntimeSettings settings, out string error)
        {
            if (settings == null)
            {
                error = "窗口设置对象不能为空。";
                return false;
            }

            if (!settings.enabled || settings.fullscreen)
            {
                error = null;
                return true;
            }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (!TryFindPlayerWindow(out error))
            {
                return false;
            }

            IntPtr currentStyle;
            if (!TryGetWindowStyle(_windowHandle, out currentStyle, out error))
            {
                return false;
            }

            if (!_hasOriginalStyle)
            {
                _originalStyle = currentStyle;
                _hasOriginalStyle = true;
            }

            long targetStyleValue = settings.borderless
                ? currentStyle.ToInt64() & ~(WsCaption | WsThickFrame)
                : _originalStyle.ToInt64();
            IntPtr targetStyle = new IntPtr(targetStyleValue);

            if (targetStyle != currentStyle
                && !TrySetWindowStyle(_windowHandle, targetStyle, out error))
            {
                return false;
            }

            IntPtr insertAfter = settings.alwaysOnTop
                ? HwndTopMost
                : HwndNoTopMost;
            NativeRect windowRect = new NativeRect
            {
                left = 0,
                top = 0,
                right = settings.width,
                bottom = settings.height
            };
            if (!AdjustWindowRect(
                    ref windowRect,
                    unchecked((uint)targetStyle.ToInt64()),
                    false))
            {
                error = "计算 Windows 窗口外框大小失败，错误码："
                    + Marshal.GetLastWin32Error();
                return false;
            }

            int windowWidth = windowRect.right - windowRect.left;
            int windowHeight = windowRect.bottom - windowRect.top;
            uint flags = SwpShowWindow | SwpFrameChanged;
            if (!settings.bringToFrontOnce)
            {
                flags |= SwpNoActivate;
            }

            if (!SetWindowPos(
                    _windowHandle,
                    insertAfter,
                    settings.x,
                    settings.y,
                    windowWidth,
                    windowHeight,
                    flags))
            {
                error = "设置 Windows 窗口位置或大小失败，错误码："
                    + Marshal.GetLastWin32Error();
                return false;
            }

            if (settings.bringToFrontOnce)
            {
                SetForegroundWindow(_windowHandle);
            }

            error = null;
            return true;
#else
            error = "Windows 原生窗口控制只支持构建后的 Windows Player。";
            return false;
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private bool TryFindPlayerWindow(out string error)
        {
            if (_windowHandle != IntPtr.Zero && IsWindow(_windowHandle))
            {
                error = null;
                return true;
            }

            uint processId = (uint)Process.GetCurrentProcess().Id;
            IntPtr foundWindow = IntPtr.Zero;

            EnumWindows(delegate(IntPtr windowHandle, IntPtr parameter)
            {
                uint windowProcessId;
                GetWindowThreadProcessId(windowHandle, out windowProcessId);
                if (windowProcessId == processId
                    && GetWindow(windowHandle, 4) == IntPtr.Zero
                    && IsWindowVisible(windowHandle))
                {
                    foundWindow = windowHandle;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            _windowHandle = foundWindow;
            if (_windowHandle == IntPtr.Zero)
            {
                error = "没有找到当前 Unity Player 的 Windows 主窗口。";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryGetWindowStyle(
            IntPtr windowHandle,
            out IntPtr style,
            out string error)
        {
            SetLastError(0);
            style = GetWindowLongPtr(windowHandle, GwlStyle);
            int errorCode = Marshal.GetLastWin32Error();
            if (style == IntPtr.Zero && errorCode != 0)
            {
                error = "读取 Windows 窗口样式失败，错误码：" + errorCode;
                return false;
            }

            error = null;
            return true;
        }

        private static bool TrySetWindowStyle(
            IntPtr windowHandle,
            IntPtr style,
            out string error)
        {
            SetLastError(0);
            IntPtr previousStyle = SetWindowLongPtr(windowHandle, GwlStyle, style);
            int errorCode = Marshal.GetLastWin32Error();
            if (previousStyle == IntPtr.Zero && errorCode != 0)
            {
                error = "修改 Windows 窗口样式失败，错误码：" + errorCode;
                return false;
            }

            error = null;
            return true;
        }

        private static IntPtr GetWindowLongPtr(IntPtr windowHandle, int index)
        {
            return IntPtr.Size == 8
                ? GetWindowLongPtr64(windowHandle, index)
                : new IntPtr(GetWindowLong32(windowHandle, index));
        }

        private static IntPtr SetWindowLongPtr(
            IntPtr windowHandle,
            int index,
            IntPtr newValue)
        {
            return IntPtr.Size == 8
                ? SetWindowLongPtr64(windowHandle, index, newValue)
                : new IntPtr(SetWindowLong32(windowHandle, index, newValue.ToInt32()));
        }

        private delegate bool EnumWindowsCallback(IntPtr windowHandle, IntPtr parameter);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr windowHandle,
            out uint processId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr windowHandle, uint command);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr windowHandle);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr windowHandle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr windowHandle,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AdjustWindowRect(
            ref NativeRect rectangle,
            uint style,
            bool hasMenu);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr windowHandle);

        [DllImport("kernel32.dll")]
        private static extern void SetLastError(uint errorCode);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr windowHandle, int index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr windowHandle, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(
            IntPtr windowHandle,
            int index,
            int newValue);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(
            IntPtr windowHandle,
            int index,
            IntPtr newValue);
#endif
    }
}
