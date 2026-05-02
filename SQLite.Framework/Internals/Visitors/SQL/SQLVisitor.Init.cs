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
            return Expression.NewArrayInit(node.Type.GetElementType()!, sqlExpressions.Select(f => f.Expression));
        }

        SQLiteParameter[]? parameters = ParameterHelpers.CombineParametersFromModels(sqlExpressions);

        return new SQLiteExpression(
            node.Type,
            Counters.IdentifierIndex++,
            $"({string.Join(", ", sqlExpressions.Select(f => f.Sql))})",
            parameters
        );
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

    [ExcludeFromCodeCoverage(Justification = "I really hope no one is doing new Dto { Collection = { item1, item2 } } inside a Where clause")]
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
        return node.Update(arguments);
    }
}
