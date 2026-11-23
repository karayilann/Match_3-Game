using System;

namespace Gameplay.Clock
{
    public sealed class SystemTimeProvider : ITimeProvider
    {
        public long UtcNowSeconds => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
