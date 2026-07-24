using System;
using UnityEngine;

namespace Syad.UnityKit.UI
{
    /// <summary>
    /// 描述具体视图 Prefab 的承载方式，并以 Prefab 上的具体组件类型作为视图标识。
    /// </summary>
    [Serializable]
    public sealed class UIViewEntry
    {
        [SerializeField] private UIView _prefab;
        [SerializeField] private UILayer _layer = UILayer.Screen;
        [SerializeField] private UICachePolicy _cachePolicy = UICachePolicy.KeepAlive;
        [SerializeField] private UIDuplicatePolicy _duplicatePolicy = UIDuplicatePolicy.SingleInstance;

        public UIViewEntry(
            UIView prefab,
            UILayer layer = UILayer.Screen,
            UICachePolicy cachePolicy = UICachePolicy.KeepAlive,
            UIDuplicatePolicy duplicatePolicy = UIDuplicatePolicy.SingleInstance)
        {
            _prefab = prefab;
            _layer = layer;
            _cachePolicy = cachePolicy;
            _duplicatePolicy = duplicatePolicy;
        }

        public UIView Prefab
        {
            get { return _prefab; }
        }

        public Type ViewType
        {
            get { return _prefab == null ? null : _prefab.GetType(); }
        }

        public UILayer Layer
        {
            get { return _layer; }
        }

        public UICachePolicy CachePolicy
        {
            get { return _cachePolicy; }
        }

        public UIDuplicatePolicy DuplicatePolicy
        {
            get { return _duplicatePolicy; }
        }
    }
}
