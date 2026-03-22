using NUnit.Framework;
using System.Collections.Generic;
using IsoRPG.Core;

namespace IsoRPG.Tests
{
    public class StateMachineTests
    {
        private class TestContext
        {
            public List<string> Log = new();
        }

        private class LogState : IState<TestContext>
        {
            private readonly string _name;
            public LogState(string name) => _name = name;

            public void Enter(TestContext ctx, IStateMachine<TestContext> machine) =>
                ctx.Log.Add($"{_name}:Enter");

            public void Execute(TestContext ctx, IStateMachine<TestContext> machine) =>
                ctx.Log.Add($"{_name}:Execute");

            public void Exit(TestContext ctx) =>
                ctx.Log.Add($"{_name}:Exit");
        }

        private class TransitionOnEnterState : IState<TestContext>
        {
            private readonly IState<TestContext> _target;
            public TransitionOnEnterState(IState<TestContext> target) => _target = target;

            public void Enter(TestContext ctx, IStateMachine<TestContext> machine)
            {
                ctx.Log.Add("TransitionOnEnter:Enter");
                machine.ChangeState(_target); // transition during Enter
            }

            public void Execute(TestContext ctx, IStateMachine<TestContext> machine) =>
                ctx.Log.Add("TransitionOnEnter:Execute");

            public void Exit(TestContext ctx) =>
                ctx.Log.Add("TransitionOnEnter:Exit");
        }

        [Test]
        public void ChangeState_CallsEnterOnNew()
        {
            var ctx = new TestContext();
            var sm = new StateMachine<TestContext>(ctx);
            var state = new LogState("A");

            sm.ChangeState(state);

            Assert.AreEqual(1, ctx.Log.Count);
            Assert.AreEqual("A:Enter", ctx.Log[0]);
        }

        [Test]
        public void ChangeState_CallsExitOnOld_ThenEnterOnNew()
        {
            var ctx = new TestContext();
            var sm = new StateMachine<TestContext>(ctx);
            sm.ChangeState(new LogState("A"));
            ctx.Log.Clear();

            sm.ChangeState(new LogState("B"));

            Assert.AreEqual(2, ctx.Log.Count);
            Assert.AreEqual("A:Exit", ctx.Log[0]);
            Assert.AreEqual("B:Enter", ctx.Log[1]);
        }

        [Test]
        public void Update_CallsExecuteOnCurrentState()
        {
            var ctx = new TestContext();
            var sm = new StateMachine<TestContext>(ctx);
            sm.ChangeState(new LogState("A"));
            ctx.Log.Clear();

            sm.Update();

            Assert.AreEqual(1, ctx.Log.Count);
            Assert.AreEqual("A:Execute", ctx.Log[0]);
        }

        [Test]
        public void Update_WithNoState_DoesNotThrow()
        {
            var ctx = new TestContext();
            var sm = new StateMachine<TestContext>(ctx);
            Assert.DoesNotThrow(() => sm.Update());
        }

        [Test]
        public void ChangeState_DuringEnter_DefersTransition()
        {
            var ctx = new TestContext();
            var sm = new StateMachine<TestContext>(ctx);
            var finalState = new LogState("Final");
            var transState = new TransitionOnEnterState(finalState);

            sm.ChangeState(transState);

            // Expected order:
            // 1. TransitionOnEnter:Enter (which calls ChangeState to Final)
            // 2. TransitionOnEnter:Exit (deferred transition executes)
            // 3. Final:Enter
            Assert.AreEqual(3, ctx.Log.Count);
            Assert.AreEqual("TransitionOnEnter:Enter", ctx.Log[0]);
            Assert.AreEqual("TransitionOnEnter:Exit", ctx.Log[1]);
            Assert.AreEqual("Final:Enter", ctx.Log[2]);
            Assert.AreSame(finalState, sm.CurrentState);
        }

        [Test]
        public void ChangeState_Null_DoesNothing()
        {
            var ctx = new TestContext();
            var sm = new StateMachine<TestContext>(ctx);
            sm.ChangeState(new LogState("A"));
            ctx.Log.Clear();

            sm.ChangeState(null);

            Assert.AreEqual(0, ctx.Log.Count);
        }

        [Test]
        public void CurrentState_ReflectsLatestTransition()
        {
            var ctx = new TestContext();
            var sm = new StateMachine<TestContext>(ctx);
            var stateA = new LogState("A");
            var stateB = new LogState("B");

            Assert.IsNull(sm.CurrentState);
            sm.ChangeState(stateA);
            Assert.AreSame(stateA, sm.CurrentState);
            sm.ChangeState(stateB);
            Assert.AreSame(stateB, sm.CurrentState);
        }
    }
}
