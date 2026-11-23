using System.Collections.Generic;
using Core.Data.Tiles;
using Gameplay.Board;

namespace Gameplay.Matching
{
    public interface IMatchDetector
    {
        bool TryFindMatches(BoardModel boardModel, List<GridCoord> resultBuffer);
        bool TryFindMatches(BoardModel boardModel, List<GridCoord> resultBuffer, List<SpecialTileCreation> creationsBuffer, GridCoord playerActionCoord);
    }
}
