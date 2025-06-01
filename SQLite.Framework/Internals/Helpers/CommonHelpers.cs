using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Models;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Common helper methods for SQLite operations.
/// </summary>
internal static class CommonHelpers
{
    public static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(byte[])
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(TimeSpan)
               || type == typeof(Guid)
               || type == typeof(DateOnly)
               || type == typeof(TimeOnly);
    }

    public static (string Path, ParameterExpression Parameter) ResolveParameterPath(Expression node)
    {
        List<string> paths = [];
        Expression? innerExpression = node;

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

        throw new NotSupportedException($"Cannot translate expression {node}");
    }

    public static (string Path, ParameterExpression? Parameter) ResolveNullableParameterPath(Expression node)
    {
        List<string> paths = [];
        Expression? innerExpression = node;

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

        return (string.Empty, null);
    }

    public static bool IsConstant(Expression node)
    {
        return node switch
        {
            ConstantExpression => true,
            MemberExpression me => me.Member switch
            {
                FieldInfo or PropertyInfo => me.Expression == null || IsConstant(me.Expression),
                _ => false
            },
            UnaryExpression ue => IsConstant(ue.Operand),
            _ => false
        };
    }

    public static object? GetConstantValue(Expression node)
    {
        return node switch
        {
            ConstantExpression ce => ce.Value,
            MemberExpression me => me.Member switch
            {
                FieldInfo fi => me.Expression != null
                    ? fi.GetValue(GetConstantValue(me.Expression))
                    : fi.GetValue(null),
                PropertyInfo pi => me.Expression != null
                    ? pi.GetValue(GetConstantValue(me.Expression))
                    : pi.GetValue(null),
                _ => throw new NotSupportedException($"Unsupported member type: {me.Member.GetType()}")
            },
            UnaryExpression { NodeType: ExpressionType.Convert } ue =>
                Convert.ChangeType(GetConstantValue(ue.Operand), Nullable.GetUnderlyingType(ue.Type) ?? ue.Type),
            _ => throw new NotSupportedException($"Cannot evaluate expression of type {node.NodeType}")
        };
    }

    public static Expression StripQuotes(Expression node)
    {
        while (node.NodeType == ExpressionType.Quote)
        {
            node = ((UnaryExpression)node).Operand;
        }

        return node;
    }

    public static SQLExpression BracketIfNeeded(SQLExpression node)
    {
        return node.RequiresBrackets
            ? new SQLExpression(node.Type, node.Identifier, $"({node.Sql})", node.Parameters)
            : node;
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

    public static SQLiteParameter[]? CombineParameters(SQLExpression expression1, SQLExpression expression2)
    {
        if (expression1.Parameters == null && expression2.Parameters == null)
        {
            return null;
        }

        return [..expression1.Parameters ?? [], ..expression2.Parameters ?? []];
    }

    public static SQLiteParameter[]? CombineParameters(SQLExpression expression1, SQLExpression expression2, SQLExpression expression3)
    {
        if (expression1.Parameters == null && expression2.Parameters == null && expression3.Parameters == null)
        {
            return null;
        }

        return [..expression1.Parameters ?? [], ..expression2.Parameters ?? [], ..expression3.Parameters ?? []];
    }

    public static SQLiteParameter[]? CombineParameters(params SQLExpression[] expressions)
    {
        if (expressions.All(f => f.Parameters == null))
        {
            return null;
        }

        List<SQLiteParameter> parameters = new();
        foreach (SQLExpression expression in expressions)
        {
            if (expression.Parameters != null)
            {
                parameters.AddRange(expression.Parameters);
            }
        }

        return parameters.ToArray();
    }

    public static SQLiteColumnType TypeToSQLiteType(Type type)
    {
        return type switch
        {
            _ when type == typeof(string) => SQLiteColumnType.Text,
            _ when type == typeof(byte[]) => SQLiteColumnType.Blob,
            _ when type == typeof(bool) => SQLiteColumnType.Integer,
            _ when type == typeof(char) => SQLiteColumnType.Text,
            _ when type == typeof(DateTime) => SQLiteColumnType.Integer,
            _ when type == typeof(DateTimeOffset) => SQLiteColumnType.Integer,
            _ when type == typeof(DateOnly) => SQLiteColumnType.Integer,
            _ when type == typeof(TimeOnly) => SQLiteColumnType.Integer,
            _ when type == typeof(Guid) => SQLiteColumnType.Text,
            _ when type == typeof(TimeSpan) => SQLiteColumnType.Integer,
            _ when type == typeof(decimal) => SQLiteColumnType.Real,
            _ when type == typeof(double) => SQLiteColumnType.Real,
            _ when type == typeof(float) => SQLiteColumnType.Real,
            _ when type == typeof(byte) => SQLiteColumnType.Integer,
            _ when type == typeof(int) => SQLiteColumnType.Integer,
            _ when type == typeof(long) => SQLiteColumnType.Integer,
            _ when type == typeof(sbyte) => SQLiteColumnType.Integer,
            _ when type == typeof(short) => SQLiteColumnType.Integer,
            _ when type == typeof(uint) => SQLiteColumnType.Integer,
            _ when type == typeof(ulong) => SQLiteColumnType.Integer,
            _ when type == typeof(ushort) => SQLiteColumnType.Integer,
            _ when type.IsEnum => SQLiteColumnType.Integer,
            _ => throw new NotSupportedException($"The type {type} is not supported.")
        };
    }
}