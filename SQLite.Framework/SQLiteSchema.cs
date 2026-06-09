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
            if (mapping.ComputedColumns.Count > 0 || mapping.Checks.Count > 0 || mapping.Indexes.Count > 0)
            {
                throw new InvalidOperationException(
                    $"FTS5 entity '{mapping.Type.Name}' does not support computed columns, checks, or indexes declared on the model. Remove them.");
            }

            return CreateFullTextSearchTable(mapping);
        }

        if (mapping.IsRTree)
        {
            return CreateRTreeTable(mapping);
        }

        int count = Database.CreateCommand(
            SchemaSqlBuilder.BuildCreateTable(Database, mapping, mapping.TableName, ifNotExists: true), []).ExecuteNonQuery();

        foreach ((string _, string indexSql) in SchemaSqlBuilder.BuildIndexes(mapping, mapping.TableName, ifNotExists: true))
        {
            count += Database.CreateCommand(indexSql, []).ExecuteNonQuery();
        }

        foreach ((string _, string triggerSql) in SchemaSqlBuilder.BuildTriggers(mapping, mapping.TableName, ifNotExists: true))
        {
            count += Database.CreateCommand(triggerSql, []).ExecuteNonQuery();
        }

        return count;
    }

    /// <summary>
    /// Reconciles the live table for <typeparamref name="T" /> with the model in place. New columns
    /// are added and removed columns are dropped with <c>ALTER TABLE</c>, so tables that reference
    /// this one through a foreign key are never touched.. Needs SQLite 3.35.0 for <c>DROP COLUMN</c>.
    /// Use <see cref="MigrateByRebuild{T}()" /> on older SQLite.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public virtual int Migrate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        return Migrate(typeof(T));
    }

    /// <summary>
    /// Reconciles the live table for <typeparamref name="T" /> with the model, filling or overriding
    /// columns from the values declared with <paramref name="fill" />.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public virtual int Migrate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill)
    {
        TableMapping mapping = Database.TableMapping<T>();
        SQLiteMigrationBuilder<T> builder = new(Database, mapping);
        fill(builder);
        return MigrateInPlace(mapping, builder.Sets);
    }

    /// <summary>
    /// Reconciles the live table for <paramref name="type" /> with the model in place. Creates the
    /// table when it is missing. Added columns use <c>ALTER TABLE ADD COLUMN</c> and removed columns
    /// use <c>ALTER TABLE DROP COLUMN</c>, so referencing tables are never touched.
    /// Changed or missing indexes and triggers are dropped and recreated.
    /// FTS5 and R-Tree tables are only ensured to exist. Returns the number of statements run.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public virtual int Migrate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        return MigrateInPlace(Database.TableMapping(type), []);
    }

    /// <summary>
    /// Reconciles the live table for <typeparamref name="T" /> with the model by rebuilding it.
    /// </summary>
    public virtual int MigrateByRebuild<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        return MigrateByRebuild(typeof(T));
    }

    /// <summary>
    /// Reconciles the live table for <typeparamref name="T" /> with the model by rebuilding it, filling
    /// or overriding columns from the values declared with <paramref name="fill" />. Use this to give a
    /// new <c>NOT NULL</c> column a value, or to recompute a column from the old row. Returns the number
    /// of statements run.
    /// </summary>
    public virtual int MigrateByRebuild<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill)
    {
        TableMapping mapping = Database.TableMapping<T>();
        SQLiteMigrationBuilder<T> builder = new(Database, mapping);
        fill(builder);
        return MigrateCore(mapping, builder.Sets);
    }

    /// <summary>
    /// Reconciles the live table for <paramref name="type" /> with the model by rebuilding it. Creates
    /// the table when it is missing. When the table definition has drifted, it rebuilds the table the
    /// way SQLite recommends (create a new table, copy rows, drop the old one, rename), so it works on
    /// any SQLite version and handles any change. Rows in columns the model keeps are preserved.
    /// Changed or missing indexes and triggers are dropped and recreated. Triggers that are not declared
    /// on the model are left alone. FTS5 and R-Tree tables are only ensured to exist. Returns the number
    /// of statements run.
    /// <para>
    /// Limitations. A computed column in a referencing table is not preserved when the referenced table
    /// is rebuilt inside a transaction. A rebuild inside an open transaction moves the rows of every
    /// referencing table out and back, and drops and recreates their triggers, so those triggers do not
    /// fire during the restore.
    /// </para>
    /// </summary>
    public virtual int MigrateByRebuild([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        return MigrateCore(Database.TableMapping(type), []);
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

        string sql = $"CREATE {uniqueClause}INDEX IF NOT EXISTS \"{indexName.Replace("\"", "\"\"")}\" ON \"{mapping.TableName}\" ({IdentifierGuard.Quote(columnName)})";
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
    /// entity mapping. Pass <paramref name="defaultValue" /> to emit a <c>DEFAULT</c> clause. SQLite
    /// needs this when you add a <c>NOT NULL</c> column to a table that already has rows.
    /// </summary>
    /// <param name="propertyName">Property name on the entity to add.</param>
    /// <param name="defaultValue">Optional default value. The framework writes it as the SQL
    /// <c>DEFAULT</c> clause and SQLite uses it to backfill existing rows. Supported types are
    /// numbers, strings, and <see cref="bool" />. SQLite does not let you use parameters inside
    /// DDL statements like <c>ALTER TABLE</c>, so the value is written straight into the SQL text.
    /// Single quotes inside strings are doubled, so a value with quotes in it cannot escape from
    /// the string and run other SQL.</param>
    public virtual int AddColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string propertyName, object? defaultValue = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        TableMapping mapping = Database.TableMapping<T>();
        TableColumn? column = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' is not mapped on {typeof(T).Name}.");

        if (column.IsPrimaryKey)
        {
            throw new InvalidOperationException(
                $"Cannot add the primary key column '{propertyName}' to the existing table '{mapping.TableName}'. " +
                "SQLite does not allow ALTER TABLE ADD COLUMN to add a PRIMARY KEY or AUTOINCREMENT column. " +
                "Recreate the table with the new schema instead.");
        }

        string? defaultOverride = defaultValue == null ? null : SqlLiteralHelper.FormatLiteral(defaultValue, Database.Options);
        string sql = $"ALTER TABLE \"{mapping.TableName}\" ADD COLUMN {ColumnSql.GetCreateColumnSql(column, defaultOverride: defaultOverride, emitForeignKey: defaultOverride == null)}";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Adds the column selected by <paramref name="property" /> to the table for
    /// <typeparamref name="T" />. The type, nullability, and primary-key flags come from the
    /// entity mapping. Pass <paramref name="defaultValue" /> to emit a <c>DEFAULT</c> clause. SQLite
    /// needs this when you add a <c>NOT NULL</c> column to a table that already has rows.
    /// </summary>
    /// <param name="property">Property selector on the entity, like <c>b =&gt; b.Pages</c>.</param>
    /// <param name="defaultValue">Optional default value. The framework writes it as the SQL
    /// <c>DEFAULT</c> clause and SQLite uses it to backfill existing rows. Supported types are
    /// numbers, strings, and <see cref="bool" />. SQLite does not let you use parameters inside
    /// DDL statements like <c>ALTER TABLE</c>, so the value is written straight into the SQL text.
    /// Single quotes inside strings are doubled, so a value with quotes in it cannot escape from
    /// the string and run other SQL.</param>
    public virtual int AddColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Expression<Func<T, object?>> property, object? defaultValue = null)
    {
        ArgumentNullException.ThrowIfNull(property);
        return AddColumn<T>(ResolvePropertyName(property), defaultValue);
    }

    /// <summary>
    /// Adds the column for the property named <paramref name="propertyName" /> to the table for
    /// <typeparamref name="T" />. The body of <paramref name="defaultExpression" /> is translated
    /// to SQL and written into the <c>DEFAULT</c> clause. SQLite restricts what is allowed inside
    /// a <c>DEFAULT</c>: the expression must not reference any column of the table, must not
    /// contain a subquery, and must only call deterministic functions. Requires SQLite 3.31.0 or newer.
    /// </summary>
    /// <param name="propertyName">Property name on the entity to add.</param>
    /// <param name="defaultExpression">A parameterless lambda that produces the default value. The
    /// framework translates the body to SQL and inlines any constants as SQL literals. Parameters
    /// are not allowed because SQLite does not accept placeholders in DDL.</param>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android31.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios14.0")]
