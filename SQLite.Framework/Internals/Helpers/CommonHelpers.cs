using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SQLite.Framework.Internals.Helpers;

internal static class CommonHelpers
{
    public static bool IsSimple(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime);
    }

    public static (string Path, ParameterExpression Parameter) ResolveParameterPath(Expression expression)
    {
        List<string> paths = [];
        Expression? innerExpression = expression;

        while (innerExpression is MemberExpression me2)
        {
            paths.Add($"{me2.Member.Name}");
            innerExpression = me2.Expression;
        }

        StringBuilder pathBuilder = new();

        for (int i = paths.Count - 1; i >= 0; i--)
        {
            pathBuilder.Append(paths[i]);
            if (i > 0)
            {
                pathBuilder.Append('.');
            }
        }

        string path = pathBuilder.ToString();

        if (innerExpression is ParameterExpression pe)
        {
            return (path, pe);
        }

        throw new NotSupportedException($"Cannot translate expression {expression}");
    }

    public static bool IsConstant(Expression expr)
    {
        return expr switch
        {
            ConstantExpression => true,
            MemberExpression me => me.Member switch
            {
                FieldInfo or PropertyInfo => IsConstant(me.Expression!),
                _ => false
            },
            UnaryExpression ue => IsConstant(ue.Operand),
            _ => false
        };
    }

    public static object? GetConstantValue(Expression expr)
    {
        return expr switch
        {
            ConstantExpression ce => ce.Value,
            MemberExpression me => me.Member switch
            {
                FieldInfo fi => fi.GetValue(GetConstantValue(me.Expression!)),
                PropertyInfo pi => pi.GetValue(GetConstantValue(me.Expression!)),
                _ => throw new NotSupportedException($"Unsupported member type: {me.Member.GetType()}")
            },
            UnaryExpression { NodeType: ExpressionType.Convert } ue =>
                Convert.ChangeType(GetConstantValue(ue.Operand), ue.Type),
            _ => throw new NotSupportedException($"Cannot evaluate expression of type {expr.NodeType}")
        };
    }

    public static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }

        return e;
    }

    public static string BracketIfNeeded(string value)
    {
        return value.Contains(' ')
            ? $"({value})"
            : value;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "We are checking the Queryable class")]
    public static Type? GetQueryableType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
        {
            return type.GenericTypeArguments[0];
        }

        return type.GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IQueryable<>))
            ?.GenericTypeArguments[0];
    }
}