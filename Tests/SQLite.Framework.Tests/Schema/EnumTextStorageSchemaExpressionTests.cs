using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Flags]
public enum H21cSchemaPerm
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
}

[Table("H21cSchemaComputedRows")]
public class H21cSchemaComputedRow
{
    [Key]
    public int Id { get; set; }

    public H21cSchemaPerm Perms { get; set; }

    public int PermsNumber { get; set; }
}

[Table("H21cSchemaCheckRows")]
public class H21cSchemaCheckRow
{
    [Key]
    public int Id { get; set; }

    public H21cSchemaPerm Perms { get; set; }
}

[Table("H21cSchemaIndexRows")]
public class H21cSchemaIndexRow
{
    [Key]
    public int Id { get; set; }

    public H21cSchemaPerm Perms { get; set; }

    public string Name { get; set; } = "";
}

public class EnumTextStorageSchemaExpressionTests
{
    [Fact]
    public void ComputedColumnOverEnumCastMatchesDotNet()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<H21cSchemaComputedRow>().Computed(r => r.PermsNumber, r => (int)r.Perms),
            b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<H21cSchemaComputedRow>().Schema.CreateTable();

        List<H21cSchemaComputedRow> rows =
        [
            new H21cSchemaComputedRow { Id = 1, Perms = H21cSchemaPerm.Read },
            new H21cSchemaComputedRow { Id = 2, Perms = H21cSchemaPerm.Read | H21cSchemaPerm.Write },
            new H21cSchemaComputedRow { Id = 3, Perms = H21cSchemaPerm.None },
            new H21cSchemaComputedRow { Id = 4, Perms = (H21cSchemaPerm)9 },
        ];
        db.Table<H21cSchemaComputedRow>().AddRange(rows);

        List<int> expected = rows.OrderBy(r => r.Id).Select(r => (int)r.Perms).ToList();
        List<int> actual = db.Table<H21cSchemaComputedRow>().OrderBy(r => r.Id).Select(r => r.PermsNumber).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CheckConstraintOverEnumCastRejectsHighValues()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<H21cSchemaCheckRow>().Check(r => (int)r.Perms < 4, name: "CK_H21cSchemaPerms"),
            b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<H21cSchemaCheckRow>().Schema.CreateTable();

        db.Table<H21cSchemaCheckRow>().Add(new H21cSchemaCheckRow { Id = 1, Perms = H21cSchemaPerm.Read | H21cSchemaPerm.Write });
        Assert.ThrowsAny<Exception>(() =>
            db.Table<H21cSchemaCheckRow>().Add(new H21cSchemaCheckRow { Id = 2, Perms = H21cSchemaPerm.Execute }));

        Assert.Equal(1, db.Table<H21cSchemaCheckRow>().Count());
    }

    [Fact]
    public void PartialUniqueIndexFilteredByEnumCastIsEnforced()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<H21cSchemaIndexRow>().Index(r => r.Name, name: "ix_h21c_schema_name", unique: true, filter: r => (int)r.Perms > 1),
            b => b.EnumStorage = EnumStorageMode.Text);
        db.Table<H21cSchemaIndexRow>().Schema.CreateTable();

        db.Table<H21cSchemaIndexRow>().Add(new H21cSchemaIndexRow { Id = 1, Perms = H21cSchemaPerm.Write, Name = "x" });
        db.Table<H21cSchemaIndexRow>().Add(new H21cSchemaIndexRow { Id = 2, Perms = H21cSchemaPerm.Read, Name = "x" });
        Assert.ThrowsAny<Exception>(() =>
            db.Table<H21cSchemaIndexRow>().Add(new H21cSchemaIndexRow { Id = 3, Perms = H21cSchemaPerm.Execute, Name = "x" }));

        Assert.Equal(2, db.Table<H21cSchemaIndexRow>().Count());
    }
}
