using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25sOptionalStructRows")]
public class H25sOptionalStructRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H25sOptionalStructFunctions
{
    public static string Describe(string name, DateTime moment = default)
    {
        return name + "@" + moment.Year;
    }

    public static string Label(string name, TimeSpan window = default)
    {
        return name + "#" + window.Ticks;
    }
}

public class OptionalStructArgumentProjectionTests
{
    [Fact]
    public void OmittedOptionalDateTimeArgumentMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(OmittedOptionalDateTimeArgumentMatchesLinq));

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H25sOptionalStructFunctions.Describe(r.Name))
            .ToList();

        List<string> actual = db.Table<H25sOptionalStructRow>()
            .OrderBy(r => r.Id)
            .Select(r => H25sOptionalStructFunctions.Describe(r.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OmittedOptionalTimeSpanArgumentMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(OmittedOptionalTimeSpanArgumentMatchesLinq));

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => H25sOptionalStructFunctions.Label(r.Name))
            .ToList();

        List<string> actual = db.Table<H25sOptionalStructRow>()
            .OrderBy(r => r.Id)
            .Select(r => H25sOptionalStructFunctions.Label(r.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25sOptionalStructRow> Rows()
    {
        return
        [
            new H25sOptionalStructRow { Id = 1, Name = "a" },
            new H25sOptionalStructRow { Id = 2, Name = "bc" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25sOptionalStructRow>().Schema.CreateTable();
        db.Table<H25sOptionalStructRow>().AddRange(Rows());
        return db;
    }
}
