namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Formats an enum value the way the registered JSON serializer writes it inside a JSON document.
/// Applies only when the serializer writes the enum as a JSON string, for example through
/// <c>JsonStringEnumConverter</c>, so a query value matches the stored member name.
/// </summary>
internal static class JsonEnumText
{
    public static object? NormalizeInValue(SQLiteOptions options, bool isJsonSource, object? value)
    {
        if (isJsonSource && TryFormat(options, value, out string? text))
        {
            return text;
        }

        return value;
    }

    public static bool IsStringStored(SQLiteOptions options, Type enumType)
    {
        Array values = Enum.GetValuesAsUnderlyingType(enumType);
        if (values.Length == 0)
        {
            return false;
        }

        object value = Enum.ToObject(enumType, values.GetValue(0)!);
        return TryFormat(options, value, out _);
    }

    public static bool TryFormat(SQLiteOptions options, object? value, out string? text)
    {
        text = null;
        if (value is not Enum)
        {
            return false;
        }

        if (options.ResolveJsonTypeInfo(value.GetType()) is not { } info)
        {
            return false;
        }

        string json = JsonSerializer.Serialize(value, info);
        if (json[0] != '"')
        {
            return false;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        text = document.RootElement.GetString();
        return true;
    }
}
