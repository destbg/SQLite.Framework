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
            && IsSingleLeafColumn(columns, path, (node as MemberExpression)?.Member.DeclaringType))
        {
            return Visit(node) as SQLiteExpression;
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
            || !Database.TryGetCachedTableMapping(node.Type, out TableMapping? mapping))
        {
            return null;
        }

        string prefix = path.Length == 0 ? "" : path + ".";
        Dictionary<string, SQLiteExpression> leaves = new(StringComparer.OrdinalIgnoreCase);
        foreach (TableColumn column in mapping.Columns)
        {
            if (!columns.TryGetValue(prefix + column.PropertyInfo.Name, out Expression? expression)
                || expression is not SQLiteExpression sqlExpression)
            {
                return null;
            }

            leaves[column.PropertyInfo.Name] = sqlExpression;
        }

        Expression? materialized = BuildEntityFromLeaves(node.Type, mapping, leaves, out List<SQLiteExpression> used);
        if (materialized == null)
        {
            return null;
        }

        Expression? allNullTest = null;
        foreach (SQLiteExpression leaf in used)
        {
            SQLiteExpression secondRead = SQLiteExpression.Alias(typeof(object), Counters.NextIdentifier(), leaf, parameters: null).WithSelectExclusion();
            secondRead.IdentifierText = leaf.IdentifierText;
            Expression isNull = Expression.Equal(secondRead, Expression.Constant(null));
            allNullTest = allNullTest == null ? isNull : Expression.AndAlso(allNullTest, isNull);
        }

        return allNullTest == null
            ? materialized
            : Expression.Condition(allNullTest, Expression.Constant(null, node.Type), materialized);
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Entity types are rooted by the user Table<T>().")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Entity types are rooted by the user Table<T>().")]
    public bool IsUnmaterializableRowMember(MemberExpression node)
    {
        (string _, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(node.Expression!);

        return pe != null
            && MethodArguments.ContainsKey(pe)
            && Database.TryGetCachedTableMapping(node.Expression!.Type, out _)
            && (node.Expression.Type.GetConstructor(Type.EmptyTypes) == null
                || TryMaterializeEntityLeaves(node.Expression) == null);
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
        bool mappedWithKey = Database.TryGetCachedTableMapping(operand.Type, out TableMapping? operandMapping)
            && operandMapping.Columns.Any(c => c.IsPrimaryKey);
        if (pe != null
            && MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? rowColumns)
            && !mappedWithKey)
        {
            if (path.Length == 0)
            {
                if (OptionalRowColumns.Contains(rowColumns))
                {
                    return BuildOptionalRowNullCheck(rowColumns, node.NodeType == ExpressionType.Equal);
                }

                return Visit(Expression.Constant(node.NodeType == ExpressionType.NotEqual)) as SQLiteExpression;
            }

            if (OptionalRowPaths.TryGetValue(rowColumns, out HashSet<string>? optionalPaths)
                && optionalPaths.Contains(path))
            {
                Dictionary<string, Expression> memberColumns = CollectMemberColumns(rowColumns, path);
                if (memberColumns.Count > 0)
                {
                    return BuildOptionalRowNullCheck(memberColumns, node.NodeType == ExpressionType.Equal);
                }
            }
        }

        return null;
    }

    private SQLiteExpression? BuildOptionalRowNullCheck(Dictionary<string, Expression> rowColumns, bool wantNull)
    {
        List<SQLiteExpression> leaves = [];
        foreach (KeyValuePair<string, Expression> column in rowColumns)
        {
            if (column.Value is SQLiteExpression leaf)
            {
                leaves.Add(leaf);
            }
        }

        if (leaves.Count == 0)
        {
            return null;
        }

        string separator = wantNull ? " IS NULL AND " : " IS NOT NULL OR ";
        string[] parts = new string[leaves.Count + 1];
        parts[0] = "(";
        for (int i = 0; i < leaves.Count; i++)
        {
            parts[i + 1] = i == leaves.Count - 1 ? (wantNull ? " IS NULL)" : " IS NOT NULL)") : separator;
        }

        return SQLiteExpression.Multi(typeof(bool), Counters.NextIdentifier(), parts, [.. leaves],
            ParameterHelpers.CombineParameters([.. leaves]));
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
        if (pe == null || !MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? columns))
        {
            return null;
        }

        if (columns.TryGetValue(path, out Expression? value)
            && value is ConditionalExpression or MemberInitExpression or NewExpression)
        {
            return value;
        }

        bool optionalPath = path.Length == 0
            ? OptionalRowColumns.Contains(columns)
            : OptionalRowPaths.TryGetValue(columns, out HashSet<string>? optionalPaths) && optionalPaths.Contains(path);
        if (!optionalPath
            && ConstructedProjectionNodes.TryGetValue(columns, out Dictionary<string, Expression>? nodes)
            && nodes.TryGetValue(path, out Expression? node))
        {
            return node;
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

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Entity types are rooted by Table<T>().")]
    private static Expression? BuildEntityFromLeaves(Type entityType, TableMapping mapping, Dictionary<string, SQLiteExpression> leaves, out List<SQLiteExpression> used)
    {
        used = [];
        ConstructorInfo? parameterless = entityType.GetConstructor(Type.EmptyTypes);
        if (parameterless != null)
        {
            List<MemberBinding> bindings = [];
            foreach (TableColumn column in mapping.Columns)
            {
                if (column.PropertyInfo.CanWrite)
                {
                    SQLiteExpression leaf = leaves[column.PropertyInfo.Name];
                    used.Add(leaf);
                    bindings.Add(Expression.Bind(column.PropertyInfo, leaf));
                }
            }

            return Expression.MemberInit(Expression.New(parameterless), bindings);
        }

        ConstructorInfo? widest = entityType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
        if (widest == null)
        {
            return null;
        }

        List<Expression> arguments = [];
        foreach (ParameterInfo parameter in widest.GetParameters())
        {
            if (!leaves.TryGetValue(parameter.Name!, out SQLiteExpression? leaf))
            {
                return null;
            }

            used.Add(leaf);
            arguments.Add(leaf);
        }

        List<MemberBinding> extra = [];
        foreach (TableColumn column in mapping.Columns)
        {
            if (column.PropertyInfo.CanWrite
                && !widest.GetParameters().Any(p => string.Equals(p.Name, column.PropertyInfo.Name, StringComparison.OrdinalIgnoreCase)))
            {
                SQLiteExpression leaf = leaves[column.PropertyInfo.Name];
                used.Add(leaf);
                extra.Add(Expression.Bind(column.PropertyInfo, leaf));
            }
        }

        return extra.Count == 0
            ? Expression.New(widest, arguments)
            : Expression.MemberInit(Expression.New(widest, arguments), extra);
    }

    private static bool IsNullConstant(Expression node)
    {
        return ExpressionHelpers.IsConstant(node) && ExpressionHelpers.GetConstantValue(node) == null;
    }

    private static Dictionary<string, Expression> CollectMemberColumns(Dictionary<string, Expression> rowColumns, string path)
    {
        Dictionary<string, Expression> subset = [];
        string prefix = path + ".";
        foreach (KeyValuePair<string, Expression> column in rowColumns)
        {
            if (column.Key.StartsWith(prefix, StringComparison.Ordinal))
            {
                subset[column.Key[prefix.Length..]] = column.Value;
            }
        }

        return subset;
    }

    private static bool IsSingleLeafColumn(Dictionary<string, Expression> columns, string path, Type? declaringType)
    {
        if (columns.TryGetValue(path, out Expression? column))
        {
            return column is SQLiteExpression;
        }

        if (AllowsParameterNameMatch(declaringType, path))
        {
            foreach (KeyValuePair<string, Expression> entry in columns)
            {
                if (string.Equals(entry.Key, path, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Value is SQLiteExpression;
                }
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
