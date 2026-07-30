using System.Text.RegularExpressions;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Builds a canonical string for a select expression's SQL and parameter values, so two
/// expressions can be compared by content. Parameter placeholders are replaced with positional
/// markers and each parameter value is appended in a stable per-type text form.
/// </summary>
internal static partial class CteSqlCanonicalizer
{
    private static readonly Regex GeneratedIdentifierPattern = GeneratedIdentifierRegex();
    private static readonly Regex WhitespacePattern = WhitespaceRegex();

    public static string Canonicalize(SQLiteExpression node)
    {
        return Canonicalize(node, null);
    }

    public static string Canonicalize(SQLiteExpression node, Dictionary<string, string>? identifierMap)
    {
        string sql = node.ToString();
        SQLiteParameter[]? parameters = node.Parameters;
        if (parameters == null || parameters.Length == 0)
        {
            return NormalizeGeneratedIdentifiers(sql, identifierMap);
        }

        int[] byLongestName = Enumerable.Range(0, parameters.Length)
            .OrderByDescending(i => parameters[i].Name.Length)
            .ToArray();
        foreach (int i in byLongestName)
        {
            sql = sql.Replace(parameters[i].Name, $"?{i}", StringComparison.Ordinal);
        }

        sql = NormalizeGeneratedIdentifiers(sql, identifierMap);

        StringBuilder builder = StringBuilderPool.Rent();
        builder.Append(sql);
        foreach (SQLiteParameter parameter in parameters)
        {
            string value = CanonicalParameterValue(parameter.Value);
            builder.Append('\u001f');
            builder.Append(parameter.Value?.GetType().Name);
            builder.Append(';');
            builder.Append(value.Length);
            builder.Append(';');
            builder.Append(value);
        }

        return StringBuilderPool.ToStringAndReturn(builder);
    }

    public static void CollectGeneratedIdentifiers(string sql, HashSet<string> sink)
    {
        foreach (Match match in GeneratedIdentifierPattern.Matches(WhitespacePattern.Replace(sql, " ")))
        {
            sink.Add(match.Value);
        }
    }

    private static string NormalizeGeneratedIdentifiers(string sql, Dictionary<string, string>? identifierMap)
    {
        sql = WhitespacePattern.Replace(sql, " ");
        Dictionary<string, string>? seen = identifierMap;
        return GeneratedIdentifierPattern.Replace(sql, match =>
        {
            seen ??= new Dictionary<string, string>(StringComparer.Ordinal);
            if (!seen.TryGetValue(match.Value, out string? replacement))
            {
                replacement = "?i" + seen.Count.ToString(CultureInfo.InvariantCulture);
                seen[match.Value] = replacement;
            }

            return replacement;
        });
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
            IConvertible or IFormattable => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null",
            _ => "#" + value.GetHashCode().ToString(CultureInfo.InvariantCulture)
        };
    }

    [GeneratedRegex("(?<![\\w@\"$])[a-z]+[0-9]+\\b|\"[0-9]+\"")]
    private static partial Regex GeneratedIdentifierRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