#endif
    public virtual int AddColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string propertyName, Expression<Func<object?>> defaultExpression)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        ArgumentNullException.ThrowIfNull(defaultExpression);
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_31, "ALTER TABLE ADD COLUMN with computed DEFAULT");
#endif

        TableMapping mapping = Database.TableMapping<T>();
        TableColumn? column = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' is not mapped on {typeof(T).Name}.");

        if (column.IsPrimaryKey)
        {
            throw new InvalidOperationException(
                $"Cannot add the primary key column '{propertyName}' to the existing table '{mapping.TableName}'. " +
                "SQLite does not allow ALTER TABLE ADD COLUMN to add a PRIMARY KEY or AUTOINCREMENT column. " +
                "Recreate the table with the new schema instead.");
        }

        string defaultSql = TranslateDefaultExpression(defaultExpression);
        string sql = $"ALTER TABLE \"{mapping.TableName}\" ADD COLUMN {ColumnSql.GetCreateColumnSql(column, defaultOverride: $"({defaultSql})", emitForeignKey: false)}";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Adds the column selected by <paramref name="property" /> to the table for
    /// <typeparamref name="T" />. The body of <paramref name="defaultExpression" /> is translated
    /// to SQL and written into the <c>DEFAULT</c> clause. Requires SQLite 3.31.0 or newer.
    /// </summary>
    /// <param name="property">Property selector on the entity, like <c>b =&gt; b.CreatedAt</c>.</param>
    /// <param name="defaultExpression">A parameterless lambda that produces the default value. See
    /// <see cref="AddColumn{T}(string, Expression{Func{object}})" /> for the SQLite restrictions on
    /// what is allowed in the body.</param>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android31.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios14.0")]
