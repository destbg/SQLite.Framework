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
            bool integerChar = visitor.Database.Options.CharStorage == CharStorageMode.Integer;
            SQLiteExpression a0t = integerChar
                ? SQLiteExpression.Wrap(typeof(char), visitor.Counters.NextIdentifier(), "CHAR(", a0, ")", null)
                : a0;
            switch (node.Method.Name)
            {
                case nameof(char.ToLower):
                    return integerChar
                        ? SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "UNICODE(LOWER(CHAR(", a0, ")))", parameters)
                        : SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "LOWER(", a0, ")", parameters);
                case nameof(char.ToUpper):
                    return integerChar
                        ? SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "UNICODE(UPPER(CHAR(", a0, ")))", parameters)
                        : SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "UPPER(", a0, ")", parameters);
                case nameof(char.IsWhiteSpace):
                    return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "TRIM(", a0t, $", {Constants.WhitespaceChars}) = ''", parameters);
                case nameof(char.IsAsciiDigit):
                    return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "(", a0t, " BETWEEN '0' AND '9')", parameters);
                case nameof(char.IsAsciiLetter):
                    return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "(LOWER(", a0t, ") BETWEEN 'a' AND 'z')", parameters);
                case nameof(char.IsAsciiLetterOrDigit):
                    return SQLiteExpression.Multi(returnType, visitor.Counters.NextIdentifier(),
                        ["((", " BETWEEN '0' AND '9') OR (LOWER(", ") BETWEEN 'a' AND 'z'))"],
                        [a0t, a0t], parameters);
                case nameof(char.IsAsciiLetterLower):
                    return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "(", a0t, " BETWEEN 'a' AND 'z')", parameters);
                case nameof(char.IsAsciiLetterUpper):
                    return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "(", a0t, " BETWEEN 'A' AND 'Z')", parameters);
            }
        }

        if (QueryableMemberVisitor.CheckConstantMethod<char>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        return visitor.NotTranslatable(node, $"char.{node.Method.Name} is not translatable to SQL.");
    }
}
