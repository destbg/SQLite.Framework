using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21kCapRows")]
public class H21kCapRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H21kCapFunctions
{
    public static string Decorate(string value, string suffix)
    {
        return "<" + value + suffix + ">";
    }
}

public class CapturedMethodParameterProjectionTests
{
    private static List<H21kCapRow> Rows()
    {
        return
        [
            new H21kCapRow { Id = 1, Name = "a" },
            new H21kCapRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21kCapRow>().Schema.CreateTable();
        db.Table<H21kCapRow>().AddRange(Rows());
        return db;
    }

    private static List<string> ExpectedDecorated(string suffix)
    {
        return Rows()
            .OrderBy(r => r.Id)
            .Select(r => H21kCapFunctions.Decorate(r.Name, suffix))
            .ToList();
    }

    private static List<string> QueryDecorated(TestDatabase db, string suffix)
    {
        return db.Table<H21kCapRow>()
            .OrderBy(r => r.Id)
            .Select(r => H21kCapFunctions.Decorate(r.Name, suffix))
            .ToList();
    }

    [Fact]
    public void ClientCallWithArgumentFromEnclosingParameterMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = ExpectedDecorated("!");
        List<string> actual = QueryDecorated(db, "!");

        Assert.Equal(expected, actual);
    }
}
