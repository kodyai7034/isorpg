namespace IsoRPG.Core
{
    public interface IState<T>
    {
        void Enter(T context);
        void Execute(T context);
        void Exit(T context);
    }

    public class StateMachine<T>
    {
        public IState<T> CurrentState { get; private set; }
        private T _context;

        public StateMachine(T context)
        {
            _context = context;
        }

        public void ChangeState(IState<T> newState)
        {
            CurrentState?.Exit(_context);
            CurrentState = newState;
            CurrentState.Enter(_context);
        }

        public void Update()
        {
            CurrentState?.Execute(_context);
        }
    }
}
