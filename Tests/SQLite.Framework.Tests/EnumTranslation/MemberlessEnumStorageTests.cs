using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H21cBlankMode
{
}

[Table("H21cBlankRows")]
public class H21cBlankRow
{
    [Key]
    public int Id { get; set; }

    public H21cBlankMode Mode { get; set; }
}

public class MemberlessEnumStorageTests
{
    [Fact]
    public void NumericCastUnderTextStorageMatchesDotNet()
    {
        using TestDatabase db = new(b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<H21cBlankRow>().Schema.CreateTable();
        List<H21cBlankRow> rows = Rows();
        db.Table<H21cBlankRow>().AddRange(rows);

        List<int> expected = rows.OrderBy(r => r.Id).Select(r => (int)r.Mode).ToList();
        List<int> actual = db.Table<H21cBlankRow>().OrderBy(x => x.Id).Select(x => (int)x.Mode).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToStringUnderIntegerStorageMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<H21cBlankRow>().Schema.CreateTable();
        List<H21cBlankRow> rows = Rows();
        db.Table<H21cBlankRow>().AddRange(rows);

        List<string> expected = rows.OrderBy(r => r.Id).Select(r => r.Mode.ToString()).ToList();
        List<string> actual = db.Table<H21cBlankRow>().OrderBy(x => x.Id).Select(x => x.Mode.ToString()).ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H21cBlankRow> Rows()
    {
        return
        [
            new H21cBlankRow { Id = 1, Mode = default },
            new H21cBlankRow { Id = 2, Mode = (H21cBlankMode)3 },
        ];
    }
}
