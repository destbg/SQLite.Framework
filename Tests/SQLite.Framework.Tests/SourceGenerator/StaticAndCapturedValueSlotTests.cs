using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21kSlotRows")]
public class H21kSlotRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H21kSlotConfig
{
    public static string Prefix => "P";
}

public static class H21kSlotFunctions
{
    public static string Combine(string first, string second, string name)
    {
        return first + "|" + second + "|" + name;
    }
}

public class StaticAndCapturedValueSlotTests
{
    private static List<H21kSlotRow> Rows()
    {
        return
        [
            new H21kSlotRow { Id = 1, Name = "a" },
            new H21kSlotRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21kSlotRow>().Schema.CreateTable();
        db.Table<H21kSlotRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ClientCallReadingStaticThenCapturedLocalMatchesLinq()
    {
        using TestDatabase db = Setup();
        string local = "L";

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H21kSlotFunctions.Combine(H21kSlotConfig.Prefix, local, r.Name))
            .ToList();

        List<string> actual = db.Table<H21kSlotRow>()
            .OrderBy(r => r.Id)
            .Select(r => H21kSlotFunctions.Combine(H21kSlotConfig.Prefix, local, r.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
