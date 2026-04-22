using System.Linq.Expressions;
using System.Text;
using SQLite.Framework.Internals.Helpers;

namespace SQLite.Framework.Internals;

/// <summary>
/// Produces a deterministic string that identifies the shape of a Select-projection
/// lambda body. The runtime uses this to look up a generated materializer for the
/// same shape. <c>SQLite.Framework.SourceGenerator</c> computes the identical string
/// at compile time from syntax, so the two keys always match for equivalent shapes.
/// </summary>
internal static class SelectSignature
{
    public static string Compute(Expression expression)
    {
        StringBuilder sb = new();
        AppendSignature(sb, expression);
        return sb.ToString();
    }

    private static void AppendSignature(StringBuilder sb, Expression? expression)
    {
        if (expression == null)
        {
            sb.Append("null");
            return;
        }

        if (expression is MemberExpression capturedMe && CommonHelpers.IsConstant(capturedMe))
        {
            sb.Append("(CapturedValue ").Append(FormatType(expression.Type)).Append(')');
            return;
        }

        if (expression is ListInitExpression capturedLie && CommonHelpers.IsConstant(capturedLie))
        {
            sb.Append("(CapturedValue ").Append(FormatType(expression.Type)).Append(')');
            return;
        }

        sb.Append('(').Append(expression.NodeType).Append(' ').Append(FormatType(expression.Type));
        switch (expression)
        {
            case BinaryExpression be:
                sb.Append(' ');
                AppendSignature(sb, be.Left);
                sb.Append(' ');
                AppendSignature(sb, be.Right);
                break;
            case UnaryExpression ue:
                sb.Append(' ');
                AppendSignature(sb, ue.Operand);
                break;
            case ConditionalExpression ce:
                sb.Append(' ');
                AppendSignature(sb, ce.Test);
                sb.Append(' ');
                AppendSignature(sb, ce.IfTrue);
                sb.Append(' ');
                AppendSignature(sb, ce.IfFalse);
                break;
            case MemberExpression me:
                sb.Append(' ').Append(me.Member.Name);
                sb.Append(' ');
                AppendSignature(sb, me.Expression);
                break;
            case MethodCallExpression mce:
                sb.Append(' ').Append(FormatType(mce.Method.DeclaringType)).Append('.').Append(mce.Method.Name);
                if (mce.Object != null)
                {
                    sb.Append(' ');
                    AppendSignature(sb, mce.Object);
                }
                foreach (Expression arg in mce.Arguments)
                {
                    sb.Append(' ');
                    AppendSignature(sb, arg);
                }
                break;
            case NewExpression ne:
                if (ne.Constructor != null)
                {
                    sb.Append(' ').Append(FormatType(ne.Constructor.DeclaringType));
                }
                foreach (Expression arg in ne.Arguments)
                {
                    sb.Append(' ');
                    AppendSignature(sb, arg);
                }
                if (ne.Members != null)
                {
                    sb.Append(" members=[");
                    for (int i = 0; i < ne.Members.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(',');
                        }
                        sb.Append(ne.Members[i].Name);
                    }
                    sb.Append(']');
                }
                break;
            case MemberInitExpression mie:
                sb.Append(' ');
                AppendSignature(sb, mie.NewExpression);
                foreach (MemberBinding binding in mie.Bindings)
                {
                    sb.Append(' ').Append(binding.BindingType).Append(':').Append(binding.Member.Name);
                    if (binding is MemberAssignment ma)
                    {
                        sb.Append('=');
                        AppendSignature(sb, ma.Expression);
                    }
                }
                break;
            case ParameterExpression pe:
                sb.Append(' ').Append(FormatType(pe.Type));
                break;
            case ConstantExpression:
                break;
            case NewArrayExpression nae:
                foreach (Expression arg in nae.Expressions)
                {
                    sb.Append(' ');
                    AppendSignature(sb, arg);
                }
                break;
            case LambdaExpression le:
                sb.Append(' ');
                foreach (ParameterExpression p in le.Parameters)
                {
                    sb.Append(FormatType(p.Type)).Append(',');
                }
                AppendSignature(sb, le.Body);
                break;
        }
        sb.Append(')');
    }

    private static string FormatType(Type? type)
    {
        if (type == null)
        {
            return "null";
        }

        if (type.IsArray)
        {
            return FormatType(type.GetElementType()) + "[]";
        }

        if (IsAnonymousType(type))
        {
            if (type.IsGenericType)
            {
                string args = string.Join(",", type.GenericTypeArguments.Select(FormatType));
                return "<anonymous<" + args + ">>";
            }
            return "<anonymous>";
        }

        if (type.IsGenericType)
        {
            string def = type.GetGenericTypeDefinition().FullName ?? type.GetGenericTypeDefinition().Name;
            int tick = def.IndexOf('`');
            if (tick >= 0)
            {
                def = def[..tick];
            }

            string args = string.Join(",", type.GenericTypeArguments.Select(FormatType));
            return def + "<" + args + ">";
        }

        return type.FullName ?? type.Name;
    }

    private static bool IsAnonymousType(Type type)
    {
        if (!type.IsGenericType && !type.Name.StartsWith("<>", StringComparison.Ordinal))
        {
            return false;
        }

        return type.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal)
            || type.Name.StartsWith("<>h__TransparentIdentifier", StringComparison.Ordinal);
    }
}
