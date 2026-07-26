using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
public enum H22gCoalescePerm
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
}

[Table("H22gCoalesceComputedRows")]
public class H22gCoalesceComputedRow
{
    [Key]
    public int Id { get; set; }

    public H22gCoalescePerm? Perms { get; set; }

    public int PermsNumber { get; set; }
}

[Table("H22gCoalesceCheckRows")]
public class H22gCoalesceCheckRow
{
    [Key]
    public int Id { get; set; }

    public H22gCoalescePerm? Perms { get; set; }
}

public class EnumTextStorageCoalescedOperandSchemaTests
{
    [Fact]
    public void ComputedColumnOverACoalescedEnumCastMatchesDotNet()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<H22gCoalesceComputedRow>().Computed(r => r.PermsNumber, r => (int)(r.Perms ?? H22gCoalescePerm.Read)),
            b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<H22gCoalesceComputedRow>().Schema.CreateTable();

        List<H22gCoalesceComputedRow> rows = ComputedRows();
        db.Table<H22gCoalesceComputedRow>().AddRange(rows);

        List<int> expected = rows.OrderBy(r => r.Id).Select(r => (int)(r.Perms ?? H22gCoalescePerm.Read)).ToList();

        List<int> actual = db.Table<H22gCoalesceComputedRow>().OrderBy(r => r.Id).Select(r => r.PermsNumber).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CheckConstraintOverACoalescedEnumCastRejectsHighValues()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<H22gCoalesceCheckRow>().Check(r => (int)(r.Perms ?? H22gCoalescePerm.Read) < 4, name: "CK_H22gCoalescePerms"),
            b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<H22gCoalesceCheckRow>().Schema.CreateTable();

        db.Table<H22gCoalesceCheckRow>().Add(new H22gCoalesceCheckRow { Id = 1, Perms = H22gCoalescePerm.Write });
        db.Table<H22gCoalesceCheckRow>().Add(new H22gCoalesceCheckRow { Id = 2, Perms = null });
        Assert.ThrowsAny<Exception>(() =>
            db.Table<H22gCoalesceCheckRow>().Add(new H22gCoalesceCheckRow { Id = 3, Perms = H22gCoalescePerm.Execute }));

        Assert.Equal(2, db.Table<H22gCoalesceCheckRow>().Count());
    }

    private static List<H22gCoalesceComputedRow> ComputedRows()
    {
        return
        [
            new H22gCoalesceComputedRow { Id = 1, Perms = H22gCoalescePerm.Write },
            new H22gCoalesceComputedRow { Id = 2, Perms = null },
            new H22gCoalesceComputedRow { Id = 3, Perms = (H22gCoalescePerm)9 }
        ];
    }
}
