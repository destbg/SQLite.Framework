namespace SQLite.Framework.Internals.Visitors;

internal partial class QueryableVisitor
{
    private SQLiteExpression VisitSetOperation(MethodCallExpression node, string setType)
    {
        SQLTranslator sqlTranslator = visitor.CloneDeeper(visitor.Level);
        SQLQuery query = sqlTranslator.Translate(node.Arguments[1]);

        SQLiteExpression sqlExpression = new(
            node.Arguments[1].Type,
            visitor.Counters.IdentifierIndex++,
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

        visitor.AssignTable(genericType, new SQLiteExpression(genericType, -1, sql, parameters.Length == 0 ? null : parameters));
        return node;
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "All types should have public properties.")]
    [UnconditionalSuppressMessage("AOT", "IL2065", Justification = "All types should have public properties.")]
    private MethodCallExpression VisitValues(MethodCallExpression node)
    {
        Type genericType = node.Method.ReturnType.GetGenericArguments()[0];
        object? value = ExpressionHelpers.GetConstantValue(node.Arguments[0]);

        List<string> columnNames = [];
        List<string> paramPlaceholders = [];
        List<SQLiteParameter> sqlParams = [];

        if (TypeHelpers.IsSimple(genericType, database.Options))
        {
            string paramName = $"@p{visitor.Counters.ParamIndex++}";
            columnNames.Add("column__1");
            paramPlaceholders.Add(paramName);
            sqlParams.Add(new SQLiteParameter { Name = paramName, Value = value });
        }
        else
        {
            foreach (PropertyInfo prop in genericType.GetProperties())
            {
                string paramName = $"@p{visitor.Counters.ParamIndex++}";
                columnNames.Add(prop.Name);
                paramPlaceholders.Add(paramName);
                sqlParams.Add(new SQLiteParameter { Name = paramName, Value = prop.GetValue(value) });
            }
        }

        char aliasChar = char.ToLowerInvariant(genericType.Name.FirstOrDefault(char.IsLetter, 'v'));
        string alias = $"{aliasChar}{visitor.Counters.NextTableIndex(aliasChar)}";

        string selectList = string.Join(", ", paramPlaceholders.Select((p, i) => $"{p} AS \"{columnNames[i]}\""));
        string valuesSql = $"(SELECT {selectList}) AS {alias}";
        SQLiteExpression fromExpression = new(genericType, -1, valuesSql, sqlParams.ToArray());
        Dictionary<string, Expression> columns = columnNames
            .ToDictionary(
                col => col == "column__1" ? string.Empty : col, Expression (col) => new SQLiteExpression(
                    col == "column__1" ? genericType : genericType.GetProperty(col)!.PropertyType,
                    visitor.Counters.IdentifierIndex++,
                    $"{alias}.\"{col}\""));

        visitor.AssignValues(fromExpression, columns);
        return node;
    }
}
