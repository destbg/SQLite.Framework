namespace SQLite.Framework.Internals.Visitors;

internal static class SQLiteFTS5FunctionsMemberVisitor
{
    public static Expression HandleSQLiteFTS5FunctionsMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        return node.Method.Name switch
        {
            nameof(SQLiteFTS5Functions.Match) => HandleFTS5Match(visitor, node),
            nameof(SQLiteFTS5Functions.Rank) => HandleFTS5Rank(visitor, node),
            nameof(SQLiteFTS5Functions.Snippet) => HandleFTS5Snippet(visitor, node),
            nameof(SQLiteFTS5Functions.Highlight) => HandleFTS5Highlight(visitor, node),
            _ => throw new NotSupportedException($"SQLiteFTS5Functions.{node.Method.Name} is not translatable to SQL."),
        };
    }

    private static SQLiteExpression HandleFTS5Match(SQLVisitor visitor, MethodCallExpression node)
    {
        Expression first = node.Arguments[0];
        Expression second = node.Arguments[1];

        bool firstIsColumn = node.Method.GetParameters()[0].ParameterType == typeof(string);

        Type entityType;
        string? columnName = null;

        if (firstIsColumn)
        {
            if (first is MemberExpression me && me.Expression != null)
            {
                columnName = me.Member.Name;
                entityType = me.Expression.Type;
            }
            else if (first is UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression me2 } && me2.Expression != null)
            {
                columnName = me2.Member.Name;
                entityType = me2.Expression.Type;
            }
            else
            {
                throw new NotSupportedException("SQLiteFTS5Functions.Match column reference must be a direct property access like a.Title.");
            }
        }
        else
        {
            entityType = first.Type;
        }

        string tableName = ResolveFTS5TableName(visitor, entityType);

        if (second.Type == typeof(string))
        {
            object? value = ExpressionHelpers.GetConstantValue(second);
            string queryString = (string)(value ?? string.Empty);
            if (columnName != null)
            {
                queryString = "{" + columnName + "} : " + queryString;
            }

            string pName = $"@p{visitor.Counters.ParamIndex++}";
            SQLiteParameter parameter = new() { Name = pName, Value = queryString };
            return new SQLiteExpression(typeof(bool), visitor.Counters.IdentifierIndex++, $"\"{tableName}\" MATCH {pName}", [parameter]);
        }

        Expression body = UnwrapPredicateBody(second);
        List<FtsQueryPart> parts = FtsHelpers.RenderFTSMatch(body, visitor);
        return BuildFTS5MatchSql(visitor, tableName, columnName, parts);
    }

    private static SQLiteExpression BuildFTS5MatchSql(SQLVisitor visitor, string tableName, string? columnName, List<FtsQueryPart> parts)
    {
        bool hasDynamic = parts.Any(p => p.DynamicSql != null);

        if (!hasDynamic)
        {
            string body = string.Concat(parts.Select(p => p.LiteralText));
            string queryString = columnName != null ? "{" + columnName + "} : (" + body + ")" : body;
            string pName = $"@p{visitor.Counters.ParamIndex++}";
            SQLiteParameter parameter = new() { Name = pName, Value = queryString };
            return new SQLiteExpression(typeof(bool), visitor.Counters.IdentifierIndex++, $"\"{tableName}\" MATCH {pName}", [parameter]);
        }

        StringBuilder operand = StringBuilderPool.Rent();
        InlineParameterBuffer8 parameters = default;

        if (columnName != null)
        {
            string prefixLiteral = "{" + columnName + "} : (";
            AppendLiteralPart(visitor, operand, ref parameters, prefixLiteral);
        }

        for (int i = 0; i < parts.Count; i++)
        {
            FtsQueryPart part = parts[i];
            if (part.LiteralText != null)
            {
                AppendLiteralPart(visitor, operand, ref parameters, part.LiteralText);
            }
            else
            {
                AppendDynamicPart(operand, ref parameters, part.DynamicSql!);
            }
        }

        if (columnName != null)
        {
            AppendLiteralPart(visitor, operand, ref parameters, ")");
        }

        string operandSql = StringBuilderPool.ToStringAndReturn(operand);
        return new SQLiteExpression(typeof(bool), visitor.Counters.IdentifierIndex++, $"\"{tableName}\" MATCH ({operandSql})", parameters.ToArray());
    }

    private static void AppendLiteralPart(SQLVisitor visitor, StringBuilder operand, ref InlineParameterBuffer8 parameters, string text)
    {
        if (operand.Length > 0)
        {
            operand.Append(" || ");
        }

        string pName = $"@p{visitor.Counters.ParamIndex++}";
        parameters.Add(new SQLiteParameter { Name = pName, Value = text });
        operand.Append(pName);
    }

    private static Expression UnwrapPredicateBody(Expression expr)
    {
        Expression stripped = ExpressionHelpers.StripQuotes(expr);
        if (stripped is LambdaExpression lambda)
        {
            return lambda.Body;
        }

        return expr;
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "FTS5 entity type is referenced by user code.")]
    private static SQLiteExpression HandleFTS5Rank(SQLVisitor visitor, MethodCallExpression node)
    {
        string alias = ResolveEntityAlias(visitor, node.Arguments[0]);
        Type entityType = node.Arguments[0].Type;
        TableMapping mapping = visitor.Database.TableMapping(entityType);

        if (mapping.FullTextSearch != null && mapping.FullTextSearch.IndexedColumns.Any(c => c.Weight != 1.0))
        {
            string weights = string.Join(", ", mapping.FullTextSearch.IndexedColumns
                .Select(c => c.Weight.ToString(CultureInfo.InvariantCulture)));
            return new SQLiteExpression(typeof(double), visitor.Counters.IdentifierIndex++, $"bm25(\"{mapping.TableName}\", {weights})");
        }

        return new SQLiteExpression(typeof(double), visitor.Counters.IdentifierIndex++, $"{alias}.rank");
    }

    private static SQLiteExpression HandleFTS5Snippet(SQLVisitor visitor, MethodCallExpression node)
    {
        Type entityType = node.Arguments[0].Type;
        string tableName = ResolveFTS5TableName(visitor, entityType);
        int columnIndex = ResolveFTS5ColumnIndex(visitor, entityType, node.Arguments[1]);

        SQLiteExpression before = ResolveAuxArg(visitor, node.Arguments[2]);
        SQLiteExpression after = ResolveAuxArg(visitor, node.Arguments[3]);
        SQLiteExpression ellipsis = ResolveAuxArg(visitor, node.Arguments[4]);
        SQLiteExpression tokens = ResolveAuxArg(visitor, node.Arguments[5]);

        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(before, after, ellipsis, tokens);
        return new SQLiteExpression(typeof(string), visitor.Counters.IdentifierIndex++, $"snippet(\"{tableName}\", {columnIndex}, {before.Sql}, {after.Sql}, {ellipsis.Sql}, {tokens.Sql})", parameters);
    }

    private static SQLiteExpression HandleFTS5Highlight(SQLVisitor visitor, MethodCallExpression node)
    {
        Type entityType = node.Arguments[0].Type;
        string tableName = ResolveFTS5TableName(visitor, entityType);
        int columnIndex = ResolveFTS5ColumnIndex(visitor, entityType, node.Arguments[1]);

        SQLiteExpression before = ResolveAuxArg(visitor, node.Arguments[2]);
        SQLiteExpression after = ResolveAuxArg(visitor, node.Arguments[3]);

        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(before, after);
        return new SQLiteExpression(typeof(string), visitor.Counters.IdentifierIndex++, $"highlight(\"{tableName}\", {columnIndex}, {before.Sql}, {after.Sql})", parameters);
    }

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "Entity type is referenced by user code via the LINQ expression.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Entity type is referenced by user code via the LINQ expression.")]
    private static string ResolveFTS5TableName(SQLVisitor visitor, Type entityType)
    {
        TableMapping mapping = visitor.Database.TableMapping(entityType);
        if (mapping.FullTextSearch == null)
        {
            throw new NotSupportedException($"SQLiteFTS5 method requires an entity with [FullTextSearch]; '{entityType.Name}' does not.");
        }

        return mapping.TableName;
    }

    private static SQLiteExpression ResolveAuxArg(SQLVisitor visitor, Expression expr)
    {
        return visitor.ResolveExpression(expr).SQLiteExpression!;
    }

    private static string ResolveEntityAlias(SQLVisitor visitor, Expression entity)
    {
        if (entity is ParameterExpression pe && visitor.MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? dict))
        {
            foreach (KeyValuePair<string, Expression> kv in dict)
            {
                if (kv.Value is SQLiteExpression sql)
                {
                    int dot = sql.Sql.IndexOf('.');
                    if (dot > 0)
                    {
                        return sql.Sql[..dot];
                    }
                }
            }
        }

        if (entity is MemberExpression member)
        {
            ResolvedModel resolved = visitor.ResolveExpression(member);
            if (resolved.SQLiteExpression != null)
            {
                int dot = resolved.SQLiteExpression.Sql.IndexOf('.');
                if (dot > 0)
                {
                    return resolved.SQLiteExpression.Sql[..dot];
                }
            }
        }

        throw new NotSupportedException($"SQLiteFTS5 method requires a direct entity reference; got {entity}.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "Entity type is referenced by user code via the LINQ expression.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Entity type is referenced by user code via the LINQ expression.")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Entity type is referenced by user code.")]
    private static int ResolveFTS5ColumnIndex(SQLVisitor visitor, Type entityType, Expression columnArg)
    {
        TableMapping mapping = visitor.Database.TableMapping(entityType);
        if (mapping.FullTextSearch == null)
        {
            throw new NotSupportedException($"SQLiteFTS5 method requires an entity with [FullTextSearch]; '{entityType.Name}' does not.");
        }

        string columnName = columnArg switch
        {
            MemberExpression me => me.Member.Name,
            UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression me2 } => me2.Member.Name,
            _ => throw new NotSupportedException("SQLiteFTS5 column argument must be a direct property reference like a.Title.")
        };

        for (int i = 0; i < mapping.FullTextSearch.IndexedColumns.Count; i++)
        {
            if (mapping.FullTextSearch.IndexedColumns[i].Name == columnName || mapping.FullTextSearch.IndexedColumns[i].Property.Name == columnName)
            {
                return i;
            }
        }

        throw new NotSupportedException($"SQLiteFTS5 column '{columnName}' is not declared on FTS entity '{entityType.Name}'.");
    }

    private static void AppendDynamicPart(StringBuilder operand, ref InlineParameterBuffer8 parameters, SQLiteExpression sql)
    {
        if (operand.Length > 0)
        {
            operand.Append(" || ");
        }

        if (sql.Parameters != null)
        {
            parameters.AddRange(sql.Parameters);
        }

        operand.Append("printf('\"%w\"', ");
        operand.Append(sql.Sql);
        operand.Append(')');
    }
}
