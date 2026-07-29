using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24bIdentityRows")]
public class H24bIdentityRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class IdentitySelectAfterNestedProjectionTests
{
    [Fact]
    public void IdentitySelectAfterANestedAnonymousProjectionKeepsTheRows()
    {
        using TestDatabase db = Setup(nameof(IdentitySelectAfterANestedAnonymousProjectionKeepsTheRows));

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Inner = new { r.A } })
            .Select(x => x)
            .Select(x => x.Inner.A)
            .ToList();

        List<int> actual = db.Table<H24bIdentityRow>().OrderBy(r => r.Id)
            .Select(r => new { r.Id, Inner = new { r.A } })
            .Select(x => x)
            .AsEnumerable()
            .Select(x => x.Inner.A)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24bIdentityRow> Rows()
    {
        return
        [
            new H24bIdentityRow { Id = 1, A = 5 },
            new H24bIdentityRow { Id = 2, A = 6 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(methodName);
        db.Table<H24bIdentityRow>().Schema.CreateTable();
        db.Table<H24bIdentityRow>().AddRange(Rows());
        return db;
    }
}
