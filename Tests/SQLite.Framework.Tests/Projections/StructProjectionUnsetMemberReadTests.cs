using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23bStructBoxRows")]
public class H23bStructBoxRow
{
    [Key]
    public int Id { get; set; }

    public bool Flag { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public struct H23bStructBox
{
    public int First { get; set; }

    public int Second { get; set; }
}

public class StructProjectionUnsetMemberReadTests
{
    [Fact]
    public void UnsetMemberOfAConditionallyBuiltStructReadsTheStructDefault()
    {
        using TestDatabase db = Setup(nameof(UnsetMemberOfAConditionallyBuiltStructReadsTheStructDefault));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H23bStructBox { First = r.A } : new H23bStructBox { First = r.B }).Second)
            .ToList();

        List<int> actual = db.Table<H23bStructBoxRow>().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H23bStructBox { First = r.A } : new H23bStructBox { First = r.B }).Second)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SetMemberOfAConditionallyBuiltStructReadsItsBranchValue()
    {
        using TestDatabase db = Setup(nameof(SetMemberOfAConditionallyBuiltStructReadsItsBranchValue));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H23bStructBox { First = r.A } : new H23bStructBox { First = r.B }).First)
            .ToList();

        List<int> actual = db.Table<H23bStructBoxRow>().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H23bStructBox { First = r.A } : new H23bStructBox { First = r.B }).First)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnsetMemberOfANestedStructReadsTheStructDefault()
    {
        using TestDatabase db = Setup(nameof(UnsetMemberOfANestedStructReadsTheStructDefault));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new H23bStructBox { First = r.A } })
            .Select(x => x.N.Second)
            .ToList();

        List<int> actual = db.Table<H23bStructBoxRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, N = new H23bStructBox { First = r.A } })
            .Select(x => x.N.Second)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23bStructBoxRow> Rows()
    {
        return
        [
            new H23bStructBoxRow { Id = 1, Flag = true, A = 5, B = 7 },
            new H23bStructBoxRow { Id = 2, Flag = false, A = 11, B = 13 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23bStructBoxRow>().Schema.CreateTable();
        db.Table<H23bStructBoxRow>().AddRange(Rows());
        return db;
    }
}
