namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private SQLiteExpression VisitSetOperation(MethodCallExpression node, string setType)
    {
        ThrowIfReverse(node.Method.Name);
        ComparerArgumentGuard.ThrowIfComparer(node);

        if (ClientTake != null || ClientSkip != null || OrderBys.Count > 0 || Take != null || Skip != null)
        {
            throw new NotSupportedException(
                $"{node.Method.Name} after OrderBy, Take or Skip is not supported because it would require wrapping the operand in a subquery. " +
                "Materialize the ordered or paged operand into a list before combining.");
        }

        if ((ClientProjection || (IsDistinct && LastSelectIsClient)) && !IsInnerQuery)
        {
            throw new NotSupportedException(
                $"{node.Method.Name} after a projection that runs in memory is not supported, " +
                "because SQLite cannot combine values the database never computes.");
        }

        SQLTranslator sqlTranslator = visitor.CloneDeeper(visitor.Level);
        sqlTranslator.SelectWrapFormats = visitor.SelectWrapFormats;
        SQLQuery query = sqlTranslator.Translate(node.Arguments[1], visitor.ExcludedSelectColumns);

        if (sqlTranslator.HasTopLevelOrderingOrPaging)
        {
            throw new NotSupportedException(
                $"{node.Method.Name} with an OrderBy, Take or Skip on the combined operand is not supported because " +
                "its ORDER BY or LIMIT would apply to the whole combined result, not just that operand. " +
                "Materialize the ordered or paged operand into a list before combining.");
        }

        if ((sqlTranslator.ClientProjection || sqlTranslator.LastSelectIsClient) && !IsInnerQuery)
        {
            throw new NotSupportedException(
                $"{node.Method.Name} with an operand whose projection runs in memory is not supported, " +
                "because SQLite cannot combine values the database never computes.");
        }

        ReconcileDayOfWeekSelects(sqlTranslator);
        ReconcileConstructedPaths(sqlTranslator);

        if (sqlTranslator.Visitor.OptionalRowColumns.Contains(sqlTranslator.Visitor.TableColumns))
        {
            visitor.OptionalRowColumns.Add(visitor.TableColumns);
        }

        string operandSql = sqlTranslator.HasSetOperations
            ? $"SELECT * FROM ({query.Sql})"
            : query.Sql;

        SQLiteExpression sqlExpression = SQLiteExpression.Leaf(
            node.Arguments[1].Type,
            visitor.Counters.NextIdentifier(),
            operandSql,
            query.Parameters.Count == 0 ? null : query.Parameters.ToArray()
        );

        SetOperations.Add((sqlExpression, setType));
        SetOperandSelects.Add(sqlTranslator.Selects.Select(s => s.IdentifierText).ToList());

        return sqlExpression;
    }

    private void ReconcileConstructedPaths(SQLTranslator operand)
    {
        if (!visitor.ConstructedProjectionPaths.TryGetValue(visitor.TableColumns, out HashSet<string>? mainPaths))
        {
            return;
        }

        if (operand.Visitor.ConstructedProjectionPaths.TryGetValue(operand.Visitor.TableColumns, out HashSet<string>? operandPaths))
        {
            mainPaths.IntersectWith(operandPaths);
        }
        else
        {
            mainPaths.Clear();
        }

        if (mainPaths.Count == 0)
        {
            visitor.ConstructedProjectionPaths.Remove(visitor.TableColumns);
        }
    }

    private void ReconcileDayOfWeekSelects(SQLTranslator operand)
    {
        if (visitor.Database.Options.EnumStorage != EnumStorageMode.Text)
        {
            return;
        }

        IReadOnlyList<SQLiteExpression> operandSelects = operand.Selects;
        int pairCount = Math.Min(Selects.Count, operandSelects.Count);
        Dictionary<string, string> identifierMap = new(StringComparer.Ordinal);
        for (int i = 0; i < pairCount; i++)
        {
            SQLiteExpression main = Selects[i];
            if (main.IsDayOfWeekInteger == operandSelects[i].IsDayOfWeekInteger)
            {
                continue;
            }

            string mainCanonical = CteSqlCanonicalizer.Canonicalize(main, identifierMap);
            SQLiteExpression replacement = main.IsDayOfWeekInteger
                ? EnumMemberVisitor.BuildEnumToNameText(visitor, typeof(DayOfWeek), main)
                : EnumMemberVisitor.BuildTextStorageEnumToNumber(visitor, typeof(int), typeof(DayOfWeek), main).WithDayOfWeekInteger();
            replacement.IdentifierText = main.IdentifierText;
            Selects[i] = replacement;

            if (main.IdentifierText is { Length: > 0 } mainName
                && visitor.TableColumns.TryGetValue(mainName, out Expression? column)
                && column is SQLiteExpression)
            {
                visitor.TableColumns[mainName] = replacement;
                continue;
            }

            foreach (KeyValuePair<string, Expression> entry in visitor.TableColumns)
            {
                if (entry.Value is SQLiteExpression entrySql
                    && CteSqlCanonicalizer.Canonicalize(entrySql, identifierMap) == mainCanonical)
                {
                    visitor.TableColumns[entry.Key] = replacement;
                    break;
                }
            }
        }
    }

    private MethodCallExpression VisitFromSql(MethodCallExpression node)
    {
        Type genericType = node.Method.ReturnType.GetGenericArguments()[0];
        string rawSql = (string)ExpressionHelpers.GetConstantValue(node.Arguments[0])!;
        if (SqlTail.HasMultipleStatements(rawSql))
        {
            throw new NotSupportedException(
                "The SQL contains more than one statement, which a query can only run partially. Use Execute for multi-statement batches.");
        }

        string sql = SqlTail.TrimStatementTail(rawSql);
        IEnumerable<object> arguments = (IEnumerable<object>)ExpressionHelpers.GetConstantValue(node.Arguments[1])!;
        SQLiteParameter[] parameters = arguments.Select(a => (SQLiteParameter)a).ToArray();

        visitor.Counters.ReserveParamNames(parameters.Select(p => p.Name));
        visitor.AssignTable(genericType, SQLiteExpression.Leaf(genericType, -1, sql, parameters.Length == 0 ? null : parameters));
        return node;
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2065", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2062", Justification = "Values element types are rooted by user code.")]
    private MethodCallExpression VisitValues(MethodCallExpression node)
    {
        Type genericType = node.Method.ReturnType.GetGenericArguments()[0];
        bool isSimple = TypeHelpers.IsSimple(genericType, database.Options);

        List<string> columnNames = [];
        List<PropertyInfo> properties = [];
        if (isSimple)
        {
            columnNames.Add("column__1");
        }
        else
        {
            properties = TypeHelpers.MappableProperties(genericType);
            foreach (PropertyInfo prop in properties)
            {
                columnNames.Add(prop.Name);
            }
        }

        bool isMulti = node.Method.GetParameters()[0].ParameterType != genericType;
        IEnumerable<object?> rows = isMulti
            ? ((IEnumerable)ExpressionHelpers.GetConstantValue(node.Arguments[0])!).Cast<object?>()
            : [ExpressionHelpers.GetConstantValue(node.Arguments[0])];

        List<SQLiteParameter> sqlParams = [];
        List<string> rowValues = [];
        foreach (object? row in rows)
        {
            string[] cells = new string[columnNames.Count];
            for (int c = 0; c < columnNames.Count; c++)
            {
                string paramName = visitor.Counters.NextParamName();
                object? cellValue = isSimple ? row : row == null ? null : properties[c].GetValue(row);
                sqlParams.Add(new SQLiteParameter { Name = paramName, Value = cellValue });
                cells[c] = paramName;
            }
            rowValues.Add("(" + string.Join(", ", cells) + ")");
        }

        char aliasChar = char.ToLowerInvariant(genericType.Name.FirstOrDefault(char.IsLetter, 'v'));
        string alias = $"{aliasChar}{visitor.Counters.NextTableIndex(aliasChar)}";

        string body = rowValues.Count == 0
            ? "SELECT " + string.Join(", ", columnNames.Select(c => $"NULL AS \"{c}\"")) + " WHERE 0"
            : "SELECT " + string.Join(", ", columnNames.Select((c, i) => $"column{i + 1} AS \"{c}\""))
                + " FROM (VALUES " + string.Join(", ", rowValues) + ")";

        string valuesSql = $"({body}) AS {alias}";
        SQLiteExpression fromExpression = SQLiteExpression.Leaf(genericType, -1, valuesSql, sqlParams.Count == 0 ? null : sqlParams.ToArray());
        Dictionary<string, Expression> columns = columnNames
            .Select((col, i) => (Name: col, Index: i))
            .ToDictionary(
                col => col.Name == "column__1" ? string.Empty : col.Name, Expression (col) => SQLiteExpression.Leaf(
                    col.Name == "column__1" ? genericType : properties[col.Index].PropertyType,
                    visitor.Counters.NextIdentifier(),
                    $"{alias}.\"{col.Name}\""));

        visitor.AssignValues(fromExpression, columns);
        return node;
    }
}
