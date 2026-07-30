using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25bCteCtorRows")]
public class H25bCteCtorRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H25bCteCtorSide
{
    public H25bCteCtorSide(int x)
    {
        X = x;
        Doubled = x * 2;
    }

    public int X { get; set; }

    public int Doubled { get; set; }
}

public class H25bCteCtorOuter
{
    public int Id { get; set; }

    public H25bCteCtorSide? Side { get; set; }
}

public class CteConstructorComputedMemberTests
{
    [Fact]
    public void AConstructorComputedNestedMemberKeepsItsValueThroughACommonTableExpression()
    {
        using TestDatabase db = Setup(nameof(AConstructorComputedNestedMemberKeepsItsValueThroughACommonTableExpression));

        List<int> expected = Rows()
            .Select(r => new H25bCteCtorOuter { Id = r.Id, Side = new H25bCteCtorSide(r.A) })
            .Select(x => x.Side!.Doubled)
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(new List<int> { 10, 40 }, expected);

        List<int> actual = db.With(() => db.Table<H25bCteCtorRow>()
                .Select(r => new H25bCteCtorOuter { Id = r.Id, Side = new H25bCteCtorSide(r.A) }))
            .Select(x => x.Side!.Doubled)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25bCteCtorRow> Rows()
    {
        return
        [
            new H25bCteCtorRow { Id = 1, A = 5 },
            new H25bCteCtorRow { Id = 2, A = 20 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25bCteCtorRow>().Schema.CreateTable();
        db.Table<H25bCteCtorRow>().AddRange(Rows());
        return db;
    }
}
