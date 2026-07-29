using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24oNonGenericRows")]
public class H24oNonGenericRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public bool IsDeleted { get; set; }
}

public class QueryFilterNonGenericTableTests
{
    [Fact]
    public void NonGenericTableReadAppliesTheRegisteredFilter()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<H24oNonGenericRow>(r => !r.IsDeleted));
        db.Table<H24oNonGenericRow>().Schema.CreateTable();
        List<H24oNonGenericRow> rows = Rows();
        db.Table<H24oNonGenericRow>().AddRange(rows);

        List<string> expected = rows.Where(r => !r.IsDeleted).Select(r => r.Name).OrderBy(n => n).ToList();

        List<string> actual = [];
        foreach (H24oNonGenericRow row in db.Table(typeof(H24oNonGenericRow)))
        {
            actual.Add(row.Name);
        }

        Assert.Equal(expected, actual.OrderBy(n => n).ToList());
    }

    private static List<H24oNonGenericRow> Rows()
    {
        return
        [
            new H24oNonGenericRow { Id = 1, Name = "live", IsDeleted = false },
            new H24oNonGenericRow { Id = 2, Name = "gone", IsDeleted = true }
        ];
    }
}
