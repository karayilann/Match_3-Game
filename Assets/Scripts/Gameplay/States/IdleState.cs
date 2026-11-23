using Core.StateMachine;
using Gameplay.Hints;
using Gameplay.Input;
using UnityEngine;

namespace Gameplay.States
{
    public sealed class IdleState : IGameState
    {
        private readonly BoardInputHandler _input;
        private readonly HintService _hintService;

        public IdleState(BoardInputHandler input, HintService hintService)
        {
            _input = input;
            _hintService = hintService;
        }

        public void Enter()
        {
            _hintService.ClearHint();
            _input.SetInputEnabled(true);
        }

        public void Execute()
        {
            _hintService.Tick(Time.deltaTime);
        }

        public void Exit()
        {
            _hintService.ClearHint();
        }
    }
}
