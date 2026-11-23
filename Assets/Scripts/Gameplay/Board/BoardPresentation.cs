using Core.Data.Tiles;
using Core.Pooling;
using Gameplay.Animation;
using View;

namespace Gameplay.Board
{
    public sealed class BoardPresentation
    {
        public readonly BoardAnimator Animator;
        public readonly PoolManager<GemView> GemPool;
        public readonly PoolManager<MatchParticleView> ParticlePool;
        public readonly PoolManager<MatchParticleView> RockBreakParticlePool;
        public readonly TileSpriteDatabase SpriteDatabase;

        public BoardPresentation(BoardAnimator animator, PoolManager<GemView> gemPool, PoolManager<MatchParticleView> particlePool, PoolManager<MatchParticleView> rockBreakParticlePool, TileSpriteDatabase spriteDatabase)
        {
            Animator = animator;
            GemPool = gemPool;
            ParticlePool = particlePool;
            RockBreakParticlePool = rockBreakParticlePool;
            SpriteDatabase = spriteDatabase;
        }
    }
}
