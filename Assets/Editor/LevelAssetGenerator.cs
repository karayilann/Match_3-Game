#if UNITY_EDITOR
using System.IO;
using Core.Data.Goals;
using Core.Data.Level;
using Core.Data.Obstacles;
using Core.Data.Tiles;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class LevelAssetGenerator
    {
        private const string Folder = "Assets/ScriptableObjects/Databases/Level";

        [MenuItem("Match-3/Generate 10 Levels")]
        public static void GenerateAllLevels()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
            }

            // 1. Level 1: Basics
            CreateLevel(1, "1. İlk Adım", 8, 8, 4, LevelLimitType.Moves, 22, 0f,
                new LevelGoalEntry[]
                {
                    new LevelGoalEntry(TileType.Red, 20),
                    new LevelGoalEntry(TileType.Blue, 20)
                }, null, null);

            // 2. Level 2: Wooden Boxes
            var obs2 = new ObstaclePlacementEntry[]
            {
                new ObstaclePlacementEntry(2, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(2, 3, ObstacleType.Box),
                new ObstaclePlacementEntry(2, 4, ObstacleType.Box),
                new ObstaclePlacementEntry(2, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 3, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 4, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(3, 3, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 3, ObstacleType.Box),
                new ObstaclePlacementEntry(3, 4, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 4, ObstacleType.Box)
            };
            CreateLevel(2, "2. Ahşap Sandıklar", 8, 8, 5, LevelLimitType.Moves, 22, 0f,
                new LevelGoalEntry[]
                {
                    new LevelGoalEntry(TileType.Box, 12),
                    new LevelGoalEntry(TileType.Green, 20)
                }, null, obs2);

            // 3. Level 3: Rocks
            var obs3 = new ObstaclePlacementEntry[]
            {
                new ObstaclePlacementEntry(1, 1, ObstacleType.Rock),
                new ObstaclePlacementEntry(6, 1, ObstacleType.Rock),
                new ObstaclePlacementEntry(1, 6, ObstacleType.Rock),
                new ObstaclePlacementEntry(6, 6, ObstacleType.Rock),
                new ObstaclePlacementEntry(3, 2, ObstacleType.Rock),
                new ObstaclePlacementEntry(4, 2, ObstacleType.Rock),
                new ObstaclePlacementEntry(3, 5, ObstacleType.Rock),
                new ObstaclePlacementEntry(4, 5, ObstacleType.Rock)
            };
            CreateLevel(3, "3. Sert Kayalar", 8, 8, 5, LevelLimitType.Moves, 25, 0f,
                new LevelGoalEntry[]
                {
                    new LevelGoalEntry(TileType.Rock, 8),
                    new LevelGoalEntry(TileType.Yellow, 20)
                }, null, obs3);

            // 4. Level 4: Ice
            var obs4 = new ObstaclePlacementEntry[14];
            int idx4 = 0;
            for (byte x = 1; x <= 6; x++)
            {
                obs4[idx4++] = new ObstaclePlacementEntry(x, 0, ObstacleType.Ice);
                obs4[idx4++] = new ObstaclePlacementEntry(x, 1, ObstacleType.Ice);
            }
            obs4[idx4++] = new ObstaclePlacementEntry(0, 0, ObstacleType.Ice);
            obs4[idx4++] = new ObstaclePlacementEntry(7, 0, ObstacleType.Ice);
            CreateLevel(4, "4. Buz Sarkıtları", 8, 8, 5, LevelLimitType.Moves, 24, 0f,
                new LevelGoalEntry[]
                {
                    new LevelGoalEntry(TileType.Ice, 14),
                    new LevelGoalEntry(TileType.Purple, 25)
                }, null, obs4);

            // 5. Level 5: Diamond Board
            var np5 = new GridCoord[]
            {
                new GridCoord(0,0), new GridCoord(0,1), new GridCoord(1,0),
                new GridCoord(0,7), new GridCoord(0,8), new GridCoord(1,8),
                new GridCoord(7,0), new GridCoord(8,0), new GridCoord(8,1),
                new GridCoord(7,8), new GridCoord(8,7), new GridCoord(8,8)
            };
            var obs5 = new ObstaclePlacementEntry[]
            {
                new ObstaclePlacementEntry(4, 3, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 4, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(3, 4, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 4, ObstacleType.Box),
                new ObstaclePlacementEntry(2, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(6, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(2, 6, ObstacleType.Box),
                new ObstaclePlacementEntry(6, 6, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 2, ObstacleType.Box)
            };
            CreateLevel(5, "5. Elmas Meydan", 9, 9, 5, LevelLimitType.Moves, 20, 0f,
                new LevelGoalEntry[]
                {
                    new LevelGoalEntry(TileType.Box, 10),
                    new LevelGoalEntry(TileType.Purple, 20)
                }, np5, obs5);

            // 6. Level 6: Mix
            var obs6 = new ObstaclePlacementEntry[]
            {
                new ObstaclePlacementEntry(2, 1, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 1, ObstacleType.Box),
                new ObstaclePlacementEntry(2, 6, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 6, ObstacleType.Box),
                new ObstaclePlacementEntry(3, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(3, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(3, 3, ObstacleType.Rock),
                new ObstaclePlacementEntry(4, 3, ObstacleType.Rock),
                new ObstaclePlacementEntry(3, 4, ObstacleType.Rock),
                new ObstaclePlacementEntry(4, 4, ObstacleType.Rock),
                new ObstaclePlacementEntry(1, 3, ObstacleType.Rock),
                new ObstaclePlacementEntry(6, 4, ObstacleType.Rock)
            };
            CreateLevel(6, "6. Kombinasyon Ustası", 8, 8, 6, LevelLimitType.Moves, 26, 0f,
                new LevelGoalEntry[]
                {
                    new LevelGoalEntry(TileType.Box, 8),
                    new LevelGoalEntry(TileType.Rock, 6),
                    new LevelGoalEntry(TileType.Blue, 25)
                }, null, obs6);

            // 7. Level 7: Hourglass & Diagonal
            var np7 = new GridCoord[]
            {
                new GridCoord(0,3), new GridCoord(1,3), new GridCoord(0,4),
                new GridCoord(1,4), new GridCoord(2,4), new GridCoord(0,5), new GridCoord(1,5),
                new GridCoord(7,3), new GridCoord(8,3), new GridCoord(6,4),
                new GridCoord(7,4), new GridCoord(8,4), new GridCoord(7,5), new GridCoord(8,5)
            };
            var obs7 = new ObstaclePlacementEntry[]
            {
                new ObstaclePlacementEntry(4, 3, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(3, 3, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 3, ObstacleType.Box),
                new ObstaclePlacementEntry(3, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 1, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 7, ObstacleType.Box),
                new ObstaclePlacementEntry(3, 1, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 1, ObstacleType.Box),
                new ObstaclePlacementEntry(3, 7, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 7, ObstacleType.Box)
            };
            CreateLevel(7, "7. Kum Saati & Çapraz Akış", 9, 9, 5, LevelLimitType.Moves, 24, 0f,
                new LevelGoalEntry[]
                {
                    new LevelGoalEntry(TileType.Box, 12),
                    new LevelGoalEntry(TileType.Red, 30)
                }, np7, obs7);

            // 8. Level 8: Timed Level
            var obs8 = new ObstaclePlacementEntry[]
            {
                new ObstaclePlacementEntry(1, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(2, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(6, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(1, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(2, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(6, 5, ObstacleType.Box)
            };
            CreateLevel(8, "8. Zamana Karşı Yarış", 8, 8, 5, LevelLimitType.Time, 0, 80f,
                new LevelGoalEntry[]
                {
                    new LevelGoalEntry(TileType.Green, 30),
                    new LevelGoalEntry(TileType.Yellow, 30),
                    new LevelGoalEntry(TileType.Box, 8)
                }, null, obs8);

            // 9. Level 9: Ice & Rock Fortress
            var obs9 = new ObstaclePlacementEntry[]
            {
                new ObstaclePlacementEntry(0, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(1, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(2, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(3, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(4, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(5, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(6, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(7, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(0, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(1, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(2, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(3, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(4, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(5, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(6, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(7, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(2, 2, ObstacleType.Rock),
                new ObstaclePlacementEntry(5, 2, ObstacleType.Rock),
                new ObstaclePlacementEntry(3, 3, ObstacleType.Rock),
                new ObstaclePlacementEntry(4, 3, ObstacleType.Rock),
                new ObstaclePlacementEntry(3, 4, ObstacleType.Rock),
                new ObstaclePlacementEntry(4, 4, ObstacleType.Rock),
                new ObstaclePlacementEntry(2, 5, ObstacleType.Rock),
                new ObstaclePlacementEntry(5, 5, ObstacleType.Rock)
            };
            CreateLevel(9, "9. Kaya ve Buz Barikatı", 8, 8, 5, LevelLimitType.Moves, 25, 0f,
                new LevelGoalEntry[]
                {
                    new LevelGoalEntry(TileType.Ice, 16),
                    new LevelGoalEntry(TileType.Rock, 8)
                }, null, obs9);

            // 10. Level 10: Grand Finale
            var np10 = new GridCoord[]
            {
                new GridCoord(1, 8), new GridCoord(3, 8), new GridCoord(5, 8), new GridCoord(7, 8),
                new GridCoord(1, 7), new GridCoord(7, 7)
            };
            var obs10 = new ObstaclePlacementEntry[]
            {
                new ObstaclePlacementEntry(0, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(1, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(7, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(8, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(0, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(1, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(7, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(8, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(3, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(4, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(5, 0, ObstacleType.Ice),
                new ObstaclePlacementEntry(4, 1, ObstacleType.Ice),
                new ObstaclePlacementEntry(2, 2, ObstacleType.Rock),
                new ObstaclePlacementEntry(6, 2, ObstacleType.Rock),
                new ObstaclePlacementEntry(2, 6, ObstacleType.Rock),
                new ObstaclePlacementEntry(6, 6, ObstacleType.Rock),
                new ObstaclePlacementEntry(4, 3, ObstacleType.Rock),
                new ObstaclePlacementEntry(4, 5, ObstacleType.Rock),
                new ObstaclePlacementEntry(3, 4, ObstacleType.Rock),
                new ObstaclePlacementEntry(5, 4, ObstacleType.Rock),
                new ObstaclePlacementEntry(3, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(2, 3, ObstacleType.Box),
                new ObstaclePlacementEntry(6, 3, ObstacleType.Box),
                new ObstaclePlacementEntry(2, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(6, 5, ObstacleType.Box),
                new ObstaclePlacementEntry(3, 6, ObstacleType.Box),
                new ObstaclePlacementEntry(5, 6, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 4, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 2, ObstacleType.Box),
                new ObstaclePlacementEntry(4, 6, ObstacleType.Box),
                new ObstaclePlacementEntry(0, 4, ObstacleType.Box)
            };
            CreateLevel(10, "10. Kraliyet Tacı", 9, 9, 6, LevelLimitType.Moves, 30, 0f,
                new LevelGoalEntry[]
                {
                    new LevelGoalEntry(TileType.Box, 12),
                    new LevelGoalEntry(TileType.Rock, 8),
                    new LevelGoalEntry(TileType.Ice, 12)
                }, np10, obs10);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[LevelAssetGenerator] 10 level assets generated successfully!");
        }

        private static void CreateLevel(
            int id, string name, byte width, byte height, byte colors,
            LevelLimitType limitType, byte moves, float timeSec,
            LevelGoalEntry[] goals,
            GridCoord[] nonPlayable,
            ObstaclePlacementEntry[] obstacles)
        {
            string path = $"{Folder}/Level_{id:D2}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var so = new SerializedObject(asset);
            so.Update();
            so.FindProperty("_levelId").intValue = id;
            so.FindProperty("_levelName").stringValue = name;
            so.FindProperty("_levelOrder").intValue = id;
            so.FindProperty("_width").intValue = width;
            so.FindProperty("_height").intValue = height;
            so.FindProperty("_activeColorCount").intValue = colors;
            so.FindProperty("_limitType").enumValueIndex = (int)limitType;
            so.FindProperty("_moveLimit").intValue = moves;
            so.FindProperty("_timeLimitSeconds").floatValue = timeSec;
            so.FindProperty("_pointsPerTile").intValue = 10;
            so.FindProperty("_enableHints").boolValue = true;
            so.FindProperty("_hintDelaySeconds").floatValue = 4.5f;
            so.FindProperty("_useRandomPlacement").boolValue = false;

            var goalsProp = so.FindProperty("_goals");
            goalsProp.ClearArray();
            for (int g = 0; g < goals.Length; g++)
            {
                goalsProp.InsertArrayElementAtIndex(g);
                var elem = goalsProp.GetArrayElementAtIndex(g);
                elem.FindPropertyRelative("TargetType").enumValueIndex = (int)goals[g].TargetType;
                elem.FindPropertyRelative("TargetCount").intValue = goals[g].TargetCount;
            }

            var npProp = so.FindProperty("_nonPlayableCells");
            npProp.ClearArray();
            if (nonPlayable != null)
            {
                for (int n = 0; n < nonPlayable.Length; n++)
                {
                    npProp.InsertArrayElementAtIndex(n);
                    var elem = npProp.GetArrayElementAtIndex(n);
                    elem.FindPropertyRelative("X").intValue = nonPlayable[n].X;
                    elem.FindPropertyRelative("Y").intValue = nonPlayable[n].Y;
                }
            }

            var obsProp = so.FindProperty("_obstaclePlacements");
            obsProp.ClearArray();
            if (obstacles != null)
            {
                for (int o = 0; o < obstacles.Length; o++)
                {
                    obsProp.InsertArrayElementAtIndex(o);
                    var elem = obsProp.GetArrayElementAtIndex(o);
                    elem.FindPropertyRelative("X").intValue = obstacles[o].X;
                    elem.FindPropertyRelative("Y").intValue = obstacles[o].Y;
                    elem.FindPropertyRelative("Type").enumValueIndex = (int)obstacles[o].Type;
                }
            }

            so.FindProperty("_goalDataFile").objectReferenceValue = null;
            so.FindProperty("_nonPlayableDataFile").objectReferenceValue = null;
            so.FindProperty("_obstacleDataFile").objectReferenceValue = null;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }
    }
}
#endif
