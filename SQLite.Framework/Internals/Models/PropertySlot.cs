namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Reflection data for one writable property on an entity type. Built once by
/// <see cref="ReflectionMaterializerCache" /> and reused for every row.
/// </summary>
internal sealed class PropertySlot
{
    public required PropertyInfo Property { get; init; }

    public required string Name { get; init; }

    public required Type PropertyType { get; init; }

    public required Type TargetType { get; init; }

    public required bool IsSimple { get; init; }

    public required bool IsEnum { get; init; }

    public required Type? EnumUnderlyingType { get; init; }

    public required Action<object, object?> Setter { get; init; }

    public Action<sqlite3_stmt, int, object>? Assigner { get; init; }
}
