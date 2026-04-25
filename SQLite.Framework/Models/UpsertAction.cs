using SQLite.Framework.Internals.Enums;

namespace SQLite.Framework.Models;

/// <summary>
/// Terminal node of <see cref="UpsertBuilder{T}" />. Represents one of <c>DO NOTHING</c>,
/// <c>DO UPDATE SET ...</c> with a fixed list of columns, or <c>DO UPDATE SET</c> for every
/// non-conflict column.
/// </summary>
public sealed class UpsertAction<T>
{
    private UpsertAction(UpsertActionKind kind, IReadOnlyList<string>? columns)
    {
        Kind = kind;
        Columns = columns;
    }

    internal UpsertActionKind Kind { get; }

    internal IReadOnlyList<string>? Columns { get; }

    internal static UpsertAction<T> DoNothing { get; } = new(UpsertActionKind.DoNothing, null);

    internal static UpsertAction<T> DoUpdateAll { get; } = new(UpsertActionKind.DoUpdateAll, null);

    internal static UpsertAction<T> DoUpdate(IReadOnlyList<string> columns)
    {
        return new UpsertAction<T>(UpsertActionKind.DoUpdate, columns);
    }
}
