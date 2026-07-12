using System.ComponentModel.DataAnnotations.Schema;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Builds the column map for a referenced common table expression. A CTE whose element type is a
/// single value, such as <c>int</c> or <c>string</c>, exposes one column named
/// <see cref="Constants.CteScalarColumn"/>. Any other element type exposes one column per public
/// property.
/// </summary>
internal static class CteColumnMapper
{
    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Entity element types have public properties.")]
    public static Dictionary<string, Expression> BuildColumns(Type elementType, string prefix, SQLiteOptions options, SQLiteCounters counters)
    {
        if (TypeHelpers.IsSimple(elementType, options))
        {
            return new Dictionary<string, Expression>
            {
                [string.Empty] = SQLiteExpression.Leaf(elementType, counters.NextIdentifier(), $"{prefix}.{IdentifierGuard.Quote(Constants.CteScalarColumn)}")
            };
        }

        Dictionary<string, Expression> columns = [];
        AddColumns(columns, elementType, string.Empty, prefix, options, counters);
        return columns;
    }

    public static string[]? ScalarColumnNames(Type elementType, SQLiteOptions options)
    {
        return TypeHelpers.IsSimple(elementType, options) ? [Constants.CteScalarColumn] : null;
    }

    public static HashSet<string>? DayOfWeekColumns(Dictionary<string, Expression> bodyColumns)
    {
        HashSet<string>? flagged = null;
        foreach (KeyValuePair<string, Expression> column in bodyColumns)
        {
            if (column.Value is SQLiteExpression { IsDayOfWeekInteger: true })
            {
                flagged ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                flagged.Add(column.Key);
            }
        }

        return flagged;
    }

    public static HashSet<string>? BodyConstructedPaths(SQLVisitor bodyVisitor)
    {
        return bodyVisitor.ConstructedProjectionPaths.TryGetValue(bodyVisitor.TableColumns, out HashSet<string>? constructed)
            ? constructed
            : null;
    }

    public static void ApplyBodyTraits(Dictionary<string, Expression> columns, CteInfo info, SQLVisitor visitor)
    {
        if (info.DayOfWeekColumns != null)
        {
            foreach (KeyValuePair<string, Expression> column in columns)
            {
                if (info.DayOfWeekColumns.Contains(column.Key))
                {
                    ((SQLiteExpression)column.Value).WithDayOfWeekInteger();
                }
            }
        }

        if (info.ConstructedPaths != null)
        {
            visitor.ConstructedProjectionPaths[columns] = [.. info.ConstructedPaths];
        }
    }

    public static string[]? BodyColumnNames(Dictionary<string, Expression> bodyColumns, IReadOnlyList<SQLiteExpression> selects)
    {
        List<string> leafKeys = [];
        foreach (KeyValuePair<string, Expression> column in bodyColumns)
        {
            if (column.Value is SQLiteExpression)
            {
                leafKeys.Add(column.Key);
            }
        }

        if (leafKeys.Count != selects.Count)
        {
            return null;
        }

        OrderKeysBySelects(leafKeys, bodyColumns, selects);

        for (int i = 0; i < selects.Count; i++)
        {
            if (selects[i].IdentifierText != leafKeys[i])
            {
                return leafKeys.ToArray();
            }
        }

        return null;
    }

    private static void OrderKeysBySelects(List<string> leafKeys, Dictionary<string, Expression> bodyColumns, IReadOnlyList<SQLiteExpression> selects)
    {
        string[] ordered = new string[selects.Count];
        HashSet<string> used = [];
        for (int i = 0; i < selects.Count; i++)
        {
            string? match = null;
            foreach (KeyValuePair<string, Expression> column in bodyColumns)
            {
                if (ReferenceEquals(column.Value, selects[i]) && used.Add(column.Key))
                {
                    match = column.Key;
                    break;
                }
            }

            if (match == null)
            {
                return;
            }

            ordered[i] = match;
        }

        leafKeys.Clear();
        leafKeys.AddRange(ordered);
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Entity element types have public properties.")]
    private static void AddColumns(Dictionary<string, Expression> columns, Type type, string pathPrefix, string tableAlias, SQLiteOptions options, SQLiteCounters counters)
    {
        foreach (PropertyInfo property in type.GetProperties().Where(f => f.GetCustomAttribute<NotMappedAttribute>() == null))
        {
            string path = pathPrefix.Length == 0 ? property.Name : $"{pathPrefix}.{property.Name}";
            if (TypeHelpers.IsSimple(property.PropertyType, options))
            {
                columns[path] = SQLiteExpression.Leaf(property.PropertyType, counters.NextIdentifier(), $"{tableAlias}.{IdentifierGuard.Quote(path)}");
            }
            else
            {
                AddColumns(columns, property.PropertyType, path, tableAlias, options, counters);
            }
        }
    }
}
