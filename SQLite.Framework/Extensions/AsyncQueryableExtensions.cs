using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
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
    public static Task<int> ExecuteDeleteAsync<T>(this IQueryable<T> source)
    {
        if (source is not BaseSQLiteTable)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteTable)}.");
        }

        return ExecuteAsync(source.ExecuteDelete);
    }

    /// <summary>
    /// Executes the query and deletes the records from the database.
    /// </summary>
    public static Task<int> ExecuteDeleteAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
    {
        if (source is not BaseSQLiteTable)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteTable)}.");
        }

        return ExecuteAsync(source.ExecuteDelete, predicate);
    }

    /// <summary>
    /// Executes the query and updates the records in the database.
    /// </summary>
    public static Task<int> ExecuteUpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this IQueryable<T> source, Func<SQLitePropertyCalls<T>, SQLitePropertyCalls<T>> setters)
    {
        if (source is not BaseSQLiteTable)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteTable)}.");
        }

        return ExecuteAsync(source.ExecuteUpdate, setters);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the row.
    /// </summary>
    public static Task<int> AddAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item)
    {
        return ExecuteAsync(source.Add, item);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the rows.
    /// </summary>
    public static Task<int> AddRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        return ExecuteAsync(source.AddRange, collection, runInTransaction, separateConnection);
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the row.
    /// </summary>
    public static Task<int> UpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item)
    {
        return ExecuteAsync(source.Update, item);
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the rows.
    /// </summary>
    public static Task<int> UpdateRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        return ExecuteAsync(source.UpdateRange, collection, runInTransaction, separateConnection);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the row.
    /// </summary>
    public static Task<int> RemoveAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item)
    {
        return ExecuteAsync(source.Remove, item);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the rows.
    /// </summary>
    public static Task<int> RemoveRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        return ExecuteAsync(source.RemoveRange, collection, runInTransaction, separateConnection);
    }

    /// <summary>
    /// Performs an INSERT OR REPLACE operation on the database table using the row.
    /// </summary>
    public static Task<int> AddOrUpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, T item)
    {
        return ExecuteAsync(source.AddOrUpdate, item);
    }

    /// <summary>
    /// Performs an INSERT OR REPLACE operation on the database table using the rows.
    /// </summary>
    public static Task<int> AddOrUpdateRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteTable<T> source, IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        return ExecuteAsync(source.AddOrUpdateRange, collection, runInTransaction, separateConnection);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table.
    /// </summary>
    /// <remarks>
    /// WARNING! This will delete all rows in the table.
    /// </remarks>
    public static Task<int> ClearAsync(this SQLiteTable source)
    {
        return ExecuteAsync(source.Clear);
    }

    /// <summary>
    /// Creates the table in the database if it does not exist.
    /// </summary>
    public static Task<int> CreateTableAsync(this SQLiteTable source)
    {
        return ExecuteAsync(source.CreateTable);
    }

    /// <summary>
    /// Deletes the table from the database.
    /// </summary>
    public static Task<int> DropTableAsync(this SQLiteTable source)
    {
        return ExecuteAsync(source.DropTable);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to a <see cref="Dictionary{TKey, TValue}" />.
    /// </summary>
    public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        where TKey : notnull
    {
        return ExecuteAsync(source.ToDictionary, keySelector, elementSelector);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to an <see cref="Array" /> of
    /// <typeparam name="T" />
    /// .
    /// </summary>
    public static Task<T[]> ToArrayAsync<T>(this IQueryable<T> source)
    {
        return ExecuteAsync(source.ToArray);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to a <see cref="List{T}" />.
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source)
    {
        return ExecuteAsync(source.ToList);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to a <see cref="HashSet{T}" />.
    /// </summary>
    public static Task<HashSet<T>> ToHashSetAsync<T>(this IQueryable<T> source)
    {
        return ExecuteAsync(source.ToHashSet);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}" /> to a <see cref="ILookup{TKey, TElement}" />.
    /// </summary>
    public static Task<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        where TKey : notnull
    {
        return ExecuteAsync(source.ToLookup, keySelector, elementSelector);
    }

    /// <summary>
    /// Returns the first element of a sequence, or throws an exception if sequence contains no elements.
    /// </summary>
    public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source)
    {
        return ExecuteAsync(source.First);
    }

    /// <summary>
    /// Returns the first element of a sequence, or throws an exception if sequence contains no elements.
    /// </summary>
    public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        return ExecuteAsync(source.First, predicate);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source)
    {
        return ExecuteAsync(source.FirstOrDefault);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, TSource defaultValue)
    {
        return ExecuteAsync(source.FirstOrDefault, defaultValue);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        return ExecuteAsync(source.FirstOrDefault, predicate);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue)
    {
        return ExecuteAsync(source.FirstOrDefault, predicate, defaultValue);
    }

    /// <summary>
    /// Returns the only element of a sequence, or throws an exception if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source)
    {
        return ExecuteAsync(source.Single);
    }

    /// <summary>
    /// Returns the only element of a sequence, or throws an exception if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        return ExecuteAsync(source.Single, predicate);
    }

    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source)
    {
        return ExecuteAsync(source.SingleOrDefault);
    }

    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, TSource defaultValue)
    {
        return ExecuteAsync(source.SingleOrDefault, defaultValue);
    }

    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        return ExecuteAsync(source.SingleOrDefault, predicate);
    }

    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue)
    {
        return ExecuteAsync(source.SingleOrDefault, predicate, defaultValue);
    }

    /// <summary>
    /// Returns a value indicating whether a sequence contains the provided item.
    /// </summary>
    public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item)
    {
        return ExecuteAsync(source.Contains, item);
    }

    /// <summary>
    /// Returns a value indicating whether a sequence contains any items.
    /// </summary>
    public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source)
    {
        return ExecuteAsync(source.Any);
    }

    /// <summary>
    /// Returns a value indicating whether a sequence contains any matching the <paramref name="predicate" />.
    /// </summary>
    public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        return ExecuteAsync(source.Any, predicate);
    }

    /// <summary>
    /// Returns a value indicating whether all values in a sequence match the <paramref name="predicate" />.
    /// </summary>
    public static Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        return ExecuteAsync(source.All, predicate);
    }

    /// <summary>
    /// Returns the number of elements in a sequence.
    /// </summary>
    public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source)
    {
        return ExecuteAsync(source.Count);
    }

    /// <summary>
    /// Returns the number of elements in a sequence matching the <paramref name="predicate" />.
    /// </summary>
    public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        return ExecuteAsync(source.Count, predicate);
    }

    /// <summary>
    /// Returns the number of elements in a sequence.
    /// </summary>
    public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source)
    {
        return ExecuteAsync(source.LongCount);
    }

    /// <summary>
    /// Returns the number of elements in a sequence matching the <paramref name="predicate" />.
    /// </summary>
    public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        return ExecuteAsync(source.LongCount, predicate);
    }

    /// <summary>
    /// Returns the minimum value in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<TSource?> MinAsync<TSource>(this IQueryable<TSource> source)
    {
        return ExecuteAsync(source.Min);
    }

    /// <summary>
    /// Returns the minimum value in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<TResult?> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
    {
        return ExecuteAsync(source.Min, selector);
    }

    /// <summary>
    /// Returns the maximum value in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<TSource?> MaxAsync<TSource>(this IQueryable<TSource> source)
    {
        return ExecuteAsync(source.Max);
    }

    /// <summary>
    /// Returns the maximum value in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<TResult?> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
    {
        return ExecuteAsync(source.Max, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<int> SumAsync(this IQueryable<int> source)
    {
        return ExecuteAsync(source.Sum);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<int?> SumAsync(this IQueryable<int?> source)
    {
        return ExecuteAsync(source.Sum);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<long> SumAsync(this IQueryable<long> source)
    {
        return ExecuteAsync(source.Sum);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<long?> SumAsync(this IQueryable<long?> source)
    {
        return ExecuteAsync(source.Sum);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float> SumAsync(this IQueryable<float> source)
    {
        return ExecuteAsync(source.Sum);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float?> SumAsync(this IQueryable<float?> source)
    {
        return ExecuteAsync(source.Sum);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> SumAsync(this IQueryable<double> source)
    {
        return ExecuteAsync(source.Sum);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> SumAsync(this IQueryable<double?> source)
    {
        return ExecuteAsync(source.Sum);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal> SumAsync(this IQueryable<decimal> source)
    {
        return ExecuteAsync(source.Sum);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal?> SumAsync(this IQueryable<decimal?> source)
    {
        return ExecuteAsync(source.Sum);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
    {
        return ExecuteAsync(source.Sum, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
    {
        return ExecuteAsync(source.Sum, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
    {
        return ExecuteAsync(source.Sum, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
    {
        return ExecuteAsync(source.Sum, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
    {
        return ExecuteAsync(source.Sum, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
    {
        return ExecuteAsync(source.Sum, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
    {
        return ExecuteAsync(source.Sum, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
    {
        return ExecuteAsync(source.Sum, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
    {
        return ExecuteAsync(source.Sum, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
    {
        return ExecuteAsync(source.Sum, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync(this IQueryable<int> source)
    {
        return ExecuteAsync(source.Average);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync(this IQueryable<int?> source)
    {
        return ExecuteAsync(source.Average);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync(this IQueryable<long> source)
    {
        return ExecuteAsync(source.Average);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync(this IQueryable<long?> source)
    {
        return ExecuteAsync(source.Average);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float> AverageAsync(this IQueryable<float> source)
    {
        return ExecuteAsync(source.Average);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float?> AverageAsync(this IQueryable<float?> source)
    {
        return ExecuteAsync(source.Average);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync(this IQueryable<double> source)
    {
        return ExecuteAsync(source.Average);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync(this IQueryable<double?> source)
    {
        return ExecuteAsync(source.Average);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal> AverageAsync(this IQueryable<decimal> source)
    {
        return ExecuteAsync(source.Average);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source)
    {
        return ExecuteAsync(source.Average);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
    {
        return ExecuteAsync(source.Average, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
    {
        return ExecuteAsync(source.Average, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
    {
        return ExecuteAsync(source.Average, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
    {
        return ExecuteAsync(source.Average, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
    {
        return ExecuteAsync(source.Average, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
    {
        return ExecuteAsync(source.Average, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
    {
        return ExecuteAsync(source.Average, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
    {
        return ExecuteAsync(source.Average, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
    {
        return ExecuteAsync(source.Average, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
    {
        return ExecuteAsync(source.Average, selector);
    }

    private static Task<T> ExecuteAsync<T>(Func<T> execute)
    {
        return Task.Factory.StartNew(execute, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    private static Task<T> ExecuteAsync<T, TP>(Func<TP, T> execute, TP parameter)
    {
        return Task.Factory.StartNew(() => execute(parameter), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    private static Task<T> ExecuteAsync<T, TP1, TP2>(Func<TP1, TP2, T> execute, TP1 parameter1, TP2 parameter2)
    {
        return Task.Factory.StartNew(() => execute(parameter1, parameter2), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    private static Task<T> ExecuteAsync<T, TP1, TP2, TP3>(Func<TP1, TP2, TP3, T> execute, TP1 parameter1, TP2 parameter2, TP3 parameter3)
    {
        return Task.Factory.StartNew(() => execute(parameter1, parameter2, parameter3), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }
}