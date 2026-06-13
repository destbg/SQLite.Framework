namespace SQLite.Framework.Internals.Visitors.SQL;

internal partial class SQLVisitor
{
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The array was passed to us.")]
    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        List<ResolvedModel> sqlExpressions = node.Expressions
            .Select(ResolveExpression)
            .ToList();

        if (sqlExpressions.Any(f => f.SQLiteExpression == null))
        {
            Type elementType = node.Type.GetElementType()!;
            return Expression.NewArrayInit(elementType, sqlExpressions.Select(f => CoerceClientExpression(f.Expression, elementType)));
        }

        SQLiteParameter[]? parameters = ParameterHelpers.CombineParametersFromModels(sqlExpressions);
        SQLiteExpression[] argExprs = sqlExpressions.Select(f => f.SQLiteExpression!).ToArray();

        return SQLiteExpression.Variadic(node.Type, Counters.NextIdentifier(), "(", argExprs, ", ", ")", parameters);
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "All types have public properties.")]
    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        NewExpression newExpression = (NewExpression)Visit(node.NewExpression);
        List<MemberBinding> bindings = node.Bindings.Select(VisitMemberBinding).ToList();

        return Expression.MemberInit(newExpression, bindings);
    }

    protected override MemberBinding VisitMemberBinding(MemberBinding node)
    {
        return node switch
        {
            MemberAssignment assignment => VisitMemberAssignment(assignment),
            MemberMemberBinding memberMemberBinding => VisitMemberMemberBinding(memberMemberBinding),
            MemberListBinding memberListBinding => VisitMemberListBinding(memberListBinding),
            _ => throw new NotSupportedException($"Unsupported binding type: {node.BindingType}")
        };
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        return node.Update(Visit(node.Expression));
    }

    protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
    {
        List<MemberBinding> bindings = node.Bindings.Select(VisitMemberBinding).ToList();
        return node.Update(bindings);
    }

    protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
    {
        List<ElementInit> initializers = node.Initializers.Select(VisitElementInit).ToList();
        return node.Update(initializers);
    }

    protected override ElementInit VisitElementInit(ElementInit node)
    {
        List<Expression> arguments = node.Arguments.Select(Visit).ToList()!;
        return node.Update(arguments);
    }

    protected override Expression VisitListInit(ListInitExpression node)
    {
        NewExpression newExpression = (NewExpression)Visit(node.NewExpression);
        List<ElementInit> initializers = node.Initializers.Select(VisitElementInit).ToList();

        return node.Update(newExpression, initializers);
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "All types have public properties.")]
    protected override Expression VisitNew(NewExpression node)
    {
        List<Expression> arguments = node.Arguments.Select(Visit).ToList()!;
        for (int i = 0; i < arguments.Count; i++)
        {
            if (arguments[i] is SQLiteExpression sqlArgument && sqlArgument.Type != node.Arguments[i].Type)
            {
                arguments[i] = SQLiteExpression.Alias(node.Arguments[i].Type, Counters.NextIdentifier(), sqlArgument, sqlArgument.Parameters);
            }
        }

        return node.Update(arguments);
    }
}
