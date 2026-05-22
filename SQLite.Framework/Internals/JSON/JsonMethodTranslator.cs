namespace SQLite.Framework.Internals.JSON;

/// <summary>
/// Dispatches LINQ collection-method calls (<see cref="Enumerable" />, <see cref="List{T}" />,
/// <see cref="Array" />) to JSON-aware SQL translations when the source is a JSON-stored column.
/// Returns <see langword="null" /> when the call is not on a JSON-stored source so callers can
/// fall through to the regular dispatch.
/// </summary>
internal static class JsonMethodTranslator
{
    private static readonly HashSet<string> ArrayLambdaMethodNames =
    [
        nameof(Array.Exists),
        nameof(Array.Find),
        nameof(Array.FindAll),
        nameof(Array.FindIndex),
        nameof(Array.FindLast),
        nameof(Array.FindLastIndex),
        nameof(Array.TrueForAll),
        nameof(Array.ConvertAll),
    ];

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

#if SQLITE_FRAMEWORK_VERSION_AWARE
        visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_9, "JSON1 collection translation (json_each, json_extract)");
#endif

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

    private static SQLiteExpression? TryEnumerable(MethodCallExpression node, SQLVisitor visitor)
    {
        ResolvedModel source = visitor.ResolveExpression(node.Arguments[0]);
        if (source.SQLiteExpression == null || !IsJsonCollectionExpression(source.SQLiteExpression, visitor.Database.Options))
        {
            return null;
        }

        string src = source.SQLiteExpression.ToString();
        SQLiteParameter[]? parameters = source.SQLiteExpression.Parameters;

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

            return sql == null ? null : SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(), sql, parameters).WithJsonSource();
        }

        if (node.Arguments.Count == 2)
        {
            ResolvedModel arg = visitor.ResolveExpression(node.Arguments[1]);
            string argSql = arg.SQLiteExpression!.ToString();
            SQLiteParameter[]? combined = ParameterHelpers.CombineParameters(
                source.SQLiteExpression,
                arg.SQLiteExpression!);

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
                return SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(), sql, combined).WithJsonSource();
            }
        }

        return null;
    }

    private static SQLiteExpression? TryList(MethodCallExpression node, SQLVisitor visitor)
    {
        ResolvedModel source = visitor.ResolveExpression(node.Object!);
        string src = source.SQLiteExpression!.ToString();

        if (node.Method.Name == nameof(List<>.Contains) && node.Arguments.Count == 1)
        {
            ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(
                source.SQLiteExpression,
                arg.SQLiteExpression!);
            return SQLiteExpression.Leaf(typeof(bool), visitor.Counters.NextIdentifier(),
                $"EXISTS (SELECT 1 FROM json_each({src}) WHERE value = {arg.SQLiteExpression})",
                parameters)
                .WithJsonSource();
        }

        if (node.Method.Name == nameof(List<>.IndexOf) && node.Arguments.Count == 1)
        {
            ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(
                source.SQLiteExpression,
                arg.SQLiteExpression!);
            return SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(),
                $"COALESCE((SELECT key FROM json_each({src}) WHERE value = {arg.SQLiteExpression} LIMIT 1), -1)",
                parameters)
                .WithJsonSource();
        }

        if (node.Method.Name == nameof(List<>.LastIndexOf) && node.Arguments.Count == 1)
        {
            ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(
                source.SQLiteExpression,
                arg.SQLiteExpression!);
            return SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(),
                $"COALESCE((SELECT key FROM json_each({src}) WHERE value = {arg.SQLiteExpression} ORDER BY key DESC LIMIT 1), -1)",
                parameters)
                .WithJsonSource();
        }

        if (node.Method.Name == nameof(List<>.GetRange) && node.Arguments.Count == 2)
        {
            ResolvedModel idx = visitor.ResolveExpression(node.Arguments[0]);
            ResolvedModel cnt = visitor.ResolveExpression(node.Arguments[1]);
            return SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(),
                $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({src}) LIMIT {cnt.SQLiteExpression} OFFSET {idx.SQLiteExpression}))",
                CombineAll(source.SQLiteExpression, idx.SQLiteExpression, cnt.SQLiteExpression))
                .WithJsonSource();
        }

