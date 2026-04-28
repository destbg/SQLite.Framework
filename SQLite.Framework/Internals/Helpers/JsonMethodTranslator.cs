namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Dispatches LINQ collection-method calls (<see cref="Enumerable" />, <see cref="List{T}" />,
/// <see cref="Array" />) to JSON-aware SQL translations when the source is a JSON-stored column.
/// Returns <see langword="null" /> when the call is not on a JSON-stored source so callers can
/// fall through to the regular dispatch.
/// </summary>
internal static class JsonMethodTranslator
{
    public static Expression? TryHandle(MethodCallExpression node, SQLVisitor visitor)
    {
        Type declaring = node.Method.DeclaringType!;

        Expression? sourceExpr = node.Object ?? (node.Arguments.Count > 0 ? node.Arguments[0] : null);
        bool sourceIsEnumerableChain = sourceExpr is MethodCallExpression mce
            && mce.Method.DeclaringType == typeof(Enumerable);
        if (!sourceIsEnumerableChain
            && (sourceExpr == null || !IsJsonCollection(sourceExpr.Type, visitor.Database.Options)))
        {
            return null;
        }

        if (declaring == typeof(Enumerable))
        {
            return JsonCollectionVisitor.TryHandle(node, visitor) ?? TryEnumerable(node, visitor);
        }

        if (declaring.IsGenericType && declaring.GetGenericTypeDefinition() == typeof(List<>))
        {
            return TryList(node, visitor);
        }

        if (declaring == typeof(Array))
        {
            return TryArray(node, visitor);
        }

        return null;
    }

    private static SQLExpression? TryEnumerable(MethodCallExpression node, SQLVisitor visitor)
    {
        ResolvedModel source = visitor.ResolveExpression(node.Arguments[0]);
        if (source.SQLExpression == null || !IsJsonCollection(source.SQLExpression.Type, visitor.Database.Options))
        {
            return null;
        }

        string src = source.SQLExpression.Sql;
        SQLiteParameter[]? parameters = source.SQLExpression.Parameters;

        if (node.Arguments.Count == 1)
        {
            string? sql = node.Method.Name switch
            {
                nameof(Enumerable.Any) => $"json_array_length({src}) > 0",
                nameof(Enumerable.Count) => $"json_array_length({src})",
                nameof(Enumerable.First) or nameof(Enumerable.FirstOrDefault) => $"json_extract({src}, '$[0]')",
                nameof(Enumerable.Last) or nameof(Enumerable.LastOrDefault) =>
                    $"CASE WHEN json_array_length({src}) > 0 THEN json_extract({src}, '$[' || (json_array_length({src}) - 1) || ']') ELSE NULL END",
                nameof(Enumerable.Single) or nameof(Enumerable.SingleOrDefault) =>
                    $"CASE WHEN json_array_length({src}) = 1 THEN json_extract({src}, '$[0]') ELSE NULL END",
                nameof(Enumerable.Min) => $"(SELECT MIN(value) FROM json_each({src}))",
                nameof(Enumerable.Max) => $"(SELECT MAX(value) FROM json_each({src}))",
                nameof(Enumerable.Sum) => $"(SELECT SUM(value) FROM json_each({src}))",
                nameof(Enumerable.Average) => $"(SELECT AVG(value) FROM json_each({src}))",
                nameof(Enumerable.Distinct) => $"(SELECT json_group_array(DISTINCT value) FROM json_each({src}))",
                nameof(Enumerable.Reverse) => $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({src}) ORDER BY key DESC))",
                _ => null,
            };

            return sql == null ? null : new SQLExpression(node.Type, visitor.IdentifierIndex.Index++, sql, parameters) { IsJsonSource = true };
        }

        if (node.Arguments.Count == 2)
        {
            ResolvedModel arg = visitor.ResolveExpression(node.Arguments[1]);
            string argSql = arg.Sql ?? string.Empty;
            SQLiteParameter[]? combined = CommonHelpers.CombineParameters(
                source.SQLExpression,
                arg.SQLExpression ?? source.SQLExpression);

            string? sql = node.Method.Name switch
            {
                nameof(Enumerable.Concat) => $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({src}) UNION ALL SELECT value FROM json_each({argSql})))",
                nameof(Enumerable.Union) => $"(SELECT json_group_array(value) FROM (SELECT DISTINCT value FROM json_each({src}) UNION SELECT DISTINCT value FROM json_each({argSql})))",
                nameof(Enumerable.Intersect) => $"(SELECT json_group_array(value) FROM json_each({src}) WHERE value IN (SELECT value FROM json_each({argSql})))",
                nameof(Enumerable.Except) => $"(SELECT json_group_array(value) FROM json_each({src}) WHERE value NOT IN (SELECT value FROM json_each({argSql})))",
                _ => null,
            };

            if (sql != null)
            {
                return new SQLExpression(node.Type, visitor.IdentifierIndex.Index++, sql, combined) { IsJsonSource = true };
            }
        }

        return null;
    }

    private static SQLExpression? TryList(MethodCallExpression node, SQLVisitor visitor)
    {
        ResolvedModel source = visitor.ResolveExpression(node.Object!);
        string src = source.SQLExpression!.Sql;

        if (node.Method.Name == nameof(List<>.Contains) && node.Arguments.Count == 1)
        {
            ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(
                source.SQLExpression,
                arg.SQLExpression ?? source.SQLExpression);
            return new SQLExpression(typeof(bool), visitor.IdentifierIndex.Index++,
                $"EXISTS (SELECT 1 FROM json_each({src}) WHERE value = {arg.Sql})",
                parameters)
            { IsJsonSource = true };
        }

        if (node.Method.Name == nameof(List<>.IndexOf) && node.Arguments.Count == 1)
        {
            ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(
                source.SQLExpression,
                arg.SQLExpression ?? source.SQLExpression);
            return new SQLExpression(typeof(int), visitor.IdentifierIndex.Index++,
                $"COALESCE((SELECT key FROM json_each({src}) WHERE value = {arg.Sql} LIMIT 1), -1)",
                parameters)
            { IsJsonSource = true };
        }

        if (node.Method.Name == nameof(List<>.LastIndexOf) && node.Arguments.Count == 1)
        {
            ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(
                source.SQLExpression,
                arg.SQLExpression ?? source.SQLExpression);
            return new SQLExpression(typeof(int), visitor.IdentifierIndex.Index++,
                $"COALESCE((SELECT key FROM json_each({src}) WHERE value = {arg.Sql} ORDER BY key DESC LIMIT 1), -1)",
                parameters)
            { IsJsonSource = true };
        }

        if (node.Method.Name == nameof(List<>.GetRange) && node.Arguments.Count == 2)
        {
            ResolvedModel idx = visitor.ResolveExpression(node.Arguments[0]);
            ResolvedModel cnt = visitor.ResolveExpression(node.Arguments[1]);
            return new SQLExpression(node.Type, visitor.IdentifierIndex.Index++,
                $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({src}) LIMIT {cnt.Sql} OFFSET {idx.Sql}))",
                CombineAll(source.SQLExpression, idx.SQLExpression, cnt.SQLExpression))
            { IsJsonSource = true };
        }

