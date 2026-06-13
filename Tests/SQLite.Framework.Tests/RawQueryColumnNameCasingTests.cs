using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class RawQueryColumnNameCasingTests
{
    [Fact]
    public void PropertyEntityMapsLowercaseColumns()
    {
        using TestDatabase db = new();

        List<CasingSampleBook> rows = db.Query<CasingSampleBook>("SELECT 5 AS id, 'x' AS title");

        Assert.Single(rows);
        Assert.Equal(5, rows[0].Id);
        Assert.Equal("x", rows[0].Title);
    }

    [Fact]
    public void PositionalEntityMapsLowercaseColumns()
    {
        using TestDatabase db = new();

        List<CasingSampleRecord> rows = db.Query<CasingSampleRecord>("SELECT 5 AS id, 'x' AS title");

        Assert.Single(rows);
        Assert.Equal(5, rows[0].Id);
        Assert.Equal("x", rows[0].Title);
    }
}

public class CasingSampleBook
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
}

public record CasingSampleRecord(int Id, string Title);
