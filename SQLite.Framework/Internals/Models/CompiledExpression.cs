using System.Linq.Expressions;

namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Compiled expressions are used to support select methods
/// using both SQL and LINQ expressions.
/// </summary>
internal class CompiledExpression : Expression
{
    public CompiledExpression(Type type, Func<QueryContext, dynamic?> call)
    {
        Type = type;
        Call = call;
    }

    public new Func<QueryContext, dynamic?> Call { get; }

    public override Type Type { get; }
    public override ExpressionType NodeType => ExpressionType.Call;
}