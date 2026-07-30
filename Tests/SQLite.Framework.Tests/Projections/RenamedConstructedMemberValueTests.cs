using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25bRenameRows")]
public class H25bRenameRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H25bRenameTenfold
{
    public H25bRenameTenfold(int value)
    {
        Value = value * 10;
    }

    public int Value { get; set; }
}

public class H25bRenameSeeded
{
    public H25bRenameSeeded(int seed)
    {
        Bumped = seed + 1;
    }

    public int Bumped { get; set; }
}

public class RenamedConstructedMemberValueTests
{
    [Fact]
    public void CarryingAConstructedMemberUnderANewNameKeepsTheConstructorValue()
    {
        using TestDatabase db = Setup(nameof(CarryingAConstructedMemberUnderANewNameKeepsTheConstructorValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { First = new H25bRenameTenfold(r.A) })
            .Select(x => new { W = x.First })
            .Select(x => x.W.Value)
            .ToList();

        List<int> actual = db.Table<H25bRenameRow>().OrderBy(r => r.Id)
            .Select(r => new { First = new H25bRenameTenfold(r.A) })
            .Select(x => new { W = x.First })
            .Select(x => x.W.Value)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CarryingAConstructedMemberWhoseParameterNameDiffersKeepsTheConstructorValue()
    {
        using TestDatabase db = Setup(nameof(CarryingAConstructedMemberWhoseParameterNameDiffersKeepsTheConstructorValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { First = new H25bRenameSeeded(r.A) })
            .Select(x => new { W = x.First })
            .Select(x => x.W.Bumped)
            .ToList();

        List<int> actual = db.Table<H25bRenameRow>().OrderBy(r => r.Id)
            .Select(r => new { First = new H25bRenameSeeded(r.A) })
            .Select(x => new { W = x.First })
            .Select(x => x.W.Bumped)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25bRenameRow> Rows()
    {
        return
        [
            new H25bRenameRow { Id = 1, A = 3 },
            new H25bRenameRow { Id = 2, A = 7 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25bRenameRow>().Schema.CreateTable();
        db.Table<H25bRenameRow>().AddRange(Rows());
        return db;
    }
}
