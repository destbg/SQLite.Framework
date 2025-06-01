using System.Diagnostics.CodeAnalysis;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Models;

namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="Queryable"/> extensions for <see cref="IQueryable{T}"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public static class QueryableExtensions
{
    /// <summary>
    /// Converts the <see cref="IQueryable{T}"/> to a <see cref="SQLiteCommand"/>.
    /// </summary>
    public static SQLiteCommand ToSqlCommand<T>(this IQueryable<T> queryable)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)queryable;
        SQLTranslator translator = new(table.Database);
        SQLQuery query = translator.Translate(queryable.Expression);

        return table.Database.CreateCommand(query.Sql, query.Parameters);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}"/> to a SQL string.
    /// </summary>
    public static string ToSql<T>(this IQueryable<T> queryable)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)queryable;
        SQLTranslator translator = new(table.Database);
        SQLQuery query = translator.Translate(queryable.Expression);

        return query.Sql;
    }
}