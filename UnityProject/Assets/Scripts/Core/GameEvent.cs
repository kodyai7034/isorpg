using System;
using UnityEngine;

namespace IsoRPG.Core
{
    /// <summary>
    /// Type-safe event channel for decoupled communication between systems.
    /// Subscribe from any system, raise from any system. No allocation on raise if no listeners.
    ///
    /// Usage:
    /// <code>
    /// // Define in GameEvents:
    /// public static readonly GameEvent&lt;DamageDealtArgs&gt; DamageDealt = new();
    ///
    /// // Subscribe:
    /// GameEvents.DamageDealt.Subscribe(OnDamageDealt);
    ///
    /// // Raise:
    /// GameEvents.DamageDealt.Raise(new DamageDealtArgs(...));
    ///
    /// // Unsubscribe (always do this in OnDestroy):
    /// GameEvents.DamageDealt.Unsubscribe(OnDamageDealt);
    /// </code>
    /// </summary>
    /// <typeparam name="T">Event argument type. Should be a readonly struct.</typeparam>
    public class GameEvent<T>
    {
        private event Action<T> _listeners;

        /// <summary>Number of active subscribers.</summary>
        public int ListenerCount { get; private set; }

        /// <summary>
        /// Subscribe a listener. Safe to call multiple times — duplicates are allowed
        /// (standard C# delegate behavior).
        /// </summary>
        /// <param name="listener">Callback to invoke when event is raised.</param>
        public void Subscribe(Action<T> listener)
        {
            if (listener == null)
                return;

            _listeners += listener;
            ListenerCount++;
        }

        /// <summary>
        /// Unsubscribe a listener. Must match a previous Subscribe call.
        /// Always call this in OnDestroy/OnDisable to prevent dangling references.
        /// </summary>
        /// <param name="listener">The callback to remove.</param>
        public void Unsubscribe(Action<T> listener)
        {
            if (listener == null)
                return;

            _listeners -= listener;
            ListenerCount = Mathf.Max(0, ListenerCount - 1);
        }

        /// <summary>
        /// Raise the event, invoking all subscribers. Catches and logs exceptions
        /// from individual listeners to prevent one bad listener from breaking others.
        /// </summary>
        /// <param name="args">Event arguments.</param>
        public void Raise(T args)
        {
            if (_listeners == null)
                return;

            foreach (var listener in _listeners.GetInvocationList())
            {
                try
                {
                    ((Action<T>)listener).Invoke(args);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>Remove all subscribers. Use with caution — typically only for teardown.</summary>
        public void Clear()
        {
            _listeners = null;
            ListenerCount = 0;
        }
    }

    /// <summary>
    /// Parameterless event channel for simple signals (e.g., BattleStarted, TurnEnded).
    /// </summary>
    public class GameEvent
    {
        private event Action _listeners;

        /// <summary>Number of active subscribers.</summary>
        public int ListenerCount { get; private set; }

        /// <summary>Subscribe a listener.</summary>
        /// <param name="listener">Callback to invoke when event is raised.</param>
        public void Subscribe(Action listener)
        {
            if (listener == null)
                return;

            _listeners += listener;
            ListenerCount++;
        }

        /// <summary>Unsubscribe a listener.</summary>
        /// <param name="listener">The callback to remove.</param>
        public void Unsubscribe(Action listener)
        {
            if (listener == null)
                return;

            _listeners -= listener;
            ListenerCount = Mathf.Max(0, ListenerCount - 1);
        }

        /// <summary>
        /// Raise the event. Catches and logs exceptions from individual listeners.
        /// </summary>
        public void Raise()
        {
            if (_listeners == null)
                return;

            foreach (var listener in _listeners.GetInvocationList())
            {
                try
                {
                    ((Action)listener).Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>Remove all subscribers.</summary>
        public void Clear()
        {
            _listeners = null;
            ListenerCount = 0;
        }
    }
}
