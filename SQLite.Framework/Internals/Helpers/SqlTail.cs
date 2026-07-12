namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Scans SQL text to find where the significant statement content ends, so trailing whitespace,
/// comments and statement separators can be recognized without preparing the text. SQLite line
/// comments run from <c>--</c> to the end of the line and block comments run from <c>/*</c> to
/// <c>*/</c> or, when unterminated, to the end of the text.
/// </summary>
internal static class SqlTail
{
    /// <summary>
    /// Returns true when <paramref name="sql" /> still holds a statement to run. Trailing
    /// whitespace, SQL comments and empty statements (bare semicolons) do not count, so a batch
    /// or query tail made of only those is treated as finished rather than as another statement.
    /// </summary>
    public static bool HasStatement(string? sql)
    {
        return sql != null && SignificantLength(sql) > 0;
    }

    /// <summary>
    /// Removes trailing semicolons, whitespace and comments from <paramref name="sql" /> so the
    /// remainder can be embedded as a subquery. Semicolons, comment markers and whitespace inside
    /// string literals or quoted identifiers are kept.
    /// </summary>
    public static string TrimStatementTail(string sql)
    {
        return sql[..SignificantLength(sql)];
    }

    /// <summary>
    /// Returns true when <paramref name="sql" /> holds more than one statement, meaning a
    /// statement-separating semicolon outside string literals, quoted identifiers and comments
    /// has significant content on both sides.
    /// </summary>
    public static bool HasMultipleStatements(string sql)
    {
        bool seenContent = false;
        int i = 0;
        while (i < sql.Length)
        {
            char c = sql[i];
            if (c == ';')
            {
                if (seenContent && HasStatement(sql[(i + 1)..]))
                {
                    return true;
                }

                i++;
            }
            else if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                while (i < sql.Length && sql[i] != '\n')
                {
                    i++;
                }
            }
            else if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                int close = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = close < 0 ? sql.Length : close + 2;
            }
            else if (c == '\'' || c == '"' || c == '`')
            {
                i = SkipQuoted(sql, i + 1, c);
                seenContent = true;
            }
            else if (c == '[')
            {
                int close = sql.IndexOf(']', i + 1);
                i = close < 0 ? sql.Length : close + 1;
                seenContent = true;
            }
            else
            {
                i++;
                seenContent = true;
            }
        }

        return false;
    }

    private static int SignificantLength(string sql)
    {
        int end = 0;
        int i = 0;
        while (i < sql.Length)
        {
            char c = sql[i];
            if (char.IsWhiteSpace(c) || c == ';')
            {
                i++;
            }
            else if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                while (i < sql.Length && sql[i] != '\n')
                {
                    i++;
                }
            }
            else if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                int close = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = close < 0 ? sql.Length : close + 2;
            }
            else if (c == '\'' || c == '"' || c == '`')
            {
                i = SkipQuoted(sql, i + 1, c);
                end = i;
            }
            else if (c == '[')
            {
                int close = sql.IndexOf(']', i + 1);
                i = close < 0 ? sql.Length : close + 1;
                end = i;
            }
            else
            {
                i++;
                end = i;
            }
        }

        return end;
    }

    private static int SkipQuoted(string sql, int start, char quote)
    {
        int i = start;
        while (i < sql.Length)
        {
            if (sql[i] != quote)
            {
                i++;
            }
            else if (i + 1 < sql.Length && sql[i + 1] == quote)
            {
                i += 2;
            }
            else
            {
                return i + 1;
            }
        }

        return i;
    }
}
