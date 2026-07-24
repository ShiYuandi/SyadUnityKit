using System;
using NUnit.Framework;
using Syad.UnityKit.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Syad.UnityKit.Tests
{
    public sealed class UIServiceTests
    {
        private GameObject _rootObject;
        private GameObject _screenPrefabObject;
        private GameObject _popupPrefabObject;
        private UIViewCatalog _catalog;
        private UIService _service;
        private UIRoot _root;
        private RectTransform _screenLayer;
        private RectTransform _popupLayer;

        [TearDown]
        public void TearDown()
        {
            if (_service != null)
            {
                _service.Dispose();
            }

            DestroyImmediate(_rootObject);
            DestroyImmediate(_screenPrefabObject);
            DestroyImmediate(_popupPrefabObject);

            if (_catalog != null)
            {
                Object.DestroyImmediate(_catalog);
            }
        }

        [Test]
        public void ActivePrefab_ShowBindsBeforeEnableAndSetsVisibleStateFirst()
        {
            BuildService(
                new UIViewEntry(CreateScreenPrefab(), UILayer.Screen));

            TestScreenView view = _service.Show<TestScreenView>(candidate => candidate.SetData("result"));

            Assert.That(view.Value, Is.EqualTo("result"));
            Assert.That(view.CreatedCount, Is.EqualTo(1));
            Assert.That(view.ShownCount, Is.EqualTo(1));
            Assert.That(view.EnabledCount, Is.EqualTo(1));
            Assert.That(view.EnabledBeforeCreated, Is.False);
            Assert.That(view.ValueDuringEnable, Is.EqualTo("result"));
            Assert.That(view.IsVisibleDuringEnable, Is.True);
            Assert.That(view.IsVisible, Is.True);
            Assert.That(view.transform.parent, Is.SameAs(_screenLayer));
        }

        [Test]
        public void SingleInstance_ReusesHiddenCachedView()
        {
            BuildService(
                new UIViewEntry(
                    CreateScreenPrefab(),
                    UILayer.Screen,
                    UICachePolicy.KeepAlive,
                    UIDuplicatePolicy.SingleInstance));

            TestScreenView first = _service.Show<TestScreenView>();
            Assert.That(_service.Hide(first), Is.True);
            TestScreenView second = _service.Show<TestScreenView>();

            Assert.That(second, Is.SameAs(first));
            Assert.That(second.CreatedCount, Is.EqualTo(1));
            Assert.That(second.ShownCount, Is.EqualTo(2));
            Assert.That(second.HiddenCount, Is.EqualTo(1));
            Assert.That(second.WasActiveDuringHidden, Is.True);
            Assert.That(second.EnabledCount, Is.EqualTo(2));
        }

        [Test]
        public void ReleaseOnHide_UnregistersAndReleasesView()
        {
            BuildService(
                new UIViewEntry(
                    CreateScreenPrefab(),
                    UILayer.Screen,
                    UICachePolicy.ReleaseOnHide));

            TestScreenView view = _service.Show<TestScreenView>();
            Assert.That(_service.Hide(view), Is.True);

            TestScreenView tracked;
            Assert.That(view.IsReleased, Is.True);
            Assert.That(view.HiddenCount, Is.EqualTo(1));
            Assert.That(view.ReleasedCount, Is.EqualTo(1));
            Assert.That(_service.TryGet(out tracked), Is.False);
        }

        [Test]
        public void AllowMultipleKeepAlive_ReusesHiddenViewBeforeCreatingAnother()
        {
            BuildService(
                new UIViewEntry(
                    CreatePopupPrefab(),
                    UILayer.Popup,
                    UICachePolicy.KeepAlive,
                    UIDuplicatePolicy.AllowMultiple));

            TestPopupView first = _service.Show<TestPopupView>();
            TestPopupView second = _service.Show<TestPopupView>();

            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(first.transform.parent, Is.SameAs(_popupLayer));
            Assert.That(second.transform.parent, Is.SameAs(_popupLayer));
            Assert.That(_service.Hide(first), Is.True);

            TestPopupView reused = _service.Show<TestPopupView>();

            Assert.That(reused, Is.SameAs(first));
            Assert.That(_popupLayer.childCount, Is.EqualTo(2));
            Assert.That(_service.HideAll<TestPopupView>(), Is.EqualTo(2));

            _service.Dispose();
            Assert.That(first.ReleasedCount, Is.EqualTo(1));
            Assert.That(second.ReleasedCount, Is.EqualTo(1));
        }

        [Test]
        public void CachedView_IdempotentSubscriptionInvokesHandlerOnce()
        {
            BuildService(
                new UIViewEntry(
                    CreateScreenPrefab(),
                    UILayer.Screen,
                    UICachePolicy.KeepAlive,
                    UIDuplicatePolicy.SingleInstance));

            int invocationCount = 0;
            Action handler = () => invocationCount++;

            TestScreenView first = _service.Show<TestScreenView>(view => view.SetData("first"));
            first.ConfirmRequested -= handler;
            first.ConfirmRequested += handler;
            _service.Hide(first);

            TestScreenView second = _service.Show<TestScreenView>(view => view.SetData("second"));
            second.ConfirmRequested -= handler;
            second.ConfirmRequested += handler;
            second.RaiseConfirmRequested();

            Assert.That(second, Is.SameAs(first));
            Assert.That(invocationCount, Is.EqualTo(1));
        }

        private void BuildService(params UIViewEntry[] entries)
        {
            _rootObject = new GameObject("UIRoot", typeof(RectTransform), typeof(UIRoot));
            _root = _rootObject.GetComponent<UIRoot>();

            RectTransform background = CreateLayer("Background");
            _screenLayer = CreateLayer("Screen");
            _popupLayer = CreateLayer("Popup");
            RectTransform overlay = CreateLayer("Overlay");
            _root.Configure(background, _screenLayer, _popupLayer, overlay);

            _catalog = UIViewCatalog.CreateRuntime(entries);
            _service = new UIService(_root, _catalog);
        }

        private RectTransform CreateLayer(string layerName)
        {
            GameObject layerObject = new GameObject(layerName, typeof(RectTransform));
            RectTransform layer = layerObject.GetComponent<RectTransform>();
            layer.SetParent(_rootObject.transform, false);
            return layer;
        }

        private TestScreenView CreateScreenPrefab()
        {
            _screenPrefabObject = new GameObject("TestScreenView", typeof(RectTransform), typeof(TestScreenView));
            return _screenPrefabObject.GetComponent<TestScreenView>();
        }

        private TestPopupView CreatePopupPrefab()
        {
            _popupPrefabObject = new GameObject("TestPopupView", typeof(RectTransform), typeof(TestPopupView));
            return _popupPrefabObject.GetComponent<TestPopupView>();
        }

        private static void DestroyImmediate(GameObject target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