#endif
    public virtual int AddColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Expression<Func<T, object?>> property, Expression<Func<object?>> defaultExpression)
    {
        ArgumentNullException.ThrowIfNull(property);
        return AddColumn<T>(ResolvePropertyName(property), defaultExpression);
    }

    /// <summary>
    /// Renames a column on the table for <typeparamref name="T" />. Both names are SQLite column
    /// names, not entity property names. Requires SQLite 3.25.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android30.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios13.0")]
#endif
    public virtual int RenameColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string fromColumn, string toColumn)
    {
        ArgumentException.ThrowIfNullOrEmpty(fromColumn);
        ArgumentException.ThrowIfNullOrEmpty(toColumn);
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_25, "ALTER TABLE RENAME COLUMN");
#endif

        TableMapping mapping = Database.TableMapping<T>();
        string sql = $"ALTER TABLE \"{mapping.TableName}\" RENAME COLUMN \"{fromColumn.Replace("\"", "\"\"")}\" TO \"{toColumn.Replace("\"", "\"\"")}\"";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Drops the column with the given SQLite name from the table for <typeparamref name="T" />.
    /// Requires SQLite 3.35.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public virtual int DropColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string columnName)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnName);
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_35, "ALTER TABLE DROP COLUMN");
#endif

        TableMapping mapping = Database.TableMapping<T>();
        string sql = $"ALTER TABLE \"{mapping.TableName}\" DROP COLUMN \"{columnName.Replace("\"", "\"\"")}\"";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Returns a fluent builder for creating the table for <typeparamref name="T" /> together
    /// with computed columns, CHECK constraints, and indexes. Call <c>.Create()</c> when done
    /// chaining to issue the DDL.
    /// </summary>
    public virtual SQLiteTableSchema<T> Table<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        return new SQLiteTableSchema<T>(this);
    }

    /// <summary>
    /// Renames the table for <typeparamref name="T" /> in the database. The mapping in your code
    /// is not updated. Usually you also update the
    /// <see cref="System.ComponentModel.DataAnnotations.Schema.TableAttribute" /> or the entity
    /// name to match.
    /// </summary>
    public virtual int RenameTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string newTableName)
    {
        ArgumentException.ThrowIfNullOrEmpty(newTableName);

        TableMapping mapping = Database.TableMapping<T>();
        string sql = $"ALTER TABLE \"{mapping.TableName}\" RENAME TO \"{newTableName.Replace("\"", "\"\"")}\"";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Creates a view named after the SQLite name of <typeparamref name="T" />. The body of
    /// the view is the SQL produced by translating <paramref name="query" />. Issues
    /// <c>CREATE VIEW IF NOT EXISTS</c>, so calling this twice does not throw.
    /// </summary>
    /// <remarks>
    /// Pair the view with <see cref="SQLiteDatabase.ReadOnlyTable{T}" /> to query it. The view
    /// body is captured once at create time. Later changes to the lambda do not affect the view
    /// in the database.
    /// </remarks>
    public virtual int CreateView<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Expression<Func<IQueryable<T>>> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        string viewName = Database.TableMapping<T>().TableName;
        SQLTranslator translator = new(Database);
        SQLQuery sqlQuery = translator.Translate(query.Body);

        string body = SqlLiteralHelper.InlineParameters(sqlQuery.Sql, sqlQuery.Parameters, Database.Options);
        string sql = $"CREATE VIEW IF NOT EXISTS \"{viewName.Replace("\"", "\"\"")}\" AS{Environment.NewLine}{body}";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Drops the view named after the SQLite name of <typeparamref name="T" />.
    /// </summary>
    public virtual int DropView<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        return DropView(Database.TableMapping<T>().TableName);
    }

    /// <summary>
    /// Drops the view whose SQLite name matches <paramref name="viewName" />.
    /// </summary>
    public virtual int DropView(string viewName)
    {
        ArgumentException.ThrowIfNullOrEmpty(viewName);
        return Database.CreateCommand($"DROP VIEW IF EXISTS \"{viewName.Replace("\"", "\"\"")}\"", []).ExecuteNonQuery();
    }

    /// <summary>
    /// Returns <see langword="true" /> when a view exists for <typeparamref name="T" />.
    /// </summary>
    public virtual bool ViewExists<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        return ViewExists(Database.TableMapping<T>().TableName);
    }

    /// <summary>
    /// Returns <see langword="true" /> when a view with the given SQLite name exists.
    /// </summary>
    public virtual bool ViewExists(string viewName)
    {
        ArgumentException.ThrowIfNullOrEmpty(viewName);
        long? count = Database.ExecuteScalar<long?>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'view' AND name = @name",
            [new SQLiteParameter { Name = "@name", Value = viewName }]);
        return count > 0;
    }

    /// <summary>
    /// Lists the names of every user view in the database, ordered alphabetically.
    /// </summary>
    public virtual IReadOnlyList<string> ListViews()
    {
        return Database.Query<string>(
            "SELECT name FROM sqlite_master WHERE type = 'view' AND name NOT LIKE 'sqlite_%' ORDER BY name");
    }

    /// <summary>
    /// Creates a trigger on the table for <typeparamref name="T" />. The body and the
    /// optional <paramref name="when" /> predicate are raw SQL fragments. Use <c>NEW</c> and
    /// <c>OLD</c> to refer to the new and old rows. Issues
    /// <c>CREATE TRIGGER IF NOT EXISTS</c>.
    /// </summary>
    /// <param name="name">The trigger name.</param>
    /// <param name="timing">When the trigger fires, relative to the row change.</param>
    /// <param name="event">The row change that fires the trigger.</param>
    /// <param name="body">The SQL statement(s) the trigger runs. Multiple statements are
    /// separated by <c>;</c>.</param>
    /// <param name="when">An optional <c>WHEN</c> predicate. Only rows for which this
    /// expression is true fire the trigger body.</param>
    public virtual int CreateTrigger<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string name, SQLiteTriggerTiming timing, SQLiteTriggerEvent @event, string body, string? when = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(body);

        string tableName = Database.TableMapping<T>().TableName;
        string sql = SchemaSqlBuilder.BuildCreateTrigger(tableName, name, timing, @event, when, body, ifNotExists: true);
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Creates a trigger whose body is built from typed LINQ statements instead of a SQL string.
    /// Use the builder's <c>Old</c> and <c>New</c> rows to reference the changed row, and add one or
    /// more <c>Update</c>, <c>Insert</c>, or <c>Delete</c> statements. Issues <c>CREATE TRIGGER IF NOT EXISTS</c>.
    /// </summary>
    /// <param name="name">The trigger name.</param>
    /// <param name="timing">When the trigger fires, relative to the row change.</param>
    /// <param name="event">The row change that fires the trigger.</param>
    /// <param name="build">Builds the body and the optional <c>When</c> guard.</param>
    public virtual int CreateTrigger<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string name, SQLiteTriggerTiming timing, SQLiteTriggerEvent @event, Action<SQLiteTriggerBuilder<T>> build)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(build);

        SQLiteTriggerBuilder<T> builder = new(Database, Database.TableMapping<T>());
        build(builder);
        if (builder.Statements.Count == 0)
        {
            throw new ArgumentException("The trigger body must contain at least one Update, Insert, or Delete statement.", nameof(build));
        }

        string body = string.Join("; ", builder.Statements);
        return CreateTrigger<T>(name, timing, @event, body, builder.WhenSql);
    }

    /// <summary>
    /// Drops the trigger with the given SQLite name.
    /// </summary>
    public virtual int DropTrigger(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return Database.CreateCommand($"DROP TRIGGER IF EXISTS \"{name.Replace("\"", "\"\"")}\"", []).ExecuteNonQuery();
    }

    /// <summary>
    /// Compares the model for <typeparamref name="T" /> against the live database and reports any
    /// drift. The drift can be a missing table, missing or extra columns, mismatched column types, a
    /// primary-key or nullability difference, a missing index, or a missing foreign key. Returns the
    /// findings instead of throwing, so the caller decides what to do. Lets you catch schema drift at
    /// startup rather than at query time.
    /// </summary>
    public virtual SQLiteModelValidationResult ValidateModel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        return ValidateModel(typeof(T));
    }

    /// <summary>
    /// Compares the model for <paramref name="type" /> against the live database. See
    /// <see cref="ValidateModel{T}()" />.
    /// </summary>
    public virtual SQLiteModelValidationResult ValidateModel([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        TableMapping mapping = Database.TableMapping(type);
        return new SQLiteModelValidationResult(ModelValidator.Validate(Database, mapping));
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
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_9, "FTS5 virtual tables");
#endif
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
            sb.Append(IdentifierGuard.Quote(column.Name));
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
    /// Emits the <c>CREATE VIRTUAL TABLE ... USING rtree(...)</c> (or <c>rtree_i32</c>) statement
    /// for an R-Tree mapping. Override to change how the R-Tree table is created (for example,
    /// to add extra options).
    /// </summary>
    /// <returns>The total number of rows affected by the issued statements.</returns>
    protected virtual int CreateRTreeTable(TableMapping mapping)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_8_5, "R-Tree virtual tables");
