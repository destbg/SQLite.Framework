namespace SQLite.Framework.SourceGenerator.Enums;

/// <summary>
/// How an entity materializer is emitted.
/// </summary>
public enum EmitStrategy
{
    /// <summary>
    /// Construct the entity directly.
    /// </summary>
    Direct,

    /// <summary>
    /// Construct the entity through reflection.
    /// </summary>
    Reflection,

    /// <summary>
    /// Construct an anonymous type.
    /// </summary>
    Anonymous,

    /// <summary>
    /// Construct through a positional constructor.
    /// </summary>
    Positional,
}
