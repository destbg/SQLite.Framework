namespace SQLite.Framework.Extensions;

/// <summary>
/// Async wrappers for <see cref="SQLiteSchema" /> and <see cref="SQLiteTableBuilder{T}" />.
/// Each method takes the connection lock and runs the sync version inside it.
/// </summary>
public static class AsyncSchemaExtensions
{
    /// <summary>
    /// Creates the table for <paramref name="type" /> if it does not exist.
    /// </summary>
    public static Task<int> CreateTableAsync(this SQLiteSchema schema, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.CreateTable(type);
        }, ct);
    }

    /// <summary>
    /// Creates the table for <typeparamref name="T" /> if it does not exist.
    /// </summary>
    public static Task<int> CreateTableAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.CreateTable<T>();
        }, ct);
    }

    /// <summary>
    /// Drops the table for <typeparamref name="T" /> if it exists.
    /// </summary>
    public static Task<int> DropTableAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.DropTable<T>();
        }, ct);
    }

    /// <summary>
    /// Drops the table whose SQLite name matches <paramref name="tableName" />.
    /// </summary>
    public static Task<int> DropTableAsync(this SQLiteSchema schema, string tableName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.DropTable(tableName);
        }, ct);
    }

    /// <summary>
    /// Creates an index over a single column.
    /// </summary>
    public static Task<int> CreateIndexAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, Expression<Func<T, object?>> column, string? name = null, bool unique = false, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.CreateIndex(column, name, unique);
        }, ct);
    }

    /// <summary>
    /// Drops an index by name if it exists.
    /// </summary>
    public static Task<int> DropIndexAsync(this SQLiteSchema schema, string indexName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.DropIndex(indexName);
        }, ct);
    }

    /// <summary>
    /// Returns whether the table for <typeparamref name="T" /> exists.
    /// </summary>
    public static Task<bool> TableExistsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.TableExists<T>();
        }, ct);
    }

    /// <summary>
    /// Returns whether a table with the given SQLite name exists.
    /// </summary>
    public static Task<bool> TableExistsAsync(this SQLiteSchema schema, string tableName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.TableExists(tableName);
        }, ct);
    }

    /// <summary>
    /// Compares the model for <typeparamref name="T" /> against the live database and reports any
    /// drift. See <see cref="SQLiteSchema.ValidateModel{T}()" />.
    /// </summary>
    public static Task<SQLiteModelValidationResult> ValidateModelAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.ValidateModel<T>();
        }, ct);
    }

    /// <summary>
    /// Returns whether an index with the given name exists.
    /// </summary>
    public static Task<bool> IndexExistsAsync(this SQLiteSchema schema, string indexName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.IndexExists(indexName);
        }, ct);
    }

    /// <summary>
    /// Returns whether a column with the given SQLite name exists on the table for <typeparamref name="T" />.
    /// </summary>
    public static Task<bool> ColumnExistsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string columnName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.ColumnExists<T>(columnName);
        }, ct);
    }

    /// <summary>
    /// Lists every user table in the database.
    /// </summary>
    public static Task<IReadOnlyList<string>> ListTablesAsync(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.ListTables();
        }, ct);
    }

    /// <summary>
    /// Lists every index in the database.
    /// </summary>
    public static Task<IReadOnlyList<string>> ListIndexesAsync(this SQLiteSchema schema, string? tableName = null, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.ListIndexes(tableName);
        }, ct);
    }

    /// <summary>
    /// Lists every column on the table for <typeparamref name="T" />.
    /// </summary>
    public static Task<IReadOnlyList<SchemaColumnInfo>> ListColumnsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.ListColumns<T>();
        }, ct);
    }

    /// <summary>
    /// Adds the column for the property named <paramref name="propertyName" /> on <typeparamref name="T" />.
    /// </summary>
    public static Task<int> AddColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string propertyName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.AddColumn<T>(propertyName);
        }, ct);
    }

    /// <summary>
    /// Adds the column for the property named <paramref name="propertyName" /> on <typeparamref name="T" />.
    /// The framework writes <paramref name="defaultValue" /> as the SQL <c>DEFAULT</c> clause. SQLite
    /// needs this when you add a <c>NOT NULL</c> column to a table that already has rows.
    /// </summary>
    public static Task<int> AddColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string propertyName, object? defaultValue, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.AddColumn<T>(propertyName, defaultValue);
        }, ct);
    }

    /// <summary>
    /// Adds the column selected by <paramref name="property" /> on <typeparamref name="T" />.
    /// </summary>
    public static Task<int> AddColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, Expression<Func<T, object?>> property, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.AddColumn(property);
        }, ct);
    }

    /// <summary>
    /// Adds the column selected by <paramref name="property" /> on <typeparamref name="T" />.
    /// The framework writes <paramref name="defaultValue" /> as the SQL <c>DEFAULT</c> clause. SQLite
    /// needs this when you add a <c>NOT NULL</c> column to a table that already has rows.
    /// </summary>
    public static Task<int> AddColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, Expression<Func<T, object?>> property, object? defaultValue, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.AddColumn(property, defaultValue);
        }, ct);
    }

    /// <summary>
    /// Adds the column for the property named <paramref name="propertyName" /> on <typeparamref name="T" />.
    /// The body of <paramref name="defaultExpression" /> is translated to SQL and written into the
    /// <c>DEFAULT</c> clause. Requires SQLite 3.31.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android31.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios14.0")]
