using System.Globalization;
using System.Text;

namespace SQLite.Framework.Internals.FTS5;

/// <summary>
/// Mutable state used by <see cref="FtsHelpers.RenderFTSMatch" /> while walking an FTS5 builder expression.
/// Accumulates literal FTS5 query text and dynamic SQL expressions into a list of
/// <see cref="FtsQueryPart" /> entries.
/// </summary>
internal sealed class FtsRenderState
{
    private StringBuilder buffer = StringBuilderPool.Rent();

    public FtsRenderState(SQLVisitor visitor)
    {
        Visitor = visitor;
    }

    public List<FtsQueryPart> Parts { get; } = [];
    public SQLVisitor Visitor { get; }

    public void AppendLiteral(string text)
    {
        buffer.Append(text);
    }

    public void AppendLiteral(char ch)
    {
        buffer.Append(ch);
    }

    public void AppendDynamic(SQLiteExpression sql)
    {
        FlushLiteral();
        Parts.Add(new FtsQueryPart(null, sql));
    }

    public void FlushLiteral()
    {
        if (buffer.Length == 0)
        {
            return;
        }

        Parts.Add(new FtsQueryPart(buffer.ToString(), null));
        buffer.Clear();
    }

    public void ReleaseBuffer()
    {
        StringBuilderPool.Return(buffer);
        buffer = null!;
    }

    public void Write(Expression node, int parentPrecedence)
    {
        switch (node)
        {
            case BinaryExpression { NodeType: ExpressionType.AndAlso } andNode:
                WriteAnd(andNode, parentPrecedence);
                return;

            case BinaryExpression { NodeType: ExpressionType.OrElse } orNode:
                WriteBinary(orNode, "OR", 1, parentPrecedence);
                return;

            case UnaryExpression { NodeType: ExpressionType.Not }:
                throw new NotSupportedException("FTS5 has no unary NOT operator. Use '!' only as the operand of '&&', e.g. 'x && !y' which translates to FTS5 'x NOT y'.");

            case MethodCallExpression call when call.Method.DeclaringType == typeof(SQLiteFTS5Builder):
                WriteFts5Call(call);
                return;
        }

        throw new NotSupportedException($"Unsupported expression inside SQLiteFTS5Functions.Match: {node}. Use the builder methods (f.Term, f.Phrase, f.Prefix, f.Near, f.Column) with C# &&, ||, or ! to build the query.");
    }

    private void WriteAnd(BinaryExpression node, int parentPrecedence)
    {
        if (node.Right is UnaryExpression { NodeType: ExpressionType.Not } rightNot)
        {
            WriteFts5Not(node.Left, rightNot.Operand, parentPrecedence);
            return;
        }

        if (node.Left is UnaryExpression { NodeType: ExpressionType.Not } leftNot)
        {
            WriteFts5Not(node.Right, leftNot.Operand, parentPrecedence);
            return;
        }

        WriteBinary(node, "AND", 2, parentPrecedence);
    }

    private void WriteFts5Not(Expression positive, Expression negated, int parentPrecedence)
    {
        bool wrap = parentPrecedence > 3;
        if (wrap)
        {
            AppendLiteral('(');
        }

        Write(positive, 3);
        AppendLiteral(" NOT ");
        Write(negated, 3);

        if (wrap)
        {
            AppendLiteral(')');
        }
    }

    private void WriteBinary(BinaryExpression node, string op, int precedence, int parentPrecedence)
    {
        bool wrap = parentPrecedence > precedence;
        if (wrap)
        {
            AppendLiteral('(');
        }

        Write(node.Left, precedence);
        AppendLiteral(' ');
        AppendLiteral(op);
        AppendLiteral(' ');
        Write(node.Right, precedence);

        if (wrap)
        {
            AppendLiteral(')');
        }
    }

    private void WriteFts5Call(MethodCallExpression call)
    {
        switch (call.Method.Name)
        {
            case nameof(SQLiteFTS5Builder.Term):
                WriteFts5Term(call.Arguments[0]);
                return;
            case nameof(SQLiteFTS5Builder.Phrase):
                WriteFts5Phrase(call.Arguments[0]);
                return;
            case nameof(SQLiteFTS5Builder.Prefix):
                WriteFts5Prefix(call.Arguments[0]);
                return;
            case nameof(SQLiteFTS5Builder.Near):
                WriteFts5Near(call.Arguments[0], call.Arguments[1]);
                return;
            case nameof(SQLiteFTS5Builder.Column):
                WriteFts5Column(call.Arguments[0], call.Arguments[1]);
                return;
        }

        throw new NotSupportedException($"Unsupported SQLiteFTS5 method '{call.Method.Name}' inside Match expression.");
    }

