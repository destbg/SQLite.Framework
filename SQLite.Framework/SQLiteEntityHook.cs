namespace SQLite.Framework;

/// <summary>
/// One registered per-entity write hook, the type it was registered for and the delegate to run.
/// The hooks for a written entity run in registration order across every registration whose type
/// covers the entity, including base types and interfaces.
/// </summary>
public sealed class SQLiteEntityHook
{
    /// <summary>
    /// Creates a hook entry for the given registration type and delegate.
    /// </summary>
    public SQLiteEntityHook(Type entityType, Delegate hook)
    {
        EntityType = entityType;
        Hook = hook;
    }

    /// <summary>
    /// The type the hook was registered for. The hook runs for every written entity this type
    /// is assignable from.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// The registered hook delegate.
    /// </summary>
    public Delegate Hook { get; }
}
