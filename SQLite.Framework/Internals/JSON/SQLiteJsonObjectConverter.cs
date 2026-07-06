namespace SQLite.Framework.Internals.JSON;

/// <summary>
/// Non-generic JSON converter used by <c>AddJsonConverters</c> and <c>AddJsonbConverters</c>
/// to register nested property types.
/// </summary>
internal sealed class SQLiteJsonObjectConverter : ISQLiteTypeConverter, IJsonTypeInfoSource
{
    private readonly bool isJsonb;

    public SQLiteJsonObjectConverter(JsonTypeInfo typeInfo, bool isJsonb)
    {
        TypeInfo = typeInfo;
        this.isJsonb = isJsonb;
    }

    public SQLiteColumnType ColumnType => isJsonb ? SQLiteColumnType.Blob : SQLiteColumnType.Text;

    public JsonTypeInfo TypeInfo { get; }

    public string? ParameterSqlExpression => isJsonb ? "jsonb({0})" : null;

    public string? ColumnSqlExpression => isJsonb ? "json({0})" : null;

    public object? ToDatabase(object? value)
    {
        return value == null ? null : JsonSerializer.Serialize(value, TypeInfo);
    }

    public object? FromDatabase(object? value)
    {
        return value is string s ? JsonSerializer.Deserialize(s, TypeInfo) : null;
    }
}
