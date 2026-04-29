using System.Text;

namespace SQLite.Framework;

/// <summary>
/// Schema operations for the database. Use <see cref="SQLiteDatabase.Schema" /> to reach an
/// instance.
/// </summary>
/// <remarks>
/// All methods run synchronous SQL. Async wrappers live in <see cref="Extensions.AsyncDatabaseExtensions" />.
/// To customize how DDL is built (for example, to change FTS5 trigger generation), inherit from
/// this class and register the subclass with <see cref="SQLiteOptionsBuilder.UseSchema" />.
/// </remarks>
public class SQLiteSchema
{
    /// <summary>
    /// Initializes a new instance of <see cref="SQLiteSchema" />.
    /// </summary>
    public SQLiteSchema(SQLiteDatabase database)
    {
        Database = database;
    }

    /// <summary>
    /// The database this schema instance is bound to.
    /// </summary>
    public SQLiteDatabase Database { get; }

    /// <summary>
    /// Creates the table for <typeparamref name="T" /> if it does not exist. Issues
    /// <c>CREATE TABLE IF NOT EXISTS</c> plus any indexes declared with
    /// <see cref="IndexedAttribute" />.
    /// </summary>
    public virtual int CreateTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        return CreateTable(typeof(T));
    }

    /// <summary>
    /// Creates the table for <paramref name="type" /> if it does not exist.
    /// </summary>
    public virtual int CreateTable([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        TableMapping mapping = Database.TableMapping(type);

        if (mapping.IsFullTextSearch)
        {
            return CreateFullTextSearchTable(mapping);
        }

        TableColumn[] primaryKeyColumns = mapping.Columns.Where(c => c.IsPrimaryKey).ToArray();
        bool hasCompositePrimaryKey = primaryKeyColumns.Length > 1;

        string columns = string.Join(", ", mapping.Columns.Select(c => c.GetCreateColumnSql(!hasCompositePrimaryKey)));
        if (hasCompositePrimaryKey)
        {
            string pkList = string.Join(", ", primaryKeyColumns.Select(c => $"\"{c.Name}\""));
            columns += $", PRIMARY KEY ({pkList})";
        }

        string sql = $"CREATE TABLE IF NOT EXISTS \"{mapping.TableName}\" ({columns})";

        if (mapping.WithoutRowId)
        {
            sql += " WITHOUT ROWID";
        }

        int count = Database.CreateCommand(sql, []).ExecuteNonQuery();

        foreach (TableColumn tableColumn in mapping.Columns)
        {
            foreach (IndexedAttribute index in tableColumn.Indices)
            {
                string indexName = index.Name ?? ("idx_" + tableColumn.Name + "_" + index.Order);
                string uniqueClause = index.IsUnique ? "UNIQUE " : string.Empty;
                string indexSql = $"CREATE {uniqueClause}INDEX IF NOT EXISTS \"{indexName}\" ON \"{mapping.TableName}\" ({tableColumn.Name})";
                count += Database.CreateCommand(indexSql, []).ExecuteNonQuery();
            }
        }

        return count;
    }

    /// <summary>
    /// Drops the table for <typeparamref name="T" /> if it exists. Drops any FTS5 sync triggers
    /// declared alongside the virtual table first.
    /// </summary>
    public virtual int DropTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        return DropTable(typeof(T));
    }

    /// <summary>
    /// Drops the table for <paramref name="type" /> if it exists.
    /// </summary>
    public virtual int DropTable([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        TableMapping mapping = Database.TableMapping(type);

        int count = 0;
        if (mapping.IsFullTextSearch && mapping.FullTextSearch!.AutoSync == FtsAutoSync.Triggers)
        {
            foreach (string trigger in TriggerNames(mapping))
            {
                count += Database.CreateCommand($"DROP TRIGGER IF EXISTS \"{trigger}\"", []).ExecuteNonQuery();
            }
        }

        count += Database.CreateCommand($"DROP TABLE IF EXISTS \"{mapping.TableName}\"", []).ExecuteNonQuery();
        return count;
    }

    /// <summary>
    /// Drops the table whose SQLite name matches <paramref name="tableName" /> if it exists. Use
    /// the typed overload when you have an entity class.
    /// </summary>
    public virtual int DropTable(string tableName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tableName);
        return Database.CreateCommand($"DROP TABLE IF EXISTS \"{tableName.Replace("\"", "\"\"")}\"", []).ExecuteNonQuery();
    }

    /// <summary>
    /// Creates an index over a single column. The index name defaults to
    /// <c>idx_{TableName}_{ColumnName}</c> when not supplied.
    /// </summary>
    public virtual int CreateIndex<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Expression<Func<T, object?>> column, string? name = null, bool unique = false)
    {
        ArgumentNullException.ThrowIfNull(column);

        TableMapping mapping = Database.TableMapping<T>();
        string columnName = ResolveColumnName(mapping, column);
        string indexName = name ?? $"idx_{mapping.TableName}_{columnName}";
        string uniqueClause = unique ? "UNIQUE " : string.Empty;

        string sql = $"CREATE {uniqueClause}INDEX IF NOT EXISTS \"{indexName}\" ON \"{mapping.TableName}\" ({columnName})";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Drops an index by name if it exists.
    /// </summary>
    public virtual int DropIndex(string indexName)
    {
        ArgumentException.ThrowIfNullOrEmpty(indexName);
        return Database.CreateCommand($"DROP INDEX IF EXISTS \"{indexName.Replace("\"", "\"\"")}\"", []).ExecuteNonQuery();
    }

    /// <summary>
    /// Returns <see langword="true" /> when the table for <typeparamref name="T" /> exists.
    /// </summary>
    public virtual bool TableExists<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        return TableExists(Database.TableMapping<T>().TableName);
    }

    /// <summary>
    /// Returns <see langword="true" /> when a table with the given SQLite name exists.
    /// </summary>
    public virtual bool TableExists(string tableName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tableName);
        long? count = Database.ExecuteScalar<long?>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name",
            [new SQLiteParameter { Name = "@name", Value = tableName }]);
        return count > 0;
    }

    /// <summary>
    /// Returns <see langword="true" /> when an index with the given name exists.
    /// </summary>
    public virtual bool IndexExists(string indexName)
    {
        ArgumentException.ThrowIfNullOrEmpty(indexName);
        long? count = Database.ExecuteScalar<long?>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = @name",
            [new SQLiteParameter { Name = "@name", Value = indexName }]);
        return count > 0;
    }

    /// <summary>
    /// Returns <see langword="true" /> when the column with the given SQLite name exists on the
    /// table for <typeparamref name="T" />.
    /// </summary>
    public virtual bool ColumnExists<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string columnName)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnName);
        return ListColumns<T>().Any(c => c.Name == columnName);
    }

    /// <summary>
    /// Lists the names of every user table in the database, ordered alphabetically. System
    /// tables (those whose name starts with <c>sqlite_</c>) are skipped.
    /// </summary>
    public virtual IReadOnlyList<string> ListTables()
    {
        return Database.Query<string>(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name");
    }

    /// <summary>
    /// Lists the names of every index in the database. When <paramref name="tableName" /> is set,
    /// only indexes that target that table are returned.
    /// </summary>
    public virtual IReadOnlyList<string> ListIndexes(string? tableName = null)
    {
        if (tableName == null)
        {
            return Database.Query<string>(
                "SELECT name FROM sqlite_master WHERE type = 'index' AND name NOT LIKE 'sqlite_%' ORDER BY name");
        }

        return Database.Query<string>(
            "SELECT name FROM sqlite_master WHERE type = 'index' AND tbl_name = @t AND name NOT LIKE 'sqlite_%' ORDER BY name",
            [new SQLiteParameter { Name = "@t", Value = tableName }]);
    }

    /// <summary>
    /// Lists every column on the table for <typeparamref name="T" /> as SQLite reports them.
    /// </summary>
    public virtual IReadOnlyList<SchemaColumnInfo> ListColumns<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        TableMapping mapping = Database.TableMapping<T>();
        return ListColumns(mapping.TableName);
    }

    /// <summary>
    /// Lists every column on the table whose SQLite name matches <paramref name="tableName" />.
    /// </summary>
    public virtual IReadOnlyList<SchemaColumnInfo> ListColumns(string tableName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tableName);

        string escaped = tableName.Replace("\"", "\"\"");
        SQLiteCommand cmd = Database.CreateCommand($"PRAGMA table_info(\"{escaped}\")", []);
        using SQLiteDataReader reader = cmd.ExecuteReader();

        int nameIdx = -1, typeIdx = -1, notNullIdx = -1, defaultIdx = -1, pkIdx = -1;
        for (int i = 0; i < reader.FieldCount; i++)
        {
            switch (reader.GetName(i))
            {
                case "name": nameIdx = i; break;
                case "type": typeIdx = i; break;
                case "notnull": notNullIdx = i; break;
                case "dflt_value": defaultIdx = i; break;
                case "pk": pkIdx = i; break;
            }
        }

        List<SchemaColumnInfo> result = [];
        while (reader.Read())
        {
            string name = (string)reader.GetValue(nameIdx, reader.GetColumnType(nameIdx), typeof(string))!;
            string type = (string)reader.GetValue(typeIdx, reader.GetColumnType(typeIdx), typeof(string))!;
            long notNull = (long)reader.GetValue(notNullIdx, reader.GetColumnType(notNullIdx), typeof(long))!;
            object? defaultRaw = reader.GetValue(defaultIdx, reader.GetColumnType(defaultIdx), typeof(string));
            long pk = (long)reader.GetValue(pkIdx, reader.GetColumnType(pkIdx), typeof(long))!;

            result.Add(new SchemaColumnInfo
            {
                Name = name,
                Type = type,
                IsNullable = notNull == 0,
                IsPrimaryKey = pk > 0,
                DefaultValue = defaultRaw as string,
            });
        }

        return result;
    }

    /// <summary>
    /// Adds the column for the property named <paramref name="propertyName" /> to the table for
    /// <typeparamref name="T" />. The type, nullability, and primary-key flags come from the
    /// entity mapping.
    /// </summary>
    public virtual int AddColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string propertyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        TableMapping mapping = Database.TableMapping<T>();
        TableColumn? column = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == propertyName);
        if (column == null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' is not mapped on {typeof(T).Name}.");
        }

        string sql = $"ALTER TABLE \"{mapping.TableName}\" ADD COLUMN {column.GetCreateColumnSql()}";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Renames a column on the table for <typeparamref name="T" />. Both names are SQLite column
    /// names, not entity property names.
    /// </summary>
    public virtual int RenameColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string fromColumn, string toColumn)
    {
        ArgumentException.ThrowIfNullOrEmpty(fromColumn);
        ArgumentException.ThrowIfNullOrEmpty(toColumn);

        TableMapping mapping = Database.TableMapping<T>();
        string sql = $"ALTER TABLE \"{mapping.TableName}\" RENAME COLUMN \"{fromColumn.Replace("\"", "\"\"")}\" TO \"{toColumn.Replace("\"", "\"\"")}\"";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Drops the column with the given SQLite name from the table for <typeparamref name="T" />.
    /// </summary>
    public virtual int DropColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string columnName)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnName);

        TableMapping mapping = Database.TableMapping<T>();
        string sql = $"ALTER TABLE \"{mapping.TableName}\" DROP COLUMN \"{columnName.Replace("\"", "\"\"")}\"";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Returns a fluent builder for creating the table for <typeparamref name="T" /> together
    /// with computed columns, CHECK constraints, and indexes. Call <c>.Create()</c> when done
    /// chaining to issue the DDL.
    /// </summary>
    public virtual SQLiteTableBuilder<T> Table<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        return new SQLiteTableBuilder<T>(this);
    }

    /// <summary>
    /// Renames the table for <typeparamref name="T" /> in the database. The mapping in your code
    /// is not updated; usually you also update the <see cref="System.ComponentModel.DataAnnotations.Schema.TableAttribute" />
    /// or the entity name to match.
    /// </summary>
    public virtual int RenameTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string newTableName)
    {
        ArgumentException.ThrowIfNullOrEmpty(newTableName);

        TableMapping mapping = Database.TableMapping<T>();
        string sql = $"ALTER TABLE \"{mapping.TableName}\" RENAME TO \"{newTableName.Replace("\"", "\"\"")}\"";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Emits the <c>CREATE VIRTUAL TABLE ... USING fts5(...)</c> statement plus any
    /// <c>AFTER</c> sync triggers when <see cref="FtsTableInfo.AutoSync" /> is set to
    /// <see cref="FtsAutoSync.Triggers" />. Override to change how an FTS5 table is created
    /// (for example, to add extra options or skip trigger creation).
    /// </summary>
    /// <returns>The total number of rows affected by the issued statements.</returns>
    protected virtual int CreateFullTextSearchTable(TableMapping mapping)
    {
        FtsTableInfo fts = mapping.FullTextSearch!;
        StringBuilder sb = new();
        sb.Append("CREATE VIRTUAL TABLE IF NOT EXISTS \"");
        sb.Append(mapping.TableName);
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
            string contentRowId = ResolveContentRowIdColumn(fts, mapping);
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
            foreach (string triggerSql in BuildTriggerSql(fts, mapping))
            {
                count += Database.CreateCommand(triggerSql, []).ExecuteNonQuery();
            }
        }

        return count;
    }

    /// <summary>
    /// Returns the SQL table name of the source content table for an external-content FTS5 table.
    /// Override to change how the source table name is resolved.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "ContentTable is referenced by user code via [FullTextSearch(ContentTable = typeof(...))], so its public properties are rooted by the user.")]
    protected virtual string ResolveContentTableName(FtsTableInfo fts)
    {
        Type sourceType = fts.Attribute.ContentTable!;
        TableMapping sourceMapping = Database.TableMapping(sourceType);
        return sourceMapping.TableName;
    }

    /// <summary>
    /// Returns the column name on the source table that the FTS5 virtual table's <c>rowid</c>
    /// links to. Defaults to the source table's <c>[Key]</c> property, falling back to
    /// <see cref="FullTextSearchAttribute.ContentRowIdColumn" /> when set. Override to choose a
    /// different column.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "ContentTable is referenced by user code via [FullTextSearch(ContentTable = typeof(...))], so its public properties are rooted by the user.")]
    protected virtual string ResolveContentRowIdColumn(FtsTableInfo fts, TableMapping mapping)
    {
        if (!string.IsNullOrEmpty(fts.Attribute.ContentRowIdColumn))
        {
            return fts.Attribute.ContentRowIdColumn!;
        }

        Type sourceType = fts.Attribute.ContentTable!;
        TableMapping sourceMapping = Database.TableMapping(sourceType);
        TableColumn[] pks = sourceMapping.Columns.Where(c => c.IsPrimaryKey).ToArray();
        if (pks.Length == 1)
        {
            return pks[0].Name;
        }

        if (pks.Length > 1)
        {
            throw new InvalidOperationException($"FTS5 entity '{mapping.Type.Name}' targets '{sourceType.Name}' which has a composite primary key. Set ContentRowIdColumn on [FullTextSearch] to pick the rowid column explicitly.");
        }

        throw new InvalidOperationException($"FTS5 entity '{mapping.Type.Name}' targets '{sourceType.Name}' but the source has no [Key] property. Mark the primary key with [Key] or set ContentRowIdColumn on [FullTextSearch].");
    }

    /// <summary>
    /// Yields the FTS5 sync trigger statements (insert, delete, update) that keep the FTS
    /// virtual table aligned with its external content table. Override to change the trigger
    /// shape, for example to add a <c>WHERE</c> clause or use partial triggers.
    /// </summary>
    protected virtual IEnumerable<string> BuildTriggerSql(FtsTableInfo fts, TableMapping mapping)
    {
        string ftsName = mapping.TableName;
        string sourceTable = ResolveContentTableName(fts);
        string sourceRowId = ResolveContentRowIdColumn(fts, mapping);

        string columnList = string.Join(", ", fts.IndexedColumns.Select(c => c.Name));
        string newValues = string.Join(", ", fts.IndexedColumns.Select(c => "new." + c.Name));
        string oldValues = string.Join(", ", fts.IndexedColumns.Select(c => "old." + c.Name));

        (string ai, string ad, string au) = TriggerNamesTuple(mapping);

        yield return $"CREATE TRIGGER IF NOT EXISTS \"{ai}\" AFTER INSERT ON \"{sourceTable}\" BEGIN " +
                     $"INSERT INTO \"{ftsName}\"(rowid, {columnList}) VALUES (new.{sourceRowId}, {newValues}); END";

        yield return $"CREATE TRIGGER IF NOT EXISTS \"{ad}\" AFTER DELETE ON \"{sourceTable}\" BEGIN " +
                     $"INSERT INTO \"{ftsName}\"(\"{ftsName}\", rowid, {columnList}) VALUES('delete', old.{sourceRowId}, {oldValues}); END";

        yield return $"CREATE TRIGGER IF NOT EXISTS \"{au}\" AFTER UPDATE ON \"{sourceTable}\" BEGIN " +
                     $"INSERT INTO \"{ftsName}\"(\"{ftsName}\", rowid, {columnList}) VALUES('delete', old.{sourceRowId}, {oldValues}); " +
                     $"INSERT INTO \"{ftsName}\"(rowid, {columnList}) VALUES (new.{sourceRowId}, {newValues}); END";
    }

    /// <summary>
    /// Returns the names of the three FTS5 sync triggers (after-insert, after-delete, after-update)
    /// emitted alongside the virtual table. Override to use a different naming convention.
    /// </summary>
    protected virtual (string ai, string ad, string au) TriggerNamesTuple(TableMapping mapping)
    {
        string baseName = mapping.TableName + "_sync";
        return (baseName + "_ai", baseName + "_ad", baseName + "_au");
    }

    /// <summary>
    /// Enumerates the trigger names that <see cref="DropTable{T}" /> drops before the virtual table.
    /// Override if <see cref="TriggerNamesTuple" /> alone is not enough to express which triggers
    /// belong to this table.
    /// </summary>
    protected virtual IEnumerable<string> TriggerNames(TableMapping mapping)
    {
        (string ai, string ad, string au) = TriggerNamesTuple(mapping);
        yield return ai;
        yield return ad;
        yield return au;
    }

    private static string ResolveColumnName<T>(TableMapping mapping, Expression<Func<T, object?>> column)
    {
        Expression body = column.Body;
        if (body is UnaryExpression unary)
        {
            body = unary.Operand;
        }

        if (body is not MemberExpression member)
        {
            throw new InvalidOperationException("Expected a property access expression on the entity, like b => b.Title.");
        }

        TableColumn col = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == member.Member.Name)
            ?? throw new InvalidOperationException($"Property '{member.Member.Name}' is not mapped on the table.");

        return col.Name;
    }
}
