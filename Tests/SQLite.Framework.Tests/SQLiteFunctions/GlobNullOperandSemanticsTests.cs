using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GlobNullRows")]
public class GlobNullRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class GlobNullOperandSemanticsTests
{
    [Fact]
    public void NegatedGlobOverNullableNameKeepsNullRows()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(x => !(x.Name != null && x.Name.StartsWith('a')))
            .Select(x => x.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<GlobNullRow>()
            .Where(x => !SQLiteFunctions.Glob("a*", x.Name!))
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GlobOverNullableNameProjectsFalseForNullRows()
    {
        using TestDatabase db = Setup();

        List<bool> expected = Rows()
            .OrderBy(x => x.Id)
            .Select(x => x.Name != null && x.Name.StartsWith('a'))
            .ToList();

        List<bool> actual = db.Table<GlobNullRow>()
            .OrderBy(x => x.Id)
            .Select(x => SQLiteFunctions.Glob("a*", x.Name!))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<GlobNullRow> Rows()
    {
        return
        [
            new GlobNullRow { Id = 1, Name = "apple" },
            new GlobNullRow { Id = 2, Name = null },
            new GlobNullRow { Id = 3, Name = "pear" },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<GlobNullRow>().Schema.CreateTable();
        db.Table<GlobNullRow>().AddRange(Rows());
        return db;
    }
}
