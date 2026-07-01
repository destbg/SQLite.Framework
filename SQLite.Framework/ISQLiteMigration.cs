namespace SQLite.Framework;

/// <summary>
/// One schema version expressed as a single class.
/// </summary>
public interface ISQLiteMigration
{
    /// <summary>
    /// The version number this migration brings the database to. Must be one or more and unique
    /// across the migrations registered on one runner.
    /// </summary>
    static abstract int Version { get; }

    /// <summary>
    /// Declares the work this version performs, using the passed <see cref="SQLiteMigrationStep" />.
    /// </summary>
    /// <param name="step">The step to declare the work on.</param>
    void Apply(SQLiteMigrationStep step);
}
