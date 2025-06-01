using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Enums;
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
    public Expression VisitSQLExpression(SQLExpression node)
    {
        return new CompiledExpression(node.Type, ctx =>
        {
            (int index, SQLiteColumnType columnType) = ctx.Columns[node.IdentifierText];
            return ctx.Reader.GetValue(index, columnType, node.Type);
        });
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        CompiledExpression left = (CompiledExpression)Visit(node.Left);
        CompiledExpression right = (CompiledExpression)Visit(node.Right);

        return new CompiledExpression(node.Type, ctx =>
        {
            dynamic? leftValue = left.Call(ctx);
            dynamic? rightValue = right.Call(ctx);

            if (node.NodeType == ExpressionType.ArrayIndex)
            {
                if (leftValue is Array array && rightValue is int index)
                {
                    return array.GetValue(index);
                }

                throw new InvalidOperationException("Array index operation requires an array on the left and an integer index on the right.");
            }

            return node.NodeType switch
            {
                ExpressionType.Equal => Equals(leftValue as object, rightValue as object),
                ExpressionType.NotEqual => !Equals(leftValue as object, rightValue as object),
                ExpressionType.GreaterThan => leftValue > rightValue,
                ExpressionType.GreaterThanOrEqual => leftValue >= rightValue,
                ExpressionType.LessThan => leftValue < rightValue,
                ExpressionType.LessThanOrEqual => leftValue <= rightValue,
                ExpressionType.And => leftValue && rightValue,
                ExpressionType.Or => leftValue || rightValue,
                ExpressionType.AndAlso => leftValue && rightValue,
                ExpressionType.OrElse => leftValue || rightValue,
                ExpressionType.Add => leftValue + rightValue,
                ExpressionType.Subtract => leftValue - rightValue,
                ExpressionType.Multiply => leftValue * rightValue,
                ExpressionType.Divide => leftValue / rightValue,
                ExpressionType.Modulo => leftValue % rightValue,
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
            dynamic? testValue = test.Call(ctx);
            return testValue != null && testValue != false ? ifTrue.Call(ctx) : ifFalse.Call(ctx);
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

    protected override Expression VisitMember(MemberExpression node)
    {
        if (CommonHelpers.IsConstant(node))
        {
            object? value = CommonHelpers.GetConstantValue(node);
            return new CompiledExpression(node.Type, _ => value);
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
            return new CompiledExpression(node.Type, ctx =>
            {
                object?[] arguments = node.Arguments
                    .Select(arg => (CompiledExpression)Visit(arg))
                    .Select(f => f.Call(ctx))
                    .ToArray();

                return node.Constructor.Invoke(arguments);
            });
        }

        throw new NotSupportedException($"The new expression '{node}' is not supported.");
    }

    [ExcludeFromCodeCoverage]
    protected override Expression VisitParameter(ParameterExpression node)
    {
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
            dynamic? operandValue = operand.Call(ctx);
            return node.NodeType switch
            {
                ExpressionType.Negate => -operandValue,
                ExpressionType.NegateChecked => -operandValue,
                ExpressionType.Not => !operandValue,
                ExpressionType.Convert => Convert.ChangeType(operandValue as object, node.Type),
                ExpressionType.ConvertChecked => Convert.ChangeType(operandValue as object, node.Type),
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
    [return: NotNullIfNotNull("node")]
    protected override LabelTarget? VisitLabelTarget(LabelTarget? node)
    {
        if (node == null)
        {
            return null;
        }

        throw new NotSupportedException($"The label target '{node}' is not supported.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "List type should be preserved")]
    protected override Expression VisitListInit(ListInitExpression node)
    {
        CompiledExpression newExpression = (CompiledExpression)Visit(node.NewExpression);
        List<(MethodInfo AddMethod, CompiledExpression[] Arguments)> initializers = node.Initializers
            .Select(init => (init.AddMethod, init.Arguments.Select(arg => (CompiledExpression)Visit(arg)).ToArray()))
            .ToList();

        return new CompiledExpression(node.Type, ctx =>
        {
            dynamic? instance = newExpression.Call(ctx);
            foreach ((MethodInfo AddMethod, CompiledExpression[] Arguments) initializer in initializers)
            {
                initializer.AddMethod.Invoke(instance, initializer.Arguments.Select(arg => arg.Call(ctx)).ToArray());
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
    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        CompiledExpression newExpression = (CompiledExpression)Visit(node.NewExpression);
        List<(MemberBinding, CompiledExpression)> bindings = node.Bindings
            .Select(f => (f, VisitMemberBindingExpression(f)))
            .ToList();

        return new CompiledExpression(node.Type, ctx =>
        {
            dynamic? instance = newExpression.Call(ctx);
            foreach ((MemberBinding binding, CompiledExpression expression) in bindings)
            {
                if (binding.Member is FieldInfo fieldInfo)
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

            return instance;
        });
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Methods should be preserved")]
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

    private CompiledExpression VisitElementInitExpression(ElementInit node)
    {
        List<CompiledExpression> arguments = node.Arguments
            .Select(arg => (CompiledExpression)Visit(arg))
            .ToList();

        return new CompiledExpression(node.AddMethod.ReturnType, ctx =>
        {
            object? instance = node.AddMethod.Invoke(null, arguments.Select(arg => arg.Call(ctx)).ToArray());
            return instance;
        });
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
        List<CompiledExpression> initializers = node.Initializers.Select(VisitElementInitExpression).ToList();

        Type type = node.Member is PropertyInfo property ? property.PropertyType : ((FieldInfo)node.Member).FieldType;

        return new CompiledExpression(type, ctx =>
        {
            foreach (CompiledExpression initializer in initializers)
            {
                initializer.Call(ctx);
            }

            return null;
        });
    }
}