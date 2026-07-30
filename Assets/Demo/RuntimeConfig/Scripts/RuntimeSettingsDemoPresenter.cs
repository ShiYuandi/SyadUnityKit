using Syad.UnityKit.RuntimeSettings;
using UnityEngine;

/// <summary>
/// 基础 Demo 的展示层，只读取 RuntimeSettingsBootstrap 状态并绘制说明。
/// 配置加载、应用和重新加载按键全部由 Bootstrap 负责。
/// </summary>
[RequireComponent(typeof(RuntimeSettingsBootstrap))]
public sealed class RuntimeSettingsDemoPresenter : MonoBehaviour
{
    private RuntimeSettingsBootstrap _bootstrap;

    private GUIStyle _titleStyle;
    private GUIStyle _textStyle;
    private GUIStyle _statusStyle;

    private void Awake()
    {
        _bootstrap = GetComponent<RuntimeSettingsBootstrap>();
    }

    private void OnGUI()
    {
        EnsureGuiStyles();

        const float width = 720f;
        const float height = 500f;
        Rect panelRect = new Rect(24f, 24f, width, height);
        GUI.Box(panelRect, GUIContent.none);

        float x = panelRect.x + 24f;
        float y = panelRect.y + 18f;
        float contentWidth = panelRect.width - 48f;
        string fullPath = Application.streamingAssetsPath
            + "/"
            + _bootstrap.ConfigRelativePath;
        string status = GetStatusText();
        ApplicationRuntimeSettings settings = _bootstrap.CurrentSettings;

        GUI.Label(new Rect(x, y, contentWidth, 36f), "SYAD RuntimeSettings 基础 Demo", _titleStyle);
        y += 48f;

        GUI.Label(new Rect(x, y, contentWidth, 28f), "R：由 Bootstrap 重新读取配置", _textStyle);
        y += 34f;
        GUI.Label(new Rect(x, y, contentWidth, 52f), "文件：" + fullPath, _textStyle);
        y += 56f;
        GUI.Label(new Rect(x, y, contentWidth, 28f), "状态：" + status, _statusStyle);
        y += 42f;

        if (settings == null)
        {
            GUI.Label(new Rect(x, y, contentWidth, 28f), "当前没有可显示的配置对象。", _textStyle);
            return;
        }

        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "是否修改窗口：" + settings.window.enabled,
            _textStyle);
        y += 30f;
        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "窗口位置：(" + settings.window.x + ", " + settings.window.y + ")"
            + "    大小：" + settings.window.width + " × " + settings.window.height,
            _textStyle);
        y += 30f;
        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "全屏：" + settings.window.fullscreen
            + "    无边框：" + settings.window.borderless
            + "    置顶：" + settings.window.alwaysOnTop,
            _textStyle);
        y += 30f;
        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "隐藏鼠标：" + settings.cursor.hidden
            + "    限制在窗口内：" + settings.cursor.confineToWindow,
            _textStyle);
        y += 30f;
        GUI.Label(
            new Rect(x, y, contentWidth, 28f),
            "画质索引：" + settings.qualityLevel + "（-1 表示保持当前画质）",
            _textStyle);
    }

    private string GetStatusText()
    {
        if (!string.IsNullOrEmpty(_bootstrap.LastError))
        {
            return "失败：" + _bootstrap.LastError;
        }

        if (_bootstrap.IsApplied)
        {
            return "配置已应用";
        }

        if (_bootstrap.IsLoaded)
        {
            return Application.isEditor
                ? "加载成功；Editor 中未应用"
                : "加载成功；等待应用完成";
        }

        return "尚未成功加载";
    }

    private void EnsureGuiStyles()
    {
        if (_titleStyle != null)
        {
            return;
        }

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold
        };

        _textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            wordWrap = true
        };

        _statusStyle = new GUIStyle(_textStyle)
        {
            fontStyle = FontStyle.Bold
        };
    }
}
