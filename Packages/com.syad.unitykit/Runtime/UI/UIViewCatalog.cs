using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syad.UnityKit.UI
{
    /// <summary>
    /// 将具体 UIView 类型映射到 Prefab 及其管理策略的 ScriptableObject 目录。
    /// </summary>
    [CreateAssetMenu(fileName = "UIViewCatalog", menuName = "SYAD Unity Kit/UI/视图目录")]
    public sealed class UIViewCatalog : ScriptableObject
    {
        [SerializeField] private List<UIViewEntry> _entries = new List<UIViewEntry>();

        [NonSerialized] private Dictionary<Type, UIViewEntry> _index;

        public IReadOnlyList<UIViewEntry> Entries
        {
            get { return _entries; }
        }

        public bool TryGet<TView>(out UIViewEntry entry) where TView : UIView
        {
            return TryGet(typeof(TView), out entry);
        }

        public bool TryGet(Type viewType, out UIViewEntry entry)
        {
            if (viewType == null)
            {
                throw new ArgumentNullException(nameof(viewType));
            }

            EnsureIndex();
            return _index.TryGetValue(viewType, out entry);
        }

        /// <summary>
        /// 创建用于代码启动和测试的临时目录。
        /// 正常项目内容应使用通过 Assets 菜单创建的目录资源。
        /// </summary>
        public static UIViewCatalog CreateRuntime(params UIViewEntry[] entries)
        {
            UIViewCatalog catalog = CreateInstance<UIViewCatalog>();
            catalog._entries = entries == null
                ? new List<UIViewEntry>()
                : new List<UIViewEntry>(entries);
            catalog.RebuildIndex();
            return catalog;
        }

        private void OnEnable()
        {
            _index = null;
        }

        private void OnValidate()
        {
            _index = null;
        }

        private void EnsureIndex()
        {
            if (_index == null)
            {
                RebuildIndex();
            }
        }

        private void RebuildIndex()
        {
            _index = new Dictionary<Type, UIViewEntry>();

            for (int i = 0; i < _entries.Count; i++)
            {
                UIViewEntry entry = _entries[i];

                if (entry == null || entry.Prefab == null)
                {
                    throw new InvalidOperationException("UIViewCatalog 的第 " + i + " 项没有设置 Prefab。");
                }

                Type viewType = entry.ViewType;
                if (_index.ContainsKey(viewType))
                {
                    throw new InvalidOperationException(
                        "UIViewCatalog 中存在多个 " + viewType.FullName + " 类型的配置项。");
                }

                _index.Add(viewType, entry);
            }
        }
    }
}
