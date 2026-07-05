namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Inlines parameter values into SQL where the SQLite engine does not allow placeholders
/// (CHECK / Computed / partial-index expressions, view bodies, trigger bodies and similar).
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

        List<string> names = literals.Keys.OrderByDescending(name => name.Length).ToList();
        StringBuilder result = new(sql.Length);
        int i = 0;
        while (i < sql.Length)
        {
            char c = sql[i];
            if (c is '\'' or '"' or '`' or '[')
            {
                int end = SkipQuoted(sql, i);
                result.Append(sql, i, end - i);
                i = end;
                continue;
            }

            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                int end = sql.IndexOf('\n', i);
                end = end < 0 ? sql.Length : end;
                result.Append(sql, i, end - i);
                i = end;
                continue;
            }

            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                int end = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? sql.Length : end + 2;
                result.Append(sql, i, end - i);
                i = end;
                continue;
            }

            string? matched = null;
            foreach (string name in names)
            {
                if (i + name.Length <= sql.Length && string.CompareOrdinal(sql, i, name, 0, name.Length) == 0)
                {
                    matched = name;
                    break;
                }
            }

            if (matched != null)
            {
                result.Append(literals[matched]);
                i += matched.Length;
                continue;
            }

            result.Append(c);
            i++;
        }

        return result.ToString();
    }

    private static int SkipQuoted(string sql, int start)
    {
        char open = sql[start];
        char close = open == '[' ? ']' : open;
        int i = start + 1;
        while (i < sql.Length)
        {
            if (sql[i] == close)
            {
                if (close != ']' && i + 1 < sql.Length && sql[i + 1] == close)
                {
                    i += 2;
                    continue;
                }

                return i + 1;
            }

            i++;
        }

        return sql.Length;
    }

    public static string FormatLiteral(object? value, SQLiteOptions options)
    {
        if (value != null && options.TypeConverters.TryGetValue(value.GetType(), out ISQLiteTypeConverter? converter))
        {
            value = converter.ToDatabase(value);
        }

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
            float f => FormatReal(f),
            double d => FormatReal(d),
            _ => throw new NotSupportedException(
                $"Cannot inline value of type {value.GetType().Name} as a SQL literal. Use a simple constant in CHECK / Computed / partial-index / view / trigger expressions or build the DDL with raw SQL."),
        };
    }

    private static string FormatReal(double value)
    {
        if (double.IsNaN(value))
        {
            return "NULL";
        }

        if (double.IsPositiveInfinity(value))
        {
            return "9e999";
        }

        if (double.IsNegativeInfinity(value))
        {
            return "-9e999";
        }

        string text = value.ToString("R", CultureInfo.InvariantCulture);
        return text.IndexOfAny(['.', 'e', 'E']) < 0 ? text + ".0" : text;
    }
}
