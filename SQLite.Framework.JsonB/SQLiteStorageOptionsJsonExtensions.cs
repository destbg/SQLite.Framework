using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SQLite.Framework.JsonB;

/// <summary>
/// Extension methods for registering JSON and JSONB function support with <see cref="SQLiteStorageOptions" />.
/// </summary>
public static class SQLiteStorageOptionsJsonExtensions
{
    /// <summary>
    /// Registers method translators for all <see cref="SQLiteJsonFunctions" /> methods so they can be used
    /// inside LINQ queries.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(SQLiteJsonFunctions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(Enumerable))]
    public static SQLiteStorageOptions AddJson(this SQLiteStorageOptions options)
    {
        Dictionary<MethodInfo, SQLiteMethodTranslator> t = options.MethodTranslators;

        t[Method(nameof(SQLiteJsonFunctions.Extract))] =
            (_, args) => $"json_extract({args[0]}, {args[1]})";

        t[Method(nameof(SQLiteJsonFunctions.Set))] =
            (_, args) => $"json_set({args[0]}, {args[1]}, {args[2]})";

        t[Method(nameof(SQLiteJsonFunctions.Insert))] =
            (_, args) => $"json_insert({args[0]}, {args[1]}, {args[2]})";

        t[Method(nameof(SQLiteJsonFunctions.Replace))] =
            (_, args) => $"json_replace({args[0]}, {args[1]}, {args[2]})";

        t[Method(nameof(SQLiteJsonFunctions.Remove))] =
            (_, args) => $"json_remove({args[0]}, {args[1]})";

        t[Method(nameof(SQLiteJsonFunctions.Type))] =
            (_, args) => $"json_type({args[0]}, {args[1]})";

        t[Method(nameof(SQLiteJsonFunctions.Valid))] =
            (_, args) => $"json_valid({args[0]})";

        t[Method(nameof(SQLiteJsonFunctions.Patch))] =
            (_, args) => $"json_patch({args[0]}, {args[1]})";

        t[MethodWithArgs(nameof(SQLiteJsonFunctions.ArrayLength), typeof(string))] =
            (_, args) => $"json_array_length({args[0]})";

        t[MethodWithArgs(nameof(SQLiteJsonFunctions.ArrayLength), typeof(string), typeof(string))] =
            (_, args) => $"json_array_length({args[0]}, {args[1]})";

        t[Method(nameof(SQLiteJsonFunctions.Minify))] =
            (_, args) => $"json({args[0]})";

        t[Method(nameof(SQLiteJsonFunctions.ToJsonb))] =
            (_, args) => $"jsonb({args[0]})";

        t[Method(nameof(SQLiteJsonFunctions.ExtractJsonb))] =
            (_, args) => $"jsonb_extract({args[0]}, {args[1]})";

        t[typeof(List<>).GetMethod(nameof(List<>.Contains))!] =
            (instance, args) => $"EXISTS (SELECT 1 FROM json_each({instance}) WHERE value = {args[0]})";

        t[EnumerableMethod(nameof(Enumerable.Any), 1)] =
            (_, args) => $"json_array_length({args[0]}) > 0";

        t[EnumerableMethod(nameof(Enumerable.Count), 1)] =
            (_, args) => $"json_array_length({args[0]})";

        t[EnumerableMethod(nameof(Enumerable.First), 1)] =
            (_, args) => $"json_extract({args[0]}, '$[0]')";

        t[EnumerableMethod(nameof(Enumerable.FirstOrDefault), 1)] =
            (_, args) => $"json_extract({args[0]}, '$[0]')";

        t[EnumerableMethod(nameof(Enumerable.Last), 1)] =
            (_, args) => $"CASE WHEN json_array_length({args[0]}) > 0 THEN json_extract({args[0]}, '$[' || (json_array_length({args[0]}) - 1) || ']') ELSE NULL END";

        t[EnumerableMethod(nameof(Enumerable.LastOrDefault), 1)] =
            (_, args) => $"CASE WHEN json_array_length({args[0]}) > 0 THEN json_extract({args[0]}, '$[' || (json_array_length({args[0]}) - 1) || ']') ELSE NULL END";

        t[EnumerableMethod(nameof(Enumerable.ElementAt), 2)] =
            (_, args) => $"json_extract({args[0]}, '$[' || {args[1]} || ']')";

        t[EnumerableMethod(nameof(Enumerable.Min), 1)] =
            (_, args) => $"(SELECT MIN(value) FROM json_each({args[0]}))";

        t[EnumerableMethod(nameof(Enumerable.Max), 1)] =
            (_, args) => $"(SELECT MAX(value) FROM json_each({args[0]}))";

        foreach (MethodInfo m in typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetParameters().Length == 1 && !m.IsGenericMethod))
        {
            if (m.Name == nameof(Enumerable.Sum))
            {
                t[m] = (_, args) => $"(SELECT SUM(value) FROM json_each({args[0]}))";
            }
            else if (m.Name == nameof(Enumerable.Average))
            {
                t[m] = (_, args) => $"(SELECT AVG(value) FROM json_each({args[0]}))";
            }
        }

