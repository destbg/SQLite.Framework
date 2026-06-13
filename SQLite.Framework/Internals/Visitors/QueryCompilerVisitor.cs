namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Compiles SQL expressions into executable expressions.
/// </summary>
/// <remarks>
/// This is only used for the select expressions to allow the end user to mix and match
/// code that will be executed inside the SQLite database and code that will be executed
/// inside the C# code.
/// </remarks>
internal class QueryCompilerVisitor : ExpressionVisitor
{
    private static readonly MethodInfo BinaryAdditionOperator;
    private static readonly MethodInfo BinarySubtractionOperator;
    private static readonly MethodInfo BinaryMultiplyOperator;
    private static readonly MethodInfo BinaryDivisionOperator;
    private static readonly MethodInfo BinaryModulusOperator;
    private static readonly MethodInfo BinaryNegationOperator;
    private static readonly MethodInfo BinaryBitwiseAndOperator;
    private static readonly MethodInfo BinaryBitwiseOrOperator;
    private static readonly MethodInfo BinaryExclusiveOrOperator;
    private static readonly MethodInfo BinaryLeftShiftOperator;
    private static readonly MethodInfo BinaryRightShiftOperator;
    private static readonly Dictionary<(Type, MethodInfo), MethodInfo> ConcreteMethodCache = [];

