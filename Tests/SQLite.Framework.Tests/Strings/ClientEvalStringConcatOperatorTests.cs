using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21kConcatRows")]
public class H21kConcatRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H21kConcatFunctions
{
    public static string Wrap(string value)
    {
        return "<" + value + ">";
    }
}

public class ClientEvalStringConcatOperatorTests
{
    private static List<H21kConcatRow> Rows()
    {
        return
        [
            new H21kConcatRow { Id = 1, Name = "a" },
            new H21kConcatRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21kConcatRow>().Schema.CreateTable();
        db.Table<H21kConcatRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ClientCallFollowedByConcatOperatorMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H21kConcatFunctions.Wrap(r.Name) + "!")
            .ToList();

        List<string> actual = db.Table<H21kConcatRow>()
            .OrderBy(r => r.Id)
            .Select(r => H21kConcatFunctions.Wrap(r.Name) + "!")
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallInsideProjectedMemberConcatOperatorMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Text = H21kConcatFunctions.Wrap(r.Name) + "!" })
            .Select(x => x.Text)
            .ToList();

        List<string> actual = db.Table<H21kConcatRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Text = H21kConcatFunctions.Wrap(r.Name) + "!" })
            .ToList()
            .Select(x => x.Text)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
