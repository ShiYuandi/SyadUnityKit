using System;
using UnityEngine;

namespace Syad.UnityKit.UI
{
    /// <summary>
    /// 显式提供 UIService 使用的层级结构，不会在场景中自动查找 Canvas。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIRoot : MonoBehaviour
    {
        [SerializeField] private RectTransform _backgroundLayer;
        [SerializeField] private RectTransform _screenLayer;
        [SerializeField] private RectTransform _popupLayer;
        [SerializeField] private RectTransform _overlayLayer;

        public bool IsConfigured
        {
            get
            {
                return _backgroundLayer != null
                    && _screenLayer != null
                    && _popupLayer != null
                    && _overlayLayer != null;
            }
        }

        /// <summary>
        /// 设置所有 UI 层级根节点，适用于显式组合入口和测试代码。
        /// </summary>
        public void Configure(
            RectTransform backgroundLayer,
            RectTransform screenLayer,
            RectTransform popupLayer,
            RectTransform overlayLayer)
        {
            _backgroundLayer = RequireLayer(backgroundLayer, nameof(backgroundLayer));
            _screenLayer = RequireLayer(screenLayer, nameof(screenLayer));
            _popupLayer = RequireLayer(popupLayer, nameof(popupLayer));
            _overlayLayer = RequireLayer(overlayLayer, nameof(overlayLayer));
        }

        public RectTransform GetLayer(UILayer layer)
        {
            RectTransform result;

            switch (layer)
            {
                case UILayer.Background:
                    result = _backgroundLayer;
                    break;
                case UILayer.Screen:
                    result = _screenLayer;
                    break;
                case UILayer.Popup:
                    result = _popupLayer;
                    break;
                case UILayer.Overlay:
                    result = _overlayLayer;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(layer), layer, "未知的 UI 层级。");
            }

            if (result == null)
            {
                throw new InvalidOperationException(
                    "UIRoot 缺少 " + layer + " 层级。请在创建 UIService 前配置全部层级引用。");
            }

            return result;
        }

        private static RectTransform RequireLayer(RectTransform layer, string parameterName)
        {
            if (layer == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return layer;
        }
    }
}
