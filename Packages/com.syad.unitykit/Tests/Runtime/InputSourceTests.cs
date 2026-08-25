using NUnit.Framework;
using Syad.UnityKit.Input;
using UnityEngine;

namespace Syad.UnityKit.Tests
{
    public sealed class InputSourceTests
    {
        [Test]
        public void RaiseCommand_NotifiesSubscriberWithStronglyTypedCommand()
        {
            GameObject gameObject = new GameObject("InputSourceTests");
            TestInputSource source = gameObject.AddComponent<TestInputSource>();
            TestInputCommand receivedCommand = TestInputCommand.Left;
            int receivedCount = 0;

            source.CommandDetected += command =>
            {
                receivedCommand = command;
                receivedCount++;
            };

            source.Emit(TestInputCommand.Confirm);

            Assert.That(receivedCount, Is.EqualTo(1));
            Assert.That(receivedCommand, Is.EqualTo(TestInputCommand.Confirm));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void RaiseCommand_WithoutSubscriberDoesNotThrow()
        {
            GameObject gameObject = new GameObject("InputSourceTests");
            TestInputSource source = gameObject.AddComponent<TestInputSource>();

            Assert.DoesNotThrow(() => source.Emit(TestInputCommand.Left));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Destroy_ClearsCommandSubscribers()
        {
            GameObject gameObject = new GameObject("InputSourceTests");
            TestInputSource source = gameObject.AddComponent<TestInputSource>();
            int receivedCount = 0;
            source.CommandDetected += command => receivedCount++;

            Object.DestroyImmediate(gameObject);
            source.Emit(TestInputCommand.Left);

            Assert.That(receivedCount, Is.EqualTo(0));
        }
    }

    public enum TestInputCommand
    {
        Left,
        Confirm
    }

    public sealed class TestInputSource : InputSource<TestInputCommand>
    {
        public void Emit(TestInputCommand command)
        {
            RaiseCommand(command);
        }
    }
}
