namespace SQLite.Framework;

/// <summary>
/// Per-table schema operations for one entity. Reach an instance with
/// <see cref="SQLiteSchema.Table{T}" /> or <see cref="SQLiteTable{T}.Schema" />. Every method here
/// is a thin wrapper over the matching generic method on <see cref="SQLiteSchema" />, scoped to
/// <typeparamref name="T" /> so you do not repeat the type argument. Schema configuration (the
/// table name, key, columns, indexes, checks, computed columns, foreign keys, defaults, STRICT,
/// WITHOUT ROWID, and triggers) is declared in <see cref="SQLiteDatabase.OnModelCreating" />.
/// </summary>
public sealed class SQLiteTableSchema<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
{
    internal SQLiteTableSchema(SQLiteSchema schema)
    {
        Database = schema.Database;
    }

    internal SQLiteDatabase Database { get; }

    /// <summary>
    /// Creates the table if it does not exist, along with the indexes and triggers declared on the
    /// model. Returns the number of statements run.
    /// </summary>
    public int CreateTable()
    {
        return Database.Schema.CreateTable<T>();
    }

    /// <summary>
    /// Reconciles the live table with the model in place. Adds and drops columns with
    /// <c>ALTER TABLE</c> so referencing tables are never touched, and falls back to
    /// <see cref="MigrateByRebuild()" /> for changes <c>ALTER TABLE</c> cannot express. Needs SQLite
    /// 3.35.0. Returns the number of statements run.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public int Migrate()
    {
        return Database.Schema.Migrate<T>();
    }

    /// <summary>
    /// Reconciles the live table with the model, filling or overriding columns from the values declared
    /// with <paramref name="fill" />. A fill always rebuilds. Returns the number of statements run.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public int Migrate(Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill)
    {
        return Database.Schema.Migrate(fill);
    }

    /// <summary>
    /// Reconciles the live table with the model by rebuilding it. Creates it when missing, rebuilds it
    /// on drift while preserving rows, and reconciles indexes and triggers. Works on any SQLite version.
    /// Returns the number of statements run.
    /// </summary>
    public int MigrateByRebuild()
    {
        return Database.Schema.MigrateByRebuild<T>();
    }

    /// <summary>
    /// Reconciles the live table with the model by rebuilding it, filling or overriding columns from
    /// the values declared with <paramref name="fill" />. Returns the number of statements run.
    /// </summary>
    public int MigrateByRebuild(Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>> fill)
    {
        return Database.Schema.MigrateByRebuild(fill);
    }

    /// <summary>
    /// Drops the table if it exists.
    /// </summary>
    public int DropTable()
    {
        return Database.Schema.DropTable<T>();
    }

    /// <summary>
    /// Renames the table to <paramref name="newTableName" />.
    /// </summary>
    public int RenameTable(string newTableName)
    {
        return Database.Schema.RenameTable<T>(newTableName);
    }

    /// <summary>
    /// Returns whether the table exists.
    /// </summary>
    public bool TableExists()
    {
        return Database.Schema.TableExists<T>();
    }

    /// <summary>
    /// Returns whether a column with the given SQLite name exists on the table.
    /// </summary>
    public bool ColumnExists(string columnName)
    {
        return Database.Schema.ColumnExists<T>(columnName);
    }

    /// <summary>
    /// Lists every column on the table as it exists in the database.
    /// </summary>
    public IReadOnlyList<SchemaColumnInfo> ListColumns()
    {
        return Database.Schema.ListColumns<T>();
    }

    /// <summary>
    /// Compares the model against the live database and returns the differences without throwing.
    /// </summary>
    public SQLiteModelValidationResult ValidateModel()
    {
        return Database.Schema.ValidateModel<T>();
    }

    /// <summary>
    /// Adds the column for the property named <paramref name="propertyName" />, optionally with a
    /// literal <c>DEFAULT</c> to backfill existing rows.
    /// </summary>
    public int AddColumn(string propertyName, object? defaultValue = null)
    {
        return Database.Schema.AddColumn<T>(propertyName, defaultValue);
    }

    /// <summary>
    /// Adds the column for <paramref name="property" />, optionally with a literal <c>DEFAULT</c> to
    /// backfill existing rows.
    /// </summary>
    public int AddColumn(Expression<Func<T, object?>> property, object? defaultValue = null)
    {
        return Database.Schema.AddColumn(property, defaultValue);
    }

    /// <summary>
    /// Adds the column for the property named <paramref name="propertyName" /> with a translated SQL
    /// expression as its <c>DEFAULT</c>.
    /// </summary>
    public int AddColumn(string propertyName, Expression<Func<object?>> defaultExpression)
    {
        return Database.Schema.AddColumn<T>(propertyName, defaultExpression);
    }

    /// <summary>
    /// Adds the column for <paramref name="property" /> with a translated SQL expression as its
    /// <c>DEFAULT</c>.
    /// </summary>
    public int AddColumn(Expression<Func<T, object?>> property, Expression<Func<object?>> defaultExpression)
    {
        return Database.Schema.AddColumn(property, defaultExpression);
    }

    /// <summary>
    /// Renames the column <paramref name="fromColumn" /> to <paramref name="toColumn" /> using their
    /// SQLite names.
    /// </summary>
    public int RenameColumn(string fromColumn, string toColumn)
    {
        return Database.Schema.RenameColumn<T>(fromColumn, toColumn);
    }

    /// <summary>
    /// Drops the column with the given SQLite name.
    /// </summary>
    public int DropColumn(string columnName)
    {
        return Database.Schema.DropColumn<T>(columnName);
    }

    /// <summary>
    /// Creates an index over a single column. The index name defaults to
    /// <c>idx_{TableName}_{ColumnName}</c> when not supplied.
    /// </summary>
    public int CreateIndex(Expression<Func<T, object?>> column, string? name = null, bool unique = false)
    {
        return Database.Schema.CreateIndex(column, name, unique);
    }

    /// <summary>
    /// Creates a SQL view named after the entity from a LINQ expression.
    /// </summary>
    public int CreateView(Expression<Func<IQueryable<T>>> query)
    {
        return Database.Schema.CreateView(query);
    }

    /// <summary>
    /// Drops the view named after the entity if it exists.
    /// </summary>
    public int DropView()
    {
        return Database.Schema.DropView<T>();
    }

    /// <summary>
    /// Returns whether the view named after the entity exists.
    /// </summary>
    public bool ViewExists()
    {
        return Database.Schema.ViewExists<T>();
    }

    /// <summary>
    /// Creates a trigger on the table from a raw SQL body and an optional <c>WHEN</c> predicate.
    /// Issues <c>CREATE TRIGGER IF NOT EXISTS</c>.
    /// </summary>
    public int CreateTrigger(string name, SQLiteTriggerTiming timing, SQLiteTriggerEvent @event, string body, string? when = null)
    {
        return Database.Schema.CreateTrigger<T>(name, timing, @event, body, when);
    }

    /// <summary>
    /// Creates a trigger on the table whose body is built from typed LINQ statements. Issues
    /// <c>CREATE TRIGGER IF NOT EXISTS</c>.
    /// </summary>
    public int CreateTrigger(string name, SQLiteTriggerTiming timing, SQLiteTriggerEvent @event, Action<SQLiteTriggerBuilder<T>> build)
    {
        return Database.Schema.CreateTrigger(name, timing, @event, build);
    }
}
