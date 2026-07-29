using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CmcsRows")]
public class CmcsRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class CmcsScaled
{
    public CmcsScaled(int value)
    {
        Value = value;
    }

    public int Value { get; }
}

public class CmcsOuter
{
    public CmcsScaled? First { get; set; }

    public CmcsScaled? Second { get; set; }
}

public class ConstructedMemberCarrySelectionTests
{
    [Fact]
    public void CarryingOneOfTwoConstructedMembersReadsItsValue()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new CmcsOuter { First = new CmcsScaled(r.A), Second = new CmcsScaled(r.Id) })
            .Select(x => new { W = x.First })
            .Select(x => x.W!.Value)
            .ToList();

        List<int> actual = db.Table<CmcsRow>().OrderBy(r => r.Id)
            .Select(r => new CmcsOuter { First = new CmcsScaled(r.A), Second = new CmcsScaled(r.Id) })
            .Select(x => new { W = x.First })
            .AsEnumerable()
            .Select(x => x.W!.Value)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CarryingAWholeRowThenOneMemberFiltersOnANestedConstructedValue()
    {
        using TestDatabase db = Seed();

        int expected = Rows()
            .Select(r => new CmcsOuter { First = new CmcsScaled(r.A), Second = new CmcsScaled(r.Id) })
            .Select(p => new { X = p })
            .Select(y => new { W = y.X })
            .Count(y => y.W.First!.Value > 3);

        int actual = db.Table<CmcsRow>()
            .Select(r => new CmcsOuter { First = new CmcsScaled(r.A), Second = new CmcsScaled(r.Id) })
            .Select(p => new { X = p })
            .Select(y => new { W = y.X })
            .Count(y => y.W.First!.Value > 3);

        Assert.Equal(expected, actual);
    }

    private static List<CmcsRow> Rows()
    {
        return
        [
            new CmcsRow { Id = 1, A = 3 },
            new CmcsRow { Id = 2, A = 7 }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<CmcsRow>().Schema.CreateTable();
        db.Table<CmcsRow>().AddRange(Rows());
        return db;
    }
}
