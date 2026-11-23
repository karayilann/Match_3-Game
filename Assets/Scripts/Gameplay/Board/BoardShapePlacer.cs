using System;
using Core.Data.Level;
using Core.Data.Tiles;
using Utils;

namespace Gameplay.Board
{
    public static class BoardShapePlacer
    {
        public static void ApplyNonPlayableCells(BoardModel model, LevelData levelData)
        {
            if (levelData.NonPlayableDataFile != null)
            {
                ApplyFromText(model, levelData.NonPlayableDataFile.text.AsSpan());
                return;
            }

            ApplyManual(model, levelData.NonPlayableCells);
        }

        private static void ApplyManual(BoardModel model, GridCoord[] coords)
        {
            if (coords == null)
            {
                return;
            }

            for (int i = 0; i < coords.Length; i++)
            {
                GridCoord coord = coords[i];
                if (!model.IsValidCoord(coord))
                {
                    continue;
                }

                model.GetCell(coord).Flags |= CellFlags.NonPlayable;
            }
        }

        private static void ApplyFromText(BoardModel model, ReadOnlySpan<char> text)
        {
            while (CsvSpanReader.TryReadLine(ref text, out ReadOnlySpan<char> line))
            {
                if (!CsvSpanReader.TryReadByte(ref line, out byte x) ||
                    !CsvSpanReader.TryReadByte(ref line, out byte y))
                {
                    continue;
                }

                GridCoord coord = new GridCoord(x, y);
                if (!model.IsValidCoord(coord))
                {
                    continue;
                }

                model.GetCell(coord).Flags |= CellFlags.NonPlayable;
            }
        }
    }
}
