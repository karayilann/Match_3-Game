using Core.Data.Tiles;
using UnityEngine;

namespace Core.Data.Obstacles
{
    [CreateAssetMenu(fileName = "ObstacleDatabase", menuName = "Match3/Obstacle Database")]
    public sealed class ObstacleDatabase : ScriptableObject
    {
        [SerializeField] private ObstacleDefinition[] _definitions;

        public ObstacleDefinition GetDefinition(ObstacleType type)
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (_definitions[i].Type == type)
                {
                    return _definitions[i];
                }
            }

            return default;
        }

        public ObstacleDefinition GetDefinitionByTileType(TileType associatedType)
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (_definitions[i].AssociatedTileType == associatedType)
                {
                    return _definitions[i];
                }
            }

            return default;
        }
    }
}