        t[EnumerableMethod(nameof(Enumerable.Single), 1)] =
            (_, args) => $"CASE WHEN json_array_length({args[0]}) = 1 THEN json_extract({args[0]}, '$[0]') ELSE NULL END";

        t[EnumerableMethod(nameof(Enumerable.SingleOrDefault), 1)] =
            (_, args) => $"CASE WHEN json_array_length({args[0]}) = 1 THEN json_extract({args[0]}, '$[0]') ELSE NULL END";

        Type listOpenType = typeof(List<>);
        Type listT = listOpenType.GetGenericArguments()[0];
        t[listOpenType.GetMethod(nameof(List<>.IndexOf), [listT])!] =
            (instance, args) => $"COALESCE((SELECT key FROM json_each({instance}) WHERE value = {args[0]} LIMIT 1), -1)";

        t[ListMethod(nameof(List<>.LastIndexOf), 1)] =
            (instance, args) => $"COALESCE((SELECT key FROM json_each({instance}) WHERE value = {args[0]} ORDER BY key DESC LIMIT 1), -1)";

        t[ListMethod(nameof(List<>.GetRange), 2)] =
            (instance, args) => $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({instance}) LIMIT {args[1]} OFFSET {args[0]}))";

#if NET9_0_OR_GREATER
        t[ListMethod(nameof(List<>.Slice), 2)] =
            (instance, args) => $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({instance}) LIMIT {args[1]} OFFSET {args[0]}))";
