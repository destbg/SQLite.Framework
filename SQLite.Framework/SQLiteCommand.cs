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
        Id = database.NextCommandId();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteCommand" /> class.
    /// </summary>
    public SQLiteCommand(SQLiteDatabase database, string commandText, List<SQLiteParameter> parameters)
    {
        Database = database;
        CommandText = commandText;
        Parameters = parameters;
        Id = database.NextCommandId();
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
    /// A per-database number assigned to this command when it is created, increasing by one for each
    /// command the database makes. A <see cref="ISQLiteCommandInterceptor" /> can read it to tie the
    /// calls for one command together.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// Executes the command against the database and returns a data reader.
    /// </summary>
    /// <remarks>
    /// Read operations do not acquire the exclusive connection lock. SQLite's own serialized-mode mutex
    /// ensures statement safety and WAL mode provides snapshot isolation from concurrent writers.
    /// Interceptors fire once per call: <c>OnExecuting</c> before the statement is prepared,
    /// <c>OnExecuted</c> after the reader is ready (before any rows are read) and <c>OnFailed</c>
    /// if preparation throws.
    /// </remarks>
    public virtual SQLiteDataReader ExecuteReader()
    {
        IDisposable connectionLock = Database.ReadLock();

        NotifyExecuting();
        SQLiteDataReader? reader = null;
        try
        {
            sqlite3_stmt statement = CreateStatement();
            reader = new(Database.GetActiveHandle(), statement, connectionLock, this)
            {
                PooledSql = CommandText,
            };
            NotifyExecuted(rowsAffected: null);
            return reader;
        }
        catch (Exception exception)
        {
            if (reader != null)
            {
                reader.Dispose();
            }
            else
            {
                connectionLock.Dispose();
            }

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
        return ExecuteNonQueryCore();
    }

    /// <summary>
    /// Executes the command and returns the number of rows affected together with the
    /// connection's <c>last_insert_rowid</c> value. The rowid is read inside the same lock as
    /// the INSERT, so a concurrent writer cannot replace it before the read.
    /// </summary>
    public virtual (int Changes, long RowId) ExecuteWithLastRowId()
    {
        using IDisposable _ = Database.Lock();
        return ExecuteWithLastRowIdCore();
    }

    internal int ExecuteNonQueryCore()
    {
        NotifyExecuting();
        try
        {
            sqlite3 handle = Database.GetActiveHandle();
            byte[] sqlBytes = Encoding.UTF8.GetBytes(CommandText);
            ReadOnlySpan<byte> remaining = sqlBytes;
            int changes = 0;

            while (!SqlTail.IsWhitespaceOrComments(Encoding.UTF8.GetString(remaining)))
            {
                SQLiteResult prepareResult = (SQLiteResult)raw.sqlite3_prepare_v2(handle, remaining, out sqlite3_stmt statement, out ReadOnlySpan<byte> tail);
                if (prepareResult != SQLiteResult.OK)
                {
                    throw new SQLiteException(prepareResult, raw.sqlite3_errmsg(handle).utf8_to_string(), CommandText);
                }

                int totalChangesBefore = raw.sqlite3_total_changes(handle);
                try
                {
                    BindParameters(statement);

                    SQLiteResult stepResult = (SQLiteResult)raw.sqlite3_step(statement);
                    while (stepResult == SQLiteResult.Row)
                    {
                        stepResult = (SQLiteResult)raw.sqlite3_step(statement);
                    }

                    if (stepResult != SQLiteResult.Done)
                    {
                        throw new SQLiteException(stepResult, raw.sqlite3_errmsg(handle).utf8_to_string(), CommandText);
                    }
                }
                finally
                {
                    raw.sqlite3_finalize(statement);
                }

                if (raw.sqlite3_total_changes(handle) != totalChangesBefore)
                {
                    changes += raw.sqlite3_changes(handle);
                }

                remaining = tail;
            }

            NotifyExecuted(changes);
            return changes;
        }
        catch (Exception exception)
        {
            NotifyFailed(exception);
            throw;
        }
    }

    internal (int Changes, long RowId) ExecuteWithLastRowIdCore()
    {
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

    internal (int Changes, long RowId, bool RowIdChanged) ExecuteWithInsertDetection()
    {
        using IDisposable _ = Database.Lock();

        NotifyExecuting();
        try
        {
            long before = raw.sqlite3_last_insert_rowid(Database.GetActiveHandle());

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
            return (changes, rowId, rowId != before);
        }
        catch (Exception exception)
        {
            NotifyFailed(exception);
            throw;
        }
    }

    internal sqlite3_stmt CreateStatement()
    {
        sqlite3_stmt stmt = Database.RentStatement(CommandText);

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

    internal void NotifyExecuting()
    {
        IReadOnlyList<ISQLiteCommandInterceptor> interceptors = Database.EffectiveCommandInterceptors;
        if (interceptors.Count == 0)
        {
            return;
        }

        for (int i = 0; i < interceptors.Count; i++)
        {
            interceptors[i].OnExecuting(this);
        }
    }

    internal void NotifyRowRead(SQLiteDataReader reader)
    {
        IReadOnlyList<ISQLiteCommandInterceptor> interceptors = Database.EffectiveCommandInterceptors;
        if (interceptors.Count == 0)
        {
            return;
        }

        for (int i = 0; i < interceptors.Count; i++)
        {
            interceptors[i].OnRowRead(this, reader);
        }
    }

    internal void NotifyReaderClosing(SQLiteDataReader reader, int readCount)
    {
        IReadOnlyList<ISQLiteCommandInterceptor> interceptors = Database.EffectiveCommandInterceptors;
        if (interceptors.Count == 0)
        {
            return;
        }

        for (int i = 0; i < interceptors.Count; i++)
        {
            interceptors[i].OnReaderClosing(this, reader, readCount);
        }
    }

    internal void NotifyExecuted(int? rowsAffected)
    {
        IReadOnlyList<ISQLiteCommandInterceptor> interceptors = Database.EffectiveCommandInterceptors;
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
        IReadOnlyList<ISQLiteCommandInterceptor> interceptors = Database.EffectiveCommandInterceptors;
        if (interceptors.Count == 0)
        {
            return;
        }

        for (int i = 0; i < interceptors.Count; i++)
        {
            interceptors[i].OnFailed(this, exception);
        }
    }

    private void BindParameters(sqlite3_stmt statement)
    {
        SQLiteOptions options = Database.Options;
        foreach (SQLiteParameter parameter in Parameters)
        {
            if (raw.sqlite3_bind_parameter_index(statement, parameter.Name) == 0)
            {
                continue;
            }

            CommandHelpers.BindParameter(statement, parameter.Name, parameter.Value, options);
        }
    }
}
