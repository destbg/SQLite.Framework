using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Models;

namespace SQLite.Framework.Internals.Visitors;

internal class SQLVisitor : ExpressionVisitor
{
    private readonly SQLiteDatabase database;
    private readonly MethodVisitor methodVisitor;
    private readonly PropertyVisitor propertyVisitor;

    public SQLVisitor(SQLiteDatabase database, ParameterIndexWrapper paramIndex, TableIndexWrapper tableIndex, int level)
    {
        this.database = database;
        ParamIndex = paramIndex;
        TableIndex = tableIndex;
        Level = level;
        methodVisitor = new(this);
        propertyVisitor = new(this);
    }

    public ParameterIndexWrapper ParamIndex { get; }
    public TableIndexWrapper TableIndex { get; }
    public int IdentifierIndex { get; set; }
    public int Level { get; }
    public string? From { get; private set; }
    public Dictionary<ParameterExpression, Dictionary<string, Expression>> MethodArguments { get; set; } = [];
    public Dictionary<string, Expression> TableColumns { get; set; } = [];

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All entities have public properties.")]
    public void AssignTable(Type entityType)
    {
        char aliasChar = char.ToLowerInvariant(entityType.Name[0]);
        string alias = $"{aliasChar}{TableIndex[aliasChar]++}";

        TableMapping tableMapping = database.TableMapping(entityType);
        From = $"\"{tableMapping.TableName}\" AS {alias}";

        TableColumns = tableMapping.Columns
            .ToDictionary(f => f.PropertyInfo.Name, Expression (f) => new SQLExpression(f.PropertyType, IdentifierIndex++, $"{alias}.{f.Name}"));
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
        (bool isLeftConstant, object? leftConstant, SQLExpression? leftSQLExpression, Expression leftExpression) = ResolveExpressionWithConstant(node.Left);
        (bool isRightConstant, object? rightConstant, SQLExpression? rightSQLExpression, Expression rightExpression) = ResolveExpressionWithConstant(node.Right);

        if (leftSQLExpression == null || rightSQLExpression == null)
        {
            return Expression.MakeBinary(node.NodeType, leftExpression, rightExpression);
        }

        SQLExpression left = CommonHelpers.BracketIfNeeded(leftSQLExpression);
        SQLExpression right = CommonHelpers.BracketIfNeeded(rightSQLExpression);

        SQLiteParameter[]? bothParameters = CommonHelpers.CombineParameters(left, right);

        if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
        {
            string op = node.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
            return new SQLExpression(typeof(bool), IdentifierIndex++, $"{left.Sql} {op} {right.Sql}", bothParameters);
        }
        else if (node.NodeType is ExpressionType.Coalesce)
        {
            return new SQLExpression(node.Type, IdentifierIndex++, $"COALESCE({left.Sql}, {right.Sql})", bothParameters);
        }

        string sqlOp = null!;

        bool equalityOp = node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual;
        bool isLeftNull = isLeftConstant && leftConstant == null;
        bool isRightNull = isRightConstant && rightConstant == null;

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

        sqlOp = node.NodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Multiply => "*",
            ExpressionType.Divide => "/",
            _ => throw new NotSupportedException($"Unsupported binary op {node.NodeType}")
        };

