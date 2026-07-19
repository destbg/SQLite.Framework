namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Compares two schema object definitions from <c>sqlite_master</c> by token content instead of
/// raw text, so quoting style, identifier case, whitespace, comments and an <c>IF NOT EXISTS</c>
/// clause do not count as drift while a direction, collation or filter change still does. Bare
/// words and quoted identifiers compare without case and string literals compare exactly.
/// </summary>
internal static class SchemaSqlNormalizer
{
    public static bool AreEquivalent(string expectedSql, string? actualSql)
    {
        if (actualSql == null)
        {
            return false;
        }

        return Tokenize(expectedSql).SequenceEqual(Tokenize(actualSql), StringComparer.Ordinal);
    }

    private static List<string> Tokenize(string sql)
    {
        List<string> tokens = [];
        bool literalList = false;
        int i = 0;
        while (i < sql.Length)
        {
            char c = sql[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (c == '\'')
            {
                int close = FindClosingQuote(sql, i);
                if (close < 0)
                {
                    tokens.Add(sql[i..]);
                    i = sql.Length;
                    continue;
                }

                string body = sql[(i + 1)..close];
                tokens.Add(IsLiteralContext(tokens, literalList)
                    ? sql[i..(close + 1)]
                    : body.Replace("''", "'", StringComparison.Ordinal).ToLowerInvariant());
                i = close + 1;
            }
            else if (c is '"' or '`')
            {
                int close = FindClosingQuote(sql, i);
                string body = close < 0 ? sql[(i + 1)..] : sql[(i + 1)..close];
                tokens.Add(body.Replace(new string(c, 2), new string(c, 1), StringComparison.Ordinal).ToLowerInvariant());
                i = close < 0 ? sql.Length : close + 1;
            }
            else if (c == '[')
            {
                int close = sql.IndexOf(']', i + 1);
                string body = close < 0 ? sql[(i + 1)..] : sql[(i + 1)..close];
                tokens.Add(body.ToLowerInvariant());
                i = close < 0 ? sql.Length : close + 1;
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

                tokens.Add(sql[start..i].ToLowerInvariant());
            }
            else
            {
                if (c == '(' && tokens.Count > 0 && tokens[^1] is "values" or "in")
                {
                    literalList = true;
                }
                else if (c == ')')
                {
                    literalList = false;
                }

                tokens.Add(sql[i].ToString());
                i++;
            }
        }

        RemoveExistsClause(tokens);
        return tokens;
    }

    private static bool IsLiteralContext(List<string> tokens, bool literalList)
    {
        if (literalList || tokens.Count == 0)
        {
            return literalList;
        }

        return tokens[^1] is "=" or "<" or ">"
            or "is" or "like" or "glob" or "regexp" or "match" or "between" or "default"
            or "select" or "then" or "else" or "case" or "and" or "or" or "not" or "where" or "when"
            or "+" or "-" or "*" or "/" or "%";
    }

    private static void RemoveExistsClause(List<string> tokens)
    {
        for (int i = 0; i + 2 < tokens.Count; i++)
        {
            if (tokens[i] == "if" && tokens[i + 1] == "not" && tokens[i + 2] == "exists")
            {
                tokens.RemoveRange(i, 3);
                return;
            }
        }
    }

    private static int FindClosingQuote(string sql, int start)
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

                return i;
            }

            i++;
        }

        return -1;
    }
}
