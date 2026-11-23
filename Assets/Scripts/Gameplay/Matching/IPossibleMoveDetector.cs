using Core.Data.Tiles;
using Gameplay.Board;

namespace Gameplay.Matching
{
    public interface IPossibleMoveDetector
    {
        bool HasPossibleMove(BoardModel board);
        bool TryFindPossibleMove(BoardModel board, out GridCoord from, out GridCoord to);
    }
}
