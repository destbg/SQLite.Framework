namespace SQLite.Framework.Internals.Visitors.Queryable;

internal partial class QueryableVisitor
{
    private SQLiteExpression VisitSetOperation(MethodCallExpression node, string setType)
    {
        ThrowIfReverse(node.Method.Name);

        if (OrderBys.Count > 0 || Take != null || Skip != null)
        {
            throw new NotSupportedException(
                $"{node.Method.Name} after OrderBy, Take, or Skip is not supported because it would require wrapping the operand in a subquery. " +
                "Materialize the ordered or paged operand into a list before combining.");
        }

        SQLTranslator sqlTranslator = visitor.CloneDeeper(visitor.Level);
        SQLQuery query = sqlTranslator.Translate(node.Arguments[1]);

        if (sqlTranslator.HasTopLevelOrderingOrPaging)
        {
            throw new NotSupportedException(
                $"{node.Method.Name} with an OrderBy, Take, or Skip on the combined operand is not supported because " +
                "its ORDER BY or LIMIT would apply to the whole combined result, not just that operand. " +
                "Materialize the ordered or paged operand into a list before combining.");
        }

        SQLiteExpression sqlExpression = SQLiteExpression.Leaf(
            node.Arguments[1].Type,
            visitor.Counters.NextIdentifier(),
            query.Sql,
            query.Parameters.Count == 0 ? null : query.Parameters.ToArray()
        );

        SetOperations.Add((sqlExpression, setType));

        return sqlExpression;
    }

    private MethodCallExpression VisitFromSql(MethodCallExpression node)
    {
        Type genericType = node.Method.ReturnType.GetGenericArguments()[0];
        string sql = (string)ExpressionHelpers.GetConstantValue(node.Arguments[0])!;
        IEnumerable<object> arguments = (IEnumerable<object>)ExpressionHelpers.GetConstantValue(node.Arguments[1])!;
        SQLiteParameter[] parameters = arguments.Select(a => (SQLiteParameter)a).ToArray();

        visitor.Counters.ReserveParamNames(parameters.Select(p => p.Name));
        visitor.AssignTable(genericType, SQLiteExpression.Leaf(genericType, -1, sql, parameters.Length == 0 ? null : parameters));
        return node;
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2065", Justification = "All types should have public properties.")]
    private MethodCallExpression VisitValues(MethodCallExpression node)
    {
        Type genericType = node.Method.ReturnType.GetGenericArguments()[0];
        bool isSimple = TypeHelpers.IsSimple(genericType, database.Options);

        List<string> columnNames = [];
        if (isSimple)
        {
            columnNames.Add("column__1");
        }
        else
        {
            foreach (PropertyInfo prop in genericType.GetProperties())
            {
                columnNames.Add(prop.Name);
            }
        }

        bool isMulti = node.Method.GetParameters()[0].ParameterType != genericType;
        IEnumerable<object?> rows = isMulti
            ? ((IEnumerable)ExpressionHelpers.GetConstantValue(node.Arguments[0])!).Cast<object?>()
            : [ExpressionHelpers.GetConstantValue(node.Arguments[0])];

        List<SQLiteParameter> sqlParams = [];
        List<string> selects = [];
        foreach (object? row in rows)
        {
            string[] cells = new string[columnNames.Count];
            for (int c = 0; c < columnNames.Count; c++)
            {
                string paramName = visitor.Counters.NextParamName();
                object? cellValue = isSimple ? row : genericType.GetProperty(columnNames[c])!.GetValue(row);
                sqlParams.Add(new SQLiteParameter { Name = paramName, Value = cellValue });
                cells[c] = selects.Count == 0 ? $"{paramName} AS \"{columnNames[c]}\"" : paramName;
            }
            selects.Add("SELECT " + string.Join(", ", cells));
        }

        char aliasChar = char.ToLowerInvariant(genericType.Name.FirstOrDefault(char.IsLetter, 'v'));
        string alias = $"{aliasChar}{visitor.Counters.NextTableIndex(aliasChar)}";

        string body = selects.Count == 0
            ? "SELECT " + string.Join(", ", columnNames.Select(c => $"NULL AS \"{c}\"")) + " WHERE 0"
            : string.Join(" UNION ALL ", selects);

        string valuesSql = $"({body}) AS {alias}";
        SQLiteExpression fromExpression = SQLiteExpression.Leaf(genericType, -1, valuesSql, sqlParams.Count == 0 ? null : sqlParams.ToArray());
        Dictionary<string, Expression> columns = columnNames
            .ToDictionary(
                col => col == "column__1" ? string.Empty : col, Expression (col) => SQLiteExpression.Leaf(
                    col == "column__1" ? genericType : genericType.GetProperty(col)!.PropertyType,
                    visitor.Counters.NextIdentifier(),
                    $"{alias}.\"{col}\""));

        visitor.AssignValues(fromExpression, columns);
        return node;
    }
}
