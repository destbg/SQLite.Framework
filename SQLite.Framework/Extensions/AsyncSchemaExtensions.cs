namespace SQLite.Framework.Extensions;

/// <summary>
/// Async wrappers for <see cref="SQLiteSchema" /> and <see cref="SQLiteTableBuilder{T}" />. All
/// methods run the underlying sync work on a background thread.
/// </summary>
[ExcludeFromCodeCoverage]
public static class AsyncSchemaExtensions
{
    /// <summary>
    /// Creates the table for <typeparamref name="T" /> if it does not exist. Runs on a background thread.
    /// </summary>
    public static Task<int> CreateTableAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.CreateTable<T>, ct);
    }

    /// <summary>
    /// Drops the table for <typeparamref name="T" /> if it exists. Runs on a background thread.
    /// </summary>
    public static Task<int> DropTableAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.DropTable<T>, ct);
    }

    /// <summary>
    /// Drops the table whose SQLite name matches <paramref name="tableName" />. Runs on a background thread.
    /// </summary>
    public static Task<int> DropTableAsync(this SQLiteSchema schema, string tableName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.DropTable, tableName, ct);
    }

    /// <summary>
    /// Creates an index over a single column. Runs on a background thread.
    /// </summary>
    public static Task<int> CreateIndexAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, Expression<Func<T, object?>> column, string? name = null, bool unique = false, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => schema.CreateIndex(column, name, unique), ct);
    }

    /// <summary>
    /// Drops an index by name if it exists. Runs on a background thread.
    /// </summary>
    public static Task<int> DropIndexAsync(this SQLiteSchema schema, string indexName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.DropIndex, indexName, ct);
    }

    /// <summary>
    /// Returns whether the table for <typeparamref name="T" /> exists. Runs on a background thread.
    /// </summary>
    public static Task<bool> TableExistsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.TableExists<T>, ct);
    }

    /// <summary>
    /// Returns whether a table with the given SQLite name exists. Runs on a background thread.
    /// </summary>
    public static Task<bool> TableExistsAsync(this SQLiteSchema schema, string tableName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.TableExists, tableName, ct);
    }

    /// <summary>
    /// Returns whether an index with the given name exists. Runs on a background thread.
    /// </summary>
    public static Task<bool> IndexExistsAsync(this SQLiteSchema schema, string indexName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.IndexExists, indexName, ct);
    }

    /// <summary>
    /// Returns whether a column with the given SQLite name exists on the table for
    /// <typeparamref name="T" />. Runs on a background thread.
    /// </summary>
    public static Task<bool> ColumnExistsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string columnName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.ColumnExists<T>, columnName, ct);
    }

    /// <summary>
    /// Lists every user table in the database. Runs on a background thread.
    /// </summary>
    public static Task<IReadOnlyList<string>> ListTablesAsync(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.ListTables, ct);
    }

    /// <summary>
    /// Lists every index in the database. Runs on a background thread.
    /// </summary>
    public static Task<IReadOnlyList<string>> ListIndexesAsync(this SQLiteSchema schema, string? tableName = null, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => schema.ListIndexes(tableName), ct);
    }

    /// <summary>
    /// Lists every column on the table for <typeparamref name="T" />. Runs on a background thread.
    /// </summary>
    public static Task<IReadOnlyList<SchemaColumnInfo>> ListColumnsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.ListColumns<T>, ct);
    }

    /// <summary>
    /// Adds the column for the property named <paramref name="propertyName" /> on
    /// <typeparamref name="T" />. Runs on a background thread.
    /// </summary>
    public static Task<int> AddColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string propertyName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.AddColumn<T>, propertyName, ct);
    }

    /// <summary>
    /// Renames a column on the table for <typeparamref name="T" />. Runs on a background thread.
    /// </summary>
    public static Task<int> RenameColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string fromColumn, string toColumn, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.RenameColumn<T>, fromColumn, toColumn, ct);
    }

    /// <summary>
    /// Drops a column on the table for <typeparamref name="T" />. Runs on a background thread.
    /// </summary>
    public static Task<int> DropColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string columnName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.DropColumn<T>, columnName, ct);
    }

    /// <summary>
    /// Renames the table for <typeparamref name="T" /> in the database. Runs on a background thread.
    /// </summary>
    public static Task<int> RenameTableAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteSchema schema, string newTableName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(schema.RenameTable<T>, newTableName, ct);
    }

    /// <summary>
    /// Issues the <c>CREATE TABLE IF NOT EXISTS</c> built up by the fluent builder, plus its
    /// indexes. Runs on a background thread.
    /// </summary>
    public static Task<int> CreateTableAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this SQLiteTableBuilder<T> builder, CancellationToken ct = default)
    {
        return AsyncRunner.Run(builder.CreateTable, ct);
    }
}
