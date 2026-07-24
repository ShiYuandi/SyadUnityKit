using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Syad.UnityKit.UI
{
    /// <summary>
    /// 创建、显示、缓存和释放目录中登记的视图。所有权由使用者显式管理，本类不是单例。
    /// </summary>
    public sealed class UIService : IDisposable
    {
        private readonly UIRoot _root;
        private readonly UIViewCatalog _catalog;
        private readonly Transform _stagingRoot;
        private readonly Dictionary<Type, List<UIView>> _instances = new Dictionary<Type, List<UIView>>();
        private readonly Dictionary<UIView, UIViewEntry> _registrations = new Dictionary<UIView, UIViewEntry>();

        private bool _isDisposed;

        public UIService(UIRoot root, UIViewCatalog catalog)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (!root.IsConfigured)
            {
                throw new InvalidOperationException("UIRoot 必须配置全部四个 UI 层级。");
            }

            _root = root;
            _catalog = catalog;
            _stagingRoot = CreateStagingRoot(root.transform);
        }

        /// <summary>
        /// 显示视图，并可选择在 OnShown 回调执行前绑定数据。
        /// </summary>
        public TView Show<TView>(Action<TView> bind = null) where TView : UIView
        {
            ThrowIfDisposed();

            UIViewEntry entry;
            if (!_catalog.TryGet<TView>(out entry))
            {
                throw new KeyNotFoundException(
                    "UIViewCatalog 中没有登记 " + typeof(TView).FullName + " 类型的视图。");
            }

            TView view = null;
            if (entry.DuplicatePolicy == UIDuplicatePolicy.SingleInstance)
            {
                TryGet(out view, true);
            }
            else if (entry.CachePolicy == UICachePolicy.KeepAlive)
            {
                TryGetHidden(out view);
            }

            if (view == null)
            {
                view = Create<TView>(entry);
            }

            if (bind != null)
            {
                bind(view);
            }

            view.BeforeShow();
            view.transform.SetAsLastSibling();
            view.gameObject.SetActive(true);
            view.AfterShow();
            return view;
        }

        /// <summary>返回指定具体视图类型中最新被跟踪的实例。</summary>
        public bool TryGet<TView>(out TView view, bool includeHidden = true) where TView : UIView
        {
            ThrowIfDisposed();

            List<UIView> views;
            if (_instances.TryGetValue(typeof(TView), out views))
            {
                for (int i = views.Count - 1; i >= 0; i--)
                {
                    UIView candidate = views[i];
                    if (candidate == null || candidate.IsReleased)
                    {
                        continue;
                    }

                    if (includeHidden || candidate.IsVisible)
                    {
                        view = (TView)candidate;
                        return true;
                    }
                }
            }

            view = null;
            return false;
        }

        /// <summary>隐藏指定具体视图类型中最新的可见实例。</summary>
        public bool Hide<TView>() where TView : UIView
        {
            TView view;
            return TryGet(out view, false) && Hide(view);
        }

        /// <summary>隐藏指定实例；采用 ReleaseOnHide 策略的实例还会被销毁。</summary>
        public bool Hide(UIView view)
        {
            ThrowIfDisposed();

            UIViewEntry entry;
            if (view == null || !_registrations.TryGetValue(view, out entry) || view.IsReleased)
            {
                return false;
            }

            if (!view.BeforeHide())
            {
                return false;
            }

            view.gameObject.SetActive(false);

            if (entry.CachePolicy == UICachePolicy.ReleaseOnHide)
            {
                return ReleaseInternal(view);
            }

            return true;
        }

        /// <summary>隐藏指定具体视图类型的所有可见实例。</summary>
        public int HideAll<TView>() where TView : UIView
        {
            ThrowIfDisposed();

            List<UIView> snapshot = GetSnapshot(typeof(TView));
            int hiddenCount = 0;

            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                UIView view = snapshot[i];
                if (view != null && view.IsVisible && Hide(view))
                {
                    hiddenCount++;
                }
            }

            return hiddenCount;
        }

        /// <summary>忽略缓存策略，直接释放指定实例。</summary>
        public bool Release(UIView view)
        {
            ThrowIfDisposed();
            return ReleaseInternal(view);
        }

        /// <summary>释放当前服务持有的全部视图实例。</summary>
        public void ReleaseAll()
        {
            ThrowIfDisposed();
            ReleaseAllInternal();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            ReleaseAllInternal();

            if (_stagingRoot != null)
            {
                Object.Destroy(_stagingRoot.gameObject);
            }

            _isDisposed = true;
        }

        private TView Create<TView>(UIViewEntry entry) where TView : UIView
        {
            RectTransform parent = _root.GetLayer(entry.Layer);
            UIView instance = Object.Instantiate(entry.Prefab, _stagingRoot, false);

            if (!(instance is TView))
            {
                Object.Destroy(instance.gameObject);
                throw new InvalidOperationException(
                    "目录中的 Prefab 类型 " + instance.GetType().FullName
                    + " 与请求的视图类型 " + typeof(TView).FullName + " 不一致。");
            }

            instance.name = entry.Prefab.name;
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(parent, false);

            List<UIView> views;
            if (!_instances.TryGetValue(typeof(TView), out views))
            {
                views = new List<UIView>();
                _instances.Add(typeof(TView), views);
            }

            views.Add(instance);
            _registrations.Add(instance, entry);

            try
            {
                instance.Create();
            }
            catch
            {
                RemoveRegistration(instance);
                Object.Destroy(instance.gameObject);
                throw;
            }

            return (TView)instance;
        }

        private bool ReleaseInternal(UIView view)
        {
            if (view == null || !_registrations.ContainsKey(view))
            {
                return false;
            }

            if (view.IsVisible)
            {
                view.BeforeHide();
                view.gameObject.SetActive(false);
            }

            view.Release();
            RemoveRegistration(view);
            Object.Destroy(view.gameObject);
            return true;
        }

        private void ReleaseAllInternal()
        {
            UIView[] snapshot = new UIView[_registrations.Count];
            _registrations.Keys.CopyTo(snapshot, 0);

            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                ReleaseInternal(snapshot[i]);
            }

            _instances.Clear();
            _registrations.Clear();
        }

        private void RemoveRegistration(UIView view)
        {
            _registrations.Remove(view);

            Type type = view.GetType();
            List<UIView> views;
            if (!_instances.TryGetValue(type, out views))
            {
                return;
            }

            views.Remove(view);
            if (views.Count == 0)
            {
                _instances.Remove(type);
            }
        }

        private List<UIView> GetSnapshot(Type viewType)
        {
            List<UIView> views;
            return _instances.TryGetValue(viewType, out views)
                ? new List<UIView>(views)
                : new List<UIView>();
        }

        private bool TryGetHidden<TView>(out TView view) where TView : UIView
        {
            List<UIView> views;
            if (_instances.TryGetValue(typeof(TView), out views))
            {
                for (int i = views.Count - 1; i >= 0; i--)
                {
                    UIView candidate = views[i];
                    if (candidate != null && !candidate.IsReleased && !candidate.IsVisible)
                    {
                        view = (TView)candidate;
                        return true;
                    }
                }
            }

            view = null;
            return false;
        }

        private static Transform CreateStagingRoot(Transform owner)
        {
            GameObject stagingObject = new GameObject("[SYAD UI 暂存区]", typeof(RectTransform));
            stagingObject.hideFlags = HideFlags.HideAndDontSave;
            stagingObject.transform.SetParent(owner, false);
            stagingObject.SetActive(false);
            return stagingObject.transform;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(UIService), "UIService 已经释放，不能继续使用。");
            }
        }
    }
}
