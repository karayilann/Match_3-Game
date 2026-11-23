using System;
using Core.Data.Level;
using Core.Data.Obstacles;
using Core.Data.Tiles;
using Gameplay.Board;
using Utils;
using Random = UnityEngine.Random;

namespace Gameplay.Obstacles
{
    public static class ObstaclePlacer
    {
        public static void PlaceObstacles(BoardData board, ObstacleDatabase obstacleDatabase, LevelData levelData)
        {
            if (levelData.UseRandomPlacement)
            {
                PlaceRandom(board, obstacleDatabase, levelData.RandomObstacleType, levelData.RandomObstacleCount);
                return;
            }

            if (levelData.ObstacleDataFile != null)
            {
                PlaceFromText(board, obstacleDatabase, levelData.ObstacleDataFile.text.AsSpan());
                return;
            }

            PlaceManual(board, obstacleDatabase, levelData.ObstaclePlacements);
        }

        private static void PlaceManual(BoardData board, ObstacleDatabase obstacleDatabase, ObstaclePlacementEntry[] placements)
        {
            if (placements == null)
            {
                return;
            }

            for (int i = 0; i < placements.Length; i++)
            {
                ObstaclePlacementEntry entry = placements[i];
                GridCoord coord = new GridCoord(entry.X, entry.Y);

                if (!board.Model.IsValidCoord(coord) || !board.Model.GetCell(coord).IsPlayable)
                {
                    continue;
                }

                ObstacleDefinition definition = obstacleDatabase.GetDefinition(entry.Type);
                PlaceAtCoord(board, coord, definition);
            }
        }

        private static void PlaceFromText(BoardData board, ObstacleDatabase obstacleDatabase, ReadOnlySpan<char> text)
        {
            while (CsvSpanReader.TryReadLine(ref text, out ReadOnlySpan<char> line))
            {
                if (!CsvSpanReader.TryReadByte(ref line, out byte x) ||
                    !CsvSpanReader.TryReadByte(ref line, out byte y) ||
                    !CsvSpanReader.TryReadByte(ref line, out byte typeValue))
                {
                    continue;
                }

                GridCoord coord = new GridCoord(x, y);
                if (!board.Model.IsValidCoord(coord) || !board.Model.GetCell(coord).IsPlayable)
                {
                    continue;
                }

                ObstacleDefinition definition = obstacleDatabase.GetDefinition((ObstacleType)typeValue);
                PlaceAtCoord(board, coord, definition);
            }
        }

        /// <summary>
        /// Uses a partial Fisher-Yates shuffle over playable flat grid indices so obstacles are placed on distinct playable cells without retry loops or overflowing playable cell count.
        /// </summary>
        private static void PlaceRandom(BoardData board, ObstacleDatabase obstacleDatabase, ObstacleType type, byte count)
        {
            byte width = board.Model.Width;
            byte height = board.Model.Height;
            int totalCells = width * height;
            int[] indices = new int[totalCells];
            int playableCount = 0;

            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    GridCoord coord = new GridCoord(x, y);
                    if (!board.Model.GetCell(coord).IsPlayable)
                    {
                        continue;
                    }

                    indices[playableCount] = board.Model.ToIndex(coord);
                    playableCount++;
                }
            }

            int targetCount = count > playableCount ? playableCount : count;
            if (targetCount == 0)
            {
                return;
            }

            ObstacleDefinition definition = obstacleDatabase.GetDefinition(type);

            for (int i = 0; i < targetCount; i++)
            {
                int swapIndex = Random.Range(i, playableCount);
                (indices[i], indices[swapIndex]) = (indices[swapIndex], indices[i]);

                int cellIndex = indices[i];
                GridCoord coord = new GridCoord((byte)(cellIndex % width), (byte)(cellIndex / width));
                PlaceAtCoord(board, coord, definition);
            }
        }

        private static void PlaceAtCoord(BoardData board, GridCoord coord, ObstacleDefinition definition)
        {
            ref GridCell cell = ref board.Model.GetCell(coord);
            cell.Type = definition.AssociatedTileType;
            cell.Durability = definition.DefaultDurability;
            cell.Flags |= CellFlags.Locked;

            int index = board.Model.ToIndex(coord);
            board.Views[index].SetSprite(definition.Sprite);
        }
    }
}
