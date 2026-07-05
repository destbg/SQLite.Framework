using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CasedText")]
public class CasedTextRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class StringReplaceComparisonTests
{
    [Fact]
    public void ReplaceWithAConstantIgnoreCaseComparisonMatchesLinq()
    {
        using TestDatabase db = new();
        db.Table<CasedTextRow>().Schema.CreateTable();
        db.Table<CasedTextRow>().Add(new CasedTextRow { Id = 1, Name = "AbcABC" });

        string expected = "AbcABC".Replace("abc", "x", StringComparison.OrdinalIgnoreCase);
        string actual = db.Table<CasedTextRow>().Select(x => x.Name.Replace("abc", "x", StringComparison.OrdinalIgnoreCase)).First();

        Assert.Equal(expected, actual);
    }
}
