namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Reconciles a computed <see cref="DateTime.DayOfWeek" /> operand, which always reads as an
/// integer in SQL, with a stored <see cref="DayOfWeek" /> operand, which follows the enum
/// storage mode.
/// </summary>
internal static class DayOfWeekHelpers
{
    /// <summary>
    /// Returns true when the operand reads <c>DayOfWeek</c> from a date or time value.
    /// </summary>
    public static bool IsComputedDayOfWeek(Expression node)
    {
        Expression stripped = ExpressionHelpers.StripUpcast(ExpressionHelpers.StripQuotes(node));
        return stripped is MemberExpression { Member.Name: nameof(DateTime.DayOfWeek), Member.DeclaringType: { } declaring }
            && (declaring == typeof(DateTime) || declaring == typeof(DateTimeOffset) || declaring == typeof(DateOnly));
    }

    /// <summary>
    /// Rewrites a <see cref="DayOfWeek" /> operand to its integer form so it can be compared
    /// against a computed day of week.
    /// </summary>
    public static Expression ConvertOperandToInt(SQLiteOptions options, Expression node)
    {
        if (ExpressionHelpers.IsConstant(node))
        {
            return ExpressionHelpers.GetConstantValue(node) is DayOfWeek dayOfWeek
                ? Expression.Constant((int)dayOfWeek, typeof(int))
                : node;
        }

        Expression stripped = ExpressionHelpers.StripUpcast(ExpressionHelpers.StripQuotes(node));
        Type strippedType = Nullable.GetUnderlyingType(stripped.Type) ?? stripped.Type;
        if (options.EnumStorage == EnumStorageMode.Text
            && !IsComputedDayOfWeek(node)
            && strippedType == typeof(DayOfWeek))
        {
            return Expression.Convert(stripped, stripped.Type == strippedType ? typeof(int) : typeof(int?));
        }

        return node;
    }
}
