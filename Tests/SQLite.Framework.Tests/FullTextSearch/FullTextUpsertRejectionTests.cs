using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26fUpsertDocs")]
[FullTextSearch]
public class H26fUpsertDoc
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class FullTextUpsertRejectionTests
{
    [Fact]
    public void UpsertOnAFullTextTableIsRejected()
    {
        using TestDatabase db = new();
        db.Table<H26fUpsertDoc>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<H26fUpsertDoc>()
            .Upsert(new H26fUpsertDoc { Id = 1, Body = "apple" }, c => c.OnConflict(d => d.Id).DoNothing()));

        Assert.Contains("Upsert is not supported on the virtual table", ex.Message);
    }
}
