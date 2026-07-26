using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H22gBlankFormatMode
{
}

public enum H22gBlankFormatUnsignedMode : ulong
{
}

[Table("H22gBlankFormatRows")]
public class H22gBlankFormatRow
{
    [Key]
    public int Id { get; set; }

    public H22gBlankFormatMode Mode { get; set; }
}

[Table("H22gBlankFormatUnsignedRows")]
public class H22gBlankFormatUnsignedRow
{
    [Key]
    public int Id { get; set; }

    public H22gBlankFormatUnsignedMode Mode { get; set; }
}

public class MemberlessEnumNumericFormatTextStorageTests
{
    [Fact]
    public void NumberFormatProjectionUnderTextStorageMatchesDotNet()
    {
        using TestDatabase db = Seed();
        List<H22gBlankFormatRow> rows = Rows();

        List<string> expected = rows.OrderBy(r => r.Id).Select(r => r.Mode.ToString("D")).ToList();

        List<string> actual = db.Table<H22gBlankFormatRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Mode.ToString("D"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NumberFormatFilterUnderTextStorageMatchesDotNet()
    {
        using TestDatabase db = Seed();
        List<H22gBlankFormatRow> rows = Rows();

        List<int> expected = rows
            .Where(r => r.Mode.ToString("D") == "3")
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<H22gBlankFormatRow>()
            .Where(r => r.Mode.ToString("D") == "3")
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NumberFormatOverAnUnsignedMemberlessEnumUnderTextStorageMatchesDotNet()
    {
        using TestDatabase db = SeedUnsigned();
        List<H22gBlankFormatUnsignedRow> rows = UnsignedRows();

        List<string> expected = rows.OrderBy(r => r.Id).Select(r => r.Mode.ToString("D")).ToList();

        List<string> actual = db.Table<H22gBlankFormatUnsignedRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Mode.ToString("D"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22gBlankFormatRow> Rows()
    {
        return
        [
            new H22gBlankFormatRow { Id = 1, Mode = default },
            new H22gBlankFormatRow { Id = 2, Mode = (H22gBlankFormatMode)3 }
        ];
    }

    private static List<H22gBlankFormatUnsignedRow> UnsignedRows()
    {
        return
        [
            new H22gBlankFormatUnsignedRow { Id = 1, Mode = default },
            new H22gBlankFormatUnsignedRow { Id = 2, Mode = (H22gBlankFormatUnsignedMode)ulong.MaxValue }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<H22gBlankFormatRow>().Schema.CreateTable();
        db.Table<H22gBlankFormatRow>().AddRange(Rows());
        return db;
    }

    private static TestDatabase SeedUnsigned()
    {
        TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Table<H22gBlankFormatUnsignedRow>().Schema.CreateTable();
        db.Table<H22gBlankFormatUnsignedRow>().AddRange(UnsignedRows());
        return db;
    }
}
