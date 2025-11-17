using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Models;

namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Handles the conversion of LINQ expressions to SQL expressions.
/// </summary>
/// <remarks>
/// This class is responsible for traversing the expression tree and converting it into a SQL representation.
/// The <see cref="QueryableMethodVisitor" /> gets all the different LINQ methods and passes them to this
/// class for conversion to SQL.
/// Not all Expressions are converted to SQL, some are left as is so that the select method can execute
/// code both as SQL and C#.
/// </remarks>
internal class SQLVisitor : ExpressionVisitor
{
    private readonly SQLiteDatabase database;
    private readonly PropertyVisitor propertyVisitor;

    public SQLVisitor(SQLiteDatabase database, ParameterIndexWrapper paramIndex, TableIndexWrapper tableIndex,
        int level)
    {
        this.database = database;
        ParamIndex = paramIndex;
        TableIndex = tableIndex;
        Level = level;
        MethodVisitor = new MethodVisitor(this);
        propertyVisitor = new PropertyVisitor(this);
    }

    public MethodVisitor MethodVisitor { get; }

    public ParameterIndexWrapper ParamIndex { get; }
    public TableIndexWrapper TableIndex { get; }
    public int IdentifierIndex { get; set; }
    public int Level { get; }
    public SQLExpression? From { get; private set; }
    public Dictionary<ParameterExpression, Dictionary<string, Expression>> MethodArguments { get; set; } = [];
    public Dictionary<string, Expression> TableColumns { get; set; } = [];

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All entities have public properties.")]
    public void AssignTable(Type entityType, SQLExpression? sql = null)
    {
        char aliasChar = char.ToLowerInvariant(entityType.Name[0]);
        string alias = $"{aliasChar}{TableIndex[aliasChar]++}";

        TableMapping tableMapping = database.TableMapping(entityType);
        From = new SQLExpression(
            entityType,
            -1,
            $"{(sql != null ? $"({sql.Sql})" : $"\"{tableMapping.TableName}\"")} AS {alias}",
            sql?.Parameters
        );

        TableColumns = tableMapping.Columns
            .ToDictionary(f => f.PropertyInfo.Name,
                Expression (f) => new SQLExpression(f.PropertyType, IdentifierIndex++, $"{alias}.{f.Name}"));
    }

    public SQLTranslator CloneDeeper(int innerLevel)
    {
        return new SQLTranslator(database, ParamIndex, TableIndex, innerLevel)
        {
            MethodArguments = MethodArguments,
            IsInnerQuery = true
        };
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.ArrayIndex)
        {
            return node;
        }

        ResolvedModel resolvedLeft = ResolveExpression(node.Left);
        ResolvedModel resolvedRight = ResolveExpression(node.Right);

        if (resolvedLeft.SQLExpression == null || resolvedRight.SQLExpression == null)
        {
            return Expression.MakeBinary(node.NodeType, resolvedLeft.Expression, resolvedRight.Expression);
        }

        SQLExpression left = CommonHelpers.BracketIfNeeded(resolvedLeft.SQLExpression);
        SQLExpression right = CommonHelpers.BracketIfNeeded(resolvedRight.SQLExpression);

        SQLiteParameter[]? bothParameters = CommonHelpers.CombineParameters(left, right);

        if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
        {
            string op = node.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
            return new SQLExpression(typeof(bool), IdentifierIndex++, $"{left.Sql} {op} {right.Sql}", bothParameters);
        }

        if (node.NodeType is ExpressionType.Coalesce)
        {
            return new SQLExpression(node.Type, IdentifierIndex++, $"COALESCE({left.Sql}, {right.Sql})",
                bothParameters);
        }

        string sqlOp = null!;

        bool equalityOp = node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual;
        bool isLeftNull = resolvedLeft is { IsConstant: true, Constant: null };
        bool isRightNull = resolvedRight is { IsConstant: true, Constant: null };

        if (equalityOp && (isLeftNull || isRightNull))
        {
            if (node.NodeType == ExpressionType.Equal)
            {
                sqlOp = "IS";
            }
            else if (node.NodeType == ExpressionType.NotEqual)
            {
                sqlOp = "IS NOT";
            }

            if (isLeftNull)
            {
                left = right;
            }

            return new SQLExpression(typeof(bool), IdentifierIndex++, $"{left.Sql} {sqlOp} NULL", left.Parameters);
        }

        (sqlOp, bool parenthesis) = node.NodeType switch
        {
            ExpressionType.Equal => ("=", false),
            ExpressionType.NotEqual => ("<>", false),
            ExpressionType.GreaterThan => (">", false),
            ExpressionType.LessThan => ("<", false),
            ExpressionType.GreaterThanOrEqual => (">=", false),
            ExpressionType.LessThanOrEqual => ("<=", false),
            ExpressionType.Add => ("+", true),
            ExpressionType.Subtract => ("-", true),
            ExpressionType.Multiply => ("*", true),
            ExpressionType.Divide => ("/", true),
            ExpressionType.Modulo => ("%", true),
            _ => throw new NotSupportedException($"Unsupported binary op {node.NodeType}")
        };

        if (parenthesis)
        {
            return new SQLExpression(node.Type, IdentifierIndex++, $"({left.Sql} {sqlOp} {right.Sql})", bothParameters);
        }

        return new SQLExpression(node.Type, IdentifierIndex++, $"{left.Sql} {sqlOp} {right.Sql}", bothParameters);
    }

    [UnconditionalSuppressMessage("AOT", "IL2062", Justification = "All types have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2065", Justification = "The type is an entity.")]
    protected override Expression VisitConstant(ConstantExpression node)
    {
        object? value = CommonHelpers.GetConstantValue(node);

        if (value is BaseSQLiteTable table)
        {
            AssignTable(table.ElementType);
            return new SQLExpression(node.Type, -1, From!.Sql, From!.Parameters);
        }

        return new SQLExpression(node.Type, IdentifierIndex++, $"@p{ParamIndex.Index++}", value);
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        ResolvedModel test = ResolveExpression(node.Test);
        ResolvedModel ifTrue = ResolveExpression(node.IfTrue);
        ResolvedModel ifFalse = ResolveExpression(node.IfFalse);

        if (test.SQLExpression == null || ifTrue.SQLExpression == null || ifFalse.SQLExpression == null)
        {
            return Expression.Condition(test.Expression, ifTrue.Expression, ifFalse.Expression);
        }

        SQLiteParameter[]? allParameters =
            CommonHelpers.CombineParameters(test.SQLExpression, ifTrue.SQLExpression, ifFalse.SQLExpression);

        return new SQLExpression(node.Type, IdentifierIndex++,
            $"(CASE WHEN {test.Sql} THEN {ifTrue.Sql} ELSE {ifFalse.Sql} END)", allParameters);
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All entities have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "The type is an entity.")]
    protected override Expression VisitMember(MemberExpression node)
    {
        if (CommonHelpers.IsConstant(node))
        {
            object? value = CommonHelpers.GetConstantValue(node);
            if (value is BaseSQLiteTable table)
            {
                AssignTable(table.ElementType);
                return new SQLExpression(node.Type, -1, From!.Sql, From!.Parameters);
            }

            return new SQLExpression(node.Type, IdentifierIndex++, $"@p{ParamIndex.Index++}", value);
        }

        if (node.Expression is not MemberExpression and not ParameterExpression and not SQLExpression)
        {
            Expression expr = ResolveMember(node);

            if (expr is MemberExpression member)
            {
                node = member;
            }
            else
            {
                return expr;
            }
        }

        if (node.Expression is MemberExpression or ParameterExpression or SQLExpression)
        {
            (string path, ParameterExpression? pe) = CommonHelpers.ResolveNullableParameterPath(node);

            if (pe == null)
            {
                if (node.Expression is SQLExpression sqlExpression)
                {
                    return ConvertMemberExpression(node, sqlExpression);
                }

                return node.Update(Visit(node.Expression));
            }

            if (MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? expressions))
            {
                if (expressions.TryGetValue(path, out Expression? expression))
                {
                    return expression;
                }
            }

            (path, pe) = CommonHelpers.ResolveParameterPath(node.Expression);

            if (MethodArguments.TryGetValue(pe, out expressions))
            {
                if (expressions.TryGetValue(path, out Expression? expression) &&
                    expression is SQLExpression sqlExpression)
                {
                    return ConvertMemberExpression(node, sqlExpression);
                }
            }
        }

        return ResolveMember(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return ResolveMember(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        ResolvedModel resolved = ResolveExpression(node.Operand);

        if (resolved.SQLExpression == null)
        {
            if (node is { NodeType: ExpressionType.Convert, Operand: ParameterExpression })
            {
                return node.Operand;
            }

            return Expression.MakeUnary(node.NodeType, resolved.Expression, node.Type);
        }

        if (resolved.IsConstant)
        {
            return node.NodeType == ExpressionType.Convert
                ? new SQLExpression(node.Type, IdentifierIndex++, $"@p{ParamIndex.Index++}",
                    Convert.ChangeType(resolved.Constant, node.Type))
                : resolved.SQLExpression;
        }

        string sql = node.NodeType switch
        {
            ExpressionType.Negate => $"-{resolved.SQLExpression.Sql}",
            ExpressionType.Not => $"NOT {resolved.SQLExpression.Sql}",
            ExpressionType.Convert => node.Type == typeof(object)
                ? resolved.SQLExpression.Sql
                : $"CAST({resolved.SQLExpression.Sql} AS {CommonHelpers.TypeToSQLiteType(node.Type).ToString().ToUpper()})",
            _ => throw new NotSupportedException($"Unsupported unary op {node.NodeType}")
        };

        return new SQLExpression(node.Type, IdentifierIndex++, sql, resolved.SQLExpression.Parameters);
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types have public properties.")]
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == nameof(object.Equals) && node.Object != null)
        {
            ResolvedModel obj = ResolveExpression(node.Object);
            ResolvedModel argument = ResolveExpression(node.Arguments[0]);

            if (obj.SQLExpression == null || argument.SQLExpression == null)
            {
                return Expression.Call(obj.Expression, node.Method, argument.Expression);
            }

            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj.SQLExpression, argument.SQLExpression);
            return new SQLExpression(typeof(bool), IdentifierIndex++, $"{obj.Sql} = {argument.Sql}", parameters);
        }

        if (node.Method.DeclaringType == typeof(string))
        {
            return MethodVisitor.HandleStringMethod(node);
        }

        if (node.Method.DeclaringType == typeof(Math))
        {
            return MethodVisitor.HandleMathMethod(node);
        }

        if (node.Method.DeclaringType == typeof(DateTime))
        {
            return MethodVisitor.HandleDateTimeMethod(node);
        }

        if (node.Method.DeclaringType == typeof(DateTimeOffset))
        {
            return MethodVisitor.HandleDateTimeOffsetMethod(node);
        }

        if (node.Method.DeclaringType == typeof(TimeSpan))
        {
            return MethodVisitor.HandleTimeSpanMethod(node);
        }

        if (node.Method.DeclaringType == typeof(DateOnly))
        {
            return MethodVisitor.HandleDateOnlyMethod(node);
        }

        if (node.Method.DeclaringType == typeof(TimeOnly))
        {
            return MethodVisitor.HandleTimeOnlyMethod(node);
        }

        if (node.Method.DeclaringType == typeof(Guid))
        {
            return MethodVisitor.HandleGuidMethod(node);
        }

        if (node.Method.DeclaringType == typeof(Queryable))
        {
            return MethodVisitor.HandleQueryableMethod(node);
        }

        if (node.Object != null)
        {
            if (node.Object.Type.IsEnum)
            {
                return MethodVisitor.HandleEnumMethod(node);
            }

            ResolvedModel obj = ResolveExpression(node.Object);
            List<ResolvedModel> arguments = node.Arguments
                .Select(ResolveExpression)
                .ToList();

            if (obj is { IsConstant: true, Constant: IEnumerable enumerable })
            {
                return MethodVisitor.HandleEnumerableMethod(node, enumerable, arguments);
            }

            return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
        }

        if (node.Arguments.Count > 0)
        {
            if (node.Arguments[0].Type.IsGenericType &&
                node.Arguments[0].Type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
            {
                return MethodVisitor.HandleGroupingMethod(node);
            }

            List<ResolvedModel> arguments = node.Arguments
                .Select(ResolveExpression)
                .ToList();

            if (arguments[0].IsConstant && arguments[0].Constant is IEnumerable enumerable)
            {
                return MethodVisitor.HandleEnumerableMethod(node, enumerable, arguments);
            }

            return Expression.Call(node.Method, arguments.Select(f => f.Expression));
        }

        return node;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The array was passed to us.")]
    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        List<ResolvedModel> sqlExpressions = node.Expressions
            .Select(ResolveExpression)
            .ToList();

        if (sqlExpressions.Any(f => f.SQLExpression == null))
        {
            return Expression.NewArrayInit(node.Type.GetElementType()!, sqlExpressions.Select(f => f.Expression));
        }

        SQLiteParameter[]? parameters =
            CommonHelpers.CombineParameters(sqlExpressions.Select(f => f.SQLExpression!).ToArray());

        return new SQLExpression(
            node.Type,
            IdentifierIndex++,
            $"({string.Join(", ", sqlExpressions.Select(f => f.Sql))})",
            parameters
        );
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "All types have public properties.")]
    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        NewExpression newExpression = (NewExpression)Visit(node.NewExpression);
        List<MemberBinding> bindings = node.Bindings.Select(VisitMemberBinding).ToList();

        return Expression.MemberInit(newExpression, bindings);
    }

    protected override MemberBinding VisitMemberBinding(MemberBinding node)
    {
        return node switch
        {
            MemberAssignment assignment => VisitMemberAssignment(assignment),
            MemberMemberBinding memberMemberBinding => VisitMemberMemberBinding(memberMemberBinding),
            MemberListBinding memberListBinding => VisitMemberListBinding(memberListBinding),
            _ => throw new NotSupportedException($"Unsupported binding type: {node.BindingType}")
        };
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        return node.Update(Visit(node.Expression));
    }

    protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
    {
        List<MemberBinding> bindings = node.Bindings.Select(VisitMemberBinding).ToList();
        return node.Update(bindings);
    }

    protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
    {
        List<ElementInit> initializers = node.Initializers.Select(VisitElementInit).ToList();
        return node.Update(initializers);
    }

    protected override ElementInit VisitElementInit(ElementInit node)
    {
        List<Expression> arguments = node.Arguments.Select(Visit).ToList()!;
        return node.Update(arguments);
    }

    protected override Expression VisitListInit(ListInitExpression node)
    {
        NewExpression newExpression = (NewExpression)Visit(node.NewExpression);
        List<ElementInit> initializers = node.Initializers.Select(VisitElementInit).ToList();

        return node.Update(newExpression, initializers);
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "All types have public properties.")]
    protected override Expression VisitNew(NewExpression node)
    {
        List<Expression> arguments = node.Arguments.Select(Visit).ToList()!;
        return node.Update(arguments);
    }

    public Expression ResolveMember(Expression node)
    {
        (string path, ParameterExpression? pe) = CommonHelpers.ResolveNullableParameterPath(node);

        if (pe == null)
        {
            if (node is MemberExpression { Expression: not null } member)
            {
                return member.Update(Visit(member.Expression));
            }

            return node;
        }

        if (MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? expressions))
        {
            if (expressions.TryGetValue(path, out Expression? expression))
            {
                return expression;
            }

            SQLExpression? sqlExpression = expressions
                .OrderBy(f => f.Key.Count(c => c == '.'))
                .ThenBy(f => f.Key.Length)
                .Select(f => f.Value)
                .OfType<SQLExpression>()
                .FirstOrDefault();

            if (sqlExpression != null)
            {
                return sqlExpression;
            }
        }

        throw new NotSupportedException($"Cannot translate expression {node}");
    }

    public ResolvedModel ResolveExpression(Expression node)
    {
        bool isConstant = CommonHelpers.IsConstant(node);
        object? constantValue;
        SQLExpression? sqlExpression;
        Expression resolvedExpression;

        if (isConstant)
        {
            constantValue = CommonHelpers.GetConstantValue(node);
            sqlExpression = new SQLExpression(node.Type, IdentifierIndex++, $"@p{ParamIndex.Index++}", constantValue);
            resolvedExpression = node;
        }
        else
        {
            constantValue = null;
            resolvedExpression = Visit(node);
            if (resolvedExpression is SQLExpression sqlResolvedExpression)
            {
                sqlExpression = sqlResolvedExpression;
            }
            else
            {
                sqlExpression = null;
            }
        }

        return new ResolvedModel
        {
            IsConstant = isConstant,
            Constant = constantValue,
            SQLExpression = sqlExpression,
            Expression = resolvedExpression
        };
    }

    private Expression ConvertMemberExpression(MemberExpression node, SQLExpression sqlExpression)
    {
        if (Nullable.GetUnderlyingType(node.Expression!.Type) != null)
        {
            return propertyVisitor.HandleNullableProperty(node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(string))
        {
            return propertyVisitor.HandleStringProperty(node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(DateTime))
        {
            return propertyVisitor.HandleDateTimeProperty(node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(DateTimeOffset))
        {
            return propertyVisitor.HandleDateTimeOffsetProperty(node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(TimeSpan))
        {
            return propertyVisitor.HandleTimeSpanProperty(node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(DateOnly))
        {
            return propertyVisitor.HandleDateOnlyProperty(node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(TimeOnly))
        {
            return propertyVisitor.HandleTimeOnlyProperty(node.Member.Name, node.Type, sqlExpression);
        }

        return sqlExpression;
    }
}