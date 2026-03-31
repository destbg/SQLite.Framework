using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SQLite.Framework.Enums;

namespace SQLite.Framework.JsonB;

/// <summary>
/// Stores a .NET object as a JSON text string in a TEXT column.
/// Pass a <see cref="JsonTypeInfo{T}" /> from a <c>JsonSerializerContext</c> to keep the converter AOT-safe.
/// </summary>
public class SQLiteJsonConverter<T>(JsonTypeInfo<T> typeInfo) : ISQLiteTypeConverter
{
    /// <inheritdoc />
    public Type Type => typeof(T);

    /// <inheritdoc />
    public SQLiteColumnType ColumnType => SQLiteColumnType.Text;

    /// <inheritdoc />
    public object? ToDatabase(object? value)
    {
        return value is T t ? JsonSerializer.Serialize(t, typeInfo) : null;
    }

    /// <inheritdoc />
    public object? FromDatabase(object? value)
    {
        return value is string s ? JsonSerializer.Deserialize(s, typeInfo) : default;
    }
}