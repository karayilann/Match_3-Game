using UnityEngine;

namespace Core.Data.Tiles
{
    [CreateAssetMenu(fileName = "TileSpriteDatabase", menuName = "Match3/Tile Sprite Database")]
    public sealed class TileSpriteDatabase : ScriptableObject
    {
        [SerializeField] private Sprite[] _sprites;

        public Sprite GetSprite(TileType type)
        {
            byte index = (byte)type;
            return _sprites != null && index < _sprites.Length ? _sprites[index] : null;
        }
    }
}
