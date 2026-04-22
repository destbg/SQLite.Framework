using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Models;

namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Compiled expressions are used to support select methods
/// using both SQL and LINQ expressions.
/// </summary>
[ExcludeFromCodeCoverage]
internal class CompiledExpression : Expression
{
    public CompiledExpression(Type type, Func<SQLiteQueryContext, object?> call)
    {
        Type = type;
        Call = call;
    }

    public new Func<SQLiteQueryContext, object?> Call { get; }

    public override Type Type { get; }
    public override ExpressionType NodeType => ExpressionType.Call;
}