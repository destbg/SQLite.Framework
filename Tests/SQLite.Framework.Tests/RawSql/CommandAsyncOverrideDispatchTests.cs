using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H22aCountingCommand : SQLiteCommand
{
    private int readerCalls;
    private int nonQueryCalls;
    private int rowIdCalls;

    public H22aCountingCommand(SQLiteDatabase database, string commandText, List<SQLiteParameter> parameters)
        : base(database, commandText, parameters)
    {
    }

    public int ReaderCalls => readerCalls;

    public int NonQueryCalls => nonQueryCalls;

    public int RowIdCalls => rowIdCalls;

    public override SQLiteDataReader ExecuteReader()
    {
        readerCalls++;
        return base.ExecuteReader();
    }

    public override int ExecuteNonQuery()
    {
        nonQueryCalls++;
        return base.ExecuteNonQuery();
    }

    public override (int Changes, long RowId) ExecuteWithLastRowId()
    {
        rowIdCalls++;
        return base.ExecuteWithLastRowId();
    }
}

public class CommandAsyncOverrideDispatchTests
{
    [Fact]
    public async Task ExecuteNonQueryAsyncRunsTheSubclassOverride()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H22aDispatchRows\" (\"Id\" INTEGER PRIMARY KEY)");
        H22aCountingCommand command = new(db, "DELETE FROM \"H22aDispatchRows\" WHERE \"Id\" = 999", []);

        command.ExecuteNonQuery();
        await command.ExecuteNonQueryAsync();

        Assert.Equal(2, command.NonQueryCalls);
    }

    [Fact]
    public async Task ExecuteWithLastRowIdAsyncRunsTheSubclassOverride()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H22aDispatchRows\" (\"Id\" INTEGER PRIMARY KEY, \"Value\" INTEGER)");
        H22aCountingCommand command = new(db, "INSERT INTO \"H22aDispatchRows\" (\"Value\") VALUES (1)", []);

        command.ExecuteWithLastRowId();
        await command.ExecuteWithLastRowIdAsync();

        Assert.Equal(2, command.RowIdCalls);
    }

    [Fact]
    public async Task ExecuteReaderAsyncRunsTheSubclassOverride()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"H22aDispatchRows\" (\"Id\" INTEGER PRIMARY KEY)");
        H22aCountingCommand command = new(db, "SELECT \"Id\" FROM \"H22aDispatchRows\"", []);

        using (SQLiteDataReader first = command.ExecuteReader())
        {
            Assert.False(first.Read());
        }

        using (SQLiteDataReader second = await command.ExecuteReaderAsync())
        {
            Assert.False(second.Read());
        }

        Assert.Equal(2, command.ReaderCalls);
    }
}
