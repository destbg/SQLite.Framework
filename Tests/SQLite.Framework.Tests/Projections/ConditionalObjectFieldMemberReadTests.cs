using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CondFieldRows")]
public class CondFieldRow
{
    [Key]
    public int Id { get; set; }

    public bool Flag { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class CondFieldBox
{
    public int First { get; set; }

    public int Loose;
}

public class ConditionalObjectFieldMemberReadTests
{
    [Fact]
    public void FieldMemberOfConditionalBranchesReadsTheClrDefault()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new CondFieldBox { First = r.A } : new CondFieldBox { First = r.B }).Loose)
            .ToList();

        List<int> actual = db.Table<CondFieldRow>()
            .OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new CondFieldBox { First = r.A } : new CondFieldBox { First = r.B }).Loose)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<CondFieldRow> Rows()
    {
        return
        [
            new CondFieldRow { Id = 1, Flag = true, A = 5, B = 7 },
            new CondFieldRow { Id = 2, Flag = false, A = 11, B = 13 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<CondFieldRow>().Schema.CreateTable();
        db.Table<CondFieldRow>().AddRange(Rows());
        return db;
    }
}
