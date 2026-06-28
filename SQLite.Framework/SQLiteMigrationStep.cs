namespace SQLite.Framework;

/// <summary>
/// Declares the work one migration version performs. Reach an instance through the callback passed
/// to <see cref="SQLiteMigrationRunner.Version" />. Use <see cref="CreateTable{T}" /> for a new
/// table and <see cref="TableChanged{T}" /> to reconcile an existing one to the current model, plus
/// the explicit methods for renames, drops, and raw SQL that a reconcile cannot work out on its own.
/// </summary>
/// <remarks>
/// Within a single run the runner does not apply these in the order written. It applies every
/// rename first, then one reconcile per table, then drops and raw SQL. So a raw SQL data step reads
/// the final shape of the table, not an in-between shape. To move data out of a column you are
/// removing, keep the old column on the model while you copy it, then remove it in a later version.
/// </remarks>
public sealed class SQLiteMigrationStep
{
    private readonly SQLiteDatabase database;
    private readonly List<MigrationOperation> operations = [];

    internal SQLiteMigrationStep(SQLiteDatabase database)
    {
        this.database = database;
    }

    internal IReadOnlyList<MigrationOperation> Operations => operations;

    /// <summary>
    /// Creates the table for <typeparamref name="T" /> from the model,
    /// with its declared indexes and triggers, if it does not already exist.
    /// </summary>
    public SQLiteMigrationStep CreateTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        TableMapping mapping = database.TableMapping<T>();
        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.CreateTable,
            Description = $"create \"{mapping.TableName}\"",
            Mapping = mapping,
        });
        return this;
    }

    /// <summary>
    /// Reconciles the table for <typeparamref name="T" /> to the current model. New columns are
    /// added, dropped columns are removed, and indexes and triggers are brought in line. Pass
    /// <paramref name="fill" /> to give new <c>NOT NULL</c> columns a value for existing rows. The
    /// runner unions the fills from every pending version before it reconciles, so a column added in
    /// a later version does not make an earlier version throw. By default the runner makes the change
    /// in place where it can and falls back to a rebuild otherwise. Set <paramref name="rebuild" /> to
    /// always rebuild, which works on any SQLite version.
    /// </summary>
    /// <param name="fill">An optional callback that sets values for columns while reconciling.</param>
    /// <param name="rebuild">Whether to always rebuild the table instead of trying in-place changes first.</param>
    public SQLiteMigrationStep TableChanged<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>>? fill = null, bool rebuild = false)
    {
        TableMapping mapping = database.TableMapping<T>();
        SQLiteMigrationBuilder<T> builder = new(database, mapping);
        fill?.Invoke(builder);

        string detail = rebuild ? " by rebuild" : string.Empty;
        string values = builder.Sets.Count > 0 ? $" with {builder.Sets.Count} value(s)" : string.Empty;
        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.Reconcile,
            Description = $"reconcile \"{mapping.TableName}\"{detail}{values}",
            Mapping = mapping,
            Sets = builder.Sets,
            Rebuild = rebuild,
        });
        return this;
    }

    /// <summary>
    /// Renames the column <paramref name="fromColumn" /> to <paramref name="toColumn" /> on the table
    /// for <typeparamref name="T" />. Both names are SQLite column names. A reconcile cannot tell a
    /// rename from a drop plus an add, so use this when you rename a column to keep its data.
    /// </summary>
    public SQLiteMigrationStep RenameColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string fromColumn, string toColumn)
    {
        ArgumentException.ThrowIfNullOrEmpty(fromColumn);
        ArgumentException.ThrowIfNullOrEmpty(toColumn);

        TableMapping mapping = database.TableMapping<T>();
        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.RenameColumn,
            Description = $"rename column \"{fromColumn}\" to \"{toColumn}\" on \"{mapping.TableName}\"",
            Mapping = mapping,
            FromColumn = fromColumn,
            ToColumn = toColumn,
        });
        return this;
    }

    /// <summary>
    /// Drops the column with the given SQLite name from the table for <typeparamref name="T" />.
    /// </summary>
    public SQLiteMigrationStep DropColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string columnName)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnName);

        TableMapping mapping = database.TableMapping<T>();
        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.DropColumn,
            Description = $"drop column \"{columnName}\" on \"{mapping.TableName}\"",
            Mapping = mapping,
            ColumnName = columnName,
        });
        return this;
    }

    /// <summary>
    /// Drops the table for <typeparamref name="T" /> if it exists. For an FTS5 table with sync
    /// triggers, the triggers are dropped too, the same as <see cref="SQLiteSchema.DropTable{T}()" />.
    /// </summary>
    public SQLiteMigrationStep DropTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        TableMapping mapping = database.TableMapping<T>();
        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.DropTable,
            Description = $"drop table \"{mapping.TableName}\"",
            Mapping = mapping,
            TableName = mapping.TableName,
        });
        return this;
    }

    /// <summary>
    /// Drops the table with the given SQLite name if it exists.
    /// </summary>
    public SQLiteMigrationStep DropTable(string tableName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tableName);

        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.DropTable,
            Description = $"drop table \"{tableName}\"",
            TableName = tableName,
        });
        return this;
    }

    /// <summary>
    /// Runs a raw SQL statement. Use this for data fixes and for changes the typed methods do not
    /// cover. The statement runs against the final shape of the tables, after the reconcile.
    /// </summary>
    public SQLiteMigrationStep Sql(string sql)
    {
        ArgumentException.ThrowIfNullOrEmpty(sql);

        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.RawSql,
            Description = "run SQL",
            Sql = sql,
        });
        return this;
    }
}
