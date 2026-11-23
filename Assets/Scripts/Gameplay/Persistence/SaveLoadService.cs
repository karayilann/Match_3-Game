using System;
using System.IO;
using Core.Data.Tiles;
using ErrorHandling;
using Gameplay.Board;
using Gameplay.Goals;
using Gameplay.Scoring;
using UnityEngine;

namespace Gameplay.Persistence
{
    public static class SaveLoadService
    {
        private const string SAVE_KEY_PREFIX = "MATCH3_SAVE_LEVEL_";
        private const byte CURRENT_SAVE_VERSION = 1;
        private const int INITIAL_BUFFER_SIZE = 512;

        private static readonly MemoryStream SAVE_STREAM = new MemoryStream(INITIAL_BUFFER_SIZE);
        private static readonly BinaryWriter SAVE_WRITER = new BinaryWriter(SAVE_STREAM);

        private static int _cachedKeyLevelId = -1;
        private static string _cachedKey;

        public static bool HasSavedGame(int levelId)
        {
            return PlayerPrefs.HasKey(GetSaveKey(levelId));
        }

        public static void ClearSavedGame(int levelId)
        {
            string key = GetSaveKey(levelId);
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        public static void SaveGame(int levelId, BoardModel board, LevelLimitService limit, ScoreService score, GoalService goal)
        {
            if (board == null || limit == null || score == null || goal == null)
            {
                return;
            }

            SAVE_STREAM.SetLength(0);
            BinaryWriter writer = SAVE_WRITER;

            writer.Write(CURRENT_SAVE_VERSION);
            writer.Write(levelId);
            writer.Write(board.Width);
            writer.Write(board.Height);

            for (byte y = 0; y < board.Height; y++)
            {
                for (byte x = 0; x < board.Width; x++)
                {
                    ref GridCell cell = ref board.GetCell(new GridCoord(x, y));
                    writer.Write((byte)cell.Type);
                    writer.Write((byte)cell.Flags);
                    writer.Write(cell.Durability);
                }
            }

            writer.Write(limit.RemainingMoves);
            writer.Write(limit.RemainingTime);
            writer.Write(score.CurrentScore);

            byte goalCount = (byte)goal.GoalCount;
            writer.Write(goalCount);
            for (int i = 0; i < goalCount; i++)
            {
                writer.Write(goal.GetCurrentCount(i));
            }

            writer.Flush();
            string base64 = Convert.ToBase64String(SAVE_STREAM.GetBuffer(), 0, (int)SAVE_STREAM.Length);
            PlayerPrefs.SetString(GetSaveKey(levelId), base64);
            PlayerPrefs.Save();
        }

        public static bool TryLoadGame(
            int levelId,
            BoardModel board,
            LevelLimitService limit,
            ScoreService score,
            GoalService goal,
            byte[] outGoalCounts)
        {
            string key = GetSaveKey(levelId);
            if (!PlayerPrefs.HasKey(key))
            {
                return false;
            }

            try
            {
                string base64 = PlayerPrefs.GetString(key);
                byte[] bytes = Convert.FromBase64String(base64);

                using (MemoryStream stream = new MemoryStream(bytes))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    byte version = reader.ReadByte();
                    int savedLevelId = reader.ReadInt32();
                    if (savedLevelId != levelId)
                    {
                        return false;
                    }

                    byte width = reader.ReadByte();
                    byte height = reader.ReadByte();
                    if (width != board.Width || height != board.Height)
                    {
                        return false;
                    }

                    for (byte y = 0; y < height; y++)
                    {
                        for (byte x = 0; x < width; x++)
                        {
                            ref GridCell cell = ref board.GetCell(new GridCoord(x, y));
                            cell.Type = (TileType)reader.ReadByte();
                            cell.Flags = (CellFlags)reader.ReadByte();
                            cell.Durability = reader.ReadByte();
                        }
                    }

                    byte moves = reader.ReadByte();
                    float time = reader.ReadSingle();
                    limit.RestoreState(moves, time);

                    int savedScore = reader.ReadInt32();
                    score.RestoreScore(savedScore);

                    byte goalCount = reader.ReadByte();
                    for (int i = 0; i < goalCount && i < outGoalCounts.Length; i++)
                    {
                        outGoalCounts[i] = reader.ReadByte();
                    }
                    goal.RestoreGoalCounts(outGoalCounts);

                    return true;
                }
            }
            catch (Exception exception)
            {
                ErrorReporter.RecordException(exception);
                ClearSavedGame(levelId);
                return false;
            }
        }

        /// <summary>
        /// The board saves after every settle, so the key string is cached per level instead of rebuilt each time.
        /// </summary>
        private static string GetSaveKey(int levelId)
        {
            if (levelId != _cachedKeyLevelId || _cachedKey == null)
            {
                _cachedKeyLevelId = levelId;
                _cachedKey = SAVE_KEY_PREFIX + levelId;
            }

            return _cachedKey;
        }
    }
}
