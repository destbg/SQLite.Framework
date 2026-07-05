namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Collects the SQL statements a migration rehearsal executes, for
/// <see cref="SQLiteMigrationRunner.Script" />. Only statements that run through the non-query
/// path are captured, so reads are left out. Savepoint bookkeeping is left out too. Parameters
/// are inlined so each captured statement runs on its own.
/// </summary>
internal sealed class MigrationScriptCapture : ISQLiteCommandInterceptor
{
    private readonly SQLiteOptions options;
    private readonly List<string> statements;

    public MigrationScriptCapture(SQLiteOptions options, List<string> statements)
    {
        this.options = options;
        this.statements = statements;
    }

    public void OnExecuting(SQLiteCommand command)
    {
    }

    public void OnExecuted(SQLiteCommand command, int? rowsAffected)
    {
        if (rowsAffected == null)
        {
            return;
        }

        string trimmed = command.CommandText.TrimStart();
        if (trimmed.StartsWith("SAVEPOINT", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("RELEASE", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        statements.Add(command.Parameters.Count == 0
            ? command.CommandText
            : SqlLiteralHelper.InlineParameters(command.CommandText, command.Parameters, options));
    }

    public void OnFailed(SQLiteCommand command, Exception exception)
    {
    }

    public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
    {
    }

    public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount)
    {
    }
}
