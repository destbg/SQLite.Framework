namespace SQLite.Framework.Internals.Visitors.Detection;

internal sealed class LocalCollectionPathCollector : ExpressionVisitor
{
    private readonly ParameterExpression parameter;
    private readonly Dictionary<string, Expression> paths = new(StringComparer.Ordinal);

    public LocalCollectionPathCollector(ParameterExpression parameter)
    {
        this.parameter = parameter;
    }

    public bool ReadsWholeParameter { get; private set; }

    public bool HasNullCheck { get; private set; }

    public IReadOnlyDictionary<string, Expression> Paths => paths;

    protected override Expression VisitMember(MemberExpression node)
    {
        (string path, ParameterExpression? root) = ExpressionHelpers.ResolveNullableParameterPath(node);
        if (root == parameter)
        {
            paths.TryAdd(path, node);
            return node;
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual
            && (IsParameterNullCheck(node.Left, node.Right) || IsParameterNullCheck(node.Right, node.Left)))
        {
            HasNullCheck = true;
            return node;
        }

        return base.VisitBinary(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == parameter)
        {
            ReadsWholeParameter = true;
        }

        return node;
    }

    private bool IsParameterNullCheck(Expression parameterExpression, Expression nullExpression)
    {
        return parameterExpression == parameter
            && nullExpression is ConstantExpression { Value: null };
    }
}
