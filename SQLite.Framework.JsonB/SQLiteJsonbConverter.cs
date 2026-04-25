using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SQLite.Framework.Enums;

namespace SQLite.Framework.JsonB;

/// <summary>
/// Stores a .NET object as a JSONB binary blob using SQLite's built-in <c>jsonb()</c> function.
/// The column is declared as BLOB. Values are written via <c>jsonb(?)</c> and read back via <c>json(?)</c>,
/// so SQLite handles all encoding and decoding.
/// Pass a <see cref="JsonTypeInfo{T}" /> from a <c>JsonSerializerContext</c> to keep the converter AOT-safe.
/// </summary>
public class SQLiteJsonbConverter<T> : ISQLiteTypeConverter
{
    private readonly JsonTypeInfo<T> typeInfo;

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
