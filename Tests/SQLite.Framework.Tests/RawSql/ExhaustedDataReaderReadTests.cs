using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public sealed class H21iReaderRowCounter : ISQLiteCommandInterceptor
{
    private int rowsRead;
    private int closedWith;

    public int RowsRead => rowsRead;

    public int ClosedWith => closedWith;

    public void OnExecuting(SQLiteCommand command)
    {
    }

    public void OnExecuted(SQLiteCommand command, int? rowsAffected)
    {
    }

    public void OnFailed(SQLiteCommand command, Exception exception)
    {
    }

    public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
    {
        rowsRead++;
    }

    public void OnReaderClosing(SQLiteCommand command, SQLiteDataReader reader, int readCount)
    {
        closedWith = readCount;
    }
}

public class ExhaustedDataReaderReadTests
{
    [Fact]
    public void ReadKeepsReturningFalsePastTheLastRow()
    {
        using TestDatabase db = new();
        List<long> rows = [1L, 2L];

        using IEnumerator<long> expected = ((IEnumerable<long>)rows).GetEnumerator();
        Assert.True(expected.MoveNext());
        Assert.True(expected.MoveNext());
        Assert.False(expected.MoveNext());
        Assert.False(expected.MoveNext());

        using SQLiteDataReader reader = db.CreateCommand("SELECT 1 UNION ALL SELECT 2", []).ExecuteReader();
        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.False(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public void SecondPassOverAnExhaustedReaderYieldsNoRows()
    {
        using TestDatabase db = new();
        List<long> rows = [1L, 2L, 3L];

        using IEnumerator<long> oracle = ((IEnumerable<long>)rows).GetEnumerator();
        int expectedFirstPass = 0;
        while (oracle.MoveNext())
        {
            expectedFirstPass++;
        }

        int expectedSecondPass = 0;
        while (expectedSecondPass < 10 && oracle.MoveNext())
        {
            expectedSecondPass++;
        }

        using SQLiteDataReader reader = db.CreateCommand("SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3", []).ExecuteReader();
        int firstPass = 0;
        while (reader.Read())
        {
            firstPass++;
        }

        int secondPass = 0;
        while (secondPass < 10 && reader.Read())
        {
            secondPass++;
        }

        Assert.Equal(expectedFirstPass, firstPass);
        Assert.Equal(expectedSecondPass, secondPass);
    }

    [Fact]
    public void ExhaustedReaderDoesNotReplayTheFirstRow()
    {
        using TestDatabase db = new();
        List<long> rows = [7L, 8L];

        using SQLiteDataReader reader = db.CreateCommand("SELECT 7 UNION ALL SELECT 8", []).ExecuteReader();
        List<long> read = [];
        while (reader.Read())
        {
            read.Add(reader.GetInt64(0));
        }

        while (read.Count < 10 && reader.Read())
        {
            read.Add(reader.GetInt64(0));
        }

        Assert.Equal(rows, read);
    }

    [Fact]
    public void RowReadCallbacksStopAtTheEndOfTheResultSet()
    {
        H21iReaderRowCounter counter = new();
        using TestDatabase db = new(b => b.AddCommandInterceptor(counter));
        List<long> rows = [1L, 2L];

        using (SQLiteDataReader reader = db.CreateCommand("SELECT 1 UNION ALL SELECT 2", []).ExecuteReader())
        {
            while (reader.Read())
            {
            }

            int guard = 0;
            while (guard < 10 && reader.Read())
            {
                guard++;
            }
        }

        Assert.Equal(rows.Count, counter.RowsRead);
        Assert.Equal(rows.Count, counter.ClosedWith);
    }
}