#endif
    public static Task<int> AddColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string propertyName, Expression<Func<object?>> defaultExpression, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.AddColumn<T>(propertyName, defaultExpression);
        }, ct);
    }

    /// <summary>
    /// Adds the column selected by <paramref name="property" /> on <typeparamref name="T" />. The
    /// body of <paramref name="defaultExpression" /> is translated to SQL and written into the
    /// <c>DEFAULT</c> clause. Requires SQLite 3.31.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android31.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios14.0")]
#endif
    public static Task<int> AddColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, Expression<Func<T, object?>> property, Expression<Func<object?>> defaultExpression, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.AddColumn(property, defaultExpression);
        }, ct);
    }

    /// <summary>
    /// Renames a column on the table for <typeparamref name="T" />. Requires SQLite 3.25.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android30.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios13.0")]
#endif
    public static Task<int> RenameColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string fromColumn, string toColumn, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.RenameColumn<T>(fromColumn, toColumn);
        }, ct);
    }

    /// <summary>
    /// Drops a column on the table for <typeparamref name="T" />. Requires SQLite 3.35.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public static Task<int> DropColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string columnName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.DropColumn<T>(columnName);
        }, ct);
    }

    /// <summary>
    /// Renames the table for <typeparamref name="T" /> in the database.
    /// </summary>
    public static Task<int> RenameTableAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string newTableName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.RenameTable<T>(newTableName);
        }, ct);
    }

    /// <summary>
    /// Creates a view named after the SQLite name of <typeparamref name="T" />.
    /// </summary>
    public static Task<int> CreateViewAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, Expression<Func<IQueryable<T>>> query, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.CreateView<T>(query);
        }, ct);
    }

    /// <summary>
    /// Drops the view named after the SQLite name of <typeparamref name="T" />.
    /// </summary>
    public static Task<int> DropViewAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.DropView<T>();
        }, ct);
    }

    /// <summary>
    /// Drops the view whose SQLite name matches <paramref name="viewName" />.
    /// </summary>
    public static Task<int> DropViewAsync(this SQLiteSchema schema, string viewName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.DropView(viewName);
        }, ct);
    }

    /// <summary>
    /// Returns <see langword="true" /> when a view exists for <typeparamref name="T" />.
    /// </summary>
    public static Task<bool> ViewExistsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.ReadLockAsync(ct);
            return schema.ViewExists<T>();
        }, ct);
    }

    /// <summary>
    /// Lists the names of every user view in the database.
    /// </summary>
    public static Task<IReadOnlyList<string>> ListViewsAsync(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.ReadLockAsync(ct);
            return schema.ListViews();
        }, ct);
    }

    /// <summary>
    /// Creates a trigger on the table for <typeparamref name="T" />.
    /// </summary>
    public static Task<int> CreateTriggerAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string name, SQLiteTriggerTiming timing, SQLiteTriggerEvent @event, string body, string? when = null, bool forEachRow = true, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.CreateTrigger<T>(name, timing, @event, body, when, forEachRow);
        }, ct);
    }

    /// <summary>
    /// Creates a trigger on the table for <typeparamref name="T" /> from a LINQ-typed body. See
    /// <see cref="SQLiteSchema.CreateTrigger{T}(string, SQLiteTriggerTiming, SQLiteTriggerEvent, Action{SQLiteTriggerBuilder{T}}, bool)" />.
    /// </summary>
    public static Task<int> CreateTriggerAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string name, SQLiteTriggerTiming timing, SQLiteTriggerEvent @event, Action<SQLiteTriggerBuilder<T>> build, bool forEachRow = true, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.CreateTrigger<T>(name, timing, @event, build, forEachRow);
        }, ct);
    }

    /// <summary>
    /// Drops the trigger with the given SQLite name.
    /// </summary>
    public static Task<int> DropTriggerAsync(this SQLiteSchema schema, string name, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await schema.Database.LockAsync(ct);
            return schema.DropTrigger(name);
        }, ct);
    }

    /// <summary>
    /// Issues the <c>CREATE TABLE IF NOT EXISTS</c> built up by the fluent builder, plus its indexes.
    /// </summary>
    public static Task<int> CreateTableAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableBuilder<T> builder, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await builder.Database.LockAsync(ct);
            return builder.CreateTable();
        }, ct);
    }
}
