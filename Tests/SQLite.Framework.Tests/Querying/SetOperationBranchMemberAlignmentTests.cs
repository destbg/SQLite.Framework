using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25oAlignLeftRows")]
public class H25oAlignLeftRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }
}

[Table("H25oAlignRightRows")]
public class H25oAlignRightRow
{
    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";
}

public class H25oAlignPair
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Note { get; set; }
}

public class SetOperationBranchMemberAlignmentTests
{
    [Fact]
    public void ConcatOfWholeEntityBranchesKeepsEveryRow()
    {
        using TestDatabase db = Setup(nameof(ConcatOfWholeEntityBranchesKeepsEveryRow));

        List<int> expected = Lefts()
            .Concat(Lefts())
            .Select(l => l.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H25oAlignLeftRow>()
            .Concat(db.Table<H25oAlignLeftRow>())
            .ToList()
            .Select(l => l.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatOfBranchesThatSetDifferentMembersKeepsEachValueInItsOwnMember()
    {
        using TestDatabase db = Setup(nameof(ConcatOfBranchesThatSetDifferentMembersKeepsEachValueInItsOwnMember));

        List<(int Id, string? Name, string? Note)> expected = Lefts()
            .Select(l => new H25oAlignPair { Id = l.Id, Name = l.Name })
            .Concat(Rights().Select(r => new H25oAlignPair { Id = r.Id, Note = r.Note }))
            .Select(p => (p.Id, p.Name, p.Note))
            .OrderBy(p => p.Id)
            .ToList();

        AssertMatchesOrIsRefused(expected, () => db.Table<H25oAlignLeftRow>()
            .Select(l => new H25oAlignPair { Id = l.Id, Name = l.Name })
            .Concat(db.Table<H25oAlignRightRow>().Select(r => new H25oAlignPair { Id = r.Id, Note = r.Note }))
            .ToList()
            .Select(p => (p.Id, p.Name, p.Note))
            .OrderBy(p => p.Id)
            .ToList());
    }

    [Fact]
    public void UnionOfBranchesThatSetDifferentMembersKeepsEachValueInItsOwnMember()
    {
        using TestDatabase db = Setup(nameof(UnionOfBranchesThatSetDifferentMembersKeepsEachValueInItsOwnMember));

        List<(int Id, string? Name, string? Note)> expected = Lefts()
            .Select(l => new H25oAlignPair { Id = l.Id, Name = l.Name })
            .Select(p => (p.Id, p.Name, p.Note))
            .Union(Rights()
                .Select(r => new H25oAlignPair { Id = r.Id, Note = r.Note })
                .Select(p => (p.Id, p.Name, p.Note)))
            .OrderBy(p => p.Id)
            .ToList();

        AssertMatchesOrIsRefused(expected, () => db.Table<H25oAlignLeftRow>()
            .Select(l => new H25oAlignPair { Id = l.Id, Name = l.Name })
            .Union(db.Table<H25oAlignRightRow>().Select(r => new H25oAlignPair { Id = r.Id, Note = r.Note }))
            .ToList()
            .Select(p => (p.Id, p.Name, p.Note))
            .OrderBy(p => p.Id)
            .ToList());
    }

    [Fact]
    public void ConcatOfAWholeRowBranchAndAPartialBranchDoesNotLeakARawSqlError()
    {
        using TestDatabase db = Setup(nameof(ConcatOfAWholeRowBranchAndAPartialBranchDoesNotLeakARawSqlError));

        List<(int Id, string? Name)> expected = Lefts()
            .Concat(Lefts().Select(l => new H25oAlignLeftRow { Id = l.Id }))
            .Select(l => (l.Id, l.Name))
            .OrderBy(l => l.Id)
            .ThenBy(l => l.Name)
            .ToList();

        AssertMatchesOrIsRefused(expected, () => db.Table<H25oAlignLeftRow>()
            .Concat(db.Table<H25oAlignLeftRow>().Select(l => new H25oAlignLeftRow { Id = l.Id }))
            .ToList()
            .Select(l => (l.Id, l.Name))
            .OrderBy(l => l.Id)
            .ThenBy(l => l.Name)
            .ToList());
    }

    private static void AssertMatchesOrIsRefused<T>(List<T> expected, Func<List<T>> run)
    {
        List<T> actual;
        try
        {
            actual = run();
        }
        catch (NotSupportedException)
        {
            return;
        }

        Assert.Equal(expected, actual);
    }

    private static List<H25oAlignLeftRow> Lefts()
    {
        return
        [
            new H25oAlignLeftRow { Id = 1, Name = "alpha" },
            new H25oAlignLeftRow { Id = 2, Name = "beta" }
        ];
    }

    private static List<H25oAlignRightRow> Rights()
    {
        return
        [
            new H25oAlignRightRow { Id = 10, Note = "first" },
            new H25oAlignRightRow { Id = 11, Note = "second" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25oAlignLeftRow>().Schema.CreateTable();
        db.Table<H25oAlignRightRow>().Schema.CreateTable();
        db.Table<H25oAlignLeftRow>().AddRange(Lefts());
        db.Table<H25oAlignRightRow>().AddRange(Rights());
        return db;
    }
}
