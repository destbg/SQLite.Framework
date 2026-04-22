using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SQLite.Framework.Models;

/// <summary>
/// Data passed to every materializer. Has the reader, the column map and optional extra lists
/// used by generated code when it cannot call a member by name.
/// </summary>
[ExcludeFromCodeCoverage]
public class SQLiteQueryContext
{
    /// <summary>
    /// The reader on the current row.
    /// </summary>
    public SQLiteDataReader? Reader { get; init; }

    /// <summary>
    /// Maps a column name to its index in the reader.
    /// </summary>
    public Dictionary<string, int>? Columns { get; init; }

    /// <summary>
    /// Extra row value used when a compiled expression needs a parameter.
    /// </summary>
    public object? Input { get; init; }

    /// <summary>
    /// Methods that the generated code cannot call by name, for example a private static helper.
    /// The generator reads them by index.
    /// </summary>
    public IReadOnlyList<MethodInfo>? ReflectedMethods { get; init; }

    /// <summary>
    /// Instance for each method in <see cref="ReflectedMethods" />. Null when the method is static.
    /// </summary>
    public IReadOnlyList<object?>? ReflectedMethodInstances { get; init; }

    /// <summary>
    /// Values that were captured from outside the lambda, for example a local variable.
    /// The generator reads them by index.
    /// </summary>
    public IReadOnlyList<object?>? CapturedValues { get; init; }

    /// <summary>
    /// Types that the generated code cannot name, for example a private nested class used as a
    /// Select target. The generator calls <c>Activator.CreateInstance</c> on these by index.
    /// </summary>
    public IReadOnlyList<Type>? ReflectedTypes { get; init; }

    /// <summary>
    /// Property or field handles used with <see cref="ReflectedTypes" /> to set values and read
    /// collections. The generator reads them by index.
    /// </summary>
    public IReadOnlyList<MemberInfo>? ReflectedMembers { get; init; }
}
