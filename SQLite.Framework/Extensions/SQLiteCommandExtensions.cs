using System.Diagnostics.CodeAnalysis;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="SQLiteCommand"/> Extensions for SQLite.
/// </summary>
public static class SQLiteCommandExtensions
{
    /// <summary>
    /// Executes a query and returns the result set returned by the query.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(SQLiteCommandExtensions))]
    public static IEnumerable<T> ExecuteQuery<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteCommand command)
    {
        using SQLiteDataReader reader = command.ExecuteReader();

        Dictionary<string, (int Index, SQLiteColumnType ColumnType)> columns = [];

        while (reader.Read())
        {
            if (columns.Count == 0)
            {
                columns = CommandHelpers.GetColumnNames(reader.Statement);
            }

            yield return (T)BuildQueryObject.CreateInstance(reader, typeof(T), columns)!;
        }
    }
}