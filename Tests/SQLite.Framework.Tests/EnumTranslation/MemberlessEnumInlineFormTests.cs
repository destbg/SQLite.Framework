using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum BlankInlineMode
{
}

public enum BlankUnsignedMode : ulong
{
}

[Table("BlankInlineRows")]
public class BlankInlineRow
{
    [Key]
    public int Id { get; set; }

    public BlankInlineMode Mode { get; set; }

    public string ModeName { get; set; } = "";

    public int ModeNumber { get; set; }
}

[Table("BlankUnsignedRows")]
public class BlankUnsignedRow
{
    [Key]
    public int Id { get; set; }

    public BlankUnsignedMode Mode { get; set; }
}

public class MemberlessEnumInlineFormTests
{
    [Fact]
    public void ToStringOverAnUnsignedMemberlessEnumMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<BlankUnsignedRow>().Schema.CreateTable();
        List<BlankUnsignedRow> rows =
        [
            new BlankUnsignedRow { Id = 1, Mode = default },
            new BlankUnsignedRow { Id = 2, Mode = (BlankUnsignedMode)ulong.MaxValue }
        ];
        db.Table<BlankUnsignedRow>().AddRange(rows);

        List<string> expected = rows.OrderBy(r => r.Id).Select(r => r.Mode.ToString()).ToList();
        List<string> actual = db.Table<BlankUnsignedRow>().OrderBy(r => r.Id)
            .Select(r => r.Mode.ToString()).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComputedNameOverAMemberlessEnumMatchesDotNet()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<BlankInlineRow>().Computed(r => r.ModeName, r => r.Mode.ToString()));
        db.Table<BlankInlineRow>().Schema.CreateTable();
        List<BlankInlineRow> rows = Rows();
        db.Table<BlankInlineRow>().AddRange(rows);

        List<string> expected = rows.OrderBy(r => r.Id).Select(r => r.Mode.ToString()).ToList();
        List<string> actual = db.Table<BlankInlineRow>().OrderBy(r => r.Id)
            .Select(r => r.ModeName).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComputedNumberOverAMemberlessEnumTextStorageMatchesDotNet()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<BlankInlineRow>().Computed(r => r.ModeNumber, r => (int)r.Mode),
            b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<BlankInlineRow>().Schema.CreateTable();
        List<BlankInlineRow> rows = Rows();
        db.Table<BlankInlineRow>().AddRange(rows);

        List<int> expected = rows.OrderBy(r => r.Id).Select(r => (int)r.Mode).ToList();
        List<int> actual = db.Table<BlankInlineRow>().OrderBy(r => r.Id)
            .Select(r => r.ModeNumber).ToList();

        Assert.Equal(expected, actual);
    }

    private static List<BlankInlineRow> Rows()
    {
        return
        [
            new BlankInlineRow { Id = 1, Mode = default },
            new BlankInlineRow { Id = 2, Mode = (BlankInlineMode)7 }
        ];
    }
}
