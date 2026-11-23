using System;
using Core.Data.Tiles;
using UnityEngine;

namespace Core.Data.Obstacles
{
    [Serializable]
    public struct ObstacleDefinition
    {
        public ObstacleType Type;
        public TileType AssociatedTileType;
        public Sprite Sprite;
        public Sprite[] DamageStageSprites;
        public byte DefaultDurability;

        [Header("Behavior")]
        public bool CanTakeAdjacentDamage;
        public bool SpawnsTileOnDestroy;
        public bool CanFall;

        public Sprite GetSpriteForDurability(byte remainingDurability)
        {
            if (DamageStageSprites == null || DamageStageSprites.Length == 0)
            {
                return Sprite;
            }

            int takenDamage = DefaultDurability > remainingDurability ? DefaultDurability - remainingDurability : 0;
            int stageIndex = takenDamage < DamageStageSprites.Length ? takenDamage : DamageStageSprites.Length - 1;
            Sprite stageSprite = DamageStageSprites[stageIndex];
            return stageSprite != null ? stageSprite : Sprite;
        }
    }
}
