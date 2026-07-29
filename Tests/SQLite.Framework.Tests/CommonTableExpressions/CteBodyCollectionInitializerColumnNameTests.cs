using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24gTaggedRows")]
public class H24gTaggedRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H24gTaggedShape
{
    public int Id { get; set; }

    public List<string> Tags { get; } = [];
}

public class H24gTaggedInner
{
    public string Label { get; set; } = "";

    public List<string> Tags { get; } = [];
}

public class H24gTaggedOuter
{
    public int Id { get; set; }

    public H24gTaggedInner Inner { get; set; } = new();
}

public class CteBodyCollectionInitializerColumnNameTests
{
    [Fact]
    public void CteBodyCollectionInitializerOverRowMemberStillNamesTheOtherColumns()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new H24gTaggedShape { Id = r.Id, Tags = { r.Name } })
            .Select(s => s.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.With(() => db.Table<H24gTaggedRow>()
                .Select(r => new H24gTaggedShape { Id = r.Id, Tags = { r.Name } }))
            .Select(s => s.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteBodyNestedCollectionInitializerOverRowMemberStillNamesTheOtherColumns()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new H24gTaggedOuter
            {
                Id = r.Id,
                Inner = new H24gTaggedInner { Label = r.Name, Tags = { r.Name } }
            })
            .Select(s => s.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.With(() => db.Table<H24gTaggedRow>()
                .Select(r => new H24gTaggedOuter
                {
                    Id = r.Id,
                    Inner = new H24gTaggedInner { Label = r.Name, Tags = { r.Name } }
                }))
            .Select(s => s.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24gTaggedRow> Rows()
    {
        return
        [
            new H24gTaggedRow { Id = 1, Name = "alpha" },
            new H24gTaggedRow { Id = 2, Name = "beta" },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H24gTaggedRow>().Schema.CreateTable();
        db.Table<H24gTaggedRow>().AddRange(Rows());
        return db;
    }
}
