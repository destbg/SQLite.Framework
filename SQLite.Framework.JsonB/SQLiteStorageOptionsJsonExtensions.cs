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
}