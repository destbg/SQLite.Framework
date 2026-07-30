using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25bRenameNullRows")]
public class H25bRenameNullRow
{
    [Key]
    public int Id { get; set; }

    public string? Note { get; set; }

    public int? Amount { get; set; }
}

public class H25bRenameNullChild
{
    public string? Note { get; set; }

    public int? Amount { get; set; }
}

public class H25bRenameNullOuter
{
    public int Id { get; set; }

    public H25bRenameNullChild Child { get; set; } = null!;
}

public class RenamedNestedObjectNullColumnTests
{
    [Fact]
    public void ANestedObjectCarriedUnderANewNameStaysNonNullWhenEveryColumnIsNull()
    {
        using TestDatabase db = Setup(nameof(ANestedObjectCarriedUnderANewNameStaysNonNullWhenEveryColumnIsNull));

        List<bool> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H25bRenameNullOuter { Id = r.Id, Child = new H25bRenameNullChild { Note = r.Note, Amount = r.Amount } })
            .Select(o => new { W = o.Child })
            .Select(x => x.W != null)
            .ToList();

        Assert.Equal(new List<bool> { true, true }, expected);

        List<bool> actual = db.Table<H25bRenameNullRow>().OrderBy(r => r.Id)
            .Select(r => new H25bRenameNullOuter { Id = r.Id, Child = new H25bRenameNullChild { Note = r.Note, Amount = r.Amount } })
            .Select(o => new { W = o.Child })
            .AsEnumerable()
            .Select(x => x.W != null)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H25bRenameNullRow> Rows()
    {
        return
        [
            new H25bRenameNullRow { Id = 1, Note = null, Amount = null },
            new H25bRenameNullRow { Id = 2, Note = "x", Amount = 5 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25bRenameNullRow>().Schema.CreateTable();
        db.Table<H25bRenameNullRow>().AddRange(Rows());
        return db;
    }
}
