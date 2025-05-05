using Microsoft.Data.Sqlite;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.Models;

namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="Queryable"/> extensions for <see cref="IQueryable{T}"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Converts the <see cref="IQueryable{T}"/> to a <see cref="SqliteCommand"/>.
    /// </summary>
    public static SqliteCommand ToSqlCommand<T>(this IQueryable<T> queryable)
    {
        SQLiteTable table = (SQLiteTable)queryable;
        SQLTranslator translator = new(table.Database);
        SQLQuery query = translator.Translate(queryable.Expression);

        return table.Database.CreateCommand(query.Sql, query.Parameters);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}"/> to a SQL string.
    /// </summary>
    public static string ToSql<T>(this IQueryable<T> queryable)
    {
        SQLiteTable table = (SQLiteTable)queryable;
        SQLTranslator translator = new(table.Database);
        SQLQuery query = translator.Translate(queryable.Expression);

        return query.Sql;
    }
}