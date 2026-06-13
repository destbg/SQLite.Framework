using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Resolves a foreign key declaration to a concrete table and column list using reflection on
/// the target type. Stateless. Safe to call during table mapping construction.
/// </summary>
internal static class ForeignKeyResolver
{
    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Foreign key target lookup by name; prefer [ReferencesTable(typeof(T))] under AOT.")]
    [UnconditionalSuppressMessage("AOT", "IL2063", Justification = "Same as above.")]
    [UnconditionalSuppressMessage("AOT", "IL2073", Justification = "Same as above.")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public static Type ResolveTargetTypeByName(Type owner, string sourceColumn, string targetName)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrEmpty(targetName);

        Type[] candidates = owner.Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                && (t.Name == targetName
                    || t.GetCustomAttribute<TableAttribute>()?.Name == targetName))
            .ToArray();

        if (candidates.Length == 0)
        {
            throw new InvalidOperationException(
                $"Foreign key on \"{owner.Name}\".\"{sourceColumn}\" declared via [ForeignKey(\"{targetName}\")] " +
                $"but assembly '{owner.Assembly.GetName().Name}' contains no class named \"{targetName}\" (or with [Table(\"{targetName}\")]). " +
                "Use [ReferencesTable(typeof(TargetType))] for cross-assembly or refactor-safe targeting.");
        }
        if (candidates.Length > 1)
        {
            throw new InvalidOperationException(
                $"Foreign key on \"{owner.Name}\".\"{sourceColumn}\" declared via [ForeignKey(\"{targetName}\")] " +
                $"matched {candidates.Length} types in '{owner.Assembly.GetName().Name}'. " +
                "Use [ReferencesTable(typeof(TargetType))] to disambiguate.");
        }
        return candidates[0];
    }

    public static (string TargetTable, string TargetColumn) ResolveSingleTarget(string sourceTable, string sourceColumn, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type targetType, string? targetColumnName)
    {
        string targetTable = GetTableName(targetType);
        string[] targetColumns = ResolveTargetColumns(sourceTable, sourceColumn, targetType, targetColumnName != null ? [targetColumnName] : null);
        return (targetTable, targetColumns[0]);
    }

    public static (string TargetTable, string[] TargetColumns) ResolveTargets(string sourceTable, IReadOnlyList<string> sourceColumns, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type targetType, IReadOnlyList<string>? targetColumnNames)
    {
        string targetTable = GetTableName(targetType);
        string[] targetColumns = ResolveTargetColumns(sourceTable, sourceColumns[0], targetType, targetColumnNames);
        if (targetColumns.Length != sourceColumns.Count)
        {
            throw new InvalidOperationException(
                $"Foreign key on \"{sourceTable}\" references \"{targetTable}\" with {sourceColumns.Count} local column(s) " +
                $"but {targetColumns.Length} target column(s). Local and target column counts must match.");
        }
        return (targetTable, targetColumns);
    }

    public static (string TargetTable, string[] TargetColumns) ResolveTargets(string sourceTable, IReadOnlyList<string> sourceColumns, TableMapping targetMapping, IReadOnlyList<string>? targetPropertyNames)
    {
        string targetTable = targetMapping.TableName;
        string[] targetColumns;

        if (targetPropertyNames == null)
        {
            TableColumn[] pks = targetMapping.Columns.Where(c => c.IsPrimaryKey).ToArray();
            if (pks.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Foreign key on \"{sourceTable}\"(\"{sourceColumns[0]}\") targets \"{targetTable}\" but it has no primary key. " +
                    "Add [Key] to the target's primary key, configure HasKey in the model, or specify the target column explicitly.");
            }
            if (pks.Length > 1 && pks.Length != sourceColumns.Count)
            {
                throw new InvalidOperationException(
                    $"Foreign key on \"{sourceTable}\"(\"{sourceColumns[0]}\") targets \"{targetTable}\" which has a composite primary key. " +
                    "Use the fluent ForeignKey overload that takes both local and target column selectors.");
            }
            targetColumns = pks.Select(c => c.Name).ToArray();
        }
        else
        {
            targetColumns = new string[targetPropertyNames.Count];
            for (int i = 0; i < targetPropertyNames.Count; i++)
            {
                string name = targetPropertyNames[i];
                TableColumn? column = targetMapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == name)
                    ?? throw new InvalidOperationException($"Foreign key on \"{sourceTable}\" targets \"{targetTable}.{name}\" but it has no mapped property named \"{name}\".");
                targetColumns[i] = column.Name;
            }
        }

        if (targetColumns.Length != sourceColumns.Count)
        {
            throw new InvalidOperationException(
                $"Foreign key on \"{sourceTable}\" references \"{targetTable}\" with {sourceColumns.Count} local column(s) " +
                $"but {targetColumns.Length} target column(s). Local and target column counts must match.");
        }

        return (targetTable, targetColumns);
    }

    public static void ValidateSetNullCompatibility(string sourceTable, IReadOnlyList<string> sourceColumns, IReadOnlyList<bool> sourceNullability, SQLiteForeignKeyAction onDelete, SQLiteForeignKeyAction onUpdate)
    {
        if (onDelete != SQLiteForeignKeyAction.SetNull && onUpdate != SQLiteForeignKeyAction.SetNull)
        {
            return;
        }
        for (int i = 0; i < sourceColumns.Count; i++)
        {
            if (!sourceNullability[i])
            {
                string clause = onDelete == SQLiteForeignKeyAction.SetNull ? "ON DELETE SET NULL" : "ON UPDATE SET NULL";
                throw new InvalidOperationException(
                    $"Foreign key on \"{sourceTable}\"(\"{sourceColumns[i]}\") declares {clause} but \"{sourceColumns[i]}\" is not nullable. " +
                    "Make the property nullable (e.g. int?) or pick a different action.");
            }
        }
    }

    private static string GetTableName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        return type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;
    }

    private static string[] ResolveTargetColumns(string sourceTable, string sourceColumn, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type targetType, IReadOnlyList<string>? targetColumnNames)
    {
        PropertyInfo[] targetProperties = targetType.GetProperties();
        if (targetColumnNames == null)
        {
            PropertyInfo[] pks = targetProperties
                .Where(p => p.GetCustomAttribute<KeyAttribute>() != null
                            && p.GetCustomAttribute<NotMappedAttribute>() == null)
                .ToArray();
            if (pks.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Foreign key on \"{sourceTable}\"(\"{sourceColumn}\") targets \"{targetType.Name}\" but \"{targetType.Name}\" has no [Key] property. " +
                    "Either add [Key] to the target's primary key, or specify the target column explicitly.");
            }
            if (pks.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Foreign key on \"{sourceTable}\"(\"{sourceColumn}\") targets \"{targetType.Name}\" which has a composite primary key. " +
                    "Use the fluent ForeignKey overload that takes both local and target column selectors.");
            }
            return [GetColumnName(pks[0])];
        }

        string[] resolved = new string[targetColumnNames.Count];
        for (int i = 0; i < targetColumnNames.Count; i++)
        {
            string name = targetColumnNames[i];
            PropertyInfo? prop = targetProperties.FirstOrDefault(p => p.Name == name);
            if (prop == null || prop.GetCustomAttribute<NotMappedAttribute>() != null)
            {
                throw new InvalidOperationException(
                    $"Foreign key on \"{sourceTable}\" targets \"{targetType.Name}.{name}\" but \"{targetType.Name}\" has no public mapped property named \"{name}\".");
            }
            resolved[i] = GetColumnName(prop);
        }
        return resolved;
    }

    private static string GetColumnName(PropertyInfo property)
    {
        return property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name;
    }
}
