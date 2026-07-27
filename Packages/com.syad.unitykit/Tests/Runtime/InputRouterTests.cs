using System;
using System.Collections.Generic;
using NUnit.Framework;
using Syad.UnityKit.Input;

namespace Syad.UnityKit.Tests
{
    public sealed class InputRouterTests
    {
        private enum TestCommand
        {
            Left,
            Right,
            Confirm
        }

        [Test]
        public void TryDispatch_FirstCommandNotifiesSubscriber()
        {
            ManualTime time = new ManualTime();
            InputRouter<TestCommand> router = new InputRouter<TestCommand>(1f, time.GetTime);
            TestCommand receivedCommand = TestCommand.Left;
            int receivedCount = 0;

            router.CommandReceived += command =>
            {
                receivedCommand = command;
                receivedCount++;
            };

            bool accepted = router.TryDispatch(TestCommand.Confirm);

            Assert.That(accepted, Is.True);
            Assert.That(receivedCount, Is.EqualTo(1));
            Assert.That(receivedCommand, Is.EqualTo(TestCommand.Confirm));
        }

        [Test]
        public void TryDispatch_CooldownBlocksThenAllowsNextCommand()
        {
            ManualTime time = new ManualTime();
            InputRouter<TestCommand> router = new InputRouter<TestCommand>(1f, time.GetTime);
            List<TestCommand> receivedCommands = new List<TestCommand>();
            router.CommandReceived += receivedCommands.Add;

            Assert.That(router.TryDispatch(TestCommand.Left), Is.True);

            time.Current = 0.9f;
            Assert.That(router.TryDispatch(TestCommand.Right), Is.False);

            time.Current = 1f;
            Assert.That(router.TryDispatch(TestCommand.Confirm), Is.True);

            Assert.That(
                receivedCommands,
                Is.EqualTo(new[] { TestCommand.Left, TestCommand.Confirm }));
        }

        [Test]
        public void SetEnabled_DisabledRouterRejectsCommand()
        {
            ManualTime time = new ManualTime();
            InputRouter<TestCommand> router = new InputRouter<TestCommand>(0f, time.GetTime);
            int receivedCount = 0;
            router.CommandReceived += command => receivedCount++;

            router.SetEnabled(false);
            Assert.That(router.TryDispatch(TestCommand.Left), Is.False);

            router.SetEnabled(true);
            Assert.That(router.TryDispatch(TestCommand.Right), Is.True);
            Assert.That(receivedCount, Is.EqualTo(1));
        }

        [Test]
        public void ResetCooldown_AllowsCommandImmediately()
        {
            ManualTime time = new ManualTime();
            InputRouter<TestCommand> router = new InputRouter<TestCommand>(5f, time.GetTime);

            Assert.That(router.TryDispatch(TestCommand.Left), Is.True);

            time.Current = 1f;
            Assert.That(router.TryDispatch(TestCommand.Right), Is.False);

            router.ResetCooldown();
            Assert.That(router.TryDispatch(TestCommand.Right), Is.True);
        }

        [Test]
        public void Dispose_ClearsSubscribersAndPreventsFurtherUse()
        {
            ManualTime time = new ManualTime();
            InputRouter<TestCommand> router = new InputRouter<TestCommand>(0f, time.GetTime);
            int receivedCount = 0;
            router.CommandReceived += command => receivedCount++;

            router.Dispose();
            router.Dispose();

            Assert.That(router.IsDisposed, Is.True);
            Assert.That(router.IsEnabled, Is.False);
            Assert.Throws<ObjectDisposedException>(() => router.TryDispatch(TestCommand.Left));
            Assert.That(receivedCount, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_NegativeCooldownThrows()
        {
            ManualTime time = new ManualTime();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new InputRouter<TestCommand>(-0.1f, time.GetTime));
        }

        private sealed class ManualTime
        {
            public float Current { get; set; }

            public float GetTime()
            {
                return Current;
            }
        }
    }
}
