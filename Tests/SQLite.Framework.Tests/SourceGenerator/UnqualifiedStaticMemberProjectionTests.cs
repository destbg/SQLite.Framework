using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22nStaticSlotRows")]
public class H22nStaticSlotRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H22nStaticSlotFunctions
{
    public static string Combine(string first, string second, string name)
    {
        return first + "|" + second + "|" + name;
    }
}

public class UnqualifiedStaticMemberProjectionTests
{
    private const string FixedPrefix = "C";

    private static readonly string sharedPrefix = "P";

    private static string SharedSuffix => "S";

    [Fact]
    public void ClientCallReadingAnUnqualifiedStaticFieldMatchesLinq()
    {
        using TestDatabase db = Setup();
        string local = "L";

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H22nStaticSlotFunctions.Combine(sharedPrefix, local, r.Name))
            .ToList();

        List<string> actual = db.Table<H22nStaticSlotRow>()
            .OrderBy(r => r.Id)
            .Select(r => H22nStaticSlotFunctions.Combine(sharedPrefix, local, r.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallReadingAnUnqualifiedStaticPropertyMatchesLinq()
    {
        using TestDatabase db = Setup();
        string local = "L";

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H22nStaticSlotFunctions.Combine(SharedSuffix, local, r.Name))
            .ToList();

        List<string> actual = db.Table<H22nStaticSlotRow>()
            .OrderBy(r => r.Id)
            .Select(r => H22nStaticSlotFunctions.Combine(SharedSuffix, local, r.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ClientCallReadingAnUnqualifiedConstantMatchesLinq()
    {
        using TestDatabase db = Setup();
        string local = "L";

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H22nStaticSlotFunctions.Combine(FixedPrefix, local, r.Name))
            .ToList();

        List<string> actual = db.Table<H22nStaticSlotRow>()
            .OrderBy(r => r.Id)
            .Select(r => H22nStaticSlotFunctions.Combine(FixedPrefix, local, r.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22nStaticSlotRow> Rows()
    {
        return
        [
            new H22nStaticSlotRow { Id = 1, Name = "a" },
            new H22nStaticSlotRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22nStaticSlotRow>().Schema.CreateTable();
        db.Table<H22nStaticSlotRow>().AddRange(Rows());
        return db;
    }
}
