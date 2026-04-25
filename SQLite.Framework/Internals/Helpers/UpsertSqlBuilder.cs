using System.Text;
using SQLite.Framework.Internals.Enums;
using SQLite.Framework.Models;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Renders an <c>INSERT INTO ... ON CONFLICT (...) DO ...</c> statement for an
/// <see cref="UpsertConflictTarget{T}" />.
/// </summary>
internal static class UpsertSqlBuilder
{
    public static (TableColumn[] Columns, string Sql) Build<T>(TableMapping table, UpsertConflictTarget<T> target, Func<TableColumn, string, string> wrapParam)
    {
        TableColumn[] insertColumns = table.Columns
            .Where(c => !c.IsPrimaryKey || !c.IsAutoIncrement)
            .ToArray();

        string columnsList = string.Join(", ", insertColumns.Select(c => c.Name));
        string parameters = string.Join(", ", insertColumns.Select((c, i) => wrapParam(c, $"@p{i}")));

        StringBuilder sb = new();
        sb.Append("INSERT INTO \"");
        sb.Append(table.TableName);
        sb.Append("\" (");
        sb.Append(columnsList);
        sb.Append(") VALUES (");
        sb.Append(parameters);
        sb.Append(')');

        sb.Append(" ON CONFLICT (");
        sb.Append(string.Join(", ", target.ConflictColumns.Select(name => ResolveSqlName(table, name))));
        sb.Append(')');

        UpsertAction<T> action = target.ResolvedAction;
        switch (action.Kind)
        {
            case UpsertActionKind.DoNothing:
                sb.Append(" DO NOTHING");
                break;

            case UpsertActionKind.DoUpdateAll:
            {
                IEnumerable<TableColumn> setColumns = insertColumns.Where(c => !target.ConflictColumns.Contains(c.PropertyInfo.Name) && !target.ConflictColumns.Contains(c.Name));
                AppendSet(sb, setColumns);
                break;
            }

            case UpsertActionKind.DoUpdate:
            {
                List<TableColumn> setColumns = [];
                foreach (string propertyName in action.Columns!)
                {
                    TableColumn column = table.Columns.FirstOrDefault(c => c.PropertyInfo.Name == propertyName)
                        ?? throw new InvalidOperationException($"Upsert.DoUpdate references property '{propertyName}' which is not a mapped column on '{table.TableName}'.");
                    setColumns.Add(column);
                }
                AppendSet(sb, setColumns);
                break;
            }

            default:
                throw new InvalidOperationException($"Unknown UpsertActionKind: {action.Kind}");
        }

        return (insertColumns, sb.ToString());
    }

    private static void AppendSet(StringBuilder sb, IEnumerable<TableColumn> setColumns)
    {
        TableColumn[] columns = setColumns.ToArray();
        if (columns.Length == 0)
        {
            sb.Append(" DO NOTHING");
            return;
        }

        sb.Append(" DO UPDATE SET ");
        for (int i = 0; i < columns.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            sb.Append(columns[i].Name);
            sb.Append(" = excluded.");
            sb.Append(columns[i].Name);
        }
    }

    private static string ResolveSqlName(TableMapping table, string propertyOrColumnName)
    {
        TableColumn? byProperty = table.Columns.FirstOrDefault(c => c.PropertyInfo.Name == propertyOrColumnName);
        if (byProperty != null)
        {
            return byProperty.Name;
        }

        TableColumn? byColumn = table.Columns.FirstOrDefault(c => c.Name == propertyOrColumnName);
        if (byColumn != null)
        {
            return byColumn.Name;
        }

        throw new InvalidOperationException($"OnConflict references '{propertyOrColumnName}' which is not a mapped column on '{table.TableName}'.");
    }
}