#if NET9_0_OR_GREATER
        if (node.Method.Name == nameof(List<>.Slice) && node.Arguments.Count == 2)
        {
            ResolvedModel idx = visitor.ResolveExpression(node.Arguments[0]);
            ResolvedModel cnt = visitor.ResolveExpression(node.Arguments[1]);
            return new SQLExpression(node.Type, visitor.IdentifierIndex.Index++,
                $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({src}) LIMIT {cnt.Sql} OFFSET {idx.Sql}))",
                CombineAll(source.SQLExpression, idx.SQLExpression, cnt.SQLExpression))
            { IsJsonSource = true };
        }
#endif

        return TryListPredicate(node, visitor, source.SQLExpression, src);
    }

    private static SQLExpression? TryListPredicate(MethodCallExpression node, SQLVisitor visitor, SQLExpression source, string src)
    {
        if (node.Arguments.Count != 1)
        {
            return null;
        }

        if (node.Method.Name is not (
            nameof(List<>.Exists)
            or nameof(List<>.Find)
            or nameof(List<>.FindAll)
            or nameof(List<>.FindIndex)
            or nameof(List<>.FindLast)
            or nameof(List<>.FindLastIndex)
            or nameof(List<>.TrueForAll)))
        {
            return null;
        }

        Expression stripped = CommonHelpers.StripQuotes(node.Arguments[0]);
        if (stripped is not LambdaExpression lambda)
        {
            return null;
        }

        (string predSql, SQLiteParameter[]? predParams) = VisitElementLambda(visitor, lambda);
        SQLiteParameter[]? combined = CombineAll(source, predParams == null ? null : new SQLExpression(typeof(object), -1, "", predParams) { IsJsonSource = true });

        return node.Method.Name switch
        {
            nameof(List<>.Exists) => new SQLExpression(typeof(bool), visitor.IdentifierIndex.Index++,
                $"EXISTS (SELECT 1 FROM json_each({src}) WHERE {predSql})", combined)
            { IsJsonSource = true },
            nameof(List<>.Find) => new SQLExpression(node.Type, visitor.IdentifierIndex.Index++,
                $"(SELECT value FROM json_each({src}) WHERE {predSql} ORDER BY key LIMIT 1)", combined)
            { IsJsonSource = true },
            nameof(List<>.FindAll) => new SQLExpression(node.Type, visitor.IdentifierIndex.Index++,
                $"(SELECT json_group_array(value) FROM json_each({src}) WHERE {predSql})", combined)
            { IsJsonSource = true },
            nameof(List<>.FindIndex) => new SQLExpression(typeof(int), visitor.IdentifierIndex.Index++,
                $"COALESCE((SELECT key FROM json_each({src}) WHERE {predSql} ORDER BY key LIMIT 1), -1)", combined)
            { IsJsonSource = true },
            nameof(List<>.FindLast) => new SQLExpression(node.Type, visitor.IdentifierIndex.Index++,
                $"(SELECT value FROM json_each({src}) WHERE {predSql} ORDER BY key DESC LIMIT 1)", combined)
            { IsJsonSource = true },
            nameof(List<>.FindLastIndex) => new SQLExpression(typeof(int), visitor.IdentifierIndex.Index++,
                $"COALESCE((SELECT key FROM json_each({src}) WHERE {predSql} ORDER BY key DESC LIMIT 1), -1)", combined)
            { IsJsonSource = true },
            _ => new SQLExpression(typeof(bool), visitor.IdentifierIndex.Index++,
                $"NOT EXISTS (SELECT 1 FROM json_each({src}) WHERE NOT ({predSql}))", combined)
            { IsJsonSource = true },
        };
    }

    private static SQLExpression? TryArray(MethodCallExpression node, SQLVisitor visitor)
    {
        ResolvedModel source = visitor.ResolveExpression(node.Arguments[0]);
        string src = source.SQLExpression!.Sql;

        if (node.Arguments.Count == 2 && node.Arguments[1] is Expression secondArg)
        {
            Expression stripped = CommonHelpers.StripQuotes(secondArg);
            if (stripped is LambdaExpression lambda
                && node.Method.Name is
                    nameof(Array.Exists)
                    or nameof(Array.Find)
                    or nameof(Array.FindAll)
                    or nameof(Array.FindIndex)
                    or nameof(Array.FindLast)
                    or nameof(Array.FindLastIndex)
                    or nameof(Array.TrueForAll)
                    or nameof(Array.ConvertAll))
            {
                (string predSql, SQLiteParameter[]? predParams) = VisitElementLambda(visitor, lambda);
                SQLiteParameter[]? combined = CombineAll(source.SQLExpression, predParams == null ? null : new SQLExpression(typeof(object), -1, "", predParams) { IsJsonSource = true });
                return node.Method.Name switch
                {
                    nameof(Array.Exists) => new SQLExpression(typeof(bool), visitor.IdentifierIndex.Index++,
                        $"EXISTS (SELECT 1 FROM json_each({src}) WHERE {predSql})", combined)
                    { IsJsonSource = true },
                    nameof(Array.Find) => new SQLExpression(node.Type, visitor.IdentifierIndex.Index++,
                        $"(SELECT value FROM json_each({src}) WHERE {predSql} ORDER BY key LIMIT 1)", combined)
                    { IsJsonSource = true },
                    nameof(Array.FindAll) => new SQLExpression(node.Type, visitor.IdentifierIndex.Index++,
                        $"(SELECT json_group_array(value) FROM json_each({src}) WHERE {predSql})", combined)
                    { IsJsonSource = true },
                    nameof(Array.FindIndex) => new SQLExpression(typeof(int), visitor.IdentifierIndex.Index++,
                        $"COALESCE((SELECT key FROM json_each({src}) WHERE {predSql} ORDER BY key LIMIT 1), -1)", combined)
                    { IsJsonSource = true },
                    nameof(Array.FindLast) => new SQLExpression(node.Type, visitor.IdentifierIndex.Index++,
                        $"(SELECT value FROM json_each({src}) WHERE {predSql} ORDER BY key DESC LIMIT 1)", combined)
                    { IsJsonSource = true },
                    nameof(Array.FindLastIndex) => new SQLExpression(typeof(int), visitor.IdentifierIndex.Index++,
                        $"COALESCE((SELECT key FROM json_each({src}) WHERE {predSql} ORDER BY key DESC LIMIT 1), -1)", combined)
                    { IsJsonSource = true },
                    nameof(Array.TrueForAll) => new SQLExpression(typeof(bool), visitor.IdentifierIndex.Index++,
                        $"NOT EXISTS (SELECT 1 FROM json_each({src}) WHERE NOT ({predSql}))", combined)
                    { IsJsonSource = true },
                    _ => new SQLExpression(node.Type, visitor.IdentifierIndex.Index++,
                        $"(SELECT json_group_array({predSql}) FROM json_each({src}))", combined)
                    { IsJsonSource = true },
                };
            }

            ResolvedModel arg = visitor.ResolveExpression(secondArg);
            string argSql = arg.Sql ?? string.Empty;
            SQLiteParameter[]? parameters = CommonHelpers.CombineParameters(
                source.SQLExpression,
                arg.SQLExpression ?? source.SQLExpression);

            return node.Method.Name switch
            {
                nameof(Array.IndexOf) => new SQLExpression(typeof(int), visitor.IdentifierIndex.Index++,
                    $"COALESCE((SELECT key FROM json_each({src}) WHERE value = {argSql} LIMIT 1), -1)", parameters)
                { IsJsonSource = true },
                nameof(Array.LastIndexOf) => new SQLExpression(typeof(int), visitor.IdentifierIndex.Index++,
                    $"COALESCE((SELECT key FROM json_each({src}) WHERE value = {argSql} ORDER BY key DESC LIMIT 1), -1)", parameters)
                { IsJsonSource = true },
                _ => null,
            };
        }

        return null;
    }

    private static (string Sql, SQLiteParameter[]? Parameters) VisitElementLambda(SQLVisitor visitor, LambdaExpression lambda)
    {
        ParameterExpression param = lambda.Parameters[0];
        Type elementType = param.Type;

        Dictionary<string, Expression> bindings;
        if (CommonHelpers.IsSimple(elementType, visitor.Database.Options))
        {
            SQLExpression valueExpr = new(elementType, -1, "value", (SQLiteParameter[]?)null);
            bindings = new Dictionary<string, Expression> { [""] = valueExpr };
        }
        else
        {
            bindings = new Dictionary<string, Expression>();
            RegisterProperties(elementType, string.Empty, "value", bindings);
        }

        visitor.MethodArguments[param] = bindings;
        try
        {
            Expression result = visitor.Visit(lambda.Body);
            if (result is SQLExpression sqlExpr)
            {
                return (sqlExpr.Sql, sqlExpr.Parameters);
            }

            throw new NotSupportedException($"Cannot translate lambda body: {lambda.Body}");
        }
        finally
        {
            visitor.MethodArguments.Remove(param);
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private static void RegisterProperties(Type type, string prefix, string valueSql, Dictionary<string, Expression> dict)
    {
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            string dictKey = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
            string sql = $"json_extract({valueSql}, '$.{dictKey}')";
            dict[dictKey] = new SQLExpression(prop.PropertyType, -1, sql, (SQLiteParameter[]?)null) { IsJsonSource = true };
        }
    }

    private static bool IsJsonCollection(Type type, SQLiteOptions options)
    {
        return options.TypeConverters.ContainsKey(type)
               && CommonHelpers.GetEnumerableElementType(type) != null;
    }

    private static SQLiteParameter[]? CombineAll(params SQLExpression?[] exprs)
    {
        SQLExpression[] non = exprs.Where(e => e != null).Cast<SQLExpression>().ToArray();
        return CommonHelpers.CombineParameters(non);
    }
}
