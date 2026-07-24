using System;
using Syad.UnityKit.UI;

namespace Syad.UnityKit.Tests
{
    public sealed class TestScreenView : UIView
    {
        public int CreatedCount { get; private set; }
        public int ShownCount { get; private set; }
        public int HiddenCount { get; private set; }
        public int ReleasedCount { get; private set; }
        public int EnabledCount { get; private set; }
        public string Value { get; private set; }
        public string ValueDuringEnable { get; private set; }
        public bool IsVisibleDuringEnable { get; private set; }
        public bool EnabledBeforeCreated { get; private set; }
        public bool WasActiveDuringHidden { get; private set; }

        public event Action ConfirmRequested;

        public void SetData(string value)
        {
            Value = value;
        }

        public void RaiseConfirmRequested()
        {
            if (ConfirmRequested != null)
            {
                ConfirmRequested();
            }
        }

        private void OnEnable()
        {
            EnabledCount++;
            ValueDuringEnable = Value;
            IsVisibleDuringEnable = IsVisible;

            if (!IsCreated)
            {
                EnabledBeforeCreated = true;
            }
        }

        protected override void OnCreated()
        {
            CreatedCount++;
        }

        protected override void OnShown()
        {
            ShownCount++;
        }

        protected override void OnHidden()
        {
            HiddenCount++;
            WasActiveDuringHidden = gameObject.activeInHierarchy;
        }

        protected override void OnReleased()
        {
            ReleasedCount++;
            ConfirmRequested = null;
        }
    }

    public sealed class TestPopupView : UIView
    {
        public int ReleasedCount { get; private set; }

        protected override void OnReleased()
        {
            ReleasedCount++;
        }
    }
}
