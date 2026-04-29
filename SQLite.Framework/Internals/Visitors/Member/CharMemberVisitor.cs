namespace SQLite.Framework.Internals.Visitors;

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
            switch (node.Method.Name)
            {
                case nameof(char.ToLower):
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"LOWER({arguments[0].Sql})",
                        arguments[0].Parameters
                    );
                case nameof(char.ToUpper):
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"UPPER({arguments[0].Sql})",
                        arguments[0].Parameters
                    );
                case nameof(char.IsWhiteSpace):
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"TRIM({arguments[0].Sql}) = ''",
                        arguments[0].Parameters
                    );
                case nameof(char.IsAsciiDigit):
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"({arguments[0].Sql} >= '0' AND {arguments[0].Sql} <= '9')",
                        arguments[0].Parameters
                    );
                case nameof(char.IsAsciiLetter):
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"(({arguments[0].Sql} >= 'a' AND {arguments[0].Sql} <= 'z') OR ({arguments[0].Sql} >= 'A' AND {arguments[0].Sql} <= 'Z'))",
                        arguments[0].Parameters
                    );
                case nameof(char.IsAsciiLetterOrDigit):
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"(({arguments[0].Sql} >= '0' AND {arguments[0].Sql} <= '9') OR ({arguments[0].Sql} >= 'a' AND {arguments[0].Sql} <= 'z') OR ({arguments[0].Sql} >= 'A' AND {arguments[0].Sql} <= 'Z'))",
                        arguments[0].Parameters
                    );
                case nameof(char.IsAsciiLetterLower):
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"({arguments[0].Sql} >= 'a' AND {arguments[0].Sql} <= 'z')",
                        arguments[0].Parameters
                    );
                case nameof(char.IsAsciiLetterUpper):
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"({arguments[0].Sql} >= 'A' AND {arguments[0].Sql} <= 'Z')",
                        arguments[0].Parameters
                    );
            }
        }

        if (QueryableMemberVisitor.CheckConstantMethod<char>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        throw new NotSupportedException($"char.{node.Method.Name} is not translatable to SQL.");
    }
}
