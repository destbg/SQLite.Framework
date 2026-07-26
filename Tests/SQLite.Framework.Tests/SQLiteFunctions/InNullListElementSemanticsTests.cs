using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22kTagRows")]
public class H22kTagRow
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

public class InNullListElementSemanticsTests
{
    [Fact]
    public void NegatedInWithANullTextElementKeepsNonMatchingRows()
    {
        using TestDatabase db = Setup();
        List<H22kTagRow> local = Rows();
        string[] wanted = ["a", null!];

        List<int> expected = local
            .Where(x => !wanted.Contains(x.Code))
            .Select(x => x.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H22kTagRow>()
            .Where(x => !SQLiteFunctions.In(x.Code, wanted))
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal([2, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegatedInWithANullTextElementProjectsTrueForNonMatchingRows()
    {
        using TestDatabase db = Setup();
        List<H22kTagRow> local = Rows();
        string[] wanted = ["a", null!];

        List<bool> expected = local
            .OrderBy(x => x.Id)
            .Select(x => !wanted.Contains(x.Code))
            .ToList();

        List<bool> actual = db.Table<H22kTagRow>()
            .OrderBy(x => x.Id)
            .Select(x => !SQLiteFunctions.In(x.Code, wanted))
            .ToList();

        Assert.Equal([false, true, true], expected);
        Assert.Equal(expected, actual);
    }

    private static List<H22kTagRow> Rows()
    {
        return
        [
            new H22kTagRow { Id = 1, Code = "a" },
            new H22kTagRow { Id = 2, Code = "b" },
            new H22kTagRow { Id = 3, Code = "c" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22kTagRow>().Schema.CreateTable();
        db.Table<H22kTagRow>().AddRange(Rows());
        return db;
    }
}
