using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24nSetOpSourceRows")]
public class H24nSetOpSourceRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Nickname { get; set; }
}

[Table("H24nSetOpTargetRows")]
public class H24nSetOpTargetRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Nickname { get; set; }
}

public class InsertFromQuerySetOperationColumnAlignmentTests
{
    [Fact]
    public void ConcatBranchesWithDifferentMemberSetsThrowNotSupported()
    {
        using TestDatabase db = Setup(nameof(ConcatBranchesWithDifferentMemberSetsThrowNotSupported));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            db.Table<H24nSetOpTargetRow>().InsertFromQuery(
                db.Table<H24nSetOpSourceRow>()
                    .Where(s => s.Id <= 2)
                    .Select(s => new H24nSetOpTargetRow { Id = s.Id, Name = s.Name })
                    .Concat(db.Table<H24nSetOpSourceRow>()
                        .Where(s => s.Id > 2)
                        .Select(s => new H24nSetOpTargetRow { Id = s.Id, Nickname = s.Nickname }))));

        Assert.Contains("same members in the same order", exception.Message);
    }

    [Fact]
    public void ConcatBranchesWithDifferentMemberCountsThrowNotSupported()
    {
        using TestDatabase db = Setup(nameof(ConcatBranchesWithDifferentMemberCountsThrowNotSupported));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            db.Table<H24nSetOpTargetRow>().InsertFromQuery(
                db.Table<H24nSetOpSourceRow>()
                    .Where(s => s.Id <= 2)
                    .Select(s => new H24nSetOpTargetRow { Id = s.Id, Name = s.Name })
                    .Concat(db.Table<H24nSetOpSourceRow>()
                        .Where(s => s.Id > 2)
                        .Select(s => new H24nSetOpTargetRow { Id = s.Id }))));

        Assert.Contains("same members in the same order", exception.Message);
    }

    [Fact]
    public void ConcatBranchesWithTheSameMemberSetsWriteEachValueIntoItsOwnColumn()
    {
        using TestDatabase db = Setup(nameof(ConcatBranchesWithTheSameMemberSetsWriteEachValueIntoItsOwnColumn));

        db.Table<H24nSetOpTargetRow>().InsertFromQuery(
            db.Table<H24nSetOpSourceRow>()
                .Where(s => s.Id <= 2)
                .Select(s => new H24nSetOpTargetRow { Id = s.Id, Name = s.Name })
                .Concat(db.Table<H24nSetOpSourceRow>()
                    .Where(s => s.Id > 2)
                    .Select(s => new H24nSetOpTargetRow { Id = s.Id, Name = s.Nickname })));

        List<(int Id, string? Name, string? Nickname)> expected = Rows()
            .Where(s => s.Id <= 2)
            .Select(s => new H24nSetOpTargetRow { Id = s.Id, Name = s.Name })
            .Concat(Rows()
                .Where(s => s.Id > 2)
                .Select(s => new H24nSetOpTargetRow { Id = s.Id, Name = s.Nickname }))
            .OrderBy(t => t.Id)
            .Select(t => (t.Id, t.Name, t.Nickname))
            .ToList();

        List<(int Id, string? Name, string? Nickname)> actual = db.Table<H24nSetOpTargetRow>()
            .OrderBy(t => t.Id)
            .ToList()
            .Select(t => (t.Id, t.Name, t.Nickname))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24nSetOpSourceRow> Rows()
    {
        return
        [
            new H24nSetOpSourceRow { Id = 1, Name = "alpha", Nickname = "a" },
            new H24nSetOpSourceRow { Id = 2, Name = "beta", Nickname = "b" },
            new H24nSetOpSourceRow { Id = 3, Name = "gamma", Nickname = "g" },
            new H24nSetOpSourceRow { Id = 4, Name = "delta", Nickname = "d" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24nSetOpSourceRow>().Schema.CreateTable();
        db.Table<H24nSetOpTargetRow>().Schema.CreateTable();
        db.Table<H24nSetOpSourceRow>().AddRange(Rows());
        return db;
    }
}
