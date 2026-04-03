using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SQLite.Framework.Window;

/// <summary>
/// Extension methods for registering window function support with <see cref="SQLiteStorageOptions" />.
/// </summary>
public static class SQLiteStorageOptionsWindowExtensions
{
    /// <summary>
    /// Registers method translators for all <see cref="SQLiteWindowFunctions" /> and
    /// <see cref="FrameBoundary" /> methods so they can be used inside LINQ queries.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(SQLiteWindowFunctions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(FrameBoundary))]
    public static SQLiteStorageOptions AddWindow(this SQLiteStorageOptions options)
    {
        Dictionary<MethodInfo, SQLiteMethodTranslator> t = options.MethodTranslators;

        t[GenericMethod(nameof(SQLiteWindowFunctions.Sum))] =
            (_, args) => $"SUM({args[0]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Avg))] =
            (_, args) => $"AVG({args[0]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Min))] =
            (_, args) => $"MIN({args[0]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Max))] =
            (_, args) => $"MAX({args[0]})";

        t[NonGenericMethodNoArgs(nameof(SQLiteWindowFunctions.Count))] =
            (_, _) => "COUNT(*)";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Count))] =
            (_, args) => $"COUNT({args[0]})";

        t[NonGenericMethod(nameof(SQLiteWindowFunctions.RowNumber))] =
            (_, _) => "ROW_NUMBER()";

        t[NonGenericMethod(nameof(SQLiteWindowFunctions.Rank))] =
            (_, _) => "RANK()";

        t[NonGenericMethod(nameof(SQLiteWindowFunctions.DenseRank))] =
            (_, _) => "DENSE_RANK()";

        t[NonGenericMethod(nameof(SQLiteWindowFunctions.PercentRank))] =
            (_, _) => "PERCENT_RANK()";

        t[NonGenericMethod(nameof(SQLiteWindowFunctions.CumeDist))] =
            (_, _) => "CUME_DIST()";

        t[NonGenericMethod(nameof(SQLiteWindowFunctions.NTile))] =
            (_, args) => $"NTILE({args[0]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Lag), 1)] =
            (_, args) => $"LAG({args[0]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Lag), 2)] =
            (_, args) => $"LAG({args[0]}, {args[1]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Lag), 3)] =
            (_, args) => $"LAG({args[0]}, {args[1]}, {args[2]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Lead), 1)] =
            (_, args) => $"LEAD({args[0]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Lead), 2)] =
            (_, args) => $"LEAD({args[0]}, {args[1]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Lead), 3)] =
            (_, args) => $"LEAD({args[0]}, {args[1]}, {args[2]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.FirstValue))] =
            (_, args) => $"FIRST_VALUE({args[0]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.LastValue))] =
            (_, args) => $"LAST_VALUE({args[0]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.NthValue))] =
            (_, args) => $"NTH_VALUE({args[0]}, {args[1]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Over))] =
            (_, args) => $"{args[0]} OVER ()";

        t[GenericMethod(nameof(SQLiteWindowFunctions.PartitionBy))] =
            (_, args) => $"{TrimClose(args[0])} PARTITION BY {args[1]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.ThenPartitionBy))] =
            (_, args) => $"{TrimClose(args[0])}, {args[1]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.OrderBy))] =
            (_, args) => $"{TrimClose(args[0])} ORDER BY {args[1]} ASC)";

        t[GenericMethod(nameof(SQLiteWindowFunctions.OrderByDescending))] =
            (_, args) => $"{TrimClose(args[0])} ORDER BY {args[1]} DESC)";

        t[GenericMethod(nameof(SQLiteWindowFunctions.ThenOrderBy))] =
            (_, args) => $"{TrimClose(args[0])}, {args[1]} ASC)";

        t[GenericMethod(nameof(SQLiteWindowFunctions.ThenOrderByDescending))] =
            (_, args) => $"{TrimClose(args[0])}, {args[1]} DESC)";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Rows))] =
            (_, args) => $"{TrimClose(args[0])} ROWS BETWEEN {args[1]} AND {args[2]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Range))] =
            (_, args) => $"{TrimClose(args[0])} RANGE BETWEEN {args[1]} AND {args[2]})";

        t[GenericMethod(nameof(SQLiteWindowFunctions.Groups))] =
            (_, args) => $"{TrimClose(args[0])} GROUPS BETWEEN {args[1]} AND {args[2]})";

        t[BoundaryMethod(nameof(FrameBoundary.UnboundedPreceding))] =
            (_, _) => "UNBOUNDED PRECEDING";

        t[BoundaryMethod(nameof(FrameBoundary.CurrentRow))] =
            (_, _) => "CURRENT ROW";

        t[BoundaryMethod(nameof(FrameBoundary.UnboundedFollowing))] =
            (_, _) => "UNBOUNDED FOLLOWING";

        t[BoundaryMethod(nameof(FrameBoundary.Preceding))] =
            (_, args) => $"{args[0]} PRECEDING";

        t[BoundaryMethod(nameof(FrameBoundary.Following))] =
            (_, args) => $"{args[0]} FOLLOWING";

        return options;
    }

    private static string TrimClose(string? sql) =>
        sql is null
            ? throw new InvalidOperationException("Expected accumulated window SQL but got null.")
            : sql[..^1];

    private static MethodInfo GenericMethod(string name) =>
        typeof(SQLiteWindowFunctions).GetMethods()
            .Single(m => m.Name == name && m.IsGenericMethod)
            .GetGenericMethodDefinition();

    private static MethodInfo GenericMethod(string name, int paramCount) =>
        typeof(SQLiteWindowFunctions).GetMethods()
            .Single(m => m.Name == name && m.IsGenericMethod && m.GetParameters().Length == paramCount)
            .GetGenericMethodDefinition();

    private static MethodInfo NonGenericMethod(string name) =>
        typeof(SQLiteWindowFunctions).GetMethod(name)
        ?? throw new InvalidOperationException($"Method '{name}' not found on SQLiteWindowFunctions.");

    private static MethodInfo NonGenericMethodNoArgs(string name) =>
        typeof(SQLiteWindowFunctions).GetMethod(name, Type.EmptyTypes)
        ?? throw new InvalidOperationException($"Method '{name}' with no parameters not found on SQLiteWindowFunctions.");

    private static MethodInfo BoundaryMethod(string name) =>
        typeof(FrameBoundary).GetMethod(name)
        ?? throw new InvalidOperationException($"Method '{name}' not found on FrameBoundary.");
}