#endif

        t[ArrayMethod(nameof(Array.IndexOf), 2)] =
            (_, args) => $"COALESCE((SELECT key FROM json_each({args[0]}) WHERE value = {args[1]} LIMIT 1), -1)";

        t[ArrayMethod(nameof(Array.LastIndexOf), 2)] =
            (_, args) => $"COALESCE((SELECT key FROM json_each({args[0]}) WHERE value = {args[1]} ORDER BY key DESC LIMIT 1), -1)";

        t[EnumerableMethod(nameof(Enumerable.Distinct), 1)] =
            (_, args) => $"(SELECT json_group_array(DISTINCT value) FROM json_each({args[0]}))";

        t[EnumerableMethod(nameof(Enumerable.Reverse), 1)] =
            (_, args) => $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({args[0]}) ORDER BY key DESC))";

        t[EnumerableMethod(nameof(Enumerable.Skip), 2)] =
            (_, args) => $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({args[0]}) LIMIT -1 OFFSET {args[1]}))";

        t[EnumerableMethod(nameof(Enumerable.Take), 2)] =
            (_, args) => $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({args[0]}) LIMIT {args[1]}))";

        t[EnumerableMethod(nameof(Enumerable.Concat), 2)] =
            (_, args) => $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({args[0]}) UNION ALL SELECT value FROM json_each({args[1]})))";

        t[EnumerableMethod(nameof(Enumerable.Union), 2)] =
            (_, args) => $"(SELECT json_group_array(value) FROM (SELECT DISTINCT value FROM json_each({args[0]}) UNION SELECT DISTINCT value FROM json_each({args[1]})))";

        t[EnumerableMethod(nameof(Enumerable.Intersect), 2)] =
            (_, args) => $"(SELECT json_group_array(value) FROM json_each({args[0]}) WHERE value IN (SELECT value FROM json_each({args[1]})))";

        t[EnumerableMethod(nameof(Enumerable.Except), 2)] =
            (_, args) => $"(SELECT json_group_array(value) FROM json_each({args[0]}) WHERE value NOT IN (SELECT value FROM json_each({args[1]})))";

        Dictionary<MethodInfo, SQLitePredicateMethodTranslator> p = options.PredicateMethodTranslators;

        p[ListMethod(nameof(List<>.Exists), 1)] =
            (instance, predicate) => $"EXISTS (SELECT 1 FROM json_each({instance}) WHERE {predicate})";

        p[ListMethod(nameof(List<>.Find), 1)] =
            (instance, predicate) => $"(SELECT value FROM json_each({instance}) WHERE {predicate} ORDER BY key LIMIT 1)";

        p[ListMethod(nameof(List<>.FindAll), 1)] =
            (instance, predicate) => $"(SELECT json_group_array(value) FROM json_each({instance}) WHERE {predicate})";

        p[ListMethod(nameof(List<>.FindIndex), 1)] =
            (instance, predicate) => $"COALESCE((SELECT key FROM json_each({instance}) WHERE {predicate} ORDER BY key LIMIT 1), -1)";

        p[ListMethod(nameof(List<>.FindLast), 1)] =
            (instance, predicate) => $"(SELECT value FROM json_each({instance}) WHERE {predicate} ORDER BY key DESC LIMIT 1)";

        p[ListMethod(nameof(List<>.FindLastIndex), 1)] =
            (instance, predicate) => $"COALESCE((SELECT key FROM json_each({instance}) WHERE {predicate} ORDER BY key DESC LIMIT 1), -1)";

        p[ListMethod(nameof(List<>.TrueForAll), 1)] =
            (instance, predicate) => $"NOT EXISTS (SELECT 1 FROM json_each({instance}) WHERE NOT ({predicate}))";

        p[ArrayPredicateMethod(nameof(Array.Exists))] =
            (instance, predicate) => $"EXISTS (SELECT 1 FROM json_each({instance}) WHERE {predicate})";

        p[ArrayPredicateMethod(nameof(Array.Find))] =
            (instance, predicate) => $"(SELECT value FROM json_each({instance}) WHERE {predicate} ORDER BY key LIMIT 1)";

        p[ArrayPredicateMethod(nameof(Array.FindAll))] =
            (instance, predicate) => $"(SELECT json_group_array(value) FROM json_each({instance}) WHERE {predicate})";

        p[ArrayPredicateMethod(nameof(Array.FindIndex))] =
            (instance, predicate) => $"COALESCE((SELECT key FROM json_each({instance}) WHERE {predicate} ORDER BY key LIMIT 1), -1)";

        p[ArrayPredicateMethod(nameof(Array.FindLast))] =
            (instance, predicate) => $"(SELECT value FROM json_each({instance}) WHERE {predicate} ORDER BY key DESC LIMIT 1)";

        p[ArrayPredicateMethod(nameof(Array.FindLastIndex))] =
            (instance, predicate) => $"COALESCE((SELECT key FROM json_each({instance}) WHERE {predicate} ORDER BY key DESC LIMIT 1), -1)";

        p[ArrayPredicateMethod(nameof(Array.TrueForAll))] =
            (instance, predicate) => $"NOT EXISTS (SELECT 1 FROM json_each({instance}) WHERE NOT ({predicate}))";

        p[ArrayPredicateMethod(nameof(Array.ConvertAll))] =
            (instance, projection) => $"(SELECT json_group_array({projection}) FROM json_each({instance}))";

        p[EnumerableSelectorMethod(nameof(Enumerable.Min))] =
            (instance, selector) => $"(SELECT MIN({selector}) FROM json_each({instance}))";

        p[EnumerableSelectorMethod(nameof(Enumerable.Max))] =
            (instance, selector) => $"(SELECT MAX({selector}) FROM json_each({instance}))";

        foreach (MethodInfo m in typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetParameters().Length == 2 && m.IsGenericMethod))
        {
            if (m.Name == nameof(Enumerable.Sum))
            {
                p[m.GetGenericMethodDefinition()] = (instance, selector) => $"(SELECT SUM({selector}) FROM json_each({instance}))";
            }
            else if (m.Name == nameof(Enumerable.Average))
            {
                p[m.GetGenericMethodDefinition()] = (instance, selector) => $"(SELECT AVG({selector}) FROM json_each({instance}))";
            }
        }

        p[EnumerableMethod(nameof(Enumerable.Any), 2)] =
            (instance, predicate) => $"EXISTS (SELECT 1 FROM json_each({instance}) WHERE {predicate})";

        p[EnumerableMethod(nameof(Enumerable.All), 2)] =
            (instance, predicate) => $"NOT EXISTS (SELECT 1 FROM json_each({instance}) WHERE NOT ({predicate}))";

        p[EnumerableMethod(nameof(Enumerable.Count), 2)] =
            (instance, predicate) => $"(SELECT COUNT(*) FROM json_each({instance}) WHERE {predicate})";

        p[EnumerableMethod(nameof(Enumerable.First), 2)] =
            (instance, predicate) => $"(SELECT value FROM json_each({instance}) WHERE {predicate} ORDER BY key LIMIT 1)";

        p[EnumerableMethod(nameof(Enumerable.FirstOrDefault), 2)] =
            (instance, predicate) => $"(SELECT value FROM json_each({instance}) WHERE {predicate} ORDER BY key LIMIT 1)";

        p[EnumerableMethod(nameof(Enumerable.Last), 2)] =
            (instance, predicate) => $"(SELECT value FROM json_each({instance}) WHERE {predicate} ORDER BY key DESC LIMIT 1)";

        p[EnumerableMethod(nameof(Enumerable.LastOrDefault), 2)] =
            (instance, predicate) => $"(SELECT value FROM json_each({instance}) WHERE {predicate} ORDER BY key DESC LIMIT 1)";

        p[EnumerableMethod(nameof(Enumerable.Single), 2)] =
            (instance, predicate) => $"CASE WHEN (SELECT COUNT(*) FROM json_each({instance}) WHERE {predicate}) = 1 THEN (SELECT value FROM json_each({instance}) WHERE {predicate} LIMIT 1) ELSE NULL END";

        p[EnumerableMethod(nameof(Enumerable.SingleOrDefault), 2)] =
            (instance, predicate) => $"CASE WHEN (SELECT COUNT(*) FROM json_each({instance}) WHERE {predicate}) = 1 THEN (SELECT value FROM json_each({instance}) WHERE {predicate} LIMIT 1) ELSE NULL END";

        p[EnumerableMethod(nameof(Enumerable.Where), 2)] =
            (instance, predicate) => $"(SELECT json_group_array(value) FROM json_each({instance}) WHERE {predicate})";

        p[EnumerableMethod(nameof(Enumerable.Select), 2)] =
            (instance, predicate) => $"(SELECT json_group_array({predicate}) FROM json_each({instance}))";

        p[EnumerableMethod(nameof(Enumerable.OrderBy), 2)] =
            (instance, predicate) => $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({instance}) ORDER BY {predicate}))";

        p[EnumerableMethod(nameof(Enumerable.OrderByDescending), 2)] =
            (instance, predicate) => $"(SELECT json_group_array(value) FROM (SELECT value FROM json_each({instance}) ORDER BY {predicate} DESC))";

        options.PropertyTranslators.Add((memberName, instanceSql) => $"json_extract({instanceSql}, '$.{memberName}')");

        options.MethodCallInterceptors.Add(JsonCollectionVisitor.TryHandle);

        return options;
    }

    private static MethodInfo Method(string name)
    {
        return typeof(SQLiteJsonFunctions).GetMethod(name)
               ?? throw new InvalidOperationException($"Method '{name}' not found on JsonFunctions.");
    }

    private static MethodInfo MethodWithArgs(string name, params Type[] parameterTypes)
    {
        return typeof(SQLiteJsonFunctions).GetMethod(name, parameterTypes)
               ?? throw new InvalidOperationException($"Method '{name}' with the given parameter types not found on JsonFunctions.");
    }

    private static MethodInfo EnumerableMethod(string name, int paramCount)
    {
        return typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == name && m.GetParameters().Length == paramCount && m.IsGenericMethod)
            .GetGenericMethodDefinition();
    }

    private static MethodInfo ListMethod(string name, int paramCount)
    {
        return typeof(List<>).GetMethods()
            .First(m => m.Name == name && m.GetParameters().Length == paramCount);
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Array generic methods are looked up by name for custom translator registration.")]
    private static MethodInfo ArrayMethod(string name, int paramCount)
    {
        return typeof(Array)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == name && m.GetParameters().Length == paramCount && m.IsGenericMethod)
            .GetGenericMethodDefinition();
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Array generic methods are looked up by name for custom translator registration.")]
    private static MethodInfo ArrayPredicateMethod(string name)
    {
        return typeof(Array)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == name && m.GetParameters().Length == 2 && m.IsGenericMethod)
            .GetGenericMethodDefinition();
    }

    private static MethodInfo EnumerableSelectorMethod(string name)
    {
        return typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == name && m.GetParameters().Length == 2 && m.IsGenericMethod && m.GetGenericArguments().Length == 2)
            .GetGenericMethodDefinition();
    }
}