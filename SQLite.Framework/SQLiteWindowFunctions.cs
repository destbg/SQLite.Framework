namespace SQLite.Framework;

/// <summary>
/// Marker methods for SQLite window functions.
/// They throw at runtime and only work inside a LINQ query, where the translator turns them into SQL.
/// Each method returns <see cref="SQLiteWindow{T}" /> so you can chain PartitionBy, OrderBy or frame methods.
/// Without any chain method, the result becomes <c>function(...) OVER ()</c>.
/// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
[UnsupportedOSPlatform("ios")]
[SupportedOSPlatform("ios13.0")]
#endif
public static class SQLiteWindowFunctions
{
    /// <summary>
    /// Computes the sum of the values in the window. Translates to <c>SUM(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<T> Sum<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of the values in the window. Translates to <c>AVG(value) OVER (...)</c>.
    /// For an integer column use the <see cref="Avg(int)" /> or <see cref="Avg(long)" /> overloads, which
    /// return a <see cref="double" /> window so the fraction is kept, matching LINQ <c>Average</c>.
    /// </summary>
    public static SQLiteWindow<T> Avg<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of an <see cref="int" /> column over the window as a <see cref="double" />,
    /// so the fractional result is kept. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double> Avg(int value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a <see cref="long" /> column over the window as a <see cref="double" />,
    /// so the fractional result is kept. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double> Avg(long value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a nullable <see cref="int" /> column over the window as a nullable
    /// <see cref="double" />. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double?> Avg(int? value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a nullable <see cref="long" /> column over the window as a nullable
    /// <see cref="double" />. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double?> Avg(long? value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a <see cref="short" /> column over the window as a <see cref="double" />,
    /// so the fractional result is kept. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double> Avg(short value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a <see cref="byte" /> column over the window as a <see cref="double" />,
    /// so the fractional result is kept. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double> Avg(byte value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of an <see cref="sbyte" /> column over the window as a <see cref="double" />,
    /// so the fractional result is kept. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double> Avg(sbyte value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a <see cref="ushort" /> column over the window as a <see cref="double" />,
    /// so the fractional result is kept. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double> Avg(ushort value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a <see cref="uint" /> column over the window as a <see cref="double" />,
    /// so the fractional result is kept. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double> Avg(uint value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a <see cref="ulong" /> column over the window as a <see cref="double" />,
    /// so the fractional result is kept. Translates to <c>AVG(value) OVER (...)</c>. A <c>ulong</c> value
    /// at or above 2 to the power 63 is stored signed, so the average of such values is not exact. See
    /// the Limitations page.
    /// </summary>
    public static SQLiteWindow<double> Avg(ulong value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a nullable <see cref="short" /> column over the window as a nullable
    /// <see cref="double" />. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double?> Avg(short? value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a nullable <see cref="byte" /> column over the window as a nullable
    /// <see cref="double" />. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double?> Avg(byte? value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a nullable <see cref="sbyte" /> column over the window as a nullable
    /// <see cref="double" />. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double?> Avg(sbyte? value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a nullable <see cref="ushort" /> column over the window as a nullable
    /// <see cref="double" />. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double?> Avg(ushort? value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a nullable <see cref="uint" /> column over the window as a nullable
    /// <see cref="double" />. Translates to <c>AVG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double?> Avg(uint? value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Computes the average of a nullable <see cref="ulong" /> column over the window as a nullable
    /// <see cref="double" />. Translates to <c>AVG(value) OVER (...)</c>. A <c>ulong</c> value at or
    /// above 2 to the power 63 is stored signed, so the average of such values is not exact.
    /// </summary>
    public static SQLiteWindow<double?> Avg(ulong? value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the minimum value in the window. Translates to <c>MIN(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<T> Min<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the maximum value in the window. Translates to <c>MAX(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<T> Max<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Counts all rows in the window. Translates to <c>COUNT(*) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<long> Count()
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Counts non-null values in the window. Translates to <c>COUNT(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<long> Count<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the sequential row number within the window partition. Translates to <c>ROW_NUMBER() OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<long> RowNumber()
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the rank of the current row with gaps for ties. Translates to <c>RANK() OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<long> Rank()
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the rank of the current row without gaps for ties. Translates to <c>DENSE_RANK() OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<long> DenseRank()
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the relative rank of the current row as a value between 0 and 1. Translates to <c>PERCENT_RANK() OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double> PercentRank()
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the cumulative distribution of the current row within its partition. Translates to <c>CUME_DIST() OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<double> CumeDist()
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Divides the partition into the specified number of buckets and assigns a bucket number to each row.
    /// Translates to <c>NTILE(buckets) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<long> NTile(long buckets)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value from the previous row in the window. Translates to <c>LAG(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<T> Lag<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value from a row <paramref name="offset" /> rows before the current row.
    /// Translates to <c>LAG(value, offset) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<T> Lag<T>(T value, long offset)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value from a row <paramref name="offset" /> rows before the current row
    /// or <paramref name="defaultValue" /> if no such row exists. Translates to <c>LAG(value, offset, default) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<T> Lag<T>(T value, long offset, T defaultValue)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value from the next row in the window. Translates to <c>LEAD(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<T> Lead<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value from a row <paramref name="offset" /> rows after the current row.
    /// Translates to <c>LEAD(value, offset) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<T> Lead<T>(T value, long offset)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value from a row <paramref name="offset" /> rows after the current row
    /// or <paramref name="defaultValue" /> if no such row exists. Translates to <c>LEAD(value, offset, default) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<T> Lead<T>(T value, long offset, T defaultValue)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value of the first row in the window frame. Translates to <c>FIRST_VALUE(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<T> FirstValue<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value of the last row in the window frame. Translates to <c>LAST_VALUE(value) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<T> LastValue<T>(T value)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Returns the value of the <paramref name="n" />th row in the window frame (1-based).
    /// Translates to <c>NTH_VALUE(value, n) OVER (...)</c>.
    /// </summary>
    public static SQLiteWindow<T> NthValue<T>(T value, long n)
    {
        throw new InvalidOperationException("SQLiteWindowFunctions methods can only be used inside a LINQ query.");
    }
}
