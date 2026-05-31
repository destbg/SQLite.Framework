using System.Text.RegularExpressions;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Inlines parameter values into SQL where the SQLite engine does not allow placeholders
/// (CHECK / Computed / partial-index expressions, view bodies, trigger bodies, and similar).
/// </summary>
internal static class SqlLiteralHelper
{
    public static string InlineParameters(string sql, IReadOnlyList<SQLiteParameter> parameters)
    {
        if (parameters.Count == 0)
        {
            return sql;
        }

        Dictionary<string, string> literals = new(parameters.Count);
        foreach (SQLiteParameter parameter in parameters)
        {
            literals[parameter.Name] = FormatLiteral(parameter.Value);
        }

        string pattern = string.Join("|", literals.Keys
            .OrderByDescending(name => name.Length)
            .Select(Regex.Escape));

        return Regex.Replace(sql, pattern, match => literals[match.Value]);
    }

    public static string FormatLiteral(object? value)
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
            ulong b => b.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString("R", CultureInfo.InvariantCulture),
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            decimal m => m.ToString(CultureInfo.InvariantCulture),
            Enum e => FormatLiteral(Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType()), CultureInfo.InvariantCulture)),
            _ => throw new NotSupportedException(
                $"Cannot inline value of type {value.GetType().Name} as a SQL literal. Use a simple constant in CHECK / Computed / partial-index / view / trigger expressions, or build the DDL with raw SQL."),
        };
    }

    public static string FormatLiteral(object? value, SQLiteOptions options)
    {
        return value switch
        {
            DateTime dt => options.DateTimeStorage switch
            {
                DateTimeStorageMode.TextTicks => FormatLiteral(dt.Ticks.ToString(CultureInfo.InvariantCulture)),
                DateTimeStorageMode.TextFormatted => FormatLiteral(dt.ToString(options.DateTimeFormat, CultureInfo.InvariantCulture)),
                _ => FormatLiteral(dt.Ticks)
            },
            DateTimeOffset dto => options.DateTimeOffsetStorage switch
            {
                DateTimeOffsetStorageMode.UtcTicks => FormatLiteral(dto.UtcTicks),
                DateTimeOffsetStorageMode.TextFormatted => FormatLiteral(dto.ToString(options.DateTimeOffsetFormat, CultureInfo.InvariantCulture)),
                _ => FormatLiteral(dto.Ticks)
            },
            TimeSpan ts => options.TimeSpanStorage == TimeSpanStorageMode.Text
                ? FormatLiteral(ts.ToString(options.TimeSpanFormat, CultureInfo.InvariantCulture))
                : FormatLiteral(ts.Ticks),
            DateOnly d => options.DateOnlyStorage == DateOnlyStorageMode.Text
                ? FormatLiteral(d.ToString(options.DateOnlyFormat, CultureInfo.InvariantCulture))
                : FormatLiteral(d.ToDateTime(default).Ticks),
            TimeOnly t => options.TimeOnlyStorage == TimeOnlyStorageMode.Text
                ? FormatLiteral(t.ToString(options.TimeOnlyFormat, CultureInfo.InvariantCulture))
                : FormatLiteral(t.Ticks),
            decimal dec => options.DecimalStorage == DecimalStorageMode.Text
                ? FormatLiteral(dec.ToString(options.DecimalFormat, CultureInfo.InvariantCulture))
                : FormatLiteral((double)dec),
            Enum e => options.EnumStorage == EnumStorageMode.Text
                ? FormatLiteral(e.ToString())
                : FormatLiteral(Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType()), CultureInfo.InvariantCulture)),
            Guid g => FormatLiteral(g.ToString()),
            char c => FormatLiteral(c.ToString()),
            byte[] bytes => "X'" + Convert.ToHexString(bytes) + "'",
            _ => FormatLiteral(value)
        };
    }
}
