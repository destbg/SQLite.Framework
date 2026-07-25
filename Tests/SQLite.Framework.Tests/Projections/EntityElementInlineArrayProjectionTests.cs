using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20ArrEntityElement")]
public class H20ArrEntityElementRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class EntityElementInlineArrayProjectionTests
{
    private static List<H20ArrEntityElementRow> Rows() =>
    [
        new H20ArrEntityElementRow { Id = 1, A = 10 },
        new H20ArrEntityElementRow { Id = 2, A = 20 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H20ArrEntityElementRow>().Schema.CreateTable();
        db.Table<H20ArrEntityElementRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void TopLevelArrayOfEntitiesMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .Select(a => a[0].A).ToList();

        List<int> actual = db.Table<H20ArrEntityElementRow>()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .ToList()
            .Select(a => a[0].A).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MemberArrayOfEntitiesMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Arr = new[] { r } })
            .Select(x => x.Arr[0].A).ToList();

        List<int> actual = db.Table<H20ArrEntityElementRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Arr = new[] { r } })
            .ToList()
            .Select(x => x.Arr[0].A).ToList();

        Assert.Equal(expected, actual);
    }
}
