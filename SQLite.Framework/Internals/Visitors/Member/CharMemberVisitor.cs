namespace SQLite.Framework.Internals.Visitors.Member;

internal static class CharMemberVisitor
{
    public static Expression HandleCharMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (arguments.Count > 0 && arguments[0].SQLiteExpression != null)
        {
            SQLiteExpression a0 = arguments[0].SQLiteExpression!;
            SQLiteParameter[]? parameters = arguments[0].Parameters;
            Type returnType = node.Method.ReturnType;
            switch (node.Method.Name)
            {
                case nameof(char.ToLower):
                    return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "LOWER(", a0, ")", parameters);
                case nameof(char.ToUpper):
                    return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "UPPER(", a0, ")", parameters);
                case nameof(char.IsWhiteSpace):
                    return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "TRIM(", a0, ") = ''", parameters);
                case nameof(char.IsAsciiDigit):
                    return SQLiteExpression.Binary(returnType, visitor.Counters.NextIdentifier(), "(", a0, " >= '0' AND ", a0, " <= '9')", parameters);
                case nameof(char.IsAsciiLetter):
                    return SQLiteExpression.Multi(returnType, visitor.Counters.NextIdentifier(),
                        ["((", " >= 'a' AND ", " <= 'z') OR (", " >= 'A' AND ", " <= 'Z'))"],
                        [a0, a0, a0, a0], parameters);
                case nameof(char.IsAsciiLetterOrDigit):
                    return SQLiteExpression.Multi(returnType, visitor.Counters.NextIdentifier(),
                        ["((", " >= '0' AND ", " <= '9') OR (", " >= 'a' AND ", " <= 'z') OR (", " >= 'A' AND ", " <= 'Z'))"],
                        [a0, a0, a0, a0, a0, a0], parameters);
                case nameof(char.IsAsciiLetterLower):
                    return SQLiteExpression.Binary(returnType, visitor.Counters.NextIdentifier(), "(", a0, " >= 'a' AND ", a0, " <= 'z')", parameters);
                case nameof(char.IsAsciiLetterUpper):
                    return SQLiteExpression.Binary(returnType, visitor.Counters.NextIdentifier(), "(", a0, " >= 'A' AND ", a0, " <= 'Z')", parameters);
            }
        }

        if (QueryableMemberVisitor.CheckConstantMethod<char>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        throw new NotSupportedException($"char.{node.Method.Name} is not translatable to SQL.");
    }
}
