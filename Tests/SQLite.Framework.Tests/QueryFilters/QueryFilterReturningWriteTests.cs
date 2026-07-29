using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24oReturnRows")]
public class H24oReturnRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H24oReturnGuard
{
    public static bool IsLive(string name)
    {
        return name.Length > 0;
    }
}

public class QueryFilterReturningWriteTests
{
    [Fact]
    public void ReturningEntityWriteDoesNotTranslateTheQueryFilter()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<H24oReturnRow>(r => H24oReturnGuard.IsLive(r.Name)));
        db.Table<H24oReturnRow>().Schema.CreateTable();

        db.Table<H24oReturnRow>().Add(new H24oReturnRow { Id = 1, Name = "plain" });

        H24oReturnRow? written = db.Table<H24oReturnRow>()
            .Returning()
            .Add(new H24oReturnRow { Id = 2, Name = "live" });

        Assert.NotNull(written);
        Assert.Equal("live", written.Name);
        Assert.Equal(2L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"H24oReturnRows\""));
    }
}
