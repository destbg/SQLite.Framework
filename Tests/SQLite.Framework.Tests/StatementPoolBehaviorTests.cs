using System.Threading.Tasks;
using SQLitePCL;
using SQLite.Framework.Extensions;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class StatementPoolBehaviorTests
{
    [Fact]
    public void StatementPool_ReusesThenEvictsAndStillServesHotQuery()
    {
        using TestDatabase db = new();

        Assert.Equal(7, db.CreateCommand("SELECT 7", []).ExecuteQuery<int>().Single());
        Assert.Equal(7, db.CreateCommand("SELECT 7", []).ExecuteQuery<int>().Single());

        for (int i = 0; i < 130; i++)
        {
            Assert.Equal(i, db.CreateCommand($"SELECT {i}", []).ExecuteQuery<int>().Single());
        }

        Assert.Equal(7, db.CreateCommand("SELECT 7", []).ExecuteQuery<int>().Single());
    }

    [Fact]
    public void StatementPool_DuplicateReturnForSameSqlIsFinalized()
    {
        using TestDatabase db = new();

        SQLiteDataReader first = db.CreateCommand("SELECT 1", []).ExecuteReader();
        SQLiteDataReader second = db.CreateCommand("SELECT 1", []).ExecuteReader();
        Assert.True(first.Read());
        Assert.True(second.Read());

        first.Dispose();
        second.Dispose();

        Assert.Equal(1, db.CreateCommand("SELECT 1", []).ExecuteQuery<int>().Single());
    }

    [Fact]
    public void StatementPool_ReturnAfterDatabaseDisposeFinalizes()
    {
        TestDatabase db = new();

        SQLiteDataReader reader = db.CreateCommand("SELECT 1", []).ExecuteReader();
        Assert.True(reader.Read());

        db.Dispose();
        reader.Dispose();
    }

    [Fact]
    public void Reader_DoubleDisposeIsSafe()
    {
        using TestDatabase db = new();

        SQLiteDataReader reader = db.CreateCommand("SELECT 1", []).ExecuteReader();
        Assert.True(reader.Read());

        reader.Dispose();
        reader.Dispose();
    }

    [Fact]
    public void Reader_PublicConstructorFinalizesOnDispose()
    {
        using TestDatabase db = new();
        db.OpenConnection();
        sqlite3 handle = db.GetActiveHandle();

        raw.sqlite3_prepare_v2(handle, "SELECT 1", out sqlite3_stmt statement);
        SQLiteCommand command = db.CreateCommand("SELECT 1", []);
        SQLiteDataReader reader = new(handle, statement, NoOpLockObject.Instance, command);

        Assert.True(reader.Read());
        Assert.Equal(1, reader.GetInt32(0));
        reader.Dispose();
    }

    [Fact]
    public void ExecuteReader_PrepareFailureReleasesLock()
    {
        using TestDatabase db = new();

        Assert.ThrowsAny<Exception>(() =>
            db.CreateCommand("THIS IS NOT VALID SQL", []).ExecuteReader());

        Assert.Equal(1, db.CreateCommand("SELECT 1", []).ExecuteQuery<int>().Single());
    }

    [Fact]
    public void ExecuteReader_InterceptorThrowAfterBuildDisposesReader()
    {
        using TestDatabase db = new(b => b.AddCommandInterceptor(new ThrowingOnExecutedInterceptor()));

        Assert.ThrowsAny<Exception>(() =>
            db.CreateCommand("SELECT 'throw_marker'", []).ExecuteReader());
    }

    [Fact]
    public async Task ExecuteReaderAsync_PrepareFailureReleasesLock()
    {
        using TestDatabase db = new();

        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await db.CreateCommand("THIS IS NOT VALID SQL", []).ExecuteReaderAsync());
    }

    [Fact]
    public async Task ExecuteReaderAsync_InterceptorThrowAfterBuildDisposesReader()
    {
        using TestDatabase db = new(b => b.AddCommandInterceptor(new ThrowingOnExecutedInterceptor()));

        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await db.CreateCommand("SELECT 'throw_marker'", []).ExecuteReaderAsync());
    }

    [Fact]
    public void TypedReaders_NullColumnsReturnDefault()
    {
        using TestDatabase db = new();
        using SQLiteDataReader reader = db.CreateCommand("SELECT NULL", []).ExecuteReader();
        Assert.True(reader.Read());

        Assert.Equal(default(DateTime), reader.GetDateTimeValue(0));
        Assert.Equal(default(DateTimeOffset), reader.GetDateTimeOffsetValue(0));
        Assert.Equal(default(TimeSpan), reader.GetTimeSpanValue(0));
        Assert.Equal(default(DateOnly), reader.GetDateOnlyValue(0));
        Assert.Equal(default(TimeOnly), reader.GetTimeOnlyValue(0));
        Assert.Equal(default(Guid), reader.GetGuidValue(0));
        Assert.Equal(default(decimal), reader.GetDecimalValue(0));
    }

    [Fact]
    public void TypedReaders_ValueColumnsReadWithoutBoxing()
    {
        using TestDatabase db = new();

        using (SQLiteDataReader reader = db.CreateCommand("SELECT 12345", []).ExecuteReader())
        {
            Assert.True(reader.Read());
            Assert.Equal(new DateTime(12345), reader.GetDateTimeValue(0));
            Assert.Equal(new DateTimeOffset(12345, TimeSpan.Zero), reader.GetDateTimeOffsetValue(0));
            Assert.Equal(TimeSpan.FromTicks(12345), reader.GetTimeSpanValue(0));
            Assert.Equal(DateOnly.FromDateTime(new DateTime(12345)), reader.GetDateOnlyValue(0));
            Assert.Equal(new TimeOnly(12345), reader.GetTimeOnlyValue(0));
        }

        using (SQLiteDataReader reader = db.CreateCommand("SELECT 5", []).ExecuteReader())
        {
            Assert.True(reader.Read());
            Assert.Equal(5m, reader.GetDecimalValue(0));
        }

        using (SQLiteDataReader reader = db.CreateCommand("SELECT 5.5", []).ExecuteReader())
        {
            Assert.True(reader.Read());
            Assert.Equal(5.5m, reader.GetDecimalValue(0));
        }

        using (SQLiteDataReader reader = db.CreateCommand("SELECT '00000000-0000-0000-0000-000000000001'", []).ExecuteReader())
        {
            Assert.True(reader.Read());
            Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), reader.GetGuidValue(0));
        }
    }
}

public sealed class ThrowingOnExecutedInterceptor : ISQLiteCommandInterceptor
{
    public void OnExecuting(SQLiteCommand command)
    {
    }

    public void OnExecuted(SQLiteCommand command, int? rowsAffected)
    {
        if (command.CommandText.Contains("throw_marker", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("interceptor failure for coverage");
        }
    }

    public void OnFailed(SQLiteCommand command, Exception exception)
    {
    }

    public void OnRowRead(SQLiteCommand command, SQLiteDataReader reader)
    {
    }
}
