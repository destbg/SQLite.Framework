using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Common helper methods for SQLite operations.
/// </summary>
internal static class CommonHelpers
{
    public static bool IsSimple(Type type, SQLiteOptions options)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (options.TypeConverters.ContainsKey(type))
        {
            return true;
        }

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

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Interface lookup is only used for known collection types with registered converters.")]
    public static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        foreach (Type iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return iface.GetGenericArguments()[0];
            }
        }

        return null;
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
            NewArrayExpression na => na.Expressions.Select(IsConstant).All(f => f),
            MemberInitExpression mie => mie.Bindings.All(b => b is MemberAssignment ma && IsConstant(ma.Expression)),
            NewExpression ne => ne.Arguments.All(IsConstant),
            ListInitExpression lie => IsConstant(lie.NewExpression)
                && lie.Initializers.All(init => init.Arguments.All(IsConstant)),
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
            NewArrayExpression na =>
                na.Expressions.Select(GetConstantValue),
            MemberInitExpression mie => CreateMember(mie),
            NewExpression ne => CreateNew(ne),
            ListInitExpression lie => CreateListInit(lie),
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

    public static SQLiteParameter[]? CombineParameters(SQLExpression expression1, SQLExpression expression2)
    {
        if (expression1.Parameters == null && expression2.Parameters == null)
        {
            return null;
        }

        return [.. expression1.Parameters ?? [], .. expression2.Parameters ?? []];
    }

    public static SQLiteParameter[]? CombineParameters(SQLExpression expression1, SQLExpression expression2, SQLExpression expression3)
    {
        if (expression1.Parameters == null && expression2.Parameters == null && expression3.Parameters == null)
        {
            return null;
        }

        return [.. expression1.Parameters ?? [], .. expression2.Parameters ?? [], .. expression3.Parameters ?? []];
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

    public static SQLiteColumnType TypeToSQLiteType(Type type, SQLiteOptions options)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (options.TypeConverters.TryGetValue(type, out ISQLiteTypeConverter? converter))
        {
            return converter.ColumnType;
        }

        return type switch
        {
            _ when type == typeof(string) => SQLiteColumnType.Text,
            _ when type == typeof(byte[]) => SQLiteColumnType.Blob,
            _ when type == typeof(bool) => SQLiteColumnType.Integer,
            _ when type == typeof(char) => SQLiteColumnType.Text,
            _ when type == typeof(DateTime) => SQLiteColumnType.Integer,
            _ when type == typeof(DateTimeOffset) => SQLiteColumnType.Integer,
            _ when type == typeof(DateOnly) && options.DateOnlyStorage == DateOnlyStorageMode.Text => SQLiteColumnType.Text,
            _ when type == typeof(DateOnly) => SQLiteColumnType.Integer,
            _ when type == typeof(TimeOnly) && options.TimeOnlyStorage == TimeOnlyStorageMode.Text => SQLiteColumnType.Text,
            _ when type == typeof(TimeOnly) => SQLiteColumnType.Integer,
            _ when type == typeof(Guid) => SQLiteColumnType.Text,
            _ when type == typeof(TimeSpan) => SQLiteColumnType.Integer,
            _ when type == typeof(decimal) && options.DecimalStorage == DecimalStorageMode.Text => SQLiteColumnType.Text,
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

    /// <summary>
    /// Walks the inner expression tree of an <c>SQLiteFunctions.Match(entity, predicate)</c> call
    /// and turns it into a list of FTS5 query parts. Each part is either a literal string or a SQL
    /// expression that builds an FTS5-quoted token at runtime (for column references inside
    /// <c>f.Term</c>, <c>f.Phrase</c>, and so on).
    /// </summary>
    public static List<FtsQueryPart> RenderFTSMatch(Expression predicate, SQLVisitor visitor)
    {
        FtsRenderState state = new(visitor);
        state.Write(predicate, parentPrecedence: 0);
        state.FlushLiteral();
        return state.Parts;
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "The type should be part of user assembly")]
    private static object CreateMember(MemberInitExpression memberInit)
    {
        object instance = memberInit.NewExpression.Constructor != null
            ? CreateNew(memberInit.NewExpression)
            : Activator.CreateInstance(memberInit.Type)
              ?? throw new InvalidOperationException($"Cannot create instance of type {memberInit.Type}");

        foreach (MemberBinding binding in memberInit.Bindings)
        {
            if (binding is MemberAssignment assignment)
            {
                object? value = GetConstantValue(assignment.Expression);
                if (assignment.Member is PropertyInfo property)
                {
                    property.SetValue(instance, value);
                }
                else if (assignment.Member is FieldInfo field)
                {
                    field.SetValue(instance, value);
                }
                else
                {
                    throw new InvalidOperationException($"Member {assignment.Member.Name} not found in type {memberInit.Type}");
                }
            }
            else
            {
                throw new NotSupportedException($"Unsupported binding type: {binding.GetType()}");
            }
        }

        return instance;
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "NewExpression.Type is rooted by the user's Select lambda.")]
    private static object CreateNew(NewExpression newExpression)
    {
        if (newExpression.Constructor == null)
        {
            return Activator.CreateInstance(newExpression.Type)
                ?? throw new InvalidOperationException($"Cannot create instance of type {newExpression.Type}");
        }

        object?[] args = newExpression.Arguments.Select(GetConstantValue).ToArray();
        return newExpression.Constructor.Invoke(args);
    }

    private static object CreateListInit(ListInitExpression listInit)
    {
        object instance = CreateNew(listInit.NewExpression);

        foreach (ElementInit init in listInit.Initializers)
        {
            object?[] args = init.Arguments.Select(GetConstantValue).ToArray();
            init.AddMethod.Invoke(instance, args);
        }

        return instance;
    }
}