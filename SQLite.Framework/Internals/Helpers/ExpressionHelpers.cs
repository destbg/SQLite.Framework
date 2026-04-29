using System.Text;

namespace SQLite.Framework.Internals.Helpers;

internal static class ExpressionHelpers
{
    public static (string Path, ParameterExpression Parameter) ResolveParameterPath(Expression node)
    {
        List<string> paths = [];
        Expression? innerExpression = node;

        while (innerExpression is MemberExpression me2)
        {
            paths.Add(me2.Member.Name);
            innerExpression = me2.Expression;
        }

        StringBuilder pathBuilder = StringBuilderPool.Rent();

        for (int i = paths.Count - 1; i >= 0; i--)
        {
            pathBuilder.Append(paths[i]);
            if (i > 0)
            {
                pathBuilder.Append('.');
            }
        }

        string path = StringBuilderPool.ToStringAndReturn(pathBuilder);

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
            paths.Add(me2.Member.Name);
            innerExpression = me2.Expression;
        }

        StringBuilder pathBuilder = StringBuilderPool.Rent();

        for (int i = paths.Count - 1; i >= 0; i--)
        {
            pathBuilder.Append(paths[i]);
            if (i > 0)
            {
                pathBuilder.Append('.');
            }
        }

        string path = StringBuilderPool.ToStringAndReturn(pathBuilder);

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

    public static SQLiteExpression BracketIfNeeded(SQLiteExpression node)
    {
        return node.RequiresBrackets
            ? new SQLiteExpression(node.Type, node.Identifier, $"({node.Sql})", node.Parameters)
            : node;
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
