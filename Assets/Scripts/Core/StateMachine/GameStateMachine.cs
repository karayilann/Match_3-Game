using System.Runtime.CompilerServices;

namespace Core.StateMachine
{
    public sealed class GameStateMachine
    {
        private IGameState _currentState;

        public IGameState CurrentState
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentState;
        }

        public void ChangeState(IGameState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        public void Tick()
        {
            _currentState?.Execute();
        }
    }
}
