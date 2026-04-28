namespace SQLite.Framework.Internals.Models;

/// <summary>
/// The compiled SQL query.
/// </summary>
[ExcludeFromCodeCoverage]
internal class SQLQuery
{
    public required string Sql { get; init; }
    public required List<SQLiteParameter> Parameters { get; init; }
    public required Func<SQLiteQueryContext, object?>? CreateObject { get; init; }
    public required bool Reverse { get; init; }
    public required bool ThrowOnEmpty { get; init; }
    public required bool ThrowOnMoreThanOne { get; init; }
    public IReadOnlyList<MethodInfo>? ReflectedMethods { get; init; }
    public IReadOnlyList<object?>? ReflectedMethodInstances { get; init; }
    public IReadOnlyList<object?>? CapturedValues { get; init; }
    public IReadOnlyList<Type>? ReflectedTypes { get; init; }
    public IReadOnlyList<MemberInfo>? ReflectedMembers { get; init; }
    public IReadOnlyList<ConstructorInfo>? ReflectedConstructors { get; init; }
}