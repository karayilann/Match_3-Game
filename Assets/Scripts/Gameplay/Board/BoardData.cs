using System.Collections.Generic;
using Core.Data.Tiles;
using Gameplay.Matching;
using View;

namespace Gameplay.Board
{
    public sealed class BoardData
    {
        public readonly BoardModel Model;
        public readonly GridToWorldConverter Converter;
        public readonly GemView[] Views;
        public readonly IMatchDetector MatchDetector;
        public readonly List<GridCoord> MatchBuffer;
        public readonly byte ActiveColorCount;

        public BoardData(BoardModel model, GridToWorldConverter converter, GemView[] views, IMatchDetector matchDetector, byte activeColorCount)
        {
            Model = model;
            Converter = converter;
            Views = views;
            MatchDetector = matchDetector;
            MatchBuffer = new List<GridCoord>(model.Width * model.Height);
            ActiveColorCount = activeColorCount;
        }
    }
}
