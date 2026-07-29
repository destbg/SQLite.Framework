using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24mFlagConstantRows")]
public class H24mFlagConstantRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Flags]
public enum H24mAccessFlags
{
    None = 0,
    Read = 1,
    Write = 2
}

public static class H24mFlagConstantFunctions
{
    public static string Describe(string name, H24mAccessFlags flags)
    {
        return name + ":" + flags;
    }
}

public class FlagsEnumConstantProjectionTests
{
    [Fact]
    public void CombinedFlagConstantArgumentMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H24mFlagConstantFunctions.Describe(r.Name, H24mAccessFlags.Read | H24mAccessFlags.Write))
            .ToList();

        List<string> actual = db.Table<H24mFlagConstantRow>()
            .OrderBy(r => r.Id)
            .Select(r => H24mFlagConstantFunctions.Describe(r.Name, H24mAccessFlags.Read | H24mAccessFlags.Write))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24mFlagConstantRow> Rows()
    {
        return
        [
            new H24mFlagConstantRow { Id = 1, Name = "a" },
            new H24mFlagConstantRow { Id = 2, Name = "b" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H24mFlagConstantRow>().Schema.CreateTable();
        db.Table<H24mFlagConstantRow>().AddRange(Rows());
        return db;
    }
}
