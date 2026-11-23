using System;
using System.Reflection;
using Core.Data.Level;
using Core.Data.Obstacles;
using Core.Data.Tiles;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// ScriptableObject fields are private serialized fields, so tests set them through reflection.
    /// </summary>
    public static class TestFactory
    {
        public static LevelData CreateLevel(LevelLimitType limitType, byte moves, float seconds)
        {
            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            Set(level, "_limitType", limitType);
            Set(level, "_moveLimit", moves);
            Set(level, "_timeLimitSeconds", seconds);
            return level;
        }

        public static ObstacleDatabase CreateObstacleDatabase(params ObstacleDefinition[] definitions)
        {
            ObstacleDatabase database = ScriptableObject.CreateInstance<ObstacleDatabase>();
            Set(database, "_definitions", definitions);
            return database;
        }

        public static ObstacleDefinition BoxDefinition(bool canTakeDamage)
        {
            return new ObstacleDefinition
            {
                Type = ObstacleType.Box,
                AssociatedTileType = TileType.Box,
                DefaultDurability = 2,
                CanTakeAdjacentDamage = canTakeDamage
            };
        }

        private static void Set(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (info == null)
            {
                throw new MissingFieldException(target.GetType().Name, field);
            }

            info.SetValue(target, value);
        }
    }
}
