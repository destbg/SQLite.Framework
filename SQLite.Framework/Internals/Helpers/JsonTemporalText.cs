namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Formats a temporal value the way System.Text.Json writes it inside a JSON document, so a
/// query value can be compared against the elements of a JSON stored collection.
/// </summary>
internal static class JsonTemporalText
{
    public static bool TryFormat(object? value, out string? text)
    {
        switch (value)
        {
            case TimeSpan timeSpan:
                text = timeSpan.ToString("c", CultureInfo.InvariantCulture);
                return true;
            case DateTime dateTime:
                text = FormatDateTime(dateTime);
                return true;
            case DateTimeOffset dateTimeOffset:
                text = FormatDateTimeOffset(dateTimeOffset);
                return true;
            case DateOnly dateOnly:
                text = dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return true;
            case TimeOnly timeOnly:
                text = timeOnly.Ticks % TimeSpan.TicksPerSecond == 0
                    ? timeOnly.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                    : timeOnly.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
                return true;
            default:
                text = null;
                return false;
        }
    }

    private static string FormatDateTime(DateTime value)
    {
        string text = value.Ticks % TimeSpan.TicksPerSecond == 0
            ? value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
            : value.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);

        if (value.Kind == DateTimeKind.Utc)
        {
            return text + "Z";
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return text + value.ToString("zzz", CultureInfo.InvariantCulture);
        }

        return text;
    }

    private static string FormatDateTimeOffset(DateTimeOffset value)
    {
        string text = value.Ticks % TimeSpan.TicksPerSecond == 0
            ? value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
            : value.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);

        return text + value.ToString("zzz", CultureInfo.InvariantCulture);
    }
}
