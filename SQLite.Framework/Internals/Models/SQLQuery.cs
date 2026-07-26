namespace SQLite.Framework.Internals.Models;

/// <summary>
/// The compiled SQL query.
/// </summary>
internal class SQLQuery
{
    public required string Sql { get; init; }
    public required List<SQLiteParameter> Parameters { get; init; }
    public required Func<SQLiteQueryContext, object?>? CreateObject { get; init; }
    public required bool Reverse { get; init; }
    public required bool ThrowOnEmpty { get; init; }
    public required bool ElementAtSemantic { get; init; }
    public required bool ThrowOnMoreThanOne { get; init; }

    public object? DefaultValue { get; init; }
    public bool ClientDistinct { get; init; }
    public bool ReverseBeforeDistinct { get; init; }
    public long? ClientTake { get; init; }
    public long? ClientSkip { get; init; }
    public bool ClientCountSemantic { get; init; }
    public bool OptionalRow { get; init; }
    public bool HasDefaultValue { get; init; }
    public bool IsRowSelector { get; init; }
    public IReadOnlyList<MethodInfo>? ReflectedMethods { get; init; }
    public IReadOnlyList<object?>? ReflectedMethodInstances { get; init; }
    public IReadOnlyList<object?>? CapturedValues { get; init; }
    public IReadOnlyList<Type>? ReflectedTypes { get; init; }
    public IReadOnlyList<MemberInfo>? ReflectedMembers { get; init; }
    public IReadOnlyList<ConstructorInfo>? ReflectedConstructors { get; init; }
    public IReadOnlyCollection<string>? ConstructedPaths { get; init; }
    public IReadOnlyDictionary<string, Type>? SelectValueTypes { get; init; }
}
