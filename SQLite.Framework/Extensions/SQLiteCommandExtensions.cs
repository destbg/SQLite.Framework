using System.Diagnostics.CodeAnalysis;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;

namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="SQLiteCommand" /> Extensions for SQLite.
/// </summary>
[ExcludeFromCodeCoverage]
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

            yield return (T)BuildQueryObject.CreateInstance(reader, typeof(T), columns, null)!;
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(SQLiteCommandExtensions))]
    internal static IEnumerable<T> ExecuteQueryInternal<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteCommand command, SQLQuery query)
    {
        IEnumerable<T> enumerable = Enumerate(command, query);
        return query.Reverse ? enumerable.Reverse() : enumerable;

        static IEnumerable<T> Enumerate(SQLiteCommand command, SQLQuery query)
        {
            using SQLiteDataReader reader = command.ExecuteReader();

            Dictionary<string, (int Index, SQLiteColumnType ColumnType)> columns = [];

            while (reader.Read())
            {
                if (columns.Count == 0)
                {
                    columns = CommandHelpers.GetColumnNames(reader.Statement);
                }

                yield return (T)BuildQueryObject.CreateInstance(reader, typeof(T), columns, query.CreateObject)!;
            }
        }
    }
}