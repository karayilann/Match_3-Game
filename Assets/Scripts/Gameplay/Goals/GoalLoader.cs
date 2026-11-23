using System;
using System.Collections.Generic;
using Core.Data.Goals;
using Core.Data.Level;
using Core.Data.Tiles;
using Utils;

namespace Gameplay.Goals
{
    public static class GoalLoader
    {
        public static void LoadGoals(LevelData levelData, List<LevelGoalEntry> targetBuffer)
        {
            targetBuffer.Clear();

            if (levelData.GoalDataFile != null)
            {
                LoadFromText(levelData.GoalDataFile.text.AsSpan(), targetBuffer);
                return;
            }

            LoadManual(levelData.Goals, targetBuffer);
        }

        private static void LoadManual(LevelGoalEntry[] goals, List<LevelGoalEntry> targetBuffer)
        {
            if (goals == null)
            {
                return;
            }

            for (int i = 0; i < goals.Length; i++)
            {
                LevelGoalEntry entry = goals[i];
                if (entry.TargetType == TileType.None || entry.TargetCount == 0)
                {
                    continue;
                }

                targetBuffer.Add(entry);
            }
        }

        private static void LoadFromText(ReadOnlySpan<char> text, List<LevelGoalEntry> targetBuffer)
        {
            while (CsvSpanReader.TryReadLine(ref text, out ReadOnlySpan<char> line))
            {
                if (!CsvSpanReader.TryReadByte(ref line, out byte typeValue) ||
                    !CsvSpanReader.TryReadByte(ref line, out byte targetCount))
                {
                    continue;
                }

                TileType targetType = (TileType)typeValue;
                if (targetType == TileType.None || targetCount == 0)
                {
                    continue;
                }

                CsvSpanReader.TryReadByte(ref line, out byte kindValue);
                targetBuffer.Add(new LevelGoalEntry(targetType, targetCount, (GoalKind)kindValue));
            }
        }
    }
}