    private void WriteFts5Term(Expression arg)
    {
        if (ExpressionHelpers.IsConstant(arg))
        {
            string term = (string)ExpressionHelpers.GetConstantValue(arg)!;
            AppendLiteral(EscapeTerm(term));
        }
        else
        {
            AppendDynamic(ResolveSqlExpression(arg));
        }
    }

    private void WriteFts5Phrase(Expression arg)
    {
        if (ExpressionHelpers.IsConstant(arg))
        {
            string phrase = (string)ExpressionHelpers.GetConstantValue(arg)!;
            AppendLiteral('"');
            AppendLiteral(phrase.Replace("\"", "\"\""));
            AppendLiteral('"');
        }
        else
        {
            AppendDynamic(ResolveSqlExpression(arg));
        }
    }

    private void WriteFts5Prefix(Expression arg)
    {
        if (ExpressionHelpers.IsConstant(arg))
        {
            string prefix = (string)ExpressionHelpers.GetConstantValue(arg)!;
            AppendLiteral(EscapeTerm(prefix));
            AppendLiteral('*');
        }
        else
        {
            AppendDynamic(ResolveSqlExpression(arg));
            AppendLiteral('*');
        }
    }

    private void WriteFts5Near(Expression distanceArg, Expression termsArg)
    {
        int distance = (int)ExpressionHelpers.GetConstantValue(distanceArg)!;

        AppendLiteral("NEAR(");
        if (termsArg is NewArrayExpression nae)
        {
            for (int i = 0; i < nae.Expressions.Count; i++)
            {
                if (i > 0)
                {
                    AppendLiteral(' ');
                }

                Expression element = nae.Expressions[i];
                if (ExpressionHelpers.IsConstant(element))
                {
                    AppendLiteral(EscapeTerm((string)ExpressionHelpers.GetConstantValue(element)!));
                }
                else
                {
                    AppendDynamic(ResolveSqlExpression(element));
                }
            }
        }
        else
        {
            string[] terms = (string[])ExpressionHelpers.GetConstantValue(termsArg)!;
            for (int i = 0; i < terms.Length; i++)
            {
                if (i > 0)
                {
                    AppendLiteral(' ');
                }

                AppendLiteral(EscapeTerm(terms[i]));
            }
        }

        AppendLiteral(", ");
        AppendLiteral(distance.ToString(CultureInfo.InvariantCulture));
        AppendLiteral(')');
    }

    private void WriteFts5Column(Expression columnArg, Expression bodyArg)
    {
        string columnName = ResolveColumnName(columnArg);
        AppendLiteral('{');
        AppendLiteral(columnName);
        AppendLiteral("} : ");
        Write(bodyArg, 4);
    }

    private SQLiteExpression ResolveSqlExpression(Expression expr)
    {
        ResolvedModel resolved = Visitor.ResolveExpression(expr);
        if (resolved.SQLiteExpression == null)
        {
            throw new NotSupportedException($"SQLiteFTS5 builder argument '{expr}' could not be translated to SQL.");
        }

        return resolved.SQLiteExpression;
    }

    private static string ResolveColumnName(Expression expr)
    {
        if (expr is MemberExpression me)
        {
            return me.Member.Name;
        }

        if (expr is UnaryExpression { NodeType: ExpressionType.Convert } u)
        {
            return ResolveColumnName(u.Operand);
        }

        throw new NotSupportedException($"SQLiteFTS5Builder.Column expects a property reference like a.Title for its first argument, got: {expr}");
    }

    private static string EscapeTerm(string term)
    {
        if (NeedsQuoting(term))
        {
            return "\"" + term.Replace("\"", "\"\"") + "\"";
        }

        return term;
    }

    private static bool NeedsQuoting(string term)
    {
        if (string.IsNullOrEmpty(term) || term is "AND" or "OR" or "NOT" or "NEAR")
        {
            return true;
        }

        foreach (char ch in term)
        {
            if (!char.IsLetterOrDigit(ch) && ch != '_')
            {
                return true;
            }
        }

        return false;
    }
}
