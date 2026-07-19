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
            MemberExpression me => me.Expression == null || IsConstant(me.Expression),
            UnaryExpression ue => IsConstant(ue.Operand),
            BinaryExpression { NodeType: ExpressionType.ArrayIndex } bi => IsConstant(bi.Left) && IsConstant(bi.Right),
            NewArrayExpression na => na.Expressions.Select(IsConstant).All(f => f),
            MemberInitExpression mie => mie.Bindings.All(b => b is MemberAssignment ma && IsConstant(ma.Expression)),
            NewExpression ne => ne.Arguments.All(IsConstant),
            ListInitExpression lie => IsConstant(lie.NewExpression)
                && lie.Initializers.All(init => init.Arguments.All(IsConstant)),
            MethodCallExpression { Method.Name: "get_Item" } mce =>
                IsConstant(mce.Object!) && mce.Arguments.All(IsConstant),
            _ => false
        };
    }

    public static object? GetConstantValue(Expression node)
    {
        return node switch
        {
            ConstantExpression ce => ce.Value,
            MemberExpression me => me.Member is FieldInfo fi
                ? (me.Expression != null ? fi.GetValue(GetConstantValue(me.Expression)) : fi.GetValue(null))
                : ((PropertyInfo)me.Member).GetValue(me.Expression != null ? GetConstantValue(me.Expression) : null),
            UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } ue => ConvertConstant(GetConstantValue(ue.Operand), ue.Type, ue.NodeType == ExpressionType.ConvertChecked),
            UnaryExpression { NodeType: ExpressionType.ArrayLength } ue => ((Array)GetConstantValue(ue.Operand)!).Length,
            UnaryExpression { NodeType: ExpressionType.Negate or ExpressionType.NegateChecked } ue => EvaluateUnary(ue),
            UnaryExpression { NodeType: ExpressionType.Not } ue => EvaluateUnary(ue),
            BinaryExpression { NodeType: ExpressionType.ArrayIndex } bi => ((Array)GetConstantValue(bi.Left)!)
                .GetValue(Convert.ToInt32(GetConstantValue(bi.Right), CultureInfo.InvariantCulture)),
            NewArrayExpression na => CreateArray(na),
            MemberInitExpression mie => CreateMember(mie),
            NewExpression ne => CreateNew(ne),
            ListInitExpression lie => CreateListInit(lie),
            MethodCallExpression { Method.Name: "get_Item" } mce => InvokeIndexer(mce),
            _ => throw new NotSupportedException($"Cannot evaluate expression of type {node.NodeType}")
        };
    }

    public static Expression StripQuotes(Expression node)
    {
        while (node is UnaryExpression { NodeType: ExpressionType.Quote } quote)
        {
            node = quote.Operand;
        }

        return node;
    }

    public static Expression StripUpcast(Expression node)
    {
        return node is UnaryExpression { NodeType: ExpressionType.Convert } cast
            && cast.Type.IsAssignableFrom(cast.Operand.Type)
            ? cast.Operand
            : node;
    }

    public static SQLiteExpression BracketIfNeeded(SQLiteExpression node)
    {
        return node.RequiresBrackets
            ? SQLiteExpression.Wrap(node.Type, node.Identifier, "(", node, ")", node.Parameters)
            : node;
    }

    public static bool TryUncheckedIntegerConvert(object value, Type target, out object? result)
    {
        result = null;

        long bits;
        switch (value)
        {
            case sbyte sb:
                bits = sb;
                break;
            case byte b:
                bits = b;
                break;
            case short s:
                bits = s;
                break;
            case ushort us:
                bits = us;
                break;
            case int i:
                bits = i;
                break;
            case uint ui:
                bits = ui;
                break;
            case long l:
                bits = l;
                break;
            case ulong ul:
                bits = unchecked((long)ul);
                break;
            case char c:
                bits = c;
                break;
            default:
                return false;
        }

        if (target == typeof(sbyte))
        {
            result = unchecked((sbyte)bits);
            return true;
        }
        if (target == typeof(byte))
        {
            result = unchecked((byte)bits);
            return true;
        }
        if (target == typeof(short))
        {
            result = unchecked((short)bits);
            return true;
        }
        if (target == typeof(ushort))
        {
            result = unchecked((ushort)bits);
            return true;
        }
        if (target == typeof(int))
        {
            result = unchecked((int)bits);
            return true;
        }
        if (target == typeof(uint))
        {
            result = unchecked((uint)bits);
            return true;
        }
        if (target == typeof(long))
        {
            result = bits;
            return true;
        }
        if (target == typeof(ulong))
        {
            result = unchecked((ulong)bits);
            return true;
        }
        if (target == typeof(char))
        {
            result = unchecked((char)bits);
            return true;
        }

        return false;
    }

    private static object? InvokeIndexer(MethodCallExpression node)
    {
        object? target = GetConstantValue(node.Object!);
        object?[] arguments = [.. node.Arguments.Select(GetConstantValue)];
        return node.Method.Invoke(target, arguments);
    }

    private static object? EvaluateUnary(UnaryExpression node)
    {
        object? operand = GetConstantValue(node.Operand);
        if (operand is null)
        {
            return null;
        }

        bool complement = node.NodeType is ExpressionType.Not;
        bool checkedNegate = node.NodeType is ExpressionType.NegateChecked;
        return operand switch
        {
            bool b => !b,
            int i => complement ? ~i : checkedNegate ? checked(-i) : -i,
            long l => complement ? ~l : checkedNegate ? checked(-l) : -l,
            uint u => ~u,
            ulong ul => ~ul,
            float f => -f,
            double d => -d,
            decimal m => -m,
            _ => throw new NotSupportedException($"Cannot evaluate a constant {node.NodeType} expression on operand type {operand.GetType()}.")
        };
    }

    private static object? ConvertConstant(object? value, Type targetType, bool checkedConversion)
    {
        if (value is null)
        {
            return null;
        }

        Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value.GetType() == underlying)
        {
            return value;
        }

        if (underlying.IsEnum)
        {
            object numeric = value is Enum
                ? Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()))
                : value;
            return Enum.ToObject(underlying, numeric);
        }

        if (IsIntegerTarget(underlying))
        {
            if (value is double dbl)
            {
                value = Math.Truncate(dbl);
            }
            else if (value is float flt)
            {
                value = (float)Math.Truncate(flt);
            }
            else if (value is decimal dec)
            {
                value = Math.Truncate(dec);
            }
        }

        if (!checkedConversion && TryUncheckedIntegerConvert(value, underlying, out object? wrapped))
        {
            return wrapped;
        }

        if (underlying.IsAssignableFrom(value.GetType()))
        {
            return value;
        }

        return Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
    }

    private static bool IsIntegerTarget(Type type)
    {
        return type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(byte)
            || type == typeof(sbyte);
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

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The element type comes from a user array literal and is already rooted.")]
    private static Array CreateArray(NewArrayExpression newArray)
    {
        Type elementType = newArray.Type.GetElementType()!;

        if (newArray.NodeType == ExpressionType.NewArrayBounds)
        {
            int length = Convert.ToInt32(GetConstantValue(newArray.Expressions[0]), CultureInfo.InvariantCulture);
            return Array.CreateInstance(elementType, length);
        }

        Array array = Array.CreateInstance(elementType, newArray.Expressions.Count);
        for (int i = 0; i < newArray.Expressions.Count; i++)
        {
            array.SetValue(GetConstantValue(newArray.Expressions[i]), i);
        }

        return array;
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
