using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Models;

namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="Queryable"/> extensions for <see cref="IQueryable{T}"/>.
/// </summary>
public static class AsyncQueryableExtensions
{
    /// <summary>
    /// Performs an INSERT operation on the database table using the row.
    /// </summary>
    public static Task<int> AddAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(this SQLiteTable<T> source, T item)
    {
        return ExecuteAsync(source.Add, source.Database, item);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the rows.
    /// </summary>
    public static Task<int> AddRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(this SQLiteTable<T> source, IEnumerable<T> collection)
    {
        return ExecuteAsync(source.AddRange, source.Database, collection);
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the row.
    /// </summary>
    public static Task<int> UpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(this SQLiteTable<T> source, T item)
    {
        return ExecuteAsync(source.Update, source.Database, item);
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the rows.
    /// </summary>
    public static Task<int> UpdateRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(this SQLiteTable<T> source, IEnumerable<T> collection)
    {
        return ExecuteAsync(source.UpdateRange, source.Database, collection);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the row.
    /// </summary>
    public static Task<int> RemoveAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(this SQLiteTable<T> source, T item)
    {
        return ExecuteAsync(source.Remove, source.Database, item);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the primary key.
    /// </summary>
    public static Task<int> RemoveAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(this SQLiteTable<T> source, object primaryKey)
    {
        return ExecuteAsync(source.Remove, source.Database, primaryKey);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the rows.
    /// </summary>
    public static Task<int> RemoveRangeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(this SQLiteTable<T> source, IEnumerable<T> collection)
    {
        return ExecuteAsync(source.RemoveRange, source.Database, collection);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table.
    /// </summary>
    /// <remarks>
    /// WARNING! This will delete all rows in the table.
    /// </remarks>
    public static Task<int> ClearAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(this SQLiteTable<T> source)
    {
        return ExecuteAsync(source.Clear, source.Database);
    }

    /// <summary>
    /// Creates the table in the database if it does not exist.
    /// </summary>
    public static Task<int> CreateTableAsync(this SQLiteTable source)
    {
        return ExecuteAsync(source.CreateTable, source.Database);
    }

    /// <summary>
    /// Deletes the table from the database.
    /// </summary>
    public static Task<int> DropTableAsync(this SQLiteTable source)
    {
        return ExecuteAsync(source.DropTable, source.Database);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}"/> to a <see cref="Dictionary{TKey, TValue}"/>.
    /// </summary>
    public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        where TKey : notnull
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.ToDictionary, table.Database, keySelector, elementSelector);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}"/> to an <see cref="Array"/> of <typeparam name="T" />.
    /// </summary>
    public static Task<T[]> ToArrayAsync<T>(this IQueryable<T> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.ToArray, table.Database);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}"/> to a <see cref="List{T}"/>.
    /// </summary>
    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.ToList, table.Database);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}"/> to a <see cref="HashSet{T}"/>.
    /// </summary>
    public static Task<HashSet<T>> ToHashSetAsync<T>(this IQueryable<T> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.ToHashSet, table.Database);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}"/> to a <see cref="ILookup{TKey, TElement}"/>.
    /// </summary>
    public static Task<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        where TKey : notnull
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.ToLookup, table.Database, keySelector, elementSelector);
    }

    /// <summary>
    /// Returns the first element of a sequence, or throws an exception if sequence contains no elements.
    /// </summary>
    public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.First, table.Database);
    }

    /// <summary>
    /// Returns the first element of a sequence, or throws an exception if sequence contains no elements.
    /// </summary>
    public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.First, table.Database, predicate);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.FirstOrDefault, table.Database);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, TSource defaultValue)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(() => source.FirstOrDefault(defaultValue), table.Database);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.FirstOrDefault, table.Database, predicate);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
    /// </summary>
    public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.FirstOrDefault, table.Database, predicate, defaultValue);
    }

    /// <summary>
    /// Returns the only element of a sequence, or throws an exception if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Single, table.Database);
    }

    /// <summary>
    /// Returns the only element of a sequence, or throws an exception if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Single, table.Database, predicate);
    }

    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.SingleOrDefault, table.Database);
    }

    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, TSource defaultValue)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.SingleOrDefault, table.Database, defaultValue);
    }

    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.SingleOrDefault, table.Database, predicate);
    }

    /// <summary>
    /// Returns the only element of a sequence, or a default value if the sequence is empty;
    /// this method throws an exception if there is more than one element in the sequence.
    /// </summary>
    public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, TSource defaultValue)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.SingleOrDefault, table.Database, predicate, defaultValue);
    }

    /// <summary>
    /// Returns a value indicating whether a sequence contains the provided item.
    /// </summary>
    public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Contains, table.Database, item);
    }

    /// <summary>
    /// Returns a value indicating whether a sequence contains any items.
    /// </summary>
    public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Any, table.Database);
    }

    /// <summary>
    /// Returns a value indicating whether a sequence contains any matching the <paramref name="predicate"/>.
    /// </summary>
    public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Any, table.Database, predicate);
    }

    /// <summary>
    /// Returns a value indicating whether all values in a sequence match the <paramref name="predicate"/>.
    /// </summary>
    public static Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.All, table.Database, predicate);
    }

    /// <summary>
    /// Returns the number of elements in a sequence.
    /// </summary>
    public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Count, table.Database);
    }

    /// <summary>
    /// Returns the number of elements in a sequence matching the <paramref name="predicate"/>.
    /// </summary>
    public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Count, table.Database, predicate);
    }

    /// <summary>
    /// Returns the number of elements in a sequence.
    /// </summary>
    public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.LongCount, table.Database);
    }

    /// <summary>
    /// Returns the number of elements in a sequence matching the <paramref name="predicate"/>.
    /// </summary>
    public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.LongCount, table.Database, predicate);
    }

    /// <summary>
    /// Returns the minimum value in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<TSource?> MinAsync<TSource>(this IQueryable<TSource> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Min, table.Database);
    }

    /// <summary>
    /// Returns the minimum value in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<TResult?> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Min, table.Database, selector);
    }

    /// <summary>
    /// Returns the maximum value in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<TSource?> MaxAsync<TSource>(this IQueryable<TSource> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Max, table.Database);
    }

    /// <summary>
    /// Returns the maximum value in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<TResult?> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Max, table.Database, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<int> SumAsync(this IQueryable<int> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<int?> SumAsync(this IQueryable<int?> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<long> SumAsync(this IQueryable<long> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<long?> SumAsync(this IQueryable<long?> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float> SumAsync(this IQueryable<float> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float?> SumAsync(this IQueryable<float?> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> SumAsync(this IQueryable<double> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> SumAsync(this IQueryable<double?> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal> SumAsync(this IQueryable<decimal> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal?> SumAsync(this IQueryable<decimal?> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database, selector);
    }

    /// <summary>
    /// Returns the sum of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Sum, table.Database, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync(this IQueryable<int> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync(this IQueryable<int?> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync(this IQueryable<long> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync(this IQueryable<long?> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float> AverageAsync(this IQueryable<float> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float?> AverageAsync(this IQueryable<float?> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync(this IQueryable<double> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync(this IQueryable<double?> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal> AverageAsync(this IQueryable<decimal> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database, selector);
    }

    /// <summary>
    /// Returns the average of the values in a generic <see cref="IQueryable{T}" />.
    /// </summary>
    public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
    {
        BaseSQLiteTable table = (BaseSQLiteTable)source;
        return ExecuteAsync(source.Average, table.Database, selector);
    }

    private static Task<T> ExecuteAsync<T>(Func<T> execute, SQLiteDatabase database)
    {
        return Task.Factory.StartNew(() =>
        {
            using (database.Lock())
            {
                return execute();
            }
        }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    private static Task<T> ExecuteAsync<T, TP>(Func<TP, T> execute, SQLiteDatabase database, TP parameter)
    {
        return Task.Factory.StartNew(() =>
        {
            using (database.Lock())
            {
                return execute(parameter);
            }
        }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    private static Task<T> ExecuteAsync<T, TP1, TP2>(Func<TP1, TP2, T> execute, SQLiteDatabase database, TP1 parameter1, TP2 parameter2)
    {
        return Task.Factory.StartNew(() =>
        {
            using (database.Lock())
            {
                return execute(parameter1, parameter2);
            }
        }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }
}