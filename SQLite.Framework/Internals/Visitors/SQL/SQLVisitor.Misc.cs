namespace SQLite.Framework.Internals.Visitors.SQL;

internal partial class SQLVisitor
{
    public RecursiveCteBody TranslateRecursiveCteBody(Type elementType, string placeholder, ParameterExpression selfParam, Expression cteBody)
    {
        bool scalarElement = TypeHelpers.IsSimple(elementType, Database.Options);
        CteSelfReference reference = new()
        {
            Placeholder = placeholder,
            ElementType = elementType,
            Columns = CteColumnMapper.BuildColumns(elementType, placeholder, Database.Options, Counters)
        };

        for (int pass = 0; ; pass++)
        {
            CteParameters[selfParam] = reference;
            MethodArguments[selfParam] = reference.Columns;

            SQLTranslator bodyTranslator = CloneDeeper(Level + 1);
            bodyTranslator.SuppressSetOperationAlignment = true;
            SQLQuery bodyQuery = bodyTranslator.Translate(cteBody);

            bool hasClientMember = CteColumnMapper.HasClientBodyMember(bodyTranslator.Visitor.TableColumns);
            string[]? columnNames = hasClientMember
                ? CteColumnMapper.BodyColumnNamesWithPlaceholders(bodyTranslator.Visitor.TableColumns, bodyTranslator.Selects)
                : CteColumnMapper.ScalarColumnNames(elementType, Database.Options)
                    ?? CteColumnMapper.BodyColumnNames(bodyTranslator.Visitor.TableColumns, bodyTranslator.Selects);
            HashSet<string>? dayOfWeekColumns = CteColumnMapper.DayOfWeekColumns(bodyTranslator.Visitor.TableColumns, scalarElement);
            HashSet<string>? jsonSourceColumns = CteColumnMapper.JsonSourceColumns(bodyTranslator.Visitor.TableColumns, scalarElement);

            if (pass > 0 || (dayOfWeekColumns == null && jsonSourceColumns == null && !hasClientMember))
            {
                return new RecursiveCteBody
                {
                    Translator = bodyTranslator,
                    Query = bodyQuery,
                    ColumnNames = columnNames,
                    DayOfWeekColumns = dayOfWeekColumns,
                    JsonSourceColumns = jsonSourceColumns,
                    HasClientMember = hasClientMember
                };
            }

            Dictionary<string, Expression>? bodyColumns = hasClientMember ? bodyTranslator.Visitor.TableColumns : null;
            reference = new CteSelfReference
            {
                Placeholder = placeholder,
                ElementType = elementType,
                Columns = CteColumnMapper.BuildColumns(elementType, placeholder, Database.Options, Counters),
                ColumnNames = columnNames,
                DayOfWeekColumns = dayOfWeekColumns,
                JsonSourceColumns = jsonSourceColumns,
                ConstructedPaths = CteColumnMapper.BodyConstructedPaths(bodyTranslator.Visitor),
                BodyColumns = bodyColumns,
                BodySelects = bodyColumns != null ? bodyTranslator.Selects : null
            };
            CteColumnMapper.ApplyDayOfWeekColumns(reference.Columns, dayOfWeekColumns);
            CteColumnMapper.ApplyJsonSourceColumns(reference.Columns, jsonSourceColumns);
        }
    }

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
            AssignTable(table);
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
            return Expression.Condition(
                test.SQLiteExpression != null ? ToClientExpression(node.Test) : test.Expression,
                ifTrue.SQLiteExpression != null ? ToClientExpression(node.IfTrue) : ifTrue.Expression,
                ifFalse.SQLiteExpression != null ? ToClientExpression(node.IfFalse) : ifFalse.Expression);
        }

        SQLiteExpression ifTrueExpr = CoalesceLiftedOrderComparison(node.IfTrue, ifTrue.SQLiteExpression);
        SQLiteExpression ifFalseExpr = CoalesceLiftedOrderComparison(node.IfFalse, ifFalse.SQLiteExpression);

        bool dayOfWeekBranch = ifTrueExpr.IsDayOfWeekInteger || ifFalseExpr.IsDayOfWeekInteger;
        if (ifTrueExpr.IsDayOfWeekInteger != ifFalseExpr.IsDayOfWeekInteger)
        {
            Expression branchNode = ifTrueExpr.IsDayOfWeekInteger ? node.IfFalse : node.IfTrue;
            Expression converted = DayOfWeekHelpers.ConvertOperandToInt(Database.Options, branchNode);
            if (!ReferenceEquals(converted, branchNode))
            {
                SQLiteExpression convertedBranch = ResolveExpression(converted).SQLiteExpression!;
                if (ifTrueExpr.IsDayOfWeekInteger)
                {
                    ifFalseExpr = convertedBranch;
                }
                else
                {
                    ifTrueExpr = convertedBranch;
                }
            }
        }

        SQLiteParameter[]? allParameters =
            ParameterHelpers.CombineParameters(test.SQLiteExpression, ifTrueExpr, ifFalseExpr);

        SQLiteExpression conditional = SQLiteExpression.Trinary(node.Type, Counters.NextIdentifier(), "(CASE WHEN ", test.SQLiteExpression!, " THEN ", ifTrueExpr, " ELSE ", ifFalseExpr, " END)", allParameters);
        if (ifTrueExpr.IsJsonSource && ifFalseExpr.IsJsonSource)
        {
            conditional.WithJsonSource();
        }

        return dayOfWeekBranch ? conditional.WithDayOfWeekInteger() : conditional;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (CteParameters.TryGetValue(node, out CteSelfReference? cteRef))
        {
            char aliasChar = cteRef.Placeholder[0];
            string alias = $"{aliasChar}{Counters.NextTableIndex(aliasChar)}";

            From = SQLiteExpression.Leaf(node.Type, -1, $"{cteRef.Placeholder} AS {alias}");
            TableColumns = CteColumnMapper.BuildSelfColumns(cteRef, alias, Database.Options, Counters, this);

            return SQLiteExpression.Alias(node.Type, -1, From, null);
        }

        return ResolveMember(node);
    }

    protected override Expression VisitInvocation(InvocationExpression node)
    {
        return NotTranslatable(node, "Invoking a delegate is not translatable to SQL.");
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        return NotTranslatable(node, $"The '{node.NodeType}' operator is not translatable to SQL.");
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Convert
            && node.Operand is ConstantExpression { Value: BaseSQLiteTable })
        {
            return Visit(node.Operand);
        }

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
                return SQLiteExpression.Wrap(typeof(bool), Counters.NextIdentifier(), "(", inner.SQLiteExpression, ") IS NOT 1", inner.SQLiteExpression.Parameters);
            }
        }

        bool previousFtsMatchAsSubquery = FtsMatchAsSubquery;
        if (node.NodeType == ExpressionType.Not)
        {
            FtsMatchAsSubquery = true;
        }

        ResolvedModel resolved = ResolveExpression(node.Operand);
        FtsMatchAsSubquery = previousFtsMatchAsSubquery;

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
            return ExpressionHelpers.IsEvaluableUnary(node)
                ? ResolveExpression(node).SQLiteExpression!
                : resolved.SQLiteExpression!;
        }

        if (node.NodeType == ExpressionType.ArrayLength
            && (resolved.SQLiteExpression.IsJsonSource || Database.Options.HasJsonConverter(node.Operand.Type)))
        {
            return SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(),
                "json_array_length(", resolved.SQLiteExpression, ")", resolved.SQLiteExpression.Parameters);
        }

        if (node.NodeType == ExpressionType.ArrayLength
            && node.Operand.Type == typeof(byte[]))
        {
            return SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(),
                "LENGTH(", resolved.SQLiteExpression, ")", resolved.SQLiteExpression.Parameters);
        }

        if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
        {
            if (node.Type == typeof(object))
            {
                return resolved.SQLiteExpression;
            }
            else if (resolved.SQLiteExpression.Type.IsGenericType
                && resolved.SQLiteExpression.Type.GetGenericTypeDefinition() == typeof(SQLiteWindow<>)
                && resolved.SQLiteExpression.Type.GetGenericArguments()[0] == node.Type)
            {
                SQLiteExpression windowValue = SQLiteExpression.Alias(node.Type, Counters.NextIdentifier(), resolved.SQLiteExpression, resolved.SQLiteExpression.Parameters);
                if (resolved.SQLiteExpression.IsDayOfWeekInteger)
                {
                    windowValue.WithDayOfWeekInteger();
                }

                return windowValue;
            }
            else if ((Nullable.GetUnderlyingType(node.Type) ?? node.Type) == typeof(char)
                && (Nullable.GetUnderlyingType(resolved.SQLiteExpression.Type) ?? resolved.SQLiteExpression.Type) is { } charSourceType
                && (TryGetIntegerInfo(charSourceType, out _, out _) || IsFloatingPointType(charSourceType)))
            {
                SQLiteExpression charSource = resolved.SQLiteExpression;
                if (IsFloatingPointType(charSourceType))
                {
                    charSource = SQLiteExpression.Wrap(typeof(long), Counters.NextIdentifier(), "CAST(", charSource, " AS INTEGER)", charSource.Parameters);
                }

                if (Database.Options.CharStorage == CharStorageMode.Integer)
                {
                    return SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), "((", charSource, $") & {Constants.UInt16Mask})", charSource.Parameters);
                }

                return Nullable.GetUnderlyingType(node.Type) == null
                    ? SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), "CHAR((", charSource, $") & {Constants.UInt16Mask})", charSource.Parameters)
                    : CommonHelpers.EvaluateOnce(this, node.Type, [charSource], v =>
                        SQLiteExpression.Multi(node.Type, Counters.NextIdentifier(),
                            ["(CASE WHEN ", " IS NULL THEN NULL ELSE CHAR((", $") & {Constants.UInt16Mask}) END)"],
                            [v[0], v[0]],
                            null));
            }
            else if ((Nullable.GetUnderlyingType(resolved.SQLiteExpression.Type) ?? resolved.SQLiteExpression.Type) == typeof(char)
                && (Nullable.GetUnderlyingType(node.Type) ?? node.Type) != typeof(char))
            {
                SQLiteExpression charCode = Database.Options.CharStorage == CharStorageMode.Integer
                    ? resolved.SQLiteExpression
                    : SQLiteExpression.Wrap(typeof(int), Counters.NextIdentifier(), "UNICODE(", resolved.SQLiteExpression, ")", resolved.SQLiteExpression.Parameters);

                if ((Nullable.GetUnderlyingType(node.Type) ?? node.Type) == typeof(int))
                {
                    return SQLiteExpression.Alias(node.Type, Counters.NextIdentifier(), charCode, charCode.Parameters);
                }

                if (TryGetNarrowingIntegerWrap(typeof(ushort), node.Type, out string? charWrapBefore, out string? charWrapAfter))
                {
                    return SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), charWrapBefore!, charCode, charWrapAfter!, charCode.Parameters);
                }

                string charSqliteType = TypeHelpers.TypeToSQLiteType(node.Type, Database.Options).ToString().ToUpper();
                return SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), "CAST(", charCode, $" AS {charSqliteType})", charCode.Parameters);
            }
            else if ((Nullable.GetUnderlyingType(resolved.SQLiteExpression.Type) ?? resolved.SQLiteExpression.Type) is { IsEnum: true } sourceEnumType
                && IsTextStoredEnum(resolved.SQLiteExpression))
            {
                Type enumUnderlying = Enum.GetUnderlyingType(sourceEnumType);
                if (resolved.SQLiteExpression.IsDayOfWeekInteger
                    && (Nullable.GetUnderlyingType(node.Type) ?? node.Type) == sourceEnumType)
                {
                    return SQLiteExpression.Alias(node.Type, Counters.NextIdentifier(), resolved.SQLiteExpression, resolved.SQLiteExpression.Parameters)
                        .WithDayOfWeekInteger();
                }

                SQLiteExpression numberExpr = resolved.SQLiteExpression.IsDayOfWeekInteger
                    ? resolved.SQLiteExpression
                    : EnumMemberVisitor.BuildTextStorageEnumToNumber(this, enumUnderlying, sourceEnumType, resolved.SQLiteExpression, SubqueryFreeSql);

                if ((Nullable.GetUnderlyingType(node.Type) ?? node.Type) == enumUnderlying)
                {
                    return numberExpr;
                }

                if (TryGetNarrowingIntegerWrap(enumUnderlying, node.Type, out string? enumWrapBefore, out string? enumWrapAfter))
                {
                    return SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), enumWrapBefore!, numberExpr, enumWrapAfter!, numberExpr.Parameters);
                }

                if (IsUlongSource(enumUnderlying) && IsRealTarget(node.Type))
                {
                    return CommonHelpers.EvaluateOnce(this, node.Type, [numberExpr], v =>
                        SQLiteExpression.Multi(node.Type, Counters.NextIdentifier(),
                            ["(CAST(", " AS REAL) + (CASE WHEN ", $" < 0 THEN {Constants.UInt64ToRealOffset} ELSE 0 END))"],
                            [v[0], v[0]],
                            null));
                }

                string enumSqliteType = TypeHelpers.TypeToSQLiteType(node.Type, Database.Options).ToString().ToUpper();
                return SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), "CAST(", numberExpr, $" AS {enumSqliteType})", numberExpr.Parameters);
            }
            else if (resolved.SQLiteExpression.Type.IsEnum && (Nullable.GetUnderlyingType(node.Type) ?? node.Type) == Enum.GetUnderlyingType(resolved.SQLiteExpression.Type))
            {
                return SQLiteExpression.Alias(node.Type, Counters.NextIdentifier(), resolved.SQLiteExpression, resolved.SQLiteExpression.Parameters);
            }
            else if (TryGetNarrowingIntegerWrap(resolved.SQLiteExpression.Type, node.Type, out string? wrapBefore, out string? wrapAfter))
            {
                return SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), wrapBefore!, resolved.SQLiteExpression, wrapAfter!, resolved.SQLiteExpression.Parameters);
            }
            else if (IsUlongSource(resolved.SQLiteExpression.Type) && IsRealTarget(node.Type))
            {
                SQLiteExpression inner = resolved.SQLiteExpression;
                return SQLiteExpression.Multi(node.Type, Counters.NextIdentifier(),
                    ["(CAST(", " AS REAL) + (CASE WHEN ", $" < 0 THEN {Constants.UInt64ToRealOffset} ELSE 0 END))"],
                    [inner, inner],
                    inner.Parameters);
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
            ExpressionType.Not when (Nullable.GetUnderlyingType(node.Type) ?? node.Type) == typeof(uint) => SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), "(~", ExpressionHelpers.BracketIfNeeded(operand), $" & {Constants.UInt32Mask})", operand.Parameters),
            ExpressionType.Not => SQLiteExpression.Wrap(node.Type, Counters.NextIdentifier(), "~", ExpressionHelpers.BracketIfNeeded(operand), "", operand.Parameters),
            _ => throw new NotSupportedException($"Unsupported unary op {node.NodeType}")
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Pragma entity types are rooted by user code.")]
    private void AssignPragma(IPragmaTableSource pragma)
    {
        Type entityType = pragma.ElementType;
        char aliasChar = char.ToLowerInvariant(entityType.Name.FirstOrDefault(char.IsLetter, 't'));
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
        char aliasChar = char.ToLowerInvariant(elementType.Name.FirstOrDefault(char.IsLetter, 't'));
        string alias = $"{aliasChar}{Counters.NextTableIndex(aliasChar)}";

        string? cachedName = CteRegistry.TryGetName(cte);
        if (cachedName != null)
        {
            From = SQLiteExpression.Leaf(elementType, -1, $"{cachedName} AS {alias}");
            AssignCteColumns(cte, elementType, alias);
            return;
        }

        LambdaExpression lambda = cte.Query;
        bool isRecursive = lambda.Parameters.Count == 1;
        Expression cteBody = QueryFilterInjector.InjectCteBody(CommonHelpers.Inline(lambda.Body), Database, Counters);

        string cteName;

        if (isRecursive)
        {
            ParameterExpression selfParam = lambda.Parameters[0];

            string placeholder = $"{aliasChar}__cte_self_{CteRegistry.Ctes.Count}__";

            RecursiveCteBody recursive = TranslateRecursiveCteBody(elementType, placeholder, selfParam, cteBody);

            string finalName = $"cte{CteRegistry.Ctes.Count}";
            string fixedSql = recursive.Query.Sql.Replace(placeholder, finalName);

            Dictionary<string, Expression>? recursiveNodes = CteColumnMapper.BodyConstructedNodes(recursive.Translator.Visitor);
            cteName = CteRegistry.Register(
                fixedSql,
                recursive.Query.Parameters.Count == 0 ? null : [.. recursive.Query.Parameters],
                isRecursive: true,
                key: cte,
                columnNames: recursive.ColumnNames,
                dayOfWeekColumns: recursive.DayOfWeekColumns,
                jsonSourceColumns: recursive.JsonSourceColumns,
                constructedPaths: CteColumnMapper.BodyConstructedPaths(recursive.Translator.Visitor),
                constructedNodes: recursiveNodes,
                bodyColumns: recursive.HasClientMember ? recursive.Translator.Visitor.TableColumns : null,
                bodySelects: recursive.HasClientMember || recursiveNodes != null ? recursive.Translator.Selects : null,
                emittedColumns: CteColumnMapper.EmittedColumnNames(recursive.ColumnNames, recursive.Translator.Selects),
                optionalRow: recursive.Translator.Visitor.OptionalRowColumns.Contains(recursive.Translator.Visitor.TableColumns),
                optionalRowPaths: recursive.Translator.Visitor.OptionalRowPaths.TryGetValue(recursive.Translator.Visitor.TableColumns, out HashSet<string>? recursiveOptionalPaths)
                    ? recursiveOptionalPaths
                    : null);

            CteParameters.Remove(selfParam);
            MethodArguments.Remove(selfParam);
        }
        else
        {
            SQLTranslator bodyTranslator = CloneDeeper(Level + 1);
            SQLQuery bodyQuery = bodyTranslator.Translate(cteBody);

            if (bodyQuery.Reverse || bodyQuery.ReverseBeforeDistinct)
            {
                throw new NotSupportedException(
                    "The common table expression body ends with Reverse(), which only runs in memory after the query returns, " +
                    "so the expression cannot keep that order. Use OrderByDescending instead.");
            }

            string[]? bodyColumnNames = CteColumnMapper.DeclaredColumnNames(
                elementType, bodyTranslator.Visitor.TableColumns, bodyTranslator.Selects, Database.Options);
            bool hasClientMember = CteColumnMapper.HasClientBodyMember(bodyTranslator.Visitor.TableColumns)
                || CteColumnMapper.BodyColumnOrderIsAmbiguous(bodyTranslator.Visitor.TableColumns, bodyTranslator.Selects);
            Dictionary<string, Expression>? bodyNodes = CteColumnMapper.BodyConstructedNodes(bodyTranslator.Visitor);
            cteName = CteRegistry.Register(
                bodyQuery.Sql,
                bodyQuery.Parameters.Count == 0 ? null : [.. bodyQuery.Parameters],
                isRecursive: false,
                key: cte,
                columnNames: bodyColumnNames,
                dayOfWeekColumns: CteColumnMapper.DayOfWeekColumns(bodyTranslator.Visitor.TableColumns, TypeHelpers.IsSimple(elementType, Database.Options)),
                jsonSourceColumns: CteColumnMapper.JsonSourceColumns(bodyTranslator.Visitor.TableColumns, TypeHelpers.IsSimple(elementType, Database.Options)),
                constructedPaths: CteColumnMapper.BodyConstructedPaths(bodyTranslator.Visitor),
                constructedNodes: bodyNodes,
                bodyColumns: hasClientMember ? bodyTranslator.Visitor.TableColumns : null,
                bodySelects: hasClientMember || bodyNodes != null ? bodyTranslator.Selects : null,
                emittedColumns: CteColumnMapper.EmittedColumnNames(bodyColumnNames, bodyTranslator.Selects),
                optionalRow: bodyTranslator.Visitor.OptionalRowColumns.Contains(bodyTranslator.Visitor.TableColumns),
                optionalRowPaths: bodyTranslator.Visitor.OptionalRowPaths.TryGetValue(bodyTranslator.Visitor.TableColumns, out HashSet<string>? bodyOptionalPaths)
                    ? bodyOptionalPaths
                    : null);
        }

        From = SQLiteExpression.Leaf(elementType, -1, $"{cteName} AS {alias}");

        AssignCteColumns(cte, elementType, alias);
    }

    private void AssignCteColumns(SQLiteCte cte, Type elementType, string alias)
    {
        CteInfo info = CteRegistry!.Info(cte);
        TableColumns = CteColumnMapper.BuildOuterColumns(info, elementType, alias, Database.Options, Counters);
        CteColumnMapper.ApplyBodyTraits(TableColumns, info, this, alias);
    }

    private bool IsTextStoredEnum(SQLiteExpression expression)
    {
        return Database.Options.EnumStorage == EnumStorageMode.Text
            || (expression.IsJsonSource && JsonEnumText.IsStringStored(Database.Options, expression.Type));
    }

    private static bool IsFloatingPointType(Type type)
    {
        return type == typeof(double) || type == typeof(float) || type == typeof(decimal);
    }

    private static bool IsUlongSource(Type sourceType)
    {
        Type unwrapped = sourceType.IsEnum
            ? Enum.GetUnderlyingType(sourceType)
            : Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        return unwrapped == typeof(ulong);
    }

    private static bool IsRealTarget(Type targetType)
    {
        Type unwrapped = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return unwrapped == typeof(double) || unwrapped == typeof(float) || unwrapped == typeof(decimal);
    }

    private static bool TryGetNarrowingIntegerWrap(Type sourceType, Type targetType, out string? before, out string? after)
    {
        before = null;
        after = null;

        sourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        if (sourceType.IsEnum)
        {
            sourceType = Enum.GetUnderlyingType(sourceType);
        }

        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (targetType.IsEnum)
        {
            targetType = Enum.GetUnderlyingType(targetType);
        }

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
            || target == typeof(int)
            || target == typeof(uint);
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
