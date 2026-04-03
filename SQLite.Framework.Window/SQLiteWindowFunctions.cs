namespace SQLite.Framework.Window;

/// <summary>
/// Marker methods for SQLite window functions. These methods throw at runtime and are only
/// valid inside a LINQ query where they are translated to their SQL equivalents.
/// Register translations by calling <see cref="SQLiteStorageOptionsWindowExtensions.AddWindow" />
/// on your <see cref="SQLiteStorageOptions" />.
/// </summary>
public static class SQLiteWindowFunctions
{
    /// <summary>
    /// Computes the sum of the values in the window. Translates to <c>SUM(value)</c>.
    /// </summary>
    public static T Sum<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of the values in the window. Translates to <c>AVG(value)</c>.
    /// </summary>
    public static T Avg<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the minimum value in the window. Translates to <c>MIN(value)</c>.
    /// </summary>
    public static T Min<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the maximum value in the window. Translates to <c>MAX(value)</c>.
    /// </summary>
    public static T Max<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Counts all rows in the window. Translates to <c>COUNT(*)</c>.
    /// </summary>
    public static long Count()
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Counts non-null values in the window. Translates to <c>COUNT(value)</c>.
    /// </summary>
    public static long Count<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the sequential row number within the window partition. Translates to <c>ROW_NUMBER()</c>.
    /// </summary>
    public static long RowNumber()
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the rank of the current row with gaps for ties. Translates to <c>RANK()</c>.
    /// </summary>
    public static long Rank()
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the rank of the current row without gaps for ties. Translates to <c>DENSE_RANK()</c>.
    /// </summary>
    public static long DenseRank()
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the relative rank of the current row as a value between 0 and 1. Translates to <c>PERCENT_RANK()</c>.
    /// </summary>
    public static double PercentRank()
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the cumulative distribution of the current row within its partition. Translates to <c>CUME_DIST()</c>.
    /// </summary>
    public static double CumeDist()
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Divides the partition into the specified number of buckets and assigns a bucket number to each row.
    /// Translates to <c>NTILE(buckets)</c>.
    /// </summary>
    public static long NTile(long buckets)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value from the previous row in the window. Translates to <c>LAG(value)</c>.
    /// </summary>
    public static T Lag<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value from a row <paramref name="offset" /> rows before the current row.
    /// Translates to <c>LAG(value, offset)</c>.
    /// </summary>
    public static T Lag<T>(T value, long offset)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value from a row <paramref name="offset" /> rows before the current row,
    /// or <paramref name="defaultValue" /> if no such row exists. Translates to <c>LAG(value, offset, default)</c>.
    /// </summary>
    public static T Lag<T>(T value, long offset, T defaultValue)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value from the next row in the window. Translates to <c>LEAD(value)</c>.
    /// </summary>
    public static T Lead<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value from a row <paramref name="offset" /> rows after the current row.
    /// Translates to <c>LEAD(value, offset)</c>.
    /// </summary>
    public static T Lead<T>(T value, long offset)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value from a row <paramref name="offset" /> rows after the current row,
    /// or <paramref name="defaultValue" /> if no such row exists. Translates to <c>LEAD(value, offset, default)</c>.
    /// </summary>
    public static T Lead<T>(T value, long offset, T defaultValue)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value of the first row in the window frame. Translates to <c>FIRST_VALUE(value)</c>.
    /// </summary>
    public static T FirstValue<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value of the last row in the window frame. Translates to <c>LAST_VALUE(value)</c>.
    /// </summary>
    public static T LastValue<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value of the <paramref name="n" />th row in the window frame (1-based).
    /// Translates to <c>NTH_VALUE(value, n)</c>.
    /// </summary>
    public static T NthValue<T>(T value, long n)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Begins the OVER clause for a window function. Translates to <c>function OVER ()</c>.
    /// Chain <see cref="PartitionBy{T, TKey}" />, <see cref="OrderBy{T, TKey}" />, or frame methods to build
    /// the window specification.
    /// </summary>
    public static T Over<T>(this T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds a PARTITION BY clause to the window. Translates by inserting <c>PARTITION BY key</c> into the OVER clause.
    /// </summary>
    public static T PartitionBy<T, TKey>(this T value, TKey key)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds an additional partition column after <see cref="PartitionBy{T, TKey}" />.
    /// Translates by appending <c>, key</c> to the existing PARTITION BY list.
    /// </summary>
    public static T ThenPartitionBy<T, TKey>(this T value, TKey key)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds an ORDER BY clause to the window in ascending order.
    /// Translates by inserting <c>ORDER BY key ASC</c> into the OVER clause.
    /// </summary>
    public static T OrderBy<T, TKey>(this T value, TKey key)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds an ORDER BY clause to the window in descending order.
    /// Translates by inserting <c>ORDER BY key DESC</c> into the OVER clause.
    /// </summary>
    public static T OrderByDescending<T, TKey>(this T value, TKey key)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds an additional ascending sort key after <see cref="OrderBy{T, TKey}" /> or
    /// <see cref="OrderByDescending{T, TKey}" />.
    /// Translates by appending <c>, key ASC</c> to the existing ORDER BY list.
    /// </summary>
    public static T ThenOrderBy<T, TKey>(this T value, TKey key)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds an additional descending sort key after <see cref="OrderBy{T, TKey}" /> or
    /// <see cref="OrderByDescending{T, TKey}" />.
    /// Translates by appending <c>, key DESC</c> to the existing ORDER BY list.
    /// </summary>
    public static T ThenOrderByDescending<T, TKey>(this T value, TKey key)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds a ROWS frame clause to the window. Translates to <c>ROWS BETWEEN start AND end</c>.
    /// Use <see cref="FrameBoundary" /> to specify the boundaries.
    /// </summary>
    public static T Rows<T>(this T value, FrameBoundary start, FrameBoundary end)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds a RANGE frame clause to the window. Translates to <c>RANGE BETWEEN start AND end</c>.
    /// Use <see cref="FrameBoundary" /> to specify the boundaries.
    /// </summary>
    public static T Range<T>(this T value, FrameBoundary start, FrameBoundary end)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds a GROUPS frame clause to the window. Translates to <c>GROUPS BETWEEN start AND end</c>.
    /// Use <see cref="FrameBoundary" /> to specify the boundaries.
    /// </summary>
    public static T Groups<T>(this T value, FrameBoundary start, FrameBoundary end)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }
}
