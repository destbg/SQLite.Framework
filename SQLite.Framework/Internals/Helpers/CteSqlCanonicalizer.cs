namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Builds a canonical string for a select expression's SQL and parameter values, so two
/// expressions can be compared by content. Parameter placeholders are replaced with positional
/// markers and each parameter value is appended in a stable per-type text form.
/// </summary>
internal static class CteSqlCanonicalizer
{
    public static string Canonicalize(SQLiteExpression node)
    {
        string sql = node.ToString();
        SQLiteParameter[]? parameters = node.Parameters;
        if (parameters == null || parameters.Length == 0)
        {
            return sql;
        }

        int[] byLongestName = Enumerable.Range(0, parameters.Length)
            .OrderByDescending(i => parameters[i].Name.Length)
            .ToArray();
        foreach (int i in byLongestName)
        {
            sql = sql.Replace(parameters[i].Name, $"?{i}", StringComparison.Ordinal);
        }

        StringBuilder builder = StringBuilderPool.Rent();
        builder.Append(sql);
        foreach (SQLiteParameter parameter in parameters)
        {
            string value = CanonicalParameterValue(parameter.Value);
            builder.Append('');
            builder.Append(parameter.Value?.GetType().Name);
            builder.Append(';');
            builder.Append(value.Length);
            builder.Append(';');
            builder.Append(value);
        }

        return StringBuilderPool.ToStringAndReturn(builder);
    }

    private static string CanonicalParameterValue(object? value)
    {
        return value switch
        {
            null => "null",
            TimeOnly time => time.Ticks.ToString(CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.Ticks.ToString(CultureInfo.InvariantCulture),
            DateTimeOffset offset => offset.UtcTicks.ToString(CultureInfo.InvariantCulture) + "+" + offset.Offset.Ticks.ToString(CultureInfo.InvariantCulture),
            TimeSpan span => span.Ticks.ToString(CultureInfo.InvariantCulture),
            DateOnly date => date.DayNumber.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            float f => f.ToString("R", CultureInfo.InvariantCulture),
            byte[] blob => Convert.ToHexString(blob),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null"
        };
    }
}
