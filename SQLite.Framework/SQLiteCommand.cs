using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Internals.Helpers;
using SQLitePCL;

namespace SQLite.Framework;

/// <summary>
/// Represents a command to be executed against the SQLite database.
/// </summary>
public class SQLiteCommand
{
    private readonly SQLiteDatabase database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteCommand" /> class.
    /// </summary>
    public SQLiteCommand(SQLiteDatabase database)
    {
        this.database = database;
        CommandText = string.Empty;
        Parameters = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteCommand" /> class.
    /// </summary>
    public SQLiteCommand(SQLiteDatabase database, string commandText, List<SQLiteParameter> parameters)
    {
        this.database = database;
        CommandText = commandText;
        Parameters = parameters;
    }

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
    /// The connection lock is acquired before the statement is prepared and held until the returned
    /// <see cref="SQLiteDataReader" /> is disposed. Callers must dispose the reader promptly.
    /// </remarks>
    public SQLiteDataReader ExecuteReader()
    {
        IDisposable connectionLock = database.Lock();

        try
        {
            sqlite3_stmt statement = CreateStatement();
            return new SQLiteDataReader(database.Handle!, statement, connectionLock);
        }
        catch
        {
            connectionLock.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Executes the command against the database and returns the number of rows affected.
    /// </summary>
    public int ExecuteNonQuery()
    {
        using IDisposable _ = database.Lock();

        sqlite3_stmt statement = CreateStatement();
        SQLiteResult result = (SQLiteResult)raw.sqlite3_step(statement);
        raw.sqlite3_finalize(statement);

        if (result != SQLiteResult.Done)
        {
            throw new SQLiteException(result, raw.sqlite3_errmsg(database.Handle).utf8_to_string(), CommandText);
        }

        return raw.sqlite3_changes(database.Handle);
    }

    private sqlite3_stmt CreateStatement()
    {
        SQLiteResult result = (SQLiteResult)raw.sqlite3_prepare_v2(
            database.Handle,
            CommandText,
            out sqlite3_stmt? stmt
        );

        if (result != 0)
        {
            throw new SQLiteException(result, raw.sqlite3_errmsg(database.Handle).utf8_to_string(), CommandText);
        }

        BindParameters(stmt);

        return stmt;
    }

    private void BindParameters(sqlite3_stmt statement)
    {
        SQLiteStorageOptions options = database.StorageOptions;
        foreach (SQLiteParameter parameter in Parameters)
        {
            CommandHelpers.BindParameter(statement, parameter.Name, parameter.Value, options);
        }
    }
}