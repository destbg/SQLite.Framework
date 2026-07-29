using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24lNullableUnwrapRows")]
public class H24lNullableUnwrapRow
{
    [Key]
    public int Id { get; set; }

    public int Num { get; set; }
}

public class NullCapturedNullableUnwrapConstantParityTests
{
    [Fact]
    public void FiltersOnANullCapturedNullableUnwrappedToItsValueType()
    {
        int? threshold = MissingThreshold();
        using TestDatabase db = Seed();
        List<H24lNullableUnwrapRow> local = Rows();

        Assert.Throws<InvalidOperationException>(() => local
            .Where(r => r.Num == (int)threshold!)
            .Select(r => r.Id)
            .ToList());

        Assert.Throws<InvalidOperationException>(() => db.Table<H24lNullableUnwrapRow>()
            .Where(r => r.Num == (int)threshold!)
            .Select(r => r.Id)
            .ToList());
    }

    [Fact]
    public void ProjectsANullCapturedNullableUnwrappedToItsValueType()
    {
        int? threshold = MissingThreshold();
        using TestDatabase db = Seed();
        List<H24lNullableUnwrapRow> local = Rows();

        Assert.Throws<InvalidOperationException>(() => local
            .OrderBy(r => r.Id)
            .Select(r => r.Num + (int)threshold!)
            .ToList());

        Assert.Throws<InvalidOperationException>(() => db.Table<H24lNullableUnwrapRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Num + (int)threshold!)
            .ToList());
    }

    private static int? MissingThreshold()
    {
        return null;
    }

    private static List<H24lNullableUnwrapRow> Rows()
    {
        return
        [
            new H24lNullableUnwrapRow { Id = 1, Num = 1 },
            new H24lNullableUnwrapRow { Id = 2, Num = 2 }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<H24lNullableUnwrapRow>().Schema.CreateTable();
        db.Table<H24lNullableUnwrapRow>().AddRange(Rows());
        return db;
    }
}