#if NET9_0_OR_GREATER
        if (node.Method.Name == nameof(List<>.Slice) && node.Arguments.Count == 2)
        {
            ResolvedModel idx = visitor.ResolveExpression(node.Arguments[0]);
            ResolvedModel cnt = visitor.ResolveExpression(node.Arguments[1]);
            return SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(),
                $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({src}) LIMIT {cnt.SQLiteExpression} OFFSET {idx.SQLiteExpression}))",
                CombineAll(source.SQLiteExpression, idx.SQLiteExpression, cnt.SQLiteExpression))
                .WithJsonSource();
        }
#endif

        return TryListPredicate(node, visitor, source.SQLiteExpression, src);
    }

    private static SQLiteExpression? TryListPredicate(MethodCallExpression node, SQLVisitor visitor, SQLiteExpression source, string src)
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

        Expression stripped = ExpressionHelpers.StripQuotes(node.Arguments[0]);
        if (stripped is not LambdaExpression lambda)
        {
            return null;
        }

        (string predSql, SQLiteParameter[]? predParams) = VisitElementLambda(visitor, lambda);
        SQLiteParameter[]? combined = CombineAll(source, predParams == null ? null : SQLiteExpression.Leaf(typeof(object), -1, "", predParams).WithJsonSource());

        return node.Method.Name switch
        {
            nameof(List<>.Exists) => SQLiteExpression.Leaf(typeof(bool), visitor.Counters.NextIdentifier(),
                $"EXISTS (SELECT 1 FROM json_each({src}) WHERE {predSql})", combined)
                .WithJsonSource(),
            nameof(List<>.Find) => SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(),
                $"(SELECT value FROM json_each({src}) WHERE {predSql} ORDER BY key LIMIT 1)", combined)
                .WithJsonSource(),
            nameof(List<>.FindAll) => SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(),
                $"(SELECT json_group_array(value) FROM json_each({src}) WHERE {predSql})", combined)
                .WithJsonSource(),
            nameof(List<>.FindIndex) => SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(),
                $"COALESCE((SELECT key FROM json_each({src}) WHERE {predSql} ORDER BY key LIMIT 1), -1)", combined)
                .WithJsonSource(),
            nameof(List<>.FindLast) => SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(),
                $"(SELECT value FROM json_each({src}) WHERE {predSql} ORDER BY key DESC LIMIT 1)", combined)
                .WithJsonSource(),
            nameof(List<>.FindLastIndex) => SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(),
                $"COALESCE((SELECT key FROM json_each({src}) WHERE {predSql} ORDER BY key DESC LIMIT 1), -1)", combined)
                .WithJsonSource(),
            _ => SQLiteExpression.Leaf(typeof(bool), visitor.Counters.NextIdentifier(),
                $"NOT EXISTS (SELECT 1 FROM json_each({src}) WHERE NOT ({predSql}))", combined)
                .WithJsonSource(),
        };
    }

    private static SQLiteExpression? TryArray(MethodCallExpression node, SQLVisitor visitor)
    {
        ResolvedModel source = visitor.ResolveExpression(node.Arguments[0]);
        string src = source.SQLiteExpression!.ToString();

        if (node.Arguments.Count == 2 && node.Arguments[1] is Expression secondArg)
        {
            Expression stripped = ExpressionHelpers.StripQuotes(secondArg);
            if (stripped is LambdaExpression lambda && ArrayLambdaMethodNames.Contains(node.Method.Name))
            {
                (string predSql, SQLiteParameter[]? predParams) = VisitElementLambda(visitor, lambda);
                SQLiteParameter[]? combined = CombineAll(source.SQLiteExpression, predParams == null ? null : SQLiteExpression.Leaf(typeof(object), -1, "", predParams).WithJsonSource());
                return node.Method.Name switch
                {
                    nameof(Array.Exists) => SQLiteExpression.Leaf(typeof(bool), visitor.Counters.NextIdentifier(),
                        $"EXISTS (SELECT 1 FROM json_each({src}) WHERE {predSql})", combined)
                        .WithJsonSource(),
                    nameof(Array.Find) => SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(),
                        $"(SELECT value FROM json_each({src}) WHERE {predSql} ORDER BY key LIMIT 1)", combined)
                        .WithJsonSource(),
                    nameof(Array.FindAll) => SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(),
                        $"(SELECT json_group_array(value) FROM json_each({src}) WHERE {predSql})", combined)
                        .WithJsonSource(),
                    nameof(Array.FindIndex) => SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(),
                        $"COALESCE((SELECT key FROM json_each({src}) WHERE {predSql} ORDER BY key LIMIT 1), -1)", combined)
                        .WithJsonSource(),
                    nameof(Array.FindLast) => SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(),
                        $"(SELECT value FROM json_each({src}) WHERE {predSql} ORDER BY key DESC LIMIT 1)", combined)
                        .WithJsonSource(),
                    nameof(Array.FindLastIndex) => SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(),
                        $"COALESCE((SELECT key FROM json_each({src}) WHERE {predSql} ORDER BY key DESC LIMIT 1), -1)", combined)
                        .WithJsonSource(),
                    nameof(Array.TrueForAll) => SQLiteExpression.Leaf(typeof(bool), visitor.Counters.NextIdentifier(),
                        $"NOT EXISTS (SELECT 1 FROM json_each({src}) WHERE NOT ({predSql}))", combined)
                        .WithJsonSource(),
                    _ => SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(),
                        $"(SELECT json_group_array({predSql}) FROM json_each({src}))", combined)
                        .WithJsonSource(),
                };
            }

            ResolvedModel arg = visitor.ResolveExpression(secondArg);
            string argSql = arg.SQLiteExpression!.ToString();
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(
                source.SQLiteExpression,
                arg.SQLiteExpression!);

            return node.Method.Name switch
            {
                nameof(Array.IndexOf) => SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(),
                    $"COALESCE((SELECT key FROM json_each({src}) WHERE value = {argSql} LIMIT 1), -1)", parameters)
                    .WithJsonSource(),
                nameof(Array.LastIndexOf) => SQLiteExpression.Leaf(typeof(int), visitor.Counters.NextIdentifier(),
                    $"COALESCE((SELECT key FROM json_each({src}) WHERE value = {argSql} ORDER BY key DESC LIMIT 1), -1)", parameters)
                    .WithJsonSource(),
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
        if (TypeHelpers.IsSimple(elementType, visitor.Database.Options))
        {
            SQLiteExpression valueExpr = SQLiteExpression.Leaf(elementType, -1, "value", (SQLiteParameter[]?)null);
            bindings = new Dictionary<string, Expression> { [""] = valueExpr };
        }
        else
        {
            bindings = new Dictionary<string, Expression>();
            RegisterProperties(elementType, "value", bindings);
        }

        visitor.MethodArguments[param] = bindings;
        try
        {
            Expression result = visitor.Visit(lambda.Body);
            if (result is SQLiteExpression sqlExpr)
            {
                return (sqlExpr.ToString(), sqlExpr.Parameters);
            }

            throw new NotSupportedException($"Cannot translate lambda body: {lambda.Body}");
        }
        finally
        {
            visitor.MethodArguments.Remove(param);
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Element type properties are part of the client assembly.")]
    private static void RegisterProperties(Type type, string valueSql, Dictionary<string, Expression> dict)
    {
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            string sql = $"json_extract({valueSql}, '$.{prop.Name}')";
            dict[prop.Name] = SQLiteExpression.Leaf(prop.PropertyType, -1, sql, null).WithJsonSource();
        }
    }

    private static bool IsJsonCollection(Type type, SQLiteOptions options)
    {
        return options.TypeConverters.ContainsKey(type)
               && TypeHelpers.GetEnumerableElementType(type) != null;
    }

    private static bool IsJsonCollectionExpression(SQLiteExpression expr, SQLiteOptions options)
    {
        return expr.IsJsonSource || IsJsonCollection(expr.Type, options);
    }

    private static SQLiteParameter[]? CombineAll(params SQLiteExpression?[] exprs)
    {
        SQLiteExpression[] non = exprs.Where(e => e != null).Cast<SQLiteExpression>().ToArray();
        return ParameterHelpers.CombineParameters(non);
    }
}