        return new SQLExpression(node.Type, IdentifierIndex++, $"{left.Sql} {sqlOp} {right.Sql}", bothParameters);
    }

    [UnconditionalSuppressMessage("AOT", "IL2062", Justification = "All types have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2065", Justification = "The type is an entity.")]
    protected override Expression VisitConstant(ConstantExpression node)
    {
        Type? qt = node.Value?.GetType();
        if (qt is { IsGenericType: true } && qt.GetGenericTypeDefinition() == typeof(SQLiteTable<>))
        {
            Type entityType = qt.GetGenericArguments()[0];
            AssignTable(entityType);
            return new SQLExpression(qt, -1, From!);
        }

        return new SQLExpression(node.Type, IdentifierIndex++, $"@p{ParamIndex.Index++}", CommonHelpers.GetConstantValue(node));
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        (SQLExpression? test, Expression testExpression) = ResolveExpression(node.Test);
        (SQLExpression? ifTrue, Expression ifTrueExpression) = ResolveExpression(node.IfTrue);
        (SQLExpression? ifFalse, Expression ifFalseExpression) = ResolveExpression(node.IfFalse);

        if (test == null || ifTrue == null || ifFalse == null)
        {
            return Expression.Condition(testExpression, ifTrueExpression, ifFalseExpression);
        }

        SQLiteParameter[]? allParameters = CommonHelpers.CombineParameters(test, ifTrue, ifFalse);

        return new SQLExpression(node.Type, IdentifierIndex++, $"(CASE WHEN {test.Sql} THEN {ifTrue.Sql} ELSE {ifFalse.Sql} END)", allParameters);
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All entities have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "The type is an entity.")]
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ConstantExpression)
        {
            object? value = CommonHelpers.GetConstantValue(node);
            if (value is BaseSQLiteTable table)
            {
                AssignTable(table.ElementType);
                return new SQLExpression(node.Type, -1, From!);
            }

            return new SQLExpression(node.Type, IdentifierIndex++, $"@p{ParamIndex.Index++}", value);
        }
        else if (node.Expression is MemberExpression or ParameterExpression)
        {
            (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(node);

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
                if (expressions.TryGetValue(path, out Expression? expression) && expression is SQLExpression sqlExpression)
                {
                    if (node.Expression.Type == typeof(DateTime))
                    {
                        return propertyVisitor.HandleDateTimeProperty(node.Member.Name, node.Type, sqlExpression);
                    }
                }
            }
        }

        return ResolveMember(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        (bool isConstants, object? value, SQLExpression? operand, Expression operandExpression) = ResolveExpressionWithConstant(node.Operand);

        if (operand == null)
        {
            return Expression.MakeUnary(node.NodeType, operandExpression, node.Type);
        }

        if (isConstants)
        {
            return node.NodeType == ExpressionType.Convert
                ? new SQLExpression(node.Type, IdentifierIndex++, $"@p{ParamIndex.Index++}", Convert.ChangeType(value, node.Type))
                : operand;
        }

        string sql = node.NodeType switch
        {
            ExpressionType.Negate => $"-{operand.Sql}",
            ExpressionType.Not => $"NOT {operand.Sql}",
            ExpressionType.Convert => $"CAST({operand.Sql} AS {CommonHelpers.TypeToSQLiteType(node.Type).ToString().ToUpper()})",
            _ => throw new NotSupportedException($"Unsupported unary op {node.NodeType}")
        };

        return new SQLExpression(node.Type, IdentifierIndex++, sql, operand.Parameters);
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types have public properties.")]
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == nameof(object.Equals) && node.Object != null)
        {
            (SQLExpression? obj, Expression objectExpression) = ResolveExpression(node.Object);
            (SQLExpression? argument, Expression argumentExpression) = ResolveExpression(node.Arguments[0]);

            if (obj == null || argument == null)
            {
                return Expression.Call(objectExpression, node.Method, argumentExpression);
            }

            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(obj, argument);
            return new SQLExpression(typeof(bool), IdentifierIndex++, $"{obj.Sql} = {argument.Sql}", parameters);
        }
        else if (node.Method.DeclaringType == typeof(string))
        {
            return methodVisitor.HandleStringMethod(node);
        }
        else if (node.Method.DeclaringType == typeof(Math))
        {
            return methodVisitor.HandleMathMethod(node);
        }
        else if (node.Method.DeclaringType == typeof(DateTime))
        {
            return methodVisitor.HandleDateTimeMethod(node);
        }
        else if (node.Method.DeclaringType == typeof(Guid))
        {
            return methodVisitor.HandleGuidMethod(node);
        }
        else if (node.Method.DeclaringType == typeof(Queryable))
        {
            return methodVisitor.HandleQueryableMethod(node);
        }
        else if (node.Object != null)
        {
            (bool isConstant, object? constant, SQLExpression? _, Expression objectExpression) = ResolveExpressionWithConstant(node.Object);
            List<(bool IsConstant, object? Constant, SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
                .Select(ResolveExpressionWithConstant)
                .ToList();

            if (isConstant && constant is IEnumerable enumerable)
            {
                return methodVisitor.HandleEnumerableMethod(node, enumerable, arguments);
            }

            return Expression.Call(objectExpression, node.Method, arguments.Select(f => f.Expression));
        }
        else if (node.Arguments.Count > 0)
        {
            List<(bool IsConstant, object? Constant, SQLExpression? Sql, Expression Expression)> arguments = node.Arguments
                .Select(ResolveExpressionWithConstant)
                .ToList();

            if (arguments[0].IsConstant && arguments[0].Constant is IEnumerable enumerable)
            {
                return methodVisitor.HandleEnumerableMethod(node, enumerable, arguments);
            }

            return Expression.Call(node.Method, arguments.Select(f => f.Expression));
        }

        return node;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The array was passed to us.")]
    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        List<(SQLExpression? Sql, Expression Expression)> sqlExpressions = node.Expressions
            .Select(ResolveExpression)
            .ToList();

        if (sqlExpressions.Any(f => f.Sql == null))
        {
            return Expression.NewArrayInit(node.Type.GetElementType()!, sqlExpressions.Select(f => f.Expression));
        }

        SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(sqlExpressions.Select(f => f.Sql!).ToArray());

        return new SQLExpression(
            node.Type,
            IdentifierIndex++,
            $"({string.Join(", ", sqlExpressions.Select(f => f.Sql!.Sql))})",
            parameters
        );
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (MethodArguments.TryGetValue(node, out Dictionary<string, Expression>? expressions))
        {
            SQLExpression? expression = expressions
                .OrderBy(f => f.Key.Count(c => c == '.'))
                .Select(f => f.Value)
                .OfType<SQLExpression>()
                .FirstOrDefault();

            if (expression != null)
            {
                return expression;
            }
        }

        return base.VisitParameter(node);
    }

    public Expression ResolveMember(Expression node)
    {
        (string path, ParameterExpression pe) = CommonHelpers.ResolveParameterPath(node);

        if (MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? expressions))
        {
            if (expressions.TryGetValue(path, out Expression? expression))
            {
                return expression;
            }

            SQLExpression? sqlExpression = expressions
                .OrderBy(f => f.Key.Count(c => c == '.'))
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

    public (SQLExpression?, Expression) ResolveExpression(Expression node)
    {
        Expression resolvedExpression = Visit(node);
        return (resolvedExpression as SQLExpression, resolvedExpression);
    }

    public (bool, object?, SQLExpression?, Expression) ResolveExpressionWithConstant(Expression node)
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

        return (isConstant, constantValue, sqlExpression, resolvedExpression);
    }
}