namespace SQLite.Framework;

/// <summary>
/// Represents a command to be executed against the SQLite database.
/// </summary>
public class SQLiteCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteCommand" /> class.
    /// </summary>
    public SQLiteCommand(SQLiteDatabase database)
    {
        Database = database;
        CommandText = string.Empty;
        Parameters = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteCommand" /> class.
    /// </summary>
    public SQLiteCommand(SQLiteDatabase database, string commandText, List<SQLiteParameter> parameters)
    {
        Database = database;
        CommandText = commandText;
        Parameters = parameters;
    }

    /// <summary>
    /// The database the command runs against.
    /// </summary>
    public SQLiteDatabase Database { get; }

    /// <summary>
    /// The SQL command to be executed.
    /// </summary>
    public string CommandText { get; set; }

    /// <summary>
    /// The parameters to be used in the command.
    /// </summary>
    public List<SQLiteParameter> Parameters { get; set; }

    /// <summary>
    /// Executes the command against the database and returns a data reader.
    /// </summary>
    /// <remarks>
    /// Read operations do not acquire the exclusive connection lock. SQLite's own serialized-mode mutex
    /// ensures statement safety, and WAL mode provides snapshot isolation from concurrent writers.
    /// Interceptors fire once per call: <c>OnExecuting</c> before the statement is prepared,
    /// <c>OnExecuted</c> after the reader is ready (before any rows are read), and <c>OnFailed</c>
    /// if preparation throws.
    /// </remarks>
    public virtual SQLiteDataReader ExecuteReader()
    {
        IDisposable connectionLock = Database.ReadLock();

        NotifyExecuting();
        try
        {
            sqlite3_stmt statement = CreateStatement();
            SQLiteDataReader reader = new(Database.GetActiveHandle(), statement, connectionLock, Database);
            NotifyExecuted(rowsAffected: null);
            return reader;
        }
        catch (Exception exception)
        {
            connectionLock.Dispose();
            NotifyFailed(exception);
            throw;
        }
    }

    /// <summary>
    /// Executes the command against the database and returns the number of rows affected.
    /// </summary>
    public virtual int ExecuteNonQuery()
    {
        using IDisposable _ = Database.Lock();

        NotifyExecuting();
        try
        {
            sqlite3_stmt statement = CreateStatement();
            SQLiteResult result = (SQLiteResult)raw.sqlite3_step(statement);
            raw.sqlite3_finalize(statement);

            if (result != SQLiteResult.Done)
            {
                throw new SQLiteException(result, raw.sqlite3_errmsg(Database.GetActiveHandle()).utf8_to_string(), CommandText);
            }

            int changes = raw.sqlite3_changes(Database.GetActiveHandle());
            NotifyExecuted(changes);
            return changes;
        }
        catch (Exception exception)
        {
            NotifyFailed(exception);
            throw;
        }
    }

    /// <summary>
    /// Executes the command and returns the number of rows affected together with the
    /// connection's <c>last_insert_rowid</c> value. The rowid is read inside the same lock as
    /// the INSERT, so a concurrent writer cannot replace it before the read.
    /// </summary>
    public virtual (int Changes, long RowId) ExecuteWithLastRowId()
    {
        using IDisposable _ = Database.Lock();

        NotifyExecuting();
        try
        {
            sqlite3_stmt statement = CreateStatement();
            SQLiteResult result = (SQLiteResult)raw.sqlite3_step(statement);
            raw.sqlite3_finalize(statement);

            if (result != SQLiteResult.Done)
            {
                throw new SQLiteException(result, raw.sqlite3_errmsg(Database.GetActiveHandle()).utf8_to_string(), CommandText);
            }

            sqlite3 handle = Database.GetActiveHandle();
            int changes = raw.sqlite3_changes(handle);
            long rowId = raw.sqlite3_last_insert_rowid(handle);
            NotifyExecuted(changes);
            return (changes, rowId);
        }
        catch (Exception exception)
        {
            NotifyFailed(exception);
            throw;
        }
    }

    internal sqlite3_stmt CreateStatement()
    {
        sqlite3 handle = Database.GetActiveHandle();

        SQLiteResult result = (SQLiteResult)raw.sqlite3_prepare_v2(
            handle,
            CommandText,
            out sqlite3_stmt? stmt
        );

        if (result != 0)
        {
            throw new SQLiteException(result, raw.sqlite3_errmsg(handle).utf8_to_string(), CommandText);
        }

        try
        {
            BindParameters(stmt);
        }
        catch
        {
            raw.sqlite3_finalize(stmt);
            throw;
        }

        return stmt;
    }

    private void BindParameters(sqlite3_stmt statement)
    {
        SQLiteOptions options = Database.Options;
        foreach (SQLiteParameter parameter in Parameters)
        {
            CommandHelpers.BindParameter(statement, parameter.Name, parameter.Value, options);
        }
    }

    internal void NotifyExecuting()
    {
        IReadOnlyList<ISQLiteCommandInterceptor> interceptors = Database.Options.CommandInterceptors;
        if (interceptors.Count == 0)
        {
            return;
        }

        for (int i = 0; i < interceptors.Count; i++)
        {
            interceptors[i].OnExecuting(this);
        }
    }

    internal void NotifyExecuted(int? rowsAffected)
    {
        IReadOnlyList<ISQLiteCommandInterceptor> interceptors = Database.Options.CommandInterceptors;
        if (interceptors.Count == 0)
        {
            return;
        }

        for (int i = 0; i < interceptors.Count; i++)
        {
            interceptors[i].OnExecuted(this, rowsAffected);
        }
    }

    internal void NotifyFailed(Exception exception)
    {
        IReadOnlyList<ISQLiteCommandInterceptor> interceptors = Database.Options.CommandInterceptors;
        if (interceptors.Count == 0)
        {
            return;
        }

        for (int i = 0; i < interceptors.Count; i++)
        {
            interceptors[i].OnFailed(this, exception);
        }
    }
}
