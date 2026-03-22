using NUnit.Framework;
using IsoRPG.Core;

namespace IsoRPG.Tests
{
    public class GameEventTests
    {
        [Test]
        public void Subscribe_And_Raise_InvokesListener()
        {
            var evt = new GameEvent<int>();
            int received = -1;
            evt.Subscribe(v => received = v);

            evt.Raise(42);

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Raise_WithNoListeners_DoesNotThrow()
        {
            var evt = new GameEvent<int>();
            Assert.DoesNotThrow(() => evt.Raise(42));
        }

        [Test]
        public void Unsubscribe_PreventsCallback()
        {
            var evt = new GameEvent<int>();
            int callCount = 0;
            void Handler(int _) => callCount++;

            evt.Subscribe(Handler);
            evt.Raise(1);
            Assert.AreEqual(1, callCount);

            evt.Unsubscribe(Handler);
            evt.Raise(2);
            Assert.AreEqual(1, callCount); // no additional call
        }

        [Test]
        public void MultipleListeners_AllInvoked()
        {
            var evt = new GameEvent<int>();
            int count = 0;
            // Use distinct lambdas (each is a unique delegate instance)
            System.Action<int> a = _ => count++;
            System.Action<int> b = _ => count++;
            System.Action<int> c = _ => count++;
            evt.Subscribe(a);
            evt.Subscribe(b);
            evt.Subscribe(c);

            evt.Raise(1);

            Assert.AreEqual(3, count);
        }

        [Test]
        public void DuplicateSubscribe_Rejected()
        {
            var evt = new GameEvent<int>();
            int count = 0;
            void Handler(int _) => count++;

            evt.Subscribe(Handler);
            evt.Subscribe(Handler); // duplicate — should be ignored

            evt.Raise(1);

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, evt.ListenerCount);
        }

        [Test]
        public void ListenerCount_TracksCorrectly()
        {
            var evt = new GameEvent<int>();
            Assert.AreEqual(0, evt.ListenerCount);

            void Handler(int _) { }
            evt.Subscribe(Handler);
            Assert.AreEqual(1, evt.ListenerCount);

            evt.Unsubscribe(Handler);
            Assert.AreEqual(0, evt.ListenerCount);
        }

        [Test]
        public void Clear_RemovesAllListeners()
        {
            var evt = new GameEvent<int>();
            int count = 0;
            evt.Subscribe(_ => count++);
            evt.Subscribe(_ => count++);

            evt.Clear();
            evt.Raise(1);

            Assert.AreEqual(0, count);
            Assert.AreEqual(0, evt.ListenerCount);
        }

        [Test]
        public void ExceptionInListener_DoesNotBreakOthers()
        {
            var evt = new GameEvent<int>();
            int count = 0;
            evt.Subscribe(_ => throw new System.Exception("bad listener"));
            evt.Subscribe(_ => count++);

            // Should not throw — exception is caught and logged
            Assert.DoesNotThrow(() => evt.Raise(1));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void ParameterlessEvent_Works()
        {
            var evt = new GameEvent();
            int count = 0;
            evt.Subscribe(() => count++);

            evt.Raise();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Subscribe_NullListener_DoesNotThrow()
        {
            var evt = new GameEvent<int>();
            Assert.DoesNotThrow(() => evt.Subscribe(null));
            Assert.AreEqual(0, evt.ListenerCount);
        }
    }
}
