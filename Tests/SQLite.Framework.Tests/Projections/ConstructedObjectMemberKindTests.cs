using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("MemberKindRows")]
public class MemberKindRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class MemberKindSide
{
    public int X { get; set; }

    public int Doubled => X * 2;

    public int Extra;
}

public class ConstructedObjectMemberKindTests
{
    [Fact]
    public void ReadingAGetOnlyMemberOffAConstructedObjectIsEvaluated()
    {
        using TestDatabase db = Setup(nameof(ReadingAGetOnlyMemberOffAConstructedObjectIsEvaluated));

        Assert.ThrowsAny<Exception>(() => db.Table<MemberKindRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new MemberKindSide { X = r.A } })
            .Select(x => x.N.Doubled)
            .ToList());
    }

    [Fact]
    public void ReadingAFieldOffAConstructedObjectMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(ReadingAFieldOffAConstructedObjectMatchesLinq));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new MemberKindSide { X = r.A } })
            .Select(x => x.N.Extra)
            .ToList();

        List<int> actual = db.Table<MemberKindRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new MemberKindSide { X = r.A } })
            .Select(x => x.N.Extra)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<MemberKindRow> Rows()
    {
        return
        [
            new MemberKindRow { Id = 1, A = 10 },
            new MemberKindRow { Id = 2, A = 20 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<MemberKindRow>().Schema.CreateTable();
        db.Table<MemberKindRow>().AddRange(Rows());
        return db;
    }
}
