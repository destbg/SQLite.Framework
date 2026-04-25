using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Models;

namespace SQLite.Framework;

/// <summary>
/// Represents a base class for SQLite tables.
/// </summary>
public class SQLiteTable : BaseSQLiteTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteTable{T}"/> class.
    /// </summary>
    public SQLiteTable(SQLiteDatabase database, TableMapping table)
        : base(database)
    {
        Table = table;
    }

    /// <summary>
    /// The mapping of the database table to the class.
    /// </summary>
    public TableMapping Table { get; }

    /// <inheritdoc />
    public override Type ElementType => Table.Type;

    /// <inheritdoc />
    public override Expression Expression => Expression.Constant(this);

    /// <inheritdoc />
    public override IQueryProvider Provider => Database;

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
    {
        return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
    }

    /// <summary>
    /// Creates the table in the database if it does not exist.
    /// </summary>
    public int CreateTable()
    {
        if (Table.IsFullTextSearch)
        {
            return CreateFullTextSearchTable();
        }

        string columns = string.Join(", ", Table.Columns.Select(c => c.GetCreateColumnSql()));

        string sql = $"CREATE TABLE IF NOT EXISTS \"{Table.TableName}\" ({columns})";

        if (Table.WithoutRowId)
        {
            sql += " WITHOUT ROWID";
        }

        int count = Database.CreateCommand(sql, []).ExecuteNonQuery();

        foreach (TableColumn tableColumn in Table.Columns)
        {
            foreach (IndexedAttribute index in tableColumn.Indices)
            {
                string indexName = index.Name ?? ("idx_" + tableColumn.Name + "_" + index.Order);

                if (index.IsUnique)
                {
                    string uniqueSql = $"CREATE UNIQUE INDEX IF NOT EXISTS \"{indexName}\" ON \"{Table.TableName}\" ({tableColumn.Name})";
                    count += Database.CreateCommand(uniqueSql, []).ExecuteNonQuery();
                }
                else
                {
                    string indexSql = $"CREATE INDEX IF NOT EXISTS \"{indexName}\" ON \"{Table.TableName}\" ({tableColumn.Name})";
                    count += Database.CreateCommand(indexSql, []).ExecuteNonQuery();
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Deletes the table from the database.
    /// </summary>
    public int DropTable()
    {
        int count = 0;
        if (Table.IsFullTextSearch && Table.FullTextSearch!.AutoSync == FtsAutoSync.Triggers)
        {
            foreach (string trigger in TriggerNames())
            {
                count += Database.CreateCommand($"DROP TRIGGER IF EXISTS \"{trigger}\"", []).ExecuteNonQuery();
            }
        }

        string sql = $"DROP TABLE IF EXISTS \"{Table.TableName}\"";
        count += Database.CreateCommand(sql, []).ExecuteNonQuery();
        return count;
    }

    /// <summary>
    /// Performs a DELETE operation on the database table.
    /// </summary>
    /// <remarks>
    /// WARNING! This will delete all rows in the table.
    /// </remarks>
    public int Clear()
    {
        string sql = $"DELETE FROM \"{Table.TableName}\"";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    private int CreateFullTextSearchTable()
    {
        FtsTableInfo fts = Table.FullTextSearch!;
        StringBuilder sb = new();
        sb.Append("CREATE VIRTUAL TABLE IF NOT EXISTS \"");
        sb.Append(Table.TableName);
        sb.Append("\" USING fts5(");

        bool first = true;
        foreach (FtsIndexedColumn column in fts.IndexedColumns)
        {
            if (!first)
            {
                sb.Append(", ");
            }

            first = false;
            sb.Append(column.Name);
            if (column.Unindexed)
            {
                sb.Append(" UNINDEXED");
            }
        }

        if (fts.ContentMode == FtsContentMode.External)
        {
            string sourceTable = ResolveContentTableName(fts);
            string contentRowId = ResolveContentRowIdColumn(fts);
            sb.Append(", content='");
            sb.Append(sourceTable.Replace("'", "''"));
            sb.Append("', content_rowid='");
            sb.Append(contentRowId.Replace("'", "''"));
            sb.Append('\'');
        }
        else if (fts.ContentMode == FtsContentMode.Contentless)
        {
            sb.Append(", content=''");
        }

        sb.Append(", tokenize='");
        sb.Append(fts.TokenizerClause.Replace("'", "''"));
        sb.Append('\'');

        if (!string.IsNullOrEmpty(fts.Attribute.Prefix))
        {
            sb.Append(", prefix='");
            sb.Append(fts.Attribute.Prefix.Replace("'", "''"));
            sb.Append('\'');
        }

        sb.Append(')');

        int count = Database.CreateCommand(sb.ToString(), []).ExecuteNonQuery();

        if (fts.ContentMode == FtsContentMode.External && fts.AutoSync == FtsAutoSync.Triggers)
        {
            foreach (string triggerSql in BuildTriggerSql(fts))
            {
                count += Database.CreateCommand(triggerSql, []).ExecuteNonQuery();
            }
        }

        return count;
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "ContentTable is referenced by user code via [FullTextSearch(ContentTable = typeof(...))], so its public properties are rooted by the user.")]
    private string ResolveContentTableName(FtsTableInfo fts)
    {
        Type sourceType = fts.Attribute.ContentTable!;
        TableMapping sourceMapping = Database.TableMapping(sourceType);
        return sourceMapping.TableName;
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "ContentTable is referenced by user code via [FullTextSearch(ContentTable = typeof(...))], so its public properties are rooted by the user.")]
    private string ResolveContentRowIdColumn(FtsTableInfo fts)
    {
        if (!string.IsNullOrEmpty(fts.Attribute.ContentRowIdColumn))
        {
            return fts.Attribute.ContentRowIdColumn!;
        }

        Type sourceType = fts.Attribute.ContentTable!;
        TableMapping sourceMapping = Database.TableMapping(sourceType);
        TableColumn? pk = sourceMapping.Columns.FirstOrDefault(c => c.IsPrimaryKey);
        if (pk != null)
        {
            return pk.Name;
        }

        throw new InvalidOperationException($"FTS5 entity '{Table.Type.Name}' targets '{sourceType.Name}' but the source has no [Key] property. Mark the primary key with [Key] or set ContentRowIdColumn on [FullTextSearch].");
    }

    private IEnumerable<string> BuildTriggerSql(FtsTableInfo fts)
    {
        string ftsName = Table.TableName;
        string sourceTable = ResolveContentTableName(fts);
        string sourceRowId = ResolveContentRowIdColumn(fts);

        string columnList = string.Join(", ", fts.IndexedColumns.Select(c => c.Name));
        string newValues = string.Join(", ", fts.IndexedColumns.Select(c => "new." + c.Name));
        string oldValues = string.Join(", ", fts.IndexedColumns.Select(c => "old." + c.Name));

        (string ai, string ad, string au) = TriggerNamesTuple();

        yield return $"CREATE TRIGGER IF NOT EXISTS \"{ai}\" AFTER INSERT ON \"{sourceTable}\" BEGIN " +
                     $"INSERT INTO \"{ftsName}\"(rowid, {columnList}) VALUES (new.{sourceRowId}, {newValues}); END";

        yield return $"CREATE TRIGGER IF NOT EXISTS \"{ad}\" AFTER DELETE ON \"{sourceTable}\" BEGIN " +
                     $"INSERT INTO \"{ftsName}\"(\"{ftsName}\", rowid, {columnList}) VALUES('delete', old.{sourceRowId}, {oldValues}); END";

        yield return $"CREATE TRIGGER IF NOT EXISTS \"{au}\" AFTER UPDATE ON \"{sourceTable}\" BEGIN " +
                     $"INSERT INTO \"{ftsName}\"(\"{ftsName}\", rowid, {columnList}) VALUES('delete', old.{sourceRowId}, {oldValues}); " +
                     $"INSERT INTO \"{ftsName}\"(rowid, {columnList}) VALUES (new.{sourceRowId}, {newValues}); END";
    }

    private (string ai, string ad, string au) TriggerNamesTuple()
    {
        string baseName = Table.TableName + "_sync";
        return (baseName + "_ai", baseName + "_ad", baseName + "_au");
    }

    private IEnumerable<string> TriggerNames()
    {
        (string ai, string ad, string au) = TriggerNamesTuple();
        yield return ai;
        yield return ad;
        yield return au;
    }
}

/// <summary>
/// Represents a table in the SQLite database.
/// </summary>
public class SQLiteTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T> : SQLiteTable, IQueryable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteTable{T}"/> class.
    /// </summary>
    public SQLiteTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    /// <summary>
    /// Wraps the provided SQL query and parameters into a queryable object.
    /// </summary>
    public IQueryable<T> FromSql(string sql, params SQLiteParameter[] parameters)
    {
        return Database.FromSql<T>(sql, parameters);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the row.
    /// </summary>
    public int Add(T item)
    {
        (TableColumn[] columns, string sql) = GetAddInfo();

        return AddOrRemoveItem(columns, sql, item);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the rows.
    /// </summary>
    public int AddRange(IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        (TableColumn[] columns, string sql) = GetAddInfo();

        int count = 0;

        if (runInTransaction)
        {
            using SQLiteTransaction transaction = Database.BeginTransaction(separateConnection);

            foreach (T item in collection)
            {
                count += AddOrRemoveItem(columns, sql, item);
            }

            transaction.Commit();
        }
        else
        {
            foreach (T item in collection)
            {
                count += AddOrRemoveItem(columns, sql, item);
            }
        }

        return count;
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the row.
    /// </summary>
    public int Update(T item)
    {
        (TableColumn[] columns, TableColumn[] primaryKeyColumns, string sql) = GetUpdateInfo();

        return UpdateItem(columns, primaryKeyColumns, sql, item);
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the rows.
    /// </summary>
    public int UpdateRange(IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        (TableColumn[] columns, TableColumn[] primaryKeyColumns, string sql) = GetUpdateInfo();

        int count = 0;

        if (runInTransaction)
        {
            using SQLiteTransaction transaction = Database.BeginTransaction(separateConnection);

            foreach (T item in collection)
            {
                count += UpdateItem(columns, primaryKeyColumns, sql, item);
            }

            transaction.Commit();
        }
        else
        {
            foreach (T item in collection)
            {
                count += UpdateItem(columns, primaryKeyColumns, sql, item);
            }
        }

        return count;
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the row.
    /// </summary>
    public int Remove(T item)
    {
        (TableColumn[] primaryKeyColumns, string sql) = GetRemoveInfo();

        return AddOrRemoveItem(primaryKeyColumns, sql, item);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the rows.
    /// </summary>
    public int RemoveRange(IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        (TableColumn[] primaryKeyColumns, string sql) = GetRemoveInfo();

        int count = 0;

        if (runInTransaction)
        {
            using SQLiteTransaction transaction = Database.BeginTransaction(separateConnection);

            foreach (T item in collection)
            {
                count += AddOrRemoveItem(primaryKeyColumns, sql, item);
            }

            transaction.Commit();
        }
        else
        {
            foreach (T item in collection)
            {
                count += AddOrRemoveItem(primaryKeyColumns, sql, item);
            }
        }

        return count;
    }

    /// <summary>
    /// Performs an INSERT OR REPLACE operation on the database table using the row.
    /// </summary>
    public int AddOrUpdate(T item)
    {
        (TableColumn[] columns, string sql) = GetAddOrUpdateInfo();

        return AddOrRemoveItem(columns, sql, item);
    }

    /// <summary>
    /// Performs an INSERT OR REPLACE operation on the database table using the rows.
    /// </summary>
    public int AddOrUpdateRange(IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        (TableColumn[] columns, string sql) = GetAddOrUpdateInfo();

        int count = 0;

        if (runInTransaction)
        {
            using SQLiteTransaction transaction = Database.BeginTransaction(separateConnection);

            foreach (T item in collection)
            {
                count += AddOrRemoveItem(columns, sql, item);
            }

            transaction.Commit();
        }
        else
        {
            foreach (T item in collection)
            {
                count += AddOrRemoveItem(columns, sql, item);
            }
        }

        return count;
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return Database.ExecuteSequenceQuery<T>(Expression).GetEnumerator();
    }

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }

    private (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        TableColumn[] columns = Table.Columns
            .Where(f => !f.IsPrimaryKey || !f.IsAutoIncrement)
            .ToArray();

        string columnsString = string.Join(", ", columns.Select(c => c.Name));
        string parametersString = string.Join(", ", columns.Select((c, i) => WrapParam($"@p{i}", c)));

        string sql = $"INSERT INTO \"{Table.TableName}\" ({columnsString}) VALUES ({parametersString})";

        return (columns, sql);
    }

    private (TableColumn[] Columns, TableColumn[] PrimaryColumns, string Sql) GetUpdateInfo()
    {
        TableColumn[] columns = Table.Columns
            .Where(f => !f.IsPrimaryKey || !f.IsAutoIncrement)
            .ToArray();

        TableColumn[] primaryKeyColumns = Table.Columns
            .Where(f => f.IsPrimaryKey)
            .ToArray();

        string setClause = string.Join(", ", columns.Select((c, i) => $"{c.Name} = {WrapParam($"@p{i}", c)}"));
        string primaryKeyClause = string.Join(" AND ",
            primaryKeyColumns.Select((c, i) => $"{c.Name} = @p{i + columns.Length}")
        );
        string sql = $"UPDATE \"{Table.TableName}\" SET {setClause} WHERE {primaryKeyClause}";

        return (columns, primaryKeyColumns, sql);
    }

    private (TableColumn[] PrimaryColumns, string Sql) GetRemoveInfo()
    {
        TableColumn[] primaryKeyColumns = Table.Columns
            .Where(f => f.IsPrimaryKey)
            .ToArray();

        if (primaryKeyColumns.Length == 0)
        {
            throw new Exception("Cannot perform a delete operation without a primary key.");
        }

        string primaryKeyClause = string.Join(" AND ",
            primaryKeyColumns.Select((c, i) => $"{c.Name} = @p{i}")
        );
        string sql = $"DELETE FROM \"{Table.TableName}\" WHERE {primaryKeyClause}";

        return (primaryKeyColumns, sql);
    }

    private (TableColumn[] Columns, string Sql) GetAddOrUpdateInfo()
    {
        TableColumn[] columns = Table.Columns
            .Where(f => !f.IsPrimaryKey || !f.IsAutoIncrement)
            .ToArray();

        string columnsString = string.Join(", ", columns.Select(c => c.Name));
        string parametersString = string.Join(", ", columns.Select((c, i) => WrapParam($"@p{i}", c)));

        string sql = $"INSERT OR REPLACE INTO \"{Table.TableName}\" ({columnsString}) VALUES ({parametersString})";

        return (columns, sql);
    }

    private string WrapParam(string placeholder, TableColumn column)
    {
        if (Database.Options.TypeConverters.TryGetValue(column.PropertyType, out ISQLiteTypeConverter? conv)
            && conv.ParameterSqlExpression is { } paramExpr)
        {
            return string.Format(paramExpr, placeholder);
        }

        return placeholder;
    }

    private int AddOrRemoveItem(TableColumn[] columns, string sql, T item)
    {
        List<SQLiteParameter> parameters = columns
            .Select((c, i) => new SQLiteParameter
            {
                Name = $"@p{i}",
                Value = c.PropertyInfo.GetValue(item)
            })
            .ToList();

        return Database.CreateCommand(sql, parameters).ExecuteNonQuery();
    }

    private int UpdateItem(TableColumn[] columns, TableColumn[] primaryColumns, string sql, T item)
    {
        IEnumerable<SQLiteParameter> primaryParameters = primaryColumns
            .Select((c, i) => new SQLiteParameter
            {
                Name = $"@p{i + columns.Length}",
                Value = c.PropertyInfo.GetValue(item)
            });

        List<SQLiteParameter> parameters = columns
            .Select((c, i) => new SQLiteParameter
            {
                Name = $"@p{i}",
                Value = c.PropertyInfo.GetValue(item)
            })
            .Concat(primaryParameters)
            .ToList();

        return Database.CreateCommand(sql, parameters).ExecuteNonQuery();
    }
}