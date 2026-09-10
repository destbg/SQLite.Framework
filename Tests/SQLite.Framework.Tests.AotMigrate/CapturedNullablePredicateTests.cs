using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Generated;

namespace SQLite.Framework.Tests.AotMigrate;

public sealed class AotMessageRow
{
    [Key]
    public int Id { get; set; }

    public string SessionId { get; set; } = "";
}

public class CapturedNullablePredicateTests
{
    [Theory]
    [InlineData(null, 3)]
    [InlineData(0, 3)]
    [InlineData(4, 2)]
    public async Task Where_CapturedNullableValue_ReturnsMessagesBeforeCursor(int? before, int expectedCount)
    {
        using SQLiteDatabase db = CreateDatabase();
        string sessionId = "session-1";
        IQueryable<AotMessageRow> query = db.Table<AotMessageRow>().Where(m => m.SessionId == sessionId);
        if (before is > 0)
        {
            query = query.Where(m => m.Id < before.Value);
        }

        List<AotMessageRow> rows = await query.OrderBy(m => m.Id)
            .ToListAsync(ct: TestContext.Current.CancellationToken);

        Assert.Equal(Enumerable.Range(2, expectedCount), rows.Select(m => m.Id));
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(0, 4)]
    [InlineData(4, 4)]
    public void Where_CapturedNullableHasValue_ReturnsMatchingRows(int? before, int expectedCount)
    {
        using SQLiteDatabase db = CreateDatabase();

        List<AotMessageRow> rows = db.Table<AotMessageRow>().Where(m => before.HasValue).ToList();

        Assert.Equal(expectedCount, rows.Count);
    }

    [Fact]
    public void Where_CapturedNullNullableValue_ThrowsInvalidOperationException()
    {
        using SQLiteDatabase db = CreateDatabase();
        int? before = null;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            db.Table<AotMessageRow>().Where(m => m.Id < before!.Value).ToList());

        Assert.Equal("Nullable object must have a value.", exception.Message);
    }

    [Fact]
    public void Where_CapturedLongNullableValue_ReturnsMessagesBeforeCursor()
    {
        using SQLiteDatabase db = CreateDatabase();
        long? before = 3;

        List<AotMessageRow> rows = db.Table<AotMessageRow>()
            .Where(m => m.Id < before.Value).OrderBy(m => m.Id).ToList();

        Assert.Equal(new[] { 1, 2 }, rows.Select(m => m.Id));
    }

    private static SQLiteDatabase CreateDatabase()
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        builder.UseGeneratedMaterializers();
        builder.DisableReflectionFallback();
        SQLiteDatabase db = new(builder.Build());
        SQLiteTable<AotMessageRow> messages = db.Table<AotMessageRow>();
        messages.Schema.CreateTable();
        messages.Add(new AotMessageRow { Id = 4, SessionId = "session-1" });
        messages.Add(new AotMessageRow { Id = 1, SessionId = "other-session" });
        messages.Add(new AotMessageRow { Id = 3, SessionId = "session-1" });
        messages.Add(new AotMessageRow { Id = 2, SessionId = "session-1" });
        return db;
    }
}
