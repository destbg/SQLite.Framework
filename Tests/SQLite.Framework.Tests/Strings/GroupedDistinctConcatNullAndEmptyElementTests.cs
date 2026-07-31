using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26dTagRows")]
public class H26dTagRow
{
    [Key]
    public int Id { get; set; }

    public int Bucket { get; set; }

    public string? Tag { get; set; }
}

public class GroupedDistinctConcatNullAndEmptyElementTests
{
    [Fact]
    public void ADistinctGroupJoinMergesANullElementIntoAnEmptyElement()
    {
        using TestDatabase db = Setup(nameof(ADistinctGroupJoinMergesANullElementIntoAnEmptyElement));

        List<string> actual = db.Table<H26dTagRow>()
            .GroupBy(r => r.Bucket)
            .OrderBy(g => g.Key)
            .Select(g => string.Join(",", g.Select(x => x.Tag).Distinct()))
            .ToList();

        Assert.Equal([""], actual);
    }

    private static List<H26dTagRow> Rows()
    {
        return
        [
            new H26dTagRow { Id = 1, Bucket = 1, Tag = null },
            new H26dTagRow { Id = 2, Bucket = 1, Tag = null },
            new H26dTagRow { Id = 3, Bucket = 1, Tag = "" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26dTagRow>().Schema.CreateTable();
        db.Table<H26dTagRow>().AddRange(Rows());
        return db;
    }
}
