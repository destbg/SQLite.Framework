namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Collects the member paths that a result selector reads from one of its parameters, so the
/// translator can tell which carried members the projection keeps and which it drops.
/// </summary>
internal sealed class CarriedPathCollector : ExpressionVisitor
{
    private readonly ParameterExpression parameter;
    private readonly HashSet<string> paths = new(StringComparer.Ordinal);
    private bool readsWholeParameter;

    public CarriedPathCollector(ParameterExpression parameter)
    {
        this.parameter = parameter;
    }

    public bool Carries(string path)
    {
        if (readsWholeParameter)
        {
            return true;
        }

        foreach (string read in paths)
        {
            if (read == path || read.StartsWith(path + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        (string path, ParameterExpression? root) = ExpressionHelpers.ResolveNullableParameterPath(node);
        if (root == parameter && path.Length > 0)
        {
            paths.Add(path);
            return node;
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == parameter)
        {
            readsWholeParameter = true;
        }

        return node;
    }
}
