namespace SQLite.Framework;

/// <summary>
/// Stores a .NET object as a JSONB binary blob using SQLite's built-in <c>jsonb()</c> function.
/// The column is declared as BLOB. Values are written via <c>jsonb(?)</c> and read back via <c>json(?)</c>,
/// so SQLite handles all encoding and decoding.
/// Pass a <see cref="JsonTypeInfo{T}" /> from a <c>JsonSerializerContext</c> to keep the converter AOT-safe.
/// </summary>
#if SQLITECIPHER
[Obsolete("JSONB is not available in SQLCipher's bundled SQLite. Use SQLite.Framework or SQLite.Framework.Bundled if you need JSONB support; otherwise switch to SQLiteJsonConverter<T> for plain JSON TEXT storage.", error: true)]
#elif SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
[SupportedOSPlatform("android36.0")]
[UnsupportedOSPlatform("ios")]
#endif
public class SQLiteJsonbConverter<T> : ISQLiteTypeConverter
{
    private readonly JsonTypeInfo<T> typeInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteJsonbConverter{T}" /> class.
    /// </summary>
    public SQLiteJsonbConverter(JsonTypeInfo<T> typeInfo)
    {
        this.typeInfo = typeInfo;
    }

    /// <inheritdoc />
    public SQLiteColumnType ColumnType => SQLiteColumnType.Blob;

    /// <inheritdoc />
    public string ParameterSqlExpression => "jsonb({0})";

    /// <inheritdoc />
    public string ColumnSqlExpression => "json({0})";

    /// <inheritdoc />
    public object? ToDatabase(object? value)
    {
        if (value is not T t)
        {
            return null;
        }

        return JsonSerializer.Serialize(t, typeInfo);
    }

    /// <inheritdoc />
    public object? FromDatabase(object? value)
    {
        if (value is not string s)
        {
            return default(T);
        }

        return JsonSerializer.Deserialize(s, typeInfo);
    }
}
