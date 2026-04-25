using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Models;

namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="Queryable" /> extensions for <see cref="IQueryable{T}" />.
/// </summary>
[ExcludeFromCodeCoverage]
public static class AsyncQueryableExtensions
{
    /// <summary>
    /// Executes the query and deletes the records from the database.
    /// </summary>
    public static Task<int> ExecuteDeleteAsync<T>(this IQueryable<T> source, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteTable)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteTable)}.");
        }

        return AsyncRunner.Run(source.ExecuteDelete, ct);
    }

    /// <summary>
    /// Executes the query and deletes the records from the database.
    /// </summary>
    public static Task<int> ExecuteDeleteAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteTable)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteTable)}.");
        }

        return AsyncRunner.Run(source.ExecuteDelete, predicate, ct);
    }

    /// <summary>
    /// Executes the query and updates the records in the database.
    /// </summary>
    public static Task<int> ExecuteUpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this IQueryable<T> source, Func<SQLitePropertyCalls<T>, SQLitePropertyCalls<T>> setters, CancellationToken ct = default)
    {
        if (source is not BaseSQLiteTable)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteTable)}.");
        }

        return AsyncRunner.Run(source.ExecuteUpdate, setters, ct);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the row.
    /// </summary>
    public static Task<int> AddAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Add, item, ct);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the rows.
    /// </summary>
    public static Task<int> AddRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.AddRange, collection, runInTransaction, separateConnection, ct);
    }

    /// <summary>
    /// Copies rows from <paramref name="query" /> into <paramref name="source" /> using a single
    /// <c>INSERT INTO ... SELECT</c> statement. Runs on a background thread.
    /// </summary>
    public static Task<int> InsertFromQueryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IQueryable<T> query, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.InsertFromQuery, query, ct);
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the row.
    /// </summary>
    public static Task<int> UpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Update, item, ct);
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the rows.
    /// </summary>
    public static Task<int> UpdateRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.UpdateRange, collection, runInTransaction, separateConnection, ct);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the row.
    /// </summary>
    public static Task<int> RemoveAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Remove, item, ct);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the rows.
    /// </summary>
    public static Task<int> RemoveRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.RemoveRange, collection, runInTransaction, separateConnection, ct);
    }

    /// <summary>
    /// Performs an <c>INSERT OR REPLACE</c> operation on the database table using the row.
    /// </summary>
    public static Task<int> AddOrUpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => source.AddOrUpdate(item, SQLiteConflict.Replace), ct);
    }

    /// <summary>
    /// Performs an <c>INSERT OR &lt;conflict&gt;</c> operation on the database table using the row.
    /// </summary>
    public static Task<int> AddOrUpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item, SQLiteConflict conflict, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => source.AddOrUpdate(item, conflict), ct);
    }

    /// <summary>
    /// Performs an <c>INSERT OR REPLACE</c> operation on the database table using the rows.
    /// </summary>
    public static Task<int> AddOrUpdateRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => source.AddOrUpdateRange(collection, runInTransaction, separateConnection, SQLiteConflict.Replace), ct);
    }

    /// <summary>
    /// Performs an <c>INSERT OR &lt;conflict&gt;</c> operation on the database table using the rows.
    /// </summary>
    public static Task<int> AddOrUpdateRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, SQLiteConflict conflict, bool runInTransaction = true, bool separateConnection = false, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => source.AddOrUpdateRange(collection, runInTransaction, separateConnection, conflict), ct);
    }

    /// <summary>
    /// Performs an <c>INSERT INTO ... ON CONFLICT (...) DO ...</c> upsert built through the
    /// <see cref="UpsertBuilder{T}" /> DSL.
    /// </summary>
    public static Task<int> UpsertAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item, Action<UpsertBuilder<T>> configure, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => source.Upsert(item, configure), ct);
    }

    /// <summary>
    /// Range version of <see cref="UpsertAsync" />.
    /// </summary>
    public static Task<int> UpsertRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, Action<UpsertBuilder<T>> configure, bool runInTransaction = true, bool separateConnection = false, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => source.UpsertRange(collection, configure, runInTransaction, separateConnection), ct);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table.
    /// </summary>
    /// <remarks>
    /// WARNING! This will delete all rows in the table.
    /// </remarks>
    public static Task<int> ClearAsync(this SQLiteTable source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Clear, ct);
    }

    /// <summary>
    /// Creates the table in the database if it does not exist.
    /// </summary>
    [Obsolete("Use Database.Schema.CreateTableAsync<T>() instead.")]
    public static Task<int> CreateTableAsync(this SQLiteTable source, CancellationToken ct = default)
    {
#pragma warning disable CS0618
        return AsyncRunner.Run(source.CreateTable, ct);
#pragma warning restore CS0618
    }

    /// <summary>
    /// Deletes the table from the database.
    /// </summary>
    [Obsolete("Use Database.Schema.DropTableAsync<T>() instead.")]
    public static Task<int> DropTableAsync(this SQLiteTable source, CancellationToken ct = default)
    {
#pragma warning disable CS0618
        return AsyncRunner.Run(source.DropTable, ct);
#pragma warning restore CS0618
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to a <see cref="Dictionary{TKey, TValue}" />.
    /// </summary>
    public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken ct = default)
        where TKey : notnull
    {
        return AsyncRunner.Run(source.ToDictionary, keySelector, elementSelector, ct);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to an <see cref="Array" /> of
    /// <typeparam name="T" />
    /// .
    /// </summary>
    public static Task<T[]> ToArrayAsync<T>(this IQueryable<T> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.ToArray, ct);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to a <see cref="List{T}" />.
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.ToList, ct);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to a <see cref="HashSet{T}" />.
    /// </summary>
    public static Task<HashSet<T>> ToHashSetAsync<T>(this IQueryable<T> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.ToHashSet, ct);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to a <see cref="ILookup{TKey, TElement}" />.
    /// </summary>
    public static Task<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken ct = default)
        where TKey : notnull
    {
        return AsyncRunner.Run(source.ToLookup, keySelector, elementSelector, ct);
    }

    /// <summary>
    /// Returns the first element of a sequence, or throws an exception if sequence contains no elements.
    /// </summary>
    public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.First, ct);
    }

    /// <summary>
    /// Returns the first element of a sequence, or throws an exception if sequence contains no elements.
    /// </summary>
    public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.First, predicate, ct);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.FirstOrDefault, ct);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, TSource defaultValue, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.FirstOrDefault, defaultValue, ct);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.FirstOrDefault, predicate, ct);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.FirstOrDefault, predicate, defaultValue, ct);
    }

    /// <summary>
    /// Returns the only element of a sequence, or throws an exception if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Single, ct);
    }

    /// <summary>
    /// Returns the only element of a sequence, or throws an exception if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Single, predicate, ct);
    }

    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.SingleOrDefault, ct);
    }

    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, TSource defaultValue, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.SingleOrDefault, defaultValue, ct);
    }

    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.SingleOrDefault, predicate, ct);
    }

    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.SingleOrDefault, predicate, defaultValue, ct);
    }

    /// <summary>
    /// Returns a value indicating whether a sequence contains the provided item.
    /// </summary>
    public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Contains, item, ct);
    }

    /// <summary>
    /// Returns a value indicating whether a sequence contains any items.
    /// </summary>
    public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Any, ct);
    }

    /// <summary>
    /// Returns a value indicating whether a sequence contains any matching the <paramref name="predicate" />.
    /// </summary>
    public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Any, predicate, ct);
    }

    /// <summary>
    /// Returns a value indicating whether all values in a sequence match the <paramref name="predicate" />.
    /// </summary>
    public static Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.All, predicate, ct);
    }

    /// <summary>
    /// Returns the number of elements in a sequence.
    /// </summary>
    public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Count, ct);
    }

    /// <summary>
    /// Returns the number of elements in a sequence matching the <paramref name="predicate" />.
    /// </summary>
    public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Count, predicate, ct);
    }

    /// <summary>
    /// Returns the number of elements in a sequence.
    /// </summary>
    public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.LongCount, ct);
    }

    /// <summary>
    /// Returns the number of elements in a sequence matching the <paramref name="predicate" />.
    /// </summary>
    public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.LongCount, predicate, ct);
    }

    /// <summary>
    /// Returns the minimum value in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<TSource?> MinAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Min, ct);
    }

    /// <summary>
    /// Returns the minimum value in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<TResult?> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Min, selector, ct);
    }

    /// <summary>
    /// Returns the maximum value in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<TSource?> MaxAsync<TSource>(this IQueryable<TSource> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Max, ct);
    }

    /// <summary>
    /// Returns the maximum value in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<TResult?> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Max, selector, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<int> SumAsync(this IQueryable<int> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<int?> SumAsync(this IQueryable<int?> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<long> SumAsync(this IQueryable<long> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<long?> SumAsync(this IQueryable<long?> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float> SumAsync(this IQueryable<float> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float?> SumAsync(this IQueryable<float?> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> SumAsync(this IQueryable<double> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> SumAsync(this IQueryable<double?> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal?> SumAsync(this IQueryable<decimal?> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, selector, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, selector, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, selector, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, selector, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, selector, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, selector, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, selector, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, selector, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, selector, ct);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Sum, selector, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync(this IQueryable<int> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync(this IQueryable<int?> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync(this IQueryable<long> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync(this IQueryable<long?> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float> AverageAsync(this IQueryable<float> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float?> AverageAsync(this IQueryable<float?> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync(this IQueryable<double> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync(this IQueryable<double?> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal> AverageAsync(this IQueryable<decimal> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, selector, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, selector, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, selector, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, selector, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, selector, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, selector, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, selector, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, selector, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, selector, ct);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken ct = default)
    {
        return AsyncRunner.Run(source.Average, selector, ct);
    }
}