namespace SQLite.Framework.Internals.Helpers;

internal static class ParameterHelpers
{
    public static SQLiteParameter[]? CombineParameters(SQLiteExpression expression1, SQLiteExpression expression2)
    {
        SQLiteParameter[]? a = expression1.Parameters;
        SQLiteParameter[]? b = expression2.Parameters;

        if (a == null)
        {
            return b;
        }
        if (b == null)
        {
            return a;
        }

        SQLiteParameter[] result = new SQLiteParameter[a.Length + b.Length];
        Array.Copy(a, 0, result, 0, a.Length);
        Array.Copy(b, 0, result, a.Length, b.Length);
        return result;
    }

    public static SQLiteParameter[]? CombineParameters(SQLiteExpression expression1, SQLiteExpression expression2, SQLiteExpression expression3)
    {
        SQLiteParameter[]? a = expression1.Parameters;
        SQLiteParameter[]? b = expression2.Parameters;
        SQLiteParameter[]? c = expression3.Parameters;

        int aLen = a?.Length ?? 0;
        int bLen = b?.Length ?? 0;
        int cLen = c?.Length ?? 0;
        int total = aLen + bLen + cLen;

        if (total == 0)
        {
            return null;
        }

        SQLiteParameter[] result = new SQLiteParameter[total];
        int offset = 0;
        if (aLen > 0)
        {
            Array.Copy(a!, 0, result, offset, aLen);
            offset += aLen;
        }
        if (bLen > 0)
        {
            Array.Copy(b!, 0, result, offset, bLen);
            offset += bLen;
        }
        if (cLen > 0)
        {
            Array.Copy(c!, 0, result, offset, cLen);
        }
        return result;
    }

    public static SQLiteParameter[]? CombineParameters(params SQLiteExpression[] expressions)
    {
        return CombineParameters((IReadOnlyList<SQLiteExpression>)expressions);
    }

    public static SQLiteParameter[]? CombineParameters(IReadOnlyList<SQLiteExpression> expressions)
    {
        int count = expressions.Count;
        int total = 0;
        for (int i = 0; i < count; i++)
        {
            SQLiteParameter[]? p = expressions[i].Parameters;
            if (p != null)
            {
                total += p.Length;
            }
        }

        if (total == 0)
        {
            return null;
        }

        SQLiteParameter[] result = new SQLiteParameter[total];
        int offset = 0;
        for (int i = 0; i < count; i++)
        {
            SQLiteParameter[]? p = expressions[i].Parameters;
            if (p != null && p.Length > 0)
            {
                Array.Copy(p, 0, result, offset, p.Length);
                offset += p.Length;
            }
        }
        return result;
    }

    public static SQLiteParameter[]? CombineParametersFromModels(IReadOnlyList<ResolvedModel> models)
    {
        int count = models.Count;
        int total = 0;
        for (int i = 0; i < count; i++)
        {
            SQLiteParameter[]? p = models[i].SQLiteExpression?.Parameters;
            if (p != null)
            {
                total += p.Length;
            }
        }

        if (total == 0)
        {
            return null;
        }

        SQLiteParameter[] result = new SQLiteParameter[total];
        int offset = 0;
        for (int i = 0; i < count; i++)
        {
            SQLiteParameter[]? p = models[i].SQLiteExpression?.Parameters;
            if (p != null && p.Length > 0)
            {
                Array.Copy(p, 0, result, offset, p.Length);
                offset += p.Length;
            }
        }
        return result;
    }
}
