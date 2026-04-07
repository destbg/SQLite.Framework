using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Enums;
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
    private readonly PropertyVisitor propertyVisitor;

    public SQLVisitor(SQLiteDatabase database, IndexWrapper paramIndex, IndexWrapper identifierIndex, TableIndexWrapper tableIndex, int level)
    {
        Database = database;
        ParamIndex = paramIndex;
        IdentifierIndex = identifierIndex;
        TableIndex = tableIndex;
        Level = level;
        MethodVisitor = new MethodVisitor(this);
        propertyVisitor = new PropertyVisitor(this);
    }

    public SQLiteDatabase Database { get; }
    public MethodVisitor MethodVisitor { get; }

    public IndexWrapper ParamIndex { get; }
    public IndexWrapper IdentifierIndex { get; }
    public TableIndexWrapper TableIndex { get; }
    public int Level { get; }
    public bool IsInSelectProjection { get; set; }
    public SQLExpression? From { get; private set; }
    public Dictionary<ParameterExpression, Dictionary<string, Expression>> MethodArguments { get; set; } = [];
    public Dictionary<string, Expression> TableColumns { get; set; } = [];
    public CteRegistry? CteRegistry { get; set; }
    public Dictionary<ParameterExpression, (string Alias, Dictionary<string, Expression> Columns)> CteParameters { get; set; } = [];

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All entities have public properties.")]
    public void AssignValues(SQLExpression fromExpression, Dictionary<string, Expression> columns)
    {
        From = fromExpression;
        TableColumns = columns;
    }

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "All entities have public properties.")]
    public void AssignTable(Type entityType, SQLExpression? sql = null)
    {
        char aliasChar = char.ToLowerInvariant(entityType.Name.FirstOrDefault(char.IsLetter, 't'));
        string alias = $"{aliasChar}{TableIndex[aliasChar]++}";

        TableMapping tableMapping = Database.TableMapping(entityType);
        From = new SQLExpression(
            entityType,
            -1,
            $"{(sql != null ? $"({sql.Sql})" : $"\"{tableMapping.TableName}\"")} AS {alias}",
            sql?.Parameters
        );

        TableColumns = tableMapping.Columns
            .ToDictionary(f => f.PropertyInfo.Name, Expression (f) =>
            {
                string colSql = $"{alias}.{f.Name}";
                if (Database.StorageOptions.TypeConverters.TryGetValue(f.PropertyType, out ISQLiteTypeConverter? conv)
                    && conv.ColumnSqlExpression is { } colExpr)
                {
                    colSql = string.Format(colExpr, colSql);
                }
                return new SQLExpression(f.PropertyType, IdentifierIndex.Index++, colSql);
            });
    }

    public SQLTranslator CloneDeeper(int innerLevel)
    {
        return new SQLTranslator(Database, ParamIndex, IdentifierIndex, TableIndex, innerLevel, true)
        {
            MethodArguments = MethodArguments,
            CteRegistry = CteRegistry,
            CteParameters = CteParameters
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "ToString does exist")]
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.ArrayIndex)
        {
            return node;
        }

        Expression leftNode = node.Left;
        Expression rightNode = node.Right;

        if (leftNode is UnaryExpression { NodeType: ExpressionType.Convert } leftEnumConvert && leftEnumConvert.Operand.Type.IsEnum)
        {
            Type enumType = leftEnumConvert.Operand.Type;
            leftNode = leftEnumConvert.Operand;
            if (CommonHelpers.IsConstant(rightNode) && rightNode.Type == Enum.GetUnderlyingType(enumType))
            {
                object? intValue = CommonHelpers.GetConstantValue(rightNode);
                rightNode = Expression.Constant(Enum.ToObject(enumType, intValue!), enumType);
            }
        }

        if (rightNode is UnaryExpression { NodeType: ExpressionType.Convert } rightEnumConvert && rightEnumConvert.Operand.Type.IsEnum)
        {
            Type enumType = rightEnumConvert.Operand.Type;
            rightNode = rightEnumConvert.Operand;
            if (CommonHelpers.IsConstant(leftNode) && leftNode.Type == Enum.GetUnderlyingType(enumType))
            {
                object? intValue = CommonHelpers.GetConstantValue(leftNode);
                leftNode = Expression.Constant(Enum.ToObject(enumType, intValue!), enumType);
            }
        }

        if (rightNode.Type == typeof(int) && leftNode is UnaryExpression leftUnary && leftUnary.Operand.Type == typeof(char))
        {
            leftNode = leftUnary.Operand;

            if (CommonHelpers.IsConstant(rightNode))
            {
                int value = (int)CommonHelpers.GetConstantValue(rightNode)!;
                rightNode = Expression.Constant(((char)value).ToString());
            }
            else
            {
                rightNode = Expression.MakeUnary(ExpressionType.Convert, rightNode, typeof(char));
            }
        }
        else if (leftNode.Type == typeof(int) && rightNode is UnaryExpression rightUnary && rightUnary.Operand.Type == typeof(char))
        {
            rightNode = rightUnary.Operand;

            if (CommonHelpers.IsConstant(leftNode))
            {
                int value = (int)CommonHelpers.GetConstantValue(leftNode)!;
                rightNode = Expression.Constant(((char)value).ToString());
            }
            else
            {
                rightNode = Expression.MakeUnary(ExpressionType.Convert, rightNode, typeof(char));
            }
        }

        ResolvedModel resolvedLeft = ResolveExpression(leftNode);
        ResolvedModel resolvedRight = ResolveExpression(rightNode);

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
            return new SQLExpression(typeof(bool), IdentifierIndex.Index++, $"{left.Sql} {op} {right.Sql}", bothParameters);
        }

        if (node.NodeType is ExpressionType.Coalesce)
        {
            return new SQLExpression(node.Type, IdentifierIndex.Index++, $"COALESCE({left.Sql}, {right.Sql})", bothParameters);
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

            return new SQLExpression(typeof(bool), IdentifierIndex.Index++, $"{left.Sql} {sqlOp} NULL", left.Parameters);
        }

        (sqlOp, bool parenthesis) = node.NodeType switch
        {
            ExpressionType.Equal => ("=", false),
            ExpressionType.NotEqual => ("<>", false),
            ExpressionType.GreaterThan => (">", false),
            ExpressionType.LessThan => ("<", false),
            ExpressionType.GreaterThanOrEqual => (">=", false),
            ExpressionType.LessThanOrEqual => ("<=", false),
            ExpressionType.Add => (node.Type == typeof(string) ? "||" : "+", node.Type != typeof(string)),
            ExpressionType.Subtract => ("-", true),
            ExpressionType.Multiply => ("*", true),
            ExpressionType.Divide => ("/", true),
            ExpressionType.Modulo => ("%", true),
            _ => throw new NotSupportedException($"Unsupported binary op {node.NodeType}")
        };

        if (parenthesis)
        {
            return new SQLExpression(node.Type, IdentifierIndex.Index++, $"({left.Sql} {sqlOp} {right.Sql})", bothParameters);
        }

        return new SQLExpression(node.Type, IdentifierIndex.Index++, $"{left.Sql} {sqlOp} {right.Sql}", bothParameters);
    }

    [UnconditionalSuppressMessage("AOT", "IL2062", Justification = "All types have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2065", Justification = "The type is an entity.")]
    protected override Expression VisitConstant(ConstantExpression node)
    {
        object? value = CommonHelpers.GetConstantValue(node);

        if (value is SQLiteCte cte)
        {
            AssignCte(cte);
            return new SQLExpression(node.Type, -1, From!.Sql, From!.Parameters);
        }

        if (value is BaseSQLiteTable table)
        {
            AssignTable(table.ElementType);
            return new SQLExpression(node.Type, -1, From!.Sql, From!.Parameters);
        }

        return new SQLExpression(node.Type, IdentifierIndex.Index++, $"@p{ParamIndex.Index++}", value);
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "All types have public properties.")]
    private void AssignCte(SQLiteCte cte)
    {
        CteRegistry ??= new CteRegistry();

        Type elementType = cte.ElementType;
        char aliasChar = char.ToLowerInvariant(elementType.Name[0]);
        string alias = $"{aliasChar}{TableIndex[aliasChar]++}";

        string? cachedName = CteRegistry.TryGetName(cte);
        if (cachedName != null)
        {
            From = new SQLExpression(elementType, -1, $"{cachedName} AS {alias}");
            TableColumns = elementType.GetProperties()
                .ToDictionary(f => f.Name, Expression (f) => new SQLExpression(f.PropertyType, IdentifierIndex.Index++, $"{alias}.{f.Name}"));
            return;
        }

        LambdaExpression lambda = cte.Query;
        bool isRecursive = lambda.Parameters.Count == 1;

        string cteName;

        if (isRecursive)
        {
            ParameterExpression selfParam = lambda.Parameters[0];

            string placeholder = $"{aliasChar}__cte_self_{CteRegistry.Ctes.Count}__";

            Dictionary<string, Expression> selfColumns = elementType.GetProperties()
                .ToDictionary(f => f.Name, Expression (f) => new SQLExpression(f.PropertyType, IdentifierIndex.Index++, $"{placeholder}.{f.Name}"));

            CteParameters[selfParam] = (placeholder, selfColumns);
            MethodArguments[selfParam] = selfColumns;

            SQLTranslator bodyTranslator = CloneDeeper(Level + 1);
            SQLQuery bodyQuery = bodyTranslator.Translate(lambda.Body);

            string finalName = $"cte{CteRegistry.Ctes.Count}";
            string fixedSql = bodyQuery.Sql.Replace(placeholder, finalName);

            cteName = CteRegistry.Register(fixedSql, bodyQuery.Parameters.Count == 0 ? null : [.. bodyQuery.Parameters], isRecursive: true, key: cte);

            CteParameters.Remove(selfParam);
            MethodArguments.Remove(selfParam);
        }
        else
        {
            SQLTranslator bodyTranslator = CloneDeeper(Level + 1);
            SQLQuery bodyQuery = bodyTranslator.Translate(lambda.Body);

            cteName = CteRegistry.Register(bodyQuery.Sql, bodyQuery.Parameters.Count == 0 ? null : [.. bodyQuery.Parameters], isRecursive: false, key: cte);
        }

        From = new SQLExpression(elementType, -1, $"{cteName} AS {alias}");

        TableColumns = elementType.GetProperties()
            .ToDictionary(f => f.Name, Expression (f) => new SQLExpression(f.PropertyType, IdentifierIndex.Index++, $"{alias}.{f.Name}"));
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

        return new SQLExpression(node.Type, IdentifierIndex.Index++,
            $"(CASE WHEN {test.Sql} THEN {ifTrue.Sql} ELSE {ifFalse.Sql} END)", allParameters);
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All entities have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "The type is an entity.")]
    protected override Expression VisitMember(MemberExpression node)
    {
        if (CommonHelpers.IsConstant(node))
        {
            object? value = CommonHelpers.GetConstantValue(node);
            if (value is SQLiteCte cte)
            {
                AssignCte(cte);
                return new SQLExpression(node.Type, -1, From!.Sql, From!.Parameters);
            }
            else if (value is BaseSQLiteTable table)
            {
                AssignTable(table.ElementType);
                return new SQLExpression(node.Type, -1, From!.Sql, From!.Parameters);
            }

            return new SQLExpression(node.Type, IdentifierIndex.Index++, $"@p{ParamIndex.Index++}", value);
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
                    if (expression is SQLExpression colExpr && !IsInSelectProjection)
                    {
                        Type colType = Nullable.GetUnderlyingType(colExpr.Type) ?? colExpr.Type;
                        if (colType == typeof(decimal) && Database.StorageOptions.DecimalStorage == DecimalStorageMode.Text)
                        {
                            return new SQLExpression(colExpr.Type, IdentifierIndex.Index++, $"CAST({colExpr.Sql} AS REAL)", colExpr.Parameters);
                        }
                    }

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
        if (CteParameters.TryGetValue(node, out (string Alias, Dictionary<string, Expression> Columns) cteRef))
        {
            char aliasChar = cteRef.Alias[0];
            string alias = $"{aliasChar}{TableIndex[aliasChar]++}";

            From = new SQLExpression(node.Type, -1, $"{cteRef.Alias} AS {alias}");
            TableColumns = cteRef.Columns
                .ToDictionary(kv => kv.Key, Expression (kv) => new SQLExpression(
                    ((SQLExpression)kv.Value).Type,
                    IdentifierIndex.Index++,
                    $"{alias}.{kv.Key}"));

            return new SQLExpression(node.Type, -1, From.Sql);
        }

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
            return ResolvedUnary(node, resolved);
        }

        if (node.NodeType == ExpressionType.Convert)
        {
            if (resolved.SQLExpression.Type == node.Type || node.Type == typeof(object))
            {
                return resolved.SQLExpression;
            }
            else if (node.Type == typeof(char) && resolved.SQLExpression.Type == typeof(int))
            {
                return new SQLExpression(node.Type, IdentifierIndex.Index++, $"CHAR({resolved.SQLExpression.Sql})", resolved.SQLExpression.Parameters);
            }
            else if (node.Type == typeof(int) && resolved.SQLExpression.Type == typeof(char))
            {
                return new SQLExpression(node.Type, IdentifierIndex.Index++, $"UNICODE({resolved.SQLExpression.Sql})", resolved.SQLExpression.Parameters);
            }
            else if (resolved.SQLExpression.Type.IsEnum && (Nullable.GetUnderlyingType(node.Type) ?? node.Type) == Enum.GetUnderlyingType(resolved.SQLExpression.Type))
            {
                return new SQLExpression(node.Type, IdentifierIndex.Index++, resolved.SQLExpression.Sql, resolved.SQLExpression.Parameters);
            }
            else
            {
                string sqliteType = CommonHelpers.TypeToSQLiteType(node.Type, Database.StorageOptions).ToString().ToUpper();
                return new SQLExpression(node.Type,
                    IdentifierIndex.Index++,
                    $"CAST({resolved.SQLExpression.Sql} AS {sqliteType})",
                    resolved.SQLExpression.Parameters
                );
            }
        }

        string sql = node.NodeType switch
        {
            ExpressionType.Negate => $"-{resolved.SQLExpression.Sql}",
            ExpressionType.Not => $"NOT {resolved.SQLExpression.Sql}",
            _ => throw new NotSupportedException($"Unsupported unary op {node.NodeType}")
        };

        return new SQLExpression(node.Type, IdentifierIndex.Index++, sql, resolved.SQLExpression.Parameters);
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
            return new SQLExpression(typeof(bool), IdentifierIndex.Index++, $"{obj.Sql} = {argument.Sql}", parameters);
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

        if (node.Method.DeclaringType == typeof(Enum))
        {
            return MethodVisitor.HandleEnumMethod(node);
        }

        if (node.Method.DeclaringType == typeof(char))
        {
            return MethodVisitor.HandleCharMethod(node);
        }

        if (node.Method.DeclaringType == typeof(int)
            || node.Method.DeclaringType == typeof(long)
            || node.Method.DeclaringType == typeof(short)
            || node.Method.DeclaringType == typeof(byte)
            || node.Method.DeclaringType == typeof(sbyte)
            || node.Method.DeclaringType == typeof(uint)
            || node.Method.DeclaringType == typeof(ulong)
            || node.Method.DeclaringType == typeof(ushort))
        {
            return MethodVisitor.HandleIntegerMethod(node);
        }

        if (node.Method.DeclaringType == typeof(double)
            || node.Method.DeclaringType == typeof(float)
            || node.Method.DeclaringType == typeof(decimal))
        {
            return MethodVisitor.HandleFloatingPointMethod(node);
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

            if (TryGetMethodTranslator(node.Method, out SQLiteMethodTranslator? translator))
            {
                return MethodVisitor.HandleCustomMethod(node, obj, arguments, translator);
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

            if (TryGetMethodTranslator(node.Method, out SQLiteMethodTranslator? translator))
            {
                return MethodVisitor.HandleCustomMethod(node, null, arguments, translator);
            }

            return Expression.Call(node.Method, arguments.Select(f => f.Expression));
        }

        if (TryGetMethodTranslator(node.Method, out SQLiteMethodTranslator? noArgTranslator))
        {
            return MethodVisitor.HandleCustomMethod(node, null, [], noArgTranslator);
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
            IdentifierIndex.Index++,
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

    [ExcludeFromCodeCoverage(Justification = "I really hope no one is doing new Dto { Collection = { item1, item2 } } inside a Where clause")]
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
            if (node is UnaryExpression { NodeType: ExpressionType.Convert } convertNode)
            {
                object? innerValue = CommonHelpers.GetConstantValue(convertNode.Operand);
                Type targetType = Nullable.GetUnderlyingType(node.Type) ?? node.Type;
                if (innerValue?.GetType().IsEnum == true && targetType == Enum.GetUnderlyingType(innerValue.GetType()))
                {
                    constantValue = innerValue;
                }
            }

            sqlExpression = new SQLExpression(node.Type, IdentifierIndex.Index++, $"@p{ParamIndex.Index++}", constantValue);
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

    [ExcludeFromCodeCoverage(Justification = "In theory we should never enter here")]
    private SQLExpression ResolvedUnary(UnaryExpression node, ResolvedModel resolved)
    {
        if (node.NodeType == ExpressionType.Convert)
        {
            object? value = resolved.Constant;
            Type targetType = Nullable.GetUnderlyingType(node.Type) ?? node.Type;
            if (value?.GetType().IsEnum == true && targetType == Enum.GetUnderlyingType(value.GetType()))
            {
                return new SQLExpression(node.Type, IdentifierIndex.Index++, $"@p{ParamIndex.Index++}", value);
            }

            return new SQLExpression(node.Type, IdentifierIndex.Index++, $"@p{ParamIndex.Index++}", Convert.ChangeType(value, targetType));
        }

        return resolved.SQLExpression!;
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
            if (IsInSelectProjection && Level == 0 && Database.StorageOptions.DateTimeStorage == DateTimeStorageMode.TextFormatted)
            {
                return node.Update(sqlExpression);
            }

            return propertyVisitor.HandleDateTimeProperty(node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(DateTimeOffset))
        {
            if (IsInSelectProjection && Level == 0 && Database.StorageOptions.DateTimeOffsetStorage == DateTimeOffsetStorageMode.TextFormatted)
            {
                return node.Update(sqlExpression);
            }

            return propertyVisitor.HandleDateTimeOffsetProperty(node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(TimeSpan))
        {
            if (IsInSelectProjection && Level == 0 && Database.StorageOptions.TimeSpanStorage == TimeSpanStorageMode.Text)
            {
                return node.Update(sqlExpression);
            }

            return propertyVisitor.HandleTimeSpanProperty(node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(DateOnly))
        {
            if (IsInSelectProjection && Level == 0 && Database.StorageOptions.DateOnlyStorage == DateOnlyStorageMode.Text)
            {
                return node.Update(sqlExpression);
            }

            return propertyVisitor.HandleDateOnlyProperty(node.Member.Name, node.Type, sqlExpression);
        }

        if (node.Expression.Type == typeof(TimeOnly))
        {
            if (IsInSelectProjection && Level == 0 && Database.StorageOptions.TimeOnlyStorage == TimeOnlyStorageMode.Text)
            {
                return node.Update(sqlExpression);
            }

            return propertyVisitor.HandleTimeOnlyProperty(node.Member.Name, node.Type, sqlExpression);
        }

        return sqlExpression;
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Open generic type methods are looked up by name for custom translator registration.")]
    private bool TryGetMethodTranslator(MethodInfo method, [NotNullWhen(true)] out SQLiteMethodTranslator? translator)
    {
        if (Database.StorageOptions.MethodTranslators.TryGetValue(method, out translator))
        {
            return true;
        }

        if (method.IsGenericMethod &&
            Database.StorageOptions.MethodTranslators.TryGetValue(method.GetGenericMethodDefinition(), out translator))
        {
            return true;
        }

        if (method.DeclaringType?.IsConstructedGenericType == true)
        {
            Type openType = method.DeclaringType.GetGenericTypeDefinition();
            MethodInfo? openMethod = openType.GetMethods()
                .FirstOrDefault(m => m.Name == method.Name && m.GetParameters().Length == method.GetParameters().Length);
            if (openMethod != null && Database.StorageOptions.MethodTranslators.TryGetValue(openMethod, out translator))
            {
                return true;
            }
        }

        return false;
    }
}