#endif
        RTreeTableInfo rtree = mapping.RTree!;
        StringBuilder sb = new();
        sb.Append("CREATE VIRTUAL TABLE IF NOT EXISTS \"");
        sb.Append(mapping.TableName);
        sb.Append("\" USING ");
        sb.Append(rtree.Storage == SQLiteRTreeStorage.Int32 ? "rtree_i32" : "rtree");
        sb.Append('(');

        sb.Append(IdentifierGuard.Quote(rtree.RowIdColumnName));

        foreach (RTreeBoundsColumn bound in rtree.Bounds)
        {
            sb.Append(", ");
            sb.Append(IdentifierGuard.Quote(bound.ColumnName));
        }

#if SQLITE_FRAMEWORK_VERSION_AWARE
        if (rtree.Auxiliaries.Count > 0)
        {
            Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_24, "R-Tree auxiliary columns");
        }
#endif

        foreach (RTreeAuxiliaryColumn aux in rtree.Auxiliaries)
        {
            sb.Append(", +");
            sb.Append(IdentifierGuard.Quote(aux.ColumnName));
        }

        sb.Append(')');

        return Database.CreateCommand(sb.ToString(), []).ExecuteNonQuery();
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
            throw new InvalidOperationException($"FTS5 entity '{mapping.Type.Name}' targets '{sourceType.Name}' which has more than one primary key column. Set ContentRowIdColumn on [FullTextSearch] to choose the rowid column.");
        }

        throw new InvalidOperationException($"FTS5 entity '{mapping.Type.Name}' targets '{sourceType.Name}' but the source has no [Key] property. Mark the primary key with [Key] or set ContentRowIdColumn on [FullTextSearch].");
    }

    /// <summary>
    /// Yields the FTS5 sync trigger statements (insert, delete, update) that keep the FTS
    /// virtual table aligned with its external content table. Override to change the trigger
    /// shape, for example to add a <c>WHERE</c> clause or use partial triggers.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "ContentTable is referenced by user code via [FullTextSearch(ContentTable = typeof(...))], so its public properties are rooted by the user.")]
    protected virtual IEnumerable<string> BuildTriggerSql(FtsTableInfo fts, TableMapping mapping)
    {
        string ftsName = mapping.TableName;
        string sourceTable = ResolveContentTableName(fts);
        string sourceRowId = IdentifierGuard.Quote(ResolveContentRowIdColumn(fts, mapping));

        TableMapping sourceMapping = Database.TableMapping(fts.Attribute.ContentTable!);
        Dictionary<string, string> sourceColumnByProperty = sourceMapping.Columns
            .ToDictionary(c => c.PropertyInfo.Name, c => c.Name, StringComparer.Ordinal);

        string columnList = string.Join(", ", fts.IndexedColumns.Select(c => IdentifierGuard.Quote(c.Name)));
        string newValues = string.Join(", ", fts.IndexedColumns.Select(c => "new." + IdentifierGuard.Quote(sourceColumnByProperty[c.Property.Name])));
        string oldValues = string.Join(", ", fts.IndexedColumns.Select(c => "old." + IdentifierGuard.Quote(sourceColumnByProperty[c.Property.Name])));

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

    private int MigrateInPlace(TableMapping mapping, IReadOnlyList<(string Column, string ValueSql)> sets)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_35, "Migrate. Use MigrateByRebuild on older SQLite");
