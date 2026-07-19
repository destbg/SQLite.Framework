namespace SQLite.Framework.Internals.Visitors.SQL;

internal partial class SQLVisitor
{
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The array was passed to us.")]
    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        if (node.NodeType == ExpressionType.NewArrayBounds)
        {
            return node.Update(node.Expressions.Select(e => ExpressionHelpers.IsConstant(e) ? e : Visit(e)!));
        }

        List<ResolvedModel> sqlExpressions = node.Expressions
            .Select(ResolveExpression)
            .ToList();

        Type elementType = node.Type.GetElementType()!;
        List<Expression> elements = new(node.Expressions.Count);
        for (int i = 0; i < node.Expressions.Count; i++)
        {
            Expression element = node.Expressions[i];
            if (element is ParameterExpression or MemberExpression
                && !TypeHelpers.IsSimple(element.Type, Database.Options)
                && TryMaterializeEntityLeaves(element) is { } materializedEntity)
            {
                elements.Add(materializedEntity);
                continue;
            }

            elements.Add(CoerceClientExpression(sqlExpressions[i].Expression, elementType));
        }

        return Expression.NewArrayInit(elementType, elements);
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "All types have public properties.")]
    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        NewExpression newExpression = (NewExpression)Visit(node.NewExpression);
        List<MemberBinding> bindings = node.Bindings.Select(VisitMemberBinding).ToList();

        if (InCustomMethodTranslator
            && node.NewExpression.Arguments.Count == 0
            && bindings is [MemberAssignment { Expression: SQLiteExpression single }])
        {
            return SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), "(", single, ")", single.Parameters);
        }

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
        Type targetType = node.Expression.Type;
        Expression visitTarget = node.Expression;
        if (node.Expression is UnaryExpression { NodeType: ExpressionType.Convert } boxing
            && (boxing.Type == typeof(object) || boxing.Type.IsInterface)
            && TypeHelpers.IsSimple(boxing.Operand.Type, Database.Options))
        {
            targetType = boxing.Operand.Type;
            visitTarget = boxing.Operand;
        }

        Expression expression = Visit(visitTarget);
        if (ClientEvalAllowed
            && expression is SQLiteExpression
            && node.Expression is not MemberExpression and not ConstantExpression)
        {
            expression = ToClientExpression(node.Expression);
        }

        if (expression is SQLiteExpression sqlExpression && sqlExpression.Type != targetType)
        {
            expression = SQLiteExpression.Alias(targetType, Counters.NextIdentifier(), sqlExpression, sqlExpression.Parameters);
        }

        return node.Update(expression);
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
        for (int i = 0; i < arguments.Count; i++)
        {
            if (arguments[i] is SQLiteExpression sqlArgument && sqlArgument.Type != node.Arguments[i].Type)
            {
                arguments[i] = SQLiteExpression.Alias(node.Arguments[i].Type, Counters.NextIdentifier(), sqlArgument, sqlArgument.Parameters);
            }
        }

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
