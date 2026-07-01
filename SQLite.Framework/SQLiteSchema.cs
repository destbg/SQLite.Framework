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
                    $"FTS5 entity '{mapping.Type.Name}' does not support computed columns, checks or indexes declared on the model. Remove them.");
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
    /// Returns a runner for ordered, versioned migrations. Declare each schema version on it, then
    /// apply them. The runner brings the database up to the current model, records the version in
    /// <c>PRAGMA user_version</c> and skips versions it has already applied.
    /// <para>
    /// A version that adds a <c>NOT NULL</c> column in place needs SQLite 3.35.0 for <c>DROP COLUMN</c>.
    /// Pass <c>rebuild: true</c> to a step's <see cref="SQLiteMigrationStep.TableChanged{T}" /> to use a
    /// rebuild that works on any SQLite version.
    /// </para>
    /// <para>
    /// Limitations. A whole run happens in one transaction, so a rebuild runs inside it. A computed
    /// column in a referencing table is not preserved when the referenced table is rebuilt inside a
    /// transaction, because the rebuild moves the rows of every referencing table out and back and drops
    /// and recreates their triggers, so those triggers do not fire during the restore.
    /// </para>
    /// </summary>
    public virtual SQLiteMigrationRunner Migrations()
    {
        return new SQLiteMigrationRunner(this);
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
    /// <typeparamref name="T" />. The type, nullability and primary-key flags come from the
    /// entity mapping. Pass <paramref name="defaultValue" /> to emit a <c>DEFAULT</c> clause. SQLite
    /// needs this when you add a <c>NOT NULL</c> column to a table that already has rows.
    /// </summary>
    /// <param name="propertyName">Property name on the entity to add.</param>
    /// <param name="defaultValue">Optional default value. The framework writes it as the SQL
    /// <c>DEFAULT</c> clause and SQLite uses it to backfill existing rows. Supported types are
    /// numbers, strings and <see cref="bool" />. SQLite does not let you use parameters inside
    /// DDL statements like <c>ALTER TABLE</c>, so the value is written straight into the SQL text.
    /// Single quotes inside strings are doubled, so a value with quotes in it cannot escape from
    /// the string and run other SQL.</param>
    public virtual int AddColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string propertyName, object? defaultValue = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        TableMapping mapping = Database.TableMapping<T>();
        TableColumn? column = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' is not mapped on {typeof(T).Name}.");

        if (TryAddComputedColumn(mapping, column, propertyName) is { } computedCount)
        {
            return computedCount;
        }

        if (column.IsPrimaryKey)
        {
            throw new InvalidOperationException(
                $"Cannot add the primary key column '{propertyName}' to the existing table '{mapping.TableName}'. " +
                "SQLite does not allow ALTER TABLE ADD COLUMN to add a PRIMARY KEY or AUTOINCREMENT column. " +
                "Recreate the table with the new schema instead.");
        }

        if (column.ForeignKey != null && defaultValue != null)
        {
            throw new InvalidOperationException(
                $"Cannot add the foreign key column '{propertyName}' to the existing table '{mapping.TableName}' with a non-null default value. " +
                "SQLite requires a column added with a REFERENCES clause to default to NULL. " +
                "Add the column with a null default or recreate the table with the new schema instead.");
        }

        string? defaultOverride = defaultValue == null ? null : SqlLiteralHelper.FormatLiteral(defaultValue, Database.Options);
        string sql = $"ALTER TABLE \"{mapping.TableName}\" ADD COLUMN {CommonHelpers.GetCreateColumnSql(column, defaultOverride: defaultOverride, emitForeignKey: true)}";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    /// <summary>
    /// Adds the column selected by <paramref name="property" /> to the table for
    /// <typeparamref name="T" />. The type, nullability and primary-key flags come from the
    /// entity mapping. Pass <paramref name="defaultValue" /> to emit a <c>DEFAULT</c> clause. SQLite
    /// needs this when you add a <c>NOT NULL</c> column to a table that already has rows.
    /// </summary>
    /// <param name="property">Property selector on the entity, like <c>b =&gt; b.Pages</c>.</param>
    /// <param name="defaultValue">Optional default value. The framework writes it as the SQL
    /// <c>DEFAULT</c> clause and SQLite uses it to backfill existing rows. Supported types are
    /// numbers, strings and <see cref="bool" />. SQLite does not let you use parameters inside
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
    /// contain a subquery and must only call deterministic functions. Requires SQLite 3.31.0 or newer.
    /// </summary>
    /// <param name="propertyName">Property name on the entity to add.</param>
    /// <param name="defaultExpression">A parameterless lambda that produces the default value. The
    /// framework translates the body to SQL and inlines any constants as SQL literals. Parameters
    /// are not allowed because SQLite does not accept placeholders in DDL.</param>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
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

        if (TryAddComputedColumn(mapping, column, propertyName) is { } computedCount)
        {
            return computedCount;
        }

        if (column.IsPrimaryKey)
        {
            throw new InvalidOperationException(
                $"Cannot add the primary key column '{propertyName}' to the existing table '{mapping.TableName}'. " +
                "SQLite does not allow ALTER TABLE ADD COLUMN to add a PRIMARY KEY or AUTOINCREMENT column. " +
                "Recreate the table with the new schema instead.");
        }

        if (column.ForeignKey != null)
        {
            throw new InvalidOperationException(
                $"Cannot add the foreign key column '{propertyName}' to the existing table '{mapping.TableName}' with a default expression. " +
                "SQLite requires a column added with a REFERENCES clause to default to NULL. " +
                "Recreate the table with the new schema instead.");
        }

        string defaultSql = TranslateDefaultExpression(defaultExpression);
        string sql = $"ALTER TABLE \"{mapping.TableName}\" ADD COLUMN {CommonHelpers.GetCreateColumnSql(column, defaultOverride: $"({defaultSql})", emitForeignKey: true)}";
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
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios13.0")]
#endif
    public virtual int RenameColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string fromColumn, string toColumn)
    {
        ArgumentException.ThrowIfNullOrEmpty(fromColumn);
        ArgumentException.ThrowIfNullOrEmpty(toColumn);
        return RenameColumnCore(Database.TableMapping<T>().TableName, fromColumn, toColumn);
    }

    /// <summary>
    /// Drops the column with the given SQLite name from the table for <typeparamref name="T" />.
    /// Requires SQLite 3.35.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public virtual int DropColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string columnName)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnName);
        return DropColumnCore(Database.TableMapping<T>().TableName, columnName);
    }

    /// <summary>
    /// Returns a fluent builder for creating the table for <typeparamref name="T" /> together
    /// with computed columns, CHECK constraints and indexes. Call <c>.Create()</c> when done
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
    /// Use the builder's <c>Old</c> and <c>New</c> rows to reference the changed row and add one or
    /// more <c>Update</c>, <c>Insert</c> or <c>Delete</c> statements. Issues <c>CREATE TRIGGER IF NOT EXISTS</c>.
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
            throw new ArgumentException("The trigger body must contain at least one Update, Insert or Delete statement.", nameof(build));
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
    /// primary-key or nullability difference, a missing index or a missing foreign key. Returns the
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

    internal int RenameColumnCore(string tableName, string fromColumn, string toColumn)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_25, "ALTER TABLE RENAME COLUMN");
