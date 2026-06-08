using System.Text.RegularExpressions;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Inlines parameter values into SQL where the SQLite engine does not allow placeholders
/// (CHECK / Computed / partial-index expressions, view bodies, trigger bodies, and similar).
/// </summary>
internal static class SqlLiteralHelper
{
    public static string InlineParameters(string sql, IReadOnlyList<SQLiteParameter> parameters, SQLiteOptions options)
    {
        if (parameters.Count == 0)
        {
            return sql;
        }

        Dictionary<string, string> literals = new(parameters.Count);
        foreach (SQLiteParameter parameter in parameters)
        {
            literals[parameter.Name] = FormatLiteral(parameter.Value, options);
        }

        string pattern = string.Join("|", literals.Keys
            .OrderByDescending(name => name.Length)
            .Select(Regex.Escape));

        return Regex.Replace(sql, pattern, match => literals[match.Value]);
    }

    public static string FormatLiteral(object? value, SQLiteOptions options)
    {
        return value switch
        {
            DateTime dt => options.DateTimeStorage switch
            {
                DateTimeStorageMode.TextTicks => Format(dt.Ticks.ToString(CultureInfo.InvariantCulture)),
                DateTimeStorageMode.TextFormatted => Format(dt.ToString(options.DateTimeFormat, CultureInfo.InvariantCulture)),
                _ => Format(dt.Ticks)
            },
            DateTimeOffset dto => options.DateTimeOffsetStorage switch
            {
                DateTimeOffsetStorageMode.UtcTicks => Format(dto.UtcTicks),
                DateTimeOffsetStorageMode.TextFormatted => Format(dto.ToString(options.DateTimeOffsetFormat, CultureInfo.InvariantCulture)),
                _ => Format(dto.Ticks)
            },
            TimeSpan ts => options.TimeSpanStorage == TimeSpanStorageMode.Text
                ? Format(ts.ToString(options.TimeSpanFormat, CultureInfo.InvariantCulture))
                : Format(ts.Ticks),
            DateOnly d => options.DateOnlyStorage == DateOnlyStorageMode.Text
                ? Format(d.ToString(options.DateOnlyFormat, CultureInfo.InvariantCulture))
                : Format(d.ToDateTime(default).Ticks),
            TimeOnly t => options.TimeOnlyStorage == TimeOnlyStorageMode.Text
                ? Format(t.ToString(options.TimeOnlyFormat, CultureInfo.InvariantCulture))
                : Format(t.Ticks),
            decimal dec => options.DecimalStorage == DecimalStorageMode.Text
                ? Format(dec.ToString(options.DecimalFormat, CultureInfo.InvariantCulture))
                : Format((double)dec),
            Enum e => options.EnumStorage == EnumStorageMode.Text
                ? Format(e.ToString())
                : Format(Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType()), CultureInfo.InvariantCulture)),
            Guid g => Format(g.ToString()),
            char c => options.CharStorage == CharStorageMode.Integer ? Format((int)c) : Format(c.ToString()),
            byte[] bytes => "X'" + Convert.ToHexString(bytes) + "'",
            _ => Format(value)
        };
    }

    private static string Format(object? value)
    {
        return value switch
        {
            null => "NULL",
            bool b => b ? "1" : "0",
            string s => "'" + s.Replace("'", "''") + "'",
            byte b => b.ToString(CultureInfo.InvariantCulture),
            sbyte b => b.ToString(CultureInfo.InvariantCulture),
            short b => b.ToString(CultureInfo.InvariantCulture),
            ushort b => b.ToString(CultureInfo.InvariantCulture),
            int b => b.ToString(CultureInfo.InvariantCulture),
            uint b => b.ToString(CultureInfo.InvariantCulture),
            long b => b.ToString(CultureInfo.InvariantCulture),
            ulong b => unchecked((long)b).ToString(CultureInfo.InvariantCulture),
            float f => f.ToString("R", CultureInfo.InvariantCulture),
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            _ => throw new NotSupportedException(
                $"Cannot inline value of type {value.GetType().Name} as a SQL literal. Use a simple constant in CHECK / Computed / partial-index / view / trigger expressions, or build the DDL with raw SQL."),
        };
    }
}
