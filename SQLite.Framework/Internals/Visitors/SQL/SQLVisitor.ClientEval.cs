namespace SQLite.Framework.Internals.Visitors.SQL;

internal partial class SQLVisitor
{
    public Expression NotTranslatable(Expression node, string message)
    {
        if (!ClientEvalAllowed)
        {
            throw new NotSupportedException(message);
        }

        ClientEvalUsed = true;
        return BuildClientEvalFallback(node);
    }

    public Expression NotTranslatableBelowVersion(Expression node, SQLiteMinimumVersion requiredVersion, string featureName)
    {
        if (!ClientEvalAllowed)
        {
            Database.Options.ThrowMinimumVersionNotSupported(requiredVersion, featureName);
        }

        ClientEvalUsed = true;
        return BuildClientEvalFallback(node);
    }

    public SQLiteExpression? TryResolveColumnLeaf(Expression node)
    {
        (string path, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(node);
        if (pe != null
            && MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? columns)
            && IsSingleLeafColumn(columns, path))
        {
            return (SQLiteExpression)Visit(node);
        }

        return null;
    }

    public Expression ToClientExpression(Expression node)
    {
        return new ClientLeafRewriter(this).Visit(node);
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Entity types are rooted by the user Table<T>().")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Entity types are rooted by the user Table<T>().")]
    public Expression? TryMaterializeEntityLeaves(Expression node)
    {
        (string path, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(node);
        if (pe == null
            || !MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? columns)
            || !Database.TryGetCachedTableMapping(node.Type, out TableMapping? mapping)
            || node.Type.GetConstructor(Type.EmptyTypes) == null)
        {
            return null;
        }

        string prefix = path.Length == 0 ? "" : path + ".";
        List<MemberBinding> bindings = [];
        Expression? allNullTest = null;
        foreach (TableColumn column in mapping.Columns)
        {
            if (!columns.TryGetValue(prefix + column.PropertyInfo.Name, out Expression? expression)
                || expression is not SQLiteExpression sqlExpression)
            {
                return null;
            }

            bindings.Add(Expression.Bind(column.PropertyInfo, sqlExpression));
            SQLiteExpression secondRead = SQLiteExpression.Alias(typeof(object), Counters.NextIdentifier(), sqlExpression, parameters: null).WithSelectExclusion();
            secondRead.IdentifierText = sqlExpression.IdentifierText;
            Expression isNull = Expression.Equal(secondRead, Expression.Constant(null));
            allNullTest = allNullTest == null ? isNull : Expression.AndAlso(allNullTest, isNull);
        }

        Expression materialized = Expression.MemberInit(Expression.New(node.Type), bindings);
        return Expression.Condition(allNullTest!, Expression.Constant(null, node.Type), materialized);
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Entity types are rooted by the user Table<T>().")]
    public bool IsUnmaterializableRowMember(MemberExpression node)
    {
        (string _, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(node.Expression!);
        return pe != null
            && MethodArguments.ContainsKey(pe)
            && Database.TryGetCachedTableMapping(node.Expression!.Type, out _)
            && TryMaterializeEntityLeaves(node.Expression) == null;
    }

    public Expression ToClientOperand(Expression original, ResolvedModel resolved)
    {
        return resolved.SQLiteExpression != null ? ToClientExpression(original) : resolved.Expression;
    }

    public SQLiteExpression? TryResolveEntityNullCheck(BinaryExpression node)
    {
        Expression? operand = ExtractNullCheckOperand(node);
        if (operand == null)
        {
            return null;
        }

        return TryFoldConstructedNullCheck(node) ?? Visit(node) as SQLiteExpression;
    }

    public SQLiteExpression? TryFoldConstructedNullCheck(BinaryExpression node)
    {
        if (ExtractNullCheckOperand(node) is not { } operand)
        {
            return null;
        }

        if (TryGetConstructedComposite(operand) is { } composite
            && TryFoldCompositeNullCheck(composite, node.NodeType == ExpressionType.Equal) is { } folded)
        {
            return Visit(folded) as SQLiteExpression;
        }

        (string path, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(operand);
        if (pe != null
            && path.Length == 0
            && MethodArguments.ContainsKey(pe)
            && Database.TryGetCachedTableMapping(pe.Type, out _) == false)
        {
            return Visit(Expression.Constant(node.NodeType == ExpressionType.NotEqual)) as SQLiteExpression;
        }

        return null;
    }

    private Expression? ExtractNullCheckOperand(BinaryExpression node)
    {
        if (IsNullConstant(node.Right) && !TypeHelpers.IsSimple(node.Left.Type, Database.Options))
        {
            return node.Left;
        }

        if (IsNullConstant(node.Left) && !TypeHelpers.IsSimple(node.Right.Type, Database.Options))
        {
            return node.Right;
        }

        return null;
    }

    public SQLiteExpression? TryResolveConstructedMemberLeaf(Expression node)
    {
        (string path, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(node);
        if (pe == null
            || !path.Contains('.')
            || !MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? columns)
            || columns.ContainsKey(path)
            || !HasConstructedBase(columns, path))
        {
            return null;
        }

        return ResolveNestedConstructedMember(columns, path) as SQLiteExpression;
    }

    private Expression? TryGetConstructedComposite(Expression operand)
    {
        if (operand is ConditionalExpression or MemberInitExpression or NewExpression)
        {
            return operand;
        }

        (string path, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(operand);
        if (pe != null
            && MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? columns)
            && columns.TryGetValue(path, out Expression? value)
            && value is ConditionalExpression or MemberInitExpression or NewExpression)
        {
            return value;
        }

        return null;
    }

    private Expression BuildClientEvalFallback(Expression node)
    {
        if (node is MethodCallExpression methodCall)
        {
            Expression[] arguments = methodCall.Arguments.Select(ToClientExpression).ToArray();
            return methodCall.Object == null
                ? Expression.Call(methodCall.Method, arguments)
                : Expression.Call(ToClientExpression(methodCall.Object), methodCall.Method, arguments);
        }

        if (node is UnaryExpression unary)
        {
            return Expression.MakeUnary(unary.NodeType, ToClientExpression(unary.Operand), unary.Type);
        }

        if (node is TypeBinaryExpression typeBinary)
        {
            return typeBinary.NodeType == ExpressionType.TypeIs
                ? Expression.TypeIs(ToClientExpression(typeBinary.Expression), typeBinary.TypeOperand)
                : Expression.TypeEqual(ToClientExpression(typeBinary.Expression), typeBinary.TypeOperand);
        }

        if (node is InvocationExpression invocation)
        {
            return Expression.Invoke(ToClientExpression(invocation.Expression), invocation.Arguments.Select(ToClientExpression));
        }

        MemberExpression memberExpression = (MemberExpression)node;
        return Expression.MakeMemberAccess(ToClientExpression(memberExpression.Expression!), memberExpression.Member);
    }

    private static bool IsNullConstant(Expression node)
    {
        return ExpressionHelpers.IsConstant(node) && ExpressionHelpers.GetConstantValue(node) == null;
    }

    private static bool IsSingleLeafColumn(Dictionary<string, Expression> columns, string path)
    {
        if (columns.TryGetValue(path, out Expression? column))
        {
            return column is SQLiteExpression;
        }

        foreach (KeyValuePair<string, Expression> entry in columns)
        {
            if (string.Equals(entry.Key, path, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value is SQLiteExpression;
            }
        }

        return path.Length == 0 && columns.Count == 1 && columns.Values.First() is SQLiteExpression;
    }

    private static bool HasConstructedBase(Dictionary<string, Expression> columns, string path)
    {
        int splitIndex = path.LastIndexOf('.');
        while (splitIndex > 0)
        {
            if (columns.TryGetValue(path[..splitIndex], out Expression? baseExpression))
            {
                return baseExpression is ConditionalExpression;
            }

            splitIndex = path.LastIndexOf('.', splitIndex - 1);
        }

        return false;
    }

    private static Expression? TryFoldCompositeNullCheck(Expression composite, bool equalNull)
    {
        if (composite is MemberInitExpression or NewExpression)
        {
            return Expression.Constant(!equalNull);
        }

        ConditionalExpression conditional = (ConditionalExpression)composite;
        bool? ifTrue = BranchIsNull(conditional.IfTrue);
        bool? ifFalse = BranchIsNull(conditional.IfFalse);
        if (ifTrue == null || ifFalse == null)
        {
            return null;
        }

        return Expression.Condition(
            conditional.Test,
            Expression.Constant(equalNull ? ifTrue.Value : !ifTrue.Value),
            Expression.Constant(equalNull ? ifFalse.Value : !ifFalse.Value));
    }

    private static bool? BranchIsNull(Expression branch)
    {
        if (ExpressionHelpers.IsConstant(branch))
        {
            return ExpressionHelpers.GetConstantValue(branch) == null;
        }

        if (branch is MemberInitExpression or NewExpression)
        {
            return false;
        }

        return null;
    }
}