#endif

        if (mapping.IsFullTextSearch || mapping.IsRTree)
        {
            return CreateTable(mapping.Type);
        }

        if (!TableExists(mapping.TableName))
        {
            return CreateTable(mapping.Type);
        }

        int count = 0;
        List<PragmaTableInfo> liveInfo = Database.Pragmas.TableInfo(mapping.TableName).ToList();
        HashSet<string> liveColumns = liveInfo.Select(c => c.Name).ToHashSet();
        HashSet<string> computedColumns = mapping.ComputedColumns.Select(c => c.Column.Name).ToHashSet();
        HashSet<string> modelColumns = mapping.Columns.Select(c => c.Name)
            .Concat(mapping.ShadowColumns.Select(s => s.Name))
            .ToHashSet();

        int columnCount = liveColumns.Count;
        foreach (TableColumn column in mapping.Columns)
        {
            if (liveColumns.Contains(column.Name) || computedColumns.Contains(column.Name))
            {
                continue;
            }

            if (column.IsPrimaryKey || column.IsAutoIncrement || (!column.IsNullable && column.DefaultSql == null))
            {
                continue;
            }

            string columnSql = ColumnSql.GetCreateColumnSql(column, defaultOverride: null, emitForeignKey: column.DefaultSql == null);
            count += Database.CreateCommand($"ALTER TABLE \"{mapping.TableName}\" ADD COLUMN {columnSql}", []).ExecuteNonQuery();
            columnCount++;
        }

        string? createSql = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{mapping.TableName.Replace("'", "''")}'");
        foreach (string liveColumn in liveColumns)
        {
            if (modelColumns.Contains(liveColumn) || !IsAlterDroppable(mapping, liveInfo, createSql, liveColumn, columnCount))
            {
                continue;
            }

            count += Database.CreateCommand($"ALTER TABLE \"{mapping.TableName}\" DROP COLUMN \"{liveColumn.Replace("\"", "\"\"")}\"", []).ExecuteNonQuery();
            columnCount--;
        }

        string intended = SchemaSqlBuilder.BuildCreateTable(Database, mapping, mapping.TableName, ifNotExists: false);
        string? live = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{mapping.TableName.Replace("'", "''")}'");
        if (sets.Count > 0 || !string.Equals(StripWhitespace(intended), StripWhitespace(live!), StringComparison.Ordinal))
        {
            count += RebuildTable(mapping, sets);
        }

        count += ReconcileIndexes(mapping);
        count += ReconcileTriggers(mapping);
        return count;
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Querying built-in string rows keeps their public members reachable.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Querying built-in string rows keeps their public members reachable.")]
    private bool IsAlterDroppable(TableMapping mapping, List<PragmaTableInfo> liveInfo, string? createSql, string columnName, int columnCount)
    {
        if (columnCount <= 1 || createSql == null)
        {
            return false;
        }

        PragmaTableInfo info = liveInfo.First(c => c.Name == columnName);
        if (info.PrimaryKeyOrder > 0)
        {
            return false;
        }

        string quoted = "\"" + columnName.Replace("\"", "\"\"") + "\"";
        int occurrences = 0;
        for (int i = createSql.IndexOf(quoted, StringComparison.Ordinal); i >= 0; i = createSql.IndexOf(quoted, i + quoted.Length, StringComparison.Ordinal))
        {
            occurrences++;
        }

        if (occurrences != 1)
        {
            return false;
        }

        bool inForeignKey = Database.Query<Dictionary<string, object?>>($"PRAGMA foreign_key_list(\"{mapping.TableName.Replace("\"", "\"\"")}\")")
            .Any(row => row["from"] as string == columnName);
        if (inForeignKey)
        {
            return false;
        }

        return !Database.Query<string>($"SELECT sql FROM sqlite_master WHERE type = 'index' AND tbl_name = '{mapping.TableName.Replace("'", "''")}' AND sql IS NOT NULL")
            .Any(sql => sql.Contains(quoted, StringComparison.Ordinal));
    }

    private int MigrateCore(TableMapping mapping, IReadOnlyList<(string Column, string ValueSql)> sets)
    {
        if (mapping.IsFullTextSearch || mapping.IsRTree)
        {
            return CreateTable(mapping.Type);
        }

        if (!TableExists(mapping.TableName))
        {
            return CreateTable(mapping.Type);
        }

        int count = 0;
        string intended = SchemaSqlBuilder.BuildCreateTable(Database, mapping, mapping.TableName, ifNotExists: false);
        string? live = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{mapping.TableName.Replace("'", "''")}'");
        if (!string.Equals(intended, live, StringComparison.Ordinal) || sets.Count > 0)
        {
            count += RebuildTable(mapping, sets);
        }

        count += ReconcileIndexes(mapping);
        count += ReconcileTriggers(mapping);
        return count;
    }

    private string TranslateDefaultExpression(Expression<Func<object?>> defaultExpression)
    {
        return DefaultExpressionTranslator.Translate(Database, defaultExpression, nameof(defaultExpression));
    }

    private int RebuildTable(TableMapping mapping, IReadOnlyList<(string Column, string ValueSql)> sets)
    {
        string table = mapping.TableName;
        string temp = table + "__sqlitefw_migrate";

        HashSet<string> liveColumns = Database.Pragmas.TableInfo(table).Select(c => c.Name).ToHashSet();
        HashSet<string> setColumns = sets.Select(s => s.Column).ToHashSet();
        HashSet<string> computedColumns = mapping.ComputedColumns.Select(c => c.Column.Name).ToHashSet();

        EnsureNoUnfilledNotNull(mapping, table, liveColumns, computedColumns, setColumns);

        List<string> copyColumns = mapping.Columns
            .Where(c => !computedColumns.Contains(c.Name))
            .Select(c => c.Name)
            .Concat(mapping.ShadowColumns.Select(s => s.Name))
            .Where(name => liveColumns.Contains(name) && !setColumns.Contains(name))
            .ToList();

        List<string> insertColumns = copyColumns.Concat(sets.Select(s => s.Column)).Select(IdentifierGuard.Quote).ToList();
        List<string> selectExpressions = copyColumns.Select(IdentifierGuard.Quote).Concat(sets.Select(s => s.ValueSql)).ToList();

        IReadOnlyList<string> liveTriggers = Database.Query<string>(
            $"SELECT sql FROM sqlite_master WHERE type = 'trigger' AND tbl_name = '{table.Replace("'", "''")}' AND sql IS NOT NULL");

        long? autoIncrementSeq = ReadAutoIncrementSequence(mapping, table);

        long foreignKeys = Database.ExecuteScalar<long>("PRAGMA foreign_keys");
        Database.Execute("PRAGMA foreign_keys = OFF");
        bool foreignKeysEnforced = Database.ExecuteScalar<long>("PRAGMA foreign_keys") == 1;
        try
        {
            using SQLiteTransaction transaction = Database.BeginTransaction();

            List<SavedTable> dependents;
            if (foreignKeysEnforced)
            {
                Database.Execute("PRAGMA defer_foreign_keys = ON");
                dependents = EmptyReferencingTables(table, [], [table]);
            }
            else
            {
                dependents = [];
            }

            int count = Database.CreateCommand(SchemaSqlBuilder.BuildCreateTable(Database, mapping, temp, ifNotExists: false), []).ExecuteNonQuery();
            if (insertColumns.Count > 0)
            {
                Database.Execute($"INSERT INTO \"{temp}\" ({string.Join(", ", insertColumns)}) SELECT {string.Join(", ", selectExpressions)} FROM \"{table}\"");
            }
            Database.Execute($"DROP TABLE \"{table}\"");
            Database.Execute($"ALTER TABLE \"{temp}\" RENAME TO \"{table}\"");
            if (autoIncrementSeq.HasValue)
            {
                RestoreAutoIncrementSequence(table, autoIncrementSeq.Value);
            }
            foreach (string trigger in liveTriggers)
            {
                Database.Execute(trigger);
            }

            RestoreReferencingTables(dependents);
            transaction.Commit();
            return count;
        }
        finally
        {
            Database.Execute($"PRAGMA foreign_keys = {foreignKeys}");
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Querying built-in string and foreign key rows keeps their public members reachable.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Querying built-in string and foreign key rows keeps their public members reachable.")]
    private List<SavedTable> EmptyReferencingTables(string table, List<SavedTable> saved, HashSet<string> visited)
    {
        string escaped = table.Replace("'", "''");
        List<string> allTables = Database.Query<string>(
            $"SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\' AND name <> '{escaped}'");

        List<string> referencing = new();
        foreach (string candidate in allTables)
        {
            bool referencesTable = Database.Query<Dictionary<string, object?>>($"PRAGMA foreign_key_list(\"{candidate.Replace("\"", "\"\"")}\")")
                .Any(row => row["table"] as string == table);
            if (referencesTable)
            {
                referencing.Add(candidate);
            }
        }

        foreach (string child in referencing)
        {
            if (!visited.Add(child))
            {
                continue;
            }

            EmptyReferencingTables(child, saved, visited);

            string childEscaped = child.Replace("'", "''");
            List<Dictionary<string, object?>> triggers = Database.Query<Dictionary<string, object?>>(
                $"SELECT name, sql FROM sqlite_master WHERE type = 'trigger' AND tbl_name = '{childEscaped}' AND sql IS NOT NULL");

            List<string> triggerSql = new();
            foreach (Dictionary<string, object?> trigger in triggers)
            {
                triggerSql.Add((string)trigger["sql"]!);
                Database.Execute($"DROP TRIGGER \"{((string)trigger["name"]!).Replace("\"", "\"\"")}\"");
            }

            Database.Execute($"CREATE TABLE \"{child}__sqlitefw_hold\" AS SELECT * FROM \"{child}\"");
            Database.Execute($"DELETE FROM \"{child}\"");
            saved.Add(new SavedTable { Name = child, Triggers = triggerSql });
        }

        return saved;
    }

    private void RestoreReferencingTables(List<SavedTable> saved)
    {
        for (int i = saved.Count - 1; i >= 0; i--)
        {
            SavedTable child = saved[i];
            Database.Execute($"INSERT INTO \"{child.Name}\" SELECT * FROM \"{child.Name}__sqlitefw_hold\"");
            Database.Execute($"DROP TABLE \"{child.Name}__sqlitefw_hold\"");
            foreach (string trigger in child.Triggers)
            {
                Database.Execute(trigger);
            }
        }
    }

    private long? ReadAutoIncrementSequence(TableMapping mapping, string table)
    {
        if (!mapping.Columns.Any(c => c.IsPrimaryKey && c.IsAutoIncrement))
        {
            return null;
        }

        bool sequenceExists = Database.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'sqlite_sequence'") > 0;
        if (!sequenceExists)
        {
            return null;
        }

        List<long> seq = Database.Query<long>($"SELECT seq FROM sqlite_sequence WHERE name = '{table.Replace("'", "''")}'");
        return seq.Count > 0 ? seq[0] : null;
    }

    private void RestoreAutoIncrementSequence(string table, long seq)
    {
        string escaped = table.Replace("'", "''");
        Database.Execute($"DELETE FROM sqlite_sequence WHERE name = '{escaped}'");
        Database.Execute($"INSERT INTO sqlite_sequence (name, seq) VALUES ('{escaped}', {seq})");
    }

    private void EnsureNoUnfilledNotNull(TableMapping mapping, string table, HashSet<string> liveColumns, HashSet<string> computedColumns, HashSet<string> setColumns)
    {
        List<(string Name, bool Nullable, bool HasDefault)> required = mapping.Columns
            .Where(c => !computedColumns.Contains(c.Name))
            .Select(c => (c.Name, c.IsNullable, c.DefaultSql != null))
            .Concat(mapping.ShadowColumns.Select(s => (s.Name, s.IsNullable, s.DefaultSql != null)))
            .Where(c => !c.Item2 && !c.Item3 && !liveColumns.Contains(c.Item1) && !setColumns.Contains(c.Item1))
            .ToList();

        if (required.Count == 0)
        {
            return;
        }

        if (Database.ExecuteScalar<long>($"SELECT COUNT(*) FROM \"{table}\"") == 0)
        {
            return;
        }

        (string name, _, _) = required[0];
        throw new InvalidOperationException(
            $"Cannot migrate table '{table}'. Column '{name}' is new and NOT NULL with no default, but the table has rows. " +
            "Give it a default in OnModelCreating, set a value with Migrate(m => m.Set(...)), or make it nullable.");
    }

    private int ReconcileIndexes(TableMapping mapping)
    {
        int count = 0;
        List<(string Name, string Sql)> declared = SchemaSqlBuilder.BuildIndexes(mapping, mapping.TableName, ifNotExists: false);
        HashSet<string> declaredNames = declared.Select(d => d.Name).ToHashSet();

        foreach (PragmaIndexList live in Database.Pragmas.IndexList(mapping.TableName).ToList())
        {
            if (live.Origin == "c" && !declaredNames.Contains(live.Name))
            {
                count += Database.Execute($"DROP INDEX IF EXISTS {IdentifierGuard.Quote(live.Name)}");
            }
        }

        foreach ((string name, string sql) in declared)
        {
            string? liveSql = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'index' AND name = '{name.Replace("'", "''")}'");
            if (!string.Equals(sql, liveSql, StringComparison.Ordinal))
            {
                Database.Execute($"DROP INDEX IF EXISTS {IdentifierGuard.Quote(name)}");
                count += Database.CreateCommand(sql, []).ExecuteNonQuery();
            }
        }

        return count;
    }

    private int ReconcileTriggers(TableMapping mapping)
    {
        int count = 0;
        foreach ((string name, string sql) in SchemaSqlBuilder.BuildTriggers(mapping, mapping.TableName, ifNotExists: false))
        {
            string? liveSql = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = '{name.Replace("'", "''")}'");
            if (!string.Equals(sql, liveSql, StringComparison.Ordinal))
            {
                Database.Execute($"DROP TRIGGER IF EXISTS {IdentifierGuard.Quote(name)}");
                count += Database.CreateCommand(sql, []).ExecuteNonQuery();
            }
        }

        return count;
    }

    private static string StripWhitespace(string value)
    {
        StringBuilder builder = new(value.Length);
        bool inLiteral = false;
        foreach (char c in value)
        {
            if (c == '\'')
            {
                inLiteral = !inLiteral;
                builder.Append(c);
                continue;
            }

            if (inLiteral || !char.IsWhiteSpace(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string ResolvePropertyName<T>(Expression<Func<T, object?>> property)
    {
        Expression body = property.Body;
        if (body is UnaryExpression unary)
        {
            body = unary.Operand;
        }

        if (body is not MemberExpression member)
        {
            throw new InvalidOperationException("Expected a property access expression on the entity, like b => b.Pages.");
        }

        return member.Member.Name;
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
