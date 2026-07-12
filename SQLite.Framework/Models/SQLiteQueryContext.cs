namespace SQLite.Framework.Models;

/// <summary>
/// Data passed to every materializer. Has the reader, the column map and optional extra lists
/// used by generated code when it cannot call a member by name.
/// </summary>
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

    /// <summary>
    /// Constructors the generator cannot call by name, for example the constructor of an anonymous
    /// type whose members include a type that is not visible from the generated file.
    /// The generator reads them by index and invokes them with positional arguments.
    /// </summary>
    public IReadOnlyList<ConstructorInfo>? ReflectedConstructors { get; init; }

    /// <summary>
    /// Dotted result paths the projection builds with an explicit object initializer or
    /// constructor call. A nested object at one of these paths is always created, even when every
    /// column it reads is NULL, since the projection code would always run its constructor. A
    /// nested path not listed here is a projected entity, which reads back as null when all of
    /// its columns are NULL.
    /// </summary>
    public IReadOnlyCollection<string>? ConstructedPaths { get; init; }

    /// <summary>
    /// The CLR type each select column carried in the projection, keyed by column name. A
    /// materializer uses this to read a column bound to an object or interface typed member
    /// with the value type the projection produced.
    /// </summary>
    public IReadOnlyDictionary<string, Type>? SelectValueTypes { get; init; }

    /// <summary>
    /// Returns the projected CLR type of the named select column or null when it is unknown.
    /// </summary>
    public Type? GetSelectValueType(string column)
    {
        return SelectValueTypes != null && SelectValueTypes.TryGetValue(column, out Type? type) ? type : null;
    }
}
