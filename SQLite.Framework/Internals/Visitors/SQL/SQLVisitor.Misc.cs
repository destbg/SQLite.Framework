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
            return SQLiteExpression.Alias(node.Type, -1, From!, From!.Parameters);
        }

        if (value is IPragmaTableSource pragmaSource)
        {
            AssignPragma(pragmaSource);
            return SQLiteExpression.Alias(node.Type, -1, From!, From!.Parameters);
        }

        if (value is BaseSQLiteTable table)
        {
            AssignTable(table.ElementType);
            return SQLiteExpression.Alias(node.Type, -1, From!, From!.Parameters);
        }

        return SQLiteExpression.Leaf(node.Type, Counters.NextIdentifier(), Counters.NextParamName(), value);
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

        return SQLiteExpression.Trinary(node.Type, Counters.NextIdentifier(), "(CASE WHEN ", test.SQLiteExpression!, " THEN ", ifTrue.SQLiteExpression!, " ELSE ", ifFalse.SQLiteExpression!, " END)", allParameters);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (CteParameters.TryGetValue(node, out (string Alias, Dictionary<string, Expression> Columns) cteRef))
        {
            char aliasChar = cteRef.Alias[0];
            string alias = $"{aliasChar}{Counters.NextTableIndex(aliasChar)}";

            From = SQLiteExpression.Leaf(node.Type, -1, $"{cteRef.Alias} AS {alias}");
            TableColumns = cteRef.Columns
                .ToDictionary(kv => kv.Key, Expression (kv) => SQLiteExpression.Leaf(
                    ((SQLiteExpression)kv.Value).Type,
                    Counters.NextIdentifier(),
                    $"{alias}.{IdentifierGuard.Quote(kv.Key)}"));

            return SQLiteExpression.Alias(node.Type, -1, From, null);
        }

        return ResolveMember(node);
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        return NotTranslatable(node, $"The '{node.NodeType}' operator is not translatable to SQL.");
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Not
            && node.Operand is BinaryExpression { NodeType: ExpressionType.Equal or ExpressionType.NotEqual } comparison)
        {
            ExpressionType flipped = comparison.NodeType == ExpressionType.Equal
                ? ExpressionType.NotEqual
                : ExpressionType.Equal;
            return Visit(Expression.MakeBinary(flipped, comparison.Left, comparison.Right));
        }

        if (node.NodeType == ExpressionType.Not
            && node.Operand is BinaryExpression
            {
                NodeType: ExpressionType.GreaterThan or ExpressionType.LessThan
                    or ExpressionType.GreaterThanOrEqual or ExpressionType.LessThanOrEqual
            } relational
            && (MayBeNull(relational.Left) || MayBeNull(relational.Right)))
        {
            ResolvedModel inner = ResolveExpression(relational);
            if (inner.SQLiteExpression != null)
            {
                string cmp = inner.SQLiteExpression.ToString();
                SQLiteParameter[]? cmpParameters = inner.SQLiteExpression.Parameters;
                return SQLiteExpression.Leaf(typeof(bool), Counters.NextIdentifier(), $"(NOT ({cmp}) OR ({cmp}) IS NULL)", cmpParameters);
            }
        }

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
            return resolved.SQLiteExpression!;
        }

        if (node.NodeType == ExpressionType.Convert)
        {
            if (node.Type == typeof(object))
            {
                return resolved.SQLiteExpression;
            }
            else if (resolved.SQLiteExpression.Type.IsGenericType
                && resolved.SQLiteExpression.Type.GetGenericTypeDefinition() == typeof(SQLiteWindow<>)
                && resolved.SQLiteExpression.Type.GetGenericArguments()[0] == node.Type)
            {
                return SQLiteExpression.Alias(node.Type, Counters.NextIdentifier(), resolved.SQLiteExpression, resolved.SQLiteExpression.Parameters);
            }
            else if (node.Type == typeof(char) && resolved.SQLiteExpression.Type == typeof(int))
            {
                return Database.Options.CharStorage == CharStorageMode.Integer
                    ? SQLiteExpression.Alias(node.Type, Counters.NextIdentifier(), resolved.SQLiteExpression, resolved.SQLiteExpression.Parameters)
                    : SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), "CHAR((", resolved.SQLiteExpression, ") & 65535)", resolved.SQLiteExpression.Parameters);
            }
            else if (node.Type == typeof(int) && resolved.SQLiteExpression.Type == typeof(char))
            {
                return Database.Options.CharStorage == CharStorageMode.Integer
                    ? SQLiteExpression.Alias(node.Type, Counters.NextIdentifier(), resolved.SQLiteExpression, resolved.SQLiteExpression.Parameters)
                    : SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), "UNICODE(", resolved.SQLiteExpression, ")", resolved.SQLiteExpression.Parameters);
            }
            else if (resolved.SQLiteExpression.Type.IsEnum && (Nullable.GetUnderlyingType(node.Type) ?? node.Type) == Enum.GetUnderlyingType(resolved.SQLiteExpression.Type))
            {
                return SQLiteExpression.Alias(node.Type, Counters.NextIdentifier(), resolved.SQLiteExpression, resolved.SQLiteExpression.Parameters);
            }
            else if (TryGetNarrowingIntegerWrap(resolved.SQLiteExpression.Type, node.Type, out string? wrapBefore, out string? wrapAfter))
            {
                return SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), wrapBefore!, resolved.SQLiteExpression, wrapAfter!, resolved.SQLiteExpression.Parameters);
            }
            else
            {
                string sqliteType = TypeHelpers.TypeToSQLiteType(node.Type, Database.Options).ToString().ToUpper();
                SQLiteExpression inner = resolved.SQLiteExpression;
                return SQLiteExpression.Wrap(node.Type,
                    Counters.NextIdentifier(),
                    "CAST(", inner, $" AS {sqliteType})",
                    resolved.SQLiteExpression.Parameters
                );
            }
        }

        SQLiteExpression operand = resolved.SQLiteExpression;

        if (node.NodeType == ExpressionType.TypeAs)
        {
            return NotTranslatable(node, $"The 'as' operator is not translatable to SQL.");
        }

        return node.NodeType switch
        {
            ExpressionType.Negate or ExpressionType.NegateChecked => SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), "-(", operand, ")", operand.Parameters),
            ExpressionType.Not when (Nullable.GetUnderlyingType(node.Type) ?? node.Type) == typeof(bool) => SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), "NOT ", BracketBooleanCompound(node.Operand, operand), "", operand.Parameters),
            ExpressionType.Not => SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), "~", operand, "", operand.Parameters),
            _ => throw new NotSupportedException($"Unsupported unary op {node.NodeType}")
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Pragma entity types are referenced by user code, so their public properties are rooted by the user.")]
    private void AssignPragma(IPragmaTableSource pragma)
    {
        Type entityType = pragma.ElementType;
        char aliasChar = char.ToLowerInvariant(entityType.Name[0]);
        string alias = $"{aliasChar}{Counters.NextTableIndex(aliasChar)}";

        TableMapping mapping = Database.TableMapping(entityType);

        SQLiteParameter[] parameters = pragma.Arguments
            .Select(arg => new SQLiteParameter { Name = Counters.NextParamName(), Value = arg })
            .ToArray();
        string argList = string.Join(", ", parameters.Select(p => p.Name));

        From = SQLiteExpression.Leaf(entityType, -1, $"{pragma.PragmaName}({argList}) AS {alias}", parameters);
        TableColumns = mapping.Columns
            .ToDictionary(f => f.PropertyInfo.Name, Expression (f) => SQLiteExpression.Leaf(f.PropertyType, Counters.NextIdentifier(), $"{alias}.\"{f.Name}\""));
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
            From = SQLiteExpression.Leaf(elementType, -1, $"{cachedName} AS {alias}");
            TableColumns = elementType.GetProperties()
                .ToDictionary(f => f.Name, Expression (f) => SQLiteExpression.Leaf(f.PropertyType, Counters.NextIdentifier(), $"{alias}.{IdentifierGuard.Quote(f.Name)}"));
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
                .ToDictionary(f => f.Name, Expression (f) => SQLiteExpression.Leaf(f.PropertyType, Counters.NextIdentifier(), $"{placeholder}.{IdentifierGuard.Quote(f.Name)}"));

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

        From = SQLiteExpression.Leaf(elementType, -1, $"{cteName} AS {alias}");

        TableColumns = elementType.GetProperties()
            .ToDictionary(f => f.Name, Expression (f) => SQLiteExpression.Leaf(f.PropertyType, Counters.NextIdentifier(), $"{alias}.{IdentifierGuard.Quote(f.Name)}"));
    }

    private static bool TryGetNarrowingIntegerWrap(Type sourceType, Type targetType, out string? before, out string? after)
    {
        before = null;
        after = null;

        if (!IsWrappableNarrowingTarget(targetType)
            || !TryGetIntegerInfo(sourceType, out int sourceBits, out bool sourceSigned)
            || !TryGetIntegerInfo(targetType, out int targetBits, out bool targetSigned))
        {
            return false;
        }

        if (IsIntegerRangeSubset(sourceBits, sourceSigned, targetBits, targetSigned))
        {
            return false;
        }

        long mask = (1L << targetBits) - 1;
        if (targetSigned)
        {
            long signBit = 1L << (targetBits - 1);
            long modulus = 1L << targetBits;
            before = "((((";
            after = ") & " + mask + ") + " + signBit + ") % " + modulus + " - " + signBit + ")";
        }
        else
        {
            before = "((";
            after = ") & " + mask + ")";
        }

        return true;
    }

    private static bool IsWrappableNarrowingTarget(Type target)
    {
        return target == typeof(sbyte)
            || target == typeof(byte)
            || target == typeof(short)
            || target == typeof(ushort)
            || target == typeof(int);
    }

    private static bool TryGetIntegerInfo(Type type, out int bits, out bool signed)
    {
        if (type == typeof(sbyte))
        {
            bits = 8;
            signed = true;
            return true;
        }
        if (type == typeof(byte))
        {
            bits = 8;
            signed = false;
            return true;
        }
        if (type == typeof(short))
        {
            bits = 16;
            signed = true;
            return true;
        }
        if (type == typeof(ushort))
        {
            bits = 16;
            signed = false;
            return true;
        }
        if (type == typeof(int))
        {
            bits = 32;
            signed = true;
            return true;
        }
        if (type == typeof(uint))
        {
            bits = 32;
            signed = false;
            return true;
        }
        if (type == typeof(long))
        {
            bits = 64;
            signed = true;
            return true;
        }
        if (type == typeof(ulong))
        {
            bits = 64;
            signed = false;
            return true;
        }

        bits = 0;
        signed = false;
        return false;
    }

    private static bool IsIntegerRangeSubset(int sourceBits, bool sourceSigned, int targetBits, bool targetSigned)
    {
        if (targetSigned)
        {
            return sourceSigned ? sourceBits <= targetBits : sourceBits <= targetBits - 1;
        }

        return !sourceSigned && sourceBits <= targetBits;
    }
}
