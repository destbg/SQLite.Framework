namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Applies a registered converter's <see cref="ISQLiteTypeConverter.ParameterSqlExpression" /> wrap
/// (for example <c>jsonb({0})</c>) around a placeholder or inlined literal when a value of a given
/// CLR type is written to a column. The lookup strips <see cref="Nullable{T}" />, so a nullable
/// value-type property still finds the converter registered for its underlying type.
/// </summary>
internal static class ConverterSql
{
    public static string WrapParameter(string placeholder, Type valueType, SQLiteOptions options)
    {
        return TryGetWrap(valueType, options, out string? paramExpr)
            ? string.Format(paramExpr, placeholder)
            : placeholder;
    }

    public static string WrapDefault(string literal, Type valueType, SQLiteOptions options)
    {
        return TryGetWrap(valueType, options, out string? paramExpr)
            ? "(" + string.Format(paramExpr, literal) + ")"
            : literal;
    }

    private static bool TryGetWrap(Type valueType, SQLiteOptions options, [NotNullWhen(true)] out string? paramExpr)
    {
        Type lookupType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        if (options.TypeConverters.TryGetValue(lookupType, out ISQLiteTypeConverter? converter)
            && converter.ParameterSqlExpression is { } expr)
        {
            paramExpr = expr;
            return true;
        }

        paramExpr = null;
        return false;
    }
}
