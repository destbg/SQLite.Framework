namespace SQLite.Framework.Internals.Visitors.SQL;

internal partial class SQLVisitor
{
    [UnconditionalSuppressMessage("AOT", "IL2062", Justification = "All types have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2065", Justification = "The type is an entity.")]
    protected override Expression VisitConstant(ConstantExpression node)
    {
        object? value = ExpressionHelpers.GetConstantValue(node);

        if (value is SQLiteCte cte)
        {
            AssignCte(cte);
            return new SQLiteExpression(node.Type, -1, From!.Sql, From!.Parameters);
        }

        if (value is BaseSQLiteTable table)
        {
            AssignTable(table.ElementType);
            return new SQLiteExpression(node.Type, -1, From!.Sql, From!.Parameters);
        }

        return new SQLiteExpression(node.Type, Counters.IdentifierIndex++, $"@p{Counters.ParamIndex++}", value);
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "All types have public properties.")]
    private void AssignCte(SQLiteCte cte)
    {
        CteRegistry ??= new CteRegistry();

        Type elementType = cte.ElementType;
        char aliasChar = char.ToLowerInvariant(elementType.Name[0]);
        string alias = $"{aliasChar}{Counters.NextTableIndex(aliasChar)}";

        string? cachedName = CteRegistry.TryGetName(cte);
        if (cachedName != null)
        {
            From = new SQLiteExpression(elementType, -1, $"{cachedName} AS {alias}");
            TableColumns = elementType.GetProperties()
                .ToDictionary(f => f.Name, Expression (f) => new SQLiteExpression(f.PropertyType, Counters.IdentifierIndex++, $"{alias}.{f.Name}"));
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
                .ToDictionary(f => f.Name, Expression (f) => new SQLiteExpression(f.PropertyType, Counters.IdentifierIndex++, $"{placeholder}.{f.Name}"));

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

        From = new SQLiteExpression(elementType, -1, $"{cteName} AS {alias}");

        TableColumns = elementType.GetProperties()
            .ToDictionary(f => f.Name, Expression (f) => new SQLiteExpression(f.PropertyType, Counters.IdentifierIndex++, $"{alias}.{f.Name}"));
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        ResolvedModel test = ResolveExpression(node.Test);
        ResolvedModel ifTrue = ResolveExpression(node.IfTrue);
        ResolvedModel ifFalse = ResolveExpression(node.IfFalse);

        if (test.SQLiteExpression == null || ifTrue.SQLiteExpression == null || ifFalse.SQLiteExpression == null)
        {
            return Expression.Condition(test.Expression, ifTrue.Expression, ifFalse.Expression);
        }

        SQLiteParameter[]? allParameters =
            ParameterHelpers.CombineParameters(test.SQLiteExpression, ifTrue.SQLiteExpression, ifFalse.SQLiteExpression);

        return new SQLiteExpression(node.Type, Counters.IdentifierIndex++,
            $"(CASE WHEN {test.Sql} THEN {ifTrue.Sql} ELSE {ifFalse.Sql} END)", allParameters);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (CteParameters.TryGetValue(node, out (string Alias, Dictionary<string, Expression> Columns) cteRef))
        {
            char aliasChar = cteRef.Alias[0];
            string alias = $"{aliasChar}{Counters.NextTableIndex(aliasChar)}";

            From = new SQLiteExpression(node.Type, -1, $"{cteRef.Alias} AS {alias}");
            TableColumns = cteRef.Columns
                .ToDictionary(kv => kv.Key, Expression (kv) => new SQLiteExpression(
                    ((SQLiteExpression)kv.Value).Type,
                    Counters.IdentifierIndex++,
                    $"{alias}.{kv.Key}"));

            return new SQLiteExpression(node.Type, -1, From.Sql);
        }

        return ResolveMember(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        ResolvedModel resolved = ResolveExpression(node.Operand);

        if (resolved.SQLiteExpression == null)
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
            if (resolved.SQLiteExpression.Type == node.Type || node.Type == typeof(object))
            {
                return resolved.SQLiteExpression;
            }
            else if (node.Type == typeof(char) && resolved.SQLiteExpression.Type == typeof(int))
            {
                return new SQLiteExpression(node.Type, Counters.IdentifierIndex++, $"CHAR({resolved.SQLiteExpression.Sql})", resolved.SQLiteExpression.Parameters);
            }
            else if (node.Type == typeof(int) && resolved.SQLiteExpression.Type == typeof(char))
            {
                return new SQLiteExpression(node.Type, Counters.IdentifierIndex++, $"UNICODE({resolved.SQLiteExpression.Sql})", resolved.SQLiteExpression.Parameters);
            }
            else if (resolved.SQLiteExpression.Type.IsEnum && (Nullable.GetUnderlyingType(node.Type) ?? node.Type) == Enum.GetUnderlyingType(resolved.SQLiteExpression.Type))
            {
                return new SQLiteExpression(node.Type, Counters.IdentifierIndex++, resolved.SQLiteExpression.Sql, resolved.SQLiteExpression.Parameters);
            }
            else
            {
                string sqliteType = TypeHelpers.TypeToSQLiteType(node.Type, Database.Options).ToString().ToUpper();
                return new SQLiteExpression(node.Type,
                    Counters.IdentifierIndex++,
                    $"CAST({resolved.SQLiteExpression.Sql} AS {sqliteType})",
                    resolved.SQLiteExpression.Parameters
                );
            }
        }

        string sql = node.NodeType switch
        {
            ExpressionType.Negate => $"-{resolved.SQLiteExpression.Sql}",
            ExpressionType.Not when node.Type == typeof(bool) => $"NOT {resolved.SQLiteExpression.Sql}",
            ExpressionType.Not => $"~{resolved.SQLiteExpression.Sql}",
            _ => throw new NotSupportedException($"Unsupported unary op {node.NodeType}")
        };

        return new SQLiteExpression(node.Type, Counters.IdentifierIndex++, sql, resolved.SQLiteExpression.Parameters);
    }

    [ExcludeFromCodeCoverage(Justification = "In theory we should never enter here")]
    private SQLiteExpression ResolvedUnary(UnaryExpression node, ResolvedModel resolved)
    {
        if (node.NodeType == ExpressionType.Convert)
        {
            object? value = resolved.Constant;
            Type targetType = Nullable.GetUnderlyingType(node.Type) ?? node.Type;
            if (value?.GetType().IsEnum == true && targetType == Enum.GetUnderlyingType(value.GetType()))
            {
                return new SQLiteExpression(node.Type, Counters.IdentifierIndex++, $"@p{Counters.ParamIndex++}", value);
            }

            return new SQLiteExpression(node.Type, Counters.IdentifierIndex++, $"@p{Counters.ParamIndex++}", Convert.ChangeType(value, targetType));
        }

        return resolved.SQLiteExpression!;
    }
}
