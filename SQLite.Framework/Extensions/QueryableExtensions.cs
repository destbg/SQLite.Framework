using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.Enums;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Models;

namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="Queryable"/> extensions for <see cref="IQueryable{T}"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Executes the query and deletes the records from the database.
    /// </summary>
    public static int ExecuteDelete<T>(this IQueryable<T> source)
    {
        if (source is not BaseSQLiteTable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteTable)}.");
        }

        SQLTranslator translator = new(table.Database)
        {
            QueryType = QueryType.Delete,
        };
        SQLQuery query = translator.Translate(source.Expression);

        return table.Database.CreateCommand(query.Sql, query.Parameters).ExecuteNonQuery();
    }

    /// <summary>
    /// Executes the query and deletes the records from the database.
    /// </summary>
    public static int ExecuteDelete<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
    {
        if (source is not BaseSQLiteTable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteTable)}.");
        }

        SQLTranslator translator = new(table.Database)
        {
            QueryType = QueryType.Delete,
        };
        SQLQuery query = translator.Translate(source.Where(predicate).Expression);

        return table.Database.CreateCommand(query.Sql, query.Parameters).ExecuteNonQuery();
    }

    /// <summary>
    /// Executes the query and updates the records in the database.
    /// </summary>
    public static int ExecuteUpdate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this IQueryable<T> source, Func<SQLitePropertyCalls<T>, SQLitePropertyCalls<T>> setters)
    {
        if (source is not BaseSQLiteTable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteTable)}.");
        }

        SQLTranslator translator = new(table.Database)
        {
            QueryType = QueryType.Update,
        };
        translator.Visit(source.Expression);

        SQLitePropertyCalls<T> propertyCalls = new(translator.Visitor, table.Database.TableMapping<T>());
        translator.SetProperties = setters(propertyCalls).SetProperties;

        SQLQuery query = translator.Translate(null);

        return table.Database.CreateCommand(query.Sql, query.Parameters).ExecuteNonQuery();
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}"/> to a <see cref="SQLiteCommand"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
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
    [ExcludeFromCodeCoverage]
    public static string ToSql<T>(this IQueryable<T> queryable)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)queryable;
        SQLTranslator translator = new(table.Database);
        SQLQuery query = translator.Translate(queryable.Expression);

        return query.Sql;
    }
}