namespace SQLite.Framework.Enums;

/// <summary>
/// Values for <c>PRAGMA synchronous</c>. Lower numbers are faster but less safe if the OS or
/// machine crashes. Higher numbers do more disk syncs.
/// </summary>
public enum SQLiteSynchronousMode
{
    /// <summary>
    /// SQLite does not call sync at all. Fastest, but data can be lost on a crash or power loss.
    /// </summary>
    Off = 0,

    /// <summary>
    /// SQLite syncs at the most important moments. The default for WAL mode.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// SQLite syncs after every commit. The default for non-WAL modes.
    /// </summary>
    Full = 2,

    /// <summary>
    /// Like <see cref="Full" /> but also syncs the directory entry. Most paranoid.
    /// </summary>
    Extra = 3,
}
