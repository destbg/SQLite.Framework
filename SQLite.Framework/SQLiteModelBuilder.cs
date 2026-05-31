namespace SQLite.Framework;

/// <summary>
/// Builds the database model. Passed to <see cref="SQLiteDatabase.OnModelCreating" />, where you
/// declare each entity's table name, primary key, columns, computed columns, checks, indexes,
/// foreign keys, defaults, STRICT, WITHOUT ROWID, and triggers once. The framework runs
/// <c>OnModelCreating</c> a single time, before any table mapping is used, so create, migrate, and
/// validate all read the same definition.
/// </summary>
public sealed class SQLiteModelBuilder
{
    private readonly SQLiteDatabase database;

    internal SQLiteModelBuilder(SQLiteDatabase database)
    {
        this.database = database;
    }

    /// <summary>
    /// Returns a builder to configure the entity mapped to <typeparamref name="T" />. Chain the
    /// configuration calls on it. The configuration becomes part of the model.
    /// </summary>
    public SQLiteEntityTypeBuilder<T> Entity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        return new SQLiteEntityTypeBuilder<T>(database);
    }
}
