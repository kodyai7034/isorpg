namespace IsoRPG.Core
{
    /// <summary>
    /// A state in a finite state machine. Receives the machine reference
    /// so states can trigger transitions from within <see cref="Execute"/> or <see cref="Enter"/>.
    /// </summary>
    /// <typeparam name="T">The context type shared across all states.</typeparam>
    public interface IState<T>
    {
        /// <summary>
        /// Called once when entering this state. Use for initialization.
        /// May call <paramref name="machine"/>.ChangeState to immediately transition.
        /// </summary>
        /// <param name="context">Shared battle/game context.</param>
        /// <param name="machine">State machine reference for triggering transitions.</param>
        void Enter(T context, IStateMachine<T> machine);

        /// <summary>
        /// Called each frame while this state is active.
        /// May call <paramref name="machine"/>.ChangeState to transition.
        /// </summary>
        /// <param name="context">Shared battle/game context.</param>
        /// <param name="machine">State machine reference for triggering transitions.</param>
        void Execute(T context, IStateMachine<T> machine);

        /// <summary>
        /// Called once when leaving this state. Use for cleanup.
        /// </summary>
        /// <param name="context">Shared battle/game context.</param>
        void Exit(T context);
    }

    /// <summary>
    /// State machine interface exposed to states for triggering transitions.
    /// </summary>
    /// <typeparam name="T">The context type shared across all states.</typeparam>
    public interface IStateMachine<T>
    {
        /// <summary>The currently active state.</summary>
        IState<T> CurrentState { get; }

        /// <summary>
        /// Transition to a new state. Calls Exit on current, then Enter on new.
        /// Safe to call from within Enter or Execute — transitions during Enter
        /// are deferred until Enter completes.
        /// </summary>
        /// <param name="newState">The state to transition to.</param>
        void ChangeState(IState<T> newState);
    }

    /// <summary>
    /// Finite state machine with transition guarding.
    /// Prevents recursive transitions: if ChangeState is called during Enter,
    /// the transition is queued and applied after Enter completes.
    /// </summary>
    /// <typeparam name="T">The context type shared across all states.</typeparam>
    public class StateMachine<T> : IStateMachine<T>
    {
        /// <inheritdoc/>
        public IState<T> CurrentState { get; private set; }

        private readonly T _context;
        private bool _isTransitioning;
        private IState<T> _pendingState;

        /// <summary>
        /// Create a state machine with the given shared context.
        /// </summary>
        /// <param name="context">Context object passed to all states.</param>
        public StateMachine(T context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public void ChangeState(IState<T> newState)
        {
            if (newState == null)
                return;

            // If we're already transitioning (e.g., ChangeState called from Enter),
            // queue the transition instead of executing immediately
            if (_isTransitioning)
            {
                _pendingState = newState;
                return;
            }

            PerformTransition(newState);

            // Process any queued transition from within Enter
            while (_pendingState != null)
            {
                var pending = _pendingState;
                _pendingState = null;
                PerformTransition(pending);
            }
        }

        /// <summary>
        /// Call each frame to execute the current state's logic.
        /// </summary>
        public void Update()
        {
            CurrentState?.Execute(_context, this);
        }

        private void PerformTransition(IState<T> newState)
        {
            _isTransitioning = true;

            CurrentState?.Exit(_context);
            CurrentState = newState;
            CurrentState.Enter(_context, this);

            _isTransitioning = false;
        }
    }
}
