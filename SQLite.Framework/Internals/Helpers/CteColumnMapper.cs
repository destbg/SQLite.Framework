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

    public static Dictionary<string, Expression> BuildOuterColumns(CteInfo info, Type elementType, string alias, SQLiteOptions options, SQLiteCounters counters)
    {
        if (info.BodyColumns != null)
        {
            return BuildBodyMappedColumns(info.BodyColumns, info.BodySelects!, info.ColumnNames, alias, options, counters);
        }

        Dictionary<string, Expression> columns = BuildColumns(elementType, alias, options, counters);
        if (info.EmittedColumns != null && !TypeHelpers.IsSimple(elementType, options))
        {
            HashSet<string> emitted = new(info.EmittedColumns, StringComparer.OrdinalIgnoreCase);
            if (columns.Keys.Any(emitted.Contains))
            {
                List<string> missing = columns.Keys.Where(key => !emitted.Contains(key)).ToList();
                foreach (string key in missing)
                {
                    columns.Remove(key);
                }
            }
        }

        return columns;
    }

    public static Dictionary<string, Expression> BuildSelfColumns(CteSelfReference reference, string alias, SQLiteOptions options, SQLiteCounters counters, SQLVisitor visitor)
    {
        Dictionary<string, Expression> columns = reference.BodyColumns != null
            ? BuildBodyMappedColumns(reference.BodyColumns, reference.BodySelects!, reference.ColumnNames, alias, options, counters)
            : BuildColumns(reference.ElementType, alias, options, counters);

        ApplyDayOfWeekColumns(columns, reference.DayOfWeekColumns);
        ApplyJsonSourceColumns(columns, reference.JsonSourceColumns);
        if (reference.ConstructedPaths != null)
        {
            visitor.ConstructedProjectionPaths[columns] = [.. reference.ConstructedPaths];
        }

        return columns;
    }

    public static string[]? ScalarColumnNames(Type elementType, SQLiteOptions options)
    {
        return TypeHelpers.IsSimple(elementType, options) ? [Constants.CteScalarColumn] : null;
    }

    public static string[]? EmittedColumnNames(string[]? columnNames, IReadOnlyList<SQLiteExpression> selects)
    {
        if (columnNames != null)
        {
            return columnNames;
        }

        if (selects.Count == 0)
        {
            return null;
        }

        string[] names = new string[selects.Count];
        for (int i = 0; i < selects.Count; i++)
        {
            if (string.IsNullOrEmpty(selects[i].IdentifierText))
            {
                return null;
            }

            names[i] = selects[i].IdentifierText;
        }

        return names;
    }

    public static string[] BodyColumnNamesWithPlaceholders(Dictionary<string, Expression> bodyColumns, IReadOnlyList<SQLiteExpression> selects)
    {
        string[] names = new string[selects.Count];
        HashSet<string> used = new(StringComparer.Ordinal);
        for (int i = 0; i < selects.Count; i++)
        {
            names[i] = MatchBodyColumnKey(bodyColumns, selects[i], used)
                ?? $"{Constants.CteBodyColumnPrefix}{i}";
        }

        return names;
    }

    public static HashSet<string>? DayOfWeekColumns(Dictionary<string, Expression> bodyColumns, bool scalarElement)
    {
        HashSet<string>? flagged = null;
        foreach (KeyValuePair<string, Expression> column in bodyColumns)
        {
            if (column.Value is SQLiteExpression { IsDayOfWeekInteger: true })
            {
                flagged ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                flagged.Add(scalarElement ? string.Empty : column.Key);
            }
        }

        return flagged;
    }

    public static HashSet<string>? JsonSourceColumns(Dictionary<string, Expression> bodyColumns, bool scalarElement)
    {
        HashSet<string>? flagged = null;
        foreach (KeyValuePair<string, Expression> column in bodyColumns)
        {
            if (column.Value is SQLiteExpression { IsJsonSource: true })
            {
                flagged ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                flagged.Add(scalarElement ? string.Empty : column.Key);
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

    public static Dictionary<string, Expression>? BodyConstructedNodes(SQLVisitor bodyVisitor)
    {
        if (!bodyVisitor.ConstructedProjectionNodes.TryGetValue(bodyVisitor.TableColumns, out Dictionary<string, Expression>? nodes))
        {
            return null;
        }

        Dictionary<string, Expression> carried = [];
        foreach (KeyValuePair<string, Expression> node in nodes)
        {
            if (node.Key.Length > 0)
            {
                carried[node.Key] = node.Value;
            }
        }

        return carried.Count > 0 ? carried : null;
    }

    public static void ApplyBodyTraits(Dictionary<string, Expression> columns, CteInfo info, SQLVisitor visitor, string alias)
    {
        ApplyDayOfWeekColumns(columns, info.DayOfWeekColumns);
        ApplyJsonSourceColumns(columns, info.JsonSourceColumns);

        if (info.ConstructedPaths != null)
        {
            visitor.ConstructedProjectionPaths[columns] = [.. info.ConstructedPaths];
        }

        if (info.ConstructedNodes != null && info.BodySelects != null)
        {
            CteClientColumnRewriter rewriter = new(info.BodySelects, info.ColumnNames, alias, visitor.Counters);
            foreach (KeyValuePair<string, Expression> node in info.ConstructedNodes)
            {
                rewriter.Seed(node.Value);
            }

            Dictionary<string, Expression> rewrittenNodes = [];
            foreach (KeyValuePair<string, Expression> node in info.ConstructedNodes)
            {
                rewrittenNodes[node.Key] = rewriter.Rewrite(node.Value);
            }

            visitor.ConstructedProjectionNodes[columns] = rewrittenNodes;
        }

        if (info.OptionalRow)
        {
            visitor.OptionalRowColumns.Add(columns);
        }

        if (info.OptionalRowPaths != null)
        {
            visitor.OptionalRowPaths[columns] = [.. info.OptionalRowPaths];
        }
    }

    public static void ApplyDayOfWeekColumns(Dictionary<string, Expression> columns, HashSet<string>? dayOfWeekColumns)
    {
        if (dayOfWeekColumns == null)
        {
            return;
        }

        foreach (KeyValuePair<string, Expression> column in columns)
        {
            if (dayOfWeekColumns.Contains(column.Key) && column.Value is SQLiteExpression sql)
            {
                sql.WithDayOfWeekInteger();
            }
        }
    }

    public static void ApplyJsonSourceColumns(Dictionary<string, Expression> columns, HashSet<string>? jsonSourceColumns)
    {
        if (jsonSourceColumns == null)
        {
            return;
        }

        foreach (KeyValuePair<string, Expression> column in columns)
        {
            if (jsonSourceColumns.Contains(column.Key) && column.Value is SQLiteExpression sql)
            {
                sql.WithJsonSource();
            }
        }
    }

    public static string[]? BodyColumnNames(Dictionary<string, Expression> bodyColumns, IReadOnlyList<SQLiteExpression> selects)
    {
        if (CountLeafColumns(bodyColumns) != selects.Count)
        {
            return null;
        }

        if (!TryOrderKeysBySelects(bodyColumns, selects, out string[] ordered))
        {
            return BodyColumnNamesWithPlaceholders(bodyColumns, selects);
        }

        for (int i = 0; i < selects.Count; i++)
        {
            if (selects[i].IdentifierText != ordered[i])
            {
                return ordered;
            }
        }

        return null;
    }

    public static bool BodyColumnOrderIsAmbiguous(Dictionary<string, Expression> bodyColumns, IReadOnlyList<SQLiteExpression> selects)
    {
        return CountLeafColumns(bodyColumns) == selects.Count
            && !TryOrderKeysBySelects(bodyColumns, selects, out _);
    }

    public static string[]? DeclaredColumnNames(Type elementType, Dictionary<string, Expression> bodyColumns, IReadOnlyList<SQLiteExpression> selects, SQLiteOptions options)
    {
        string[]? names = ScalarColumnNames(elementType, options) ?? BodyColumnNames(bodyColumns, selects);
        return names != null && names.Length != selects.Count
            ? BodyColumnNamesWithPlaceholders(bodyColumns, selects)
            : names;
    }

    public static Dictionary<string, Expression> BuildBodyMappedColumns(Dictionary<string, Expression> bodyColumns, IReadOnlyList<SQLiteExpression> selects, string[]? columnNames, string alias, SQLiteOptions options, SQLiteCounters counters)
    {
        HashSet<string> declared = new(StringComparer.Ordinal);
        for (int i = 0; i < selects.Count; i++)
        {
            declared.Add(columnNames != null ? columnNames[i] : selects[i].IdentifierText);
        }

        CteClientColumnRewriter rewriter = new(selects, columnNames, alias, counters);
        foreach (KeyValuePair<string, Expression> member in bodyColumns)
        {
            rewriter.Seed(member.Value);
        }

        Dictionary<string, Expression> columns = [];
        foreach (KeyValuePair<string, Expression> member in bodyColumns)
        {
            if (declared.Contains(member.Key))
            {
                columns[member.Key] = BuildDeclaredBodyLeaf(member.Value, member.Key, alias, counters);
                continue;
            }

            if (member.Value is SQLiteExpression sql)
            {
                columns[member.Key] = rewriter.Rewrite(sql);
                continue;
            }

            Dictionary<string, Expression> expansion = [];
            AddColumns(expansion, member.Value.Type, member.Key, alias, options, counters);
            bool covered = expansion.Count > 0
                && expansion.Keys.All(key => bodyColumns.TryGetValue(key, out Expression? value) && value is SQLiteExpression);
            if (covered)
            {
                continue;
            }

            List<string> declaredParts = expansion.Keys.Where(declared.Contains).ToList();
            if (declaredParts.Count > 0)
            {
                foreach (string part in declaredParts)
                {
                    columns[part] = expansion[part];
                }

                continue;
            }

            columns[member.Key] = rewriter.Rewrite(member.Value);
        }

        return columns;
    }

    public static SQLiteExpression BuildDeclaredBodyLeaf(Expression value, string key, string alias, SQLiteCounters counters)
    {
        SQLiteExpression leaf = SQLiteExpression.Leaf(value.Type, counters.NextIdentifier(), $"{alias}.{IdentifierGuard.Quote(key)}");
        if (value is SQLiteExpression { IsJsonSource: true })
        {
            leaf.WithJsonSource();
        }

        return leaf;
    }

    public static bool HasClientBodyMember(Dictionary<string, Expression> bodyColumns)
    {
        foreach (KeyValuePair<string, Expression> member in bodyColumns)
        {
            if (member.Value is not SQLiteExpression)
            {
                return true;
            }
        }

        return false;
    }

    private static string? MatchBodyColumnKey(Dictionary<string, Expression> bodyColumns, SQLiteExpression select, HashSet<string> used)
    {
        SQLiteExpression inner = Unaliased(select);
        foreach (KeyValuePair<string, Expression> column in bodyColumns)
        {
            if (column.Value is not SQLiteExpression columnSql)
            {
                continue;
            }

            if ((ReferenceEquals(columnSql, select) || ReferenceEquals(Unaliased(columnSql), inner))
                && used.Add(column.Key))
            {
                return column.Key;
            }
        }

        if (!string.IsNullOrEmpty(select.IdentifierText)
            && bodyColumns.TryGetValue(select.IdentifierText, out Expression? named)
            && named is SQLiteExpression
            && used.Add(select.IdentifierText))
        {
            return select.IdentifierText;
        }

        return null;
    }

    private static SQLiteExpression Unaliased(SQLiteExpression expression)
    {
        return expression is AliasSqlExpression alias ? alias.Inner : expression;
    }

    private static int CountLeafColumns(Dictionary<string, Expression> bodyColumns)
    {
        int count = 0;
        foreach (KeyValuePair<string, Expression> column in bodyColumns)
        {
            if (column.Value is SQLiteExpression)
            {
                count++;
            }
        }

        return count;
    }

    private static bool TryOrderKeysBySelects(Dictionary<string, Expression> bodyColumns, IReadOnlyList<SQLiteExpression> selects, out string[] ordered)
    {
        ordered = new string[selects.Count];
        HashSet<string> used = new(StringComparer.Ordinal);
        for (int i = 0; i < selects.Count; i++)
        {
            string? match = MatchBodyColumnKey(bodyColumns, selects[i], used);
            if (match == null)
            {
                return false;
            }

            ordered[i] = match;
        }

        return true;
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
