namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Parses the lambda expressions used by <see cref="UpsertBuilder{T}" /> down to the .NET
/// property names of the chosen columns. The mapping from property name to the SQL column name
/// happens later, in <see cref="UpsertSqlBuilder" />, using the entity's
/// <see cref="TableMapping" />.
/// </summary>
internal static class UpsertExpressionParser
{
    public static IReadOnlyList<string> ResolveConflictColumns<T, TKey>(Expression<Func<T, TKey>> conflictTarget)
    {
        Expression body = StripConvert(conflictTarget.Body);

        if (body is MemberExpression me)
        {
            return [me.Member.Name];
        }

        if (body is NewExpression ne && ne.Arguments.Count > 0)
        {
            string[] names = new string[ne.Arguments.Count];
            for (int i = 0; i < ne.Arguments.Count; i++)
            {
                Expression arg = StripConvert(ne.Arguments[i]);
                if (arg is not MemberExpression argMember)
                {
                    throw new NotSupportedException($"OnConflict expects a property reference like b.Id or new {{ b.A, b.B }}. Argument {i} is not a member access: {arg}.");
                }
                names[i] = argMember.Member.Name;
            }
            return names;
        }

        throw new NotSupportedException($"OnConflict expects a property reference like b.Id or new {{ b.A, b.B }}. Got: {body}.");
    }

    public static IReadOnlyList<string> ResolveColumnList<T>(Expression<Func<T, object?>>[] columns)
    {
        string[] names = new string[columns.Length];
        for (int i = 0; i < columns.Length; i++)
        {
            Expression body = StripConvert(columns[i].Body);
            if (body is not MemberExpression me)
            {
                throw new NotSupportedException($"DoUpdate expects property references like b.Title, b.Price. Argument {i} is not a member access: {body}.");
            }
            names[i] = me.Member.Name;
        }
        return names;
    }

    private static Expression StripConvert(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert } u)
        {
            expression = u.Operand;
        }
        return expression;
    }
}
