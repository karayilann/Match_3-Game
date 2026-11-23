namespace Gameplay.Clock
{
    /// <summary>
    /// The single source of wall-clock time for timers that persist across sessions (lives, later daily rewards).
    /// Keeping it behind one interface lets a server or NTP time replace the device clock without touching callers.
    /// </summary>
    public interface ITimeProvider
    {
        long UtcNowSeconds { get; }
    }
}
