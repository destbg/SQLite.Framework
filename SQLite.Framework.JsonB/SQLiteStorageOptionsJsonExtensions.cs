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
}