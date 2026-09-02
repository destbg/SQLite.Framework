using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26qConstructorMemberRows")]
public class H26qConstructorMemberRow
{
    [Key]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class H26qConstructorMemberChild
{
    public int Value { get; set; }

    public int Preset { get; set; } = 13;
}

public class H26qConstructorMemberOuter
{
    public H26qConstructorMemberOuter(H26qConstructorMemberChild child)
    {
        Child = child;
    }

    public H26qConstructorMemberChild Child { get; }
}

public class ConstructorMemberBoundaryParityTests
{
    [Fact]
    public void AConstructorChildUsesItsPropertyNameAfterPagingAndDistinct()
    {
        using TestDatabase db = Setup(nameof(AConstructorChildUsesItsPropertyNameAfterPagingAndDistinct));

        List<int> expected = Rows()
            .Select(r => new H26qConstructorMemberOuter(new H26qConstructorMemberChild { Value = r.Value }))
            .Take(4)
            .Distinct()
            .Skip(1)
            .Where(o => o.Child.Value > 0)
            .Select(o => o.Child.Preset)
            .ToList();

        List<int> actual = db.Table<H26qConstructorMemberRow>()
            .Select(r => new H26qConstructorMemberOuter(new H26qConstructorMemberChild { Value = r.Value }))
            .Take(4)
            .Distinct()
            .Skip(1)
            .Where(o => o.Child.Value > 0)
            .Select(o => o.Child.Preset)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AConstructorChildUsesItsPropertyNameAfterASetBoundary()
    {
        using TestDatabase db = Setup(nameof(AConstructorChildUsesItsPropertyNameAfterASetBoundary));

        List<int> expected = Rows()
            .Take(2)
            .Select(r => new H26qConstructorMemberOuter(new H26qConstructorMemberChild { Value = r.Value }))
            .Union(Rows()
                .Skip(2)
                .Select(r => new H26qConstructorMemberOuter(new H26qConstructorMemberChild { Value = r.Value })))
            .Where(o => o.Child.Value > 0)
            .OrderBy(o => o.Child.Value)
            .Select(o => o.Child.Preset)
            .ToList();

        List<int> actual = db.Table<H26qConstructorMemberRow>()
            .Take(2)
            .Select(r => new H26qConstructorMemberOuter(new H26qConstructorMemberChild { Value = r.Value }))
            .Union(db.Table<H26qConstructorMemberRow>()
                .Skip(2)
                .Select(r => new H26qConstructorMemberOuter(new H26qConstructorMemberChild { Value = r.Value })))
            .Where(o => o.Child.Value > 0)
            .OrderBy(o => o.Child.Value)
            .Select(o => o.Child.Preset)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26qConstructorMemberRow> Rows()
    {
        return
        [
            new H26qConstructorMemberRow { Id = 1, Value = 0 },
            new H26qConstructorMemberRow { Id = 2, Value = 2 },
            new H26qConstructorMemberRow { Id = 3, Value = 4 },
            new H26qConstructorMemberRow { Id = 4, Value = 8 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26qConstructorMemberRow>().Schema.CreateTable();
        db.Table<H26qConstructorMemberRow>().AddRange(Rows());
        return db;
    }
}
