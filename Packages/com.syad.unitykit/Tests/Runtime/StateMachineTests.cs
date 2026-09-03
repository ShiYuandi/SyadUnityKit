using System;
using System.Collections.Generic;
using NUnit.Framework;
using Syad.UnityKit.StateMachine;

namespace Syad.UnityKit.Tests
{
    public sealed class StateMachineTests
    {
        [Test]
        public void Initialize_SetsCurrentStateAndCallsEnterOnce()
        {
            StateMachine<TestState> stateMachine = new StateMachine<TestState>();
            TestState state = new TestState("初始");

            stateMachine.Initialize(state);

            Assert.That(stateMachine.CurrentState, Is.SameAs(state));
            Assert.That(state.EnterCount, Is.EqualTo(1));
            Assert.That(state.UpdateCount, Is.EqualTo(0));
            Assert.That(state.ExitCount, Is.EqualTo(0));
        }

        [Test]
        public void Initialize_WhenAlreadyRunningThrows()
        {
            StateMachine<TestState> stateMachine = new StateMachine<TestState>();
            TestState firstState = new TestState("第一个");
            TestState secondState = new TestState("第二个");
            stateMachine.Initialize(firstState);

            Assert.Throws<InvalidOperationException>(
                () => stateMachine.Initialize(secondState));

            Assert.That(stateMachine.CurrentState, Is.SameAs(firstState));
            Assert.That(secondState.EnterCount, Is.EqualTo(0));
        }

        [Test]
        public void Update_ForwardsOnlyToCurrentState()
        {
            StateMachine<TestState> stateMachine = new StateMachine<TestState>();
            TestState firstState = new TestState("第一个");
            TestState secondState = new TestState("第二个");
            stateMachine.Initialize(firstState);

            stateMachine.Update();
            stateMachine.ChangeState(secondState);
            stateMachine.Update();
            stateMachine.Update();

            Assert.That(firstState.UpdateCount, Is.EqualTo(1));
            Assert.That(secondState.UpdateCount, Is.EqualTo(2));
        }

        [Test]
        public void ChangeState_UsesExitAssignEnterOrder()
        {
            List<string> calls = new List<string>();
            StateMachine<TestState> stateMachine = new StateMachine<TestState>();
            TestState firstState = new TestState("旧状态", calls);
            TestState secondState = new TestState("新状态", calls);
            stateMachine.Initialize(firstState);
            calls.Clear();

            stateMachine.ChangeState(secondState);

            CollectionAssert.AreEqual(
                new[] { "旧状态.Exit", "新状态.Enter" },
                calls);
            Assert.That(stateMachine.CurrentState, Is.SameAs(secondState));
        }

        [Test]
        public void ChangeState_ToSameInstanceExitsAndEntersAgain()
        {
            StateMachine<TestState> stateMachine = new StateMachine<TestState>();
            TestState state = new TestState("相同状态");
            stateMachine.Initialize(state);

            stateMachine.ChangeState(state);

            Assert.That(state.EnterCount, Is.EqualTo(2));
            Assert.That(state.ExitCount, Is.EqualTo(1));
            Assert.That(stateMachine.CurrentState, Is.SameAs(state));
        }

        [Test]
        public void Stop_ExitsAndClearsCurrentStateAndCanRepeat()
        {
            StateMachine<TestState> stateMachine = new StateMachine<TestState>();
            TestState state = new TestState("停止状态");
            stateMachine.Initialize(state);

            stateMachine.Stop();
            Assert.DoesNotThrow(() => stateMachine.Stop());

            Assert.That(state.ExitCount, Is.EqualTo(1));
            Assert.That(stateMachine.CurrentState, Is.Null);
        }

        [Test]
        public void EmptyStateMachine_UpdateAndStopDoNotThrow()
        {
            StateMachine<TestState> stateMachine = new StateMachine<TestState>();

            Assert.DoesNotThrow(() => stateMachine.Update());
            Assert.DoesNotThrow(() => stateMachine.Stop());
        }

        [Test]
        public void InvalidStateArgumentsAndOrderThrowClearExceptions()
        {
            StateMachine<TestState> stateMachine = new StateMachine<TestState>();

            Assert.Throws<ArgumentNullException>(() => stateMachine.Initialize(null));
            Assert.Throws<ArgumentNullException>(() => stateMachine.ChangeState(null));
            Assert.Throws<InvalidOperationException>(
                () => stateMachine.ChangeState(new TestState("未初始化")));

            TestState state = new TestState("重新启动");
            stateMachine.Initialize(state);
            stateMachine.Stop();

            Assert.DoesNotThrow(() => stateMachine.Initialize(state));
            Assert.That(state.EnterCount, Is.EqualTo(2));
        }

        private sealed class TestState : IState
        {
            private readonly string _name;
            private readonly List<string> _calls;

            public int EnterCount { get; private set; }
            public int UpdateCount { get; private set; }
            public int ExitCount { get; private set; }

            public TestState(string name, List<string> calls = null)
            {
                _name = name;
                _calls = calls;
            }

            public void Enter()
            {
                EnterCount++;
                AddCall("Enter");
            }

            public void Update()
            {
                UpdateCount++;
                AddCall("Update");
            }

            public void Exit()
            {
                ExitCount++;
                AddCall("Exit");
            }

            private void AddCall(string lifecycle)
            {
                if (_calls != null)
                {
                    _calls.Add(_name + "." + lifecycle);
                }
            }
        }
    }
}
