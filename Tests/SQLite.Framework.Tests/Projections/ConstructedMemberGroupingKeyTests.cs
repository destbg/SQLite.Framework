using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24bKeyRows")]
public class H24bKeyRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public record H24bKeyInner(int A, int B);

public class H24bKeyOuter
{
    public int Id { get; set; }

    public H24bKeyInner? Inner { get; set; }
}

public class ConstructedMemberGroupingKeyTests
{
    [Fact]
    public void GroupingByAConstructedMemberOfANamedOuterUsesEveryKeyPart()
    {
        using TestDatabase db = Setup(nameof(GroupingByAConstructedMemberOfANamedOuterUsesEveryKeyPart));

        List<int> expected = Rows()
            .Select(r => new H24bKeyOuter { Id = r.Id, Inner = new H24bKeyInner(r.A, r.B) })
            .GroupBy(x => x.Inner)
            .Select(g => g.Count())
            .OrderBy(c => c)
            .ToList();

        List<int> actual = db.Table<H24bKeyRow>()
            .Select(r => new H24bKeyOuter { Id = r.Id, Inner = new H24bKeyInner(r.A, r.B) })
            .GroupBy(x => x.Inner)
            .Select(g => g.Count())
            .AsEnumerable()
            .OrderBy(c => c)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupingByAConstructedMemberOfAnAnonymousOuterUsesEveryKeyPart()
    {
        using TestDatabase db = Setup(nameof(GroupingByAConstructedMemberOfAnAnonymousOuterUsesEveryKeyPart));

        List<int> expected = Rows()
            .Select(r => new { r.Id, Inner = new H24bKeyInner(r.A, r.B) })
            .GroupBy(x => x.Inner)
            .Select(g => g.Count())
            .OrderBy(c => c)
            .ToList();

        List<int> actual = db.Table<H24bKeyRow>()
            .Select(r => new { r.Id, Inner = new H24bKeyInner(r.A, r.B) })
            .GroupBy(x => x.Inner)
            .Select(g => g.Count())
            .AsEnumerable()
            .OrderBy(c => c)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24bKeyRow> Rows()
    {
        return
        [
            new H24bKeyRow { Id = 1, A = 1, B = 1 },
            new H24bKeyRow { Id = 2, A = 1, B = 2 },
            new H24bKeyRow { Id = 3, A = 2, B = 1 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(methodName);
        db.Table<H24bKeyRow>().Schema.CreateTable();
        db.Table<H24bKeyRow>().AddRange(Rows());
        return db;
    }
}
