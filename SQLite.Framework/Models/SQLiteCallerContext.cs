namespace SQLite.Framework.Models;

/// <summary>
/// Represents the context of a caller invoking SQLite operations, providing necessary information for query translation and execution.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SQLiteCallerContext
{
    internal SQLiteCallerContext(SQLVisitor visitor, Expression node)
    {
        Visitor = visitor;
        Node = node;
    }

    internal SQLVisitor Visitor { get; }

    /// <summary>
    /// The current expression being translated. For built-in handlers this is the
    /// <see cref="MethodCallExpression" /> being dispatched.
    /// </summary>
    public Expression Node { get; }

    /// <summary>
    /// Gets the counters used for generating unique aliases and parameter names during SQL translation.
    /// </summary>
    public SQLiteCounters Counters => Visitor.Counters;

    /// <summary>
    /// Gets the current level of the expression tree being processed, which can be used to determine the depth of nested queries or expressions.
    /// </summary>
    public int Level => Visitor.Level;

    /// <summary>
    /// Gets or sets a value indicating whether the current context is within a SELECT projection.
    /// </summary>
    public bool IsInSelectProjection => Visitor.IsInSelectProjection;

    /// <summary>
    /// Gets the current FROM expression in the SQL query.
    /// </summary>
    public SQLiteExpression? From => Visitor.From;

    /// <summary>
    /// Gets or sets the method arguments for the current context.
    /// </summary>
    public Dictionary<ParameterExpression, Dictionary<string, Expression>> MethodArguments => Visitor.MethodArguments;

    /// <summary>
    /// Gets or sets the table columns for the current context.
    /// </summary>
    public Dictionary<string, Expression> TableColumns => Visitor.TableColumns;

    /// <summary>
    /// Visits the given expression with the current visitor, returning the translated tree.
    /// A node that translates to SQL comes back as an <see cref="SQLiteExpression" />.
    /// </summary>
    public Expression Visit(Expression node)
    {
        return Visitor.Visit(node);
    }
}