#endif
        string sql = $"ALTER TABLE \"{tableName}\" RENAME COLUMN \"{fromColumn.Replace("\"", "\"\"")}\" TO \"{toColumn.Replace("\"", "\"\"")}\"";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }

    internal int DropColumnCore(string tableName, string columnName)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_35, "ALTER TABLE DROP COLUMN");
#endif
        string sql = $"ALTER TABLE \"{tableName}\" DROP COLUMN \"{columnName.Replace("\"", "\"\"")}\"";
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
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "ContentTable type is rooted by user code.")]
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
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "ContentTable type is rooted by user code.")]
    protected virtual string ResolveContentRowIdColumn(FtsTableInfo fts, TableMapping mapping)
    {
        Type sourceType = fts.Attribute.ContentTable!;
        TableMapping sourceMapping = Database.TableMapping(sourceType);

        if (!string.IsNullOrEmpty(fts.Attribute.ContentRowIdColumn))
        {
            string configured = fts.Attribute.ContentRowIdColumn!;
            TableColumn? mapped = sourceMapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == configured)
                ?? sourceMapping.Columns.FirstOrDefault(c => c.Name == configured);
            return mapped?.Name ?? configured;
        }

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
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "ContentTable type is rooted by user code.")]
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

    private int? TryAddComputedColumn(TableMapping mapping, TableColumn column, string propertyName)
    {
        ComputedColumnSpec? computed = mapping.ComputedColumns.FirstOrDefault(c => c.Column.Name == column.Name);
        if (computed == null)
        {
            return null;
        }

        if (computed.Stored)
        {
            throw new InvalidOperationException(
                $"Cannot add the stored computed column '{propertyName}' to the existing table '{mapping.TableName}'. " +
                "SQLite only allows adding a VIRTUAL generated column with ALTER TABLE ADD COLUMN. " +
                "Recreate the table with the new schema instead.");
        }

        string computedSql =
            $"ALTER TABLE \"{mapping.TableName}\" ADD COLUMN {IdentifierGuard.Quote(column.Name)} " +
            $"{column.ColumnType.ToString().ToUpperInvariant()} GENERATED ALWAYS AS ({computed.ExpressionSql}) VIRTUAL";
        return Database.CreateCommand(computedSql, []).ExecuteNonQuery();
    }

    private string TranslateDefaultExpression(Expression<Func<object?>> defaultExpression)
    {
        return CommonHelpers.Translate(Database, defaultExpression, nameof(defaultExpression));
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