    private readonly IReadOnlyCollection<ParameterExpression>? inputParameters;
    private readonly SQLiteOptions options;

    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(QueryCompilerVisitor))]
    static QueryCompilerVisitor()
    {
        BinaryAdditionOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryAdditionOperator), BindingFlags.Static | BindingFlags.NonPublic)!;
        BinarySubtractionOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinarySubtractionOperator), BindingFlags.Static | BindingFlags.NonPublic)!;
        BinaryMultiplyOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryMultiplyOperator), BindingFlags.Static | BindingFlags.NonPublic)!;
        BinaryDivisionOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryDivisionOperator), BindingFlags.Static | BindingFlags.NonPublic)!;
        BinaryModulusOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryModulusOperator), BindingFlags.Static | BindingFlags.NonPublic)!;
        BinaryNegationOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryNegationOperator), BindingFlags.Static | BindingFlags.NonPublic)!;
        BinaryBitwiseAndOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryBitwiseAndOperator), BindingFlags.Static | BindingFlags.NonPublic)!;
        BinaryBitwiseOrOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryBitwiseOrOperator), BindingFlags.Static | BindingFlags.NonPublic)!;
        BinaryExclusiveOrOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryExclusiveOrOperator), BindingFlags.Static | BindingFlags.NonPublic)!;
        BinaryLeftShiftOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryLeftShiftOperator), BindingFlags.Static | BindingFlags.NonPublic)!;
        BinaryRightShiftOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryRightShiftOperator), BindingFlags.Static | BindingFlags.NonPublic)!;
    }

    public QueryCompilerVisitor(SQLiteOptions options, IReadOnlyCollection<ParameterExpression>? inputParameters = null)
    {
        if (options.ReflectionFallbackDisabled)
        {
            throw new InvalidOperationException(
                "The runtime query compiler (reflection fallback) was invoked while ReflectionFallbackDisabled is set.");
        }

        this.inputParameters = inputParameters;
        this.options = options;
    }

    public Expression VisitSQLExpression(SQLiteExpression node)
    {
        return new CompiledExpression(node.Type, ctx =>
        {
            int index = ctx.Columns![node.IdentifierText];
            return ctx.Reader!.GetValue(index, ctx.Reader.GetColumnType(index), node.Type);
        });
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        CompiledExpression left = (CompiledExpression)Visit(node.Left);
        CompiledExpression right = (CompiledExpression)Visit(node.Right);

        return new CompiledExpression(node.Type, ctx =>
        {
            object? leftValue = left.Call(ctx);
            object? rightValue = right.Call(ctx);

            if (node.NodeType == ExpressionType.ArrayIndex)
            {
                if (leftValue is Array array && rightValue is int index)
                {
                    return array.GetValue(index);
                }

                throw new InvalidOperationException("Array index operation requires an array on the left and an integer index on the right.");
            }

            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    return Equals(leftValue, rightValue);
                case ExpressionType.NotEqual:
                    return !Equals(leftValue, rightValue);
                case ExpressionType.Coalesce:
                    return leftValue ?? rightValue;
                case ExpressionType.AndAlso:
                    return (bool)leftValue! && (bool)rightValue!;
                case ExpressionType.OrElse:
                    return (bool)leftValue! || (bool)rightValue!;
                case ExpressionType.And:
                    return ApplyLiftedBitwise(BinaryBitwiseAndOperator, leftValue, rightValue, options);
                case ExpressionType.Or:
                    return ApplyLiftedBitwise(BinaryBitwiseOrOperator, leftValue, rightValue, options);
                case ExpressionType.GreaterThan:
                    return leftValue != null && rightValue != null && CompareValues(leftValue, rightValue) > 0;
                case ExpressionType.GreaterThanOrEqual:
                    return leftValue != null && rightValue != null && CompareValues(leftValue, rightValue) >= 0;
                case ExpressionType.LessThan:
                    return leftValue != null && rightValue != null && CompareValues(leftValue, rightValue) < 0;
                case ExpressionType.LessThanOrEqual:
                    return leftValue != null && rightValue != null && CompareValues(leftValue, rightValue) <= 0;
            }

            bool isLiftedArithmetic = node.NodeType is ExpressionType.ExclusiveOr
                or ExpressionType.Add or ExpressionType.AddChecked
                or ExpressionType.Subtract or ExpressionType.SubtractChecked
                or ExpressionType.Multiply or ExpressionType.MultiplyChecked
                or ExpressionType.Divide or ExpressionType.Modulo
                or ExpressionType.LeftShift or ExpressionType.RightShift;

            if (!isLiftedArithmetic)
            {
                throw new NotSupportedException($"The binary operator '{node.NodeType}' is not supported.");
            }

            if (leftValue == null || rightValue == null)
            {
                return null;
            }

            if (node.Method != null)
            {
                return InvokeUnwrapped(node.Method, null, [leftValue, rightValue]);
            }

            return node.NodeType switch
            {
                ExpressionType.ExclusiveOr => InvokeOperator(BinaryExclusiveOrOperator, leftValue, rightValue, options),
                ExpressionType.Add => InvokeOperator(BinaryAdditionOperator, leftValue, rightValue, options),
                ExpressionType.AddChecked => InvokeCheckedArithmetic(ExpressionType.Add, leftValue, rightValue, BinaryAdditionOperator, options),
                ExpressionType.Subtract => InvokeOperator(BinarySubtractionOperator, leftValue, rightValue, options),
                ExpressionType.SubtractChecked => InvokeCheckedArithmetic(ExpressionType.Subtract, leftValue, rightValue, BinarySubtractionOperator, options),
                ExpressionType.Multiply => InvokeOperator(BinaryMultiplyOperator, leftValue, rightValue, options),
                ExpressionType.MultiplyChecked => InvokeCheckedArithmetic(ExpressionType.Multiply, leftValue, rightValue, BinaryMultiplyOperator, options),
                ExpressionType.Divide => InvokeOperator(BinaryDivisionOperator, leftValue, rightValue, options),
                ExpressionType.Modulo => InvokeOperator(BinaryModulusOperator, leftValue, rightValue, options),
                ExpressionType.LeftShift => InvokeOperator(BinaryLeftShiftOperator, leftValue, rightValue, options),
                ExpressionType.RightShift => InvokeOperator(BinaryRightShiftOperator, leftValue, rightValue, options),
                _ => throw new NotSupportedException($"The binary operator '{node.NodeType}' is not supported.")
            };
        });
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        CompiledExpression test = (CompiledExpression)Visit(node.Test);
        CompiledExpression ifTrue = (CompiledExpression)Visit(node.IfTrue);
        CompiledExpression ifFalse = (CompiledExpression)Visit(node.IfFalse);

        return new CompiledExpression(node.Type, ctx =>
        {
            return (bool)test.Call(ctx)! ? ifTrue.Call(ctx) : ifFalse.Call(ctx);
        });
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        return new CompiledExpression(node.Type, _ => node.Value);
    }

    protected override Expression VisitBlock(BlockExpression node)
    {
        throw new NotSupportedException($"The block expression '{node}' is not supported.");
    }

    protected override Expression VisitDefault(DefaultExpression node)
    {
        throw new NotSupportedException($"The default expression '{node}' is not supported.");
    }

    protected override Expression VisitDynamic(DynamicExpression node)
    {
        throw new NotSupportedException($"The dynamic expression '{node}' is not supported.");
    }

    protected override Expression VisitExtension(Expression node)
    {
        throw new NotSupportedException($"The extension method '{node}' is not supported.");
    }

    protected override Expression VisitGoto(GotoExpression node)
    {
        throw new NotSupportedException($"The goto expression '{node}' is not supported.");
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        // Array[Index] = value
        throw new NotSupportedException($"The index expression '{node}' is not supported.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "List type should be preserved")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Field should be preserved")]
    protected override Expression VisitMember(MemberExpression node)
    {
        if (ExpressionHelpers.IsConstant(node))
        {
            object? value = ExpressionHelpers.GetConstantValue(node);
            return new CompiledExpression(node.Type, _ => value);
        }

        if (node.Expression != null
            && node.Member.Name == nameof(Nullable<int>.HasValue)
            && IsNullableValueType(node.Member.DeclaringType))
        {
            CompiledExpression nullableInner = (CompiledExpression)Visit(node.Expression);
            return new CompiledExpression(node.Type, ctx => nullableInner.Call(ctx) != null);
        }

        CompiledExpression innerExpression = (CompiledExpression)Visit(node.Expression!);

        return new CompiledExpression(node.Type, context =>
        {
            object? instance = innerExpression.Call(context);
            return node.Member is FieldInfo field
                ? field.GetValue(instance)
                : ((PropertyInfo)node.Member).GetValue(instance);
        });
    }

    protected override Expression VisitInvocation(InvocationExpression node)
    {
        throw new NotSupportedException($"The invocation expression '{node}' is not supported.");
    }

    protected override Expression VisitLabel(LabelExpression node)
    {
        throw new NotSupportedException($"The label expression '{node}' is not supported.");
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        throw new NotSupportedException($"The lambda expression '{node}' is not supported.");
    }

    protected override Expression VisitLoop(LoopExpression node)
    {
        throw new NotSupportedException($"The loop expression '{node}' is not supported.");
    }

    protected override Expression VisitNew(NewExpression node)
    {
        if (node.Constructor != null)
        {
            CompiledExpression[] compiledArgs = node.Arguments
                .Select(arg => (CompiledExpression)Visit(arg))
                .ToArray();

            return new CompiledExpression(node.Type, ctx =>
            {
                object?[] arguments = compiledArgs.Select(f => f.Call(ctx)).ToArray();
                return node.Constructor.Invoke(arguments);
            });
        }

        throw new NotSupportedException($"The new expression '{node}' is not supported.");
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (inputParameters != null && inputParameters.Contains(node))
        {
            return new CompiledExpression(node.Type, ctx => ctx.Input);
        }

        throw new NotSupportedException($"The parameter expression '{node}' is not supported.");
    }

    protected override Expression VisitSwitch(SwitchExpression node)
    {
        throw new NotSupportedException($"The switch expression '{node}' is not supported.");
    }

    protected override Expression VisitTry(TryExpression node)
    {
        throw new NotSupportedException($"The try expression '{node}' is not supported.");
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        CompiledExpression operand = (CompiledExpression)Visit(node.Operand);

        return new CompiledExpression(node.Type, ctx =>
        {
            object? operandValue = operand.Call(ctx);

            if (operandValue == null
                && node.NodeType is ExpressionType.Negate or ExpressionType.NegateChecked
                or ExpressionType.Not or ExpressionType.OnesComplement)
            {
                return null;
            }

            return node.NodeType switch
            {
                ExpressionType.Negate or ExpressionType.NegateChecked => node.Method != null
                    ? InvokeUnwrapped(node.Method, null, [operandValue])
                    : InvokeUnaryOperator(BinaryNegationOperator, operandValue!, options),
                ExpressionType.Not => operandValue is bool b ? !b : ApplyOnesComplement(operandValue!),
                ExpressionType.OnesComplement => ApplyOnesComplement(operandValue!),
                ExpressionType.Convert => ConvertOperand(operandValue, node.Type, checkedConversion: false),
                ExpressionType.ConvertChecked => ConvertOperand(operandValue, node.Type, checkedConversion: true),
                ExpressionType.TypeAs => node.Type.IsInstanceOfType(operandValue) ? operandValue : null,
                _ => throw new NotSupportedException($"The unary operator '{node.NodeType}' is not supported.")
            };
        });
    }

    protected override CatchBlock VisitCatchBlock(CatchBlock node)
    {
        throw new NotSupportedException($"The catch block '{node}' is not supported.");
    }

    protected override Expression VisitDebugInfo(DebugInfoExpression node)
    {
        throw new NotSupportedException($"The debug info expression '{node}' is not supported.");
    }

    [return: NotNullIfNotNull(nameof(node))]
    protected override LabelTarget? VisitLabelTarget(LabelTarget? node)
    {
        if (node == null)
        {
            return null;
        }

        throw new NotSupportedException($"The label target '{node}' is not supported.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "List type should be preserved")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Method should be preserved")]
    protected override Expression VisitListInit(ListInitExpression node)
    {
        CompiledExpression newExpression = (CompiledExpression)Visit(node.NewExpression);
        List<(MethodInfo AddMethod, CompiledExpression[] Arguments)> initializers = node.Initializers
            .Select(init => (init.AddMethod, init.Arguments.Select(arg => (CompiledExpression)Visit(arg)).ToArray()))
            .ToList();

        return new CompiledExpression(node.Type, ctx =>
        {
            object? instance = newExpression.Call(ctx);
            foreach ((MethodInfo addMethod, CompiledExpression[] arguments) in initializers)
            {
                addMethod.Invoke(instance, arguments.Select(arg => arg.Call(ctx)).ToArray());
            }

            return instance;
        });
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        throw new NotSupportedException($"The member assignment '{node}' is not supported.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "DTO types should be preserved")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Field should be preserved")]
    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        CompiledExpression newExpression = (CompiledExpression)Visit(node.NewExpression);
        List<(MemberBinding, CompiledExpression)> bindings = node.Bindings
            .Where(b => b.BindingType != MemberBindingType.ListBinding)
            .Select(f => (f, VisitMemberBindingExpression(f)))
            .ToList();
        List<(MemberInfo Member, List<(MethodInfo AddMethod, CompiledExpression[] Args)> Initializers)> listBindings = [];
        foreach (MemberListBinding lb in node.Bindings.OfType<MemberListBinding>())
        {
            List<(MethodInfo AddMethod, CompiledExpression[] Args)> initializers = [];
            foreach (ElementInit init in lb.Initializers)
            {
                CompiledExpression[] args = init.Arguments.Select(a => (CompiledExpression)Visit(a)).ToArray();
                initializers.Add((init.AddMethod, args));
            }
            listBindings.Add((lb.Member, initializers));
        }

        return new CompiledExpression(node.Type, ctx =>
        {
            object? instance = newExpression.Call(ctx);
            foreach ((MemberBinding binding, CompiledExpression expression) in bindings)
            {
                if (binding is MemberMemberBinding)
                {
                    expression.Call(ctx);
                }
                else if (binding.Member is FieldInfo fieldInfo)
                {
                    object? value = expression.Call(ctx);
                    fieldInfo.SetValue(instance, value);
                }
                else
                {
                    PropertyInfo propertyInfo = (PropertyInfo)binding.Member;
                    object? value = expression.Call(ctx);
                    propertyInfo.SetValue(instance, value);
                }
            }

            foreach ((MemberInfo member, List<(MethodInfo AddMethod, CompiledExpression[] Args)> initializers) in listBindings)
            {
                object? collection = member is PropertyInfo mp ? mp.GetValue(instance) : ((FieldInfo)member).GetValue(instance);
                foreach ((MethodInfo addMethod, CompiledExpression[] args) in initializers)
                {
                    addMethod.Invoke(collection, args.Select(a => a.Call(ctx)).ToArray());
                }
            }

            return instance;
        });
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Methods should be preserved")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Method should be preserved")]
    [UnconditionalSuppressMessage("AOT", "IL2062", Justification = "The nullable underlying type is a value type with a default constructor.")]
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        CompiledExpression? instance = Visit(node.Object) as CompiledExpression;
        CompiledExpression[] arguments = node.Arguments
            .Select(arg => (CompiledExpression)Visit(arg))
            .ToArray();

        if (instance != null && IsNullableValueType(node.Object!.Type))
        {
            Type underlying = node.Object.Type.GetGenericArguments()[0];
            string methodName = node.Method.Name;
            return new CompiledExpression(node.Type, ctx =>
            {
                object? boxed = instance.Call(ctx);
                return methodName switch
                {
                    nameof(Nullable<int>.GetValueOrDefault) => boxed ?? (arguments.Length == 1 ? arguments[0].Call(ctx) : Activator.CreateInstance(underlying)),
                    nameof(object.GetHashCode) => boxed?.GetHashCode() ?? 0,
                    nameof(object.Equals) => Equals(boxed, arguments.Length == 1 ? arguments[0].Call(ctx) : null),
                    nameof(object.ToString) => boxed?.ToString() ?? string.Empty,
                    _ => boxed == null
                        ? throw new InvalidOperationException("Nullable object must have a value.")
                        : InvokeUnwrapped(node.Method, boxed, arguments.Select(arg => arg.Call(ctx)).ToArray()),
                };
            });
        }

        return new CompiledExpression(node.Type, ctx =>
        {
            object?[] args = arguments.Select(arg => arg.Call(ctx)).ToArray();
            return InvokeUnwrapped(node.Method, instance?.Call(ctx), args);
        });
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Array types should be preserved")]
    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        CompiledExpression[] expressions = node.Expressions
            .Select(arg => (CompiledExpression)Visit(arg))
            .ToArray();

        return new CompiledExpression(node.Type, ctx =>
        {
            object?[] args = expressions.Select(arg => arg.Call(ctx)).ToArray();

            Array arr = Array.CreateInstance(node.Type.GetElementType()!, args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                arr.SetValue(args[i], i);
            }

            return arr;
        });
    }

    protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
    {
        throw new NotSupportedException($"The runtime variables expression '{node}' is not supported.");
    }

    protected override SwitchCase VisitSwitchCase(SwitchCase node)
    {
        throw new NotSupportedException($"The switch case '{node}' is not supported.");
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        CompiledExpression operand = (CompiledExpression)Visit(node.Expression);
        return new CompiledExpression(typeof(bool), ctx =>
        {
            object? value = operand.Call(ctx);
            return node.NodeType == ExpressionType.TypeIs
                ? node.TypeOperand.IsInstanceOfType(value)
                : value?.GetType() == node.TypeOperand;
        });
    }

    protected override ElementInit VisitElementInit(ElementInit node)
    {
        throw new NotSupportedException($"The element init expression '{node}' is not supported.");
    }

    protected override MemberBinding VisitMemberBinding(MemberBinding node)
    {
        throw new NotSupportedException($"The member binding '{node}' is not supported.");
    }

    protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
    {
        throw new NotSupportedException($"The member list binding '{node}' is not supported.");
    }

    protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
    {
        throw new NotSupportedException($"The member member binding '{node}' is not supported.");
    }

    private CompiledExpression VisitMemberBindingExpression(MemberBinding node)
    {
        return node.BindingType switch
        {
            MemberBindingType.Assignment => VisitMemberAssignmentExpression((MemberAssignment)node),
            MemberBindingType.MemberBinding => VisitMemberMemberBindingExpression((MemberMemberBinding)node),
            MemberBindingType.ListBinding => VisitMemberListBindingExpression((MemberListBinding)node),
            _ => throw new Exception("Invalid member binding type.")
        };
    }

    private CompiledExpression VisitMemberAssignmentExpression(MemberAssignment node)
    {
        CompiledExpression expression = (CompiledExpression)Visit(node.Expression);
        return new CompiledExpression(node.Expression.Type, ctx => expression.Call(ctx));
    }

    private CompiledExpression VisitMemberMemberBindingExpression(MemberMemberBinding node)
    {
        List<CompiledExpression> bindings = node.Bindings
            .Select(VisitMemberBindingExpression)
            .ToList();

        Type type = node.Member is PropertyInfo property ? property.PropertyType : ((FieldInfo)node.Member).FieldType;

        return new CompiledExpression(type, ctx =>
        {
            foreach (CompiledExpression binding in bindings)
            {
                binding.Call(ctx);
            }

            return null;
        });
    }

    private CompiledExpression VisitMemberListBindingExpression(MemberListBinding node)
    {
        throw new NotSupportedException($"List binding '{node.Member.Name}' is not supported inside a nested member binding.");
    }

    private static object? InvokeUnwrapped(MethodInfo method, object? instance, object?[] args)
    {
        try
        {
            return method.Invoke(instance, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private static object? ApplyLiftedBitwise(MethodInfo openMethod, object? left, object? right, SQLiteOptions options)
    {
        if (left is bool || right is bool)
        {
            bool? leftBool = (bool?)left;
            bool? rightBool = (bool?)right;

            if (openMethod == BinaryBitwiseAndOperator)
            {
                if (leftBool == false || rightBool == false)
                {
                    return false;
                }

                return leftBool == null || rightBool == null ? null : true;
            }

            if (leftBool == true || rightBool == true)
            {
                return true;
            }

            return leftBool == null || rightBool == null ? null : false;
        }

        if (left == null || right == null)
        {
            return null;
        }

        return InvokeOperator(openMethod, left, right, options);
    }

    private static object InvokeCheckedArithmetic(ExpressionType op, object left, object right, MethodInfo fallbackOpen, SQLiteOptions options)
    {
        checked
        {
            switch (left)
            {
                case int l:
                    return op switch
                    {
                        ExpressionType.Add => l + (int)right,
                        ExpressionType.Subtract => l - (int)right,
                        _ => l * (int)right
                    };
                case long l:
                    return op switch
                    {
                        ExpressionType.Add => l + (long)right,
                        ExpressionType.Subtract => l - (long)right,
                        _ => l * (long)right
                    };
                case uint l:
                    return op switch
                    {
                        ExpressionType.Add => l + (uint)right,
                        ExpressionType.Subtract => l - (uint)right,
                        _ => l * (uint)right
                    };
                case ulong l:
                    return op switch
                    {
                        ExpressionType.Add => l + (ulong)right,
                        ExpressionType.Subtract => l - (ulong)right,
                        _ => l * (ulong)right
                    };
            }
        }

        return InvokeOperator(fallbackOpen, left, right, options)!;
    }

    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Fallback path for non-primitive types; may require user-supplied DynamicDependency hints under AOT.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Fallback path for non-primitive types; may require user-supplied DynamicDependency hints under AOT.")]
    private static object? InvokeOperator(MethodInfo openMethod, object left, object right, SQLiteOptions options)
    {
        if (openMethod == BinaryAdditionOperator)
        {
            return left switch
            {
                int l => l + (int)right,
                long l => l + (long)right,
                double l => l + (double)right,
                float l => l + (float)right,
                decimal l => l + (decimal)right,
                short l => (short)(l + (short)right),
                ushort l => (ushort)(l + (ushort)right),
                byte l => (byte)(l + (byte)right),
                sbyte l => (sbyte)(l + (sbyte)right),
                uint l => l + (uint)right,
                ulong l => l + (ulong)right,
                _ => InvokeGenericOperator(openMethod, left, right, options)
            };
        }

        if (openMethod == BinarySubtractionOperator)
        {
            return left switch
            {
                int l => l - (int)right,
                long l => l - (long)right,
                double l => l - (double)right,
                float l => l - (float)right,
                decimal l => l - (decimal)right,
                short l => (short)(l - (short)right),
                ushort l => (ushort)(l - (ushort)right),
                byte l => (byte)(l - (byte)right),
                sbyte l => (sbyte)(l - (sbyte)right),
                uint l => l - (uint)right,
                ulong l => l - (ulong)right,
                _ => InvokeGenericOperator(openMethod, left, right, options)
            };
        }

        if (openMethod == BinaryMultiplyOperator)
        {
            return left switch
            {
                int l => l * (int)right,
                long l => l * (long)right,
                double l => l * (double)right,
                float l => l * (float)right,
                decimal l => l * (decimal)right,
                short l => (short)(l * (short)right),
                ushort l => (ushort)(l * (ushort)right),
                byte l => (byte)(l * (byte)right),
                sbyte l => (sbyte)(l * (sbyte)right),
                uint l => l * (uint)right,
                ulong l => l * (ulong)right,
                _ => InvokeGenericOperator(openMethod, left, right, options)
            };
        }

        if (openMethod == BinaryDivisionOperator)
        {
            return left switch
            {
                int l => l / (int)right,
                long l => l / (long)right,
                double l => l / (double)right,
                float l => l / (float)right,
                decimal l => l / (decimal)right,
                short l => (short)(l / (short)right),
                ushort l => (ushort)(l / (ushort)right),
                byte l => (byte)(l / (byte)right),
                sbyte l => (sbyte)(l / (sbyte)right),
                uint l => l / (uint)right,
                ulong l => l / (ulong)right,
                _ => InvokeGenericOperator(openMethod, left, right, options)
            };
        }

        if (openMethod == BinaryModulusOperator)
        {
            return left switch
            {
                int l => l % (int)right,
                long l => l % (long)right,
                double l => l % (double)right,
                float l => l % (float)right,
                decimal l => l % (decimal)right,
                short l => (short)(l % (short)right),
                ushort l => (ushort)(l % (ushort)right),
                byte l => (byte)(l % (byte)right),
                sbyte l => (sbyte)(l % (sbyte)right),
                uint l => l % (uint)right,
                ulong l => l % (ulong)right,
                _ => InvokeGenericOperator(openMethod, left, right, options)
            };
        }

        if (openMethod == BinaryBitwiseAndOperator)
        {
            return left switch
            {
                bool l => l & (bool)right,
                int l => l & (int)right,
                long l => l & (long)right,
                uint l => l & (uint)right,
                ulong l => l & (ulong)right,
                _ => InvokeGenericOperator(openMethod, left, right, options)
            };
        }

        if (openMethod == BinaryBitwiseOrOperator)
        {
            return left switch
            {
                bool l => l | (bool)right,
                int l => l | (int)right,
                long l => l | (long)right,
                uint l => l | (uint)right,
                ulong l => l | (ulong)right,
                _ => InvokeGenericOperator(openMethod, left, right, options)
            };
        }

        if (openMethod == BinaryExclusiveOrOperator)
        {
            return left switch
            {
                bool l => l ^ (bool)right,
                int l => l ^ (int)right,
                long l => l ^ (long)right,
                uint l => l ^ (uint)right,
                ulong l => l ^ (ulong)right,
                _ => InvokeGenericOperator(openMethod, left, right, options)
            };
        }

        if (openMethod == BinaryLeftShiftOperator)
        {
            return left switch
            {
                int l => l << (int)right,
                long l => l << (int)right,
                uint l => l << (int)right,
                ulong l => l << (int)right,
                _ => InvokeGenericOperator(openMethod, left, right, options)
            };
        }

        if (openMethod == BinaryRightShiftOperator)
        {
            return left switch
            {
                int l => l >> (int)right,
                long l => l >> (int)right,
                uint l => l >> (int)right,
                ulong l => l >> (int)right,
                _ => InvokeGenericOperator(openMethod, left, right, options)
            };
        }

        return InvokeGenericOperator(openMethod, left, right, options);
    }

    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Fallback path for non-primitive types; may require user-supplied DynamicDependency hints under AOT.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Fallback path for non-primitive types; may require user-supplied DynamicDependency hints under AOT.")]
    private static object? InvokeUnaryOperator(MethodInfo openMethod, object operand, SQLiteOptions options)
    {
        if (openMethod == BinaryNegationOperator)
        {
            return operand switch
            {
                int o => -o,
                long o => -o,
                double o => -o,
                float o => -o,
                decimal o => -o,
                short o => -o,
                sbyte o => -o,
                _ => InvokeGenericUnaryOperator(openMethod, operand, options)
            };
        }

        return InvokeGenericUnaryOperator(openMethod, operand, options);
    }

    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Fallback path for non-primitive types; user types implementing IAdditionOperators etc. must supply DynamicDependency hints under AOT.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Fallback path for non-primitive types; user types implementing IAdditionOperators etc. must supply DynamicDependency hints under AOT.")]
    private static object? InvokeGenericOperator(MethodInfo openMethod, object left, object right, SQLiteOptions options)
    {
        Type type = left.GetType();
        MethodInfo concrete;
        lock (ConcreteMethodCache)
        {
            if (!ConcreteMethodCache.TryGetValue((type, openMethod), out concrete!))
            {
                ThrowIfDynamicCodeUnsupported(openMethod, type, options);
                concrete = openMethod.MakeGenericMethod(type);
                ConcreteMethodCache[(type, openMethod)] = concrete;
            }
        }

        return concrete.Invoke(null, [left, right]);
    }

    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Fallback path for non-primitive types; user types implementing IUnaryNegationOperators must supply DynamicDependency hints under AOT.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Fallback path for non-primitive types; user types implementing IUnaryNegationOperators must supply DynamicDependency hints under AOT.")]
    private static object? InvokeGenericUnaryOperator(MethodInfo openMethod, object operand, SQLiteOptions options)
    {
        Type type = operand.GetType();
        MethodInfo concrete;
        lock (ConcreteMethodCache)
        {
            if (!ConcreteMethodCache.TryGetValue((type, openMethod), out concrete!))
            {
                ThrowIfDynamicCodeUnsupported(openMethod, type, options);
                concrete = openMethod.MakeGenericMethod(type);
                ConcreteMethodCache[(type, openMethod)] = concrete;
            }
        }

        return concrete.Invoke(null, [operand]);
    }

    private static void ThrowIfDynamicCodeUnsupported(MethodInfo openMethod, Type type, SQLiteOptions options)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported && options.EntityMaterializers.Count == 0)
        {
            throw new NotSupportedException(
                $"Translating '{openMethod.DeclaringType!.Name}.{openMethod.Name}' for runtime type '{type.FullName}' " +
                "requires runtime code generation, which is unavailable under NativeAOT (PublishAot=true). " +
                "Use the SQLite.Framework source generator and call UseGeneratedMaterializers, or rewrite the query so " +
                "this operator is not invoked dynamically at runtime.");
        }
    }

    private static int CompareValues(object? left, object? right)
    {
        if (left is IComparable comparable)
        {
            return comparable.CompareTo(right);
        }
        else if (right is IComparable comparable2)
        {
            return comparable2.CompareTo(left);
        }
        else if (left == null && right == null)
        {
            return 0;
        }

        throw new NotSupportedException($"Cannot compare values of type '{left?.GetType()}'.");
    }

    private static T ApplyBinaryAdditionOperator<T>(T left, T right)
        where T : IAdditionOperators<T, T, T>
    {
        return left + right;
    }

    private static T ApplyBinarySubtractionOperator<T>(T left, T right)
        where T : ISubtractionOperators<T, T, T>
    {
        return left - right;
    }

    private static T ApplyBinaryMultiplyOperator<T>(T left, T right)
        where T : IMultiplyOperators<T, T, T>
    {
        return left * right;
    }

    private static T ApplyBinaryDivisionOperator<T>(T left, T right)
        where T : IDivisionOperators<T, T, T>
    {
        return left / right;
    }

    private static T ApplyBinaryModulusOperator<T>(T left, T right)
        where T : IModulusOperators<T, T, T>
    {
        return left % right;
    }

    private static T ApplyBinaryNegationOperator<T>(T operand)
        where T : IUnaryNegationOperators<T, T>
    {
        return -operand;
    }

    private static T ApplyBinaryBitwiseAndOperator<T>(T left, T right)
        where T : IBitwiseOperators<T, T, T>
    {
        return left & right;
    }

    private static T ApplyBinaryBitwiseOrOperator<T>(T left, T right)
        where T : IBitwiseOperators<T, T, T>
    {
        return left | right;
    }

    private static T ApplyBinaryExclusiveOrOperator<T>(T left, T right)
        where T : IBitwiseOperators<T, T, T>
    {
        return left ^ right;
    }

    private static T ApplyBinaryLeftShiftOperator<T>(T left, int right)
        where T : IShiftOperators<T, int, T>
    {
        return left << right;
    }

    private static T ApplyBinaryRightShiftOperator<T>(T left, int right)
        where T : IShiftOperators<T, int, T>
    {
        return left >> right;
    }

    private static bool IsNullableValueType(Type? type)
    {
        return type is { IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    private static object? ConvertOperand(object? value, Type targetType, bool checkedConversion)
    {
        if (value == null)
        {
            return null;
        }

        Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying.IsInstanceOfType(value))
        {
            return value;
        }

        if (underlying.IsEnum)
        {
            object numeric = value is Enum
                ? Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()), CultureInfo.InvariantCulture)
                : value;
            return Enum.ToObject(underlying, numeric);
        }

        if (value is double d && IsIntegerType(underlying))
        {
            value = Math.Truncate(d);
        }
        else if (value is float f && IsIntegerType(underlying))
        {
            value = (float)Math.Truncate(f);
        }

        if (!checkedConversion
            && IsIntegerType(underlying)
            && ExpressionHelpers.TryUncheckedIntegerConvert(value, underlying, out object? wrapped))
        {
            return wrapped;
        }

        return Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
    }

    private static object ApplyOnesComplement(object operand)
    {
        return operand switch
        {
            int o => ~o,
            long o => ~o,
            uint o => ~o,
            ulong o => ~o,
            short o => ~o,
            ushort o => ~o,
            byte o => ~o,
            sbyte o => ~o,
            _ => throw new NotSupportedException($"Cannot apply the complement operator to type '{operand.GetType()}'.")
        };
    }

    private static bool IsIntegerType(Type type)
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
}
