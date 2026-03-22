using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoRPG.Core
{
    /// <summary>
    /// Type-safe event channel for decoupled communication between systems.
    /// Subscribe from any system, raise from any system.
    ///
    /// Uses a List-backed listener store instead of multicast delegates
    /// to avoid allocation on Raise and maintain accurate listener count.
    ///
    /// Usage:
    /// <code>
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
        private readonly List<Action<T>> _listeners = new();

        /// <summary>Number of active subscribers.</summary>
        public int ListenerCount => _listeners.Count;

        /// <summary>
        /// Subscribe a listener. Duplicate subscriptions are rejected.
        /// </summary>
        /// <param name="listener">Callback to invoke when event is raised.</param>
        public void Subscribe(Action<T> listener)
        {
            if (listener == null || _listeners.Contains(listener))
                return;

            _listeners.Add(listener);
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

            _listeners.Remove(listener);
        }

        /// <summary>
        /// Raise the event, invoking all subscribers. Catches and logs exceptions
        /// from individual listeners to prevent one bad listener from breaking others.
        /// Zero allocation when no listeners. No allocation during iteration.
        /// </summary>
        /// <param name="args">Event arguments.</param>
        public void Raise(T args)
        {
            // Iterate by index to avoid List enumerator allocation
            // and to safely handle subscribers that unsubscribe during Raise
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                try
                {
                    _listeners[i].Invoke(args);
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
            _listeners.Clear();
        }
    }

    /// <summary>
    /// Parameterless event channel for simple signals (e.g., BattleStarted, TurnEnded).
    /// </summary>
    public class GameEvent
    {
        private readonly List<Action> _listeners = new();

        /// <summary>Number of active subscribers.</summary>
        public int ListenerCount => _listeners.Count;

        /// <summary>Subscribe a listener. Duplicate subscriptions are rejected.</summary>
        /// <param name="listener">Callback to invoke when event is raised.</param>
        public void Subscribe(Action listener)
        {
            if (listener == null || _listeners.Contains(listener))
                return;

            _listeners.Add(listener);
        }

        /// <summary>Unsubscribe a listener.</summary>
        /// <param name="listener">The callback to remove.</param>
        public void Unsubscribe(Action listener)
        {
            if (listener == null)
                return;

            _listeners.Remove(listener);
        }

        /// <summary>
        /// Raise the event. Catches and logs exceptions from individual listeners.
        /// </summary>
        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                try
                {
                    _listeners[i].Invoke();
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
            _listeners.Clear();
        }
    }
}
