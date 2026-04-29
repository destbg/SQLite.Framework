namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Represents a SQL expression in the form of a string.
/// </summary>
public class SQLiteExpression : Expression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteExpression"/> class.
    /// </summary>
    public SQLiteExpression(Type type, int identifier, string sql)
    {
        Type = type;
        Identifier = identifier;
        Sql = sql;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteExpression"/> class.
    /// </summary>
    public SQLiteExpression(Type type, int identifier, string sql, object? parameter)
    {
        Type = type;
        Identifier = identifier;
        Sql = sql;
        Parameters =
        [
            new SQLiteParameter
            {
                Name = sql,
                Value = parameter
            }
        ];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteExpression"/> class.
    /// </summary>
    public SQLiteExpression(Type type, int identifier, string sql, SQLiteParameter[]? parameters)
    {
        Type = type;
        Identifier = identifier;
        Sql = sql;
        Parameters = parameters;
    }

    /// <summary>
    /// Gets the unique identifier for this SQL expression, which can be used for caching and comparison purposes.
    /// </summary>
    public int Identifier { get; }

    /// <summary>
    /// Gets the SQL string that represents this expression. This string may contain parameter placeholders if parameters are used.
    /// </summary>
    public string Sql { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this SQL expression should be wrapped in brackets when included in a larger SQL statement. This is important for ensuring correct operator precedence and grouping in complex expressions.
    /// </summary>
    public bool RequiresBrackets { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this SQL expression is a source of JSON data. This can affect how the expression is processed and translated, especially when dealing with JSON functions and operators in SQLite.
    /// </summary>
    public bool IsJsonSource { get; set; }

    /// <summary>
    /// Gets the parameters associated with this SQL expression, if any. These parameters can be used to safely include user input or variable data in the SQL statement without risking SQL injection attacks. Each parameter should have a unique name that corresponds to a placeholder in the SQL string.
    /// </summary>
    public SQLiteParameter[]? Parameters { get; }

    /// <summary>
    /// Gets or sets the text representation of the identifier for this SQL expression. This is used for debugging and logging purposes, and can be set to a more descriptive string if desired. If not set, it defaults to the string representation of the identifier integer value.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public string IdentifierText
    {
        get => field ??= Identifier.ToString();
        set;
    }

    /// <inheritdoc/>
    public override Type Type { get; }

    /// <inheritdoc/>
    public override ExpressionType NodeType => ExpressionType.Quote;

    /// <inheritdoc/>
    protected override Expression Accept(ExpressionVisitor visitor)
    {
        if (visitor is SelectVisitor selectVisitor)
        {
            return selectVisitor.VisitSQLExpression(this);
        }
        else if (visitor is QueryCompilerVisitor queryCompilerVisitor)
        {
            return queryCompilerVisitor.VisitSQLExpression(this);
        }

        return this;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Sql;
    }
}