namespace SQLite.Framework;

/// <summary>
/// Typed access to common SQLite pragmas. Read or write them as properties instead of using raw
/// <c>PRAGMA</c> SQL. Get the instance from <see cref="SQLiteDatabase.Pragmas" />. To add more
/// pragmas, write a class that inherits from this one and pass it to <see cref="SQLiteOptionsBuilder.UsePragmas" />.
/// </summary>
public class SQLitePragmas
{
    /// <summary>
    /// Creates a new instance that reads from and writes to <paramref name="database" />.
    /// </summary>
    public SQLitePragmas(SQLiteDatabase database)
    {
        Database = database;
    }

    /// <summary>
    /// The database this accessor reads from and writes to.
    /// </summary>
    protected SQLiteDatabase Database { get; }

    /// <summary>
    /// <c>PRAGMA foreign_keys</c>. Turns enforcement of foreign key constraints on or off for the
    /// connection. SQLite has it off by default.
    /// </summary>
    public virtual bool ForeignKeys
    {
        get => Database.ExecuteScalar<int>("PRAGMA foreign_keys") == 1;
        set => Database.Execute($"PRAGMA foreign_keys = {(value ? 1 : 0)}");
    }

    /// <summary>
    /// <c>PRAGMA journal_mode</c>. Possible values are <c>DELETE</c>, <c>WAL</c>, <c>MEMORY</c>,
    /// <c>TRUNCATE</c>, <c>PERSIST</c>, <c>OFF</c>.
    /// </summary>
    public virtual string JournalMode
    {
        get => Database.ExecuteScalar<string>("PRAGMA journal_mode") ?? "delete";
        set => Database.ExecuteScalar<string>($"PRAGMA journal_mode = {value}");
    }

    /// <summary>
    /// <c>PRAGMA cache_size</c>. Tells SQLite how many pages to keep in memory for the connection.
    /// A negative number is read as kibibytes instead of pages.
    /// </summary>
    public virtual int CacheSize
    {
        get => Database.ExecuteScalar<int>("PRAGMA cache_size");
        set => Database.Execute($"PRAGMA cache_size = {value}");
    }

    /// <summary>
    /// <c>PRAGMA synchronous</c>. Tells SQLite how often to call sync on the disk.
    /// </summary>
    public virtual SQLiteSynchronousMode SynchronousMode
    {
        get => (SQLiteSynchronousMode)Database.ExecuteScalar<int>("PRAGMA synchronous");
        set => Database.Execute($"PRAGMA synchronous = {(int)value}");
    }

    /// <summary>
    /// <c>PRAGMA user_version</c>. An integer your app picks. SQLite stores it in the file header.
    /// Apps often use it to track schema versions.
    /// </summary>
    public virtual int UserVersion
    {
        get => Database.ExecuteScalar<int?>("PRAGMA user_version") ?? 0;
        set => Database.Execute($"PRAGMA user_version = {value}");
    }

    /// <summary>
    /// <c>PRAGMA page_size</c>. Read only once the database file has been written.
    /// </summary>
    public virtual long PageSize => Database.ExecuteScalar<long>("PRAGMA page_size");

    /// <summary>
    /// <c>PRAGMA freelist_count</c>. The number of unused pages in the database file.
    /// </summary>
    public virtual long FreelistCount => Database.ExecuteScalar<long>("PRAGMA freelist_count");

    /// <summary>
    /// <c>PRAGMA recursive_triggers</c>. Turns recursive trigger calls on or off.
    /// </summary>
    public virtual bool RecursiveTriggers
    {
        get => Database.ExecuteScalar<int>("PRAGMA recursive_triggers") == 1;
        set => Database.Execute($"PRAGMA recursive_triggers = {(value ? 1 : 0)}");
    }

    /// <summary>
    /// <c>PRAGMA temp_store</c>. Tells SQLite where to keep temporary tables and indexes.
    /// <c>0</c> is the default, <c>1</c> is file, <c>2</c> is memory.
    /// </summary>
    public virtual int TempStore
    {
        get => Database.ExecuteScalar<int>("PRAGMA temp_store");
        set => Database.Execute($"PRAGMA temp_store = {value}");
    }

    /// <summary>
    /// <c>PRAGMA secure_delete</c>. When this is on, SQLite writes zeros over deleted content
    /// before it gives the pages back.
    /// </summary>
    public virtual bool SecureDelete
    {
        get => Database.ExecuteScalar<int>("PRAGMA secure_delete") == 1;
        set => Database.ExecuteScalar<int>($"PRAGMA secure_delete = {(value ? 1 : 0)}");
    }
}
