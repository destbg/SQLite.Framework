using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Internals.Visitors;

namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Represents a SQL expression in the form of a string.
/// </summary>
[ExcludeFromCodeCoverage]
internal class SQLExpression : Expression
{
    public SQLExpression(Type type, int identifier, string sql)
    {
        Type = type;
        Identifier = identifier;
        Sql = sql;
    }

    public SQLExpression(Type type, int identifier, string sql, object? parameter)
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

    public SQLExpression(Type type, int identifier, string sql, SQLiteParameter[]? parameters)
    {
        Type = type;
        Identifier = identifier;
        Sql = sql;
        Parameters = parameters;
    }

    public int Identifier { get; }
    public string Sql { get; }
    public bool RequiresBrackets { get; set; }
    public SQLiteParameter[]? Parameters { get; }

    [field: AllowNull, MaybeNull]
    public string IdentifierText
    {
        get => field ??= Identifier.ToString();
        set;
    }

    public override Type Type { get; }
    public override ExpressionType NodeType => ExpressionType.Quote;

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

    public override string ToString()
    {
        return Sql;
    }
}