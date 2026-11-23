using Core.StateMachine;

namespace Gameplay.States
{
    /// <summary>
    /// The states reference each other in a cycle (Match, Fall, Fill), so they are created first and
    /// wired here once instead of through per-state setters.
    /// </summary>
    public sealed class BoardStates
    {
        public readonly GameStateMachine Machine;

        public IdleState Idle { get; private set; }
        public SwapState Swap { get; private set; }
        public MatchState Match { get; private set; }
        public FallState Fall { get; private set; }
        public FillState Fill { get; private set; }
        public ReshuffleState Reshuffle { get; private set; }

        public BoardStates(GameStateMachine machine)
        {
            Machine = machine;
        }

        public void Init(IdleState idle, SwapState swap, MatchState match, FallState fall, FillState fill, ReshuffleState reshuffle)
        {
            Idle = idle;
            Swap = swap;
            Match = match;
            Fall = fall;
            Fill = fill;
            Reshuffle = reshuffle;
        }
    }
}
