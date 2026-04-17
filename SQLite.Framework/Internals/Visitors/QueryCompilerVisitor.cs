using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;

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
    private static readonly Dictionary<(Type, MethodInfo), MethodInfo> ConcreteMethodCache = [];

    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(QueryCompilerVisitor))]
    static QueryCompilerVisitor()
    {
        BinaryAdditionOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryAdditionOperator), BindingFlags.Static | BindingFlags.NonPublic)
                                 ?? throw new InvalidOperationException("Binary operator method not found.");
        BinarySubtractionOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinarySubtractionOperator), BindingFlags.Static | BindingFlags.NonPublic)
                                    ?? throw new InvalidOperationException("Binary operator method not found.");
        BinaryMultiplyOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryMultiplyOperator), BindingFlags.Static | BindingFlags.NonPublic)
                                 ?? throw new InvalidOperationException("Binary operator method not found.");
        BinaryDivisionOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryDivisionOperator), BindingFlags.Static | BindingFlags.NonPublic)
                                 ?? throw new InvalidOperationException("Binary operator method not found.");
        BinaryModulusOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryModulusOperator), BindingFlags.Static | BindingFlags.NonPublic)
                                ?? throw new InvalidOperationException("Binary operator method not found.");
        BinaryNegationOperator = typeof(QueryCompilerVisitor).GetMethod(nameof(ApplyBinaryNegationOperator), BindingFlags.Static | BindingFlags.NonPublic)
                                 ?? throw new InvalidOperationException("Binary operator method not found.");
    }

    private readonly IReadOnlyCollection<ParameterExpression>? inputParameters;

    public QueryCompilerVisitor(IReadOnlyCollection<ParameterExpression>? inputParameters = null)
    {
        this.inputParameters = inputParameters;
    }

    public Expression VisitSQLExpression(SQLExpression node)
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

            if (node.Method != null)
            {
                return node.Method.Invoke(null, [leftValue, rightValue]);
            }

            return node.NodeType switch
            {
                ExpressionType.Equal => Equals(leftValue, rightValue),
                ExpressionType.NotEqual => !Equals(leftValue, rightValue),
                ExpressionType.And => (bool)leftValue! && (bool)rightValue!,
                ExpressionType.Or => (bool)leftValue! || (bool)rightValue!,
                ExpressionType.AndAlso => (bool)leftValue! && (bool)rightValue!,
                ExpressionType.OrElse => (bool)leftValue! || (bool)rightValue!,
                ExpressionType.GreaterThan => CompareValues(leftValue, rightValue) > 0,
                ExpressionType.GreaterThanOrEqual => CompareValues(leftValue, rightValue) >= 0,
                ExpressionType.LessThan => CompareValues(leftValue, rightValue) < 0,
                ExpressionType.LessThanOrEqual => CompareValues(leftValue, rightValue) <= 0,
                ExpressionType.Add when leftValue is string sl && rightValue is string sr => sl + sr,
                ExpressionType.Add => InvokeOperator(BinaryAdditionOperator, leftValue!, rightValue!),
                ExpressionType.Subtract => InvokeOperator(BinarySubtractionOperator, leftValue!, rightValue!),
                ExpressionType.Multiply => InvokeOperator(BinaryMultiplyOperator, leftValue!, rightValue!),
                ExpressionType.Divide => InvokeOperator(BinaryDivisionOperator, leftValue!, rightValue!),
                ExpressionType.Modulo => InvokeOperator(BinaryModulusOperator, leftValue!, rightValue!),
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
            object? testValue = test.Call(ctx);
            return testValue is not null and not false ? ifTrue.Call(ctx) : ifFalse.Call(ctx);
        });
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        return new CompiledExpression(node.Type, _ => node.Value);
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitBlock(BlockExpression node)
    {
        throw new NotSupportedException($"The block expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitDefault(DefaultExpression node)
    {
        throw new NotSupportedException($"The default expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitDynamic(DynamicExpression node)
    {
        throw new NotSupportedException($"The dynamic expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitExtension(Expression node)
    {
        throw new NotSupportedException($"The extension method '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitGoto(GotoExpression node)
    {
        throw new NotSupportedException($"The goto expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitIndex(IndexExpression node)
    {
        // Array[Index] = value
        throw new NotSupportedException($"The index expression '{node}' is not supported.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "List type should be preserved")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Field should be preserved")]
    protected override Expression VisitMember(MemberExpression node)
    {
        if (CommonHelpers.IsConstant(node))
        {
            object? value = CommonHelpers.GetConstantValue(node);
            return new CompiledExpression(node.Type, _ => value);
        }
        else if (node.Expression != null)
        {
            CompiledExpression innerExpression = (CompiledExpression)Visit(node.Expression);

            return new CompiledExpression(node.Type, context =>
            {
                object? instance = innerExpression.Call(context);
                return node.Member switch
                {
                    FieldInfo field => field.GetValue(instance),
                    PropertyInfo property => property.GetValue(instance),
                    _ => throw new NotSupportedException($"The member '{node.Member}' is not supported.")
                };
            });
        }

        throw new NotSupportedException($"The member expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitInvocation(InvocationExpression node)
    {
        throw new NotSupportedException($"The invocation expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitLabel(LabelExpression node)
    {
        throw new NotSupportedException($"The label expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        throw new NotSupportedException($"The lambda expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    protected override Expression VisitSwitch(SwitchExpression node)
    {
        throw new NotSupportedException($"The switch expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
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

            return node.NodeType switch
            {
                ExpressionType.Negate or ExpressionType.NegateChecked => node.Method != null
                    ? node.Method.Invoke(null, [operandValue])
                    : InvokeUnaryOperator(BinaryNegationOperator, operandValue!),
                ExpressionType.Not => !(bool)operandValue!,
                ExpressionType.Convert => Convert.ChangeType(operandValue, node.Type),
                ExpressionType.ConvertChecked => Convert.ChangeType(operandValue, node.Type),
                _ => throw new NotSupportedException($"The unary operator '{node.NodeType}' is not supported.")
            };
        });
    }

    [ExcludeFromCodeCoverage]
    protected override CatchBlock VisitCatchBlock(CatchBlock node)
    {
        throw new NotSupportedException($"The catch block '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitDebugInfo(DebugInfoExpression node)
    {
        throw new NotSupportedException($"The debug info expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
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
        List<(MemberInfo Member, List<(MethodInfo AddMethod, CompiledExpression[] Args)> Initializers)> listBindings = node.Bindings
            .OfType<MemberListBinding>()
            .Select(lb => (
                lb.Member,
                lb.Initializers
                    .Select(init => (init.AddMethod, init.Arguments.Select(a => (CompiledExpression)Visit(a)).ToArray()))
                    .ToList()
            ))
            .ToList();

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
                else if (binding.Member is PropertyInfo propertyInfo)
                {
                    object? value = expression.Call(ctx);
                    propertyInfo.SetValue(instance, value);
                }
                else
                {
                    throw new NotSupportedException($"The member binding '{binding}' is not supported.");
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
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        CompiledExpression? instance = Visit(node.Object) as CompiledExpression;
        CompiledExpression[] arguments = node.Arguments
            .Select(arg => (CompiledExpression)Visit(arg))
            .ToArray();

        return new CompiledExpression(node.Type, ctx =>
        {
            object?[] args = arguments.Select(arg => arg.Call(ctx)).ToArray();
            return node.Method.Invoke(instance?.Call(ctx), args);
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
            if (!node.Type.IsArray)
            {
                return null;
            }

            object?[] args = expressions.Select(arg => arg.Call(ctx)).ToArray();

            Array arr = Array.CreateInstance(node.Type.GetElementType()!, args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                arr.SetValue(args[i], i);
            }

            return arr;
        });
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
    {
        throw new NotSupportedException($"The runtime variables expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override SwitchCase VisitSwitchCase(SwitchCase node)
    {
        throw new NotSupportedException($"The switch case '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        throw new NotSupportedException($"The type binary expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override ElementInit VisitElementInit(ElementInit node)
    {
        throw new NotSupportedException($"The element init expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override MemberBinding VisitMemberBinding(MemberBinding node)
    {
        throw new NotSupportedException($"The member binding '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
    {
        throw new NotSupportedException($"The member list binding '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    private CompiledExpression VisitMemberListBindingExpression(MemberListBinding node)
    {
        throw new NotSupportedException($"List binding '{node.Member.Name}' is not supported inside a nested member binding.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Generic operator methods are resolved at runtime; types are preserved via TrimmerRootDescriptor")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Generic operator methods are resolved at runtime; types are preserved via TrimmerRootDescriptor")]
    private static object? InvokeOperator(MethodInfo openMethod, object left, object right)
    {
        Type type = left.GetType();
        MethodInfo concrete;
        lock (ConcreteMethodCache)
        {
            if (!ConcreteMethodCache.TryGetValue((type, openMethod), out concrete!))
            {
                concrete = openMethod.MakeGenericMethod(type);
                ConcreteMethodCache[(type, openMethod)] = concrete;
            }
        }

        return concrete.Invoke(null, [left, right]);
    }

    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Generic operator methods are resolved at runtime; types are preserved via TrimmerRootDescriptor")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Generic operator methods are resolved at runtime; types are preserved via TrimmerRootDescriptor")]
    private static object? InvokeUnaryOperator(MethodInfo openMethod, object operand)
    {
        Type type = operand.GetType();
        MethodInfo concrete;
        lock (ConcreteMethodCache)
        {
            if (!ConcreteMethodCache.TryGetValue((type, openMethod), out concrete!))
            {
                concrete = openMethod.MakeGenericMethod(type);
                ConcreteMethodCache[(type, openMethod)] = concrete;
            }
        }

        return concrete.Invoke(null, [operand]);
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
}