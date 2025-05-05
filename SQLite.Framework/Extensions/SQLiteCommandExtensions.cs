using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="SqliteCommand"/> Extensions for SQLite.
/// </summary>
public static class SQLiteCommandExtensions
{
    /// <summary>
    /// Executes a query and returns the result set returned by the query.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(SQLiteCommandExtensions))]
    public static IEnumerable<T> ExecuteQuery<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SqliteCommand command)
    {
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            yield return (T)BuildQueryObject.CreateInstance(reader, typeof(T))!;
        }
    }
}