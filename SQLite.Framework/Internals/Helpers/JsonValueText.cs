namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Formats a query value the way the JSON serializer writes it inside a JSON document, so an
/// <c>IN</c> clause over a JSON sourced value compares against the stored text form. Covers
/// temporal values through <see cref="JsonTemporalText"/> and string stored enums through
/// <see cref="JsonEnumText"/>.
/// </summary>
internal static class JsonValueText
{
    public static object? NormalizeInValue(SQLiteOptions options, bool isJsonSource, object? value)
    {
        if (isJsonSource
            && (JsonTemporalText.TryFormat(value, out string? text) || JsonEnumText.TryFormat(options, value, out text)))
        {
            return text;
        }

        return value;
    }
}
