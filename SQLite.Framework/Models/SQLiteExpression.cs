namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Represents a SQL expression that writes its SQL into a <see cref="StringBuilder" /> when needed.
/// Build new ones with the static factory methods (<see cref="Leaf(Type, int, string)" />,
/// <see cref="Wrap(Type, int, string, SQLiteExpression, string, SQLiteParameter[])" />,
/// <see cref="Binary(Type, int, string, SQLiteExpression, string, SQLiteExpression, string, SQLiteParameter[])" />,
/// <see cref="Trinary(Type, int, string, SQLiteExpression, string, SQLiteExpression, string, SQLiteExpression, string, SQLiteParameter[])" />,
/// <see cref="Variadic(Type, int, string, SQLiteExpression[], string, string, SQLiteParameter[])" />,
/// or <see cref="Lambda(Type, int, Action{StringBuilder}, SQLiteParameter[])" />).
/// Each one picks the layout that fits the number of children with the lowest cost.
/// </summary>
public abstract class SQLiteExpression : Expression
{
    private protected SQLiteExpression(Type type, int identifier, SQLiteParameter[]? parameters)
    {
        Type = type;
        Identifier = identifier;
        Parameters = parameters;
    }

    /// <summary>
    /// The unique number for this SQL expression. Used for caching and for comparing expressions.
    /// </summary>
    public int Identifier { get; }

    /// <summary>
    /// When true, the SQL for this expression is wrapped in brackets when it is part of a larger
    /// SQL statement. Use this to keep the right operator order and grouping in complex expressions.
    /// </summary>
    public bool RequiresBrackets { get; set; }

    /// <summary>
    /// When true, this expression returns JSON data. Some JSON functions and operators in SQLite
    /// only work on values that are already JSON, so this flag tells the translator to treat the
    /// value as JSON.
    /// </summary>
    public bool IsJsonSource { get; set; }

    /// <summary>
    /// The parameters used by this SQL expression, or <c>null</c> if it has none. Use parameters
    /// to safely pass user input or variable data into a SQL statement, instead of building the
    /// SQL string by hand. Each parameter must have a unique name that matches a placeholder in
    /// the SQL.
    /// </summary>
    public SQLiteParameter[]? Parameters { get; }

    /// <summary>
    /// The text form of the identifier. Useful for debugging and logging. You can set this to a
    /// more descriptive name. If not set, it falls back to the number from <see cref="Identifier"/>.
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

    /// <summary>
    /// Sets <see cref="IsJsonSource"/> to <c>true</c> and returns this expression. Useful for
    /// chaining with the factory methods.
    /// </summary>
    public SQLiteExpression WithJsonSource()
    {
        IsJsonSource = true;
        return this;
    }

    /// <summary>
    /// Writes the SQL for this expression directly into <paramref name="sb"/>, without first
    /// building a separate string.
    /// </summary>
    public abstract void WriteSqlTo(StringBuilder sb);

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
        StringBuilder sb = StringBuilderPool.Rent();
        WriteSqlTo(sb);
        return StringBuilderPool.ToStringAndReturn(sb);
    }

    /// <summary>
    /// Creates a leaf expression: a single SQL string with no children.
    /// </summary>
    public static SQLiteExpression Leaf(Type type, int identifier, string sql)
    {
        return new LeafSqlExpression(type, identifier, sql, null);
    }

    /// <summary>
    /// Creates a leaf expression with one parameter. The <paramref name="sql"/> is also used as the
    /// parameter name (often a placeholder like <c>@p0</c>).
    /// </summary>
    public static SQLiteExpression Leaf(Type type, int identifier, string sql, object? parameter)
    {
        return new LeafSqlExpression(type, identifier, sql, [
            new SQLiteParameter
            {
                Name = sql,
                Value = parameter
            }]);
    }

    /// <summary>
    /// Creates a leaf expression with a fixed list of parameters.
    /// </summary>
    public static SQLiteExpression Leaf(Type type, int identifier, string sql, SQLiteParameter[]? parameters)
    {
        return new LeafSqlExpression(type, identifier, sql, parameters);
    }

    /// <summary>
    /// Creates an alias whose SQL is the same as <paramref name="inner"/>, but with a different
    /// <c>Type</c>, <c>Identifier</c>, or <c>Parameters</c>. Cheaper than <see cref="Wrap"/> with
    /// two empty strings.
    /// </summary>
    public static SQLiteExpression Alias(Type type, int identifier, SQLiteExpression inner, SQLiteParameter[]? parameters)
    {
        return new AliasSqlExpression(type, identifier, inner, parameters);
    }

    /// <summary>
    /// Creates an expression with the shape <c>{before}{child}{after}</c>. Use for unary operators
    /// and function calls with one argument.
    /// </summary>
    public static SQLiteExpression Wrap(Type type, int identifier, string before, SQLiteExpression child, string after, SQLiteParameter[]? parameters)
    {
        return new WrapSqlExpression(type, identifier, before, child, after, parameters);
    }

    /// <summary>
    /// Creates an expression with the shape <c>{before}{a}{mid}{b}{after}</c>. Use for binary
    /// operators and function calls with two arguments.
    /// </summary>
    public static SQLiteExpression Binary(Type type, int identifier, string before, SQLiteExpression a, string mid, SQLiteExpression b, string after, SQLiteParameter[]? parameters)
    {
        return new BinarySqlExpression(type, identifier, before, a, mid, b, after, parameters);
    }

    /// <summary>
    /// Creates an expression with the shape <c>{before}{a}{mid1}{b}{mid2}{c}{after}</c>. Use for
    /// function calls with three arguments and for ternary expressions.
    /// </summary>
    public static SQLiteExpression Trinary(Type type, int identifier, string before, SQLiteExpression a, string mid1, SQLiteExpression b, string mid2, SQLiteExpression c, string after, SQLiteParameter[]? parameters)
    {
        return new TrinarySqlExpression(type, identifier, before, a, mid1, b, mid2, c, after, parameters);
    }

    /// <summary>
    /// Creates an expression with the shape <c>{before}{children[0]}{sep}{children[1]}{sep}...{after}</c>.
    /// Use for function calls that take any number of arguments, like <c>string.Concat</c>,
    /// <c>IN</c> lists, and <c>COALESCE</c> chains.
    /// </summary>
    public static SQLiteExpression Variadic(Type type, int identifier, string before, SQLiteExpression[] children, string sep, string after, SQLiteParameter[]? parameters)
    {
        return new VariadicSqlExpression(type, identifier, before, children, sep, after, parameters);
    }

    /// <summary>
    /// Creates an expression with the shape <c>{parts[0]}{children[0]}{parts[1]}...{parts[N]}</c>
    /// where <c>parts.Length == children.Length + 1</c>. Use for four or more child slots. For
    /// one to three children, use <see cref="Wrap"/>, <see cref="Binary"/>, or <see cref="Trinary"/>
    /// instead.
    /// </summary>
    public static SQLiteExpression Multi(Type type, int identifier, string[] parts, SQLiteExpression[] children, SQLiteParameter[]? parameters)
    {
        return new MultiPartSqlExpression(type, identifier, parts, children, parameters);
    }

    /// <summary>
    /// Creates an expression that writes its SQL through an <see cref="Action{StringBuilder}" />.
    /// Use the other factory methods when one of them fits the shape: the lambda here captures
    /// its child expressions, which allocates an extra object.
    /// </summary>
    public static SQLiteExpression Lambda(Type type, int identifier, Action<StringBuilder> writer, SQLiteParameter[]? parameters)
    {
        return new LambdaSqlExpression(type, identifier, writer, parameters);
    }
}
