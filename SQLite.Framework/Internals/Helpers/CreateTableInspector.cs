namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Reads structure from a table's stored CREATE statement without preparing it, so a rebuild can
/// decide whether the table keeps a rowid. The scan skips string literals, comments and quoted
/// identifiers, so the text WITHOUT ROWID that only appears inside a check, a default, a comment
/// or a quoted name is not mistaken for the table clause.
/// </summary>
internal static class CreateTableInspector
{
    public static bool HasWithoutRowIdClause(string sql)
    {
        string previousWord = "";
        int i = 0;
        while (i < sql.Length)
        {
            char c = sql[i];
            if (c is '\'' or '"' or '`')
            {
                i = SkipQuoted(sql, i);
                previousWord = "";
            }
            else if (c == '[')
            {
                int close = sql.IndexOf(']', i + 1);
                i = close < 0 ? sql.Length : close + 1;
                previousWord = "";
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
            else if (char.IsLetterOrDigit(c) || c == '_' || c == '$')
            {
                int start = i;
                while (i < sql.Length && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_' || sql[i] == '$'))
                {
                    i++;
                }

                string word = sql[start..i];
                if (string.Equals(previousWord, "WITHOUT", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(word, "ROWID", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                previousWord = word;
            }
            else
            {
                if (!char.IsWhiteSpace(c))
                {
                    previousWord = "";
                }

                i++;
            }
        }

        return false;
    }

    private static int SkipQuoted(string sql, int start)
    {
        char quote = sql[start];
        int i = start + 1;
        while (i < sql.Length)
        {
            if (sql[i] == quote)
            {
                if (i + 1 < sql.Length && sql[i + 1] == quote)
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
}
