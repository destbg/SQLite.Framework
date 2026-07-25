using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SiaRow")]
public class SiaRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Amount { get; set; }
}

public class StringInlineArrayClientElementTests
{
    private static List<SiaRow> Rows() =>
    [
        new SiaRow { Id = 1, Name = "a", Amount = 10 },
        new SiaRow { Id = 2, Name = "b", Amount = 20 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<SiaRow>().Schema.CreateTable();
        db.Table<SiaRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ConcatArrayWithClientElementMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => string.Concat(new object[] { r.Name, CmcClientFns.Pass(r.Amount) }))
            .ToList();

        List<string> actual = db.Table<SiaRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Concat(new object[] { r.Name, CmcClientFns.Pass(r.Amount) }))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JoinStringArrayWithClientElementMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => string.Join("-", new[] { r.Name, CmcClientFns.Tag(r.Name) }))
            .ToList();

        List<string> actual = db.Table<SiaRow>()
            .OrderBy(r => r.Id)
            .Select(r => string.Join("-", new[] { r.Name, CmcClientFns.Tag(r.Name) }))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
